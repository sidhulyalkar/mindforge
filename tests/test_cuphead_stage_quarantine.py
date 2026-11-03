from __future__ import annotations

import json

from mindforge_neuro.cuphead_stage import (
    CupheadStageMonitor, CupheadStageObservation, SOURCE,
)


def frame(stage, seq, source_time_s, *, event="sample", session="a",
          source=SOURCE, paused=False, focused=True):
    base = json.loads(CupheadStageObservation.synthetic(
        stage, seq, event=event, session_id=session,
        source_time_s=source_time_s,
    ).to_json())
    base["source"] = source
    base["game_version"] = "pinned-runtime"
    base["paused"] = paused
    base["app_focused"] = focused
    return CupheadStageObservation.from_dict(base)


def test_cuphead_stage_quarantine_requires_repeated_native_level_samples():
    gate = CupheadStageMonitor(ttl_s=1.25)
    assert not gate.cosmetic_level_permitted_at(0.)
    assert gate.accept(frame("unknown", 1, 0.), 1.0)
    assert gate.accept(frame("level", 2, .5), 1.5)
    assert not gate.cosmetic_level_permitted_at(1.6)  # only 1 level sample
    assert gate.accept(frame("level", 3, 1.), 2.0)
    assert gate.cosmetic_level_permitted_at(2.01)
    assert not gate.cosmetic_level_permitted_at(3.4)  # source TTL expired


def test_cuphead_stage_scene_change_resets_level_consistency_and_holds():
    gate = CupheadStageMonitor(ttl_s=1.25)
    assert gate.accept(frame("level", 1, 0.), 0.)
    assert gate.accept(frame("level", 2, .5), .5)
    assert not gate.cosmetic_level_permitted_at(.51)  # quarantine
    assert gate.cosmetic_level_permitted_at(.8)
    assert gate.accept(frame("unknown", 3, .9, event="scene_changed"), .9)
    assert not gate.cosmetic_level_permitted_at(.91)
    assert gate.accept(frame("level", 4, 1.0), 1.0)
    assert gate.accept(frame("level", 5, 1.5), 1.5)
    assert not gate.cosmetic_level_permitted_at(1.6)
    assert gate.cosmetic_level_permitted_at(1.66)


def test_cuphead_stage_map_pause_focus_and_synthetic_all_hide():
    gate = CupheadStageMonitor(ttl_s=1.25)
    assert gate.accept(frame("level", 1, 0.), 0.)
    assert gate.accept(frame("level", 2, .5), .5)
    assert gate.cosmetic_level_permitted_at(.8)
    assert gate.accept(frame("map", 3, 1.), 1.0)
    assert not gate.cosmetic_level_permitted_at(1.01)
    assert gate.accept(frame("level", 4, 1.5), 1.5)
    assert gate.accept(frame("level", 5, 2.), 2.)
    assert gate.cosmetic_level_permitted_at(2.01)
    assert gate.accept(frame("level", 6, 2.1, paused=True), 2.1)
    assert not gate.cosmetic_level_permitted_at(2.11)
    assert gate.accept(frame("level", 7, 2.5, focused=False), 2.5)
    assert not gate.cosmetic_level_permitted_at(2.51)
    assert gate.accept(frame("level", 8, 3.), 3.)
    assert gate.accept(frame("level", 9, 3.5), 3.5)
    assert gate.cosmetic_level_permitted_at(3.51)
    # The stage simulation is never a native observed game signal.
    synthetic = frame("level", 10, 4., source="synthetic_stage_observation")
    assert gate.accept(synthetic, 4.)
    assert not gate.cosmetic_level_permitted_at(4.01)


def test_cuphead_visual_veto_source_code_matches_python_semantics():
    from pathlib import Path
    root = Path(__file__).resolve().parents[1]
    source = (root / "mods/bepinex/Mindforge.PortableOrbs/"
              "CupheadStageReceiver.cs").read_text()
    assert "consistentLevelSamples >= 2" in source
    assert "lastTransitionAt >= 0.75f" in source
    assert "consistentLevelSamples = 0" in source
    assert 'next.@event == "scene_changed"' in source
    assert "Time.realtimeSinceStartup - receivedAt < 1.25f" in source
