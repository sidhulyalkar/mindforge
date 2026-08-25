#!/usr/bin/env python3
"""Create a judge-facing Mindforge closed-loop session report from Unity JSON telemetry.

This report does not claim to measure cognitive fatigue. It visualizes observed neural
control robustness, signal-quality authority, suspected artifact flags, selections,
and boss phase over time.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

import matplotlib.pyplot as plt
import numpy as np

ARTIFACT_REASONS = {"EMG_SUSPECTED", "FAST_TRANSIENT", "COMMON_MODE_TRANSIENT", "SATURATION", "TOO_FEW_CHANNELS"}


def rolling_fraction(values: np.ndarray, window: int = 20) -> np.ndarray:
    if values.size == 0:
        return values
    n = min(window, values.size)
    kernel = np.ones(n, dtype=float) / n
    return np.convolve(values.astype(float), kernel, mode="same")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("session_json")
    parser.add_argument("--out", default=None, help="PNG output path")
    parser.add_argument("--pdf", default=None, help="Optional PDF output path for printing")
    args = parser.parse_args()

    path = Path(args.session_json)
    data = json.loads(path.read_text(encoding="utf-8"))
    records = data.get("records", [])
    evidence = [r for r in records if r.get("category") == "neural_evidence"]
    if not evidence:
        raise SystemExit("session contains no neural_evidence records")

    t0 = float(evidence[0].get("realtime_s", 0.0))
    t = np.asarray([float(r.get("realtime_s", 0.0)) - t0 for r in evidence])
    sight = np.asarray([float(r.get("sight_score", 0.0)) for r in evidence])
    guard = np.asarray([float(r.get("guard_score", 0.0)) for r in evidence])
    margin = np.asarray([float(r.get("margin", 0.0)) for r in evidence])
    quality = np.asarray([float(r.get("quality", 0.0)) for r in evidence])
    accepted = np.asarray([r.get("event_type") == "AURA_SELECTED" for r in evidence], dtype=float)
    artifact = np.asarray([str(r.get("reason") or "") in ARTIFACT_REASONS for r in evidence], dtype=float)

    fig = plt.figure(figsize=(11.0, 8.5))
    gs = fig.add_gridspec(4, 1, hspace=0.34)
    ax1 = fig.add_subplot(gs[0, 0])
    ax2 = fig.add_subplot(gs[1, 0], sharex=ax1)
    ax3 = fig.add_subplot(gs[2, 0], sharex=ax1)
    ax4 = fig.add_subplot(gs[3, 0], sharex=ax1)

    ax1.plot(t, sight, label="Sight FBCCA score")
    ax1.plot(t, guard, label="Guard FBCCA score")
    ax1.set_ylabel("Decoder score")
    ax1.legend(loc="upper right", ncol=2)

    ax2.plot(t, margin, label="Winner margin")
    ax2.plot(t, quality, label="Quality authority")
    ax2.set_ylabel("Evidence")
    ax2.legend(loc="upper right", ncol=2)

    ax3.plot(t, rolling_fraction(accepted), label="Rolling accepted fraction")
    ax3.plot(t, rolling_fraction(artifact), label="Rolling suspected-artifact fraction")
    ax3.set_ylim(-0.03, 1.03)
    ax3.set_ylabel("Rolling fraction")
    ax3.legend(loc="upper right", ncol=2)

    phase_records = [r for r in records if r.get("category") == "boss_phase"]
    for r in phase_records:
        x = float(r.get("realtime_s", 0.0)) - t0
        ax4.axvline(x, alpha=0.55)
        ax4.text(x, 0.92, str(r.get("event_type", "phase")), rotation=90, va="top", fontsize=8)
    selections = [r for r in records if r.get("category") == "neural_authority" and r.get("event_type") == "AURA_SELECTED"]
    for r in selections:
        x = float(r.get("realtime_s", 0.0)) - t0
        ax4.scatter([x], [0.55 if r.get("target") == "sight" else 0.30], marker="o")
    ax4.set_ylim(0, 1)
    ax4.set_yticks([0.30, 0.55])
    ax4.set_yticklabels(["Guard", "Sight"])
    ax4.set_xlabel("Seconds since first neural evidence")
    ax4.set_title("Accepted selections and boss-phase transitions")

    total = max(1, len(evidence))
    accepted_n = int(np.sum(accepted))
    artifact_n = int(np.sum(artifact))
    abstain_n = sum(r.get("event_type") == "ABSTAIN" for r in evidence)
    source = data.get("source_mode") or "unknown"
    fig.suptitle(
        f"Mindforge closed-loop session · {data.get('outcome', 'UNKNOWN')} · {str(source).upper()}\n"
        f"windows={len(evidence)}  selections={accepted_n}  abstentions={abstain_n}  "
        f"suspected-artifact={artifact_n} ({artifact_n / total:.1%})",
        fontsize=12,
    )

    out = Path(args.out) if args.out else path.with_suffix(".report.png")
    fig.savefig(out, dpi=180, bbox_inches="tight")
    if args.pdf:
        fig.savefig(Path(args.pdf), bbox_inches="tight")
    print(out)


if __name__ == "__main__":
    main()
