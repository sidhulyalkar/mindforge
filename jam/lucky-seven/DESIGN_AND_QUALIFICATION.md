# Lucky Seven: design, BCI scientific contract, and jam ship plan

## Pitch

**Seven little lives. Two ways of seeing. One lucky way home.**

We follow a flying squirrel through a moonlit canopy. Ordinary movement feels light, quick and forgiving: sprint, leap, tail-steer, glide, perch. But the forest has *two hidden maps*. Attending to a Wind visual target reveals moving air bridges; attending to a Root visual target reveals living bark routes. The player must use both to collect **seven** lucky seeds before the home nest opens. Neural choices happen at quiet perches; the demanding part is the route planning and execution after a choice.

This is a stylized *umwelt* game about sensing as an animal, not a claim to decode an animal's actual consciousness or read a human's feelings.

### Animal design laboratory

| Animal | Embodied movement | World interpretation | Candidate neural decision | Scope |
|---|---|---|---|---|
| Japanese dwarf flying squirrel | Sprint, branch vault, controlled glide, tail pitch | Wind lanes vs bark paths | Wind / Root | **Jam's one complete playable hero** |
| Newt | Walk, swim, cling, slip through roots | Water currents vs chemical trails | Flow / Trace | First expansion |
| Naked mole rat | Dig, turn, squeeze, communal push | Substrate vibration vs scent-route cues | Tunnel / Colony | First expansion |
| Jumping spider | Precision leaps, tethering | Near depth targets vs safe anchor routes | Leap map / Anchor | Future |
| Axolotl | Buoyant dive, fin movement | Water-flow disturbance vs shelter | Flow / Refuge | Future |
| Gecko | Wall grip, short pounces | Adhesion-safe surfaces vs refuge | Grip / Hide | Future |
| Star-nosed mole | Rapid tactile exploration | Contact landmarks vs tunnel vibration | Touch / Vibrate | Future |

Biological motifs are inspiration, not a claim of literal perception; ecological accuracy and species consultation belong in later design review. The two neural target *classes* stay fixed. Animal-specific vocabulary and environmental consequences change.

### Experience pillars

1. **Delight on contact:** silhouette and squash/stretch are readable at a glance; cute does not mean slow.
2. **Two worlds, one choice:** a sense switch changes actual geometry, not merely UI color.
3. **Neural patience, kinetic payoff:** ~1–3 seconds of measurement should initiate an extended sequence of action, not delay a reflex.
4. **Safe uncertainty:** decoder abstention never creates a guessed action or sudden instant death.
5. **Seven is the challenge:** seven collectibles, each tied to an intentional route or skill; collecting the seventh visibly changes the nest and ends the level.

## Sensor-to-game responsibility

```text
[EEG amplifier] -> [quality checks + calibration + evidence + intention decision]
                       |
                       v
         live NeuralEvent v2, Sight or Guard, TTL
                       |
                  localhost UDP
                       |
             LuckyBrainGate (native)
                       |
                Wind / Root latch
                       |
     [route collider + visual feedback, safe transitions]
```

- **Humans move the animal; neural attention selects a world-state.** It is not motor imagery and not a generic mental "focus score."
- Two visual SSVEP targets must be rendered with measured timings in a future hardware mode. This prototype intentionally **does not** generate unverified 10/12 Hz flashing elements.
- The implementation reuses the published **contract shape** of Mindforge NeuralEvent v2, but deliberately does not reuse any old boss fight, character, art or game scene.
- Only `source_mode=live` can grant sense in native strict mode. Replay, simulated decisions, abstention and loss cannot grant authority.
- The currently implemented checks are software-only: event type, source mode, session/calibration continuity, sequence, packet freshness, quality, artifact flag and evidence. They do **not** prove that source EEG was genuine, that gaze was independent, or that stimulus frames were accurately displayed.
- Real acquisition needs display refresh/phase measurement, independent EOG/gaze/EMG evidence, cable artifact controls, local-only socket security, false-accept/false-reject benchmarks and per-person calibration. Any source physically indistinguishable from an intentional SSVEP remains an epistemic limit.

### Accessible judging versus live scientific demo

A jam judge must be able to play on itch.io without installing extra software or owning an EEG headset. Therefore **WebGL defaults to a clearly labelled keyboard simulation of neural choices.** It tests gameplay, not BCI. The separate native hardware path can require real decisions and disclose its evidence level. These modes are not interchangeable in scientific reporting.

### Neuroscience qualification matrix (not yet passed)

| Gate | Test | Pass condition |
|---|---|---|
| Software | duplicate/out-of-order events | rejected, no double selection |
| Software | simulated/replay/manual packets in live | rejected |
| Software | unknown session/calibration | rejected |
| Software | delayed packet past TTL | rejected |
| Software | artifact, low quality, absent evidence | rejected |
| Engine | Unity 2022.3 import, scene, physics | no compile errors, no inaccessible seeds |
| Engine | WebGL on actual itch page | loads and remains controllable in supported desktop browsers |
| Human | 2-choice calibration with physical EEG | measured and reported, no inferred pass |
| Human | eyes vs controller artifact and display interference | independent controls/negative cases |
| Human | perching/walking/gliding | completion, selection error, latency, fatigue measured |
| Safety | photosensitive stimuli | no coded visual flash without opt-in and measured rendering |

For a real study log: participant pseudonym, headset montage, display refresh, target frequency/phase, sample rate, decoder version, calibration and signal quality, decision source mode, trial timestamps, selection confusion matrix, elapsed time, number of accidental switches, and opt-out/fatigue events. Do not upload raw EEG or personal data in a public jam build.

### Prototype scoring

- **Completion:** seven seeds and return to nest; show seed count.
- **Game feel:** median time to understand controls; death/respawn friction; missed ledge and unintended glide rates; camera framing review.
- **BCI meaningfulness:** number of required correct neural selections and ability to play strict mode without keyboard commands, under qualified live sessions only.
- **Accessibility:** reduced motion option, flicker-free jam mode, readable contrast, gamepad mapping, no penalty for an abstained neural choice.
- **Ablation:** compare a manually selected route, a replayed route (explicitly not EEG), and qualified live EEG to quantify extra latency/frustration and enjoyment.

## Jam execution

| Date (Pacific) | Deliverable | Hard gate |
|---|---|---|
| Nov 6 | Project created, squirrel greybox, seven seeds, two sense bridges | Unity import and play |
| Nov 7 | Squirrel feel, gliding, landing, camera, safe perch UX | all seven reachable; no bypass |
| Nov 8 | Art silhouette, tree layers, effects, minimal instruction | coherent 15-second first impression |
| Nov 9 | Better Wind/Root route puzzles, deterministic seed and respawn logic | playtest 3 complete runs |
| Nov 10 | WebGL browser build and small screen layout | run on itch-style iframe |
| Nov 11 | Sound, pause, credits, tutorial, reduced-motion options | accessibility check |
| Nov 12 | Freeze features, hardening and regression | green manual/automated matrix |
| Nov 13 | Package and upload, provenance, AI-use disclosure | link can be played without installer |
| Nov 14 at 4 AM PST | Submission closes | no post-deadline changes |

**Scope cuts if necessary:** next animals, voiced narration, procedural worlds, randomized levels, scoreboards, passive affect inference and imagined speech. A single lovely squirrel level with one meaningful neural affordance is a stronger jam entry than seven half-built species.

## Documentation and asset integrity

- Any reused infrastructure must be acknowledged as pre-existing and eligible under the official contest rules. The game itself, its scene and design must originate during the jam.
- All third-party assets must have distributable rights and attribution. No proprietary game sprites/sounds.
- Disclose any AI assistance to code, art or writing in the itch.io submission (mandatory for prize eligibility under the jam page).
- The submitted browser game must not depend on a sidecar service, external website, or network-delivered updates during judging.
