from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_guardian_motor_upgrades_prototype_planar_body_into_fixed_tick_3d_movement():
    motor = read("Combat", "GuardianMotor.cs")

    for token in (
        "RigidbodyConstraints.FreezePositionY",
        "_body.constraints &= ~(RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionY)",
        "_body.useGravity = false",
        "Physics.gravity.y",
        "private void FixedUpdate()",
        "Physics.SphereCastNonAlloc",
        "QueryTriggerInteraction.Ignore",
        "maxGroundSlopeDegrees = 52f",
        "groundStickSpeed = 2.2f",
        "PhysicMaterial(\"MindforgeGuardianLowFriction\")",
        "frictionCombine = PhysicMaterialCombine.Minimum",
    ):
        assert token in motor

    assert "private void Update()" not in motor
    assert "Time.deltaTime" not in motor
    assert "_body.AddForce" not in motor


def test_jump_has_modern_forgiveness_variable_height_air_control_and_one_air_jump():
    motor = read("Combat", "GuardianMotor.cs")

    for token in (
        "jumpVelocity = 7.2f",
        "airJumpVelocity = 6.8f",
        "coyoteTimeSeconds = 0.11f",
        "jumpBufferSeconds = 0.13f",
        "jumpReleaseVelocityMultiplier = 0.52f",
        "risingGravityMultiplier = 1.18f",
        "apexGravityMultiplier = 0.88f",
        "releasedJumpGravityMultiplier = 2.45f",
        "fallingGravityMultiplier = 2.30f",
        "terminalFallSpeed = 28f",
        "airAcceleration = 34f",
        "airSpeedMultiplier = 0.92f",
        "public bool RequestJump()",
        "public bool CanAirJump",
        "ConsumeBufferedJump()",
        "Jumped?.Invoke()",
        "DoubleJumped?.Invoke()",
        "Landed?.Invoke",
    ):
        assert token in motor

    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
        "sight_score",
        "guard_score",
        "TryApply(",
    ):
        assert forbidden not in motor


def test_space_is_aerial_traversal_shift_is_primary_roll_and_tape_records_edges():
    input_source = read("Combat", "GuardianCombatInput.cs")
    tape = read("Combat", "GuardianInputTape.cs")

    for token in (
        "Input.GetKeyDown(KeyCode.Space)",
        "Input.GetKey(KeyCode.Space)",
        "Input.GetKeyDown(KeyCode.LeftShift)",
        "Input.GetKeyDown(KeyCode.RightShift)",
        "Input.GetMouseButtonDown(1)",
        "Input.GetKeyDown(KeyCode.LeftControl)",
        "Input.GetKeyDown(KeyCode.LeftAlt)",
        "jump_down = _jumpLatched",
        "jump_held = _jumpHeld",
        "motor.SetJumpHeld(command.jump_held)",
        "motor.RequestJump()",
    ):
        assert token in input_source

    # V3 remains loadable for old aerial tapes; V4 is now the recording schema because it
    # adds mounted commands without changing the existing jump fields.
    for token in (
        'SchemaV3 = "mindforge.guardian_input_tape.v3"',
        'SchemaV4 = "mindforge.guardian_input_tape.v4"',
        "schema = GuardianInputTape.SchemaV4",
        "public bool jump_down",
        "public bool jump_held",
        "jump_down = jump_down",
        "jump_held = jump_held",
    ):
        assert token in tape


def test_input_suspension_clears_all_active_edge_triggered_actions_and_neutralizes_retired_guard():
    input_source = read("Combat", "GuardianCombatInput.cs")
    disable = input_source[input_source.index("private void OnDisable()"):input_source.index("private void Update()")]

    for token in (
        "_move = Vector2.zero",
        "_cleaveLatched = false",
        "_counterLatched = false",
        "_dashLatched = false",
        "_jumpLatched = false",
        "_jumpHeld = false",
        "_bloomLatched = false",
        "_swordAttackLatched = false",
        "motor?.SetMoveInput(Vector2.zero)",
        "motor?.SetJumpHeld(false)",
        "physicalCombat?.SetGuardHeld(false, _currentAimDirection)",
    ):
        assert token in disable

    # Retired controls must not linger as held runtime state that can fire when the input
    # authority is re-enabled after checkpoint/calibration suspension.
    for retired in ("_fireHeld", "_guardHeld", "_guardDownLatched"):
        assert retired not in input_source


def test_camera_is_tighter_smooths_vertical_travel_and_ignores_dynamic_actors_for_collision():
    camera = read("Presentation", "ShowcaseCameraRig.cs")

    for token in (
        "pivotHeight = 1.28f",
        "freeDistance = 4.45f",
        "lockDistance = 5.20f",
        "shoulderOffset = 0.70f",
        "initialPitch = 12f",
        "verticalFollowSmoothSeconds = 0.105f",
        "_smoothedPivotY = Mathf.SmoothDamp",
        "IsDynamicActor(collider)",
        "GetComponentInParent<CombatantVitals>()",
        "Physics.SphereCastNonAlloc",
    ):
        assert token in camera

    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "TryApply(",
        "RequestJump(",
        "RequestDash(",
    ):
        assert forbidden not in camera


def test_jump_and_landing_have_downstream_visual_language_without_movement_authority():
    polish = read("Presentation", "GuardianMotionPolish.cs")
    vfx = read("Presentation", "GuardianLocomotionVfx.cs")
    animator = read("Presentation", "GuardianAnimatorBridge.cs")

    for token in (
        "motor.Jumped += OnJumped",
        "motor.Landed += OnLanded",
        "bool grounded = motor == null || motor.IsGrounded",
        "rise01",
        "fall01",
        "_landingImpulse",
    ):
        assert token in polish

    for token in (
        "!motor.IsGrounded",
        "motor.Jumped += OnJumped",
        "motor.Landed += OnLanded",
        "OnLanded(float impactSpeed)",
        "fullSpeedReference = 11.2f",
    ):
        assert token in vfx

    for token in (
        'Animator.StringToHash("VerticalSpeed")',
        'Animator.StringToHash("Grounded")',
        'Animator.StringToHash("Airborne")',
        'Animator.StringToHash("Jump")',
        'Animator.StringToHash("Land")',
        "motor.Jumped += OnJump",
        "motor.Landed += OnLand",
    ):
        assert token in animator

    for source in (polish, vfx, animator):
        for forbidden in ("RequestJump(", "RequestDash(", "ReceiveDamage(", "TryApply("):
            assert forbidden not in source


def test_null_ward_hud_teaches_grounded_roll_and_aerial_controls_then_gets_out_of_the_way():
    hud = read("World", "NullWardHud.cs")

    for token in (
        "controlsHintSeconds = 13f",
        "showControls = Time.realtimeSinceStartupAsDouble - _enteredAt",
        "SHIFT/RMB roll · SPACE jump ×2 / hold hover · T lock · F/LMB Aetherblade",
        "float height = showControls ? 58f : 38f",
    ):
        assert token in hud


def test_optional_traversal_layer_gives_jump_real_geometry_without_gating_main_route():
    builder = read("Editor", "NullWardTraversalPlayabilityBuilder.cs")
    showcase = read("Editor", "ShowcaseEditorMenu.cs")

    for token in (
        'RootName = "Mindforge_NullWard_TraversalPlayability_V1"',
        'new GameObject("Maintenance_JumpLine")',
        '"SignalBlock_A"',
        '"SignalBlock_B"',
        '"SignalBlock_C"',
        '"LandingPad_A"',
        '"LandingPad_B"',
        "opposite side of the 5 m maintenance run open as a no-jump bypass",
        'new GameObject("Market_TraversalPlinths")',
        "primary Null Ward route remains ground-completable",
        "StaticEditorFlags.BatchingStatic",
    ):
        assert token in builder

    assert "NullWardSceneBuilder.BuildOpenScene();" in showcase
    assert "NullWardVisualInfrastructureBuilder.ApplyOpenScene();" in showcase
    assert "NullWardTraversalPlayabilityBuilder.ApplyOpenScene();" in showcase
    assert showcase.index("NullWardSceneBuilder.BuildOpenScene();") < showcase.index("NullWardTraversalPlayabilityBuilder.ApplyOpenScene();")

    for forbidden in (
        "CombatantVitals",
        "GuardianMotor",
        "GuardianCombatInput",
        "NeuralEvent",
        "VepAuraStimulus",
        "TryApply(",
        "ReceiveDamage(",
    ):
        assert forbidden not in builder
