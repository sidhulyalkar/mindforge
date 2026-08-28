from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_one_click_showcase_applies_visual_v2_after_authoritative_null_ward_build():
    menu = read("Editor", "ShowcaseEditorMenu.cs")
    null_ward = menu.index("NullWardSceneBuilder.BuildOpenScene();")
    visual_v2 = menu.index("NullWardVisualInfrastructureBuilder.ApplyOpenScene();")
    gate = menu.index("CompetitionGateValidator.ValidateAndWrite(false);")

    assert null_ward < visual_v2 < gate
    assert "FirstJourneySceneBuilder.BuildOpenScene" not in menu


def test_visual_v2_detail_is_collider_free_shared_material_and_static_optimized():
    source = read("Editor", "NullWardVisualInfrastructureBuilder.cs")

    for token in (
        'DetailRootName = "Mindforge_NullWard_StaticDetail_V2"',
        '"ArenaBasalt"',
        '"ObsidianArchitecture"',
        '"GuardianMetal"',
        '"AetherCyan"',
        '"WispVerdant"',
        '"FracturedRing"',
        "StaticEditorFlags.BatchingStatic",
        "StaticEditorFlags.OccluderStatic",
        "StaticEditorFlags.OccludeeStatic",
        "StaticEditorFlags.ReflectionProbeStatic",
        "UnityEngine.Object.DestroyImmediate(collider)",
        "renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off",
    ):
        assert token in source

    # URP Lit is normally SRP-Batcher compatible. Do not churn shared material assets
    # by blindly toggling GPU instancing, which Unity documents as a separate path that
    # does not combine with the ordinary SRP Batcher GameObject path.
    assert "enableInstancing" not in source

    # Dynamic/semantic renderers must never be frozen into environment batching.
    for token in (
        "renderer is LineRenderer",
        "renderer is TrailRenderer",
        "renderer is ParticleSystemRenderer",
        "renderer.GetComponentInParent<CombatantVitals>() != null",
        "renderer.GetComponentInParent<JourneyGate>() != null",
        "renderer.GetComponentInParent<FracturedEchoNode>() != null",
    ):
        assert token in source

    # The V2 pass consumes the central material vocabulary rather than minting a new one.
    assert "CinematicMaterialAuthoring.Load(name)" in source
    assert "AssetDatabase.CreateAsset(material" not in source
    assert "new Material(" not in source

    for forbidden in (
        "ReceiveDamage(",
        "TryLightAttack(",
        "SetGuardHeld(",
        "RequestDash(",
        "TryApply(",
        "CalibrationReady =",
        "Award(",
    ):
        assert forbidden not in source


def test_visual_v2_provides_independent_production_art_anchors_for_every_zone():
    builder = read("Editor", "NullWardVisualInfrastructureBuilder.cs")
    profile = read("Presentation", "NullWardArtProfile.cs")
    authoring = read("Editor", "NullWardArtProfileAuthoring.cs")

    for anchor in (
        "NullWard_ArtAnchor_MemoryForge",
        "NullWard_ArtAnchor_Causeway",
        "NullWard_ArtAnchor_Market",
        "NullWard_ArtAnchor_Maintenance",
        "NullWard_ArtAnchor_Cathedral",
    ):
        assert anchor in builder

    for binding in (
        "memoryForge",
        "synapseCauseway",
        "nullMarket",
        "maintenanceLoop",
        "signalCathedral",
    ):
        assert f"public ZoneBinding {binding}" in profile

    assert 'MenuItem("Mindforge/Showcase/Open Null Ward Art Binding Profile"' in authoring
    assert '"Assets/Mindforge/Resources/Cinematic/NullWardArtProfile.asset"' in authoring


def test_imported_room_art_is_visual_payload_and_cannot_smuggle_authority_or_custom_scripts():
    installer = read("Presentation", "NullWardArtOverrideInstaller.cs")

    assert 'Resources.Load<NullWardArtProfile>("Cinematic/NullWardArtProfile")' in installer
    assert "FindObjectsOfType<Transform>(true)" in installer
    for token in (
        "GetComponentsInChildren<Rigidbody>(true)",
        "GetComponentsInChildren<Collider>(true)",
        "GetComponentsInChildren<Joint>(true)",
        "GetComponentsInChildren<Rigidbody2D>(true)",
        "GetComponentsInChildren<Collider2D>(true)",
        "GetComponentsInChildren<Joint2D>(true)",
        "GetComponentsInChildren<Camera>(true)",
        "GetComponentsInChildren<AudioListener>(true)",
        "GetComponentsInChildren<MonoBehaviour>(true)",
        "if (behaviour != null) Destroy(behaviour)",
        "hideProceduralDetailForBoundZones",
    ):
        assert token in installer

    # Room-art binding never invokes game or neural authority.
    for forbidden in (
        "ReceiveDamage(",
        "TryLightAttack(",
        "SetGuardHeld(",
        "RequestDash(",
        "TryApply(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
        "CalibrationReady =",
        "Award(",
    ):
        assert forbidden not in installer


def test_budget_audit_is_zone_aware_and_tracks_real_scene_cost_proxies():
    audit = read("Editor", "PresentationBudgetAudit.cs")

    # Schema stays additive-compatible with the existing presentation-budget artifact.
    assert 'schema = "mindforge.presentation_budget.v1"' in audit
    for token in (
        "estimated_triangles",
        "transparent_material_slots",
        "batching_static_renderers",
        "null_ward_zones",
        '"memory_forge"',
        '"synapse_causeway"',
        '"null_market"',
        '"maintenance_loop"',
        '"signal_cathedral"',
        "mesh.GetIndexCount(i)",
        "GameObjectUtility.GetStaticEditorFlags",
        "material.renderQueue >= 3000",
        "zone.particle_capacity",
        "zone.shadow_lights",
    ):
        assert token in audit

    for forbidden in (
        "QualitySettings.SetQualityLevel",
        "ScalableBufferManager",
        "Time.timeScale =",
        "TryApply(",
        "ReceiveDamage(",
    ):
        assert forbidden not in audit


def test_cinematic_runtime_rebinding_preserves_coded_vep_renderer_contract():
    source = read("Presentation", "CinematicRuntimeMaterialOverride.cs")

    assert "renderer.GetComponentInParent<VepAuraStimulus>() != null" in source
    assert "line.GetComponentInParent<VepAuraStimulus>() != null" in source
    assert "if (selected == null) continue;" in source

    # Renderer-wide settings are applied only after the selected-material gate.
    selected_gate = source.index("if (selected == null) continue;")
    assert selected_gate < source.index("renderer.shadowCastingMode = ShadowCastingMode.On")
    assert selected_gate < source.index("renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox")

    for forbidden in (
        ".Configure(10",
        ".Configure(12",
        "frequencyHz =",
        "TryApply(",
        "ReceiveDamage(",
    ):
        assert forbidden not in source
