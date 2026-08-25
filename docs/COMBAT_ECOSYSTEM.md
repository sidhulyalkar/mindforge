# Mindforge Combat Ecosystem

## North star

Mindforge should not try to be "Elden Ring with EEG." Its combat needs a different thesis:

> **A high-mobility bullet-duel where physical skill creates openings, visual attention reallocates power, and enemy projectiles are resources rather than only hazards.**

The goal is to make the player constantly choose between positioning, offense, defense, neural attention, and projectile manipulation without giving the BCI frame-critical authority.

## Five interacting combat resources

### 1. Position

Movement has acceleration, momentum, drag, dash impulses, knockback, and collision truth. The player does not teleport between WASD coordinates. Momentum matters.

### 2. Health

Health creates the natural demand for Guard. Guard is regeneration, not an automatic dodge or parry.

### 3. Enemy poise

Heavy manual attacks, reflected projectiles, and special attacks damage poise. A poise break creates a short **Signal Break** vulnerability window.

### 4. Flux

Flux is a manual-skill resource. It is earned by:

- near-missing hostile projectiles while moving quickly;
- perfect Counter Pulse reflections;
- breaking enemy poise;
- shattering Echo enemies;
- successfully switching between Sight and Guard during combat.

Flux is capped at 3 and is spent on Gravity Bloom.

### 5. Neural aura state

The BCI chooses which Wisp aura is currently refreshed:

- Sight boosts offense;
- Guard restores health;
- overlapping both creates **Concord**.

Concord is not a third EEG class. It is an emergent consequence of successfully maintaining both independently timed BCI states.

---

# Player moveset

## Pulse Shot

**Input:** Space / primary fire

Fast ranged pressure with mild recoil.

Base behavior:

- quick projectile;
- moderate damage;
- low poise damage;
- inherits a small fraction of player velocity.

With Sight:

- higher projectile speed;
- higher damage;
- one additional pierce;
- brighter blue trail.

Purpose: continuous pressure while moving.

## Rift Cleave

**Input:** F / secondary fire

A short-range directional arc.

Properties:

- high poise damage;
- physical knockback;
- stronger hit-stop and camera impulse;
- rewards committing close to danger.

With Sight, range/arc and damage improve. This creates a reason for aggressive players to use their BCI power window at melee range rather than simply shooting from safety.

## Phase Dash

**Input:** Shift

A velocity impulse rather than a teleport.

Properties:

- brief collision immunity;
- preserves a readable trajectory;
- creates afterimages/trails;
- can be aimed using movement input or current aim direction.

Passing dangerously close to a projectile during high-speed movement creates a **Thread the Needle** near-miss and grants Flux.

The dash therefore transforms enemy bullets into a potential resource.

## Counter Pulse

**Input:** C

A short 180 ms reflection field.

It does not block sustained damage. It exists to reward exact timing.

Successful Counter Pulse:

- reverses a hostile projectile;
- aims it back toward the primary enemy;
- increases projectile speed and poise damage;
- grants Flux;
- creates asymmetric hit-stop;
- under Guard, restores a small additional amount of health.

Reflected attacks become one of the strongest ways to break boss poise.

## Gravity Bloom

**Input:** R at full Flux

Gravity Bloom is Mindforge's signature manual special.

The Guardian creates a short-lived distortion field that pulls nearby hostile projectiles inward. Captured projectiles disappear into the field. When the field collapses, they are re-emitted toward the boss as friendly neural shards.

This changes the emotional meaning of a dense bullet pattern. A screen full of danger can become ammunition if the player has prepared Flux and positions correctly.

## Twin Eclipse

Twin Eclipse is not a separate input or neural class.

If the player activates Gravity Bloom while **Sight + Guard are both active**, Gravity Bloom mutates:

- larger capture radius;
- stronger pull;
- additional emitted shards;
- higher poise damage;
- one direct shock hit;
- distinct dual-color VFX/audio.

This is the mechanical payoff for expert neural switching.

---

# Enemy ecosystem

## The Fractured Signal

The boss is designed around projectile ownership, poise, and attention pressure.

### Phase I: Pressure

The player learns the physical grammar.

Attack families:

- aimed needle fans;
- radial petals;
- clearly telegraphed lances.

Sight is easy to exploit here.

### Phase II: Attrition

The boss introduces Echo nodes that orbit it and fire independently. Chip damage makes Guard strategically useful.

The player must decide whether to kill Echoes for Flux or continue boss pressure.

### Phase III: Interference

Patterns become curved/homing and overlap with Echo pressure. The goal is not visual noise for its own sake. The fight creates moments where staring at a Wisp aura has a real opportunity cost.

The player can now:

- dash through bullets for Flux;
- parry high-value projectiles;
- cleave Echoes;
- use Gravity Bloom to invert dense patterns;
- overlap Sight + Guard for Twin Eclipse.

## Echo nodes

Small orbiting enemies with lower health and poise.

They:

- split the player's spatial attention;
- create crossfire;
- award Flux when shattered;
- are vulnerable to melee knockback and piercing Sight shots.

They make the arena an ecosystem rather than a single health bar.

---

# Physics rules

## Fixed simulation

The browser prototype runs combat at **120 Hz fixed simulation**. Unity should target fixed-step authoritative physics as well.

Rendering can interpolate independently.

## Swept projectile collision

Fast projectiles use swept segment/circle checks in the browser and continuous/sphere-cast collision in Unity. No projectile should tunnel through a target because the renderer dropped a frame.

## Momentum

Movement uses acceleration and exponential drag. Dashes set a strong directional velocity impulse. Attacks can impart recoil and enemies can receive knockback impulses.

## Poise

Damage and poise are separate quantities. Pulse Shot provides pressure; Rift Cleave and reflected projectiles are much stronger poise tools.

When poise reaches zero:

1. the boss enters Signal Break;
2. attacks pause briefly;
3. knockback dampens;
4. the player receives a clear vulnerability opportunity;
5. Flux is awarded;
6. poise resets after the break.

## Hit-stop

Strong impacts use short **asymmetric hit-stop** to make contact legible.

Critical BCI invariant:

> VEP stimulus timing uses an unscaled real-time clock and must not pause or slow with gameplay hit-stop.

Unity `VepAuraStimulus` already uses `Time.realtimeSinceStartupAsDouble`, allowing combat time scaling without silently changing target frequency.

---

# Aura attack mutations

The BCI should change how an excellent player approaches combat, not merely add a number to a HUD.

## Sight active

- outgoing damage multiplier;
- Pulse Shot velocity increase;
- Pulse Shot pierce;
- larger Rift Cleave threat area;
- more aggressive blue impact language.

## Guard active

- continuous regeneration;
- modest damage reduction as an initial tuning lever;
- successful projectile counters produce a small repair pulse.

## Concord active

Sight and Guard remain independent. If both timers overlap:

- the HUD identifies Concord;
- Gravity Bloom becomes Twin Eclipse;
- no third BCI classifier is introduced.

---

# Feel targets

Mindforge should feel unusually responsive without becoming frictionless.

## Movement

- acceleration is visible but quick;
- releasing input produces a short glide, not ice skating;
- dash is decisive and directional;
- knockback never removes player authority for long.

## Attacks

- Pulse Shot: almost no visual interruption;
- Rift Cleave: commitment plus heavy contact;
- Counter Pulse: tiny window, enormous clarity;
- Gravity Bloom: large anticipation and release.

## Camera

Use small, high-frequency impact shake for ordinary hits and larger low-frequency impulse for poise breaks / Twin Eclipse. Never shake the Wisp stimuli so aggressively that visual target tracking becomes impossible.

## Audio

Every ability needs a distinct frequency and temporal silhouette. The browser prototype uses procedural oscillators. Unity should replace these with authored layers:

- dry transient;
- tonal identity;
- low-frequency body;
- aura-specific harmonic layer;
- side-chain/ducking on major impacts.

---

# Graphics direction

Mindforge should avoid literal "brain graphics." The Forge is a machine-world that behaves like a nervous system without looking like anatomy.

## Materials

- dark near-black architecture;
- subsurface-looking luminous seams;
- physically readable silhouettes;
- blue Sight energy with sharp angular motifs;
- green Guard energy with round repair motifs;
- violet enemy corruption;
- white/silver player body.

## VFX hierarchy

Gameplay-critical information always outranks decoration.

1. enemy attack telegraph;
2. Wisp VEP target;
3. player collision state;
4. active ability geometry;
5. impact decoration;
6. ambient particles.

The Wisp must remain legible even during Twin Eclipse.

---

# Why this combat is distinct

The most interesting loop is not a combo string. It is this transformation:

```text
boss fills arena with projectiles
        ↓
player moves close enough to harvest near-miss Flux
        ↓
parries one high-value projectile back into boss poise
        ↓
visually attends Guard while repositioning
        ↓
heals through chip damage
        ↓
switches to Sight before burst window
        ↓
Sight + Guard overlap = Concord
        ↓
full Flux Gravity Bloom
        ↓
projectile field collapses into player
        ↓
Twin Eclipse re-fires the enemy pattern back at its creator
        ↓
Signal Break
        ↓
Rift Cleave burst
```

That is the fight ecosystem Mindforge should own.

---

# Competition scope discipline

For BR41N.IO, do not add ten weapons or a large campaign before this one fight is excellent.

P0 combat work:

- movement feel;
- Pulse Shot;
- Rift Cleave;
- Phase Dash;
- Counter Pulse;
- Flux;
- Gravity Bloom / Twin Eclipse;
- boss poise;
- Echo nodes;
- three boss phases;
- strong VFX/audio readability;
- complete controller fallback;
- no disruption to VEP timing.

Everything else is optional until this loop survives naive-player testing.
