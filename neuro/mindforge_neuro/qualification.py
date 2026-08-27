from __future__ import annotations

import json
import platform
import sys
import xml.etree.ElementTree as ET
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from difflib import SequenceMatcher
from pathlib import Path
from typing import Iterable

from .markers import GameMarker


@dataclass(frozen=True)
class SoftwareGateSummary:
    schema: str
    generated_utc: str
    commit: str
    python_version: str
    platform: str
    tests: int
    failures: int
    errors: int
    skipped: int
    passed: bool
    junit_path: str


@dataclass(frozen=True)
class MarkerComparison:
    schema: str
    generated_utc: str
    reference_path: str
    candidate_path: str
    reference_count: int
    candidate_count: int
    exact_match: bool
    similarity: float
    common_prefix: int
    first_mismatch_index: int
    reference_first_mismatch: str | None
    candidate_first_mismatch: str | None


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def _suite_totals(root: ET.Element) -> tuple[int, int, int, int]:
    if root.tag == "testsuite":
        suites = [root]
    elif root.tag == "testsuites":
        suites = list(root.findall("testsuite"))
    else:
        suites = list(root.iter("testsuite"))
    if not suites:
        raise ValueError("JUnit XML contains no testsuite elements")

    tests = failures = errors = skipped = 0
    for suite in suites:
        tests += int(suite.attrib.get("tests", 0))
        failures += int(suite.attrib.get("failures", 0))
        errors += int(suite.attrib.get("errors", 0))
        skipped += int(suite.attrib.get("skipped", 0))
    return tests, failures, errors, skipped


def build_software_gate(junit_path: str | Path, *, commit: str = "unknown") -> SoftwareGateSummary:
    path = Path(junit_path)
    root = ET.parse(path).getroot()
    tests, failures, errors, skipped = _suite_totals(root)
    return SoftwareGateSummary(
        schema="mindforge.software_gate.v1",
        generated_utc=utc_now(),
        commit=commit or "unknown",
        python_version=sys.version.split()[0],
        platform=f"{platform.system()} {platform.release()}",
        tests=tests,
        failures=failures,
        errors=errors,
        skipped=skipped,
        passed=tests > 0 and failures == 0 and errors == 0,
        junit_path=str(path),
    )


def load_markers(path: str | Path) -> list[GameMarker]:
    markers: list[GameMarker] = []
    for line_number, raw in enumerate(Path(path).read_text(encoding="utf-8").splitlines(), start=1):
        line = raw.strip()
        if not line:
            continue
        try:
            markers.append(GameMarker.from_json(line))
        except Exception as exc:
            raise ValueError(f"invalid GameMarker at {path}:{line_number}: {exc}") from exc
    return markers


def semantic_marker_signature(marker: GameMarker) -> str:
    # Deliberately exclude transport/session/timing identity. P4 asks whether the
    # replay caused the same semantic game consequences, not whether it happened at
    # the same wall-clock instant or used the same run ID.
    payload = {
        "event": marker.event,
        "category": marker.category,
        "stage": marker.stage,
        "action": marker.action,
        "target": marker.target,
        "reason": marker.reason,
        "value": round(float(marker.value), 4),
        "boss_phase": int(marker.boss_phase),
        "stimulus_epoch": int(marker.stimulus_epoch),
        "planned_duration_s": round(float(marker.planned_duration_s), 4),
    }
    return json.dumps(payload, sort_keys=True, separators=(",", ":"))


def compare_marker_streams(reference: Iterable[GameMarker], candidate: Iterable[GameMarker], *,
                           reference_path: str = "reference", candidate_path: str = "candidate") -> MarkerComparison:
    ref = [semantic_marker_signature(m) for m in reference]
    cand = [semantic_marker_signature(m) for m in candidate]
    prefix = 0
    for a, b in zip(ref, cand):
        if a != b:
            break
        prefix += 1
    exact = ref == cand
    mismatch = -1 if exact else prefix
    ref_mismatch = None if mismatch < 0 or mismatch >= len(ref) else ref[mismatch]
    cand_mismatch = None if mismatch < 0 or mismatch >= len(cand) else cand[mismatch]
    similarity = SequenceMatcher(a=ref, b=cand, autojunk=False).ratio()
    return MarkerComparison(
        schema="mindforge.marker_comparison.v1",
        generated_utc=utc_now(),
        reference_path=reference_path,
        candidate_path=candidate_path,
        reference_count=len(ref),
        candidate_count=len(cand),
        exact_match=exact,
        similarity=similarity,
        common_prefix=prefix,
        first_mismatch_index=mismatch,
        reference_first_mismatch=ref_mismatch,
        candidate_first_mismatch=cand_mismatch,
    )


def compare_marker_files(reference_path: str | Path, candidate_path: str | Path) -> MarkerComparison:
    return compare_marker_streams(
        load_markers(reference_path),
        load_markers(candidate_path),
        reference_path=str(reference_path),
        candidate_path=str(candidate_path),
    )


def build_promotion_manifest(*, commit: str, software_report: dict | None = None,
                             unity_report: dict | None = None, replay_report: dict | None = None) -> dict:
    gates: list[dict] = []

    def add(gate: str, label: str, evidence: dict | None, pass_key: str) -> None:
        if evidence is None:
            gates.append({"gate": gate, "label": label, "status": "UNOBSERVED", "evidence": None})
            return
        passed = bool(evidence.get(pass_key, False))
        gates.append({"gate": gate, "label": label, "status": "PASS" if passed else "FAIL", "evidence": evidence})

    add("P0", "software contracts/tests", software_report, "passed")
    add("P1", "clean-checkout Unity assemble + validate", unity_report, "passed")
    gates.append({"gate": "P2", "label": "controller-only full encounter", "status": "UNOBSERVED", "evidence": None})
    gates.append({"gate": "P3", "label": "simulated_decision -> Unity", "status": "UNOBSERVED", "evidence": None})
    add("P4", "decision replay semantic reproduction", replay_report, "exact_match")
    for gate, label in (
        ("P5", "neurOS synthetic EEG -> production decoder -> Unity"),
        ("P6", "render/network fault rehearsal"),
        ("P7", "measured physical display timing"),
        ("P8", "real Unicorn acquisition metadata/units"),
        ("P9", "stationary human Sight vs Guard"),
        ("P10", "moving selection"),
        ("P11", "selection while player moves"),
        ("P12", "light combat"),
        ("P13", "full Fractured Signal encounter"),
    ):
        gates.append({"gate": gate, "label": label, "status": "UNOBSERVED", "evidence": None})

    return {
        "schema": "mindforge.promotion_manifest.v1",
        "generated_utc": utc_now(),
        "commit": commit or "unknown",
        "gates": gates,
    }


def write_json(path: str | Path, payload: object) -> None:
    destination = Path(path)
    destination.parent.mkdir(parents=True, exist_ok=True)
    if hasattr(payload, "__dataclass_fields__"):
        payload = asdict(payload)
    destination.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
