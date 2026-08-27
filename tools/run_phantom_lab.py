#!/usr/bin/env python3
"""Adversarial Mindforge decoder benchmark driven by neurOS Phantom Unicorn.

This is synthetic qualification only. It is useful for finding software/game
failures and tuning experiment ranges, never for claiming human BCI accuracy.
"""
from __future__ import annotations

import argparse
import json
from collections import Counter
from dataclasses import asdict, dataclass

from mindforge_neuro import AuraTarget, SsvepConfig, SsvepDecoder
from mindforge_neuro.calibration import calibrate_decoder

try:
    from neuros.drivers.synthetic_eeg import SyntheticEEGConfig, SyntheticEEGGenerator
except ImportError as exc:
    raise SystemExit(
        "neurOS Phantom lab is required. Install the neurOS-v1 feat/ssvep-phantom-unicorn "
        "branch (neuros-core + neuros-drivers) in this environment."
    ) from exc


@dataclass(frozen=True)
class ScenarioResult:
    name: str
    windows: int
    accepted: int
    correct: int
    false: int
    abstained: int
    mean_quality: float
    mean_margin: float
    reasons: dict[str, int]


def make_window(*, target: AuraTarget | None, gain: float, seed: int,
                cfg: SsvepConfig, alpha_hz: float = 9.4, alpha_uv: float = 2.8,
                artifact: str | None = None, oz_gain: float = 1.0):
    synth_cfg = SyntheticEEGConfig(
        sampling_rate_hz=cfg.sample_rate_hz,
        seed=seed,
        alpha_frequency_hz=alpha_hz,
        alpha_amplitude_uv=alpha_uv,
    )
    generator = SyntheticEEGGenerator(synth_cfg)
    if target is None:
        generator.set_attention(None)
    else:
        generator.set_attention(cfg.target_frequencies[target], gain=gain)
    generator.set_channel_gain("Oz", oz_gain)
    if artifact is not None:
        generator.inject_artifact(artifact, duration_seconds=0.50, severity=1.0)
    return generator.render(cfg.window_samples).data_uv


def build_profile(decoder: SsvepDecoder, cfg: SsvepConfig, trials_per_target: int = 10):
    trials = []
    for i in range(trials_per_target):
        trials.append((AuraTarget.SIGHT, make_window(target=AuraTarget.SIGHT, gain=1.0, seed=100 + i, cfg=cfg)))
        trials.append((AuraTarget.GUARD, make_window(target=AuraTarget.GUARD, gain=1.0, seed=200 + i, cfg=cfg)))
    return calibrate_decoder(decoder, trials, model_id="phantom-lab-calibration")


def evaluate(decoder: SsvepDecoder, cfg: SsvepConfig, *, name: str,
             truth: AuraTarget | None, gain: float = 1.0, alpha_hz: float = 9.4,
             alpha_uv: float = 2.8, artifact: str | None = None,
             oz_gain: float = 1.0, windows: int = 16, seed_base: int = 1000) -> ScenarioResult:
    accepted = correct = false = 0
    qualities: list[float] = []
    margins: list[float] = []
    reasons: Counter[str] = Counter()
    for index in range(windows):
        eeg = make_window(
            target=truth, gain=gain, seed=seed_base + index, cfg=cfg,
            alpha_hz=alpha_hz, alpha_uv=alpha_uv, artifact=artifact, oz_gain=oz_gain,
        )
        decision = decoder.decide(eeg)
        qualities.append(decision.quality.score)
        margins.append(decision.margin)
        if decision.accepted and decision.target is not None:
            accepted += 1
            if truth is not None and decision.target == truth:
                correct += 1
            else:
                false += 1
        else:
            reasons[decision.reason or "ABSTAIN"] += 1
    return ScenarioResult(
        name=name,
        windows=windows,
        accepted=accepted,
        correct=correct,
        false=false,
        abstained=windows - accepted,
        mean_quality=sum(qualities) / windows,
        mean_margin=sum(margins) / windows,
        reasons=dict(reasons),
    )


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--windows", type=int, default=16)
    parser.add_argument("--json", dest="json_path", default=None)
    args = parser.parse_args()

    cfg = SsvepConfig()
    decoder = SsvepDecoder(cfg)
    profile = build_profile(decoder, cfg)
    scenarios = [
        dict(name="strong_sight", truth=AuraTarget.SIGHT, gain=1.0),
        dict(name="strong_guard", truth=AuraTarget.GUARD, gain=1.0),
        dict(name="weak_sight", truth=AuraTarget.SIGHT, gain=0.35),
        dict(name="weak_guard", truth=AuraTarget.GUARD, gain=0.35),
        dict(name="alpha_collision_10hz_no_target", truth=None, gain=0.0, alpha_hz=10.0, alpha_uv=8.0),
        dict(name="sight_with_blink", truth=AuraTarget.SIGHT, artifact="blink"),
        dict(name="sight_with_jaw_emg", truth=AuraTarget.SIGHT, artifact="jaw"),
        dict(name="sight_with_controller_emg", truth=AuraTarget.SIGHT, artifact="controller"),
        dict(name="sight_with_motion", truth=AuraTarget.SIGHT, artifact="motion"),
        dict(name="sight_with_oz_dropout", truth=AuraTarget.SIGHT, oz_gain=0.0),
    ]
    results = [evaluate(decoder, cfg, windows=args.windows, seed_base=1000 + 100 * i, **scenario)
               for i, scenario in enumerate(scenarios)]

    print("Synthetic-only Phantom Unicorn stress report")
    print(
        f"calibration accuracy={profile.training_accuracy:.3f} accepted={profile.accepted_fraction:.3f} "
        f"score>={profile.min_score:.3f} margin>={profile.min_margin:.3f}"
    )
    for result in results:
        print(
            f"{result.name:32s} accepted={result.accepted:2d}/{result.windows:<2d} "
            f"correct={result.correct:2d} false={result.false:2d} abstain={result.abstained:2d} "
            f"q={result.mean_quality:.2f} margin={result.mean_margin:.3f} reasons={result.reasons}"
        )

    report = {
        "schema": "mindforge.phantom_lab.v1",
        "synthetic_only": True,
        "warning": "Synthetic results are not observed human BCI evidence.",
        "decoder": {
            "window_seconds": cfg.window_seconds,
            "frequencies_hz": [cfg.blue_frequency_hz, cfg.green_frequency_hz],
            "decode_channel_indices": list(cfg.decode_channel_indices),
        },
        "calibration": asdict(profile),
        "scenarios": [asdict(result) for result in results],
    }
    # Enum keys in calibration are not JSON keys by default.
    report["calibration"]["trials_per_target"] = {
        key.value: value for key, value in profile.trials_per_target.items()
    }
    if args.json_path:
        with open(args.json_path, "w", encoding="utf-8") as handle:
            json.dump(report, handle, indent=2)
        print(f"wrote {args.json_path}")


if __name__ == "__main__":
    main()
