from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
ORB = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Runtime" / "MindforgeBciOrbV31.cs"


def test_bci_orb_has_local_pause_control_without_gameplay_pause_authority():
    text = ORB.read_text(encoding="utf-8")
    for token in (
        "using UnityEngine.InputSystem;",
        "Keyboard keyboard = Keyboard.current;",
        "keyboard.bKey.wasPressedThisFrame",
        "simulationEnabled = !simulationEnabled;",
        '"BCI SIM  •  REDUCED CONTRAST  •  B PAUSE"',
        '"BCI SIM PAUSED  •  B RESUME"',
    ):
        assert token in text

    for forbidden in (
        "Time.timeScale",
        "EditorApplication.isPaused",
        "MindforgeIntentBusV29.Publish(",
        "ChangeState(",
        "TakeDamage(",
    ):
        assert forbidden not in text
