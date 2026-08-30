# Graphics + World Acceleration V0.7

V0.7 is the first tranche that treats world art as a **production system** rather than a pile of individually authored decorations.

The V0.6 rule remains absolute:

```text
authored gameplay topology
        ↓
constraint-solved annex topology
        ↓
stable world identities + persistence
        ↓
V0.7 local presentation passes
```

Visual generation may make solved space richer. It may not decide where the player can go, which `E` offer wins, what a quest means, what is saved, how combat resolves, or what the BCI does.

## Public repositories reviewed

### mxgmn/WaveFunctionCollapse

License: MIT for the software implementation.

Already adapted in V0.6 as the compact constraint solver. The upstream repository explicitly separates sample images/tiles from the software license, so Mindforge copied no upstream visual tiles. The MIT notice remains checked in under:

`unity/Assets/Mindforge/ThirdParty/Wfc/LICENSE.txt`

### ChichoRD/Unity-Modular-Procedural-Generation

License: MIT.

Useful idea: perform generation in **local module steps**, where each solved module receives additional behavior/detail based on its own context rather than asking one enormous global generator to own everything.

V0.7 adapts that architectural idea, not literal source. `GeneratedWorldCellV07` exposes solved socket/height context and `NeuralGothicWorldDetailerV07` performs a local deterministic art pass per cell.

This separation is valuable because local art passes can be replaced, rerun, or budgeted without changing topology.

### keijiro/ShaderGraphExamples

The Shader Graph files under the repository's example directory are released as CC0-1.0.

Useful idea: establish a small reusable vocabulary of geometric/Fresnel/noise/emission motifs rather than inventing a bespoke shader for every object.

V0.7 does **not** import those Shader Graphs yet. Mindforge first reuses its existing URP/Lit PBR maps and small emission palette. If a later profiling/art pass shows that custom graphs materially improve the world, we can adapt specific CC0 graphs with explicit provenance.

### Delt06/urp-toon-shader-cyberpunk-demo

License: MIT.

Useful presentation lesson: a cyberpunk scene can establish identity through a restrained combination of silhouette, emission, fog, additional lights and coherent material response. Mindforge borrows the economy of that composition, not its toon rendering model.

V0.7 therefore uses six bounded point lights with shadows disabled plus existing emission materials rather than scattering dynamic lights everywhere.

### VKev/Unity-URP-Shaders-Code

License: MIT.

Useful reference for URP-friendly GPU instancing, lighting, outline and vegetation patterns. No shader code is copied in V0.7 because Mindforge's existing deterministic PBR path already gives us the most important near-term gains with lower integration risk.

## What V0.7 builds

### 1. Solved-cell metadata

Every generated V0.6 cell receives `GeneratedWorldCellV07`:

- grid coordinate;
- stable tile ID;
- north/east/south/west socket type;
- height band;
- cell size.

This metadata is read-only presentation context. The WFC assembler remains the topology authority.

### 2. Neural-gothic local cell detail

`NeuralGothicWorldDetailerV07` creates deterministic local detail from the solved metadata.

Current grammar includes:

- corner buttresses and caps;
- open-side jamb towers and lintels;
- closed-wall ribs;
- floor signal inlays;
- vertical signal spires;
- overhead crossbeams;
- relic plinths;
- patina terminals;
- broken shard clusters;
- signal nodes.

A cell may contain at most **34** generated decorative primitives. All generated V0.7 primitive colliders are destroyed immediately. V0.6 floors, walls and stair connectors remain physical authority.

This is the central graphics multiplier: improving one local detail grammar upgrades every compatible generated cell.

### 3. Small PBR palette

`NeuralGothicMaterialAuthoringV07` clones and retunes the existing deterministic PBR maps instead of adding unrelated shaders:

- `CloisterStoneV07`
- `CloisterDarkStoneV07`
- `CloisterMetalV07`
- `CloisterPatinaV07`
- `CloisterAshStoneV07`

They inherit the existing generated albedo/normal/metal-smoothness/occlusion maps and vary tint, metallic response, smoothness and tiling.

The intended palette is restrained:

```text
mass      dark stone / ash stone
structure worn metal / patina
signal    cyan / verdant / violet
hostility existing red fracture vocabulary
```

### 4. Three visual scales

V0.7 adds authored visual anchors that are not gameplay geometry:

**Long range**

- distant asymmetric skyline pillars;
- Cathedral Relay;
- Cloister Archive Spire;
- Memory Loom.

**Traversal range**

- Neural Cloister threshold gate;
- modular arches/buttresses/crossbeams;
- sagging cables;
- Resonance Well;
- Null Market Reliquary.

**Close range**

- signal nodes;
- relics;
- terminals;
- floor inlays;
- alternating metal/patina structural detail.

This should make movement through the world feel like progressing through actual districts rather than crossing repeated floor plates.

### 5. Bounded lighting

V0.7 authors six point lights across the entire new visual layer:

- three Neural Cloister lights;
- one Memory Loom light;
- one Market Reliquary light;
- one Cathedral Relay light.

All have dynamic shadows disabled. Existing world lighting remains primary.

### 6. Visual density audit

`NeuralGothicWorldArtAuditV07` is runtime-safe but read-only. Current soft budgets:

- 760 renderers across the generated annex + V0.7 hero layer;
- 10 lights;
- 48 line renderers.

The audit **reports** visual pressure. It never changes fixed timestep, quality level, render scale, BCI stimulus, gameplay or world state.

## Why we are not copying entire public Unity scenes

Whole-scene imports are fast for a screenshot and expensive for a game. They tend to bring:

- unknown asset licenses nested inside otherwise permissive repositories;
- incompatible render-pipeline assumptions;
- hundreds of materials and textures with no visual hierarchy;
- scripts with overlapping input/gameplay authority;
- large binary assets that are difficult to review;
- hidden performance costs;
- an art direction that belongs to a different game.

The faster long-term strategy is narrower:

1. copy/adapt permissively licensed **algorithms or isolated techniques**;
2. preserve required license notices;
3. generate or author Mindforge-native visual content against those techniques;
4. expose stable seams so better art can replace placeholder art without rewriting systems.

## Next graphics multiplication targets

After Unity runtime validation of V0.7:

1. **Prefab materialization**: convert the strongest procedural local structures into a reusable 20–40 piece prefab kit so artists can hand-edit hero variants.
2. **World-space/triplanar surface study**: eliminate obvious stretched primitive UVs, preferably through one URP-compatible shared solution rather than many materials.
3. **Vertex/mesh damage grammar**: generate chipped corners, missing wall segments, tilted slabs and structural asymmetry without introducing traversal holes.
4. **District art profiles**: Forge, Causeway, Market, Cloister and Cathedral should share the same base kit with different weights/palettes/hero motifs.
5. **Distance-aware density**: reduce tertiary detail in cells that are purely skyline background while protecting silhouette.
6. **CC0/clean-room asset ingestion**: where binary modular assets materially outperform procedural primitives, ingest only assets with individually verified permissive/CC0 provenance and record them in a central manifest.
7. **GPU vegetation/ambient field layer**: only after measuring the current frame, consider instanced low-cost growth/cable/shard fields inspired by permissive URP examples.

The production philosophy is simple:

**make one good visual rule, then let the world use it hundreds of times.**
