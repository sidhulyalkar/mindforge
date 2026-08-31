# Canonical Unity build

## One development target

For ordinary Mindforge development there is exactly one supported Unity entry point:

**Mindforge → Latest → PLAY LATEST (BCI Simulation)**

This rebuilds and opens the canonical integrated scene, then enters Play Mode.

The current product label is **V0.13 Integrated**. The scene asset remains `Assets/Mindforge/Scenes/MindforgeDemoV11.unity` because V0.11 is the version of the clean world assembler, not the version of the complete game. Renaming the scene solely to chase product-version numbers would create unnecessary serialized-asset churn.

The canonical build combines:

- the clean V0.11 district/world assembler;
- current Guardian movement and combat authority;
- current encounter and presentation runtime;
- the V0.13 Channel Wisp resonance system;
- current persistence, telemetry and BCI interfaces.

`WispResonanceWindow` installs its current runtime after scene load, so the clean scene builder does not need to duplicate the BCI/Wisp scene wiring.

## Latest menu

`Mindforge → Latest` intentionally contains only:

- **PLAY LATEST (BCI Simulation)** — normal day-to-day test command. Rebuilds the current integrated scene in controller-only qualification mode, opens it and starts Play Mode. During a Wisp listening window, Editor-only `1`, `2`, `0` simulate Sight, Guard and abstain.
- **Rebuild Latest Integrated Scene** — rebuild without entering Play Mode.
- **Open Latest Integrated Scene** — open the existing canonical scene; builds it first only if the asset is absent.
- **Validate Latest Architecture** — run the clean-scene architecture audit. In Play Mode this also checks runtime ownership that cannot be proven in edit mode.
- **Build Neural-Hardware Variant** — rebuild with controller-only qualification disabled. Use this only when testing the live neural service/hardware path.

## Legacy policy

Historical V0.5-V0.10 showcase/build commands and the old V0.11 menu are implementation history, not supported development entry points. Their underlying builder methods remain in source because the current assemblers may still reuse pieces and because they are useful for archaeology/recovery. Their Unity menu entries live only under:

**Mindforge → Legacy**

Do not compose a new release by manually running historical `Apply ...` commands. If the canonical latest scene needs an older builder, the latest assembler must call it explicitly and deterministically.

## Playtest flow

1. Pull `main` and allow Unity to finish compiling/importing.
2. Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)**.
3. Verify the route loads from Memory Forge through the Fractured Signal encounter.
4. Engage an enemy. Ordinary movement/combat remains conventional input.
5. Hold `V` to Channel Wisp.
6. During `LISTENING`, press `1` for Sight, `2` for Guard or `0` for abstain.
7. Verify release, timeout or abstention applies no invented aura.
8. Run **Validate Latest Architecture** while the canonical scene is playing to exercise the fuller runtime audit.

For real BCI testing, stop Play Mode, run **Build Neural-Hardware Variant**, start the neural service and then play the canonical scene.

## Development rule

There should never again be multiple equally plausible "latest" builders. A new world/combat revision updates the implementation behind `Mindforge → Latest`; it does not add another top-level version menu.
