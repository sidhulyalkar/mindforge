"""Offline, redacted, derived-only EEG calibration reports and standalone HTML.

No raw EEG enters Unity. This research visualization does not authorize gameplay.
Scalp electrode positions are schematic, not cortical source localization.
"""
from __future__ import annotations

from dataclasses import asdict
import hashlib
import html
import json
from pathlib import Path
from typing import Any, Sequence

import numpy as np

from .adaptive_spatial import DEFAULT_GROUPS, evaluate_electrode_groups

SCHEMA = "mindforge.calibration_lab.v2"
ELECTRODE_XY = {
    "FP1":(-.27,-.78), "FPZ":(0,-.82), "FP2":(.27,-.78),
    "F7":(-.75,-.42), "F3":(-.36,-.45), "FZ":(0,-.48), "F4":(.36,-.45), "F8":(.75,-.42),
    "FT7":(-.86,-.24), "FC3":(-.39,-.24), "FCZ":(0,-.23), "FC4":(.39,-.24), "FT8":(.86,-.24),
    "T7":(-.87,0), "C3":(-.46,0), "CZ":(0,0), "C4":(.46,0), "T8":(.87,0),
    "TP7":(-.86,.25), "CP3":(-.39,.24), "CPZ":(0,.23), "CP4":(.39,.24), "TP8":(.86,.25),
    "P7":(-.76,.45), "P3":(-.36,.47), "PZ":(0,.47), "P4":(.36,.47), "P8":(.76,.45),
    "PO7":(-.47,.66), "PO3":(-.25,.63), "POZ":(0,.66), "PO4":(.25,.63), "PO8":(.47,.66),
    "O1":(-.27,.81), "OZ":(0,.85), "O2":(.27,.81),
}


def synthetic_research_epochs(seed: int = 31):
    """Artificial signal with central-group class correlation; NOT imagined speech evidence."""
    rng = np.random.default_rng(seed)
    channels = ("FZ", "C3", "CZ", "C4", "PZ", "PO7", "OZ", "PO8")
    fs, n = 250., 500
    t = np.arange(n) / fs
    epochs, labels, sessions = [], [], []
    for day in range(3):
        for trial in range(40):
            cls = trial % 2
            x = rng.normal(0, 1.0, (len(channels), n))
            x[1] += (1.0 + cls * 2.5) * np.sin(2 * np.pi * 12 * t + rng.uniform(0, 6.2))
            x[2] += (1.0 + cls * 2.0) * np.sin(2 * np.pi * 17 * t + rng.uniform(0, 6.2))
            epochs.append(x)
            labels.append("bridge" if cls else "wall")
            sessions.append(f"synthetic-day-{day+1}")
    return np.asarray(epochs), labels, sessions, channels, fs


def _counts(labels, mask, classes):
    return {
        "trials": int(mask.sum()),
        "class_counts": {str(c): int(np.sum(labels[mask] == c)) for c in classes},
    }


def build_lab_report(
    epochs_uv: np.ndarray, labels: Sequence[str], session_ids: Sequence[str],
    channel_names: Sequence[str], sample_rate_hz: float, *,
    task: str = "imagined_speech", source_kind: str = "unverified",
) -> dict[str, Any]:
    """Freeze candidate selection on validation; report holdout and redacted provenance."""
    if source_kind not in {"synthetic_eeg", "recorded_eeg", "unverified"}:
        raise ValueError("source_kind must be synthetic_eeg, recorded_eeg, or unverified")
    selection = evaluate_electrode_groups(
        epochs_uv, labels, session_ids, channel_names, sample_rate_hz, task=task,
    )
    y, sessions = np.asarray(labels, dtype=str), np.asarray(session_ids, dtype=str)
    audit = {
        "training": _counts(y, np.isin(sessions, list(selection.train_sessions)), selection.class_labels),
        "validation": _counts(y, sessions == selection.validation_session, selection.class_labels),
        "holdout": _counts(y, sessions == selection.holdout_session, selection.class_labels),
        "train_session_count": len(selection.train_sessions),
        "validation_to_holdout_gap": round(
            selection.validation_balanced_accuracy - selection.holdout_balanced_accuracy, 6),
    }
    spatial = asdict(selection)
    spatial["train_sessions"] = [f"train-{i+1}" for i in range(len(selection.train_sessions))]
    spatial["validation_session"] = "validation-redacted"
    spatial["holdout_session"] = "holdout-redacted"
    available = set(spatial["channel_names"])
    coverage = {
        group.name: {
            "present": [e for e in group.electrodes if e in available],
            "missing": [e for e in group.electrodes if e not in available],
        } for group in DEFAULT_GROUPS
    }
    unmapped = [e for e in spatial["channel_names"] if e not in ELECTRODE_XY]
    warnings = list(spatial["warnings"])
    warnings.extend({
        "synthetic_eeg": ["SYNTHETIC_DEMO_NOT_HUMAN_EVIDENCE"],
        "recorded_eeg": ["RECORDING_PROVENANCE_SELF_DECLARED_NOT_VERIFIED"],
        "unverified": ["PROVENANCE_UNVERIFIED"],
    }[source_kind])
    if unmapped:
        warnings.append("UNMAPPED_ELECTRODES_RENDERED_IN_TEXT_ONLY")
    if abs(audit["validation_to_holdout_gap"]) >= 0.15:
        warnings.append("LARGE_VALIDATION_HOLDOUT_GAP_REVIEW_GENERALIZATION")
    report = {
        "schema": SCHEMA, "task": selection.task, "source_kind": source_kind,
        "source_kind_verified": False,
        "analysis": "time-split spectral log-bandpower / nearest prototype baseline",
        "sample_rate_hz": float(sample_rate_hz),
        "montage": {
            "selected": spatial["chosen_electrodes"], "available": spatial["channel_names"],
            "head_coordinates": [
                {"electrode": e, "x": ELECTRODE_XY[e][0], "y": ELECTRODE_XY[e][1]}
                for e in spatial["channel_names"] if e in ELECTRODE_XY
            ],
            "unmapped": unmapped, "group_coverage": coverage,
            "projection": "illustrative scalp schematic, not cortical source localization",
        },
        "selection": spatial, "audit": audit,
        "gates": {
            "passes_offline_screen": bool(selection.passes_offline_screen),
            "human_speech_decoding_demonstrated": False,
            "live_gameplay_authorized": False,
        },
        "warnings": warnings,
    }
    canonical = json.dumps(report, sort_keys=True, ensure_ascii=True, separators=(",", ":"))
    report["report_id"] = hashlib.sha256(canonical.encode()).hexdigest()[:16]
    return report


def report_json(report: dict[str, Any]) -> str:
    if report.get("schema") != SCHEMA:
        raise ValueError("unsupported calibration lab schema")
    return json.dumps(report, sort_keys=True, ensure_ascii=True, indent=2, allow_nan=False) + "\n"


def _esc(x) -> str:
    return html.escape(str(x), quote=True)


def _pct(x) -> str:
    return f"{float(x) * 100:.1f}%"


def render_lab_html(report: dict[str, Any]) -> str:
    """CSP-restricted, JS-free standalone report. All display strings HTML-escaped."""
    if report.get("schema") != SCHEMA:
        raise ValueError("unsupported calibration lab schema")
    selection, montage, audit = report["selection"], report["montage"], report["audit"]
    chosen = set(selection["chosen_electrodes"])
    available = set(montage["available"])
    dots = []
    for name, (x, y) in ELECTRODE_XY.items():
        cx, cy = 240 + x * 149, 210 + y * 149
        state = "selected" if name in chosen else "recorded" if name in available else "missing"
        radius = 10 if state != "missing" else 7
        label = (f'<text x="{cx:.1f}" y="{cy+23:.1f}" text-anchor="middle">{_esc(name)}</text>'
                 if state != "missing" else "")
        dots.append(f'<g class="{state}"><circle cx="{cx:.1f}" cy="{cy:.1f}" r="{radius}"/>{label}</g>')
    bars = []
    for name, score in sorted(selection["candidate_validation_balanced_accuracy"].items(),
                              key=lambda item: (-item[1], item[0])):
        label = " · selected" if name == selection["chosen_group"] else ""
        bars.append(
            f'<div class="bar"><span>{_esc(name)}{_esc(label)}</span>'
            f'<div class="track" aria-label="{_esc(name)} validation {_pct(score)}">'
            f'<div class="fill" style="width:{max(0., min(100., float(score)*100)):.1f}%"></div></div>'
            f'<b>{_pct(score)}</b></div>'
        )
    trial_rows = []
    for partition in ("training", "validation", "holdout"):
        info = audit[partition]
        counts = ", ".join(f"{_esc(k)}: {int(v)}" for k, v in info["class_counts"].items())
        trial_rows.append(
            f"<tr><th>{_esc(partition.title())}</th><td>{int(info['trials'])}</td><td>{counts}</td></tr>"
        )
    montage_rows = []
    for group, coverage in montage["group_coverage"].items():
        present = ", ".join(_esc(v) for v in coverage["present"]) or "None"
        missing = ", ".join(_esc(v) for v in coverage["missing"]) or "None"
        montage_rows.append(f"<tr><th>{_esc(group)}</th><td>{present}</td><td>{missing}</td></tr>")
    warnings = "".join(f"<li>{_esc(w)}</li>" for w in report["warnings"])
    status = "Offline screen passed" if report["gates"]["passes_offline_screen"] else "Offline screen not passed"
    proxy_val = selection["high_frequency_proxy_validation_accuracy"]
    proxy_test = selection["high_frequency_proxy_holdout_accuracy"]
    proxy_val = _pct(proxy_val) if proxy_val is not None else "Unavailable"
    proxy_test = _pct(proxy_test) if proxy_test is not None else "Unavailable"
    return f'''<!doctype html><html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<meta name="referrer" content="no-referrer">
<meta http-equiv="Content-Security-Policy" content="default-src 'none';style-src 'unsafe-inline';img-src data:;base-uri 'none';form-action 'none'">
<title>Mindforge · Neural Calibration Lab</title>
<style>
:root{{--bg:#09111c;--panel:#101c2c;--line:#2c4056;--ink:#ebf5fd;--muted:#9cb5ca;--a:#5bd7df}}
*{{box-sizing:border-box}}body{{background:radial-gradient(ellipse at 10% 0,#1b3443,var(--bg) 60%);color:var(--ink);margin:0;font:15px/1.55 system-ui,-apple-system,sans-serif}}
main{{max-width:1160px;margin:auto;padding:42px 28px}}header{{display:flex;justify-content:space-between;gap:20px;align-items:start;flex-wrap:wrap}}
h1{{font-size:clamp(30px,5vw,49px);letter-spacing:-.04em;margin:4px 0 6px}}h2{{font-size:20px;margin:0 0 16px}}
p{{color:var(--muted)}}.eyebrow{{font-size:12px;letter-spacing:.13em;color:var(--a)}}.tag{{color:var(--muted);border:1px solid var(--line);padding:10px 14px;border-radius:99px;font-size:12px}}
.stats{{display:grid;grid-template-columns:repeat(3,1fr);gap:12px;margin:27px 0 18px}}
.metric,.panel{{background:var(--panel);border:1px solid var(--line);border-radius:17px;padding:23px}}
.metric b{{font-size:29px;display:block;letter-spacing:-.04em}}.metric small{{display:block;color:var(--muted)}}
.grid{{display:grid;grid-template-columns:1fr 1fr;gap:18px}}.wide{{grid-column:1/-1}}
.head{{max-width:100%;height:auto;max-height:390px}}svg .scalp{{fill:#0c1b2a;stroke:#59728b;stroke-width:2}}
svg .nose,svg .ears{{fill:none;stroke:#59728b;stroke-width:3}}
svg circle{{stroke-width:1.8}}svg text{{fill:#bfd1e2;font-size:9px}}
svg .selected circle{{fill:#57dbe4;stroke:#e0ffff}}svg .recorded circle{{fill:#57759d;stroke:#bad4ed}}svg .missing circle{{fill:#192a3e;stroke:#344d65}}
.legend{{font-size:12px;color:var(--muted)}}.bar{{display:grid;grid-template-columns:155px 1fr 62px;align-items:center;gap:12px;margin:18px 0;font-size:13px}}
.track{{height:13px;border-radius:12px;background:#25384e;overflow:hidden}}.fill{{height:100%;border-radius:12px;background:linear-gradient(90deg,#328c9b,#67e1dd)}}
.notice{{border:1px solid #665839;background:#28271f;border-radius:14px;padding:14px 17px;margin-top:28px}}
.notice strong{{color:#f4d18f}}.notice p{{color:#d0cbb9}}table{{border-collapse:collapse;width:100%;text-align:left;font-size:13px}}
th,td{{text-align:left;border-bottom:1px solid var(--line);padding:10px 8px;overflow-wrap:anywhere}}td{{color:var(--muted)}}
li{{padding-bottom:5px;color:var(--muted);overflow-wrap:anywhere}}.small{{font-size:12px;color:var(--muted)}}footer{{padding-top:25px;font-size:12px;color:var(--muted)}}
@media(max-width:760px){{main{{padding:24px 13px}}.grid{{grid-template-columns:1fr}}.stats{{grid-template-columns:1fr 1fr}}.metric,.panel{{padding:17px}}.bar{{grid-template-columns:120px 1fr 54px;gap:7px}}}}
</style></head><body><main><header><div><div class="eyebrow">MINDFORGE / RESEARCH TOOLS / V2</div>
<h1>Neural Calibration Lab</h1><p>Held-out scalp sensor evaluation. No raw EEG or automatic gameplay authority.</p></div>
<div class="tag">Report {_esc(report["report_id"])}<br>Source: {_esc(report["source_kind"])}</div></header>
<section class="stats"><div class="metric"><b>{_pct(selection["validation_balanced_accuracy"])}</b><small>Validation balanced accuracy</small></div>
<div class="metric"><b>{_pct(selection["holdout_balanced_accuracy"])}</b><small>Untouched session accuracy</small></div>
<div class="metric"><b>{_pct(selection["chance_balanced_accuracy"])}</b><small>Balanced chance reference</small></div></section>
<div class="grid"><section class="panel"><h2>Scalp sensor map</h2>
<svg viewBox="0 0 480 435" class="head" role="img" aria-label="Illustrative scalp sensor map">
<ellipse cx="240" cy="210" rx="157" ry="174" class="scalp"/>
<path d="M219 37 L240 14 L261 37" class="nose"/>
<path d="M85 166 Q59 182 72 229 Q77 252 91 250 M395 166 Q421 182 408 229 Q403 252 389 250" class="ears"/>
{''.join(dots)}</svg><p class="legend">Turquoise: selected · Blue: recorded · Dark: absent</p>
<p class="small">Stylized scalp projection only, not cortical source localization.</p>
<p class="small">Selected: {_esc(", ".join(selection["chosen_electrodes"]))}<br>
Unmapped: {_esc(", ".join(montage["unmapped"]) or "None")}</p></section>
<section class="panel"><h2>Candidate comparison</h2><p class="small">
Groups selected on validation session only. Holdout was reserved for final evaluation.</p>
{''.join(bars)}<div class="notice"><strong>{_esc(status)}</strong>
<p>A passing offline screen does not prove imagined speech, cortical origin, or gameplay safety.</p></div>
<p class="small">High-frequency proxy · validation: {proxy_val} · holdout: {proxy_test}</p></section>
<section class="panel wide"><h2>Session evidence and validity</h2>
<p>Task: <b>{_esc(report["task"])}</b> · Sample rate: {float(report["sample_rate_hz"]):g} Hz · {_esc(report["analysis"])}</p>
<p>Source kind is self-declared; real human speech decoding demonstrated: <b>No</b>. Live gameplay authorized: <b>No</b>.</p>
<table><thead><tr><th>Session partition</th><th>Epochs</th><th>Class counts</th></tr></thead>
<tbody>{''.join(trial_rows)}</tbody></table>
<p class="small">Validation minus holdout balanced accuracy: {audit["validation_to_holdout_gap"]:+.3f}</p>
<h3>Warnings and limitations</h3><ul>{warnings}</ul>
<details><summary>Montage coverage details</summary>
<table><thead><tr><th>Sensor family</th><th>Recorded</th><th>Missing</th></tr></thead>
<tbody>{''.join(montage_rows)}</tbody></table></details></section></div>
<footer>Derived-only report. No external assets, scripts, or network requests. Synthetic performance is not human evidence.</footer>
</main></body></html>'''


def write_lab_artifacts(report: dict[str, Any], output_dir: str | Path) -> tuple[Path, Path]:
    folder = Path(output_dir)
    folder.mkdir(parents=True, exist_ok=True)
    json_path, html_path = folder / "calibration.json", folder / "index.html"
    json_path.write_text(report_json(report), encoding="utf-8")
    html_path.write_text(render_lab_html(report), encoding="utf-8")
    return json_path, html_path
