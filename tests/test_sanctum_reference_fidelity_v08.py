from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "unity/Assets/Mindforge/Editor/SanctumReferenceFidelityV08Builder.cs"
MATERIALS = ROOT / "unity/Assets/Mindforge/Editor/SanctumReferenceMaterialAuthoringV08.cs"
CLARITY = ROOT / "unity/Assets/Mindforge/World/SanctumVisualClarityV08.cs"
MENU = ROOT / "unity/Assets/Mindforge/Editor/ShowcaseEditorMenu.cs"


def source(path: Path) -> str:
    assert path.exists(), f"missing V0.8 reference-fidelity source: {path}"
    return path.read_text(encoding="utf-8")


def test_reference_fidelity_builder_preserves_real_world_spacing_and_clear_navigation():
    text = source(BUILDER)
    assert 'RootName = "Sanctum_Reference_Fidelity_V08"' in text
    assert "HallClearHalfWidth = 5.0f" in text
    assert "TerraceClearHalfWidth = 5.25f" in text
    assert "CourtClearHalfWidth = 8.0f" in text
    assert "ProcessionalRoadWidth = 9.5f" in text
    assert "MinimumOpeningSentrySpacing = 10.0f" in text
    assert "ValidateProtectedClearance" in text
    assert "ValidateOpeningEnemySpacing" in text
    assert '"ProcessionalSpine"' in text
    assert '"VistaProcessionalRoad"' in text
    assert "VistaWalkway_" in text


def test_resonance_stations_are_side_chapels_not_center_lane_obstacles():
    text = source(BUILDER)
    assert 'MoveDeepChild(sanctum, "Resonance_Station_01_8Hz", new Vector3(-8.4f, 0f, -56.7f))' in text
    assert 'MoveDeepChild(sanctum, "Resonance_Station_02_10Hz", new Vector3(8.4f, 0f, -52.0f))' in text
    assert 'MoveDeepChild(sanctum, "Resonance_Station_03_12Hz", new Vector3(-8.4f, 0f, -47.2f))' in text
    assert "processional axis stays fully open" in text


def test_generated_reference_architecture_has_crisp_edges_pointed_ribs_and_layered_depth():
    text = source(BUILDER)
    for token in (
        "HallFloorJoint_",
        "PierShadowReveal_",
        "PierGoldReveal_",
        "PierCapital_",
        "OuterButtress_",
        "BuildWindowLancet",
        "BuildPointedRib",
        "ThresholdDeepRib_",
        "WallPilaster_",
        "Reference_World_Vista",
        "NearGardenTerrace_",
        "MidSanctumBlock_",
        "FarPhaseRing_A",
        "FarPhaseRing_B",
        "FarPhaseRing_C",
        "FarRoadForkL",
        "FarRoadForkR",
    ):
        assert token in text


def test_enemy_roster_is_visually_distinct_without_parallel_gameplay_authority():
    text = source(BUILDER)
    for token in (
        'EnemyRootName = "ReferenceSilhouetteV08"',
        "BuildChoirReliquarySentry",
        "BuildChromePenitentLancer",
        "BuildShardCantor",
        "BuildNeedleSeraph",
        "BuildCathedralWarden",
        "BuildRiftStalker",
        "ReliquaryLens",
        "PenitentLanceBlade",
        "CantorChoirRing",
        "NeedleEye",
        "WardenCore",
        "StalkerEye",
    ):
        assert token in text
    assert "AddComponent<JourneyEnemyController>" not in text
    assert "AddComponent<CombatantVitals>" not in text
    assert "AddComponent<Rigidbody>" not in text
    assert "enemy.transform.position =" not in text
    assert "enemy.transform.localPosition =" not in text


def test_reference_enemy_signals_do_not_steal_sight_or_guard_colors():
    materials = source(MATERIALS)
    builder = source(BUILDER)
    assert 'ThreatAmber = "SanctumThreatAmberV08"' in materials
    assert 'ThreatWhite = "SanctumThreatWhiteV08"' in materials
    assert 'EnemyCeramic = "SanctumEnemyCeramicV08"' in materials
    assert "SanctumReferenceMaterialAuthoringV08.ThreatAmber" in builder
    assert "SanctumReferenceMaterialAuthoringV08.ThreatWhite" in builder
    assert 'Require("AetherCyan")' not in builder
    assert 'Require("WispVerdant")' not in builder


def test_reference_fidelity_geometry_is_presentation_only():
    text = source(BUILDER)
    # Every enemy part funnels through collider=false. Architectural presentation parts
    # also explicitly request collider=false; canonical V0.8 geometry owns collision.
    assert "Primitive(name, type, parent, localPosition, localScale, material, false, localEuler);" in text
    assert "UnityEngine.Object.DestroyImmediate(shape)" in text
    assert "Existing floors, walls, gates" in text
    assert "enemy controllers, colliders, interactions and neural authority remain canonical" in text


def test_visual_clarity_policy_improves_definition_without_movement_or_neural_authority():
    text = source(CLARITY)
    assert "targetCamera.allowHDR = true" in text
    assert "targetCamera.allowMSAA = true" in text
    assert "targetCamera.useOcclusionCulling = true" in text
    assert "minimumFarClip = 420f" in text
    assert "desktopShadowDistance = 85f" in text
    assert "AnisotropicFiltering.ForceEnable" in text
    forbidden = (
        "Rigidbody",
        "GuardianMotor",
        "NeuralEvent",
        "VepAuraStimulus",
        "transform.position =",
        "transform.localPosition =",
    )
    for token in forbidden:
        assert token not in text


def test_reference_fidelity_is_part_of_one_click_showcase_pipeline():
    menu = source(MENU)
    onboarding = menu.find("SanctumOnboardingV08Builder.ApplyOpenScene();")
    hero = menu.find("SanctumHeroV08Builder.ApplyOpenScene();")
    fidelity = menu.find("SanctumReferenceFidelityV08Builder.ApplyOpenScene();")
    assert onboarding >= 0
    assert hero > onboarding
    assert fidelity > hero
