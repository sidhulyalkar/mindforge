from __future__ import annotations

from dataclasses import replace

from mindforge_neuro.combat_context import CombatContext, ContextOrbPolicy, OrbMode


def context(game: str = "silksong", **updates) -> CombatContext:
    return replace(CombatContext(game_id=game, session_id="session-1", seq=1), **updates)


def test_exploration_hidden_and_nearby_only_primes():
    policy = ContextOrbPolicy("silksong")
    assert policy.update(context(), 1.0)
    assert policy.decide(1.1).mode == OrbMode.HIDDEN
    assert policy.update(context(seq=2, enemy_nearby=True), 1.2)
    frame = policy.decide(1.3)
    assert frame.mode == OrbMode.PRIMED
    assert not frame.can_offer_selection
    assert not frame.stimulus_enabled


def test_boss_context_enables_presence_but_never_actual_stimulus():
    policy = ContextOrbPolicy("hollow_knight")
    assert policy.update(context("hollow_knight", boss_encounter=True,
                                 recovery_opening=True, opening_duration_s=3), 1.)
    out = policy.decide(1.1)
    assert out.mode == OrbMode.PRESENT
    assert out.placement_hint == "player_follow_hint"
    assert out.can_offer_selection
    assert not out.stimulus_enabled


def test_cuphead_uses_edge_position_during_bullet_hell():
    policy = ContextOrbPolicy("cuphead")
    policy.update(context("cuphead", enemy_engaged=True), 5.)
    out = policy.decide(5.1)
    assert out.mode == OrbMode.PRESENT
    assert out.placement_hint == "edge_compact"
    policy.update(context("cuphead", seq=2, boss_encounter=True, incoming_attack=True), 5.2)
    out = policy.decide(5.3)
    assert out.mode == OrbMode.DOCKED
    assert not out.can_offer_selection


def test_telegraph_parry_precision_and_platforming_preempt_selection():
    for veto in ("incoming_attack", "precision_window", "platforming_hazard"):
        p = ContextOrbPolicy("silksong")
        c = context(enemy_engaged=True, recovery_opening=True,
                    opening_duration_s=4.0, **{veto: True})
        p.update(c, 0.)
        out = p.decide(0.1)
        assert out.mode == OrbMode.DOCKED
        assert not out.can_offer_selection


def test_hard_veto_hides_even_during_boss():
    for veto, value in (("paused", True), ("menu_open", True),
                        ("cutscene", True), ("scene_transition", True),
                        ("player_alive", False), ("app_focused", False),
                        ("in_game", False)):
        p = ContextOrbPolicy("cuphead")
        p.update(context("cuphead", boss_encounter=True, **{veto: value}), 1.)
        assert p.decide(1.1).mode == OrbMode.HIDDEN


def test_postcombat_lingers_then_hides_and_stale_context_is_neutral():
    p = ContextOrbPolicy("hollow_knight")
    p.update(context("hollow_knight", boss_encounter=True), 1.)
    p.update(context("hollow_knight", seq=2), 1.4)
    assert p.decide(1.5).mode == OrbMode.RESTING
    # Renew context without a fight to keep the observation stream fresh.
    p.update(context("hollow_knight", seq=3), 3.)
    assert p.decide(3.1).mode == OrbMode.RESTING
    p.update(context("hollow_knight", seq=4), 3.5)
    assert p.decide(3.6).mode == OrbMode.HIDDEN
    assert p.decide(4.5).reason == "no_valid_game_context"


def test_cross_session_out_of_order_and_invalid_context_rejected():
    p = ContextOrbPolicy("silksong")
    first = context(seq=4, enemy_engaged=True)
    assert p.update(first, 1.)
    assert not p.update(first, 1.1)
    assert not p.update(replace(first, session_id="another", seq=1), 1.2)
    assert not p.update(replace(first, seq=5, source="unverified"), 1.3)
    assert p.update(replace(first, session_id="another", seq=1), 2.)
    assert p.decide(2.1).mode == OrbMode.PRESENT


def test_safe_opening_requires_real_opportunity_duration():
    p = ContextOrbPolicy("hollow_knight")
    p.update(context("hollow_knight", enemy_engaged=True,
                     recovery_opening=True, opening_duration_s=0.3), 0)
    assert not p.decide(0.1).can_offer_selection
    p.update(context("hollow_knight", seq=2, enemy_engaged=True,
                     recovery_opening=True, opening_duration_s=2.5), 0.2)
    assert p.decide(0.3).can_offer_selection
