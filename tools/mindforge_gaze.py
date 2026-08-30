#!/usr/bin/env python3
"""Mindforge gaze development and Pupil Labs Neon screen bridge.

The Unity boundary intentionally receives only normalized, screen-mapped gaze. Raw eye
images and vendor-specific payloads remain outside the game process.

Examples:
    python tools/mindforge_gaze.py mouse
    python tools/mindforge_gaze.py point --x 0.5 --y 0.5
    python tools/mindforge_gaze.py replay experiments/gaze/session.jsonl
    python tools/mindforge_gaze.py neon-screen

The ``neon-screen`` command uses Pupil Labs' official Real-Time API plus the MIT-licensed
``real_time_screen_gaze`` package. Four AprilTags are placed in small top-most windows
at the display corners so the glasses' scene camera can map gaze into screen pixels.
For the first hardware slice, run Mindforge borderless/full-screen on that display.
"""

from __future__ import annotations

import argparse
import base64
import json
import math
import socket
import sys
import time
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Iterable, Optional, Tuple

SCHEMA = "mindforge.gaze_event.v1"
DEFAULT_HOST = "127.0.0.1"
DEFAULT_PORT = 19746


@dataclass(frozen=True)
class GazeEvent:
    schema: str
    seq: int
    source_mode: str
    timestamp_ns: int
    x: float
    y: float
    confidence: float
    fixation: bool
    worn: bool
    coordinate_origin: str
    surface: str

    @classmethod
    def create(
        cls,
        *,
        seq: int,
        source_mode: str,
        x: float,
        y: float,
        confidence: float = 1.0,
        fixation: bool = False,
        worn: bool = True,
        coordinate_origin: str = "top_left",
        surface: str = "screen",
        timestamp_ns: Optional[int] = None,
    ) -> "GazeEvent":
        if coordinate_origin not in {"top_left", "bottom_left"}:
            raise ValueError("coordinate_origin must be top_left or bottom_left")
        for name, value in (("x", x), ("y", y), ("confidence", confidence)):
            if not math.isfinite(value):
                raise ValueError(f"{name} must be finite")
        return cls(
            schema=SCHEMA,
            seq=max(0, int(seq)),
            source_mode=source_mode,
            timestamp_ns=time.monotonic_ns() if timestamp_ns is None else max(0, int(timestamp_ns)),
            x=min(1.0, max(0.0, float(x))),
            y=min(1.0, max(0.0, float(y))),
            confidence=min(1.0, max(0.0, float(confidence))),
            fixation=bool(fixation),
            worn=bool(worn),
            coordinate_origin=coordinate_origin,
            surface=surface or "screen",
        )


class UdpEmitter:
    def __init__(self, host: str, port: int) -> None:
        self._target = (host, port)
        self._socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    def send(self, event: GazeEvent) -> None:
        payload = json.dumps(asdict(event), separators=(",", ":"), sort_keys=True).encode("utf-8")
        self._socket.sendto(payload, self._target)

    def close(self) -> None:
        self._socket.close()

    def __enter__(self) -> "UdpEmitter":
        return self

    def __exit__(self, *_: object) -> None:
        self.close()


def _sleep_to_rate(deadline: float, hz: float) -> float:
    period = 1.0 / max(1.0, hz)
    deadline += period
    delay = deadline - time.monotonic()
    if delay > 0:
        time.sleep(delay)
        return deadline
    return time.monotonic()


def run_point(args: argparse.Namespace) -> int:
    with UdpEmitter(args.host, args.port) as emitter:
        seq = 0
        deadline = time.monotonic()
        try:
            while True:
                emitter.send(
                    GazeEvent.create(
                        seq=seq,
                        source_mode="simulated_script",
                        x=args.x,
                        y=args.y,
                        fixation=True,
                    )
                )
                seq += 1
                if args.seconds > 0 and seq / args.hz >= args.seconds:
                    return 0
                deadline = _sleep_to_rate(deadline, args.hz)
        except KeyboardInterrupt:
            return 0


def run_mouse(args: argparse.Namespace) -> int:
    try:
        import tkinter as tk
    except ImportError as exc:
        raise SystemExit("mouse mode requires Python tkinter support") from exc

    root = tk.Tk()
    root.withdraw()
    width = max(1, int(root.winfo_screenwidth()))
    height = max(1, int(root.winfo_screenheight()))
    print(f"Streaming pointer simulation from {width}x{height} display to UDP {args.host}:{args.port}")

    with UdpEmitter(args.host, args.port) as emitter:
        seq = 0
        deadline = time.monotonic()
        try:
            while True:
                root.update_idletasks()
                root.update()
                x = root.winfo_pointerx() / width
                y = root.winfo_pointery() / height
                emitter.send(
                    GazeEvent.create(
                        seq=seq,
                        source_mode="simulated_pointer",
                        x=x,
                        y=y,
                        confidence=1.0,
                        fixation=False,
                    )
                )
                seq += 1
                deadline = _sleep_to_rate(deadline, args.hz)
        except (KeyboardInterrupt, tk.TclError):
            return 0
        finally:
            try:
                root.destroy()
            except tk.TclError:
                pass


def _iter_replay(path: Path) -> Iterable[dict]:
    for line_number, raw in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        raw = raw.strip()
        if not raw:
            continue
        try:
            value = json.loads(raw)
        except json.JSONDecodeError as exc:
            raise SystemExit(f"Invalid JSON at {path}:{line_number}: {exc}") from exc
        if not isinstance(value, dict):
            raise SystemExit(f"Expected JSON object at {path}:{line_number}")
        yield value


def run_replay(args: argparse.Namespace) -> int:
    path = Path(args.path)
    if not path.is_file():
        raise SystemExit(f"Replay file does not exist: {path}")

    previous_timestamp: Optional[int] = None
    seq = 0
    with UdpEmitter(args.host, args.port) as emitter:
        for raw in _iter_replay(path):
            timestamp = int(raw.get("timestamp_ns", 0) or 0)
            if previous_timestamp is not None and timestamp > previous_timestamp and args.speed > 0:
                time.sleep(min(args.max_sleep, (timestamp - previous_timestamp) / 1e9 / args.speed))
            previous_timestamp = timestamp or previous_timestamp
            emitter.send(
                GazeEvent.create(
                    seq=seq,
                    source_mode="gaze_replay",
                    x=float(raw.get("x", 0.5)),
                    y=float(raw.get("y", 0.5)),
                    confidence=float(raw.get("confidence", 1.0)),
                    fixation=bool(raw.get("fixation", False)),
                    worn=bool(raw.get("worn", True)),
                    coordinate_origin=str(raw.get("coordinate_origin", "top_left")),
                    surface=str(raw.get("surface", "screen")),
                )
            )
            seq += 1
    return 0


class MarkerOverlay:
    """Four tiny top-most AprilTag windows used by Pupil Labs screen mapping."""

    def __init__(self, width: int, height: int, marker_size: int, inset: int) -> None:
        try:
            import cv2
            import tkinter as tk
            from pupil_labs.real_time_screen_gaze import marker_generator
        except ImportError as exc:
            raise SystemExit(
                "Neon screen mode dependencies are missing. Install: "
                "pip install 'pupil-labs-realtime-api>=1.1.0' real_time_screen_gaze"
            ) from exc

        self._tk = tk
        self.root = tk.Tk()
        self.root.withdraw()
        self._windows = []
        self.marker_verts = {}
        size = max(40, int(marker_size))
        inset = max(0, int(inset))
        positions = (
            (inset, inset),
            (width - inset - size, inset),
            (width - inset - size, height - inset - size),
            (inset, height - inset - size),
        )

        for marker_id, (x, y) in enumerate(positions):
            image = marker_generator.generate_marker(marker_id=marker_id, side_pixels=size)
            ok, encoded = cv2.imencode(".png", image)
            if not ok:
                raise SystemExit(f"Could not encode AprilTag {marker_id}")
            encoded_b64 = base64.b64encode(encoded.tobytes()).decode("ascii")

            window = tk.Toplevel(self.root)
            window.overrideredirect(True)
            window.attributes("-topmost", True)
            window.geometry(f"{size}x{size}+{x}+{y}")
            photo = tk.PhotoImage(data=encoded_b64)
            label = tk.Label(window, image=photo, borderwidth=0, highlightthickness=0)
            label.image = photo
            label.pack(fill="both", expand=True)
            self._windows.append(window)
            self.marker_verts[marker_id] = [
                (x, y),
                (x + size, y),
                (x + size, y + size),
                (x, y + size),
            ]

        self.pump()

    def pump(self) -> None:
        self.root.update_idletasks()
        self.root.update()

    def close(self) -> None:
        try:
            self.root.destroy()
        except self._tk.TclError:
            pass


def _screen_dimensions(args: argparse.Namespace) -> Tuple[int, int]:
    if args.screen_width > 0 and args.screen_height > 0:
        return int(args.screen_width), int(args.screen_height)
    try:
        import tkinter as tk
    except ImportError as exc:
        raise SystemExit("Automatic screen sizing requires tkinter") from exc
    root = tk.Tk()
    root.withdraw()
    width, height = int(root.winfo_screenwidth()), int(root.winfo_screenheight())
    root.destroy()
    return width, height


def run_neon_screen(args: argparse.Namespace) -> int:
    try:
        from pupil_labs.realtime_api.simple import discover_one_device
        from pupil_labs.real_time_screen_gaze.gaze_mapper import GazeMapper
    except ImportError as exc:
        raise SystemExit(
            "Install the optional Neon bridge dependencies first: "
            "pip install 'pupil-labs-realtime-api>=1.1.0' real_time_screen_gaze"
        ) from exc

    width, height = _screen_dimensions(args)
    overlay = MarkerOverlay(width, height, args.marker_size, args.marker_inset)
    print(f"Discovering Neon on the local network for up to {args.discover_seconds:.1f}s...")
    device = discover_one_device(max_search_duration_seconds=args.discover_seconds)
    if device is None:
        overlay.close()
        raise SystemExit("No Pupil Labs Neon device discovered. Check Companion and local-network connectivity.")

    calibration = device.get_calibration()
    mapper = GazeMapper(calibration)
    surface = mapper.add_surface(overlay.marker_verts, (width, height))
    print(
        f"Neon mapped-gaze bridge live on {width}x{height}. "
        f"Keep all four corner AprilTags visible. Sending to UDP {args.host}:{args.port}."
    )

    seq = 0
    with UdpEmitter(args.host, args.port) as emitter:
        try:
            while True:
                overlay.pump()
                frame, gaze = device.receive_matched_scene_video_frame_and_gaze()
                if frame is None or gaze is None:
                    continue
                result = mapper.process_frame(frame, gaze)
                mapped = result.mapped_gaze.get(surface.uid, ())
                for surface_gaze in mapped:
                    x_px = float(surface_gaze.x)
                    y_px = float(surface_gaze.y)
                    if not (math.isfinite(x_px) and math.isfinite(y_px)):
                        continue
                    emitter.send(
                        GazeEvent.create(
                            seq=seq,
                            source_mode="live_pupil_neon_surface",
                            x=x_px / width,
                            y=y_px / height,
                            confidence=1.0,
                            fixation=False,
                            worn=bool(getattr(gaze, "worn", True)),
                            coordinate_origin="top_left",
                            surface="primary_display",
                        )
                    )
                    seq += 1
        except (KeyboardInterrupt, Exception) as exc:
            # Ctrl+C is the normal exit. Tk raises its own exception if the display closes.
            if not isinstance(exc, KeyboardInterrupt):
                print(f"Neon bridge stopped: {exc}", file=sys.stderr)
                return 2
            return 0
        finally:
            overlay.close()
            close = getattr(device, "close", None)
            if callable(close):
                close()


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Mindforge gaze bridge")
    parser.add_argument("--host", default=DEFAULT_HOST)
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    sub = parser.add_subparsers(dest="command", required=True)

    mouse = sub.add_parser("mouse", help="stream desktop pointer as simulated gaze")
    mouse.add_argument("--hz", type=float, default=60.0)
    mouse.set_defaults(func=run_mouse)

    point = sub.add_parser("point", help="stream a fixed normalized gaze point")
    point.add_argument("--x", type=float, default=0.5)
    point.add_argument("--y", type=float, default=0.5)
    point.add_argument("--hz", type=float, default=30.0)
    point.add_argument("--seconds", type=float, default=0.0, help="0 runs until interrupted")
    point.set_defaults(func=run_point)

    replay = sub.add_parser("replay", help="replay a JSONL GazeEvent tape")
    replay.add_argument("path")
    replay.add_argument("--speed", type=float, default=1.0)
    replay.add_argument("--max-sleep", type=float, default=0.25)
    replay.set_defaults(func=run_replay)

    neon = sub.add_parser("neon-screen", help="map live Neon gaze onto the primary display")
    neon.add_argument("--discover-seconds", type=float, default=10.0)
    neon.add_argument("--screen-width", type=int, default=0)
    neon.add_argument("--screen-height", type=int, default=0)
    neon.add_argument("--marker-size", type=int, default=72)
    neon.add_argument("--marker-inset", type=int, default=24)
    neon.set_defaults(func=run_neon_screen)
    return parser


def main(argv: Optional[list[str]] = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    if args.host not in {"127.0.0.1", "localhost", "::1"}:
        parser.error("Mindforge gaze transport is intentionally loopback-only")
    if not 1 <= args.port <= 65535:
        parser.error("port must be in [1, 65535]")
    return int(args.func(args))


if __name__ == "__main__":
    raise SystemExit(main())
