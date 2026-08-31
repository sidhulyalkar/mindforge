from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
EDITOR = ROOT / "unity" / "Assets" / "Mindforge" / "Editor"
LATEST = EDITOR / "MindforgeLatestEditorMenu.cs"
V11 = EDITOR / "MindforgeDemoV11Builder.cs"
WORLD_SOUL = EDITOR / "WorldSoulV20Builder.cs"
READINESS = EDITOR / "MindforgeLatestReadinessAuditV17.cs"
WISP = ROOT / "unity" / "Assets" / "Mindforge" / "SoulWisp" / "WispResonanceWindow.cs"


def test_latest_menu_is_the_single_supported_play_surface():
    source = LATEST.read_text(encoding="utf-8")
    assert 'ProductVersion = "V0.20 World Soul"' in source
    assert 'Mindforge/Latest/PLAY LATEST (BCI Simulation)' in source
    assert 'Mindforge/Latest/Rebuild Latest Integrated Scene' in source
    assert 'Mindforge/Latest/Open Latest Integrated Scene' in source
    assert 'Mindforge/Latest/Validate Latest Readiness' in source
    assert 'Mindforge/Latest/Build Neural-Hardware Variant' in source
    assert "BuildCanonical(controllerOnlyByDefault: true)" in source
    assert "BuildCanonical(controllerOnlyByDefault: false)" in source
    assert "MindforgeDemoV11Builder.BuildDemoScene(controllerOnlyByDefault);" in source
    assert "WorldSoulV20Builder.ApplyOpenScene();" in source
    assert "EnsureWorldSoulOpenScene();" in source
    assert "MindforgeLatestReadinessAuditV17.AuditActiveDemo()" in source
    assert "MindforgeDemoV11Audit.AuditActiveDemo()" not in source


def test_latest_readiness_audit_tracks_current_bci_and_presentation_owners():
    source = READINESS.read_text(encoding="utf-8")
    assert 'schema = "mindforge.latest_readiness.v17"' in source
    assert "physical_ssvep_qualified = false" in source
    for token in (
        "VepAuraStimulus",
        "FrequencyHz",
        "QualifiedRefreshHz",
        "DisplayTimingMonitor",
        "StimulusPairAvailable",
        "VisualIdentityV16Installer",
        "MindforgeDirectedDemoV17",
        '"MindforgeGameplayCameraV17"',
        '"MindforgeDemoCameraV11"',
        '"MindforgeDemoHudV17"',
        '"MindforgeDemoHudV11"',
    ):
        assert token in source


def test_latest_menu_targets_clean_world_assembler_plus_one_world_soul_layer_not_showcase_chain():
    latest = LATEST.read_text(encoding="utf-8")
    builder = V11.read_text(encoding="utf-8")
    world_soul = WORLD_SOUL.read_text(encoding="utf-8")
    assert 'DemoScenePath = "Assets/Mindforge/Scenes/MindforgeDemoV11.unity"' in builder
    assert "CompetitionSceneAssembler.BuildCompetitionScene();" in builder
    assert "historical world decorators omitted" in builder
    assert 'RootName = "Mindforge_World_Soul_V20"' in world_soul
    for forbidden in (
        "ShowcaseEditorMenu",
        "ProductionArtAutoHookV09",
        "GroundedWorldCompositionV2Builder",
        "HackathonPlaythroughV1Builder",
    ):
        assert forbidden not in latest


def test_historical_build_menus_are_archived_behind_legacy_root():
    violations = []
    legacy_showcase_entries = 0
    legacy_v11_entries = 0
    for path in EDITOR.rglob("*.cs"):
        source = path.read_text(encoding="utf-8")
        if 'MenuItem("Mindforge/Showcase/' in source:
            violations.append(f"{path.relative_to(ROOT)} exposes Showcase")
        if 'MenuItem("Mindforge/V0.11 Demo/' in source:
            violations.append(f"{path.relative_to(ROOT)} exposes V0.11 Demo")
        legacy_showcase_entries += source.count('MenuItem("Mindforge/Legacy/Showcase/')
        legacy_v11_entries += source.count('MenuItem("Mindforge/Legacy/V0.11 Demo/')

    assert not violations, "\n".join(violations)
    assert legacy_showcase_entries >= 30
    assert legacy_v11_entries >= 4


def test_current_wisp_runtime_auto_installs_on_the_canonical_scene():
    source = WISP.read_text(encoding="utf-8")
    assert "RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)" in source
    assert "private static void Install()" in source
    assert "WispResonanceBootstrap" in source
    assert "WispResonanceHud" in source
    assert "UNITY_EDITOR" in source


def test_latest_build_documentation_forbids_manual_version_stack_composition():
    doc = (ROOT / "docs" / "LATEST_UNITY_BUILD.md").read_text(encoding="utf-8")
    assert "Mindforge → Latest → PLAY LATEST (BCI Simulation)" in doc
    assert "V0.20 World Soul" in doc
    assert "WorldSoulV20Builder" in doc
    assert "Validate Latest Readiness" in doc
    assert "Do not compose a new release by manually running historical `Apply ...` commands." in doc
    assert "There should never again be multiple equally plausible \"latest\" builders." in doc
