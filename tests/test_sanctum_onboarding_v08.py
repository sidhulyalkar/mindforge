from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_hostile_projectiles_receive_opening_readability_scale_only():
    projectile = read("Combat", "MindforgeProjectile.cs")
    opening = read("World", "OpeningExperienceV08.cs")

    assert "OpeningExperienceDirectorV08.EnemyProjectileSpeedScale" in projectile
    assert "newTeam == CombatTeam.Enemy" in projectile
    assert "_body.velocity = launchVelocity" in projectile
    # Reflected player projectiles remain explicit and are not passed through opening assist.
    assert "_body.velocity = direction * speed" in projectile

    for token in (
        "arrivalProjectileScale = 0.60f",
        "calibrationProjectileScale = 0.60f",
        "practiceProjectileScale = 0.66f",
        "revealProjectileScale = 0.70f",
        "firstEncounterProjectileScale = 0.74f",
        "releasedProjectileScale = 0.82f",
    ):
        assert token in opening


def test_opening_replaces_cramped_geometry_with_human_scale_clearances():
    builder = read("Editor", "SanctumOnboardingV08Builder.cs")

    for token in (
        'RootName = "Mindforge_Sanctum_Onboarding_V08"',
        '"SanctumFloor"',
        "new Vector3(30f, 0.58f, 25f)",
        "float x = side * 11.1f",
        '"Sanctum_Threshold_Gate_V08"',
        "side * 6.65f",
        "new Vector3(11.6f, 7.4f, 0.42f)",
        '"TerraceFloor"',
        "new Vector3(30f, 0.54f, 14f)",
        '"ProcessionalLane"',
        "new Vector3(10.5f, 0.08f, 13.5f)",
        '"Sanctum_First_Sentinel_Court_V08"',
        "new Vector3(30f, 0.55f, 12.5f)",
    ):
        assert token in builder

    assert "Structural rhythm lives outside +/-8m" in builder
    assert "Clear 10m central procession lane" in builder


def test_floor_rift_hollows_are_removed_from_first_causeway_not_merely_hidden():
    builder = read("Editor", "SanctumOnboardingV08Builder.cs")
    ecosystem = read("Editor", "NullWardArenaEcosystemBuilder.cs")

    # Legacy ecosystem did intentionally add the floor rushers.
    assert '"Causeway_RiftHollow_A"' in ecosystem
    assert '"Causeway_RiftHollow_B"' in ecosystem
    assert "JourneyEnemyArchetype.Hollow" in ecosystem

    # V0.8 edits the authoritative zone array and destroys those opening instances.
    assert "RemoveOpeningFloorRushers(encounterDirector)" in builder
    assert "enemy.Archetype != JourneyEnemyArchetype.Hollow" in builder
    assert "DestroyImmediate(enemy.gameObject)" in builder
    assert "zone.enemies = FilterEnemies" in builder
    assert "Suspended Sentries telegraph slow tracking bolts across a wide court" in builder


def test_early_enemies_are_spread_across_wider_courts_and_later_beats_move_deeper():
    builder = read("Editor", "SanctumOnboardingV08Builder.cs")

    for token in (
        '"Causeway_NullSentry_A", new Vector3(-7.1f, -0.30f, -27.4f)',
        '"Causeway_NullSentry_B", new Vector3(7.0f, -0.30f, -25.6f)',
        'zone.activationPoint.localPosition = new Vector3(0f, 0f, -30.8f)',
        'zone.activationPoint.localPosition = new Vector3(0f, 0f, -23.2f)',
        '"Market_ChromePenitent", new Vector3(-4.8f, -0.30f, -20.7f)',
        '"Market_Shardsinger", new Vector3(5.8f, 1.35f, -20.2f)',
        'zone.activationPoint.localPosition = new Vector3(0f, 0f, -11.8f)',
    ):
        assert token in builder


def test_opening_phase_order_is_arrival_calibration_practice_reveal_encounter_release():
    source = read("World", "OpeningExperienceV08.cs")
    builder = read("Editor", "SanctumOnboardingV08Builder.cs")

    enum = source[source.index("public enum OpeningExperiencePhaseV08"):source.index("public sealed class OpeningExperienceDirectorV08")]
    order = ["Arrival", "Calibration", "Practice", "WorldReveal", "FirstEncounter", "Released"]
    positions = [enum.index(name) for name in order]
    assert positions == sorted(positions)

    for token in (
        "OpeningExperiencePhaseV08.Practice, \"sanctum_threshold_crossed\"",
        "OpeningExperiencePhaseV08.WorldReveal, \"threshold_overlook_reached\"",
        "OpeningExperiencePhaseV08.FirstEncounter, \"first_sentinel_court_entered\"",
        "OpeningExperiencePhaseV08.Released, \"sanctum_onboarding_complete\"",
    ):
        assert token in builder


def test_resonance_stations_reuse_single_context_e_and_preview_never_claims_bci_evidence():
    source = read("World", "OpeningExperienceV08.cs")
    interaction = read("World", "WorldInteractionV1.cs")

    assert "SanctumCalibrationOrbV08 : WorldInteractionSourceV1" in source
    assert "public override int Priority => 21" in source
    assert "MemoryForgeInteractionV1 : WorldInteractionSourceV1" in interaction
    assert "public override int Priority => 30" in interaction

    # No V0.8 calibration station samples input itself.
    assert "Input.GetKey" not in source
    assert "GuardianControlAction.Interact" not in source

    for token in (
        '"VISUAL_PREVIEW_NOT_NEURAL_EVIDENCE"',
        '"CONTROLLER_PREVIEW_COMPLETE"',
        '"NEURAL_CALIBRATION_ACCEPTED"',
        "Visual preview only. Do not use this render-frame flicker as scientific timing.",
    ):
        assert token in source


def test_participant_specific_calibration_extension_is_derived_data_only_and_persistent():
    source = read("World", "OpeningExperienceV08.cs")
    event = read("NeuralBridge", "NeuralEvent.cs")
    calibration = read("Calibration", "AwakeningCalibrationDirector.cs")

    # Existing scientific handshake remains the authority.
    assert "baselineSeconds = 5f" in calibration
    assert "sightSeconds = 5f" in calibration
    assert "guardSeconds = 5f" in calibration
    assert "if (arenaRoot != null) arenaRoot.SetActive(false)" in calibration
    assert "guardianInput?.SetCombatActionsEnabled(false)" in calibration
    assert "evt.IsCalibrationReady" in calibration

    for token in (
        "public float stimulus_hz",
        "public int candidate_rank",
        "public float selected_sight_hz",
        "public float selected_guard_hz",
        '"CALIBRATION_CANDIDATE_SCORE"',
    ):
        assert token in event

    for token in (
        "ParticipantCalibrationProfileV08",
        "receiver.EventReceived += OnNeuralEvent",
        "evt.IsCalibrationCandidateScore",
        '"profile.bci.selected_sight_hz"',
        '"profile.bci.selected_guard_hz"',
        '"profile.bci.calibration_confidence"',
        '"profile.bci.calibration_quality"',
        "No raw EEG or sample arrays cross into Unity",
    ):
        assert token in source


def test_sanctum_has_natural_world_reveal_and_restrained_bright_palette():
    materials = read("Editor", "SanctumMaterialAuthoringV08.cs")
    builder = read("Editor", "SanctumOnboardingV08Builder.cs")

    for name in (
        "SanctumIvoryV08",
        "SanctumPearlV08",
        "SanctumGoldV08",
        "SanctumBlueGlassV08",
        "SanctumWaterV08",
        "SanctumGardenV08",
    ):
        assert name in materials

    for token in (
        '"WaterL"',
        '"WaterR"',
        '"TreeCrown_',
        '"Sanctum_World_Reveal_V08"',
        '"VistaCanal"',
        '"VistaBridge"',
        '"VistaTower_"',
        "new Vector3(0f, 0f, 48f)",
        "RenderSettings.fog = false",
    ):
        assert token in builder


def test_v08_runs_after_v07_but_before_validation():
    menu = read("Editor", "ShowcaseEditorMenu.cs")
    v06 = menu.index("WorldV06Builder.ApplyOpenScene();")
    v07 = menu.index("WorldV07Builder.ApplyOpenScene();")
    v08 = menu.index("SanctumOnboardingV08Builder.ApplyOpenScene();")
    validation = menu.index("CompetitionGateValidator.ValidateAndWrite(false);")
    assert v06 < v07 < v08 < validation

    # Preserve the exact wording required by the V0.5 UI regression contract.
    assert "Tab opens kit + controls + objective" in menu


def test_v08_unity_guids_exist_and_are_unique_repository_wide():
    metas = (
        UNITY / "World" / "OpeningExperienceV08.cs.meta",
        UNITY / "Editor" / "SanctumMaterialAuthoringV08.cs.meta",
        UNITY / "Editor" / "SanctumOnboardingV08Builder.cs.meta",
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
            if line.startswith("guid: "):
                guid = line.split(":", 1)[1].strip()
                all_guids.setdefault(guid, []).append(path)
                break
    for guid in expected:
        assert len(all_guids.get(guid, [])) == 1, (guid, all_guids.get(guid, []))
