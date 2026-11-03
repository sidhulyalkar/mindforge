# Mindforge v7: Cuphead native stage observation and Mono compatibility

## Evidence status

This branch implements a **read-only Cuphead stage observer** as a distinct
BepInEx project, plus a game-specific, strict Python contract and audit CLI.
It is NOT yet native-game-qualified: compilation against installed Cuphead
DLLs, gameplay screenshots, logs, loader behavior and timing remain unobserved.
Neither a passing Python test nor assembly hashes can establish those facts.

Mindforge's previous Silksong/Hollow Knight `game_observation` event stream
is intentionally **not** used here. We do not have a qualified Cuphead
attack, boss, parry or player-movement event hook.

## Why this is a separate integration

Community Cuphead source provides specific evidence for two important design
choices:

- `Level.Current` and `Map.Current` are accessed in the
  [Cuphead4PFixes JoinGate](https://github.com/Gergofi/Cuphead4PFixes/blob/main/src/JoinGatePatch.cs).
  These are candidate read-only stage markers, NOT guaranteed in every
  installed version or every modified game.
- Its [build project](https://github.com/Gergofi/Cuphead4PFixes/blob/main/Cuphead4PFixes.csproj)
  documents the Cuphead Unity 2017.4/legacy Mono need for **net35**.
  The previous Mindforge PortableOrbs project targeted net46 by default.
  We now support `-p:MindforgeGame=cuphead` -> net35 and
  `-p:MindforgeGame=silksong` -> net46, each built against its own
  locally installed BepInEx and Unity assemblies. This does not
  establish native loading until verified on the game computer.

Cuphead's scene categories are radically different: the overworld,
run-and-gun stages, grounded boss battles, and airplane/shmup boss battles
must not share a premature "safe to stare at EEG orb" rule. Even a level
being loaded is insufficient evidence that a boss fight is happening.

## Dataflow

```text
Cuphead game (owned, mod loader manually installed)
   BepInEx 5 Mono source (Enabled=false by default)
     public Unity SceneManager.activeSceneChanged
     Level.Current / Map.Current (read by reflection at 2Hz)
       |
       v
  127.0.0.1:19757       127.0.0.1:19758
    | audit                      | cosmetic veto only
    v                            v
  tools/mindforge_            PortableOrbs' CupheadStageReceiver
  cuphead_stage.py            strict sequence/session/TTL checks
    |                            |
    v                            v
  JSONL audit                  IsPlayableLevel only if fresh,
  reports                      Level.Current and no pause/focus loss
                                    |
                              AND independent contextual policy
                                    |
                              static, cosmetic UI only
```

**Cuphead stage observations can only veto the orb UI.** The optional
overlay's `[Overlay] RequireCupheadPlayableStage=true` default
requires a fresh observed `level` stage *and* the existing independent
orb-context policy. Overworld, transitions, missing stage source, an
offline observer or focus/pause changes hide the visual. A level being
loaded NEVER independently creates an orb, triggers Sight/Guard, changes
damage or alters Cuphead physics.

Audit and UI receive separate local ports (19757 and 19758) to avoid two
subscribers stealing each other's UDP datagrams. No raw EEG enters the
Cuphead stage observer. If the native plugin fails to load, the default
UI gate remains closed. Disable the stage prerequisite only for an
explicitly labeled cosmetic-simulation development session, never as
evidence of native game compatibility.

The native plugin reads `Level.Current` and `Map.Current` at most
twice per second. It caches reflection members and maps conflicting,
missing, throwing or simultaneously-active observations to
`stage=unknown`. Unknown, focus loss, pause, stale source and scene
transition must never be interpreted as a playable combat scene.
No Harmony patches are installed in this adapter.

### Validation and semantics

- `game_id=cuphead`, explicit `game_version`, unique session ID,
  monotonic sequence and source timestamp.
- `stage` is `unknown`, `map`, or `level`.
- `event` is `sample` or `scene_changed`; a transition always
  emits `stage=unknown`.
- Source is `observed_stage_reflection_unqualified` in native mode,
  `synthetic_stage_observation` in the no-game demo.
- `boss_phase_verified=false`, `combat_engagement_verified=false`,
  `parry_window_verified=false` and `ssvep_stimulus_enabled=false`
  in the Python report, irrespective of stage.
- A missing source, invalid packet, interleaved session, backwards source
  clock, duplicate sequence, or stale timestamp does not result in
  an "active level" claim.

## Run Python without Cuphead

```bash
python -m pip install -e '.[test]'
python -m pytest -q tests/test_cuphead_stage.py tests/test_cuphead_stage_udp.py
python tools/mindforge_cuphead_stage.py --demo
```

This prints synthetic map/level transitions only. It is not a gameplay
recording or an EEG demonstration.

## Run on the owned Windows Cuphead installation

Do not replace game-managed DLLs, commit copyrighted game binaries, or
automatically install artifacts as a post-build action. Back up saves.

1. Confirm Cuphead is installed and BepInEx **5 for legacy Mono** is
   compatible with this installed Cuphead version; do not copy the
   Silksong mod loader blindly.
2. Run the read-only assembly preflight (this does not install anything):

```powershell
$gameRoot = "C:\Program Files (x86)\Steam\steamapps\common\Cuphead"
python tools/mindforge_native_preflight.py --game cuphead --game-root $gameRoot --report cuphead-preflight.json
```

3. Build Cuphead's separate BepInEx stage observer against the local
   game references:

```powershell
$env:CupheadManagedDir = Join-Path $gameRoot "Cuphead_Data\Managed"
$env:CupheadBepInExCore = Join-Path $gameRoot "BepInEx\core"
dotnet build mods/bepinex/Mindforge.CupheadStageObservation/Mindforge.CupheadStageObservation.csproj -c Release
```

4. If required for a later cosmetic-only overlay experiment, **build the
   existing PortableOrbs source separately for Cuphead**. This is a
   net35 build, not the Silksong net46 artifact:

```powershell
$env:GameManagedDir = $env:CupheadManagedDir
$env:BepInExDir = $env:CupheadBepInExCore
dotnet build mods/bepinex/Mindforge.PortableOrbs/Mindforge.PortableOrbs.csproj -c Release -p:MindforgeGame=cuphead
```

Do **not** install or enable PortableOrbs during the *first*
stage-only qualification. The context renderer's gameplay semantics
still need independent Cuphead HUD/visual-interference testing.

For a **second, explicitly synthetic and cosmetic-only integration test**
after the native stage stream is confirmed, install the Cuphead-built
PortableOrbs DLL separately and leave
`RequireCupheadPlayableStage=true` and `RequireCombatContext=true`.
Then run:

```powershell
# Terminal 1: synthetic combat-context decisions only, no EEG
python tools/mindforge_orb_context_bridge.py --game cuphead --demo --seconds 24
# The native stage observer sends its real unqualified stage state on :19758.
```

The overlay should stay hidden on Cuphead's map and through transitions
despite synthetic combat context. It may display a dim labeled **SIM
CONTEXT** orb in an observed active level, but that is not a safe
selection window and does not prove a physical visual stimulus or actual
neural control. Return to the map, pause, close the native observer and
verify it withdraws within its 1.25s local source TTL.

If the game version requires an incompatible .NET or Unity assembly,
stop here. Do not force-load the net46 Silksong binary into Cuphead.

5. Install ONLY the compiled
   `Mindforge.CupheadStageObservation.dll` into the BepInEx plugins
   directory as directed by the supported loader. Its config defaults
   to `[Observation] Enabled = false`. Explicitly set it to `true`
   and restart the game for the read-only trial.
6. Inspect the BepInEx log for the observed `Application.version` and
   `Level=True, Map=True` reflection discovery markers. This is
   a compatibility observation, not proof of reliable game semantics.
   If either member is missing or the mod fails to load, disable the
   plugin rather than inventing a substitute class name.
7. Using the exact version from the plugin log, start the observer:

```powershell
python tools/mindforge_cuphead_stage.py --seconds 180 --expected-build "<OBSERVED-CUPHEAD-VERSION>" --output cuphead-stages.jsonl --report cuphead-stage-audit.json
```

Walk the overworld, enter a level, pause, transition and return to map.
Compare `cuphead-stages.jsonl` to a continuous screen capture. Verify
every scene-change event becomes `unknown`, no duplicate/out-of-order
events are accepted, and no stage label is shown as a confirmed boss phase.

8. Disable `[Observation] Enabled` and restart. Verify completely
   unchanged Cuphead controls, loading transitions, boss scripting,
   parry actions, frame pacing and saves.

## Next scientific and product qualification

The next game-native hook should be **one confirmed player action**,
preferably a read-only parry-success event, identified in a pinned
Cuphead build. The independent
[Cuphead local haptics source](https://github.com/omrlw/cuphead-local-haptics/blob/main/src/CupheadHapticsPlugin.cs)
mentions candidate `LevelPlayerMotor.OnParrySuccess` and
`ArcadePlayerMotor.OnParrySuccess` events, but their existence,
delegate signatures and meaning in the owned installed game have
**not** been verified here. Do not subscribe to a guessed event or
infer successful parry from a pink object in video.

Future evaluation should split (a) run-and-gun, (b) grounded boss,
(c) plane/shmup, and (d) map. Start with no overlay, then non-flickering
edge feedback. Only after independent visual and EEG validation should
any active visual selection or timing-critical game mechanic be considered.

All three games ultimately share a **derived semantic protocol** but not
native hook names, game binaries, loader assumptions, or automatically
inferred tactical states.
