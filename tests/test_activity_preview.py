from __future__ import annotations

import json
import math
import pytest

from mindforge_neuro.activity_preview import ActivityPreviewBridge
from mindforge_neuro.combat_context import CombatContext, ContextOrbPolicy, OrbMode
from mindforge_neuro.context_transport import GameContextPacket, OrbContextBridge, make_context_packet
from mindforge_neuro.game_observation import GameObservation


def observation(event, seq, *, session="test-session",
                mode="observed_game_hook_unqualified", **flags):
    return GameObservation.make(
        "silksong", event, seq, session_id=session, source_mode=mode,
        game_version="exact-test-build", **flags)


def test_solo_attack_jump_and_dash_never_indicate_combat():
    preview = ActivityPreviewBridge("silksong")
    for seq, event in enumerate(("hero_ready", "hero_jump", "hero_dash", "hero_attack"), 1):
        assert preview.accept(observation(event, seq), seq * .1)
    packet = preview.snapshot(.45)
    assert packet is not None
    assert packet.source == "observed_game_hook_unqualified"
    assert not packet.context.player_activity_recent
    assert not packet.context.enemy_engaged
    assert not packet.context.boss_encounter
    assert not packet.context.recovery_opening
    assert packet.anchor_x is None


def test_double_attack_primes_cosmetic_edge_with_zero_stimulus_authority():
    preview = ActivityPreviewBridge("silksong")
    assert preview.accept(observation("hero_ready", 1), 0.)
    assert preview.accept(observation("hero_attack", 2), .15)
    assert preview.accept(observation("hero_attack", 3), .4)
    packet = preview.snapshot(.45)
    assert packet is not None and packet.context.player_activity_recent
    assert packet.context.enemy_nearby is False
    assert packet.context.incoming_attack is False
    context = OrbContextBridge("silksong")
    assert context.accept(GameContextPacket.from_json(packet.to_json()), .45)
    state = context.snapshot(.46)
    assert state["mode"] == "primed"
    assert state["placement"] == "edge_compact"
    assert state["source"] == "observed_game_hook_unqualified"
    assert state["selection_opportunity_only"] is False
    assert state["stimulus_enabled"] is False
    assert state["presentation_only"] is True
    assert state["anchor_x"] is None


def test_activity_expires_without_more_attacks_even_with_heartbeat():
    preview = ActivityPreviewBridge("silksong")
    for event, seq, t in (
        ("hero_ready", 1, 0.), ("hero_attack", 2, .2),
        ("hero_attack", 3, .35), ("heartbeat", 4, .75),
    ):
        assert preview.accept(observation(event, seq), t)
    assert preview.snapshot(.76).context.player_activity_recent
    assert not preview.snapshot(1.1).context.player_activity_recent


def test_loss_of_observation_fails_hidden_even_when_last_attack_recent():
    preview = ActivityPreviewBridge("silksong")
    for event, seq, t in (
        ("hero_ready", 1, 0.), ("hero_attack", 2, .2),
        ("hero_attack", 3, .25),
    ):
        assert preview.accept(observation(event, seq), t)
    assert preview.snapshot(.3).context.player_activity_recent
    state = preview.snapshot(1.01)
    assert not state.context.in_game
    assert not state.context.player_activity_recent
    assert OrbContextBridge("silksong").snapshot(1.01)["mode"] == "hidden"


@pytest.mark.parametrize("event,flags", [
    ("hero_damaged", {}),
    ("hero_die", {"in_game": False}),
    ("scene_changed", {"in_game": False}),
    ("heartbeat", {"paused": True}),
    ("heartbeat", {"app_focused": False}),
    ("heartbeat", {"in_game": False}),
])
def test_danger_or_visibility_veto_clears_live_preview(event, flags):
    preview = ActivityPreviewBridge("silksong")
    for name, seq, t in (
        ("hero_ready", 1, 0.), ("hero_attack", 2, .1),
        ("hero_attack", 3, .2),
    ):
        assert preview.accept(observation(name, seq), t)
    assert preview.snapshot(.25).context.player_activity_recent
    assert preview.accept(observation(event, 4, **flags), .3)
    assert not preview.snapshot(.31).context.player_activity_recent


def test_scene_change_needs_new_hero_ready_before_activity_returns():
    preview = ActivityPreviewBridge("silksong")
    for event, seq, t, flags in (
        ("hero_ready", 1, 0., {}),
        ("hero_attack", 2, .1, {}),
        ("scene_changed", 3, .2, {"in_game": False}),
        ("hero_attack", 4, .25, {}),
        ("hero_attack", 5, .3, {}),
    ):
        assert preview.accept(observation(event, seq, **flags), t)
    assert not preview.snapshot(.35).context.player_activity_recent
    assert preview.accept(observation("hero_ready", 6), .4)
    assert preview.accept(observation("hero_attack", 7), .5)
    assert preview.accept(observation("hero_attack", 8), .6)
    assert preview.snapshot(.65).context.player_activity_recent


def test_duplicate_cross_session_and_reordered_observation_cannot_prime():
    preview = ActivityPreviewBridge("silksong")
    assert preview.accept(observation("hero_ready", 1), 0.)
    assert preview.accept(observation("hero_attack", 2), .1)
    assert not preview.accept(observation("hero_attack", 2), .2)
    assert not preview.accept(observation("hero_attack", 1, session="other"), .3)
    assert not preview.snapshot(.35).context.player_activity_recent
    assert preview.accept(observation("hero_ready", 1, session="other"), 2.5)
    assert not preview.snapshot(2.6).context.player_activity_recent
    assert preview.audit_report()["combat_and_boss_inference"] is False


def test_unqualified_source_cannot_assert_enemy_boss_or_safe_opening():
    prohibited = (
        {"enemy_nearby": True}, {"enemy_engaged": True},
        {"boss_encounter": True}, {"incoming_attack": True},
        {"precision_window": True}, {"recovery_opening": True,
                                      "opening_duration_s": 3.},
    )
    for flags in prohibited:
        with pytest.raises(ValueError):
            make_context_packet(
                "silksong", "unqualified", 1,
                source="observed_game_hook_unqualified", **flags)
    with pytest.raises(ValueError):
        make_context_packet("silksong", "unqualified", 1,
                            source="observed_game_hook_unqualified",
                            anchor=(.5, .5), player_activity_recent=True)


def test_policy_defense_in_depth_rejects_direct_forged_unqualified_context():
    policy = ContextOrbPolicy("silksong")
    invalid = CombatContext(
        game_id="silksong", session_id="forged", seq=1,
        source="observed_game_hook_unqualified",
        enemy_engaged=True, recovery_opening=True, opening_duration_s=3.)
    assert not policy.update(invalid, 0.)
    assert policy.decide(.1).mode == OrbMode.HIDDEN


def test_synthetic_observations_remain_labeled_synthetic_when_bridged():
    preview = ActivityPreviewBridge("silksong")
    for event, seq, t in (
        ("hero_ready", 1, 0.), ("hero_attack", 2, .1),
        ("hero_attack", 3, .2),
    ):
        assert preview.accept(observation(event, seq, mode="synthetic_game_observation"), t)
    packet = preview.snapshot(.25)
    assert packet.source == "synthetic_game_context"
    assert packet.context.player_activity_recent
    assert packet.to_dict()["context"]["player_activity_recent"] is True
