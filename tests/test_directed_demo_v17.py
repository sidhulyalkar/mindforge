from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "unity/Assets/Mindforge/Presentation/MindforgeDirectedDemoV17.cs"
AUDIT = ROOT / "unity/Assets/Mindforge/Editor/MindforgeLatestReadinessAuditV17.cs"


def source() -> str:
    return RUNTIME.read_text(encoding="utf-8")


def section(text: str, start: str, end: str | None = None) -> str:
    value = text.split(start, 1)[1]
    if end is not None:
        value = value.split(end, 1)[0]
    return value


def test_v17_installs_only_on_the_canonical_demo_and_replaces_not_layers_hud():
    text = source()
    assert 'RootName = "Mindforge_DirectedDemo_V17"' in text
    assert "RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)" in text
    assert "FindObjectOfType<MindforgeDemoV11Marker>(true) == null" in text
    assert "legacyHud.enabled = false" in text
    assert "AddComponent<MindforgeGameplayCameraV17>()" in text
    assert "AddComponent<MindforgeTargetPresenceV17>()" in text
    assert "AddComponent<MindforgeDemoHudV17>()" in text


def test_v17_camera_is_closer_fixed_fov_and_hands_off_only_after_combat_returns():
    camera = section(source(), "public sealed class MindforgeGameplayCameraV17", "public sealed class MindforgeTargetPresenceV17")
    assert "FixedFov = 56f" in camera
    assert "FreeDistance = 6.65f" in camera
    assert "LockDistance = 7.75f" in camera
    assert "FreeShoulder = 0.58f" in camera
    assert "LockShoulder = 0.26f" in camera
    assert "_camera.fieldOfView = FixedFov" in camera
    assert "_camera.farClipPlane = 420f" in camera
    assert "if (_input != null && _input.CombatActionsEnabled) ActivateGameplayCamera();" in camera
    assert "_legacy.enabled = false" in camera
    assert "if (NeuralVisualFieldActive()) return;" in camera
    assert "_wisp.ResonanceWindowActive" in camera
    for forbidden in (
        "SetLocked(",
        "AcquireBest(",
        "Cycle(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "BeginWindow(",
        "EndWindow(",
        "ConfigureTiming(",
        "frequencyHz",
    ):
        assert forbidden not in camera


def test_v17_camera_collision_clearance_always_beats_preferred_framing_distance():
    camera = section(source(), "public sealed class MindforgeGameplayCameraV17", "public sealed class MindforgeTargetPresenceV17")
    resolver = section(camera, "private Vector3 ResolveCollision", "private bool IsGuardian")
    assert "Physics.SphereCastNonAlloc" in resolver
    assert "nearest - CollisionSafetyEpsilon" in resolver
    assert "nearest - CollisionPadding" in resolver
    assert "resolved = Mathf.Min(resolved, clearance)" in resolver
    assert "Mathf.Max(MinDistance, nearest" not in resolver
    assert "MinDistance = 2.65f" not in camera


def test_target_presence_is_non_authoritative_and_disappears_for_neural_visual_field():
    ring = section(source(), "public sealed class MindforgeTargetPresenceV17", "public sealed class MindforgeDemoHudV17")
    assert "LineRenderer" in ring
    assert "_ring.enabled = false" in ring
    assert "if (NeuralVisualFieldActive())" in ring
    assert "_wisp.ResonanceWindowActive" in ring
    assert "_targetLock.Target" in ring
    for forbidden in (
        "SetLocked(",
        "AcquireBest(",
        "Cycle(",
        "AddComponent<Collider>",
        "AddComponent<Rigidbody>",
        "NeuralEvent",
        "BeginWindow(",
        "EndWindow(",
    ):
        assert forbidden not in ring


def test_v17_hud_reads_combat_and_wisp_state_but_creates_no_authority():
    hud = section(source(), "public sealed class MindforgeDemoHudV17")
    for token in (
        "GUARDIAN",
        "THE FRACTURED SIGNAL",
        "BCI SIMULATION",
        "NEURAL LINK · READY",
        "NEURAL WINDOW  ·  KEEP GAZE ON BLUE / GREEN",
        "SIGHT  ·  BREAK POISE · PRESS THE OPENING",
        "GUARD  ·  COUNTER THE NEXT THREAT",
        "CONCORD  ·  EXECUTE THE OPENING",
        "T  ·  LOCK THE FRACTURED SIGNAL",
        "V HOLD  ·  CHANNEL WISP",
    ):
        assert token in hud
    for forbidden in (
        "Input.Get",
        "SetLocked(",
        "AcquireBest(",
        "Cycle(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "TryApply(",
        "ReceiveDamage",
    ):
        assert forbidden not in hud


def test_latest_readiness_report_explicitly_refuses_physical_ssvep_claim():
    audit = AUDIT.read_text(encoding="utf-8")
    assert 'schema = "mindforge.latest_readiness.v17"' in audit
    assert "physical_ssvep_qualified = false" in audit
    assert "10f" in audit and "12f" in audit
    assert "ExpectedRefreshHz" in audit
    assert "TimingHealthy" in audit
    assert "StimulusPairAvailable" in audit
    assert '"MindforgeGameplayCameraV17"' in audit
    assert '"MindforgeDemoCameraV11"' in audit
    assert '"MindforgeDemoHudV17"' in audit
    assert '"MindforgeDemoHudV11"' in audit
