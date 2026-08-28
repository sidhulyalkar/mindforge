# Mindforge First Journey Vertical Slice

## Experience target

Mindforge should first feel like a polished third-person action game and only then reveal why the neural layer belongs inside it.

The first playable journey is a compact 8–12 minute authored route that begins with simple movement and ends at **The Fractured Signal**. A practiced run should take roughly 4–6 minutes. The route must work controller-only, in simulated neural modes, and with live calibrated BCI without changing combat rules.

The experience target is:

> Wake in a strange ruin, move through a cavern into a broken house and its cellar, learn the Guardian's physical combat language against increasingly demanding enemies, break a Warden guarding the sealed chamber, then enter the Fractured Signal arena already fluent in movement, lock-on, dodge, sword, shield, parry, projectile manipulation, Flux, and the Sight/Guard spatial language.

The vertical slice is successful when a first-time player can reach the boss without needing an external explanation of the controls, and when the BCI targets read as part of combat composition rather than as a separate minigame.

---

## Non-negotiable design laws

1. **Hands own precision. The brain owns transformation.** EEG may never move, rotate the camera, acquire a target, swing, guard, dodge, fire, or parry.
2. **The physical game must be fun with BCI disabled.** Every encounter is qualified controller-only first.
3. **Uncertainty removes neural authority.** Poor or ambiguous evidence abstains rather than guessing.
4. **BCI opportunities must tolerate second-scale evidence.** No neural action may be required for frame-critical survival.
5. **The camera is part of combat.** Environment widths, enemy counts, encounter positions, and effects must respect third-person readability.
6. **No invisible tutorial rules.** Enemy telegraphs and spatial composition must teach the mechanic the player needs next.
7. **No content before control.** If movement, facing, lock-on, collision, hit confirmation, recovery, or camera comfort are not good enough, additional rooms and enemies do not count as progress.
8. **Every effect has one owner.** Gameplay authority, presentation, neural evidence, timing, and telemetry stay separated.

---

# 1. Journey structure

The journey travels primarily along +Z so direction is legible even when the player freely explores each room. It remains mostly planar for this competition slice because the Guardian currently owns a deliberately planar Rigidbody. The environment can imply descent into a cellar through architecture, ceiling height, light, sound, and transitions without introducing unreliable vertical locomotion before the core combat is qualified.

## Segment A — The Listening Cavern

**Purpose:** movement, camera orbit, lock-on, sword, directional dodge.

**Spatial shape**
- Spawn in a quiet 8–10 m wide cavern mouth.
- The first enemy is visible before it is dangerous.
- Rock columns create camera occlusion practice without creating a claustrophobic corridor.
- The route bends slightly so the player naturally rotates the camera instead of holding W down a straight hallway.

**Encounter A1**
- Two low-health melee Hollows, introduced sequentially or with enough spacing that only one can pressure at first.
- The first Hollow uses a long, clear melee wind-up.
- The second appears after the player has already crossed the first combat space.

**What the player learns without a modal tutorial**
- WASD follows camera heading.
- Mouse/trackpad or arrows orbit the view.
- T locks a conventional target.
- Locked movement becomes orbit/strafe movement.
- Space dodges in held WASD direction.
- F/LMB chains sword attacks.

**Failure tolerance**
- Low enemy damage.
- Large telegraph windows.
- Long recoveries.
- No overlapping ranged pressure.

## Segment B — The Ruined House

**Purpose:** target switching, line-of-sight, mixed melee/ranged pressure, shield introduction.

**Spatial shape**
- A broken front wall opens into a wider foyer.
- Furniture/columns create partial cover but preserve camera lanes.
- Side alcoves reward looking around without becoming navigation traps.
- A neural-seal gate at the rear provides a visible goal.

**Encounter B1**
- One Hollow advances.
- One Shardcaster stays farther back and fires clearly telegraphed projectiles.

**What the player learns**
- T acquires the most useful target in view.
- While locked, left/right arrow or mouse-wheel target switching may select another enemy.
- RMB/E shield is a directional commitment.
- Strafing changes projectile geometry.
- Sword swings can parry projectiles.
- Perfect shield timing reflects projectiles.

**BCI role**
- In calibrated neural modes, locking a target gives Sight and Guard a stable left/right home around that enemy.
- In controller-only qualification, this visual language may still be shown but is clearly labeled as non-authoritative.

## Segment C — The Cellar Passage

**Purpose:** combine dodge, guard, sword-parry, Counter Pulse, ranged pressure, Flux.

**Spatial shape**
- Ceiling and walls visually compress.
- Gameplay width remains generous enough for a 5–6 m camera boom plus collision retraction.
- Copper/teal conduits guide the eye toward the next chamber.
- Cover is asymmetric so the player can choose an approach.

**Encounter C1**
- Two Hollows attack on staggered cadence.
- One Shardcaster applies ranged pressure from deeper in the room.
- Attack scheduling avoids simultaneous unavoidable melee + projectile impact.

**What the player learns**
- Space is a repositioning tool, not merely an iframe button.
- C Counter Pulse has a short, intentional reflection window.
- Near misses, parries, and pressure build Flux.
- R becomes meaningful when Flux is full.

## Segment D — The Warden Chamber

**Purpose:** mastery check before the boss.

**Enemy:** Signal Warden.

The Warden is not a miniature copy of the final boss. It is a compact test of the player's physical vocabulary:
- stronger poise,
- deliberate melee cleave,
- short ranged burst,
- readable alternation between pressure modes,
- enough health for a 20–40 second duel,
- no complex multi-phase scripting.

The Warden should demand:
- lock-on orbiting,
- reading a telegraph,
- one or more dodges,
- shield/parry competence,
- understanding when to continue or stop a sword chain,
- optional Flux payoff.

Clearing the Warden opens the final threshold but does not immediately start the boss.

## Segment E — The Fractured Signal Threshold

**Purpose:** decompression and anticipation.

- Combat audio thins.
- The corridor opens dramatically.
- The final Arena V3 architecture becomes visible before the boss activates.
- A separate boss activation threshold prevents the boss from firing down the corridor.
- Crossing the threshold closes the entrance seal behind the Guardian and activates the boss.

This is also the cleanest place to let the two neural targets become visually prominent, because the player is already looking toward the boss and has a brief attentional reset before phase one begins.

## Segment F — The Fractured Signal

The current boss remains the culmination, but its first phase must now assume the player has already learned the physical vocabulary. Phase one can therefore become cleaner rather than easier: fewer explanation prompts, stronger readable patterns, and more deliberate opportunities for Sight/Guard setup.

---

# 2. Third-person control quality plan

## Camera

Required behavior:
- persistent behind-the-player free camera,
- full mouse/trackpad orbit,
- arrow-key orbit fallback,
- bounded pitch,
- shoulder offset,
- environment collision that ignores the Guardian hierarchy,
- smooth but not laggy position response,
- conventional target lock framing,
- no camera authority from EEG.

Next polish passes:
- tune mouse sensitivity at 60/120/144 Hz,
- tune laptop trackpad sensitivity separately if needed,
- evaluate shoulder offset at narrow doors,
- add optional shoulder swap only if occlusion proves common,
- add short collision recovery hysteresis so the camera does not pop outward after leaving a wall,
- reserve camera shake for combat impact rather than locomotion noise,
- keep VEP targets outside camera shake transforms.

## Movement

Required feel:
- direct WASD sampling,
- camera-relative world movement,
- fast acceleration,
- faster reversal than acceleration,
- strong deceleration,
- smooth yaw toward travel direction while unlocked,
- locked strafing while continuing to face the current target,
- no stamina cost for ordinary movement/dodge in the competition slice.

Polish targets:
- 0–90% max-speed response should feel nearly immediate but not digital,
- stop distance should be short enough for precise spacing,
- diagonal speed remains normalized,
- movement should never be suppressed by a camera mode,
- narrow geometry must not snag the Rigidbody,
- attack movement reduction should feel like weight, not input loss.

## Dodge

The dodge must remain directional and chainable.

Design target:
- held WASD has direction priority,
- stationary dodge uses combat heading,
- short invulnerability window,
- no cooldown economy,
- buffered press near dash end,
- immediate facing response,
- retained exit velocity low enough to regain steering quickly.

Later polish:
- authored animation with root visual motion while authority stays on the Rigidbody,
- foot/cloth trails,
- subtle FOV compression only on high-speed start,
- different presentation for forward/side/back dodge without different authority rules.

## Target lock

The single-boss assumption must be removed.

Target lock v2 requires:
- discover active enemy CombatantVitals in range,
- reject dead/inactive candidates,
- prefer targets near camera center,
- include distance in acquisition score,
- avoid obvious through-wall acquisition,
- automatically release or reacquire after death,
- support target cycling while locked,
- preserve T as player-owned lock/unlock input,
- expose current target to camera, Wisp, projectile reflection and combat presentation.

Suggested laptop mapping:
- T: lock/unlock best target,
- Left/Right Arrow while locked: previous/next target,
- Mouse wheel while locked: cycle targets,
- arrows return to camera orbit when unlocked.

---

# 3. Combat interaction matrix

## Sword

Player-owned input: F or LMB.

Baseline behavior:
- three-step light chain,
- swept physical capsule contact,
- attack movement reduction,
- queue window instead of button-mash polling,
- third hit has stronger damage/poise/recovery,
- active blade volume can intercept hostile projectiles.

Feel pass:
- stronger anticipatory pose on step 3,
- hit-stop stays short on normal enemies,
- impact VFX should originate at actual contact point,
- enemy hit reaction should communicate poise independently from HP,
- whiff recovery should be legible but not punitive.

BCI Sight may only modulate bounded properties after the player attacks:
- damage,
- reach,
- projectile parry payoff,
- selected presentation intensity.

## Shield

Player-owned input: hold RMB or E.

Baseline behavior:
- directional coverage,
- Guard Integrity pressure budget,
- chip damage,
- perfect-guard timing window,
- reflected projectile on perfect guard,
- movement slowdown while raised.

Feel pass:
- shield should visibly orient to combat heading,
- incoming projectile should produce distinct block vs perfect-guard feedback,
- guard break requires a strong audiovisual cue and short recovery opportunity,
- normal guard should not cover 360 degrees.

BCI Guard may only modulate bounded properties after the player raises the shield:
- coverage,
- stability,
- absorption,
- regenerative payoff.

## Pulse Shot

Player-owned input: Shift.

Role:
- low-friction ranged pressure,
- useful for finishing ranged enemies,
- not stronger than mastering sword spacing,
- can carry Sight amplification.

## Rift Cleave

Player-owned input: Q.

Role:
- committed close-range crowd tool,
- strong poise pressure,
- useful in cellar multi-enemy encounter,
- obvious recovery prevents spam from replacing the sword.

## Counter Pulse

Player-owned input: C.

Role:
- short 180 ms reflection window,
- teaches predictive projectile timing,
- stronger Flux reward than passive defense,
- reflected target should be the current conventional target, not a hard-coded boss.

## Gravity Bloom

Player-owned input: R when Flux is full.

Role:
- converts projectile pressure into a player-controlled spectacle,
- captures hostile projectiles over a short window,
- returns them toward the current target,
- provides a natural high-impact reward in the cellar and boss.

## Concord / Twin Eclipse

BCI creates strategic setup; the hand decides execution.

Sight + Guard overlap creates Concord. Full Flux + Concord + R creates Twin Eclipse.

This remains a rare payoff. The journey should expose the ingredients before expecting the player to understand the full combination.

---

# 4. Enemy family

## Hollow

Role: melee fundamentals.

Behavior:
- approaches to sword range,
- clear 0.45–0.60 s wind-up,
- single directional strike,
- long recovery,
- low HP and poise,
- mild lateral approach so fights do not look robotic.

Teaches:
- lock,
- circle,
- sword chain,
- directional dodge,
- shield coverage.

## Shardcaster

Role: ranged geometry.

Behavior:
- prefers 5–8 m distance,
- visibly charges a projectile,
- strafes or retreats when crowded,
- fires one readable shot at first,
- later encounters may use a two-shot spread.

Teaches:
- camera awareness,
- target switching,
- cover,
- shield reflect,
- sword projectile parry,
- Counter Pulse.

## Signal Warden

Role: pre-boss mastery test.

Behavior:
- higher health/poise,
- alternates committed melee and ranged burst,
- faster recovery than Hollow but slower than final boss,
- cannot stack unreadable attacks,
- grants significant Flux on defeat.

Teaches:
- complete physical combat loop,
- resource conversion,
- patience around recovery windows.

All enemies share one reusable authority component with archetype tuning rather than three unrelated scripts.

---

# 5. Enemy telegraph grammar

Telegraphs must be readable without depending on color alone.

Every attack communicates:
1. **intent** — enemy pose/core changes,
2. **geometry** — ring/line/cone or projectile charge indicates where danger will be,
3. **time** — animation/energy build visibly progresses,
4. **release** — sharp sound/flash at authority resolution,
5. **recovery** — enemy posture tells the player when retaliation is safe.

Visual conventions:
- hostile intent: warm magenta/red-orange accents,
- Sight: reserved blue,
- Guard: reserved green,
- Concord: distinct violet/gold treatment,
- environment guidance: cyan/teal/copper, never pulsing at SSVEP frequencies.

Nothing in environmental decoration may accidentally behave like a 10 or 12 Hz coded target.

---

# 6. BCI spatial integration

The Wisp should follow the current conventional target lock rather than a hard-coded boss.

Unlocked:
- Wisp stays near the Guardian.
- Neural targets can remain hidden or subdued unless a combat target has been intentionally established.

Locked:
- Sight settles screen-left of the target.
- Guard settles screen-right of the target.
- coded luminance remains owned exclusively by VepAuraStimulus,
- shell feedback may respond slowly/non-periodically to accepted evidence,
- no camera or target-lock state is created by EEG.

This produces a natural visual sentence:

`I choose who I am fighting -> the Wisp binds that enemy -> I may attend left/right -> accepted neural state modifies the tool I physically use.`

---

# 7. Environment art direction

The route should feel like one place, not four theme-park tiles.

Narrative read:
- cavern: raw stone and faint dormant neural veins,
- ruined house: human/architectural history invaded by signal architecture,
- cellar: denser conduits, lower ceiling, stronger corrupted signal light,
- Warden chamber: deliberate ritual geometry,
- final arena: full Arena V3 language revealed at scale.

Shared palette:
- midnight/indigo stone,
- desaturated slate,
- restrained copper/bronze,
- cyan/teal world guidance,
- hostile magenta/crimson,
- neural blue and green remain reserved.

Graphical priorities:
1. silhouette readability,
2. lighting hierarchy,
3. material contrast,
4. impact VFX,
5. animation/secondary motion,
6. small decorative detail.

Do not spend time on micro-props while silhouettes, camera framing, or hit feedback are still weak.

---

# 8. Audio plan

Audio should perform mechanical work.

Required categories:
- footsteps and surface variation,
- sword whoosh, contact, armored contact, projectile parry,
- shield block, perfect guard, guard break,
- enemy wind-up and release cues,
- projectile travel cue,
- gate open/close,
- room ambience,
- Wisp acceptance cue,
- Signal Break sensory-rest mix,
- boss-phase escalation.

Important BCI rule:
- do not provide rhythmic audio at target frequencies that could become an unintended confound.

The route should use dynamic music intensity rather than a continuous maximal mix. Quiet traversal gives combat impacts room to feel large.

---

# 9. Progression architecture

Add a reusable FirstJourneyDirector with explicit encounter stages.

Each stage owns:
- activation point/radius,
- enemy set,
- forward gate,
- objective/lesson copy,
- clear event,
- optional recovery reward.

State flow:

`TRAVERSAL -> ENCOUNTER_ARMED -> ENCOUNTER_ACTIVE -> CLEAR -> GATE_OPEN -> TRAVERSAL`

After the Warden:

`WARDEN_CLEAR -> BOSS_UNLOCKED -> THRESHOLD_CROSSED -> BOSS_SEAL_CLOSE -> BOSS_ACTIVE`

The boss remains inactive until the player crosses its threshold so it cannot attack through the journey corridor.

The director must not fake calibration, neural acceptance, damage, or player input.

---

# 10. Checkpoints and failure recovery

The first implementation may begin with one start-state reset, but competition-ready journey needs checkpoints.

Planned checkpoints:
- cavern entrance,
- house cleared,
- cellar cleared,
- Warden cleared / boss threshold.

Checkpoint data should include only deterministic game state needed to reconstruct the run:
- player position/rotation,
- player HP/Guard Integrity/Flux,
- cleared encounter ids,
- boss-unlocked flag,
- loadout,
- neural authority provenance remains separate.

Never serialize raw EEG into gameplay checkpoint state.

---

# 11. HUD and onboarding

The HUD should progressively reveal information.

Traversal:
- small current objective,
- Guardian HP/Guard/Flux,
- no boss bar.

Combat:
- target lock indicator,
- short contextual lesson only until the action has been demonstrated,
- enemy telegraph carries more information than text.

Boss:
- boss HP/poise appears only when boss authority is active.

Judge lens remains opt-in and explains physical vs neural authority without dominating the player experience.

---

# 12. Instrumentation and playtest metrics

Software tests cannot tell us whether movement feels good. Human playtests must generate structured observations.

Track per run:
- time to first movement,
- time to first lock,
- time to first successful sword hit,
- dodge count and damage avoided near dodge windows,
- lock switches,
- guard raises/blocks/perfect guards/breaks,
- sword projectile parries,
- Counter Pulse attempts/successes,
- Flux gained/spent,
- damage taken per encounter,
- encounter duration,
- deaths by stage,
- boss entry time,
- boss result.

Human review after a run:
- camera comfort 1–5,
- movement precision 1–5,
- lock-on usefulness 1–5,
- melee responsiveness 1–5,
- defensive readability 1–5,
- graphical clarity 1–5,
- enjoyment 1–5,
- could explain what the BCI changes: yes/no.

These human ratings remain separate from machine qualification evidence.

---

# 13. Implementation sequence

## J0 — Preserve the qualified third-person base

Branch from exact green head. No journey work lands directly on main until the branch is independently qualified.

## J1 — Generalize combat targeting

- GuardianTargetLock v2 discovers/cycles multiple enemies.
- current conventional lock target becomes the target for Counter Pulse, sword parry, perfect guard reflection, Gravity Bloom, and Wisp placement.
- fallback boss target remains for compatibility.
- tests guarantee EEG cannot alter lock state.

Gate: multi-enemy targeting contracts green.

## J2 — Reusable journey enemies

- JourneyEnemyController authority component.
- Hollow, Shardcaster, Warden archetype tuning.
- deterministic telegraph/recovery state machine.
- melee uses existing shield/dodge authority.
- ranged uses existing MindforgeProjectile.
- enemy death/Flux events.
- JourneyEnemyPresentation is presentation-only.

Gate: enemies can be damaged, die, attack, respect dodge/guard, and never manipulate neural authority.

## J3 — Encounter progression

- FirstJourneyDirector.
- neural seal gates.
- staged enemy activation.
- Warden clear unlocks boss threshold.
- threshold activates boss and closes arena seal.
- boss death reopens seal.

Gate: no boss attacks before threshold and no stage can silently skip its required enemies.

## J4 — Authored environment

- editor-authored cavern/house/cellar/Warden chamber/final approach.
- move existing Arena V3 presentation to final arena.
- preserve physics floor/colliders and VEP materials.
- keep camera-safe widths.
- procedural placeholder art remains replaceable by production assets.

Gate: one-click Showcase build creates the complete route from a clean checkout.

## J5 — Journey HUD/onboarding

- current objective,
- stage lesson copy,
- player HUD visible before boss,
- boss HUD appears only when boss active,
- target cycling discoverable,
- controller-only BCI status truthful.

## J6 — Feel pass

Human Unity playtests, then tune:
- movement acceleration/deceleration/reversal,
- camera sensitivity and collision recovery,
- lock acquisition score/range,
- attack timing and queue windows,
- guard movement and chip pressure,
- dodge duration/exit steering,
- enemy windups/recoveries,
- room dimensions.

No tuning value is promoted solely because it looks good in source.

## J7 — Visual/audio pass

- replace enemy placeholder silhouettes,
- animation bridges,
- contact VFX,
- environmental lighting hierarchy,
- dynamic ambience/music,
- sound telegraphs,
- gate effects,
- boss reveal.

## J8 — Neural integration qualification

Run the full ladder:
1. controller-only complete journey,
2. decision simulation,
3. NeuralEvent replay,
4. EEG replay through production decoder,
5. neurOS Phantom full path,
6. physical display timing,
7. live stationary calibration,
8. live movement/lock selection,
9. light journey combat,
10. full boss encounter.

## J9 — Competition hardening

- checkpoint/restart,
- one-command demo launch,
- no-service controller fallback clearly labeled,
- bounded queues and stale-event handling unchanged,
- frame timing telemetry,
- build validation,
- exact-head evidence bundle,
- judge-mode rehearsal.

---

# 14. Promotion gates

The journey is not competition-ready until all are true:

### Control gate
- WASD works from clean Unity project state.
- camera can orbit 360 degrees without losing player orientation.
- lock-on with 3 enemies is predictable.
- directional dodge never consumes movement input unexpectedly.

### Combat gate
- every enemy attack has visible wind-up, authority resolution, and recovery.
- no unavoidable overlapping attack combination in tutorial encounters.
- sword, shield, dodge, Counter Pulse and Bloom each have at least one useful situation.

### Camera gate
- no self-collision collapse.
- no geometry forces frequent clipping.
- boss and Guardian remain readable under lock.
- VEP targets remain stable enough for physical timing validation.

### Visual gate
- environment hierarchy is readable without HUD arrows.
- hostile attacks are visually distinct from Sight/Guard.
- hit confirmation is obvious.
- boss reveal feels materially larger than prior rooms.

### BCI gate
- player can complete the game with BCI disabled.
- accepted Sight/Guard changes are clearly perceivable but bounded.
- abstention never creates a phantom command.
- target lock and camera remain completely conventional.

### Evidence gate
- exact-head CI green.
- Unity build/compile observed on the competition project.
- controller-only human playthrough recorded.
- physical 10/12 Hz display timing measured before making timing claims.
- live Unicorn claims only after actual hardware runs.

---

# Immediate implementation tranche

The first branch implementation should deliver a testable end-to-end greybox rather than isolated subsystems:

1. multi-target GuardianTargetLock,
2. dynamic reflected-projectile targeting,
3. lock-following Soul Wisp,
4. Hollow / Shardcaster / Warden enemy authority,
5. enemy telegraph presentation,
6. journey gates,
7. FirstJourneyDirector progression,
8. cavern -> ruined house -> cellar -> Warden chamber -> boss threshold geometry,
9. final Arena V3 moved to the end of the route,
10. HUD changes so player state exists throughout the journey and boss state appears only at boss activation,
11. one-click Showcase build integration,
12. structural tests and exact-head CI.

After this tranche, the next work should be driven by the first actual Unity playtest rather than assumptions about feel.
