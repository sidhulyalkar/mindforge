from __future__ import annotations

from mindforge_neuro.encounter import analyze_encounter
from mindforge_neuro.markers import GameMarker


def marker(event: str, t: float, *, seq: int, value: float = 0.0, reason: str | None = None,
           target: str | None = None, phase: int = 1) -> GameMarker:
    return GameMarker(
        schema="mindforge.game_marker.v1",
        seq=seq,
        session_id="game-1",
        calibration_id="cal-1",
        event=event,
        category="combat_outcome",
        unity_realtime_s=t,
        game_time_s=t,
        frame=seq * 10,
        fixed_tick=seq * 20,
        reason=reason,
        target=target,
        value=value,
        boss_phase=phase,
    )


def test_encounter_report_summarizes_combat_pressure_and_signature_loop():
    events = [
        marker("PULSE_SHOT", 0.0, seq=1),
        marker("RIFT_CLEAVE", 12.0, seq=2),
        marker("RIFT_CLEAVE_HIT", 12.1, seq=3),
        marker("COUNTER_PULSE", 20.0, seq=4),
        marker("COUNTER_REFLECT", 20.1, seq=5),
        marker("NEAR_MISS", 31.0, seq=6),
        marker("PLAYER_DAMAGED", 45.0, seq=7, value=7.5, reason="LIGHT"),
        marker("PLAYER_DAMAGED", 60.0, seq=8, value=12.0, reason="HEAVY"),
        marker("BOSS_DAMAGED", 61.0, seq=9, value=18.0, reason="HEAVY"),
        marker("NEURAL_BUFF_APPLIED", 80.0, seq=10, target="sight"),
        marker("NEURAL_BUFF_APPLIED", 90.0, seq=11, target="guard"),
        marker("CONCORD_ESTABLISHED", 91.0, seq=12),
        marker("TWIN_ECLIPSE_CHARGE", 110.0, seq=13),
        marker("TWIN_ECLIPSE_RELEASE", 112.0, seq=14, value=4),
        marker("SIGNAL_BREAK", 130.0, seq=15),
        marker("FLUX_CHANGED", 150.0, seq=16, value=0.65),
        marker("VICTORY", 220.0, seq=17, phase=3),
    ]
    report = analyze_encounter(events)
    assert report.outcome == "VICTORY"
    assert report.source_duration_s == 220.0
    assert report.pulse_shots == 1
    assert report.rift_cleave_hit_rate == 1.0
    assert report.counter_success_rate == 1.0
    assert report.near_misses == 1
    assert report.player_damage_events == 2
    assert report.player_damage_total == 19.5
    assert report.player_heavy_hits == 1
    assert report.boss_damage_total == 18.0
    assert report.signal_breaks == 1
    assert report.twin_eclipse_releases == 1
    assert report.concord_established == 1
    assert report.sight_buffs == 1 and report.guard_buffs == 1
    assert report.final_flux == 0.65
    assert report.max_boss_phase == 3
    assert report.diagnostic_flags == ()


def test_encounter_report_flags_design_questions_without_inventing_a_fun_score():
    events = [
        marker("COUNTER_PULSE", 0.0, seq=1),
        marker("COUNTER_PULSE", 10.0, seq=2),
        marker("COUNTER_PULSE", 20.0, seq=3),
        marker("RIFT_CLEAVE", 25.0, seq=4),
        marker("RIFT_CLEAVE", 30.0, seq=5),
        marker("RIFT_CLEAVE", 35.0, seq=6),
        marker("RIFT_CLEAVE", 40.0, seq=7),
        marker("PLAYER_DAMAGED", 50.0, seq=8, value=10.0),
        marker("VICTORY", 70.0, seq=9),
    ]
    report = analyze_encounter(events)
    assert "ENCOUNTER_UNDER_90_SECONDS" in report.diagnostic_flags
    assert "COUNTERS_ATTEMPTED_WITH_ZERO_REFLECTS" in report.diagnostic_flags
    assert "CLEAVES_ATTEMPTED_WITH_ZERO_HITS" in report.diagnostic_flags
    assert "TERMINAL_RUN_WITH_ZERO_SIGNAL_BREAKS" in report.diagnostic_flags
    assert "PLAYER_TOOK_DAMAGE_WITHOUT_RECORDED_BOSS_DAMAGE" in report.diagnostic_flags
    payload = report.to_dict()
    assert "fun_score" not in payload
    assert "passed" not in payload


def test_bci_degradation_duration_is_measured_from_marker_timeline():
    events = [
        marker("PULSE_SHOT", 0.0, seq=1),
        marker("BCI_DEGRADED", 10.0, seq=2),
        marker("BCI_RECOVERED", 14.5, seq=3),
        marker("BCI_DEGRADED", 20.0, seq=4),
        marker("DEFEAT", 26.0, seq=5),
    ]
    report = analyze_encounter(events)
    assert report.bci_degradations == 2
    assert report.bci_degraded_seconds == 10.5
    assert "NEURAL_LINK_DEGRADED_DURING_RUN" in report.diagnostic_flags


def test_game_marker_bridge_exposes_damage_and_near_miss_semantics():
    from pathlib import Path

    root = Path(__file__).resolve().parents[1]
    bridge = (root / "unity/Assets/Mindforge/Telemetry/MindforgeGameMarkerBridge.cs").read_text(encoding="utf-8")
    near = (root / "unity/Assets/Mindforge/Combat/ProjectileNearMissSensor.cs").read_text(encoding="utf-8")
    assert "NearMissAwarded" in near
    assert "nearMissSensor.NearMissAwarded += OnNearMiss" in bridge
    assert "bossVitals.Damaged += OnBossDamaged" in bridge
    assert "playerVitals.Damaged += OnPlayerDamaged" in bridge
    assert '"NEAR_MISS"' in bridge
    assert '"PLAYER_DAMAGED"' in bridge
    assert '"BOSS_DAMAGED"' in bridge
