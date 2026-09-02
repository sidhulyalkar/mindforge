#!/usr/bin/env python3
"""Apply Mindforge's tracked V0.29 overlay to a local Dragon Souls checkout.

The overlay is intentionally restricted to Assets/Mindforge. It never rewrites
upstream gameplay scripts, ProjectSettings, Packages, scenes, or third-party art
in-place. This keeps the first playable chassis qualification close to upstream
and makes every Mindforge-specific mutation easy to diff and remove.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OVERLAY_ROOT = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge"
EXPECTED_VERSION = "2021.3.20f1"


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def validate_project(project: Path) -> None:
    version = project / "ProjectSettings" / "ProjectVersion.txt"
    assets = project / "Assets"
    if not version.is_file() or not assets.is_dir():
        raise SystemExit(f"Not a Dragon Souls Unity project: {project}")
    editor = ""
    for line in version.read_text(encoding="utf-8").splitlines():
        if line.startswith("m_EditorVersion: "):
            editor = line.split(": ", 1)[1].strip()
            break
    if editor != EXPECTED_VERSION:
        raise SystemExit(
            f"V0.29 preserves upstream Unity {EXPECTED_VERSION}; found {editor or 'unknown'}"
        )


def copy_overlay(project: Path, source_commit: str) -> dict:
    if not OVERLAY_ROOT.is_dir():
        raise SystemExit(f"Tracked overlay missing: {OVERLAY_ROOT}")

    target = project / "Assets" / "Mindforge"
    if target.exists():
        shutil.rmtree(target)
    shutil.copytree(OVERLAY_ROOT, target)

    files = []
    for path in sorted(p for p in target.rglob("*") if p.is_file()):
        files.append(
            {
                "path": path.relative_to(project).as_posix(),
                "sha256": sha256(path),
            }
        )

    manifest = {
        "schema": "mindforge.dragonsouls_overlay.v1",
        "source_commit": source_commit,
        "overlay_scope": "Assets/Mindforge",
        "file_count": len(files),
        "files": files,
    }
    provenance = target / "Provenance"
    provenance.mkdir(parents=True, exist_ok=True)
    (provenance / "overlay-manifest.json").write_text(
        json.dumps(manifest, indent=2) + "\n", encoding="utf-8"
    )
    return manifest


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--source-commit", required=True)
    args = parser.parse_args()

    project = args.project.expanduser().resolve()
    validate_project(project)
    manifest = copy_overlay(project, args.source_commit)
    print(
        f"[Mindforge:V29] Applied {manifest['file_count']} tracked overlay files "
        f"under {manifest['overlay_scope']}."
    )


if __name__ == "__main__":
    main()
