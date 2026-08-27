# Realized Neural Payoff Evidence

Mindforge should be able to answer two different questions without conflating them:

1. **Did the neural system accept a Sight or Guard selection?**
2. **Did that accepted state materially change a later game consequence?**

`NEURAL_BUFF_APPLIED` answers the first question.

This document defines the first conservative evidence layer for the second.

## Principle

> Count a neural payoff only when the incremental consequence actually occurs.

Do not infer payoff from overlapping timers after the fact.

A Sight aura can expire while a boosted projectile is still flying. The projectile therefore carries its incremental direct-damage attribution until the hit resolves. Guard healing is measured from actual HP restored after max-health clipping.

## Realized direct-damage bonus

`DamagePacket` carries two additive fields:

```text
NeuralPayoffKind
NeuralBonusDamage
```

The bonus is defined against the same action's non-neural direct-damage baseline.

Current labels are:

```text
SIGHT_PULSE_DAMAGE
SIGHT_CLEAVE_DAMAGE
CONCORD_COUNTER_DAMAGE
TWIN_ECLIPSE_DAMAGE
```

### Overkill correction

Requested bonus damage is not automatically realized bonus damage.

Suppose:

```text
target HP before hit = 8
ordinary Pulse damage = 13
Sight Pulse damage = 19
requested neural bonus = 6
```

The ordinary Pulse already removes all 8 remaining HP. The realized incremental direct-damage contribution of Sight is therefore **0**, not 6.

In general:

```text
actual boosted damage = min(HP_before, boosted_damage)
baseline actual damage = min(HP_before, boosted_damage - requested_bonus)
realized neural bonus = max(0, actual boosted damage - baseline actual damage)
```

This calculation happens in `CombatantVitals`, where pre-hit health is known.

## Realized Guard healing

`CombatantVitals.Heal` returns the amount actually restored after max-health clipping.

Continuous Guard regeneration is accumulated and emitted at a bounded cadence instead of producing a UDP marker every render frame.

Counter-linked Guard healing is emitted immediately because it is already a discrete combat event.

Current labels are:

```text
GUARD_REGEN_REALIZED
GUARD_COUNTER_HEAL_REALIZED
```

A Guard selection while already at full health may legitimately produce zero realized healing.

## GameMarker evidence

The Unity semantic trace adds:

```text
NEURAL_DAMAGE_BONUS_REALIZED
NEURAL_GUARD_HEAL_REALIZED
```

Damage markers carry:

```text
target = boss | echo
reason = payoff kind
value  = realized incremental direct damage
```

Healing markers carry:

```text
target = guardian
reason = GUARD_REGEN_REALIZED | GUARD_COUNTER_HEAL_REALIZED
value  = actual HP restored
```

Dynamic Fractured Echoes are explicitly observed so a player is not penalized in the evidence simply for spending a Sight window on the tactically correct secondary target.

## Encounter report

`mindforge.encounter_report.v1` remains additive and now reports:

```text
neural_damage_bonus_events
realized_neural_bonus_damage_total
sight_pulse_bonus_damage
sight_cleave_bonus_damage
concord_counter_bonus_damage
twin_eclipse_bonus_damage
neural_damage_bonus_to_boss
neural_damage_bonus_to_echoes

guard_heal_events
realized_guard_healing_total
guard_regen_healing
guard_counter_healing
```

It also raises review flags when Sight or Guard was accepted but the corresponding conservative payoff was never observed.

## What this ledger deliberately does not count

The totals are a **lower bound**, not a complete valuation of the BCI.

They do not currently price:

- Sight projectile-speed advantage;
- Sight pierce value;
- Sight Cleave range/arc expansion;
- damage avoided because Guard healing changed later survival;
- the strategic value of preserving a heal for a later moment;
- attention-switch Flux;
- Concord radius expansion;
- extra Twin Eclipse capture duration;
- target-access value against Echoes;
- future damage enabled by an earlier Signal Break;
- subjective confidence, excitement, or perceived agency.

Those effects may matter substantially, but assigning them a scalar value without an explicit counterfactual would make the evidence less trustworthy.

## What this still does not prove

A realized payoff marker proves that **the game consequence was different under the accepted neural state according to the implemented game rules**.

It does not by itself prove:

- that the EEG selection was physiologically valid;
- that the participant intentionally produced the selection;
- that Sight/Guard improves player performance overall;
- that the BCI improves enjoyment;
- that the chosen game balance is optimal.

Those questions require higher promotion gates and controlled comparison.

## Future controlled comparison

Once P1–P5 are reliable, the strongest evaluation is not a bigger payoff counter. It is a counterbalanced comparison such as:

```text
A: controller-only baseline
B: same encounter + neural strategic layer
```

using comparable players/runs and predefined outcomes such as completion, damage taken, Signal Break cadence, resource use, comprehension, and enjoyment.

The realized-payoff ledger then explains **where** the neural layer changed the run without pretending that one additive number captures the whole experience.
