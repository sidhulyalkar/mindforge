from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
COMBAT = ROOT / "unity" / "Assets" / "Mindforge" / "Combat"
PRESENTATION = ROOT / "unity" / "Assets" / "Mindforge" / "Presentation"


def read(name: str) -> str:
    return (COMBAT / name).read_text(encoding="utf-8")


def read_presentation(name: str) -> str:
    return (PRESENTATION / name).read_text(encoding="utf-8")


def test_guardian_input_tape_is_fixed_tick_versioned_contextual_and_fail_neutral():
    tape = read("GuardianInputTape.cs")
    for version in range(1, 6):
        assert f'SchemaV{version} = "mindforge.guardian_input_tape.v{version}"' in tape
    assert "schema = GuardianInputTape.SchemaV5" in tape
    assert "SupportedSchema(_tape.schema)" in tape
    assert "schema == SchemaV1 || schema == SchemaV2 || schema == SchemaV3 || schema == SchemaV4 || schema == SchemaV5" in tape
    assert "GuardianInputTapeMode.Live" in tape
    assert "GuardianInputTapeMode.Record" in tape
    assert "GuardianInputTapeMode.Replay" in tape
    assert '"-mindforgeInputMode"' in tape
    assert '"-mindforgeInputTape"' in tape
    assert "MindforgeSessionContext.GameSessionId" in tape
    assert "_tape.frames.Add(recorded)" in tape
    assert "GuardianCommandFrame.Neutral(live.tick)" in tape
    assert "replay exhausted; returning neutral commands" in tape
    assert "RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)" in tape
    assert "sword_attack_down" in tape and "guard_held" in tape and "guard_down" in tape
    assert "jump_down" in tape and "jump_held" in tape
    assert "mount_toggle_down" in tape
    assert "mounted_attack_down" in tape
    assert "mounted_boost_down" in tape
    assert "public bool context_down" in tape
    assert "context_down = context_down" in tape
    assert "context_down |= other.context_down" in tape
    assert "public bool IsLegacyPreContextReplay" in tape


def test_v5_tape_is_idempotent_for_multiple_consumers_on_one_absolute_tick():
    tape = read("GuardianInputTape.cs")
    assert "public static long FixedTickNow" in tape
    assert "private long _lastResolvedTick = long.MinValue" in tape
    assert "private GuardianCommandFrame _lastResolvedFrame" in tape
    assert "_lastResolvedTick == live.tick" in tape
    assert "_lastResolvedFrame.MergeFrom(live)" in tape
    assert "return _lastResolvedFrame.CopyForTick(live.tick)" in tape
    assert "public void MergeFrom(GuardianCommandFrame other)" in tape
    for token in (
        "mount_toggle_down |= other.mount_toggle_down",
        "mounted_attack_down |= other.mounted_attack_down",
        "mounted_boost_down |= other.mounted_boost_down",
        "context_down |= other.context_down",
    ):
        assert token in tape

    same_tick = tape.index("_lastResolvedTick == live.tick")
    replay_advance = tape.index("_tape.frames[_replayIndex++]")
    assert same_tick < replay_advance


def test_guardian_input_recording_does_not_write_per_tick():
    tape = read("GuardianInputTape.cs")
    resolve_start = tape.index("public GuardianCommandFrame Resolve")
    resolve_end = tape.index("public string SaveRecording")
    resolve_body = tape[resolve_start:resolve_end]
    assert "File.WriteAllText" not in resolve_body
    assert "_tape.frames.Add" in resolve_body
    assert "File.WriteAllText" in tape[tape.index("public string SaveRecording"):]


def test_guardian_combat_input_uses_canonical_profile_then_executes_on_fixed_tick():
    source = read("GuardianCombatInput.cs")
    controls = read("GuardianControlProfileV1.cs")
    camera = read_presentation("ShowcaseCameraRig.cs")
    lock = read("GuardianTargetLock.cs")
    update_start = source.index("private void Update()")
    fixed_start = source.index("private void FixedUpdate()")
    apply_start = source.index("private void Apply(")

    update_body = source[update_start:fixed_start]
    fixed_body = source[fixed_start:apply_start]
    apply_body = source[apply_start:]

    assert "GuardianControlProfileV1 controls" in source
    assert "_move = controls.SampleMovement()" in update_body
    for action in (
        "JumpHover",
        "EvadeBoost",
        "Blade",
        "Cleave",
        "Counter",
        "Bloom",
    ):
        assert f"GuardianControlAction.{action}" in update_body

    # Defaults live in one profile, not in each gameplay component.
    for token in (
        "interact = KeyCode.E",
        "targetLock = KeyCode.T",
        "jumpHover = KeyCode.Space",
        "evadeBoostPrimary = KeyCode.LeftShift",
        "blade = KeyCode.F",
        "cleave = KeyCode.Q",
        "counter = KeyCode.C",
        "bloom = KeyCode.R",
        "menu = KeyCode.Tab",
    ):
        assert token in controls

    for literal in (
        "Input.GetKeyDown(KeyCode.Space)",
        "Input.GetKeyDown(KeyCode.E)",
        "Input.GetKeyDown(KeyCode.LeftShift)",
        "Input.GetKeyDown(KeyCode.F)",
        "Input.GetKeyDown(KeyCode.Q)",
        "Input.GetKeyDown(KeyCode.C)",
        "Input.GetKeyDown(KeyCode.R)",
    ):
        assert literal not in update_body

    # Hidden legacy dodge aliases stay available without being part of the advertised profile.
    assert "Input.GetKeyDown(KeyCode.LeftControl)" in update_body
    assert "Input.GetKeyDown(KeyCode.LeftAlt)" in update_body

    for key in ("KeyCode.UpArrow", "KeyCode.DownArrow", "KeyCode.LeftArrow", "KeyCode.RightArrow"):
        assert key not in update_body
        assert key in camera
    assert "GuardianControlProfileV1 controls" in lock
    assert "GuardianControlAction.TargetLock" in lock
    assert "Input.mouseScrollDelta.y" in lock
    assert "KeyCode.LeftArrow" not in lock
    assert "KeyCode.RightArrow" not in lock

    assert "combat.FirePulse" not in update_body
    assert "combat.RiftCleave" not in update_body
    assert "motor.RequestDash" not in update_body
    assert "motor.RequestJump" not in update_body
    assert "physicalCombat?.TryLightAttack" not in update_body

    assert "GuardianCommandFrame" in fixed_body
    assert "_fixedInputTick = GuardianInputTape.FixedTickNow" in fixed_body
    assert "jump_down = _jumpLatched" in fixed_body
    assert "jump_held = _jumpHeld" in fixed_body
    assert "mount_toggle_down = false" in fixed_body
    assert "context_down = false" in fixed_body
    assert "fire_held = false" in fixed_body
    assert "guard_held = false" in fixed_body
    assert "guard_down = false" in fixed_body
    assert "_cleaveLatched = false" in fixed_body
    assert "_counterLatched = false" in fixed_body
    assert "_dashLatched = false" in fixed_body
    assert "_jumpLatched = false" in fixed_body
    assert "_bloomLatched = false" in fixed_body
    assert "_swordAttackLatched = false" in fixed_body
    assert "inputTape.Resolve" in fixed_body

    assert "_guardDownLatched" not in source
    assert "_guardHeld" not in source
    assert "_fireHeld" not in source
    assert "_mountLatched" not in source

    assert "motor.SetMoveInput(command.Move)" in apply_body
    assert "motor.SetJumpHeld(command.jump_held)" in apply_body
    assert "if (!CombatActionsEnabled)" in apply_body
    assert "physicalCombat?.SetGuardHeld(false, aim)" in apply_body
    assert "bool accepted = physicalCombat != null && physicalCombat.TryLightAttack(aim)" in apply_body
    assert "if (accepted) return;" in apply_body
    assert "physicalCombat.ActionState != GuardianActionState.Locomotion" in apply_body
    assert "if (command.counter_down && combat.BeginCounter()) return;" in apply_body
    assert "if (command.cleave_down && combat.RiftCleave(aim)) return;" in apply_body
    assert "if (command.bloom_down && bloom != null && bloom.TryActivate()) return;" in apply_body
    assert "combat.FirePulse(aim)" not in apply_body
    assert "QueueDodgeCommand(aim)" in apply_body
    assert "TryConsumeQueuedDodge()" in apply_body
    assert "motor.RequestDash(_dodgeCommandAim)" in apply_body
    assert "motor.RequestJump()" in apply_body

    resolve_index = fixed_body.index("inputTape.Resolve")
    apply_call_index = fixed_body.index("Apply(command)")
    assert resolve_index < apply_call_index
    assert "inputTape.Mode" not in apply_body[apply_body.index("private void Apply("):apply_body.index("private void ResolveDependencies()")]

    arbitration = apply_body[apply_body.index("if (command.dash_down)"):]
    order = (
        "if (command.dash_down)",
        "if (motor.IsDashing) return;",
        "if (command.jump_down",
        "if (command.sword_attack_down)",
        "physicalCombat.ActionState != GuardianActionState.Locomotion",
        "if (command.counter_down",
        "if (command.cleave_down",
        "if (command.bloom_down",
    )
    indices = [arbitration.index(token) for token in order]
    assert indices == sorted(indices)


def test_replay_never_has_a_live_input_fallback_after_exhaustion():
    tape = read("GuardianInputTape.cs")
    exhaustion = tape[tape.index("if (_tape == null || _tape.frames == null"):
                      tape.index("GuardianCommandFrame source")]
    assert "return live" not in exhaustion
    assert "GuardianCommandFrame.Neutral(live.tick)" in exhaustion
