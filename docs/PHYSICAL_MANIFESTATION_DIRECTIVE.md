# Mindforge Physical Manifestation Directive

**Feature freeze:** no new combat mechanics and no new neural target classes. Work is restricted to assembly, validation, and polish.

## Gate 1 — Software Crucible

### Reproducible Unity project

Mindforge is pinned to Unity `2022.3.76f1` and URP 14.0.11. From Unity Hub, open the repository's `unity/` directory.

After package import, run:

```text
Mindforge > Competition > Build Competition Scene
Mindforge > Competition > Validate Gate 1 Scene
```

The batch equivalent is:

```bash
Unity -batchmode -nographics -projectPath unity \
  -executeMethod Mindforge.Editor.CompetitionSceneAssembler.BuildAndValidate \
  -logFile unity-gate1.log
```

A successful validation writes `experiments/reports/unity-gate1-latest.json`.

### Golden path

1. Start neurOS Phantom Unicorn in strong-responder mode.
2. Start `tools/run_unity_calibrated_decoder.py --source-mode simulation`.
3. Launch the Unity scene.
4. Observe REST -> SIGHT -> GUARD calibration.
5. Require `CALIBRATION_READY` before combat input or arena authority unlocks.
6. Complete Phase I -> II -> III -> victory or defeat.
7. Verify a final `mindforge.session.v1` JSON exists.
8. Generate the PNG/PDF report with `tools/plot_session_report.py`.

### Torture tests

- **F6:** intentional ~50 ms Unity main-thread stall.
- **F7:** intentional ~120 ms Unity main-thread stall.
- neurOS Phantom `x`: two-second source silence.
- neurOS Phantom contamination controls: jaw/controller/motion/saturation.

Pass conditions:

- neural queue remains bounded;
- no burst of stale gameplay authority after a stall;
- calibration rejection remains in Awakening and can retry;
- source silence enters a neutral `NEURAL LINK UNSTABLE` pause;
- existing projectiles and active Gravity Bloom suspend coherently;
- recovery must stay stable before combat resumes;
- participant stop remains terminal;
- telemetry survives interruption.

## Gate 2 — Electro-optical bridge

The software target is a 120 Hz VSync-bound display, but software timing is not physical proof.

### Photodiode instrument

- **F10:** show/hide qualification square.
- **F11:** switch square between Sight 10 Hz and Guard 12 Hz source clocks.
- **F12:** export Unity-side phase-edge timestamps for comparison with scope data.

During human sessions the qualification square must be disabled or physically occluded by the photodiode because an exposed patch is itself an additional visual stimulus.

Measure both 10 Hz and 12 Hz under:

1. idle Awakening/arena;
2. Counter Pulse;
3. Rift Cleave hit-stop;
4. Signal Break transition and resume;
5. Phase III projectile density;
6. Twin Eclipse / worst presentation load;
7. F6/F7 forced stalls.

A software `DisplayTimingMonitor` health flag and edge CSV are diagnostics only. Promotion requires the oscilloscope/photodiode trace from the actual target display.

## Gate 3 — Unicorn wet lab

Do not start until Gates 1 and 2 pass on the target machine/display. Run stationary, moving-aura, controller-contamination, light-combat, and full-combat sessions. Tune only from observed `mindforge.session.v1` and calibration evidence. Do not infer cognitive fatigue from performance drift.

## Draft-PR promotion rule

PR #1 stays draft until at least: Unity Editor compile, serialized scene validation, Phantom golden path, calibration failure/retry, source-silence recovery, telemetry/report generation, and physical display timing are observed. Physical Unicorn and human combat remain subsequent evidence gates.
