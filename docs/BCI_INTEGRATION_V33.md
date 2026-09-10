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

Python only decodes gameplay authority after Unity emits:

`NEURAL_WINDOW_LISTENING`

with a concrete `stimulus_epoch`.

The resulting `AURA_SELECTED` must carry the same epoch. V0.33 rejects selections when:

- there is no active Unity listening window;
- the event belongs to another epoch;
- calibration is not ready;
- the artifact flag is set;
- confidence or signal quality is below the Unity semantic gate;
- evidence is shorter than the minimum gate when evidence duration is available;
- semantic refractory time has not elapsed.

A timeout becomes `NEURAL_WINDOW_ABSTAINED`. Abstention is a valid outcome and never falls back to whichever class happened to score higher.

## Calibration handshake

The decoder runner already supports the protocol used by V0.33:

```bash
python tools/run_unity_calibrated_decoder.py \
  --stream-name UnicornMock \
  --source-mode synthetic_eeg
```

For a live headset, use the correct LSL stream identity and `--source-mode live`.

Unity owns presentation timing and sends three labeled stages:

1. baseline, 4 s;
2. Sight, 5 s;
3. Guard, 5 s.

During Sight/Guard calibration both production targets remain available while the requested target is emphasized. Python fits the session profile and must return a matching `CALIBRATION_READY` event. Unity never invents successful calibration locally.

## Gameplay receptors

### Sight

A successful Sight intent reveals the nearest encounter actor with a temporary collider-free resonance marker. It is intentionally information-only in V0.33: enemy AI, health, navigation and damage are untouched.

### Guard

A successful Guard intent creates a temporary collider-free stabilization field around the player. It exposes `GuardActive` for later neural-hazard mechanics but V0.33 does not silently grant invulnerability or rewrite incoming damage.

These are deliberately modest first receptors. They prove that a neural decision produces visible, semantically distinct game consequences without stealing authority from the inherited action-game chassis.

## Native test scene

Materialize the branch into the local Dragon Souls checkout:

```bash
git fetch origin
git switch feat/v33-bci-integration-spine
git pull --ff-only origin feat/v33-bci-integration-spine
bash tools/bootstrap_dragonsouls_chassis.sh
```

Open:

`external/DragonSouls-Unity3D/ThirdPersonCombat`

with Unity `2021.3.20f1`.

Run:

**Mindforge -> World V0.33 -> PLAY BCI INTEGRATION**

The developer HUD shows link, calibration, neural-window and display-cadence state.

### B0: game-only semantic qualification

No Python process is required.

1. press **N** to open a development neural window;
2. press **1** to inject Sight through `MindforgeNeuralIntentBridgeV33`;
3. verify a nearby enemy receives the Sight reveal marker;
4. wait for the short refractory;
5. press **N**, then **2**;
6. verify the player receives the Guard stabilization ring;
7. press **N** and make no selection; verify it times out as abstention;
8. run **Mindforge -> World V0.33 -> Audit BCI Integration**.

This proves gameplay semantics and the causal intent seam without making an EEG claim.

### B1/B2: synthetic closed loop

Run the synthetic LSL source used by the existing Mindforge phantom workflow, then:

```bash
python tools/run_unity_calibrated_decoder.py \
  --stream-name UnicornMock \
  --source-mode synthetic_eeg
```

In Unity:

1. wait for the HUD to report the neural service;
2. wait for software timing to become healthy;
3. press **C** to begin calibration;
4. allow baseline -> Sight -> Guard to finish without pausing the stimulus;
5. wait for `CALIBRATED`;
6. press **N** to open a production neural window;
7. drive the Phantom source toward Sight or Guard;
8. verify that only an event from the active epoch reaches the semantic receptor.

The session JSONL path is printed to the Unity Console and stored beneath `Application.persistentDataPath/mindforge-bci`.

### Live participant progression

Do not jump directly to boss combat. Promote evidence in this order:

1. stationary calibration;
2. stationary Sight/Guard practice;
3. selection while walking;
4. selection while moving the camera;
5. selection under light enemy pressure;
6. optional tactical use in a real encounter;
7. boss integration;
8. external optical timing measurement.

## Participant comfort

The targets use reduced smooth luminance modulation and **B** immediately pauses temporal modulation while leaving the interface visible. Reduced contrast is not a safety guarantee. Participants should be warned that rhythmic visual stimulation can be uncomfortable and can provoke symptoms in photosensitive people, and testing should stop immediately if discomfort occurs.

## Promotion gate

V0.33 is ready to merge back into the showcase flow only when all of the following are observed on the same native branch head:

- V0.31 world/combat still loads and plays;
- two production stimulus nodes exist at requested 10/12 Hz;
- V0.31 legacy 8/10/12 preview is suppressed;
- B0 Sight, Guard and abstention behavior all work;
- UDP receiver stays bounded under event bursts;
- calibration uses a matching decoder acknowledgement;
- synthetic EEG can produce a causally valid Sight and Guard decision;
- stale, low-quality, artifact, wrong-epoch and uncalibrated selections are rejected;
- JSONL evidence is written with no raw EEG;
- native audit reports no failures.

Only after this gate should V0.33 be integrated into V0.32's `BciReveal -> SightPuzzle -> Traversal -> EliteEncounter -> BossApproach` chapter arc.
