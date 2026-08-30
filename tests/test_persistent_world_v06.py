from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_v05_single_context_router_and_priority_contract_survive_v06():
    router = read("Combat", "GuardianInteractionRouterV1.cs")
    base = read("World", "WorldInteractionV1.cs")
    content = read("World", "WorldInteractionContentV06.cs")
    shortcut = read("World", "WorldShortcutInteractionV06.cs")

    assert "controls.Pressed(GuardianControlAction.Interact)" in router
    assert "WorldInteractionSourceV1.FindBest" in router
    assert "public override int Priority => 30" in base
    assert "public override int Priority => 10" in base

    # Every V0.6 offer still enters the same registry and remains below Memory Forge priority.
    assert "PersistentWorldInteractionV06 : WorldInteractionSourceV1" in content
    assert "NpcDialogueInteractionV06 : WorldInteractionSourceV1" in content
    assert "public override int Priority => 28" in content
    assert "public override int Priority => 24" in content
    assert "public override int Priority => 22" in content
    assert "public override int Priority => 18" in content
    assert "public override int Priority => 27" in shortcut

    for source in (content, shortcut):
        assert "Input.GetKeyDown" not in source
        assert "KeyCode.E" not in source


def test_shortcut_relinquishes_legacy_g_prompt_when_context_router_owns_it():
    source = read("World", "WorldShortcut.cs")
    adapter = read("World", "WorldShortcutInteractionV06.cs")

    for token in (
        "public bool ExternalInteractionOwned",
        "public bool CanUnlockNow",
        "public void SetExternalInteractionOwned(bool owned)",
        "public void RestoreUnlocked(bool unlocked, bool immediate = true)",
        "if (_externalInteractionOwned || !CanUnlockNow) return;",
    ):
        assert token in source

    assert "shortcut.SetExternalInteractionOwned(true)" in adapter
    assert "shortcut?.RestoreUnlocked(unlocked, true)" in adapter
    assert '"memory_forge_market_loop"' not in adapter  # identity is authored by builder, not hardwired here


def test_profile_v2_is_one_persistence_envelope_with_explicit_physical_adapters():
    source = read("World", "WorldPersistenceV06.cs")

    for token in (
        '"mindforge.player_profile.v2"',
        "PlayerProgressionSnapshot progression",
        "PlayerInventorySnapshotV06 inventory",
        "List<WorldStateEntry> durable_world_facts",
        "List<WorldPersistentRecordV06> physical_world_records",
        "public interface IWorldPersistentAdapterV06",
        "string StableWorldId { get; }",
        "string CapturePersistentState()",
        "void RestorePersistentState(string stateJson)",
        "checkpoint.Rested += OnForgeRested",
        "WorldSignalKind.EncounterStarted",
        "WorldSignalKind.EncounterCleared",
        "SaveNow();",
        "TryMigrateLegacyV05()",
        '"profile-v1.json"',
        '"profile-v2.json"',
    ):
        assert token in source

    # Arbitrary physical ledger keys are still not whitelisted. They must have a restore adapter.
    semantic = source[source.index("private static bool IsDurableSemanticFact"):source.index("private string ResolvePath")]
    assert 'StartsWith("story."' in semantic
    assert 'StartsWith("profile."' in semantic
    for forbidden in (
        'StartsWith("world."',
        'StartsWith("encounter."',
        'StartsWith("boss."',
        'StartsWith("checkpoint."',
    ):
        assert forbidden not in semantic


def test_inventory_receipts_make_world_rewards_idempotent_and_persist_equipment_regions():
    source = read("World", "WorldPersistenceV06.cs")
    content = read("World", "WorldInteractionContentV06.cs")

    for token in (
        "List<InventoryStackV06> stacks",
        "List<EquipmentBindingV06> equipped",
        "List<string> reward_receipts",
        "List<string> discovered_regions",
        "public bool HasReceipt(string receiptId)",
        "public bool Grant(string itemId, int quantity, string rewardReceipt = null)",
        "rewardReceipts.Contains(receipt)",
        "public bool TryEquip(string slot, string itemId)",
        "public bool DiscoverRegion(string regionId)",
    ):
        assert token in source

    assert 'private string Receipt => "world_pickup:" + StableWorldId' in content
    assert "inventory.HasReceipt(Receipt)" in content
    assert "inventory.Grant(itemId, quantity, Receipt)" in content
    assert "PersistentPickupStateV06 { claimed = _claimed }" in content


def test_shrine_npc_dialogue_and_region_discovery_share_semantic_truth():
    source = read("World", "WorldInteractionContentV06.cs")

    for token in (
        "public sealed class PersistentShrineInteractionV06",
        "persistence?.SaveNow();",
        '"world.shrine." + StableWorldId + ".visited"',
        "public sealed class RegionDiscoveryV06",
        "inventory.DiscoverRegion(regionId)",
        '"profile.region." + PlayerInventoryV06.NormalizeId(regionId) + ".discovered"',
        "public sealed class DialogueGraphV06 : ScriptableObject",
        "public sealed class DialogueSessionV06",
        "public sealed class NpcDialogueInteractionV06 : WorldInteractionSourceV1",
        '"dialogue_choice"',
        '"dialogue_node"',
    ):
        assert token in source

    # Dialogue is presentation/semantic state only. It does not become a second input owner.
    assert "Input.GetKey" not in source
    assert "GuardianControlAction.Interact" not in source


def test_constraint_world_generation_is_deterministic_bounded_and_height_aware():
    solver = read("ThirdParty", "Wfc", "MindforgeConstraintCollapse.cs")
    assembler = read("World", "ModularWorldAssemblerV06.cs")
    license_text = read("ThirdParty", "Wfc", "LICENSE.txt")

    for token in (
        "Adapted from Maxim Gumin's WaveFunctionCollapse (MIT)",
        "NextUnobservedCell",
        "double entropy = Math.Log(sum) - sumWeightLogWeight / sum",
        "Collapse(cell, random)",
        "Propagate()",
        "new Random(seed)",
    ):
        assert token in solver

    for token in (
        "WorldTileDefinitionV06",
        "heightSteps",
        "SocketMatches",
        "MindforgeConstraintCollapse",
        "seed + attempt * 7919",
        "BuildVerticalConnectors",
        "BuildPerimeter",
        '"path"',
        '"sealed"',
    ):
        assert token in assembler

    assert "MIT License" in license_text
    assert "Copyright (c) 2016 Maxim Gumin" in license_text
    assert "sample images and tiles" in license_text


def test_v06_builder_installs_concrete_content_inside_existing_safe_world():
    builder = read("Editor", "WorldV06Builder.cs")
    showcase = read("Editor", "ShowcaseEditorMenu.cs")

    for token in (
        'RootName = "Mindforge_Persistent_World_V06"',
        "foundationRoot.AddComponent<PlayerInventoryV06>()",
        "foundationRoot.AddComponent<PlayerProfileSaveV06>()",
        "legacy.enabled = false",
        "shortcut.gameObject.AddComponent<WorldShortcutInteractionV06>()",
        '"memory_forge_market_loop"',
        '"Neural_Cloister_Procedural_Annex"',
        "new Vector2Int(3, 5)",
        "serialized.FindProperty(\"enclosePerimeter\").boolValue = false",
        "assembler.Generate()",
        '"forge.memory_shard.01"',
        '"cloister.aether_lens.01"',
        '"cloister.signal_shrine.01"',
        '"null_market.archivist.01"',
        '"neural_cloister"',
    ):
        assert token in builder

    foundation = showcase.index("GameFoundationV1Builder.ApplyOpenScene();")
    v05 = showcase.index("UxInteractionSaveV05Builder.ApplyOpenScene();")
    v06 = showcase.index("WorldV06Builder.ApplyOpenScene();")
    validation = showcase.index("CompetitionGateValidator.ValidateAndWrite(false);")
    assert foundation < v05 < v06 < validation


def test_tab_retains_current_quest_and_exposes_inventory_equipment_and_regions():
    menu = read("Presentation", "GuardianEquipmentMenu.cs")

    for token in (
        "PlayerInventoryV06 inventory",
        "GetPrimaryActiveQuest()",
        "GetCurrentStep(quest.id)",
        '"CURRENT OBJECTIVE"',
        '"PERSISTENT WORLD"',
        "inventory.Stacks",
        "inventory.Equipped",
        "inventory.DiscoveredRegions.Count",
        '"WORLD TRUTH"',
        '"MOUSE / ARROWS"',
        '"Lock / unlock enemy · wheel cycles target"',
    ):
        assert token in menu


def test_new_v06_csharp_guids_exist_and_are_unique_repository_wide():
    metas = (
        UNITY / "World" / "WorldPersistenceV06.cs.meta",
        UNITY / "World" / "WorldInteractionContentV06.cs.meta",
        UNITY / "World" / "ModularWorldAssemblerV06.cs.meta",
        UNITY / "World" / "WorldShortcutInteractionV06.cs.meta",
        UNITY / "ThirdParty" / "Wfc" / "MindforgeConstraintCollapse.cs.meta",
        UNITY / "Editor" / "WorldV06Builder.cs.meta",
    )
    expected = []
    for path in metas:
        text = path.read_text(encoding="utf-8")
        assert "fileFormatVersion: 2" in text
        guid = next(line for line in text.splitlines() if line.startswith("guid: ")).split(":", 1)[1].strip()
        assert len(guid) == 32
        expected.append(guid)
    assert len(expected) == len(set(expected))

    all_guids = {}
    for path in (ROOT / "unity" / "Assets").rglob("*.meta"):
        text = path.read_text(encoding="utf-8", errors="ignore")
        for line in text.splitlines():
            if not line.startswith("guid: "):
                continue
            guid = line.split(":", 1)[1].strip()
            all_guids.setdefault(guid, []).append(path)
            break
    for guid in expected:
        assert len(all_guids.get(guid, [])) == 1, (guid, all_guids.get(guid, []))
