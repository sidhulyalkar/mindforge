# Canonical Unity build

## One development target

For ordinary Mindforge development there is exactly one supported Unity entry point:

**Mindforge → Latest → PLAY LATEST (BCI Simulation)**

This deterministically rebuilds and opens the canonical integrated scene, then enters Play Mode.

The current product label is **V0.27 Guardian Embodiment + Fractured Beast**. The scene asset remains `Assets/Mindforge/Scenes/MindforgeDemoV11.unity` because V0.11 is the clean systems/traversal assembler version, not the complete-game product version.

`MindforgeLatestEditorMenu.BuildCanonical(...)` now has nine ordered authoring stages:

1. `MindforgeDemoV11Builder.BuildDemoScene(...)` creates the authoritative systems and traversal kernel.
2. `WorldSoulV20Builder.ApplyOpenScene()` creates continuous terrain/material/world grammar.
3. `WorldCohesionV21Builder.ApplyOpenScene()` applies arena correction and local cohesion work.
4. `WorldIntegrityV22Builder.ApplyOpenScene()` normalizes structural render state and seals the broad cavern/world envelope.
5. `WorldFoundationV23Builder.ApplyOpenScene()` reconciles visible geometry with collision and fixes the inward cavern/foundation shell.
6. `WorldCathedralV24Builder.ApplyOpenScene()` imposes the white-cathedral palette, cleanup, processional route and architectural grammar.
7. `SensoryFidelityV25Builder.ApplyOpenScene()` promotes the pinned URP fidelity stack, restrained post-processing, static data inlays and maintained sensory presentation.
8. `WorldRenderingV26Builder.ApplyOpenScene()` replaces remaining primitive cathedral render silhouettes, adds continuous vault webs and restores cavern/material depth.
9. `CombatEmbodimentV27Builder.ApplyOpenScene()` adds collider-free encounter staging for the Fractured Signal while runtime presentation supplies the Guardian sword arm and new animalistic beast body.

Runtime then composes the maintained Guardian/combat presentation, Fractured Signal scheduler, manual-Wisp intermission and SSVEP/telemetry systems. V0.27 adds `GuardianCombatEmbodimentV27`, `FracturedSignalBeastV27` and `FracturedArenaDynamicsV27` downstream of those authorities.

## What V0.27 changes

V0.26 made the cathedral geometry read more like production architecture. The most obvious remaining prototype silhouettes were therefore the moving characters and the static boss-stage response.

V0.27 addresses those directly:

- replaces only the visible Guardian right arm/hand with an articulated shoulder → elbow → wrist presentation chain;
- solves that arm from `GuardianSwordShieldController` combo state and the actual runtime Aetherblade hilt, while keeping mathematical sword contact unchanged;
- adds bounded torso/chest/head/off-hand follow-through so attacks read as body motion rather than a floating blade;
- retires the V0.19 shard-knight render root in favor of a continuous low animalistic parasite body with jowls, maw, jaw, sensory eyes, forelimbs, feelers and dorsal signal crystals;
- drives beast pose only from existing movement, phase, telegraph, fired and damage events;
- adds thin floor rites, perimeter corruption spines, beast altar framing and encounter-local lights without adding colliders or Rigidbody components;
- makes arena scale/emission/light response follow the existing boss phase/attack events;
- freezes dynamic V0.27 presentation to neutral during Wisp calibration/resonance windows.

See `docs/COMBAT_EMBODIMENT_V27.md` for the detailed authority and playtest contract.

## What each stage owns

The stack is intentionally layered rather than mutually authoritative:

- **V0.11**: gameplay systems and canonical traversal surfaces.
- **V0.20**: deterministic outer landforms/world surfaces.
- **V0.21**: first-boss bowl geometry correction and cohesion.
- **V0.22**: structural opacity, cavern/perimeter containment and boss-duel stability.
- **V0.23**: floor/collision reconciliation, terrain collision and inward cavern/foundation shell.
- **V0.24**: white-cathedral palette, modular architecture, cleanup and final foreground world grammar.
- **V0.25**: render fidelity and read-only sensory presentation.
- **V0.26**: production world-render geometry and atmospheric/material depth only.
- **V0.27**: character embodiment and encounter-stage presentation only.

The physical sword sweep remains owned by `GuardianSwordShieldController`. The boss movement/attacks remain owned by the existing Fractured Signal combat components. V0.27 cannot create damage, reach, collision, locomotion, target selection, Flux or neural evidence.

## Canonical composition

The canonical build combines:

- clean V0.11 systems/traversal authority;
- V0.20 deterministic terrain and world-surface generation;
- V0.21 boss-arena geometry correction;
- V0.22 structural opacity, cavern envelope and stable boss-duel behavior;
- V0.23 floor/collision reconciliation, terrain collision and inward cavern shell;
- V0.24 white-cathedral material palette and deterministic generated stone textures;
- V0.24 semantic modular building kit;
- V0.24 narthex, nave, cloister, choir and Fractured Signal apse composition;
- V0.25 high-fidelity URP/SSAO/shadow/post stack;
- V0.25 static data-cathedral route inlays;
- V0.25 pooled combat/locomotion consequence VFX, compact HUD, diegetic prompts and spatial audio;
- V0.26 chamfered structural meshes, tapered buttresses, wall-niche depth, continuous vault webs and cavern depth separation;
- V0.27 articulated Guardian sword-arm embodiment;
- V0.27 animalistic Fractured Signal parasite presentation;
- V0.27 collider-free encounter rites, phase spines, altar frame and local arena response;
- current Guardian responsive movement, double jump, hover, air dash and physical sword/guard authority;
- current Wisp/SSVEP/display-timing/persistence/telemetry systems.

## Graphics engineering policy

World and character presentation code should improve **what the proven game renders**, not become a competing gameplay engine. Generated or procedural geometry can iterate quickly, but final production assets may later replace it behind the same presentation contracts.

When borrowing from public projects:

- confirm the upstream license before adapting code or logic;
- record the upstream and usage in `third_party/manifest.json` where applicable;
- include required license notices when source is actually vendored or substantially adapted;
- prefer narrow techniques over importing a competing world-authority framework;
- do not copy another game's character identity, level art or visual signature;
- preserve the SSVEP visual-control boundary.

## Authority boundary

V0.27 is presentation only.

The visible sword hilt may be translated to meet the solved gauntlet, but physical contact remains the fixed-tick mathematical sweep from `GuardianSwordShieldController`. The V0.27 boss body is a renderer hierarchy attached to the existing boss root; it creates no boss Rigidbody, collider, navigation or attack path. The V0.27 arena builder fails if its root contains any Collider or Rigidbody.

During neural evidence collection, Guardian embodiment, beast pose and arena dynamic response settle to neutral. Software readiness still does not substitute for photodiode/display timing or real EEG qualification.

## Latest menu

`Mindforge → Latest` intentionally contains only:

- **PLAY LATEST (BCI Simulation)**: rebuild V0.11 → V0.20 → V0.21 → V0.22 → V0.23 → V0.24 → V0.25 → V0.26 → V0.27, open and play in controller BCI simulation.
- **Rebuild Latest Integrated Scene**: perform the same deterministic build without Play Mode.
- **Open Latest Integrated Scene**: open the canonical scene and upgrade missing layers in order.
- **Validate Latest Readiness**: run the maintained readiness audit. It is software/scene evidence, not physical SSVEP qualification.
- **Build Neural-Hardware Variant**: build the same product with controller-only qualification disabled for real neural-service/hardware testing.

## Legacy policy

Historical build commands remain implementation history, not supported development entry points.

**Do not compose a new release by manually running historical `Apply ...` commands.** The latest assembler must call the smallest required implementation explicitly and deterministically.

There should never again be multiple equally plausible "latest" builders.

## V0.27 focused playtest

1. Pull the intended V0.27 branch and allow Unity to compile/import.
2. Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)**.
3. Lock onto the boss and orbit the Guardian. The Aetherblade should now visibly belong to a shoulder/elbow/wrist chain instead of floating beside the primitive torso.
4. Perform all three sword combo steps from frontal and oblique camera angles. Watch for elbow popping, wrist separation or sword/gauntlet drift.
5. Guard, dodge and immediately attack. The arm should recover into a readable combat-ready pose while physical contact behavior remains identical to V0.26.
6. Orbit the Fractured Signal. It should read first as one heavy low creature with a broad maw and second as a corrupted signal organism, not as a loose shard construct.
7. Observe attack telegraph/release and phase transitions. Head/jaw/crystals and arena spines/lights should reinforce, not obscure, the authoritative telegraph.
8. Traverse the complete boss floor and perimeter. New rites, altar and spines must have zero gameplay collision.
9. Trigger calibration/Wisp resonance. V0.27 dynamic presentation must settle to neutral while coded-core instructions remain explicit.
10. Run **Mindforge → Latest → Validate Latest Readiness** and inspect the Console.

The next tranche should be selected from the V0.27 recording. Likely candidates are full Guardian torso/leg replacement, richer beast material/skin breakup, authored creature attack locomotion and arena environmental ecology, but those should follow observed play rather than another speculative layer.
