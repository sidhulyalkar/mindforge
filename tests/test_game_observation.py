from __future__ import annotations

import json
import subprocess
import sys
from dataclasses import replace
from pathlib import Path

import pytest

from mindforge_neuro.game_observation import GameObservation, ObservationMonitor, EVENTS

ROOT = Path(__file__).resolve().parents[1]


def test_schema_and_native_telemetry_conform_to_derived_only_contract():
    schema = json.loads((ROOT / "contracts/game_observation.v1.schema.json").read_text())
    assert set(schema["required"]) == set(GameObservation.__dataclass_fields__)
    plugin = (ROOT / "mods/bepinex/Mindforge.SilksongObservation/SilksongObservationPlugin.cs").read_text()
    assert 'Config.Bind("Observation", "Enabled", false' in plugin
    assert "Harmony.Patch" not in plugin or "harmony.Patch" in plugin
    assert "hero_attack" in plugin and "hero_die" in plugin
    assert "mindforge.game_observation.v1" in plugin
    assert "observed_game_hook_unqualified" in plugin
    assert "127.0.0.1:19742" not in plugin
    assert "ModifyDamage" not in plugin and "raw_eeg" not in plugin.lower()


def test_all_explicit_observations_are_parsable_and_do_not_mean_boss_context():
    monitor = ObservationMonitor("silksong")
    for i, name in enumerate(sorted(EVENTS), start=1):
        frame = GameObservation.make("silksong", name, i, source_time_s=i*.05)
        restored = GameObservation.from_json(frame.to_json())
        assert restored == frame
        assert monitor.accept(frame, float(i)*.05)
    report = monitor.report()
    assert sum(report["counts"].values()) == len(EVENTS)
    assert not report["boss_phase_verified"]
    assert not report["combat_engagement_verified"]
    assert not report["safe_ssvep_window_verified"]


@pytest.mark.parametrize("patch", [
    {"schema": "mindforge.neural_event.v2"},
    {"source_mode": "verified_game_hook"},
    {"game_id": "unknown"},
    {"session_id": ""},
    {"seq": 0},
    {"seq": 1.0},
    {"game_version": ""},
    {"app_focused": 1},
    {"hero_x": .2, "hero_y": None},
    {"hero_x": float("nan"), "hero_y": .2},
    {"source_time_s": float("inf")},
    {"hook_target": "HeroController.Die"},
])
def test_malformed_or_misrepresented_observations_are_rejected(patch):
    obj = json.loads(GameObservation.make("silksong", "hero_attack", 1).to_json())
    obj.update(patch)
    with pytest.raises((ValueError, TypeError)):
        GameObservation.from_dict(obj)


def test_unknown_extra_fields_are_rejected_instead_of_awarding_game_authority():
    obj = json.loads(GameObservation.make("silksong", "hero_attack", 1).to_json())
    obj["boss_stagger_s"] = 5
    with pytest.raises(ValueError):
        GameObservation.from_dict(obj)


def test_death_and_scene_change_cannot_carry_player_anchor():
    for event in ("hero_die", "scene_changed"):
        with pytest.raises(ValueError):
            GameObservation.make("silksong", event, 1, anchor=(0.5, 0.5))


def test_monitor_rejects_duplicate_order_and_concurrent_session():
    monitor = ObservationMonitor("silksong")
    a = GameObservation.make("silksong", "hero_attack", 8, session_id="one")
    assert monitor.accept(a, 1.)
    assert not monitor.accept(a, 1.1)
    b = GameObservation.make("silksong", "hero_jump", 1, session_id="two")
    assert not monitor.accept(b, 1.2)
    assert monitor.accept(b, 4.0)
    assert monitor.report()["counts"]["hero_jump"] == 1
    assert monitor.report()["rejections"]["concurrent_session"] == 1


def test_monitor_counts_sequence_gaps_without_asserting_missing_action_type():
    monitor = ObservationMonitor("silksong")
    assert monitor.accept(GameObservation.make("silksong", "heartbeat", 1), 1.)
    assert monitor.accept(GameObservation.make("silksong", "hero_dash", 4), 1.2)
    assert monitor.report()["missing_seq"] == 2


def test_deterministic_simulated_audit_cli_is_not_a_measured_game():
    run = subprocess.run(
        [sys.executable, str(ROOT / "tools/mindforge_game_observe.py"),
         "--game", "silksong", "--demo", "--seconds", "1"],
        cwd=ROOT, capture_output=True, text=True, timeout=8,
    )
    assert run.returncode == 0, run.stdout + run.stderr
    assert "SYNTHETIC" in run.stdout
    assert '"combat_engagement_verified": false' in run.stdout
    assert '"hero_attack": 1' in run.stdout
