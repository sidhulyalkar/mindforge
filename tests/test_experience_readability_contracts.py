from __future__ import annotations

from pathlib import Path

from mindforge_neuro.encounter import analyze_encounter
from mindforge_neuro.markers import GameMarker, GAME_MARKER_V1


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def _read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def _marker(seq: int, time_s: float, event: str, *, reason: str | None = None, value: float = 0.0) -> GameMarker:
    return GameMarker(
        schema=GAME_MARKER_V1,
        seq=seq,
        session_id="session-readability",
        calibration_id=None,
        event=event,
        category="test",
        unity_realtime_s=time_s,
        game_time_s=time_s,
        frame=seq,
        fixed_tick=seq,
        reason=reason,
        value=value,
        boss_phase=3,
    )


def test_combat_state_hud_is_gameplay_first_and_non_authoritative():
    hud = _read("Presentation", "CombatStateHud.cs")

    for token in (
        "THE FRACTURED SIGNAL",
        "GUARDIAN",
        "FLUX",
        "SIGHT  offense",
        "GUARD  recovery",
        "CONCORD",
        "TWIN ECLIPSE",
        "SIGNAL BREAK · PUNISH WINDOW",
        "ControllerOnlyQualificationActive",
        "P2 CONTROLLER-ONLY",
        "BCI intentionally disabled",
        "RuntimeInitializeOnLoadMethod",
    ):
        assert token in hud

    for forbidden in (
        ".FirePulse(",
        ".RiftCleave(",
        ".BeginCounter(",
        ".RequestDash(",
        ".TryActivate(",
        ".TryApply(",
        ".Award(",
        ".TryConsumeFull(",
        "ReceiveDamage(",
        "BeginCalibration(",
    ):
        assert forbidden not in hud


def test_radial_telegraph_previews_the_same_angular_lattice_as_spawn():
    telegraph = _read("Combat", "FracturedSignalTelegraph.cs")
    director = _read("Combat", "FracturedSignalDirector.cs")

    assert "ShowRadial(Vector3 origin, int projectileCount" in telegraph
    assert "EnsureRayCapacity(count)" in telegraph
    assert "i / (float)count * 360f" in telegraph
    assert "i / (float)count * 360f" in director
    assert "telegraph?.ShowRadial(origin, count, heavy)" in director
    assert "AttackTelegraphed?.Invoke(\"RADIAL\", count, heavy)" in director
    assert "AttackFired?.Invoke(\"RADIAL\", count, heavy)" in director


def test_onboarding_is_staged_instead_of_dumping_all_controls_at_once():
    guide = _read("Presentation", "PlayerAgencyGuide.cs")

    assert "CurrentLesson()" in guide
    assert "WASD move  |  MOUSE / ARROWS aim  |  SPACE Pulse" in guide
    assert "F Cleave up close  |  C Counter incoming crimson projectiles" in guide
    assert "BUILD FLUX  |  near miss, perfect Counter, and Signal Break" in guide
    assert "FLUX FULL  |  R GRAVITY BLOOM" in guide
    assert "CONCORD ACTIVE  |  R TWIN ECLIPSE" in guide

    # The tutorial observes accepted actions but does not execute them.
    assert "combat.ActionAccepted += OnCombatAction" in guide
    for forbidden in (".FirePulse(", ".RiftCleave(", ".BeginCounter(", ".RequestDash(", ".TryActivate("):
        assert forbidden not in guide


def test_boss_pattern_markers_reach_the_semantic_game_marker_bridge():
    bridge = _read("Telemetry", "MindforgeGameMarkerBridge.cs")

    assert "bossDirector.AttackTelegraphed += OnBossAttackTelegraphed" in bridge
    assert "bossDirector.AttackFired += OnBossAttackFired" in bridge
    assert '"BOSS_ATTACK_TELEGRAPH"' in bridge
    assert '"BOSS_ATTACK_FIRED"' in bridge
    assert '"boss_pattern"' in bridge


def test_encounter_report_tracks_recent_pattern_exposure_without_claiming_causation():
    markers = [
        _marker(1, 1.0, "BOSS_ATTACK_TELEGRAPH", reason="FAN_LIGHT", value=3),
        _marker(2, 1.5, "BOSS_ATTACK_FIRED", reason="FAN_LIGHT", value=3),
        _marker(3, 2.3, "PLAYER_DAMAGED", reason="LIGHT", value=10),
        _marker(4, 4.0, "BOSS_ATTACK_TELEGRAPH", reason="RADIAL_HEAVY", value=20),
        _marker(5, 4.4, "BOSS_ATTACK_FIRED", reason="RADIAL_HEAVY", value=20),
        _marker(6, 5.0, "PLAYER_DAMAGED", reason="HEAVY", value=15),
        _marker(7, 9.0, "PLAYER_DAMAGED", reason="LIGHT", value=8),
        _marker(8, 10.0, "DEFEAT"),
    ]

    report = analyze_encounter(markers)
    assert report.boss_attack_telegraphs == 2
    assert report.boss_attacks_fired == 2
    assert report.fan_attacks_fired == 1
    assert report.radial_attacks_fired == 1
    assert report.heavy_attacks_fired == 1
    assert report.player_damage_after_recent_fan == 1
    assert report.player_damage_after_recent_radial == 1
    assert report.player_damage_without_recent_primary_pattern == 1

    source = (ROOT / "neuro/mindforge_neuro/encounter.py").read_text(encoding="utf-8")
    assert "without claiming causation" in source
    assert "definitely caused the hit?" in source
