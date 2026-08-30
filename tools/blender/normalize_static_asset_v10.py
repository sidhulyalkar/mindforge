#!/usr/bin/env python3
"""Blender-side static asset normalizer for Mindforge Content Foundry V0.10.

Run inside Blender, for example:
  blender --background --python tools/blender/normalize_static_asset_v10.py -- \
    --input source.glb --recipe content/recipes/architecture/cathedral_arch_v10.json \
    --output normalized.fbx --report normalized.report.json

Recipes use Unity coordinates (X right, Y up, Z forward). Blender operates Z-up, so
recipe bounds are explicitly mapped Unity [X,Y,Z] -> Blender [X,Z,Y] before fitting.
The FBX export then declares Y-up / -Z-forward for Unity ingestion.

This script only handles static presentation meshes. Character rigs/animations require a
separate pipeline so the static normalizer never destroys skeletal information by accident.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector


def args_after_separator() -> list[str]:
    return sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []


def parse_args():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", required=True)
    parser.add_argument("--recipe", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--report", required=True)
    return parser.parse_args(args_after_separator())


def load_recipe(path: Path) -> dict:
    value = json.loads(path.read_text(encoding="utf-8"))
    if value.get("schema") != "mindforge.content_asset_recipe.v1":
        raise ValueError("unsupported Mindforge asset recipe")
    if value.get("semantic_role") not in {"arch", "column", "door", "spire", "tree", "rock", "prop"}:
        raise ValueError("static normalizer cannot process humanoid/robot rig recipes")
    authority = value.get("authority", {})
    if any(authority.get(key) is not False for key in ("gameplay", "collision", "bci")):
        raise ValueError("content recipe violates presentation-only authority boundary")
    return value


def import_source(path: Path) -> None:
    suffix = path.suffix.lower()
    if suffix == ".fbx":
        bpy.ops.import_scene.fbx(filepath=str(path))
    elif suffix in {".glb", ".gltf"}:
        bpy.ops.import_scene.gltf(filepath=str(path))
    elif suffix == ".obj":
        if hasattr(bpy.ops.wm, "obj_import"):
            bpy.ops.wm.obj_import(filepath=str(path))
        else:
            bpy.ops.import_scene.obj(filepath=str(path))
    else:
        raise ValueError(f"unsupported static source format: {suffix}")


def mesh_objects():
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def strip_non_mesh_objects() -> None:
    for obj in list(bpy.context.scene.objects):
        if obj.type != "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)


def join_meshes():
    meshes = mesh_objects()
    if not meshes:
        raise ValueError("source contains no mesh objects")
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.hide_set(False)
        obj.hide_viewport = False
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = "MindforgeContent"
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return obj


def triangulate(obj) -> None:
    mesh = obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()


def world_bounds(obj):
    corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    low = Vector((min(v.x for v in corners), min(v.y for v in corners), min(v.z for v in corners)))
    high = Vector((max(v.x for v in corners), max(v.y for v in corners), max(v.z for v in corners)))
    return low, high


def unity_size_to_blender(target_size_unity) -> tuple[float, float, float]:
    """Unity X/Y/Z (Y-up) -> Blender X/Y/Z (Z-up)."""
    return float(target_size_unity[0]), float(target_size_unity[2]), float(target_size_unity[1])


def blender_size_to_unity(size_blender) -> list[float]:
    return [float(size_blender.x), float(size_blender.z), float(size_blender.y)]


def fit_uniform(obj, target_size_unity) -> float:
    low, high = world_bounds(obj)
    size = high - low
    if min(size.x, size.y, size.z) <= 1e-6:
        raise ValueError(f"source bounds are degenerate: {tuple(size)}")
    target_blender = unity_size_to_blender(target_size_unity)
    scales = [target_blender[i] / size[i] for i in range(3)]
    scale = min(scales)
    obj.scale = Vector((scale, scale, scale))
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return scale


def place_bottom_center_at_origin(obj) -> None:
    """Blender is Z-up: center X/Y footprint and put the lowest Z at zero."""
    low, high = world_bounds(obj)
    center_x = (low.x + high.x) * 0.5
    center_y = (low.y + high.y) * 0.5
    obj.location -= Vector((center_x, center_y, low.z))
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)


def validate_mesh(obj, recipe: dict) -> dict:
    mesh = obj.data
    mesh.calc_loop_triangles()
    triangles = len(mesh.loop_triangles)
    vertices = len(mesh.vertices)
    materials = len(mesh.materials)
    geometry = recipe["geometry"]
    render = recipe["render"]
    if triangles > int(geometry["max_triangles"]):
        raise ValueError(f"triangle budget exceeded: {triangles}/{geometry['max_triangles']}")
    if materials > int(render["max_materials"]):
        raise ValueError(f"material budget exceeded: {materials}/{render['max_materials']}")

    for vertex in mesh.vertices:
        values = tuple(vertex.co) + tuple(vertex.normal)
        if any(not math.isfinite(float(value)) for value in values):
            raise ValueError("mesh contains non-finite vertex or normal values")

    low, high = world_bounds(obj)
    size = high - low
    if min(size.x, size.y, size.z) <= 1e-6:
        raise ValueError("normalized mesh has zero/degenerate bounds")
    return {
        "vertices": vertices,
        "triangles": triangles,
        "materials": materials,
        "bounds_m_unity_xyz": blender_size_to_unity(size),
    }


def export_fbx(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    obj = bpy.context.view_layer.objects.active
    obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_triangles=True,
        add_leaf_bones=False,
        bake_anim=False,
    )


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    recipe_path = Path(args.recipe).resolve()
    output_path = Path(args.output).resolve()
    report_path = Path(args.report).resolve()
    recipe = load_recipe(recipe_path)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    import_source(input_path)
    strip_non_mesh_objects()
    obj = join_meshes()
    triangulate(obj)
    scale = fit_uniform(obj, recipe["geometry"]["target_size_m"])
    place_bottom_center_at_origin(obj)
    stats = validate_mesh(obj, recipe)
    export_fbx(output_path)

    report = {
        "schema": "mindforge.content_normalization_report.v1",
        "asset_id": recipe["asset_id"],
        "source": str(input_path),
        "output": str(output_path),
        "coordinate_contract": "recipe Unity X/Y/Z Y-up -> Blender X/Y/Z Z-up -> FBX Y-up -Z-forward",
        "uniform_scale_applied": scale,
        "authority": {"gameplay": False, "collision": False, "bci": False},
        "stats": stats,
    }
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2, sort_keys=True))


if __name__ == "__main__":
    main()
