"""Fail-closed correlation audit of Unity GameMarker events.

A matched marker chain only establishes what Unity claimed. It does not establish
human intention, trustworthy display timing, independent effect verification,
source-stream completeness, or physical signal quality.
"""
from __future__ import annotations

import json
import math
from collections import defaultdict
from pathlib import Path
from typing import Any, Iterable

from .markers import GameMarker

SCHEMA = "mindforge.native_selection_evidence_report.v24"
EVENTS = {
    "NEURAL_SELECTION_OBSERVED", "NEURAL_SELECTION_DENIED",
    "NEURAL_SELECTION_ACCEPTED", "NEURAL_EFFECT_OBSERVED",
    "NEURAL_EFFECT_UNVERIFIED",
}


def _pairs_no_duplicates(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    item: dict[str, Any] = {}
    for key, value in pairs:
        if key in item:
            raise ValueError(f"duplicate JSON key: {key}")
        item[key] = value
    return item


def load_markers(path: str | Path) -> list[GameMarker]:
    source = Path(path)
    if source.stat().st_size > 64 * 1024 * 1024:
        raise ValueError("capture exceeds 64 MiB safety bound")
    content = source.read_bytes()
    if content and not content.endswith(b"\n"):
        raise ValueError("truncated marker capture: missing terminal LF")
    allowed = set(GameMarker.__dataclass_fields__)
    result: list[GameMarker] = []
    for line_no, raw in enumerate(content.splitlines(), 1):
        if not raw:
            raise ValueError(f"blank row at line {line_no}")
        try:
            obj = json.loads(
                raw, object_pairs_hook=_pairs_no_duplicates,
                parse_constant=lambda x: (_ for _ in ()).throw(ValueError("nonfinite JSON: " + x)),
            )
            if not isinstance(obj, dict) or set(obj) - allowed:
                raise ValueError("unknown marker fields or non-object row")
            # GameMarker.from_dict retains backward compatibility by coercing numeric
            # values. Scientific ingest must reject such coercion at the wire boundary.
            if (type(obj.get("seq")) is not int or obj["seq"] <= 0 or
                    type(obj.get("stimulus_epoch", -1)) is not int or
                    type(obj.get("unity_realtime_s")) not in (int, float) or
                    not math.isfinite(obj["unity_realtime_s"]) or
                    not isinstance(obj.get("session_id"), str) or
                    not (0 < len(obj["session_id"]) <= 512) or
                    not isinstance(obj.get("event"), str) or
                    not (0 < len(obj["event"]) <= 128)):
                raise ValueError("invalid strict marker wire types")
            for field in ("trial_id", "target", "reason"):
                value = obj.get(field)
                if value is not None and (not isinstance(value, str) or len(value) > 512):
                    raise ValueError("invalid strict marker wire field: " + field)
            marker = GameMarker.from_dict(obj)
            if marker.schema != "mindforge.game_marker.v1":
                raise ValueError("legacy marker not eligible for causal audit")
            if not marker.session_id or marker.seq <= 0 or not math.isfinite(marker.unity_realtime_s):
                raise ValueError("missing valid session/sequence/clock")
            result.append(marker)
        except (ValueError, TypeError, KeyError, UnicodeError, OverflowError) as exc:
            raise ValueError(f"{source}:{line_no}: {exc}") from exc
    return result


def audit_selection_markers(rows: Iterable[GameMarker]) -> dict[str, Any]:
    issues: list[dict[str, Any]] = []
    trials: dict[tuple[str, str], dict[str, Any]] = {}
    streams: dict[str, tuple[int, float]] = {}
    event_counts: dict[str, int] = defaultdict(int)
    legacy_unattributed_buffs = 0

    def issue(code: str, **detail: Any) -> None:
        issues.append({"code": code, **detail})

    count = 0
    for marker in rows:
        count += 1
        sid = marker.session_id
        previous = streams.get(sid)
        if not sid or marker.seq <= 0 or not math.isfinite(marker.unity_realtime_s):
            issue("INVALID_STREAM_IDENTITY", row=count)
            continue
        if previous is not None:
            if marker.seq <= previous[0]:
                issue("SEQUENCE_REUSED_OR_REORDERED", session=sid, seq=marker.seq)
            elif marker.seq != previous[0] + 1:
                issue("SOURCE_SEQUENCE_GAP", session=sid, expected=previous[0] + 1, observed=marker.seq)
            if marker.unity_realtime_s < previous[1]:
                issue("UNITY_CLOCK_REGRESSION", session=sid, seq=marker.seq)
        streams[sid] = (marker.seq, marker.unity_realtime_s)
        if marker.event == "NEURAL_BUFF_APPLIED" and not marker.trial_id:
            legacy_unattributed_buffs += 1
        if marker.event not in EVENTS:
            continue
        event_counts[marker.event] += 1
        trial_id = marker.trial_id
        if (not trial_id or len(trial_id) > 512 or
                marker.stimulus_epoch < 0 or marker.target not in {"sight", "guard"}):
            issue("INVALID_SELECTION_IDENTITY", session=sid, seq=marker.seq)
            continue
        prefix = f"selection-v24:{sid}:{marker.stimulus_epoch}:"
        suffix = trial_id[len(prefix):] if trial_id.startswith(prefix) else ""
        if not suffix.isascii() or not suffix.isdecimal() or int(suffix or 0) <= 0:
            issue("INVALID_SELECTION_CORRELATION_ID", session=sid, seq=marker.seq)
        key = (sid, trial_id)
        if key not in trials:
            trials[key] = {"target": marker.target, "epoch": marker.stimulus_epoch,
                           "observed": False, "accepted": False,
                           "denied": False, "effect": False, "unverified": False}
        trial = trials[key]
        if trial["target"] != marker.target or trial["epoch"] != marker.stimulus_epoch:
            issue("MUTATED_TRIAL_IDENTITY", session=sid, trial_id=trial_id)
        event = marker.event
        if event == "NEURAL_SELECTION_OBSERVED":
            if trial["observed"] or any(trial[t] for t in ("accepted", "denied", "effect", "unverified")):
                issue("DUPLICATE_OR_LATE_OBSERVATION", session=sid, trial_id=trial_id)
            trial["observed"] = True
        elif event == "NEURAL_SELECTION_DENIED":
            if not trial["observed"] or any(trial[t] for t in ("accepted", "denied", "effect", "unverified")):
                issue("INVALID_DENIAL_TRANSITION", session=sid, trial_id=trial_id)
            trial["denied"] = True
        elif event == "NEURAL_SELECTION_ACCEPTED":
            if not trial["observed"] or any(trial[t] for t in ("accepted", "denied", "effect", "unverified")):
                issue("INVALID_ACCEPTANCE_TRANSITION", session=sid, trial_id=trial_id)
            trial["accepted"] = True
        elif event in {"NEURAL_EFFECT_OBSERVED", "NEURAL_EFFECT_UNVERIFIED"}:
            if not trial["accepted"] or trial["denied"] or trial["effect"] or trial["unverified"]:
                issue("UNSUPPORTED_OR_DUPLICATE_EFFECT", session=sid, trial_id=trial_id)
            trial["effect" if event == "NEURAL_EFFECT_OBSERVED" else "unverified"] = True

    for (sid, trial_id), state in trials.items():
        if not state["observed"]:
            issue("MISSING_SELECTION_OBSERVATION", session=sid, trial_id=trial_id)
        if state["observed"] and not (state["accepted"] or state["denied"]):
            issue("MISSING_SELECTION_OUTCOME", session=sid, trial_id=trial_id)
        if state["accepted"] and not (state["effect"] or state["unverified"]):
            issue("MISSING_EFFECT_OBSERVATION", session=sid, trial_id=trial_id)
    if not trials:
        issue("NO_NEURAL_SELECTION_TRIALS")
    return {
        "schema": SCHEMA,
        "marker_count": count,
        "session_count": len(streams),
        "trial_count": len(trials),
        "event_counts": dict(sorted(event_counts.items())),
        "unattributed_legacy_buff_markers": legacy_unattributed_buffs,
        "software_consistent": not issues,
        "issues": issues,
        "source_stream_complete": False,
        "independent_effect_verified": False,
        "neural_intention_verified": False,
        "scientifically_qualified": False,
        "claim_boundary": (
            "Unity-emitted marker correlation is not independent game-state verification, "
            "a complete source journal, human intention, or valid EEG/display evidence."
        ),
    }
