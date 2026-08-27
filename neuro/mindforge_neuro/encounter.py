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


def analyze_encounter(markers: Iterable[GameMarker], *, marker_path: str = "memory") -> EncounterReport:
    ordered = sorted(list(markers), key=lambda marker: (marker.unity_realtime_s, marker.seq))
    session_ids = tuple(sorted({marker.session_id for marker in ordered if marker.session_id}))
    if ordered:
        duration = max(0.0, ordered[-1].unity_realtime_s - ordered[0].unity_realtime_s)
    else:
        duration = 0.0

    terminal = [marker.event for marker in ordered if marker.event in {"VICTORY", "DEFEAT"}]
    outcome = terminal[-1] if terminal else "INCOMPLETE"

    pulse_shots = _count(ordered, "PULSE_SHOT")
    cleaves = _count(ordered, "RIFT_CLEAVE")
    cleave_hits = _count(ordered, "RIFT_CLEAVE_HIT")
    counters = _count(ordered, "COUNTER_PULSE")
    reflects = _count(ordered, "COUNTER_REFLECT")

    player_damage_events, player_damage_total, player_heavy_hits = _damage(ordered, "PLAYER_DAMAGED")
    boss_damage_events, boss_damage_total, boss_heavy_hits = _damage(ordered, "BOSS_DAMAGED")

    flux_values = [float(marker.value) for marker in ordered if marker.event == "FLUX_CHANGED"]
    sight_buffs = sum(marker.event == "NEURAL_BUFF_APPLIED" and (marker.target or "").lower() == "sight" for marker in ordered)
    guard_buffs = sum(marker.event == "NEURAL_BUFF_APPLIED" and (marker.target or "").lower() == "guard" for marker in ordered)

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
    if _count(ordered, "SIGNAL_BREAK") == 0 and outcome != "INCOMPLETE":
        flags.append("TERMINAL_RUN_WITH_ZERO_SIGNAL_BREAKS")
    if player_damage_total > 0 and boss_damage_total <= 0:
        flags.append("PLAYER_TOOK_DAMAGE_WITHOUT_RECORDED_BOSS_DAMAGE")
    if _count(ordered, "BCI_DEGRADED") > 0:
        flags.append("NEURAL_LINK_DEGRADED_DURING_RUN")

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
        bci_degradations=_count(ordered, "BCI_DEGRADED"),
        bci_degraded_seconds=_degraded_seconds(ordered),
        final_flux=flux_values[-1] if flux_values else None,
        max_boss_phase=max((marker.boss_phase for marker in ordered), default=0),
        diagnostic_flags=tuple(flags),
    )


def analyze_encounter_file(path: str | Path) -> EncounterReport:
    return analyze_encounter(load_markers(path), marker_path=str(path))
