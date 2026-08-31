import math

import pytest

from mindforge_neuro.ssvep_observations import (
    SSVEP_OBSERVATION_V1,
    SsvepObservation,
)


def payload(**overrides):
    base = {
        "schema": SSVEP_OBSERVATION_V1,
        "seq": 7,
        "session_id": "game-session-01",
        "unity_realtime_s": 12.5,
        "game_time_s": 10.1,
        "frame": 1500,
        "stimulus_epoch": 3,
        "mode": "gameplay",
        "neural_state": "listening",
        "coded_active": True,
        "target_name": "FracturedSignal",
        "target_kind": "boss:fractured_signal",
        "target_locked": True,
        "target_lock_reason": "boss_encounter_auto_lock",
        "target_distance_m": 8.2,
        "target_viewport_x": 0.50,
        "target_viewport_y": 0.48,
        "target_viewport_z": 8.4,
        "sight_frequency_hz": 10.0,
        "guard_frequency_hz": 12.0,
        "qualified_refresh_hz": 120.0,
        "sight_phase_start_frame": 1480,
        "guard_phase_start_frame": 1480,
        "sight_viewport_x": 0.43,
        "sight_viewport_y": 0.47,
        "sight_viewport_z": 3.2,
        "guard_viewport_x": 0.57,
        "guard_viewport_y": 0.47,
        "guard_viewport_z": 3.2,
        "sight_visible": True,
        "guard_visible": True,
        "actual_separation_deg": 10.0,
        "sight_actual_diameter_deg": 3.0,
        "guard_actual_diameter_deg": 3.0,
        "focus_backdrop_active": True,
        "camera_fov_deg": 56.0,
        "camera_aspect": 16 / 9,
        "camera_speed_m_s": 0.01,
        "camera_angular_speed_deg_s": 0.2,
        "screen_width_px": 1920,
        "screen_height_px": 1080,
        "display_expected_refresh_hz": 120.0,
        "display_observed_refresh_hz": 119.9,
        "display_has_measurement": True,
        "display_timing_healthy": True,
    }
    base.update(overrides)
    return base


def test_observation_round_trip_and_epoch_grouping():
    observation = SsvepObservation.from_dict(payload())
    assert observation.epoch_group_key == ("game-session-01", "gameplay", 3)
    assert observation.geometry_qualified
    restored = SsvepObservation.from_json(observation.to_dict() | {"schema": SSVEP_OBSERVATION_V1} if False else observation.to_dict().__repr__())


def test_json_round_trip_uses_real_json():
    import json

    observation = SsvepObservation.from_dict(payload())
    restored = SsvepObservation.from_json(json.dumps(observation.to_dict()))
    assert restored == observation


def test_gameplay_requires_real_epoch_and_calibration_is_not_promoted_to_one():
    with pytest.raises(ValueError, match="stimulus_epoch"):
        SsvepObservation.from_dict(payload(stimulus_epoch=-1))

    calibration = SsvepObservation.from_dict(payload(mode="calibration", stimulus_epoch=-1))
    assert calibration.epoch_group_key == ("game-session-01", "calibration", -1)


def test_observation_rejects_schema_nan_and_impossible_render_geometry():
    with pytest.raises(ValueError, match="schema"):
        SsvepObservation.from_dict(payload(schema="mindforge.unknown"))
    with pytest.raises(ValueError, match="finite"):
        SsvepObservation.from_dict(payload(camera_speed_m_s=math.nan))
    with pytest.raises(ValueError, match="screen dimensions"):
        SsvepObservation.from_dict(payload(screen_width_px=0))
    with pytest.raises(ValueError, match="must differ"):
        SsvepObservation.from_dict(payload(guard_frequency_hz=10.0))


def test_geometry_qualification_fails_closed_on_missing_retinal_context():
    hidden = SsvepObservation.from_dict(payload(sight_visible=False))
    no_backdrop = SsvepObservation.from_dict(payload(focus_backdrop_active=False))
    uncoded = SsvepObservation.from_dict(payload(coded_active=False))
    assert not hidden.geometry_qualified
    assert not no_backdrop.geometry_qualified
    assert not uncoded.geometry_qualified
