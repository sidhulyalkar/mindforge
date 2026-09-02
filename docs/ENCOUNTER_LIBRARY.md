# Mindforge Encounter Library

Encounters are authored compositions of semantic enemy roles, waves and spacing. They are not arbitrary spawn counts.

`MindforgeEncounterLibraryV32` currently defines the first two showcase recipes.

## Enemy roles

### Remnant

Baseline melee pressure.

Expected group behavior:
- approaches primary combat ring;
- commits clearly;
- leaves readable recovery;
- low simultaneous-pressure priority.

### Warden

Defensive timing role.

Expected group behavior:
- anchors a lane;
- blocks or counters predictable frontal pressure;
- creates a reason to flank, heavy-attack or use a neural mechanic.

### Ranger

Long-range displacement role.

Expected group behavior:
- remains outside melee ring;
- attacks on slower cadence;
- should not overlap melee commitment timing constantly.

### Stalker

Flank role.

Expected group behavior:
- changes lateral angle;
- commits briefly;
- disengages rather than permanently stacking on the player.

### Resonant

Area-control / neural role.

Expected group behavior:
- creates high-information telegraph;
- pressures location rather than simply adding another melee body;
- natural target for Sight/Guard interactions.

### Brute

Large spacing role.

Expected group behavior:
- controls a broad arc;
- attacks less frequently;
- must retain camera/player separation;
- creates punish windows after commitment.

## Recipe: First Fracture

ID: `showcase.first_real_encounter`

Purpose: teach target lock and roll with two distinct threat vectors.

Composition:
- wave 1 Remnant at approximately 3.4 m preferred range;
- wave 1 Ranger at approximately 7.0 m preferred range, delayed 1.8 s;
- maximum one simultaneously committed attacker;
- intended player breathing room at least 3.2 m.

The Ranger is support pressure, not a second body entering the same melee point.

## Recipe: Broken Choir

ID: `showcase.elite_encounter`

Purpose: require target management and spacing before the boss approach.

Composition:
- wave 1 Brute anchor;
- wave 1 Stalker after 0.9 s;
- wave 2 Resonant after 2.0 s;
- optional Ranger after 3.2 s;
- maximum two simultaneously committed attackers;
- intended breathing room at least 3.6 m.

The optional Ranger is a tuning lever. Do not enable it merely to increase difficulty if the first three roles already saturate readability.

## Encounter socket resolver target

A future `MindforgeEncounterResolver` should map semantic roles onto qualified local enemy prefabs.

Requirements:
- role-to-prefab mapping is explicit and versioned;
- spawn sockets are authored in chunk metadata;
- every spawn position is sampled to NavMesh;
- no spawn directly inside player camera view unless intentionally staged;
- no spawn within player breathing-room radius;
- no hidden spawn inside solid geometry;
- waves activate deterministically;
- encounter reset restores exact starting state;
- completed one-time encounters persist by stable world ID where appropriate.

## Attack-token coordinator target

The existing V0.31 formation layer improves approach positions. V0.32+ should separately coordinate attack commitment.

A future coordinator may grant a bounded number of attack tokens per encounter.

This coordinator may decide **when a role is allowed to commit**, but must not directly play animations or deal damage. Enemy AI/state machines remain responsible for executing their own attacks.

Initial token budgets:
- tutorial duel: 1;
- First Fracture: 1;
- Broken Choir: 2;
- boss: boss-specific, not generic crowd token logic.

## Reset contract

On player death or checkpoint reset:
- active wave timers reset;
- enemy health/state resets through existing enemy reset authority;
- dead temporary enemies return if encounter was not completed;
- one-time reward duplication is prevented;
- encounter-specific visual effects are removed;
- BCI semantic state does not silently persist unless designed to.

## Qualification

For every encounter recipe capture:
- player entry;
- initial enemy positions;
- first attack commitment;
- target switching;
- roll opportunity;
- worst crowd compression moment;
- final enemy death;
- reset after player death.

Reject an encounter if challenge comes primarily from overlapping bodies or unreadable simultaneous telegraphs.
