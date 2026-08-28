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


def ready(seq: int = 1) -> GameMarker:
    return marker(seq, "NEURAL_PAYOFF_LEDGER_READY", reason="CONSERVATIVE_DIRECT_DAMAGE_AND_HEAL_V1")


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
    assert "_neuralPayoffKind" in projectile
    assert "_neuralBonusDamage" in projectile
    assert "_neuralBonusDamage));" in projectile


def test_guard_healing_reports_actual_restored_hp_and_batches_only_at_telemetry_boundary():
    vitals = read("Combat", "CombatantVitals.cs")
    controller = read("Combat", "GuardianCombatController.cs")
    bridge = read("Telemetry", "MindforgeGameMarkerBridge.cs")

    assert "public float Heal(float amount)" in vitals
    assert "return Mathf.Max(0f, Health - before)" in vitals

    # Guard regeneration is gameplay payoff, so it must advance on the authoritative
    # fixed simulation clock rather than render-frame delta time.
    fixed_start = controller.index("private void FixedUpdate()")
    aura_start = controller.index("private void OnAuraApplied")
    fixed_body = controller[fixed_start:aura_start]
    assert "auras.HealingPerSecond * Time.fixedDeltaTime" in fixed_body
    assert "Time.deltaTime" not in fixed_body
    assert 'NeuralPayoffObserved?.Invoke("GUARD_REGEN_REALIZED", restored)' in fixed_body

    assert '"GUARD_COUNTER_HEAL_REALIZED"' in controller
    assert "_guardHealPending" not in controller
    assert "guardHealMarkerInterval = 0.75f" in bridge
    assert "_guardRegenPending += value" in bridge
    assert "private void Update()" in bridge
    assert 'reason: "GUARD_REGEN_REALIZED"' in bridge
    assert "private void OnBossDied()" in bridge and "private void OnPlayerDied()" in bridge


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


def test_game_marker_bridge_declares_ledger_and_observes_dynamic_echo_targets():
    bridge = read("Telemetry", "MindforgeGameMarkerBridge.cs")
    echo = read("Combat", "FracturedEchoNode.cs")

    assert "public CombatantVitals Vitals => vitals" in echo
    assert '"NEURAL_PAYOFF_LEDGER_READY"' in bridge
    assert '"CONSERVATIVE_DIRECT_DAMAGE_AND_HEAL_V1"' in bridge
    assert "ObserveEchoVitals()" in bridge
    assert "vitals.Damaged += OnEchoDamaged" in bridge
    assert '"NEURAL_DAMAGE_BONUS_REALIZED"' in bridge
    assert '"NEURAL_GUARD_HEAL_REALIZED"' in bridge
    assert "packet.NeuralBonusDamage <= 0f" in bridge
    assert 'EmitNeuralDamageBonus(packet, "echo")' in bridge
    assert 'EmitNeuralDamageBonus(packet, "boss")' in bridge


def test_encounter_report_sums_only_realized_payoff_markers():
    markers = [
        ready(1),
        marker(2, "NEURAL_BUFF_APPLIED", target="sight"),
        marker(3, "NEURAL_DAMAGE_BONUS_REALIZED", value=6.0, reason="SIGHT_PULSE_DAMAGE", target="boss"),
        marker(4, "NEURAL_DAMAGE_BONUS_REALIZED", value=4.5, reason="SIGHT_CLEAVE_DAMAGE", target="echo"),
        marker(5, "NEURAL_DAMAGE_BONUS_REALIZED", value=7.5, reason="CONCORD_COUNTER_DAMAGE", target="boss"),
        marker(6, "NEURAL_DAMAGE_BONUS_REALIZED", value=13.5, reason="TWIN_ECLIPSE_DAMAGE", target="boss"),
        marker(7, "NEURAL_BUFF_APPLIED", target="guard"),
        marker(8, "NEURAL_GUARD_HEAL_REALIZED", value=2.2, reason="GUARD_REGEN_REALIZED", target="guardian"),
        marker(9, "NEURAL_GUARD_HEAL_REALIZED", value=1.8, reason="GUARD_COUNTER_HEAL_REALIZED", target="guardian"),
        marker(10, "VICTORY"),
    ]

    report = analyze_encounter(markers)
    assert report.neural_payoff_ledger_ready is True
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


def test_zero_payoff_diagnostics_require_explicit_ledger_capability():
    instrumented = analyze_encounter([
        ready(1),
        marker(2, "NEURAL_BUFF_APPLIED", target="sight"),
        marker(3, "NEURAL_BUFF_APPLIED", target="guard"),
        marker(4, "DEFEAT"),
    ])
    assert instrumented.neural_payoff_ledger_ready is True
    assert "SIGHT_ACCEPTED_WITH_ZERO_RECORDED_DAMAGE_BONUS" in instrumented.diagnostic_flags
    assert "GUARD_ACCEPTED_WITH_ZERO_RECORDED_HEALING" in instrumented.diagnostic_flags

    legacy = analyze_encounter([
        marker(1, "NEURAL_BUFF_APPLIED", target="sight"),
        marker(2, "NEURAL_BUFF_APPLIED", target="guard"),
        marker(3, "DEFEAT"),
    ])
    assert legacy.neural_payoff_ledger_ready is False
    assert "SIGHT_ACCEPTED_WITH_ZERO_RECORDED_DAMAGE_BONUS" not in legacy.diagnostic_flags
    assert "GUARD_ACCEPTED_WITH_ZERO_RECORDED_HEALING" not in legacy.diagnostic_flags
