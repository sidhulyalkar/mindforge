# Mindforge Combat Design

Mindforge combat should feel readable, mobile and deliberate. The Dragon Souls chassis supplies the working animation/state/damage skeleton; Mindforge should improve game feel and encounter design without duplicating that authority.

## Authority chain

The canonical melee chain remains:

`InputReader -> player combat state -> authored attack animation -> animation event -> CombatController -> Sword.StartAttack -> CapsuleCollider + Damage + TrailRenderer -> Health`

Mindforge may observe or present this chain. It must not create a second melee damage path.

## Player combat vocabulary

The showcase should prove:

- light attack;
- chained light combo;
- heavy attack;
- target lock;
- target switching;
- dodge roll;
- sprint repositioning;
- sword throw / recall;
- healing;
- hit reaction / readable impact;
- boss melee punish windows.

Future additions should be evaluated against this vocabulary before adding more buttons.

## Combat readability

Every enemy attack should communicate:

`WINDUP -> COMMITMENT -> IMPACT -> RECOVERY`

Windup must be long enough to read from the production camera. Commitment should prevent enemies from perfectly tracking the player through every dodge. Recovery creates the punish window.

Different roles should have recognizably different timing signatures.

## Simultaneous pressure

A crowd is not difficult merely because several colliders overlap the player.

V0.32 encounter recipes specify `maximumSimultaneousAttackers` and intended breathing room. The future encounter coordinator should allow inactive threats to reposition, posture, cast, or prepare rather than all entering melee commitment simultaneously.

Initial targets:

- first encounter: maximum 1 committed attacker;
- elite encounter: maximum 2 committed attackers;
- boss adds, if any: never obscure the boss's primary telegraph language.

## Enemy archetypes

### Remnant

Purpose: teach baseline sword spacing.

- medium movement speed;
- obvious single / double melee sequence;
- moderate recovery;
- low tracking after commitment.

### Warden

Purpose: timing / guard-break pressure.

- defensive posture;
- shielded frontal state;
- slower punishable counter;
- encourages flank, heavy attack or neural mechanic.

### Ranger

Purpose: displacement pressure.

- stays at range;
- clearly telegraphed projectile;
- repositions after firing;
- low close-range durability.

### Stalker

Purpose: lateral awareness.

- quick flank movement;
- short burst attack;
- disengages rather than body-blocking player indefinitely.

### Resonant

Purpose: neural/area control.

- slower, highly visible setup;
- creates a positional hazard or signal field;
- natural connection to Sight/Guard/Concord mechanics.

### Brute

Purpose: space control.

- large silhouette;
- slow commitment;
- broad attack arcs;
- very readable recovery;
- should never sit directly inside the player camera.

## Dodge roll

Roll should remain the primary active avoidance mechanic for the showcase.

Desired qualities:

- clear startup and direction;
- useful but bounded invulnerability window;
- enough recovery that random spam is suboptimal;
- enemy commitment that allows a successful roll to create a punish opportunity;
- camera framing that preserves player silhouette during the roll.

Do not make every threat require rolling. Walking, sprinting and positioning should remain valid for some attacks.

## Aetherblade presentation

The Aetherblade is a visual child of the authoritative Dragon Souls Sword transform.

Presentation targets:

- stable alignment with hand animation;
- bright core + restrained bloom;
- visible swing trail only when authoritative trail/swing window is active;
- compact hit sparks;
- no giant full-screen particles;
- no duplicate damage collider;
- future blade length can respond to BCI engagement only through an explicit semantic adapter.

## Hit feel

Preferred tools, in order:

1. animation and timing;
2. enemy reaction;
3. sound;
4. sword trail / impact sparks;
5. bounded camera impulse;
6. extremely small visual hit pause if needed.

Avoid global `Time.timeScale` manipulation as a default hit-stop system because it can disturb animation, physics, BCI timing and future network/decoder cadence.

## Boss combat

The Fractured Signal should be defined by recognizable rules rather than simply having more health.

### Phase 1: Learn

- sweep;
- lunge;
- ground strike.

Goal: teach silhouette, spacing and recovery windows.

### Phase 2: Adapt

Add one arena-control / signal mechanic.

Sight may expose a vulnerable region or safe window, but must not become mandatory until simulated semantic control is robust.

### Phase 3: Master

Add a meaningful rule change, such as fracture nodes that must be destroyed or synchronized to create a damage window.

Do not implement Phase 3 as only faster animations and more projectiles.

## Boss geometry

- boss visible body and damage envelope should approximately agree;
- camera must not enter boss body;
- player should not disappear inside the boss;
- minimum separation should coexist with reachable melee range;
- attack origins should visually agree with mouth/limb/core effects;
- arena must retain at least the 32 m diameter world-grammar minimum unless a specific encounter design justifies more.

## Qualification

A combat tranche is not qualified merely because animation plays.

Observe:

- real swing window opened;
- trail presentation appeared;
- real `Damage.OnHitGiven` occurred;
- hitbox closed after swing;
- enemy health changed once per intended contact;
- lock-on still works;
- roll still works;
- death/respawn still works;
- sword throw/recall still works;
- no enemy pile-up causes unreadable player silhouette.
