from __future__ import annotations

from mindforge_neuro.passive_mod_policy import AmbientContext, PassiveAmbientPolicy


def frame(seq=1, session="p1", **updates):
    record = {
        "schema": "mindforge.neural_feature_frame.v1",
        "seq": seq, "source_session_id": session,
        "source_mode": "simulated_feature",
        "calibration_id": None,
        "feature": "posterior_alpha_relative_power",
        "unit": "ratio", "value": 0.85,
        "quality": 0.92, "artifact": False,
        "passive_only": True, "ttl_ms": 1000,
    }
    record.update(updates)
    return record


def test_default_off_no_neural_decoration():
    policy = PassiveAmbientPolicy(baseline_alpha=0.5)
    assert not policy.accept(frame(), 1.)
    assert policy.step(1., AmbientContext()) == 0.
    assert policy.step(2., AmbientContext()) == 0.


def test_no_baseline_means_no_claim_or_output_even_when_opted_in():
    policy = PassiveAmbientPolicy(opted_in=True)
    assert not policy.accept(frame(), 0.)
    assert policy.step(0., AmbientContext()) == 0.
    assert policy.step(1., AmbientContext()) == 0.


def test_spectral_delta_is_bounded_and_slew_limited_not_emotion():
    policy = PassiveAmbientPolicy(opted_in=True, baseline_alpha=0.5)
    assert policy.accept(frame(), 0.)
    assert policy.step(0., AmbientContext()) == 0.
    value = policy.step(0.5, AmbientContext())
    assert 0 < value <= 0.04 + 1e-9
    assert policy.step(0.75, AmbientContext()) <= 0.08
    assert policy.step(3., AmbientContext()) < value


def test_artifact_or_low_quality_withdraws_old_target():
    for field in ({"artifact": True}, {"quality": 0.4},
                  {"passive_only": False}, {"source_mode": "unknown"},
                  {"value": float("nan")}):
        policy = PassiveAmbientPolicy(opted_in=True, baseline_alpha=0.5)
        policy.accept(frame(), 0.)
        policy.step(0., AmbientContext())
        assert policy.step(0.5, AmbientContext()) > 0
        assert not policy.accept(frame(seq=2, **field), 0.6)
        old = policy.step(0.6, AmbientContext())
        assert policy.step(1.0, AmbientContext()) < old


def test_stop_visual_modulation_during_active_ssvep_and_calibration():
    policy = PassiveAmbientPolicy(opted_in=True, baseline_alpha=0.5)
    policy.accept(frame(), 0.)
    policy.step(0., AmbientContext())
    assert policy.step(0.3, AmbientContext()) > 0
    assert policy.step(0.4, AmbientContext(active_ssvep_window=True)) == 0
    assert policy.step(0.5, AmbientContext(calibration_window=True)) == 0


def test_stale_feature_and_invalid_session_are_safe():
    p = PassiveAmbientPolicy(opted_in=True, baseline_alpha=0.5)
    assert p.accept(frame(), 0.)
    assert not p.accept(frame(), 0.2)
    assert not p.accept(frame(seq=1, session="intruder"), 0.3)
    assert p.accept(frame(seq=1, session="fresh"), 1.2)
    assert p.source_session_id == "fresh"


def test_live_requires_calibration_identity():
    p = PassiveAmbientPolicy(opted_in=True, baseline_alpha=0.5)
    assert not p.accept(frame(source_mode="live"), 0.)
    assert p.accept(frame(source_mode="live", calibration_id="cal-3"), 0.2)


def test_optout_immediately_clears_decorative_effect():
    p = PassiveAmbientPolicy(opted_in=True, baseline_alpha=0.5)
    p.accept(frame(), 0.)
    p.step(0., AmbientContext())
    assert p.step(0.5, AmbientContext()) > 0
    p.opt_out()
    assert p.step(0.6, AmbientContext()) == 0
    assert not p.accept(frame(seq=2), 0.7)
