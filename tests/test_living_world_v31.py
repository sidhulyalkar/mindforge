from __future__ import annotations

import json
from pathlib import Path
import subprocess
import sys

import pytest

from mindforge_neuro.living_world_parity import (
    canonical_marker, compare_semantics, load_marker_trace,
)
from mindforge_neuro.markers import GameMarker


def marker(seq: int, *, event: str = "COMBAT_ACTION", action: str = "DODGE",
           session: str = "offline-session", value: float = 0.5) -> GameMarker:
    return GameMarker(
        schema="mindforge.game_marker.v1", seq=seq, session_id=session,
        event=event, category="combat", unity_realtime_s=1.25 + seq,
        game_time_s=1.25 + seq, frame=33 + seq, fixed_tick=22 + seq,
        stage=None, action=action, value=value,
    )


def test_identical_gameplay_with_different_clock_session_identifiers_passes():
    off = [canonical_marker(marker(1)), canonical_marker(marker(2))]
    on = [canonical_marker(marker(100, session="other")), canonical_marker(marker(200, session="other"))]
    report = compare_semantics(off, on)
    assert report["pass"] is True
    assert report["off_digest"] == report["on_digest"]
    assert not report["physical_time_parity_proven"]
    assert not report["render_parity_proven"]


def test_action_changes_fail_and_point_to_semantic_field():
    a = [canonical_marker(marker(1)), canonical_marker(marker(2))]
    b = [canonical_marker(marker(1)), canonical_marker(marker(2, action="ATTACK"))]
    report = compare_semantics(a, b)
    assert report["pass"] is False
    assert report["first_mismatch_index"] == 1
    assert report["mismatched_fields"] == ["action"]


def test_missing_or_extra_marker_fails_even_when_prefix_matches():
    baseline = [canonical_marker(marker(1)), canonical_marker(marker(2))]
    current = [canonical_marker(marker(1))]
    report = compare_semantics(baseline, current)
    assert not report["pass"] and report["first_mismatch_index"] == 1
    assert report["off_marker_count"] == 2 and report["on_marker_count"] == 1


def test_empty_trace_cannot_pass():
    with pytest.raises(ValueError, match="both runs"):
        compare_semantics([], [canonical_marker(marker(1))])


def test_roundoff_tolerance_is_explicit_and_order_preserved():
    a = [canonical_marker(marker(1, value=.50004))]
    b = [canonical_marker(marker(1, value=.50009))]
    assert compare_semantics(a, b)["pass"]
    assert not compare_semantics(a, [canonical_marker(marker(2, value=.502))])["pass"]


def test_jsonl_load_and_cli_return_codes(tmp_path):
    off_path, on_path, out = tmp_path / "off.jsonl", tmp_path / "on.jsonl", tmp_path / "parity.json"
    off_path.write_text(marker(1).to_json() + "\n" + marker(2).to_json() + "\n")
    on_path.write_text(marker(1, session="fresh").to_json() + "\n" +
                       marker(2, session="fresh").to_json() + "\n")
    assert len(load_marker_trace(off_path)) == 2
    command = [sys.executable, "-m", "mindforge_neuro.living_world_parity", "--off",
               str(off_path), "--on", str(on_path), "--output", str(out)]
    done = subprocess.run(command, capture_output=True, text=True)
    assert done.returncode == 0, done.stderr
    assert json.loads(out.read_text())["pass"]
    on_path.write_text(marker(1, action="ATTACK").to_json() + "\n")
    failed = subprocess.run(command, capture_output=True, text=True)
    assert failed.returncode == 2
    assert not json.loads(out.read_text())["pass"]


def test_malformed_or_legacy_markers_fail_closed(tmp_path):
    file = tmp_path / "broken.jsonl"
    file.write_text('{"schema":"other"}\n')
    with pytest.raises(ValueError, match="line 1"):
        load_marker_trace(file)
    file.write_text("\n")
    with pytest.raises(ValueError, match="empty"):
        load_marker_trace(file)


def test_v31_runtime_is_opt_in_and_never_shares_bci_authority():
    root = Path(__file__).resolve().parents[1] / "unity" / "Assets" / "Mindforge" / "LivingWorld"
    integration = (root / "LivingWorldExperienceV31.cs").read_text()
    surface = (root / "LivingWorldReactiveSurface.cs").read_text()
    assert "MindforgeDemoV11Marker" in integration
    assert 'PlayerPrefs.GetInt(EnabledKey, 0)' in integration
    assert "_connectionRoot.SetActive(false);" in integration
    assert "_visualRoot.SetActive(false);" in integration
    assert "F10" in integration
    assert "LivingWorldExperienceV31.MapFeature" in surface
    assert "Mindforge_Competition" not in integration
    assert "RenderSettings." not in integration
    assert "targetFrameRate" not in integration
    assert "AddForce" not in integration
    assert "AURA_SELECTED" not in integration
    assert "NeuralEvent" not in integration


def test_receiver_and_dedicated_visuals_stay_within_existing_passive_software():
    root = Path(__file__).resolve().parents[1] / "unity" / "Assets" / "Mindforge"
    integration = (root / "LivingWorld" / "LivingWorldExperienceV31.cs").read_text()
    assert "LineRenderer" in integration
    assert "ShadowCastingMode.Off" in integration
    assert "Collider" in integration  # Explicit doc prohibits creating colliders.
    assert "new Material(shader)" in integration
    assert 'Mindforge_Demo_World_V11' in integration
    assert "KeyCode.F10" in integration
    assert "GetComponentsInChildren<Transform>(true)" in integration
    assert "_visualRoot.transform.SetParent(authoredWorld, false);" in integration
    assert "GameObject.Find(AuthoredWorldRootName)" not in integration
    assert 'authoredWorld.Find("Mindforge_White_Cathedral_V24")' in integration
    assert "if (_visualRoot != null) Destroy(_visualRoot);" in integration
