from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_world_composition_v2_gives_each_district_a_distinct_spatial_landmark():
    world = read("Editor", "GroundedWorldCompositionV2Builder.cs")

    for token in (
        'RootName = "Mindforge_GroundedWorld_Composition_V2"',
        '"District_MemoryForgeKeep"',
        '"District_CausewayRibGallery"',
        '"District_NullMarketCourt"',
        '"District_FractureTower"',
        '"District_CathedralAscent"',
        '"District_ArenaOuterRing"',
        '"ForgeKeep_Base"',
        '"CausewayGallery_Deck"',
        '"MarketCourt_Dais"',
        '"FractureTower_Core"',
        '"CathedralGrandStair"',
        '"ArenaRing_Center"',
    ):
        assert token in world

    # V2 must build on the qualified collision shell instead of replacing its safety contract.
    assert "GroundedWorldV1Builder.RootName" in world
    assert "requires Grounded World V1" in world


def test_vertical_world_has_ground_routes_landing_pockets_and_aerial_shortcuts():
    world = read("Editor", "GroundedWorldCompositionV2Builder.cs")

    for token in (
        "CreateStairRun(",
        "CreateRamp(",
        "CreateLandingPocket(",
        '"ForgeKeep_RoofPocket"',
        '"CausewayGallery_LandingA"',
        '"CausewayGallery_LandingB"',
        '"MarketCourt_AerialPocket"',
        '$"FractureTower_Pocket_{level}"',
        '"CathedralSidePocket_N"',
        '"CathedralSidePocket_S"',
        "direct double",
        "full conventional stair switchback exists",
    ):
        assert token in world

    # The Fracture Tower is the clearest authored vertical landmark and must retain
    # multiple conventional switchbacks rather than becoming a jump-only stack.
    assert "for (int level = 0; level < 4; level++)" in world
    assert 'CreateStairRun(root, $"FractureTower_Switchback_{level}"' in world


def test_reachable_composition_geometry_is_collision_backed_while_signal_trim_is_not():
    world = read("Editor", "GroundedWorldCompositionV2Builder.cs")

    for reachable in (
        'Block("ForgeKeep_Base"',
        'Block("CausewayGallery_Deck"',
        'Block("MarketCourt_Lower"',
        'Block("FractureTower_Core"',
        'Block("CathedralUpperTerrace"',
        'Block("ArenaRing_Center"',
        'Block(name + "_Deck", parent, center, size, body, true)',
        'Block(name + "_Collision", parent, center, new Vector3(width, 0.24f, length), body, true',
    ):
        assert reachable in world

    assert "if (!collider && c != null) UnityEngine.Object.DestroyImmediate(c)" in world
    assert "SignalStrip(" in world
    assert "Block(name, parent, position, scale, material, false, euler)" in world


def test_composition_v2_is_editor_authoring_only_and_cannot_become_gameplay_or_neural_authority():
    world = read("Editor", "GroundedWorldCompositionV2Builder.cs")

    assert world.startswith("#if UNITY_EDITOR")
    for forbidden in (
        "CombatantVitals",
        "GuardianMotor",
        "GuardianCombatInput",
        "GuardianSwordShieldController",
        "ReceiveDamage(",
        "TryLightAttack(",
        "RequestDash(",
        "RequestJump(",
        "TrySpend(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
        "AuraBuffController",
        "NeuralFocusResonance",
    ):
        assert forbidden not in world


def test_showcase_builds_safe_shell_then_authored_composition_before_population():
    menu = read("Editor", "ShowcaseEditorMenu.cs")

    shell = menu.index("GroundedWorldV1Builder.ApplyOpenScene();")
    composition = menu.index("GroundedWorldCompositionV2Builder.ApplyOpenScene();")
    tuning = menu.index("GroundedWorldTuningV1.ApplyOpenScene();")
    population = menu.index("NullWardArenaEcosystemBuilder.ApplyOpenScene();")
    silhouettes = menu.index("NullWardEnemySilhouetteV3Builder.ApplyOpenScene();")
    dressing = menu.index("NullWardArenaSetDressingV3Builder.ApplyOpenScene();")
    gate = menu.index("CompetitionGateValidator.ValidateAndWrite(false);")

    assert shell < composition < tuning < population < silhouettes < dressing < gate
    assert "Forge keep, Causeway rib gallery, Market court, Fracture" in menu
    assert "tower, Cathedral ascent and Arena ring" in menu
    assert "No reachable route intentionally exposes" in menu
    assert "the void" in menu
