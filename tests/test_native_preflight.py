from __future__ import annotations

import hashlib
import json
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "tools/mindforge_native_preflight.py"


def call(root: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(SCRIPT), "--game-root", str(root)],
        cwd=ROOT, capture_output=True, text=True, timeout=10,
    )


def test_preflight_records_hashes_but_does_not_claim_plugin_or_gameplay_works(tmp_path):
    core = tmp_path / "BepInEx/core"
    managed = tmp_path / "Hollow Knight Silksong_Data/Managed"
    core.mkdir(parents=True)
    managed.mkdir(parents=True)
    for path in (core / "BepInEx.dll", core / "0Harmony.dll",
                 managed / "UnityEngine.CoreModule.dll",
                 managed / "UnityEngine.IMGUIModule.dll"):
        path.write_bytes(b"synthetic-assembly-fixture")
    x = call(tmp_path)
    assert x.returncode == 0, x.stderr + x.stdout
    result = json.loads(x.stdout)
    assert result["build_reference_prerequisites_present"]
    assert result["file_signatures"]["harmony"]["sha256"] == hashlib.sha256(
        b"synthetic-assembly-fixture").hexdigest()
    assert result["native_plugin_compiled"] is False
    assert result["runtime_hook_validated"] is False
    assert result["in_game_overlay_verified"] is False
    assert result["evidence_level"] == "filesystem_only_not_runtime_qualification"
    assert str(tmp_path) not in x.stdout


def test_preflight_missing_loader_and_gui_is_nonzero_and_readonly(tmp_path):
    original = sorted(tmp_path.iterdir())
    x = call(tmp_path)
    assert x.returncode == 2
    result = json.loads(x.stdout)
    assert not result["build_reference_prerequisites_present"]
    assert "missing BepInEx/core/BepInEx.dll" in result["missing_prerequisites"]
    assert any("IMGUI" in issue for issue in result["missing_prerequisites"])
    assert sorted(tmp_path.iterdir()) == original
