# Null Ward Visual Infrastructure V2

## Goal

Raise the shipping Null Ward from a structurally correct graybox toward a production-art-ready Neural-Gothic district without changing combat, world progression, or BCI authority.

The graphics lane follows one rule:

```text
world / combat / neural authority
            ↓
     semantic state
            ↓
 presentation infrastructure
            ↓
 meshes · materials · lights · VFX · authored art
```

Presentation may disappear completely and the authoritative run must still resolve identically.

## What V2 adds

### Static optimized environment detail

`NullWardVisualInfrastructureBuilder` runs after `NullWardSceneBuilder` in the one-click cinematic Showcase.

It adds a removable hierarchy:

`Mindforge_NullWard_StaticDetail_V2`

with independent Memory Forge, Causeway, Market, Maintenance, and Cathedral detail roots. All generated detail is collider-free. Structural metal/stone detail may cast shadows; thin emissive trim does not.

Only non-gameplay `MeshRenderer` objects are marked for static batching, occlusion, and reflection-probe participation. Enemies, gates, Fractured Echoes, LineRenderers, TrailRenderers, and particle renderers are excluded.

The pass reuses the existing cinematic material vocabulary instead of generating a second Null Ward material library.

### Authored art seams

The visual pass creates five presentation anchors:

- `NullWard_ArtAnchor_MemoryForge`
- `NullWard_ArtAnchor_Causeway`
- `NullWard_ArtAnchor_Market`
- `NullWard_ArtAnchor_Maintenance`
- `NullWard_ArtAnchor_Cathedral`

`NullWardArtProfile` exposes one optional prefab per district. Use:

`Mindforge → Showcase → Open Null Ward Art Binding Profile`

to create/select `Resources/Cinematic/NullWardArtProfile`.

The runtime binder treats imported room prefabs as an untrusted rendering payload. It strips:

- Rigidbody / Collider / Joint;
- Rigidbody2D / Collider2D / Joint2D;
- Camera;
- AudioListener;
- every custom `MonoBehaviour`, regardless of namespace.

Animator, Renderer, Light, Cloth, ParticleSystem, AudioSource and pure visual components remain available. If a room needs scripted presentation, that behavior should be deliberately bound from Mindforge rather than inherited from an asset-pack prefab.

When authored art is supplied, only the corresponding collider-free V2 detail root may be hidden. Base collision/authority geometry remains present.

### District-aware presentation budget

`Mindforge → Showcase → Audit Presentation Budget`

writes the additive-compatible `mindforge.presentation_budget.v1` report and now includes per-zone budgets for:

- renderer count;
- material slots;
- estimated triangle count;
- transparent material slots;
- batching-static renderers;
- particle systems/capacity;
- line renderers;
- lights and shadowed lights.

The global audit also reports triangle estimate, transparent-slot pressure, and batching-static coverage.

The one-click cinematic build calls this audit automatically after gate validation and before entering Play Mode.

### Controller-only runtime performance evidence

`PresentationPerformanceProbe` exists only under `UNITY_EDITOR || DEVELOPMENT_BUILD` and arms only after `ControllerOnlyQualificationBootstrap.Active`.

After a warmup it records a bounded window using Unity `ProfilerRecorder` counters for:

- Main Thread;
- Draw Calls Count;
- Batches Count;
- SetPass Calls Count;
- Triangles Count;
- GC Allocated In Frame.

Per-frame measurement writes to preallocated storage. Sorting, JSON creation and file I/O happen only after the sample window closes.

The Editor report is:

`experiments/reports/presentation-runtime-latest.json`

with schema:

`mindforge.presentation_runtime.v1`

It records mean/p95/max main-thread time, draw/batch/SetPass/triangle summaries, GC allocated per frame, Unity version, graphics device and render resolution. The probe does not change quality, target frame rate, fixed timestep, gameplay or neural state.

## BCI rendering boundary

The coded 10/12 Hz cores remain special-purpose scientific renderers.

V2 does not edit `VepAuraStimulus`, does not read decoder scores, and does not generate visual behavior from neural evidence.

`CinematicRuntimeMaterialOverride` now explicitly excludes renderers under `VepAuraStimulus`. Renderer-wide shadow/probe settings are also applied only to objects that actually receive a selected cinematic replacement material.

This fixes an earlier over-broad graphics pass where unrelated renderers could inherit cinematic shadow/probe settings.

## Material and batching policy

Prefer, in order:

1. shared cinematic URP materials;
2. SRP Batcher-compatible shaders/material layout;
3. static batching for fixed architectural geometry when profiling shows it is useful;
4. authored geometry variation;
5. small numbers of purposeful transparent neural-field surfaces.

Unity 2022.3 documents ordinary GameObject GPU instancing and the SRP Batcher as separate paths, with the SRP Batcher taking priority for compatible renderers. V2 therefore does **not** toggle `Material.enableInstancing` across the cinematic material library simply because repeated meshes exist. Any future move to explicit instanced/indirect drawing should be evidence-driven and isolated to a rendering subsystem that actually benefits from it.

Do not introduce per-renderer material instances merely to make repeated architecture slightly different. Do not put adaptive visual behavior into the coded VEP material path.

## Recommended authored-art workflow

Keep the generated Null Ward as the authority/reference skeleton.

For one district at a time:

1. export or author set dressing against the zone anchor;
2. keep collision and custom scripts out of the art prefab;
3. bind the prefab in `NullWardArtProfile`;
4. rebuild and run the cinematic Showcase;
5. inspect the automatically emitted presentation-budget report;
6. allow the controller-only runtime probe to finish its sample window;
7. inspect Unity Profiler + Frame Debugger;
8. capture representative gameplay frames;
9. revise lighting/material hierarchy before adding more particle density.

This allows multiple art agents to work independently without creating scene-merge conflicts.

## Visual priority order

The image should remain readable in this order:

1. hostile telegraph / immediate threat;
2. Guardian action feedback;
3. coded BCI target while a selection is expected;
4. accepted Wisp state;
5. tactical secondary target;
6. architectural focal point;
7. ambient detail.

A prettier frame that reverses that order is a regression.

## Qualification boundary

Source contracts and CI can establish architecture, authority isolation, and regression protection. They cannot establish visual quality or performance.

Before promotion, run the exact branch in Unity `2022.3.62f3` and capture:

- compiler/import result;
- cinematic Showcase screenshot/video;
- presentation budget JSON;
- runtime presentation JSON;
- CPU/GPU frame timing;
- GC allocation during ordinary combat and VFX bursts;
- Frame Debugger draw/batch inspection;
- physical 10/12 Hz timing again after final graphics integration.

Do not claim the new static/detail pass improves FPS until those measurements exist.
