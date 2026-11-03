from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ORBS = ROOT / "mods/bepinex/Mindforge.PortableOrbs"
STAGE = ROOT / "mods/bepinex/Mindforge.CupheadStageObservation"


def test_cuphead_stage_is_veto_only_and_requires_independent_context():
    overlay = (ORBS / "PortableOrbsPlugin.cs").read_text()
    receiver = (ORBS / "CupheadStageReceiver.cs").read_text()
    assert 'Config.Bind("Overlay", "RequireCupheadPlayableStage", true' in overlay
    assert 'gameId == "cuphead"' in overlay
    assert "cupheadStage.IsPlayableLevel" in overlay
    assert 'new CupheadStageReceiver()' in overlay
    assert 'new ContextPresentationReceiver()' in overlay
    assert 'combatContext.Mode' in overlay
    assert "cupheadStage.Poll()" in overlay
    assert "cupheadStage.Dispose()" in overlay
    assert 'next.stage == "level"' not in receiver  # never read gameplay by guessing
    assert 'latest.stage == "level"' in receiver
    assert 'latest.app_focused && !latest.paused' in receiver
    assert "Time.realtimeSinceStartup - receivedAt < 1.25f" in receiver
    assert 'next.source != "observed_stage_reflection_unqualified"' in receiver
    assert 'new IPEndPoint(IPAddress.Loopback, 19758)' in receiver
    assert 'next.@event == "scene_changed" && next.stage != "unknown"' in receiver
    assert "next.seq <= lastSeq" in receiver
    assert "next.source_time_s < lastSourceS" in receiver
    assert "stimulus_enabled" not in receiver
    assert "Boss" not in receiver


def test_independent_cuphead_audit_and_ui_channels_have_no_competing_listener():
    observer = (STAGE / "CupheadStageObservationPlugin.cs").read_text()
    receiver = (ORBS / "CupheadStageReceiver.cs").read_text()
    assert "sender.Connect(IPAddress.Loopback, 19757)" in observer
    assert "overlaySender.Connect(IPAddress.Loopback, 19758)" in observer
    assert "sender.Send(raw, raw.Length)" in observer
    assert "overlaySender.Send(raw, raw.Length)" in observer
    assert 'new IPEndPoint(IPAddress.Loopback, 19758)' in receiver
    assert "Harmony.Patch" not in observer
    assert "PlayerManager" not in observer


def test_cuphead_portable_orbs_target_net35_while_silksong_remains_net46():
    project = (ORBS / "Mindforge.PortableOrbs.csproj").read_text()
    assert 'MindforgeGame' in project
    assert "'$(MindforgeGame)' == 'cuphead'\">net35</TargetFramework>" in project
    assert "'$(MindforgeGame)' != 'cuphead'\">net46</TargetFramework>" in project
    assert 'Microsoft.NETFramework.ReferenceAssemblies' in project
    assert 'DeployToPlugins' not in project
