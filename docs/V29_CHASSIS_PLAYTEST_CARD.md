# V0.29 chassis playtest card

This is the shortest useful native qualification for the Dragon Souls production pivot.

## 1. Materialize the pinned game

From the Mindforge repository root:

```bash
bash tools/bootstrap_dragonsouls_chassis.sh
```

Expected project:

`external/DragonSouls-Unity3D/ThirdPersonCombat`

The bootstrap must finish on exact upstream commit:

`f54824255517801d5d3443848e1e4275d8d5066d`

## 2. Open the known-good editor version

Use **Unity 2021.3.20f1** for the first run.

Do not upgrade the project, URP, Cinemachine, Input System, or serialized scenes during this qualification.

Allow the initial import to finish completely.

## 3. Prove the fast combat loop

Run:

**Mindforge → Chassis → PLAY COMBAT SANDBOX**

Spend roughly 60–90 seconds checking:

- normal movement and camera orbit;
- target lock;
- sword attack chain;
- dodge/roll;
- sprint/stamina;
- heal;
- sword throw/recall if the sandbox exposes it;
- defeat at least one ordinary enemy;
- enter or activate the dragon boss encounter;
- verify boss animation, movement and attacks still work.

## 4. Check the Mindforge presentation seam

During the same run:

- the old ordinary sword mesh should be hidden;
- the cyan Aetherblade should remain attached to the real animated sword/hand path;
- attack swings should therefore move the arm and blade together;
- sword throw/recall must still use the upstream Rigidbody and return behavior;
- the dragon should remain one coherent authored animal, with no generated body pieces or floating shards;
- its material palette should read cooler/stone-like with restrained neural/corruption accents.

If the Aetherblade orientation is wrong, capture front/side/overhead views instead of manually rotating the upstream Sword prefab. We will correct the presentation child transform while preserving combat authority.

## 5. Run the native audit

While still in Play Mode run:

**Mindforge → Chassis → Audit Active Chassis**

Capture the Console result.

The runtime audit checks the actual chassis owners:

- one `PlayerStateMachine`;
- one authoritative `Sword`;
- Cinemachine brain/collision;
- player CharacterController/combat/stamina/health;
- boss manager;
- Nightmare Dragon controller;
- installed Aetherblade presentation;
- installed Mindforge dragon presentation.

## 6. Prove the complete game still exists

Stop Play Mode and run:

**Mindforge → Chassis → PLAY MAIN GAME**

You do not need a full playthrough yet. Spend another 60–90 seconds confirming that the main world, traversal, ordinary enemies, UI and progression loop load rather than only the test sandbox.

## 7. Create our owned production scene

Stop Play Mode and run:

**Mindforge → Chassis → Build + Open Mindforge Combat Slice**

This creates:

`Assets/Mindforge/Scenes/MindforgeCombatSliceV29.unity`

It is a copy of the working upstream `GameplayTestScene`, not an edit to upstream. This becomes the scene where we will:

- enlarge the primary hall to at least 14 m clear width;
- keep ordinary traversal corridors at least 8 m clear width;
- preserve at least 2 m of decorative shoulder clearance;
- reserve at least a 32 m clear boss-arena diameter;
- replace scenery only in coherent architectural chunks;
- enforce visible boundary ↔ collider agreement;
- remove floating/scattered clutter from the gameplay-camera corridor;
- build the realistic cathedral/cavern Mindforge art direction on top of proven gameplay.

## What to send back

The most useful evidence is one **2–4 minute recording** containing the combat sandbox, dragon encounter and a short main-game traversal, plus the Console after **Audit Active Chassis**.

That recording is the gate for the next large art tranche. If the baseline imports and plays correctly, we stop spending effort on the old procedural world and rebuild the owned combat slice around this functioning chassis.
