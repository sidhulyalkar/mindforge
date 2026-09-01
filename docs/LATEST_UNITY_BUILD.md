# Canonical Unity build

## One development target

For ordinary Mindforge development there is exactly one supported Unity entry point:

**Mindforge → Latest → PLAY LATEST (BCI Simulation)**

This rebuilds and opens the canonical integrated scene, then enters Play Mode.

The current product label is **V0.25 Sensory Fidelity + Data Cathedral**. The scene asset remains `Assets/Mindforge/Scenes/MindforgeDemoV11.unity` because V0.11 is the clean systems/traversal assembler version, not the complete-game product version.

`MindforgeLatestEditorMenu.BuildCanonical(...)` now has seven deterministic authoring stages:

1. `MindforgeDemoV11Builder.BuildDemoScene(...)` creates the authoritative systems and traversal kernel.
2. `WorldSoulV20Builder.ApplyOpenScene()` creates continuous terrain/material/world grammar.
3. `WorldCohesionV21Builder.ApplyOpenScene()` applies the arena correction and earlier local cohesion work.
4. `WorldIntegrityV22Builder.ApplyOpenScene()` normalizes structural render state and seals the broad cavern/world envelope.
5. `WorldFoundationV23Builder.ApplyOpenScene()` reconciles visible geometry with collision, makes generated terrain explorable and fixes the inward cavern/foundation shell.
6. `WorldCathedralV24Builder.ApplyOpenScene()` removes obsolete foreground clutter and imposes the canonical white-cathedral material, module, floor, lighting and architectural grammar.
7. `SensoryFidelityV25Builder.ApplyOpenScene()` promotes the pinned high-fidelity URP configuration, SSAO/screen-space shadows, ACES/bloom/color response and static collider-free data-cathedral inlays.

Runtime then composes the maintained Guardian/combat presentation, Fractured Signal movement/scheduler, manual-Wisp intermission and SSVEP/telemetry systems. V0.25 additionally installs the canonical sensory presentation root for pooled combat/locomotion VFX, bounded camera impact, Fractured Signal surface depth, quieter HUD/diegetic prompts and restrained spatial audio.

## What V0.25 changes

V0.24 solved architectural trust. V0.25 tackles the next problem visible in playtest captures: the game still **presents like a greybox even when the world logic is increasingly mature**.

The root causes are explicit in the codebase:

- many V0.24 structural modules are still cube-derived deterministic geometry;
- the canonical V0.11 Guardian shell is still primitive-based;
- the Fractured Signal has an improved procedural silhouette but stock surface response makes its large facets look flat;
- the canonical V0.11 presentation firewall correctly blocks the historical showcase stack, which also means old post/VFX helpers were not automatically reaching Latest;
- the V0.17 HUD is functional but intentionally utilitarian.

V0.25 fixes the presentation-routing and sensory-depth problems without destabilizing the world:

- promotes `CinematicFidelityConfigurator` into Latest rather than forking another URP asset;
- enables HDR, depth/normals, four-cascade shadows, SSAO and screen-space shadows on the pinned URP 14 forward renderer;
- adds ACES tonemapping, restrained bloom, high-key color response, white balance and a very light vignette;
- lifts white-cathedral ambient/key response so pale stone separates from recessed geology;
- adds static cyan processional data inlays through the nave, market, choir rise and apse with **zero colliders**;
- promotes existing pooled combat VFX into the canonical V0.11 path;
- adds bounded dash/jump/landing VFX and tiny conventional-combat camera impulses;
- adds a custom Fractured Signal shader with low-amplitude vertex displacement, main-light depth and fresnel fracture edges;
- replaces the V0.17 conventional bottom prompt with world-space lock/channel/action prompts;
- keeps neural calibration/resonance instructions screen-stable and explicit;
- adds restrained spatial boss ambience and conventional action tones.

All dynamic V0.25 presentation freezes, hides or mutes during calibration or Wisp resonance windows.

See `docs/SENSORY_FIDELITY_V25.md` for the full critique, ownership boundaries and playtest gate.

## What each world stage owns

The stack is intentionally layered rather than mutually authoritative:

- **V0.11**: gameplay systems and canonical traversal surfaces.
- **V0.20**: deterministic outer landforms/world surfaces.
- **V0.21**: first-boss bowl geometry correction and cohesion.
- **V0.22**: opaque structural normalization, cavern/perimeter containment and boss-duel stability.
- **V0.23**: floor/collision reconciliation, terrain collision and inward cavern/foundation shell.
- **V0.24**: white-cathedral palette, modular architecture, cleanup and final foreground world grammar.
- **V0.25**: render fidelity and read-only sensory presentation only.

V0.25 does not become a second world generator and does not own collision.

## Canonical composition

The canonical build combines:

- clean V0.11 systems/traversal authority;
- V0.20 deterministic terrain and world-surface generation;
- V0.21 boss-arena geometry correction;
- V0.22 structural opacity, cavern envelope and stable boss-duel behavior;
- V0.23 floor/collision reconciliation, terrain collision and inward cavern shell;
- V0.24 white-cathedral material palette and deterministic generated stone textures;
- V0.24 semantic modular building kit (`FloorSkin`, `Column`, `PointedArch`, `Buttress`, `WallPanel`, `LumenSconce`, etc.);
- V0.24 narthex, nave, cloister, choir and Fractured Signal apse composition;
- V0.25 high-fidelity URP/SSAO/shadow/post stack;
- V0.25 static data-cathedral route inlays;
- V0.25 pooled combat and locomotion consequence VFX;
- V0.25 Fractured Signal depth/corruption surface treatment;
- V0.25 diegetic conventional prompts and compact neural-aware HUD;
- current Guardian responsive movement, double jump, hover, air dash and physical sword/guard authority;
- current Wisp/SSVEP/display-timing/persistence/telemetry systems.

## Graphics engineering policy

World authoring uses public codebases as engineering references, not an asset landfill. V0.20's deterministic noise remains adapted from MIT-licensed `SebLague/Procedural-Landmass-Generation`; the mesh-recipe workflow remains informed by MIT-licensed `aadebdeb/ProceduralMesh`; V0.23 uses MIT-licensed `SebLague/Procedural-Cave-Generation` as a reference for shared visible/physical cave topology.

V0.24 and V0.25 do not import another game's cathedral art or a competing environment framework. Generated V0.24/V0.25 materials and profiles live under ignored `Assets/Mindforge/Generated` paths.

When borrowing from public projects:

- confirm the upstream license before adapting code or logic;
- record the upstream and usage in `third_party/manifest.json` where applicable;
- include required license notices when source is actually vendored or substantially adapted;
- prefer narrow techniques over importing a competing world-authority framework;
- do not copy another game's character identity, level art or visual signature;
- preserve the SSVEP visual-control boundary.

## Authority boundary

V0.25 is presentation only.

It does not create damage, modify boss cadence, call locomotion requests, award Flux, change target authority, mutate persistence, or create neural evidence. Existing `HitStopController` remains the one hit-stop owner. V0.25 reads authoritative combat and locomotion events and emits optional pooled effects downstream.

The data inlays are collider-free. Canonical V0.11/V0.23 route surfaces remain physical authority.

The custom Fractured Signal shader has a motion-scale freeze, and runtime sets that motion to zero through the complete calibration/Wisp resonance visual-field interval. Diegetic prompts, camera impact and V0.25 audio also suppress during that interval.

Software readiness still does not substitute for photodiode/display timing or real EEG qualification.

## Latest menu

`Mindforge → Latest` intentionally contains only:

- **PLAY LATEST (BCI Simulation)**: rebuild V0.11, apply V0.20 → V0.21 → V0.22 → V0.23 → V0.24 → V0.25, open and play in controller BCI simulation.
- **Rebuild Latest Integrated Scene**: perform the same deterministic build without Play Mode.
- **Open Latest Integrated Scene**: open the canonical scene and upgrade missing world layers in order.
- **Validate Latest Readiness**: run the maintained readiness audit. It is software/scene evidence, not physical SSVEP qualification.
- **Build Neural-Hardware Variant**: build the same world with controller-only qualification disabled for real neural-service/hardware testing.

## Manual Wisp and first-boss contract

Holding `V` remains the deliberate Wisp listening ritual. V0.25 does not alter that ceasefire, neural-link safety owner, target policy or combat scheduler.

Conventional combat presentation becomes richer outside neural windows. During neural evidence collection, V0.25 deliberately becomes quieter and more static.

## Legacy policy

Historical V0.5-V0.10 showcase/build commands and the old V0.11 menu are implementation history, not supported development entry points. Their Unity menu entries live only under:

**Mindforge → Legacy**

Do not compose a new release by manually running historical `Apply ...` commands. If the canonical latest scene needs an older capability, the latest assembler must call the smallest required implementation explicitly and deterministically.

There should never again be multiple equally plausible "latest" builders.

## V0.25 playtest flow

1. Pull the intended branch or `main` and allow Unity to compile/import.
2. Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)**.
3. At spawn, inspect pale stone contacts. Columns/floors should have substantially better shadow and occlusion weight than V0.24.
4. Follow the cyan data inlays through the nave and market. They should guide direction without behaving like flashing stimuli or combat telegraphs.
5. Re-test the Choir ascent with jump, double-jump, hover and air dash. The inlay must not create collision or a second floor.
6. Lock the Fractured Signal and orbit it. It should read as dark fractured mass with hot edges/core rather than uniform flat magenta geometry.
7. Dash, jump, double-jump, land, strike and perfect-guard. Pooled effects should be short and bounded; camera impact should be noticeable but small.
8. Confirm conventional lock/channel guidance is anchored in world space rather than a persistent bottom-screen banner.
9. Start calibration/Wisp resonance. Boss displacement must freeze and V0.25 diegetic prompts, camera kick and audio must suppress while coded-core instructions remain explicit.
10. Re-run outer terrain and boss-bowl exploration to confirm no V0.23/V0.24 collision regression.
11. Run **Mindforge → Latest → Validate Latest Readiness** and inspect the Console.

The next dedicated visual tranche after V0.25 should be **production mesh/character replacement**, not another layer of post-processing. V0.25 makes the current architecture readable; it does not pretend primitive-derived silhouettes are final art.