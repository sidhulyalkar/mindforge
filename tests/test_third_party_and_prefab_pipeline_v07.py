import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_third_party_manifest_has_strict_policy_and_known_sources():
    path = ROOT / "third_party" / "manifest.json"
    data = json.loads(path.read_text(encoding="utf-8"))

    assert data["schema"] == "mindforge.third_party_manifest.v1"
    policy = data["policy"]
    assert policy["require_license_notice_for_vendored_code"] is True
    assert policy["require_asset_level_provenance_for_binary_art"] is True
    assert policy["unknown_or_ambiguous_assets_allowed"] is False

    entries = {entry["id"]: entry for entry in data["entries"]}
    assert {
        "mxgmn.wave_function_collapse",
        "chichord.unity_modular_procedural_generation",
        "keijiro.shader_graph_examples",
        "delt06.urp_toon_shader_cyberpunk_demo",
        "vkev.unity_urp_shaders_code",
    }.issubset(entries)

    wfc = entries["mxgmn.wave_function_collapse"]
    assert wfc["license"] == "MIT"
    assert wfc["usage"] == "adapted_code"
    assert "unity/Assets/Mindforge/ThirdParty/Wfc/MindforgeConstraintCollapse.cs" in wfc["vendored_paths"]
    assert "unity/Assets/Mindforge/ThirdParty/Wfc/LICENSE.txt" in wfc["vendored_paths"]

    supported_usage = {
        "adapted_code",
        "reference_only",
        "vendored_asset",
        "local_asset_source",
        "package_dependency",
        "editor_acquired_asset_source",
    }
    for entry in entries.values():
        assert entry["upstream"].startswith("https://")
        assert entry["license"].strip()
        assert entry["usage"] in supported_usage
        if entry["usage"] != "local_asset_source":
            # Code/reference/package/editor-acquired provenance remains GitHub-addressable.
            # Local-only source art is the sole category permitted to originate elsewhere.
            assert entry["upstream"].startswith("https://github.com/")
        for relative in entry["vendored_paths"]:
            assert (ROOT / relative).exists(), relative
        if entry["usage"] in {"reference_only", "local_asset_source", "editor_acquired_asset_source"}:
            assert entry["vendored_paths"] == []
        if entry["usage"] == "package_dependency":
            # A package dependency may legitimately pin its package manifest and a vendored
            # license notice while the package source itself remains external.
            assert entry["vendored_paths"]


def test_no_unmanifested_binary_art_is_hiding_under_thirdparty():
    data = json.loads((ROOT / "third_party" / "manifest.json").read_text(encoding="utf-8"))
    declared = {
        item["path"]
        for item in data.get("binary_art_assets", [])
        if isinstance(item, dict) and "path" in item
    }
    binary_extensions = {
        ".fbx", ".obj", ".blend", ".png", ".jpg", ".jpeg", ".tga", ".psd",
        ".exr", ".hdr", ".wav", ".mp3", ".ogg", ".mp4", ".mov",
    }
    found = []
    third_party = UNITY / "ThirdParty"
    for path in third_party.rglob("*"):
        if path.is_file() and path.suffix.lower() in binary_extensions:
            found.append(path.relative_to(ROOT).as_posix())
    assert set(found) == declared


def test_prefab_baker_materializes_a_small_editable_visual_kit():
    baker = read("Editor", "NeuralGothicPrefabBakerV07.cs")

    assert 'PrefabFolder = "Assets/Mindforge/Generated/WorldV07/Prefabs"' in baker
    assert "PrefabUtility.SaveAsPrefabAsset(root, path)" in baker
    assert "NeuralGothicMaterialAuthoringV07.EnsureAuthored()" in baker
    assert "UnityEngine.Object.DestroyImmediate(collider)" in baker
    assert "never rewrites an existing scene" in baker

    piece_ids = (
        "NG_FloorPlinth",
        "NG_CornerPier",
        "NG_ArchJamb",
        "NG_ArchLintel",
        "NG_WallRib",
        "NG_SignalFin",
        "NG_Terminal",
        "NG_RelicPlinth",
        "NG_BrokenShardCluster",
        "NG_Crossbeam",
        "NG_SignalSpire",
        "NG_GateCrown",
    )
    for piece in piece_ids:
        assert f'PieceOf("{piece}"' in baker
    assert baker.count("PieceOf(") == len(piece_ids) + 1  # catalog calls + helper declaration

    for forbidden in (
        "WorldStateLedger",
        "WorldSignalBus",
        "WorldInteractionSourceV1",
        "GuardianControlAction",
        "Rigidbody",
        "Input.GetKey",
    ):
        assert forbidden not in baker


def test_prefab_baker_meta_guid_is_unique():
    meta = UNITY / "Editor" / "NeuralGothicPrefabBakerV07.cs.meta"
    text = meta.read_text(encoding="utf-8")
    guid = next(line for line in text.splitlines() if line.startswith("guid: ")).split(":", 1)[1].strip()
    assert len(guid) == 32

    matches = []
    for path in (ROOT / "unity" / "Assets").rglob("*.meta"):
        if f"guid: {guid}" in path.read_text(encoding="utf-8", errors="ignore"):
            matches.append(path)
    assert matches == [meta]
