#!/usr/bin/env python3
"""Run the same Mindforge SSVEP decoder against Phantom or physical Unicorn LSL.

The source changes. The decoder/game boundary does not.
"""
from __future__ import annotations

import argparse
import time

import numpy as np

from mindforge_neuro import AuraTarget, SsvepConfig, SsvepDecoder
from mindforge_neuro.acquisition import SlidingWindowBuffer, UnicornLslSource
from mindforge_neuro.calibration import calibrate_decoder
from mindforge_neuro.runtime import AuraSelectionRuntime, UdpEventSink


def collect_window(source: UnicornLslSource, samples: int) -> np.ndarray:
    chunks: list[np.ndarray] = []
    total = 0
    deadline = time.monotonic() + 8.0
    while total < samples:
        if time.monotonic() > deadline:
            raise RuntimeError("timed out while collecting calibration EEG")
        chunk = source.pull_chunk(max_samples=min(128, samples - total), timeout_s=0.5)
        if chunk is None:
            continue
        chunks.append(chunk.samples_uv)
        total += chunk.samples_uv.shape[1]
    return np.concatenate(chunks, axis=1)[:, :samples]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--stream-name", default="UnicornMock")
    parser.add_argument("--source-id", default=None)
    parser.add_argument("--scale-to-uv", type=float, default=1.0)
    parser.add_argument("--trials", type=int, default=6)
    parser.add_argument("--hop-seconds", type=float, default=0.25)
    parser.add_argument("--udp-host", default="127.0.0.1")
    parser.add_argument("--udp-port", type=int, default=19742)
    parser.add_argument("--source-mode", choices=("simulation", "live", "replay"), default="simulation")
    parser.add_argument("--model-id", default=None)
    args = parser.parse_args()

    if args.trials < 3:
        raise SystemExit("--trials must be >= 3 per target")
    cfg = SsvepConfig()
    decoder = SsvepDecoder(cfg)
    source = UnicornLslSource(
        stream_name=args.stream_name,
        source_id=args.source_id,
        scale_to_uv=args.scale_to_uv,
    )
    source.connect()
    model_id = args.model_id or f"{args.source_mode}-{int(time.time())}"
    print(f"Connected to {source.stream_identity}; source_mode={args.source_mode}")

    trials: list[tuple[AuraTarget, np.ndarray]] = []
    try:
        for target, frequency in ((AuraTarget.SIGHT, cfg.blue_frequency_hz), (AuraTarget.GUARD, cfg.green_frequency_hz)):
            print(f"\nCalibration: {target.value.upper()} ({frequency:g} Hz)")
            print("Set the Phantom source to this target, or visually attend the matching physical aura.")
            input("Press Enter when ready... ")
            for trial in range(args.trials):
                window = collect_window(source, cfg.window_samples)
                decision = decoder.decide(window)
                print(
                    f"  trial {trial + 1}/{args.trials}: quality={decision.quality.score:.2f} "
                    f"artifact={decision.quality.reason or '-'}"
                )
                trials.append((target, window))
                time.sleep(0.15)

        profile = calibrate_decoder(decoder, trials, model_id=model_id)
        print(
            f"\nCalibration complete: training_accuracy={profile.training_accuracy:.3f}, "
            f"accepted_fraction={profile.accepted_fraction:.3f}, "
            f"min_score={profile.min_score:.3f}, min_margin={profile.min_margin:.3f}"
        )
        print("Streaming derived events to Unity. Ctrl-C to stop.\n")

        runtime = AuraSelectionRuntime(decoder, profile, source_mode=args.source_mode)
        sink = UdpEventSink(args.udp_host, args.udp_port)
        buffer = SlidingWindowBuffer(8, cfg.window_samples, max(1, int(round(args.hop_seconds * cfg.sample_rate_hz))))
        try:
            while True:
                chunk = source.pull_chunk(max_samples=128, timeout_s=0.35)
                if chunk is None:
                    continue
                for window, _timestamps in buffer.push(chunk.samples_uv, chunk.timestamps_s):
                    event = runtime.process(window)
                    sink.send(event)
                    print(
                        f"{event.event.value:13s} target={(event.target.value if event.target else '-'):5s} "
                        f"S={event.sight_score:.3f} G={event.guard_score:.3f} "
                        f"margin={event.margin:.3f} q={event.quality:.2f} "
                        f"reason={event.reason or '-'}"
                    )
        except KeyboardInterrupt:
            print("\nStopping decoder.")
        finally:
            sink.close()
    finally:
        source.close()


if __name__ == "__main__":
    main()
