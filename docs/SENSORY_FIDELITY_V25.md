# V0.25 Sensory Fidelity + Data Cathedral

## Why this tranche exists

V0.24 fixed the architectural grammar and world-integrity problems, but the resulting playtest still reads too much like a greybox. That is not because the repository lacks every polish system. The current canonical scene has a **presentation-routing problem**:

- V0.24 intentionally uses a narrow deterministic module kit, and many walls, retaining pieces, trims and buttresses are still cube-derived geometry;
- the canonical V0.11 Guardian shell is still assembled from capsules, cubes and spheres;
- the Fractured Signal has a stronger asymmetric silhouette in V0.19, but its stock materials still make large facets read as flat colored geometry;
- the V0.11 presentation firewall correctly suppresses the historical showcase installer, which also means the old pooled impact VFX and showcase post-processing are not automatically promoted into Latest;
- V0.17's HUD is coherent but deliberately utilitarian and screen-space-heavy;
- V0.24 lighting is static and safe, but Latest was not yet explicitly running the repository's higher-fidelity URP/SSAO configuration.

V0.25 fixes that disconnect. It does **not** replace the V0.24 world or introduce new gameplay mechanics.

## Presentation ownership

V0.25 is split into five read-only owners.

### 1. Editor rendering owner: `SensoryFidelityV25Builder`

Owns only project/scene presentation configuration:

- promotes the existing `CinematicFidelityConfigurator` into Latest;
- enables HDR, depth/normals, four-cascade shadows, SSAO and screen-space shadows on the pinned URP 14 forward path;
- authors one global ACES volume with restrained bloom, color response, white balance and light vignette;
- lifts the white-cathedral ambient/key-light response;
- adds static collider-free cyan data inlays through the processional spine, choir rise and apse.

It does not bake gameplay into rendering and does not author a second collision surface.

### 2. Corruption owner: `FracturedSignalFidelityV25`

Owns only the Fractured Signal surface language.

The new `Mindforge/FracturedSignalV25` URP shader adds:

- low-amplitude vertex displacement;
- main-light depth response;
- fresnel fracture glow;
- separate armor, edge, core and void material roles.

The motion scale is forced to zero throughout calibration or an armed Wisp resonance field. The boss remains visually expressive in conventional combat without becoming an uncontrolled periodic stimulus during neural evidence collection.

### 3. Combat/locomotion feedback owner

`CombatVfxOrchestrator` is promoted into the canonical V0.11 path rather than copied. V0.25 adds `MindforgeLocomotionVfxV25` for jump, double-jump, dash, air-dash and landing consequences using the same bounded `PresentationFxPool`.

`MindforgeCameraImpactV25` adds a deliberately tiny post-camera positional impulse after conventional combat consequences. It cannot alter target lock, damage, movement or FOV, and clears itself during neural visual fields.

Existing `HitStopController` remains the sole hit-stop authority. V0.25 does not invent another time-scale owner.

### 4. UI owner

V0.25 disables the V0.17 HUD after resolving its dependencies and replaces it with `MindforgeDemoHudV25` plus `MindforgeDiegeticGuideV25`.

The screen-space HUD is reduced to:

- Guardian health/endurance/Flux;
- Fractured Signal health;
- one compact neural-mode chip;
- neural-window/calibration instructions only when those windows are active.

Conventional prompts move into the world:

- `T // LOCK FRACTURED SIGNAL` near the target;
- `V HOLD // CHANNEL WISP` near the Guardian;
- short Sight/Guard/Concord action language near the Guardian.

Those diegetic prompts hide for the entire neural visual-field interval.

### 5. Audio owner

`MindforgeSpatialAudioV25` adds a restrained procedural boss hum and short conventional impact/dash tones. Boss ambience uses logarithmic 3D rolloff. Conventional one-shots are mostly local to the player.

All V0.25 audio is muted during calibration or Wisp resonance windows.

## What the current gameplay recording tells us

The current visual weaknesses are structural rather than isolated bugs:

1. **Shape vocabulary is still too primitive.** Even with a white-cathedral palette, long planar cubes and simple columns read as editor geometry when viewed at combat-camera distance.
2. **Material response is not enough without light separation.** Pale surfaces need contact shadow, local occlusion, highlight rolloff and controlled bloom or they collapse into flat grey planes.
3. **The boss contrast is correct but depth is wrong.** Bright magenta against a quiet white cathedral is the right macro decision. The remaining problem is that the facets do not yet feel like one corrupting volumetric organism.
4. **Combat systems have more feel than the canonical presentation reveals.** Hit stop and event seams already exist, but the clean V0.11 presentation path was not surfacing all pooled consequence VFX.
5. **The HUD competes with the world.** The bottom instructional banner explains mechanics, but its visual language makes the experience feel like a test harness.
6. **The camera is functional but impacts are visually under-resolved.** Movement is readable, yet strikes and evasions do not currently alter the sensory frame enough to communicate weight.

V0.25 addresses points 2 through 6 directly. Point 1 requires the next dedicated asset/mesh-replacement tranche rather than hiding primitive silhouettes under more bloom.

## Deliberate non-goals

V0.25 does not:

- change boss damage, cadence, phase logic or projectile speed;
- change Guardian acceleration, dodge, jump, hover or air-dash authority;
- change Wisp pause semantics or SSVEP frequencies/phases;
- add fullscreen flashes, rhythmic environment pulsing or neural-driven lighting;
- replace V0.23/V0.24 floor or collision authority;
- pretend that procedural boxes are final production environment art.

## Next geometry tranche

After V0.25 is visually verified, the highest-value follow-up is a **V0.26 Cathedral Asset Replacement** pass:

- replace cube wall panels with recessed/chamfered production meshes;
- replace block buttresses with tapered authored meshes;
- replace the remaining primitive Guardian shell with a proper skinned or high-quality modular character mesh while preserving root-motion separation;
- give the nave and apse proper vault surfaces instead of relying on ribs plus broad backing planes;
- introduce sparse authored eco-tech elements such as water channels, luminous moss beds and frosted data-glass only after the primary architecture reads cleanly;
- add a capture-based visual-regression checklist for silhouette, floor seams, floating pieces, contact shadows and material diversity.

The rule stays the same: **remove a weak visual authority before adding another one.**

## V0.25 playtest gate

Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)** and inspect in this order:

1. At spawn, pale stone should separate from recessed dark geology without crushing blacks.
2. Column/floor contacts should have visible occlusion and shadow weight.
3. Cyan floor inlays should guide the route without looking like a second SSVEP or a combat telegraph.
4. The Choir ramp must remain one physical floor despite its new presentation strip.
5. Lock the Fractured Signal and circle it. The boss should show dark armor depth, hot fracture edges and restrained surface motion rather than uniform flat pink.
6. During calibration/Wisp resonance, boss surface motion, diegetic prompts, camera impact and V0.25 audio must suppress/freeze.
7. Dash, double-jump, land, strike and perfect-guard. Each should have short bounded VFX feedback without obscuring the camera.
8. Confirm the old bottom conventional prompt banner is gone and lock/channel prompts are anchored in-world.
9. Verify neural-window instructions remain explicit and screen-stable.
10. Re-run the ascent, outer terrain and boss bowl to confirm V0.23/V0.24 collision integrity has not regressed.
