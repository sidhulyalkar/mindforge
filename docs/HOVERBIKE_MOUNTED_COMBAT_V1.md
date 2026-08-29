# Prism Hoverbike Mounted Combat V1

## Goal

Add a fast optional traversal/combat mode that plays the role of a mount in a large action world while remaining unmistakably Mindforge: hard-light rails, inertial hovering, oversized mounted Aetherblade, and no second player avatar.

## Authority model

The Guardian Rigidbody remains the only player body.

On foot:

- `GuardianMotor` owns locomotion;
- `GuardianCombatInput` owns conventional foot command sampling/application.

Mounted:

- `GuardianHoverbikeController` becomes the exclusive locomotion/input authority;
- foot input and foot motor components are disabled for the mounted interval;
- the parked bike visual attaches to the Guardian;
- `GuardianSwordShieldController` continues to own Aetherblade action timing and hit authority;
- target lock remains conventional;
- BCI evidence cannot mount, dismount, steer, boost or swing.

The bike itself must not create a second Rigidbody-driven player that can desynchronize from the Guardian collider.

## V1 controls

- E near a parked bike: mount
- E mounted: dismount
- WASD: camera-relative inertial travel
- Shift or RMB: short speed boost
- F or LMB: mounted Aetherblade attack
- T: existing target lock
- camera orbit: existing camera system

No mounted ranged weapon in V1.

## Movement feel

Target feel is an arcade hard-light destrier rather than a simulation motorcycle.

Initial tuning target:

- cruise speed: ~15 m/s
- boost speed: ~21 m/s
- strong acceleration but visible inertia
- high deceleration so enclosed districts remain controllable
- smooth turn toward desired travel heading
- hover spring keeps body above collision-backed ground
- no boost invulnerability
- no bike-only fall-off route; world perimeter remains the safety system

## Hover probe

Mounted controller raycasts downward in fixed tick, ignoring Guardian and attached bike presentation colliders. It drives vertical velocity toward an authored ride height. If no ground is found, bounded fall gravity applies and the existing world-safety system remains the final recovery layer.

## Mounted combat

Aetherblade attack calls the existing physical combat authority. The bike may scale travel speed during sword commitment but may not change damage, reach, combo timing or BCI resonance.

The rider should be able to:

- approach an anchor enemy at speed;
- slash while passing;
- turn away during recovery;
- lock a ranged enemy and circle it;
- dismount into ordinary foot combat.

Mounted combat must not become the universally optimal answer. Tight towers, stairs and vertical pockets favor foot combat; broad causeways and outer rings favor the bike.

## Presentation

- bike visual is collider-free except for its ordinary interaction state;
- rider presentation poses the block-squire above the saddle without moving the authoritative body;
- trails and reactor glow are presentation only;
- boost uses denser trails/emission rather than a large camera/FOV effect;
- HUD prompt is compact and disappears when irrelevant.

## Qualification matrix

### Authority

- foot motor/input disabled while mounted;
- both restored on dismount;
- no neural/BCl types referenced by bike controller;
- bike presentation has no damage calls;
- mounted attack still goes through `TryLightAttack`.

### Physics

- only Guardian Rigidbody moves as player authority;
- bike has no dynamic Rigidbody;
- ground hover operates in FixedUpdate;
- dismount location is clamped near the Guardian and inside authored world geometry;
- no boost invulnerability.

### Gameplay

- mount and dismount repeatedly without stuck input;
- target lock remains usable;
- mounted Aetherblade hits the same enemies as on foot;
- no duplicate attack edges after re-enabling foot input;
- death/checkpoint/disable cannot leave foot locomotion permanently disabled.

### Presentation

- bike is visible and readable at distance;
- rider does not run in place while mounted;
- sword trail does not merge with bike rail trails;
- enemy warning geometry remains more salient than bike glow.

## V1 exclusions

- bike health;
- mounted enemy AI;
- vehicle inventory;
- bike upgrades;
- jousting lance variant;
- rail grinding;
- required mounted boss phase.

Those should be earned by V1 play rather than assumed.