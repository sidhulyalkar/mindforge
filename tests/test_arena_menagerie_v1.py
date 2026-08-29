from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_menagerie_authors_ten_named_enemy_identities_and_three_readable_waves():
    builder = read("Editor", "ArenaMenagerieV1Builder.cs")
    director = read("World", "ArenaMenagerieDirector.cs")

    names = (
        "Menagerie_RiftHollow",
        "Menagerie_Shardsinger",
        "Menagerie_SignalWarden",
        "Menagerie_NullSentry",
        "Menagerie_ChromePenitent",
        "Menagerie_RiftStalker",
        "Menagerie_ChoirDrone",
        "Menagerie_PrismMaw",
        "Menagerie_VeilReaper",
        "Menagerie_OrbitSeraph",
    )
    for name in names:
        assert name in builder
    assert builder.count('CreateRole("Menagerie_') == 10
    assert 'new[] { 3, 3, 4 }' in builder
    assert 'Center = new Vector3(5.0f, 0f, 18.0f)' in builder
    assert '"Crucible_OuterSignalRing"' in builder
    assert '"Crucible_InnerSignalRing"' in builder

    assert "private void FixedUpdate()" in director
    assert "Time.fixedTime" in director
    assert "interWaveDelayTicks = 84" in director
    assert "StartWave(0)" in director
    assert "CurrentWaveCleared()" in director
    assert "enemy.Arm()" in director
    assert "enemy.Disarm()" in director
    assert "ResetForCheckpoint()" not in director


def test_variant_profile_survives_controller_on_enable_base_default_reapplication():
    director = read("World", "ArenaMenagerieDirector.cs")
    profile = read("World", "ArenaMenagerieRoleProfile.cs")

    assert "if (profile == null) profile = enemy.gameObject.AddComponent<ArenaMenagerieRoleProfile>()" in director
    assert "if (!profile.Captured) profile.CaptureFromCurrent(enemy)" in director
    capture = director.index("profile.CaptureFromCurrent(enemy)")
    deactivate = director.index("enemy.gameObject.SetActive(false)", capture)
    assert capture < deactivate

    activate = director.index("enemy.gameObject.SetActive(true)")
    apply = director.index("profile?.Apply()", activate)
    arm = director.index("enemy.Arm()", apply)
    assert activate < apply < arm

    for token in (
        "public void CaptureFromCurrent",
        'GetField<float>(enemy, "moveSpeed")',
        'GetField<float>(enemy, "desiredDistance")',
        'GetField<float>(enemy, "retreatDistance")',
        'GetField<float>(enemy, "strafeStrength")',
        'GetField<float>(enemy, "meleeVerticalReach")',
        'GetField<int>(enemy, "firstAttackDelayTicks")',
        'GetField<EnemyAttackDefinition[]>(enemy, "attackDefinitions")',
        'SetField(enemy, "attackDefinitions", attackDefinitions',
        '"RebuildCooldownState"',
        "public bool Captured => captured",
    ):
        assert token in profile

    for forbidden in (
        "private void FixedUpdate()",
        "ReceiveDamage(",
        "RequestDash(",
        "TryLightAttack(",
        "Input.Get",
        "NeuralEvent",
        "UdpNeuralReceiver",
    ):
        assert forbidden not in profile


def test_menagerie_variants_share_one_enemy_authority_but_have_distinct_attack_grammars():
    builder = read("Editor", "ArenaMenagerieV1Builder.cs")

    for attack in (
        '"hollow_snap"',
        '"shardsinger_lance"',
        '"warden_judgement"',
        '"sentry_lockbolt"',
        '"penitent_bell"',
        '"stalker_pounce"',
        '"choir_crescendo"',
        '"prism_maw_cone"',
        '"reaper_toll"',
        '"seraph_horizon"',
    ):
        assert attack in builder

    assert "EnemyAttackType.Melee" in builder
    assert "EnemyAttackType.Projectile" in builder
    assert "EnemyAttackType.Burst" in builder
    assert "EnemyAttackType.Retreat" in builder
    assert 'SetRef(enemy, "attackDefinitions", attacks)' in builder
    assert 'InvokePrivate(enemy, "RebuildCooldownState")' in builder
    assert "JourneyEnemyController" in builder
    assert "ArenaMenagerieDirector" in builder

    for forbidden in (
        "ReceiveDamage(",
        "RequestDash(",
        "RequestJump(",
        "TryLightAttack(",
        "FirePulse(",
        "BeginCounter(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
    ):
        assert forbidden not in builder


def test_signature_attacks_have_distinct_danger_geometry_but_no_new_gameplay_authority():
    intent = read("Presentation", "JourneyEnemyIntentVfx.cs")

    for token in (
        'case "stalker_pounce":',
        "DrawChargeLane(attack)",
        'case "prism_maw_cone":',
        "DrawConeWedge(attack)",
        'case "choir_crescendo":',
        'case "seraph_horizon":',
        "DrawSpokeFan(attack",
        'case "reaper_toll":',
        "DrawHeavyDoomArc(attack)",
        "controller.AttackTelegraphProgress01",
        "controller.AttackTrackingLocked",
        "controller.RecoveryProgress01",
    ):
        assert token in intent

    assert "attack.Id" in intent
    assert "Time.unscaledTime" in intent
    for forbidden in (
        "ReceiveDamage(",
        "RequestDash(",
        "TryLightAttack(",
        "Instantiate(projectile",
        "NeuralEvent",
        "UdpNeuralReceiver",
    ):
        assert forbidden not in intent


def test_ten_menagerie_roles_have_non_humanoid_identity_geometry_without_hitbox_drift():
    silhouettes = read("Editor", "ArenaMenagerieSilhouetteV1Builder.cs")

    builders = (
        "BuildRiftHollow",
        "BuildShardsinger",
        "BuildSignalWarden",
        "BuildNullSentry",
        "BuildChromePenitent",
        "BuildRiftStalker",
        "BuildChoirDrone",
        "BuildPrismMaw",
        "BuildVeilReaper",
        "BuildOrbitSeraph",
    )
    for token in builders:
        assert token in silhouettes

    for signature in (
        '"Stalker_MandibleL"',
        '"Choir_HaloA"',
        '"Maw_JawTop"',
        '"Reaper_ScytheL"',
        '"Seraph_OrbitBlade_',
    ):
        assert signature in silhouettes

    assert "DestroyChild(visuals, NullWardEnemySilhouetteV3Builder.RootName)" in silhouettes
    assert "UnityEngine.Object.DestroyImmediate(c)" in silhouettes
    assert "AddComponent<CapsuleCollider>" not in silhouettes
    assert "AddComponent<Rigidbody>" not in silhouettes
    assert "ReceiveDamage(" not in silhouettes


def test_menagerie_collision_uses_one_role_fitted_root_capsule_not_decorative_hitboxes():
    collision = read("Editor", "ArenaMenagerieColliderV1Builder.cs")

    for role in (
        'name.Contains("RiftHollow")',
        'name.Contains("Shardsinger")',
        'name.Contains("SignalWarden")',
        'name.Contains("NullSentry")',
        'name.Contains("ChromePenitent")',
        'name.Contains("RiftStalker")',
        'name.Contains("ChoirDrone")',
        'name.Contains("PrismMaw")',
        'name.Contains("VeilReaper")',
        'name.Contains("OrbitSeraph")',
    ):
        assert role in collision

    assert "enemy.GetComponent<CapsuleCollider>()" in collision
    assert "collider.radius = radius * scale" in collision
    assert "collider.height = Mathf.Max(collider.radius * 2.05f, height * scale)" in collision
    assert "collider.center = Vector3.up * centerY * scale" in collision
    assert 'name.Contains("RiftStalker")' in collision and "height = 1.05f" in collision
    assert 'name.Contains("PrismMaw")' in collision and "height = 1.20f" in collision
    assert 'name.Contains("VeilReaper")' in collision and "height = 2.30f" in collision

    for forbidden in (
        "AddComponent<CapsuleCollider>",
        "BoxCollider",
        "SphereCollider",
        "ReceiveDamage(",
        "RequestDash(",
        "NeuralEvent",
    ):
        assert forbidden not in collision


def test_aetherblade_v2_is_nested_energy_presentation_not_gameplay_authority():
    blade = read("Presentation", "AetherbladeVisualPolishV2.cs")

    for token in (
        'VisualRootName = "AetherbladeVisualPolishV2"',
        'EnergyVisualRootName = "AetherbladeEnergyVisualsV2"',
        'AfterimageTipName = "AetherbladeAfterimageTipV2"',
        '"AetherbladeOuterBloom"',
        '"AetherbladeTipCapV2"',
        '"AetherbladeEmitterVent_',
        '"AetherbladeEmitterLightV2"',
        "afterimageTip.AddComponent<TrailRenderer>()",
        "_combat.IsAttacking",
        "_combat.IsAttackActive",
        "_combat.AttackProgress",
        "_combat.SightResonance",
        "MaterialPropertyBlock",
        "Time.unscaledTime",
    ):
        assert token in blade

    assert '_afterTrail.name = "AetherbladeAfterimageTrailV2"' not in blade
    assert 'tipAnchor.Find(AfterimageTipName)' in blade
    for forbidden in (
        "ReceiveDamage(",
        "TryLightAttack(",
        "RequestDash(",
        "RequestJump(",
        "RiftCleave(",
        "BeginCounter(",
        "SetLocked(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
        "reachMeters =",
        "damage =",
    ):
        assert forbidden not in blade


def test_menagerie_hud_is_compact_identity_only_and_not_an_authority_surface():
    hud = read("Presentation", "ArenaMenagerieHud.cs")

    for token in (
        '"MENAGERIE CRUCIBLE · CLEAR"',
        '"MENAGERIE CRUCIBLE · WAVE',
        '"SIGNAL QUIET · NEXT WAVE FORMING"',
        'enemy.name.StartsWith("Menagerie_")',
        "director.WaveIndex",
        "director.WaveCount",
        "new Rect(x, 14f, width, director.Complete ? 48f : 66f)",
    ):
        assert token in hud

    for forbidden in (
        "ReceiveDamage(",
        "RequestDash(",
        "TryLightAttack(",
        "SetLocked(",
        "NeuralEvent",
        "sight_score",
        "guard_score",
        "Input.Get",
    ):
        assert forbidden not in hud


def test_showcase_build_orders_menagerie_population_collision_then_identity_presentation():
    menu = read("Editor", "ShowcaseEditorMenu.cs")
    ecosystem = menu.index("NullWardArenaEcosystemBuilder.ApplyOpenScene();")
    menagerie = menu.index("ArenaMenagerieV1Builder.ApplyOpenScene();")
    menagerie_collision = menu.index("ArenaMenagerieColliderV1Builder.ApplyOpenScene();")
    collider = menu.index("NullWardEnemyColliderProfileBuilder.ApplyOpenScene();")
    v3 = menu.index("NullWardEnemySilhouetteV3Builder.ApplyOpenScene();")
    identity = menu.index("ArenaMenagerieSilhouetteV1Builder.ApplyOpenScene();")
    visual = menu.index("NullWardVisualInfrastructureBuilder.ApplyOpenScene();")
    assert ecosystem < menagerie < menagerie_collision < collider < v3 < identity < visual
    assert "Menagerie Crucible adds five specialized variants for a ten-identity" in menu


def test_new_unity_scripts_have_unique_pinned_guids():
    metas = (
        UNITY / "World" / "ArenaMenagerieDirector.cs.meta",
        UNITY / "World" / "ArenaMenagerieRoleProfile.cs.meta",
        UNITY / "Editor" / "ArenaMenagerieV1Builder.cs.meta",
        UNITY / "Editor" / "ArenaMenagerieColliderV1Builder.cs.meta",
        UNITY / "Editor" / "ArenaMenagerieSilhouetteV1Builder.cs.meta",
        UNITY / "Presentation" / "AetherbladeVisualPolishV2.cs.meta",
        UNITY / "Presentation" / "ArenaMenagerieHud.cs.meta",
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
