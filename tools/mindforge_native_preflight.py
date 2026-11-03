#!/usr/bin/env python3
"""Read-only local build-reference preflight for Mindforge's native game mods.

No game launch, DLL copy, plugin install, reverse engineering, or private EEG
data access. Hashes are only provenance for the exact locally owned assembly
files. A passing report is NOT proof that runtime hooks or mods work.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import shutil


def fingerprint(file: Path) -> dict[str, object]:
    if not file.is_file():
        return {"present": False, "bytes": 0, "sha256": None}
    digest = hashlib.sha256()
    count = 0
    with file.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
            count += len(chunk)
    return {"present": True, "bytes": count, "sha256": digest.hexdigest()}


def inspect_silksong(root: Path) -> dict[str, object]:
    """Returns a portable manifest without serializing the user's home path."""
    root = root.expanduser()
    expected = root / "Hollow Knight Silksong_Data" / "Managed"
    choices = [expected] if expected.is_dir() else list(root.glob("*_Data/Managed"))
    if len(choices) != 1:
        managed = expected
        managed_detected = False
    else:
        managed = choices[0]
        managed_detected = True
    core = root / "BepInEx" / "core"
    files = {
        "bepinex_loader": core / "BepInEx.dll",
        "harmony": core / "0Harmony.dll",
        "unity_legacy": managed / "UnityEngine.dll",
        "unity_core": managed / "UnityEngine.CoreModule.dll",
        "unity_imgui": managed / "UnityEngine.IMGUIModule.dll",
        "unity_text": managed / "UnityEngine.TextRenderingModule.dll",
    }
    fingerprints = {key: fingerprint(path) for key, path in files.items()}
    loader = fingerprints["bepinex_loader"]["present"]
    harmony = fingerprints["harmony"]["present"]
    unity = (fingerprints["unity_legacy"]["present"] or
             fingerprints["unity_core"]["present"])
    ui = (fingerprints["unity_legacy"]["present"] or
          fingerprints["unity_imgui"]["present"])
    errors = []
    if not managed_detected:
        errors.append("expected exactly one recognized *_Data/Managed folder")
    if not loader:
        errors.append("missing BepInEx/core/BepInEx.dll")
    if not harmony:
        errors.append("missing BepInEx/core/0Harmony.dll")
    if not unity:
        errors.append("missing UnityEngine.dll or UnityEngine.CoreModule.dll")
    if not ui:
        errors.append("missing Unity IMGUI module required for the orb overlay")

    return {
        "schema": "mindforge.native_build_preflight.v1",
        "game_id": "silksong",
        "file_signatures": fingerprints,
        "managed_dir_detected": managed_detected,
        "dotnet_sdk_in_path": shutil.which("dotnet") is not None,
        "build_reference_prerequisites_present": not errors,
        "missing_prerequisites": errors,
        "native_plugin_compiled": False,
        "runtime_hook_validated": False,
        "in_game_overlay_verified": False,
        "source_of_truth": "local_owned_game_assembly_files",
        "evidence_level": "filesystem_only_not_runtime_qualification",
    }



def inspect_hollow_knight(root: Path) -> dict[str, object]:
    """Hollow Knight has a separate API; do not require BepInEx or Harmony."""
    root = root.expanduser()
    expected = root / "hollow_knight_Data" / "Managed"
    choices = [expected] if expected.is_dir() else list(root.glob("*_Data/Managed"))
    if len(choices) != 1:
        managed = expected
        managed_detected = False
    else:
        managed = choices[0]
        managed_detected = True
    files = {
        "game_and_modding_api": managed / "Assembly-CSharp.dll",
        "unity_legacy": managed / "UnityEngine.dll",
        "unity_core": managed / "UnityEngine.CoreModule.dll",
        "unity_imgui": managed / "UnityEngine.IMGUIModule.dll",
        "unity_text": managed / "UnityEngine.TextRenderingModule.dll",
    }
    fingerprints = {key: fingerprint(path) for key, path in files.items()}
    errors = []
    if not managed_detected:
        errors.append("expected exactly one recognized *_Data/Managed folder")
    if not fingerprints["game_and_modding_api"]["present"]:
        errors.append("missing *_Data/Managed/Assembly-CSharp.dll")
    if not (fingerprints["unity_legacy"]["present"] or
            fingerprints["unity_core"]["present"]):
        errors.append("missing UnityEngine.dll or UnityEngine.CoreModule.dll")
    if not (fingerprints["unity_legacy"]["present"] or
            fingerprints["unity_imgui"]["present"]):
        errors.append("missing Unity IMGUI module required by the optional orb overlay")
    # The existence of Assembly-CSharp.dll does NOT prove it contains
    # hk-modding/API. This must be separately confirmed by an actual build.
    return {
        "schema": "mindforge.native_build_preflight.v1",
        "game_id": "hollow_knight",
        "file_signatures": fingerprints,
        "managed_dir_detected": managed_detected,
        "dotnet_sdk_in_path": shutil.which("dotnet") is not None,
        "build_reference_prerequisites_present": not errors,
        "missing_prerequisites": errors,
        "modding_api_verified": False,
        "native_plugin_compiled": False,
        "runtime_hook_validated": False,
        "in_game_overlay_verified": False,
        "source_of_truth": "local_owned_game_assembly_files",
        "evidence_level": "filesystem_only_not_runtime_qualification",
    }


def inspect_cuphead(root: Path) -> dict[str, object]:
    """Cuphead's legacy Unity build must not inherit Silksong's net46 target."""
    root = root.expanduser()
    expected = root / "Cuphead_Data" / "Managed"
    choices = [expected] if expected.is_dir() else list(root.glob("*_Data/Managed"))
    if len(choices) != 1:
        managed = expected
        managed_detected = False
    else:
        managed = choices[0]
        managed_detected = True
    core = root / "BepInEx" / "core"
    paths = {
        "bepinex_loader": core / "BepInEx.dll",
        "game_managed": managed / "Assembly-CSharp.dll",
        "unity_legacy": managed / "UnityEngine.dll",
        "unity_core": managed / "UnityEngine.CoreModule.dll",
    }
    signatures = {name: fingerprint(path) for name, path in paths.items()}
    errors = []
    if not managed_detected:
        errors.append("expected one recognized Cuphead_Data/Managed directory")
    if not signatures["bepinex_loader"]["present"]:
        errors.append("missing BepInEx/core/BepInEx.dll")
    if not signatures["game_managed"]["present"]:
        errors.append("missing Cuphead_Data/Managed/Assembly-CSharp.dll")
    if not (signatures["unity_legacy"]["present"] or
            signatures["unity_core"]["present"]):
        errors.append("missing UnityEngine.dll or UnityEngine.CoreModule.dll")
    return {
        "schema": "mindforge.native_build_preflight.v1",
        "game_id": "cuphead",
        "file_signatures": signatures,
        "managed_dir_detected": managed_detected,
        "dotnet_sdk_in_path": shutil.which("dotnet") is not None,
        "build_reference_prerequisites_present": not errors,
        "missing_prerequisites": errors,
        "required_target_framework": "net35",
        "runtime_compatibility_verified": False,
        "native_plugin_compiled": False,
        "runtime_hook_validated": False,
        "in_game_overlay_verified": False,
        "source_of_truth": "local_owned_game_assembly_files",
        "evidence_level": "filesystem_only_not_runtime_qualification",
    }


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--game", choices=("silksong", "hollow_knight", "cuphead"),
                   default="silksong", help="Native game adapter to check")
    p.add_argument("--game-root", type=Path, required=True,
                   help="Installed game root directory")
    p.add_argument("--report", type=Path, help="Optional JSON report output")
    args = p.parse_args()
    result = (inspect_silksong(args.game_root) if args.game == 'silksong'
              else inspect_hollow_knight(args.game_root)
              if args.game == 'hollow_knight'
              else inspect_cuphead(args.game_root))
    text = json.dumps(result, indent=2, sort_keys=True) + "\n"
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(text, encoding="utf-8")
    print(text, end="")
    raise SystemExit(0 if result["build_reference_prerequisites_present"] else 2)


if __name__ == "__main__":
    main()
