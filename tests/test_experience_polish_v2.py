from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_fallback_guardian_has_one_locomotion_owner_instead_of_two_stride_clocks():
    avatar = read("Presentation", "GuardianAvatarPresentation.cs")
    polish = read("Presentation", "GuardianMotionPolish.cs")

    for token in (
        "fallback rig construction, facing and coarse action poses only",
        "GuardianMotionPolish owns every locomotion-cycle, airborne",
        "_leftLeg.localRotation = Quaternion.Slerp",
        "_rightLeg.localRotation = Quaternion.Slerp",
        "_visualRoot.localPosition = new Vector3(0f, 0.02f, 0f)",
    ):
        assert token in avatar

    # The fallback rig no longer runs a second speed-derived stride oscillator beneath
    # GuardianMotionPolish. This was especially visible as bicycle legs while airborne.
    for duplicate_gait in (
        "_stride",
        "Mathf.Sin(_stride)",
        "speed / 6.0f",
        "flutter = Mathf.Sin",
    ):
        assert duplicate_gait not in avatar

    for token in (
        "Sole locomotion/secondary-motion owner",
        "fullStrideReferenceSpeed = 11.2f",
        "legSwingDegrees = 25f",
        "armSwingDegrees = 13f",
        "pelvisBobMeters = 0.040f",
        "lateralSwayMeters = 0.028f",
        "if (grounded) _locomotionPhase += dt * strideHz * Mathf.PI * 2f",
        "Mathf.Max(0f, legSwingDegrees) * groundedMove01",
        "Mathf.Max(0f, armSwingDegrees) * groundedMove01",
        "rise01",
        "fall01",
        "_landingImpulse",
    ):
        assert token in polish

    for source in (avatar, polish):
        for forbidden in (
            "body.velocity =",
            "RequestDash(",
            "RequestJump(",
            "ReceiveDamage(",
            "TryLightAttack(",
            "TryApply(",
            "NeuralEvent",
            "UdpNeuralReceiver",
        ):
            assert forbidden not in source


def test_null_ward_enemy_archetypes_get_distinct_collider_free_silhouettes():
    builder = read("Editor", "NullWardEnemySilhouetteBuilder.cs")
    menu = read("Editor", "ShowcaseEditorMenu.cs")

    for token in (
        'RootName = "ArchetypeSilhouetteV2"',
        "JourneyEnemyArchetype.NullSentry",
        "JourneyEnemyArchetype.ChromePenitent",
        "legacyRenderer.enabled = false",
        '"NullSentry_Keel"',
        '"NullSentry_Fin_L"',
        '"NullSentry_Fin_R"',
        '"NullSentry_Visor"',
        '"ChromePenitent_Chest"',
        '"ChromePenitent_Pauldron_L"',
        '"ChromePenitent_Pauldron_R"',
        '"ChromePenitent_CleaverMass"',
        '"ChromePenitent_ChestSignal"',
        "UnityEngine.Object.DestroyImmediate(collider)",
        "renderer.sharedMaterial = material",
    ):
        assert token in builder

    # Presentation geometry is dynamic with the enemy but has no collision or static
    # batching identity of its own. Existing controller/collider authority stays below it.
    for forbidden in (
        "AddComponent<Rigidbody>",
        "AddComponent<Collider>",
        "AddComponent<CapsuleCollider>",
        "ReceiveDamage(",
        "ConfigureRuntime(",
        "SetMoveInput(",
        "RequestDash(",
        "RequestJump(",
        "TryApply(",
        "NeuralEvent",
        "VepAuraStimulus",
        "StaticEditorFlags",
    ):
        assert forbidden not in builder

    world = menu.index("NullWardSceneBuilder.BuildOpenScene();")
    silhouettes = menu.index("NullWardEnemySilhouetteBuilder.ApplyOpenScene();")
    visual_v2 = menu.index("NullWardVisualInfrastructureBuilder.ApplyOpenScene();")
    traversal = menu.index("NullWardTraversalPlayabilityBuilder.ApplyOpenScene();")
    gate = menu.index("CompetitionGateValidator.ValidateAndWrite(false);")
    assert world < silhouettes < visual_v2 < traversal < gate


def test_enemy_motion_identity_is_archetype_specific_but_presentation_only():
    presentation = read("Journey", "JourneyEnemyPresentation.cs")

    for token in (
        "nullSentryColor = new Color(0.92f, 0.08f, 0.34f)",
        "chromePenitentColor = new Color(1.00f, 0.24f, 0.06f)",
        "BobAmplitudeForArchetype()",
        "BobSpeedForArchetype()",
        "SpinSpeedForArchetype()",
        "TelegraphScaleForArchetype()",
        "idleBobAmplitude * 1.55f",
        "idleBobAmplitude * 0.35f",
        "coreSpinDegreesPerSecond * 1.65f",
        "coreSpinDegreesPerSecond * 0.60f",
        "case JourneyEnemyArchetype.NullSentry: return nullSentryColor",
        "case JourneyEnemyArchetype.ChromePenitent: return chromePenitentColor",
    ):
        assert token in presentation

    for forbidden in (
        ".ReceiveDamage(",
        ".SetMoveInput(",
        ".RequestDash(",
        ".RequestJump(",
        ".FirePulse(",
        ".TryLightAttack(",
        ".TryApply(",
        "NeuralEvent",
        "UdpNeuralReceiver",
    ):
        assert forbidden not in presentation
