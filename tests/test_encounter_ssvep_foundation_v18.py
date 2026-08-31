from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    path = UNITY.joinpath(*parts)
    assert path.exists(), f"missing V0.18 source: {path}"
    return path.read_text(encoding="utf-8")


def test_target_lock_exposes_exact_conventional_encounter_assist_without_neural_authority():
    text = read("Combat", "GuardianTargetLock.cs")
    for token in (
        "TryLockTarget(Transform candidate",
        "TargetChangedWithReason",
        "LastChangeReason",
        '"conventional_player_input"',
        '"encounter_assist"',
        "TargetAvailable(candidate)",
        "HorizontalDistanceTo(candidate) > Mathf.Max(lockRange, breakRange)",
    ):
        assert token in text

    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "GazeAttentionRouter",
        "TryApply(",
        "ReceiveDamage(",
        "BeginWindow(",
        "EndWindow(",
    ):
        assert forbidden not in text


def test_encounter_assist_prefers_boss_and_selected_high_information_enemies_only():
    text = read("Combat", "EncounterTargetAssistV18.cs")
    for token in (
        "FracturedSignalDirector",
        "JourneyEnemyArchetype.SignalWarden",
        "JourneyEnemyArchetype.ChromePenitent",
        "JourneyEnemyArchetype.NullSentry",
        "enemy.Armed",
        "manualReleaseGraceSeconds = 8.0f",
        "NeuralVisualFieldActive()",
        "targetLock.TryLockTarget(candidate, reason)",
        '"boss_encounter_auto_lock"',
        '"priority_enemy_auto_lock"',
        '"TARGET_LOCK_ASSIST"',
    ):
        assert token in text

    # Low-level teaching enemies remain manual so the game does not become an aim magnet.
    priority_block = text[text.index("private static int PriorityFor") : text.index("private bool NeuralVisualFieldActive")]
    assert "JourneyEnemyArchetype.Hollow" not in priority_block
    assert "JourneyEnemyArchetype.Shardcaster" not in priority_block

    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "GazeAttentionRouter",
        "AuraBuffController",
        "ReceiveDamage(",
        "TryApply(",
        "BeginWindow(",
        "EndWindow(",
    ):
        assert forbidden not in text


def test_ssvep_focus_backdrop_is_noncoded_static_and_neural_interval_only():
    text = read("Presentation", "SsvepFocusBackdropV18.cs")
    for token in (
        "wisp.CalibrationStimuliActive || wisp.ResonanceWindowActive",
        "ShadowCastingMode.Off",
        "LightProbeUsage.Off",
        "ReflectionProbeUsage.Off",
        'RootName = "Mindforge_SsvepFocusBackdrop_V18"',
    ):
        assert token in text
    assert "local contrast" in text.lower()

    # Trigonometry is allowed to build the static disc mesh. Runtime presentation must not
    # periodically modulate that plate, or it becomes an uncontrolled visual tag.
    runtime = text[text.index("private void LateUpdate()") : text.index("private static float ResolveWorldDiameter")]
    for forbidden in ("Mathf.Sin", "Mathf.Cos", "Time.time", "Time.unscaledTime"):
        assert forbidden not in runtime

    for forbidden in (
        "_EmissionColor",
        "EnableKeyword(\"_EMISSION\")",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "TryApply(",
        "BeginWindow(",
        "EndWindow(",
        "SetLocked(",
        "TryLockTarget(",
    ):
        assert forbidden not in text


def test_ssvep_dataset_stream_records_actual_render_context_but_has_zero_authority():
    text = read("Telemetry", "SsvepDatasetTelemetryV18.cs")
    for token in (
        'SchemaV1 = "mindforge.ssvep_observation.v1"',
        "observerPort = 19746",
        "stimulus_epoch",
        "target_lock_reason",
        "actual_separation_deg",
        "sight_actual_diameter_deg",
        "guard_actual_diameter_deg",
        "camera_speed_m_s",
        "camera_angular_speed_deg_s",
        "display_timing_healthy",
        "HorizontalDistance(_guardian, target)",
        "MindforgeSessionContext.GameSessionId",
        "focusBackdrop.Active",
    ):
        assert token in text

    for forbidden in (
        "SetLocked(",
        "TryLockTarget(",
        "AcquireBest(",
        "TryApply(",
        "ReceiveDamage(",
        "BeginWindow(",
        "EndWindow(",
        "NeuralEvent",
        "UdpNeuralReceiver",
    ):
        assert forbidden not in text


def test_v18_runtime_scripts_have_pinned_unique_meta_guids():
    paths = (
        UNITY / "Combat" / "EncounterTargetAssistV18.cs.meta",
        UNITY / "Presentation" / "SsvepFocusBackdropV18.cs.meta",
        UNITY / "Telemetry" / "SsvepDatasetTelemetryV18.cs.meta",
    )
    guids = []
    for path in paths:
        text = path.read_text(encoding="utf-8")
        assert "fileFormatVersion: 2" in text
        line = next(line for line in text.splitlines() if line.startswith("guid: "))
        guid = line.split(":", 1)[1].strip()
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
