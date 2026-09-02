from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
BOOTSTRAP = ROOT / "tools" / "bootstrap_dragonsouls_chassis.sh"
OVERLAY_TOOL = ROOT / "tools" / "apply_dragonsouls_overlay.py"
OVERLAY = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge"
DOC = ROOT / "docs" / "DRAGONSOULS_CHASSIS_V29.md"
GITIGNORE = ROOT / ".gitignore"
LICENSE = ROOT / "third_party" / "licenses" / "DragonSouls_Unity3D_MIT.txt"
AETHERBLADE = OVERLAY / "Runtime" / "MindforgeAetherbladePresentationV29.cs"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.29 chassis source: {path}"
    return path.read_text(encoding="utf-8")


def test_v29_bootstrap_pins_exact_upstream_and_known_good_unity():
    source = read(BOOTSTRAP)
    assert 'UPSTREAM_URL="https://github.com/btuhany/DragonSouls-Unity3D.git"' in source
    assert 'UPSTREAM_COMMIT="f54824255517801d5d3443848e1e4275d8d5066d"' in source
    assert 'EXPECTED_UNITY="2021.3.20f1"' in source
    assert 'PROJECT_ROOT="${CHECKOUT_ROOT}/ThirdPersonCombat"' in source
    assert 'git clone --filter=blob:none --no-checkout' in source
    assert 'git -C "${CHECKOUT_ROOT}" checkout --detach "${UPSTREAM_COMMIT}"' in source
    assert 'grep -q "MIT License"' in source
    assert 'actual_unity' in source


def test_v29_external_chassis_is_local_and_never_committed_as_bulk_art():
    ignore = read(GITIGNORE)
    assert "external/DragonSouls-Unity3D/" in ignore
    source = read(BOOTSTRAP)
    assert 'CHECKOUT_ROOT="${REPO_ROOT}/external/DragonSouls-Unity3D"' in source
    assert "cp -R" not in source
    assert "git add" not in source
    assert "git commit" not in source


def test_v29_overlay_is_bounded_to_assets_mindforge():
    source = read(OVERLAY_TOOL)
    assert 'OVERLAY_ROOT = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge"' in source
    assert 'target = project / "Assets" / "Mindforge"' in source
    assert '"overlay_scope": "Assets/Mindforge"' in source
    # The tool may read ProjectSettings to validate the pinned Unity version, but
    # no tracked overlay write/copy target may escape Assets/Mindforge.
    assert 'target = project / "Packages"' not in source
    assert 'target = project / "ProjectSettings"' not in source
    assert 'shutil.copytree(OVERLAY_ROOT, project / "Packages"' not in source
    assert 'shutil.copytree(OVERLAY_ROOT, project / "ProjectSettings"' not in source
    assert "shutil.copytree(OVERLAY_ROOT, target)" in source


def test_v29_unity_overlay_has_fast_play_entry_and_neural_seam_without_combat_authority():
    menu = read(OVERLAY / "Editor" / "MindforgeChassisMenu.cs")
    intent = read(OVERLAY / "Runtime" / "MindforgeIntentBusV29.cs")
    provenance = read(OVERLAY / "Provenance" / "UPSTREAM.txt")

    for token in (
        'MenuItem("Mindforge/Chassis/PLAY MAIN GAME"',
        'MainGameScene = "Assets/Levels/Scenes/MainGameScene.unity"',
        'MainMenuScene = "Assets/Levels/Scenes/MainMenuScene.unity"',
        'GameplayTestScene = "Assets/Levels/Scenes/GameplayTestScene.unity"',
        "m_EditorVersion: 2021.3.20f1",
    ):
        assert token in menu

    for token in ("Sight", "Guard", "Concord", "IntentPublished", "controller_simulation"):
        assert token in intent
    for forbidden in ("ReceiveDamage", "MovePosition", "MoveRotation", "fieldOfView", "Animator.Play"):
        assert forbidden not in intent

    assert "f54824255517801d5d3443848e1e4275d8d5066d" in provenance
    assert "separately audited or replaced" in provenance
    assert "known permissive/public-domain production art" in provenance


def test_v29_aetherblade_replaces_only_visible_sword_mesh_and_preserves_upstream_combat():
    source = read(AETHERBLADE)
    for token in (
        'UpstreamMeshName = "Sword1_1_3"',
        'PresentationRootName = "Mindforge_Aetherblade_V29"',
        "_retiredUpstreamRenderer.enabled = false",
        "GetComponentInChildren<TrailRenderer>(true)",
        'go.name != "Sword"',
        "go.AddComponent<MindforgeAetherbladePresentationV29>()",
        'Shader.Find("Universal Render Pipeline/Unlit")',
        '"Blade_Core"',
        '"Blade_Glow"',
        '"Hilt"',
        '"Aetherblade_LocalLight"',
    ):
        assert token in source

    # The presentation layer may remove colliders from primitives it creates, but
    # must never create or mutate the authoritative sword hit/Rigidbody machinery.
    for forbidden in (
        "AddComponent<Rigidbody>",
        "AddComponent<BoxCollider>",
        "AddComponent<CapsuleCollider>",
        "AddComponent<SphereCollider>",
        "MovePosition(",
        "MoveRotation(",
        "AddForce(",
        "ReceiveDamage(",
        "AttackDamage",
        "HitControlPosition",
    ):
        assert forbidden not in source


def test_v29_documentation_commits_to_chassis_first_world_and_spacing_rules():
    doc = read(DOC)
    for phrase in (
        "complete third-person action-game chassis",
        "primary combat hall clear width: **>= 14 m**",
        "ordinary traversal corridor clear width: **>= 8 m**",
        "boss arena clear diameter: **>= 32 m**",
        "every visually solid wall/floor/column",
        "do not merge the two Unity projects asset-by-asset",
        "one polished, readable combat slice",
        "behavior-tree",
        "Aetherblade",
        "Sight / Guard / Concord",
    ):
        assert phrase in doc


def test_v29_retains_upstream_mit_notice_verbatim_enough_for_redistribution():
    notice = read(LICENSE)
    assert notice.startswith("MIT License")
    assert "Copyright (c) 2023 btuhany" in notice
    assert "Permission is hereby granted, free of charge" in notice
    assert "THE SOFTWARE IS PROVIDED \"AS IS\"" in notice
