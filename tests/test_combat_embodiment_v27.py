from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"
EDITOR = UNITY / "Editor"
PRESENTATION = UNITY / "Presentation"
LATEST = EDITOR / "MindforgeLatestEditorMenu.cs"
READINESS = EDITOR / "MindforgeLatestReadinessAuditV17.cs"
BUILDER = EDITOR / "CombatEmbodimentV27Builder.cs"
GUARDIAN = PRESENTATION / "GuardianCombatEmbodimentV27.cs"
BEAST = PRESENTATION / "FracturedSignalBeastV27.cs"
ARENA = PRESENTATION / "FracturedArenaDynamicsV27.cs"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.27 source: {path}"
    return path.read_text(encoding="utf-8")


def test_v27_is_the_final_latest_stage_after_v26_world_rendering():
    latest = read(LATEST)
    assert 'ProductVersion = "V0.27 Guardian Embodiment + Fractured Beast"' in latest
    v26 = latest.index("WorldRenderingV26Builder.ApplyOpenScene();")
    v27 = latest.index("CombatEmbodimentV27Builder.ApplyOpenScene();", v26)
    assert v26 < v27
    assert "if (!CombatEmbodimentV27Builder.PresentInOpenScene())" in latest
    assert 'RootName = "Mindforge_Combat_Embodiment_V27"' in read(BUILDER)


def test_guardian_arm_follows_authoritative_sword_without_becoming_hit_authority():
    source = read(GUARDIAN)
    for token in (
        'RootName = "GuardianCombatEmbodimentV27"',
        'transform.Find("PhysicalArsenalRig/SwordRoot")',
        "GuardianSwordShieldController",
        "combat.IsAttacking",
        "combat.IsGuarding",
        "combat.ComboStep",
        "combat.AttackProgress",
        "ComputeWristTarget",
        "SolveArm",
        "SetSegment",
        "_swordRoot.position =",
        'HideLegacyPart("ArmR")',
        'HideLegacyPart("HandR")',
    ):
        assert token in source

    for forbidden in (
        "ReceiveDamage(",
        "ApplyDamage(",
        "RequestDash(",
        "RequestJump(",
        "SetMoveInput(",
        "Time.timeScale =",
        "Flux.Award(",
        "MovePosition(",
        "AddForce(",
        "NeuralEvent",
        "UdpNeuralReceiver",
    ):
        assert forbidden not in source


def test_guardian_arm_is_articulated_and_combat_pose_has_body_followthrough():
    source = read(GUARDIAN)
    for token in (
        '"RightPauldron"',
        '"RightUpperArm"',
        '"RightElbowGuard"',
        '"RightForearm"',
        '"RightGauntlet"',
        '"AetherWristBand"',
        "ApplyUpperBodyPose",
        "_torso",
        "_chest",
        "_helmet",
        "_leftArm",
        "Quadratic(",
        "UpperLength",
        "ForeLength",
    ):
        assert token in source


def test_fractured_signal_is_now_an_organic_beast_not_a_shard_cloud():
    source = read(BEAST)
    for token in (
        'RootName = "FracturedSignalBeastV27"',
        "BuildOrganicBodyMesh",
        '"ParasiteBody"',
        '"BellyMass"',
        '"BroadJowl"',
        '"MawCavity"',
        '"LowerJawRig"',
        '"SignalTongue"',
        '"SensoryEye_L"',
        '"SensoryEye_R"',
        '"LeftForelimb"',
        '"RightForelimb"',
        '"SignalCrystal_',
        "FracturedSignalCharacterV19.RootName",
        "headMass.localScale",
        "lowerJaw.localScale",
        "_crystalBaseScales",
    ):
        assert token in source

    for forbidden in (
        "MovePosition(",
        "AddForce(",
        "ReceiveDamage(",
        "SetExternalPause(",
        "TryApply(",
        "RequestDash(",
        "NeuralEvent",
        "UdpNeuralReceiver",
    ):
        assert forbidden not in source


def test_beast_animation_is_downstream_of_existing_boss_events():
    source = read(BEAST)
    for token in (
        "director.PhaseChanged += OnPhaseChanged",
        "director.AttackTelegraphed += OnTelegraphed",
        "director.AttackFired += OnAttackFired",
        "vitals.Damaged += OnDamaged",
        "movement.MovementActive",
        "_jaw.localRotation",
        "_head.localRotation",
        "ApplyEmission(_crystalRenderers",
    ):
        assert token in source


def test_v27_arena_is_visual_only_and_preserves_existing_fight_geometry():
    builder = read(BUILDER)
    for token in (
        'ArenaRootName = "V27_Fractured_Signal_Arena"',
        "ArenaCenterZ = 94f",
        "ArenaFloorY = 4.095f",
        '"V27_RiteFloor"',
        '"V27_CorruptionSpine_',
        '"V27_Beast_Altar_Frame"',
        '"V27_Beast_Altar_Arch"',
        '"V27_EncounterLights"',
        "AddComponent<FracturedArenaDynamicsV27>()",
        "GetComponentsInChildren<Collider>(true).Length != 0",
        "GetComponentsInChildren<Rigidbody>(true).Length != 0",
    ):
        assert token in builder

    for forbidden in (
        "AddComponent<BoxCollider>",
        "AddComponent<MeshCollider>",
        "AddComponent<Rigidbody>",
        "ReceiveDamage(",
        "MovePosition(",
        "SetExternalPause(",
    ):
        assert forbidden not in builder


def test_arena_dynamics_are_event_driven_presentation_only():
    source = read(ARENA)
    for token in (
        "director.PhaseChanged += OnPhaseChanged",
        "director.AttackTelegraphed += OnTelegraphed",
        "director.AttackFired += OnFired",
        'StartsWith("V27_CorruptionSpine_"',
        "light.intensity =",
        "renderer.SetPropertyBlock(_block)",
    ):
        assert token in source

    for forbidden in (
        "ReceiveDamage(",
        "ApplyDamage(",
        "MovePosition(",
        "AddForce(",
        "SetExternalPause(",
        "Time.timeScale =",
        "NeuralEvent",
        "UdpNeuralReceiver",
    ):
        assert forbidden not in source


def test_all_dynamic_v27_presentation_freezes_for_neural_visual_windows():
    for path in (GUARDIAN, BEAST, ARENA):
        source = read(path)
        assert "CalibrationStimuliActive" in source
        assert "ResonanceWindowActive" in source
        assert "NeuralVisualFieldActive" in source


def test_latest_readiness_tracks_v27_editor_and_runtime_owners():
    source = read(READINESS)
    for token in (
        '"product_version_v27"',
        'StartsWith("V0.27"',
        '"v27_combat_embodiment_authored"',
        "CombatEmbodimentV27Builder.PresentInOpenScene()",
        '"GuardianCombatEmbodimentV27"',
        '"FracturedSignalBeastV27"',
        '"FracturedArenaDynamicsV27"',
    ):
        assert token in source


def test_v27_scripts_have_pinned_unique_unity_guids():
    paths = (
        PRESENTATION / "GuardianCombatEmbodimentV27.cs.meta",
        PRESENTATION / "FracturedSignalBeastV27.cs.meta",
        PRESENTATION / "FracturedArenaDynamicsV27.cs.meta",
        EDITOR / "CombatEmbodimentV27Builder.cs.meta",
    )
    guids = []
    for path in paths:
        text = read(path)
        assert "fileFormatVersion: 2" in text
        guid = next(line.split(":", 1)[1].strip() for line in text.splitlines() if line.startswith("guid: "))
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
