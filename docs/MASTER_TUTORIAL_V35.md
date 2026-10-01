# Mindforge V0.35 Master Combat + BCI Tutorial

## Why this tranche exists

Mindforge now has enough working systems that the primary risk is no longer "can we build a
combat game?" The risk is fragmentation.

V0.29-V0.31 established a playable Dragon Souls-derived combat/world chassis. V0.32 established
a strong showcase chapter grammar. V0.33 established the production neural integration seam.
V0.34 established adaptive gaze onboarding, frozen stimulus geometry, held-out calibration and
evidence receipts.

Those pieces are individually useful, but they are not yet one player-facing proof. V0.32 is a
sibling branch of V0.33/V0.34 rather than their ancestor, so the strongest chapter pacing and the
strongest BCI pipeline currently live on separate lines of development. V0.35 begins collapsing
that split by making one canonical tutorial scene on the V0.34 stack and teaching the complete
gameplay vocabulary through real runtime evidence.

The immediate product goal is:

> One launch action, one world, one tutorial path, one evidence trail, from first movement to
> boss defeat.

This is an integration milestone, not an excuse to invent a second combat system.

## Critical progress assessment

### What is genuinely strong

**Combat authority is real.** V0.31 does not fake melee. The authoritative chain remains:

`InputReader -> player combat state -> authored animation -> animation event ->
CombatController -> Sword.StartAttack -> CapsuleCollider + Damage -> Health`.

`MindforgeSwordCombatAssuranceV31` observes real hitbox windows, trail presentation and
`Damage.OnHitGiven` contacts without opening hitboxes itself.

**The inherited game already exposes a broad ability vocabulary.** The pinned input/state
surface includes movement, camera control, sprint, light attacks, heavy attacks, target lock,
target switching, roll, aim, sword throw/recall, sheath/unsheath, heal, bonfire interaction,
pause, death/respawn and boss state.

**The BCI architecture has a defensible authority boundary.** V0.33/V0.34 keep raw EEG outside
Unity, bind neural selections to causal epochs, preserve source identity and allow Sight/Guard
only through the semantic intent path. V0.34 adds independent gaze profiling without silently
using gaze to classify EEG.

**The evidence culture is unusually good for a game prototype.** Software gates, native
qualification gates, provenance, session receipts and "unobserved" states reduce the chance that
a pretty demo accidentally becomes a scientific claim.

### What is not good enough yet

**The production story is fragmented across draft branches.** `main` is still V0.30 while
V0.31, V0.32, V0.33 and V0.34 remain open draft PRs. The player-facing showcase work in V0.32
does not sit underneath V0.33/V0.34. This increases regression risk and makes "the latest game"
ambiguous.

**The BCI tutorial is much stronger than the game tutorial.** V0.34 carefully verifies gaze and
neural calibration but its controls step teaches only a minimal laptop subset. It does not prove
the broader combat system that makes the game interesting.

**Control messaging is split across generations.** V0.31 exposes a full mouse/keyboard combat
scheme, V0.33 adds Space and arrow-key laptop affordances, and V0.32 presentation still describes
the older three-node BCI preview. V0.35 must expose one canonical control legend for the actual
stack being played.

**Several gameplay features exist but are not demonstrated as outcomes.** A prompt saying
"roll" or "target" is not proof that the player entered the roll state or acquired a target. The
tutorial needs observed game-state evidence, not keypress theater.

**Native qualification is still the bottleneck.** V0.31-V0.34 have strong source/software
evidence, but native Unity execution remains the promotion boundary. V0.35 must not turn
unobserved native behavior into a paper PASS.

**The showcase encounters are still more design grammar than deterministic authored content.**
V0.32 defines First Fracture and Broken Choir well, but the role-to-prefab resolver, attack-token
coordination and deterministic tutorial encounter staging are not yet the canonical runtime path.

## V0.35 authority contract

V0.35 may:

- display instructions and progress;
- subscribe to existing input/gameplay events;
- read existing movement, targeting, health, sword, bonfire, BCI and boss state;
- begin the already-existing V0.34 adaptive onboarding module;
- request a calibrated V0.33 neural listening window for tutorial field-use demonstrations;
- write a tutorial evidence receipt;
- add non-interactive UI;
- add desktop bindings that still flow through the inherited InputActions/InputReader path.

V0.35 must not:

- call sword `StartAttack` or `StopAttack`;
- invoke player attack events directly;
- move or teleport the player as tutorial authority;
- write enemy or player health;
- create a second damage path;
- force target selection;
- grant global invulnerability;
- change enemy AI state to manufacture a PASS;
- publish Sight/Guard directly to the semantic bus;
- treat gaze as EEG classifier input;
- claim a native or physical result that was not observed.

## Canonical controls for the V0.35 desktop tutorial

| Intent | Control | Evidence used |
| --- | --- | --- |
| Move | WASD | non-zero authoritative movement vector sustained |
| Camera | Arrow keys or mouse | non-zero authoritative camera vector sustained |
| Sprint | Left Shift | inherited sprint state / hold state |
| Light attack | LMB or Space | InputReader light event plus real sword swing windows |
| Heavy attack | RMB | InputReader heavy event plus a real sword swing window |
| Target lock | MMB or T | target event plus non-null current target |
| Target switch | Mouse wheel | TargetSelect event plus actual target transform change |
| Dodge roll | Left Alt | Roll event plus inherited roll/invulnerability state |
| Aim | Q hold | inherited AimHold |
| Throw blade | Q + attack | sword becomes not-returned through inherited aim/throw path |
| Recall blade | R | sword returns through CombatController |
| Sheath / unsheath | X | inherited sword sheath state changes |
| Heal | H | Health.OnHealthIncreased |
| Bonfire | E | BonfiresManager.OnTakeRestEvent |
| Pause | Esc | inherited pause path, documented but not required for tutorial progression |
| BCI field window | tutorial-owned window | same V0.33 causal window controller |
| Sight / Guard | production decoder or development simulation | receptor activation after accepted same-epoch semantic event |

The V0.31 mouse controls and V0.33 laptop controls are complementary. V0.35 should not force a
player to choose one input style just to understand the game.

## Exact tutorial progression

### 1. Movement

Prompt: `WASD`.

Pass only after meaningful movement input is sustained for a bounded interval.

Purpose: establish locomotion without immediately stacking combat instructions.

### 2. Camera

Prompt: arrow keys or mouse.

Pass only after authoritative camera input is sustained.

Purpose: prove the player can frame the character and world before pressure begins.

### 3. Sprint

Prompt: Left Shift while moving.

Pass only when the inherited sprint state or hold state is observed.

### 4. Light combo

Prompt: LMB or Space.

Pass only after at least three new animation-driven sword swing windows are observed after this
stage begins. This prevents previous combat activity from satisfying the lesson.

### 5. Heavy attack

Prompt: RMB.

Pass only after a new heavy input event and a new real swing window occur after stage entry.

### 6. Damage contact

Prompt: land a hit.

Pass only when `Damage.OnHitGiven` increments through the authoritative sword assurance layer.

This separates "animation looked correct" from "combat actually connected."

### 7. Target lock

Prompt: MMB or T.

Pass only after a target input and a non-null `CurrentTargetTransform`.

### 8. Target switch

Prompt: mouse wheel.

Pass only when the actual selected target changes. A wheel event by itself is not enough.

This stage requires at least two targetable enemies to be available. V0.35B should guarantee
that with a deterministic training encounter instead of relying on ambient world placement.

### 9. Dodge roll

Prompt: Left Alt.

Pass only when the roll input occurs and the inherited roll/invulnerability state is observed.

### 10. Aim + throw

Prompt: hold Q and attack.

Pass only after the aim path is active and `CombatController.IsSwordReturned` becomes false.

### 11. Recall

Prompt: R.

Pass only after the weapon-return event and `IsSwordReturned` becomes true.

### 12. Sheath

Prompt: X.

Pass only when the inherited sheath state is reached.

### 13. Heal

Prompt: take readable damage, then H.

Pass only on `Health.OnHealthIncreased`. Pressing H at full health cannot satisfy this lesson.

V0.35B should stage a safe, readable source of recoverable damage so the lesson never becomes
an awkward scavenger hunt for something to hurt the player.

### 14. Bonfire rest

Prompt: E at a bonfire.

Pass only on `BonfiresManager.OnTakeRestEvent`.

This demonstrates recovery/checkpoint semantics rather than only combat buttons.

### 15. Neural attunement

Run the existing V0.34 adaptive onboarding intact:

`controls -> gaze health -> gaze grid -> free explore -> gaze switch -> layout freeze ->
service wait -> repeated-block calibration -> Sight practice -> Guard practice -> movement stress`.

A V0.34 partial outcome makes the master tutorial partial. V0.35 does not silently bypass failed
calibration.

### 16. Sight field use

After successful attunement, V0.35 opens a calibrated causal window using the production V0.33
window controller. Pass only when the Sight receptor activates after an accepted semantic event.

For development, `1` may provide controller simulation while the window is open. For synthetic
or live EEG, the production decoder should provide the event. Source mode must remain visible in
the evidence trail.

### 17. Guard field use

Same contract as Sight, but pass only on Guard receptor activation.

V0.35A proves the semantic path. V0.35B must connect Guard to one authored hazard contract so the
player learns why it matters rather than only seeing a visual ring.

### 18. Boss entry

The player travels to the existing Fractured Signal encounter.

Pass only when `BossManager.IsInBoss` becomes true.

### 19. Boss defeat

Pass only on `BossManager.OnBossDefeated`.

This is the mastery exam. No tutorial system kills the boss or changes its health.

### 20. Complete

Write `mindforge.combat_tutorial_receipt.v1` containing provenance plus observed abilities.

Any hard dependency failure or failed V0.34 neural onboarding ends as Partial rather than
fabricating completion.

## V0.35A implementation scope

The first V0.35 code tranche now targets:

1. a new branch stacked on the exact V0.34 software-qualified head;
2. alternate keyboard target lock on `T`;
3. mouse-wheel target switching through the inherited `TargetSelect` InputAction;
4. one observer-driven master tutorial state machine;
5. one master tutorial runtime installer;
6. one editor command that materializes a canonical V0.35 tutorial scene from V0.34;
7. one machine-readable completion/partial receipt;
8. source-contract tests that forbid duplicate combat authority.

The V0.35A scene intentionally inherits V0.34 and the complete V0.31 world/boss chassis. It does
not yet import V0.32's nine route checkpoint objects.

## V0.35B encounter-showcase scope

After V0.35A compiles and runs natively, the next tranche should make every tutorial lesson
deterministic and visually authored.

### Training court

Provide a bounded but non-blocking training space near the opening route with:

- one passive/low-pressure target for basic swings and damage contact;
- two targetable enemies for lock and switch;
- enough clear radius for roll and sprint;
- a safe line for aim/throw/recall;
- a nearby bonfire;
- no tutorial-only damage collider.

### First Fracture

Promote the V0.32 recipe into runtime content:

- Remnant melee role;
- delayed Ranger support;
- one committed attacker maximum;
- readable roll window;
- target-switch opportunity;
- deterministic reset.

### Neural proving ground

Create two explicit gameplay contracts:

**Sight:** reveal the correct route, weak point, rune or enemy information. It changes
information, not enemy authority.

**Guard:** stabilize one authored hazard or attack window. It must not become global
invulnerability.

### Broken Choir

Promote the V0.32 elite recipe:

- Brute anchor;
- Stalker flank;
- Resonant second wave;
- optional Ranger only if readability remains good;
- at most two simultaneous committed attackers.

### Boss mastery

Boss phases should demonstrate:

- readable sweep/lunge/ground-strike fundamentals;
- one Sight-relevant information window;
- one Guard-relevant stabilization window;
- ordinary sword punish windows;
- no mandatory continuous BCI;
- safe abstention/offline behavior.

## V0.35C presentation scope

Only after mechanics are native-green:

- replace debug-style OnGUI tutorial presentation with production UI;
- show one current objective and a compact control glyph;
- show success confirmation for less than a few seconds;
- keep the center of the screen clear;
- present keyboard/mouse and gamepad glyphs from the same action schema;
- hide developer neural metrics by default, with an explicit diagnostic toggle;
- add spatial/environmental teaching cues instead of relying on text alone;
- preserve readable player silhouette during combat.

## Character framing and motion-readability contract

V0.35 owns the final ordinary third-person presentation after the older V0.31/V0.33 camera
layers have initialized. It does not replace Dragon Souls camera switching.

The reason for this pass is concrete: V0.31 free-roam used a relatively close 49 degree view,
approximately 3.75 m middle orbit radius and a left-biased ScreenX of 0.44, while V0.33 applied a
separate one-time minimum FOV correction to selected virtual cameras. Target combat was then
retuned every LateUpdate by V0.31. Those overlapping contracts can make the player feel oddly
placed and make animation scale change between gameplay modes.

V0.35 replaces that ambiguity with one final presentation contract:

- free-roam baseline FOV: 52 degrees;
- sprint FOV: 55 degrees;
- roll FOV: 56 degrees;
- free-roam middle orbit radius: 4.75 m;
- character horizontal composition: centered at ScreenX 0.50;
- ordinary free-roam vertical composition: ScreenY 0.56 with a torso tracking offset;
- target combat baseline: 56 degrees;
- crowded encounter target view: 59 degrees;
- boss-proximity target view: 62 degrees;
- target composition: centered horizontally at ScreenX 0.50;
- manual look always wins over automatic sprint recentering;
- sprint recentering can engage only after a short no-input grace period;
- aim and bonfire cameras remain inherited and are not retuned by this layer;
- camera collision uses a larger safety radius plus short smoothing so walls do not cause severe
  camera snapping or put the camera inside the player.

### Screen-space guardrail

V0.35 measures the combined bounds of the active player SkinnedMeshRenderers in viewport space
during ordinary FreeLook and target combat.

The intended safe frame keeps the complete rendered body inside approximately:

- 5.5 percent horizontal margins;
- 6 percent vertical margins.

If an animation, camera collision or transition persistently pushes the character outside that
frame, the camera may add up to 4 degrees of temporary FOV as a presentation-only safety valve.
The assist widens quickly and returns slowly to avoid visible zoom pumping.

Persistent violations are counted and logged. They are qualification evidence, not silently
ignored.

### Native camera acceptance capture

The first V0.35 native run should capture all of the following:

1. idle full-body framing;
2. forward/back/strafe locomotion;
3. sustained sprint with no manual camera input;
4. sustained sprint while actively rotating the camera, confirming auto-recenter yields;
5. three-hit light combo;
6. heavy attack;
7. roll toward, away from and across the camera;
8. target lock with one enemy;
9. target switch with two enemies;
10. crowded target combat;
11. boss-proximity target combat;
12. a wall/corner traversal where CinemachineCollider must recover;
13. aim/throw, confirming the inherited aim composition remains unchanged;
14. bonfire framing, confirming the inherited cinematic composition remains unchanged.

Reject the camera pass if the character's head or feet are routinely cropped, the camera enters
the body, the player changes apparent scale abruptly between ordinary combat states, target-lock
hides footwork, sprint recentering fights manual input, or obstacle recovery produces repeated
large snaps.

## Qualification ladder

### Q0 source contract

Required:

- tests pass;
- builder derives from V0.34;
- exactly one player and sword authority;
- V0.35 root remains non-physical;
- no attack/damage/health mutation API in V0.35 tutorial;
- target-switch bindings exist through InputActions.

### Q1 native compile

Required:

- exact V0.35 head bootstraps into pinned Dragon Souls commit;
- Unity 2021.3.20f1 compiles with zero unexpected errors;
- `Mindforge -> World V0.35 -> PLAY MASTER TUTORIAL` launches.

### Q2 basic controls

Observe movement, camera and sprint.

### Q3 melee

Observe three light swing windows, heavy attack, one real hit, no stuck sword collider.

### Q4 targeting/evasion

Observe target lock, real target change and roll.

### Q5 Aetherblade utility

Observe aim/throw, physical return and sheath state.

### Q6 recovery/progression

Observe real heal plus actual bonfire rest. Also verify death/respawn does not corrupt the
current tutorial state or combat authority.

### Q7 neural

Complete V0.34 attunement and then produce one Sight and one Guard activation in field windows.

### Q8 boss

Enter and defeat the boss without editor intervention.

### Q9 repeatability

Complete the full synthetic-EEG tutorial three consecutive times from a clean materialization.
No unexpected red Console entries, no editor intervention, no evidence mismatch.

### Q10 live instrumentation

Only after the game tutorial itself is stable:

- live gaze;
- live EEG stationary;
- movement stress;
- encounter use;
- physical display timing measurement.

## Definition of done

The master tutorial is recording-grade when one exact clean Git commit can be bootstrapped into
the pinned Dragon Souls project, launched from one menu command, and played from first movement
through boss defeat while every required gameplay ability is demonstrated through authoritative
runtime evidence, Sight and Guard pass through the production causal neural seam, failure remains
safe, and the final receipt truthfully records what was and was not observed.

At that point Mindforge stops being a collection of impressive subsystems and becomes one
explainable game loop that a new player can learn, perform and demonstrate without a developer
standing beside them.
