from __future__ import annotations

from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Iterable

from .markers import GameMarker
from .qualification import load_markers, utc_now


@dataclass(frozen=True)
class EncounterReport:
    schema: str
    generated_utc: str
    marker_path: str
    session_ids: tuple[str, ...]
    source_duration_s: float
    outcome: str

    pulse_shots: int
    rift_cleaves: int
    rift_cleave_hits: int
    rift_cleave_hit_rate: float | None
    counter_attempts: int
    counter_reflects: int
    counter_success_rate: float | None
    phase_dashes: int
    near_misses: int
    echo_spawns: int
    echo_shatters: int
    echo_shatter_rate: float | None

    boss_attack_telegraphs: int
    boss_attacks_fired: int
    fan_attacks_fired: int
    radial_attacks_fired: int
    heavy_attacks_fired: int
    player_damage_after_recent_fan: int
    player_damage_after_recent_radial: int
    player_damage_without_recent_primary_pattern: int

    player_damage_events: int
    player_damage_total: float
    player_heavy_hits: int
    boss_damage_events: int
    boss_damage_total: float
    boss_heavy_hits: int

    signal_breaks: int
    gravity_bloom_charges: int
    gravity_bloom_releases: int
    twin_eclipse_charges: int
    twin_eclipse_releases: int
    concord_established: int
    sight_buffs: int
    guard_buffs: int

    neural_payoff_ledger_ready: bool
    neural_damage_bonus_events: int
    realized_neural_bonus_damage_total: float
    sight_pulse_bonus_damage: float
    sight_cleave_bonus_damage: float
    concord_counter_bonus_damage: float
    twin_eclipse_bonus_damage: float
    neural_damage_bonus_to_boss: float
    neural_damage_bonus_to_echoes: float
    guard_heal_events: int
    realized_guard_healing_total: float
    guard_regen_healing: float
    guard_counter_healing: float

    bci_degradations: int
    bci_degraded_seconds: float
    final_flux: float | None
    max_boss_phase: int
    diagnostic_flags: tuple[str, ...]

    def to_dict(self) -> dict:
        payload = asdict(self)
        payload["session_ids"] = list(self.session_ids)
        payload["diagnostic_flags"] = list(self.diagnostic_flags)
        return payload


def _count(markers: list[GameMarker], event: str) -> int:
    return sum(marker.event == event for marker in markers)


def _damage(markers: list[GameMarker], event: str) -> tuple[int, float, int]:
    matching = [marker for marker in markers if marker.event == event]
    return (
        len(matching),
        sum(max(0.0, float(marker.value)) for marker in matching),
        sum((marker.reason or "").upper() == "HEAVY" for marker in matching),
    )


def _sum_event(markers: list[GameMarker], event: str, *, reason: str | None = None, target: str | None = None) -> float:
    total = 0.0
    for marker in markers:
        if marker.event != event:
            continue
        if reason is not None and (marker.reason or "").upper() != reason.upper():
            continue
        if target is not None and (marker.target or "").lower() != target.lower():
            continue
        total += max(0.0, float(marker.value))
    return total


def _rate(success: int, attempts: int) -> float | None:
    if attempts <= 0:
        return None
    return success / attempts


def _degraded_seconds(markers: list[GameMarker]) -> float:
    started: float | None = None
    total = 0.0
    for marker in markers:
        if marker.event == "BCI_DEGRADED" and started is None:
            started = marker.unity_realtime_s
        elif marker.event == "BCI_RECOVERED" and started is not None:
            total += max(0.0, marker.unity_realtime_s - started)
            started = None
    if started is not None and markers:
        total += max(0.0, markers[-1].unity_realtime_s - started)
    return total


def _recent_primary_pattern_before_damage(
    markers: list[GameMarker], *, lookback_seconds: float = 2.25
) -> tuple[int, int, int]:
    """Count recent primary boss patterns before damage without claiming causation.

    Projectiles have travel time and Echoes fire independently, so these are exposure
    diagnostics only. They answer "what primary pattern had just fired?", not "what
    definitely caused the hit?".
    """
    fan = 0
    radial = 0
    unmatched = 0
    fired: list[GameMarker] = []
    for marker in markers:
        if marker.event == "BOSS_ATTACK_FIRED":
            fired.append(marker)
            continue
        if marker.event != "PLAYER_DAMAGED":
            continue
        recent = [
            attack for attack in fired
            if 0.0 <= marker.unity_realtime_s - attack.unity_realtime_s <= lookback_seconds
        ]
        if not recent:
            unmatched += 1
            continue
        reason = (recent[-1].reason or "").upper()
        if reason.startswith("FAN_"):
            fan += 1
        elif reason.startswith("RADIAL_"):
            radial += 1
        else:
            unmatched += 1
    return fan, radial, unmatched


def analyze_encounter(markers: Iterable[GameMarker], *, marker_path: str = "memory") -> EncounterReport:
    ordered = sorted(list(markers), key=lambda marker: (marker.unity_realtime_s, marker.seq))
    session_ids = tuple(sorted({marker.session_id for marker in ordered if marker.session_id}))
    duration = max(0.0, ordered[-1].unity_realtime_s - ordered[0].unity_realtime_s) if ordered else 0.0

    terminal = [marker.event for marker in ordered if marker.event in {"VICTORY", "DEFEAT"}]
    outcome = terminal[-1] if terminal else "INCOMPLETE"

    pulse_shots = _count(ordered, "PULSE_SHOT")
    cleaves = _count(ordered, "RIFT_CLEAVE")
    cleave_hits = _count(ordered, "RIFT_CLEAVE_HIT")
    counters = _count(ordered, "COUNTER_PULSE")
    reflects = _count(ordered, "COUNTER_REFLECT")
    echo_spawns = _count(ordered, "ECHO_SPAWNED")
    echo_shatters = _count(ordered, "ECHO_SHATTERED")

    telegraphs = [marker for marker in ordered if marker.event == "BOSS_ATTACK_TELEGRAPH"]
    attacks = [marker for marker in ordered if marker.event == "BOSS_ATTACK_FIRED"]
    fan_attacks = sum((marker.reason or "").upper().startswith("FAN_") for marker in attacks)
    radial_attacks = sum((marker.reason or "").upper().startswith("RADIAL_") for marker in attacks)
    heavy_attacks = sum((marker.reason or "").upper().endswith("_HEAVY") for marker in attacks)
    recent_fan_damage, recent_radial_damage, unmatched_damage = _recent_primary_pattern_before_damage(ordered)

    player_damage_events, player_damage_total, player_heavy_hits = _damage(ordered, "PLAYER_DAMAGED")
    boss_damage_events, boss_damage_total, boss_heavy_hits = _damage(ordered, "BOSS_DAMAGED")

    flux_values = [float(marker.value) for marker in ordered if marker.event == "FLUX_CHANGED"]
    sight_buffs = sum(marker.event == "NEURAL_BUFF_APPLIED" and (marker.target or "").lower() == "sight" for marker in ordered)
    guard_buffs = sum(marker.event == "NEURAL_BUFF_APPLIED" and (marker.target or "").lower() == "guard" for marker in ordered)

    payoff_ledger_ready = _count(ordered, "NEURAL_PAYOFF_LEDGER_READY") > 0
    neural_damage_events = [marker for marker in ordered if marker.event == "NEURAL_DAMAGE_BONUS_REALIZED"]
    guard_heals = [marker for marker in ordered if marker.event == "NEURAL_GUARD_HEAL_REALIZED"]
    realized_neural_bonus_damage_total = sum(max(0.0, float(marker.value)) for marker in neural_damage_events)
    realized_guard_healing_total = sum(max(0.0, float(marker.value)) for marker in guard_heals)

    flags: list[str] = []
    if not ordered:
        flags.append("NO_MARKERS")
    if outcome == "INCOMPLETE":
        flags.append("NO_TERMINAL_OUTCOME")
    if duration > 0 and duration < 90:
        flags.append("ENCOUNTER_UNDER_90_SECONDS")
    if duration > 420:
        flags.append("ENCOUNTER_OVER_7_MINUTES")
    if counters >= 3 and reflects == 0:
        flags.append("COUNTERS_ATTEMPTED_WITH_ZERO_REFLECTS")
    if cleaves >= 4 and cleave_hits == 0:
        flags.append("CLEAVES_ATTEMPTED_WITH_ZERO_HITS")
    if echo_spawns > 0 and echo_shatters == 0 and outcome != "INCOMPLETE":
        flags.append("ECHOES_SPAWNED_WITH_ZERO_SHATTERS")
    if _count(ordered, "SIGNAL_BREAK") == 0 and outcome != "INCOMPLETE":
        flags.append("TERMINAL_RUN_WITH_ZERO_SIGNAL_BREAKS")
    if player_damage_total > 0 and boss_damage_total <= 0:
        flags.append("PLAYER_TOOK_DAMAGE_WITHOUT_RECORDED_BOSS_DAMAGE")
    if _count(ordered, "BCI_DEGRADED") > 0:
        flags.append("NEURAL_LINK_DEGRADED_DURING_RUN")
    if attacks and not telegraphs:
        flags.append("BOSS_ATTACKS_FIRED_WITHOUT_TELEGRAPH_MARKERS")
    if payoff_ledger_ready and sight_buffs > 0 and realized_neural_bonus_damage_total <= 0:
        flags.append("SIGHT_ACCEPTED_WITH_ZERO_RECORDED_DAMAGE_BONUS")
    if payoff_ledger_ready and guard_buffs > 0 and realized_guard_healing_total <= 0:
        flags.append("GUARD_ACCEPTED_WITH_ZERO_RECORDED_HEALING")

    return EncounterReport(
        schema="mindforge.encounter_report.v1",
        generated_utc=utc_now(),
        marker_path=marker_path,
        session_ids=session_ids,
        source_duration_s=duration,
        outcome=outcome,
        pulse_shots=pulse_shots,
        rift_cleaves=cleaves,
        rift_cleave_hits=cleave_hits,
        rift_cleave_hit_rate=_rate(cleave_hits, cleaves),
        counter_attempts=counters,
        counter_reflects=reflects,
        counter_success_rate=_rate(reflects, counters),
        phase_dashes=_count(ordered, "PHASE_DASH"),
        near_misses=_count(ordered, "NEAR_MISS"),
        echo_spawns=echo_spawns,
        echo_shatters=echo_shatters,
        echo_shatter_rate=_rate(echo_shatters, echo_spawns),
        boss_attack_telegraphs=len(telegraphs),
        boss_attacks_fired=len(attacks),
        fan_attacks_fired=fan_attacks,
        radial_attacks_fired=radial_attacks,
        heavy_attacks_fired=heavy_attacks,
        player_damage_after_recent_fan=recent_fan_damage,
        player_damage_after_recent_radial=recent_radial_damage,
        player_damage_without_recent_primary_pattern=unmatched_damage,
        player_damage_events=player_damage_events,
        player_damage_total=player_damage_total,
        player_heavy_hits=player_heavy_hits,
        boss_damage_events=boss_damage_events,
        boss_damage_total=boss_damage_total,
        boss_heavy_hits=boss_heavy_hits,
        signal_breaks=_count(ordered, "SIGNAL_BREAK"),
        gravity_bloom_charges=_count(ordered, "GRAVITY_BLOOM_CHARGE"),
        gravity_bloom_releases=_count(ordered, "GRAVITY_BLOOM_RELEASE"),
        twin_eclipse_charges=_count(ordered, "TWIN_ECLIPSE_CHARGE"),
        twin_eclipse_releases=_count(ordered, "TWIN_ECLIPSE_RELEASE"),
        concord_established=_count(ordered, "CONCORD_ESTABLISHED"),
        sight_buffs=sight_buffs,
        guard_buffs=guard_buffs,
        neural_payoff_ledger_ready=payoff_ledger_ready,
        neural_damage_bonus_events=len(neural_damage_events),
        realized_neural_bonus_damage_total=realized_neural_bonus_damage_total,
        sight_pulse_bonus_damage=_sum_event(ordered, "NEURAL_DAMAGE_BONUS_REALIZED", reason="SIGHT_PULSE_DAMAGE"),
        sight_cleave_bonus_damage=_sum_event(ordered, "NEURAL_DAMAGE_BONUS_REALIZED", reason="SIGHT_CLEAVE_DAMAGE"),
        concord_counter_bonus_damage=_sum_event(ordered, "NEURAL_DAMAGE_BONUS_REALIZED", reason="CONCORD_COUNTER_DAMAGE"),
        twin_eclipse_bonus_damage=_sum_event(ordered, "NEURAL_DAMAGE_BONUS_REALIZED", reason="TWIN_ECLIPSE_DAMAGE"),
        neural_damage_bonus_to_boss=_sum_event(ordered, "NEURAL_DAMAGE_BONUS_REALIZED", target="boss"),
        neural_damage_bonus_to_echoes=_sum_event(ordered, "NEURAL_DAMAGE_BONUS_REALIZED", target="echo"),
        guard_heal_events=len(guard_heals),
        realized_guard_healing_total=realized_guard_healing_total,
        guard_regen_healing=_sum_event(ordered, "NEURAL_GUARD_HEAL_REALIZED", reason="GUARD_REGEN_REALIZED"),
        guard_counter_healing=_sum_event(ordered, "NEURAL_GUARD_HEAL_REALIZED", reason="GUARD_COUNTER_HEAL_REALIZED"),
        bci_degradations=_count(ordered, "BCI_DEGRADED"),
        bci_degraded_seconds=_degraded_seconds(ordered),
        final_flux=flux_values[-1] if flux_values else None,
        max_boss_phase=max((marker.boss_phase for marker in ordered), default=0),
        diagnostic_flags=tuple(flags),
    )


def analyze_encounter_file(path: str | Path) -> EncounterReport:
    return analyze_encounter(load_markers(path), marker_path=str(path))
