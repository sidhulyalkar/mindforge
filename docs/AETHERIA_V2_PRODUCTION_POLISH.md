# Aetheria V2 · Production Polish Contract

Aetheria V2 is a hardening pass, not an ability expansion. Its purpose is to make the existing vertical slice reproducible, comfortable at mounted speed, and materially more tactile without creating new gameplay or neural authorities.

## 1. One conventional-input history

`GuardianInputTape` schema v4 extends the canonical fixed-tick command frame with three mounted edges:

- `mount_toggle_down`
- `mounted_attack_down`
- `mounted_boost_down`

Movement and aim reuse the existing `move_*` and `aim_*` fields. The tape is idempotent per absolute fixed tick so foot and mounted consumers can resolve the same simulation tick without recording or consuming two frames. V1-V3 tapes remain loadable and deserialize the new fields as false.

Replay remains fail-neutral. A recorded mount edge must still find the same available authored bike within the interaction radius; replay never teleports into a successful mount.

## 2. Mounted camera without optical stimulus modulation

The showcase camera keeps one fixed gameplay FOV in foot, jump, hover, cruise and boost states.

Mounted readability comes from physical composition instead:

- wider camera orbit
- slightly higher pivot
- bounded velocity look-ahead
- slightly softer high-speed position response
- existing world collision sphere cast
- existing conventional target-lock framing

This deliberately avoids speed-reactive FOV as a BCI-adjacent confound. Locomotion state must not change coded-stimulus angular scale through a dynamic projection.

## 3. Kinetic bike presentation

`HoverbikeKineticPresentationV2` reads authoritative mounted velocity and events to drive:

- bounded visual banking
- bounded acceleration pitch
- exhaust length
- boost pulse
- mounted-attack pulse

It cannot write Rigidbody state, damage, input, targeting, or neural state. Bike art remains collider-free.

## 4. Event-driven audio

`AetheriaCombatAudioV2` synthesizes a small set of clips once at startup, then reuses them. It listens to existing authoritative events for:

- jump / double jump
- dodge / air dash / landing
- Aetherblade start / contact / projectile parry / perfect guard
- hoverbike mount / boost
- Malatract projectile and melee anticipation / fire

Audio is presentation only. It never decides whether an action or hit occurred and never calls gameplay or BCI authority.

## 5. Malatract phase readability

`LordMalatractPhaseStagingV2` layers on top of `LordMalatractPresentationV1` and consumes the existing boss phase/attack events.

- Phase 1 remains narrow and controlled.
- Phase 2 opens the crown and introduces a restrained control halo.
- Phase 3 extends the Ordered Ruin silhouette and mantle scale.
- Heavy telegraphs create a short visual pulse, but enemy telegraph geometry remains the higher-priority combat signal.

Phase thresholds, health, projectiles, melee contact and scheduling remain entirely in the existing Fractured Signal boss authorities.

## 6. Explicit non-goals

V2 does **not** add:

- a second player Rigidbody
- vehicle health or collision damage
- mounted invulnerability
- rail grinding
- mounted guns
- another enemy AI scheduler
- another boss scheduler
- animation-event hit authority
- neural mount/steer/attack input
- dynamic FOV tied to BCI, movement speed or presentation quality

## Qualification

Software CI can qualify source contracts, Python tooling and browser modules. It does not prove Unity import/compile, camera comfort, audio mix, runtime presentation budget, collision behavior or physical 10/12 Hz VEP timing.

Before promotion, Unity must verify:

1. record a route containing foot movement, mount, cruise, boost, mounted attack, dismount and foot combat; replay it without live-input fallback;
2. repeated mount/dismount never leaves foot authority disabled;
3. cruise and boost camera remain comfortable and collision-safe;
4. FOV remains constant through foot/mounted transitions;
5. bike bank/exhaust are readable without corrupting rider or camera geometry;
6. audio is useful punctuation rather than constant masking noise;
7. Malatract phases read at a glance while attack telegraphs remain dominant;
8. presentation budget reports remain acceptable;
9. physical VEP timing and target salience are re-qualified after the visual pass.
