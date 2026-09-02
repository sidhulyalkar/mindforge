# V0.26 Production Geometry + Cathedral Depth

V0.26 is the first pass that treats the remaining world-rendering problem as a **mesh and depth problem**, not another lighting-polish problem.

V0.23 established trustworthy collision authority. V0.24 established the white-cathedral architecture and route grammar. V0.25 removed historical runtime presentation conflicts and promoted the high-fidelity URP/post stack. The remaining gameplay-camera weakness is that many visible V0.24 modules still resolve to Unity's built-in cube mesh, and V0.25's deliberately flat ambient fill can make the cavern, cathedral, supports and wall surfaces collapse into the same value range.

## What V0.26 changes

### Primitive render geometry becomes production geometry

V0.26 walks the semantic V0.24 cathedral and replaces built-in cube **render meshes** on structural supports, boundary walls, retaining structure and ornament with a deterministic chamfered block mesh. Walkable floor skins and mystic/data accents are deliberately excluded.

This does not replace or move BoxCollider/MeshCollider authority. V0.23 and the canonical route still own collision. The change is visual only: corners catch highlights, silhouettes stop reading as raw boxes, and SSAO has real beveled geometry to describe.

### Buttresses stop being three stacked boxes

V0.24 buttresses use `Foot + Body + Crown` block construction. V0.26 disables only those old renderers, keeps any existing collision untouched, and overlays a tapered buttress shell plus a small cathedral-spire finial. Cloister, Choir and apse supports therefore taper into the architecture instead of looking like scaled cubes placed on top of one another.

### Wall panels gain actual recess depth

Narthex and nave wall panels receive pointed-arch niche frames and inset sills. Wide sanctuary panels use three bays; smaller nave panels use one. The darker existing inset remains behind the new frame, producing a proper foreground-frame / recessed-shadow / wall hierarchy instead of a flat rectangle with a differently colored rectangle on it.

### Transverse ribs become a continuous vault

V0.24 had strong cathedral ribs but large stretches of visible cavern between them. V0.26 connects the five established vault stations with four deterministic inward-facing Gothic vault webs. Three restrained longitudinal crown ribs break up the broad ceiling surface.

The vault mesh uses inward/downward triangle winding because the gameplay camera observes it from below. It remains collider-free. The V0.23 cavern shell is still the physical ceiling authority.

### Cavern depth is separated from cathedral depth

V0.24 intentionally normalized too much of the scene into one cool-stone family while establishing a coherent art direction. V0.26 introduces three derived material roles:

- `V26_DeepCavern`: darker, rougher enclosing cavern/backwall surfaces;
- `V26_DistantStone`: slightly lifted but desaturated outer terrain;
- `V26_VaultPlaster`: pale vault web material that sits between white architecture and dark rock.

All variants retain the existing production shader/textures and change only material response/tint.

### Flat ambient fill becomes vertical ambient depth

V0.25's flat ambient mode was useful while diagnosing the greybox-runtime conflict. V0.26 switches to tri-light ambience:

- brighter cool sky fill;
- mid-value equator fill;
- darker ground bounce.

Fog begins farther from the player and resolves toward a deeper blue-slate distance color. Shadow reach is extended from the old 52 m showcase value to at least 68 m so long nave/cloister views do not lose all structural shadowing halfway down the frame.

## Authority boundary

V0.26 is editor-authored static presentation.

It does **not** create colliders, Rigidbody components, damage, attacks, movement, target-lock state, Flux, boss scheduling, Wisp state, calibration state, neural evidence or time-varying stimuli. The new V0.26 root is validated to remain collider-free and Rigidbody-free.

Existing collision authority remains the canonical V0.11/V0.23 world. V0.24 remains architecture/layout authority. V0.25 remains runtime sensory presentation authority.

## Local playtest gate

Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)** and inspect from the ordinary gameplay camera, not only Scene view.

Focus on these views:

1. walk slowly through the Causeway nave and look for clean beveled highlight transitions on bases, capitals, walls and trim;
2. orbit the camera close to narthex/nave wall panels and verify the pointed recesses read as depth rather than decals;
3. inspect Cloister, Choir and apse buttresses from oblique angles and verify the old stacked-box silhouette is gone;
4. look upward from the nave/cloister and verify the ribs now belong to one continuous ceiling rather than floating independently in an open dark roof;
5. look down the long route and verify white architecture separates from the darker cavern and distant terrain without losing playable-floor readability;
6. hammer the Choir ascent, outer terrain, boss arena and camera collision again to confirm that visual replacement did not disturb the already-qualified collision authority.

If V0.26 still reads as a prototype after those changes, the next step should be authored character/hero props and region-specific facade modules, not another global post-processing pass.
