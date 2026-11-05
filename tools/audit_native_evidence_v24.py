#!/usr/bin/env python3
"""Audit correlated native selection markers without granting neural authority."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from mindforge_neuro.native_evidence_v24 import audit_selection_markers, load_markers


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--markers", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--enforce-consistency", action="store_true")
    parser.add_argument("--strict-science", action="store_true")
    args = parser.parse_args()

    report = audit_selection_markers(load_markers(args.markers))
    report["input_sha256"] = hashlib.sha256(args.markers.read_bytes()).hexdigest()
    report["source_file"] = args.markers.name
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2, sort_keys=True))
    if args.enforce_consistency and not report["software_consistent"]:
        return 3
    if args.strict_science and not report["scientifically_qualified"]:
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
