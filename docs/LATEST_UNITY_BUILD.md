# Canonical Unity build

## One development target

For ordinary Mindforge development there is exactly one supported Unity entry point:

**Mindforge → Latest → PLAY LATEST (BCI Simulation)**

This deterministically rebuilds and opens the canonical integrated scene, then enters Play Mode.

The current product label is **V0.28 Professional Creature + World Staging**. Its explicit predecessor is **V0.27 Guardian Embodiment + Fractured Beast**, which remains composed as the Guardian/encounter-presentation stage beneath V0.28. The scene asset remains `Assets/Mindforge/Scenes/MindforgeDemoV11.unity` because V0.11 is the clean systems/traversal assembler version, not the complete-game product version.

`MindforgeLatestEditorMenu.BuildCanonical(...)` now has ten ordered authoring stages:

1. `MindforgeDemoV11Builder.BuildDemoScene(...)` creates the authoritative systems and traversal kernel.
2. `WorldSoulV20Builder.ApplyOpenScene()` creates continuous terrain/material/world grammar.
3. `WorldCohesionV21Builder.ApplyOpenScene()` applies arena correction and local cohesion work.
4. `WorldIntegrityV22Builder.ApplyOpenScene()` normalizes structural render state and seals the broad cavern/world envelope.
5. `WorldFoundationV23Builder.ApplyOpenScene()` reconciles visible geometry with collision and fixes the inward cavern/foundation shell.
6. `WorldCathedralV24Builder.ApplyOpenScene()` imposes the white-cathedral palette, cleanup, processional route and architectural grammar.
7. `SensoryFidelityV25Builder.ApplyOpenScene()` promotes the pinned URP fidelity stack, restrained post-processing, static data inlays and maintained sensory presentation.
8. `WorldRenderingV26Builder.ApplyOpenScene()` replaces remaining primitive cathedral render silhouettes, adds continuous vault webs and restores cavern/material depth.
9. `CombatEmbodimentV27Builder.ApplyOpenScene()` supplies Guardian sword-arm embodiment and collider-free encounter staging.
10. `ProfessionalEncounterV28Builder.ApplyOpenScene()` replaces the procedural boss proxy with pinned authored quadruped art, derives the matching sword-contact envelope, installs actor-separation/camera readability support and adds sparse socketed cathedral dressing.

## What V0.28 changes

V0.27 proved that the first boss needed an animal body, but the generated proxy still read as primitive art and could visually overlap the Guardian. V0.28 treats those as structural production problems rather than shader problems.

The pass now:

- keeps boss-player minimum spacing inside `FracturedSignalFirstBossV19`, the existing boss movement owner;
- replaces the V0.27 generated creature with Gobkit's pinned CC0 rigged `Rhino.glb` anatomy and authored idle/walk/attack/death clips;
- normalizes and grounds the imported body from its actual renderer bounds;
- derives four trigger-only anatomical sword hurt volumes from that rendered body so head, chest, flank and rear contact agree with visible anatomy;
- disables the old humanoid-sized V0.22 boss combat hull;
- adds a bounded V0.28 camera post-resolver only when the boss actually occludes the Guardian sight corridor;
- imports a deliberately tiny CC0 KayKit subset for banners, mounted fixtures, chairs, relic tables and a reliquary;
- places those props only from deterministic side sockets with a protected processional corridor and boss clear radius;
- removes all collision/Rigidbody authority from decorative imported props and applies Mindforge's existing white-cathedral materials;
- reduces half of the V0.27 radial floor-line noise rather than adding another full visual layer;
- keeps all dynamic V0.28 creature/camera behavior neutral during neural visual fields.

See `docs/PROFESSIONAL_ENCOUNTER_V28.md` for the full authority, provenance and validation contract.

## Public art acquisition

The first V0.28 rebuild may need network access in the Unity Editor because the source art is acquired from immutable GitHub commit URLs into the ignored `Assets/Mindforge/Generated/V28/ThirdParty` cache.

This is intentional. Source art is not manually dragged into the project and is not fetched at runtime. `PublicAssetAcquisitionV28` verifies every download against its exact upstream Git blob SHA-1 before import. A missing network connection or hash mismatch fails the V0.28 build closed rather than silently restoring the procedural creature.

The pinned sources are:

- **Gobkit Free Assets**, CC0, commit `0d654ab3306515b1b63621a5c6548554034482dc`, using only `animal/Rhino.glb`.
- **KayKit Dungeon Remastered 1.0**, CC0, commit `b0ca9bd96a8072ab36a3a5464f00ed1e06a16d07`, using only five selected OBJ props.
- **Khronos UnityGLTF**, MIT, pinned to `release/2.20.0`, used solely for glTF import.

Exact asset-level provenance is recorded in `third_party/manifest.json`.

## Authority boundary

The physical sword sweep remains owned by `GuardianSwordShieldController`. Multiple V0.28 hurt boxes all resolve to the same `CombatantVitals`, and the sword already deduplicates each receiver per swing.

Boss locomotion remains owned by `FracturedSignalFirstBossV19`; V0.28 adds its minimum-separation contract there rather than creating a second push controller. Boss attack scheduling remains unchanged.

The canonical V0.17 gameplay camera still owns orbit, fixed FOV and ordinary framing. `MindforgeActorOcclusionGuardV28` is a bounded post-resolver that only acts when target renderer bounds block the camera-to-Guardian sight corridor.

V0.23 remains world collision/foundation authority. V0.28 decorative staging is collider-free.

Neural calibration and Wisp systems remain the only owners of the neural visual-field interval. V0.28 animation/camera corrections freeze or neutralize during that interval.

## Latest menu

`Mindforge → Latest` intentionally contains only:

- **PLAY LATEST (BCI Simulation)**: rebuild V0.11 → V0.20 → V0.21 → V0.22 → V0.23 → V0.24 → V0.25 → V0.26 → V0.27 → V0.28, open and play in controller BCI simulation.
- **Rebuild Latest Integrated Scene**: perform the same deterministic build without Play Mode.
- **Open Latest Integrated Scene**: open the canonical scene and upgrade missing layers in order.
- **Validate Latest Readiness**: run the maintained readiness audit. It is software/scene evidence, not physical SSVEP qualification.
- **Build Neural-Hardware Variant**: build the same product with controller-only qualification disabled for real neural-service/hardware testing.

Historical build commands remain implementation history. Do not manually compose a release from old `Apply ...` commands.

## V0.28 focused playtest

1. Pull the V0.28 branch and allow Unity Package Manager to resolve UnityGLTF.
2. Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)**. The first build downloads and hash-verifies the pinned CC0 art if the generated cache is empty.
3. Walk directly into the Fractured Signal from several angles. The boss should retreat before the Guardian disappears inside the visible body.
4. Lock on at point-blank range and circle both directions. The V0.17 camera remains primary; the V0.28 occlusion guard should only make small corrections when the creature blocks the Guardian sightline.
5. Attack head, chest, both flanks and rear. The Aetherblade should register on visible anatomy instead of passing through broad parts of the creature.
6. Observe idle, movement, attack and death behavior. The boss should read as one authored quadruped with skeletal motion, not a procedural dinosaur or shard cloud.
7. Traverse Memory Forge, Causeway and Market slowly. Side dressing should create lived-in cathedral detail while the central processional corridor remains visually and physically open.
8. Run the full boss-floor perimeter. No imported decorative prop should intrude into the protected encounter radius.
9. Trigger a Wisp/calibration visual window. Creature animation correction and actor-occlusion correction must neutralize while coded visual evidence is active.
10. Run **Mindforge → Latest → Validate Latest Readiness** and capture the Console plus a new gameplay video.

V0.28 should be judged by coherence and negative space, not raw object count: one readable cathedral, one readable Guardian, one credible corrupted animal, and clear combat geometry between them.
