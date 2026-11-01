# Mindforge Living World v3: passive-only neural atmosphere

## Delivered slice

A functioning *optional* Unity demo environment plus a versioned Python-to-Unity **passive-only** feature channel. The first demonstrated feature is **posterior alpha relative spectral power** (8–13 Hz / 4–30 Hz) measured on posterior scalp electrodes. It is **not** validated as focus, calm, emotion, or mental imagery, and the synthetic demo does not contain human EEG.

No existing Unity scene, SSVEP classifier, Sight/Guard neural authority, character controller, combat, or VEP-core stimulus is modified.

## Architecture

    Hardware / offline EEG / deterministic synthetic derived features
                     |
        Python feature extraction & signal-quality gating
                     |
          NeuralFeatureFrame v1 (derived scalar, not raw EEG)
                     |
            UDP 127.0.0.1:19747 (not authority port 19742)
                     |
            Unity LivingWorldPassiveReceiver
                     |
          PassiveFeaturePolicy (quality, seq, TTL, session)
                     |
       slow low-amplitude color on dedicated decorative meshes

Source contract: `contracts/neural_feature_frame.v1.schema.json`.
Producer: `neuro/mindforge_neuro/passive_features.py`.
CLI: `tools/mindforge_living_world.py`.
Unity: `Assets/Mindforge/LivingWorld` and editor demo scene builder.

Safety invariants: loopback-only sockets, separate passive UDP port, no EEG samples or game actions on the wire, one declared feature, mandatory provenance, bounded queue, age-limited authority to *render only*, quality/artifact abstention, monotonic per-producer packet sequence, TTL, smooth baseline fade on stale/loss and color changes rate-limited to at most 0.10 normalized units/s. This number is a **visual design clamp**, not a clinical safety threshold.

A new artifact or low-quality frame withdraws the current passive target without inventing a substitute neural state. A new source session cannot steal a still-active stream. Producer timestamps are provenance only, **never subtracted from Unity process clock**. Coded SSVEP evidence and calibration windows suppress each dedicated decorative renderer entirely.

## Try in Unity (requires actual Editor; Python CI does not compile Unity)

1. Open the repo's `unity/` project with pinned Unity **2022.3.62f3**.
2. Menu: **Mindforge → Living World → Create Passive Demo Scene**.
3. Save/open the generated `Assets/Mindforge/Scenes/LivingWorldPassiveDemo.unity` scene and press Play.
4. In another terminal from repo root:

```bash
python -m pip install -e '.[test]'
python tools/mindforge_living_world.py --demo --seconds 40 --hz 5 \
  --output-tape experiments/tapes/living-world-demo.jsonl
```

The standalone demo contains static ground and fourteen dedicated unlit non-colliding shapes. Their hue varies gently, and the HUD displays source provenance, filtered feature value, accepted/rejected counts, and disconnection status. The objects are standalone lab visuals, **not** a finished art integration.

To replay the same derived stimulus trace at the same nominal speed:

```bash
python tools/mindforge_living_world.py --replay experiments/tapes/living-world-demo.jsonl
```

Replayed records always say `feature_replay`, never `live`. Stop the producer to test stale-signal fallback: the ratio and colors should fade back to neutral. There is no global lighting/fog change, no camera effect, no music integration and no active combat or world-building modification yet. The first iteration prioritizes an observable, safe, testable passive lane.

To inspect the protocol without sending UDP, use:

```bash
python tools/mindforge_living_world.py --demo --seconds 2 --dry-run \
  --output-tape experiments/tapes/living-world-dry-run.jsonl
python -m pytest -q tests/test_passive_living_world.py
```

## Qualify before using with human EEG

1. Compare measured monitor luminance/SSVEP accuracy with/without passive visuals. No assumptions about flicker or photic safety based on code alone.
2. Run native Unity Editor tests in `LivingWorldPassivePolicyTests.cs`; confirm the generated scene compiles in pinned Unity and that cycling through opening/closing the scene does not leak UDP sockets.
3. Verify real EEG units, montage, acquisition rate, artifact rates and alpha ratio distribution in independent sessions. A proper posterior montage is required; a minimum of two known posterior sensors is enforced.
4. Add a user-facing opt-in and accessibility control, photosensitivity settings, smooth intensity limits, opt-out reset, and optional reduced-motion palette before installing this into the actual competition encounter.
5. Verify no gameplay marker/collision/health/timing differences on a deterministic controller+neural tape with passive enabled vs disabled.
6. Keep EEG+EMG/gaze artifact and cue-constrained interpretation. Do not report this alpha ratio as mood or attention without a separate validation study.

**Release statement:** Python regression CI passing is a P0 software gate only. The Unity compile, physical display, real human neural effects, and full combat parity gates must be observed separately. PR is intentionally a **stacked draft** on v2 until predecessor branches merge.

## Next iteration

Add a quality-controlled prerecorded-EEG replay adapter and telemetry comparing passive-on/off render performance; then optionally integrate into dedicated ambient material surfaces in the real scene. V4 imagined-speech study stays offline until independently validated.
