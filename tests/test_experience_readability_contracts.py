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
        '"GUARD"',
        "FLUX",
        "BLUE · BLADE",
        "GREEN · SHIELD",
        "CONCORD",
        "TWIN ECLIPSE",
        "SIGNAL BREAK · ATTACK NOW",
        "PERFECT GUARD · REFLECT",
        "AETHER PARRY",
        "ControllerOnlyQualificationActive",
        "CONTROLLER-ONLY MODE",
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
        ".TryLightAttack(",
        ".SetGuardHeld(",
        ".TryApply(",
        ".Award(",
        ".TryConsumeFull(",
        "ReceiveDamage(",
        "BeginCalibration(",
        "TrySpend(",
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


def test_onboarding_is_staged_around_laptop_physical_combat_then_arcane_options():
    guide = _read("Presentation", "PlayerAgencyGuide.cs")

    assert "CurrentLesson()" in guide
    assert "WASD MOVE" in guide
    assert "ARROWS / MOUSE AIM" in guide
    assert "T ENEMY FOCUS" in guide
    assert "focus changes camera only" in guide
    assert "F SWORD" in guide
    assert "SPACE DASH" in guide
    assert "RMB / E HOLD SHIELD" in guide
    assert "PERFECT GUARD reflect" in guide
    assert "SHIFT PULSE SHOT" in guide
    assert "slash hostile projectiles back at the enemy" in guide
    assert "Q RIFT CLEAVE" in guide
    assert "C COUNTER PULSE" in guide
    assert "TAB BUILD" in guide
    assert "FLUX FULL  |  R GRAVITY BLOOM" in guide
    assert "CONCORD ACTIVE  |  R TWIN ECLIPSE" in guide

    assert "combat.ActionAccepted += OnCombatAction" in guide
    assert "physicalCombat.SwordAttackStarted += OnSwordAttack" in guide
    assert "physicalCombat.GuardChanged += OnGuardChanged" in guide
    for forbidden in (
        ".FirePulse(",
        ".RiftCleave(",
        ".BeginCounter(",
        ".RequestDash(",
        ".TryActivate(",
        ".TryLightAttack(",
        ".SetGuardHeld(",
        "TrySpend(",
    ):
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
