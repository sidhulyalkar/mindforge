# Mindforge Presentation Cohesion v1

## Goal

Make the current third-person action game look cleaner, move more smoothly, and scale visual density predictably **before** the BCI Wisp becomes a dominant presentation element.

This tranche is intentionally a glue/optimization layer. It does not replace the PBR, animation, world-building, combat, or BCI branches already in flight.

Core invariant:

```text
120 Hz gameplay authority
        ↓
authoritative consequence / accepted aura
        ↓
presentation smoothing + pooled VFX + Wisp shell
        ↓
rendered frame
```

The coded VEP luminance path remains separate.

---

## Public repositories reviewed

### Unity-Technologies/BoatAttack

https://github.com/Unity-Technologies/BoatAttack

Official URP demo. Useful reference for building a visually rich scene from a relatively small set of coherent lighting/material/water/environment systems rather than stacking unrelated effects.

Takeaways for Mindforge:

- stay on URP rather than migrating render pipelines during the competition sprint;
- make lighting/material hierarchy do most of the visual work;
- treat reflections, atmospheric depth and camera composition as a coordinated system;
- profile the complete frame rather than optimizing individual shaders in isolation.

We study the project; we do not import it wholesale.

### Unity-Technologies/Graphics

https://github.com/Unity-Technologies/Graphics

The public SRP, URP, Shader Graph and rendering-package source.

Takeaways:

- use the actual URP implementation as the reference when renderer-feature or batching behavior is unclear;
- prefer URP-native features over custom full-screen passes when the built-in path already solves the problem;
- avoid speculative renderer hacks near the BCI stimulus path.

Mindforge remains on its pinned Unity 2022.3 / URP 14 line.

### Unity-Technologies/VisualEffectGraph-Samples

https://github.com/Unity-Technologies/VisualEffectGraph-Samples

The repository includes a Unity 2022.3 / VFX Graph 14 release and examples of GPU-driven effects, flipbooks, output events and reusable effect building blocks.

Takeaways:

- VFX Graph is appropriate later for a few hero effects such as Twin Eclipse, boss phase eruptions and large spatial capture fields;
- it is not necessary for every sword spark, guard response or foot contact;
- CPU ParticleSystem effects should be pooled and bounded first;
- importing the multi-gigabyte sample project would be counterproductive.

`com.unity.visualeffectgraph` is therefore **not added by this branch**. Add it only after real GPU profiling says the hero effects justify it.

### UnityTechnologies/open-project-1

https://github.com/UnityTechnologies/open-project-1

Apache-2.0 Unity Open Project. Useful as an architecture reference for keeping gameplay, runtime services, data and presentation modular.

Takeaway for Mindforge:

presentation services should be composable and disposable. Combat must not know whether a decorative particle or camera effect was actually rendered.

### keijiro/ShaderGraphExamples

https://github.com/keijiro/ShaderGraphExamples

The example Shader Graph assets are CC0-1.0.

Takeaways for later authored art:

- build a small library of reusable procedural subgraphs instead of a unique shader for every object;
- favor geometry/Fresnel/noise/field-line motifs for the neural-gothic style;
- keep time-varying shell graphics physically separate from coded VEP target luminance.

Recommended Mindforge subgraphs after authored materials arrive:

```text
MF_FresnelField
MF_FractureMask
MF_DataConduitFlow
MF_NeuralEdgeEmission
MF_DistanceFade
MF_DissolveBoundary
```

Do not use a generic periodic emission pulse on the coded Sight/Guard cores.

### Unity-Technologies/com.unity.cinemachine

https://github.com/Unity-Technologies/com.unity.cinemachine

Public source for Unity's camera system.

Takeaways:

- damping should be time-constant based, not frame-count based;
- camera collision should use non-allocating spatial queries where practical;
- target-lock framing and impact impulses should remain presentation-only.

The current `ShowcaseCameraRig` already uses `LateUpdate`, exponential rotational damping, `Vector3.SmoothDamp`, and `SphereCastNonAlloc`. There is no compelling reason to add the Cinemachine package during this sprint unless repeated P2 playtests expose a camera problem the current rig cannot solve.

### Unity-Technologies/ProjectAuditor

https://github.com/Unity-Technologies/ProjectAuditor

The public repository itself now warns that it is outdated and recommends Unity's built-in/current Project Auditor package instead.

Takeaway:

use Project Auditor as a qualification tool, not as source to vendor into Mindforge.

### KyryloKuzyk/PrimeTween

https://github.com/KyryloKuzyk/PrimeTween

High-performance allocation-free tweening reference.

Takeaway:

smooth visual motion should not require coroutine/closure allocation storms. Mindforge's current exponential smoothing and direct transform updates already cover the small number of continuous combat presentation signals, so this branch does **not** add another runtime dependency. If UI/cinematic animation grows substantially, evaluate PrimeTween through its documented package-manager route rather than vendoring source.

### Delt06/urp-toon-shader-cyberpunk-demo

https://github.com/Delt06/urp-toon-shader-cyberpunk-demo

MIT-licensed compact cyberpunk URP demo. Useful for examining how a limited set of ramp lighting, emission, fog and silhouettes can establish a strong technological identity.

Mindforge should borrow the economy of the approach, not necessarily the toon rendering style.

---

## What this branch implements

Branch:

```text
feat/presentation-cohesion-v1
```

Base:

```text
main @ ce07575eab3cc62ccee8e48c45345eb6d486abba
```

### 1. Bounded pooled impact effects

`PresentationFxPool`

The prior `CombatVfxOrchestrator` built and destroyed a new `GameObject` plus `ParticleSystem` or `LineRenderer` for common combat consequences.

The new path prewarms reusable bursts/rings and caps their maximum concurrent count.

```text
combat consequence
      ↓
CombatVfxOrchestrator
      ↓
PresentationFxPool
      ↓
available pooled effect? ─ yes → configure + play
      │
      no
      ↓
drop optional visual
```

Saturation can reduce spectacle.

It cannot reduce damage, alter hit timing, move an actor, change stamina, modify Flux or affect neural authority.

This is the correct failure mode for presentation.

### 2. Soft visual budget governor

`PresentationQualityGovernor`

Tracks smoothed unscaled render-frame duration and exposes only:

- optional effect density;
- preferred transient-ring segment count;
- whether tertiary shell detail is worth rendering.

It deliberately does **not** modify:

- `Time.timeScale`;
- fixed timestep;
- application target frame rate;
- dynamic resolution;
- Unity quality level;
- render scale;
- VEP stimulus state.

Default policy:

```text
controller-only dev run → adaptive optional detail allowed
live/release BCI         → fixed Showcase presentation
```

That keeps automated visual adaptation out of neural evidence collection unless we explicitly decide to validate such adaptation later.

### 3. Accepted-state-only Soul Wisp shell

`WispPresentationShell`

The Wisp now has a separate, non-coded visual hierarchy:

```text
Soul Wisp root
├── existing coded / gameplay-owned children
└── MindforgeWispPresentationShell
    ├── NeutralRing
    ├── SightRing
    ├── GuardRing
    └── ConcordRing
```

The shell observes only already-accepted `AuraBuffController` state:

```text
SightActive
GuardActive
ConcordActive
AuraApplied event
ConcordTriggered event
```

It does not fetch decoder evidence and does not fetch/configure the coded stimulus component.

Sight and Guard ease into stable cyan/viridian orbital structures. Concord adds an interlocked tertiary orbit. Accepted events receive a single decaying geometry accent rather than a repeating luminance pulse.

This makes neural state feel physically embedded in the world while preserving the experimental target.

### 4. Presentation budget audit

Editor command:

```text
Mindforge → Showcase → Audit Presentation Budget
```

Writes:

```text
experiments/reports/presentation-budget-latest.json
```

Schema:

```text
mindforge.presentation_budget.v1
```

The report includes:

- renderer count;
- material slots;
- unique shared materials;
- apparent material-instance count;
- ParticleSystem count;
- aggregate max-particle capacity;
- TrailRenderer count;
- LineRenderer count;
- shadow-casting light count;
- camera count;
- realtime reflection-probe count;
- Wisp shell count;
- coded VEP stimulus count;
- bounded warnings for obvious presentation-budget smells.

The audit is read-only.

---

## Why this is merge-friendly

The branch intentionally avoids changing:

- combat authority;
- `SoulWispController`;
- `VepAuraStimulus`;
- neural transport;
- boss scheduler;
- world generation;
- PBR authoring;
- Animator bridges;
- camera rig;
- package manifest.

Only one existing runtime source file is materially changed:

```text
Presentation/CombatVfxOrchestrator.cs
```

All other implementation files are additive.

That should make this tranche straightforward to merge beside the Null Ward / combat work. If another branch also edits `CombatVfxOrchestrator`, preserve its semantic event handlers and route the final spawn calls through `PresentationFxPool`.

---

## Concrete next graphics pass after branch integration

### Environment

For the Null Ward branch:

1. turn repeated architecture into modular prefabs;
2. reuse shared materials aggressively;
3. add LODGroups to large hero props only after actual distance testing;
4. use baked/static lighting where motion does not justify realtime shadow cost;
5. reserve realtime shadowed lights for the Guardian, major threats and key hero moments;
6. keep realtime reflection probes few and intentionally updated;
7. use decals/trim sheets/vertex variation to break repetition instead of unique materials everywhere;
8. preserve strong dark-to-light value grouping around combat space.

### Characters

Prioritize silhouette, motion and material response before microscopic mesh density.

A good production order is:

```text
rigged silhouette
→ locomotion/attack motion
→ armor material breakup
→ weapon/ward readability
→ secondary cloth
→ facial/detail polish
```

The player sees motion and silhouette far more often than a close-up normal map.

### Wisp

The current line shell is a deliberately lightweight bridge.

When authored art exists, replace the line renderers underneath the same state contract with:

- a compact transparent/refractive mesh shell;
- GPU-instanced motes;
- a small number of field-line ribbons;
- Sight/Guard-specific geometry shapes;
- one hero Concord structure;
- optional VFX Graph only for Twin Eclipse / Bloom release.

Do not replace `AuraBuffController` as the state seam.

### Hero VFX

Use VFX Graph selectively for:

- Twin Eclipse capture/release;
- Fractured Signal phase eruption;
- Gravity Bloom projectile suspension field;
- large boss death/victory sequence.

Keep ordinary:

- sword hit sparks;
- block sparks;
- small rings;
- foot contacts;
- simple ambient motes;

on pooled lightweight systems unless profiling demonstrates otherwise.

### Shader discipline

Use a small shared neural-gothic shader vocabulary:

```text
opaque PBR architecture
opaque PBR character
transparent/additive neural shell
unlit telegraph
particle additive
UI
coded VEP core (isolated)
```

Avoid dozens of visually similar shader variants.

### Profiling gates

Before adding another major visual feature, capture:

1. CPU main-thread frame time;
2. render-thread time;
3. GPU frame time;
4. batches / SetPass calls;
5. triangles/vertices;
6. shadow caster count;
7. transparent overdraw hotspots;
8. GC allocations during combat;
9. memory footprint;
10. physical VEP timing on the actual display after the final visual stack is present.

The optimization target is not simply "highest FPS." It is **stable headroom with visually consistent timing**.

---

## Visual hierarchy for BCI integration

Mindforge should reserve visual salience in this order:

```text
1. immediate enemy threat / telegraph
2. Guardian action confirmation
3. coded Sight / Guard targets when evidence is needed
4. accepted Wisp state
5. tactical secondary target
6. environment spectacle
7. ambient decoration
```

This prevents a gorgeous environment from becoming an attention tax on the actual BCI task.

The Wisp should feel native because the entire game's visual language already uses:

- field lines;
- fractured signal geometry;
- constrained cyan/viridian semantics;
- neural conduits;
- structured energy;
- geometric phase relationships.

The BCI layer then arrives as the culmination of the art direction rather than a HUD widget floating on top of an unrelated action game.

---

## Claim boundary

Source changes do not establish:

- successful Unity compilation on this exact head;
- improved GPU frame time;
- reduced GC allocations in a profiler capture;
- better subjective game feel;
- correct physical VEP luminance timing;
- human SSVEP performance;
- final production-art quality.

After merge, the next evidence step is:

```text
software contracts
→ Unity import/compile
→ controller-only capture
→ presentation-budget report
→ Profiler + Frame Debugger / RenderDoc-style inspection where available
→ physical stimulus timing re-check
→ human BCI validation
```
