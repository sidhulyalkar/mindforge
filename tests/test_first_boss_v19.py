from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    path = UNITY.joinpath(*parts)
    assert path.exists(), f"missing V19 source: {path}"
    return path.read_text(encoding="utf-8")


def test_first_boss_is_a_moving_duelist_not_a_stationary_projectile_turret():
    boss = read("Combat", "FracturedSignalFirstBossV19.cs")
    director = read("Combat", "FracturedSignalDirector.cs")

    for token in (
        "phaseOneInterval = 2.15f",
        "phaseTwoInterval = 1.78f",
        "phaseThreeInterval = 1.48f",
        "phaseOnePreferredDistance = 4.35f",
        "phaseTwoPreferredDistance = 5.10f",
        "phaseThreePreferredDistance = 4.20f",
        "Rigidbody _body",
        "_body.MovePosition(candidate)",
        "_body.MoveRotation(rotation)",
        "Physics.OverlapSphereNonAlloc",
        "homeLeashRadius = 5.4f",
        "TelegraphDuration()",
        "postAttackRecovery = 0.62f",
        "radialCount = 7",
        "maxEchoes = 2",
        "NeuralVisualFieldActive()",
        "_director.ExternalPaused",
        "CanSetPrivate<float>",
        "V19 profile applied nothing",
    ):
        assert token in boss

    # The V19 adapter deliberately fails loudly if the legacy attack scheduler is refactored.
    for field in (
        "phaseOneInterval",
        "phaseTwoInterval",
        "phaseThreeInterval",
        "phaseOneTelegraph",
        "phaseTwoTelegraph",
        "phaseThreeTelegraph",
        "radialCount",
        "maxEchoes",
    ):
        assert f"private float {field}" in director or f"private int {field}" in director
        assert f'SetPrivate("{field}"' in boss

    assert boss.index("if (!fieldsAvailable)") < boss.index('SetPrivate("phaseOneInterval"')

    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "GazeAttentionRouter",
        "TryApply(",
        "MarkResolved(",
    ):
        assert forbidden not in boss


def test_manual_wisp_window_remains_a_two_sided_combat_intermission():
    intermission = read("SoulWisp", "WispCombatIntermissionV19.cs")
    input_source = read("Combat", "GuardianCombatInput.cs")

    for token in (
        "WindowArmed += OnWindowArmed",
        "WindowEnded += OnWindowEnded",
        "DegradationStateChanged += OnLinkDegradationChanged",
        "OnLinkDegradationChanged(bool degraded)",
        "ReassertIntermission()",
        "_boss.SetExternalPause(true)",
        "_guardianInput.SetCombatActionsEnabled(false)",
        "projectile.SetExternalPause(true)",
        "_linkContingency.Degraded",
        "_linkContingency.ParticipantStopped",
        "_pausedBossByUs",
        "_pausedGuardianByUs",
        "ordinary movement/jump",
    ):
        assert token in intermission

    # Disabled combat still applies movement/jump before the branch returns, but it cannot
    # reach sword/special commands. That preserves the current manual-Wisp feel without
    # turning the ceasefire into a complete locomotion freeze.
    move = input_source.index("motor.SetMoveInput(command.Move);")
    disabled = input_source.index("if (!CombatActionsEnabled)")
    sword = input_source.index("if (command.sword_attack_down)")
    assert move < disabled < sword

    # Keep expensive projectile discovery out of the per-frame evidence path. It is allowed
    # on arm and the rare link-recovery transition, not every Update during SSVEP.
    update_start = intermission.index("private void Update()")
    update_end = intermission.index("private void OnDisable()", update_start)
    assert "FindObjectsOfType<MindforgeProjectile>" not in intermission[update_start:update_end]

    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "TryApply(",
        "MarkResolved(",
        "ReceiveDamage(",
    ):
        assert forbidden not in intermission


def test_fractured_signal_character_has_anatomy_weapon_and_neural_safe_pose_freeze():
    character = read("Presentation", "FracturedSignalCharacterV19.cs")
    mesh = read("Presentation", "OpenSourceMeshPrimitivesV19.cs")

    for token in (
        'RootName = "FracturedSignalCharacterV19"',
        '"FracturedHeart"',
        '"SignalMaskRig"',
        '"LeftShoulder"',
        '"RightShoulder"',
        '"LeftUpperArm"',
        '"RightUpperArm"',
        '"FractureBlade"',
        '"BrokenHalo"',
        '"RaggedPlate_',
        '"Crown_',
        'transform.Find("FracturedSignalShowcaseAvatar")',
        'transform.Find("FracturedSignalThreatSilhouette")',
        "NeuralVisualFieldActive()",
        "ApplyPose(Time.unscaledTime, true)",
        "director.AttackTelegraphed += OnTelegraph",
        "director.AttackFired += OnFired",
    ):
        assert token in character

    assert "CreateFacetedIcosahedron" in mesh
    assert "CreateTorus" in mesh
    assert "CreateShard" in mesh
    assert "aadebdeb/ProceduralMesh" in mesh

    # Render-only presentation must not become a second gameplay body.
    for forbidden in (
        "AddComponent<Collider",
        "AddComponent<Rigidbody",
        "ReceiveDamage(",
        "TryLockTarget(",
        "SetExternalPause(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "TryApply(",
    ):
        assert forbidden not in character


def test_v19_visual_motion_stops_during_ssvep_field_instead_of_becoming_background_flicker():
    character = read("Presentation", "FracturedSignalCharacterV19.cs")
    start = character.index("private void LateUpdate()")
    end = character.index("private void ApplyPose", start)
    update = character[start:end]
    assert "if (NeuralVisualFieldActive())" in update
    assert "return;" in update
    if "Mathf.Sin" in update:
        assert update.index("if (NeuralVisualFieldActive())") < update.index("Mathf.Sin")

    movement = read("Combat", "FracturedSignalFirstBossV19.cs")
    authority = movement[movement.index("private bool MovementAuthorityAvailable") : movement.index("private bool NeuralVisualFieldActive")]
    assert "if (NeuralVisualFieldActive()) return false;" in authority


def test_v19_does_not_vendor_unqualified_fullscreen_glitch_or_third_party_character_assets():
    character = read("Presentation", "FracturedSignalCharacterV19.cs")
    mesh = read("Presentation", "OpenSourceMeshPrimitivesV19.cs")
    combined = character + mesh
    for forbidden in (
        "FlashGlitch",
        "RenderFeature",
        "ScriptableRendererFeature",
        "Blit(",
        "OnRenderImage",
        "AssetBundle",
        ".fbx",
        ".blend",
    ):
        assert forbidden not in combined


def test_v19_runtime_scripts_have_pinned_unique_unity_meta_guids():
    paths = (
        UNITY / "Combat" / "FracturedSignalFirstBossV19.cs.meta",
        UNITY / "SoulWisp" / "WispCombatIntermissionV19.cs.meta",
        UNITY / "Presentation" / "OpenSourceMeshPrimitivesV19.cs.meta",
        UNITY / "Presentation" / "FracturedSignalCharacterV19.cs.meta",
    )
    guids = []
    for path in paths:
        text = path.read_text(encoding="utf-8")
        assert "fileFormatVersion: 2" in text
        line = next(line for line in text.splitlines() if line.startswith("guid: "))
        guid = line.split(":", 1)[1].strip()
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
