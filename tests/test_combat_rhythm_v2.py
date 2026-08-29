from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_dodge_edge_is_fixed_tick_buffered_through_sword_commitment():
    source = read("Combat", "GuardianCombatInput.cs")

    for token in (
        'Header("Combat rhythm")',
        "dodgeCommandBufferSeconds = 0.15f",
        "private bool _dodgeCommandQueued",
        "private long _dodgeCommandExpiresTick",
        "private Vector3 _dodgeCommandAim",
        "QueueDodgeCommand(aim)",
        "TryConsumeQueuedDodge()",
        "_dodgeCommandExpiresTick = _fixedInputTick + SecondsToInputTicks",
        "if (physicalCombat != null && !physicalCombat.CanDodge) return false;",
    ):
        assert token in source

    # The edge enters the queue only after the command frame is resolved. Therefore live,
    # record and replay all execute the same deterministic buffer path.
    resolved = source.index("GuardianCommandFrame command = inputTape != null ? inputTape.Resolve")
    apply = source.index("Apply(command)", resolved)
    queue = source.index("QueueDodgeCommand(aim)", apply)
    assert resolved < apply < queue


def test_buffer_is_not_a_sword_animation_cancel_or_new_authority_path():
    source = read("Combat", "GuardianCombatInput.cs")
    sword = read("Combat", "GuardianSwordShieldController.cs")

    assert "This is input buffering, not an animation cancel" in source
    assert "public bool CanDodge => ActionState == GuardianActionState.Locomotion || ActionState == GuardianActionState.Guard" in sword
    assert "AttackStartup" in sword and "AttackActive" in sword and "AttackRecovery" in sword

    for forbidden in (
        "animator",
        "AnimationEvent",
        "SetTrigger(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
        "NeuralFocusResonance",
    ):
        assert forbidden not in source


def test_endurance_is_spent_only_if_buffered_roll_actually_starts():
    source = read("Combat", "GuardianCombatInput.cs")

    request = source.index("if (!motor.RequestDash(_dodgeCommandAim))")
    spend = source.index('endurance?.TrySpend(cost, grounded ? "DODGE_ROLL" : "AIR_DASH")', request)
    clear = source.index("ClearDodgeCommand();", spend)
    assert request < spend < clear

    # The buffer cannot wait for stamina recovery and surprise the player later.
    assert "if (endurance != null && !endurance.CanSpend(cost))" in source
    assert "until Endurance regenerates" in source
    assert source.count("ClearDodgeCommand();") >= 5


def test_pending_roll_owns_committed_action_priority_until_execution_or_expiry():
    source = read("Combat", "GuardianCombatInput.cs")
    start = source.index("if (command.dash_down)")
    body = source[start:source.index("private void QueueDodgeCommand", start)]

    queue = body.index("QueueDodgeCommand(aim)")
    pending_return = body.index("if (_dodgeCommandQueued) return;")
    jump = body.index("if (command.jump_down")
    sword = body.index("if (command.sword_attack_down)")
    special_lock = body.index("physicalCombat.ActionState != GuardianActionState.Locomotion")
    assert queue < pending_return < jump < sword < special_lock


def test_buffer_clears_on_authority_suspension_and_uses_fixed_time_only():
    source = read("Combat", "GuardianCombatInput.cs")

    disable = source[source.index("private void OnDisable()"):source.index("private void Update()")]
    set_enabled = source[source.index("public void SetCombatActionsEnabled"):source.index("private void Start()")]
    assert "ClearDodgeCommand();" in disable
    assert "if (!enabled) ClearDodgeCommand();" in set_enabled
    assert "Time.fixedDeltaTime" in source
    assert "Time.time" not in source
    assert "Time.deltaTime" not in source
