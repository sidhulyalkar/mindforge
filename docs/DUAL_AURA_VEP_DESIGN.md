# Dual Aura VEP Design

## The mechanic in one sentence

A persistent magical companion called the **Soul Wisp** splits into two visually encoded aura targets around the current enemy: attending to the **blue Sight aura** temporarily amplifies damage, while attending to the **green Guard aura** temporarily accelerates healing.

The player continues moving, dodging, aiming, and attacking with normal controls while allocating visual attention between the two moving auras.

## What the EEG actually measures

The decoder does **not** measure “damage intent,” “healing intent,” courage, focus, emotion, or a general mental state.

It solves a narrower and more defensible problem:

> Which of two temporally coded visual targets is producing the stronger steady-state visual evoked response in the participant's posterior EEG?

The game maps the decoded target to a gameplay consequence.

```text
look at blue 10 Hz aura
        ↓
visual cortex responds to target modulation
        ↓
posterior EEG contains target-frequency evidence
        ↓
filter-bank CCA scores 10 Hz vs 12 Hz references
        ↓
quality + confidence + dwell gate
        ↓
AURA_SELECTED: sight
        ↓
3.4 s offensive amplification
```

The same pipeline maps the green target to Guard.

## Why SSVEP fits this game better than P300

P300 is excellent for discrete oddball selection. The Dual Aura mechanic asks the player to **continually reallocate attention** between two persistent moving objects during combat.

SSVEP is therefore the primary competition paradigm because each aura can carry a continuous temporal code, selection can be estimated in sliding windows, and the player can switch without waiting for a new oddball sequence.

P300 remains a research comparison or fallback interaction, but it is no longer the main game grammar.

## Hardware target

Primary target: **g.tec Unicorn Hybrid Black**.

Relevant characteristics:

- 8 EEG channels;
- 250 Hz sampling per channel;
- 24-bit acquisition;
- dry or wet hybrid electrodes;
- commonly used channel set Fz, C3, Cz, C4, Pz, PO7, Oz, PO8;
- Bluetooth acquisition;
- accelerometer and gyroscope available for movement context.

For this mechanic, the most informative channels are expected to be posterior sites, especially **Oz, PO7, PO8, and Pz**. That expectation still needs physical validation.

## Stimulus design

| Aura | Gameplay | Initial temporal code | Shape cue |
|---|---|---:|---|
| Blue | Neural Sight / damage | 10 Hz | triangle / three-ray glyph |
| Green | Neural Guard / heal | 12 Hz | ring + cross glyph |

10 and 12 Hz are engineering defaults, not immutable truths. The final codebook must be selected against the exact event display and verified for refresh-rate compatibility, emitted luminance timing, harmonic separation, comfort, and participant classification accuracy.

The Unity implementation uses sampled sinusoidal luminance modulation rather than an abrupt full-black/full-white square-wave flicker.

### Motion

The auras orbit the engaged enemy roughly opposite one another.

Initial orbit speed:

```text
0.92 rad/s ≈ 0.146 Hz ≈ one revolution every 6.8 s
```

This is intentionally slow. Moving visual targets are feasible for VEP BCIs, but faster superimposed motion can reduce SSVEP performance. Game animation must therefore respect the neural signal.

The orbit should be camera-facing so targets remain visible, apparent target size remains stable, and neither aura repeatedly hides behind the boss.

## Soul Wisp lifecycle

### Exploration

The unsplit Wisp floats near the player's shoulder/body as a soft magical balloon-like companion. It follows with spring-like lag and communicates connection/signal quality subtly.

### Enemy engagement

The Wisp travels toward the active enemy and bifurcates.

#### Blue: Neural Sight

Initial playtest tuning:

```text
1.65× outgoing damage
for 3.4 seconds
```

#### Green: Neural Guard

Initial playtest tuning:

```text
+4.2 HP/s regeneration
for 3.4 seconds
```

These are gameplay values, not neuroscience parameters.

### Skilled play

Sight and Guard have independent timers, creating an expert loop:

```text
attend blue
→ earn damage window
→ return attention to combat
→ attend green
→ overlap healing with remaining damage buff
→ attack / dodge
→ refresh blue before expiry
→ adapt continuously
```

The player's optimization problem is **attention allocation under combat pressure**.

## Selection timing

Initial decoder configuration:

- 250 Hz EEG;
- 1.25 s analysis window;
- intended ~0.25 s hop;
- 3 harmonics;
- filter-bank CCA;
- two consecutive accepted windows before switching;
- quality, absolute-score, and winner-margin gates;
- short refractory period after an accepted switch.

A typical successful selection may therefore require roughly 1–2 seconds of usable attention, depending on actual hop size and participant thresholds. The game does not promise sub-second thought control.

## Calibration ritual

1. **Wisp awakening** — signal readiness while the companion appears.
2. **Blue attunement** — labeled Sight windows.
3. **Green attunement** — labeled Guard windows.
4. **Alternation** — randomized target prompts teach switching and estimate score distributions.
5. **Moving-orb validation** — both targets orbit at combat speed while selections are prompted.
6. **Session threshold** — fit conservative acceptance gates from that participant's session.

A stationary calibration that fails once the targets move is not competition-ready.

If performance is not usable, the system should check electrode quality, repeat a short block, optionally test another predeclared target codebook, or fall back gracefully rather than claim a working BCI.

## Decoder

The first implementation uses **filter-bank canonical correlation analysis (FBCCA)**.

For target frequency `f`, construct sine/cosine references over the first three harmonics. For each filter-bank band, band-pass the EEG, compute canonical correlation against each target's reference bank, combine weighted scores, choose the highest target, and reject if absolute score or winner margin is weak.

Future measured variants can include individual-template CCA or TRCA/eTRCA. Complexity should increase only if physical data justifies it.

## Artifact and uncertainty policy

Movement is unavoidable because this is a videogame. The decoder therefore needs to handle blinks, jaw/face EMG, head motion, loose electrodes, saturation, flat channels, Bluetooth packet gaps, and stale streams.

The key rule is:

> An uncertain window produces no gameplay switch.

It emits `ABSTAIN`. The previous buff simply continues until its timer expires.

## Input independence

The BCI must not secretly consume controller state as evidence for neural target identity. If the player is low on health, the classifier must not bias itself toward Guard merely because Guard would be strategically sensible.

Gameplay context may influence presentation, but decoded aura identity must remain grounded in the neural target score.

## Accessibility

Blue and green are never the only distinction.

Sight also uses a triangular glyph, sharper motion language, and a higher-pitched motif. Guard uses a ring/cross glyph, rounded pulse motion, and a warmer/lower motif.

Controller-only mode remains fully playable. Visual periodic stimulation requires a warning and immediate opt-out.

## Why this is competition-worthy

The jury can understand it immediately:

> “The player is fighting with the controller while deciding with visual attention whether the soul companion should empower offense or recovery.”

The causal chain is visible:

```text
gaze / attention target
→ posterior VEP evidence
→ decoder score
→ accepted neural event
→ aura flashes into player
→ damage or healing visibly changes
```

A novice may hold Sight. A strong player learns to refresh Sight before burst attacks, trigger Guard after damage, exploit overlap between independent timers, and decide when taking their eyes off hazards is worth the neural benefit.
