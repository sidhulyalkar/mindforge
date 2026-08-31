from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "unity" / "Assets" / "Mindforge" / "Presentation" / "MindforgeDemoV11BossLifecycle.cs"


def source() -> str:
    return SOURCE.read_text(encoding="utf-8")


def test_v11_boss_lifecycle_is_marker_scoped_and_attaches_to_existing_director():
    text = source()
    assert "FindObjectOfType<MindforgeDemoV11Marker>(true)" in text
    assert "FindObjectOfType<FracturedSignalDirector>(true)" in text
    assert "GetComponent<CombatantVitals>()" in text


def test_v11_boss_lifecycle_reads_is_alive_and_only_changes_visual_roots():
    text = source()
    assert "_vitals.IsAlive" in text
    assert 'transform.Find("V11BossVisual")' in text
    assert 'transform.Find("V11BossPhaseStaging")' in text
    assert "visual.gameObject.SetActive(true)" in text
    assert "visual.localScale = Vector3.one" in text
    assert "_baseVisual.gameObject.SetActive(false)" in text
    assert "_phaseVisual.gameObject.SetActive(false)" in text


def test_v11_boss_lifecycle_never_writes_health_or_encounter_authority():
    text = source()
    forbidden = (
        "ReceiveDamage(",
        "ResetForCheckpoint(",
        "Health =",
        "SetExternalPause(",
        "Phase =",
        "TakeDamage(",
        "NeuralEvent",
    )
    for token in forbidden:
        assert token not in text
