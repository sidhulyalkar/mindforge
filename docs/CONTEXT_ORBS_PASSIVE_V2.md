# Mindforge v2: context-aware orbs and passive-BCI game experience

**Review branch:** `feat/contextual-orbs-passive-policy-v2`, stacked on portable
mod bridge draft PR #65. **Scope today:** Python policy, deterministic synthetic
scenario demo, and regression tests. No verified game-state hooks, game ability
edits, SSVEP changes, gameplay effects, passive feature transport to the BepInEx
mod, or live user-study claims.

## Fundamental separation

- `CombatContext`: typed game facts, sourced from a *verified* game-specific
  semantic hook or marked as synthetic test data. A gameplay event is not EEG.
- `ContextOrbPolicy`: whether a small **cosmetic** orb UI may be present and
  where it should appear. `can_offer_selection` only describes a possible
  safe opening for later human/display qualification; `stimulus_enabled` is
  **always false** in v2.
- `NeuralEvent v2`: independent explicit Sight/Guard event authority from the
  calibrated BCI path. The portable bridge remains presentation-only today.
- `NeuralFeatureFrame v1`: separately versioned, derived-only passive
  physiological observation from Living World draft PR #64, never an ability.
- `PassiveAmbientPolicy`: optional, low-amplitude, rate-limited decorative
  coefficient. It cannot affect movement, AI, score, hitboxes, loot, healing,
  attacks, damage or invulnerability. Its output is not a mental-state label.

**Separation of concerns:**

    verified game hooks -> CombatContext -> orb presentation and safety mask
                                      |
    EEG -> quality -> calibrated SSVEP -> NeuralEvent -> active ability *later*
                                      |
    EEG -> quality -> passive spectral feature -> bounded ambient decoration
                                      |
                SSVEP window/calibration owns visual priority

Do not infer player arousal from “boss encountered”, and do not infer player
combat intentions from posterior alpha. The two information sources have
different causal meanings.

## Combat scenarios and orb placement

| Game scenario | Orb presentation | Selection opportunity | Why |
| --- | --- | --- | --- |
| All games: exploration | Hidden | No | Preserve original atmosphere and navigation |
| Enemy nearby, not engaged | Primed, edge | No | Approach is advisory; proximity is not aggression |
| Ordinary fight with readable spacing | Present | Only a verified long safe opening, future work | Offer meaningful strategic power, avoid input overload |
| Hard attack telegraph or precision platforming | Docked at edge | No | Do not lure gaze away from vital information |
| Player parry window | Docked at edge | No | Parry remains manual, never EEG-timed |
| Boss stagger / safe inter-wave break | Present | Possible candidate if verified duration >= 2.0s | Decoder needs enough evidence plus recovery time |
| Combat ends | Resting then hidden | No | Gentle exit rather than UI flicker |
| Pause, death, cutscene, inventory, transition, lost focus | Hidden | No | Avoid surprise overlay and bad game-state interactions |
| Semantic source stops delivering updates | Hidden within 0.75s | No | Stale game context must not be treated as a fight |

The numeric durations in this policy are **provisional UX defaults** and require
instrumentation and user testing. A candidate 2s opening is not proof that a
1.25s EEG analysis window can be delivered safely or reliably; display,
acquisition, dwell, attention-switching and recovery all consume time.

### Hollow Knight

A strong fit for a player-following *cosmetic* companion after native hook
verification. In ordinary exploration hide the companion completely; one enemy
aggro can prime it without activating visual targets. On multi-enemy fights
presence remains restrained. During boss staggers and longer quiet windows,
evaluate selection availability, but never interrupt pogo timing, focus/heal
windows, or a boss's incoming attack. Avoid covering nail strike, Soul meter,
enemy telegraph, platform boundaries and mask health. No extra geometry or
timing advantages should be created merely by enabling the mod.

### Silksong

Hornet's movement and Silk economy create shorter tactical intervals; a
player-following companion may move too much during jumps, silk skills, needle
parries and airborne Bind. Prefer a stable screen anchor or smooth bounded
offset during rapid traversal. Use smaller, slower visual feedback, and do not
confuse active Guard feedback with invulnerability, health, or Silk available.
Never alter Bind costs/animation or infer healing intention from EEG. Boss
stagger can propose an opportunity only when the game hook verifies its
remaining duration.

### Cuphead

Dense projectile fields and pink parry cues make player-following/flickering
stimuli especially undesirable. Use a compact **edge-docked**, stationary
status indicator throughout normal boss fighting, suppress new selection
windows during dodging, flight bullet-hell, parry timing, and heavy motion.
Offer any future deliberate BCI selection only in a verified transition,
inter-wave respite or explicit opt-in practice scene that preserves ordinary
game pacing. A pink parry object remains a native game affordance; never
recolor, occlude or replace it with Mindforge visuals. Preserve normal EX
resource and dash/parry latency.

## Three separate notions of “orb present”

1. **Visual presence:** decorative companion state, nonperiodic and optionally
   static. This is what v2 implements as a **policy**.
2. **Ability feedback:** whether an accepted Sight/Guard selection is currently
   active. This is modeled already by the v1 portable mod bridge, but native
   gameplay effects are not enabled.
3. **Visual coded stimulus:** actual SSVEP target. **Disabled in all external
   games** until the specific display, scene, gaze and photosensitivity gates
   pass. “No stimulus” remains the initial safe default even if the policy
   reports `can_offer_selection=True`.

Never use cosmetics to imply evidence or ability authority. An icon may be
present during lost EEG, but must be clearly passive/inactive. A user must be
able to switch off all BCI visuals.

## Passive BCI: scientific rationale and limits

### What to measure first

**First candidate:** quality-gated, posterior 8–13 Hz relative power, defined
as 8–13 Hz band power divided by 4–30 Hz band power, with verified posterior
channels. This is the existing experimental feature in Living World PR #64.
It is a **spectral measurement**, not a validated calm/focus/boredom/emotion
classifier. It can change because of display stimuli, gaze, movement artifacts,
eye state, alpha blocking and task context. The current 10/12 Hz SSVEP targets
fall *inside* this band, therefore the posterior-alpha measure can be directly
contaminated during active coded stimulation. Never use it to “adapt” the game
during active VEP or calibration windows.

### Proposed passive experiences (all hypotheses)

| Mechanic | Passive observation | Permitted intervention | Key falsification |
| --- | --- | --- | --- |
| Living environment | Bounded deviation of relative alpha from own validated baseline | Slightly change color on **dedicated** non-colliding surfaces, not world luminance | Does it improve presence versus a yoked non-EEG animation? |
| Recovery sanctuary | Longer-term quality-gated baseline change + repeated failure events | Offer *optional* quiet audio/visual mode at safe pause/menu | Is it preferable to player-controlled settings? |
| Ambient music stems | Calibrated task-linked features, and game context | Optional very gradual stem balance or richness adjustment, after user preference and audio QA | Does it interfere with warning sounds or music enjoyment? |
| Tutorial pacing | Primarily game performance/inputs; physiological features exploratory | Suggest optional explanations between encounters; never secretly nerf boss | Does it improve agency and retention versus performance-only? |
| Reflective replay | Time-aligned game events + EEG quality and derived features | Post-session visualization of physiological correlations and uncertainty | Are observed differences above artifacts and repeated-session noise? |

Only the first row has an implemented pure policy; none of these proposals
currently changes a shipped game. Even the first row needs an actual renderer
and native qualification before a user-facing claim.

### Safety, privacy and confounds

- Require explicit **opt-in**, session identity, verified sensor montage and
  units, per-user baseline and quality/artifact gating; passive is off by
  default. A synthetic demo baseline is not a calibrated human baseline.
- Read only derived and bounded features; no raw EEG or medical/psychological
  trait predictions go to the game. Do not store individual profiles without
  explicit opt-in and a documented retention/deletion path.
- Return to neutral on loss, artifact, calibration, active SSVEP, pause or
  source staleness. **Immediately suppress** decor during a coded stimulus,
  not simply fade out eventually.
- Keep any ambient modulation confined to dedicated, optional visual surfaces:
  normalized coefficient +/-0.08, derivative <=0.08/s in v2 prototype.
  These are UX constraints, *not* validated photic-health limits.
- Never adapt damage, aim, loot, healing, boss difficulty or rank from
  unvalidated neural features, and never penalize people with low SSVEP
  responsiveness.
- Respect reduced motion, high contrast, palette, photosensitivity and
  explicit disable options. No rhythmic flashes are generated by v2.
- Monitor frame time, attention shifts, early deaths, manual control
  interference, and self-reported distraction before considering more visual
  effects.

## Research design: does passive BCI *actually* improve enjoyment?

**Primary question:** Does opt-in quality-gated passive modulation improve
self-reported presence/enjoyment or reduce distraction relative to matched
ambient changes, without worsening gameplay?

Pre-register a within-player, counterbalanced 2x2 factorial pilot:

- Active BCI: off versus qualified Sight/Guard mode
- Passive scenery: off/static versus EEG-contingent
- Include a **yoked/sham** replay from the player's own prior session if feasible
  so the visuals are comparable but no longer causally coupled to current EEG.
- Blinding is partial: keep the visual amplitude and instructions matched;
  record the participant's perceived condition and expectations afterward.
- Same arena/seed/boss phase, counterbalanced order, logged display conditions
  and separate EEG calibration per session.

Primary: enjoyment, agency, perceived workload and distraction; define the
primary scale and minimum meaningful change **before** data collection.
Secondary: survival, hit rate, damage during gaze changes, time-to-selection,
selection accuracy, abstentions, artifact rate, FPS, latency, retention,
time spent looking at the orbs and voluntary replays. Reports should separate
mechanics benefits from novelty and task learning.

Use phase-matched and subject-held-out evaluation; do not split temporally
adjacent windows into train/test, and distinguish real human EEG from S0/S1
synthetic fixtures. No positive claim without confidence intervals, repeated
sessions, negative controls, and reasonable sensitivity analyses.

## What's implemented in this v2 PR

- `neuro/mindforge_neuro/combat_context.py`: pure, game-agnostic state policy
  with per-game placement hints and hard visual vetoes.
- `neuro/mindforge_neuro/passive_mod_policy.py`: opt-in, quality-gated
  spectral-feature -> decorative coefficient. No guessed mental states.
- `tests/test_combat_context.py` / `tests/test_passive_mod_policy.py`:
  synthetic deterministic regressions for danger, postcombat, source staleness,
  replay, opt-out, non-SSVEP independence and safety.
- `tools/mindforge_context_demo.py`: scenario JSONL to exercise policies
  without installing hooks or connecting a headset.

Run (from project root after `pip install -e '.[test]'`):

```bash
pytest -q tests/test_combat_context.py tests/test_passive_mod_policy.py
python tools/mindforge_context_demo.py --game hollow_knight
python tools/mindforge_context_demo.py --game silksong
python tools/mindforge_context_demo.py --game cuphead
```

## Next PRs and promotion

**V2.1: semantic hook instrumentation.** For each game, independently verify
real game-state events and camera projections using the game's appropriate
modding API, keep fixtures from named game/version, inspect localization and
boss variations. Never hardcode guessed character field names. Local native
compile + actual capture is required.

**V2.2: orb art/UX and runtime wire-up.** Convert policy hints into smooth,
non-interactive visual followers or edge HUD, with obstruction avoidance and
resolution-independent positioning; transmit separate context packets only
from validated source. Integrate into the existing portable BepInEx mod
behind feature flags. Capture actual gameplay, pause, focus and FPS.

**V2.3: passive ambient demo.** Reuse the exact `NeuralFeatureFrame v1`
contract from draft PR #64 once its branch is reviewed. Add source fan-out
and a game-specific decorative surface, then verify controller/combat marker
parity with passive on/off. Keep coded SSVEP-core visuals isolated.

**V2.4: qualified active target calibration.** Install actual coded stimuli
only when participant, source montage, renderer, game timing and physical
photodiode evidence support it. Separate hard fails: photosensitive display
constraints, game pause/time scale changes, high-frequency shaders, Unity
frame pacing, unsafe attack attention and response confusion.

**V2.5: opt-in playtesting.** Validate a strong no-BCI baseline, deterministic
faults, real EEG source fidelity, gaze cost, adverse experience reporting,
and repeated-play enjoyment. Do not ship claims based on a single successful
simulated run.
