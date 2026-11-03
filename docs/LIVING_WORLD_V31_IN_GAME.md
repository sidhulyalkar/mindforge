# Living World v3.1: in-game opt-in and qualification

## Scope

This is the first in-game integration of the passive neural-feature channel, scoped specifically to the **authored Legacy V0.11** route. The runtime installer requires both a MindforgeDemoV11Marker and the Mindforge_Demo_World_V11 world root. It does **not** claim to integrate the newer Latest/cathedral routes. The canonical Latest scene reuses the V11 marker and root, so the integration additionally checks for the Mindforge_White_Cathedral_V24 child and **disables itself explicitly** there, preventing misplaced legacy accents.

Passive visual effects are **OFF by default**. No packet receiver is bound and no ornament is visible until explicit user opt-in (or a previously saved opt-in). The existing Sight/Guard combat and 10/12 Hz SSVEP systems are untouched.

### Visual design

Six thin, collider-free unlit decorative LineRenderer accents are placed on sanctuary side walls (X ±9.35, Z -19.5/-13.5/-7.5). Only these dedicated material property blocks respond to the passive scalar. No global lighting, camera, fog, world physics, combat telegraph, enemy, coded stimulus or photodiode is modified.

The signal is posterior alpha relative spectral power, **not** a validated estimate of emotion, focus, or imagined speech.

## Run on your Mac

1. Fetch and switch to branch feat/living-world-v31-production-optin.
2. Open the unity/ project using **Unity 2022.3.62f3**.
3. Choose **Mindforge → Legacy → V0.11 Demo → Build + Play Presentable Demo**.
4. Choose **Mindforge → Living World → V3.1 → Enable Opt-in Preview** (or open in-game F10, sometimes Fn+F10 on a Mac). To turn off: **Disable and Restore Baseline** or the F10 menu.
5. From the repo root run:

       python -m pip install -e '.[test]'
       python tools/mindforge_living_world.py --demo --seconds 80 --hz 5 \
         --output-tape experiments/tapes/v31-passive-demo.jsonl

The six sanctuary accents should respond slowly. They should be completely hidden during calibration/SSVEP resonance windows. They should return to a baseline color after stream interruption. Visual changes must not alter combat, stimulus luminance/frequency/phase, camera, collisions, boss health, or neural action authority.

The F10 menu defaults to **reduced visuals ON**, and user intensity 16% (capped to 6% while reduced visuals is enabled). User can opt out immediately. All decorative surfaces are owned and destroyed with the marker scene. The passive socket is localhost-only on port 19747 and opens only with opt-in, separate from active neural authority port 19742.

## Strict semantic parity study

The new recorder listens only to the **observer GameMarker** lane on localhost port 19745. Avoid another consumer on that port. Run once for a controller/BCI-tape baseline with passive disabled:

    python tools/mindforge_capture_game_markers.py \
      --seconds 90 --output experiments/reports/v31/markers-off.jsonl

Reset the scene and replay the **same deterministic controller and BCI decisions**, with passive enabled. Record the second run:

    python tools/mindforge_capture_game_markers.py \
      --seconds 90 --output experiments/reports/v31/markers-on.jsonl

Compare ordered gameplay facts:

    python -m mindforge_neuro.living_world_parity \
      --off experiments/reports/v31/markers-off.jsonl \
      --on experiments/reports/v31/markers-on.jsonl \
      --output experiments/reports/v31/semantic-parity.json

The comparator checks event, category, stage, action, target, reason, value (rounded to three decimals), boss phase, stimulus epoch and planned duration. It intentionally ignores per-run session ID and wall-clock timestamps. It detects missing, extra and changed gameplay events but **cannot prove physics trajectory, photon timing, render performance or neural decoder validity**. Comparing different manual playthroughs is not a valid parity study.

## Release gates

| Gate | Requirement | Current |
| --- | --- | --- |
| P0 | Complete repo Python regression matrix including v3.1 | Check exact-head GitHub CI |
| P1 | Unity 2022.3.62f3 scene import, native EditMode tests | Unobserved until Unity actually executes |
| V11 visual | Verify six accents, opt-in persistence, F10, collision-free geometry | Needs local playtest |
| SSVEP physical | Compare display photodiode frequency/phase and real discriminator behavior on/off | Unobserved |
| Gameplay parity | Capture and compare identical deterministic marker traces on/off | Unobserved |
| Performance | GPU/CPU frame-time p95 and allocations, matched hardware, passive off/on | Unobserved |
| Accessibility | Confirm legibility, comfort, photosensitive controls, and clean opt-out | Human check pending |

A green GitHub Unity workflow containing a *skipped actual Unity test step* is NOT a native compilation gate. Python CI is a software-contract gate only.

## Next release

Build an adapter for the actual current Latest-world presentation owner only after locating its camera/HUD/atmosphere architecture, and run physical display/marker regression tests. Do not introduce global light modulation or automatic imagined-speech actions before evidence exists.

This is a **stacked draft** following v1 PR #62, v2 PR #63 and v3 PR #64.
