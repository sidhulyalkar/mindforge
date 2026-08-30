import json
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
RECIPES = ROOT / "content" / "recipes"
BINDINGS = ROOT / "content" / "local_asset_bindings.v1.json"
SCHEMA = ROOT / "contracts" / "content_asset_recipe.v1.schema.json"
TOOL = ROOT / "tools" / "content_foundry.py"
BLENDER = ROOT / "tools/blender/normalize_static_asset_v10.py"
UNITY = ROOT / "unity/Assets/Mindforge/Editor/ContentFoundryV10.cs"
LEGACY_REPLACER = ROOT / "unity/Assets/Mindforge/Editor/ExternalArtReplacementV09.cs"
CAPTURE = ROOT / "unity/Assets/Mindforge/Editor/ContentFoundryVisualCaptureV10.cs"
MESH_LIBRARY = ROOT / "unity/Assets/Mindforge/Editor/ProductionMeshLibraryV09.cs"
WORKFLOW = ROOT / ".github/workflows/test-neuro.yml"


def recipes():
    return [json.loads(path.read_text(encoding="utf-8")) for path in sorted(RECIPES.rglob("*.json"))]


def test_recipe_contract_is_typed_and_content_authority_is_always_false():
    schema = json.loads(SCHEMA.read_text(encoding="utf-8"))
    assert schema["properties"]["schema"]["const"] == "mindforge.content_asset_recipe.v1"
    assert schema["properties"]["authority"]["properties"]["gameplay"]["const"] is False
    assert schema["properties"]["authority"]["properties"]["collision"]["const"] is False
    assert schema["properties"]["authority"]["properties"]["bci"]["const"] is False

    values = recipes()
    assert len(values) >= 3
    assert {value["semantic_role"] for value in values} >= {"arch", "column", "tree"}
    assert len({value["asset_id"] for value in values}) == len(values)
    for value in values:
        assert value["authority"] == {"gameplay": False, "collision": False, "bci": False}
        assert value["quality"]["require_finite_normals"] is True
        assert value["quality"]["require_nonzero_bounds"] is True
        assert value["quality"]["reject_magenta_material"] is True
        assert value["unity"]["target_tokens"]
        assert value["unity"]["fallback_symbol"]


def test_generated_fallback_symbols_exist_in_the_v09_mesh_library():
    text = MESH_LIBRARY.read_text(encoding="utf-8")
    for value in recipes():
        if value["source"]["kind"] != "generated_fallback":
            continue
        symbol = value["unity"]["fallback_symbol"]
        prefix = "ProductionMeshLibraryV09."
        assert symbol.startswith(prefix)
        method = symbol[len(prefix):]
        assert f" {method}()" in text, f"missing generated fallback method for {value['asset_id']}: {symbol}"


def test_local_bindings_are_explicit_and_start_empty_in_public_source():
    value = json.loads(BINDINGS.read_text(encoding="utf-8"))
    assert value == {"schema": "mindforge.local_asset_bindings.v1", "bindings": []}


def test_python_foundry_validate_fingerprint_and_plan_are_deterministic(tmp_path):
    validate = subprocess.run(
        [sys.executable, str(TOOL), "validate"],
        cwd=ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    assert validate.returncode == 0, validate.stderr
    assert "Content Foundry PASS" in validate.stdout

    one = subprocess.run([sys.executable, str(TOOL), "fingerprint"], cwd=ROOT, capture_output=True, text=True, check=True).stdout.strip()
    two = subprocess.run([sys.executable, str(TOOL), "fingerprint"], cwd=ROOT, capture_output=True, text=True, check=True).stdout.strip()
    assert one == two
    assert len(one) == 64

    output = tmp_path / "plan.json"
    planned = subprocess.run(
        [sys.executable, str(TOOL), "plan", "--output", str(output)],
        cwd=ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    assert planned.returncode == 0, planned.stderr
    value = json.loads(output.read_text(encoding="utf-8"))
    assert value["schema"] == "mindforge.content_foundry_plan.v1"
    assert value["authority"] == {"gameplay": False, "collision": False, "bci": False}
    assert [stage["id"] for stage in value["stages"]] == ["validate", "normalize", "unity_ingest", "visual_capture"]
    assert value["stages"][2]["status"] == "requires_unity_editor"
    assert value["stages"][3]["observed_runtime_evidence"] is False


def test_unity_compiler_uses_explicit_bindings_and_strips_external_authority():
    text = UNITY.read_text(encoding="utf-8")
    assert "AssetDatabase.LoadAssetAtPath<GameObject>(binding.unity_asset_path)" in text
    assert "Assets/Mindforge/LocalArt/" in text
    assert "ExternalArtDropV09.FindCandidates" not in text
    assert "colliders[i].enabled = false" in text
    assert "DestroyImmediate(bodies[i])" in text
    assert "lights[i].enabled = false" in text
    assert "cameras[i].enabled = false" in text
    assert "listeners[i].enabled = false" in text
    assert "ProductionArtAutoHookV09.ApplyNow();" in text
    assert "full-rebuild qualification" in text
    assert "ValidateExpectedHash(binding);" in text
    assert "ShaderUtil.ShaderHasError" in text
    assert "Triangle budget exceeded" in text
    assert "Material budget exceeded" in text


def test_foundry_suppresses_v09_filename_heuristic_only_while_compiling():
    foundry = UNITY.read_text(encoding="utf-8")
    legacy = LEGACY_REPLACER.read_text(encoding="utf-8")
    assert "public static bool SuppressAutomaticReplacement" in legacy
    assert "if (SuppressAutomaticReplacement) return 0;" in legacy
    assert "ExternalArtReplacementV09.SuppressAutomaticReplacement = true;" in foundry
    assert "ExternalArtReplacementV09.SuppressAutomaticReplacement = legacySuppression;" in foundry
    assert "finally" in foundry


def test_blender_normalizer_is_static_only_and_enforces_recipe_budgets():
    text = BLENDER.read_text(encoding="utf-8")
    assert "static normalizer cannot process humanoid/robot rig recipes" in text
    assert '"arch", "column", "door", "spire", "tree", "rock", "prop"' in text
    assert "strip_non_mesh_objects()" in text
    assert "bmesh.ops.triangulate" in text
    assert 'geometry["max_triangles"]' in text
    assert 'render["max_materials"]' in text
    assert "unity_size_to_blender" in text
    assert "center_y" in text
    assert "low.z" in text
    assert "axis_forward=\"-Z\"" in text
    assert "axis_up=\"Y\"" in text
    assert "use_triangles=True" in text
    assert '"gameplay": False' in text
    assert '"collision": False' in text
    assert '"bci": False' in text


def test_incremental_cache_is_local_and_full_showcase_gate_is_not_replaced():
    text = UNITY.read_text(encoding="utf-8")
    assert '"Library", "MindforgeContentFoundry"' in text
    assert "canonical ShowcaseEditorMenu full rebuild" in text
    showcase = (ROOT / "unity/Assets/Mindforge/Editor/ShowcaseEditorMenu.cs").read_text(encoding="utf-8")
    assert "CompetitionSceneAssembler.BuildCompetitionScene();" in showcase
    assert "CompetitionGateValidator.ValidateAndWrite(false);" in showcase
    assert "ProductionArtAutoHookV09.ApplyNow();" in showcase


def test_visual_capture_has_named_stable_review_views_and_is_non_authoritative():
    text = CAPTURE.read_text(encoding="utf-8")
    for target in (
        "Production_Sanctum_Nave",
        "Production_Threshold_Facade",
        "Production_Market_Arcade",
        "Production_Fracture_Landmark",
        "Production_Cathedral_Approach",
        "Production_Skyline",
    ):
        assert f'"{target}"' in text
    assert "camera.Render();" in text
    assert "EncodeToPNG" in text
    assert "canonical_promotion_evidence" in text
    for forbidden in ("CombatantVitals", "GuardianMotor", "NeuralEvent", "UdpNeuralReceiver"):
        assert forbidden not in text


def test_ci_executes_foundry_contract_validation():
    text = WORKFLOW.read_text(encoding="utf-8")
    assert "python -m py_compile tools/content_foundry.py" in text
    assert "python -m py_compile tools/blender/normalize_static_asset_v10.py" in text
    assert "python tools/content_foundry.py validate" in text
    assert "python tools/content_foundry.py plan" in text
