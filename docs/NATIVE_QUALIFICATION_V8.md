# Mindforge native-qualification sprint (compilation, telemetry, gameplay)

**No runtime claims.** The Python, UDP, data-contract and source tests
demonstrate software wiring only. The game-specific C# mods have not been
compiled or run against the user's installed game assemblies in GitHub CI.
A native game build, a mod-loader handshake and a recorded encounter are
separate gates that cannot be inferred from each other.

This sprint works on the **existing PR #72 stack**, not a new feature branch.

## Why there are three native build targets

| Game | Native API | Project | Expected target |
|---|---|---|---|
| Silksong | BepInEx 5 Mono + Harmony | `Mindforge.SilksongObservation.csproj` | net46 |
| Hollow Knight | Hollow Knight Modding API | `Mindforge.HKObservation.csproj` | net472 |
| Cuphead | BepInEx 5, legacy Mono | `Mindforge.CupheadStageObservation.csproj` | net35 |

The shared visual `Mindforge.PortableOrbs.csproj` must be built
separately, targeting **net46 for Silksong** or **net35 for Cuphead**.
The Hollow Knight native HUD is compiled into its own Modding API plugin,
not into the BepInEx DLL. No compiled DLL is installed automatically.
Do not copy copyrighted game DLLs, loader DLLs, raw video or private
compiler logs into the repository.

## First: validate the Python gates

```powershell
git fetch origin
git switch feat/cuphead-stage-observer-v7
python -m pip install -e ".[test]"
python -m pytest -q tests/test_native_qualification.py tests/test_cuphead_stage_quarantine.py
```

All three local trials require `dotnet` on Windows with appropriate
Framework targeting packs, an owned game installation, and the appropriate
mod loader. The Cuphead net35 build may require additional targeting
assemblies. When references are missing, the build must fail; do not
substitute an unrelated downloaded Unity DLL.

## One-command local native build evidence

In PowerShell, from the Mindforge repo root, use **one game at a time**:

```powershell
$game = "hollow_knight"
$gameRoot = "C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight"
$evidence = "experiments/local-native/hollow_knight"

python tools/mindforge_native_qualify.py build `
  --game $game `
  --game-root $gameRoot `
  --evidence-dir $evidence
```

Substitute the following game configurations for other runs:

```powershell
# Silksong:
$game = "silksong"
$gameRoot = "C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight Silksong"
$evidence = "experiments/local-native/silksong"

# Cuphead:
$game = "cuphead"
$gameRoot = "C:\Program Files (x86)\Steam\steamapps\common\Cuphead"
$evidence = "experiments/local-native/cuphead"
```

The tool runs the existing read-only local assembly preflight; if
prerequisites are missing it will not attempt compilation. Otherwise
it invokes exactly the known, game-specific project with `dotnet build`,
without installation into the game. It writes:

- `preflight.json`: SHA-256 hashes and sizes of *local referenced*
  game/loader DLLs; no raw game binaries
- `build.log`: actual compiler stdout/stderr, **may contain local paths**
- `build/<mod>.dll`: compiled native plugin from the local source
- `build-evidence.json`: target framework, compiler exit code, exact
  DLL digest and whether a successful compiler output was observed

A successful build is **not a successful mod load**. Hashes establish
exact-file identity, not compatibility or electrical/visual safety.
These local logs and DLLs are gitignored under
`experiments/local-native/`.

### Native mod loader trial

Follow the individual game runbooks to manually install the compiled
DLL. Start with only the observer, not the cosmetic orb overlay, and
leave neural hardware and SSVEP flashing disabled.

- Silksong: `docs/SILKSONG_ACTIVITY_PREVIEW_V5.md`
- Hollow Knight: `docs/HOLLOW_KNIGHT_OBSERVER_V6.md`
- Cuphead: `docs/CUPHEAD_NATIVE_STAGE_V7.md`

Back up saves, pin the exact installed application version, capture the
native loader's actual "loaded" diagnostics and preserve a continuous
gameplay video that shows the same play session as the event log.

A plugin that is listed by the loader is not necessarily dispatching the
correct game events. We need BOTH loader log and event-to-video comparison.

## Record a full native-labeled event tape

Start the observer before entering gameplay, one local UDP listener at
a time. The following are examples; set `$build` to the
`Application.version` **actually reported by the native plugin**:

```powershell
# Silksong or Hollow Knight:
$build = "<NATIVE-OBSERVED-APPLICATION-VERSION>"
python tools/mindforge_game_observe.py --game $game `
  --seconds 120 --expected-build $build `
  --output "experiments/local-native/$game/events.jsonl" `
  --report "experiments/local-native/$game/observation-audit.json"
```

For Cuphead, use the dedicated stage-only auditor, NOT the other
games' event vocabulary:

```powershell
$build = "<NATIVE-OBSERVED-APPLICATION-VERSION>"
python tools/mindforge_cuphead_stage.py --seconds 120 `
  --expected-build $build `
  --output "experiments/local-native/cuphead/events.jsonl" `
  --report "experiments/local-native/cuphead/observation-audit.json"
```

The minimum test script:

| Game | Required observed sequence | What it does NOT prove |
|---|---|---|
| Silksong | Hero ready, player attack, scene change | Enemy was struck or boss is vulnerable |
| Hollow Knight | Hero ready, player attack, scene change | Boss engaged or safe heal/SSVEP window |
| Cuphead | Map sample, scene transition, level sample | Boss phase, parry target or projectile threat |

Do not run both the native auditor and the activity-preview service
on port 19756 simultaneously. They are independent *alternatives*
consuming the same UDP source. For Cuphead the dedicated stage audit
port is 19757; its cosmetic veto port 19758 is separate.

## Evaluate the capture without inflating claims

```powershell
python tools/mindforge_native_qualify.py evaluate `
  --game $game `
  --evidence-dir $evidence `
  --tape "$evidence/events.jsonl" `
  --expected-build $build
```

The evaluator checks the DLL digest still matches the saved compiler
manifest, validates every packet's schema and exact game version,
rejects cross-game sources, synthetic provenance, backwards timestamps,
out-of-order/duplicate sequences, and interleaved sessions. It requires
a minimal scene test for each game and writes:

- `tape-audit.json`: accepted/rejected events and trace coverage
- `qualification.json`: an explicit evidence ladder with pending gates

Even a passing, *native-labeled* event tape cannot prove its sender is
the game process. Localhost UDP is not authenticated. The report
therefore **always** leaves these gates pending until separately
reviewed: installed-game loader and hook validation, video-to-event
alignment, controls/fps parity, physical visual stimulus timing,
and participant-held-out EEG selection.

No current integration is authorized to modify combat from this report.

## Cuphead's extra transition safeguard

The Cuphead overlay now has two independent presentation prerequisites.
First, a context packet must request a cosmetic orb. Second, the
Cuphead stage receiver must observe **two consecutive level samples**
after a 750-ms transition quarantine. Map, unknown, paused,
unfocused, synthetic and stale stage packets cannot enable display.
If only one `Level.Current` reflection sample appears after a
transition, the orb stays hidden.

The two-sample rule is a conservative UX heuristic, not proof that
Cuphead is safe for gaze shifts. It provides no SSVEP authority.

## What a real qualification session must capture

1. **Baseline unmodded gameplay**: one entire scene or encounter, control
   response, hit/parry timing, and video/FPS statistics.
2. **Read-only native observer**: same scene and route, loader log,
   versioned event JSONL, video, confirm no gameplay differences.
3. **Synthetic cosmetic overlay**: enable visual-only UI, verify
   boss/HUD/projectile/pink-parry sightline occlusion at 16:9 and 16:10.
4. **Network faults**: kill the Python source, terminate/restart game,
   pause/unfocus, intentionally drop UDP and verify fail-hidden behavior.
5. **Human review**: inspect video frame-by-frame with labeled events,
   note any input delay or combat interruption. Avoid declaring an
   arbitrary boss stagger safe without measuring both timing and gaze
   cost.
6. **Future neural qualification**: photodiode/display timing, measured
   EEG, source isolation, user-controlled stop, actual independent
   decoder and sham/yoked controls. These are NOT attempted by the
   build-qualification tool.

## Promotion policy

A native build makes the adapter *eligible for local loading tests*.
A validated event tape makes it *eligible for video alignment review*.
A clean gameplay video makes it *eligible for cosmetic-only pilot*.
No level implies EEG ability permission, and no automated report alone
should greenlight gameplay modifications.
