# Playtest Metrics: Turning a Fight Into Design Evidence

Mindforge should not wait for a headset to answer whether its combat is readable, rewarding, learnable and appropriately paced.

Every controller-only and no-headset development run can now be captured from the passive `GameMarker` lane and summarized with:

```bash
python tools/mindforge_dev.py marker-log \
  --output experiments/markers/playtest-01.jsonl

python tools/mindforge_encounter.py \
  experiments/markers/playtest-01.jsonl \
  --output experiments/reports/playtest-01.json
```

The report schema is:

```text
mindforge.encounter_report.v1
```

## What is measured

### Encounter shape

- terminal outcome (`VICTORY`, `DEFEAT`, or `INCOMPLETE`);
- observed marker duration;
- highest boss phase reached.

### Conventional combat

- Pulse Shots;
- Rift Cleave attempts and successful hits;
- cleave hit rate;
- Counter Pulse attempts and successful reflects;
- counter conversion rate;
- Phase Dashes;
- rewarded near misses.

### Pressure

- player damage events and total damage;
- heavy player hits;
- boss damage events and total damage;
- heavy boss hits.

### Fight punctuation

- Signal Breaks;
- Gravity Bloom charge/release count;
- final observed Flux value.

### Signature BCI loop

When a neural development source is active, the same report also shows:

- Sight buff applications;
- Guard buff applications;
- Concord establishments;
- Twin Eclipse charge/releases;
- neural-link degradation count and observed degraded duration.

These are game-system observations. They do not claim physiological validity.

## Diagnostic flags are questions, not grades

The analyzer intentionally does **not** compute a global `fun_score` and does not mark a playtest PASS/FAIL.

Instead, it raises specific questions such as:

```text
ENCOUNTER_UNDER_90_SECONDS
ENCOUNTER_OVER_7_MINUTES
COUNTERS_ATTEMPTED_WITH_ZERO_REFLECTS
CLEAVES_ATTEMPTED_WITH_ZERO_HITS
TERMINAL_RUN_WITH_ZERO_SIGNAL_BREAKS
PLAYER_TOOK_DAMAGE_WITHOUT_RECORDED_BOSS_DAMAGE
NEURAL_LINK_DEGRADED_DURING_RUN
```

A flag is a prompt to watch the run and investigate. It is not proof that the tuning is wrong.

For example, three failed Counter Pulses may mean:

- the counter window is too tight;
- the visual telegraph is too weak;
- projectiles are entering from outside the player's attention cone;
- onboarding did not teach the mechanic;
- the player simply mistimed three attempts.

The metric narrows the question. Video/play observation answers it.

## P2 controller-only playtest protocol

For early P2 sessions, prioritize the game before the BCI.

A useful loop is:

1. generate/validate the competition scene through P1;
2. play one complete encounter with conventional combat;
3. capture the passive GameMarker stream;
4. generate an encounter report;
5. write 3–5 timestamped observations from the run;
6. change only a small number of tuning variables;
7. play again.

Recommended first-pass design targets are intentionally broad:

```text
time to meaningful control      < ~60–90 s
full encounter                  ~4–6 min
counter mechanic                understood and used
Signal Break                    occurs as readable punctuation
near miss                       feels earned, not accidental
player damage                   explainable on review
boss phases                     feel qualitatively different
```

These are tuning targets, not qualification claims.

## What to ask the player

After a session, ask a few short questions rather than a long survey:

- Did you know why you were hit?
- Which action felt best to use?
- Was there a moment you felt in control of the fight?
- Was anything visually noisy or hard to parse?
- If Sight/Guard were active, did looking toward the Wisp make combat unfair?
- Did Concord/Twin Eclipse feel earned rather than automatic?
- Would you play another round?

The last question is especially valuable. A technically impressive BCI system that nobody wants to replay is not the reference example we are trying to build.

## How to use the data

The encounter report should steer development toward concrete game problems:

```text
low counter conversion
    → telegraph/window/readability investigation

high damage + low near misses
    → projectile readability/mobility investigation

very short fight
    → boss durability/phase pacing investigation

very long fight + low boss damage
    → damage opportunities or player comprehension investigation

no Signal Breaks
    → poise economy / reflected projectile / cleave incentives

Concord established but no Twin Eclipse
    → Flux pacing / payoff communication / activation opportunity

BCI degraded during combat
    → source liveness or contingency investigation before polish
```

## The development philosophy

A field-leading Mindforge should make game design and BCI engineering observable on the same causal timeline without confusing them.

`GameMarker` is the common language:

```text
what the player did
what the boss did
what damage occurred
what the neural system authorized
what payoff occurred
what the transport did
```

That lets us improve the game aggressively while retaining scientific honesty about which layer each observation actually evaluates.
