from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OVERLAY = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge"
BUILDER = OVERLAY / "Editor" / "MindforgeProductionWorldBuilderV30.cs"
READINESS = OVERLAY / "Editor" / "MindforgeWorldReadinessV30.cs"
WORLD = OVERLAY / "Runtime" / "MindforgeWorldPresentationV30.cs"
ENEMY = OVERLAY / "Runtime" / "MindforgeEnemyPresentationV30.cs"
DOC = ROOT / "docs" / "PRODUCTION_WORLD_V30.md"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.30 source: {path}"
    return path.read_text(encoding="utf-8")


def test_v30_promotes_complete_main_game_scene_into_mindforge_owned_world():
    source = read(BUILDER)
    for token in (
        "SourceScene = MindforgeChassisMenu.MainGameScene",
        'DestinationScene = "Assets/Mindforge/Scenes/MindforgeWorldV30.unity"',
        'MarkerRoot = "Mindforge_Production_World_V30"',
        "AssetDatabase.CopyAsset(SourceScene, DestinationScene)",
        "MindforgeWorldPresentationV30",
        "ValidateInheritedWorldSystems()",
        "ValidateMindforgeRootIsPresentationOnly(root)",
        "PlayerStateMachine",
        "Sword",
        "CinemachineVirtualCamera",
        "EnemyStateMachine",
        "BossManager",
        "EnemyNightmareDragonController",
        "Bonfire",
        "BonfiresManager",
    ):
        assert token in source


def test_v30_builder_adds_presentation_only_root_without_procedural_gameplay_geometry():
    source = read(BUILDER)
    for forbidden in (
        "PrimitiveType.",
        "GameObject.CreatePrimitive",
        "AddComponent<BoxCollider>",
        "AddComponent<CapsuleCollider>",
        "AddComponent<SphereCollider>",
        "AddComponent<Rigidbody>",
        "AddComponent<CharacterController>",
        "NavMeshAgent",
        "ReceiveDamage(",
        "AttackDamage",
    ):
        assert forbidden not in source

    for token in (
        "root.GetComponentsInChildren<Collider>(true).Length",
        "root.GetComponentsInChildren<Rigidbody>(true).Length",
        "root.GetComponentsInChildren<CharacterController>(true).Length",
        "CreatePointLight",
        "LightShadows.None",
    ):
        assert token in source


def test_v30_world_presentation_preserves_material_assets_and_gameplay_authority():
    source = read(WORLD)
    for token in (
        "MaterialPropertyBlock",
        "Object.FindObjectsOfType<MeshRenderer>(true)",
        'material.HasProperty("_BaseColor")',
        "renderer.SetPropertyBlock(block)",
        "VolumeProfile",
        "Bloom",
        "ColorAdjustments",
        "TonemappingMode.ACES",
        "Vignette",
        "EnemyStateMachine",
        "MindforgeEnemyPresentationV30",
    ):
        assert token in source

    for forbidden in (
        "Update()",
        "FixedUpdate()",
        "MovePosition(",
        "MoveRotation(",
        "AddForce(",
        "NavMeshAgent",
        "ReceiveDamage(",
        "AttackDamage",
        "sharedMaterial =",
        "sharedMaterials =",
        "transform.position =",
        "transform.localPosition =",
    ):
        assert forbidden not in source


def test_v30_enemy_identity_is_visual_only_and_keeps_enemy_state_machine_authority():
    source = read(ENEMY)
    assert "SkinnedMeshRenderer" in source
    assert "MaterialPropertyBlock" in source
    assert "ArchetypeColor" in source
    assert "renderer.SetPropertyBlock(block)" in source
    for forbidden in (
        "EnemyStateMachine",
        "Update()",
        "FixedUpdate()",
        "MovePosition(",
        "MoveRotation(",
        "NavMeshAgent",
        "ReceiveDamage(",
        "AttackDamage",
        "health",
        "stamina",
    ):
        assert forbidden not in source


def test_v30_readiness_observes_full_world_navigation_progression_combat_and_presentation():
    source = read(READINESS)
    for token in (
        'schema = "mindforge.dragonsouls_world_readiness.v30"',
        '"pinned_unity_2021_3_20f1"',
        '"mindforge_world_scene"',
        '"presentation_root_no_colliders"',
        '"presentation_root_no_rigidbodies"',
        '"single_player"',
        '"single_authoritative_sword"',
        '"standard_enemy_population"',
        '"boss_pipeline"',
        '"bonfire_progression"',
        "Bonfire[]",
        "BonfiresManager[]",
        '"baked_navmesh_runtime"',
        "NavMesh.CalculateTriangulation()",
        '"presentation_installed_runtime"',
        '"enemy_identity_runtime"',
    ):
        assert token in source


def test_v30_documentation_commits_to_complete_world_first_and_measured_regional_rebuilds():
    doc = read(DOC)
    for phrase in (
        "complete MainGameScene",
        "baked NavMesh",
        "source scene is never edited in place",
        "primary combat hall clear width >= 14 m",
        "ordinary traversal corridor clear width >= 8 m",
        "boss arena clear diameter >= 32 m",
        "does not claim the inherited Dragon Souls level already satisfies",
        "regional rather than global",
        "rebake NavMesh",
        "PLAY PRODUCTION WORLD",
    ):
        assert phrase in doc
