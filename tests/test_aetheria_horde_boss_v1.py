from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_melee_advance_is_bounded_attack_data_resolved_by_existing_fixed_tick_authority():
    attack = read("Enemies", "EnemyAttackDefinition.cs")
    controller = read("Journey", "JourneyEnemyController.cs")

    for token in (
        '[SerializeField, Range(0f, 3.5f)] private float advanceDistance;',
        'public float AdvanceDistance => Mathf.Clamp(advanceDistance, 0f, 3.5f);',
        'float advance = 0f)',
        'advanceDistance = advance',
    ):
        assert token in attack

    for token in (
        'Vector3 attackOrigin = ResolveCommittedMeleeAdvance(attack);',
        'private Vector3 ResolveCommittedMeleeAdvance(EnemyAttackDefinition attack)',
        'attack.AdvanceDistance <= 0.001f',
        'body.SweepTest(direction, out RaycastHit hit, requested, QueryTriggerInteraction.Ignore)',
        'safeDistance = Mathf.Clamp(hit.distance - 0.08f, 0f, requested)',
        'body.MovePosition(target)',
        'Vector3 delta = Planar(player.position - attackOrigin)',
    ):
        assert token in controller

    # The shared movement is still in the existing fixed-tick enemy authority, not VFX.
    assert "private void FixedUpdate()" in controller
    assert "Time.fixedTime" in controller


def test_aetheria_horde_reuses_ten_menagerie_slots_and_authors_two_committed_advances():
    builder = read("Editor", "AetheriaHordeBossV1Builder.cs")

    for token in (
        'Find(enemies, "Menagerie_RiftHollow")',
        'Find(enemies, "Menagerie_SignalWarden")',
        'Find(enemies, "Menagerie_NullSentry")',
        'Find(enemies, "Menagerie_RiftStalker")',
        '"Menagerie_ScrapGoblin"',
        '"Menagerie_BassGolem"',
        '"Menagerie_AeroGargoyle"',
        'FindAttack(attacks, "stalker_pounce")',
        'SetAttackAdvance(pounce, 1.62f)',
        '"gargoyle_dive"',
        '2.05f);',
        'SetField(gargoyle, "meleeVerticalReach", 2.0f)',
        'LordMalatractPresentationV1',
    ):
        assert token in builder

    assert 'boss.gameObject.name = "Lord_Malatract"' not in builder
    assert "FracturedSignalDirector boss" in builder
    assert "JourneyEnemyController[] enemies" in builder

    for forbidden in (
        "private void FixedUpdate()",
        "ReceiveDamage(",
        "RequestDash(",
        "TryLightAttack(",
        "StartCoroutine(",
        "NeuralEvent",
        "UdpNeuralReceiver",
    ):
        assert forbidden not in builder


def test_story_horde_characters_are_collider_free_and_downstream_of_enemy_truth():
    presentation = read("Presentation", "AetheriaHordeCharacterPresentationV1.cs")

    for token in (
        "AetheriaHordeIdentity.ScrapGoblin",
        "AetheriaHordeIdentity.BassGolem",
        "AetheriaHordeIdentity.AeroGargoyle",
        '"Goblin_RGBHoard"',
        '"Goblin_LaserDaggerL"',
        '"BassGolem_SubwooferCore"',
        '"BassGolem_TinyEmbarrassedSkeleton"',
        '"Gargoyle_WingL"',
        '"Gargoyle_JetL"',
        "controller.Defeated += OnDefeated",
        "controller.Reconstructed += OnReconstructed",
        "controller.AttackTelegraphProgress01",
        "controller.AttackTrackingLocked",
        'controller.CurrentAttackId == "gargoyle_dive"',
        "collider.enabled = false",
    ):
        assert token in presentation

    for forbidden in (
        "ReceiveDamage(",
        "TryLightAttack(",
        "RequestDash(",
        "RequestJump(",
        "AddComponent<Rigidbody>",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
    ):
        assert forbidden not in presentation


def test_bass_golem_defeat_payoff_follows_authoritative_defeat_and_reconstructs_cleanly():
    presentation = read("Presentation", "AetheriaHordeCharacterPresentationV1.cs")

    assert "private void OnDefeated(JourneyEnemyController enemy)" in presentation
    assert "_defeatStarted = Time.unscaledTime" in presentation
    assert "AnimateArmorExplosion(time)" in presentation
    assert "Vector3 ballistic =" in presentation
    assert "elapsed > 0.14f" in presentation
    assert "_tinySkeleton.gameObject.SetActive(true)" in presentation
    assert "private void OnReconstructed(JourneyEnemyController enemy)" in presentation
    assert "_armorDebris[i].localPosition = _armorStart[i]" in presentation
    assert "renderers[i].enabled = true" in presentation


def test_malatract_is_visual_semantics_over_existing_boss_scheduler_not_a_second_boss_brain():
    malatract = read("Presentation", "LordMalatractPresentationV1.cs")

    for token in (
        "FracturedSignalDirector director",
        "FracturedSignalMeleeDirector melee",
        "director.PhaseChanged += OnPhaseChanged",
        "director.AttackTelegraphed += OnAttackTelegraphed",
        "director.AttackFired += OnAttackFired",
        "melee.MeleeTelegraphed += OnMeleeTelegraphed",
        "melee.MeleeResolved += OnMeleeResolved",
        'RootName = "LordMalatractPresentationV1"',
        '"MalatractMask"',
        '"MalatractVisor"',
        '"MalatractCrownL"',
        '"OrderedRuinBlade"',
        'transform.Find("FracturedSignalShowcaseAvatar")',
        "collider.enabled = false",
    ):
        assert token in malatract

    for forbidden in (
        "private void FixedUpdate()",
        "StartCoroutine(",
        "ReceiveDamage(",
        "TryResolveIncomingStrike(",
        "Instantiate(projectile",
        "RequestDash(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
    ):
        assert forbidden not in malatract


def test_showcase_applies_horde_identity_after_truthful_menagerie_silhouettes_before_world_dressing():
    menu = read("Editor", "ShowcaseEditorMenu.cs")

    menagerie = menu.index("ArenaMenagerieV1Builder.ApplyOpenScene();")
    collider = menu.index("ArenaMenagerieColliderV1Builder.ApplyOpenScene();")
    silhouette = menu.index("ArenaMenagerieSilhouetteV1Builder.ApplyOpenScene();")
    horde = menu.index("AetheriaHordeBossV1Builder.ApplyOpenScene();")
    aetheria = menu.index("AetheriaWorldV1Builder.ApplyOpenScene();")
    visual = menu.index("NullWardVisualInfrastructureBuilder.ApplyOpenScene();")
    assert menagerie < collider < silhouette < horde < aetheria < visual

    assert "Scrap Goblin, Bass Golem and Aero Gargoyle" in menu
    assert "existing Fractured Signal projectile/melee scheduler" in menu


def test_horde_boss_unity_scripts_have_unique_guids():
    metas = (
        UNITY / "Presentation" / "AetheriaHordeCharacterPresentationV1.cs.meta",
        UNITY / "Presentation" / "LordMalatractPresentationV1.cs.meta",
        UNITY / "Editor" / "AetheriaHordeBossV1Builder.cs.meta",
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
