# Mindforge Game Masterplan V1

Status: product + architecture north star for post-hackathon development.

This document is intentionally larger than the current vertical slice, but every near-term milestone is grounded in systems that already exist in the repository. It is not a promise to manufacture AAA content volume overnight.

## 1. Product thesis

Mindforge is a third-person action RPG in which conventional input owns precision and the player's neural state owns transformation.

**Hands own precision. The brain owns transformation.**

The player should always feel responsible for:

- movement;
- camera and target selection;
- jumping and traversal;
- dodge timing;
- attacks and parries;
- mounted steering;
- positioning and encounter decisions.

Accepted BCI state may transform bounded systems such as:

- weapon reach / energy expression;
- defensive or perceptual stance;
- resonance of world mechanisms;
- optional routes and environmental states;
- build synergies;
- boss mechanics designed around sustained cognitive state;
- cooperative or competitive transformation rules.

BCI must never silently originate movement, attacks, jumps, dodges, target selection, or other precision actions.

## 2. What makes the game worth building

Mindforge should not compete with large action RPGs by immediately matching their number of square kilometers. It should compete by having a combat/world idea those games cannot simply bolt on later.

The differentiators are:

1. **Neural transformation as a legible combat resource.** Neural state changes the possibility space while conventional skill executes within it.
2. **A cyber-mythic world built around cognition.** Architecture, factions and monsters embody memory, prediction, attention, inhibition, synchronization and corrupted signal processing without becoming a neuroscience lecture.
3. **Readable high-skill combat.** Enemy intent commits early enough that correct reads and dodges work. Player mastery comes from timing, spacing, route choice and state management.
4. **A spectator-readable neural layer.** Competitive viewers can understand what transformation is active and why without viewing raw EEG or opaque classifier output.
5. **Scientific honesty.** Raw EEG stays outside Unity. Accepted events are bounded, logged and separable from presentation.

## 3. Design pillars

### 3.1 Read → commit → transform → punish

Every strong encounter should expose an understandable loop:

1. read enemy geometry / telegraph;
2. commit to a conventional physical action;
3. exploit or manage an optional neural transformation;
4. punish recovery or reposition.

Enemies cannot track indefinitely through the player's correct dodge. Existing tracking-lock contracts are the model for all future attacks.

### 3.2 Vertical worlds, not flat arenas

Traversal should create tactical decisions:

- landing pockets;
- conventional stairs and ramps;
- risky jump / air-dash shortcuts;
- high ranged positions that remain reachable;
- loops and unlockable shortcuts;
- mounted lanes where scale benefits from speed;
- visual landmarks visible across districts.

Large spaces must still have authored combat geometry. Empty acreage is not scope.

### 3.3 Transformation, not autopilot

The neural layer should change parameters or modes with explicit bounds. Examples:

- Sight extends Aetherblade energy reach inside a declared cap;
- Guard can later transform stability / recovery / counter windows without creating a shield-button replacement;
- Concord can alter an arena mechanism after both channels satisfy a contract;
- a future boss may expose two vulnerable geometries depending on accepted cognitive state;
- a future traversal relic may resonate with sustained state, opening an optional route while ordinary traversal remains possible.

### 3.4 World truth is semantic

Concrete systems own physical outcomes. They publish semantic facts after those outcomes happen.

Example:

`JourneyEnemyController` resolves an enemy defeat → encounter director observes completion → semantic bridge publishes `encounter.cleared` → world ledger stores a durable fact → quest system evaluates it → progression adapter grants an idempotent reward → HUD and spectator systems observe the result.

The quest system does **not** kill enemies or open gates itself.

## 4. Full game shape

The intended full campaign is a connected set of large regions rather than one seamless unbounded simulation.

### Act I: Aetheria, the Fractured City

Purpose: teach the complete movement/combat/neural grammar.

Current vertical-slice locations become the seed of a real first region:

- Prism Bastion
- Neon Causeway
- Market of Broken Momentum
- Choir of Ruined Towers
- Hall of Excessive Gravitas
- Menagerie Crucible
- Signal Cathedral / Null Ward

Major beats:

- recover the Aetherblade;
- discover that the city still executes contradictory cognitive protocols;
- survive the Menagerie combat examination;
- confront Lord Malatract;
- learn that Malatract is enforcing only one fragment of a larger broken consensus;
- unlock passage to the Aetheria Frontier.

### Act II: The Aetheria Frontier

Purpose: convert the corridor-like vertical slice into true exploration.

Region grammar:

- wider horizontal exploration;
- ruined transit / hoverbike routes;
- optional neural shrines;
- hostile faction camps;
- underground signal caverns;
- roaming elite encounters;
- multiple approaches to regional objectives.

Primary question: who is benefiting from Aetheria remaining fractured?

### Act III: The Memory Sea

Purpose: make memory and reconstruction physically navigable.

Possible mechanics:

- areas that reconstruct from persistent world facts;
- mirrored encounter variants based on previously chosen routes;
- enemies whose geometry changes between remembered and current forms;
- optional BCI transformations affecting what layer is perceptually emphasized, never replacing conventional navigation.

### Act IV: The Choir Beyond

Purpose: large multi-faction conflict and systemic world reactions.

Features:

- faction reputation;
- settlements with durable state;
- regional bosses that alter travel conditions;
- cooperative challenge spaces;
- tournament arenas embedded naturally in the world.

### Act V: The Consensus Engine

Purpose: endgame convergence of physical skill, build mastery, story choices and neural transformation.

The final conflict should test whether the player can keep agency while using systems designed to predict and shape them.

## 5. Combat architecture

### Player conventional actions

Core actions remain:

- locomotion;
- jump / double jump;
- hover / air dash;
- grounded dodge roll;
- Aetherblade light chain;
- committed heavy / cleave;
- counter / parry;
- target lock;
- mounted steering / boost / blade attacks.

Every new weapon should satisfy the same authority rule: input originates the action, combat authority resolves it, neural state may only transform declared parameters.

### Weapon families, later

Do not add these until Aetherblade feel is excellent.

Candidate families:

- Aetherblade: spacing, parry, mobile chains;
- Resonance Pike: deliberate reach and aerial control;
- Orbit Knuckles: high commitment close-range stance;
- Signal Bow: conventional aiming with neural transformation of projectile behavior, never BCI aiming;
- Gravitas Hammer: poise destruction and environmental interactions.

Each family needs a unique timing/spacing identity, not merely different damage numbers.

## 6. Enemy ecosystem

Enemy design is based on **combat questions**, not skins.

Every enemy contract declares:

- silhouette;
- preferred range;
- locomotion profile;
- anticipation time;
- tracking lock fraction;
- committed attack geometry;
- recovery / punish window;
- vertical reach;
- role inside mixed groups;
- neural interaction, if any;
- deterministic seed / replay requirements when applicable.

Current Menagerie identities are a first vocabulary:

- Scrap Goblin: pressure / harassment;
- Shardsinger: ranged timing;
- Bass Golem: large readable commitment;
- Chrome Penitent: timing mix-ups;
- Rift Stalker: committed movement pressure;
- Choir Drone: spatial interference;
- Aero Gargoyle: vertical threat;
- Prism Maw: area denial / close threat;
- Veil Reaper: execution pressure;
- Orbit Seraph: high-order mixed threat.

Future factions should remix these combat questions rather than cloning controllers per region.

## 7. Boss philosophy

Bosses need three layers:

1. **authority:** deterministic attacks, collision, damage, phase rules;
2. **readability:** anticipation, lock, contact, recovery and camera-safe geometry;
3. **identity:** presentation, narrative semantics and neural transformation hooks.

Lord Malatract is the first example of identity layered over an existing boss authority.

Future bosses should introduce one new rule at a time and then combine rules. A boss phase should not become difficult solely by increasing particle density, health or tracking.

## 8. Progression

Progression must reward mastery without making the opening hours feel intentionally bad.

### Currencies

- **Resonance:** broad exploration / quest currency for build expression and world systems.
- **Mastery:** rarer proof-of-skill currency earned from major combat achievements.

### Unlock categories

- combat techniques;
- weapon forms;
- traversal techniques;
- passive build modifiers;
- neural transformation modifiers;
- challenge / replay access;
- codex and world knowledge;
- region permissions.

Unlock flags are semantic. A dedicated gameplay adapter must explicitly consume a flag before it changes physical gameplay.

### Avoid

- +2% damage filler trees;
- huge stat inflation that invalidates reading attacks;
- grind requirements for basic responsiveness;
- neural classification quality becoming a player power stat.

## 9. Quest and story architecture

Quests are ordered graphs over semantic world facts.

A quest contains:

- stable id;
- title / description;
- prerequisite quest ids;
- ordered steps;
- conditions over typed world facts;
- declarative rewards.

The quest runtime evaluates only. A separate reward runtime grants progression with durable reward receipts.

Future story systems may subscribe to quest / story signals for:

- dialogue branches;
- NPC relocation requests;
- codex entries;
- cinematic requests;
- music state;
- objective presentation.

Those systems still require explicit concrete adapters for physical scene changes.

## 10. Saving and persistence

Current Foundation V1 provides memory snapshots of:

- typed world facts;
- progression currencies;
- unlocks;
- reward receipts.

Next persistence milestone should add a versioned save envelope containing:

- content revision;
- world-state snapshot;
- player-progression snapshot;
- checkpoint identifier;
- equipped build ids;
- accessibility settings;
- conventional input settings;
- optional calibration reference metadata, never raw EEG.

Do not write a generic save file until each concrete authority has a restore contract. Semantic state alone must not claim to restore a physically impossible scene.

## 11. World interactions

The next interaction system should provide one conventional-input authority and typed interaction contracts.

Candidate interactions:

- rest / reconstruct;
- talk;
- examine;
- activate mechanism;
- mount;
- open ordinary door;
- accept challenge;
- collect durable item.

Avoid multiple MonoBehaviours independently sampling the same key. Existing Memory Forge and hoverbike bindings should be migrated behind one action router when the interaction tranche begins.

## 12. Multiplayer and esports direction

Competitive play should emerge from the deterministic combat foundation rather than bolting networking onto arbitrary campaign state.

### Initial competitive modes

1. **Menagerie Time Trial**
   - identical authored encounter;
   - deterministic enemy seed;
   - fixed loadout category;
   - split times per wave;
   - optional BCI and non-BCI divisions;
   - replay evidence required.

2. **Boss Trial**
   - standardized boss phase seed;
   - score based on completion time, damage taken, parry quality and objective rules;
   - no hidden adaptive difficulty.

3. **Relay / cooperative cognition challenge** later
   - players retain conventional control of their own characters;
   - neural transformations may affect shared mechanisms under explicit rules.

### Ranked eligibility

`competitive_candidate` means the encounter is designed to be observable and reproducible.

`ranked_eligible` must stay false until the actual build qualifies:

- deterministic replay contract;
- Unity runtime performance bounds;
- no variable frame-rate gameplay authority;
- input-tape verification;
- anti-cheat / tamper evidence;
- fixed ruleset hash;
- display / stimulus qualification for BCI divisions;
- clear policy for pauses, disconnects and degraded neural link;
- privacy-safe telemetry.

## 13. Spectator experience

A spectator should understand:

- player health / endurance / Flux;
- current weapon form;
- current accepted neural transformation;
- encounter / wave / boss phase;
- split times;
- major parries / breaks;
- build identity;
- whether the run is BCI-enabled and qualified.

Never expose raw EEG as entertainment telemetry by default.

The semantic signal bus is the correct source for spectator overlays because it is downstream of gameplay truth.

## 14. BCI product modes

The game should support three explicit modes:

### Conventional

No BCI required. Full game remains playable and skill-complete.

### Assisted / showcase BCI

Accepted neural events transform bounded gameplay / presentation systems. Useful for demos, accessibility research and early competition formats.

### Qualified BCI

Requires calibration, device/display evidence, source provenance, transport health and ruleset compliance. Only this mode may be used for a future ranked BCI division.

A degraded or missing neural link must fail toward conventional agency rather than trapping the player.

## 15. Visual direction

Neural Gothic / cyber-mythic, but disciplined.

Priority hierarchy:

1. immediate threat;
2. Guardian confirmation;
3. coded VEP target when scientifically required;
4. accepted Wisp / neural transformation;
5. secondary combat objective;
6. architecture;
7. ambient spectacle.

Rules:

- no generic glow soup;
- use structured field lines, conduits, fractured signal geometry and readable material families;
- silhouettes must survive the actual gameplay camera;
- distant architecture implies scale without requiring physical simulation;
- decorative geometry stays collider-free unless explicitly authored as traversal/combat space;
- do not let post-processing alter coded luminance timing.

## 16. Audio direction

Audio should become a first-class combat language.

Needed layers:

- attack anticipation signature per enemy family;
- committed attack transient;
- parry confirmation;
- hit-stop-compatible impact layer;
- endurance warning;
- neural acceptance sound distinct from evidence noise;
- regional musical identity;
- boss phase stems;
- competition mix with unambiguous critical cues.

Dubstep / bass-music influence can live in rhythmic design and sound palette without turning every combat cue into a drop.

## 17. Technical architecture

### Concrete authorities

- GuardianMotor / mounted controller: locomotion;
- Guardian combat controllers: player attacks / parry;
- JourneyEnemyController and boss directors: enemy combat;
- encounter directors: encounter activation / completion;
- checkpoint: reconstruction lifecycle;
- accepted neural state controllers: bounded BCI transformations.

### Semantic layer

- WorldSignalBus;
- WorldStateLedger;
- HackathonWorldSemanticBridgeV1;
- WorldQuestRuntime;
- PlayerProgressionLedger;
- WorldQuestRewardRuntime;
- EncounterContractRegistry;
- passive telemetry / competitive observer;
- read-only HUD.

### Future services

- interaction router;
- save coordinator;
- inventory / equipment catalog;
- dialogue graph;
- region streaming coordinator;
- content registry / stable ids;
- ruleset hashing;
- replay verifier;
- spectator protocol;
- network authority for dedicated competitive modes.

## 18. Content pipeline

Large-world production should be data multiplication, not controller multiplication.

A region package should eventually declare:

- stable region id;
- scene / streaming cells;
- traversal anchors;
- encounter contracts;
- enemy roster references;
- story discoveries;
- quests;
- checkpoints;
- loot tables;
- music profile;
- lighting / weather profile;
- performance budget;
- validation tests.

An enemy package should declare data and presentation while reusing stable authority classes whenever possible.

## 19. Performance contracts

Before increasing world volume:

- profile actual Unity CPU/GPU frame time;
- measure batches / SetPass / triangles / overdraw;
- validate camera collision against tall architecture;
- keep far-scale silhouettes visual-only;
- pool repeated dynamic VFX and projectiles where needed;
- establish per-region active-enemy budget;
- establish max dynamic lights and shadow casters;
- keep coded luminance isolated from adaptive quality.

Target frame-rate should be a ruleset property for competitive builds, not a vague preference.

## 20. Accessibility

Build accessibility into the action language:

- conventional mode never requires BCI;
- remappable controls;
- separate camera sensitivity;
- telegraph contrast / geometry options that do not invalidate competitive rules;
- subtitles / story-memory log;
- reduced camera shake;
- reduced flash mode, with qualified BCI mode clearly explaining any incompatible display requirement;
- readable endurance / health / transformation state;
- practice versions of new combat rules.

## 21. Production roadmap

### Version GF1 / First Journey V2: current tranche

- typed semantic signal bus;
- typed world-state ledger;
- snapshot restore notification;
- ordered prerequisite quest graph;
- idempotent progression rewards;
- Resonance / Mastery / unlock ledger;
- six story-memory discoveries;
- encounter contract registry;
- passive competitive run splits;
- journey HUD;
- current Hackathon playthrough remains concrete gameplay authority.

### Version 0.5: Interaction + Save Contract

- one conventional interaction router;
- migrate Memory Forge / mount / examine interactions behind actions;
- stable content ids;
- save envelope + per-authority restore interfaces;
- equipped build snapshot;
- story-memory log.

### Version 0.6: Combat Depth

- finish Aetherblade feel / animation / audio;
- introduce one additional weapon family only if Aetherblade benchmark is strong;
- enemy group coordination contracts;
- elite modifiers as data;
- boss readability and punish-window pass;
- encounter difficulty profiles.

### Version 0.7: Aetheria Region

- convert the current sequence into a larger looping region;
- three optional sub-dungeons;
- roaming elite encounters;
- meaningful hoverbike routes;
- shortcuts / checkpoints;
- regional quest branches;
- first real equipment / build choices.

### Version 0.8: Replay + Challenge

- deterministic ruleset hashes;
- replay verifier;
- Menagerie challenge mode;
- ghost / split comparison;
- spectator overlay protocol;
- conventional ranked prototype.

### Version 0.9: Qualified BCI Challenge

- explicit BCI ruleset;
- calibrated display/device qualification;
- accepted-state replay evidence;
- neural-link contingency rules;
- privacy review;
- separate BCI leaderboard prototype.

### Version 1.0 target

Do not define 1.0 by hours of filler. Define it by a complete coherent campaign:

- several large authored regions;
- deep, polished core combat;
- multiple bosses with unique rule combinations;
- durable progression/build identity;
- complete story arc;
- robust save/accessibility;
- challenge/replay modes;
- conventional game fully playable without BCI;
- optional BCI transformation layer scientifically and competitively honest.

## 22. Immediate definition of done for the next playable build

The current Foundation V1 tranche is not accepted because source files exist. It is accepted when:

1. exact-head source CI is green;
2. main is a clean fast-forward of the qualified head;
3. Unity imports/compiles with zero errors;
4. Build + Play creates one foundation root only;
5. the route advances all three quests in order;
6. each quest reward is granted once and only once;
7. reconstructing at the Memory Forge does not duplicate rewards;
8. six story discoveries fire once and remain durable in a snapshot;
9. Menagerie wave clears produce stable semantic facts and run splits;
10. competitive candidates remain `ranked_eligible = false`;
11. no foundation class originates movement, attack, encounter scheduling or neural authority;
12. the HUD stays subordinate to combat readability;
13. actual Unity frame time remains acceptable after the added observers/UI;
14. physical VEP timing/salience is separately re-qualified before any BCI claim.

That build becomes the stable floor for the interaction/save and first-region expansion tranches.
