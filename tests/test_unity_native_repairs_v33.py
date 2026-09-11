from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
EDITOR = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Editor"
TMP_REPAIR = EDITOR / "MindforgeTmpEssentialResourcesV33.cs"
V29_BUILDER = EDITOR / "MindforgeCombatSliceBuilderV29.cs"
V29_AUDIT = EDITOR / "MindforgeChassisReadinessV29.cs"
V30_BUILDER = EDITOR / "MindforgeProductionWorldBuilderV30.cs"


def read(path: Path) -> str:
    assert path.exists(), f"missing native repair source: {path}"
    return path.read_text(encoding="utf-8")


def test_tmp_essential_resources_are_repaired_before_native_play():
    repair = read(TMP_REPAIR)

    for token in (
        "[InitializeOnLoad]",
        'private const string TmpSettingsResource = "TMP Settings";',
        "Resources.Load<TMP_Settings>(TmpSettingsResource)",
        "TMP_PackageUtilities.ImportProjectResourcesMenu();",
        "SessionState.GetBool(SessionAttemptKey, false)",
        'MenuItem("Mindforge/Chassis/Repair TextMesh Pro Essential Resources"',
        "Do not qualify native gameplay until",
    ):
        assert token in repair


def test_v29_gameplay_sandbox_uses_upstream_boss_scope_instead_of_false_manager_requirement():
    builder = read(V29_BUILDER)

    assert 'BossManagerPrefabPath = "Assets/Levels/Prefabs/Core/BossManager.prefab"' in builder
    assert "AssetDatabase.LoadAssetAtPath<GameObject>(BossManagerPrefabPath)" in builder
    assert "EnemyNightmareDragonController dragon" in builder
    assert "if (dragon == null)" in builder
    assert "if (bossManagerPrefab == null)" in builder
    assert "GameplayTestScene intentionally carries no BossManager scene instance" in builder
    assert "if (boss == null || dragon == null)" not in builder


def test_v29_native_audit_accepts_the_owned_slice_and_scopes_boss_manager_by_scene():
    audit = read(V29_AUDIT)

    assert "MindforgeCombatSliceBuilderV29.DestinationScene" in audit
    assert "bool fullWorldBossAuthority = report.scene == MindforgeChassisMenu.MainGameScene;" in audit
    assert "fullWorldBossAuthority ? bosses.Length == 1 : bosses.Length <= 1" in audit
    assert "sandbox scope; scene manager not required" in audit


def test_v30_full_world_still_fail_closes_on_boss_manager_and_dragon():
    builder = read(V30_BUILDER)

    assert "BossManager[] bosses" in builder
    assert "EnemyNightmareDragonController[] dragons" in builder
    assert "if (bosses.Length == 0 || dragons.Length == 0)" in builder
    assert 'throw new UnityEditor.Build.BuildFailedException("V0.30 full world lost the dragon boss pipeline.");' in builder
