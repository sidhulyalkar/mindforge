#!/usr/bin/env python3
"""Send deterministic rehearsal commands to neurOS Phantom Unicorn."""
from __future__ import annotations

import argparse
import socket
import time


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("commands", nargs="+", help="e.g. 1 2 0 j silence:2.5 gain:0.65")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=19744)
    parser.add_argument("--delay", type=float, default=0.0,
                        help="seconds between multiple commands")
    args = parser.parse_args()
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        for i, command in enumerate(args.commands):
            command = command.strip().lower()
            if not command:
                continue
            sock.sendto(command.encode("utf-8"), (args.host, args.port))
            print(f"phantom <- {command}")
            if args.delay > 0 and i + 1 < len(args.commands):
                time.sleep(args.delay)
    finally:
        sock.close()


if __name__ == "__main__":
    main()
