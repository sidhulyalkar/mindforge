from __future__ import annotations

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_optional_presentation_budget_cannot_change_game_or_vep_timing():
    source = read("Presentation", "PresentationQualityGovernor.cs")

    assert "Time.unscaledDeltaTime" in source
    assert "allowAdaptiveOutsideControllerOnly = false" in source
    assert "ControllerOnlyQualificationBootstrap" in source
    assert "PresentationQualityGovernor.FxDensity" not in source

    for forbidden in (
        "Time.timeScale =",
        "Time.fixedDeltaTime =",
        "Application.targetFrameRate =",
        "ScalableBufferManager",
        "QualitySettings.SetQualityLevel",
        "VepAuraStimulus",
        "TryApply(",
        "ReceiveDamage(",
        "RequestDash(",
        "Award(",
    ):
        assert forbidden not in source


def test_combat_vfx_use_a_bounded_pool_instead_of_per_hit_gameobject_churn():
    orchestrator = read("Presentation", "CombatVfxOrchestrator.cs")
    pool = read("Presentation", "PresentationFxPool.cs")

    assert "PresentationFxPool.GetOrCreate()" in orchestrator
    assert "EmitBurst(position, color, count, speed, size)" in orchestrator
    assert "EmitRing(position, normal, color, startRadius, endRadius, lifetime, width)" in orchestrator
    assert 'new GameObject("MindforgeImpactBurst")' not in orchestrator
    assert 'new GameObject("MindforgeImpactRing")' not in orchestrator
    assert "AddComponent<ParticleSystem>()" not in orchestrator
    assert "AddComponent<LineRenderer>()" not in orchestrator

    for token in (
        "Stack<PooledParticleBurst>",
        "Stack<PooledTransientRing>",
        "burstPrewarm",
        "ringPrewarm",
        "maximumBursts",
        "maximumRings",
        "PresentationQualityGovernor.FxDensity",
        "PresentationQualityGovernor.PreferredRingSegments",
        "ParticleSystemStopAction.Callback",
        "main.useUnscaledTime = true",
    ):
        assert token in pool

    # Saturation is allowed to drop an optional visual, but never mutate combat.
    assert "if (_createdBursts >= Mathf.Max(1, maximumBursts)) return null;" in pool
    assert "if (_createdRings >= Mathf.Max(1, maximumRings)) return null;" in pool
    for forbidden in (
        "ReceiveDamage(",
        "TryLightAttack(",
        "SetGuardHeld(",
        "RequestDash(",
        "TryApply(",
        "Award(",
    ):
        assert forbidden not in pool


def test_wisp_shell_consumes_only_accepted_aura_state_and_never_the_coded_stimulus():
    shell = read("SoulWisp", "WispPresentationShell.cs")

    for token in (
        "AuraBuffController",
        "SightActive",
        "GuardActive",
        "ConcordActive",
        "AuraApplied += OnAuraApplied",
        "ConcordTriggered += OnConcordTriggered",
        "Time.unscaledDeltaTime",
        "MindforgeWispPresentationShell",
    ):
        assert token in shell

    # Documentation may name the stimulus class, but runtime code must not fetch,
    # configure, or retain it. The shell is downstream of accepted aura state only.
    for forbidden in (
        "GetComponent<VepAuraStimulus>",
        "FindObjectOfType<VepAuraStimulus>",
        "UdpNeuralReceiver",
        "NeuralEvent",
        "NeuralFocusResonance",
        "sight_score",
        "guard_score",
        "confidence",
        "frequencyHz",
        "TryApply(",
        "ReceiveDamage(",
        "TryLightAttack(",
        "RequestDash(",
        "Award(",
    ):
        assert forbidden not in shell


def test_wisp_shell_bootstrap_is_additive_and_does_not_require_authority_edits():
    shell = read("SoulWisp", "WispPresentationShell.cs")
    controller = read("SoulWisp", "SoulWispController.cs")

    assert "WispPresentationShellBootstrap" in shell
    assert "GetComponent<WispPresentationShell>() == null" in shell
    assert "AddComponent<WispPresentationShell>()" in shell
    assert "WispPresentationShell" not in controller
