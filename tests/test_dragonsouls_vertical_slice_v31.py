from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OVERLAY = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge"
CAMERA = OVERLAY / "Runtime" / "MindforgeProductionCameraV31.cs"
FORMATION = OVERLAY / "Runtime" / "MindforgeEnemyFormationV31.cs"
IDENTITY = OVERLAY / "Runtime" / "MindforgeEnemyIdentityV31.cs"
BOSS = OVERLAY / "Runtime" / "MindforgeBossEncounterPresentationV31.cs"
FEEDBACK = OVERLAY / "Runtime" / "MindforgeCombatFeedbackV31.cs"
HUD = OVERLAY / "Runtime" / "MindforgeHudPresentationV31.cs"
RUNTIME = OVERLAY / "Runtime" / "MindforgeVerticalSliceRuntimeV31.cs"
BUILDER = OVERLAY / "Editor" / "MindforgeVerticalSliceBuilderV31.cs"
READINESS = OVERLAY / "Editor" / "MindforgeVerticalSliceReadinessV31.cs"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.31 source: {path}"
    return path.read_text(encoding="utf-8")


def test_v31_camera_retunes_existing_cinemachine_authority_instead_of_creating_competing_camera():
    text = read(CAMERA)
    for token in (
        "using States;",
        "PlayerStateMachine",
        "cameraController",
        "_cinemachineFreeLookCam",
        "_cinemachineTargetCam",
        "_cinemachineAimCam",
        "m_Orbits[0]",
        "m_Orbits[1]",
        "m_Orbits[2]",
        "m_ScreenX = 0.44f",
        "m_CameraRadius",
        "crowdedEnemyThreshold = 3",
        "bossTargetFov = 57f",
    ):
        assert token in text

    for forbidden in (
        "new GameObject",
        "AddComponent<Camera>",
        "Camera.main.transform.position",
        "PlayerStateMachine.Instance.transform.position =",
        "Time.timeScale",
    ):
        assert forbidden not in text


def test_v31_enemy_spacing_steers_navmesh_destinations_without_owning_character_motion_or_attacks():
    text = read(FORMATION)
    for token in (
        "using States;",
        "NavMeshAgent",
        "HighQualityObstacleAvoidance",
        "avoidancePriority",
        "meleeRingRadius = 3.45f",
        "rangedRingRadius = 6.25f",
        "casterRingRadius = 7.10f",
        "NavMesh.SamplePosition",
        "_agent.destination = hit.position",
        "closeCombatReleaseRadius = 2.45f",
    ):
        assert token in text

    for forbidden in (
        "CharacterController.Move",
        "movementController.Move",
        "transform.position =",
        "transform.rotation =",
        "TakeDamage(",
        "ReceiveDamage(",
        "ChangeState(",
        "attackState",
        "Time.timeScale",
    ):
        assert forbidden not in text


def test_v31_enemy_identity_desaturates_inherited_rigs_and_preserves_gameplay_authority():
    text = read(IDENTITY)
    for token in (
        "DefaultExecutionOrder(790)",
        "MaterialPropertyBlock",
        "GetComponentsInChildren<Renderer>(true)",
        "GetPropertyBlock(block, m)",
        "SetPropertyBlock(block, m)",
        "desaturation = 0.76f",
        "identityBlend = 0.62f",
        "SignalCaster",
        "SignalRanger",
        "CathedralBrute",
        "CorruptedBeast",
        "BoneRemnant",
        "_EmissionColor",
        "ShadowCastingMode.On",
    ):
        assert token in text

    for forbidden in (
        "EnemyStateMachine",
        "NavMeshAgent",
        "CharacterController",
        "TakeDamage(",
        "ReceiveDamage(",
        "ChangeState(",
        "transform.position =",
        "sharedMaterial =",
        "sharedMaterials =",
    ):
        assert forbidden not in text


def test_v31_boss_presentation_is_health_phase_and_authored_particle_driven_only():
    text = read(BOSS)
    for token in (
        "using Combat;",
        "OnHealthUpdated += HandleHealthUpdated",
        "OnDead += HandleDead",
        "Phase = _healthFraction > 0.66f ? 1 : _healthFraction > 0.34f ? 2 : 3",
        "GetComponentsInChildren<ParticleSystem>(true)",
        "new ParticleSystem.MinMaxGradient(corruptionColor, neuralColor)",
        "AuthoredAttackActivity()",
        "Mindforge_Boss_SignalCore_V31",
        "MaterialPropertyBlock",
        "_EmissionColor",
    ):
        assert token in text

    for forbidden in (
        "SpawnFireball(",
        "ThrowFireProjectile(",
        "ThrowFireWall(",
        "TakeDamage(",
        "ChangeState(",
        "AddForce(",
        "NavMeshAgent",
        "Time.timeScale",
    ):
        assert forbidden not in text


def test_v31_hit_feedback_is_downstream_of_health_events_and_never_deals_damage():
    text = read(FEEDBACK)
    for token in (
        "using Combat;",
        "GetComponentInParent<Health>()",
        "OnHealthUpdated += HandleHealthUpdated",
        "OnDead += HandleDead",
        "MaterialPropertyBlock",
        "ParticleSystem",
        "EnterHitPosition",
        "Mindforge_HitSparks_V31",
        "RestoreRendererBlocks",
    ):
        assert token in text

    for forbidden in (
        "TakeDamage(",
        "IncreaseHealth(",
        "ResetHealth(",
        "AddForce(",
        "MovePosition(",
        "Time.timeScale",
    ):
        assert forbidden not in text


def test_v31_hud_only_styles_inherited_widgets_and_never_writes_gameplay_values():
    text = read(HUD)
    for token in (
        "FindObjectsOfType<Slider>(true)",
        "slider.fillRect",
        "fillImage.color",
        "FindObjectsOfType<TextMeshProUGUI>(true)",
        "healthColor",
        "staminaColor",
        "bossColor",
    ):
        assert token in text

    for forbidden in (
        "slider.value =",
        "slider.maxValue =",
        "TakeDamage(",
        "IncreaseHealth(",
        "Health.OnHealthUpdated",
        "Stamina",
    ):
        assert forbidden not in text


def test_v31_runtime_culls_grass_and_installs_camera_spacing_identity_boss_feedback_hud_and_postfx():
    text = read(RUNTIME)
    for token in (
        'ProductVersion = "V0.31 Production Vertical Slice"',
        "MindforgeProductionCameraV31",
        "MindforgeEnemyFormationV31",
        "MindforgeEnemyIdentityV31",
        "MindforgeBossEncounterPresentationV31",
        "MindforgeCombatFeedbackV31",
        "MindforgeHudPresentationV31",
        "terrain.detailObjectDensity",
        "terrain.detailObjectDistance",
        "terrain.treeDistance",
        "FogMode.ExponentialSquared",
        "WhiteBalance",
        "TonemappingMode.ACES",
    ):
        assert token in text

    for forbidden in (
        "TakeDamage(",
        "ReceiveDamage(",
        "CharacterController.Move",
        "NavMeshAgent.destination",
        "transform.position =",
    ):
        assert forbidden not in text


def test_v31_builder_creates_new_scene_from_working_v30_world_and_never_edits_baseline_in_place():
    text = read(BUILDER)
    for token in (
        "SourceScene = MindforgeProductionWorldBuilderV30.DestinationScene",
        'DestinationScene = "Assets/Mindforge/Scenes/MindforgeVerticalSliceV31.unity"',
        "MindforgeProductionWorldBuilderV30.Build(refresh: refresh)",
        "AssetDatabase.CopyAsset(SourceScene, DestinationScene)",
        "MindforgeVerticalSliceRuntimeV31",
        "RebuildAuthoredRoute()",
        "ValidateInheritedGame()",
    ):
        assert token in text


def test_v31_route_uses_authored_grounded_boundaries_with_definite_collision_and_large_clear_lane():
    text = read(BUILDER)
    for token in (
        "ProtectedHalfWidth = 7f",
        "BossExclusionRadius = 20f",
        "MaximumAddedSolidModules = 12",
        "Metal_Wall_With_Pillars.prefab",
        "Rock_Wall.prefab",
        "PrefabUtility.InstantiatePrefab",
        "NavMesh.CalculatePath",
        "StationFractions",
        "GroundInstance",
        "CalculateBounds",
        "ValidateBoundaryClearance",
        "HasRealBoundaryCollider",
        "mesh.sharedMesh != null",
        "innerEdgeDistance < ProtectedHalfWidth",
    ):
        assert token in text

    for forbidden in (
        "GameObject.CreatePrimitive",
        "PrimitiveType.",
        "Random.Range",
        "Random.value",
        "AddComponent<BoxCollider>",
        "AddComponent<MeshCollider>",
        "NavMeshBuilder",
        "BuildNavMesh",
    ):
        assert forbidden not in text


def test_v31_builder_preserves_working_player_sword_camera_enemy_bonfire_and_boss_authority():
    text = read(BUILDER)
    for token in (
        "PlayerStateMachine",
        "Sword",
        "CinemachineVirtualCamera",
        "EnemyStateMachine",
        "Bonfire",
        "BonfiresManager",
        "EnemyNightmareDragonController",
    ):
        assert token in text


def test_v31_native_readiness_tracks_geometry_and_runtime_presentation_owners():
    text = read(READINESS)
    for token in (
        'schema = "mindforge.dragonsouls_vertical_slice_readiness.v31"',
        '"v31_scene"',
        '"authored_boundaries_have_real_collision"',
        '"authored_boundary_budget"',
        '"baked_navmesh_runtime"',
        '"runtime_installed"',
        '"production_camera_runtime"',
        '"enemy_formation_runtime"',
        '"enemy_identity_runtime"',
        '"boss_presentation_runtime"',
        '"combat_feedback_runtime"',
        '"hud_presentation_runtime"',
        "NavMesh.CalculateTriangulation()",
    ):
        assert token in text
