# Mindforge V0.5 — Interaction, Save Contract, and UX Clarity

Status: implementation contract for `feat/ux-interaction-save-v05`.

## Product goal

V0.5 makes the existing vertical slice easier to understand without weakening player agency or scattering new input authority through the scene.

The player-facing control vocabulary is intentionally small:

- WASD — move
- mouse / arrow keys — camera
- Space — jump, double-jump, hold hover while descending
- Shift / RMB — ground dodge, air dash, mounted boost
- F / LMB — Aetherblade
- T — lock / unlock target
- mouse wheel — cycle targets while locked
- E — contextual interaction
- Q — Rift Cleave
- C — Counter Pulse
- R — Gravity Bloom / Twin Eclipse
- Tab — objective, kit, and full control reference
- F10 — judge / authority lens

Compatibility dodge aliases may remain internally but are not part of the advertised mental model.

## 1. One control source of truth

`GuardianControlProfileV1` owns the conventional default bindings and human-readable labels used by gameplay samplers and presentation.

Gameplay components must not independently hard-code the advertised bindings. A future rebinding/settings layer should mutate or replace the profile rather than edit tutorial strings and input scripts separately.

This does not move gameplay authority into the profile. It only describes conventional input bindings.

## 2. Contextual E

`GuardianInteractionRouterV1` owns the explicit contextual-interaction edge and the single interaction prompt.

The router may:

- discover valid offers;
- rank them by authored priority, distance, and view direction;
- show one prompt;
- forward the accepted E edge to the selected concrete source;
- publish a downstream semantic `interaction.performed` signal after success.

The router may not:

- reconstruct checkpoints itself;
- move the Guardian;
- change encounter state;
- open arbitrary gates;
- grant rewards;
- originate neural state.

Concrete interaction sources remain authoritative for their own actions.

Current adapters:

- `MemoryForgeInteractionV1` delegates to `MemoryForgeCheckpoint`;
- hoverbike offers delegate to `GuardianHoverbikeController`.

Future doors, NPCs, loot, shrines, elevators, and challenge terminals should implement the same source contract instead of sampling E themselves.

## 3. Interaction priority

Mounted dismount is always the active mounted context.

While on foot, all offers participate in the same ranking system. Authored priority dominates distance/view angle. The Memory Forge outranks a nearby parked hoverbike, preventing vehicles from stealing important world interactions when both are valid.

No source should rely on colliders merely to become interactable. Collider use remains a physical-world design decision.

## 4. Target-lock clarity

Arrow keys remain camera controls.

Target lock uses:

- T to acquire/release;
- mouse wheel to cycle while locked.

No advertised key should simultaneously mean camera orbit and target cycling.

## 5. Replay schema V5

`GuardianInputTape` V5 adds `context_down`.

Schemas V1–V4 remain loadable.

Legacy `mount_toggle_down` is not globally reinterpreted as contextual interaction. During pre-V5 replay it may satisfy only a focused hoverbike mount/dismount offer. This prevents an old E press from acquiring a newly-authored shrine, NPC, or checkpoint meaning.

Replay remains fail-neutral after exhaustion and fixed-tick/idempotent across input consumers.

## 6. Progressive onboarding

The opening guide should teach one layer at a time:

1. movement, camera, jump, blade;
2. evade and target lock;
3. contextual E when the player reaches an offer;
4. Q/C/R after the core rhythm is understood.

Context prompts outrank generic tutorial prose in the same screen region.

Tab is the durable recovery surface for a player who forgets a control or objective.

## 7. Safe persistent profile

`PlayerProfileSaveV05` intentionally persists only state that can be reconstructed honestly today:

- Resonance;
- Mastery;
- semantic unlocks;
- idempotent reward receipts;
- `story.*` discoveries;
- explicitly non-physical `profile.*` facts.

It intentionally excludes physical world truth such as:

- active / completed encounter state;
- boss phase or completion;
- current enemy health / activation;
- checkpoint physical reconstruction state;
- doors, shortcuts, or regional geometry that lack explicit restore adapters.

A semantic fact must not claim the world has resumed a state that its concrete authority cannot reconstruct.

## 8. Save write safety

Profile saving uses a temporary write followed by replace-with-backup semantics when supported, with a bounded fallback. The previous valid profile should not be deleted before a replacement exists.

Reward receipts make quest reward reconciliation idempotent after loading.

Story beacons observe their own restored state keys so a loaded discovery cannot fire again merely because the player walks near its beacon.

## 9. BCI boundary

Nothing in V0.5 changes the BCI authority contract.

Raw EEG never enters Unity. The control profile, interaction router, profile save, quest help UI, and interaction sources do not read neural evidence or generate accepted neural state.

Hands still own movement, camera, target selection, jump, dodge, attack, parry, mount steering, and interaction confirmation.

## 10. Full-world save prerequisite

The next persistence tranche must introduce explicit restore adapters for concrete physical authorities before full campaign resume is enabled.

A physical restore contract should declare at minimum:

- stable authority id;
- schema version;
- capture payload;
- restore order;
- dependency ids;
- invalid / stale content behavior;
- reset fallback;
- deterministic reconstruction requirements where relevant.

Priority adapters:

1. checkpoint / Guardian transform and owned combat windows;
2. Menagerie encounter director + individual enemy lifecycle state;
3. Null Ward encounter progression;
4. Lord Malatract boss phase / scheduler state;
5. world gates, doors, shortcuts, and one-shot pickups.

Until those exist, profile persistence remains deliberately narrower than world persistence.

## 11. Qualification gates

Source qualification must protect these invariants:

- one canonical advertised control profile;
- gameplay consumers use the profile rather than duplicate bindings;
- E is sampled by the context router, not individual interaction sources;
- Memory Forge and hoverbike retain physical authority;
- target cycling does not share arrow-key camera bindings;
- V5 records `context_down`;
- V1–V4 remain loadable;
- legacy mount edges cannot trigger newly-authored non-bike interactions;
- persisted world facts are whitelist-based;
- encounter/boss state is excluded from V0.5 profile persistence;
- restored story memories remain discovered;
- V0.5 authoring runs after Game Foundation V1;
- new Unity GUIDs remain unique.

A green source gate is not Unity runtime qualification. Unity import/compile, input feel, prompt arbitration, save filesystem behavior, camera/HUD composition, and BCI display timing still require real runtime evidence.

## 12. Next version

After V0.5 qualifies, the next architecture tranche should build reusable world-content primitives over these seams:

- physical-authority save coordinator and restore graph;
- typed doors / gates / shortcuts;
- NPC interaction + dialogue graph;
- durable pickups / loot grants;
- shrines / neural mechanisms;
- inventory and equipment catalog;
- map / region discovery;
- quest objective markers that remain subordinate to exploration;
- explicit content ids and migration/version contracts.

The aim is to increase meaningful game content without increasing the number of competing managers or player-facing buttons.
