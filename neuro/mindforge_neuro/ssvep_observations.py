from __future__ import annotations

import json
import math
import socket
from dataclasses import asdict, dataclass
from typing import Any, Mapping


SSVEP_OBSERVATION_V1 = "mindforge.ssvep_observation.v1"


@dataclass(frozen=True)
class SsvepObservation:
    """One observer-only sample of the retinal/game context around an SSVEP interval.

    The record intentionally contains no raw EEG and no decoder internals. Acquisition joins
    this stream to raw EEG and game markers using ``session_id`` plus ``stimulus_epoch`` and
    timestamps. Gameplay epochs are valid grouping units; samples inside one epoch must never
    be split across train and validation/test sets.
    """

    schema: str
    seq: int
    session_id: str
    unity_realtime_s: float
    game_time_s: float
    frame: int
    stimulus_epoch: int
    mode: str
    neural_state: str
    coded_active: bool

    target_name: str
    target_kind: str
    target_locked: bool
    target_lock_reason: str
    target_distance_m: float
    target_viewport_x: float
    target_viewport_y: float
    target_viewport_z: float

    sight_frequency_hz: float
    guard_frequency_hz: float
    qualified_refresh_hz: float
    sight_phase_start_frame: int
    guard_phase_start_frame: int

    sight_viewport_x: float
    sight_viewport_y: float
    sight_viewport_z: float
    guard_viewport_x: float
    guard_viewport_y: float
    guard_viewport_z: float
    sight_visible: bool
    guard_visible: bool

    actual_separation_deg: float
    sight_actual_diameter_deg: float
    guard_actual_diameter_deg: float
    focus_backdrop_active: bool

    camera_fov_deg: float
    camera_aspect: float
    camera_speed_m_s: float
    camera_angular_speed_deg_s: float
    screen_width_px: int
    screen_height_px: int

    display_expected_refresh_hz: float
    display_observed_refresh_hz: float
    display_has_measurement: bool
    display_timing_healthy: bool

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> "SsvepObservation":
        schema = str(payload.get("schema") or "")
        if schema != SSVEP_OBSERVATION_V1:
            raise ValueError(f"unsupported SSVEP observation schema: {schema!r}")

        mode = str(payload.get("mode") or "")
        if mode not in {"gameplay", "calibration"}:
            raise ValueError(f"invalid SSVEP observation mode: {mode!r}")

        session_id = str(payload.get("session_id") or "").strip()
        if not session_id:
            raise ValueError("SSVEP observation requires a non-empty session_id")

        epoch = int(payload.get("stimulus_epoch", -1))
        if mode == "gameplay" and epoch < 0:
            raise ValueError("gameplay SSVEP observation requires a non-negative stimulus_epoch")

        def f(name: str, default: float = 0.0) -> float:
            value = float(payload.get(name, default))
            if not math.isfinite(value):
                raise ValueError(f"{name} must be finite")
            return value

        def i(name: str, default: int = 0) -> int:
            return int(payload.get(name, default))

        sight_hz = f("sight_frequency_hz")
        guard_hz = f("guard_frequency_hz")
        refresh_hz = f("qualified_refresh_hz")
        separation = f("actual_separation_deg")
        sight_diameter = f("sight_actual_diameter_deg")
        guard_diameter = f("guard_actual_diameter_deg")
        fov = f("camera_fov_deg")
        aspect = f("camera_aspect")
        width = i("screen_width_px")
        height = i("screen_height_px")

        if sight_hz <= 0 or guard_hz <= 0 or refresh_hz <= 0:
            raise ValueError("stimulus frequencies and qualified refresh rate must be positive")
        if sight_hz == guard_hz:
            raise ValueError("Sight and Guard stimulus frequencies must differ")
        if separation < 0 or sight_diameter < 0 or guard_diameter < 0:
            raise ValueError("angular geometry cannot be negative")
        if not 1.0 <= fov < 180.0 or aspect <= 0:
            raise ValueError("invalid camera geometry")
        if width <= 0 or height <= 0:
            raise ValueError("screen dimensions must be positive")

        return cls(
            schema=schema,
            seq=max(0, i("seq")),
            session_id=session_id,
            unity_realtime_s=f("unity_realtime_s"),
            game_time_s=f("game_time_s"),
            frame=i("frame", -1),
            stimulus_epoch=epoch,
            mode=mode,
            neural_state=str(payload.get("neural_state") or "unavailable"),
            coded_active=bool(payload.get("coded_active", False)),
            target_name=str(payload.get("target_name") or ""),
            target_kind=str(payload.get("target_kind") or "none"),
            target_locked=bool(payload.get("target_locked", False)),
            target_lock_reason=str(payload.get("target_lock_reason") or ""),
            target_distance_m=f("target_distance_m", -1.0),
            target_viewport_x=f("target_viewport_x", -1.0),
            target_viewport_y=f("target_viewport_y", -1.0),
            target_viewport_z=f("target_viewport_z", -1.0),
            sight_frequency_hz=sight_hz,
            guard_frequency_hz=guard_hz,
            qualified_refresh_hz=refresh_hz,
            sight_phase_start_frame=i("sight_phase_start_frame", -1),
            guard_phase_start_frame=i("guard_phase_start_frame", -1),
            sight_viewport_x=f("sight_viewport_x"),
            sight_viewport_y=f("sight_viewport_y"),
            sight_viewport_z=f("sight_viewport_z"),
            guard_viewport_x=f("guard_viewport_x"),
            guard_viewport_y=f("guard_viewport_y"),
            guard_viewport_z=f("guard_viewport_z"),
            sight_visible=bool(payload.get("sight_visible", False)),
            guard_visible=bool(payload.get("guard_visible", False)),
            actual_separation_deg=separation,
            sight_actual_diameter_deg=sight_diameter,
            guard_actual_diameter_deg=guard_diameter,
            focus_backdrop_active=bool(payload.get("focus_backdrop_active", False)),
            camera_fov_deg=fov,
            camera_aspect=aspect,
            camera_speed_m_s=f("camera_speed_m_s"),
            camera_angular_speed_deg_s=f("camera_angular_speed_deg_s"),
            screen_width_px=width,
            screen_height_px=height,
            display_expected_refresh_hz=f("display_expected_refresh_hz"),
            display_observed_refresh_hz=f("display_observed_refresh_hz"),
            display_has_measurement=bool(payload.get("display_has_measurement", False)),
            display_timing_healthy=bool(payload.get("display_timing_healthy", False)),
        )

    @classmethod
    def from_json(cls, raw: str | bytes) -> "SsvepObservation":
        if isinstance(raw, bytes):
            raw = raw.decode("utf-8")
        payload = json.loads(raw)
        if not isinstance(payload, dict):
            raise ValueError("SSVEP observation JSON must be an object")
        return cls.from_dict(payload)

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)

    @property
    def epoch_group_key(self) -> tuple[str, str, int]:
        """Minimum leakage-safe grouping key for gameplay data.

        Participant identity belongs outside Unity and must be prepended by the dataset builder.
        Calibration observations need a calibration/trial identifier before they are eligible for
        train/test splitting because their Unity ``stimulus_epoch`` is intentionally ``-1``.
        """
        return (self.session_id, self.mode, self.stimulus_epoch)

    @property
    def geometry_qualified(self) -> bool:
        """Whether the rendered observation is usable as a controlled SSVEP context sample."""
        return (
            self.coded_active
            and self.sight_visible
            and self.guard_visible
            and self.actual_separation_deg > 0
            and self.sight_actual_diameter_deg > 0
            and self.guard_actual_diameter_deg > 0
            and self.camera_fov_deg > 0
            and self.focus_backdrop_active
        )


class UdpSsvepObservationSource:
    """Passive UDP inlet for the Unity SSVEP observation lane (default port 19746)."""

    def __init__(self, host: str = "127.0.0.1", port: int = 19746, *, timeout_s: float = 0.25):
        self.address = (host, int(port))
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.socket.bind(self.address)
        self.socket.settimeout(max(0.001, float(timeout_s)))

    def receive(self) -> SsvepObservation | None:
        try:
            raw, _remote = self.socket.recvfrom(65535)
        except socket.timeout:
            return None
        try:
            return SsvepObservation.from_json(raw)
        except (UnicodeDecodeError, json.JSONDecodeError, ValueError, TypeError, OverflowError):
            return None

    def close(self) -> None:
        self.socket.close()

    def __enter__(self) -> "UdpSsvepObservationSource":
        return self

    def __exit__(self, exc_type, exc, tb) -> None:
        self.close()
