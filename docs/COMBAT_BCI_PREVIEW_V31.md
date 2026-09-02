# V0.31 Combat + BCI Preview

V0.31 makes the Dragon Souls production slice explicitly prove two things at the same time:

1. the inherited sword system is opening real animation-driven damage windows and landing real `Damage -> Health` contacts;
2. Mindforge's Sight / Guard / Concord stimulus language can remain visible during ordinary combat without taking over movement or combat authority.

## Sword combat authority

Mindforge does not implement a parallel sword damage system in V0.31.

The authoritative chain remains the pinned Dragon Souls chain:

1. `InputReader` publishes the existing light/heavy attack events;
2. `PlayerCombatState` selects a serialized `Attack`, consumes stamina and plays its authored attack animation;
3. animation events call `CombatController.EnableSwordHitbox()` and `DisableSwordHitbox()`;
4. `EnableSwordHitbox()` calls the inherited `Sword.StartAttack()`;
5. `Sword.StartAttack()` enables its existing `CapsuleCollider`, `Damage`, swipe sound and `TrailRenderer`;
6. `Damage.OnTriggerEnter()` calls the target `Health.TakeDamage()` and emits `OnHitGiven`;
7. `Sword.StopAttack()` closes the collider, damage owner and trail.

The Mindforge Aetherblade remains a collider-free visual child of that exact Sword root, so the glowing blade follows the inherited hand/skeleton animation while the original Sword collider remains the only melee contact authority.

## Desktop input repair

The pinned Dragon Souls input asset contains complete gamepad combat bindings but an incomplete Mouse+Keyboard binding set. V0.31 repairs that locally at runtime by adding bindings to the existing generated `Controllers.Player` action map owned by the inherited `InputReader`.

No upstream `.inputactions` asset is modified and no Mindforge component invokes attack events directly.

### V0.31 desktop controls

| Action | Mouse / keyboard |
| --- | --- |
| Move | WASD |
| Camera | Mouse movement |
| Jump | Space |
| Light sword attack / combo | Left mouse |
| Heavy sword attack / LLH finisher input | Right mouse |
| Lock target | Middle mouse |
| Sprint | Left Shift |
| Roll | Left Alt |
| Aim / sword throw stance | Q |
| Recall sword | R |
| Sheath / unsheath | X |
| Heal | H |
| Memory Forge / bonfire interaction | E |
| Pause | Escape |

Gamepad bindings remain inherited and unchanged.

## Sword runtime assurance

`MindforgeSwordCombatAssuranceV31` is read-only instrumentation around the inherited sword.

It verifies the scene contains:

- one player combat controller;
- one authoritative `Sword`;
- a real sword `CapsuleCollider`;
- a real `Damage` component;
- the inherited sword `TrailRenderer`;
- at least three authored light attacks;
- at least one authored heavy attack;
- positive attack durations and damage values;
- the Mindforge Aetherblade presentation.

During Play Mode it records:

- each transition into a real collider + Damage swing window;
- whether the sword trail was active when that window opened;
- the current authored attack animation name;
- each `Damage.OnHitGiven` contact;
- any attack window that remains open beyond the bounded diagnostic duration.

It never opens or closes a hitbox, writes damage, changes state or moves the player.

## BCI orb preview

`MindforgeBciOrbV31` is a small camera-anchored orb with three simultaneous temporal stimulus nodes:

| Intent | Requested simulation frequency | Identity |
| --- | ---: | --- |
| Sight | 8 Hz | cyan |
| Guard | 10 Hz | amber |
| Concord | 12 Hz | magenta |

Each node uses analytic sinusoidal luminance modulation based on `Time.unscaledTime` rather than a hand-authored blinking animation. The three nodes therefore continue modulating while the player moves, targets, attacks, rolls and fights the boss.

The default preview uses an 18% luminance modulation range and high-contrast preview is disabled. The stimulus is intentionally compact and kept in peripheral screen space so the first native test evaluates whether it is readable without dominating combat composition.

### Important visual-stimulus caution

Rapid luminance modulation can be uncomfortable and can provoke symptoms in photosensitive users. The reduced-contrast, compact default is a development choice, not a guarantee of safety. High-contrast preview remains disabled by default and should not be enabled casually.

## Scientific boundary

The numbers 8 Hz, 10 Hz and 12 Hz are **requested simulation frequencies**.

They are not yet claims about the exact optical stimulus emitted by the display. Actual presentation depends on:

- monitor refresh rate;
- Unity frame pacing;
- dropped or duplicated frames;
- display pixel response;
- compositor behavior.

The V0.31 readiness audit therefore leaves `bci_physical_display_frequency` explicitly **UNOBSERVED**. Physical stimulus timing requires measured display evidence, ideally frame telemetry plus a photodiode trace.

The orb also does not decode or publish an intent. It only listens to `MindforgeIntentBusV29.IntentPublished` so a separately simulated or future decoded intent can temporarily highlight the corresponding node. Movement and combat continue to ignore the orb.

## Native V0.31 qualification run

Use the existing local Dragon Souls checkout. Do not use `--refresh` unless the external checkout itself is missing or corrupt.

```bash
git fetch origin
git checkout feat/v31-production-vertical-slice
git pull --ff-only origin feat/v31-production-vertical-slice
bash tools/bootstrap_dragonsouls_chassis.sh
```

Open the local Dragon Souls `ThirdPersonCombat` project in Unity `2021.3.20f1`.

Run:

**Mindforge -> World V0.31 -> PLAY VERTICAL SLICE**

Then perform this focused sequence:

1. confirm the BCI orb shows Sight 8 Hz, Guard 10 Hz and Concord 12 Hz simultaneously;
2. move with WASD and verify mouse camera remains usable with the orb visible;
3. press left mouse at least three times with combo timing and confirm visibly distinct authored sword swings and trail windows;
4. press right mouse and confirm a heavy sword animation;
5. land at least one ordinary sword hit on a normal enemy and confirm health/hit feedback changes;
6. middle-click a target and repeat light/heavy attacks while locked on;
7. roll with Left Alt during an enemy attack;
8. aim with Q, throw if appropriate, then recall with R;
9. verify the Aetherblade remains attached to the animated hand during ordinary attacks and still follows the inherited throw/recall root;
10. continue into the boss encounter if practical and verify sword attacks still open and close cleanly while the BCI orb remains visible.

After at least one swing and one landed hit, run:

**Mindforge -> World V0.31 -> Audit Vertical Slice**

The focused combat evidence should show:

- `desktop_combat_bindings_runtime`: PASS;
- `sword_combat_assurance_runtime`: PASS;
- `sword_swing_window_observed`: PASS;
- `sword_damage_hit_observed`: PASS;
- `bci_orb_runtime`: PASS;
- `bci_requested_frequency_map`: PASS;
- `bci_reduced_contrast_default`: PASS;
- `bci_physical_display_frequency`: deferred / unobserved by design.

A 20 to 40 second screen recording containing movement, three light swings, one heavy swing, one landed hit, one roll, target lock and the orb in-frame is more useful than a long exploratory capture for the next tuning pass.

## Promotion rule

V0.31 should remain a draft until the exact branch head passes software contracts and the native Unity run proves:

- zero compiler errors;
- desktop combat bindings actually drive the inherited state machine;
- at least one animation-driven sword window opens and closes;
- at least one real sword damage contact lands;
- no stuck sword collider is observed;
- Aetherblade motion remains attached to the authored swing animation;
- the BCI orb remains legible without materially blocking the combat scene;
- ordinary enemies, roll, target lock, sword throw/recall, death/respawn and boss entry still function.
