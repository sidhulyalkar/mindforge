import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_gaze_event_contract_is_bounded_and_contains_no_raw_biometric_payloads():
    schema = json.loads((ROOT / "contracts" / "gaze_event.v1.schema.json").read_text(encoding="utf-8"))
    assert schema["additionalProperties"] is False
    assert schema["properties"]["schema"]["const"] == "mindforge.gaze_event.v1"
    assert schema["properties"]["x"]["minimum"] == 0.0
    assert schema["properties"]["x"]["maximum"] == 1.0
    assert schema["properties"]["y"]["minimum"] == 0.0
    assert schema["properties"]["y"]["maximum"] == 1.0
    assert schema["properties"]["coordinate_origin"]["enum"] == ["top_left", "bottom_left"]

    serialized = json.dumps(schema).lower()
    for forbidden in ("eye_image", "scene_video", "raw_eeg", "pupil_frame", "camera_frame"):
        assert forbidden not in serialized


def test_udp_gaze_receiver_is_loopback_latest_only_and_non_authoritative():
    source = read("Gaze", "UdpGazeReceiver.cs")
    for token in (
        "IPAddress.Loopback",
        "ConcurrentQueue<ReceivedPacket>",
        "Stopwatch.GetTimestamp()",
        "maxPacketQueueAgeSeconds",
        "maxQueuedPackets",
        "maxDrainPerFrame",
        "sample.seq <= _lastSeenSequence",
        "GazeEvent newest = null",
        "SampleReceived?.Invoke(newest)",
        "port = 19746",
    ):
        assert token in source

    for forbidden in (
        "ReceiveDamage(",
        "TryLightAttack(",
        "RequestDash(",
        "SetGuardHeld(",
        "SetLocked(",
        "NeuralEvent",
    ):
        assert forbidden not in source


def test_attention_router_requires_quality_dwell_and_semantic_enemy_hit():
    source = read("Gaze", "GazeAttentionRouter.cs")
    for token in (
        "sample.IsUsable(minimumConfidence)",
        "ViewportPointToRay",
        "Physics.RaycastAll",
        "CombatTeam.Enemy",
        "targetDwellSeconds = 0.12f",
        "targetReleaseGraceSeconds = 0.18f",
        "sampleTimeoutSeconds = 0.55f",
        "TryGetStableEnemy",
        "SuggestedCombatTargetChanged",
    ):
        assert token in source

    for forbidden in (
        "ReceiveDamage(",
        "TryLightAttack(",
        "FirePulse(",
        "RequestDash(",
        "SetGuardHeld(",
        "SetLocked(",
        "Input.GetKeyDown",
    ):
        assert forbidden not in source


def test_gaze_target_assist_refines_only_an_existing_player_confirmation():
    assist = read("Gaze", "GazeTargetLockAssist.cs")
    lock = read("Combat", "GuardianTargetLock.cs")

    for token in (
        "[DefaultExecutionOrder(10000)]",
        "Input.GetKeyDown(targetLock.ToggleKey)",
        "!targetLock.Locked",
        "attention.TryGetStableEnemy",
        "targetLock.Cycle(1)",
        "HashSet<Transform>",
    ):
        assert token in assist

    for forbidden in (
        "targetLock.SetLocked(",
        "ReceiveDamage(",
        "TryLightAttack(",
        "FirePulse(",
        "RequestDash(",
        "SetGuardHeld(",
        "NeuralEvent",
    ):
        assert forbidden not in assist

    # The canonical lock component remains independent of gaze and neural transports.
    assert "Mindforge.Gaze" not in lock
    assert "GazeEvent" not in lock
    assert "UdpGazeReceiver" not in lock
    assert "NeuralEvent" not in lock


def test_gaze_platform_bootstrap_is_optional_and_idempotent():
    bootstrap = read("Gaze", "MindforgeGazePlatformBootstrap.cs")
    for token in (
        "RuntimeInitializeLoadType.AfterSceneLoad",
        'GameObject.Find("MindforgeGazePlatform")',
        "FindObjectOfType<UdpGazeReceiver>()",
        "FindObjectOfType<GazeAttentionRouter>()",
        "router.Bind(receiver)",
        "FindObjectsOfType<GuardianTargetLock>(true)",
        "GetComponent<GazeTargetLockAssist>()",
        "assist.Configure(targetLock, router)",
    ):
        assert token in bootstrap


def test_hardware_bridge_has_simulation_replay_and_live_neon_surface_modes():
    tool = (ROOT / "tools" / "mindforge_gaze.py").read_text(encoding="utf-8")
    for token in (
        'SCHEMA = "mindforge.gaze_event.v1"',
        "DEFAULT_PORT = 19746",
        'source_mode="simulated_pointer"',
        'source_mode="simulated_script"',
        'source_mode="gaze_replay"',
        'source_mode="live_pupil_neon_surface"',
        "discover_one_device",
        "GazeMapper",
        "receive_matched_scene_video_frame_and_gaze",
        "mapper.process_frame(frame, gaze)",
        "marker_generator.generate_marker",
        'coordinate_origin="top_left"',
    ):
        assert token in tool

    assert "raw eye" in tool.lower()
    assert "socket.SOCK_DGRAM" in tool


def test_gaze_plan_preserves_hybrid_authority_and_next_promotion_gates():
    plan = (ROOT / "docs" / "GAZE_ATTENTION_PLATFORM_V11.md").read_text(encoding="utf-8")
    for token in (
        "gaze-first hybrid",
        "Eyes answer WHERE. Hands answer NOW. Brain answers WHICH MODE / TRANSFORMATION.",
        "Gaze is initially **advisory, not authoritative**",
        "pupil-labs/real-time-screen-gaze",
        "pupil-labs/gaze-control",
        "pupil-labs/neon-xr",
        "V0.11A: qualify target preference",
        "V0.11B: gaze-aware contextual interaction",
        "V0.11C: BCI + gaze composition",
        "V0.11D: gamebuilding / UX analytics",
    ):
        assert token in plan
