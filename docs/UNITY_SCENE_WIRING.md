# Unity Competition Scene Wiring

This file turns the code architecture into a concrete competition-scene checklist.

## Neural runtime root

Create `MindforgeNeuralRuntime` with `UdpNeuralReceiver`, `DualAuraCombatDirector`, `NeuralEvidenceHud`, and `NeuralHapticFeedback`. Use UDP port `19742`, a bounded queue, a queue-age limit around 0.75 s, and a stale connection threshold around 2.5 s. The spectator evidence HUD should not cover the player's action-gaze corridor.

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

Guardian root should contain `GuardianMotor`, `GuardianCombatController`, `AuraBuffController`, `FluxMeter`, `CombatantVitals`, `GravityBloomAbility`, and `ProjectileNearMissSensor` with shared `CombatTuning`, `HitStopController`, and `CombatPresentationDirector` references.

All Concord consumers must use `AuraBuffController.ConcordActive`.

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

## Transport stress rehearsal

With neurOS `UnicornMock`, inject LSL jitter, dropped chunks, source silence and recovery while triggering Counter Pulse, Rift Cleave, Signal Break, Twin Eclipse and heavy particle load.

Verify:

```text
queue stays bounded
old packets are discarded
no conflicting aura-selection burst
PARTICIPANT_STOP survives
BCI loss/recovery is visible
controller combat never blocks
```

## Display qualification

Software checks:

- refresh/VSync locked;
- `DisplayTimingMonitor` healthy;
- no long-frame spike during Twin Eclipse;
- VEP cores stay visible;
- no temporal post effect touches VEP cores.

Physical photodiode checks:

- Sight core;
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
3. controller-only fight completes end-to-end;
4. Phantom Unicorn reaches Unity through LSL → Python → UDP;
5. forced render stalls do not burst stale neural authority;
6. physical VEP timing is measured;
7. physical Unicorn acquisition is verified;
8. stationary, moving, player-movement, and full-combat human sessions are completed.
