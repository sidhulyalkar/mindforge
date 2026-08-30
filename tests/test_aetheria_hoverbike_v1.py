from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"
DOCS = ROOT / "docs"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_aetheria_builder_layers_identity_over_existing_safe_world_and_adds_two_optional_bikes():
    builder = read("Editor", "AetheriaWorldV1Builder.cs")

    for token in (
        'RootName = "Mindforge_AetheriaWorld_V1"',
        "GroundedWorldV1Builder.RootName",
        '"Aetheria_PrismBastion"',
        '"Aetheria_NeonCauseway"',
        '"Aetheria_BrokenMomentumMarket"',
        '"Aetheria_RuinedChoir"',
        '"Aetheria_HallOfExcessiveGravitas"',
        '"PrismHoverbike_Causeway"',
        '"PrismHoverbike_Arena"',
        "guardian.AddComponent<GuardianHoverbikeController>()",
        "guardian.AddComponent<HoverbikeHud>()",
        "guardian.AddComponent<PrismSquirePresentationV1>()",
        "root.AddComponent<AetheriaNarrativeDirector>()",
    ):
        assert token in builder

    assert "UnityEngine.Object.DestroyImmediate(collider)" in builder
    assert "AddComponent<Rigidbody>" not in builder
    assert "ReceiveDamage(" not in builder
    assert "RequestDash(" not in builder
    assert "NeuralEvent" not in builder
    assert "VepAuraStimulus" not in builder


def test_moving_hoverbike_geometry_is_removed_from_static_batching_after_world_authoring():
    safety = read("Editor", "AetheriaDynamicMountSafetyBuilder.cs")

    assert "AetheriaWorldV1Builder.RootName" in safety
    assert "GetComponentsInChildren<AetherHoverbikeMount>(true)" in safety
    assert "GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0)" in safety
    assert "ReceiveDamage(" not in safety
    assert "RequestDash(" not in safety
    assert "NeuralEvent" not in safety


def test_hoverbike_keeps_guardian_rigidbody_as_sole_player_body_and_excludes_bci_authority():
    bike = read("Traversal", "GuardianHoverbikeController.cs")
    mount = read("Traversal", "AetherHoverbikeMount.cs")

    for token in (
        "[RequireComponent(typeof(Rigidbody))]",
        "private void FixedUpdate()",
        "footInput.enabled = false",
        "footMotor.enabled = false",
        "RestoreFootAuthority()",
        "bladeCombat != null && bladeCombat.TryLightAttack(aim)",
        "_body.velocity = horizontal + Vector3.up * vertical",
        "Physics.RaycastNonAlloc(",
        "TryStartBoost()",
    ):
        assert token in bike

    for forbidden in (
        "IsInvulnerable",
        "invulnerable",
        "RequestDash(",
        "ReceiveDamage(",
        "SetLocked(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
        "sight_score",
        "guard_score",
        "AddComponent<Rigidbody>",
    ):
        assert forbidden not in bike

    for forbidden in (
        "[RequireComponent(typeof(Rigidbody))]",
        "GetComponent<Rigidbody>",
        "AddComponent<Rigidbody>",
        "private Rigidbody",
        "public Rigidbody",
    ):
        assert forbidden not in mount
    assert "ReceiveDamage(" not in mount
    assert "TryLightAttack(" not in mount
    assert "collider.enabled = false" in mount
    assert "AttachTo(Transform rider)" in mount
    assert "DetachTo(Vector3 worldPosition" in mount


def test_mounted_mode_is_mutually_exclusive_with_foot_input_and_restores_it_on_all_exit_paths():
    bike = read("Traversal", "GuardianHoverbikeController.cs")

    mount = bike.index("if (footInput != null) footInput.enabled = false")
    motor_off = bike.index("if (footMotor != null) footMotor.enabled = false", mount)
    attach = bike.index("bike.AttachTo(transform)", motor_off)
    mounted = bike.index("_mounted = true", attach)
    assert mount < motor_off < attach < mounted

    assert "if (_mounted) Dismount(true);" in bike
    assert "if (vitals != null && !vitals.IsAlive)" in bike
    assert "if (_mounted) Dismount(true);" in bike
    assert "if (footMotor != null) footMotor.enabled = _footMotorWasEnabled" in bike
    assert "if (footInput != null) footInput.enabled = _footInputWasEnabled" in bike


def test_prism_squire_is_bright_block_presentation_and_handles_motion_wrapped_rig():
    squire = read("Presentation", "PrismSquirePresentationV1.cs")

    for token in (
        "[DefaultExecutionOrder(1100)]",
        'RootName = "PrismSquireOverlayV1"',
        '"OversizedHelmet"',
        '"HelmetVisor"',
        '"PrismChest"',
        '"GuildPennant"',
        '"PrismSquire_Cyan"',
        '"PrismSquire_Rose"',
        '"PrismSquire_Gold"',
        '_avatar.Find("Motion_Body")',
        'bodyMotion.Find("Motion_" + name)',
        "bike != null && bike.Mounted",
        "Time.unscaledTime",
    ):
        assert token in squire

    for forbidden in (
        "ReceiveDamage(",
        "TryLightAttack(",
        "RequestDash(",
        "RequestJump(",
        "SetMoveInput(",
        "NeuralEvent",
        "UdpNeuralReceiver",
    ):
        assert forbidden not in squire


def test_narrative_and_mount_hud_are_read_only_presentation_surfaces():
    narrative = read("Presentation", "AetheriaNarrativeDirector.cs")
    hud = read("Presentation", "HoverbikeHud.cs")

    for token in (
        '"PRISM BASTION"',
        '"THE NEON CAUSEWAY"',
        '"MARKET OF BROKEN MOMENTUM"',
        '"CHOIR OF RUINED TOWERS"',
        '"HALL OF EXCESSIVE GRAVITAS"',
        '"MENAGERIE CRUCIBLE"',
        '"MALATRACT // Motion is error.',
        "Time.unscaledTime",
    ):
        assert token in narrative

    assert '"PRISM HOVERBIKE ·' in hud
    assert "bike.HorizontalSpeed" in hud
    assert "bike.Boosting" in hud
    assert "MOUNT PRISM HOVERBIKE" not in hud
    assert "DISMOUNT" not in hud
    assert "GuardianInteractionRouterV1 is the single player-facing owner of E offers" in hud

    for source in (narrative, hud):
        for forbidden in (
            "ReceiveDamage(",
            "TryLightAttack(",
            "RequestDash(",
            "RequestJump(",
            "SetMoveInput(",
            "Input.Get",
            "NeuralEvent",
            "UdpNeuralReceiver",
            "VepAuraStimulus",
        ):
            assert forbidden not in source


def test_showcase_inserts_aetheria_after_truthful_enemy_identity_and_before_ambient_visuals():
    menu = read("Editor", "ShowcaseEditorMenu.cs")

    ecosystem = menu.index("NullWardArenaEcosystemBuilder.ApplyOpenScene();")
    menagerie = menu.index("ArenaMenagerieV1Builder.ApplyOpenScene();")
    menagerie_collision = menu.index("ArenaMenagerieColliderV1Builder.ApplyOpenScene();")
    ordinary_collision = menu.index("NullWardEnemyColliderProfileBuilder.ApplyOpenScene();")
    ordinary_silhouette = menu.index("NullWardEnemySilhouetteV3Builder.ApplyOpenScene();")
    menagerie_silhouette = menu.index("ArenaMenagerieSilhouetteV1Builder.ApplyOpenScene();")
    aetheria = menu.index("AetheriaWorldV1Builder.ApplyOpenScene();")
    v2 = menu.index("AetheriaStateOfArtV2Builder.ApplyOpenScene();")
    dynamic_mount = menu.index("AetheriaDynamicMountSafetyBuilder.ApplyOpenScene();")
    visual = menu.index("NullWardVisualInfrastructureBuilder.ApplyOpenScene();")
    assert ecosystem < menagerie < menagerie_collision < ordinary_collision < ordinary_silhouette < menagerie_silhouette < aetheria < v2 < dynamic_mount < visual

    assert "E is the single contextual world action" in menu
    assert "Two optional Prism hoverbikes use the existing Guardian" in menu
    assert "Rigidbody as mounted authority" in menu
    assert "UxInteractionSaveV05Builder.ApplyOpenScene();" in menu


def test_aetheria_design_artifacts_exist_and_keep_scope_bounded():
    names = (
        "AETHERIA_VERTICAL_SLICE_GDD.md",
        "AETHERIA_SCENE_IMPLEMENTATION_PLAN.md",
        "AETHERIA_PLAYER_ENEMY_ROSTER.md",
        "LORD_MALATRACT_BOSS_SPEC.md",
        "AETHERIA_ART_DIRECTION.md",
        "HOVERBIKE_MOUNTED_COMBAT_V1.md",
    )
    for name in names:
        assert (DOCS / name).exists()

    gdd = (DOCS / "AETHERIA_VERTICAL_SLICE_GDD.md").read_text(encoding="utf-8")
    bike = (DOCS / "HOVERBIKE_MOUNTED_COMBAT_V1.md").read_text(encoding="utf-8")
    assert "BCI evidence may transform" in gdd
    assert "may never originate" in gdd
    assert "The Guardian Rigidbody remains the only player body" in bike
    assert "No mounted ranged weapon in V1" in bike
    assert "no boost invulnerability" in bike


def test_new_aetheria_unity_scripts_have_unique_guids():
    metas = (
        UNITY / "Traversal" / "AetherHoverbikeMount.cs.meta",
        UNITY / "Traversal" / "GuardianHoverbikeController.cs.meta",
        UNITY / "Presentation" / "HoverbikeHud.cs.meta",
        UNITY / "Presentation" / "PrismSquirePresentationV1.cs.meta",
        UNITY / "Presentation" / "AetheriaNarrativeDirector.cs.meta",
        UNITY / "Editor" / "AetheriaWorldV1Builder.cs.meta",
        UNITY / "Editor" / "AetheriaDynamicMountSafetyBuilder.cs.meta",
    )
    guids = []
    for path in metas:
        text = path.read_text(encoding="utf-8")
        assert "fileFormatVersion: 2" in text
        line = next(line for line in text.splitlines() if line.startswith("guid: "))
        guid = line.split(":", 1)[1].strip()
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
