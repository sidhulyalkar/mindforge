from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "unity" / "Assets" / "Mindforge" / "Presentation" / "MindforgeDemoV11EchoLifecycle.cs"


def source() -> str:
    return SOURCE.read_text(encoding="utf-8")


def test_v11_echo_lifecycle_is_marker_scoped_and_route_echo_only():
    text = source()
    assert "FindObjectOfType<MindforgeDemoV11Marker>(true)" in text
    assert 'echo.name.StartsWith("V11Echo_"' in text
    assert "configured >= 3" in text


def test_v11_echo_lifecycle_mirrors_existing_authoritative_events():
    text = source()
    assert "_echo.Shattered += OnShattered" in text
    assert "_echo.Reconstructed += OnReconstructed" in text
    assert "_echo.Shattered -= OnShattered" in text
    assert "_echo.Reconstructed -= OnReconstructed" in text
    assert "_visual.gameObject.SetActive(false)" in text
    assert "_visual.gameObject.SetActive(true)" in text
    assert "_echo.Vitals.IsAlive" in text


def test_v11_echo_lifecycle_does_not_create_or_reset_gameplay_truth():
    text = source()
    forbidden = (
        "TakeDamage(",
        "ApplyDamage(",
        "ResetForCheckpoint(",
        "SetExternalPause(",
        "Award(",
        "ConfigureWorldEcho(",
        "Instantiate(",
        "Destroy(_echo",
        "NeuralEvent",
    )
    for token in forbidden:
        assert token not in text
