from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_pointed_arch_polish_follows_shared_topology_and_adds_no_collision():
    source = read("Presentation", "NeuralGothicArchPolishV07.cs")

    for token in (
        'Revision = "NEURAL_GOTHIC_ARCH_POLISH_V07"',
        'RootName = "NeuralGothicArchPolish_V07"',
        "GeneratedWorldCellV07[] cells",
        "Dictionary<Vector2Int, GeneratedWorldCellV07>",
        "cell.IsOpen(direction)",
        "neighbor.IsOpen(opposite)",
        "new Vector2Int(0, 1)",
        "new Vector2Int(1, 0)",
        "PointedArch_Left",
        "PointedArch_Right",
        "PointedArchSignal_Left",
        "PointedArchSignal_Right",
        "PointedArch_Key",
        "maxSharedArches = 24",
    ):
        assert token in source

    assert "Collider collider = go.GetComponent<Collider>()" in source
    assert "DestroyImmediate(collider)" in source

    for forbidden in (
        "Rigidbody",
        "WorldStateLedger",
        "WorldSignalBus",
        "WorldInteractionSourceV1",
        "GuardianControlAction",
        "Input.GetKey",
        "PlayerProfileSave",
        "CombatantVitals",
    ):
        assert forbidden not in source


def test_bci_decorative_lighting_gate_is_scoped_monotonic_and_conservative():
    source = read("Presentation", "BciSafeDecorativeLightingV07.cs")

    for token in (
        "Transform decorativeLightRoot",
        "AwakeningCalibrationDirector calibration",
        "controllerOnlyScale = 1f",
        "calibratedBciScale = 0.38f",
        "calibration.ControllerOnlyQualificationActive",
        "decorativeLightRoot.GetComponentsInChildren<Light>(true)",
        "light.intensity = _authoredIntensities[i] * safe",
        "Time.unscaledDeltaTime",
        "Mathf.Exp(",
    ):
        assert token in source

    # Editor scene construction must preserve authored intensities rather than serializing
    # a dimmed BCI baseline that controller-only mode can never recover from.
    assert "if (Application.isPlaying) ApplyImmediate(ResolveTargetScale())" in source
    assert "else _currentScale = 1f" in source

    # No rhythmic modulation belongs in decorative lighting. The only temporal behavior is a
    # monotonic transition between two static scales when qualification mode changes.
    for forbidden in (
        "Mathf.Sin",
        "Mathf.Cos",
        "Mathf.PingPong",
        "Time.time",
        "frequency",
        "VepAuraStimulus",
        "Renderer.enabled",
        "QualitySettings",
    ):
        assert forbidden not in source


def test_readability_builder_layers_after_v07_and_reaudits_scene_budget():
    builder = read("Editor", "WorldV07ReadabilityPolishBuilder.cs")
    showcase = read("Editor", "ShowcaseEditorMenu.cs")

    for token in (
        'Revision = "NEURAL_GOTHIC_READABILITY_POLISH_V07"',
        "EditorSceneLookup.FindIncludingInactive(WorldV07Builder.RootName)",
        'DecorativeLightRootName = "World_Light_Rhythm_V07"',
        "annex.AddComponent<NeuralGothicArchPolishV07>()",
        "v07Root.AddComponent<BciSafeDecorativeLightingV07>()",
        "FindObjectOfType<AwakeningCalibrationDirector>(true)",
        "showcaseScale: 1f",
        "bciScale: 0.38f",
        "audit.Evaluate(true)",
    ):
        assert token in builder

    v07 = showcase.index("WorldV07Builder.ApplyOpenScene();")
    polish = showcase.index("WorldV07ReadabilityPolishBuilder.ApplyOpenScene();")
    validation = showcase.index("CompetitionGateValidator.ValidateAndWrite(false);")
    assert v07 < polish < validation


def test_readability_polish_preserves_existing_v05_and_v06_authorities():
    arch = read("Presentation", "NeuralGothicArchPolishV07.cs")
    lighting = read("Presentation", "BciSafeDecorativeLightingV07.cs")
    builder = read("Editor", "WorldV07ReadabilityPolishBuilder.cs")
    combined = arch + lighting + builder

    for forbidden in (
        "WorldShortcutInteractionV06",
        "PersistentPickupInteractionV06",
        "PersistentShrineInteractionV06",
        "NpcDialogueInteractionV06",
        "GuardianInteractionRouterV1",
        "PlayerProfileSaveV06",
        "MindforgeConstraintCollapse",
        "Generate()",
    ):
        assert forbidden not in combined

    assert "No collider, topology, interaction, persistence, combat or coded-stimulus authority was added" in builder


def test_readability_polish_csharp_guids_are_repository_unique():
    metas = (
        UNITY / "Presentation" / "BciSafeDecorativeLightingV07.cs.meta",
        UNITY / "Presentation" / "NeuralGothicArchPolishV07.cs.meta",
        UNITY / "Editor" / "WorldV07ReadabilityPolishBuilder.cs.meta",
    )

    expected = []
    for path in metas:
        text = path.read_text(encoding="utf-8")
        assert "fileFormatVersion: 2" in text
        guid = next(line for line in text.splitlines() if line.startswith("guid: ")).split(":", 1)[1].strip()
        assert len(guid) == 32
        expected.append(guid)
    assert len(expected) == len(set(expected))

    all_guids = {}
    for path in (ROOT / "unity" / "Assets").rglob("*.meta"):
        text = path.read_text(encoding="utf-8", errors="ignore")
        for line in text.splitlines():
            if not line.startswith("guid: "):
                continue
            guid = line.split(":", 1)[1].strip()
            all_guids.setdefault(guid, []).append(path)
            break

    for guid in expected:
        assert len(all_guids.get(guid, [])) == 1, (guid, all_guids.get(guid, []))
