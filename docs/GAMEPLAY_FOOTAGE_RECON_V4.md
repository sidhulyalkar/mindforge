# Game footage reconnaissance for Mindforge BCI mod integration (v4)

## Evidence boundary
**Source review, not frame-level video inspection.** Public long-form gameplay
video listings/chapters and game mechanics references were inspected. The
current assistant environment did not provide reliable direct playback or
timestamped frame extraction of the external YouTube video streams. Nothing
below claims that a frame, animation duration, camera trajectory, frequency
or measured safe-selection interval was directly observed in video. Full
video-frame annotation still requires local footage or user-uploaded clips.

This distinction is fundamental: a boss named in a video chapter proves
that footage has been indexed, not that an SSVEP selection is safe at any
particular moment.

## Curated videos and chapter-level targets

| Game | Recording | Chapters worth instrumenting | Purpose |
|---|---|---|---|
| Hollow Knight | [BossRunner, all bosses no damage](https://www.youtube.com/watch?v=sY3QHqjrM3k) | Mantis Lords 07:08; Nightmare King Grimm 1:09:32; Pure Vessel 1:59:00; Absolute Radiance 2:05:30 | Telegraph readability, healing/stagger tradeoffs, aerial hazards |
| Hollow Knight | [Waffles316, first attempts at NKG](https://www.youtube.com/watch?v=r07i0K3UEzU) | First attempt 00:00; repeat attempts and eventual success 10:29–14:56 | Less-skilled errors, restart fatigue, deliberate gaze shifts |
| Silksong | [Shirrako, all bosses 4K/60fps](https://www.youtube.com/watch?v=00rXa-VboV8) | Lace 03:06; Last Judge 25:00; Grand Mother Silk 44:49; Lost Lace 2:12:10 | Fast mobile combat, overhead/aerial hazards, needle counters and Bind decisions |
| Silksong | [ProsafiaGaming, no-damage boss compilation](https://www.youtube.com/watch?v=7cs48ezTz1M) | Lace 02:07; Last Judge 12:01; Lace rematch 20:37; Lost Lace 1:08:09 | Independent skilled reference and multiple encounter types |
| Cuphead | [NintendoBase, expert S-rank no damage](https://www.youtube.com/watch?v=loMReQWuwJ4) | Grim Matchstick 19:24; Dr. Kahl 28:47; King Dice 35:31; Devil 39:28 | Platform occupancy, bullet patterns, parry cues and flight versus grounded |
| Cuphead | [packattack04082, DLC expert S-rank](https://www.youtube.com/watch?v=Vz2KBPu-I58) | Esther Winchester 04:19; Howling Aces 08:26; Chef Saltbaker 10:13 | Plane and camera transformations, high visual load |

These are independently hosted recordings; do not redistribute footage or
check copyrighted game assets into the Mindforge repository. Use local game
capture with player consent to measure frames, latency, mistakes and attention.

## Combat-specific conclusions grounded in external mechanics references

### Hollow Knight: no generic "boss stagger = safe selection"
- Nightmare King Grimm's documented stagger is around **2s**. A 1.25s EEG
  window, acquisition/dwell, visual refocusing and recovery can consume this
  whole interval. Therefore the default action is **status only**, not
  opening coded stimuli. Its fire bats, pillars, cloak spikes and pufferfish
  patterns require continuous visual monitoring.
- Pure Vessel chains rapid attacks, teleports, and has a parry stance.
  Stationary-looking poses do not imply safety. Short heal opportunities
  are not long enough to infer independent SSVEP feasibility.
- Mantis Lords/Sisters of Battle prioritize tracking lane changes and
  attack timing. A player-following decorative orb might conceal the small
  Knight or an incoming strike; default to compact edge docking.
- Model concrete attack/rest states only after hook validation and frame
  annotation. Game action detection != verified safe window.
Source: https://hollowknight.wiki/w/Nightmare_King
https://hollowknight.wiki/w/Pure_Vessel

### Silksong: mobile needle combat and recovery competition
- Lace's documented advancing slashes, dive, lunge, parry stance and AoE
  show why a generic "enemy near" trigger overstates useful time windows.
- A boss stagger in Silksong is an opportunity for **Bind or offense**.
  It cannot be reserved for an EEG selection unless player testing
  demonstrates that attention competition is tolerable.
- Cross Stitch is a manually timed Silk counter and must remain untouched.
- Suggested active BCI mechanic: bank a choice at a deliberate safe
  pre-encounter station, then display a low-profile, static in-combat
  state. Reserve direct in-fight SSVEP only for independently qualified
  game-specific intervals.
Source: https://hollowknight.wiki/w/Combat_(Silksong)
https://www.gamesradar.com/games/action/silksong-lace-boss-guide/
https://hollowknight.wiki/w/Cross_Stitch

### Cuphead: hard visual-attention exclusion zones
- Grim Matchstick demands tracking cloud platforms, eye ripples,
  fireballs and later multiple fire-head patterns. The **platform region
  itself is critical gameplay information**, so screen-edge placement
  must be adapted to the occupied side, not always placed at bottom-right.
- Dr. Kahl's Robot combines simultaneous laser/drones/parry requirements
  and later radial gems + walls in aircraft combat. Visual targets
  should default OFF for the whole active encounter, not just
  a handful of "high threat" event flags.
- The game features mode-specific control and screen-space
  distributions (run-and-gun, platform boss, plane/shmup,
  King Dice segment). These must be separate context profiles.
- Never recolor or obscure pink parry objects, flight hazards,
  EX gauge, health HUD or the stage's moving platform cues.
Sources: https://cuphead.wiki.gg/wiki/Grim_Matchstick
https://cuphead.wiki.gg/wiki/Dr._Kahl%27s_Robot

## Annotation rubric for true footage analysis

For each continuous clip, record in a versioned JSONL/CSV:
- game build, boss/phase, difficulty, mode, display FPS, video FPS, editor;
- timecode start/end, source-clock uncertainty, whether edits/drop frames exist;
- telegraph onset, attack onset, first required movement, active hitbox,
  projectile density, occupied screen regions, safe visibility regions;
- player grounded/airborne, dash/parry/heal/Bind use, mistakes and damage;
- boss active/staggered/transitioning, **verified** not guessed;
- projected player bbox and enemy/projectile bbox across consecutive frames;
- potential lower-bound distraction-free interval, with confidence and
  whether it survives gaze travel + VEP accumulation + return-to-combat;
- expected BCI ability usability, expected perceptual interference,
  observed overlap with HUD and accessibility risks.

Do not mark a window "safe" based on high-skill no-hit clips alone.
Pair skilled play with first attempts, unsuccessful attempts,
different monitors, reduced-motion preferences and game-speed settings.
An offline video cannot prove psychophysics or active EEG efficacy.

## Revised design gates for Mindforge

1. **Game action telemetry**: allow only semantics directly observed from
   audited hooks. A hero attack never equals `enemy_engaged=true`.
2. **Visual presence**: primed, present or docked indications may be shown
   conservatively as cosmetic UI; suppress on death/focus/pause/stale context.
3. **SSVEP candidate**: require verified remaining no-threat duration,
   scene-specific availability, player-specific measured calibration and
   luminance evidence. No new stimulus authorized in current mods.
4. **Active mode**: prefer precharged choices and conventional manual
   triggering rather than visually demanding last-second combat selections.
5. **Passive mode**: if opted in, experiment with noncritical ambient
   decoration only, suppress in active SSVEP and dangerous combat. The
   8–13Hz feature overlaps the existing 10/12Hz SSVEP stimuli and is not
   a reliable stress/focus/emotion classifier.
6. **Evidence**: use instrumented native captures and frame-by-frame
   analysis of *owned game recordings* before changing hook state mapping.

## Most valuable next experiment

Record the same encounter with Mindforge context orbs disabled/enabled,
using a no-headset synthetic decision source and verified passive game hooks.
Compare trace parity, projectile occlusion, missed telegraphs, hit timing and
frame pacing. Establish a safe overlay first. EEG-on tests come after.
