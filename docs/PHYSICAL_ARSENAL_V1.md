# Physical Arsenal v1

Mindforge's first physical-combat vertical slice turns the Guardian into a real sword-and-shield combatant while preserving the project's central BCI authority rule:

> **Hands choose the action. Neural evidence may amplify an already-chosen action, but never creates one.**

This tranche is intentionally one complete build rather than a shallow catalog of cosmetic items.

## The v1 Guardian build

| Slot | Item | Mechanical identity |
| --- | --- | --- |
| Main hand | **Aetherblade Longsword** | 3.2 kg, 2.15 m nominal reach, 132° committed light sweep, stamina-backed impact |
| Off hand | **Verdant Ward Shield** | 7.4 kg kite shield, physical projectile coverage, stability, chip damage, perfect-guard timing |
| Armor | **Warden Weave** | 16 kg medium armor; contributes to equip load in v1 |

Default equipped mass is approximately **26.6 kg** against a 52 kg capacity, producing **Medium Load**.

Armor damage mitigation is represented in the future-facing data contract but is **not an active v1 mechanic**. The build screen therefore describes armor as load-bearing rather than claiming unimplemented protection.

## Controls

- **WASD**: move
- **Mouse / Arrow keys**: player-owned aim
- **LMB**: Aetherblade light sweep
- **RMB hold**: raise Verdant Ward
- **Left Shift**: dodge roll
- **Tab**: Guardian build screen
- **Space**: legacy ranged Pulse
- **F**: Rift Cleave
- **C**: arcane Counter Pulse
- **R**: Gravity Bloom / Twin Eclipse when available

The onboarding teaches sword, shield and roll before the older arcane verbs.

## Combat grammar

Sword, shield and roll share a single stamina economy.

### Sword

A light sword action has wind-up, active contact and recovery. Only the middle portion of the animation owns hit frames. The player cannot cast another offensive ability through the sword commitment window.

Contact uses a swept capsule along the current blade direction rather than a simple distance check. A target may be damaged at most once per swing.

The physical impact proxy uses:

```text
swing momentum proxy = weapon mass × effective reach × angular velocity
```

This is a gameplay model, **not a claim of SI-accurate rigid-body injury simulation**. The value is bounded and converted into damage, poise and impulse scaling so different future weapons can feel physically distinct without destabilizing the encounter.

### Shield

The raised shield owns an actual trigger volume in front of the Guardian. Hostile projectiles encounter that surface before body damage resolution.

For an ordinary block:

```text
guard stamina cost ∝ incoming damage × shield guard cost / effective stability
chip damage = incoming damage × (1 - effective absorption)
```

If stamina is insufficient, guard breaks and the projectile is not magically consumed.

A short **perfect-guard window** immediately after raising the shield reduces stamina cost and reflects the projectile. If Concord is active, reflected damage is genuinely increased first; only the realized difference from the non-Concord baseline is eligible for neural payoff attribution.

Holding guard is itself a choice with opportunity cost:

- movement is reduced;
- stamina recovery falls to roughly one-third;
- offensive verbs are suppressed while the shield is raised.

### Dodge

A successful dodge:

- spends stamina according to equip load;
- immediately lowers guard;
- owns the fixed command frame so an attack cannot be started simultaneously;
- has a short independent i-frame window;
- continues moving after the i-frame expires.

The projectile system treats an i-frame overlap as a miss and leaves the projectile alive so it can physically continue past the Guardian.

The i-frame duration is intentionally independent of the full roll duration. Heavy equipment can therefore lengthen/sluggishly alter roll motion without accidentally granting extra invulnerability.

## Equip load

`GuardianEquipmentLoadout` is the source of truth for the current build and future inventory system.

Total equipped mass determines one of four load classes:

- Light
- Medium
- Heavy
- Overloaded

Load class changes movement speed, roll speed, roll duration and roll stamina cost. Future greatswords, spears, axes, hammers, bucklers and tower shields already have explicit archetype seams, but they are not claimed as implemented content.

The v1 build screen is deliberately read-only. It proves the UI/data boundary without pretending an inventory exists before multiple items are actually authored and qualified.

## Neural armament resonance

`NeuralFocusResonance` observes the existing derived `NeuralEvent` evidence stream. It does not read raw EEG and has no methods capable of issuing combat commands.

Fresh, artifact-free Sight and Guard scores are converted into smooth bounded 0–1 resonance values using score strength, relative dominance and signal quality.

Those values have no direct authority until the corresponding **accepted aura state** is active.

### Sight → sword

When accepted Sight is active:

- stronger current Sight evidence lengthens the visible blade;
- the blade widens slightly;
- blue emission, point light and trail intensity increase;
- gameplay reach grows by the same bounded fraction used by the visual rig;
- only the incremental direct damage above the physical baseline is tagged `SIGHT_SWORD_DAMAGE`.

The existing realized-payoff ledger still performs overkill correction at `CombatantVitals`, so nominal bonus is not automatically counted as realized neural value.

### Guard → shield

When accepted Guard is active:

- stronger current Guard evidence enlarges shield coverage;
- green emission/light strengthens;
- effective stability increases;
- damage absorption increases within a hard cap.

The player must still hold RMB. EEG cannot raise the shield.

### Why continuous evidence is secondary, not authority

A classifier selection and a continuous evidence trace are different things. The accepted Sight/Guard event determines whether the neural state may influence gameplay. The continuously smoothed score then determines how strongly the already-authorized armament manifests.

This gives the player immediate graphical biofeedback without letting noisy frame-to-frame evidence create actions.

## Soul Wisp and SSVEP boundary

The existing Soul Wisp already presents blue Sight and green Guard targets around the enemy gaze corridor. `NeuralAuraFeedback` explicitly modifies the **non-coded shell** rather than the measured luminance material that carries the 10/12 Hz code.

The physical arsenal follows the same principle: sword flames/trails and shield growth are downstream feedback. They must not amplitude-modulate the stimulus core that generated the evidence.

The current moving/orbiting presentation remains subject to real display and participant qualification. Software architecture alone does not establish that an orbiting 10/12 Hz target preserves the desired physiological response under every display, viewing distance or participant.

## Telemetry

`PhysicalArsenalMarkerBridge` additively emits:

- `PHYSICAL_ARSENAL_READY`
- `SWORD_LIGHT`
- `SWORD_HIT`
- `SHIELD_RAISED`
- `SHIELD_LOWERED`
- `SHIELD_BLOCK`
- `PERFECT_GUARD`
- `GUARD_BROKEN`

Encounter analytics now report:

- equipped mass/load class;
- sword attempt/hit rate;
- shield raises/blocks;
- perfect guards;
- guard breaks;
- total shield chip damage;
- realized Sight sword bonus damage.

This lets P2/P3 playtests distinguish mechanical difficulty from neural effectiveness. For example, repeated guard breaks indicate a stamina/timing problem even if the decoder itself is functioning perfectly.

## Procedural art status

The runtime bootstrap currently creates a procedural glowing blade, hilt, shield, shield outline, energy trail and lights. These are **functional prototype presentation**, not a claim of AAA asset fidelity.

The important architectural property is that presentation is behind typed rig/configuration seams. Authored meshes, hand sockets, animation clips, VFX Graph effects, shaders and character rigs can replace the procedural geometry without changing combat authority or payoff accounting.

## What should come next

Do not add ten weapons before this one feels good.

The next qualification sequence should be:

1. **P0 software:** exact-head tests and static contracts.
2. **P1 Unity:** compile, scene launch, bootstrap wiring, visible sword/shield, collision sanity.
3. **P2 controller-only:** repeatedly play the boss with neural authority disabled; tune sword commitment, roll, shield stability, stamina and boss spacing until the physical game is fun on its own.
4. **P3 neural strategy:** compare accepted Sight/Guard conditions and verify that continuous armament feedback is understandable, useful and physiologically stable.
5. **Art/animation tranche:** replace procedural geometry with authored character/weapon animation and VFX while preserving the same combat rules.
6. **Only then expand inventory:** add genuinely different weapon/shield/armor families and a mutable build screen.

A larger inventory should amplify a proven combat system, not conceal an unproven one.

## Non-claims

This branch does **not** by itself prove:

- Unity compilation or runtime scene integrity;
- good sword feel;
- correct difficulty balance;
- physical display timing;
- participant SSVEP performance;
- superior enjoyment versus the controller-only build;
- AAA visual fidelity;
- complete armor defense;
- a production inventory/equipment economy.

Those require the next evidence gates rather than more source-code confidence.
