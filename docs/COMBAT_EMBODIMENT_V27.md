# V0.27 Guardian Embodiment + Fractured Beast

V0.27 is the first Mindforge tranche that treats **character embodiment and encounter staging as production presentation problems** rather than asking post-processing to disguise primitive bodies.

The product label is **V0.27 Guardian Embodiment + Fractured Beast**. It composes after V0.26 and does not replace any proven movement, hit detection, collision, boss scheduling, Wisp or neural authority.

## Why this tranche exists

Recent playtests show that the cathedral is becoming increasingly coherent while the two most important moving silhouettes still expose the prototype underneath:

- the Guardian carries a convincing energy blade but the procedural right arm does not visibly own the weapon arc;
- the Fractured Signal reads primarily as an arrangement of illuminated shards instead of a creature with mass, anatomy and intent;
- the boss chamber is visually improved, but its static architecture does not react to the encounter strongly enough to make the duel feel staged.

V0.27 fixes those three presentation gaps while preserving the game systems below them.

## 1. Guardian sword-arm embodiment

`GuardianCombatEmbodimentV27` is a render-only upper-body layer.

It waits for the canonical `V11GuardianVisual` and runtime `PhysicalArsenalRig/SwordRoot`, then retires only the old visible `ArmR` and `HandR`. The existing Guardian collider, Rigidbody, locomotion controller, sword controller and mathematical sword sweep are untouched.

The replacement arm is a small procedural rig:

- pauldron;
- tapered upper arm;
- elbow guard;
- tapered forearm;
- gauntlet;
- emissive Aether wrist band.

Each frame after combat simulation it reads:

- `GuardianSwordShieldController.IsAttacking`;
- `IsGuarding`;
- `ComboStep`;
- `AttackProgress`;
- the conventional aim direction.

It derives a shoulder-to-wrist target from those authoritative values and solves a two-bone triangle for the elbow. Combo 1 and 2 use opposing lateral arcs; combo 3 uses an overhead quadratic path. The torso, chest, helmet and off-hand add bounded follow-through so the swing reads as a body action rather than a rotating prop.

The visible sword hilt is translated to the solved wrist. **This does not move hit authority.** `GuardianSwordShieldController` continues to calculate the physical sweep independently from fixed-tick combo state, so presentation cannot create extra reach, damage, parries or contacts.

During Wisp calibration or resonance the rig settles to a neutral static pose.

## 2. Fractured Signal as an animalistic cathedral parasite

`FracturedSignalBeastV27` replaces the abstract broken-knight render root with a new organic body while leaving the boss root and all combat components intact.

The silhouette is deliberately low and heavy rather than humanoid:

- one continuous lofted parasite body;
- darker belly mass;
- broad jowls;
- recessed maw and articulated lower jaw;
- corrupted signal tongue;
- paired cyan sensory eyes;
- short load-bearing forelimbs;
- sensory feelers;
- nine dorsal magenta signal crystals.

The visual concept is **a cathedral organism overtaken by corrupted signal**, not a licensed or copied film creature. The broad slug-like mass supplies animal readability; the dorsal fractures preserve the existing Fractured Signal identity.

Animation remains downstream of existing authoritative events:

- movement activity produces a slow crawl/sway;
- the head tracks the Guardian within a small angular envelope;
- boss telegraph events raise the head/jaw and signal charge;
- attack-fired events release the jaw and crystal response;
- damage events produce a short body recoil;
- phase changes deepen corruption intensity.

No new attack, damage, navigation or collision path is introduced.

## 3. Encounter-stage dynamics

`CombatEmbodimentV27Builder` authors a new collider-free root under the canonical world:

`Mindforge_Combat_Embodiment_V27/V27_Fractured_Signal_Arena`

It preserves the existing arena floor and duel dimensions. New pieces are presentation only:

- thin gold ritual ring segments;
- magenta radial signal axes;
- a broken outer cyan rite ring;
- ten perimeter corruption spines;
- a north-side beast altar / pointed-arch frame;
- six restrained encounter-local lights.

`FracturedArenaDynamicsV27` listens to the existing boss phase, telegraph and attack-fired events. It changes only spine scale, renderer emission and local light intensity. The scene builder fails closed if the new root contains a Collider or Rigidbody.

This gives phases a visible environmental response without turning the arena dressing into a second hazard system.

## Neural visual-field boundary

All three V0.27 runtime presentation systems explicitly inspect the Wisp calibration/resonance state.

During those windows:

- Guardian combat embodiment returns to a neutral arm pose;
- the beast stops idle/crawl/target-tracking animation and settles its jaw/crystals;
- arena dynamic growth and event response settle to a static neutral state.

V0.27 therefore does not add an uncontrolled temporal visual stimulus during neural evidence collection.

## Authority map

- **V0.11 / V0.23:** player movement, route collision and physical world authority.
- **GuardianSwordShieldController:** sword sweep, contact, parry, damage and combo truth.
- **FracturedSignalFirstBossV19 / directors:** boss movement and attack truth.
- **V0.24:** cathedral layout/material grammar.
- **V0.25:** post, sensory presentation, pooled VFX and HUD.
- **V0.26:** production world geometry and depth.
- **V0.27:** character embodiment and encounter-stage presentation only.

## Focused playtest gate

Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)** and evaluate these before tuning damage or boss cadence:

1. Idle near the boss while locked on. The Guardian should hold the sword with an identifiable shoulder/elbow/wrist chain rather than a detached glowing bar.
2. Perform all three sword combo steps from front, side and diagonal camera angles. The off-hand and torso should visibly counterbalance the right arm without stretching or snapping.
3. Guard, release guard, dodge and immediately attack. The hilt must stay visually attached to the gauntlet and the physical hit result must remain unchanged.
4. Orbit the new Fractured Signal. It should read first as a heavy animal/parasite and second as signal corruption, not as a floating shard cloud.
5. Watch a telegraph and attack release. Jaw, head, crystals and arena response should reinforce the same event without obscuring the actual combat telegraph.
6. Enter phase 2 and phase 3. Perimeter corruption should become more assertive while leaving the playable floor completely unobstructed.
7. Trigger calibration or Wisp resonance. Guardian, beast and arena dynamic presentation must settle instead of continuing to pulse or sway.
8. Re-test the full boss floor, edges and approach. No new collision should exist anywhere inside the V0.27 arena root.

## Deliberate limits

V0.27 is still procedurally authored character art, not a final externally sculpted/skinned production character. It is intended to prove silhouette, embodiment, encounter framing and animation language before committing to a larger authored asset pipeline.

The next visual tranche should be driven by the V0.27 recording. Likely targets are the Guardian's remaining torso/legs, stronger creature skin/material breakup, authored boss attack locomotion and environmental set dressing, but those should be chosen from observed gameplay rather than added pre-emptively.
