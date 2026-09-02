import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"
EDITOR = UNITY / "Editor"
PRESENTATION = UNITY / "Presentation"
COMBAT = UNITY / "Combat"
LATEST = EDITOR / "MindforgeLatestEditorMenu.cs"
BUILDER = EDITOR / "ProfessionalEncounterV28Builder.cs"
ACQUIRE = EDITOR / "PublicAssetAcquisitionV28.cs"
CREATURE = PRESENTATION / "FracturedSignalCreaturePresentationV28.cs"
OCCLUSION = PRESENTATION / "MindforgeActorOcclusionGuardV28.cs"
MOVEMENT = COMBAT / "FracturedSignalFirstBossV19.cs"
MANIFEST = ROOT / "third_party" / "manifest.json"
PACKAGES = ROOT / "unity" / "Packages" / "manifest.json"
SMOKE = UNITY / "Tests" / "Editor" / "ProfessionalEncounterV28SmokeTests.cs"
DOC = ROOT / "docs" / "PROFESSIONAL_ENCOUNTER_V28.md"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.28 source: {path}"
    return path.read_text(encoding="utf-8")


def test_v28_is_latest_stage_after_v27():
    latest = read(LATEST)
    assert 'ProductVersion = "V0.28 Professional Creature + World Staging"' in latest
    v26 = latest.index("WorldRenderingV26Builder.ApplyOpenScene();")
    v27 = latest.index("CombatEmbodimentV27Builder.ApplyOpenScene();", v26)
    v28 = latest.index("ProfessionalEncounterV28Builder.ApplyOpenScene();", v27)
    assert v26 < v27 < v28
    assert "if (!ProfessionalEncounterV28Builder.PresentInOpenScene())" in latest
    assert 'RootName = "Mindforge_Professional_Encounter_V28"' in read(BUILDER)


def test_v28_pins_real_public_art_and_importer_instead_of_generating_another_beast():
    acquire = read(ACQUIRE)
    packages = read(PACKAGES)
    assert 'org.khronos.unitygltf' in packages
    assert 'release/2.20.0' in packages
    for token in (
        'GobkitCommit = "0d654ab3306515b1b63621a5c6548554034482dc"',
        'KayKitCommit = "b0ca9bd96a8072ab36a3a5464f00ed1e06a16d07"',
        'Rhino.glb',
        'banner_white.obj',
        'torch_mounted.obj',
        'chair.obj',
        'table_small_decorated_A.obj',
        'chest_gold.obj',
        'ComputeGitBlobSha1',
        'blob " + bytes.Length + "\\0"',
        'hash mismatch',
        'ModelImporterMaterialImportMode.None',
    ):
        assert token in acquire
    assert "RuntimeInitializeOnLoadMethod" not in acquire


def test_v28_public_art_provenance_is_asset_level_and_cc0_or_mit():
    manifest = json.loads(read(MANIFEST))
    entries = {e["id"]: e for e in manifest["entries"]}
    assert entries["khronos.unitygltf"]["license"] == "MIT"
    assert entries["khronos.unitygltf"]["source_ref"] == "release/2.20.0"
    assert entries["gobkit.free_assets"]["license"] == "CC0-1.0"
    assert entries["kaykit.dungeon_remastered"]["license"] == "CC0-1.0"

    assets = {a["id"]: a for a in manifest["binary_art_assets"]}
    expected = {
        "gobkit.rhino.v28": "f638b1cf00a6472192beb85b1a4162535bfc189e",
        "kaykit.banner_white.v28": "caf89af21053f2aa8081421d05b4d393f5b06fc7",
        "kaykit.torch_mounted.v28": "b29c171929a5995de358a35bad91c63f475cab2b",
        "kaykit.chair.v28": "0532f0992b7ce9cadcdc8921e3762760ca87441f",
        "kaykit.relic_table.v28": "f49a5780d08533f1fa17b7506b847b18be28a8f5",
        "kaykit.chest_gold.v28": "9d0bf6592588cca750940b0f0688e7158e2e51fa",
    }
    for asset_id, blob in expected.items():
        assert assets[asset_id]["source_git_blob_sha1"] == blob
        assert assets[asset_id]["license"] == "CC0-1.0"
        assert "Generated/V28" in assets[asset_id]["generated_path"]


def test_v28_boss_locomotion_owns_minimum_actor_separation_without_second_push_authority():
    movement = read(MOVEMENT)
    for token in (
        "minimumSeparationDistance = 2.75f",
        "emergencySeparationSpeed = 4.8f",
        "separationHysteresis = 0.22f",
        "public float MinimumSeparationDistance",
        "public bool EmergencySeparating",
        "if (TryEmergencySeparation()) return;",
        "preferred = Mathf.Max(PreferredDistance(), MinimumSeparationDistance + 0.9f)",
        "PositionClear(candidate, ignoreGuardian: true)",
        "ClampToHomeLeash",
    ):
        assert token in movement
    assert movement.index("if (TryEmergencySeparation()) return;") < movement.index("if (Time.unscaledTime < _holdUntil)")
    assert "ReceiveDamage(" not in movement
    assert "NeuralVisualFieldActive()" in movement


def test_v28_replaces_childish_procedural_beast_with_authored_rigged_animation():
    builder = read(BUILDER)
    creature = read(CREATURE)
    for token in (
        'FracturedSignalCreaturePresentationV28.RootName',
        'FracturedSignalCreaturePresentationV28.ModelName',
        'TargetCreatureLength = 3.70f',
        'FindClip(PublicAssetAcquisitionV28.RhinoPath, "idle")',
        'FindClip(PublicAssetAcquisitionV28.RhinoPath, "walk")',
        'FindClip(PublicAssetAcquisitionV28.RhinoPath, "attack")',
        'FindClip(PublicAssetAcquisitionV28.RhinoPath, "dead")',
        "NormalizeCreature",
        "Ground the actual rendered feet/belly",
        "DisableRetiredBossPresentation",
    ):
        assert token in builder

    for token in (
        "AnimationClip.SampleAnimation",
        "animator.applyRootMotion = false",
        "animator.enabled = false",
        "RestoreModelRootTransform",
        "movement.MovementActive",
        "director.AttackTelegraphed",
        "vitals.Died",
        "FracturedSignalBeastV27",
        "NeuralVisualFieldActive()",
    ):
        assert token in creature
    for forbidden in ("MovePosition(", "MoveRotation(", "ReceiveDamage(", "AddComponent<Collider", "AddComponent<Rigidbody"):
        assert forbidden not in creature


def test_v28_anatomical_sword_contact_matches_rendered_creature_body():
    builder = read(BUILDER)
    for token in (
        'CombatEnvelopeName = "V28_BeastCombatEnvelope"',
        '"V28_Hurt_Head"',
        '"V28_Hurt_Chest"',
        '"V28_Hurt_Midbody"',
        '"V28_Hurt_Rear"',
        "ComputeLocalRenderBounds",
        "BoxCollider collider = go.AddComponent<BoxCollider>()",
        "collider.isTrigger = true",
        'FindDeep(boss, "V22_BossCombatHull")',
        "old[i].enabled = false",
        "hurt.Length != 4",
    ):
        assert token in builder
    assert "AddComponent<Rigidbody>" not in builder


def test_v28_camera_guard_only_corrects_actual_target_occlusion_and_freezes_for_neural_field():
    source = read(OCCLUSION)
    for token in (
        "target.GetComponentsInChildren<Renderer>(true)",
        "targetBounds.SqrDistance(cameraPosition)",
        "corridorBlocked",
        "maximumLateralCorrection = 1.55f",
        "maximumLiftCorrection = 0.58f",
        "Never let the guard pull the camera closer",
        "NeuralVisualFieldActive()",
    ):
        assert token in source
    for forbidden in ("fieldOfView", "Input.GetAxis", "MovePosition(", "ReceiveDamage(", "SetExternalPause("):
        assert forbidden not in source


def test_v28_world_detail_is_socketed_sparse_and_never_clutters_traversal_or_boss_floor():
    builder = read(BUILDER)
    for token in (
        'WorldStagingName = "V28_Socketed_World_Staging"',
        "RouteClearHalfWidth = 3.15f",
        "BossClearRadius = 14.4f",
        '"V28_Sanctum_Dressing"',
        '"V28_Nave_Dressing"',
        '"V28_Cloister_Dressing"',
        '"V28_Nave_Torch_L_',
        '"V28_Nave_Banner_L_',
        '"V28_Cloister_Reliquary"',
        "GroundImportedProp",
        "StagedProps.Count < 16",
        "prop violates processional clearance",
        "prop violates boss clear radius",
        "staged props overlap excessively",
    ):
        assert token in builder
    assert "UnityEngine.Random" not in builder
    assert "DestroyImmediate(colliders[i])" in builder
    assert "DestroyImmediate(bodies[i])" in builder


def test_v28_smoke_docs_and_unique_guids_are_present():
    smoke = read(SMOKE)
    doc = read(DOC)
    for token in (
        "V28GitBlobHash_MatchesGitObjectContract",
        "V28ActorOcclusionGuard_CanBeConstructedByUnity",
        "V28PresentationTypes_AreRuntimeComponentsNotEditorStubs",
    ):
        assert token in smoke
    for phrase in (
        "minimum separation",
        "authored creature",
        "cc0",
        "socket",
        "hurt envelope",
        "neural",
    ):
        assert phrase in doc.lower()

    paths = (
        EDITOR / "PublicAssetAcquisitionV28.cs.meta",
        EDITOR / "ProfessionalEncounterV28Builder.cs.meta",
        PRESENTATION / "FracturedSignalCreaturePresentationV28.cs.meta",
        PRESENTATION / "MindforgeActorOcclusionGuardV28.cs.meta",
        UNITY / "Tests" / "Editor" / "ProfessionalEncounterV28SmokeTests.cs.meta",
    )
    guids = []
    for path in paths:
        text = read(path)
        assert "fileFormatVersion: 2" in text
        guid = next(line.split(":", 1)[1].strip() for line in text.splitlines() if line.startswith("guid: "))
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
