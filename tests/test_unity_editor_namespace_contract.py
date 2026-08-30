from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EDITOR = ROOT / "unity" / "Assets" / "Mindforge" / "Editor"


def test_presentation_budget_audit_has_editor_namespace_facade():
    source = (EDITOR / "PresentationBudgetAuditCompat.cs").read_text(encoding="utf-8")
    assert "namespace Mindforge.Editor" in source
    assert "public static class PresentationBudgetAudit" in source
    assert "Mindforge.EditorTools.PresentationBudgetAudit.Run();" in source


def test_foundry_audit_call_resolves_through_stable_editor_namespace():
    foundry = (EDITOR / "ContentFoundryV10.cs").read_text(encoding="utf-8")
    assert "namespace Mindforge.Editor" in foundry
    assert "PresentationBudgetAudit.Run();" in foundry


def test_authoritative_audit_implementation_remains_single_source_of_truth():
    authoritative = (EDITOR / "PresentationBudgetAudit.cs").read_text(encoding="utf-8")
    facade = (EDITOR / "PresentationBudgetAuditCompat.cs").read_text(encoding="utf-8")
    assert "namespace Mindforge.EditorTools" in authoritative
    assert "public static void Run()" in authoritative
    assert "BuildReport()" in authoritative
    assert "BuildReport()" not in facade
