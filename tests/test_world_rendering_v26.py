from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"
EDITOR = UNITY / "Editor"
LATEST = EDITOR / "MindforgeLatestEditorMenu.cs"
GEOMETRY = EDITOR / "ProductionGeometryV26.cs"
BUILDER = EDITOR / "WorldRenderingV26Builder.cs"
SMOKE = UNITY / "Tests" / "Editor" / "WorldRenderingV26SmokeTests.cs"
DOC = ROOT / "docs" / "WORLD_RENDERING_V26.md"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.26 source: {path}"
    return path.read_text(encoding="utf-8")


def test_v26_remains_production_geometry_stage_before_v27_encounter_presentation():
    latest = read(LATEST)
    assert 'ProductVersion = "V0.27 Guardian Embodiment + Fractured Beast"' in latest
    v24 = latest.index("WorldCathedralV24Builder.ApplyOpenScene();")
    v25 = latest.index("SensoryFidelityV25Builder.ApplyOpenScene();", v24)
    v26 = latest.index("WorldRenderingV26Builder.ApplyOpenScene();", v25)
    v27 = latest.index("CombatEmbodimentV27Builder.ApplyOpenScene();", v26)
    assert v24 < v25 < v26 < v27
    assert "if (!WorldRenderingV26Builder.PresentInOpenScene())" in latest
    assert "if (!CombatEmbodimentV27Builder.PresentInOpenScene())" in latest
    assert 'RootName = "Mindforge_Production_World_Rendering_V26"' in read(BUILDER)


def test_v26_replaces_visible_cube_render_meshes_without_owning_collision():
    source = read(BUILDER)
    for token in (
        "UpgradePrimitiveCathedral",
        "ProductionGeometryV26.ChamferedBlock()",
        "role.Role == CathedralRoleV24.StructuralRole.WalkableFloor",
        "role.Role == CathedralRoleV24.StructuralRole.MysticAccent",
        "filter.sharedMesh = production",
        "V0.26 left production-visible primitive cube mesh",
        "root.GetComponentsInChildren<Collider>(true).Length != 0",
        "root.GetComponentsInChildren<Rigidbody>(true).Length != 0",
    ):
        assert token in source

    for forbidden in (
        "AddComponent<BoxCollider>",
        "AddComponent<MeshCollider>",
        "AddComponent<Rigidbody>",
        "ReceiveDamage(",
        "RequestDash(",
        "RequestJump(",
        "SetExternalPause(",
    ):
        assert forbidden not in source


def test_v26_has_project_authored_chamfer_buttress_and_vault_mesh_recipes():
    source = read(GEOMETRY)
    for token in (
        'Root = "Assets/Mindforge/Generated/V26/Meshes"',
        "MeshRevision = 1",
        "BuildChamferedBlock",
        "BuildTaperedButtress",
        "BuildVaultWeb",
        "BuildTransientChamferedBlock",
        "BuildTransientVaultWeb",
        "BuildTransientTaperedButtress",
        "RecalculateNormals()",
        "RecalculateTangents()",
        "triangles.Add(a); triangles.Add(c); triangles.Add(d);",
        "triangles.Add(a); triangles.Add(b); triangles.Add(c);",
    ):
        assert token in source
    assert "GameObject.CreatePrimitive" not in source
    assert "UnityEngine.Random" not in source


def test_v26_replaces_stacked_box_buttresses_and_adds_recessed_wall_depth():
    source = read(BUILDER)
    for token in (
        '"V26_Tapered_Buttresses"',
        '"V26_ButtressShell_',
        '"V26_ButtressFinial_',
        "ProductionGeometryV26.TaperedButtress()",
        "renderer.enabled = false",
        '"V26_Recessed_Wall_Niches"',
        '"V26_NicheArch_',
        '"V26_NicheSill_',
        "ProductionMeshLibraryV09.PointedArch()",
        "_buttressShells < 10",
        "_wallNiches < 8",
    ):
        assert token in source


def test_v26_builds_continuous_inward_vault_webs_between_existing_ribs():
    source = read(BUILDER)
    geometry = read(GEOMETRY)
    for token in (
        '"V26_Continuous_Vault_Webs"',
        '"V26_VaultWeb_',
        '"V26_LongitudinalVaultRib_',
        "float[] z = { -2f, 33f, 58f, 84f, 112f }",
        "ProductionGeometryV26.VaultWeb()",
        "pitch = -Mathf.Atan2",
        "vaultMesh.normals[vaultMesh.normals.Length / 2].y > -0.35f",
    ):
        assert token in source
    assert "Inward/downward-facing winding" in geometry


def test_v26_restores_cavern_depth_separation_and_vertical_ambient_gradient():
    source = read(BUILDER)
    for token in (
        'DeepCavernMaterialPath = MaterialRoot + "/V26_DeepCavern.mat"',
        'DistantStoneMaterialPath = MaterialRoot + "/V26_DistantStone.mat"',
        'VaultPlasterMaterialPath = MaterialRoot + "/V26_VaultPlaster.mat"',
        "ApplyWorldDepthMaterials",
        '"CavernVault"',
        '"Landmass"',
        "RenderSettings.ambientMode = AmbientMode.Trilight",
        "RenderSettings.ambientSkyColor",
        "RenderSettings.ambientEquatorColor",
        "RenderSettings.ambientGroundColor",
        "RenderSettings.fogStartDistance = 84f",
        "RenderSettings.fogEndDistance = 238f",
        "pipeline.shadowDistance = Mathf.Max(pipeline.shadowDistance, 68f)",
    ):
        assert token in source


def test_v26_is_static_visual_authoring_not_runtime_or_neural_authority():
    combined = read(BUILDER) + read(GEOMETRY)
    for forbidden in (
        "RuntimeInitializeOnLoadMethod",
        "private void Update(",
        "private void LateUpdate(",
        "private void FixedUpdate(",
        "Time.deltaTime",
        "Time.unscaledDeltaTime",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "CalibrationStimuliActive",
        "ResonanceWindowActive",
        "UnityEngine.Random",
    ):
        assert forbidden not in combined


def test_v26_native_smoke_docs_and_guids_are_present():
    smoke = read(SMOKE)
    doc = read(DOC)
    for token in (
        "V26ChamferedBlock_HasProductionEdgeGeometry",
        "V26VaultWeb_FacesIntoGameplaySpace",
        "V26TaperedButtress_IsNotABoxPrimitive",
    ):
        assert token in smoke
    for phrase in (
        "primitive",
        "vault",
        "buttress",
        "cavern",
        "collision authority",
    ):
        assert phrase in doc.lower()

    paths = (
        EDITOR / "ProductionGeometryV26.cs.meta",
        EDITOR / "WorldRenderingV26Builder.cs.meta",
        UNITY / "Tests" / "Editor" / "WorldRenderingV26SmokeTests.cs.meta",
    )
    guids = []
    for path in paths:
        text = read(path)
        assert "fileFormatVersion: 2" in text
        guid = next(line.split(":", 1)[1].strip() for line in text.splitlines() if line.startswith("guid: "))
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
