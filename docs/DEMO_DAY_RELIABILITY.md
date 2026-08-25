# Demo-Day Reliability and Scientific Optics

This document defines the final non-gameplay systems that protect a BR41N.IO demonstration.

## 1. Awakening calibration

Calibration is an in-world handshake, not a decorative countdown.

```text
Python connects to verified LSL source
  -> CALIBRATION_SERVICE_READY
Unity Awakening room
  -> 5 s baseline, no periodic aura stimulus
  -> 5 s Sight / 10 Hz
  -> 5 s Guard / 12 Hz
  -> UDP presentation markers to Python
Python
  -> characterize resting posterior alpha
  -> fit labeled session score/margin gates
  -> require minimum separability
  -> CALIBRATION_READY or CALIBRATION_FAILED
Unity
  -> enter arena only after READY
```

Baseline is deliberately collected before SSVEP stimulation. Resting alpha is a confound diagnostic, not permission to relax artifact gates.

## 2. Photodiode patch

`PhotodiodePatch` is a qualification tool tied to `SightVepCore` phase.

- toggle with F10 by default;
- bottom-right square should be approximately 1 inch physically on the qualification display;
- high phase -> white;
- low phase -> black;
- stimulus rest -> black;
- tape/occlude the patch with the physical photodiode during participant sessions.

The patch validates frame-visible phase edges. It does not prove the exact luminance waveform emitted by the aura material, so direct aura measurement remains useful for final qualification.

## 3. Neural-link contingency

After calibration, `UdpNeuralReceiver` declares stale input after ~1.5 s without valid derived events.

The contingency response is intentionally fair rather than advantageous:

- boss scheduler pauses;
- Echo nodes stop firing;
- Guardian movement remains available;
- Guardian attack/parry/dash/Bloom actions are disabled;
- existing neural buffs continue to expire on real time;
- `NEURAL LINK UNSTABLE` appears;
- recovery must remain stable for ~0.75 s before authority resumes.

This prevents radio/network silence from killing the player or creating a free boss-damage window.

## 4. Session evidence

`MindforgeSessionLogger` records derived evidence and game events only. It never writes raw EEG.

Logged categories include:

- calibration stage;
- every coalesced neural evidence window;
- gameplay-authoritative neural events;
- link degradation/recovery;
- boss phase;
- Signal Break;
- Flux changes;
- victory/defeat/interruption.

A partial JSON checkpoint is written periodically and atomically replaced. Victory/defeat creates the final `mindforge.session.v1` artifact under `Application.persistentDataPath/mindforge_sessions`.

## 5. Judge report

```bash
python tools/plot_session_report.py path/to/mindforge-SESSION.json \
  --out session-report.png \
  --pdf session-report.pdf
```

The report uses conservative language:

- neural-control robustness;
- decoder winner margin;
- quality authority;
- accepted/abstained decisions;
- suspected artifact flags;
- accepted Sight/Guard selections;
- boss phases.

It does **not** label performance drift as cognitive fatigue, and `EMG_SUSPECTED` is explicitly an engineering flag rather than confirmed EMG measurement.

## Promotion gate

Before this layer is called competition-ready, observe:

1. Unity Editor/Player compile;
2. Awakening handshake with neurOS Phantom;
3. deliberate calibration failure and retry;
4. 2+ s Phantom source silence during Phase III;
5. fair pause and stable recovery;
6. telemetry JSON finalization after victory and defeat;
7. report generation;
8. photodiode timing under idle, Counter Pulse, Signal Break, and Twin Eclipse;
9. repeat the complete path with physical Unicorn.
