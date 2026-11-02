#!/usr/bin/env python3
"""Deterministic synthetic game-context walkthrough for Mindforge orb policy.

Not a game hook, EEG recording, or real SSVEP stimulus. Writes JSONL proposals
that can be reviewed/compared before installing any native mod code.
"""
from __future__ import annotations

import argparse
from dataclasses import asdict, replace
import json

from mindforge_neuro.combat_context import CombatContext, ContextOrbPolicy


SCENES = (
    ("exploration", {}),
    ("approach", {"enemy_nearby": True}),
    ("engage", {"enemy_engaged": True}),
    ("telegraph", {"enemy_engaged": True, "incoming_attack": True}),
    ("recovery", {"boss_encounter": True, "recovery_opening": True,
                  "opening_duration_s": 2.8}),
    ("parry", {"boss_encounter": True, "precision_window": True}),
    ("finish", {}),
    ("clear", {}),
    ("menu", {"menu_open": True}),
)


def walkthrough(game: str):
    policy = ContextOrbPolicy(game)
    result = []
    for i, (name, patch) in enumerate(SCENES, 1):
        now = i * 0.5
        c = replace(CombatContext(game_id=game, session_id="synthetic-context",
                                  seq=i), **patch)
        assert policy.update(c, now)
        decision = policy.decide(now)
        result.append({
            "scenario": name, "game": game, "context_source": c.source,
            "time_s": now, "mode": decision.mode.value,
            "placement_hint": decision.placement_hint,
            "reason": decision.reason,
            "selection_opportunity_only": decision.can_offer_selection,
            "stimulus_enabled": decision.stimulus_enabled,
            "game_hooks_installed": False,
        })
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game", choices=("silksong", "cuphead", "hollow_knight"),
                        required=True)
    parser.add_argument("--output", help="Optional JSONL file path")
    args = parser.parse_args()
    text = "".join(json.dumps(row, sort_keys=True) + "\n"
                   for row in walkthrough(args.game))
    if args.output:
        from pathlib import Path
        output = Path(args.output)
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(text, encoding="utf-8")
        print(f"Saved synthetic scenario plan to {output}")
    else:
        print(text, end="")


if __name__ == "__main__":
    main()
