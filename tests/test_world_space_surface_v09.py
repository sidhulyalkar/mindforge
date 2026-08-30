import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SHADER = ROOT / "unity/Assets/Mindforge/Shaders/ProductionTriplanarLitV09.shader"
MATERIALS = ROOT / "unity/Assets/Mindforge/Editor/ProductionMaterialAuthoringV09.cs"
MANIFEST = ROOT / "third_party/manifest.json"


def read(path: Path) -> str:
    assert path.exists(), path
    return path.read_text(encoding="utf-8")


def test_shader_uses_world_position_and_three_axis_blending_instead_of_object_uv_scale():
    text = read(SHADER)
    assert 'Shader "Mindforge/ProductionTriplanarLitV09"' in text
    assert "float3 positionWS : TEXCOORD0" in text
    assert "TriplanarWeights" in text
    assert "BuildWorldUvs" in text
    assert "positionWS.z * axisSign.x" in text
    assert "positionWS.x, positionWS.z * axisSign.y" in text
    assert "positionWS.x * axisSign.z" in text
    assert "_MetersPerTile" in text
    assert "TRANSFORM_TEX" not in text.split('Name "ForwardLit"', 1)[1].split('Name "ShadowCaster"', 1)[0]


def test_shader_supports_triplanar_albedo_and_deterministic_rgb_normal_mapping():
    text = read(SHADER)
    forward = text.split('Name "ForwardLit"', 1)[1].split('Name "ShadowCaster"', 1)[0]
    assert "SampleTriplanarAlbedo" in forward
    assert forward.count("SAMPLE_TEXTURE2D(_BaseMap") == 3
    assert "DecodeGeneratedNormal" in forward
    assert "packed.xyz * 2.0h - 1.0h" in forward
    assert forward.count("SAMPLE_TEXTURE2D(_BumpMap") == 3
    assert "UnpackNormalScale" not in forward
    assert "SampleTriplanarNormal" in forward
    assert "dot(blended, geometricNormalWS) < 0.0h" in forward


def test_normal_sampling_is_distance_bounded_to_reduce_far_field_cost():
    text = read(SHADER)
    assert "_NormalFadeDistance" in text
    assert "UNITY_BRANCH" in text
    assert "cameraDistance < _NormalFadeDistance" in text
    assert "float fadeStart = _NormalFadeDistance * 0.68" in text
    assert "lerp(geometricNormalWS, mapped, normalWeight)" in text


def test_shader_keeps_pbr_lighting_shadows_depth_fog_and_instancing():
    text = read(SHADER)
    for token in (
        "UniversalFragmentPBR",
        "MixFog",
        'Name "ShadowCaster"',
        'Name "DepthOnly"',
        "ShadowCasterPass.hlsl",
        "DepthOnlyPass.hlsl",
        "#pragma multi_compile_instancing",
        "SAMPLE_GI",
        "VertexLighting",
    ):
        assert token in text


def test_shader_has_no_screen_effect_or_gameplay_or_neural_authority():
    text = read(SHADER)
    for forbidden in (
        "GrabPass",
        "_CameraOpaqueTexture",
        "_CameraDepthTexture",
        "ComputeScreenPos",
        "MotionBlur",
        "DepthOfField",
        "Neural",
        "Input.Get",
        "Rigidbody",
        "Collider",
        "TakeDamage",
    ):
        assert forbidden not in text


def test_only_opaque_production_surfaces_migrate_to_shared_triplanar_shader():
    text = read(MATERIALS)
    assert 'TriplanarShaderName = "Mindforge/ProductionTriplanarLitV09"' in text
    assert text.count("EnsureWorldLitMaterial(") == 6  # five calls + helper declaration
    assert "material.shader = shader" in text
    for prop in ("_MetersPerTile", "_BlendSharpness", "_NormalFadeDistance"):
        assert f'material.SetFloat("{prop}"' in text

    # Small metal trim and transparent surfaces deliberately stay on stock URP/Lit.
    assert "EnsureMetalMaterial(Gold" in text
    assert "EnsureTransparentMaterial(Water" in text
    assert "EnsureTransparentMaterial(Glass" in text
    assert 'Shader.Find("Universal Render Pipeline/Lit")' in text


def test_world_scale_is_tuned_per_material_not_one_magic_repeat_value():
    text = read(MATERIALS)
    for metres in ("2.45f", "1.75f", "2.80f", "1.45f", "1.05f"):
        assert metres in text
    for fade in ("82f", "74f", "68f", "58f"):
        assert fade in text


def test_cc0_urp_template_reference_is_provenanced_but_not_vendored():
    data = json.loads(read(MANIFEST))
    entries = {entry["id"]: entry for entry in data["entries"]}
    cyan = entries["cyanilux.urp_shader_code_templates"]
    assert cyan["license"] == "CC0-1.0"
    assert cyan["usage"] == "reference_only"
    assert cyan["vendored_paths"] == []
    assert "triplanar" in cyan["asset_policy"].lower()


def test_triplanar_shader_meta_guid_is_unique():
    meta = Path(str(SHADER) + ".meta")
    assert meta.exists()
    guid = next(
        line.split(":", 1)[1].strip()
        for line in meta.read_text(encoding="utf-8").splitlines()
        if line.startswith("guid: ")
    )
    assert len(guid) == 32
    matches = []
    for candidate in (ROOT / "unity/Assets").rglob("*.meta"):
        if f"guid: {guid}" in candidate.read_text(encoding="utf-8", errors="ignore"):
            matches.append(candidate)
    assert matches == [meta]
