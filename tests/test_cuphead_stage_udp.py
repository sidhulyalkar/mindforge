from __future__ import annotations

import json
import socket
import subprocess
import sys
import time
from pathlib import Path

from mindforge_neuro.cuphead_stage import (
    CupheadStageObservation, SOURCE,
)

ROOT=Path(__file__).resolve().parents[1]


def free_port() -> int:
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as s:
        s.bind(("127.0.0.1", 0))
        return s.getsockname()[1]


def native_like(stage: str, event: str, seq: int, source_t: float,
                *, version: str = "test-pinned-build") -> bytes:
    data=json.loads(CupheadStageObservation.synthetic(
        stage,seq,event=event,source_time_s=source_t).to_json())
    data["source"]=SOURCE
    data["game_version"]=version
    return CupheadStageObservation.from_dict(data).to_json().encode("utf8")


def test_native_labeled_cuphead_udp_observation_is_audit_only(tmp_path):
    port=free_port()
    while port in (19742,19747,19752,19753,19754,19755,19756):
        port=free_port()
    report=tmp_path/"report.json"
    tape=tmp_path/"tape.jsonl"
    cmd=[sys.executable,str(ROOT/"tools/mindforge_cuphead_stage.py"),
         "--seconds","3.1","--listen-port",str(port),
         "--expected-build","test-pinned-build",
         "--output",str(tape),"--report",str(report)]
    child=subprocess.Popen(cmd,cwd=ROOT,stdout=subprocess.PIPE,
                           stderr=subprocess.PIPE,text=True)
    try:
        # Repeatedly emit unique ordered samples until startup completes.
        # The receiver is intentionally not assumed ready after a fixed sleep.
        with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as send:
            start=time.monotonic()
            seq=0
            while time.monotonic()-start < 1.55:
                seq+=1
                time_s=seq * 0.07
                stage="level" if seq%3 else "unknown"
                event="sample" if stage=="level" else "scene_changed"
                send.sendto(native_like(stage,event,seq,time_s),
                            ("127.0.0.1",port))
                time.sleep(.055)
        out,err=child.communicate(timeout=9)
        assert child.returncode==0,err+out
        assert report.exists(),err+out
        data=json.loads(report.read_text(encoding="utf8"))
        accepted=[json.loads(s) for s in tape.read_text(encoding="utf8").splitlines()]
        assert len(accepted)>=3, "no native-labeled Cuphead UDP records accepted"
        assert all(x["source"]==SOURCE for x in accepted)
        assert all(x["game_id"]=="cuphead" for x in accepted)
        assert data["stage"]=="unknown", "source timeout must withdraw stage"
        assert data["stage_samples"]["level"]>=2
        assert data["stage_samples"]["unknown"]>=1
        assert data["combat_engagement_verified"] is False
        assert data["parry_window_verified"] is False
        assert data["ssvep_stimulus_enabled"] is False
    finally:
        if child.poll() is None:
            child.terminate()
            try:
                child.communicate(timeout=3)
            except subprocess.TimeoutExpired:
                child.kill()
                child.communicate(timeout=3)
