# Sanctum Reference Fidelity V0.8

## Purpose

The generated cathedral images are now the visual-direction source of truth for the V0.8 opening. This pass translates their qualities into testable game-space rules rather than treating them as loose mood art.

The target is a bright neural cathedral-city with crisp construction, believable human-scale circulation, monumental but usable thresholds, strong near/mid/far depth, restrained neural color semantics and enemies that can be identified by silhouette before they attack.

This is still a vertical slice. Distant roads, bridges and skyline structures may remain presentation-only until their districts become authored traversal spaces. Nothing in this pass may invent gameplay collision, enemy authority or neural evidence.

## Reference qualities translated into game rules

### 1. Architecture must have true edge hierarchy

Large pale masses need visible construction boundaries. The reference pass therefore adds:

- thin dark shadow reveals between major architectural planes;
- warm-stone plinths and bases;
- gold inlays used as sparse hierarchy rather than broad glow;
- pointed ribs with separate stone and gold layers;
- capitals, buttresses, lancet-like window bays and inset glazing;
- floor joints that establish scale without becoming obstacles.

The result should read as assembled ceramic/stone/metal architecture, not stretched Unity primitives.

### 2. Movement space is sacred

The opening must be navigable before it is decorative.

Protected collider clearance:

| Space | Protected central width | Intent |
| --- | ---: | --- |
| Initiation Hall | 10.0 m | run, camera orbit, roll, double jump, hover |
| Threshold Terrace | 10.5 m | reveal and transition space |
| First Sentinel Court | 16.0 m | lateral dodging and target management |

Any non-trigger collider entering these protected envelopes above floor/curb height fails the editor build.

The three resonance stations are side chapels, not center-lane furniture:

- 8 Hz: left chapel at x = -8.4 m;
- 10 Hz: right chapel at x = +8.4 m;
- 12 Hz: left chapel farther toward the threshold at x = -8.4 m.

This preserves the fantasy of optional attunement alcoves while leaving the processional route visually obvious.

### 3. Doors and roads need believable scale

The Sanctum threshold remains approximately 12 m wide, deliberately monumental but physically usable.

The exterior vista introduces a 9.5 m processional road plus separate 2.4 m pedestrian margins. This is presentation-only today, but its proportions establish a world that could actually support characters, patrols, vehicles and crowds later.

### 4. Navigation should be environmental

The opening uses one quiet gold processional spine, repeated floor nodes, large threshold framing and city sightlines instead of floating arrow spam.

A new player should be able to answer these questions visually:

1. Where did I come from?
2. Where is the threshold?
3. Where are the optional resonance stations?
4. Where is the first combat court?
5. What larger landmark am I travelling toward?

If the HUD is disabled and those answers become unclear, the environmental navigation pass is not finished.

### 5. The city needs near, mid and far depth

The generated images derive much of their scale from compositional layering. V0.8 now explicitly separates:

- **near:** garden terraces, cypress-like vertical foliage, parapets and road edges;
- **mid:** broad bridge structure, flanking sanctuary blocks and repeated pointed ribs;
- **far:** cathedral towers, giant phase-ring infrastructure and branching roads.

Neural infrastructure should be geometrically legible. Prefer phase rings, ribs, traces, wave structures and field geometry over generic colored fog.

### 6. Neural colors remain semantic

Sight cyan and Guard green are valuable because they mean something. The ordinary enemy reference pass therefore uses:

- dark ceramic / graphite body mass;
- high-value white structural accents;
- amber / red-orange hostile sensors and weapon cores.

Ordinary enemies must not casually borrow the Sight cyan or Guard green palette.

## Enemy silhouette grammar

The V0.8 reference pass is presentation-only and reuses the existing deterministic `JourneyEnemyController` archetypes.

### Choir Reliquary Sentry

Existing `NullSentry` authority.

Visual read:

- suspended reliquary/keel body;
- large lateral fins with negative space;
- white crown;
- compact amber optical core;
- thin halo structure.

It should read as a hovering ranged observer at a glance.

### Chrome Penitent Lancer

Existing `ChromePenitent` authority.

Visual read:

- upright humanoid proportions relatable to the Guardian;
- separated legs, torso and shoulders;
- dark mask and amber visor;
- one very long lance that communicates melee reach before engagement.

### Shard Cantor

Existing `Shardcaster` authority.

Visual read:

- floating central core;
- three asymmetrical choir shards;
- large negative-space orbit;
- no humanoid melee silhouette.

### Needle Seraph

Existing high-lane `Shardcaster` variant.

Visual read:

- extremely narrow vertical spine;
- paired blade-wings;
- tiny amber eye;
- unmistakable sniper/aerial profile.

### Cathedral Warden

Existing `SignalWarden` authority.

Visual read:

- broad body mass;
- side buttresses;
- high crown/spires;
- exposed hostile core;
- single weapon pylon.

This should be the heaviest ordinary-enemy silhouette.

### Rift Stalker

Existing deeper `Hollow` authority only. Hollows remain absent from the opening Causeway.

If encountered later, the visual is elevated into a deliberate blade-stalker with recognizable chest, forelegs, flank blades and eye rather than an indistinct floor crawler.

## Visual clarity policy

`SanctumVisualClarityV08` is presentation-only. On desktop-class builds it requests:

- HDR camera output;
- MSAA where supported by the active render path;
- occlusion culling;
- at least 420 m far clip for the skyline;
- a near clip no larger than 0.10 m;
- forced anisotropic filtering;
- at least 85 m shadow distance;
- four shadow cascades.

These are clarity requests, not proof of target-platform performance. The real Unity build still needs frame-time and GPU validation.

## One-click build authority

`Mindforge -> Showcase -> Build + Play Cinematic Showcase` runs, in order:

1. V0.8 Sanctum onboarding recomposition;
2. V0.8 Memory Forge hero/persistence pass;
3. V0.8 reference-fidelity pass.

A player should never need to know which editor builders created the scene.

## Runtime acceptance checklist

Do not call the visual pass qualified from static CI alone. In a fresh Unity rebuild capture the first 5 to 10 minutes and verify:

- [ ] Pale architecture has crisp dark/gold plane separation instead of soft white blobs.
- [ ] Pointed ribs are visible at normal play camera distance and do not alias into noisy lines.
- [ ] The full center route stays free of pillars, plinths, calibration stations and decorative collision.
- [ ] The Guardian can run, reverse, dodge-roll, double jump, hover and air-dash down the main axis without snagging.
- [ ] The 12 m threshold reads as monumental, not absurdly narrow or toy-sized.
- [ ] Resonance stations feel like optional side chapels and never obscure where to go.
- [ ] The first two Sentries are at least 10 m apart and are easy to distinguish against the architecture.
- [ ] Enemy amber/white signals remain distinguishable from Sight cyan and Guard green.
- [ ] Chrome Penitent reads as upright humanoid/lancer before its attack begins.
- [ ] Shard Cantor, Needle Seraph and Cathedral Warden can be identified from silhouette alone.
- [ ] The exterior reads in at least three depth layers: near gardens, mid architecture, far skyline.
- [ ] The road and bridge feel wide enough for a real inhabited city rather than a miniature diorama.
- [ ] Looking through the threshold immediately provides a dominant destination landmark.
- [ ] Camera clipping through buttresses/window structures is rare or absent.
- [ ] No decorative renderer or LineRenderer owns gameplay collision.
- [ ] Performance remains stable enough that architectural detail does not damage movement feel or future stimulus timing.

## Next fidelity frontier

Once this pass is visually accepted in Unity, the next highest-value art work is not more decorative primitives. It is production mesh replacement and lighting refinement:

1. replace the closest hero ribs/piers/doors with bevelled modular meshes;
2. add authored trim sheets / decals for seams, wear and inscriptions;
3. add reflection probes and light-probe coverage where the actual render pipeline benefits;
4. add restrained atmospheric perspective to the far city without sacrificing neural color semantics;
5. build one fully traversable exterior street block at the same 9.5 m road scale;
6. give each ordinary enemy one high-quality rigged production mesh while retaining the current deterministic gameplay roots;
7. validate all of it from normal play-camera screenshots rather than editor beauty shots.
