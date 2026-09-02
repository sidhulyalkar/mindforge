from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OVERLAY = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Runtime"
TUTORIAL = OVERLAY / "MindforgeShowcaseTutorialV32.cs"
FLOW = OVERLAY / "MindforgeShowcaseFlowV32.cs"


def test_v32_tutorial_is_noninteractive_stage_driven_presentation_only():
    text = TUTORIAL.read_text(encoding="utf-8")
    for token in (
        "MindforgeShowcaseFlowV32",
        "StageChanged += HandleStageChanged",
        "MilestoneObserved += HandleMilestoneObserved",
        "CanvasGroup",
        "blocksRaycasts = false",
        "raycastTarget = false",
        'Show("AWAKEN"',
        'Show("AETHERBLADE"',
        'Show("NEURAL ORB ONLINE"',
        'Show("THE FRACTURED SIGNAL"',
        "Time.unscaledTime",
        "Time.unscaledDeltaTime",
    ):
        assert token in text

    for forbidden in (
        "Input.Get",
        "TakeDamage(",
        "ChangeState(",
        "CharacterController",
        "NavMeshAgent",
        "Time.timeScale",
        "Button",
    ):
        assert forbidden not in text


def test_v32_flow_requires_tutorial_and_still_has_no_gameplay_authority():
    text = FLOW.read_text(encoding="utf-8")
    assert "RequireComponent(typeof(MindforgeShowcaseTutorialV32))" in text
    for forbidden in (
        "TakeDamage(",
        "StartAttack(",
        "StopAttack(",
        "ChangeState(",
        "Time.timeScale",
    ):
        assert forbidden not in text
