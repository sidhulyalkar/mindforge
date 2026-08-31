# Canonical Unity build

## One development target

For ordinary Mindforge development there is exactly one supported Unity entry point:

**Mindforge → Latest → PLAY LATEST (BCI Simulation)**

This rebuilds and opens the canonical integrated scene, then enters Play Mode.

The current product label is **V0.17 Directed Demo**. The scene asset remains `Assets/Mindforge/Scenes/MindforgeDemoV11.unity` because V0.11 is the version of the clean world assembler, not the version of the complete game. Renaming the scene solely to chase product-version numbers would create unnecessary serialized-asset churn.

The canonical build combines:

- the clean V0.11 district/world assembler;
- current Guardian movement and combat authority;
- the V0.14 synchronized SSVEP epoch/decoder contract;
- the V0.15 EEG-ready cinematic and neural-quiet presentation;
- the V0.16 recording-driven world readability layer;
- the V0.17 directed gameplay camera, target-presence presentation and canonical demo HUD;
- current persistence, telemetry and BCI interfaces.

`WispResonanceWindow`, `VisualIdentityV16Installer` and `MindforgeDirectedDemoV17` install their current runtime layers after scene load. The clean scene builder therefore does not duplicate version-specific BCI/presentation wiring.

## Latest menu

`Mindforge → Latest` intentionally contains only:

- **PLAY LATEST (BCI Simulation)** — normal day-to-day test command. Rebuilds the current integrated scene in controller-only qualification mode, opens it and starts Play Mode. During a Wisp listening window, Editor-only `1`, `2`, `0` simulate Sight, Guard and abstain.
- **Rebuild Latest Integrated Scene** — rebuild without entering Play Mode.
- **Open Latest Integrated Scene** — open the existing canonical scene; builds it first only if the asset is absent.
- **Validate Latest Readiness** — run the current scene/runtime readiness audit. In Play Mode it verifies V0.16/V0.17 ownership, the two coded VEP stimuli, display-timing software contract, canonical HUD ownership and the post-reveal camera handoff. This is diagnostic evidence, not physical SSVEP qualification.
- **Build Neural-Hardware Variant** — rebuild with controller-only qualification disabled. Use this only when testing the live neural service/hardware path on a display that has been physically qualified.

## Directed demo behavior

After the cinematic/calibration sequence returns combat authority, V0.17 transfers gameplay-camera ownership from the legacy V0.11 camera to a closer fixed-56° composition. The Guardian is intentionally larger on screen, target-lock framing is tighter, and the far clip remains large enough for the V0.16 skyline.

The camera never creates movement, attacks, target lock or neural evidence. During calibration or an armed Wisp window, user-orbit and target-driven yaw changes freeze and camera follow becomes more damped. This reduces avoidable background optic flow while preserving the coded cores' camera-relative angular geometry.

The canonical HUD presents one reading order:

1. Guardian HP / endurance / flux;
2. current Fractured Signal health and conventional target-lock state;
3. neural-link readiness;
4. one contextual action line: lock target, channel Wisp, maintain gaze, exploit Sight, execute Guard or use Concord.

The V0.11 and ProductionHudV09 runtime HUDs are disabled once V0.17 owns the Latest demo presentation.

## Legacy policy

Historical V0.5-V0.10 showcase/build commands and the old V0.11 menu are implementation history, not supported development entry points. Their underlying builder methods remain in source because the current assemblers may still reuse pieces and because they are useful for archaeology/recovery. Their Unity menu entries live only under:

**Mindforge → Legacy**

Do not compose a new release by manually running historical `Apply ...` commands. If the canonical latest scene needs an older builder, the latest assembler must call it explicitly and deterministically.

## Playtest flow

1. Pull the intended branch/`main` and allow Unity to finish compiling/importing.
2. Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)**.
3. Let the intro/reveal complete. Confirm the closer V0.17 gameplay camera takes ownership only after combat controls return.
4. Walk around large architecture and verify V0.16 camera ghosting improves visibility while collision remains unchanged.
5. Press `T` to lock The Fractured Signal. Confirm the target-presence ring appears and the top-center health hierarchy is readable.
6. Hold `V` to Channel Wisp. During the neural window confirm the target ring disappears, camera orbit is stabilized and no decorative scene recomposition occurs.
7. During `LISTENING`, press `1` for Sight, `2` for Guard or `0` for abstain.
8. Verify the contextual HUD changes to the selected tactical payoff and release/timeout/abstention invents no aura.
9. Run **Mindforge → Latest → Validate Latest Readiness** while the canonical scene is playing. The report is written to `experiments/reports/latest-readiness-v17.json` and explicitly records `physical_ssvep_qualified=false`.

For real BCI testing, stop Play Mode, run **Build Neural-Hardware Variant**, start the neural service and then play the canonical scene. A green readiness report still does not substitute for photodiode timing or real EEG qualification.

## Development rule

There should never again be multiple equally plausible "latest" builders. A new world/combat revision updates the implementation behind `Mindforge → Latest`; it does not add another top-level version menu.
