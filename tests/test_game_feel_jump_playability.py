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


def test_jump_has_modern_forgiveness_variable_height_and_air_control():
    motor = read("Combat", "GuardianMotor.cs")

    for token in (
        "jumpVelocity = 7.2f",
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
        "ConsumeBufferedJump()",
        "Jumped?.Invoke()",
        "Landed?.Invoke",
    ):
        assert token in motor

    # Jump/gravity remain conventional gameplay authority and never touch neural state.
    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
        "sight_score",
        "guard_score",
        "TryApply(",
    ):
        assert forbidden not in motor


def test_space_is_jump_ctrl_alt_are_dodge_and_tape_records_both_jump_edges():
    input_source = read("Combat", "GuardianCombatInput.cs")
    tape = read("Combat", "GuardianInputTape.cs")

    for token in (
        "Input.GetKeyDown(KeyCode.Space)",
        "Input.GetKey(KeyCode.Space)",
        "Input.GetKeyDown(KeyCode.LeftControl)",
        "Input.GetKeyDown(KeyCode.LeftAlt)",
        "jump_down = _jumpLatched",
        "jump_held = _jumpHeld",
        "motor.SetJumpHeld(command.jump_held)",
        "motor.RequestJump()",
    ):
        assert token in input_source

    for token in (
        'SchemaV3 = "mindforge.guardian_input_tape.v3"',
        "schema = GuardianInputTape.SchemaV3",
        "public bool jump_down",
        "public bool jump_held",
        "jump_down = jump_down",
        "jump_held = jump_held",
    ):
        assert token in tape


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


def test_null_ward_hud_teaches_controls_then_gets_out_of_the_way():
    hud = read("World", "NullWardHud.cs")

    for token in (
        "controlsHintSeconds = 16f",
        "showControls = Time.realtimeSinceStartupAsDouble - _enteredAt",
        "SPACE jump · CTRL/ALT dodge",
        "float height = showControls ? 58f : 38f",
    ):
        assert token in hud
