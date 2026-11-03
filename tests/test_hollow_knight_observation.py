from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

import pytest

from mindforge_neuro.game_observation import GameObservation, ObservationMonitor
from mindforge_neuro.activity_preview import ActivityPreviewBridge
from mindforge_neuro.context_transport import GameContextPacket, OrbContextBridge
from mindforge_neuro.combat_context import OrbMode

ROOT = Path(__file__).resolve().parents[1]


def native_event(event: str, seq: int, **kwargs) -> GameObservation:
    return GameObservation.make(
        "hollow_knight", event, seq,
        session_id="hk-observation-test",
        game_version="synthetic-installed-version",
        **kwargs,
    )


def test_hk_native_hooks_have_distinct_real_api_provenance():
    attack = native_event("hero_attack", 1)
    assert attack.hook_target == "ModHooks.AttackHook"
    assert native_event("hero_ready", 2).hook_target == "ModHooks.HeroUpdateHook"
    assert native_event("heartbeat", 3).hook_target == "ModHooks.HeroUpdateHook"
    assert native_event("scene_changed", 4, in_game=False).hook_target ==         "SceneManager.activeSceneChanged"
    assert GameObservation.from_json(attack.to_json()) == attack
    with pytest.raises(ValueError):
        GameObservation.make("hollow_knight", "hero_dash", 5)


def test_hk_rejects_silksong_harmony_masquerading_as_modding_api():
    record = json.loads(native_event("hero_attack", 1).to_json())
    record["hook_target"] = "HeroController.Attack"
    with pytest.raises(ValueError):
        GameObservation.from_dict(record)
    schema = json.loads((ROOT / "contracts/game_observation.v1.schema.json").read_text())
    assert "ModHooks.AttackHook" in schema["properties"]["hook_target"]["enum"]


def test_hk_observation_never_promotes_player_actions_to_boss_or_decoding():
    observer = ActivityPreviewBridge("hollow_knight")
    for seq, (name, now) in enumerate([
        ("hero_ready", 0.), ("hero_attack", .2), ("hero_attack", .4)
    ], 1):
        assert observer.accept(native_event(name, seq), now)
    context_packet = observer.snapshot(.45)
    assert context_packet is not None
    assert context_packet.context.player_activity_recent
    assert context_packet.source == "synthetic_game_context"
    assert not context_packet.context.enemy_engaged
    assert not context_packet.context.boss_encounter
    assert not context_packet.context.recovery_opening
    assert context_packet.anchor_x is None
    bridge = OrbContextBridge("hollow_knight")
    assert bridge.accept(GameContextPacket.from_json(context_packet.to_json()), .45)
    decision = bridge.snapshot(.46)
    assert decision["mode"] == "primed"
    assert decision["stimulus_enabled"] is False
    assert decision["selection_opportunity_only"] is False
    assert decision["placement"] == "edge_compact"


def test_hk_observation_withdraws_on_scene_change_and_needs_new_hero_ready():
    observer = ActivityPreviewBridge("hollow_knight")
    events = [(1, "hero_ready", 0., True),
              (2, "hero_attack", .1, True),
              (3, "hero_attack", .2, True),
              (4, "scene_changed", .3, False),
              (5, "hero_attack", .4, True),
              (6, "hero_attack", .5, True)]
    for seq, name, now, in_game in events:
        assert observer.accept(native_event(name, seq, in_game=in_game), now)
    state = observer.snapshot(.55)
    assert not state.context.player_activity_recent
    assert observer.accept(native_event("hero_ready", 7), .6)
    assert observer.accept(native_event("hero_attack", 8), .7)
    assert observer.accept(native_event("hero_attack", 9), .8)
    assert observer.snapshot(.85).context.player_activity_recent


def test_hk_installed_mod_is_explicit_optin_and_unsubscribes():
    source = (ROOT / "mods/hollow-knight/Mindforge.HKObservation/"
              "HollowKnightObservation.cs").read_text()
    display = (ROOT / "mods/hollow-knight/Mindforge.HKObservation/"
               "HKActivityOverlay.cs").read_text()
    assert 'Mindforge.Observer.optin' in source
    assert 'ENABLE_READ_ONLY_GAME_TELEMETRY_V1' in source
    assert 'ModHooks.AttackHook += OnAttack' in source
    assert 'ModHooks.AttackHook -= OnAttack' in source
    assert 'ModHooks.HeroUpdateHook += OnHeroUpdate' in source
    assert 'ModHooks.HeroUpdateHook -= OnHeroUpdate' in source
    assert 'SceneManager.activeSceneChanged += OnSceneChanged' in source
    assert 'SceneManager.activeSceneChanged -= OnSceneChanged' in source
    assert 'new GameObject("MindforgeHKReadOnlyDiagnostic")' in source
    assert 'next.stimulus_enabled' in display
    assert 'next.selection_opportunity_only' in display
    assert 'Time.timeScale <= 0.0001f' in display
    assert "hollow_knight" in display
    assert "ModifyDamage" not in source and "raw_eeg" not in source.lower()


def test_hk_native_reference_preflight_never_invents_modding_api_qualification(tmp_path):
    managed = tmp_path / "hollow_knight_Data/Managed"
    managed.mkdir(parents=True)
    (managed / "Assembly-CSharp.dll").write_bytes(b"fixture-api-unverified")
    (managed / "UnityEngine.dll").write_bytes(b"fixture-unity")
    run = subprocess.run(
        [sys.executable, str(ROOT / "tools/mindforge_native_preflight.py"),
         "--game", "hollow_knight", "--game-root", str(tmp_path)],
        cwd=ROOT, capture_output=True, text=True, timeout=8,
    )
    assert run.returncode == 0, run.stderr + run.stdout
    data = json.loads(run.stdout)
    assert data["game_id"] == "hollow_knight"
    assert data["build_reference_prerequisites_present"] is True
    assert data["modding_api_verified"] is False
    assert data["native_plugin_compiled"] is False
    assert data["runtime_hook_validated"] is False
    assert str(tmp_path) not in run.stdout


def test_hk_synthetic_audit_does_not_fabricate_hooks_or_boss_phases():
    run = subprocess.run(
        [sys.executable, str(ROOT / "tools/mindforge_game_observe.py"),
         "--game", "hollow_knight", "--demo"],
        cwd=ROOT, capture_output=True, text=True, timeout=8,
    )
    assert run.returncode == 0, run.stderr + run.stdout
    assert "SYNTHETIC" in run.stdout
    data = json.loads(run.stdout[run.stdout.index("{"):])
    assert data["counts"]["hero_attack"] == 2
    assert data["boss_phase_verified"] is False
    assert data["safe_ssvep_window_verified"] is False
