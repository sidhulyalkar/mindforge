from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
AUDIT = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Editor" / "MindforgeWorldGeometryAuditV30.cs"


def source() -> str:
    assert AUDIT.exists(), f"missing geometry audit: {AUDIT}"
    return AUDIT.read_text(encoding="utf-8")


def test_v30_geometry_audit_uses_baked_navmesh_and_measured_collision_clearance():
    text = source()
    for token in (
        'schema = "mindforge.world_geometry_audit.v30"',
        "OrdinaryCorridorTarget = 8f",
        "BossArenaRadiusTarget = 16f",
        "MaxProbeDistance = 20f",
        "SampleSpacing = 2f",
        "NavMesh.CalculateTriangulation()",
        "NavMesh.CalculatePath",
        "NavMeshPathStatus.PathComplete",
        "Physics.RaycastAll",
        "minimumPathClearWidth",
        "minimumBossClearRadius",
        "chokeSamples",
        "largeInvisibleColliderCandidates",
    ):
        assert token in text


def test_v30_geometry_audit_ignores_actor_colliders_and_never_modifies_world():
    text = source()
    for token in (
        "GetComponentInParent<PlayerStateMachine>()",
        "GetComponentInParent<EnemyStateMachine>()",
        "MindforgeProductionWorldBuilderV30.MarkerRoot",
        "QueryTriggerInteraction.Ignore",
    ):
        assert token in text

    for forbidden in (
        "transform.position =",
        "transform.localPosition =",
        "transform.rotation =",
        "transform.localRotation =",
        "Object.Destroy",
        "DestroyImmediate",
        "GameObject.CreatePrimitive",
        "AddComponent<Collider>",
        "AddComponent<Rigidbody>",
        "NavMeshBuilder",
        "BuildNavMesh",
        "MovePosition(",
        "MoveRotation(",
    ):
        assert forbidden not in text


def test_v30_geometry_probe_can_actually_observe_passing_boss_radius():
    text = source()
    assert "BossArenaRadiusTarget = 16f" in text
    assert "MaxProbeDistance = 20f" in text
    assert "minimumBossClearRadius = MaxProbeDistance" in text
