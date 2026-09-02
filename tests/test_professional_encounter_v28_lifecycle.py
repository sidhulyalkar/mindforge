from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PRESENTATION = ROOT / "unity" / "Assets" / "Mindforge" / "Presentation"
CREATURE = PRESENTATION / "FracturedSignalCreaturePresentationV28.cs"


def test_v28_retires_historical_runtime_boss_presentations_before_render():
    source = CREATURE.read_text(encoding="utf-8")

    assert "private void LateUpdate()" in source
    assert "HideRetiredBossVisuals();" in source
    assert source.index("HideRetiredBossVisuals();", source.index("private void LateUpdate()")) < source.index("ApplySurface(Time.unscaledTime, false);")

    for token in (
        "FracturedSignalBeastV27 v27 = GetComponent<FracturedSignalBeastV27>();",
        "if (v27.enabled) v27.enabled = false;",
        "DisableChild(FracturedSignalBeastV27.RootName);",
        "Destroy(v27);",
        "FracturedSignalCharacterV19 v19 = GetComponent<FracturedSignalCharacterV19>();",
        "if (v19.enabled) v19.enabled = false;",
        "DisableChild(FracturedSignalCharacterV19.RootName);",
        "Destroy(v19);",
        'DisableChild("V11BossVisual")',
        'DisableChild("FracturedSignalShowcaseAvatar")',
        'DisableChild("FracturedSignalThreatSilhouette")',
    ):
        assert token in source


def test_v28_legacy_retirement_is_presentation_cleanup_not_gameplay_authority():
    source = CREATURE.read_text(encoding="utf-8")
    retirement = source[source.index("private void HideRetiredBossVisuals()") : source.index("private void DisableChild")]

    for forbidden in (
        "MovePosition(",
        "MoveRotation(",
        "AddForce(",
        "ReceiveDamage(",
        "ResetForCheckpoint(",
        "SetExternalPause(",
        "RequestDash(",
        "RequestJump(",
        "Time.timeScale",
    ):
        assert forbidden not in retirement
