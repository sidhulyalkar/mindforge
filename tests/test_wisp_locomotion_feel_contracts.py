from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_fantasy_wisp_drifts_organically_around_guardian_without_moving_neural_authority():
    wisp = read("SoulWisp", "SoulWispController.cs")

    for token in (
        "Fantasy companion drift · presentation only",
        "companionNearRadius = 0.85f",
        "companionFarRadius = 3.8f",
        "companionDriftFrequency = 0.20f",
        "companionCatchupDistance = 5.4f",
        "companionTeleportDistance = 10.5f",
        "UpdateCompanionDrift(activeTarget)",
        "Mathf.PerlinNoise(seed, time)",
        "PlaceFreeCombatTargets(activeTarget)",
        "freeHorizontalSeparation = 1.34f",
        "wispCore.gameObject.SetActive(true)",
    ):
        assert token in wisp

    # The visible companion can wander, but the coded gaze targets no longer circle it.
    assert "PlaceOrbitingAura" not in wisp
    assert "orbitAngularSpeedRadians" not in wisp
    assert "_orbitPhase" not in wisp

    # Sight/Guard remain the only coded targets and retain explicit stable placement.
    assert "sightStimulus?.Configure(sightFrequencyHz, sightColor)" in wisp
    assert "guardStimulus?.Configure(guardFrequencyHz, guardColor)" in wisp
    assert "PlaceStableLockedTargets" in wisp
    assert "PlaceStableAura(sightAura" in wisp
    assert "PlaceStableAura(guardAura" in wisp

    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "sight_score",
        "guard_score",
        "TryApply(",
        "ReceiveDamage(",
        "Award(",
    ):
        assert forbidden not in wisp


def test_wisp_shell_uses_tapered_fantasy_tendrils_instead_of_orbital_rings():
    shell = read("SoulWisp", "WispPresentationShell.cs")

    for token in (
        "NeutralTendril",
        "SightTendril",
        "GuardTendril",
        "ConcordTendril",
        "line.loop = false",
        "line.widthCurve = new AnimationCurve",
        "ResolveTrailDirectionWorld",
        "_presentationVelocity",
        "Vector3.ClampMagnitude(rawVelocity, 9f)",
        "PresentationQualityGovernor.OptionalShellDetail",
    ):
        assert token in shell

    for forbidden in (
        "NeutralRing",
        "SightRing",
        "GuardRing",
        "ConcordRing",
        "CreateRing(",
        "line.loop = true",
        "sightStimulus",
        "guardStimulus",
        "sight_score",
        "guard_score",
        "TryApply(",
    ):
        assert forbidden not in shell


def test_guardian_forward_travel_is_fast_and_reaches_speed_quickly_on_fixed_ticks():
    motor = read("Combat", "GuardianMotor.cs")

    for token in (
        "minimumAcceleration = 82f",
        "deceleration = 94f",
        "reversalAcceleration = 122f",
        "forwardSpeedMultiplier = 1.55f",
        "strafeSpeedMultiplier = 1.22f",
        "backwardSpeedMultiplier = 1.05f",
        "dashExitVelocityRetention = 0.48f",
        "DirectionalSpeedMultiplier(_moveInput)",
        "tuning.maxSpeed * loadMultiplier * stanceMultiplier * directionalMultiplier",
        "private void FixedUpdate()",
        "response * Time.fixedDeltaTime",
    ):
        assert token in motor

    # Camera-relative vector input remains clamped, so diagonal movement does not add an
    # accidental sqrt(2) speed bonus on top of the directional profile.
    assert "Vector3.ClampMagnitude(right * _moveInput.x + forward * _moveInput.y, 1f)" in motor

    # Movement feel changes must not introduce render-frame gameplay timing.
    assert "private void Update()" not in motor
    assert "Time.deltaTime" not in motor

    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "TryApply(",
    ):
        assert forbidden not in motor
