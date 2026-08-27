from __future__ import annotations

import hashlib
from dataclasses import asdict, dataclass
from pathlib import Path

from .encounter import EncounterReport, analyze_encounter_file
from .qualification import load_markers, utc_now, write_json


CONTROLLER_ONLY_MODE = "CONTROLLER_ONLY_NO_BCI"


@dataclass(frozen=True)
class PlaytestCaptureReport:
    schema: str
    generated_utc: str
    started_utc: str
    ended_utc: str
    marker_path: str
    encounter_report_path: str
    session_id: str | None
    marker_count: int
    terminal_observed: bool
    outcome: str
    stop_reason: str
    git_commit: str | None
    marker_sha256: str
    controller_only_declared: bool
    qualification_modes: tuple[str, ...]

    def to_dict(self) -> dict:
        payload = asdict(self)
        payload["qualification_modes"] = list(self.qualification_modes)
        return payload


def sha256_file(path: str | Path) -> str:
    digest = hashlib.sha256()
    with Path(path).open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _qualification_modes(markers) -> tuple[str, ...]:
    values: set[str] = set()
    for marker in markers:
        if marker.event != "QUALIFICATION_MODE":
            continue
        value = (marker.reason or marker.target or marker.action or "").strip()
        if value:
            values.add(value)
    return tuple(sorted(values))


def finalize_playtest_bundle(
    marker_path: str | Path,
    output_dir: str | Path,
    *,
    stop_reason: str,
    started_utc: str,
    ended_utc: str | None = None,
    expected_session_id: str | None = None,
    git_commit: str | None = None,
) -> tuple[PlaytestCaptureReport, EncounterReport]:
    marker_path = Path(marker_path)
    output_dir = Path(output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    if not marker_path.exists():
        marker_path.parent.mkdir(parents=True, exist_ok=True)
        marker_path.touch()

    markers = load_markers(marker_path)
    session_ids = sorted({marker.session_id for marker in markers if marker.session_id})
    if len(session_ids) > 1:
        raise ValueError(f"playtest bundle contains multiple Unity sessions: {session_ids}")

    session_id = session_ids[0] if session_ids else None
    if expected_session_id is not None and session_id != expected_session_id:
        raise ValueError(
            f"playtest session mismatch: expected {expected_session_id!r}, observed {session_id!r}"
        )

    qualification_modes = _qualification_modes(markers)
    controller_only_declared = CONTROLLER_ONLY_MODE in qualification_modes

    encounter_path = output_dir / "encounter.json"
    capture_path = output_dir / "capture.json"
    encounter = analyze_encounter_file(marker_path)
    write_json(encounter_path, encounter.to_dict())

    report = PlaytestCaptureReport(
        schema="mindforge.playtest_capture.v1",
        generated_utc=utc_now(),
        started_utc=started_utc,
        ended_utc=ended_utc or utc_now(),
        marker_path=str(marker_path),
        encounter_report_path=str(encounter_path),
        session_id=session_id,
        marker_count=len(markers),
        terminal_observed=encounter.outcome in {"VICTORY", "DEFEAT"},
        outcome=encounter.outcome,
        stop_reason=str(stop_reason),
        git_commit=git_commit or None,
        marker_sha256=sha256_file(marker_path),
        controller_only_declared=controller_only_declared,
        qualification_modes=qualification_modes,
    )
    write_json(capture_path, report.to_dict())
    return report, encounter
