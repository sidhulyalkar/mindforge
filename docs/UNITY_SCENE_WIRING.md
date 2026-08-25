# Unity Competition Scene Wiring

This file turns the code architecture into a concrete competition-scene checklist.

## Neural runtime root

Create `MindforgeNeuralRuntime` with:

- `UdpNeuralReceiver`
  - derived-event UDP port `19742`
  - bounded queue
  - queue-age limit around `0.75 s`
  - stale connection threshold around `1.5 s`
- `DualAuraCombatDirector`
- `NeuralEvidenceHud`
- `NeuralHapticFeedback`
- `NeuralLinkContingency`
- `MindforgeSessionLogger`

The spectator evidence HUD should not cover the player's action-gaze corridor.

## Awakening calibration room

Add an `AwakeningCalibrationDirector` and `CalibrationMarkerSender`.

Python should be running `tools/run_unity_calibrated_decoder.py` before the ritual begins. The sequence is a handshake:

```text
Python LSL connected
  -> CALIBRATION_SERVICE_READY
Unity
  -> baseline 5 s
  -> Sight 10 Hz 5 s
  -> Guard 12 Hz 5 s
  -> markers on UDP 19743
Python
  -> resting-alpha diagnostic
  -> labeled session calibration
  -> CALIBRATION_READY or CALIBRATION_FAILED
Unity
  -> arena opens only after READY
```

Recommended calibration hierarchy:

```text
AwakeningRoom
├── Guardian
├── SoulWispCalibrationRoot
│   ├── WispCore
│   ├── SightAuraRoot
│   └── GuardAuraRoot
└── CalibrationStatusText
```

Wire `AwakeningCalibrationDirector.linkContingency` so successful calibration arms demo-day signal-loss protection. Failed calibration remains in the room and can be retried with Enter by default.

## Soul Wisp hierarchy

```text
SoulWispRoot
├── WispCore
├── SightAuraRoot
│   ├── SightVepCore        <- VepAuraStimulus owns luminance
│   └── SightFeedbackShell  <- NeuralAuraFeedback owns shell/particles
└── GuardAuraRoot
    ├── GuardVepCore        <- VepAuraStimulus owns luminance
    └── GuardFeedbackShell  <- NeuralAuraFeedback owns shell/particles
```

Never put `NeuralAuraFeedback` on the VEP core renderer.

Create one `MindforgeVisualPalette` asset and assign it to the Wisp, projectile prefabs, feedback shell and boss telegraph. Sight blue / Guard green are reserved.

## Guardian combat

Guardian root should contain `GuardianMotor`, `GuardianCombatController`, `GuardianCombatInput`, `AuraBuffController`, `FluxMeter`, `CombatantVitals`, `GravityBloomAbility`, and `ProjectileNearMissSensor` with shared `CombatTuning`, `HitStopController`, and `CombatPresentationDirector` references.

All Concord consumers must use `AuraBuffController.ConcordActive`.

`NeuralLinkContingency` should reference `GuardianCombatInput`. During a stale neural link, ordinary movement remains available but attack, parry, dash, and Gravity Bloom actions are disabled so the paused boss cannot become free damage.

## Camera hierarchy

```text
FollowRig               <- normal tracking / lock-on
└── ImpactPivot          <- CombatPresentationDirector owns local offset
    └── GameplayCamera
```

Do not let impact presentation and follow tracking write the same transform. Environment lights may opt into dimming; VEP core materials must ignore `_MindforgeAmbientDim`.

## Fractured Signal boss

Boss root:

- `CombatantVitals`
- `PoiseSystem`
- `FracturedSignalDirector`
- `SignalBreakReward`
- `FracturedSignalTelegraph`

Wire the Wisp, player's `FluxMeter`, telegraph component, Echo prefab, projectile prefab, and shared `CombatPresentationDirector`.

Also wire `NeuralLinkContingency.bossDirector` to this director. Its `SetExternalPause` must pause the attack scheduler and all active `FracturedEchoNode` firing.

Echo prefab:

- `FracturedEchoNode`
- `CombatantVitals`
- optional `PoiseSystem`
- hostile projectile prefab
- angular/fractured visual mesh

Provide enough `LineRenderer` rays for the largest Phase III fan plus one radial ring. Telegraphs should use hostile crimson/orange only.

## Hit-stop targets

```text
light            0.020 s
Counter Pulse    0.020 s
Rift Cleave      0.055 s
Signal Break     0.080 s
Twin Eclipse     0.120 s
```

`HitStopController` remains the single scaled-time freeze authority.

## Projectile language

```text
enemy normal  -> crimson/magenta
enemy heavy   -> orange-red
Guardian      -> ivory
reflected     -> violet
```

Enemy meshes should be angular. Neural targets remain smooth.

## Photodiode qualification patch

Place a UI `Image` at the absolute bottom-right and attach `PhotodiodePatch`.

- source -> Sight `VepAuraStimulus`
- F10 toggles the patch by default
- white/black follows the Sight high/low phase
- patch holds black during VEP rest
- size it to about one physical inch on the qualification monitor

The patch is qualification-only. During a human run, either disable it or physically cover it with the photodiode. An uncovered 10 Hz black/white square is itself another visual stimulus.

## Neural-link contingency

After calibration, deliberately silence the Phantom source for more than 1.5 seconds.

Expected behavior:

```text
receiver becomes stale
  -> NEURAL LINK UNSTABLE
  -> boss scheduler paused
  -> Echo firing paused
  -> Guardian offensive actions disabled
  -> movement remains available
  -> existing buffs continue to expire on realtime
  -> source recovers
  -> ~0.75 s stable recovery dwell
  -> combat resumes
```

Do not pause only the boss while leaving player attacks enabled.

## Session telemetry

Wire `MindforgeSessionLogger` to:

- `UdpNeuralReceiver`
- `AwakeningCalibrationDirector`
- `NeuralLinkContingency`
- `FracturedSignalDirector`
- boss/player `CombatantVitals`
- player `FluxMeter`

The logger writes no raw EEG. It periodically atomically replaces a `.partial.json`, then finalizes `mindforge.session.v1` under `Application.persistentDataPath/mindforge_sessions` on victory, defeat, or application quit.

Generate the judge artifact with:

```bash
python tools/plot_session_report.py path/to/mindforge-SESSION.json \
  --out session-report.png \
  --pdf session-report.pdf
```

Use the phrase **neural-control robustness**, not unmeasured cognitive fatigue. `EMG_SUSPECTED` remains an engineering flag, not confirmed EMG measurement.

## Transport stress rehearsal

With neurOS `UnicornMock`, inject LSL jitter, dropped chunks, source silence and recovery while triggering Counter Pulse, Rift Cleave, Signal Break, Twin Eclipse and heavy particle load.

Verify:

```text
queue stays bounded
old packets are discarded
no conflicting aura-selection burst
PARTICIPANT_STOP survives
normal combat never blocks socket I/O
link loss enters the explicit fair-pause state
recovery does not burst old authority
```

## Display qualification

Software checks:

- refresh/VSync locked;
- `DisplayTimingMonitor` healthy;
- no long-frame spike during Twin Eclipse;
- VEP cores stay visible;
- no temporal post effect touches VEP cores.

Physical photodiode checks:

- Sight core / qualification patch;
- Guard core;
- idle timing;
- full boss load;
- Counter Pulse;
- Signal Break transition;
- Twin Eclipse;
- resume after visual rest.

## Demo provenance

Every judge-facing run visibly says `SIMULATION`, `REPLAY`, or `LIVE`. Phantom Unicorn is engineering evidence, never human BCI evidence.

## Promotion gate

Do not call the Unity vertical slice qualified until:

1. Unity 2022.3 imports and compiles the complete project;
2. scene/prefab references are serialized correctly;
3. Awakening succeeds and fails/retries correctly with Phantom;
4. controller-only fight completes end-to-end;
5. Phantom Unicorn reaches Unity through LSL -> Python -> UDP;
6. forced render stalls do not burst stale neural authority;
7. >1.5 s source silence enters and recovers from fair pause;
8. telemetry finalizes and the report generator produces PNG/PDF output;
9. physical VEP timing is measured;
10. physical Unicorn acquisition is verified;
11. stationary, moving, player-movement, and full-combat human sessions are completed.
