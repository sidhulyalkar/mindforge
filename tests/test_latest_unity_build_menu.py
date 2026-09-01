from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
EDITOR = ROOT / "unity" / "Assets" / "Mindforge" / "Editor"
LATEST = EDITOR / "MindforgeLatestEditorMenu.cs"
V11 = EDITOR / "MindforgeDemoV11Builder.cs"
WORLD_SOUL = EDITOR / "WorldSoulV20Builder.cs"
WORLD_COHESION = EDITOR / "WorldCohesionV21Builder.cs"
WORLD_INTEGRITY = EDITOR / "WorldIntegrityV22Builder.cs"
WORLD_FOUNDATION = EDITOR / "WorldFoundationV23Builder.cs"
WORLD_CATHEDRAL = EDITOR / "WorldCathedralV24Builder.cs"
SENSORY_FIDELITY = EDITOR / "SensoryFidelityV25Builder.cs"
READINESS = EDITOR / "MindforgeLatestReadinessAuditV17.cs"
WISP = ROOT / "unity" / "Assets" / "Mindforge" / "SoulWisp" / "WispResonanceWindow.cs"


def test_latest_menu_is_the_single_supported_play_surface():
    source = LATEST.read_text(encoding="utf-8")
    assert 'ProductVersion = "V0.25 Sensory Fidelity + Data Cathedral"' in source
    assert 'Mindforge/Latest/PLAY LATEST (BCI Simulation)' in source
    assert 'Mindforge/Latest/Rebuild Latest Integrated Scene' in source
    assert 'Mindforge/Latest/Open Latest Integrated Scene' in source
    assert 'Mindforge/Latest/Validate Latest Readiness' in source
    assert 'Mindforge/Latest/Build Neural-Hardware Variant' in source
    assert "BuildCanonical(controllerOnlyByDefault: true)" in source
    assert "BuildCanonical(controllerOnlyByDefault: false)" in source
    assert "MindforgeDemoV11Builder.BuildDemoScene(controllerOnlyByDefault);" in source
    assert "WorldSoulV20Builder.ApplyOpenScene();" in source
    assert "WorldCohesionV21Builder.ApplyOpenScene();" in source
    assert "WorldIntegrityV22Builder.ApplyOpenScene();" in source
    assert "WorldFoundationV23Builder.ApplyOpenScene();" in source
    assert "WorldCathedralV24Builder.ApplyOpenScene();" in source
    assert "SensoryFidelityV25Builder.ApplyOpenScene();" in source
    assert "EnsureWorldLayersOpenScene();" in source
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


def test_latest_menu_targets_clean_assembler_plus_ordered_world_layers():
    latest = LATEST.read_text(encoding="utf-8")
    builder = V11.read_text(encoding="utf-8")
    world_soul = WORLD_SOUL.read_text(encoding="utf-8")
    cohesion = WORLD_COHESION.read_text(encoding="utf-8")
    integrity = WORLD_INTEGRITY.read_text(encoding="utf-8")
    foundation = WORLD_FOUNDATION.read_text(encoding="utf-8")
    cathedral = WORLD_CATHEDRAL.read_text(encoding="utf-8")
    sensory = SENSORY_FIDELITY.read_text(encoding="utf-8")
    assert 'DemoScenePath = "Assets/Mindforge/Scenes/MindforgeDemoV11.unity"' in builder
    assert "CompetitionSceneAssembler.BuildCompetitionScene();" in builder
    assert "historical world decorators omitted" in builder
    assert 'RootName = "Mindforge_World_Soul_V20"' in world_soul
    assert 'RootName = "Mindforge_World_Cohesion_V21"' in cohesion
    assert 'RootName = "Mindforge_World_Integrity_V22"' in integrity
    assert 'RootName = "Mindforge_World_Foundation_V23"' in foundation
    assert 'RootName = "Mindforge_White_Cathedral_V24"' in cathedral
    assert 'RootName = "Mindforge_Sensory_Fidelity_V25"' in sensory
    v20 = latest.index("WorldSoulV20Builder.ApplyOpenScene();")
    v21 = latest.index("WorldCohesionV21Builder.ApplyOpenScene();", v20)
    v22 = latest.index("WorldIntegrityV22Builder.ApplyOpenScene();", v21)
    v23 = latest.index("WorldFoundationV23Builder.ApplyOpenScene();", v22)
    v24 = latest.index("WorldCathedralV24Builder.ApplyOpenScene();", v23)
    v25 = latest.index("SensoryFidelityV25Builder.ApplyOpenScene();", v24)
    assert v20 < v21 < v22 < v23 < v24 < v25
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
    assert "V0.25 Sensory Fidelity + Data Cathedral" in doc
    assert "WorldSoulV20Builder" in doc
    assert "WorldCohesionV21Builder" in doc
    assert "WorldIntegrityV22Builder" in doc
    assert "WorldFoundationV23Builder" in doc
    assert "WorldCathedralV24Builder" in doc
    assert "SensoryFidelityV25Builder" in doc
    assert "Validate Latest Readiness" in doc
    assert "Do not compose a new release by manually running historical `Apply ...` commands." in doc
    assert "There should never again be multiple equally plausible \"latest\" builders." in doc