from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SCOPE = ROOT / "unity/Assets/Mindforge/Editor/SanctumEnemyPresentationScopeV08.cs"
FIDELITY = ROOT / "unity/Assets/Mindforge/Editor/SanctumReferenceFidelityV08Builder.cs"


def test_reference_enemy_pass_cannot_flatten_specialized_menagerie_roster():
    scope = SCOPE.read_text(encoding="utf-8")
    fidelity = FIDELITY.read_text(encoding="utf-8")
    assert "ArenaMenagerieDirector" in scope
    assert "GetComponentInParent<ArenaMenagerieDirector>" in scope
    assert "SanctumReferenceFidelityV08Builder.EnemyRootName" in scope
    assert "DestroyImmediate(reference.gameObject)" in scope
    assert "ten-identity presentation remains intact" in scope
    assert 'EnemyRootName = "ReferenceSilhouetteV08"' in fidelity


def test_scope_guard_is_presentation_cleanup_only():
    text = SCOPE.read_text(encoding="utf-8")
    for forbidden in (
        "CombatantVitals",
        "EnemyAttackDefinition",
        "Rigidbody",
        "TakeDamage",
        "SetArmed",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in text
