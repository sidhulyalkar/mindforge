from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"
EDITOR = UNITY / "Editor"
COMBAT = UNITY / "Combat"
LATEST = EDITOR / "MindforgeLatestEditorMenu.cs"
V11 = EDITOR / "MindforgeDemoV11Builder.cs"
V20 = EDITOR / "WorldSoulV20Builder.cs"
V21 = EDITOR / "WorldCohesionV21Builder.cs"
V22 = EDITOR / "WorldIntegrityV22Builder.cs"
V23 = EDITOR / "WorldFoundationV23Builder.cs"
V24 = EDITOR / "WorldCathedralV24Builder.cs"
MOBILITY = COMBAT / "FracturedSignalArenaMobilityV21.cs"
LIFECYCLE = UNITY / "Tests" / "Editor" / "MindforgeUnityLifecycleSmokeTests.cs"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.21 source: {path}"
    return path.read_text(encoding="utf-8")


def test_v21_remains_the_cohesion_stage_between_world_soul_and_v22_integrity():
    latest = read(LATEST)
    assert 'ProductVersion = "V0.25 Sensory Fidelity + Data Cathedral"' in latest
    v11_i = latest.index("MindforgeDemoV11Builder.BuildDemoScene(controllerOnlyByDefault);")
    v20_i = latest.index("WorldSoulV20Builder.ApplyOpenScene();", v11_i)
    v21_i = latest.index("WorldCohesionV21Builder.ApplyOpenScene();", v20_i)
    v22_i = latest.index("WorldIntegrityV22Builder.ApplyOpenScene();", v21_i)
    v23_i = latest.index("WorldFoundationV23Builder.ApplyOpenScene();", v22_i)
    v24_i = latest.index("WorldCathedralV24Builder.ApplyOpenScene();", v23_i)
    v25_i = latest.index("SensoryFidelityV25Builder.ApplyOpenScene();", v24_i)
    assert v11_i < v20_i < v21_i < v22_i < v23_i < v24_i < v25_i
    assert "EnsureWorldLayersOpenScene();" in latest
    assert 'RootName = "Mindforge_World_Integrity_V22"' in read(V22)
    assert 'RootName = "Mindforge_World_Foundation_V23"' in read(V23)
    assert 'RootName = "Mindforge_White_Cathedral_V24"' in read(V24)


def test_recording_driven_arena_is_materially_larger_and_center_is_flat():
    source = read(V21)
    legacy = read(V11)
    assert "const float radius = 13.0f" in legacy
    assert "new Vector3(25f, 0.72f, 24f)" in legacy
    assert 'Block("FractureInnerDais"' in legacy

    for token in (
        "ArenaFloorWidth = 36f",
        "ArenaFloorDepth = 34f",
        "ArenaWallRadius = 18.3f",
        "floor.localScale = new Vector3(ArenaFloorWidth, 0.72f, ArenaFloorDepth)",
        "dais.localScale = new Vector3(12.5f, 0.08f, 12.5f)",
        "DestroyImmediate(daisCollider)",
        "child.localScale = new Vector3(8.1f, 4.5f, 0.86f)",
        "16.4f",
        "PushWorldSoulCraterOutward",
        "Mathf.Max(21.2f",
        "Mathf.Max(19.4f",
    ):
        assert token in source


def test_v21_boss_spacing_matches_the_new_arena_and_fails_closed():
    source = read(MOBILITY)
    v19 = read(COMBAT / "FracturedSignalFirstBossV19.cs")

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
        "orbitSideHoldSeconds",
        "homeLeashRadius",
        "collisionProbeRadius",
        "postAttackRecovery",
    ):
        assert f'CanSet<float>("{field}")' in source
        assert f'Set("{field}"' in source
        assert f"private float {field}" in v19

    for token in (
        'Set("phaseOnePreferredDistance", 5.25f)',
        'Set("phaseTwoPreferredDistance", 6.10f)',
        'Set("phaseThreePreferredDistance", 5.35f)',
        'Set("orbitBias", 0.80f)',
        'Set("orbitSideHoldSeconds", 2.35f)',
        'Set("homeLeashRadius", 9.0f)',
        'Set("collisionProbeRadius", 0.78f)',
        "movement field contract changed; arena mobility profile applied nothing",
    ):
        assert token in source

    assert source.index("if (!fieldsAvailable)") < source.index('Set("phaseOnePreferredDistance"')
    for forbidden in (
        "FixedUpdate(",
        "MovePosition(",
        "MoveRotation(",
        "ReceiveDamage(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "TryApply(",
        "SetExternalPause(",
    ):
        assert forbidden not in source


def test_v21_runtime_adapter_is_in_native_unity_lifecycle_smoke():
    smoke = read(LIFECYCLE)
    assert "V21ArenaMobilityAdapter_CanBeConstructedByUnity" in smoke
    assert "AddComponent<FracturedSignalArenaMobilityV21>()" in smoke


def test_v21_patina_is_static_collider_free_world_evidence():
    source = read(V21)
    for token in (
        '"V21_Surface_Transitions"',
        '"V21_Fracture_Arena_Patina"',
        '"V21_Foreground_Ecology"',
        '"V21_Near_City_Facades"',
        '"V21_Landmark_Composition"',
        "BuildContactScatter",
        "BuildFern",
        "BuildFacadeHouse",
        '"RoofInner"',
        '"RoofOuter"',
        '"BossOuterArchLeft"',
        '"BossOuterArchRight"',
        '"ForgeOfferingStone_',
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
        "AddComponent<Collider",
        "AddComponent<Rigidbody",
        "UnityEngine.Random",
    ):
        assert forbidden not in source
    assert "GameObject.CreatePrimitive" in source
    assert "DestroyImmediate(collider)" in source


def test_v21_editor_and_runtime_scripts_have_pinned_unique_unity_guids():
    paths = (
        EDITOR / "WorldCohesionV21Builder.cs.meta",
        COMBAT / "FracturedSignalArenaMobilityV21.cs.meta",
    )
    guids = []
    for path in paths:
        text = read(path)
        assert "fileFormatVersion: 2" in text
        guid = next(line.split(":", 1)[1].strip() for line in text.splitlines() if line.startswith("guid: "))
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
