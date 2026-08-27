# P2 Experience Readability Review

This review treats Mindforge as a game first and a BCI system second.

The platform can already explain where authority came from. P2 now has to answer a more ordinary and more dangerous question:

> Can a new player understand the fight quickly enough to enjoy making decisions inside it?

## Findings addressed in this tranche

### 1. Gameplay state was less visible than decoder state

The competition scene had a strong neural evidence HUD, but no equivalent gameplay-first view for Guardian health, boss health/poise, Flux, Sight/Guard duration, Concord, or the current Bloom payoff.

That information hierarchy was backwards for the player.

`CombatStateHud` now observes those authoritative states without mutating them. It also punctuates boss phase changes, Signal Break, neural aura application, Concord, Gravity Bloom and Twin Eclipse.

### 2. Radial telegraphs were semantically weak

The boss could fire 12–20 radial projectile lanes while showing only a small ring around itself. A ring says “something radial is happening,” but it does not teach the player where the actual lanes are.

`FracturedSignalTelegraph.ShowRadial` now previews the same angular lattice used by `SpawnRadial`, dynamically expanding its presentation-only line pool when later phases need more lanes.

A telegraph is treated as a promise, not decoration.

### 3. Onboarding exposed too many controls at once

The previous opening prompt listed movement, aim, Pulse, Cleave, Counter, Dash and Bloom simultaneously.

The guide now teaches in layers:

1. move + aim + Pulse;
2. Cleave + Counter + Dash;
3. how Flux is earned;
4. Gravity Bloom only when Flux is full;
5. Twin Eclipse only when Concord + full Flux make it real.

The tutorial observes accepted actions. It never executes them.

### 4. Damage telemetry lacked pattern context

Phase-level labels were too coarse for tuning readability. A player could take repeated damage in phase three without the report telling us whether the recent primary pattern was a fan or radial burst.

The boss now emits semantic `BOSS_ATTACK_TELEGRAPH` and `BOSS_ATTACK_FIRED` markers with pattern, weight and projectile count. The encounter report records fan/radial/heavy attack exposure and counts which primary pattern had fired recently before a damage event.

This is explicitly **not causal attribution**. Projectile travel time and Echo fire mean a recent primary pattern may not be the projectile that actually hit the player.

## What remains unobserved

Do not tune these from source alone:

- whether phase-one telegraph time feels generous or sluggish;
- whether the 180 ms Counter window feels learnable;
- whether phase-three 0.48 s cadence is exhilarating or visually saturated;
- whether 12/16/20 radial lanes remain readable at the actual camera angle and display size;
- whether Echo priority is strategically interesting;
- whether the gameplay HUD is informative without becoming clutter;
- whether the staged tutorial appears at the right moments;
- whether Signal Break feels like punctuation or dead time;
- whether Gravity Bloom and Twin Eclipse are memorable enough;
- whether mouse/arrow precision aim feels natural in the real Unity player;
- gamepad/right-stick behavior on the actual competition controller.

Those require P1 + P2 observation.

## P2 tuning questions

For each full controller-only session, inspect machine evidence and human review together.

### Readability

- Which boss pattern preceded the majority of damage events?
- Did the player understand radial gaps before being hit by them?
- Did heavy fan telegraphs read differently from light fans?
- Did Echoes compete with the boss for attention in an interesting way or merely add noise?

### Agency

- Did the player intentionally target at least one Echo?
- Did they understand why a Counter succeeded or failed?
- Did Dash feel directional and deliberate?
- Did the player aim freely, or mostly fall back into boss-lock behavior?

### Resource loop

- Did the player understand what increased Flux?
- How long after full Flux did they activate Bloom?
- Did Signal Break meaningfully accelerate the next payoff?
- Did the player ever save full Flux for a Concord window?

### BCI legibility

For later P3/P5 sessions:

- Could the player describe what Sight changed?
- Could the player describe what Guard changed?
- Did looking at the Soul Wisp make primary combat warnings harder to read?
- Did Concord create anticipation before Twin Eclipse?

## Features deliberately deferred

Until repeated P2 evidence exists, do **not** add:

- new boss phases;
- more projectile pattern families;
- additional neural classes;
- motor-imagery movement;
- P300 menus;
- progression trees;
- procedural encounters;
- VR;
- multiplayer;
- large difficulty retunes based solely on code inspection.

The next tuning change should be explainable by an observed failure or an observed missed opportunity.

## Promotion boundary

This branch can earn software P0 in CI.

It cannot earn P1 without Unity 2022.3.76f1 actually importing/compiling/assembling the exact head, and it cannot earn P2 without an actual controller-only session.
