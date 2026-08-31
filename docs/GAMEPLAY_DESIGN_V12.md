# Mindforge V0.12 Gameplay Design Contract

## Product thesis

Mindforge should feel excellent with a controller before any EEG is connected. Hands own precision. Neural input owns transformation, emphasis and strategic state changes. BCI augments a complete action game; it never rescues weak movement, combat or level design.

## Playable demo target

A new player should understand movement, dodge, blade combat, target lock and the first neural transformation within three minutes, complete the vertical slice in 15-25 minutes, and leave with one memorable conclusion: the game becomes meaningfully different when their neural state is used well.

## Moment-to-moment loop

1. Read space and enemy intent.
2. Move, jump, double-jump, hover or dodge to create position.
3. Commit with the Aetherblade, parry or a contextual interaction.
4. Build Flux through competent play.
5. Spend or amplify Flux through a neural transformation window.
6. Gain access to a safer route, stronger combat opportunity or newly legible information.
7. Recover at a checkpoint, unlock a shortcut and continue deeper.

No step should require EEG for basic survival or locomotion.

## Movement feel contract

- Ground acceleration should be responsive rather than instantaneous, with a short acceleration curve and strong braking.
- Air control should preserve intent but never allow arbitrary direction reversal.
- Jump should have coyote time and input buffering.
- Double-jump should be a distinct second impulse, not a copy of jump one.
- Hover should extend a committed aerial route, not erase gravity indefinitely.
- Dodge is the primary defensive movement action and should have a readable startup, short invulnerability window, recovery and stamina cost.
- Landing from meaningful height should have a short visual/audio impact without adding sluggish mandatory recovery during ordinary traversal.
- Camera-relative movement must remain stable whether target lock is active or not.

Target feel: traversal should be enjoyable in an empty room for several minutes.

## Combat grammar

Every enemy must expose four readable facts:

1. What space does it threaten?
2. What is the anticipation cue?
3. What is the correct counter family?
4. When is the punish window?

Core Guardian verbs:

- light blade chain: fast commitment, low recovery;
- charged/heavy blade: stronger poise damage, longer commitment;
- dodge: positional defense;
- parry: high-skill timing defense against designated readable attacks;
- aerial strike: converts vertical traversal into offense;
- target lock: camera/combat aid, never mandatory;
- neural transformation: strategic modifier, never frame-perfect input.

Normal encounters should mix at most three simultaneous threat grammars. Difficulty should come from composition and timing, not unreadable projectile volume.

## Enemy ecology

### Needle
Fast skirmisher. Threatens a narrow line and teaches lateral dodge. Low poise and short punish windows.

### Bastion
Heavy melee/area-denial enemy. Slow, wide attacks and strong poise. Teaches spacing, charged attacks and parry opportunities.

### Choir
Aerial/ranged enemy. Repositions vertically and launches clearly telegraphed projectiles. Teaches target switching, jump/hover pursuit and projectile counterplay.

### Warden
Elite combination enemy. Alternates between shielded pressure and exposed recovery. Teaches reading phases rather than damage racing.

These archetypes must eventually receive genuinely distinct authoritative mechanics. Distinct silhouettes alone are not enough for V0.12 completion.

## Encounter rhythm

Each district uses a deliberate alternation:

explore -> teach -> fight -> breathe -> vertical/traversal challenge -> stronger fight -> reward/shortcut -> transition.

Avoid long corridors filled with continuous enemies. After a demanding combat encounter, provide 10-30 seconds of low-threat movement, discovery or environmental storytelling.

## District gameplay identity

### Memory Forge Sanctum
Purpose: onboarding and safety.

Teach camera, movement, jump, dodge and blade one at a time. The Memory Forge demonstrates the first interaction and establishes the visual language for neural transformation. No lethal multi-enemy encounter here.

### Neon Causeway
Purpose: movement under pressure.

Wide readable lanes, one Needle encounter, projectile lanes and optional side platforms. First useful shortcut becomes visible before the player can unlock it.

### Market of Broken Momentum
Purpose: choice and mixed combat.

A wider arena with two routes, environmental cover and Archivist interaction. Introduce Bastion plus Needle composition. Reward exploration with a Flux/recovery pickup or lore fragment rather than mandatory stat inflation.

### Choir Tower Ascent
Purpose: vertical mastery.

Jump, double-jump and hover become required in forgiving combinations. Introduce Choir enemies and aerial combat. Falling should return the player to a nearby safe ledge rather than cause long repetition.

### Fractured Signal Arena
Purpose: synthesis.

Boss phases test movement, threat reading, parry/dodge, aerial positioning and neural transformation. Each phase should add one idea while retaining earlier ideas, not simply increase projectile count.

## BCI design

Primary neural interaction remains two-state SSVEP/VEP.

### Sight
A strategic information transformation.

Possible effects:
- reveal weak points;
- reveal safe traversal geometry or hidden route anchors;
- increase target-legibility and attack telegraph contrast;
- expose boss vulnerability windows.

### Guard
A defensive transformation.

Possible effects:
- modestly enlarge designated parry windows;
- reinforce poise/guard state;
- reduce specific incoming neural-corruption effects;
- stabilize a dangerous route or arena hazard.

BCI should be selected during 1-3 second strategic windows, not during 100 ms reaction checks. Every neural benefit must have a controller-only fallback for accessibility and qualification, clearly labelled as non-neural.

## Neural payoff rule

Every BCI event needs three layers:

1. perceptible cause: the player knows a neural choice is available;
2. immediate transformation: world/weapon/UI visibly changes;
3. gameplay consequence: the transformation changes what can be seen, safely attempted or efficiently punished.

If the only consequence is a particle effect or small damage multiplier, the BCI feature is not strong enough.

## Boss design: Fractured Signal

### Phase 1: Read

Boss teaches two core attacks with generous anticipation. Sight exposes a weak-point cycle. Guard supports one clearly marked parryable strike.

### Phase 2: Reposition

Fracture ring activates. Boss creates spatial denial that forces jump/dodge and occasional aerial movement. One previously learned attack returns faster, but remains readable.

### Phase 3: Transform

Outer crown activates. Arena exposes alternating Sight and Guard opportunities. Correct neural transformation should create a major but non-mandatory advantage, such as revealing the real core or stabilizing a dangerous attack pattern. The finale should test mastery rather than projectile saturation.

## Progression

For the demo, progression stays shallow and legible:

- checkpoints restore state and anchor persistence;
- shortcuts reduce repetition;
- pickups refill or temporarily increase Flux/recovery resources;
- one or two optional relics can alter play style;
- no sprawling inventory, crafting tree or stat spreadsheet before the core loop is proven.

## Camera contract

- Exploration camera: elevated 3/4 composition, bounded orbit, stable horizon.
- Combat lock: frames Guardian plus selected target with minimum camera distance.
- Camera collision should fade presentation-only obstructions when shortening alone would destroy readability.
- No normal gameplay frame should remain >50% occluded by a nearby opaque environment object for more than a brief transition.
- Boss camera may widen FOV slightly but should not become a separate cinematic control scheme.

## UI contract

One UI coordinator owns ordinary gameplay presentation.

Exploration: health, endurance, Flux, small neural status, one objective.
Combat: adds target/boss information only when useful.
Dialogue: suppresses combat/objective clutter and establishes interaction safety.
Neural selection: creates a dedicated clear stimulus-safe layout.
Death/rest: explicit state transition, no overlapping world prompts.

Only one contextual E prompt may be visible at once.

## Performance and feel targets

- stable 60 FPS target on the demo machine;
- input sampling and gameplay simulation remain deterministic where already established;
- no gameplay action depends on frame rate;
- ordinary controller action should feel responsive under typical local latency;
- no per-frame scene-wide FindObjects scans in production paths after initialization;
- pool recurring projectiles/VFX where allocation becomes measurable.

## V0.12 implementation order

1. Movement feel pass: acceleration/braking, coyote time, jump buffer, aerial tuning and dodge recovery.
2. Real authoritative Needle/Bastion/Choir mechanics and encounter compositions.
3. Combat feel pass: hit pause, hit reactions, poise readability, blade trail scale, parry telegraphs and enemy anticipation cues.
4. District encounter scripting and breathing-space rhythm.
5. Archivist interaction with combat-safe dialogue state.
6. Checkpoints, shortcuts and lightweight pickups across the clean route.
7. Sight/Guard strategic transformation windows embedded into traversal and combat.
8. Three-phase Fractured Signal redesign around learned mechanics.
9. Audio and VFX pass tied to authoritative gameplay events.
10. Real controller playtests, then real BCI qualification, then production asset replacement where observed framing proves stable.

## Completion gate

V0.12 is not complete because it contains more systems. It is complete when a new player can finish the slice without developer explanation, movement is enjoyable, every death is understandable, combat produces deliberate decisions, BCI visibly changes strategy, and the entire route can be played without camera collapse, UI duplication, persistence errors or runtime exceptions.
