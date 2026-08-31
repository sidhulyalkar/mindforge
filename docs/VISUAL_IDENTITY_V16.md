# Mindforge V0.16 — Recording-Driven Visual Identity + Combat Readability

V0.16 is based on the August 30 gameplay capture rather than another abstract art target.
The capture made a useful distinction visible: Mindforge already has real traversal/combat/BCI
architecture, but its presentation still often reads as **mechanics inside a blockout**.

This tranche attacks that gap without changing gameplay or neural authority.

## What the recording exposed

### 1. Architecture becomes a black screen-space wall

Large near-black towers/walls repeatedly occupy major portions of the camera. The problem is
not only collision. Many of these are decorative or oversized render forms sitting around a
smaller collision-qualified route.

V0.16 therefore separates:

- collision authority: unchanged;
- presentation visibility: allowed to ghost when it blocks the Guardian;
- EEG epochs: ghost visibility is frozen for the entire neural evidence interval.

`CameraOcclusionGhostV16` only changes `Renderer.enabled`. It never disables a collider,
moves geometry, changes a GameObject active state, or changes target-lock authority.

### 2. The palette has insufficient value hierarchy

The capture contains large areas of almost pure black next to light beige arches and bright
signal colors. A wall, tower, floor, trim piece and far building often share the same dark
value, so architectural depth collapses.

`LegacyMaterialHierarchyV16` applies a restrained constant hierarchy through
`MaterialPropertyBlock`:

- traversal/floor family: blue graphite;
- structural arch/column family: desaturated warm stone;
- metal/trim family: medium steel;
- deep massing family: dark slate, not absolute black.

The pass explicitly preserves:

- `SightVepCore`;
- `GuardVepCore`;
- photodiode presentation;
- emissive/signal/rune/core renderers;
- Wisp/energy/blade/visor elements.

It never references or mutates a VEP stimulus component.

### 3. The horizon exposes the level boundary

The capture frequently shows a flat grey horizon outside the playable shell. That makes the
world feel like a platform even when the route itself is bounded and safe.

`WorldDepthBackdropV16` surveys the existing visual world and creates three static depth
planes outside it:

1. low terrain/horizon shelves;
2. nearer cathedral-city silhouettes;
3. deeper skyline masses plus side spires for parallax.

Every generated piece is collider-free, non-emissive, shadow-cheap and static.
The visual build defers if calibration/resonance currently owns the retinal field.

### 4. Guardian and enemy silhouettes collapse at gameplay distance

The bright cyan blade remains readable, but the Guardian body itself often becomes a small
black shape. The Fractured Signal is visually energetic but can read as a pile of pink
primitives rather than an intentional hostile silhouette.

`CombatSilhouetteV16` adds a deterministic fallback layer:

- Guardian chest/shoulder/helmet hierarchy with medium-value metal and ivory;
- a narrow helmet crest to preserve facing silhouette;
- an asymmetric hostile shard crown around the current/highest-value enemy;
- no periodic animation or flicker;
- no collider or combat component.

This is still fallback art. The production-art seam remains the final destination.

### 5. The HUD is too quiet and fragmented

The capture's HUD technically communicates state, but it does not establish a strong reading
order. V0.16 rebuilds `ProductionHudV09` around:

1. Guardian health/endurance/flux;
2. locked/current target health at top center;
3. neural-link state;
4. current objective;
5. contextual `V HOLD · CHANNEL WISP` / active neural-window instruction;
6. transient conventional-control hint.

The HUD remains read-only. It cannot lock targets, create neural events, or invoke gameplay.
The F7 research/evidence HUD remains a separate layer.

## Neural visual-field invariant

V0.16 extends the V0.15 rule:

```text
presentation may change before an EEG epoch
presentation may change after an EEG epoch
presentation must not recompose inside an EEG epoch
```

The relevant visual states are treated as neural-owned when any of the following is true:

- calibration is in progress;
- calibration coded stimuli are active;
- player resonance window is armed/active.

One-time material/backdrop/silhouette construction waits for the field to become idle.
Camera occluder state freezes while the field is owned by neural evidence.

The coded blue/green cores remain outside V0.16 presentation authority.

## Expected visual effect

The target is not "more stuff." It is a stronger depth and attention hierarchy:

```text
foreground: Guardian / Wisp / immediate combat threat
midground: traversal architecture + encounter geometry
background: district silhouettes + skyline
signal layer: cyan / green / fracture color
HUD layer: only the state needed right now
```

The current capture has foreground and signal layers but lets the midground swallow them and
lets the background disappear. V0.16 specifically repairs those two missing layers.

## Runtime qualification checklist

After Unity compiles the branch:

1. Replay the same route as the August 30 capture.
2. Confirm the Guardian is never hidden behind an opaque decorative tower/wall for more than
   the small ghosting grace period.
3. Confirm ghosting never changes collision: run directly into a visually ghosted wall.
4. Verify no floor/bridge disappears due the camera readability filter.
5. Confirm the world no longer resolves into near-black slabs at ordinary exposure.
6. Turn the camera toward all four horizons and verify world silhouettes remain outside the
   playable shell.
7. Confirm the Guardian torso/head remain readable without relying only on the cyan blade.
8. Confirm the Fractured Signal/current locked target has an intentional top-center health read.
9. Verify target changes update the target health panel without creating or changing lock.
10. Run calibration and hold `V`; no camera occluder should appear/disappear during the
    evidence interval.
11. Verify the VEP coded cores retain their original material/timing/geometry.
12. Record a new 60-second playthrough at the same resolution and compare it side by side
    against the source capture.

## What V0.16 deliberately does not claim

This pass is still source-only until opened in the pinned Unity editor. The repository does not
yet have Unity Editor compile/render CI, so static/software-gate success cannot prove visual
composition or C# compilation in Unity.

The next visual frontier after this pass is production assets and animation, not another layer
of primitives: authored environment modules, a real skinned Guardian, authored Fractured
Signal asset, combat animation, sound, and final VFX. V0.16 is intended to make the current
procedural game coherent enough that those assets can replace clean seams instead of fighting
against a noisy blockout.
