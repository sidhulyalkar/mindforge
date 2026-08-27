from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
COMBAT = ROOT / "unity" / "Assets" / "Mindforge" / "Combat"


def read(name: str) -> str:
    return (COMBAT / name).read_text(encoding="utf-8")


def test_guardian_input_tape_is_fixed_tick_versioned_and_fail_neutral():
    tape = read("GuardianInputTape.cs")
    assert 'SchemaV1 = "mindforge.guardian_input_tape.v1"' in tape
    assert 'SchemaV2 = "mindforge.guardian_input_tape.v2"' in tape
    assert "schema = GuardianInputTape.SchemaV2" in tape
    assert "_tape.schema != SchemaV1 && _tape.schema != SchemaV2" in tape
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


def test_guardian_input_recording_does_not_write_per_tick():
    tape = read("GuardianInputTape.cs")
    resolve_start = tape.index("public GuardianCommandFrame Resolve")
    resolve_end = tape.index("public string SaveRecording")
    resolve_body = tape[resolve_start:resolve_end]
    assert "File.WriteAllText" not in resolve_body
    assert "_tape.frames.Add" in resolve_body
    assert "File.WriteAllText" in tape[tape.index("public string SaveRecording"):]


def test_guardian_combat_input_samples_in_update_and_executes_on_fixed_tick():
    source = read("GuardianCombatInput.cs")
    update_start = source.index("private void Update()")
    fixed_start = source.index("private void FixedUpdate()")
    apply_start = source.index("private void Apply(")

    update_body = source[update_start:fixed_start]
    fixed_body = source[fixed_start:apply_start]
    apply_body = source[apply_start:]

    # Update is device sampling/latching only. Gameplay authority is fixed-tick.
    assert "Input.GetAxisRaw" in update_body
    assert "Input.GetKeyDown" in update_body
    assert "Input.GetMouseButtonDown" in update_body
    assert "combat.FirePulse" not in update_body
    assert "combat.RiftCleave" not in update_body
    assert "motor.RequestDash" not in update_body
    assert "physicalCombat?.TryLightAttack" not in update_body

    assert "GuardianCommandFrame" in fixed_body
    assert "_cleaveLatched = false" in fixed_body
    assert "_counterLatched = false" in fixed_body
    assert "_dashLatched = false" in fixed_body
    assert "_bloomLatched = false" in fixed_body
    assert "_swordAttackLatched = false" in fixed_body
    assert "_guardDownLatched = false" in fixed_body
    assert "inputTape.Resolve" in fixed_body

    assert "motor.SetMoveInput(command.Move)" in apply_body
    assert "if (!CombatActionsEnabled)" in apply_body
    assert "physicalCombat?.SetGuardHeld(false, aim)" in apply_body
    assert "physicalCombat?.SetGuardHeld(command.guard_held, aim)" in apply_body
    assert "physicalCombat?.TryLightAttack(aim)" in apply_body
    assert "combat.FirePulse(aim)" in apply_body
    assert "combat.RiftCleave(aim)" in apply_body
    assert "combat.BeginCounter()" in apply_body
    assert "motor.RequestDash(aim)" in apply_body
    assert "bloom?.TryActivate()" in apply_body


def test_replay_never_has_a_live_input_fallback_after_exhaustion():
    tape = read("GuardianInputTape.cs")
    exhaustion = tape[tape.index("if (_tape == null || _tape.frames == null"):
                      tape.index("GuardianCommandFrame recorded")]
    assert "return live" not in exhaustion
    assert "GuardianCommandFrame.Neutral(live.tick)" in exhaustion
