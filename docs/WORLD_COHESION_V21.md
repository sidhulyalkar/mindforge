# V0.21 Arena + Patina

V0.21 is a gameplay-camera-driven correction pass on top of V0.20 World Soul. The August 31 capture showed that the world is beginning to read as a place, but the first-boss encounter still exposes the procedural blockout underneath it: the apparent arena is larger than the comfortable movement plane, the Fractured Signal is leashed into a small central pocket, and pristine material boundaries reveal where authored pieces were assembled.

The design rule for this tranche is:

> Make space playable first, then make age and use visible at the seams.

## Recording diagnosis

The Fractured Signal is now much more legible as a moving character, but the arena still behaves like a decorated corridor. Three authored constraints compound:

- the canonical wall ring is only 13 m from center;
- the boss V0.19 home leash is 5.4 m;
- the raised 9 x 9 m inner dais owns collision and creates a second movement surface inside the duel.

The capture therefore repeatedly pushes the Guardian and boss into the same center pocket. At close range the boss silhouette dominates the camera and lateral dodge/lock-on movement loses room.

## Arena correction

`WorldCohesionV21Builder` retunes the existing V0.11 collision shell rather than adding an overlapping second arena:

- floor: 36 x 34 m;
- wall radius: 18.3 m;
- wall segments widened to keep the boundary visually continuous at the larger radius;
- central dais reduced to a shallow visual medallion and its collider removed;
- boss spires move to 16.4 m radius;
- V0.20 exterior crater rocks/signals are pushed beyond the new movement bowl so decorative geology never masquerades as a collision boundary.

The broad southern entrance remains open.

## Boss mobility correction

`FracturedSignalArenaMobilityV21` is a fail-closed tuning adapter over the V0.19 locomotion owner. It does not run its own `FixedUpdate`, move a Rigidbody, schedule attacks or read neural state.

The larger arena is paired with:

- phase preferred distances of 5.25 / 6.10 / 5.35 m;
- 9.0 m home leash;
- slightly higher movement speeds;
- stronger lateral orbit bias;
- a 2.35 s orbit-side hold instead of 3.2 s, reducing long wall-facing stalls;
- smaller 0.78 m movement probe;
- slightly shorter post-attack recovery.

Every reflected V0.19 field is validated before any value is written. If the V0.19 contract changes, V0.21 applies none of the profile and logs a loud error.

The V0.19 Wisp ceasefire remains untouched. Neural evidence still freezes boss movement/animation through the maintained authority path.

## Patina, not prop spam

The graphics work deliberately concentrates at contact zones and composition anchors.

### Material transitions

Thin collider-free moss, soil, damp-stone and rubble clusters break the hard seams where:

- Causeway masonry meets canal water;
- Sanctum and Market walls meet ground;
- the Ascent ramp meets surrounding geology.

These are static authored meshes using the existing V0.20 world palette and deterministic mesh/noise recipes.

### Arena history

The boss floor receives restrained fracture paths, soot chips and wall-foot erosion around the perimeter. The center remains quieter so attacks, sword silhouettes and SSVEP-facing combat presentation retain visual priority.

### Close-camera ecology

The Sanctum and Causeway receive small authored fern clusters and low leaf litter rather than more giant canopy blobs. These are intentionally concentrated at gameplay-camera distance, where V0.20's coarse procedural vegetation reads weakest.

### City depth and roofs

Near-Market buildings now get an intermediate facade layer between the route and the distant V0.20 skyline:

- recessed luminous windows;
- limestone piers and lintels;
- stone bases;
- paired pitched roof slabs;
- occasional roof spires.

This is not WFC yet. V0.21 first establishes a richer block vocabulary and visual grammar. WFC becomes valuable once those modules are individually convincing, because coherence generated from weak modules only produces coherent weakness.

### Landmark composition

Two broken outer arches frame the Fractured Signal approach from beyond the movement bowl, and the Memory Forge gains small offering stones that imply repeated use without adding another glowing objective prop.

## Neural / runtime boundary

All new V0.21 environmental visuals are editor-authored static presentation. They add no runtime `Update`, `LateUpdate` or `FixedUpdate`, no particles, no periodic light modulation and no neural consumer. Decorative primitives immediately lose Unity's temporary colliders.

The only gameplay-authority change is explicit and bounded: the existing boss arena collision shell is enlarged and its inner dais collider is removed. Boss movement remains owned by V0.19 and is only retuned by V0.21.

## Why NoiseShader and WFC are still deferred

`keijiro/NoiseShader` remains a useful reference if the close gameplay camera later reveals objectionable texture repetition that cannot be solved by the existing triplanar normals/material scale. V0.21 does not add runtime GPU variation simply because it exists.

Likewise, WFC should enter when we have several proven facade/roof/courtyard modules. The immediate problem is not lack of combinations. It is lack of convincing transitions and depth inside each combination.

## Required local Unity playtest

Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)** and focus on the same path as the August 31 recording:

1. Enter the boss arena and circle continuously around the Fractured Signal with target lock. The duel should use the whole bowl rather than collapsing onto the old dais.
2. Roll laterally and backward near every wall quadrant. The camera should retain room before the boundary and the boss should not sit motionless against one obstruction for multiple attack cycles.
3. Verify the center feels essentially flat. The medallion can remain visible, but it must not catch movement.
4. Verify no V0.20 crater rock has become an invisible or misleading obstacle inside the enlarged arena.
5. Hold `V` during combat. The existing Wisp ceasefire and SSVEP animation freeze must remain unchanged.
6. Inspect Causeway water edges, Market/Sanctum wall feet and the Ascent toe from gameplay camera height. The surface transitions should hide the clean procedural seams without becoming clutter.
7. Walk close to the new ferns and near-Market facades. They should survive close viewing better than the coarse distant vegetation and flat city boxes.
8. Check that the new roofs/facades improve middle-distance depth while the distant city remains background-only.
9. Check the Console for V0.21 errors and specifically for the fail-closed boss mobility warning.
10. Judge the result from play mode, not Scene view. If an asset looks good only while orbiting it in the editor, it has failed this tranche.
