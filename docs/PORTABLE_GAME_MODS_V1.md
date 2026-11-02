# Portable BCI game mods, v1: Silksong + Cuphead

**Status:** source-level, opt-in *cosmetic* prototype. The Python adapter is testable
without a headset. The BepInEx DLL still needs local compilation and live verification
against each owned game version. Hollow Knight is a planned separate API adapter,
not an installed mod in this PR.

## The shared boundary

```text
S0/S1 simulation OR a qualified NeuralEvent producer
    |  NeuralEvent v2 (derived only; source mode + session + TTL)
    v
127.0.0.1:19752  (one producer, one Python listener)
    |
ModBridgeState: source sequence/session/quality/stop/loss gates
    |  mindforge.mod_state.v1 (presentation_only=true)
    v
127.0.0.1:19753  (one game, loopback only)
    |
BepInEx PortableOrbsPlugin (Silksong or Cuphead)
    |
Static blue/green status orbs. No EEG-driven gameplay mutation.
```

The Unity competition receiver remains on 19742. The separate bridge avoids
competing listeners on a single UDP port. **Do not send one UDP datagram to two
receivers and assume both get a copy.** Use an explicit future producer fan-out
for simultaneous Mindforge Unity and game-mod output.

The bridge only accepts v2 derived events with a nonempty session and known
source provenance. Duplicate/out-of-order events, stale source state, explicit
participant stop, link loss, low confidence, poor quality, and marked artifacts
do not create an active cosmetic orb. For `live` selections it additionally
requires evidence and a calibration identity. The adapter does **not** prove
that an event arriving over localhost is fresh relative to the EEG acquisition
clock, and `authority_ttl_ms` is not treated as a game buff duration.

A frame's `ttl_ms` is enforced using the **receiving game process clock**;
monotonic times from other processes are never subtracted.

## Try without any game or EEG

From the repository root, using Python 3.10+:

```bash
python -m pip install -e '.[test]'
python -m pytest -q tests/test_mod_bridge.py
python tools/mindforge_mod_bridge.py --game silksong --demo --seconds 20
```

The demo creates explicitly simulated decision outputs and publishes fixed,
non-flickering, presentation-only state updates at 10 Hz. It does **not** collect,
simulate, or verify a physiological SSVEP response. No game must be running to
test the Python side.

## Feed the bridge with the existing decision service

Run these **on the same Windows machine as the game**. 127.0.0.1 is not a
cross-machine address.

Terminal 1:

```bash
python tools/mindforge_mod_bridge.py --game silksong
```

Terminal 2:

```bash
python tools/mindforge_dev.py decision --port 19752 --hz 4 --script sight:3,guard:3,abstain:2,lost:2,recovered:1,sight:3
```

Switch `--game cuphead` when testing Cuphead. Keep only one adapter process
and one game listener bound to port 19753 at once. Stop the old process before
changing games. If both local games must run simultaneously, allocate separate
output ports and plugin configurations in a subsequent version.

For a real/recorded EEG producer, explicitly configure its **NeuralEvent v2**
output port to 19752, preserve session/calibration identity, and qualify its
decoder separately. Avoid replacing the simulation with an unqualified decoder
and presenting it as validated neural control.

## Build the opt-in Unity/BepInEx visual adapter

Supported *development target*: owned Windows installations of Silksong and
Cuphead with an appropriate **BepInEx 5 Mono** loader already installed.
Compatibility is **not** confirmed until locally tested. Follow each game's
current community loader instructions and avoid copying compiled game files
into the Mindforge repository.

Install the .NET SDK and a .NET Framework 4.6 targeting pack if your local
toolchain requires it. In PowerShell, adjust the paths for the one game
you are building against:

```powershell
$env:GameManagedDir = "C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight Silksong\Hollow Knight Silksong_Data\Managed"
$env:BepInExDir = "C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight Silksong\BepInEx\core"
dotnet build mods/bepinex/Mindforge.PortableOrbs/Mindforge.PortableOrbs.csproj -c Release
```

Copy the resulting `Mindforge.PortableOrbs.dll` into the installed game's
`BepInEx/plugins/Mindforge.PortableOrbs/` folder, launch the game, and inspect
`BepInEx/LogOutput.log`. For Cuphead, point those two environment variables
to Cuphead's own folders, rebuild against *that game's* assemblies, and copy
into Cuphead's plugin folder. Do not assume the Silksong DLL is binary-compatible
with Cuphead. On startup the plugin auto-selects the game from Unity
`Application.productName` and disables itself for unknown games.

The overlay intentionally does not track a player object yet. It draws two
**static**, screen-positioned orbs with a small connection/source label.
It changes no damage, invulnerability, ability timing, controls, or physics.
It does **not** present 10/12 Hz coded stimuli. Introducing true SSVEP targets
requires per-game measured physical luminance, frame-pacing and photosensitivity
qualification before any human EEG claim.

## Visual and gameplay design profiles for later versions

| Game | Sight preview | Guard preview | Proposed hook (NOT implemented) |
| --- | --- | --- | --- |
| Hollow Knight | blue orbit | green orbit | API-backed state hooks; retain original pogo/parry |
| Silksong | blue orbit | green orbit | optional temporary attack amplification / defense opportunity |
| Cuphead | blue orbit | green orbit | optional timed EX resource modifier / defensive window |

The names are game fantasy semantics, **not** imagined-speech or motor-intent
decoder classes. Do not grant frame-critical parries or jumps directly from a
1.25-second EEG classifier. Conventional input always owns immediate actions.

## Qualification before upgrading to a gameplay-mod branch

1. Exact-head Python tests green; connection/loss/sequence fault tests green.
2. Build BepInEx plugin on both owned, pinned game versions, log loader success,
   and prove clean enable/disable/uninstall without affecting saved files.
3. Verify two static orbs render in menus, gameplay, full-screen and resolution
   changes; pause/resume/restart; distinguish `simulated_decision` vs live.
4. Force UDP loss, input silence, stop, repeated event IDs, extra game processes;
   stale indicators must go neutral in under the documented TTL.
5. Identify legal and stable player/ability hooks *per game*, add reversible
   game-specific logic behind feature flags, and test that native gameplay
   remains enjoyable with the BCI turned off.
6. Only then design integrated per-game coded visual targets, validate actual
   display timing with physical measurements, collect participant data, and
   promote claimed neural-control performance.

No raw EEG, personal identifiers, game assets, or third-party mod-loader DLLs
are included in the mod bridge transport or source release. The packet listener
is loopback-only, but localhost UDP is not authenticated and is for a trusted
development PC, not an untrusted multiuser machine.

## Follow-on adapter targets

- **Hollow Knight:** use the established Hollow Knight Modding API and Lumafly
  ecosystem, rather than pretending that this BepInEx plugin is already
  compatible.
- **Silksong:** continue with the community BepInEx ecosystem and test actual
  game-specific player hooks, never guess runtime class names.
- **Cuphead:** test the BepInEx loader and Mono/Unity reference compatibility,
  and keep fast parry decisions on conventional input.
- **Game-agnostic fallback:** implement a non-invasive companion UI only after
  desktop capture/overlay and OS focus behavior have been validated.
