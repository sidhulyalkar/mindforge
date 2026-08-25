#!/usr/bin/env python3
"""Sweep Mindforge macro-buff cadence against neurOS Phantom EEG timelines.

The model intentionally separates calibration response strength from response
strength during combat. This helps expose moving-target / attention-load
attenuation without pretending synthetic latency is human latency.
"""
from __future__ import annotations

import argparse
import json
import statistics
from dataclasses import asdict, dataclass

from mindforge_neuro import AuraTarget, SsvepConfig, SsvepDecoder
from mindforge_neuro.acquisition import SlidingWindowBuffer
from mindforge_neuro.calibration import calibrate_decoder
from mindforge_neuro.runtime import AuraSelectionRuntime

try:
    from neuros.drivers.synthetic_eeg import SyntheticEEGConfig, SyntheticEEGGenerator
except ImportError as exc:
    raise SystemExit("Install the neurOS Phantom EEG branch before running this sweep.") from exc


@dataclass(frozen=True)
class CadenceResult:
    buff_seconds: float
    concord_grace_seconds: float
    calibration_gain: float
    combat_gain: float
    intended_switch_seconds: float
    accepted_events: int
    stale_events: int
    median_switch_latency_seconds: float | None
    p95_switch_latency_seconds: float | None
    sight_uptime_fraction: float
    guard_uptime_fraction: float
    concord_uptime_fraction: float


def make_calibration(decoder: SsvepDecoder, cfg: SsvepConfig, gain: float):
    trials = []
    for index in range(10):
        for target, base in ((AuraTarget.SIGHT, 100), (AuraTarget.GUARD, 200)):
            generator = SyntheticEEGGenerator(SyntheticEEGConfig(seed=base + index))
            generator.set_attention(cfg.target_frequencies[target], gain=gain)
            trials.append((target, generator.render(cfg.window_samples).data_uv))
    return calibrate_decoder(decoder, trials, model_id=f"phantom-cal-gain-{gain:.2f}")


def percentile95(values: list[float]) -> float | None:
    if not values:
        return None
    ordered = sorted(values)
    index = min(len(ordered) - 1, int(round(0.95 * (len(ordered) - 1))))
    return ordered[index]


def run_case(*, buff_seconds: float, concord_grace_seconds: float,
             calibration_gain: float, combat_gain: float,
             intended_switch_seconds: float, duration_seconds: float,
             inject_artifacts: bool) -> CadenceResult:
    cfg = SsvepConfig()
    decoder = SsvepDecoder(cfg)
    profile = make_calibration(decoder, cfg, calibration_gain)
    runtime = AuraSelectionRuntime(decoder, profile, source_mode="simulation")
    buffer = SlidingWindowBuffer(8, cfg.window_samples, max(1, int(round(0.25 * cfg.sample_rate_hz))))
    generator = SyntheticEEGGenerator(SyntheticEEGConfig(seed=7301))

    sight_until = guard_until = concord_until = 0.0
    sight_uptime = guard_uptime = concord_uptime = 0.0
    accepted_events = stale_events = 0
    switch_latencies: list[float] = []
    pending_target: AuraTarget | None = None
    pending_since = 0.0
    previous_intended: AuraTarget | None = None
    injected: set[str] = set()

    chunk_samples = 25  # 100 ms at 250 Hz
    chunk_seconds = chunk_samples / cfg.sample_rate_hz
    now = 0.0
    while now < duration_seconds:
        intended = AuraTarget.GUARD if int(now // intended_switch_seconds) % 2 == 0 else AuraTarget.SIGHT
        if intended != previous_intended:
            pending_target = intended
            pending_since = now
            previous_intended = intended
        generator.set_attention(cfg.target_frequencies[intended], gain=combat_gain)

        if inject_artifacts:
            schedule = ((8.0, "controller"), (14.0, "blink"), (21.0, "jaw"))
            for artifact_time, kind in schedule:
                key = f"{artifact_time}:{kind}"
                if key not in injected and now >= artifact_time:
                    generator.inject_artifact(kind, duration_seconds=0.50, severity=1.0)
                    injected.add(key)

        block = generator.render(chunk_samples)
        timestamps = (now + (1 + __import__("numpy").arange(chunk_samples)) / cfg.sample_rate_hz)
        for window, _ in buffer.push(block.data_uv, timestamps):
            event = runtime.process(window, now=now)
            if event.event.value != "AURA_SELECTED" or event.target is None:
                continue
            accepted_events += 1
            if event.target != intended:
                stale_events += 1
            elif pending_target == event.target:
                switch_latencies.append(max(0.0, now - pending_since))
                pending_target = None

            if event.target == AuraTarget.SIGHT:
                sight_until = max(sight_until, now + buff_seconds)
            else:
                guard_until = max(guard_until, now + buff_seconds)
            if now < sight_until and now < guard_until:
                concord_until = max(concord_until, now + concord_grace_seconds)

        if now < sight_until: sight_uptime += chunk_seconds
        if now < guard_until: guard_uptime += chunk_seconds
        if now < concord_until: concord_uptime += chunk_seconds
        now += chunk_seconds

    return CadenceResult(
        buff_seconds=buff_seconds,
        concord_grace_seconds=concord_grace_seconds,
        calibration_gain=calibration_gain,
        combat_gain=combat_gain,
        intended_switch_seconds=intended_switch_seconds,
        accepted_events=accepted_events,
        stale_events=stale_events,
        median_switch_latency_seconds=statistics.median(switch_latencies) if switch_latencies else None,
        p95_switch_latency_seconds=percentile95(switch_latencies),
        sight_uptime_fraction=sight_uptime / duration_seconds,
        guard_uptime_fraction=guard_uptime / duration_seconds,
        concord_uptime_fraction=concord_uptime / duration_seconds,
    )


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--duration", type=float, default=30.0)
    parser.add_argument("--calibration-gain", type=float, default=1.0)
    parser.add_argument("--combat-gains", default="1.0,0.8,0.65")
    parser.add_argument("--switch-seconds", type=float, default=3.25)
    parser.add_argument("--buff-seconds", default="3.6,4.5,5.25")
    parser.add_argument("--grace-seconds", default="3.0,4.5,6.0")
    parser.add_argument("--no-artifacts", action="store_true")
    parser.add_argument("--json", dest="json_path", default=None)
    args = parser.parse_args()

    gains = [float(v) for v in args.combat_gains.split(",")]
    buffs = [float(v) for v in args.buff_seconds.split(",")]
    graces = [float(v) for v in args.grace_seconds.split(",")]
    results = [
        run_case(
            buff_seconds=buff,
            concord_grace_seconds=grace,
            calibration_gain=args.calibration_gain,
            combat_gain=gain,
            intended_switch_seconds=args.switch_seconds,
            duration_seconds=args.duration,
            inject_artifacts=not args.no_artifacts,
        )
        for gain in gains for buff in buffs for grace in graces
    ]

    print("Synthetic-only BCI combat cadence sweep")
    print("gain  buff grace events stale medLat p95Lat sight guard concord")
    for r in results:
        med = "-" if r.median_switch_latency_seconds is None else f"{r.median_switch_latency_seconds:.2f}"
        p95 = "-" if r.p95_switch_latency_seconds is None else f"{r.p95_switch_latency_seconds:.2f}"
        print(
            f"{r.combat_gain:4.2f} {r.buff_seconds:4.2f} {r.concord_grace_seconds:4.2f} "
            f"{r.accepted_events:6d} {r.stale_events:5d} {med:>6s} {p95:>6s} "
            f"{r.sight_uptime_fraction:5.2f} {r.guard_uptime_fraction:5.2f} {r.concord_uptime_fraction:7.2f}"
        )

    if args.json_path:
        payload = {
            "schema": "mindforge.phantom_cadence.v1",
            "synthetic_only": True,
            "warning": "Use this sweep to choose physical experiments, not to claim human performance.",
            "results": [asdict(r) for r in results],
        }
        with open(args.json_path, "w", encoding="utf-8") as handle:
            json.dump(payload, handle, indent=2)
        print(f"wrote {args.json_path}")


if __name__ == "__main__":
    main()
