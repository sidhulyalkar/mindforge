# Mindforge v6: Hollow Knight read-only Modding API + cosmetic activity HUD

**Scope:** source-level second-game integration on
`feat/hollow-knight-readonly-observer-v6`, stacked above #70.
Native game compilation and runtime behavior are **unverified**.
The Python protocol and localhost smoke tests run without game installation.

## Purpose and architecture

Hollow Knight has a distinct Modding API. We must NOT inject the Silksong
BepInEx Harmony patch DLL into it or mislabel game-hook provenance.

```text
Hollow Knight modding API [opt-in marker, actual installed game]
   ModHooks.HeroUpdateHook   -> hero_ready / heartbeat
   ModHooks.AttackHook       -> hero_attack
   SceneManager.activeSceneChanged -> scene_changed
          |
          | mindforge.game_observation.v1
          | source=observed_game_hook_unqualified
          v
 127.0.0.1:19756
          |
 tools/mindforge_activity_preview.py --game hollow_knight
          |
          | mindforge.game_context.v1
          | player_activity_recent only, no boss/combat claims
          v
 127.0.0.1:19754
          |
 tools/mindforge_orb_context_bridge.py --game hollow_knight
          |
          | mindforge.orb_context_state.v1
          | only hidden/primed, no SSVEP, no gameplay authority
          v
 127.0.0.1:19755
          |
 Hollow Knight Modding API overlay component
          |
 small static cyan player-activity diagnostic, TTL gated
```

The two sources are intentionally one-way. Incoming UI packets cannot
modify native game state. The optional overlay exists only when the
mod's opt-in file is present; removing that file and restarting disables
the whole observer.

## The first honest event vocabulary

Only these events are emitted by the HK native observer:

| Event | Actual HK callback | Permitted meaning |
|---|---|---|
| `hero_ready` | `ModHooks.HeroUpdateHook`, first after scene | Hero controller update observed |
| `heartbeat` | `ModHooks.HeroUpdateHook`, rate-limited 2Hz | Game's hero-update callback is still running |
| `hero_attack` | `ModHooks.AttackHook` | Player initiated an attack, **not an enemy hit** |
| `scene_changed` | `SceneManager.activeSceneChanged` | Unconditional context withdrawal |

**Never emit** `hero_dash`, `hero_damaged`, `hero_die`, boss stagger,
`incoming_attack`, `enemy_engaged`, or safe opening until an actual
supported event or observed runtime state has been independently qualified.
The absence of a death hook in this version is an explicit limitation:
we depend on scene-change, pause/focus, and freshness timeout until
a direct death signal is verified.

The fields are strictly validated per game by
`GameObservation.from_dict` and the shared
`contracts/game_observation.v1.schema.json`. Silksong's Harmony names
remain unchanged, and the HK source uses its real ModHooks names. The
viewer only accepts hidden/primed activity output, and never draws a
player-following stimulus or claims an EEG command.

## Test without installing the game

From repo root (Python 3.10+):

```bash
python -m pip install -e '.[test]'
python -m pytest -q tests/test_hollow_knight_observation.py tests/test_hollow_knight_udp.py

# Terminal 1: derived context -> cosmetic state on UDP 19755
python tools/mindforge_orb_context_bridge.py --game hollow_knight

# Terminal 2: simulated ModHooks events -> context on UDP 19754
python tools/mindforge_activity_preview.py --game hollow_knight --demo --seconds 6
```

The demo only proves Python data flow. Its source is explicitly
`synthetic_game_context`, **not** an observed player or a trained neural
decoder. No game is required for the Python tests.

## Native Windows installation and opt-in

1. Use an **owned, compatible Hollow Knight installation** and install
   the current supported **Hollow Knight Modding API** using its documented
   installer/manager. The Modding API is a different loader from
   BepInEx. See
   https://github.com/hk-modding/api and
   https://hk-modding.github.io/api/ .
   Back up saves and test offline or in an allowed modding environment.
2. Run the non-mutating prerequisites check and save the report:

```powershell
$gameRoot = "C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight"
python tools/mindforge_native_preflight.py --game hollow_knight --game-root $gameRoot --report hollow-knight-preflight.json
```

`build_reference_prerequisites_present=true` only means local files
exist, **not** that the Modding API is installed or compatible.

3. Build using the **actual modded game's** `Assembly-CSharp.dll` and
Unity assemblies:

```powershell
$env:HollowKnightManagedDir = Join-Path $gameRoot "hollow_knight_Data\Managed"
dotnet build mods/hollow-knight/Mindforge.HKObservation/Mindforge.HKObservation.csproj -c Release
```

If the compiler cannot resolve `Modding.Mod`, `ITogglableMod`, or
`ModHooks.AttackHook`, **do not fake references** or rename arbitrary
game methods. Inspect your installed API and its version.

4. Install the resulting `Mindforge.HKObservation.dll` in the mod
   directory recognized by your *installed* Modding API manager. Do not
   overwrite game-managed DLLs manually. The exact folder may depend
   on the manager/version; verify the active mod assembly path in the
   mod loader's log.
5. The mod is disabled by default. To deliberately enable the read-only
   diagnostic, create a plain text file named
   `Mindforge.Observer.optin` **in the same actual directory as the DLL**
   containing exactly:

```text
ENABLE_READ_ONLY_GAME_TELEMETRY_V1
```

Restart Hollow Knight. No GUI setting silently enables it. The marker
can be removed and the game restarted to opt out, or the mod can be
uninstalled through its manager. No raw EEG or game saves are written
by this mod.

6. **First audit only** (do not run the preview service simultaneously,
   since they both receive port 19756). Start the Python monitor before
   launching/entering the game:

```powershell
python tools/mindforge_game_observe.py --game hollow_knight --seconds 60 --output hk-observations.jsonl --report hk-audit.json
```

Compare callback counts with a video of the **same gameplay session**.
Check `game_version` in the first record and validate that the observed
API callback names match the game and its installed mod loader. Attack
counts must not be confused with hit counts.

7. **Then try the in-game static orb** with exact build pin. Stop the
   auditor, launch two terminals:

```powershell
# Terminal A: policy output and context TTL enforcement
python tools/mindforge_orb_context_bridge.py --game hollow_knight

# Terminal B: unqualified native events -> player activity preview
python tools/mindforge_activity_preview.py --game hollow_knight --seconds 300 --expected-build "<YOUR-OBSERVED-APPLICATION-VERSION>" --tape hk-preview.jsonl --report hk-preview-audit.json
```

Two legitimate attack callback events within the provisional 1.2s
interval cause a small, dim, static edge indicator for at most 650ms
after the last attack. No jumping, dashing, player movement or enemy
proximity is guessed. The indicator is labeled
`PLAYER ACTIVITY ONLY`. It is not an SSVEP target and not a spell,
shield, parry or buff.

## Fault-injection and native qualification

- No opt-in file -> no native ModHooks subscriptions or UI.
- Missing/mismatched API assembly -> build fails rather than guessing.
- Pause/focus loss -> native OnGUI veto plus neutralized Python context.
- Scene change -> observer emits a scene event and locally suppresses
  the HUD until a new hero-update readiness event.
- Stop either Python process -> stale packets naturally hide the marker.
- Restart the game/observer -> new session prevents old sequence reuse.
- Duplicate/reordered packets -> monitor and cosmetic receiver reject.
- Wrong game ID, unqualified safe-window claims, or old TTL -> reject.
- Unity fullscreen/windowed, different aspect ratios and native
  30/60/120 FPS need actual video and frame-time evidence.
- Test user death in the same scene, where no direct death hook is
  currently qualified: the preview might persist briefly if hero
  updates continue. Treat this as an outstanding native failure gate.
- Verify fully reversible uninstall, including removal of the opt-in
  file and diagnostic game object.

No real game build is shipped. Source-level C# compatibility can only
be promoted after compiling against the user's pinned local files.
Python CI passing does not prove the native assembly runs.

## Next expansion: Cuphead

The native `mindforge.game_observation.v1` contract explicitly excludes
Cuphead until a real Cuphead API callback is inspected, compiled and verified.
Its cosmetic `game_context` simulation remains supported independently.
This prevents accidentally presenting a Silksong Harmony callback as Cuphead
evidence.

Cuphead needs **another genuine native observer**, not Hollow Knight
ModHooks. Plan three categories of scenes: grounded boss phases,
airplane/shmup phases and run-and-gun traversal. Restrict to read-only
player and stage-mode events first. A colored/pink parry projectile
must never be covered, recoded, or guessed from audio. Do not declare
an SSVEP opportunity just because the boss is temporarily motionless.

## Scientific and product direction

The next release-worthy demonstration is two *separately owned* game
installations with reproducible **visual-only** passive activity
indicators, no-gameplay regressions, and valid source provenance. Then
annotate full encounters for boss hitbox, parry, screen occlusion and
usable gaze intervals. Only after real EEG, physical-display timing,
and consent gates may a separate later experimental build include
coded neural selection and genuine gameplay abilities.
