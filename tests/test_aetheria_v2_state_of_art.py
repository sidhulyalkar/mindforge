from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"
DOCS = ROOT / "docs"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_mounted_v2_uses_same_fixed_tick_tape_without_parallel_vehicle_replay():
    tape = read("Combat", "GuardianInputTape.cs")
    foot = read("Combat", "GuardianCombatInput.cs")
    bike = read("Traversal", "GuardianHoverbikeController.cs")

    assert 'SchemaV4 = "mindforge.guardian_input_tape.v4"' in tape
    assert "mount_toggle_down" in tape
    assert "mounted_attack_down" in tape
    assert "mounted_boost_down" in tape
    assert "mounted_move_x" in tape and "mounted_move_y" in tape and "mounted_move_z" in tape
    assert "public Vector3 MountedMove" in tape
    assert "public static long FixedTickNow" in tape
    assert "_lastResolvedFrame.MergeFrom(live)" in tape

    assert "_fixedInputTick = GuardianInputTape.FixedTickNow" in foot
    assert "mount_toggle_down = _mountLatched" in foot
    assert "private long FixedTick => GuardianInputTape.FixedTickNow" in bike
    assert "GuardianCommandFrame live = new GuardianCommandFrame" in bike
    assert "inputTape.Resolve(live, fixedHz)" in bike
    assert "command.mount_toggle_down" in bike
    assert "command.mounted_attack_down" in bike
    assert "command.mounted_boost_down" in bike
    assert "liveMountedMove = _mounted ? CameraRelativeDirection(_moveInput) : Vector3.zero" in bike
    assert "mounted_move_x = liveMountedMove.x" in bike
    assert "mounted_move_y = liveMountedMove.y" in bike
    assert "mounted_move_z = liveMountedMove.z" in bike
    assert "ApplyMountedMovement(aim, command.MountedMove)" in bike
    assert "bladeCombat != null && bladeCombat.TryLightAttack(aim)" in bike
    assert "Replay mount edge could not resolve the authored bike in range" in bike

    # Camera-relative steering is a record-time transform only. Replayed movement consumes
    # the stored world vector directly and is therefore independent of live camera yaw.
    assert "CameraRelativeDirection(command.Move)" not in bike
    assert "Vector3.ProjectOnPlane(resolvedWorldMove, Vector3.up)" in bike

    for forbidden in (
        "VehicleInputTape",
        "HoverbikeInputTape",
        "AddComponent<Rigidbody>",
        "ReceiveDamage(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
    ):
        assert forbidden not in bike


def test_mounted_boost_trades_turn_authority_for_speed_without_invulnerability():
    bike = read("Traversal", "GuardianHoverbikeController.cs")
    assert "boostTurnMultiplier = 0.78f" in bike
    assert "Boosting ? Mathf.Clamp(boostTurnMultiplier, 0.25f, 1f) : 1f" in bike
    assert "boostSpeed = 21.2f" in bike
    assert "cruiseSpeed = 15.2f" in bike
    for forbidden in ("IsInvulnerable", "invulnerable", "RequestDash("):
        assert forbidden not in bike


def test_high_speed_camera_changes_physical_composition_but_keeps_projection_fixed():
    camera = read("Presentation", "ShowcaseCameraRig.cs")

    for token in (
        "GuardianHoverbikeController hoverbike",
        "mountedPivotHeight = 1.48f",
        "mountedFreeDistance = 5.65f",
        "mountedLockDistance = 6.15f",
        "mountedVelocityLookAhead = 1.75f",
        "hoverbike.PlanarVelocity",
        "hoverbike.Speed01",
        "ResolveCameraCollision(pivot, desiredPosition)",
        "gameplayCamera.fieldOfView = Mathf.Clamp(gameplayFieldOfView, 45f, 75f)",
    ):
        assert token in camera

    fov_line = "gameplayCamera.fieldOfView = Mathf.Clamp(gameplayFieldOfView, 45f, 75f)"
    assert camera.count(fov_line) == 1
    assert "fieldOfView = Mathf.Lerp" not in camera
    assert "fieldOfView = Mathf.SmoothDamp" not in camera
    assert "Boosting ?" not in camera[camera.index("if (gameplayCamera != null)"):]
    for forbidden in ("ReceiveDamage(", "SetMoveInput(", "TryLightAttack(", "NeuralEvent", "VepAuraStimulus"):
        assert forbidden not in camera


def test_hoverbike_kinetics_are_read_only_and_do_not_move_physics_body():
    source = read("Presentation", "HoverbikeKineticPresentationV2.cs")
    for token in (
        "bike.PlanarVelocity",
        "bike.Speed01",
        "bike.MountedChanged +=",
        "bike.BoostStarted +=",
        "bike.MountedAttackIssued +=",
        "maximumBankDegrees = 14f",
        'IndexOf("Exhaust"',
        "Time.unscaledDeltaTime",
    ):
        assert token in source

    for forbidden in (
        "GetComponent<Rigidbody>",
        "AddComponent<Rigidbody>",
        "private Rigidbody",
        "public Rigidbody",
        "MovePosition(",
        ".velocity =",
        "ReceiveDamage(",
        "TryLightAttack(",
        "Input.Get",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
    ):
        assert forbidden not in source


def test_procedural_audio_is_cached_event_driven_and_does_not_double_fire_air_dash():
    source = read("Presentation", "AetheriaCombatAudioV2.cs")
    motor = read("Combat", "GuardianMotor.cs")
    for token in (
        "AudioClip.Create(",
        "clip.SetData(data, 0)",
        "motor.Jumped +=",
        "motor.DoubleJumped +=",
        "motor.DashStarted += OnDash",
        "motor.Landed +=",
        "blade.SwordAttackStarted +=",
        "blade.SwordHit +=",
        "blade.SwordProjectileParried +=",
        "bike.MountedChanged +=",
        "bike.BoostStarted +=",
        "boss.AttackTelegraphed +=",
        "boss.AttackFired +=",
        "bossMelee.MeleeTelegraphed +=",
        "_motorLoop.clip = _motor",
        "motor != null && motor.IsAirDashing",
        'Tone("PrismBike_MotorLoop", 0.32f, 75f, 75f',
    ):
        assert token in source

    # GuardianMotor emits the generic dash edge for every dash and additionally emits the
    # air-specific edge. Audio listens only to the generic event and branches on resolved
    # motor state, preventing an air dash from stacking two one-shots.
    assert "DashStarted?.Invoke();" in motor
    assert "if (airDash) AirDashStarted?.Invoke();" in motor
    assert "motor.AirDashStarted +=" not in source
    assert "motor.AirDashStarted -=" not in source
    assert "OnAirDash" not in source

    # Synthesis happens once in BuildClips; Update only changes a cached loop source.
    update = source[source.index("private void Update()"):source.index("private void Resolve()")]
    assert "AudioClip.Create" not in update
    assert "SetData(" not in update

    for forbidden in (
        "ReceiveDamage(",
        "RequestDash(",
        "RequestJump(",
        "SetMoveInput(",
        "TryLightAttack(",
        "Input.Get",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
    ):
        assert forbidden not in source


def test_malatract_v2_consumes_existing_phase_truth_only():
    source = read("Presentation", "LordMalatractPhaseStagingV2.cs")
    for token in (
        "director.PhaseChanged += OnPhaseChanged",
        "director.AttackTelegraphed += OnAttackTelegraphed",
        "director.AttackFired += OnAttackFired",
        "director.Phase",
        "LordMalatractPresentationV1.RootName",
        '"MalatractCrownL"',
        '"MalatractCrownR"',
        '"OrderedRuinBlade"',
        '"MalatractPhaseHaloV2"',
    ):
        assert token in source

    for forbidden in (
        "ReceiveDamage(",
        "Instantiate(projectile",
        "SpawnRadial",
        "SpawnAimedFan",
        "ExecuteCleave(",
        "ExecuteSlam(",
        "Input.Get",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in source


def test_v2_builder_is_an_isolated_presentation_layer_in_deterministic_build_order():
    builder = read("Editor", "AetheriaStateOfArtV2Builder.cs")
    menu = read("Editor", "ShowcaseEditorMenu.cs")

    for token in (
        'RootName = "Mindforge_Aetheria_StateOfArt_V2"',
        "AetheriaWorldV1Builder.RootName",
        "guardian.AddComponent<HoverbikeKineticPresentationV2>()",
        "guardian.AddComponent<AetheriaCombatAudioV2>()",
        "boss.gameObject.AddComponent<LordMalatractPhaseStagingV2>()",
        "LordMalatractPresentationV1",
    ):
        assert token in builder

    for forbidden in (
        "AddComponent<Rigidbody>",
        "ReceiveDamage(",
        "RequestDash(",
        "RequestJump(",
        "SetMoveInput(",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in builder

    horde = menu.index("AetheriaHordeBossV1Builder.ApplyOpenScene();")
    world = menu.index("AetheriaWorldV1Builder.ApplyOpenScene();")
    v2 = menu.index("AetheriaStateOfArtV2Builder.ApplyOpenScene();")
    safety = menu.index("AetheriaDynamicMountSafetyBuilder.ApplyOpenScene();")
    visual = menu.index("NullWardVisualInfrastructureBuilder.ApplyOpenScene();")
    assert horde < world < v2 < safety < visual
    assert "one v4 fixed-tick conventional-input tape" in menu
    assert "keeping FOV fixed" in menu


def test_aetheria_v2_contract_and_new_unity_guids_exist():
    contract = DOCS / "AETHERIA_V2_PRODUCTION_POLISH.md"
    assert contract.exists()
    text = contract.read_text(encoding="utf-8")
    assert "One conventional-input history" in text
    assert "mounted_move_*" in text
    assert "live later" in text
    assert "speed-reactive FOV" in text
    assert "Replay remains fail-neutral" in text
    assert "does **not** add" in text

    metas = (
        UNITY / "Presentation" / "HoverbikeKineticPresentationV2.cs.meta",
        UNITY / "Presentation" / "AetheriaCombatAudioV2.cs.meta",
        UNITY / "Presentation" / "LordMalatractPhaseStagingV2.cs.meta",
        UNITY / "Editor" / "AetheriaStateOfArtV2Builder.cs.meta",
    )
    guids = []
    for path in metas:
        value = path.read_text(encoding="utf-8")
        assert "fileFormatVersion: 2" in value
        guid = next(line for line in value.splitlines() if line.startswith("guid: ")).split(":", 1)[1].strip()
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
