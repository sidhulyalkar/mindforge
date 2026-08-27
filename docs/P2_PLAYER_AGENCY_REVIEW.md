# P2 Player-Agency Review

Mindforge's architecture is now stronger than its observed player experience. That is a good problem to have only if we stop expanding architecture and use it to improve the game.

The competition thesis remains:

> **Hands own precision. The brain owns transformation.**

P2 exists to prove the first half before EEG is allowed to help with the second.

## What is already unusually strong

Mindforge has a narrow neural authority boundary, abstention and stale-data behavior, source provenance, calibration identity, bounded transport, semantic Unity-to-tools telemetry, deterministic conventional-input replay, explicit controller-only qualification, neurOS fault/simulation hooks, and a promotion ladder that refuses to convert software tests into physical or human claims.

That technical rigor is an advantage. It is not the product experience.

## Hard critique

### 1. The original control scheme contradicted the pitch

The Guardian previously derived every attack aim vector from the boss `aimTarget`. That meant the player controlled movement and timing, but not truly precision. It also made the phase-two/three Fractured Echo priority targets difficult to address intentionally.

`feat/p2-player-agency-v1` changes boss lock to a fallback. Mouse movement now activates world-space pointer aim and arrow keys provide a keyboard-only directional aim path. The resolved aim vector remains inside `GuardianCommandFrame`, so record/replay keeps the same authority model.

### 2. We have not yet observed whether the combat is fun

The repository can count shots, cleaves, counters, near misses, damage, Signal Breaks, Bloom, Twin Eclipse, Flux, neural payoff and degradation. None of those metrics can prove enjoyment.

P2 therefore keeps machine telemetry and human report separate. `mindforge_playtest.py --prompt-review` may write `review.json` with explicit human-reported clarity, responsiveness, enjoyment, tactical comprehension and free-text observations. Those fields never participate in automatic P2 pass/fail.

### 3. The boss has tactical ideas that were not fully observable

Fractured Echoes are intended to force target-priority decisions, but their lifecycle was absent from GameMarker evidence. P2 now records `ECHO_SPAWNED` and `ECHO_SHATTERED`, reports a shatter rate, and flags a completed encounter that spawned Echoes but shattered none.

This does not say that every Echo should be killed. It tells us when the intended tactical layer may be invisible, inaccessible or strategically irrelevant.

### 4. Judge comprehension was too dependent on explanation outside the game

The evidence HUD exposes useful decoder/transport facts, but a jury member should not need to reverse-engineer why those numbers matter.

The optional **F10 Judge Lens** states the authority split directly:

- hands move, aim, fire, cleave, counter and dash;
- BCI selects Sight offense or Guard recovery;
- EEG never moves, aims, fires, dodges or parries.

The same guide exposes the controls during the opening combat window and gives precision aim a visible reticle. It is presentation-only and cannot invoke combat actions or neural authority.

### 5. The visual slice is still a qualification scene, not a finished competition world

`CompetitionSceneAssembler` deliberately builds from primitive geometry. That is excellent for detecting broken references and terrible as evidence that the final game has production-level visual identity.

Do not confuse a robust generated scene with final art direction.

### 6. Combat cadence is still hand-authored theory

The current Fractured Signal cadence has explicit phase intervals and telegraph durations, but those values have not yet been justified by repeated human play. The numbers should be treated as hypotheses until P2 says otherwise.

In particular, watch for:

- phase-three visual overload;
- telegraphs that are readable in code but not at normal play speed;
- Counter Pulse windows that feel arbitrary rather than learnable;
- Signal Break rest periods that interrupt rhythm rather than reward mastery;
- Echo pressure that becomes clutter instead of a tactical choice.

### 7. Calibration is scientifically cleaner than its UX is proven

The baseline → Sight → Guard sequence is deliberate and defensible, but a scientifically sensible 15-second protocol can still feel long, confusing or anticlimactic in a game opening. That question belongs to later neural/human gates, not controller-only P2, but it should remain visible.

### 8. Physical presentation remains unqualified

The software can request 10/12 Hz and expose photodiode instrumentation. That is not proof of measured physical luminance timing, visual comfort, Unicorn signal quality or human decoder performance.

Those claims remain downstream gates.

## P2 playtest protocol

Run P1 first on the exact candidate head using the pinned Unity editor. Do not use Python CI as a substitute.

For each controller-only P2 run:

```bash
python tools/mindforge_playtest.py --require-terminal --prompt-review
```

Start the generated competition scene and enter explicit controller-only mode with **F8** in the Editor or the supported development-build flag. The run must visibly declare `P2 CONTROLLER-ONLY · BCI DISABLED` and emit `QUALIFICATION_MODE / CONTROLLER_ONLY_NO_BCI`.

During the encounter, do not optimize for a predetermined metric. Observe whether the player naturally discovers and uses the systems.

Afterward, inspect together:

- `markers.jsonl` for the raw semantic trace;
- `encounter.json` for machine-derived encounter facts;
- `capture.json` for exact session/Git/provenance identity;
- optional `review.json` for human-reported experience.

## Questions each P2 run should answer

1. Can the player move and deliberately aim at something other than the boss?
2. Can the player intentionally target an Echo when it appears?
3. Does Counter Pulse become understandable through play rather than memorization?
4. Are near misses exciting or merely accidental?
5. Does Rift Cleave have a readable purpose relative to Pulse Shot?
6. Does Gravity Bloom feel earned at full Flux?
7. Can the player explain what Sight and Guard would change without believing EEG controls locomotion or aim?
8. Does the boss escalate in texture, not merely projectile density?
9. Is there at least one moment the player wants to describe afterward?
10. What single change would most improve the next run?

## What not to build yet

Until repeated P2 runs are informative and the P1 Unity gate is observed on the exact candidate head, do **not** add:

- a third neural class;
- motor-imagery movement;
- P300 menus;
- emotion recognition;
- foundation-model inference in the control loop;
- multiplayer;
- VR-specific dependencies;
- a second boss;
- a large progression tree.

Those features increase surface area before the core loop has earned it.

## Promotion mindset

A good P2 result is not `VICTORY` by itself. A useful P2 result is a reproducible run where we can explain what the player tried, what the game rewarded, what they understood, where they struggled, and which concrete change should be tested next.

Mindforge should win because its BCI authority is trustworthy **and** because the underlying game is worth giving that authority to.
