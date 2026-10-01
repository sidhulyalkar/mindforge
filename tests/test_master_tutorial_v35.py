from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Runtime"
EDITOR = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Editor"
DOCS = ROOT / "docs"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.35 source: {path}"
    return path.read_text(encoding="utf-8")


def test_v35_master_tutorial_observes_real_gameplay_authority():
    text = read(RUNTIME / "MindforgeCombatTutorialV35.cs")

    for token in (
        "MovementOn2DAxis",
        "CameraMovementOn2DAxis",
        "SwingWindowsObserved",
        "HitsObserved",
        "CurrentTargetTransform",
        "IsSwordReturned",
        "IsSwordInSheath",
        "OnHealthIncreased",
        "OnTakeRestEvent",
        "MindforgeAdaptiveBciTutorialV34",
        "MindforgeSightReceptorV33",
        "MindforgeGuardReceptorV33",
        "BossManager.Instance.IsInBoss",
        "OnBossDefeated",
        'schema = "mindforge.combat_tutorial_receipt.v1"',
    ):
        assert token in text

    for forbidden in (
        "StartAttack(",
        "StopAttack(",
        ".TakeDamage(",
        ".IncreaseHealth(",
        "CharacterController.Move",
        "transform.position =",
        "MindforgeIntentBusV29.Publish",
        "InjectControllerSimulation(",
    ):
        assert forbidden not in text


def test_v35_tutorial_covers_complete_combat_and_gameplay_vocabulary_in_order():
    text = read(RUNTIME / "MindforgeCombatTutorialV35.cs")
    ordered = (
        "Movement = 1",
        "Camera = 2",
        "Sprint = 3",
        "LightCombo = 4",
        "HeavyAttack = 5",
        "DamageContact = 6",
        "TargetLock = 7",
        "TargetSwitch = 8",
        "DodgeRoll = 9",
        "AimThrow = 10",
        "Recall = 11",
        "Sheath = 12",
        "Heal = 13",
        "BonfireRest = 14",
        "NeuralAttunement = 15",
        "SightFieldUse = 16",
        "GuardFieldUse = 17",
        "BossEntry = 18",
        "BossDefeat = 19",
        "Complete = 20",
    )
    positions = [text.index(token) for token in ordered]
    assert positions == sorted(positions)


def test_v35_target_switch_requires_actual_target_change_not_just_input():
    text = read(RUNTIME / "MindforgeCombatTutorialV35.cs")
    section = text.split("private void UpdateTargetSwitch()", 1)[1].split(
        "private void UpdateAimThrow()", 1
    )[0]
    assert "_targetSelectInputs > _baselineTargetSelectInputs" in section
    assert "current != _targetAtStageEntry" in section


def test_v35_melee_gates_require_real_sword_windows_and_contacts():
    text = read(RUNTIME / "MindforgeCombatTutorialV35.cs")
    assert "_swordAssurance.SwingWindowsObserved - _baselineSwingWindows >= requiredLightSwingWindows" in text
    assert "_lightInputs - _baselineLightInputs >= requiredLightSwingWindows" in text
    assert "_heavyInputs > _baselineHeavyInputs" in text
    assert "_swordAssurance.SwingWindowsObserved > _baselineSwingWindows" in text
    assert "_swordAssurance.HitsObserved > _baselineHits" in text


def test_v35_builder_derives_from_v34_and_preserves_single_authorities():
    text = read(EDITOR / "MindforgeMasterTutorialBuilderV35.cs")
    for token in (
        "MindforgeAdaptiveBciBuilderV34.DestinationScene",
        "MindforgeAdaptiveBciBuilderV34.Build(refresh: refresh)",
        "root.AddComponent<MindforgeMasterTutorialRuntimeV35>()",
        "FindObjectsOfType<PlayerStateMachine>(true).Length != 1",
        "FindObjectsOfType<Sword>(true).Length != 1",
        "EnemyNightmareDragonController",
        "MindforgeVerticalSliceRuntimeV31",
    ):
        assert token in text
    assert "root.GetComponentsInChildren<Collider>(true).Length != 0" in text
    assert "root.GetComponentsInChildren<Rigidbody>(true).Length != 0" in text


def test_v35_runtime_requires_v33_v34_and_adds_only_tutorial_orchestration():
    text = read(RUNTIME / "MindforgeMasterTutorialRuntimeV35.cs")
    assert "MindforgeBciIntegrationRuntimeV33" in text
    assert "MindforgeAdaptiveBciRuntimeV34" in text
    assert "AddComponent<MindforgeCombatTutorialV35>()" in text
    for forbidden in ("StartAttack", "TakeDamage", "ChangeState", "CharacterController.Move"):
        assert forbidden not in text


def test_v35_desktop_target_bindings_use_inherited_actions():
    text = read(RUNTIME / "MindforgeDesktopCombatBindingsV31.cs")
    assert 'AddBinding(player.Target, "<Keyboard>/t")' in text
    assert 'AddBinding(player.TargetSelect, "<Mouse>/scroll")' in text
    assert 'HasBinding(player.TargetSelect, "<Mouse>/scroll")' in text


def test_v35_scope_document_pins_authority_and_promotion_ladder():
    text = read(DOCS / "MASTER_TUTORIAL_V35.md")
    for token in (
        "V0.35 authority contract",
        "Exact tutorial progression",
        "V0.35A implementation scope",
        "V0.35B encounter-showcase scope",
        "V0.35C presentation scope",
        "Qualification ladder",
        "Definition of done",
        "one canonical tutorial scene",
    ):
        assert token in text


def test_v35_character_camera_centers_and_pulls_back_for_motion_readability():
    text = read(RUNTIME / "MindforgeCharacterReadabilityCameraV35.cs")
    for token in (
        "freeLookFov = 52f",
        "middleRadius = 4.75f",
        "freeLookScreenX = 0.50f",
        "freeLookScreenY = 0.56f",
        "targetFov = 56f",
        "crowdedTargetFov = 59f",
        "bossTargetFov = 62f",
        "targetScreenX = 0.50f",
        "targetScreenY = 0.54f",
        "TryMeasureCharacterViewport",
        "CharacterInsideSafeFrame",
        "maximumFramingAssistFov = 4.0f",
    ):
        assert token in text


def test_v35_character_camera_preserves_gameplay_authority_and_aim_bonfire_framing():
    text = read(RUNTIME / "MindforgeCharacterReadabilityCameraV35.cs")
    assert 'return "aim"' in text
    assert 'return "bonfire"' in text
    assert "Aim and bonfire framing are deliberately left alone." in text
    for forbidden in (
        "ChangeState(",
        "CharacterController.Move",
        "transform.position =",
        "TakeDamage(",
        "StartAttack(",
        "StopAttack(",
        "MindforgeIntentBusV29.Publish",
    ):
        assert forbidden not in text


def test_v35_camera_manual_input_overrides_sprint_recentering():
    text = read(RUNTIME / "MindforgeCharacterReadabilityCameraV35.cs")
    section = text.split("private void UpdateSprintRecentering()", 1)[1].split(
        "private string ResolveActiveMode()", 1
    )[0]
    assert "manualRecently" in section
    assert "sprinting" in section
    assert "moving" in section
    assert 'ActiveMode, "free"' in section
    assert "m_RecenterToTargetHeading.m_enabled = allowed" in section


def test_v35_camera_collision_and_persistent_framing_are_observable():
    text = read(RUNTIME / "MindforgeCharacterReadabilityCameraV35.cs")
    for token in (
        "cameraCollisionRadius = 0.32f",
        "minimumCameraTargetDistance = 0.55f",
        "occlusionSmoothingSeconds = 0.10f",
        "FramingViolationFrames",
        "PersistentFramingWarnings",
        "Persistent character framing pressure",
    ):
        assert token in text


def test_v35_runtime_installs_character_readability_camera_before_tutorial():
    text = read(RUNTIME / "MindforgeMasterTutorialRuntimeV35.cs")
    camera_pos = text.index("AddComponent<MindforgeCharacterReadabilityCameraV35>()")
    tutorial_pos = text.index("AddComponent<MindforgeCombatTutorialV35>()")
    assert camera_pos < tutorial_pos


def test_v35_tutorial_receipt_records_camera_readability_evidence():
    text = read(RUNTIME / "MindforgeCombatTutorialV35.cs")
    for token in (
        "camera_readability_installed",
        "camera_readability_pass",
        "camera_framing_violation_frames",
        "camera_persistent_warnings",
        "camera_last_viewport_height",
        "MindforgeCharacterReadabilityCameraV35",
    ):
        assert token in text
