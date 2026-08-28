from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def _cs_sources():
    return list(UNITY.rglob("*.cs"))


def test_generated_ui_uses_supported_unity_2022_builtin_font():
    assembler = (UNITY / "Editor" / "CompetitionSceneAssembler.cs").read_text(encoding="utf-8")
    assert 'Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")' in assembler
    assert "Arial.ttf" not in assembler


def test_no_unity_source_uses_removed_arial_builtin_font():
    offenders = []
    for path in _cs_sources():
        source = path.read_text(encoding="utf-8")
        if "Arial.ttf" in source:
            offenders.append(str(path.relative_to(ROOT)))
    assert offenders == []


def test_system_diagnostics_files_do_not_leave_unity_debug_ambiguous():
    offenders = []
    for path in _cs_sources():
        source = path.read_text(encoding="utf-8")
        if "using System.Diagnostics;" not in source:
            continue
        if re.search(r"(?m)^\s*Debug\.(?:Log|LogWarning|LogError)\(", source):
            if "using Debug = UnityEngine.Debug;" not in source:
                offenders.append(str(path.relative_to(ROOT)))
    assert offenders == []


def test_system_namespace_files_do_not_use_ambiguous_bare_unity_object_lookup():
    offenders = []
    for path in _cs_sources():
        source = path.read_text(encoding="utf-8")
        if "using System;" not in source:
            continue
        if re.search(r"(?<!UnityEngine\.)\bObject\.(?:FindObjectOfType|FindObjectsOfType)\b", source):
            if "using Object = UnityEngine.Object;" not in source:
                offenders.append(str(path.relative_to(ROOT)))
    assert offenders == []


def test_urp_quality_enums_are_explicitly_unityengine_qualified():
    source = (UNITY / "Editor" / "CinematicFidelityConfigurator.cs").read_text(encoding="utf-8")
    for token in (
        "UnityEngine.ShadowQuality.All",
        "UnityEngine.ShadowResolution.VeryHigh",
        "UnityEngine.ShadowProjection.StableFit",
        "UnityEngine.AnisotropicFiltering.ForceEnable",
        "UnityEngine.SkinWeights.FourBones",
    ):
        assert token in source
