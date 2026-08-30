from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_generated_cells_expose_topology_metadata_without_moving_authority():
    assembler = read("World", "ModularWorldAssemblerV06.cs")
    art = read("World", "GeneratedWorldArtV07.cs")

    for token in (
        "public sealed class GeneratedWorldCellV07 : MonoBehaviour",
        "public Vector2Int Grid => grid",
        "public string North =>",
        "public string East =>",
        "public string South =>",
        "public string West =>",
        "public int HeightSteps => heightSteps",
        "public bool IsOpen(int direction)",
    ):
        assert token in art

    for token in (
        "GeneratedWorldCellV07 metadata = instance.GetComponent<GeneratedWorldCellV07>()",
        "metadata = instance.AddComponent<GeneratedWorldCellV07>()",
        "metadata.Configure(",
        "tile.north",
        "tile.east",
        "tile.south",
        "tile.west",
        "tile.heightSteps",
        "cellSize",
    ):
        assert token in assembler

    # V0.6 remains the only source of solved topology and physical generated walls/floors.
    assert "MindforgeConstraintCollapse" in assembler
    assert "BuildFallbackTile(instance.transform, tile)" in assembler
    assert "BuildVerticalConnectors" in assembler
    assert "Collider" not in art.split("public sealed class GeneratedWorldCellV07", 1)[1].split("public sealed class NeuralGothicWorldDetailerV07", 1)[0]


def test_local_detailer_is_deterministic_bounded_and_presentation_only():
    art = read("World", "GeneratedWorldArtV07.cs")

    for token in (
        "public sealed class NeuralGothicWorldDetailerV07 : MonoBehaviour",
        "private int detailSeed = 70731",
        "public const string DetailRootName = \"NeuralGothicDetail_V07\"",
        "StableHash(cell.TileId + \"\:\" + cell.Grid.x + \"\:\" + cell.Grid.y + \"\:\" + detailSeed)",
        "System.Random random = new System.Random(hash)",
        "maxDecorativePrimitivesPerCell = 34",
        "if (_createdThisCell >= cap) return null",
        "BuildCorners",
        "BuildOpenSide",
        "BuildClosedSide",
        "BuildVerticalSilhouette",
        "BuildProps",
        "BuildSignals",
    ):
        assert token in art

    # No new gameplay/control/semantic authority is allowed in the visual pass.
    for forbidden in (
        "Input.GetKey",
        "GuardianControlAction",
        "WorldInteractionSourceV1",
        "WorldStateLedger",
        "PlayerProfileSave",
        "CombatantVitals",
        "GuardianMotor",
        "WorldSignalBus",
        "Rigidbody",
    ):
        assert forbidden not in art

    # Primitive colliders are destroyed immediately; solved V0.6 cell collision stays authoritative.
    assert "Collider collider = go.GetComponent<Collider>()" in art
    assert "DestroyImmediate(collider)" in art
    assert "go.isStatic = true" in art


def test_v07_materials_reuse_existing_pbr_maps_instead_of_adding_shader_zoo():
    materials = read("Editor", "NeuralGothicMaterialAuthoringV07.cs")

    for token in (
        "CinematicMaterialAuthoring.EnsureAuthored()",
        'public const string Stone = "CloisterStoneV07"',
        'public const string DarkStone = "CloisterDarkStoneV07"',
        'public const string Metal = "CloisterMetalV07"',
        'public const string Patina = "CloisterPatinaV07"',
        'public const string AshStone = "CloisterAshStoneV07"',
        "new Material(source)",
        'CopyTextureProperty(source, existing, "_BaseMap")',
        'CopyTextureProperty(source, existing, "_BumpMap")',
        'CopyTextureProperty(source, existing, "_MetallicGlossMap")',
        'CopyTextureProperty(source, existing, "_OcclusionMap")',
    ):
        assert token in materials

    assert "Shader.Find" not in materials
    assert "Shader Graph" not in materials
    assert "Packages/manifest.json" not in materials


def test_v07_builds_three_scale_visual_hierarchy_without_colliders():
    builder = read("Editor", "WorldV07Builder.cs")

    for token in (
        'RootName = "Mindforge_NeuralGothic_World_V07"',
        '"Cloister_Threshold_V07"',
        '"Cloister_Archive_Spire_V07"',
        '"Cloister_Resonance_Well_V07"',
        '"Memory_Forge_Loom_V07"',
        '"Null_Market_Reliquary_V07"',
        '"Cathedral_Relay_V07"',
        '"Distant_Silhouette_Anchors_V07"',
        "CreateRing(",
        "CreateCable(",
        "BuildLightRhythm(",
        "GameObjectUtility.SetStaticEditorFlags(go, VisualStatic)",
    ):
        assert token in builder

    # All authored V0.7 primitives deliberately pass collider=false.
    assert "All V0.7 geometry is collider-free presentation" in builder
    assert "Primitive(" in builder
    assert ", true" not in "\n".join(
        line for line in builder.splitlines() if "Primitive(" in line and "private static GameObject Primitive" not in line
    )


def test_v07_light_rhythm_is_small_and_shadow_free():
    builder = read("Editor", "WorldV07Builder.cs")
    audit = read("World", "NeuralGothicWorldArtAuditV07.cs")

    assert builder.count("AddPointLight(") == 7  # six calls plus helper declaration
    assert "light.shadows = LightShadows.None" in builder
    assert "light.renderMode = LightRenderMode.Auto" in builder

    for token in (
        "rendererBudget = 760",
        "lightBudget = 10",
        "lineBudget = 48",
        "counts.renderers <=",
        "counts.lights <=",
        "counts.lines <=",
        "it never changes".lower(),
    ):
        if token == "it never changes":
            assert "it never changes" in audit.lower()
        else:
            assert token in audit

    for forbidden in (
        "QualitySettings",
        "Time.timeScale",
        "Time.fixedDeltaTime",
        "ScalableBufferManager",
        "VepAuraStimulus",
    ):
        assert forbidden not in audit


def test_runtime_art_audit_is_not_in_editor_folder():
    runtime = UNITY / "World" / "NeuralGothicWorldArtAuditV07.cs"
    editor = UNITY / "Editor" / "WorldV07Builder.cs"
    assert runtime.exists()
    assert editor.exists()
    assert "public sealed class NeuralGothicWorldArtAuditV07" in runtime.read_text(encoding="utf-8")
    assert "public sealed class NeuralGothicWorldArtAuditV07" not in editor.read_text(encoding="utf-8")


def test_showcase_orders_visual_generation_after_persistence_and_before_validation():
    showcase = read("Editor", "ShowcaseEditorMenu.cs")

    v05 = showcase.index("UxInteractionSaveV05Builder.ApplyOpenScene();")
    v06 = showcase.index("WorldV06Builder.ApplyOpenScene();")
    v07 = showcase.index("WorldV07Builder.ApplyOpenScene();")
    validation = showcase.index("CompetitionGateValidator.ValidateAndWrite(false);")
    assert v05 < v06 < v07 < validation

    assert "V0.7 is presentation-only" in showcase
    assert "topology, E routing, persistence, combat and BCI remain untouched" in showcase


def test_v05_input_language_is_unchanged_by_v07():
    showcase = read("Editor", "ShowcaseEditorMenu.cs")
    menu = read("Presentation", "GuardianEquipmentMenu.cs")
    art = read("World", "GeneratedWorldArtV07.cs")

    assert "T locks and mouse wheel cycles targets" in showcase
    assert "E is the single contextual world action" in showcase
    assert "Tab opens kit + controls + objective" in showcase
    assert '"MOUSE / ARROWS", "Orbit camera"' in menu
    assert '"Lock / unlock enemy · wheel cycles target"' in menu
    assert "Input.GetKey" not in art


def test_v07_csharp_meta_guids_exist_and_are_repository_unique():
    metas = (
        UNITY / "World" / "GeneratedWorldArtV07.cs.meta",
        UNITY / "World" / "NeuralGothicWorldArtAuditV07.cs.meta",
        UNITY / "Editor" / "NeuralGothicMaterialAuthoringV07.cs.meta",
        UNITY / "Editor" / "WorldV07Builder.cs.meta",
    )
    expected = []
    for path in metas:
        text = path.read_text(encoding="utf-8")
        assert "fileFormatVersion: 2" in text
        guid = next(line for line in text.splitlines() if line.startswith("guid: ")).split(":", 1)[1].strip()
        assert len(guid) == 32
        expected.append(guid)
    assert len(expected) == len(set(expected))

    all_guids = {}
    for path in (ROOT / "unity" / "Assets").rglob("*.meta"):
        text = path.read_text(encoding="utf-8", errors="ignore")
        for line in text.splitlines():
            if not line.startswith("guid: "):
                continue
            guid = line.split(":", 1)[1].strip()
            all_guids.setdefault(guid, []).append(path)
            break
    for guid in expected:
        assert len(all_guids.get(guid, [])) == 1, (guid, all_guids.get(guid, []))
