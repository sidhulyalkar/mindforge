from __future__ import annotations

from pathlib import Path

from mindforge_neuro.encounter import analyze_encounter
from mindforge_neuro.markers import GAME_MARKER_V1, GameMarker


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def marker(
    seq: int,
    event: str,
    *,
    value: float = 0.0,
    reason: str | None = None,
    target: str | None = None,
) -> GameMarker:
    return GameMarker(
        schema=GAME_MARKER_V1,
        seq=seq,
        session_id="payoff-session",
        calibration_id="cal-1",
        event=event,
        category="neural_payoff",
        unity_realtime_s=float(seq),
        game_time_s=float(seq),
        frame=seq,
        fixed_tick=seq,
        reason=reason,
        target=target,
        value=value,
        boss_phase=2,
    )


def test_damage_attribution_travels_with_actual_consequence_and_clips_overkill():
    contracts = read("Combat", "CombatContracts.cs")
    vitals = read("Combat", "CombatantVitals.cs")
    projectile = read("Combat", "MindforgeProjectile.cs")

    assert "NeuralPayoffKind" in contracts
    assert "NeuralBonusDamage" in contracts
    assert "neuralPayoffKind = null" in contracts
    assert "neuralBonusDamage = 0f" in contracts

    assert "float baselineDamage = Mathf.Max(0f, requestedDamage - requestedBonus)" in vitals
    assert "float baselineActual = Mathf.Min(before, baselineDamage)" in vitals
    assert "float realizedBonus = Mathf.Max(0f, actualDamage - baselineActual)" in vitals
    assert "actualDamage" in vitals

    assert "_neuralPayoffKind" in projectile
    assert "_neuralBonusDamage" in projectile
    assert "_neuralPayoffKind," in projectile
    assert "_neuralBonusDamage));" in projectile


def test_guard_healing_reports_actual_restored_hp_not_requested_rate():
    vitals = read("Combat", "CombatantVitals.cs")
    controller = read("Combat", "GuardianCombatController.cs")

    assert "public float Heal(float amount)" in vitals
    assert "return Mathf.Max(0f, Health - before)" in vitals
    assert "float restored = vitals.Heal(auras.HealingPerSecond * Time.deltaTime)" in controller
    assert "_guardHealPending += restored" in controller
    assert '"GUARD_REGEN_REALIZED"' in controller
    assert '"GUARD_COUNTER_HEAL_REALIZED"' in controller
    assert "guardHealEvidenceInterval = 0.75f" in controller


def test_sight_concord_and_twin_eclipse_use_explicit_incremental_baselines():
    controller = read("Combat", "GuardianCombatController.cs")
    bloom = read("Combat", "GravityBloomAbility.cs")

    assert "damage - tuning.shotDamage" in controller
    assert '"SIGHT_PULSE_DAMAGE"' in controller
    assert "totalDamage - tuning.cleaveDamage" in controller
    assert '"SIGHT_CLEAVE_DAMAGE"' in controller
    assert "reflectedDamage - baselineDamage" in controller
    assert '"CONCORD_COUNTER_DAMAGE"' in controller

    assert "float baselineDamage = tuning.reflectedDamage" in bloom
    assert "damage - baselineDamage" in bloom
    assert '"TWIN_ECLIPSE_DAMAGE"' in bloom


def test_game_marker_bridge_emits_realized_payoff_and_observes_echo_targets():
    bridge = read("Telemetry", "MindforgeGameMarkerBridge.cs")
    echo = read("Combat", "FracturedEchoNode.cs")

    assert "public CombatantVitals Vitals => vitals" in echo
    assert "ObserveEchoVitals()" in bridge
    assert "vitals.Damaged += OnEchoDamaged" in bridge
    assert '"NEURAL_DAMAGE_BONUS_REALIZED"' in bridge
    assert '"NEURAL_GUARD_HEAL_REALIZED"' in bridge
    assert "packet.NeuralBonusDamage <= 0f" in bridge
    assert 'target: "echo"' not in bridge  # helper receives target dynamically
    assert 'EmitNeuralDamageBonus(packet, "echo")' in bridge
    assert 'EmitNeuralDamageBonus(packet, "boss")' in bridge


def test_encounter_report_sums_only_realized_payoff_markers():
    markers = [
        marker(1, "NEURAL_BUFF_APPLIED", target="sight"),
        marker(2, "NEURAL_DAMAGE_BONUS_REALIZED", value=6.0, reason="SIGHT_PULSE_DAMAGE", target="boss"),
        marker(3, "NEURAL_DAMAGE_BONUS_REALIZED", value=4.5, reason="SIGHT_CLEAVE_DAMAGE", target="echo"),
        marker(4, "NEURAL_DAMAGE_BONUS_REALIZED", value=7.5, reason="CONCORD_COUNTER_DAMAGE", target="boss"),
        marker(5, "NEURAL_DAMAGE_BONUS_REALIZED", value=13.5, reason="TWIN_ECLIPSE_DAMAGE", target="boss"),
        marker(6, "NEURAL_BUFF_APPLIED", target="guard"),
        marker(7, "NEURAL_GUARD_HEAL_REALIZED", value=2.2, reason="GUARD_REGEN_REALIZED", target="guardian"),
        marker(8, "NEURAL_GUARD_HEAL_REALIZED", value=1.8, reason="GUARD_COUNTER_HEAL_REALIZED", target="guardian"),
        marker(9, "VICTORY"),
    ]

    report = analyze_encounter(markers)
    assert report.neural_damage_bonus_events == 4
    assert report.realized_neural_bonus_damage_total == 31.5
    assert report.sight_pulse_bonus_damage == 6.0
    assert report.sight_cleave_bonus_damage == 4.5
    assert report.concord_counter_bonus_damage == 7.5
    assert report.twin_eclipse_bonus_damage == 13.5
    assert report.neural_damage_bonus_to_boss == 27.0
    assert report.neural_damage_bonus_to_echoes == 4.5
    assert report.guard_heal_events == 2
    assert report.realized_guard_healing_total == 4.0
    assert report.guard_regen_healing == 2.2
    assert report.guard_counter_healing == 1.8
    assert "SIGHT_ACCEPTED_WITH_ZERO_RECORDED_DAMAGE_BONUS" not in report.diagnostic_flags
    assert "GUARD_ACCEPTED_WITH_ZERO_RECORDED_HEALING" not in report.diagnostic_flags


def test_accepted_neural_state_without_realized_payoff_is_diagnostic_not_success():
    report = analyze_encounter([
        marker(1, "NEURAL_BUFF_APPLIED", target="sight"),
        marker(2, "NEURAL_BUFF_APPLIED", target="guard"),
        marker(3, "DEFEAT"),
    ])
    assert report.realized_neural_bonus_damage_total == 0.0
    assert report.realized_guard_healing_total == 0.0
    assert "SIGHT_ACCEPTED_WITH_ZERO_RECORDED_DAMAGE_BONUS" in report.diagnostic_flags
    assert "GUARD_ACCEPTED_WITH_ZERO_RECORDED_HEALING" in report.diagnostic_flags
