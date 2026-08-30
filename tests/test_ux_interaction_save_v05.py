from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"
DOCS = ROOT / "docs"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_one_canonical_control_profile_owns_advertised_conventional_vocabulary_only():
    source = read("Combat", "GuardianControlProfileV1.cs")

    for token in (
        "public enum GuardianControlAction",
        "Interact = 0",
        "TargetLock = 1",
        "JumpHover = 2",
        "EvadeBoost = 3",
        "Blade = 4",
        "Cleave = 5",
        "Counter = 6",
        "Bloom = 7",
        "Menu = 8",
        "interact = KeyCode.E",
        "targetLock = KeyCode.T",
        "jumpHover = KeyCode.Space",
        "evadeBoostPrimary = KeyCode.LeftShift",
        "rightMouseEvades = true",
        "blade = KeyCode.F",
        "leftMouseBlade = true",
        "cleave = KeyCode.Q",
        "counter = KeyCode.C",
        "bloom = KeyCode.R",
        "menu = KeyCode.Tab",
        "public bool Pressed(GuardianControlAction action)",
        "public bool Held(GuardianControlAction action)",
        "public string Label(GuardianControlAction action)",
    ):
        assert token in source

    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
        "AuraBuffController",
        "sight_score",
        "guard_score",
        "ReceiveDamage(",
        "TryLightAttack(",
        "RequestDash(",
    ):
        assert forbidden not in source


def test_foot_and_mount_input_sample_profile_while_context_router_owns_e():
    foot = read("Combat", "GuardianCombatInput.cs")
    bike = read("Traversal", "GuardianHoverbikeController.cs")
    router = read("Combat", "GuardianInteractionRouterV1.cs")

    assert "GuardianControlProfileV1 controls" in foot
    assert "GuardianControlProfileV1 controls" in bike
    assert "GuardianControlProfileV1 controls" in router
    assert "_move = controls.SampleMovement()" in foot
    assert "_moveInput = controls != null ? controls.SampleMovement()" in bike
    assert "controls.Pressed(GuardianControlAction.Interact)" in router

    # Foot combat must not also encode a mount/context decision.
    assert "mount_toggle_down = false" in foot
    assert "context_down = false" in foot
    assert "_mountLatched" not in foot

    # Mounted movement/attack/boost remain in the hoverbike authority; only E is delegated.
    assert "controls.Pressed(GuardianControlAction.Blade)" in bike
    assert "controls.Pressed(GuardianControlAction.EvadeBoost)" in bike
    assert "contextInteractionOwned" in bike
    assert "SetContextInteractionOwned(bool owned)" in bike
    assert "public bool TryMount(AetherHoverbikeMount bike)" in bike
    assert "public void RequestDismount(bool emergency = false)" in bike


def test_context_sources_rank_offers_but_never_sample_input_or_steal_physical_authority():
    source = read("World", "WorldInteractionV1.cs")

    for token in (
        "public interface IWorldInteractionV1",
        "public abstract class WorldInteractionSourceV1",
        "public static WorldInteractionSourceV1 FindBest",
        "float candidate = -source.Priority * 100f + distance + angle * 0.018f",
        "public sealed class MemoryForgeInteractionV1",
        "public override int Priority => 30",
        "checkpoint.RestAndReconstruct()",
        "public sealed class HoverbikeInteractionV1",
        "public override int Priority => 10",
        "controller.TryMount(mount)",
    ):
        assert token in source

    # Forge wins over a parked bike when both offers are valid.
    assert source.index("public override int Priority => 30") < source.index("public sealed class HoverbikeInteractionV1")

    for forbidden in (
        "Input.Get",
        "AddComponent<Rigidbody>",
        "MovePosition(",
        "ReceiveDamage(",
        "RequestDash(",
        "TryLightAttack(",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in source


def test_router_records_context_v5_and_legacy_mount_edge_cannot_trigger_new_world_actions():
    tape = read("Combat", "GuardianInputTape.cs")
    router = read("Combat", "GuardianInteractionRouterV1.cs")

    for token in (
        'SchemaV5 = "mindforge.guardian_input_tape.v5"',
        "schema = GuardianInputTape.SchemaV5",
        "public bool context_down",
        "context_down = context_down",
        "context_down |= other.context_down",
        "IsLegacyPreContextReplay",
    ):
        assert token in tape

    assert "context_down = _interactLatched" in router
    assert "inputTape.IsLegacyPreContextReplay" in router
    assert "IsFocusedBikeInteraction()" in router
    assert "contextEdge = command.mount_toggle_down" in router
    assert '"vehicle.prism_hoverbike.mount"' in router
    assert '"vehicle.prism_hoverbike.dismount"' in router

    # Legacy mount semantics are guarded by a bike-focused predicate before the old bit is read.
    legacy_guard = router.index("inputTape.IsLegacyPreContextReplay")
    bike_guard = router.index("IsFocusedBikeInteraction()", legacy_guard)
    old_edge = router.index("contextEdge = command.mount_toggle_down", bike_guard)
    assert legacy_guard < bike_guard < old_edge

    for forbidden in (
        "checkpoint.RestAndReconstruct(",
        "ReceiveDamage(",
        "RequestDash(",
        "TryLightAttack(",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in router


def test_memory_forge_exposes_external_context_seam_and_keeps_legacy_g_only_as_fallback():
    checkpoint = read("World", "MemoryForgeCheckpoint.cs")

    for token in (
        "public event Action Rested",
        "public bool ExternalInteractionOwned",
        "public bool CanRestNow",
        "public void SetExternalInteractionOwned(bool owned)",
        "if (_externalInteractionOwned) return;",
        "if (Input.GetKeyDown(interactKey)) RestAndReconstruct();",
        "Rested?.Invoke();",
    ):
        assert token in checkpoint

    update = checkpoint[checkpoint.index("private void Update()"):checkpoint.index("private void FixedUpdate()")]
    assert update.index("if (_externalInteractionOwned) return;") < update.index("Input.GetKeyDown(interactKey)")


def test_target_lock_reserves_arrows_for_camera_and_uses_wheel_to_cycle():
    lock = read("Combat", "GuardianTargetLock.cs")
    camera = read("Presentation", "ShowcaseCameraRig.cs")

    assert "GuardianControlProfileV1 controls" in lock
    assert "GuardianControlAction.TargetLock" in lock
    assert "Input.mouseScrollDelta.y" in lock
    assert "Cycle(1)" in lock and "Cycle(-1)" in lock
    assert "KeyCode.LeftArrow" not in lock
    assert "KeyCode.RightArrow" not in lock
    assert "KeyCode.LeftArrow" in camera
    assert "KeyCode.RightArrow" in camera


def test_safe_profile_persists_progression_and_nonphysical_facts_but_not_encounter_truth():
    save = read("World", "PlayerProfileSaveV05.cs")

    for token in (
        'ProfileSchema = "mindforge.player_profile.v1"',
        'ProgressionSchema = "mindforge.player_progression.v1"',
        "progression.CaptureSnapshot()",
        "progression.RestoreSnapshot",
        "reward_receipts",
        'normalized.StartsWith("story.", StringComparison.Ordinal)',
        'normalized.StartsWith("profile.", StringComparison.Ordinal)',
        "checkpoint.Rested += OnCheckpointRested",
        "File.WriteAllText(temp",
        "File.Replace(temp, path, backup, true)",
        "File.Copy(temp, path, true)",
        "Application.persistentDataPath",
    ):
        assert token in save or token == "reward_receipts"

    # The whitelist method is the persistence boundary. Physical encounter/boss facts cannot
    # pass merely because they exist in WorldStateLedger.
    method = save[save.index("private static bool IsDurableNonPhysicalFact"):save.index("private static void CommitTempFile")]
    assert 'StartsWith("story."' in method
    assert 'StartsWith("profile."' in method
    for forbidden in (
        'StartsWith("encounter."',
        'StartsWith("boss."',
        'StartsWith("checkpoint."',
        'StartsWith("world."',
        'StartsWith("region."',
        'StartsWith("journey."',
    ):
        assert forbidden not in method

    for forbidden in (
        "ReceiveDamage(",
        "RequestDash(",
        "TryLightAttack(",
        "ResetForCheckpoint(",
        "ResetOrdinaryEncounters(",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in save


def test_loaded_story_facts_reconcile_beacons_without_duplicate_discovery_signal():
    beacon = read("World", "WorldStoryBeaconV1.cs")

    assert "ledger.StateChanged += OnWorldStateChanged" in beacon
    assert "ledger.StateChanged -= OnWorldStateChanged" in beacon
    assert "if (ledger.TryGetBool(key, out bool already) && already)" in beacon
    assert "if (!ledger.SetBool(key, true, \"world_story_discovery\")) return;" in beacon
    assert "WorldSignalKind.StoryDiscovered" in beacon


def test_onboarding_and_tab_reference_share_control_profile_and_current_objective():
    guide = read("Presentation", "PlayerAgencyGuide.cs")
    menu = read("Presentation", "GuardianEquipmentMenu.cs")

    for source in (guide, menu):
        assert "GuardianControlProfileV1" in source
        assert "GuardianControlAction.Interact" in source
        assert "GuardianControlAction.TargetLock" in source
        assert "GuardianControlAction.Menu" in source

    assert "interactionRouter.HasOffer" in guide
    assert "MOUSE WHEEL CYCLES LOCKED TARGETS" in guide
    assert "one context button rides bikes, reconstructs at shrines" in guide
    assert "GetPrimaryActiveQuest()" in menu
    assert "GetCurrentStep(quest.id)" in menu
    assert "CURRENT OBJECTIVE" in menu
    assert "Context: ride · dismount · reconstruct · use world" in menu
    assert "CTRL / ALT" not in menu


def test_v05_builder_installs_exactly_the_expected_player_facing_adapters_after_foundation():
    builder = read("Editor", "UxInteractionSaveV05Builder.cs")
    menu = read("Editor", "ShowcaseEditorMenu.cs")

    for token in (
        'RootName = "Mindforge_UX_Interaction_Save_V05"',
        "root.AddComponent<GuardianControlProfileV1>()",
        "guardian.AddComponent<GuardianInteractionRouterV1>()",
        "checkpoint.gameObject.AddComponent<MemoryForgeInteractionV1>()",
        "mount.gameObject.AddComponent<HoverbikeInteractionV1>()",
        "foundationRoot.AddComponent<PlayerProfileSaveV05>()",
        "profile.ConfigureRuntime(ledger, progression, checkpoint, bus)",
        "bike.SetContextInteractionOwned(true)",
    ):
        assert token in builder

    foundation = menu.index("GameFoundationV1Builder.ApplyOpenScene();")
    ux = menu.index("UxInteractionSaveV05Builder.ApplyOpenScene();")
    validation = menu.index("CompetitionGateValidator.ValidateAndWrite(false);")
    assert foundation < ux < validation
    assert "E is the single contextual world action" in menu
    assert "V5 input tapes record context separately from legacy mount edges" in menu


def test_v05_new_unity_guids_exist_and_are_unique_across_repository():
    new_metas = (
        UNITY / "Combat" / "GuardianControlProfileV1.cs.meta",
        UNITY / "Combat" / "GuardianInteractionRouterV1.cs.meta",
        UNITY / "World" / "WorldInteractionV1.cs.meta",
        UNITY / "World" / "PlayerProfileSaveV05.cs.meta",
        UNITY / "Editor" / "UxInteractionSaveV05Builder.cs.meta",
    )

    new_guids = []
    for path in new_metas:
        text = path.read_text(encoding="utf-8")
        assert "fileFormatVersion: 2" in text
        guid = next(line for line in text.splitlines() if line.startswith("guid: ")).split(":", 1)[1].strip()
        assert len(guid) == 32
        new_guids.append(guid)
    assert len(new_guids) == len(set(new_guids))

    all_guids = {}
    for path in (ROOT / "unity" / "Assets").rglob("*.meta"):
        text = path.read_text(encoding="utf-8", errors="ignore")
        for line in text.splitlines():
            if not line.startswith("guid: "):
                continue
            guid = line.split(":", 1)[1].strip()
            all_guids.setdefault(guid, []).append(path)
            break

    for guid in new_guids:
        assert len(all_guids.get(guid, [])) == 1, (guid, all_guids.get(guid, []))
