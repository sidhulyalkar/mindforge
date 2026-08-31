from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PRESENTATION = ROOT / "unity/Assets/Mindforge/Presentation"
EDITOR = ROOT / "unity/Assets/Mindforge/Editor"
MATERIALS = PRESENTATION / "LegacyMaterialHierarchyV16.cs"
OCCLUSION = PRESENTATION / "CameraOcclusionGhostV16.cs"
BACKDROP = PRESENTATION / "WorldDepthBackdropV16.cs"
V11 = EDITOR / "MindforgeDemoV11Builder.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def test_v16_recording_fixes_scan_the_actual_latest_world_root():
    canonical = '"Mindforge_Demo_World_V11"'
    for path in (MATERIALS, OCCLUSION, BACKDROP):
        text = read(path)
        assert canonical in text, f"{path.name} does not scan canonical Latest world"

    builder = read(V11)
    assert 'RootName = "Mindforge_Demo_World_V11"' in builder


def test_canonical_fracture_spires_are_camera_ghost_candidates_not_new_collision_authority():
    builder = read(V11)
    occlusion = read(OCCLUSION)
    assert 'Spire($"FractureSpire_{i}"' in builder
    assert '"Spire"' in occlusion
    assert "renderer.enabled = targetEnabled" in occlusion
    assert "collider.enabled" not in occlusion
    assert "SetActive(" not in occlusion


def test_v16_material_hierarchy_can_restyle_v11_dark_architecture_but_preserves_emissive_signals():
    materials = read(MATERIALS)
    assert '"Mindforge_Demo_World_V11"' in materials
    assert 'material.IsKeywordEnabled("_EMISSION")' in materials
    assert '"SightVepCore"' in materials
    assert '"GuardVepCore"' in materials
    assert "MaterialPropertyBlock" in materials
    assert "renderer.SetPropertyBlock(_block)" in materials
    assert "renderer.sharedMaterial =" not in materials


def test_v16_depth_uses_v11_playable_bounds_without_recursing_existing_skyline():
    backdrop = read(BACKDROP)
    assert '"Mindforge_Demo_World_V11"' in backdrop
    for token in ('"Skyline"', '"Distant"', '"Backdrop"', '"Horizon"', '"Vista"'):
        assert token in backdrop
    assert "IgnoreForBounds(renderer.gameObject.name)" in backdrop
    assert "candidate.extents.x > 80f" in backdrop
    assert "candidate.extents.z > 110f" in backdrop
