from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_double_jump_hover_and_air_dash_are_fixed_tick_guardian_authority():
    motor = read("Combat", "GuardianMotor.cs")

    for token in (
        "airJumpVelocity = 6.8f",
        "minimumAirJumpDelaySeconds = 0.08f",
        "hoverMaximumSeconds = 1.35f",
        "hoverFallSpeed = 2.15f",
        "hoverBrakeAcceleration = 24f",
        "airDashSpeedMultiplier = 1.08f",
        "airDashDurationMultiplier = 0.82f",
        "airDashInvulnerabilitySeconds = 0.075f",
        "public bool CanAirJump",
        "public bool CanAirDash",
        "public bool IsHovering",
        "public bool IsAirDashing",
        "public event Action DoubleJumped",
        "public event Action AirDashStarted",
        "public event Action<bool> HoverChanged",
        "if (airJump) _airJumpConsumed = true",
        "if (airDash) _airDashConsumed = true",
        "_airJumpConsumed = false",
        "_airDashConsumed = false",
        "_hoverRemainingSeconds = Mathf.Max(0f, hoverMaximumSeconds)",
        "private void FixedUpdate()",
        "ApplyVerticalMotion(dt)",
    ):
        assert token in motor

    assert "private void Update()" not in motor
    assert "Time.deltaTime" not in motor

    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "CalibrationReady =",
        "TryApply(",
    ):
        assert forbidden not in motor


def test_hold_space_transitions_from_variable_jump_to_bounded_hover():
    motor = read("Combat", "GuardianMotor.cs")

    assert "_jumpHeld &&" in motor
    assert "velocity.y <= hoverActivationVerticalSpeed" in motor
    assert "_hoverRemainingSeconds > 0f" in motor
    assert "velocity.y = Mathf.MoveTowards(" in motor
    assert "gravity * Mathf.Clamp(hoverGravityMultiplier" in motor
    assert "_hoverRemainingSeconds = Mathf.Max(0f, _hoverRemainingSeconds - dt)" in motor
    assert "if (!held && _hovering) SetHovering(false)" in motor


def test_airborne_attacks_keep_bounded_steering_without_erasing_ground_commitment():
    motor = read("Combat", "GuardianMotor.cs")

    assert "airborneCombatMovementFloor = 0.62f" in motor
    assert "if (!_grounded && physicalCombat != null" in motor
    assert "stanceMultiplier = Mathf.Max(stanceMultiplier, Mathf.Clamp01(airborneCombatMovementFloor))" in motor
    assert "physicalCombat.MovementMultiplier" in motor


def test_shift_is_primary_dash_and_pulse_moves_to_x_or_middle_mouse():
    source = read("Combat", "GuardianCombatInput.cs")

    for token in (
        "Input.GetKeyDown(KeyCode.LeftShift)",
        "Input.GetKeyDown(KeyCode.RightShift)",
        "Input.GetKey(KeyCode.X) || Input.GetMouseButton(2)",
        "Space: jump / double jump; hold while descending to hover / slow fall",
        "Left/Right Shift: directional dodge / air dash",
        "X or MMB: Pulse Shot",
    ):
        assert token in source

    assert "_fireHeld = Input.GetKey(KeyCode.LeftShift)" not in source
    assert "if (command.dash_down" in source
    assert source.index("if (command.dash_down") < source.index("if (command.jump_down")


def test_existing_input_tape_fields_replay_aerial_actions_without_new_schema_surface():
    tape = read("Combat", "GuardianInputTape.cs")

    assert 'SchemaV3 = "mindforge.guardian_input_tape.v3"' in tape
    assert "public bool dash_down" in tape
    assert "public bool jump_down" in tape
    assert "public bool jump_held" in tape
    assert "dash_down = dash_down" in tape
    assert "jump_down = jump_down" in tape
    assert "jump_held = jump_held" in tape


def test_ordinary_enemies_respect_height_for_melee_and_track_airborne_targets_with_projectiles():
    enemy = read("Journey", "JourneyEnemyController.cs")

    for token in (
        "meleeVerticalReach = 1.45f",
        "projectileTargetHeight = 0.85f",
        "_lockedProjectileDirection",
        "ResolveProjectileAimDirection()",
        "verticalDelta > Mathf.Max(0.4f, meleeVerticalReach)",
        "attack.Type == EnemyAttackType.Projectile || attack.Type == EnemyAttackType.Burst",
        "Quaternion.AngleAxis(angle, Vector3.up) * baseDirection",
        "Vector3 target = player.position + Vector3.up * Mathf.Max(0f, projectileTargetHeight)",
    ):
        assert token in enemy

    # Locomotion remains planar while projectile authority is allowed to aim in 3D.
    assert "Vector3 toPlayer = Planar(player.position - transform.position)" in enemy
    assert "Vector3 next = body.position + _desiredMove.normalized * moveSpeed * Time.fixedDeltaTime" in enemy


def test_hud_teaches_aerial_controls_without_adding_a_third_large_panel():
    ward = read("World", "NullWardHud.cs")
    hud = read("Presentation", "CombatStateHud.cs")
    guide = read("Presentation", "PlayerAgencyGuide.cs")

    assert "SPACE jump ×2 / hold to hover · SHIFT dash / air dash" in ward
    assert "motor.HoverRemaining01" in hud
    assert "SPACE ×2 / HOLD HOVER · SHIFT AIR DASH" in hud
    assert "const float width = 326f" in hud
    assert "const float width = 300f" in hud
    assert "SPACE JUMP ×2 / HOLD HOVER" in guide
    assert "X / MMB PULSE SHOT" in guide
    assert "EEG never moves, jumps, hovers, air-dashes" in guide
