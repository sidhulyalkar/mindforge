# Mindforge Content Foundry V0.10

## Executive purpose

Mindforge already has enough gameplay systems and enough procedural builders. V0.10 changes the production model from **adding another scene-generation layer** to **compiling presentation content through typed, reproducible contracts**.

The Content Foundry exists to increase iteration speed without weakening the project's strongest invariant:

> Presentation may become dramatically easier to replace. Gameplay, collision, persistence and BCI authority do not move with it.

The canonical `Mindforge/Showcase/Build + Play Cinematic Showcase` path remains the promotion gate. V0.10's incremental path is an iteration accelerator until observed Unity evidence proves it can replace larger portions of the historical builder chain.

---

## Architecture

```text
concept / reference / licensed source / AI candidate
                    |
                    v
         content asset recipe v1
                    |
                    v
      deterministic DCC normalization
        (Blender static lane first)
                    |
                    v
        explicit local-art binding
                    |
                    v
        Unity Content Foundry V0.10
                    |
          +---------+---------+
          |                   |
          v                   v
 generated V0.9 fallback   bound local asset
          |                   |
          +---------+---------+
                    |
                    v
     presentation-only replacement
                    |
                    v
       deterministic review captures
                    |
                    v
       canonical full Unity runtime gate
```

AI is allowed to produce **candidates**. AI output is not allowed to become gameplay truth merely because it looks good.

---

## 1. Typed asset recipes

Contract:

`contracts/content_asset_recipe.v1.schema.json`

Initial recipe families:

- `mf_cathedral_arch_v10`
- `mf_fluted_column_v10`
- `mf_cypress_tree_v10`

Each recipe records:

- semantic role;
- districts in which the asset belongs;
- source/tool/provenance metadata;
- redistribution policy;
- physical target dimensions in metres;
- coordinate/pivot policy;
- triangle and submesh budgets;
- material/texture/LOD budgets;
- Unity target tokens;
- deterministic generated fallback symbol;
- mandatory quality firewalls;
- explicit `gameplay=false`, `collision=false`, `bci=false` authority.

A future generated FBX is therefore not "some model in LocalArt". It is a candidate for one declared Mindforge asset identity.

---

## 2. Explicit local bindings replace filename heuristics

Manifest:

`content/local_asset_bindings.v1.json`

The public repository begins with no bindings. A local production machine may bind a recipe to a lawfully obtained asset under:

`Assets/Mindforge/LocalArt/`

Example local-only binding:

```json
{
  "asset_id": "mf_cathedral_arch_v10",
  "unity_asset_path": "Assets/Mindforge/LocalArt/MyPack/Architecture/Arch_A.fbx",
  "expected_sha256": null
}
```

The binding is semantic and exact. V0.10 does not need to guess that a file is an arch because its filename contains `arch`.

V0.9's heuristic scanner remains available as a transitional exploratory convenience. It is no longer the desired production identity system.

---

## 3. Deterministic planner

Tool:

`tools/content_foundry.py`

Validate contracts:

```bash
python tools/content_foundry.py validate
```

Print the content fingerprint:

```bash
python tools/content_foundry.py fingerprint
```

Generate the staged plan:

```bash
python tools/content_foundry.py plan \
  --output experiments/reports/content-foundry-plan.json
```

The plan separates:

1. recipe validation;
2. DCC normalization;
3. Unity ingestion;
4. visual capture.

It deliberately labels Unity and capture stages as requiring Unity observation. A Python plan cannot promote the game.

The plan is now uploaded with ordinary exact-head CI evidence.

---

## 4. Blender static normalization lane

Script:

`tools/blender/normalize_static_asset_v10.py`

This is the first deterministic DCC lane. It is intentionally limited to static presentation assets.

Example:

```bash
blender --background \
  --python tools/blender/normalize_static_asset_v10.py -- \
  --input /path/to/source.glb \
  --recipe content/recipes/architecture/cathedral_arch_v10.json \
  --output /path/to/normalized.fbx \
  --report /path/to/normalized.report.json
```

The static lane:

- starts from an empty Blender scene;
- imports FBX, glTF/GLB or OBJ;
- deletes non-mesh scene baggage;
- joins static mesh pieces;
- applies transform scale/rotation;
- triangulates deterministically;
- fits the asset uniformly into recipe bounds;
- places the bottom-center pivot at the origin;
- rejects triangle/material budget violations;
- rejects non-finite geometry;
- rejects degenerate bounds;
- exports a canonical Unity-facing FBX;
- writes a normalization report.

It intentionally refuses humanoid/robot rig recipes. Character rigging/animation will receive a separate compiler because destroying a skeleton to make a static mesh pipeline convenient would be the wrong abstraction.

---

## 5. Unity incremental compiler

Editor command:

`Mindforge -> Content Foundry -> Compile Production Art Incremental`

Implementation:

`unity/Assets/Mindforge/Editor/ContentFoundryV10.cs`

The compiler:

1. validates every recipe again inside Unity;
2. validates exact local bindings;
3. fingerprints recipe + binding inputs;
4. stores its cache only under Unity `Library/`;
5. skips an unchanged production-art iteration when a compatible production root is already present;
6. otherwise invokes the qualified V0.9 production presentation seam;
7. applies explicit recipe-bound local replacements;
8. disables external colliders;
9. removes external Rigidbodies;
10. disables external MonoBehaviours;
11. disables external lights, cameras and AudioListeners;
12. validates the resulting presentation authority boundary;
13. runs the presentation budget audit;
14. writes a diagnostic report.

The cache is intentionally local and disposable. Deleting it changes performance, not game truth.

### Critical promotion rule

The Foundry incremental compile is **not** a replacement for:

`Mindforge -> Showcase -> Build + Play Cinematic Showcase`

until a later tranche demonstrates exact semantic and visual equivalence across clean rebuilds.

---

## 6. Deterministic visual regression lane

Editor command:

`Mindforge -> Content Foundry -> Capture Production Reference Views`

Implementation:

`ContentFoundryVisualCaptureV10.cs`

Current named views:

- Sanctum nave;
- threshold facade;
- Market arcade;
- Fracture landmark;
- Cathedral approach;
- skyline.

The capture camera derives its target center from named production geometry and renders a fixed 1280x720 / 58-degree-FOV edit-mode image. Each PNG receives a SHA-256 entry in the generated manifest.

These captures answer questions such as:

- Did the Cathedral disappear?
- Did a local replacement blow up in scale?
- Did a material become magenta?
- Did a district lose its silhouette?
- Did one art change unexpectedly alter several districts?

They do **not** answer:

- Is movement fun?
- Does the camera feel good in motion?
- Are combat telegraphs readable at speed?
- Is BCI luminance physically correct?

Those remain runtime/physical evidence questions.

---

## 7. Tool strategy from AI Game DevTools

The external AI-game-devtools catalog is a technology radar, not a dependency manifest.

### Preferred candidate lanes

**Concept/reference**

Use controlled image-generation systems for architecture, enemy, prop and material sheets. Reference images should be retained with hashes when licensing permits.

**Static 3D**

Evaluate high-quality text/image-to-3D systems for hero props, architectural replacements, vegetation and enemy shell pieces. Generated output must still pass the recipe + normalization path.

**Textures/materials**

Use generative texture systems to create source candidates, but collapse them into Mindforge's small material-family vocabulary rather than importing a shader/material zoo.

**Animation**

Treat motion-generation systems as candidate producers. Retargeted clips must remain downstream of existing movement/combat timing authority.

**Audio**

Use text/video-to-audio systems for Foley candidate multiplication. Authoritative hit/dash/parry events remain the trigger source.

### Explicitly not a near-term runtime dependency

Do not put world models, autonomous general game agents, large language models, foundation models or generative asset inference inside the frame-by-frame shipped gameplay loop for the competition build.

---

## 8. AI asset tournament protocol

For a high-value role, generate several candidates instead of accepting the first plausible output.

```text
recipe
  -> 8-16 source candidates
  -> normalization
  -> hard technical rejection
  -> turntable/reference capture
  -> style + silhouette review
  -> 2-3 finalists
  -> human art-direction choice
  -> exact local binding
```

Hard rejection occurs before taste:

- broken/non-finite mesh;
- zero bounds;
- triangle overflow;
- material overflow;
- unacceptable license/provenance;
- wrong scale/pivot;
- hidden gameplay components;
- import failure.

The human should spend time choosing among technically valid candidates, not repairing arbitrary generator output.

---

## 9. Migration away from the historical builder conveyor belt

V0.10 does **not** immediately delete historical builders. That would combine a production migration with a large behavioral rewrite.

Migration proceeds by authority-preserving stages:

### M0 - current

Canonical full showcase executes historical builders and V0.9 synchronously.

Foundry accelerates only the final production presentation iteration.

### M1 - production art catalog

Convert arches, columns, trees, spires, props and landmarks from code-selected mesh vocabulary to recipe-backed catalogs.

### M2 - district profiles

Move visual weighting into data:

- Sanctum profile;
- Promenade profile;
- Market profile;
- Fracture profile;
- Cathedral profile.

### M3 - compiled production stage

Produce one deterministic production-art output from catalog + district profiles.

Require clean-rebuild equivalence before retiring V0.7/V0.8/V0.9 presentation builders.

### M4 - character/audio foundries

Add separate recipe families for character meshes, animation and audio. Do not overload the static mesh compiler.

### M5 - canonical stage DAG

Only after equivalence evidence exists, reduce the canonical showcase assembly to a small stage graph such as:

```text
GAME FOUNDATION
   -> WORLD AUTHORITY
   -> GAMEPLAY CONTENT
   -> PRODUCTION PRESENTATION
   -> FINISHING
   -> QUALIFICATION
```

Historical builders then move to legacy/reference status instead of executing every iteration.

---

## 10. Branch and promotion policy

V0.10 is intentionally stacked on the V0.9 production-art branch while V0.9 awaits observed runtime qualification.

This allows architecture development to continue without changing V0.9's claim boundary.

Promotion order:

1. V0.9 exact-head software CI green;
2. V0.9 observed Unity runtime checklist passes;
3. merge V0.9;
4. retarget V0.10 onto `main`;
5. V0.10 exact-head software CI green;
6. run full canonical showcase rebuild;
7. run V0.10 incremental compile repeatedly and verify idempotence;
8. capture all six reference views;
9. bind at least one real arch, column or tree and verify fallback/replacement behavior;
10. run canonical runtime gate again;
11. only then merge V0.10.

The Foundry is successful when content changes become dramatically faster **and** the game becomes less mysterious, not more.
