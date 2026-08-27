# Mindforge Visual Fidelity v2

## Purpose

This document defines the visual-production contract for **Mindforge: The First Guardian** after the physical-combat vertical slice.

The target is deliberately ambitious: the game should eventually hold up under the same kinds of close visual scrutiny players apply to premium third-person action RPGs. That does **not** mean that renderer settings or procedural textures make the current build equivalent to a finished AAA title. Cinematic credibility emerges from the complete stack:

```text
shape language
  × material truth
  × lighting
  × animation
  × camera
  × VFX
  × atmosphere
  × sound
  × authored imperfection
  × performance stability
```

The purpose of v2 is to make that stack technically possible without contaminating Mindforge's combat or BCI authority boundaries.

---

## The critique of v1

The v1 showcase solved a different problem. It made the game **legible**.

It established:

- a coherent arena;
- recognizable Guardian and boss silhouettes;
- sword/shield presentation;
- truthful telegraphs;
- semantic impact VFX;
- a tactical camera;
- restrained URP post-processing.

But almost all visible forms were still Unity primitives and almost all surfaces were still scalar colors on URP/Lit. That leaves five major realism failures.

### 1. Light had almost nothing to describe

A uniform dark material with metallic and smoothness sliders is not stone, armor, cloth, bone, leather, dust, or weathered metal.

Realistic perception depends heavily on high-frequency variation:

- tangent-space normals;
- micro-roughness;
- cavity/occlusion;
- edge wear;
- scratches;
- pores;
- seams;
- material transitions;
- accumulated dirt;
- directional grain;
- local changes in specular response.

Without those signals, stronger lights mostly reveal the fact that the surface is simple.

### 2. Geometry was too mathematically clean

Perfect cylinders, spheres, cubes, concentric rings and identical pillars read as a level editor, not a place with history.

Premium environment art relies on hierarchy:

```text
large shape
  → secondary breakup
      → tertiary damage / weathering
          → surface microdetail
```

The v1 arena had the first layer and a little of the second. It lacked the rest.

### 3. Character anatomy was symbolic

The procedural Guardian communicates *armored human*, but it is not a human-quality character asset.

Missing production ingredients include:

- anatomical sculpt;
- intentional armor construction;
- layered cloth/leather/metal materials;
- realistic hands;
- deformation-ready topology;
- facial or helmet detail;
- skinning;
- authored skeleton;
- cloth behavior;
- motion capture or authored keyframe animation;
- additive reactions;
- foot IK;
- weapon socket discipline.

The same critique applies to The Fractured Signal. Its current abstract form is visually coherent, but the geometry is still generated rather than sculpted.

### 4. Animation did not yet carry mass

A premium sword game is judged in tens of milliseconds and a few centimeters.

The player notices:

- anticipatory shoulder rotation;
- hip loading;
- planted feet;
- center-of-mass travel;
- recovery inertia;
- shield recoil;
- hit reaction direction;
- cloth lag;
- hand placement;
- weapon arc continuity;
- transition quality.

Procedural transform animation can validate timing and readability. It cannot be the final animation solution.

### 5. The world did not yet reflect itself

Metal without convincing environmental reflection is visually weak. Dark stone without contact shadow and local occlusion floats. The v1 stack lacked a deliberate reflection volume and renderer-level contact occlusion.

v2 addresses those weaknesses at the architectural level.

---

## What cinematic fidelity v2 implements now

### Renderer profile

`CinematicFidelityConfigurator` keeps the qualified Unity/URP boundary and raises quality within it:

- Unity 2022.3 remains pinned;
- URP 14 remains pinned;
- HDR enabled;
- linear color retained;
- four shadow cascades;
- 4096 main-light shadow map target;
- 2048 additional-light shadow map target;
- soft shadows;
- longer shadow distance;
- high LOD bias;
- anisotropic filtering;
- realtime reflection-probe support;
- SSAO renderer feature;
- screen-space shadow feature;
- 120 Hz application target retained.

This is an intentional constraint. The graphics branch is **not** an excuse to migrate Unity versions underneath the physical-display qualification stack.

### BCI-safe anti-aliasing

Temporal AA is attractive for cinematic motion because it stabilizes high-frequency edges across frames. That same temporal accumulation is undesirable around a luminance-coded 10/12 Hz visual stimulus.

Therefore:

```text
CONTROLLER-ONLY CINEMATIC SHOWCASE
    → TAA High

CALIBRATED / LIVE BCI PATH
    → SMAA High
```

The cinematic branch must never require temporal reconstruction for the VEP target to look correct.

### Generated PBR fallback library

`CinematicMaterialAuthoring` creates deterministic editor assets for the source-only showcase.

Each non-emissive fallback surface gets distinct:

- albedo;
- tangent-space normal;
- metallic/smoothness mask;
- occlusion.

Current families:

- arena basalt;
- obsidian architecture;
- worn Guardian metal;
- Guardian armor;
- Guardian cloth;
- Fractured Signal shard material.

Emission remains a separate family for:

- blue Aether;
- cyan arena energy;
- violet fracture energy;
- ember fracture energy;
- green Wisp energy;
- Fractured Signal core/rings.

These generated maps exist so the repository can demonstrate a true PBR lighting pipeline without committing third-party texture licenses. They are **not final art**.

### Environment depth

`CinematicSceneDetailer` adds only collider-free set dressing:

- fractured ground plates;
- varied rubble;
- irregular peripheral breakup;
- tall ruin silhouettes;
- sparse energy seams;
- a box-projected realtime reflection probe;
- warm directional key light;
- restrained colored rim lights;
- procedural sky/environment response;
- denser but lower fog;
- reflection-probe usage across renderers.

The mechanically clean center remains intact so boss telegraphs do not become camouflage.

### Filmic post stack

The cinematic post layer now uses:

- ACES tonemapping;
- restrained bloom;
- subtle vignette;
- color adjustment;
- white balance;
- very light film grain;
- tiny chromatic aberration;
- dithering.

Signal Break lowers all major sensory-pressure terms instead of adding another overlay.

A useful rule for this project is:

> If an effect makes a screenshot more dramatic but makes an enemy telegraph harder to parse, it is probably the wrong effect.

---

## The production-art seam

The most important v2 addition is not a renderer setting. It is the ability to replace prototype art **without replacing authority**.

`CinematicArtProfile` accepts optional visual prefabs for:

- Guardian;
- The Fractured Signal;
- arena set dressing.

`CinematicArtOverrideInstaller` parents those prefabs underneath the existing authoritative objects and strips accidental gameplay components from imported art:

- `Rigidbody`;
- `Collider`;
- `CombatantVitals`;
- `GuardianCombatInput`;
- `FracturedSignalDirector`.

That means a future 150,000-polygon hero mesh and a 400-triangle placeholder can occupy the same gameplay contract.

The renderer changes. The sword hit does not.

---

## Art ingestion

Production source art belongs under:

```text
Assets/Mindforge/Art/
```

`CinematicAssetImportRules` provides conservative defaults.

### Textures

Hero/character texture maximum: **4096 px**.

General environment texture maximum: **2048 px** by default.

Normals and data maps are imported in linear space. Albedo/emissive color maps remain sRGB.

Naming conventions understood by the importer include:

```text
*_Normal
*_N
*_ORM
*_Mask
*_Rough
*_Metal
*_AO
```

Textures use mipmaps, Kaiser filtering, anisotropic filtering and high-quality compression.

### Models

Production models import with:

- source normals retained;
- MikkTSpace tangents;
- mesh compression disabled;
- optimized vertices/polygons;
- no auto-generated colliders;
- imported cameras/lights disabled;
- blend shapes retained;
- character assets prepared for animation.

Art should not contain gameplay authority.

---

## Production target: the Guardian

The final Guardian should read at three distances.

### 20+ meters: silhouette

The player should identify:

- sword side;
- shield side;
- facing;
- guard state;
- mantle motion;
- body mass;
- blue/green neural manifestation.

### 5–15 meters: construction

The viewer should see:

- layered plate thickness;
- bevels that actually catch light;
- straps and attachment logic;
- cloth underlayers;
- edge wear;
- sword geometry;
- shield construction;
- material differences.

### close framing: microdetail

The asset should sustain:

- 4K hero textures;
- controlled micro-normal detail;
- scratches that follow use;
- woven cloth response;
- metal roughness variation;
- intentional dirt/cavity distribution;
- high-quality silhouette normals.

A useful material split is:

```text
plate metal
painted / oxidized metal
woven underlayer
leather / grip
energy conduit
cloth mantle
weapon steel / aether edge
shield metal / ward field
```

Do not make every surface shiny. Material contrast is realism.

---

## Production target: The Fractured Signal

The boss should remain less literal than the Guardian.

Its visual identity should communicate **a physical thing failing to remain one physical thing**.

Potential final construction:

- massive damaged central shell;
- exposed emissive fracture heart;
- levitating armor plates or mineral fragments;
- physically modeled crack depth;
- translucent/refractive inner energy only where readability permits;
- asymmetric silhouette;
- phase-dependent structural failure;
- debris orbit that has inertia rather than mathematically perfect circles;
- contact shadow beneath hovering mass;
- attack charge that propagates through visible material seams before release.

The boss should not become a generic neon hologram. Its energy needs something heavy to tear through.

---

## Production target: arena

The arena should eventually tell a story without text.

A strong direction is a ruined neural observatory / cathedral where an old physical measurement system has become sacred architecture.

Useful visual motifs:

- brutal stone mass;
- instrument-like rings;
- fractured measurement monoliths;
- ancient calibration geometry;
- metal inserts and optical apparatus;
- cables or conduit that disappear into stone;
- weathering inconsistent with ordinary earthly decay;
- sparse living Wisp color inside otherwise dead material.

The playable floor should remain flatter and cleaner than the perimeter. Combat readability outranks environmental density.

---

## Animation roadmap

If only one expensive art discipline can be upgraded after the model pass, choose **animation**.

Recommended order:

1. locomotion set with starts/stops/strafe and aim-relative movement;
2. three-step sword combo with root/hip/shoulder continuity;
3. guard raise / hold / lower;
4. projectile shield impact recoil;
5. perfect guard recoil + counter reaction;
6. dodge roll with readable recovery;
7. directional damage reactions;
8. boss cleave and slam animation matched exactly to authority telegraphs;
9. Signal Break stagger;
10. phase-transition animation;
11. additive upper-body and cloth layers;
12. foot IK / slope adaptation if the arena later gains meaningful verticality.

Gameplay events should continue to decide **when** contact occurs. Animation decides how that decision looks and feels.

---

## VFX roadmap

The current semantic VFX system already separates outcomes. Final VFX should increase physical specificity.

### Sword

- short-lived sparks tied to surface type;
- aether edge distortion kept narrow;
- directional dust or debris on heavy contact;
- impact light measured in milliseconds, not persistent glow.

### Shield

- field deformation around actual impact point;
- visible propagation across shield surface;
- perfect guard collapses/reverses incoming energy rather than producing a generic larger explosion.

### Boss

- cracks brighten before mass separation;
- slam drives floor dust radially after the authoritative contact frame;
- phase changes shed material and alter silhouette, not just color.

### Atmosphere

Use particles to describe air, not to decorate every empty region.

---

## Performance contract

A visually premium BCI game that misses its display cadence is not premium.

The visual target remains compatible with the project's display qualification work.

The desired hierarchy is:

```text
120 Hz capable presentation on target demo hardware
    ↓
no persistent frame spikes from realtime effects
    ↓
no temporal filtering on live VEP core
    ↓
stable luminance-coded stimulus
    ↓
visual quality maximized inside that envelope
```

Candidate visual features should be degradable in this order if needed:

1. nonessential particle counts;
2. realtime reflection refresh frequency/resolution;
3. additional shadowed lights;
4. volumetric-like atmosphere approximations;
5. secondary set dressing LOD;
6. post-processing niceties.

Do **not** solve performance problems by changing the authoritative fixed simulation or silently changing VEP timing.

---

## What still separates this branch from Elden Ring-class presentation

The remaining difference is predominantly content craftsmanship and production volume, not a missing `QualitySettings` flag.

The branch still needs:

1. a sculpted, retopologized, UV'd and rigged Guardian;
2. a fully authored Fractured Signal boss asset;
3. production-quality weapon/shield assets;
4. authored high-resolution PBR texture sets or legally usable scan-derived materials;
5. a modular environment kit with bevels, damage, decals and LODs;
6. a full skeletal animation library with transitions, IK and reactions;
7. production VFX shaders/particles;
8. production sound design and music mixing;
9. lighting passes performed on the actual target monitor/GPU;
10. repeated capture-and-critique cycles from the real Unity build.

Those are not reasons to lower the target. They tell us where effort now has the highest marginal return.

---

## Unity workflow

For the current branch:

```text
git checkout feat/cinematic-fidelity-v2
```

Open the `unity/` directory with the pinned Unity editor.

Use:

```text
Mindforge
  → Showcase
      → Build + Play Cinematic Showcase
```

This will:

1. apply the high-quality URP configuration;
2. author the deterministic PBR fallback library;
3. rebuild the qualified competition scene;
4. apply the base showcase environment;
5. apply the cinematic set-dressing/reflection/light pass;
6. run the scene validator;
7. enter the explicit controller-only showcase path.

To bind production art:

```text
Mindforge
  → Showcase
      → Open Production Art Binding Profile
```

Assign visual prefabs to the generated `MindforgeArtProfile` asset.

---

## Visual review checklist

During P1 visual review, evaluate the image before evaluating the feature count.

Ask:

- Does basalt visibly break highlights at multiple scales?
- Does armor read as metal rather than blue plastic?
- Does cloth have a different response from plate?
- Are dark surfaces still readable in shadow?
- Do characters feel grounded by contact occlusion?
- Does metal reflect a coherent environment?
- Are silhouettes readable against every camera quadrant?
- Is fog creating depth rather than gray wash?
- Does bloom describe high-energy surfaces rather than soften the whole image?
- Does the boss remain readable when VFX peak?
- Can every red melee telegraph still be read immediately?
- Can every projectile still be identified at sword distance?
- Does the 120 Hz target remain plausible on the demo machine?
- In BCI mode, does the VEP target retain physically measured timing and contrast?

The next visual decision should be made from screenshots/video and frame timing produced by the real Unity build, not from source aesthetics alone.
