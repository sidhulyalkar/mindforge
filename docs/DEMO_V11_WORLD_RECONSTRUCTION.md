# Mindforge V0.11 — clean world reconstruction

V0.11 is a production-demo path, not another additive legacy showcase pass.

## Why it exists

The August 30 gameplay capture showed a systemic presentation failure: historical builders, collision proxies, runtime character shells, HUDs and VFX were all technically valid in isolation but visually stacked into one incoherent frame. The fix is architectural.

V0.11 keeps the tested competition systems kernel and rebuilds the player-facing world around it.

## Non-negotiable contracts

- Gameplay, damage, locomotion, BCI, telemetry and calibration remain inherited authorities.
- V0.11 world geometry does not invoke the V0.5–V0.10 world-builder chain.
- Primary route floors and walls are visible collision owners. No invisible proxy collision is required for the demo route.
- V0.11 has one runtime camera owner, one HUD owner and one Guardian visual owner.
- The historical `ShowcaseRuntimeInstaller` explicitly bails out when `MindforgeDemoV11Marker` is present.
- Controller-only demo mode is explicitly labelled and cannot be interpreted as BCI evidence.
- Boss and route Echo authority are distance/threshold gated through existing `SetExternalPause` seams; V0.11 does not invent a second attack scheduler.

## Current playable route

1. **Memory Forge Sanctum** — calm enclosed spawn, Forge core, readable threshold.
2. **Neon Causeway** — broad walled bridge, canal margins, first projectile Echo.
3. **Market of Broken Momentum** — wider courtyard, elevation pockets, second Echo.
4. **Choir Tower Ascent** — continuous collision-backed ramp with aerial room and third Echo.
5. **Fractured Signal** — walled boss arena on the raised dais.

The route is deliberately compact. The skyline and lower-city mass create scale without making the traversable footprint difficult to understand.

## How to run the presentable demo

Use Unity 2022.3.62f3.

1. Pull `feat/v11-world-reconstruction`.
2. Let Unity finish script compilation.
3. Use **Mindforge → V0.11 Demo → Build + Play Presentable Demo**.
4. The demo automatically enters explicitly-labelled controller-only qualification in Editor/development builds.
5. Controls: WASD move, Space jump/hover, Shift/RMB dodge, F/LMB blade, T target lock.

The builder creates `Assets/Mindforge/Scenes/MindforgeDemoV11.unity` as a generated review scene. The original competition scene and legacy showcase builder remain available.

## What V0.11 intentionally does not do yet

- It does not claim final production character art.
- It does not claim real BCI qualification.
- It does not migrate the full historical quest/persistence journey into the clean route yet.
- It does not use Content Foundry binary art bindings yet.
- It does not reactivate the full ten-enemy Menagerie roster.

Those are subsequent tranches after the clean demo passes an observed Unity playthrough.

## Qualification checklist

Before merging V0.11 into `main`, observe in Unity:

- zero compile errors;
- Guardian spawns visibly on the Sanctum floor;
- no legacy world geometry appears in the demo route;
- no camera wall fills the majority of the frame during ordinary movement;
- camera stays roughly three metres or farther from the Guardian except unavoidable collision recovery;
- all route side boundaries are visible and collision-backed;
- three route Echoes remain dormant until the Guardian approaches;
- Fractured Signal remains paused until the arena threshold;
- Guardian shell, Aetherblade and hands read as one coherent silhouette;
- sword trail remains narrow enough to keep enemies visible;
- only the compact V0.11 HUD is visible in controller-only mode;
- target lock can acquire nearby Echoes and the boss;
- jump, double jump, hover, dodge and blade chain remain functional;
- boss fight is reachable without leaving the collision-backed route;
- no BCI-off run is recorded or described as neural evidence.

## Next production tranche

Once the observed playthrough is clean, the next work should be:

1. authored district prefabs / stable presentation socket IDs;
2. production Guardian rig and animation rather than procedural shell geometry;
3. four strongly distinct ordinary enemy archetypes;
4. Archivist dialogue rebuilt as a safe interaction state;
5. Content Foundry replacement of the V0.11 architectural kit;
6. final lighting, audio and restrained combat VFX;
7. real BCI calibration and readability qualification.
