# V0.20 World Soul

## Goal

Mindforge should feel like a place with history, ecology and scale, not a sequence of clean combat rooms surrounded by empty sky.

The clean V0.11 rebuild was the correct architectural reset: one understandable systems/traversal kernel replaced a long historical chain of visual decorators. Its remaining weakness is visual. Large portions of the canonical route still read as simple Unity forms with flat materials, and the world often stops where the traversable floor stops.

V0.20 keeps the clean architecture and changes the presentation model.

The design sentence is:

> **the playable route is one surviving path through a much larger ruined world.**

## Canonical composition

`Mindforge → Latest → PLAY LATEST (BCI Simulation)` now builds in two deterministic editor stages:

1. `MindforgeDemoV11Builder` creates systems, gameplay geometry and the canonical district route.
2. `WorldSoulV20Builder` creates the surrounding static presentation world and saves the same scene.

There is no separate “pretty build” and no restored V0.5-V0.10 decorator stack.

## World layers

### Continuous landform

Four generated terrain meshes wrap the canonical path:

- west landmass;
- east landmass;
- south foreground terrain behind the Sanctum;
- north highlands beyond the Fractured Signal arena.

The side terrain follows the route's longitudinal elevation before rising away from the playable corridor. This visually supports the Causeway, Market and Ascent without replacing their authored collision.

### Surface history

V0.20 replaces many canonical flat architecture materials with generated repeatable surfaces:

- limestone;
- basalt;
- worn civic stone;
- soil;
- moss;
- bark;
- foliage;
- water;
- ember/fracture stone.

The generated albedo textures combine broad octave variation, fine breakup, ridge structure, crack masks and restrained organic staining. They are not intended to imitate one photographed material exactly. Their job is to add scale cues and weathering so a ten-meter wall no longer looks like a ten-meter solid-color cube.

### Sanctum Grove

The opening Sanctum gains old trees, shrubs and small votive lights beyond the traversable walls. The intention is not “forest level.” It is a ritual place slowly being reclaimed by life.

### Causeway Banks

The canals gain bank rocks and static reed structure. The Causeway should feel like infrastructure crossing water and terrain, not a road slab floating beside blue rectangles.

### Market Ruins

Outside the Market's collision-backed court, broken columns, lintels, rubble and surviving warm lights imply a district that once had ordinary civic use.

### Ascent Geology

Rock masses and a damaged arch flank the existing climb. The elevation change should read as architecture fitted into a geological rise rather than a single rotated ramp.

### Fracture Crater

The first-boss space gains a rough exterior crater/rock ring beyond the existing authoritative arena wall. The south approach remains deliberately open. Residual static fracture seams support the boss palette without turning the arena into a screen-wide flicker source.

### Distant City

Far-city masses and spires now populate both sides of the route. They are render-only silhouettes with sparse static window accents. They exist to answer a crucial visual question: “what is beyond this road?”

They are not fake explorable geometry and carry no colliders.

## Public codebases and provenance

V0.20 uses public repositories as a technique library.

### SebLague/Procedural-Landmass-Generation

Repository: `https://github.com/SebLague/Procedural-Landmass-Generation`

License: **MIT**.

Mindforge adapts the project's deterministic multi-octave terrain grammar: seeded octave offsets, persistence controlling amplitude falloff and lacunarity controlling frequency growth. `WorldSoulNoiseV20` uses stable hashed octave offsets rather than allocating a random-number generator for every sample.

No SebLague runtime package, scene or art asset is vendored.

### aadebdeb/ProceduralMesh

Repository: `https://github.com/aadebdeb/ProceduralMesh`

License: **MIT**.

This project reinforces the mesh-recipe approach already used by Mindforge's `ProductionMeshLibraryV09`: generate reusable meshes from code rather than commit a growing pile of opaque model binaries. `WorldSoulMeshLibraryV20` uses that philosophy for terrain patches and reusable rock variants.

### keijiro/NoiseShader

Repository: `https://github.com/keijiro/NoiseShader`

License: **MIT**.

NoiseShader provides production-quality HLSL Perlin/simplex noise functions and is a strong candidate for a later GPU surface/material pass. V0.20 intentionally does **not** import the package. The immediate problem is world composition and large-scale surface repetition, which can be solved deterministically at authoring time without adding a runtime dependency.

## Why not simply import a realistic environment pack?

A large environment pack can improve screenshots quickly while making the project harder to reason about, legally redistribute and art-direct. It also tends to impose another project's scale, texture language and architectural assumptions.

Mindforge's current strategy is layered:

1. borrow permissively licensed algorithms and rendering techniques where they are genuinely useful;
2. generate original terrain, material variation and environmental forms from those techniques;
3. preserve a clean seam for selected third-party/local art where a bespoke model is actually worth the dependency;
4. keep gameplay and neural authority independent from presentation assets.

This makes later replacement straightforward. A hand-authored hero tree, ruin model or scanned stone material can replace a V0.20 procedural placeholder without changing encounter code.

## SSVEP constraint

A believable world does not require everything to move.

V0.20 is editor-authored static presentation. It adds no runtime `Update`, `LateUpdate` or `FixedUpdate` loop, no procedural wind, no particle weather and no periodic environmental light animation. Local lights are static.

This matters because the Wisp's 10 Hz / 12 Hz SSVEP targets need a controlled retinal environment. During a neural decision window, the maintained camera/boss systems already freeze relevant temporal motion. World Soul adds visual complexity while adding essentially no new temporal modulation.

A later vegetation-wind or atmospheric-particle system must therefore be neural-aware by construction: either freeze during evidence or be scientifically demonstrated not to compromise the task. It should not be added casually for ambience.

## Authority contract

World Soul may:

- change world renderer materials;
- generate render-only terrain/rocks/foliage/ruins;
- set static lighting, skybox, fog and ambient parameters;
- mark renderers static for batching/occlusion/reflection participation.

World Soul may not:

- add gameplay colliders or Rigidbodies;
- alter canonical route transforms or existing colliders;
- own combat, health, enemy selection or movement;
- consume neural events;
- alter SSVEP frequencies, luminance, timing or retinal geometry;
- create a competing top-level Unity build path.

## Next art tranche

V0.20 establishes the environmental grammar, but it is not the endpoint for realism. The next graphical work should be driven by actual `PLAY LATEST` captures and should prioritize the largest perceptual gaps:

- stronger stone normals/roughness and slope-aware material blending;
- more authored transitions where architecture meets soil/water;
- higher-quality hero vegetation close to the camera;
- facade depth and roofline variety in the distant city;
- local decals such as leaks, erosion, soot, moss edges and fracture scars;
- boss-arena material response tied to phase without temporal contamination during SSVEP;
- restrained volumetric-looking depth cues that remain static during neural evidence;
- audio ecology so districts differ even when the camera is facing similar stone.

The rule is to improve the world from the gameplay camera outward. A beautiful Scene view that does not improve play is not the target.
