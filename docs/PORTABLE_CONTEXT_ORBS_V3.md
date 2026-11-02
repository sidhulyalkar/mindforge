# Portable contextual orbs v3: real transport, conservative render

## Status / exact scope

This branch is stacked **on draft PR #66**, itself stacked on PR #65.
Python policy + UDP sidecar and Unity/BepInEx source are implemented. Native
Silksong/Cuphead builds, actual player/game-state hooks, installation QA,
photodiode verification and human EEG findings are **not** claimed.

The implemented v3 bridge takes *declared* game-context data and produces
a presentation-only UI state. No transport packet can request an attack,
parry, hit-stop, health change, input injection, SSVEP flicker, or enemy edit.

## Architecture and dedicated localhost ports

```text
Python SSVEP decision (or explicit simulated source)
  | neural_event.v2
  +---- 127.0.0.1:19752
                    |
              mod_bridge.py
                    |
          127.0.0.1:19753
                    |
                 BepInEx --------> static Sight / Guard appearance

Game-specific semantic hook [NOT YET IMPLEMENTED]
  | game_context.v1 (derived game facts only)
  +---- 127.0.0.1:19754
                    |
       mindforge_orb_context_bridge.py
       ContextOrbPolicy:
       source/session/sequence/TTL + threat vetoes
                    |
          127.0.0.1:19755
                    |
                 BepInEx --------> hidden / primed / present /
                                  docked / resting + safe UI placement

Independent passive Living World research lane (PR #64):
       derived EEG feature -> 127.0.0.1:19747
       NOT installed into the external game mod by this PR.
```

Each UDP socket is bound to IPv4 loopback only. The ports are separate so
listeners never race on the same datagram queue. Both mod display channels
require source freshness. Real EEG data **never** enters the mod process.

The host process still has no cryptographic authentication on localhost;
this is intended only for a trusted single-user development PC.

## What the data means

Input `contracts/game_context.v1.schema.json` includes:
`game_id`, `session_id`, `seq`, `source`, `ttl_ms`, an explicit
`context` object, and optional normalized on-screen player coordinates.
The context reports enemy approach, engagement, boss encounter, incoming
attack, dangerous precision window, recovery opening, scene transition,
pause, death and focus.

`source=synthetic_game_context` means test fixture, NOT gameplay observation.
`source=verified_game_hook` may only be used **after** a native game-specific
hook has been inspected and tested against a pinned installed game version.
The source label is a declaration, not a security credential.

The output `contracts/orb_context_state.v1.schema.json` has
`presentation_only=true` and `stimulus_enabled=false` always.
`selection_opportunity_only` is a **candidate** designer window, not
a command to create a visual flicker or to award a buff.

The sidecar rejects wrong games, invalid flags, mixed player-coordinate
types, nonfinite values, duplicate/out-of-order sequences, unexpected
fields and interleaved sessions. It withdraws appearance when context
expires (up to 750ms input TTL; default outgoing TTL 250ms).

The UI additionally enforces its own receive-clock TTL, app focus and
paused Unity time veto. Normalized coordinate origin is the **top-left
of the viewport**. Player-follow placement is allowed only for a declared
verified semantic hook with both finite coordinates; absent a grounded
player anchor, the UI stays edge-docked and smooth, never guesses
the player transform or player collider.

## Quick start: no headset or installed game required

From repo root:

```bash
python -m pip install -e '.[test]'
python -m pytest -q tests/test_context_transport.py tests/test_context_bridge_udp.py
python -m pytest -q tests/test_combat_context.py tests/test_passive_mod_policy.py

# Terminal A: cosmetic Sight/Guard simulation, output on :19753
python tools/mindforge_mod_bridge.py --game silksong --demo --seconds 24

# Terminal B: synthetic combat context, output on :19755
python tools/mindforge_orb_context_bridge.py --game silksong --demo --seconds 24
```

Both programs can be run with `--game cuphead` instead. The Python demo
does not need a running game; it reports changing modes in the terminal.
To see them in the game, locally build and install the BepInEx plugin using
the previous `docs/PORTABLE_GAME_MODS_V1.md` instructions. V3 adds the
`RequireCombatContext=true` overlay setting by default, so **without**
fresh context, no orbs will appear (unlike the v1 always-visible experiment).

Note: the simulation and contextual-demo use different UDP channels. A
"simulated decision" and a "synthetic game event" are different assertions.
Do not publish these recordings as measured brain-controlled combat.

## Native game integration: what is missing and how to add it

**Silksong:** identify an actual combat/scene/player hook from an appropriate
BepInEx plugin. Verify animation transitions for hornet movement, Bind,
parry, boss threat and safe boss down-time in a pinned build. A game hook
should send only semantic facts. **Do not** patch health, silk, damage or
ability timing for v3. Be careful that the UI follower does not cover
platform edges or opponent telegraphs.

**Cuphead:** the optional status stays edge-docked even during normal
fights. Verify actual boss phase and projectile danger state rather than
just reading the existence of bullet GameObjects. Pink parry cues and
flight sections must override any candidate neural selection window.
No projectile direction, collision or damage modification is included.

**Hollow Knight:** a separate adapter using hk-modding/API is required.
The shared Python ContextOrbPolicy already accepts `hollow_knight`,
but the BepInEx plugin deliberately does not claim Hollow Knight
compatibility. Boss stagger/phase and safe heal windows need explicit
version-qualified native hooks.

Game-specific engine code must be isolated from Mindforge's Python
decoders. Expected project seam:

```text
IGameSemanticSource (per-game implementation)
 -> versioned GameContextPacket
 -> fixed localhost context port 19754
 -> shared policy and cosmetic state port 19755
```

A plugin may send context at 10–20Hz and should explicitly emit a hidden
event on pause/death/transition rather than relying solely on expiry.
However, the TTL fallback must always work if the game process crashes.

## Combat timing and user-experience policy

- **Exploration:** fully hidden; do not crowd the original art.
- **Enemy proximity:** primed but not selecting; proximity alone never
  triggers coded flicker.
- **Combat:** readable static status; a long safe opening may be flagged
  as a possible selection opportunity.
- **Incoming telegraph / precision platforming / parry:** switch to
  smaller edge-docked indicator and disable new selection opportunities.
- **Boss stagger:** candidate only if a verified opening is sufficiently
  long; the current 2s threshold is a provisional UX placeholder, not
  proof of calibrated decoding or display safety.
- **Pause, death, cutscene, focus loss, stale context:** hide.
- **Finish:** short resting appearance then hide.

A fixed overlay can cover a HUD or a projectile, so the edge anchor must
eventually be configurable per game, resolution, safe-area and accessibility
mode. This draft does not claim that the default anchor is optimal.

## Passive BCI is separate and remains opt-in

The existing experimental posterior 8–13Hz ratio is not a stress,
attention, workload or enjoyment detector. Because the active SSVEP
targets use 10Hz and 12Hz, this band is contaminated during coded
stimulus windows. Keep passive modulation withdrawn during active SSVEP,
calibration, and dangerous combat.

Next prototype: wire the already-versioned passive feature protocol to
a **separate, off-by-default decorative renderer**, preserving independent
sequence/session/TTL gating and a user-specific baseline, then run paired
controller-only gameplay tapes with passive off/on. The success criterion
is unchanged gameplay semantics and acceptable frame-time cost, not a
claim that passive EEG makes anyone enjoy the game more.

After the renderer works, compare participant enjoyment against
both static controls and yoked non-contingent animations in repeated,
counterbalanced sessions.

## Qualification checklist (must pass before merge)

1. Latest exact-head Python CI and UDP tests green, including malformed
   inputs, sequence/session interleaving, timeouts, threats and hard vetoes.
2. Build native BepInEx DLL separately for Silksong and Cuphead from
   locally installed game assemblies; inspect loader/compile logs.
3. Confirm context demo renders hidden/primed/present/docked/resting,
   supports window resize, and never shows periodic SSVEP flicker.
4. Kill either Python source independently: BCI ability feedback goes
   stale without pretending an accepted choice; game-context loss
   hides the orbs independently, with no adverse native-game effects.
5. Validate real native context events and player coordinates in an
   actual recorded scene before setting `verified_game_hook`.
6. Measure frame-time overhead, 1% lows, collision/hit/parry/gameplay
   parity, scene reload behavior and complete uninstall cleanliness.
7. Only after physical stimulus qualification and consent may a later
   separate adapter propose active BCI-driven game abilities.

No copyrighted game DLLs or assets should be checked into this repo.
