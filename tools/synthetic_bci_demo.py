"""Send physically plausible synthetic SSVEP decisions through the real decoder.

This is an integration fixture, not observed EEG. It exercises:
synthetic EEG -> FBCCA -> dwell gate -> NeuralEvent -> UDP:19742.
"""

from __future__ import annotations

import argparse
import time
import numpy as np

from mindforge_neuro import AuraTarget, SsvepConfig, SsvepDecoder
from mindforge_neuro.calibration import calibrate_decoder
from mindforge_neuro.runtime import AuraSelectionRuntime, UdpEventSink


def synth(freq: float, cfg: SsvepConfig, rng: np.random.Generator) -> np.ndarray:
    t = np.arange(cfg.window_samples) / cfg.sample_rate_hz
    channels = []
    for _ in range(8):
        phase = rng.uniform(0, 2 * np.pi)
        sig = 10 * np.sin(2 * np.pi * freq * t + phase)
        sig += 3 * np.sin(2 * np.pi * 2 * freq * t + phase / 2)
        sig += rng.normal(0, 6, size=t.shape)
        channels.append(sig)
    return np.stack(channels)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=19742)
    args = parser.parse_args()

    cfg = SsvepConfig(window_seconds=1.25)
    decoder = SsvepDecoder(cfg)
    rng = np.random.default_rng(7)
    trials = []
    for _ in range(8):
        trials.append((AuraTarget.SIGHT, synth(cfg.blue_frequency_hz, cfg, rng)))
        trials.append((AuraTarget.GUARD, synth(cfg.green_frequency_hz, cfg, rng)))
    profile = calibrate_decoder(decoder, trials, model_id="synthetic-integration")
    runtime = AuraSelectionRuntime(decoder, profile)
    sink = UdpEventSink(args.host, args.port)

    print("Synthetic BCI fixture. Type b=Sight, g=Guard, q=quit.")
    try:
        while True:
            command = input("> ").strip().lower()
            if command == "q":
                break
            target = AuraTarget.SIGHT if command == "b" else AuraTarget.GUARD if command == "g" else None
            if target is None:
                continue
            freq = cfg.target_frequencies[target]
            for _ in range(cfg.dwell_windows):
                event = runtime.process(synth(freq, cfg, rng))
                sink.send(event)
                print(event.to_json())
                time.sleep(0.25)
    finally:
        sink.close()


if __name__ == "__main__":
    main()
