# Mindforge Animation & Graphics v3

This tranche raises the cinematic showcase from pose-driven prototype motion toward a production-ready animation architecture while preserving the fixed-step combat simulation as the only gameplay authority.

## Core rule

Animation is downstream of gameplay.

```text
120 Hz combat authority
        ↓
resolved movement / attack / guard / dodge / damage state
        ↓
presentation motion + Animator parameters + VFX
        ↓
rendered pose
```

No animation clip, animation event, particle effect or procedural motion script may move the authoritative Guardian, create a hit, alter stamina, raise the shield, schedule a boss attack or apply neural state.

Root motion is explicitly disabled on production Animator bridges.

## Immediate procedural motion pass

`GuardianMotionPolish` adds secondary motion above the existing procedural Warden:

- acceleration/deceleration lean;
- pelvis bob and lateral weight transfer;
- locomotion cadence;
- torso counter-rotation;
- head tracking;
- attack anticipation, contact and recovery windows;
- combo-direction body rotation;
- heavier third-hit commitment;
- shield recoil;
- stronger perfect-guard snap;
- guard-break collapse;
- dodge lean;
- damage recoil;
- mantle inertia.

The component creates additive wrapper transforms around the existing procedural body parts. `GuardianAvatarPresentation` continues to own the primary pose, while the wrappers add secondary motion after it. This prevents animation polish from changing gameplay geometry.

## Production Guardian Animator contract

A future authored Guardian prefab may contain an `Animator`. `GuardianAnimatorBridge` discovers the visual Animator, forces `applyRootMotion = false`, and writes only parameters that actually exist in that Animator Controller.

Recommended parameter contract:

| Parameter | Type | Meaning |
|---|---|---|
| `Speed` | Float | planar Guardian speed |
| `MoveX` | Float | strafe velocity relative to aim |
| `MoveY` | Float | forward velocity relative to aim |
| `Attack` | Bool | sword attack currently active |
| `AttackProgress` | Float | normalized authoritative sword window |
| `ComboStep` | Int | 1-3 light-chain step |
| `Guard` | Bool | shield currently raised |
| `SightResonance` | Float | bounded accepted Sight modulation |
| `GuardResonance` | Float | bounded accepted Guard modulation |
| `AttackTrigger` | Trigger | new combo step accepted |
| `Dodge` | Trigger | dodge accepted |
| `Hit` | Trigger | damage consequence observed |
| `PerfectGuard` | Trigger | perfect guard consequence observed |
| `GuardBreak` | Trigger | guard-break consequence observed |

A production controller should use these to drive blend trees, layers and reaction states. The clips must not depend on animation events to create damage or defensive authority.

## Recommended Guardian animation graph

### Base locomotion layer

Use an aim-relative 2D blend tree:

- idle;
- forward walk/run;
- backward movement;
- left/right strafe;
- diagonal movement.

Feet should be authored around the real movement speeds in the current load classes. Avoid cartoon acceleration inside clips because the Rigidbody already defines acceleration.

### Upper-body combat layer

Use masked upper-body states for:

- guard enter;
- guard hold;
- guard exit;
- light sword 1;
- light sword 2 reverse sweep;
- light sword 3 finisher;
- perfect-guard reaction;
- guard-break reaction.

The visual contact frame should align with the existing authoritative sword active window rather than redefining it.

### Full-body reaction layer

Use brief full-body states for:

- dodge;
- heavy hit;
- guard break;
- defeat.

The dodge animation must visually cover the actual locomotion window, while its most committed evasive pose should align with the much shorter authoritative i-frame window.

## Fractured Signal production Animator contract

`FracturedSignalAnimatorBridge` exposes:

| Parameter | Type | Meaning |
|---|---|---|
| `Phase` | Int | authoritative boss phase |
| `Heavy` | Bool | current telegraph/fire classified heavy |
| `Telegraph` | Trigger | scheduler began a telegraph |
| `Fire` | Trigger | scheduler fired the attack |
| `Hit` | Trigger | boss damage observed |
| `PhaseChanged` | Trigger | phase transition observed |

Root motion is disabled here too. The boss scheduler remains the only attack authority.

## Procedural boss motion pass

`FracturedSignalMotionPolish` adds:

- slow inertial hover;
- phase-dependent drift;
- compression during telegraph charge;
- expansion on fire;
- hit kick;
- stronger phase-change eruption;
- phase-scaled rotational energy.

This sits above the existing core/rings/shards animation and gives the procedural boss a stronger sense of stored and released energy.

## Armament VFX pass

`CinematicArmamentVfxPolish` adds visual density without changing sword or shield geometry:

### Aetherblade

- short high-readability primary trail;
- thinner delayed afterimage;
- Sight-dependent trail opacity and width;
- additional energy motes;
- heavier third-hit trail persistence.

### Verdant Ward

- Guard-dependent edge-field thickness;
- ambient Guard motes;
- block pulse;
- stronger perfect-guard pulse;
- red/green fracture response on guard break.

The existing `GuardianSwordShieldController` still owns physical reach, shield coverage, block resolution and damage.

## Ground interaction

`GuardianLocomotionVfx` emits restrained floor dust from resolved planar velocity and stronger bursts when a dodge is accepted. It never modifies velocity.

This is intentionally subtle. Expensive-feeling movement depends more on contact, timing and body mechanics than on covering the floor in particles.

## Next authored asset pass

This source tranche creates the pipeline. The next art pass should supply real content:

1. rigged Guardian with production skeleton and deforming cloth/armor layers;
2. hand-authored idle, locomotion and directional strafe set;
3. three sword light attacks matching current collision arcs;
4. guard enter/hold/exit, block recoil, perfect guard and guard break;
5. directional dodge animations aligned to the current Rigidbody motion;
6. hit reactions keyed by impact direction/weight;
7. production Fractured Signal phase/telegraph/fire reactions;
8. authored weapon trails and shield shaders replacing runtime fallback materials;
9. footstep/armor/cloth audio tied to presentation events, not gameplay authority;
10. final VFX Graph or equivalent effects after actual Unity performance profiling.

## Unity visual validation checklist

On the exact branch head, run:

**Mindforge → Showcase → Build + Play Cinematic Showcase**

Then verify:

- walking visibly transfers body weight instead of sliding a rigid torso;
- acceleration/deceleration changes body lean;
- sword step 1 and step 2 visibly oppose each other;
- step 3 carries more anticipation and recovery;
- the rendered body recovers after a swing instead of snapping to idle;
- shield blocks move the upper body;
- perfect guard reads more strongly than a normal block;
- guard break visibly collapses posture;
- dodge body lean matches the actual movement direction/timing;
- damage recoil does not alter player position;
- sword afterimage follows the authoritative swing rather than lagging a full frame behind it;
- Guard particles never obscure the coded green VEP core;
- locomotion dust is visible but does not hide telegraphs;
- boss telegraph compression and fire expansion reinforce, rather than contradict, telegraph geometry;
- no Animator root motion moves the authoritative Guardian or boss;
- 120 Hz target and BCI display timing are rechecked on the actual competition machine.

## Claims not made by source code

This tranche does not claim final AAA animation fidelity. That requires authored motion, mocap/keyframe polish, skinning, deformation, foot IK, cloth/hair simulation, audio, real production meshes and repeated capture-based critique.

The purpose of v3 is to make those future assets plug into a stable gameplay contract instead of forcing final art to become gameplay code.
