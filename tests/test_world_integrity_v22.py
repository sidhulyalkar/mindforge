from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"
EDITOR = UNITY / "Editor"
COMBAT = UNITY / "Combat"
LATEST = EDITOR / "MindforgeLatestEditorMenu.cs"
WORLD = EDITOR / "WorldIntegrityV22Builder.cs"
FOUNDATION = EDITOR / "WorldFoundationV23Builder.cs"
CATHEDRAL = EDITOR / "WorldCathedralV24Builder.cs"
DUEL = COMBAT / "FracturedSignalDuelStabilityV22.cs"
V19 = COMBAT / "FracturedSignalFirstBossV19.cs"
DIRECTOR = COMBAT / "FracturedSignalDirector.cs"
MELEE = COMBAT / "FracturedSignalMeleeDirector.cs"
WISP = UNITY / "SoulWisp" / "WispCombatIntermissionV19.cs"
LINK = UNITY / "NeuralBridge" / "NeuralLinkContingency.cs"
SMOKE = UNITY / "Tests" / "Editor" / "FracturedSignalDuelStabilityV22SmokeTests.cs"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.22 source: {path}"
    return path.read_text(encoding="utf-8")


def test_v22_remains_the_integrity_stage_before_v23_v24_v25_and_v26_rendering():
    latest = read(LATEST)
    assert 'ProductVersion = "V0.26 Production Geometry + Cathedral Depth"' in latest
    v11 = latest.index("MindforgeDemoV11Builder.BuildDemoScene(controllerOnlyByDefault);")
    v20 = latest.index("WorldSoulV20Builder.ApplyOpenScene();", v11)
    v21 = latest.index("WorldCohesionV21Builder.ApplyOpenScene();", v20)
    v22 = latest.index("WorldIntegrityV22Builder.ApplyOpenScene();", v21)
    v23 = latest.index("WorldFoundationV23Builder.ApplyOpenScene();", v22)
    v24 = latest.index("WorldCathedralV24Builder.ApplyOpenScene();", v23)
    v25 = latest.index("SensoryFidelityV25Builder.ApplyOpenScene();", v24)
    v26 = latest.index("WorldRenderingV26Builder.ApplyOpenScene();", v25)
    assert v11 < v20 < v21 < v22 < v23 < v24 < v25 < v26
    assert "if (!WorldIntegrityV22Builder.PresentInOpenScene())" in latest
    assert "if (!WorldFoundationV23Builder.PresentInOpenScene())" in latest
    assert "if (!WorldCathedralV24Builder.PresentInOpenScene())" in latest
    assert "if (!SensoryFidelityV25Builder.PresentInOpenScene())" in latest
    assert "if (!WorldRenderingV26Builder.PresentInOpenScene())" in latest
    assert 'RootName = "Mindforge_World_Foundation_V23"' in read(FOUNDATION)
    assert 'RootName = "Mindforge_White_Cathedral_V24"' in read(CATHEDRAL)


def test_v22_forces_structural_surfaces_back_to_opaque_depth_writing_state():
    source = read(WORLD)
    for token in (
        'material.SetFloat("_Surface", 0f)',
        'material.SetFloat("_Blend", 0f)',
        'material.SetFloat("_SrcBlend", (float)BlendMode.One)',
        'material.SetFloat("_DstBlend", (float)BlendMode.Zero)',
        'material.SetFloat("_ZWrite", 1f)',
        'material.SetFloat("_AlphaClip", 0f)',
        'material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT")',
        'material.DisableKeyword("_ALPHAPREMULTIPLY_ON")',
        'material.DisableKeyword("_ALPHATEST_ON")',
        'material.SetOverrideTag("RenderType", "Opaque")',
        'material.renderQueue = (int)RenderQueue.Geometry',
        'material.SetShaderPassEnabled("ShadowCaster", true)',
        "ForceOpaque(palette.Water)",
    ):
        assert token in source

    for token in ("Glass", "Window", "SignalOrb", "Wisp", "Telegraph", "Vep", "Stimulus"):
        assert token in source
    assert "IsSemanticTransparentSurface" in source


def test_v22_closes_ground_roof_walls_and_far_world_boundary():
    source = read(WORLD)
    for token in (
        '"V22_Continuous_Ground_Underlay"',
        '"LowerRouteUnderlay"',
        '"AscentUnderlay"',
        '"BossPlateauUnderlay"',
        '"V22_Cavern_Vault"',
        '"CavernVaultUnderside"',
        '"WestCavernBackwall"',
        '"EastCavernBackwall"',
        '"SouthCavernBackwall"',
        '"NorthCavernBackwall"',
        '"V22_Traversal_Envelope"',
        '"WestBoundary"',
        '"EastBoundary"',
        '"SouthBoundary"',
        '"NorthBoundary"',
        "WorldSoulMeshLibraryV20.TerrainPatch",
        "MeshCollider roofCollider = roof.AddComponent<MeshCollider>()",
        "BoxCollider collider = go.AddComponent<BoxCollider>()",
        'CloneOpaqueMaterial("V22_VaultBasalt", palette.Basalt, true)',
        'material.SetFloat("_Cull", doubleSided ? (float)CullMode.Off : (float)CullMode.Back)',
    ):
        assert token in source

    for forbidden in (
        "RuntimeInitializeOnLoadMethod",
        "private void Update(",
        "private void LateUpdate(",
        "private void FixedUpdate(",
        "Time.deltaTime",
        "Time.unscaledDeltaTime",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "ReceiveDamage(",
        "UnityEngine.Random",
    ):
        assert forbidden not in source


def test_v22_ties_cavern_architecture_into_route_and_boss_chamber():
    source = read(WORLD)
    for token in (
        '"V22_Vault_Wall_Transitions"',
        '"VaultRib_',
        "ProductionMeshLibraryV09.PointedArch()",
        '"V22_Fractured_Signal_Chamber"',
        '"ChamberButtress_',
        '"ChamberCrownRib_',
        '"V22_Route_Luminance_Anchors"',
        '"RouteLumen_',
    ):
        assert token in source


def test_v22_boss_uses_most_of_the_chamber_and_reduces_projectile_noise():
    source = read(DUEL)
    v19 = read(V19)
    director = read(DIRECTOR)

    for field in (
        "phaseOnePreferredDistance",
        "phaseTwoPreferredDistance",
        "phaseThreePreferredDistance",
        "distanceBand",
        "phaseOneMoveSpeed",
        "phaseTwoMoveSpeed",
        "phaseThreeMoveSpeed",
        "retreatMultiplier",
        "orbitBias",
        "turnSharpness",
        "homeLeashRadius",
        "collisionProbeRadius",
        "postAttackRecovery",
        "orbitSideHoldSeconds",
    ):
        assert f'CanSet<FracturedSignalFirstBossV19, float>("{field}")' in source
        assert f"private float {field}" in v19

    for token in (
        'Set(_movement, "homeLeashRadius", 14.2f)',
        'Set(_movement, "collisionProbeRadius", 0.58f)',
        'Set(_movement, "orbitSideHoldSeconds", 1.55f)',
        'Set(_director, "phaseOneInterval", 2.35f)',
        'Set(_director, "phaseTwoInterval", 2.02f)',
        'Set(_director, "phaseThreeInterval", 1.72f)',
        'Set(_director, "radialCount", 6)',
        'Set(_director, "maxEchoes", 1)',
    ):
        assert token in source
    assert "private int radialCount = 12" in director


def test_v22_melee_is_readable_and_sword_contact_has_trigger_only_hull():
    source = read(DUEL)
    melee = read(MELEE)
    for field in (
        "engageDistance",
        "cleaveRange",
        "cleaveArcDegrees",
        "cleaveTelegraphPhaseOne",
        "cleaveTelegraphPhaseTwo",
        "cleaveTelegraphPhaseThree",
        "slamRadius",
        "slamTelegraphPhaseTwo",
        "slamTelegraphPhaseThree",
    ):
        assert f'CanSet<FracturedSignalMeleeDirector, float>("{field}")' in source
        assert f"private float {field}" in melee

    for token in (
        '"V22_BossCombatHull"',
        "CapsuleCollider collider = hull.AddComponent<CapsuleCollider>()",
        "collider.isTrigger = true",
        "collider.radius = 1.08f",
        "collider.height = 3.15f",
    ):
        assert token in source


def test_v22_stall_recovery_waits_for_commitment_and_preserves_real_pause_owners():
    source = read(DUEL)
    wisp = read(WISP)
    link = read(LINK)
    for token in (
        "AttackTelegraphed += OnAttackTelegraphed",
        "AttackFired += OnAttackFired",
        "Time.unscaledTime < _commitUntil",
        "stallWindowSeconds = 0.85f",
        "KickOrbitRecovery()",
        'GetField(\n                "_orbitSide"',
        'GetField(\n                "_nextOrbitSwap"',
        'GetField(\n                "_holdUntil"',
        "_wispIntermission.Active",
        "_linkContingency.Degraded",
        "_linkContingency.ParticipantStopped",
        "_director.SetExternalPause(false)",
        "_guardianInput.SetCombatActionsEnabled(true)",
    ):
        assert token in source

    assert "public bool Active => _active" in wisp
    assert "public bool Degraded => _degraded" in link
    assert "public bool ParticipantStopped => _participantStopped" in link
    assert source.index("if (wispOwnsPause || safetyOwnsPause)") < source.index("_director.SetExternalPause(false)")


def test_v22_has_native_unity_construction_smoke_and_pinned_guids():
    smoke = read(SMOKE)
    assert "V22DuelStability_CanBeConstructedByUnity" in smoke
    assert "AddComponent<FracturedSignalDuelStabilityV22>()" in smoke

    paths = (
        EDITOR / "WorldIntegrityV22Builder.cs.meta",
        COMBAT / "FracturedSignalDuelStabilityV22.cs.meta",
    )
    guids = []
    for path in paths:
        text = read(path)
        assert "fileFormatVersion: 2" in text
        guid = next(line.split(":", 1)[1].strip() for line in text.splitlines() if line.startswith("guid: "))
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
