from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_cinematic_showcase_emits_static_budget_before_controller_only_play():
    menu = read("Editor", "ShowcaseEditorMenu.cs")

    build = menu.index("NullWardVisualInfrastructureBuilder.ApplyOpenScene();")
    gate = menu.index("CompetitionGateValidator.ValidateAndWrite(false);")
    budget = menu.index("PresentationBudgetAudit.Run();")
    play = menu.index("EditorApplication.isPlaying = true;")

    assert build < gate < budget
    assert "EditorPrefs.SetBool(ShowcasePreviewBootstrap.EditorPreferenceKey, true)" in menu
    assert play < build  # source order: BuildAndPlay is declared before BuildScene

    # Evidence collection is presentation/qualification plumbing, not game authority.
    for forbidden in (
        "TryApply(",
        "ReceiveDamage(",
        "TryLightAttack(",
        "RequestDash(",
        "CalibrationReady =",
    ):
        assert forbidden not in menu


def test_static_and_runtime_reports_have_distinct_schemas_and_paths():
    audit = read("Editor", "PresentationBudgetAudit.cs")
    runtime = read("Qualification", "PresentationPerformanceProbe.cs")

    assert '"mindforge.presentation_budget.v1"' in audit
    assert '"presentation-budget-latest.json"' in audit
    assert '"mindforge.presentation_runtime.v1"' in runtime
    assert '"presentation-runtime-latest.json"' in runtime
