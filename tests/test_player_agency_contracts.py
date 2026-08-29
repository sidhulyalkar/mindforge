from __future__ import annotations

import subprocess
import sys
from pathlib import Path

from mindforge_neuro.encounter import analyze_encounter
from mindforge_neuro.markers import GameMarker


ROOT = Path(__file__).resolve().parents[1]


def _marker(event: str, seq: int, *, boss_phase: int = 2) -> GameMarker:
    return GameMarker(
        schema="mindforge.game_marker.v1",
        seq=seq,
        session_id="p2-session",
        event=event,
        category="combat_outcome",
        unity_realtime_s=float(seq),
        game_time_s=float(seq),
        frame=seq * 10,
        fixed_tick=seq * 20,
        boss_phase=boss_phase,
    )


def test_guardian_third_person_heading_and_aerial_commands_are_replayable():
    source = (ROOT / "unity/Assets/Mindforge/Combat/GuardianCombatInput.cs").read_text(encoding="utf-8")

    assert "Third-person combat heading" in source
    assert "SampleWasdMovement" in source
    assert "SampleArrowAim" not in source
    assert "Input.GetAxisRaw" not in source
    for key in ("KeyCode.W", "KeyCode.A", "KeyCode.S", "KeyCode.D"):
        assert key in source

    assert "Mouse/trackpad or arrow keys: orbit camera" in source
    assert "T: conventional target lock" in source
    assert "Space: jump / double jump; hold while descending to hover / slow fall" in source
    assert "Left/Right Shift: directional dodge / air dash" in source
    assert "Left Ctrl / Left Alt: compatibility dodge aliases" in source
    assert "X or MMB: Pulse Shot" in source
    assert "GuardianTargetLock targetLock" in source
    assert "targetLock.Locked" in source
    assert "targetLock.DirectionFrom(transform.position)" in source
    assert "Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up)" in source
    assert "PrecisionAimActive" in source
    assert "CurrentAimPoint" in source

    assert "aim_x = liveAim.x" in source
    assert "aim_y = liveAim.y" in source
    assert "aim_z = liveAim.z" in source
    assert "dash_down = _dashLatched" in source
    assert "jump_down = _jumpLatched" in source
    assert "jump_held = _jumpHeld" in source
    assert "inputTape.Resolve(live, fixedHz)" in source

    resolve_index = source.index("GuardianCommandFrame command =")
    presentation_index = source.index("UpdateResolvedAimPresentation(command")
    assert resolve_index < presentation_index
    assert "inputTape.Mode == GuardianInputTapeMode.Replay" in source


def test_player_agency_guide_is_presentation_only_and_judge_legible():
    guide = (ROOT / "unity/Assets/Mindforge/Presentation/PlayerAgencyGuide.cs").read_text(encoding="utf-8")

    for token in (
        "HANDS: WASD move · camera · T lock · Space jump/double-jump/hover · Shift dash/air-dash · sword · shield · skills",
        "BCI: bounded blade/shield resonance after accepted Sight/Guard",
        "EEG never moves, jumps, hovers, air-dashes, locks a target, rotates the camera, swings, blocks, fires, or parries",
        "KeyCode.F10",
        "JudgeLensFlag",
        "PrecisionAimActive",
        "CurrentAimPoint",
        "TargetFocusActive",
        "TARGET LOCK",
        "T  LOCK ON",
        "SPACE JUMP ×2 / HOLD HOVER",
        "SHIFT DASH / AIR DASH",
        "X / MMB PULSE SHOT",
    ):
        assert token in guide

    for forbidden in (
        ".FirePulse(",
        ".RiftCleave(",
        ".BeginCounter(",
        ".RequestDash(",
        ".RequestJump(",
        ".TryActivate(",
        ".TryLightAttack(",
        ".SetGuardHeld(",
        ".TryApply(",
        ".Award(",
        "ReceiveDamage(",
        "TrySpend(",
    ):
        assert forbidden not in guide


def test_p2_human_review_is_opt_in_and_never_changes_promotion_result():
    tool = (ROOT / "tools/mindforge_playtest.py").read_text(encoding="utf-8")

    assert '"--prompt-review"' in tool
    assert '"mindforge.playtest_review.v1"' in tool
    assert '"clarity_1_to_5"' in tool
    assert '"control_1_to_5"' in tool
    assert '"neural_value_1_to_5"' in tool
    assert '"fun_1_to_5"' in tool
    assert '"compare_to_controller"' in tool
    assert '"notes"' in tool
    assert "result = promote(report)" in tool
    assert "return result" in tool


def test_encounter_report_counts_physical_agency_actions_without_crediting_them_to_neural_control():
    markers = [
        _marker("SWORD_LIGHT", 1),
        _marker("SWORD_HIT", 2),
        _marker("SHIELD_RAISED", 3),
        _marker("SHIELD_BLOCK", 4),
        _marker("PERFECT_GUARD", 5),
        _marker("SHIELD_LOWERED", 6),
        _marker("SIGNAL_BREAK", 7),
        _marker("VICTORY", 8),
    ]
    report = analyze_encounter(markers)
    assert report.sword_attacks == 1
    assert report.sword_hits == 1
    assert report.shield_raises == 1
    assert report.shield_blocks == 1
    assert report.perfect_guards == 1


def test_playtest_cli_help_remains_importable_after_player_agency_changes():
    process = subprocess.run(
        [sys.executable, str(ROOT / "tools" / "mindforge_playtest.py"), "--help"],
        cwd=ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    assert process.returncode == 0
    assert "--prompt-review" in process.stdout
