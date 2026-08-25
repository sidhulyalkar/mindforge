# Unity Competition Scene Wiring

This file turns the code architecture into a concrete competition-scene checklist.

## 1. Neural runtime root

Create `MindforgeNeuralRuntime` with:

- `UdpNeuralReceiver`
  - port `19742`
  - max queue age ~`0.75 s`
  - stale connection threshold ~`2.5 s`
  - bounded queue / bounded drain per frame
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
- `NeuralAuraFeedback`;
- `FracturedSignalTelegraph`.

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

## 6. Boss encounter

Fractured Signal root:

- `CombatantVitals`
- `PoiseSystem`
- `FracturedSignalDirector`
- `SignalBreakReward`
- optional `FracturedSignalTelegraph`

Wire:

- `FracturedSignalDirector.soulWisp` → player's Wisp;
- `FracturedSignalDirector.playerFlux` → player's FluxMeter;
- `FracturedSignalDirector.telegraph` → hostile-colored telegraph component;
- `FracturedSignalDirector.echoPrefab` → configured Echo prefab;
- `SignalBreakReward.presentation` → shared `CombatPresentationDirector`.

### Echo prefab

Echo prefab should contain:

- `FracturedEchoNode`
- `CombatantVitals`
- optional `PoiseSystem`
- hostile projectile prefab reference
- angular/fractured visual mesh

Destroying an Echo rewards Flux and should be visually readable without using Sight blue or Guard green.

### Telegraph object

Provide enough `LineRenderer` rays for the largest Phase III fan and one radial `LineRenderer` ring. Telegraph materials should use unlit/additive crimson or orange and should never be confused with the smooth neural targets.

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

## 10. Transport stress rehearsal

With neurOS `UnicornMock` active, inject:

- LSL delivery jitter;
- dropped chunks;
- 2 s source silence;
- recovery.

Simultaneously trigger:

- Counter Pulse;
- Rift Cleave;
- Signal Break;
- Twin Eclipse;
- high particle load.

Watch the spectator HUD transport counters and verify:

```text
queue stays bounded
old packets are discarded
no burst of conflicting aura selections
PARTICIPANT_STOP survives
BCI loss/recovery is visible
controller combat never blocks
```

## 11. Demo provenance

Every judge-facing run must visibly identify one of:

```text
SIMULATION
REPLAY
LIVE
```

A Phantom Unicorn run is useful engineering evidence but must never be presented as observed human BCI performance.

## 12. Promotion gate

Do not call the Unity vertical slice qualified until all of these are observed:

1. Unity 2022.3 Editor imports and compiles the full project;
2. scene/prefab references are serialized correctly;
3. controller-only fight completes end-to-end;
4. Phantom Unicorn reaches Unity through the full LSL → Python → UDP path;
5. forced render stalls do not burst stale neural authority;
6. physical VEP timing is measured;
7. physical Unicorn acquisition is verified;
8. stationary, moving, movement, and full-combat human sessions are completed.
