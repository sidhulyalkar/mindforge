from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
EXPORTER = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Editor" / "MindforgeWorldQualificationExporterV30.cs"


def source() -> str:
    assert EXPORTER.exists(), f"missing qualification exporter: {EXPORTER}"
    return EXPORTER.read_text(encoding="utf-8")


def test_v30_qualification_export_combines_readiness_and_measured_geometry():
    text = source()
    for token in (
        'schema = "mindforge.world_qualification.v30"',
        "MindforgeWorldReadinessV30.AuditActiveScene()",
        "MindforgeWorldGeometryAuditV30.AuditActiveScene()",
        "readinessPassedChecks",
        "readinessFailedChecks",
        "readinessDeferredChecks",
        "minimumPathClearWidth",
        "narrowestPathPosition",
        "minimumBossClearRadius",
        "minimumBossClearAngleDegrees",
        "largeInvisibleColliderCandidates",
        "JsonUtility.ToJson(report, true)",
    ):
        assert token in text


def test_v30_qualification_export_is_local_report_io_not_gameplay_authority():
    text = source()
    assert 'Path.Combine(projectRoot, "MindforgeReports")' in text
    assert '"v30-world-qualification-{stamp}.json"' in text
    assert "File.WriteAllText(filePath" in text
    assert "EditorUtility.RevealInFinder(path)" in text

    for forbidden in (
        'Path.Combine(Application.dataPath, "MindforgeReports")',
        "GameObject.CreatePrimitive",
        "AddComponent<",
        "MovePosition(",
        "MoveRotation(",
        "ReceiveDamage(",
        "NavMeshBuilder",
        "BuildNavMesh",
        "AssetDatabase.CreateAsset",
        "AssetDatabase.CopyAsset",
    ):
        assert forbidden not in text
