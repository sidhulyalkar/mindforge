# V0.33 BCI Integration Spine

V0.33 is the first Dragon Souls production slice whose BCI presentation, decoder transport, causal neural window, semantic intent bus and gameplay receptors are designed to run as one closed loop.

It deliberately starts from the native-qualified V0.31 game rather than the larger V0.32 showcase branch. The goal is to make the BCI seam independently testable before it is woven back into the full chapter flow.

## Production classes

V0.33 exposes exactly two production EEG targets:

| Semantic intent | Requested visual frequency | Decoder target |
| --- | ---: | --- |
| Sight | 10 Hz | `AuraTarget.SIGHT` |
| Guard | 12 Hz | `AuraTarget.GUARD` |

Concord remains part of the Mindforge game vocabulary, but V0.33 does **not** present it as a third EEG class. The Python decoder currently qualifies Sight and Guard only, so the game no longer advertises a neural command the production decoder cannot emit.

The requested frequencies are software presentation parameters, not claims about emitted optical frequency. `MindforgeDisplayTimingMonitorV33` observes Unity frame cadence only. Physical qualification still requires an external photodiode/high-speed-camera measurement on the target display.

## Closed-loop authority

The production path is:

```text
Unity Sight/Guard presentation
        |
        +--> UDP 19743 GameMarker: calibration/window epochs
        |
        v
EEG -> LSL -> Python SSVEP decoder
        |
        +--> derived NeuralEvent v1/v2 on UDP 19742
                         |
                         v
              MindforgeUdpNeuralReceiverV33
                         |
                         v
              MindforgeNeuralIntentBridgeV33
              confidence / quality / artifact
              calibration / causal epoch / refractory
                         |
                         v
                  MindforgeIntentBusV29
                    /              \
                   v                v
          Sight receptor       Guard receptor
```

Raw EEG never enters Unity.

`MindforgeNeuralIntentBridgeV33` is the only decoder-to-game semantic seam. It cannot move the player, swing the sword, write health, select an animation or alter enemy AI.

## Causal neural windows

Python only decodes gameplay authority after Unity emits `NEURAL_WINDOW_LISTENING` with a concrete `stimulus_epoch`.

The resulting `AURA_SELECTED` must carry the same epoch. V0.33 rejects selections when there is no active Unity listening window, the event belongs to another epoch, calibration is not ready, the artifact flag is set, confidence or signal quality is below the Unity semantic gate, evidence is too short, or semantic refractory time has not elapsed.

A timeout becomes `NEURAL_WINDOW_ABSTAINED`. Abstention is a valid outcome and never falls back to whichever class happened to score higher. Participant pause is also an authority boundary: pausing the temporal stimulus ends an active neural epoch before any later selection can become authoritative.

## Calibration handshake

The decoder runner supports the protocol used by V0.33:

```bash
python tools/run_unity_calibrated_decoder.py \
  --stream-name UnicornMock \
  --source-mode synthetic_eeg
```

For a live headset, use the correct LSL stream identity and `--source-mode live`.

Unity owns presentation timing and sends baseline, Sight and Guard labeled stages. Python fits the session profile and must return a matching `CALIBRATION_READY` event. Unity never invents successful calibration locally.

## Gameplay receptors

**Sight** reveals the nearest encounter actor with a temporary collider-free resonance marker. It is information-only: enemy AI, health, navigation and damage are untouched.

**Guard** creates a temporary collider-free stabilization field around the player. It exposes `GuardActive` for later neural-hazard mechanics but does not silently grant invulnerability or rewrite incoming damage.

These intentionally modest receptors prove that a neural decision can produce visible, semantically distinct consequences without stealing authority from the inherited action-game chassis.

## Exact-source native provenance

`tools/bootstrap_dragonsouls_chassis.sh` writes two records into the materialized Unity project:

- `.mindforge_chassis.json` identifies the pinned upstream Dragon Souls checkout and Unity version;
- `.mindforge_overlay.json` identifies the exact Mindforge Git commit used to apply the tracked overlay, whether that worktree was dirty, and whether the overlay was actually applied.

A dirty checkout is allowed for development, but it cannot produce promotable B0 evidence. This distinction is deliberate: **functional** means the native behavior worked; **PASS** means it worked from a sealed clean source identity.

## Native test scene

Materialize the branch into the local Dragon Souls checkout:

```bash
git fetch origin
git switch feat/v33-bci-integration-spine
git pull --ff-only origin feat/v33-bci-integration-spine
git status --short
bash tools/bootstrap_dragonsouls_chassis.sh
```

For promotion, `git status --short` should be empty before bootstrapping.

Open `external/DragonSouls-Unity3D/ThirdPersonCombat` with Unity `2021.3.20f1`, then run:

**Mindforge -> World V0.33 -> PLAY BCI INTEGRATION**

The developer HUD shows link, calibration, neural-window, source identity, B0 qualification state and software display-cadence state.

### B0: deterministic controller-only native qualification

No Python process and no EEG are required. Once the scene is running, press **F8** once.

`MindforgeBciQualificationHarnessV33` deterministically exercises the existing production semantic path rather than manipulating receptors directly:

1. open a causal window and inject controller-only Sight through `MindforgeNeuralIntentBridgeV33`;
2. require the Sight receptor to activate;
3. repeat for Guard and require the Guard field to become active;
4. open a window and require a real timeout abstention;
5. open another window, pause the participant stimulus, and require immediate `participant_paused` epoch termination;
6. write a JSON receipt beneath `Application.persistentDataPath/mindforge-bci`.

The HUD reports one of:

- `B0 UNRUN`: no receipt attempt yet;
- `B0 RUNNING`: deterministic sequence in progress;
- `B0 FUNCTIONAL`: behavior passed, but exact-source provenance was missing or dirty;
- `B0 PASS`: behavior passed and the overlay came from a clean sealed Git source;
- `B0 FAIL`: at least one functional requirement failed.

The receipt schema is `mindforge.bci_b0_receipt.v1`. It explicitly records `mode=controller_only`, `source_mode=simulated_decision`, `eeg_observed=false`, and `physical_display_timing_observed=false`, so B0 can never be mistaken for a neural or optical-timing result.

After the run, validate the receipt independently from the repository root:

```bash
python tools/validate_bci_b0_receipt.py \
  "/path/to/b0-native-YYYYMMDDTHHMMSSfffZ.json" \
  --expected-commit "$(git rev-parse HEAD)"
```

Then run **Mindforge -> World V0.33 -> Audit BCI Integration**. The audit reports B0 functional status, promotable-receipt status and clean-source provenance separately from synthetic/live EEG gates.

Manual `N -> 1`, `N -> 2`, timeout and `B` testing remains useful for visual inspection, but the F8 receipt is the repeatable B0 gate.

### B1/B2: synthetic closed loop

Run the synthetic LSL source used by the existing Mindforge phantom workflow, then:

```bash
python tools/run_unity_calibrated_decoder.py \
  --stream-name UnicornMock \
  --source-mode synthetic_eeg
```

In Unity, wait for the neural service and healthy software timing, press **C** to begin calibration, allow baseline -> Sight -> Guard to finish, wait for `CALIBRATED`, then open a neural window and drive the synthetic source toward Sight or Guard. Only an event from the active epoch may reach the semantic receptor.

The session JSONL path is printed to the Unity Console and stored beneath `Application.persistentDataPath/mindforge-bci`.

### Live participant progression

Do not jump directly to boss combat. Promote evidence in this order: stationary calibration, stationary Sight/Guard practice, walking, moving the camera, light enemy pressure, optional tactical use in a real encounter, boss integration, then external optical timing measurement.

## Participant comfort

The targets use reduced smooth luminance modulation and **B** immediately pauses temporal modulation while leaving the interface visible. Reduced contrast is not a safety guarantee. Participants should be warned that rhythmic visual stimulation can be uncomfortable and can provoke symptoms in photosensitive people, and testing should stop immediately if discomfort occurs.

## Promotion gate

V0.33 is ready to merge back into the showcase flow only when all of the following are observed on the same native branch head:

- V0.31 world/combat still loads and plays;
- exact clean overlay provenance is available;
- two production stimulus nodes exist at requested 10/12 Hz;
- V0.31 legacy 8/10/12 preview is suppressed;
- deterministic B0 Sight, Guard, abstention and participant-pause checks produce a validated receipt;
- UDP receiver stays bounded under event bursts;
- calibration uses a matching decoder acknowledgement;
- synthetic EEG can produce a causally valid Sight and Guard decision;
- stale, low-quality, artifact, wrong-epoch and uncalibrated selections are rejected;
- JSONL evidence is written with no raw EEG;
- native audit reports no failures.

Only after this gate should V0.33 be integrated into V0.32's `BciReveal -> SightPuzzle -> Traversal -> EliteEncounter -> BossApproach` chapter arc.
