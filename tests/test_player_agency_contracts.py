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


def test_guardian_third_person_heading_and_wasd_are_replayable():
    source = (ROOT / "unity/Assets/Mindforge/Combat/GuardianCombatInput.cs").read_text(encoding="utf-8")

    assert "Third-person combat heading" in source
    assert "SampleWasdMovement" in source
    assert "SampleArrowAim" not in source
    assert "Input.GetAxisRaw" not in source
    for key in ("KeyCode.W", "KeyCode.A", "KeyCode.S", "KeyCode.D"):
        assert key in source

    assert "Mouse/trackpad or arrow keys: orbit camera" in source
    assert "T: conventional target lock" in source
    assert "Space: jump" in source
    assert "Left Ctrl / Left Alt: directional dodge/dash" in source
    assert "GuardianTargetLock targetLock" in source
    assert "targetLock.Locked" in source
    assert "targetLock.DirectionFrom(transform.position)" in source
    assert "Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up)" in source
    assert "PrecisionAimActive" in source
    assert "CurrentAimPoint" in source

    assert "aim_x = liveAim.x" in source
    assert "aim_y = liveAim.y" in source
    assert "aim_z = liveAim.z" in source
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
        "HANDS: WASD move · mouse/arrows camera · T lock · Space jump · Ctrl/Alt dodge · sword · shield · skills",
        "BCI: bounded blade/shield resonance after accepted Sight/Guard",
        "EEG never moves, jumps, locks a target, rotates the camera, swings, blocks, dodges, fires, or parries",
        "KeyCode.F10",
        "JudgeLensFlag",
        "PrecisionAimActive",
        "CurrentAimPoint",
        "TargetFocusActive",
        "TARGET LOCK",
        "T  LOCK ON",
        "T LOCK ON",
        "SPACE JUMP",
        "CTRL / ALT DODGE",
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
    assert '"responsiveness_1_to_5"' in tool
    assert '"enjoyment_1_to_5"' in tool
    assert '"intentionally_targeted_echo"' in tool
    assert '"could_explain_bci_role"' in tool
    assert "These answers stay separate from machine telemetry and do not auto-pass P2" in tool

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


def test_echo_priority_targeting_is_semantically_observable():
    echo = (ROOT / "unity/Assets/Mindforge/Combat/FracturedEchoNode.cs").read_text(encoding="utf-8")
    boss = (ROOT / "unity/Assets/Mindforge/Combat/FracturedSignalDirector.cs").read_text(encoding="utf-8")
    bridge = (ROOT / "unity/Assets/Mindforge/Telemetry/MindforgeGameMarkerBridge.cs").read_text(encoding="utf-8")

    assert "public event Action Shattered" in echo
    assert "public event Action EchoSpawned" in boss
    assert "public event Action EchoShattered" in boss
    assert '"ECHO_SPAWNED"' in bridge
    assert '"ECHO_SHATTERED"' in bridge


def test_encounter_report_flags_completed_runs_that_ignore_every_echo():
    ignored = analyze_encounter([
        _marker("ECHO_SPAWNED", 1),
        _marker("ECHO_SPAWNED", 2),
        _marker("VICTORY", 3, boss_phase=3),
    ])
    assert ignored.echo_spawns == 2
    assert ignored.echo_shatters == 0
    assert ignored.echo_shatter_rate == 0.0
    assert "ECHOES_SPAWNED_WITH_ZERO_SHATTERS" in ignored.diagnostic_flags

    engaged = analyze_encounter([
        _marker("ECHO_SPAWNED", 1),
        _marker("ECHO_SPAWNED", 2),
        _marker("ECHO_SHATTERED", 3),
        _marker("VICTORY", 4, boss_phase=3),
    ])
    assert engaged.echo_spawns == 2
    assert engaged.echo_shatters == 1
    assert engaged.echo_shatter_rate == 0.5
    assert "ECHOES_SPAWNED_WITH_ZERO_SHATTERS" not in engaged.diagnostic_flags
