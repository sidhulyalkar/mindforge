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

    # No parallel combat authority in the authoring layer.
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


def test_aetherblade_v2_is_nested_energy_presentation_not_gameplay_authority():
    blade = read("Presentation", "AetherbladeVisualPolishV2.cs")

    for token in (
        'VisualRootName = "AetherbladeVisualPolishV2"',
        '"AetherbladeOuterBloom"',
        '"AetherbladeTipCapV2"',
        '"AetherbladeEmitterVent_',
        '"AetherbladeAfterimageTrailV2"',
        '"AetherbladeEmitterLightV2"',
        "_combat.IsAttacking",
        "_combat.IsAttackActive",
        "_combat.AttackProgress",
        "_combat.SightResonance",
        "MaterialPropertyBlock",
        "Time.unscaledTime",
    ):
        assert token in blade

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


def test_showcase_build_orders_menagerie_population_before_identity_presentation():
    menu = read("Editor", "ShowcaseEditorMenu.cs")
    ecosystem = menu.index("NullWardArenaEcosystemBuilder.ApplyOpenScene();")
    menagerie = menu.index("ArenaMenagerieV1Builder.ApplyOpenScene();")
    collider = menu.index("NullWardEnemyColliderProfileBuilder.ApplyOpenScene();")
    v3 = menu.index("NullWardEnemySilhouetteV3Builder.ApplyOpenScene();")
    identity = menu.index("ArenaMenagerieSilhouetteV1Builder.ApplyOpenScene();")
    visual = menu.index("NullWardVisualInfrastructureBuilder.ApplyOpenScene();")
    assert ecosystem < menagerie < collider < v3 < identity < visual
    assert "Menagerie Crucible adds five specialized variants for a ten-identity" in menu


def test_new_unity_scripts_have_unique_pinned_guids():
    metas = (
        UNITY / "World" / "ArenaMenagerieDirector.cs.meta",
        UNITY / "Editor" / "ArenaMenagerieV1Builder.cs.meta",
        UNITY / "Editor" / "ArenaMenagerieSilhouetteV1Builder.cs.meta",
        UNITY / "Presentation" / "AetherbladeVisualPolishV2.cs.meta",
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
