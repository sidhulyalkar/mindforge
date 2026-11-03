# Mindforge v5: read-only Silksong action -> cosmetic orb preview

**Status:** source-level native plugin + tested Python transport. The native
BepInEx assemblies still need to be built and run on an installed Silksong
version. No observed in-game playback, boss-phase hook, EEG classifier result,
SSVEP stimulus, or actual combat buff is claimed.

## Why this slice exists

Earlier PRs #65–#68 provided a visual overlay, contextual presentation
policy, and unqualified read-only Silksong event monitor. They were not
connected. This PR adds a conservative **off-by-default diagnostic preview**
without falsely upgrading raw player actions into combat or boss facts.

- Two observed `HeroController.Attack` calls within 1.2s produce a short
  `player_activity_recent` observation. The orb is `primed` on the edge
  of the viewport for at most 0.65s after the last action.
- A single attack, jump, dash or heartbeat never triggers it.
- Damage, death, scene transition, pause, loss of app focus, source timeout,
  and missing `HeroController.Start` readiness withdraw it.
- This is *player action telemetry*, **not** confirmed fighting. The player
  may simply be swinging their needle while exploring. UI labels must
  display that caveat, not `BOSS ENGAGED`.
- No SSVEP target, shield, boost, auto-parry, jump, damage or game
  difficulty changes are emitted by this pipeline.

## Port and data pipeline

```text
Silksong BepInEx/Harmony read-only plugin (manual opt-in)
         |
         | mindforge.game_observation.v1
         | source=observed_game_hook_unqualified
         v
 UDP 127.0.0.1:19756
         |
 tools/mindforge_activity_preview.py [manual start, exact game-version pin]
         | two fast attacks -> player_activity_recent
         | source=observed_game_hook_unqualified
         | NO enemy / boss assertions, NO player anchor
         v
 UDP 127.0.0.1:19754
         |
 tools/mindforge_orb_context_bridge.py
         | mindforge.orb_context_state.v1
         | mode=primed (otherwise hidden)
         | presentation_only=true, stimulus_enabled=false
         v
 UDP 127.0.0.1:19755
         |
 Existing BepInEx PortableOrbs plugin (opt-in overlay config)
         |
 Small static dimmed edge indicator, "PLAYER ACTIVITY ONLY" label
```

The existing separate neural feedback process uses ports 19752/19753, and
passive research features use 19747. **Do not run two listeners on port
19756**. The original `tools/mindforge_game_observe.py` is an alternate
read-only auditor; stop it before starting this preview service.

Both publishers use IPv4 loopback only. It is not authenticated and is
intended for a trusted local development machine, **not** multiuser network
use. Neither raw EEG nor game assets are transmitted.

## First: run Python synthetic walkthrough

From repository root on Python 3.10+:

```powershell
python -m pip install -e ".[test]"
python -m pytest -q tests/test_activity_preview.py tests/test_activity_preview_udp.py

# Terminal 1: context policy -> UI transport
python tools/mindforge_orb_context_bridge.py --game silksong

# Terminal 2: synthetic native-like events -> context policy
python tools/mindforge_activity_preview.py --game silksong --demo --seconds 6
```

The printed source must be `synthetic_game_context`. The context sidecar
will show a `primed` transition during two quick synthetic attacks and
return to `hidden` on damage, a scene change or death. This demonstrates
software wiring only, **not live game compatibility**.

## Native Windows test with owned installed Silksong

1. Pin the installed Silksong build and compatible BepInEx **5 Mono**
   loader. This code is not an IL2CPP mod and has not been tested on an
   unknown version. Copy and back up your save data first.
2. Build the two distinct projects against that **same game's own**
   managed assemblies. Set `BepInExDir` and `GameManagedDir` on your
   machine, not inside the repository:

```powershell
$gameRoot = "C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight Silksong"
$env:BepInExDir = Join-Path $gameRoot "BepInEx\core"
$env:GameManagedDir = Join-Path $gameRoot "Hollow Knight Silksong_Data\Managed"

dotnet build mods/bepinex/Mindforge.SilksongObservation/Mindforge.SilksongObservation.csproj -c Release
dotnet build mods/bepinex/Mindforge.PortableOrbs/Mindforge.PortableOrbs.csproj -c Release
```

Resolve any Unity assembly or game-version mismatch *before* installing.
Do not check copyrighted managed assemblies or plugin binaries into Git.
Copy each compiled DLL into its separate game `BepInEx/plugins/` folder,
following the game loader's compatible installation instructions.

3. Launch the game once. In the BepInEx configuration for
   `dev.mindforge.silksong.observation`, set
   `[Observation] Enabled = true`. Leave the cosmetic overlay setting
   `[Overlay] RequireCombatContext = true`. Restart Silksong after
   changing the observation setting. No plugin writes to saved game state.
4. First record **only** read-only telemetry, with no context preview
   process, to confirm the actual `Application.version` and hook counts:

```powershell
python tools/mindforge_game_observe.py --game silksong --seconds 45 --output observations.jsonl --report observations-audit.json
python -c "import json; print(json.loads(open('observations.jsonl', encoding='utf8').readline())['game_version'])"
```

The version is a metadata string, not a binary digest or complete plugin
compatibility proof. Review `BepInEx/LogOutput.log`: every method hook must
resolve uniquely, and callback counts must match footage of the same play
session. If `HeroController.Start` is absent, the preview remains hidden.

5. Stop the auditor. Launch two terminals for the actual cosmetic
   preview, substituting the EXACT `Application.version` read above:

```powershell
# Terminal 1
python tools/mindforge_orb_context_bridge.py --game silksong

# Terminal 2
python tools/mindforge_activity_preview.py --game silksong --seconds 300 --expected-build "<YOUR-OBSERVED-APPLICATION-VERSION>" --tape observations-preview.jsonl --report observations-preview-audit.json
```

Run Silksong. Two quickly observed attacks should briefly produce small
edge-docked cosmetic orbs with the label `PLAYER ACTIVITY ONLY`. They
should not appear merely on jump, dash, walking, or camera movement. Note:
without a separate neural feedback source, the BCI link is legitimately
offline. The preview itself does not produce Sight/Guard selections.

6. Verify **fail-quiet** behavior: pause, alt-tab, player damage, death,
   scene change, and killing either Python process. Confirm no lingering
   visible orbs, game action changes, added input lag or corrupted saves.

## Design constraints and evidence ladder

A v5 packet may be `source=observed_game_hook_unqualified`, and its
`player_activity_recent` flag may be true. The Python schema and policy
REJECT that source asserting ANY of:
`enemy_nearby`, `enemy_engaged`, `boss_encounter`,
`incoming_attack`, `precision_window`, `platforming_hazard`,
`recovery_opening`, nonzero recovery time, or a player-following anchor.

The BepInEx receiver independently rejects unqualified sources claiming
`present`, `docked`, `player_anchor`, or an active selection opportunity.
Actual SSVEP stimulus is always `false`. The current game owns all
attacks, healing, Bind, dodging and jumps.

This diagnostic is deliberately **not** the final mechanic. Repeated
ordinary attacks can occur during exploration. The next scientifically
meaningful milestone is to instrument a ground-truth enemy/scene event in
the user's installed build, compare its timing against a recorded boss
encounter, and only then qualify one semantic `enemy_engaged` or
`boss_encounter` claim. Camera-safe overlay placement must also be tested
on actual footage at multiple aspect ratios.

## Follow-on integrations

**Hollow Knight:** independent adapter using the Hollow Knight Modding API
and explicit cleanup/unsubscribe, not the Silksong Harmony plugin.
Document available AttackHook semantics, boss/scene transitions, and
screen projection from pinned installed versions before marking context
verified.

**Cuphead:** independent BepInEx adapter, with distinct plane/shmup,
run-and-gun, and boss-combat presentation modes. Default to edge-only
status and no new coded visual choices during projectile-heavy sequences.
Never suppress pink parry readability or moving-platform sightlines.

**Passive BCI:** keep the existing research feature channel separate.
Avoid interpreting 8–13 Hz posterior alpha as stress or enjoyment, especially
during the 10/12 Hz active-SSVEP stimulation windows. Its eventual
decorative renderer remains opt-in, physically qualified and incapable of
changing gameplay semantics.

## Release/merge gates

- Exact-head Python CI green, including unit regression and two-process UDP
  simulation with real loopback sockets.
- Native BepInEx DLLs compile and load on installed pinned Silksong build.
- Verified observation callbacks and video-to-event correspondence.
- Source kill, port conflict and game pause are fail-quiet.
- Visual capture at 16:9, 16:10 and a narrower viewport; HUD and
  telegraphs remain unobstructed.
- Absent/uncertain source data never creates false boss/safety claims.
- All settings can be disabled and DLLs removed without affecting native
  game state.

Stacked PR order: #65 -> #66 -> #67 -> #68 -> this PR. Keep drafts until
native game behavior and display conditions are verified.
