from __future__ import annotations

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_showcase_has_one_click_editor_build_play_path_without_faking_bci():
    menu = read("Editor", "ShowcaseEditorMenu.cs")
    preview = read("Qualification", "ShowcasePreviewBootstrap.cs")

    assert 'MenuItem("Mindforge/Showcase/Build + Play Combat Showcase"' in menu
    assert 'MenuItem("Mindforge/Showcase/Build + Play Cinematic Showcase"' in menu
    assert "CompetitionSceneAssembler.BuildCompetitionScene()" in menu
    assert "ShowcaseSceneDecorator.DecorateOpenScene()" in menu
    assert "CinematicSceneDetailer.EnhanceOpenScene()" in menu
    assert "CompetitionGateValidator.ValidateAndWrite(false)" in menu
    assert "EditorApplication.isPlaying = true" in menu
    assert "ShowcasePreviewBootstrap.EditorPreferenceKey" in menu

    assert 'CommandLineFlag = "-mindforge-showcase"' in preview
    assert "ControllerOnlyQualificationBootstrap" in preview
    assert 'qualification.EnterControllerOnly("SHOWCASE_PREVIEW")' in preview
    assert "BCI explicitly disabled" in preview
    for forbidden in ("CalibrationReady = true", "AURA_SELECTED", "TryApply(", "NeuralEvent("):
        assert forbidden not in preview


def test_showcase_environment_is_editor_authored_scenery_not_gameplay_authority():
    scenery = read("Editor", "ShowcaseSceneDecorator.cs")

    for token in (
        'ShowcaseRootName = "Mindforge_Showcase_Environment"',
        '"DuelFloor"',
        '"ArenaRing_Outer"',
        '"ArenaRing_Mid"',
        '"FractureMonolith_',
        '"HorizonWall_',
        '"ArenaRimLight_',
        '"ListeningHalo"',
        "RenderSettings.fog",
        "AmbientMode.Trilight",
    ):
        assert token in scenery

    for forbidden in (
        "ReceiveDamage(",
        "TryLightAttack(",
        "SetGuardHeld(",
        "RequestDash(",
        "TryApply(",
        "Award(",
    ):
        assert forbidden not in scenery


def test_showcase_runtime_composes_character_boss_camera_vfx_post_and_melee_telegraph():
    installer = read("Presentation", "ShowcaseRuntimeInstaller.cs")

    for token in (
        "GuardianAvatarPresentation",
        "FracturedSignalAvatar",
        "FracturedSignalMeleePresentation",
        "ShowcaseCameraRig",
        "ArenaVisibilityDirector",
        "CombatVfxOrchestrator",
        "ShowcasePostProcessing",
        "FracturedSignalMeleeDirector",
        "CinematicRuntimeMaterialOverride",
        "CinematicArtOverrideInstaller",
    ):
        assert token in installer

    # This compositor is not allowed to issue player/boss authority.
    for forbidden in (
        "ReceiveDamage(",
        "TryLightAttack(",
        "SetGuardHeld(",
        "RequestDash(",
        "TryApply(",
        "Award(",
    ):
        assert forbidden not in installer


def test_close_range_boss_patterns_share_scheduler_and_have_truthful_geometry():
    boss = read("Combat", "FracturedSignalDirector.cs")
    melee = read("Combat", "FracturedSignalMeleeDirector.cs")
    telegraph = read("Presentation", "FracturedSignalMeleePresentation.cs")

    assert "melee.CanEngage" in boss
    assert "yield return melee.ExecuteCleave" in boss
    assert "yield return melee.ExecuteSlam" in boss
    assert "Projectile and melee patterns share this scheduler" in boss

    assert "ResolveCleave(direction, range, arc" in melee
    assert "Vector3.Angle(lockedDirection, delta.normalized) > arc * 0.5f" in melee
    assert "ResolveSlam(radius" in melee
    assert "delta.magnitude > radius" in melee
    assert "playerMotor.IsInvulnerable" in melee
    assert "TryResolveIncomingStrike" in melee
    for outcome in ("SPACED", "SIDESTEPPED", "DODGED", "BLOCKED", "PERFECT_GUARD", "GUARD_BROKEN", "FLANKED"):
        assert outcome in melee

    # Presentation consumes the exact authority-provided range/arc/direction.
    assert "MeleeTelegraphed += OnTelegraph" in telegraph
    assert "_range = Mathf.Max(0.1f, range)" in telegraph
    assert "_arcDegrees = Mathf.Clamp(arcDegrees" in telegraph
    assert "Quaternion.AngleAxis(angle, Vector3.up) * _direction" in telegraph
    assert "MeleeResolved += OnResolved" in telegraph


def test_showcase_presentation_classes_remain_non_authoritative():
    files = (
        ("Presentation", "GuardianAvatarPresentation.cs"),
        ("Presentation", "FracturedSignalAvatar.cs"),
        ("Presentation", "ShowcaseCameraRig.cs"),
        ("Presentation", "ArenaVisibilityDirector.cs"),
        ("Presentation", "ShowcasePostProcessing.cs"),
        ("Presentation", "CombatVfxOrchestrator.cs"),
        ("Presentation", "FracturedSignalMeleePresentation.cs"),
        ("Presentation", "CinematicRuntimeMaterialOverride.cs"),
    )
    forbidden = (
        "ReceiveDamage(",
        "TryLightAttack(",
        "SetGuardHeld(",
        "RequestDash(",
        "FirePulse(",
        "RiftCleave(",
        "BeginCounter(",
        "TryApply(",
        "Award(",
    )
    for parts in files:
        source = read(*parts)
        assert all(token not in source for token in forbidden), (parts, [token for token in forbidden if token in source])


def test_showcase_hotkeys_do_not_collide_with_judge_lens_or_controller_preview():
    photodiode = read("Presentation", "PhotodiodePatch.cs")
    guide = read("Presentation", "PlayerAgencyGuide.cs")
    camera = read("Presentation", "ShowcaseCameraRig.cs")
    controller = read("Qualification", "ControllerOnlyQualificationBootstrap.cs")

    assert "toggleKey = KeyCode.F9" in photodiode
    assert "switchSourceKey = KeyCode.F11" in photodiode
    assert "Input.GetKeyDown(KeyCode.F10)" in guide
    assert "targetFocusToggleKey = KeyCode.T" in camera
    assert "EditorHotkey = KeyCode.F8" in controller


def test_post_stack_is_visual_only_readable_and_signal_break_reduces_sensory_load():
    post = read("Presentation", "ShowcasePostProcessing.cs")

    for token in (
        "VolumeProfile",
        "Bloom",
        "Vignette",
        "ColorAdjustments",
        "WhiteBalance",
        "FilmGrain",
        "ChromaticAberration",
        "TonemappingMode.ACES",
        "renderPostProcessing = true",
        "rest ? 0.12f : 0.27f",
        "rest ? 0.035f : 0.060f",
        "rest ? 3f : 7f",
        "rest ? 0.30f : 0.38f",
        "rest ? 0.012f : 0.025f",
        "rest ? 0.002f : 0.006f",
    ):
        assert token in post

    # Temporal reconstruction is restricted to controller-only visual review; the
    # calibrated/live path stays frame-local with SMAA for VEP timing integrity.
    assert "ControllerOnlyQualificationActive" in post
    assert "AntialiasingMode.TemporalAntiAliasing" in post
    assert "AntialiasingMode.SubpixelMorphologicalAntiAliasing" in post
    assert post.index("if (controllerOnly)") < post.index("AntialiasingMode.TemporalAntiAliasing")
    assert post.index("AntialiasingMode.TemporalAntiAliasing") < post.index("AntialiasingMode.SubpixelMorphologicalAntiAliasing")
    assert "VepAuraStimulus" not in post
