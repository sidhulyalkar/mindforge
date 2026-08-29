from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
COMBAT = ROOT / "unity" / "Assets" / "Mindforge" / "Combat"
PRESENTATION = ROOT / "unity" / "Assets" / "Mindforge" / "Presentation"


def read(name: str) -> str:
    return (COMBAT / name).read_text(encoding="utf-8")


def read_presentation(name: str) -> str:
    return (PRESENTATION / name).read_text(encoding="utf-8")


def test_guardian_input_tape_is_fixed_tick_versioned_and_fail_neutral():
    tape = read("GuardianInputTape.cs")
    assert 'SchemaV1 = "mindforge.guardian_input_tape.v1"' in tape
    assert 'SchemaV2 = "mindforge.guardian_input_tape.v2"' in tape
    assert 'SchemaV3 = "mindforge.guardian_input_tape.v3"' in tape
    assert "schema = GuardianInputTape.SchemaV3" in tape
    assert "_tape.schema != SchemaV1 && _tape.schema != SchemaV2 && _tape.schema != SchemaV3" in tape
    assert "GuardianInputTapeMode.Live" in tape
    assert "GuardianInputTapeMode.Record" in tape
    assert "GuardianInputTapeMode.Replay" in tape
    assert '"-mindforgeInputMode"' in tape
    assert '"-mindforgeInputTape"' in tape
    assert "MindforgeSessionContext.GameSessionId" in tape
    assert "_tape.frames.Add(live.CopyForTick(live.tick))" in tape
    assert "GuardianCommandFrame.Neutral(live.tick)" in tape
    assert "replay exhausted; returning neutral commands" in tape
    assert "RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)" in tape
    assert "sword_attack_down" in tape and "guard_held" in tape and "guard_down" in tape
    assert "jump_down" in tape and "jump_held" in tape


def test_guardian_input_recording_does_not_write_per_tick():
    tape = read("GuardianInputTape.cs")
    resolve_start = tape.index("public GuardianCommandFrame Resolve")
    resolve_end = tape.index("public string SaveRecording")
    resolve_body = tape[resolve_start:resolve_end]
    assert "File.WriteAllText" not in resolve_body
    assert "_tape.frames.Add" in resolve_body
    assert "File.WriteAllText" in tape[tape.index("public string SaveRecording"):]


def test_guardian_combat_input_samples_actions_in_update_and_executes_on_fixed_tick():
    source = read("GuardianCombatInput.cs")
    camera = read_presentation("ShowcaseCameraRig.cs")
    lock = read("GuardianTargetLock.cs")
    update_start = source.index("private void Update()")
    fixed_start = source.index("private void FixedUpdate()")
    apply_start = source.index("private void Apply(")

    update_body = source[update_start:fixed_start]
    fixed_body = source[fixed_start:apply_start]
    apply_body = source[apply_start:]

    assert "Input.GetAxisRaw" not in update_body
    for key in ("KeyCode.W", "KeyCode.A", "KeyCode.S", "KeyCode.D"):
        assert key in update_body
    for key in ("KeyCode.UpArrow", "KeyCode.DownArrow", "KeyCode.LeftArrow", "KeyCode.RightArrow"):
        assert key not in update_body
        assert key in camera
    assert "toggleKey = KeyCode.T" in lock
    assert "Input.GetKeyDown(toggleKey)" in lock
    assert "Input.GetKeyDown(KeyCode.T)" not in update_body
    assert "Input.GetKeyDown(KeyCode.Space)" in update_body
    assert "Input.GetKey(KeyCode.Space)" in update_body
    assert "Input.GetKeyDown(KeyCode.LeftShift)" in update_body
    assert "Input.GetMouseButtonDown(1)" in update_body
    assert "Input.GetKeyDown(KeyCode.LeftControl)" in update_body
    assert "Input.GetKeyDown(KeyCode.LeftAlt)" in update_body
    assert "combat.FirePulse" not in update_body
    assert "combat.RiftCleave" not in update_body
    assert "motor.RequestDash" not in update_body
    assert "motor.RequestJump" not in update_body
    assert "physicalCombat?.TryLightAttack" not in update_body

    assert "GuardianCommandFrame" in fixed_body
    assert "jump_down = _jumpLatched" in fixed_body
    assert "jump_held = _jumpHeld" in fixed_body
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

    # The replay schema remains backward compatible, but the grounded-world live command
    # source has no held runtime state for retired Pulse/Guard actions.
    assert "_guardDownLatched" not in source
    assert "_guardHeld" not in source
    assert "_fireHeld" not in source

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
    assert "motor.RequestDash(aim)" in apply_body
    assert "motor.RequestJump()" in apply_body

    arbitration = apply_body[apply_body.index("// Roll has first refusal."):]
    order = (
        "if (command.dash_down",
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
                      tape.index("GuardianCommandFrame recorded")]
    assert "return live" not in exhaustion
    assert "GuardianCommandFrame.Neutral(live.tick)" in exhaustion
