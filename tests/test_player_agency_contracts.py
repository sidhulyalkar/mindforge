from __future__ import annotations

import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


def test_guardian_precision_aim_is_player_owned_and_replayable():
    source = (ROOT / "unity/Assets/Mindforge/Combat/GuardianCombatInput.cs").read_text(encoding="utf-8")

    assert "Player-owned aim" in source
    assert "ScreenPointToRay" in source
    assert "new Plane(Vector3.up, transform.position)" in source
    assert "KeyCode.RightArrow" in source
    assert "KeyCode.LeftArrow" in source
    assert "KeyCode.UpArrow" in source
    assert "KeyCode.DownArrow" in source
    assert "PrecisionAimActive" in source
    assert "CurrentAimPoint" in source

    # The boss/lock target must be a fallback after explicit player aim paths.
    assert source.index("if (_keyboardAim.sqrMagnitude > 0.01f)") < source.index("if (aimTarget != null)")
    assert source.index("if (mouseAimEnabled && _mouseAimActive") < source.index("if (aimTarget != null)")

    # Resolved conventional aim is part of the fixed command tape contract.
    assert "aim_x = aim.x" in source
    assert "aim_y = aim.y" in source
    assert "aim_z = aim.z" in source
    assert "inputTape.Resolve(live, fixedHz)" in source


def test_player_agency_guide_is_presentation_only_and_judge_legible():
    guide = (ROOT / "unity/Assets/Mindforge/Presentation/PlayerAgencyGuide.cs").read_text(encoding="utf-8")

    for token in (
        "HANDS: move, aim, fire, cleave, counter, dash",
        "BCI: Sight offense / Guard recovery only",
        "EEG never moves, aims, fires, dodges, or parries",
        "KeyCode.F10",
        "JudgeLensFlag",
        "PrecisionAimActive",
        "CurrentAimPoint",
    ):
        assert token in guide

    # The explainer may observe resolved state, but must never mutate combat authority.
    for forbidden in (
        ".FirePulse(",
        ".RiftCleave(",
        ".BeginCounter(",
        ".RequestDash(",
        ".TryActivate(",
        ".TryApply(",
        ".Award(",
        "ReceiveDamage(",
    ):
        assert forbidden not in guide


def test_p2_human_review_is_opt_in_and_never_changes_promotion_result():
    tool = (ROOT / "tools/mindforge_playtest.py").read_text(encoding="utf-8")

    assert '"--prompt-review"' in tool
    assert '"mindforge.playtest_review.v1"' in tool
    assert '"clarity_1_to_5"' in tool
    assert '"responsiveness_1_to_5"' in tool
    assert '"enjoyment_1_to_5"' in tool
    assert '"intentionally_targeted_echo"' in tool
    assert '"could_explain_bci_role"' in tool
    assert "These answers stay separate from machine telemetry and do not auto-pass P2" in tool

    # Human review is written before the existing machine-evidence return gates and
    # does not participate in controller_only_declared/terminal pass conditions.
    review_call = tool.index("_write_human_review(output_dir, capture_report)")
    controller_gate = tool.index("if not capture_report.controller_only_declared")
    terminal_gate = tool.index("if args.require_terminal and not capture_report.terminal_observed")
    assert review_call < controller_gate < terminal_gate


def test_playtest_cli_exposes_review_without_requiring_interaction():
    tool = ROOT / "tools/mindforge_playtest.py"
    result = subprocess.run(
        [sys.executable, str(tool), "--help"],
        cwd=ROOT,
        capture_output=True,
        text=True,
        timeout=10,
        check=False,
    )
    assert result.returncode == 0, result.stderr
    assert "--prompt-review" in result.stdout
