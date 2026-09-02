from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
EDITOR = ROOT / "unity" / "Assets" / "Mindforge" / "Editor"
LATEST = EDITOR / "MindforgeLatestEditorMenu.cs"
DETAIL = EDITOR / "ProfessionalWorldDetailV28Builder.cs"
META = EDITOR / "ProfessionalWorldDetailV28Builder.cs.meta"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.28 detail source: {path}"
    return path.read_text(encoding="utf-8")


def test_v28_world_detail_composes_after_professional_encounter_without_new_product_authority():
    latest = read(LATEST)
    source = read(DETAIL)
    encounter = latest.index("ProfessionalEncounterV28Builder.ApplyOpenScene();")
    detail = latest.index("ProfessionalWorldDetailV28Builder.ApplyOpenScene();", encounter)
    assert encounter < detail
    assert "if (!ProfessionalWorldDetailV28Builder.PresentInOpenScene())" in latest
    assert 'ProductVersion = "V0.28 Professional Creature + World Staging"' in latest
    assert 'RootName = "Mindforge_Professional_World_Detail_V28"' in source
    assert "ProfessionalEncounterV28Builder.PresentInOpenScene()" in source


def test_v28_detail_reuses_verified_assets_and_protects_negative_space():
    source = read(DETAIL)
    for token in (
        "PublicAssetAcquisitionV28.EnsureAll();",
        "RouteClearHalfWidth = 3.15f",
        "BossClearRadius = 14.4f",
        '"V28_Choir_Ascent_Detail"',
        '"V28_Distant_Apse_Detail"',
        '"V28_Choir_Torch_L_',
        '"V28_Choir_Banner_L_',
        '"V28_Apse_Reliquary_L"',
        '"V28_Apse_Reliquary_R"',
        "StagedProps.Count < 20 || StagedProps.Count > 28",
        "prop violates ascent clearance",
        "prop violates boss clear radius",
    ):
        assert token in source

    assert "UnityEngine.Random" not in source
    assert "System.Random" not in source


def test_v28_detail_is_render_only_and_strips_imported_physics():
    source = read(DETAIL)
    for token in (
        "GetComponentsInChildren<Collider>(true)",
        "DestroyImmediate(colliders[i])",
        "GetComponentsInChildren<Rigidbody>(true)",
        "DestroyImmediate(bodies[i])",
        "must remain collider-free",
        "must remain Rigidbody-free",
    ):
        assert token in source

    for forbidden in (
        "AddComponent<BoxCollider>",
        "AddComponent<MeshCollider>",
        "AddComponent<Rigidbody>",
        "ReceiveDamage(",
        "MovePosition(",
        "MoveRotation(",
        "AddForce(",
        "SetExternalPause(",
        "NeuralEvent",
        "UdpNeuralReceiver",
    ):
        assert forbidden not in source


def test_v28_detail_uses_canonical_ascent_elevation_and_stays_side_socketed():
    source = read(DETAIL)
    assert "if (z <= 54f) return 0f;" in source
    assert "if (z >= 86f) return 3.65f;" in source
    assert "Mathf.InverseLerp(54f, 86f, z)" in source
    assert "-7.15f" in source and "7.15f" in source
    assert "-7.42f" in source and "7.42f" in source
    assert "-6.55f" in source and "6.55f" in source


def test_v28_detail_builder_guid_is_pinned_and_unique():
    text = read(META)
    assert "fileFormatVersion: 2" in text
    guid = next(line.split(":", 1)[1].strip() for line in text.splitlines() if line.startswith("guid: "))
    assert len(guid) == 32

    matches = []
    for path in (ROOT / "unity" / "Assets").rglob("*.meta"):
        if f"guid: {guid}" in path.read_text(encoding="utf-8", errors="ignore"):
            matches.append(path)
    assert matches == [META]
