# Game Foundation V1

Game Foundation V1 turns the Hackathon playthrough into a reusable large-game seam without replacing any existing physical authority.

## Runtime chain

`concrete gameplay → WorldSignalBus → WorldStateLedger → WorldQuestRuntime → WorldQuestRewardRuntime → PlayerProgressionLedger`

Passive consumers:

- `WorldSignalTelemetryAdapter`
- `CompetitiveRunObserverV1`
- `GameFoundationHudV1`
- future save/story/spectator services

## Concrete authority remains unchanged

Foundation V1 must not originate:

- Guardian movement, jump, roll or air dash;
- attacks, parries or damage;
- enemy attacks or movement;
- Menagerie wave activation;
- Null Ward zone activation;
- boss scheduling;
- gates or checkpoint reconstruction;
- BCI evidence, acceptance or stimulus timing.

Those responsibilities remain with their existing controllers/directors.

## World facts

Representative durable facts:

- `journey.stage`
- `region.arrival.entered`
- `region.causeway.entered`
- `region.brokenmomentum.entered`
- `region.ruinedchoir.entered`
- `region.gravitas.entered`
- `region.crucible.entered`
- `encounter.menagerie.wave`
- `encounter.menagerie.waves_cleared`
- `encounter.menagerie.complete`
- `world.null_ward.protocol_open`
- `boss.malatract.started`
- `world.null_ward.complete`
- `checkpoint.memory_forge.active`
- `story.<id>.discovered`

Region entry is prefix-monotonic. If a stage jump reaches `RuinedChoir`, all earlier regions are recorded as entered too.

## Ordered quests

### Read the Fractured City

1. Cross the Neon Causeway.
2. Enter the Market of Broken Momentum.
3. Reach the Choir of Ruined Towers.

Rewards:

- 10 Resonance
- `codex.aetheria_regions`

### The Menagerie Exam

Prerequisite: Read the Fractured City.

1. Cross the Hall of Excessive Gravitas.
2. Enter the Menagerie Crucible.
3. Clear wave 1.
4. Clear wave 2.
5. Complete the 3 / 4 / 3 exam.

Rewards:

- 30 Resonance
- 1 Mastery
- `challenge.menagerie_replay`

### Reconnect the Null Ward

Prerequisite: The Menagerie Exam.

1. Open the Protocol Veil.
2. Confront Lord Malatract.
3. Complete the Null Ward.

Rewards:

- 60 Resonance
- 2 Mastery
- `region.aetheria_frontier`

## Reward idempotence

`PlayerProgressionLedger` stores durable reward receipts by quest id.

`WorldQuestRewardRuntime` must claim the receipt before applying rewards. Reconciliation after a progression snapshot restore therefore cannot duplicate currency or unlock grants.

## Snapshot semantics

`WorldStateLedger.CaptureSnapshot()` returns a sorted memory-only world-state snapshot.

`RestoreSnapshot(...)` replaces semantic facts and emits one `SnapshotRestored` event so derived systems re-evaluate. Optional per-key signals remain separate.

`PlayerProgressionLedger` independently snapshots:

- Resonance;
- Mastery;
- unlock ids;
- reward receipts.

Foundation V1 deliberately does not write save files yet. A future save coordinator must restore the concrete encounter/checkpoint/equipment authorities as well as semantic state.

## Story discoveries

Six proximity-only, collider-free discovery beacons are authored:

- Prism Bastion
- Neon Causeway
- Market of Broken Momentum
- Choir of Ruined Towers
- Hall of Excessive Gravitas
- Menagerie Crucible

Each stores `story.<id>.discovered` and publishes one `StoryDiscovered` signal. Snapshot restore reconciles beacon state.

No story beacon samples input or alters gameplay.

## Encounter contracts

`EncounterContractRegistry` describes encounters for tuning, replay and spectator services.

Menagerie and Lord Malatract are marked `competitive_candidate = true` but `ranked_eligible = false`.

That is deliberate. Source architecture cannot qualify a ranked build.

## Competitive observer

`CompetitiveRunObserverV1` records passive realtime splits for:

- region entry;
- wave clear;
- encounter clear;
- boss start;
- world completion.

It publishes `RunSplit` semantic signals but ignores its own split signals to avoid recursion.

It does not modify the run.

## HUD

`GameFoundationHudV1` displays:

- primary active quest;
- current quest step;
- Resonance;
- Mastery;
- passive run elapsed time;
- short world-memory / quest-complete pulses.

It is presentation-only and may be removed or restyled without changing gameplay state.

## One-click build order

`GameFoundationV1Builder.ApplyOpenScene()` runs only after:

- world topology;
- enemy population;
- enemy presentation;
- Aetheria world passes;
- Hackathon playthrough;
- mount safety;
- visual infrastructure;
- set dressing;
- traversal playability.

Foundation therefore binds to the final authored world rather than references that a later builder might replace.

## Unity acceptance checklist

After pulling the qualified main SHA:

1. Open Unity 2022.3.62f3.
2. Run `Mindforge → Showcase → Build + Play Cinematic Showcase`.
3. Confirm Console has no compile/import exceptions.
4. Confirm exactly one `Mindforge_GameFoundation_V1` exists.
5. Walk from Prism Bastion through Causeway, Market and Choir; verify the first quest completes once.
6. Continue through Gravitas and the Menagerie; verify wave 1 and wave 2 advance the quest before final completion.
7. Verify Menagerie reward total adds exactly 30 Resonance + 1 Mastery once.
8. Rest / reconstruct and repeat encounter lifecycle; reward totals must not duplicate.
9. Complete the Null Ward / Lord Malatract path and verify the third quest.
10. Verify story-memory pulses do not obscure threat telegraphs or coded VEP targets.
11. Verify the HUD remains readable at the intended game resolution.
12. Inspect run splits through telemetry/logging if enabled.
13. Profile CPU/GPU frame time with Foundation enabled.
14. Re-run physical VEP timing/salience qualification separately before making BCI timing claims.
