from __future__ import annotations

from dataclasses import replace
import json
import math
from pathlib import Path

import pytest

from mindforge_neuro.context_transport import (
    CONTEXT_INPUT_V1, CONTEXT_OUTPUT_V1, GameContextPacket,
    OrbContextBridge, make_context_packet,
)

ROOT = Path(__file__).resolve().parents[1]


def packet(**flags):
    return make_context_packet("silksong", "game-session-a", 4, **flags)


def test_game_context_roundtrip_and_contract_are_derived_only():
    entry = packet(enemy_engaged=True, recovery_opening=True,
                   opening_duration_s=3.0)
    restored = GameContextPacket.from_json(entry.to_json())
    assert restored.context.enemy_engaged
    assert restored.context.opening_duration_s == 3.
    assert json.loads(entry.to_json())["schema"] == CONTEXT_INPUT_V1
    assert "raw_eeg" not in entry.to_json()
    assert "damage" not in entry.to_json()


@pytest.mark.parametrize("mutation", [
    lambda d: d.update({"unauthorized": True}),
    lambda d: d.update({"schema": "evil.v0"}),
    lambda d: d.update({"game_id": "elden_ring"}),
    lambda d: d.update({"seq": True}),
    lambda d: d.update({"ttl_ms": 99999}),
    lambda d: d.update({"session_id": ""}),
    lambda d: d.update({"anchor_x": .4, "anchor_y": None}),
    lambda d: d.update({"anchor_x": math.nan, "anchor_y": .5}),
    lambda d: d["context"].update({"incoming_attack": 1}),
    lambda d: d["context"].update({"opening_duration_s": float("inf")}),
    lambda d: d["context"].update({"fake_combat_signal": True}),
])
def test_malformed_payload_rejected(mutation):
    original = packet().to_dict()
    mutation(original)
    with pytest.raises((ValueError, TypeError)):
        GameContextPacket.from_dict(original)


def test_without_context_the_overlay_is_invisible_and_non_authoritative():
    bridge = OrbContextBridge("cuphead")
    state = bridge.snapshot(0.)
    assert state["mode"] == "hidden"
    assert state["session_id"] == ""
    assert state["presentation_only"] is True
    assert state["stimulus_enabled"] is False
    assert state["selection_opportunity_only"] is False


def test_combat_visibility_and_safe_precision_docking():
    bridge = OrbContextBridge("silksong")
    assert bridge.accept(packet(enemy_engaged=True), 1.)
    state = bridge.snapshot(1.1)
    assert state["mode"] == "present"
    assert state["placement"] == "edge_compact"  # no verified player anchor
    assert state["stimulus_enabled"] is False

    threat = make_context_packet("silksong", "game-session-a", 5,
                                 enemy_engaged=True, incoming_attack=True)
    assert bridge.accept(threat, 1.2)
    state = bridge.snapshot(1.21)
    assert state["mode"] == "docked"
    assert state["selection_opportunity_only"] is False
    assert state["placement"] == "edge_compact"


def test_player_anchor_only_used_with_declared_verified_game_hook():
    bridge = OrbContextBridge("silksong")
    assert bridge.accept(packet(enemy_engaged=True, anchor=(.61, .48)), 1.)
    frame = bridge.snapshot(1.01)
    assert frame["placement"] == "edge_compact"
    assert frame["anchor_x"] is None
    verified = make_context_packet(
        "silksong", "game-session-a", 5, source="verified_game_hook",
        enemy_engaged=True, anchor=(.61, .48))
    assert bridge.accept(verified, 1.02)
    frame = bridge.snapshot(1.03)
    assert frame["placement"] == "player_anchor"
    assert frame["anchor_x"] == pytest.approx(.61)
    assert frame["anchor_y"] == pytest.approx(.48)
    assert frame["stimulus_enabled"] is False


def test_source_timeout_and_stale_data_do_not_keep_orbs_visible():
    bridge = OrbContextBridge("cuphead")
    assert bridge.accept(make_context_packet(
        "cuphead", "game-session-a", 1, boss_encounter=True, ttl_ms=200), 1.)
    assert bridge.snapshot(1.1)["mode"] == "present"
    assert bridge.snapshot(1.21)["mode"] == "hidden"
    assert bridge.snapshot(1.21)["reason"] == "missing_or_stale_context"


def test_cross_session_replay_rejected_until_context_ttl():
    bridge = OrbContextBridge("hollow_knight")
    first = make_context_packet("hollow_knight", "first", 7, enemy_engaged=True)
    second = make_context_packet("hollow_knight", "second", 1, enemy_engaged=True)
    assert bridge.accept(first, 1.)
    assert not bridge.accept(first, 1.01)
    assert not bridge.accept(second, 1.1)
    assert bridge.accept(second, 1.8)
    assert bridge.snapshot(1.82)["session_id"] == "second"


def test_pause_death_cutscene_and_focus_are_hard_vetoes():
    for veto in ("paused", "menu_open", "cutscene", "scene_transition"):
        bridge = OrbContextBridge("silksong")
        assert bridge.accept(packet(enemy_engaged=True, **{veto: True}), 1.)
        assert bridge.snapshot(1.1)["mode"] == "hidden"
    bridge = OrbContextBridge("silksong")
    assert bridge.accept(packet(enemy_engaged=True, player_alive=False), 1.)
    assert bridge.snapshot(1.1)["mode"] == "hidden"


def test_no_stimulus_even_during_candidate_recovery():
    bridge = OrbContextBridge("hollow_knight")
    p = make_context_packet("hollow_knight", "a", 1, boss_encounter=True,
                            recovery_opening=True, opening_duration_s=3.)
    assert bridge.accept(p, 1.)
    frame = bridge.snapshot(1.1)
    assert frame["selection_opportunity_only"]
    assert frame["stimulus_enabled"] is False
    assert frame["presentation_only"] is True


def test_contract_paths_are_versioned():
    # Ensure docs and source control maintain named transport schemas.
    inputs = ROOT / "contracts" / "game_context.v1.schema.json"
    outputs = ROOT / "contracts" / "orb_context_state.v1.schema.json"
    assert inputs.is_file() and outputs.is_file()
    assert json.loads(inputs.read_text())["properties"]["schema"]["const"] == CONTEXT_INPUT_V1
    assert json.loads(outputs.read_text())["properties"]["schema"]["const"] == CONTEXT_OUTPUT_V1
