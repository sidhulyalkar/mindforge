# Mindforge Unity Combat Showcase

This document is the shortest path from a fresh checkout to the current designed combat vertical slice.

The showcase is intentionally split into two modes:

1. **Controller-only showcase** for game feel, visuals, collision, camera, boss readability, stamina, sword and shield validation.
2. **Calibrated neural mode** for real Sight/Guard authority and BCI evidence.

The controller-only path never fabricates neural evidence or calibration success.

## Required Unity version

Open the repository's `unity/` directory as the Unity project using:

- **Unity 2022.3.76f1**
- Universal Render Pipeline package pinned by the project (`14.0.11`)

Do not upgrade the project on first import. A Unity upgrade changes too many variables at once and invalidates the intended Gate-1 environment.

## Fresh import

1. Clone or check out the branch containing the showcase work.
2. Open **Unity Hub**.
3. Choose **Add project from disk**.
4. Select the repository's `unity/` directory, not the repository root.
5. Open it with **2022.3.76f1**.
6. Allow the initial package import / script compilation to finish.
7. If Unity asks to enter Safe Mode because of compile errors, capture the first compiler error before making any manual edits. The exact branch should be fixed in source rather than repaired ad hoc in the generated scene.

## One-click gameplay preview

From the Unity menu choose:

**Mindforge → Showcase → Build + Play Combat Showcase**

That command performs the intended preview workflow:

1. rebuilds the deterministic competition scene;
2. adds the static showcase environment;
3. runs the existing scene validator;
4. saves generated assets and scene state;
5. enters Play Mode;
6. enters the existing development-only controller qualification path with BCI explicitly disabled.

The final scene remains at the normal competition scene path created by `CompetitionSceneAssembler`.

You can also use:

- **Mindforge → Showcase → Rebuild Showcase Scene** to rebuild without entering Play Mode;
- **Mindforge → Showcase → Open Showcase Scene** to open the current generated scene;
- **F8** while playing in the Editor to enter the existing controller-only qualification path manually.

## What should appear

### Awakening / environment

The authored showcase layer adds:

- a dark basalt duel arena;
- concentric emissive floor rings;
- radial floor seams;
- broken fracture monoliths around the perimeter;
- dark horizon architecture;
- restrained blue/violet rim lights;
- atmospheric fog and tri-light ambient treatment;
- a redesigned Awakening dais and calibration rings.

The scenery is deliberately non-authoritative. Existing floor/collision/gameplay objects remain the source of physical truth.

### Guardian

The placeholder capsule renderer is hidden at runtime and replaced visually by a procedural Warden silhouette with:

- armored torso and pelvis;
- helmet, visor and crown fin;
- pauldrons;
- articulated visual arms and legs;
- gauntlet accents;
- mantle / back cloth;
- emissive aether chest and visor details;
- movement, guard, sword, roll and damage-reaction presentation.

The original Guardian collider and Rigidbody remain authoritative.

### Physical loadout

The first qualified design target is intentionally one coherent build:

- **Aetherblade Longsword**
- **Verdant Ward Shield**
- **Warden Weave**

Press **Tab** to open the current read-only Guardian Build screen. It exposes the real equipment-data/load contract without pretending a full authored inventory already exists.

### The Fractured Signal

The original boss collider/vitals/scheduler remain authoritative, while its runtime body becomes:

- an emissive fracture core;
- multiple rotating energy rings;
- orbiting shards;
- phase-responsive coloration;
- telegraph charge animation;
- damage pulse;
- phase escalation lighting.

The boss now mixes ranged and close-range vocabulary through a single scheduler. Melee is not a second overlapping attack loop.

## Controls

| Input | Action |
|---|---|
| WASD | Move |
| Mouse movement | World-space precision aim |
| Arrow keys | Keyboard precision aim fallback |
| Left mouse | Aetherblade light attack / queue next combo step |
| Right mouse (hold) | Raise and hold Verdant Ward |
| Left Shift | Dodge roll |
| Space | Pulse Shot |
| F | Rift Cleave |
| C | Counter Pulse |
| R | Gravity Bloom / Twin Eclipse when eligible |
| Tab | Guardian Build screen |
| F8 | Controller-only qualification in Editor |
| F9 | Qualification photodiode patch toggle |
| F10 | Judge Lens |
| F11 | Photodiode source: Sight / Guard |

The sword/shield inputs are fixed-tick command inputs and are included in Guardian input-tape v2.

## Sword validation

The Aetherblade is not a distance-button attack. During its active contact window it sweeps a capsule volume along the rendered attack direction.

Validate all three light-chain steps:

1. **Step 1**: fast opening sweep;
2. **Step 2**: sweep reverses direction;
3. **Step 3**: wider, heavier finisher with higher commitment, poise and impact presentation.

Check that:

- clicking once does not accidentally execute a full combo;
- a second/third click must arrive during the queue window;
- stamina is spent when each actual step begins, not when a future step is merely queued;
- one enemy cannot be hit repeatedly by the same swing;
- step-two visual direction matches step-two hit direction;
- step-three hit stop feels stronger without becoming sluggish;
- a roll cannot be started through an active sword commitment;
- the sword trail follows the actual weapon sweep.

## Shield validation

The Verdant Ward is a physical forward-facing defense.

Validate:

- RMB raises a real shield trigger in front of the Guardian;
- movement slows while guarding;
- stamina recovery is strongly reduced while guarding;
- projectile contact reaches the shield before the body;
- ordinary blocks spend stamina and may leak chip damage;
- insufficient stamina breaks the guard;
- attacks from outside the shield's forward coverage can flank the player;
- raising guard shortly before contact creates the short perfect-guard window;
- perfect projectile guard reflects the projectile;
- perfect melee guard applies boss poise pressure rather than inventing free direct damage.

A raised shield is not global invulnerability.

## Dodge validation

Roll motion and roll invulnerability are intentionally different windows.

Validate:

- Shift consumes stamina;
- equip load changes roll movement;
- the invulnerability window remains bounded independently of the motion duration;
- guard drops when a roll begins;
- a projectile overlapping during a valid i-frame passes through rather than disappearing;
- attacks cannot be initiated through a committed roll.

## Boss melee validation

Entering sword distance unlocks close-range Fractured Signal attacks inside the existing scheduler.

### Fracture Cleave

The red/orange wedge is the actual locked evaluation geometry.

Try to produce each outcome intentionally:

- **SPACED**: leave range;
- **SIDESTEPPED**: leave the locked wedge;
- **DODGED**: remain geometrically inside but cross contact during roll i-frames;
- **BLOCKED**: face the attack and absorb it with adequate stamina;
- **PERFECT_GUARD**: raise the shield shortly before contact;
- **GUARD_BROKEN**: absorb more pressure than remaining stamina supports;
- **FLANKED**: hold guard while facing outside shield coverage;
- **HIT**: take the strike directly.

### Fracture Slam

The warning ring is the actual damage radius. Crossing outside the ring before resolve must result in spacing, not a hidden hit.

## Camera / readability validation

The tactical camera observes player and boss rather than controlling either.

Check that it:

- keeps both actors readable at ordinary fighting distance;
- gives the player slightly more framing weight;
- leads movement subtly rather than lagging behind the Guardian;
- backs up when player/boss separation grows;
- does not fight `CombatPresentationDirector` impact kick;
- does not make the SSVEP targets unreadable in combat.

## Semantic combat VFX

Different outcomes intentionally use different visual words:

- sword contact → blue/white cut burst;
- Sight-amplified sword contact → stronger blue energy response;
- shield block → green ward burst;
- perfect guard → violet/green high-contrast counter burst;
- guard break → red fracture burst;
- player hit → red/orange impact;
- boss phase change → large boss-centered fracture ring;
- heavy boss attack → orange danger pulse.

The effects observe outcomes only. They cannot deal damage or award gameplay resources.

## Neural manifestation validation

The showcase's key BCI fantasy remains:

**hands choose the action; neural evidence may amplify the already-chosen equipment state.**

In a calibrated/SIMULATION/REPLAY neural run:

### Sight

Accepted Sight plus stronger fresh continuous evidence should visibly:

- extend the blade;
- slightly widen it;
- brighten blue emission;
- strengthen the trail/light;
- increase bounded gameplay reach by the corresponding contract;
- attribute only realized incremental damage through `SIGHT_SWORD_DAMAGE`.

### Guard

Accepted Guard plus stronger fresh continuous evidence should visibly:

- enlarge the shield manifestation;
- increase green emission;
- widen bounded physical coverage;
- improve stability;
- improve absorption.

EEG never raises the shield, swings the sword, aims, rolls or performs a parry.

## VEP / VFX separation

The moving decorative Wisp shell and armament effects are downstream feedback. They must remain distinct from the coded 10 Hz / 12 Hz stimulus core.

- **F9** toggles the photodiode qualification patch.
- **F10** toggles Judge Lens and no longer controls the patch.
- **F11** changes the photodiode source.

Physical photodiode measurement and participant testing are still required before claiming the moving combat presentation preserves physiological SSVEP performance.

## Current visual-fidelity boundary

This branch is designed to make the complete game loop visible and critiqueable in Unity without requiring external commercial assets.

The procedural Guardian, boss and arena are **production-architecture / vertical-slice visuals**, not a claim that the project has final AAA character art. The important replacement seams already exist:

- authoritative collision is separate from character meshes;
- sword/shield rules are separate from their presentation transforms;
- camera is presentation-only;
- semantic VFX listen to outcomes;
- environment decoration is editor-only scenery;
- neural authority is separate from continuous feedback;
- the coded VEP core is separate from decorative feedback.

That means authored character models, animation clips, Shader Graph/VFX Graph assets, sound design and environment art can replace the current procedural presentation without rewriting combat or evidence authority.

## Recommended validation order

1. Run the repository's software CI on the exact branch head.
2. Open in Unity **2022.3.76f1**.
3. Use **Mindforge → Showcase → Build + Play Combat Showcase**.
4. Confirm zero C# compiler errors.
5. Confirm scene validation completes.
6. Complete several controller-only fights.
7. Record control/readability/fun observations before tuning numbers.
8. Run a calibrated/simulated neural session and validate armament resonance separately.
9. Only after the physical loop is fun, expand inventory and authored art breadth.

## What source code alone still cannot prove

Even after a green software gate, source inspection cannot prove:

- Unity C# compilation on your installed editor;
- final render correctness on your GPU/display;
- good combat feel;
- fair timing under actual player behavior;
- photodiode-qualified 10/12 Hz output;
- participant SSVEP accuracy/comfort;
- authored AAA asset fidelity.

Those are exactly the things this showcase workflow is intended to make easy to observe next.
