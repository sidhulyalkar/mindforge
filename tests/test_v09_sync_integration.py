from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
HOOK = ROOT / "unity/Assets/Mindforge/Editor/ProductionArtAutoHookV09.cs"
SHOWCASE = ROOT / "unity/Assets/Mindforge/Editor/ShowcaseEditorMenu.cs"


def read(path: Path) -> str:
    assert path.exists(), path
    return path.read_text(encoding="utf-8")


def test_canonical_showcase_applies_v09_before_validation_audit_and_play_scheduling():
    text = read(SHOWCASE)
    assert "ProductionArtAutoHookV09.ApplyNow();" in text
    v08 = text.index("SanctumCrispGeometryV08Builder.ApplyOpenScene();")
    v09 = text.index("ProductionArtAutoHookV09.ApplyNow();")
    gate = text.index("CompetitionGateValidator.ValidateAndWrite(false);")
    audit = text.index("PresentationBudgetAudit.Run();")
    assert v08 < v09 < gate < audit

    build_and_play = text.split("public static void BuildAndPlay()", 1)[1].split(
        "private static void FocusGameViewWhenPlayStarts", 1
    )[0]
    assert build_and_play.index("BuildScene();") < build_and_play.index("EditorApplication.delayCall")


def test_delayed_fallback_hook_is_a_noop_when_canonical_production_is_complete():
    text = read(HOOK)
    assert "CompletePresentationReady(production)" in text
    assert "if (production != null && CompletePresentationReady(production)) return;" in text
    ready = text.split("private static bool CompletePresentationReady", 1)[1].split(
        "private static void ApplyInternal", 1
    )[0]
    assert "ProductionWorldStorytellingV09Builder.RootName" in ready
    assert "ProductionPostFxV09Builder.RootName" in ready
    assert "GetComponent<ProductionHudV09>()" in ready
    assert "GetComponent<ProductionEchoVisualBootstrapV09>()" in ready
    assert "GetComponent<ProductionGuardianV09>()" in ready


def test_fallback_hook_remains_available_for_incomplete_manual_editor_workflows():
    text = read(HOOK)
    assert "[InitializeOnLoad]" in text
    assert "EditorSceneManager.sceneSaved" in text
    assert "ApplyInternal(false);" in text
    assert "ProductionLegacyVisualQuarantineV09.ApplyOpenScene();" in text
    assert "EnsureStorytelling(production);" in text
    assert "EnsurePostFx(production);" in text
    assert "EnsurePresentationComponents();" in text
    assert "ExternalArtReplacementV09.ApplyOpenScene();" in text
