#!/usr/bin/env python3
"""Summarize a Mindforge GameMarker playthrough into game-design metrics."""
from __future__ import annotations

import argparse
import json

from mindforge_neuro.encounter import analyze_encounter_file
from mindforge_neuro.qualification import write_json


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("markers", help="GameMarker JSONL captured from the passive observer lane")
    parser.add_argument("--output", default=None, help="optional JSON report path")
    args = parser.parse_args()

    report = analyze_encounter_file(args.markers)
    payload = report.to_dict()
    if args.output:
        write_json(args.output, payload)
    print(json.dumps(payload, indent=2, sort_keys=True))


if __name__ == "__main__":
    main()
