from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
QUARANTINE = ROOT / "unity/Assets/Mindforge/Editor/ProductionLegacyVisualQuarantineV09.cs"
HOOK = ROOT / "unity/Assets/Mindforge/Editor/ProductionArtAutoHookV09.cs"


def read(path: Path) -> str:
    assert path.exists(), path
    return path.read_text(encoding="utf-8")


def test_quarantine_removes_known_collider_free_legacy_layers():
    text = read(QUARANTINE)
    assert "NullWardVisualInfrastructureBuilder.DetailRootName" in text
    assert "NullWardArenaSetDressingV3Builder.WardRootName" in text
    assert "NullWardArenaSetDressingV3Builder.ArenaBackdropRootName" in text
    assert "GroundedWorldCompositionV2Builder.RootName" in text
    assert "root.gameObject.SetActive(false)" in text


def test_quarantine_fails_safe_when_a_visual_root_ever_acquires_collision():
    text = read(QUARANTINE)
    assert "GetComponentsInChildren<Collider>(true)" in text
    assert "ownsEnabledCollision" in text
    assert "HasEnabledCollider(renderer.gameObject)" in text
    assert "renderer.enabled = false" in text
    # It may inspect collider state to avoid hiding authority, but never mutates it.
    assert ".enabled = false;" in text
    assert "colliders[i].enabled = false" not in text
    assert "DestroyImmediate(colliders" not in text


def test_quarantine_preserves_semantic_navigation_visuals():
    text = read(QUARANTINE)
    for token in (
        '"Signal"', '"Intent"', '"Target"', '"Vep"', '"Gate"', '"Shortcut"',
        '"Landing"', '"Stair"', '"Ramp"', '"Bridge"', '"Conduit"', '"Threshold"',
    ):
        assert token in text
    assert "ShouldKeepSemanticRenderer" in text


def test_quarantine_has_zero_gameplay_or_neural_authority():
    text = read(QUARANTINE)
    for forbidden in (
        "AddComponent<JourneyEnemyController>",
        "AddComponent<CombatantVitals>",
        "AddComponent<Rigidbody>",
        "TakeDamage",
        "Input.Get",
        "UdpNeuralReceiver",
        "NeuralEvent",
        "WorldStateLedger",
        "PlayerProfileSave",
    ):
        assert forbidden not in text


def test_production_hook_runs_quarantine_before_local_art_replacements():
    text = read(HOOK)
    quarantine = text.index("ProductionLegacyVisualQuarantineV09.ApplyOpenScene()")
    external = text.index("ExternalArtReplacementV09.ApplyOpenScene()")
    assert quarantine < external


def test_quarantine_script_has_unique_unity_meta_guid():
    meta = Path(str(QUARANTINE) + ".meta")
    assert meta.exists()
    guid = next(
        line.split(":", 1)[1].strip()
        for line in meta.read_text(encoding="utf-8").splitlines()
        if line.startswith("guid: ")
    )
    matches = []
    for candidate in (ROOT / "unity/Assets").rglob("*.meta"):
        if f"guid: {guid}" in candidate.read_text(encoding="utf-8", errors="ignore"):
            matches.append(candidate)
    assert matches == [meta]
