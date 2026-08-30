# Persistent World V0.6

V0.6 turns Mindforge's V0.5 interaction/save seams into a reusable world substrate. The goal is not an uncontrolled infinite map. The authored Memory Forge → Synapse Causeway → Null Market → Fracture Court → Cathedral journey remains the narrative/combat spine. Procedural generation fills selected safe annexes and architectural negative space inside the existing Grounded World collision basin.

## One interaction language

`GuardianInteractionRouterV1` remains the only conventional context-input owner. World objects publish offers through `WorldInteractionSourceV1`; they never poll `E` themselves.

Priority remains deliberate:

- Memory Forge: 30
- persistent gate: 28
- Memory Conduit shortcut: 27
- shrine: 24
- loot: 22
- NPC dialogue: 18
- parked Prism hoverbike: 10

That ordering makes the required V0.5 ambiguity test deterministic: if a valid Memory Forge and parked bike overlap, the Forge wins. V0.6 content cannot steal that interaction merely because it is closer.

## One persistence language

`PlayerProfileSaveV06` writes `profile-v2.json` and is the sole active disk writer in a V0.6 showcase scene. The V0.5 component remains present but disabled so old `profile-v1.json` data can be migrated.

The V2 envelope contains:

1. player progression and quest reward receipts;
2. persistent inventory stacks;
3. equipped item bindings;
4. discovered regions;
5. durable `story.*` and `profile.*` semantic facts;
6. physical world records only for objects that implement `IWorldPersistentAdapterV06`.

This last rule is the important one. A semantic ledger fact cannot claim that a gate is open or loot is gone unless concrete scene code can restore that physical state. Encounter start/clear signals trigger boundary saves, but V0.6 does not pretend to support arbitrary mid-frame combat resume.

Pickups carry a receipt derived from their stable world ID. A receipt is persisted with inventory and reconciled when the pickup loads, so resting at the Forge and restarting cannot grant the same world reward twice.

## Stable world IDs

Meaningful persistent objects use authored IDs rather than scene-instance names or transient object references. Current slice examples:

- `memory_forge_market_loop`
- `forge.memory_shard.01`
- `cloister.aether_lens.01`
- `cloister.signal_shrine.01`
- `null_market.archivist.01`
- `neural_cloister`

Do not encode runtime coordinates into these IDs. Moving an authored object should not erase its history.

## Neural Cloister procedural annex

`ModularWorldAssemblerV06` builds a deterministic 3 × 5 annex at the eastern side of the current grounded basin. It uses a small socket grammar (`path` / `sealed`) with three height bands and constraint propagation to select compatible neighboring cells. One-step elevation differences receive conventional staircase geometry, preserving vertical exploration without making double-jump the only valid route.

The procedural solver is adapted from Maxim Gumin's `WaveFunctionCollapse` software under the MIT License. Mindforge keeps the required license notice in `unity/Assets/Mindforge/ThirdParty/Wfc/LICENSE.txt`. Upstream sample images and tile artwork are explicitly outside the upstream software license and are **not** copied into Mindforge. The generator consumes Mindforge-authored primitive geometry today and can later consume our own modular prefabs/material kits.

This is the intended acceleration pattern for public repositories: reuse well-licensed algorithms and architectural techniques, preserve attribution, and keep the game's art direction/data ours. Avoid wholesale scene or asset imports whose provenance is ambiguous.

## V0.5 runtime gate before promoting V0.6

Run the normal **Mindforge → Showcase → Build + Play Cinematic Showcase** command and verify all of the following in one play session:

1. **Single context prompt**: move through bike, Forge, shortcut, loot, shrine and NPC ranges. Never allow two contextual `E` prompts to be visible at once.
2. **Forge priority**: park a Prism hoverbike beside the Memory Forge inside both interaction radii. The visible offer must be `Reconstruct at Memory Forge`; `E` must reconstruct rather than mount.
3. **Target ownership**: press `T` around multiple valid enemies. Mouse wheel must cycle locked targets. Left/right arrows must orbit the camera and must never change the selected target.
4. **Tab truth**: open `Tab` during every quest stage. `CURRENT OBJECTIVE` must match `WorldQuestRuntime`; the V0.6 persistent panel must accurately reflect loot/equipment/region changes.
5. **Rest/restart idempotence**: claim `forge.memory_shard.01`, open the Memory Conduit, discover/commune with the Cloister shrine, then rest at the Forge. Restart the game. The shortcut and shrine must remain restored, the loot must remain absent, inventory must still contain exactly one shard reward, and no duplicate reward signal should be granted.
6. **Encounter boundaries**: enter and clear an encounter while watching profile status/logging. Start and clear boundaries must commit the profile without serializing arbitrary mid-encounter enemy transforms or health as durable truth.
7. **Generated annex safety**: traverse the Neural Cloister conventionally and with jump/double-jump/air-dash. No generated collider may create a route outside the Grounded World perimeter or expose a fall into the void.

Static regression coverage for these ownership/persistence rules lives in `tests/test_persistent_world_v06.py`. Runtime promotion still requires the playtest above because on-screen prompt singularity, physical placement, collider safety and restart reconstruction are scene behaviors rather than text contracts.

## Next content multiplication

Once the V0.5/V0.6 runtime gate is clean, richer production should mostly become content authoring rather than new architecture:

- replace primitive Cloister cells with a coherent Mindforge modular prefab kit;
- grow multiple seeded annex grammars for Forge/Causeway/Market/Cathedral visual identities;
- add loot tables and equipment definitions without changing receipt semantics;
- author more shrines and NPC dialogue graphs using the same `E` router;
- add persistent gates only through explicit restore adapters;
- introduce authored encounter-boundary restore adapters before attempting any deeper combat resume;
- use generated geometry for variation, while keeping major landmarks, quest beats, boss arenas and navigation guarantees authored.

The governing rule is deliberately boring and powerful: **one input owner, one stable identity, one semantic truth, one persistence architecture.**
