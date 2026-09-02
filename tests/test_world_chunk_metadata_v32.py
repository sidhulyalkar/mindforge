from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = (
    ROOT
    / "dragonsouls_overlay"
    / "Assets"
    / "Mindforge"
    / "Runtime"
    / "MindforgeChunkMetadataV32.cs"
)


def test_v32_chunk_metadata_has_stable_ids_semantic_sockets_and_clearance_validation():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "MindforgeWorldSocketV32",
        "MindforgeChunkDescriptorV32",
        "stableId",
        "MindforgeSocketKindV32",
        "compatibilityTag",
        "clearanceRadius",
        "MindforgeRegionIdV32",
        "MindforgeChunkKindV32",
        "supportsCombat",
        "supportsPersistence",
        "MinimumGeneralCorridorWidth",
        "MinimumCombatHallWidth",
        "GetComponentsInChildren<MindforgeWorldSocketV32>(true)",
        "HashSet<string>",
        "duplicate socket stableId",
    ):
        assert token in text

    for forbidden in (
        "Guid.NewGuid",
        "Random.Range",
        "Random.value",
        "GetInstanceID",
        "TakeDamage(",
        "ChangeState(",
        "NavMeshAgent",
    ):
        assert forbidden not in text
