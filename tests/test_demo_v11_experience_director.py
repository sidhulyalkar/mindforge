from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PRESENTATION = ROOT / "unity" / "Assets" / "Mindforge" / "Presentation"
SOURCE = PRESENTATION / "MindforgeDemoV11ExperienceDirector.cs"


def source() -> str:
    return SOURCE.read_text(encoding="utf-8")


def test_v11_experience_director_is_marker_scoped_and_presentation_only():
    text = source()
    assert "FindObjectOfType<MindforgeDemoV11Marker>(true)" in text
    assert "MindforgeDemoV11ExperienceDirector" in text
    forbidden = (
        "TakeDamage(",
        "ApplyDamage(",
        "Configure(CombatTeam",
        "SetExternalPause(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "AuraBuffController",
        "GuardianCombatController.PrimaryTarget =",
        "Instantiate(projectile",
    )
    for token in forbidden:
        assert token not in text


def test_v11_district_profiles_cover_the_entire_route():
    text = source()
    for district in ("sanctum", "causeway", "market", "ascent", "fracture"):
        assert f'"{district}"' in text
    for boundary in ("-2f", "32f", "58f", "83f", "float.PositiveInfinity"):
        assert boundary in text
    assert "DistrictIndexFor(_guardian.position.z)" in text


def test_v11_spatial_atmosphere_is_disabled_for_neural_hardware_demo():
    text = source()
    assert "_marker.ControllerOnlyByDefault" in text
    assert "if (!_controllerPresentation || _district < 0) return;" in text
    assert "RenderSettings.ambientLight" in text
    assert "RenderSettings.fogColor" in text
    # Landmark emphasis is proximity-driven rather than a periodic luminance clock.
    assert "Vector3.Distance(guardianPosition, accent.renderer.bounds.center)" in text
    assert "Mathf.SmoothStep" in text
    assert "Mathf.Sin" not in text
    assert "Mathf.Cos" not in text
    assert "Mathf.PingPong" not in text


def test_v11_landmarks_are_authored_as_spatial_guides():
    text = source()
    for landmark in (
        "MemoryForgeCore",
        "CausewayAetherSpine",
        "MarketSignalOrb",
        "AscentAetherGuide",
        "SkylineAetherBeacon",
        "FractureSpire_",
    ):
        assert landmark in text
    assert 'SetColor("_EmissionColor"' in text
    assert "SpatialWeight" in text


def test_v11_echoes_gain_three_distinct_visual_silhouettes_without_mechanic_claims():
    text = source()
    assert "BuildNeedle" in text
    assert "BuildBastion" in text
    assert "BuildChoir" in text
    assert "FracturedEchoNode[] echoes" in text
    assert 'GetType().Name, "MindforgeDemoEchoV11"' in text
    assert "collider.enabled = false;" in text
    assert "visual progression only" in text


def test_v11_boss_staging_reads_existing_phase_and_never_writes_phase_authority():
    text = source()
    assert "Mathf.Clamp(_director.Phase, 1, 3)" in text
    assert "PhaseTwoFractureRing" in text
    assert "PhaseThreeFractureCrown" in text
    assert "_phaseTwo.gameObject.SetActive(_phase >= 2)" in text
    assert "_phaseThree.gameObject.SetActive(_phase >= 3)" in text
    assert "_director.Phase =" not in text
    assert "maxHealth" not in text


def test_v11_experience_visual_primitives_never_add_collision_authority():
    text = source()
    assert text.count("collider.enabled = false;") >= 2
    assert "AddComponent<Rigidbody>" not in text
    assert "AddComponent<Collider>" not in text
