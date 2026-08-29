# Aerial Combat v1

## Direction

The latest Unity capture establishes that the faster Guardian motor is the correct baseline. Ground traversal finally matches the scale of the Null Ward. The next useful expansion is vertical combat vocabulary, not another independent ability bar.

Mindforge should now be treated as a **high-mobility third-person neural action game**:

- hands own all locomotion, camera, targeting, attack, defense and timing;
- accepted neural state changes the properties and tactical payoff of equipment;
- movement must feel good with BCI completely disabled;
- coded Sight/Guard targets remain stable even while the Guardian and fantasy Wisp move aggressively.

## Controls

| Input | Action |
| --- | --- |
| WASD | camera-relative movement |
| mouse / arrows | orbit camera |
| Space press | jump; one additional air jump per airtime |
| Space hold while descending | bounded hover / slow fall |
| Shift | directional ground dash; one air dash per airtime |
| Ctrl / Alt | compatibility dash aliases |
| T | target lock |
| F / LMB | Aetherblade light chain / projectile parry |
| RMB / E | Verdant Ward guard |
| X / MMB | Pulse Shot |
| Q | Rift Cleave |
| C | Counter Pulse |
| R | Gravity Bloom / Twin Eclipse |

## Fixed-tick movement contract

`GuardianMotor` remains the sole traversal authority.

### Double jump

- Ground/coyote jump remains the first jump.
- One air jump is available after a short minimum airborne delay.
- Air jump availability resets only on grounded contact.
- The input-tape schema does not need a new surface because `jump_down` already records the edge deterministically.

Current tuning:

- ground launch: `7.2 m/s`
- air launch: `6.8 m/s`
- coyote time: `110 ms`
- jump buffer: `130 ms`
- minimum air-jump delay: `80 ms`

### Hover

Hover is deliberately **not flight**.

Holding Space after entering descent:

- brakes a fast fall toward a bounded descent rate;
- then applies low gravity while Space remains held;
- consumes a finite airtime budget;
- stops immediately when Space is released;
- fully recharges on landing.

Current tuning:

- maximum hover budget: `1.35 s`
- target descent: `2.15 m/s`
- braking acceleration: `24 m/s²`
- gravity multiplier: `0.20x`

This creates time for target selection, sword timing, projectile parry and aerial lane changes without allowing indefinite safe hovering.

### Air dash

Shift is now the canonical dash key.

- Ground dashes retain the existing responsive chainable behavior.
- One air dash is available per airtime.
- The air dash is slightly shorter and has a shorter invulnerability window than the ground dash.
- It removes most downward momentum and gives a tiny vertical bias so it reads as an intentional aerial lane change rather than a diagonal fall.
- Air-dash availability resets on landing.

Ctrl/Alt remain aliases for compatibility, but UI and onboarding teach Shift first.

Pulse Shot moves to `X` or middle mouse.

## Aerial combat commitment

Aerial combat should not erase attack commitment, but a committed sword attack also should not turn the Guardian into a frozen brick.

The motor therefore preserves the existing ground movement multipliers while enforcing a bounded aerial movement floor during ordinary combat states. The attack still has startup/contact/recovery timing, but the player retains enough steering to fight in three dimensions.

Air dash cannot cancel an active sword commitment because the existing `CanDodge` state contract remains authoritative.

## Enemy counterplay

Adding vertical mobility without changing enemy truth would create two bad outcomes:

1. planar melee magically hits an airborne player; or
2. hover becomes a universal cheese strategy.

Aerial Combat v1 resolves both.

### Ordinary enemies

- Enemy locomotion remains planar.
- Melee attacks have explicit vertical reach and miss when the Guardian is genuinely above the strike volume.
- Projectile/Burst attacks lock and track a separate full-3D projectile direction toward the Guardian.
- Null Sentries therefore become natural anti-hover pressure while Chrome Penitents remain ground-space threats.

### Fractured Echo

Echo projectiles already aim using a full 3D direction to the player and naturally pressure hover.

### The Fractured Signal

- Fan projectiles already aim at the Guardian's full 3D position.
- Radial projectile patterns remain primarily horizontal and can therefore be countered with vertical movement.
- Cleave has explicit vertical reach.
- Slam has a lower explicit vertical reach and is intentionally jumpable.
- If the Guardian rises beyond the boss melee engagement envelope, the boss stops selecting close melee and returns to projectile pressure.

This creates a readable combat grammar rather than one universally best altitude.

## Presentation

The persistent HUD is reduced instead of adding a third aerial panel.

- Guardian and resonance panels are smaller.
- Hover budget appears inside the Guardian panel only while airborne.
- The action hint changes between ground, air, hover and air-dash states.
- The Tab loadout screen, Null Ward onboarding and Judge Lens share the same canonical controls.

The authored Animator seam now optionally exposes:

- `Hover` bool
- `AirDash` bool
- `DoubleJump` trigger
- `AirDashTrigger` trigger

Existing `Grounded`, `Airborne`, `VerticalSpeed`, `Jump`, `Land`, `Dodge` and `LandingImpact` remain supported. Root motion remains disabled.

## BCI boundary

Aerial traversal is conventional input only.

EEG may never:

- jump;
- double jump;
- engage hover;
- dash or air dash;
- steer;
- rotate the camera;
- select a target;
- swing, block, fire or parry.

Aerial movement must not dynamically alter coded VEP frequency or luminance. Sight and Guard targets must remain stable/predictable enough for deliberate gaze while the fantasy Wisp can continue independent presentation drift.

## Current build critique from the latest capture

### Improved

- Ground traversal speed is finally proportional to the Ward's room scale.
- The tighter camera makes forward motion read much more strongly.
- The Wisp reads more independently from the Guardian.
- The game now has enough movement energy that adding vertical space is justified.

### Still weak

1. **Guardian production art**: the fallback body is still visibly primitive-driven. Motion polish cannot substitute for a rigged authored character.
2. **Environment composition**: rooms still reveal their procedural origin through parallel walls, orthogonal silhouettes and even spacing. Future environment work should prioritize framing, occlusion, height and landmark hierarchy over raw detail count.
3. **Audio**: combat has far more semantic depth than its current sound language. Sword startup/contact, perfect guard, air dash, double jump, hover, enemy telegraphs and Signal Break need authored audio identities.
4. **HUD/debug footprint**: the lower HUD is now reduced, but qualification/debug surfaces should collapse automatically for normal play captures.
5. **Vertical encounter design**: existing optional jump geometry was authored for basic jump testing. Future rooms should contain high/low lanes, ledges and threats designed around double jump/hover/air dash without making aerial movement mandatory for progression.
6. **Enemy locomotion**: ranged enemies can now aim upward, but future archetypes should include explicit vertical pressure rather than every enemy remaining floor-bound.
7. **Animation**: production clips need separate takeoff, double-jump, hover, air-dash, aerial attack and landing language while remaining downstream of fixed-tick authority.

## Unity qualification checklist

Use Unity `2022.3.62f3` and `Mindforge → Showcase → Build + Play Cinematic Showcase`.

### Traversal

- standing jump, tap vs held;
- running jump;
- coyote jump;
- buffered landing jump;
- second Space press gives exactly one air jump;
- third press before landing does not create another jump;
- walk off an edge and verify the air jump still provides a rescue after coyote expires;
- hold Space after apex and verify descent slows rather than freezes;
- release/re-hold Space and verify remaining hover budget is preserved rather than refilled;
- land and verify full hover/double-jump/air-dash reset.

### Dash

- Shift ground dash in all WASD directions;
- Shift air dash with and without movement input;
- second air dash before landing is rejected;
- target-lock air dash follows movement intent rather than snapping toward the target;
- Ctrl/Alt still function as aliases;
- active sword commitment cannot be cancelled by dash.

### Combat

- sword attacks remain possible in air;
- aerial sword commitment retains controllable but reduced steering;
- shield and projectile parry remain readable while airborne;
- Null Sentry bolts visibly pitch upward toward hover;
- ground melee visibly misses when the Guardian is well above its reach;
- boss slam can be jumped;
- boss cleave reaches higher than slam;
- high hover causes the boss to return to projectile pressure;
- horizontal radial pressure and 3D fan pressure remain visually distinct.

### Camera / presentation

- camera does not clip through the Guardian during double jump;
- vertical follow does not become seasick during repeated jump/air-dash chains;
- Wisp does not block the camera while airborne;
- coded Sight/Guard targets remain stable and readable;
- hover meter is legible but not visually dominant;
- lower HUD leaves materially more gameplay image visible;
- no new Console warnings/errors.

### BCI follow-up

After controller-only feel is accepted, re-run physical 10/12 Hz display timing and participant gaze/readability checks before promoting this movement stack into a calibrated BCI build.
