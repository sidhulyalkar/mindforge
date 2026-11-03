from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

import pytest

from mindforge_neuro.cuphead_stage import (
    CupheadStageMonitor, CupheadStageObservation, SCHEMA, SOURCE,
)

ROOT = Path(__file__).resolve().parents[1]


def sample(stage="map", seq=1, *, event="sample", session="a", t=0.):
    payload = json.loads(CupheadStageObservation.synthetic(
        stage, seq, event=event, session_id=session, source_time_s=t).to_json())
    payload["source"] = SOURCE
    payload["game_version"] = "pin-1"
    return CupheadStageObservation.from_dict(payload)


def test_stage_contract_has_no_boss_or_neural_authority():
    x = sample("level")
    assert CupheadStageObservation.from_json(x.to_json()) == x
    schema = json.loads((ROOT / "contracts/cuphead_stage_observation.v1.schema.json").read_text())
    assert set(schema["required"]) == set(CupheadStageObservation.__dataclass_fields__)
    assert schema["properties"]["schema"]["const"] == SCHEMA
    assert "boss_encounter" not in x.to_json()
    assert "ssvep" not in x.to_json()
    assert "hero_attack" not in x.to_json()


@pytest.mark.parametrize("mutation", [
    lambda d: d.update({"schema":"unknown"}), lambda d:d.update({"game_id":"silksong"}),
    lambda d:d.update({"seq":True}), lambda d:d.update({"seq":0}),
    lambda d:d.update({"stage":"boss"}), lambda d:d.update({"stage":"level","event":"scene_changed"}),
    lambda d:d.update({"source":"verified_boss_hook"}), lambda d:d.update({"source_time_s":float("nan")}),
    lambda d:d.update({"source_time_s":-1}), lambda d:d.update({"paused":1}),
    lambda d:d.update({"extra_boss_safety":True}),
])
def test_invalid_cuphead_stage_wire_data_rejected(mutation):
    obj = json.loads(sample().to_json())
    mutation(obj)
    with pytest.raises((ValueError, TypeError)):
        CupheadStageObservation.from_dict(obj)


def test_monitor_handles_scene_transitions_ttl_and_pause():
    m = CupheadStageMonitor(ttl_s=.75)
    assert m.stage_at(0.) == "unknown"
    assert m.accept(sample("map", 1, t=0.), 0.)
    assert m.stage_at(.2) == "map"
    assert m.accept(sample("unknown", 2, event="scene_changed", t=.5), .5)
    assert m.stage_at(.51) == "unknown"
    assert m.accept(sample("level", 3, t=.7), .7)
    assert m.stage_at(.71) == "level"
    assert m.stage_at(1.6) == "unknown"
    paused = json.loads(sample("level", 4, t=.9).to_json())
    paused["paused"] = True
    assert m.accept(CupheadStageObservation.from_dict(paused), 1.7)
    assert m.stage_at(1.8) == "unknown"


def test_monitor_prevents_replay_clock_reversal_and_concurrent_producers():
    m = CupheadStageMonitor(ttl_s=1.)
    assert m.accept(sample("map", 1, t=1.), 3.)
    assert not m.accept(sample("map", 1, t=1.), 3.1)
    assert not m.accept(sample("level", 2, session="another", t=0.), 3.2)
    assert not m.accept(sample("level", 2, t=.9), 3.3)
    assert m.accept(sample("level", 3, t=1.5), 3.4)
    assert m.accept(sample("map", 1, session="another", t=0.), 5.)
    report = m.report(5.1)
    assert report["rejections"]["replay_or_reorder"] == 1
    assert report["rejections"]["concurrent_session"] == 1
    assert report["rejections"]["source_clock_reversed"] == 1
    assert report["boss_phase_verified"] is False
    assert report["ssvep_stimulus_enabled"] is False


def test_cuphead_stage_cli_explicit_synthetic_demo():
    r = subprocess.run(
        [sys.executable, str(ROOT / "tools/mindforge_cuphead_stage.py"), "--demo"],
        cwd=ROOT, capture_output=True, text=True, timeout=8,
    )
    assert r.returncode == 0, r.stderr + r.stdout
    assert "SYNTHETIC CUPHEAD STAGE" in r.stdout
    report = json.loads(r.stdout[r.stdout.index("{"):])
    assert report["game_id"] == "cuphead"
    assert report["boss_phase_verified"] is False
    assert report["combat_engagement_verified"] is False
    assert report["stage_samples"]["level"] >= 1


def test_cuphead_native_preflight_does_not_claim_runtime_compatibility(tmp_path):
    managed=tmp_path/"Cuphead_Data"/"Managed"
    core=tmp_path/"BepInEx"/"core"
    managed.mkdir(parents=True)
    core.mkdir(parents=True)
    (core/"BepInEx.dll").write_bytes(b"bepinex-fixture")
    (managed/"Assembly-CSharp.dll").write_bytes(b"cuphead-fixture")
    (managed/"UnityEngine.dll").write_bytes(b"unity-fixture")
    run=subprocess.run(
        [sys.executable,str(ROOT/"tools/mindforge_native_preflight.py"),
         "--game","cuphead","--game-root",str(tmp_path)],
        cwd=ROOT,capture_output=True,text=True,timeout=8
    )
    assert run.returncode==0,run.stderr+run.stdout
    result=json.loads(run.stdout)
    assert result["required_target_framework"]=="net35"
    assert result["build_reference_prerequisites_present"]
    assert result["runtime_compatibility_verified"] is False
    assert result["native_plugin_compiled"] is False
    assert str(tmp_path) not in run.stdout


def test_cuphead_source_is_a_standalone_off_by_default_stage_observer():
    base=ROOT/"mods"/"bepinex"/"Mindforge.CupheadStageObservation"
    plugin=(base/"CupheadStageObservationPlugin.cs").read_text()
    csproj=(base/"Mindforge.CupheadStageObservation.csproj").read_text()
    assert 'Config.Bind("Observation", "Enabled", false' in plugin
    assert 'FindGameType("Level")' in plugin
    assert 'FindGameType("Map")' in plugin
    assert '"Current"' in plugin
    assert "scene_changed" in plugin
    assert 'sender.Connect(IPAddress.Loopback, 19757)' in plugin
    assert "<TargetFramework>net35</TargetFramework>" in csproj
    assert "DeployToPlugins" not in csproj
    assert "Harmony.Patch" not in plugin
    assert "PlayerManager.SetPlayerCanJoin" not in plugin
    assert "DamageMultiplier" not in plugin
