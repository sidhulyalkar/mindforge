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


def test_shift_and_rmb_are_canonical_evade_air_dash_inputs_while_player_pulse_is_retired():
    source = read("Combat", "GuardianCombatInput.cs")
    controls = read("Combat", "GuardianControlProfileV1.cs")

    for token in (
        "evadeBoostPrimary = KeyCode.LeftShift",
        "evadeBoostSecondary = KeyCode.RightShift",
        "rightMouseEvades = true",
        "Input.GetMouseButtonDown(1)",
        "jumpHover = KeyCode.Space",
    ):
        assert token in controls

    for token in (
        "controls.Pressed(GuardianControlAction.EvadeBoost)",
        "controls.Pressed(GuardianControlAction.JumpHover)",
        "controls.Held(GuardianControlAction.JumpHover)",
        "Shield hold and player Pulse fire are intentionally retired",
        "endurance.DodgeBaseCost",
        "dodgeCommandBufferSeconds = 0.15f",
    ):
        assert token in source

    assert "Input.GetKey(KeyCode.X)" not in source
    assert "Input.GetMouseButton(2)" not in source
    assert "combat.FirePulse(aim)" not in source
    dash = source.index("if (command.dash_down)")
    jump = source.index("if (command.jump_down", dash)
    assert dash < jump
    assert "QueueDodgeCommand(aim)" in source[dash:jump]


def test_existing_input_tape_fields_replay_aerial_actions_without_new_aerial_schema_surface():
    tape = read("Combat", "GuardianInputTape.cs")

    assert 'SchemaV3 = "mindforge.guardian_input_tape.v3"' in tape
    assert 'SchemaV5 = "mindforge.guardian_input_tape.v5"' in tape
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

    assert "Vector3 toPlayer = Planar(player.position - transform.position)" in enemy
    assert "Vector3 next = body.position + _desiredMove.normalized * moveSpeed * Time.fixedDeltaTime" in enemy


def test_fractured_signal_melee_has_truthful_vertical_reach_and_jumpable_slam():
    boss = read("Combat", "FracturedSignalMeleeDirector.cs")

    for token in (
        "engageVerticalReach = 2.2f",
        "cleaveVerticalReach = 1.85f",
        "slamVerticalReach = 1.05f",
        "vertical <= Mathf.Max(0.5f, engageVerticalReach)",
        'return "JUMPED"',
        "VerticalDistanceToPlayer() > Mathf.Max(0.4f, cleaveVerticalReach)",
        "VerticalDistanceToPlayer() > Mathf.Max(0.25f, slamVerticalReach)",
    ):
        assert token in boss

    director = read("Combat", "FracturedSignalDirector.cs")
    assert "Vector3 center = (player.position - origin).normalized" in director
    assert "Quaternion.AngleAxis(offset, Vector3.up) * center" in director


def test_grounded_hud_teaches_aerial_escape_without_restoring_shield_or_pulse_clutter():
    ward = read("World", "NullWardHud.cs")
    hud = read("Presentation", "GroundedCombatHud.cs")
    guide = read("Presentation", "PlayerAgencyGuide.cs")
    menu = read("Presentation", "GuardianEquipmentMenu.cs")

    assert "SHIFT/RMB roll · SPACE jump ×2 / hold hover" in ward
    assert '"ENDURANCE"' in hud
    assert '"SPACE ×2 / HOLD · SHIFT AIR DASH"' in hud
    assert '"F / LMB BLADE   ·   SHIFT / RMB ROLL' in hud
    assert "GuardianControlAction.EvadeBoost" in guide
    assert '" EVADE   ·   "' in guide
    assert "MOUSE WHEEL CYCLES LOCKED TARGETS" in guide
    assert "EEG never moves, jumps, hovers, evades" in guide
    assert "GuardianControlAction.EvadeBoost" in menu
    assert '"Dodge roll · air dash · mounted boost"' in menu
    assert '"Pulse Shot"' not in menu
    assert '"Shield"' not in menu
