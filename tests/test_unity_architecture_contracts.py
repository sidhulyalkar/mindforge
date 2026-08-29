from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_udp_receiver_is_threaded_bounded_and_latest_authoritative():
    src = read("NeuralBridge", "UdpNeuralReceiver.cs")
    assert "ConcurrentQueue<ReceivedPacket>" in src
    assert 'Name = "Mindforge-Neural-UDP"' in src
    assert "maxQueuedPackets" in src
    assert "maxDrainPerFrame" in src
    assert "maxPacketQueueAgeSeconds" in src
    assert "Stopwatch.GetTimestamp()" in src
    assert "EvidenceReceived" in src
    assert "participantStop" in src
    assert "DrainPending();" in src
    assert "while (drained < maxDrainPerFrame" in src


def test_cross_process_monotonic_clock_is_not_used_as_unity_packet_age():
    src = read("NeuralBridge", "UdpNeuralReceiver.cs")
    assert "PacketAgeSeconds(ReceivedPacket packet)" in src
    assert "packet.ReceiveTicks" in src
    assert "evt.monotonic_ns" not in src


def test_hitstop_is_extendable_and_does_not_recapture_zero_timescale():
    src = read("Combat", "HitStopController.cs")
    assert "_freezeUntil" in src
    assert "System.Math.Max(_freezeUntil" in src
    assert "if (_routine != null) return;" in src
    assert "_ownsTimeScale" in src
    assert "WaitForSecondsRealtime" not in src


def test_sticky_concord_is_the_combat_authority():
    guardian = read("Combat", "GuardianCombatController.cs")
    bloom = read("Combat", "GravityBloomAbility.cs")
    assert "auras.ConcordActive" in guardian
    assert "auras.ConcordActive" in bloom
    assert "twinEclipseHitStop" in bloom


def test_decoder_feedback_does_not_mutate_the_vep_core():
    feedback = read("SoulWisp", "NeuralAuraFeedback.cs")
    stimulus = read("SoulWisp", "VepAuraStimulus.cs")
    assert "EvidenceReceived" in feedback
    assert "VepAuraStimulus" not in feedback
    assert "Time.realtimeSinceStartupAsDouble" in stimulus
    assert "frequencyHz" in stimulus


def test_haptics_are_post_decision_not_evidence_lock_rumble():
    src = read("SoulWisp", "NeuralHapticFeedback.cs")
    assert "evt.IsSelection" in src
    assert "ConcordTriggered" in src
    assert "EvidenceReceived" not in src


def test_reserved_neural_colors_are_not_projectile_identity_colors():
    palette = read("Presentation", "CombatVisualPalette.cs")
    projectile = read("Combat", "MindforgeProjectile.cs")
    assert "sightTarget" in palette and "guardTarget" in palette
    assert "hostilePrimary" in palette and "hostileHeavy" in palette
    assert "guardianPrimary" in projectile
    assert "hostilePrimary" in projectile
    assert "reflected" in projectile
    assert "sightTarget" not in projectile
    assert "guardTarget" not in projectile


def test_signal_break_is_both_visual_rest_and_sensory_rest():
    boss = read("Combat", "FracturedSignalDirector.cs")
    reward = read("Combat", "SignalBreakReward.cs")
    assert "RestStimuli(signalBreakVisualRestSeconds)" in boss
    assert "presentation?.SignalBreak" in reward


def test_boss_has_cognitive_pacing_and_echo_pressure():
    boss = read("Combat", "FracturedSignalDirector.cs")
    assert "phaseOneTelegraph" in boss
    assert "phaseTwoTelegraph" in boss
    assert "phaseThreeTelegraph" in boss
    assert "SpawnEchoIfNeeded" in boss
    assert "FracturedSignalTelegraph" in boss
    assert "SetExternalPause" in boss


def test_presentation_dimming_is_opt_in_and_vep_independent():
    presentation = read("Presentation", "CombatPresentationDirector.cs")
    stimulus = read("SoulWisp", "VepAuraStimulus.cs")
    assert 'Shader.SetGlobalFloat("_MindforgeAmbientDim"' in presentation
    assert "_MindforgeAmbientDim" not in stimulus
    assert "Time.unscaledDeltaTime" in presentation


def test_twin_eclipse_and_counter_have_asymmetric_hitstop():
    tuning = read("Combat", "CombatTuning.cs")
    guardian = read("Combat", "GuardianCombatController.cs")
    bloom = read("Combat", "GravityBloomAbility.cs")
    # Counter contact remains deliberately crisp while the rare Twin Eclipse payoff
    # receives a much larger freeze. The contract follows the authored tuning rather
    # than preserving an obsolete 20 ms prototype literal.
    assert "parryHitStop = 0.024f" in tuning
    assert "twinEclipseHitStop = 0.120f" in tuning
    assert "bool reflectedAny" in guardian
    assert "if (reflectedAny)" in guardian
    assert "tuning.twinEclipseHitStop" in bloom
