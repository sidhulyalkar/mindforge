# Mindforge V0.11 Authored Experience Pass

Status: **source implemented; Unity runtime observation still required**.

This tranche sits on top of the clean V0.11 world reconstruction. It exists to make the demo feel intentionally directed without reintroducing the historical presentation stack or creating a second gameplay authority.

## Design rule

The experience director may **read**:

- Guardian world position;
- existing district geometry names;
- existing `FracturedEchoNode` instances;
- existing `CombatantVitals` state;
- the existing `FracturedSignalDirector.Phase` value;
- the V0.11 marker's controller-only versus neural-hardware presentation mode.

It may **write only presentation**:

- ambient/fog presentation in the controller-only demo;
- material property blocks on non-coded scenery;
- non-shadow-casting decorative point lights in the controller-only demo;
- collider-free enemy presentation geometry;
- collider-free boss phase geometry.

It must not create or schedule attacks, apply damage, change target lock, alter boss health thresholds, pause/unpause encounters, emit neural events, touch VEP stimulus timing, change aura authority, or mutate persistence.

## District atmosphere

The five route districts have bounded spatial profiles:

| District | Z envelope | Presentation intent |
| --- | --- | --- |
| Memory Forge Sanctum | `< -2` | warm-neutral stone, cool aether core, protected beginning |
| Neon Causeway | `< 32` | cooler blue distance, narrow forward pull |
| Market of Broken Momentum | `< 58` | slightly warmer ambient read, broader combat courtyard |
| Choir Tower Ascent | `< 83` | cooler elevated haze, stronger vertical silhouette |
| Fractured Signal | final | restrained hostile red-violet distance, boss destination |

The transition is position-driven and smoothly approaches the new profile. There is no periodic global luminance oscillator.

For **neural-hardware builds**, district-driven global atmosphere is disabled. The conservative static lighting authored by `MindforgeDemoV11Builder` remains in place so presentation polish cannot silently become a BCI stimulus change.

## Landmark guidance

The following non-coded landmarks receive proximity-reactive emission using `MaterialPropertyBlock`:

- `MemoryForgeCore`
- `CausewayAetherSpine`
- `MarketSignalOrb`
- `AscentAetherGuide`
- `SkylineAetherBeacon`
- `FractureSpire_0..3`

Intensity is a monotonic function of player distance. It does not use sine waves, ping-pong timers, flicker or a periodic luminance clock.

Decorative local point lights around the Sanctum, Market and Fracture landmarks are controller-demo-only and cast no shadows.

## Echo visual progression

The route still uses the exact same `FracturedEchoNode` gameplay authority. V0.11 now replaces the compatibility shell after startup with one of three collider-free silhouettes:

1. **Needle**: small core and three narrow fins;
2. **Bastion**: broader dark mass with gold lateral plates;
3. **Choir**: taller vertical spine with three gold wings.

These are **visual progression only**. Until separate enemy mechanics are authored and qualified, the silhouettes must not be described to players as mechanically distinct classes.

## Fractured Signal staging

The existing boss health thresholds remain authoritative. Presentation reads `FracturedSignalDirector.Phase`:

- Phase 1: compact base silhouette;
- Phase 2: fracture ring becomes visible;
- Phase 3: outer fracture crown becomes visible.

The presentation component cannot write the phase value or boss health. Decorative core light is disabled in the neural-hardware presentation path.

## What this should improve in the next capture

Compared with the previous fragmented showcase, the next observed playthrough should show:

- clearer visual identity when crossing district boundaries;
- stronger visual pull toward the next landmark without another HUD layer;
- three route encounters that do not read as copies of the same spinning object;
- a boss whose escalation is visible before reading the HUD;
- no temporal scenery flicker competing with BCI-coded targets;
- no reappearance of legacy presentation layers.

## Unity qualification

Use Unity **2022.3.62f3** and branch `feat/v11-world-reconstruction`.

Run:

`Mindforge -> V0.11 Demo -> Build + Play Presentable Demo`

Then verify:

1. no compile errors;
2. only one Guardian shell and one HUD;
3. Sanctum core emphasis strengthens as the Guardian approaches it and recedes spatially after leaving;
4. Causeway, Market, Ascent and boss arena atmosphere changes are gradual, not flashing;
5. all three Echoes remain hittable exactly as before despite different silhouettes;
6. no Echo presentation primitive blocks traversal or projectiles;
7. the boss remains combat-paused until the authored arena threshold;
8. phase-two geometry appears only when the existing boss reaches phase 2;
9. phase-three geometry appears only when the existing boss reaches phase 3;
10. controller-only mode remains labelled `DEMO · BCI OFF`;
11. neural-hardware rebuild retains static global lighting and does not enable the controller-demo accent lights.

Do not promote this tranche based only on Python/source CI. A clean Unity compile plus an observed playthrough remains required.
