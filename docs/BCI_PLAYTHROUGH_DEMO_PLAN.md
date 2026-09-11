# Mindforge BCI Playthrough Demo Plan

## Demo promise

The first production demo should be a truthful 8-12 minute third-person action-game playthrough in which ordinary controller input remains authoritative for movement, camera and melee combat while BCI contributes two visible tactical verbs:

- **Sight (10 Hz):** reveal or identify useful encounter information.
- **Guard (12 Hz):** create a temporary stabilization state used by a deliberately authored hazard or combat window.

The BCI should feel like part of the game rather than a diagnostic overlay pasted on top of it. It also must remain scientifically legible: controller simulation, synthetic EEG and live EEG are different evidence modes and must never be silently conflated.

## Non-negotiable authority boundaries

Dragon Souls remains authoritative for locomotion, rolling, aiming, target lock, sword combat, health, enemy AI, boss logic and inherited camera switching. Mindforge may present BCI targets, receive derived decoder events, gate semantic decisions, render information or stabilization effects, author non-destructive demo progression and tune presentation framing.

Raw EEG never enters Unity. A neural selection can affect the game only through the existing causal window -> neural intent bridge -> semantic intent bus -> receptor path.

## Playthrough arc

The V0.32 showcase already gives us the correct chapter skeleton. V0.33 should be integrated into it rather than inventing a second demo flow.

| Stage | Player experience | BCI role | Completion evidence |
| --- | --- | --- | --- |
| Awakening / Memory Forge | Establish world, movement and visual identity | Hidden | Stable camera and clean runtime |
| Blade Training | Learn movement, light/heavy attack, roll and target lock | Hidden | Ordinary combat remains intact |
| First Encounter | Defeat a normal enemy with controller combat | Hidden | Baseline gameplay readability |
| BCI Reveal | Introduce two neural targets and calibrate | Sight + Guard presentation | Matching decoder calibration acknowledgement |
| Sight Puzzle | A hidden/ambiguous route or object must be identified | Sight | Valid same-epoch Sight event visibly reveals the answer |
| Traversal | Move through the world with targets still available | Optional Sight | No camera/HUD obstruction while walking/running/rolling |
| Elite Encounter | A readable telegraphed hazard creates a tactical stabilization opportunity | Guard | Valid same-epoch Guard creates the intended safe/stable window |
| Boss Approach | Re-establish ordinary game control and raise stakes | Optional BCI | Decoder can abstain or disconnect without corrupting gameplay |
| Boss Fight | Finish with inherited combat; BCI adds tactical information/support but never becomes a hidden cheat | Sight / Guard optional | Boss remains defeatable and all authority boundaries hold |

## Delivery gates

### G0: native game and presentation foundation

**Goal:** make the game itself pleasant and trustworthy before asking EEG to do anything.

Acceptance criteria:

- Unity 2021.3.20f1 opens the materialized scene without unexpected Console errors.
- V0.31/V0.33 movement, camera, sword combat, targeting, rolling, healing and boss pipeline remain playable.
- Third-person FreeLook and Target-state framing keep the complete player readable during idle, walk, run, attack and roll at common 16:9 and 16:10 Game views.
- Aim and authored bonfire/cinematic framing remain inherited and are not rewritten by the BCI layer.
- BCI targets and developer HUD do not cover the central player silhouette or primary enemy read.

**Confidence:** high. This is entirely under repository/Unity control and should be qualified before further game design work.

### G1: deterministic native B0

**Goal:** prove the semantic game path independently of EEG and Python timing.

Acceptance criteria:

- F8 exercises Sight, Guard, timeout abstention and participant-pause termination through the production bridge/receptors.
- A clean exact-source checkout produces a validated `mindforge.bci_b0_receipt.v1` with `source_mode=simulated_decision`.
- The receipt explicitly records `eeg_observed=false` and `physical_display_timing_observed=false`.
- Native V0.33 audit has zero failures.

**Confidence:** high. The machinery already exists; the remaining gate is repeatable native observation on the repaired exact head.

### G2: synthetic EEG closed loop

**Goal:** prove the actual Unity -> marker -> LSL -> decoder -> derived event -> Unity loop without depending on human EEG quality.

Acceptance criteria:

- Python service connects using `source_mode=synthetic_eeg`.
- Unity baseline -> Sight -> Guard calibration completes only after a matching `CALIBRATION_READY` acknowledgement.
- At least one Sight and one Guard selection are accepted from the production decoder with the exact active `stimulus_epoch`.
- Wrong-epoch, stale, uncalibrated, artifact-marked, low-confidence and low-quality decisions are rejected.
- Window timeout produces abstention, never a forced class choice.
- UDP queues remain bounded under burst/fault rehearsal.
- JSONL evidence is derived-event only and contains no raw EEG.

**Confidence:** high after G0/G1. This is the first demo mode we should make recording-grade because it is deterministic enough to rehearse repeatedly while still exercising the real cross-process path.

### G3: unify V0.33 with the V0.32 showcase chapter

**Goal:** stop treating BCI integration and the playthrough as separate scenes.

Implementation target:

- Port the V0.33 integration owner into a V0.32-derived playthrough scene.
- Replace V0.32's legacy V0.31 orb reveal with the production two-target V0.33 presentation.
- Keep the existing nine route checkpoints and inherited player-to-boss NavMesh.
- Arm calibration at `BciReveal` rather than at scene start.
- Expose stage transitions to the BCI logger so every accepted/abstained decision has chapter context.

Acceptance criteria:

- One menu command builds and plays the complete demo scene from a clean checkout.
- Progression from Awakening through BossFight is possible without editor intervention.
- A decoder outage never freezes movement/combat or silently advances a BCI requirement.

**Confidence:** high. Both halves already exist in the repo and share the V0.31 base. The main risk is integration regression, not unknown research.

### G4: turn Sight and Guard into real game design

**Goal:** make each neural class valuable enough that a viewer immediately understands why it exists.

**Sight puzzle:** author one deterministic encounter-space ambiguity where the correct route, target, rune, weak point or interactable is obscured until Sight is accepted. Sight remains information-only and cannot move the player or damage an enemy.

**Guard encounter:** author one telegraphed neural hazard or elite attack with an explicit stabilization interface. Guard should affect only that authored hazard contract, not globally grant invulnerability or rewrite Dragon Souls damage. A missed/abstained Guard should have a fair ordinary-game consequence and recovery path.

Acceptance criteria:

- Sight changes what the player knows, not the underlying enemy authority.
- Guard changes one explicit Mindforge-authored hazard/stability variable, not global health/damage authority.
- Both effects are visible within ~250 ms of Unity accepting the semantic event, while decoder latency is logged separately.
- Neither command is required continuously. BCI is a sparse tactical channel, not a replacement joystick.

**Confidence:** high for the game mechanics. Whether live EEG can trigger them at an acceptable rate is a separate later gate.

### G5: recording-grade synthetic playthrough

**Goal:** create a demo we can confidently show even before live EEG is qualified.

Acceptance criteria:

- 8-12 minute start-to-boss route can be completed three consecutive times without restarting Unity.
- No unexpected red Console entries.
- No scene/editor intervention after pressing Play.
- Calibration, Sight puzzle and Guard encounter are understandable without reading the developer HUD.
- Optional diagnostic overlay can show source mode, active epoch, confidence/quality, calibration identity and accepted/abstained outcome.
- Presentation always labels `synthetic_eeg` when synthetic source is used. It never presents itself as human neural control.
- Session log plus B0/P-gate evidence are preserved alongside the recording commit SHA.

**Confidence:** high once G3/G4 are complete. This should be the first externally shareable milestone.

### G6: live EEG progression

**Goal:** substitute a real acquisition stream without changing game semantics.

Progression order:

1. Validate stream identity, units, channel metadata and sample cadence.
2. Stationary calibration outside combat.
3. Stationary Sight/Guard selections.
4. Selections while walking.
5. Selections while moving the camera.
6. Light enemy pressure.
7. Sight puzzle and Guard encounter.
8. Boss-context optional use.
9. External optical stimulus timing measurement on the actual display.

Acceptance criteria should be empirical, not assumed. We should record selection accuracy, abstention rate, decision latency, calibration time, artifact rejection, failures under movement and participant comfort before deciding whether live BCI is recording-grade.

**Confidence:** conditional on headset signal quality, participant physiology, display timing and motion artifacts. The architecture can be production-ready while these experimental measurements remain honestly unresolved.

## Camera composition contract

The V0.33 playthrough camera correction is intentionally narrow. It identifies player-targeted `FreeLook` and `TargetState` Cinemachine virtual cameras, widens their field of view to at least 55 degrees and pushes transposer follow distance to at least 5.6 m. It does not change Cinemachine priorities, the state-driven camera map, player targets, aiming, bonfire framing or gameplay transforms.

Native visual acceptance should include screenshots or a short capture of:

- idle and run in FreeLook;
- roll and sword combo in FreeLook;
- target lock around a normal enemy;
- target lock around the dragon/boss arena;
- aim mode, verifying that the inherited aim composition is unchanged;
- BCI targets visible without covering the character's head, hands or feet.

If 55 degrees / 5.6 m is still too tight on the native Game view, tune only these presentation constants from evidence. Do not compensate by changing the player model, camera targets or combat state machine.

## Definition of done for the first playthrough demo

The first demo is done when one exact Git commit can be materialized into a clean Dragon Souls checkout, opened in Unity 2021.3.20f1, and launched from a single Mindforge menu action into a complete route where the full character is readable, ordinary combat remains intact, V0.33 calibration occurs at the BCI reveal, a production-decoder Sight decision solves the Sight beat, a production-decoder Guard decision affects the authored Guard beat, abstention/offline behavior is safe, the player can reach and defeat the boss, and the session leaves enough provenance/log evidence to state exactly whether the source was simulated decisions, synthetic EEG or live EEG.

For the first recording-grade milestone, `synthetic_eeg` is sufficient and should be explicitly labeled. Live EEG becomes a later promotion of the same interface, not a prerequisite for proving that the game and BCI system form a coherent product demo.
