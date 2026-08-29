from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"
DOCS = ROOT / "docs"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_hackathon_builder_densifies_all_major_world_districts_without_new_collision_authority():
    source = read("Editor", "HackathonPlaythroughV1Builder.cs")

    for token in (
        'RootName = "Mindforge_HackathonPlaythrough_V1"',
        '"Hackathon_PrismBastionArrival"',
        '"Hackathon_NeonCausewayMegastructure"',
        '"Hackathon_BrokenMomentumBazaar"',
        '"Hackathon_RuinedChoirSkyline"',
        '"Hackathon_MenagerieCrucible"',
        '"Hackathon_GravitasProcessional"',
        '"Hackathon_DistantAetheria"',
        "FarAetherSpire_",
        "CrucibleTerrace_",
        "BazaarStallShell_",
        "CausewayOverbeam_",
        "UnityEngine.Object.DestroyImmediate(collider)",
    ):
        assert token in source

    for forbidden in (
        "AddComponent<Rigidbody>",
        "ReceiveDamage(",
        "RequestDash(",
        "RequestJump(",
        "SetMoveInput(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
    ):
        assert forbidden not in source


def test_first_large_encounter_is_authored_three_four_three_over_existing_enemy_authority():
    builder = read("Editor", "HackathonPlaythroughV1Builder.cs")
    director = read("World", "ArenaMenagerieDirector.cs")

    assert "new[] { 3, 4, 3 }" in builder
    for name in (
        "Menagerie_ScrapGoblin",
        "Menagerie_Shardsinger",
        "Menagerie_BassGolem",
        "Menagerie_ChromePenitent",
        "Menagerie_RiftStalker",
        "Menagerie_ChoirDrone",
        "Menagerie_AeroGargoyle",
        "Menagerie_PrismMaw",
        "Menagerie_VeilReaper",
        "Menagerie_OrbitSeraph",
    ):
        assert name in builder

    assert "director.ConfigureRuntime(guardian, activation, ordered, new[] { 3, 4, 3 })" in builder
    assert "JourneyEnemyController" in director
    assert "sole ordinary-enemy combat authority" in director
    assert "enemy.Arm()" in director
    assert "enemy.Vitals.IsAlive" in director


def test_all_ten_enemy_identities_receive_readable_collider_free_second_pass_detail():
    source = read("Presentation", "HackathonEnemyPresentationV1.cs")

    for identity in (
        "ScrapGoblin",
        "Shardsinger",
        "BassGolem",
        "ChromePenitent",
        "RiftStalker",
        "ChoirDrone",
        "AeroGargoyle",
        "PrismMaw",
        "VeilReaper",
        "OrbitSeraph",
    ):
        assert identity in source

    for signature in (
        "GoblinEarL",
        "SingerForkL",
        "GolemSpeakerJaw",
        "PenitentExecutionEdge",
        "StalkerScytheLegA_",
        "ChoirNode_",
        "GargoyleOuterWing_",
        "MawUpperJaw",
        "ReaperScytheBlade",
        "SeraphOrbital_",
    ):
        assert signature in source

    assert "controller.PendingAttack" in source
    assert "controller.IsRecovering" in source
    assert "collider.enabled = false" in source
    for forbidden in (
        "Rigidbody",
        "ReceiveDamage(",
        "Arm()",
        "Disarm()",
        "TryLightAttack(",
        "Input.Get",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in source


def test_guardian_v2_detail_is_motion_downstream_and_keeps_physics_out():
    source = read("Presentation", "PrismSquirePresentationV2.cs")

    for token in (
        'RootName = "PrismSquireOverlayV2"',
        '"LayeredBreastplate"',
        '"ShoulderFins"',
        '"BackReactor"',
        '"AetherHalfCape"',
        '"HeroCrest"',
        '"CrestBlade"',
        '"VisorBrow"',
        '"KneePlate_"',
        "motor.Velocity",
        "motor.IsGrounded",
        "motor.IsDashing",
        "combat.IsAttacking",
        "bike.Mounted",
        "flux.Value",
        "collider.enabled = false",
    ):
        assert token in source

    # The animated crest wrapper must actually own the crest geometry; otherwise the
    # LateUpdate crest motion becomes a dead transform with no visible child.
    assert '_crestRoot = Node("HeroCrest", head, Vector3.zero)' in source
    assert 'Part("CrestBlade", _crestRoot' in source

    for forbidden in (
        "GetComponent<Rigidbody>",
        "MovePosition(",
        ".velocity =",
        "ReceiveDamage(",
        "RequestDash(",
        "RequestJump(",
        "TryLightAttack(",
        "Input.Get",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in source


def test_encounter_presentation_consumes_wave_events_but_does_not_schedule_combat():
    source = read("Presentation", "HackathonEncounterPresentationV1.cs")
    for token in (
        "director.WaveStarted += OnWaveStarted",
        "director.WaveCleared += OnWaveCleared",
        "director.Completed += OnCompleted",
        "HackathonWaveBeacon",
        "victoryCrown",
        "Time.unscaledDeltaTime",
    ):
        # Beacon names are authored by the builder, while the runtime receives references.
        if token == "HackathonWaveBeacon":
            assert token in read("Editor", "HackathonPlaythroughV1Builder.cs")
        else:
            assert token in source

    for forbidden in (
        "StartWave(",
        "enemy.Arm()",
        "ReceiveDamage(",
        "RequestDash(",
        "Input.Get",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in source


def test_playthrough_progression_is_monotonic_semantic_signal_not_quest_god_object():
    source = read("World", "HackathonPlaythroughDirectorV1.cs")
    for token in (
        "HackathonPlaythroughStage",
        "Arrival",
        "Causeway",
        "BrokenMomentum",
        "RuinedChoir",
        "Gravitas",
        "Crucible",
        "Aftermath",
        "StageChanged",
        "if ((int)candidate > (int)stage)",
        "menagerie.Completed += OnEncounterCompleted",
    ):
        assert token in source

    for forbidden in (
        "SetActive(false)",
        "Instantiate(",
        "ReceiveDamage(",
        "RequestDash(",
        "RequestJump(",
        "SetMoveInput(",
        "PlayerPrefs",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in source


def test_one_click_showcase_places_hackathon_pass_after_aetheria_v2_before_safety_and_ambient():
    menu = read("Editor", "ShowcaseEditorMenu.cs")
    v2 = menu.index("AetheriaStateOfArtV2Builder.ApplyOpenScene();")
    hackathon = menu.index("HackathonPlaythroughV1Builder.ApplyOpenScene();")
    safety = menu.index("AetheriaDynamicMountSafetyBuilder.ApplyOpenScene();")
    visual = menu.index("NullWardVisualInfrastructureBuilder.ApplyOpenScene();")
    assert v2 < hackathon < safety < visual
    assert "3/4/3 hackathon encounter" in menu
    assert "all ten Menagerie enemies receive unique close/mid-distance silhouette detail" in menu


def test_hackathon_contract_and_unity_guids_exist():
    contract = DOCS / "HACKATHON_PLAYTHROUGH_V1.md"
    assert contract.exists()
    text = contract.read_text(encoding="utf-8")
    assert "3 / 4 / 3" in text
    assert "twenty-eight distant Aetheria skyline spires" in text
    assert "monotonic stage enum" in text
    assert "physical VEP timing/salience" in text

    metas = (
        UNITY / "Editor" / "HackathonPlaythroughV1Builder.cs.meta",
        UNITY / "World" / "HackathonPlaythroughDirectorV1.cs.meta",
        UNITY / "Presentation" / "HackathonEncounterPresentationV1.cs.meta",
        UNITY / "Presentation" / "HackathonEnemyPresentationV1.cs.meta",
        UNITY / "Presentation" / "PrismSquirePresentationV2.cs.meta",
    )
    guids = []
    for path in metas:
        value = path.read_text(encoding="utf-8")
        assert "fileFormatVersion: 2" in value
        guid = next(line for line in value.splitlines() if line.startswith("guid: ")).split(":", 1)[1].strip()
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
