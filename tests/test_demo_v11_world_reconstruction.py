from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EDITOR = ROOT / "unity" / "Assets" / "Mindforge" / "Editor"
PRESENTATION = ROOT / "unity" / "Assets" / "Mindforge" / "Presentation"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def test_v11_builder_uses_competition_systems_kernel_without_legacy_world_stack():
    source = read(EDITOR / "MindforgeDemoV11Builder.cs")
    assert "CompetitionSceneAssembler.BuildCompetitionScene();" in source
    assert "WorldV06Builder.ApplyOpenScene" not in source
    assert "WorldV07Builder.ApplyOpenScene" not in source
    assert "SanctumOnboardingV08Builder.ApplyOpenScene" not in source
    assert "ProductionArtAutoHookV09.ApplyNow" not in source
    assert "GroundedWorldCompositionV2Builder.ApplyOpenScene" not in source
    assert "NullWardArenaSetDressingV3Builder.ApplyOpenScene" not in source


def test_v11_route_has_five_coherent_districts_and_visible_collision_owners():
    source = read(EDITOR / "MindforgeDemoV11Builder.cs")
    for token in (
        "V11_Memory_Forge_Sanctum",
        "V11_Neon_Causeway",
        "V11_Market_of_Broken_Momentum",
        "V11_Choir_Tower_Ascent",
        "V11_Fractured_Signal_Arena",
    ):
        assert token in source
    for collider_owner in (
        'Block("SanctumFloor"',
        'Block("CausewayRoad"',
        'Block("MarketFloor"',
        'Block("AscentRamp"',
        'Block("FractureFloor"',
    ):
        assert collider_owner in source
    assert "true);" in source


def test_v11_route_is_walled_and_camera_envelope_has_real_minimum_distance():
    builder = read(EDITOR / "MindforgeDemoV11Builder.cs")
    runtime = read(PRESENTATION / "MindforgeDemoV11Runtime.cs")
    for wall in (
        "SanctumWallL",
        "SanctumWallR",
        "CausewayWallL",
        "CausewayWallR",
        "MarketWallL",
        "MarketWallR",
        "AscentWallL",
        "AscentWallR",
        "FractureWall_",
    ):
        assert wall in builder
    assert "private const float MinDistance = 3.0f;" in runtime
    assert "Physics.SphereCastNonAlloc" in runtime
    assert "Physics.CheckSphere" in runtime


def test_v11_has_one_presentation_owner_and_legacy_showcase_bails_out():
    showcase = read(PRESENTATION / "ShowcaseRuntimeInstaller.cs")
    runtime = read(PRESENTATION / "MindforgeDemoV11Runtime.cs")
    assert "FindObjectOfType<MindforgeDemoV11Marker>(true) != null" in showcase
    assert "MindforgeDemoCameraV11" in runtime
    assert "MindforgeDemoHudV11" in runtime
    assert "MindforgeDemoGuardianV11" in runtime
    assert "CinematicArmamentVfxPolish" not in runtime
    assert "ShowcasePostProcessing" not in runtime


def test_v11_encounters_cannot_snipe_across_the_world():
    source = read(PRESENTATION / "MindforgeDemoV11EncounterGate.cs")
    assert "bossReleaseZ = 82f" in source
    assert "echoWakeDistance = 18f" in source
    assert "_boss.SetExternalPause(true);" in source
    assert "_boss.SetExternalPause(false);" in source
    assert "echo.SetExternalPause" in source


def test_v11_demo_keeps_bci_controller_boundary_truthful():
    runtime = read(PRESENTATION / "MindforgeDemoV11Runtime.cs")
    marker = read(PRESENTATION / "MindforgeDemoV11Marker.cs")
    assert "ControllerOnlyQualificationBootstrap" in runtime
    assert 'qualification.EnterControllerOnly("V11_PRESENTABLE_DEMO")' in runtime
    assert "ControllerOnlyByDefault" in marker
    assert "NEURAL LINK · READY" in runtime
    assert "DEMO · BCI OFF" in runtime


def test_v11_guardian_shell_does_not_replace_gameplay_authority():
    source = read(PRESENTATION / "MindforgeDemoV11Runtime.cs")
    assert "Renderer rootRenderer = GetComponent<Renderer>();" in source
    assert "rootRenderer.enabled = false;" in source
    assert "GetComponent<Rigidbody>()" in source
    assert "Destroy(guardian" not in source
    assert "AddComponent<GuardianMotor>" not in source
    assert "AddComponent<GuardianCombatController>" not in source


def test_v11_demo_has_explicit_one_click_menu():
    source = read(EDITOR / "MindforgeDemoV11Builder.cs")
    assert 'MenuItem("Mindforge/V0.11 Demo/Build + Play Presentable Demo"' in source
    assert "BuildDemoScene(true);" in source
    assert "MindforgeDemoV11.unity" in source
