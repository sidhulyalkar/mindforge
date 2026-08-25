# Unity Competition Scene Wiring

This file turns the code architecture into a concrete competition-scene checklist.

## 1. Neural runtime root

Create `MindforgeNeuralRuntime` with:

- `UdpNeuralReceiver`
  - port `19742`
  - max queue age ~`0.75 s`
  - stale connection threshold ~`2.5 s`
- `DualAuraCombatDirector`
  - receiver → `UdpNeuralReceiver`
  - buffs → Guardian `AuraBuffController`
- `NeuralEvidenceHud`
  - receiver → same `UdpNeuralReceiver`
- `NeuralHapticFeedback`
  - receiver → same receiver
  - buffs → same `AuraBuffController`

The evidence HUD is spectator-facing. It should not cover the player's action-gaze corridor.

## 2. Soul Wisp hierarchy

Recommended hierarchy:

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

`SoulWispController` owns target positioning and receives the shared `CombatVisualPalette`.

## 3. Visual palette

Create one `MindforgeVisualPalette` asset from `CombatVisualPalette`.

Assign it to:

- `SoulWispController`;
- projectile prefabs;
- `NeuralAuraFeedback`.

The exact Sight blue / Guard green must not be reused by enemy fire or generic player ordnance.

## 4. Guardian combat

Guardian root:

- `GuardianMotor`
- `GuardianCombatController`
- `AuraBuffController`
- `FluxMeter`
- `CombatantVitals`
- `GravityBloomAbility`
- `ProjectileNearMissSensor`

Shared references:

- `CombatTuning` asset;
- `HitStopController`;
- `CombatPresentationDirector`;
- current boss target.

`GuardianCombatController.ConcordActive` and `GravityBloomAbility` must use `AuraBuffController.ConcordActive`, not instantaneous Sight+Guard overlap.

## 5. Camera feel

Camera hierarchy:

```text
FollowRig               <- authoritative tracking / lock-on
└── ImpactPivot          <- CombatPresentationDirector owns local offset only
    └── GameplayCamera
```

Do not put `CombatPresentationDirector` on the same transform that the follow/lock-on system writes directly.

Assign:

- `impactPivot` → dedicated child;
- `gameplayCamera` → actual camera;
- ambient lights → non-BCI environment lights only;
- optional low-pass filter → combat/music mix source;
- optional Signal Break pulse → bass/heartbeat cue.

VEP core materials should ignore the `_MindforgeAmbientDim` shader global.

## 6. Boss

Fractured Signal root:

- `CombatantVitals`
- `PoiseSystem`
- `FracturedSignalDirector`
- `SignalBreakReward`

Wire `FracturedSignalDirector.soulWisp` to the player's Wisp so Signal Break calls `RestStimuli`.

Wire `SignalBreakReward.presentation` to the same `CombatPresentationDirector` so poise collapse also triggers audio/ambient rest.

`PoiseSystem.breakDuration` and `signalBreakVisualRestSeconds` should remain intentionally aligned for the competition build unless experiment data justifies separating them.

## 7. Hit-stop values

Initial competition tuning:

```text
light            0.020 s
Counter Pulse    0.020 s
Rift Cleave      0.055 s
Signal Break     0.080 s
Twin Eclipse     0.120 s
```

`HitStopController` must remain the single authority for scaled-time freezes.

## 8. Projectile visual language

Enemy projectile prefabs should use angular meshes.

`MindforgeProjectile` colors them automatically when a `CombatVisualPalette` is assigned:

```text
enemy normal  -> crimson/magenta
enemy heavy   -> orange-red
Guardian      -> ivory
reflected     -> violet
```

The VEP target colors are deliberately absent from this list.

## 9. Display qualification

Software checklist:

- VSync / refresh locked;
- `DisplayTimingMonitor` healthy;
- no long-frame spike during Twin Eclipse;
- VEP core remains visible during all boss patterns;
- no temporal post effect applied to VEP cores.

Physical checklist:

- photodiode on Sight core;
- photodiode on Guard core;
- idle timing;
- full boss load;
- Counter Pulse hit-stop;
- Signal Break rest transition;
- Twin Eclipse;
- resume after rest.

## 10. Demo provenance

Every judge-facing run must visibly identify one of:

```text
SIMULATION
REPLAY
LIVE
```

A Phantom Unicorn run is useful engineering evidence but must never be presented as observed human BCI performance.
