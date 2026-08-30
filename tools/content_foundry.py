#!/usr/bin/env python3
"""Validate and fingerprint Mindforge Content Foundry recipes.

This tool does not generate assets or invoke external programs. It produces a stable
plan for downstream Blender/Unity steps while keeping gameplay, collision and BCI
authority outside the content pipeline.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_RECIPES = ROOT / "content" / "recipes"
DEFAULT_BINDINGS = ROOT / "content" / "local_asset_bindings.v1.json"
DEFAULT_PLAN = ROOT / "experiments" / "reports" / "content-foundry-plan.json"
RECIPE_SCHEMA = "mindforge.content_asset_recipe.v1"
BINDING_SCHEMA = "mindforge.local_asset_bindings.v1"
ROLES = {"column", "arch", "door", "spire", "tree", "rock", "prop", "humanoid", "robot"}
ASSET_ID = re.compile(r"^mf_[a-z0-9_]+$")
SHA256 = re.compile(r"^[0-9a-f]{64}$")


def canonical(value: Any) -> bytes:
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode()


def digest(value: Any) -> str:
    return hashlib.sha256(canonical(value)).hexdigest()


def load_recipes(root: Path) -> list[tuple[Path, dict[str, Any]]]:
    recipes = []
    for path in sorted(root.rglob("*.json")) if root.exists() else []:
        value = json.loads(path.read_text(encoding="utf-8"))
        if not isinstance(value, dict):
            raise ValueError(f"{path}: recipe root must be an object")
        recipes.append((path, value))
    return recipes


def validate_recipe(path: Path, data: dict[str, Any]) -> list[str]:
    e: list[str] = []
    label = path.as_posix()
    if data.get("schema") != RECIPE_SCHEMA:
        e.append(f"{label}: wrong schema")
    asset_id = data.get("asset_id")
    if not isinstance(asset_id, str) or not ASSET_ID.fullmatch(asset_id):
        e.append(f"{label}: invalid asset_id")
    if data.get("semantic_role") not in ROLES:
        e.append(f"{label}: invalid semantic_role")
    districts = data.get("districts")
    if not isinstance(districts, list) or not districts or len(districts) != len(set(districts)):
        e.append(f"{label}: districts must be non-empty and unique")

    source = data.get("source", {})
    if source.get("kind") not in {"generated_fallback", "authored", "local_art", "ai_generated"}:
        e.append(f"{label}: invalid source kind")
    if source.get("redistribution_policy") not in {"repository_safe", "local_only", "build_only"}:
        e.append(f"{label}: invalid redistribution policy")
    for key in ("tool", "tool_version", "license"):
        if not isinstance(source.get(key), str) or not source[key].strip():
            e.append(f"{label}: source.{key} is required")
    prompt_hash = source.get("prompt_hash")
    if prompt_hash is not None and (not isinstance(prompt_hash, str) or not SHA256.fullmatch(prompt_hash)):
        e.append(f"{label}: prompt_hash must be null or SHA-256")

    geometry = data.get("geometry", {})
    size = geometry.get("target_size_m")
    if not isinstance(size, list) or len(size) != 3 or any(not isinstance(v, (int, float)) or v <= 0 for v in size):
        e.append(f"{label}: target_size_m must contain three positive values")
    if not isinstance(geometry.get("max_triangles"), int) or geometry.get("max_triangles", 0) < 12:
        e.append(f"{label}: max_triangles is invalid")
    if not isinstance(geometry.get("max_submeshes"), int) or not 1 <= geometry.get("max_submeshes", 0) <= 16:
        e.append(f"{label}: max_submeshes is invalid")
    if geometry.get("forward_axis") == geometry.get("up_axis"):
        e.append(f"{label}: forward_axis and up_axis must differ")

    render = data.get("render", {})
    lod = render.get("lod_ratios")
    if not isinstance(lod, list) or not lod or lod[0] != 1.0 or any(lod[i] <= lod[i + 1] for i in range(len(lod) - 1)):
        e.append(f"{label}: lod_ratios must begin at 1.0 and strictly decrease")
    if not isinstance(render.get("max_materials"), int) or not 1 <= render.get("max_materials", 0) <= 8:
        e.append(f"{label}: max_materials is invalid")

    unity = data.get("unity", {})
    tokens = unity.get("target_tokens")
    if not isinstance(tokens, list) or not tokens or any(not isinstance(v, str) or not v.strip() for v in tokens):
        e.append(f"{label}: target_tokens are required")
    if not isinstance(unity.get("fallback_symbol"), str) or not unity.get("fallback_symbol", "").strip():
        e.append(f"{label}: fallback_symbol is required")

    authority = data.get("authority", {})
    for key in ("gameplay", "collision", "bci"):
        if authority.get(key) is not False:
            e.append(f"{label}: authority.{key} must be false")
    quality = data.get("quality", {})
    for key in ("require_finite_normals", "require_nonzero_bounds", "reject_magenta_material"):
        if quality.get(key) is not True:
            e.append(f"{label}: quality.{key} must remain true")
    score = quality.get("minimum_score")
    if not isinstance(score, (int, float)) or not 0 <= score <= 1:
        e.append(f"{label}: minimum_score must be 0..1")
    return e


def load_bindings(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {"schema": BINDING_SCHEMA, "bindings": []}
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError("binding manifest must be an object")
    return value


def validate(recipes_root: Path, bindings_path: Path):
    recipes = load_recipes(recipes_root)
    errors: list[str] = []
    if not recipes:
        errors.append("no content recipes found")
    ids: set[str] = set()
    for path, recipe in recipes:
        errors.extend(validate_recipe(path, recipe))
        asset_id = recipe.get("asset_id")
        if asset_id in ids:
            errors.append(f"duplicate asset_id: {asset_id}")
        if isinstance(asset_id, str):
            ids.add(asset_id)

    bindings = load_bindings(bindings_path)
    if bindings.get("schema") != BINDING_SCHEMA or not isinstance(bindings.get("bindings"), list):
        errors.append("invalid local asset binding manifest")
    else:
        bound: set[str] = set()
        for item in bindings["bindings"]:
            if not isinstance(item, dict) or item.get("asset_id") not in ids:
                errors.append("binding references unknown asset_id")
                continue
            if item["asset_id"] in bound:
                errors.append(f"duplicate binding: {item['asset_id']}")
            bound.add(item["asset_id"])
            unity_path = item.get("unity_asset_path")
            if not isinstance(unity_path, str) or not unity_path.startswith("Assets/Mindforge/LocalArt/") or ".." in unity_path:
                errors.append(f"unsafe local binding path for {item['asset_id']}")
    return recipes, bindings, errors


def make_plan(recipes, bindings, blender_present: bool) -> dict[str, Any]:
    manifest = [
        {
            "path": path.relative_to(ROOT).as_posix(),
            "asset_id": data["asset_id"],
            "role": data["semantic_role"],
            "fingerprint": digest(data),
        }
        for path, data in recipes
    ]
    recipe_hash = digest(manifest)
    binding_hash = digest(bindings)
    validation_hash = digest(["content-foundry-v1", recipe_hash, binding_hash])
    normalization_hash = digest([validation_hash, "normalize-v1", blender_present])
    unity_hash = digest([normalization_hash, "unity-ingest-v10"])
    capture_hash = digest([unity_hash, "visual-capture-v10"])
    bound_count = len(bindings.get("bindings", []))
    return {
        "schema": "mindforge.content_foundry_plan.v1",
        "generated_utc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "authority": {"gameplay": false, "collision": false, "bci": false},
        "recipe_count": len(recipes),
        "bound_local_asset_count": bound_count,
        "recipes": manifest,
        "fingerprints": {
            "recipes": recipe_hash,
            "bindings": binding_hash,
            "validate": validation_hash,
            "normalize": normalization_hash,
            "unity_ingest": unity_hash,
            "visual_capture": capture_hash
        },
        "stages": [
            {"id": "validate", "status": "ready", "observed_runtime_evidence": false},
            {"id": "normalize", "status": "ready" if bound_count == 0 or blender_present else "external_tool_missing", "required": bound_count > 0, "observed_runtime_evidence": false},
            {"id": "unity_ingest", "status": "requires_unity_editor", "observed_runtime_evidence": false},
            {"id": "visual_capture", "status": "requires_unity_editor", "observed_runtime_evidence": false}
        ]
    }


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--recipes", default=str(DEFAULT_RECIPES))
    parser.add_argument("--bindings", default=str(DEFAULT_BINDINGS))
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("validate")
    sub.add_parser("fingerprint")
    plan = sub.add_parser("plan")
    plan.add_argument("--output", default=str(DEFAULT_PLAN))
    plan.add_argument("--blender-present", action="store_true")
    args = parser.parse_args(argv)

    try:
        recipes, bindings, errors = validate(Path(args.recipes).resolve(), Path(args.bindings).resolve())
    except Exception as exc:
        print(f"content-foundry: {exc}", file=sys.stderr)
        return 2
    if errors:
        for error in errors:
            print(f"ERROR {error}", file=sys.stderr)
        return 1

    fp = digest({"recipes": [digest(data) for _, data in recipes], "bindings": bindings})
    if args.command == "validate":
        print(f"Content Foundry PASS: {len(recipes)} recipes, fingerprint={fp}")
    elif args.command == "fingerprint":
        print(fp)
    elif args.command == "plan":
        output = Path(args.output).resolve()
        output.parent.mkdir(parents=True, exist_ok=True)
        value = make_plan(recipes, bindings, args.blender_present)
        output.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(json.dumps(value, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
