# V0.33 Laptop Keyboard Controls

Mindforge V0.33 includes a binding-only keyboard profile for developing and recording the native Unity playthrough on a laptop without requiring a gamepad or a mouse-driven combat workflow.

## Primary mapping

| Input | Action |
| --- | --- |
| `W` / `A` / `S` / `D` | Move |
| Arrow keys | Move the gameplay camera / view |
| `Space` | Light attack |

The profile is intentionally small. These are the three controls required for the current MacBook-first development loop. Existing Dragon Souls gamepad and mouse bindings are not removed.

## Why this is an adapter instead of an upstream edit

The pinned Dragon Souls input asset already contains a WASD `2DVector` binding for `Move`, but its `Camera` action is bound to the gamepad right stick and its `LightAttack` action is bound to the gamepad right shoulder. Space is originally assigned to `Jump`, whose pinned player handler is currently inert.

`MindforgeKeyboardControlProfileV33` therefore modifies only the live runtime `InputAction` instances:

- it verifies/adds WASD to the existing `Move` action;
- it adds an arrow-key `2DVector` to the Dragon Souls `Camera` action;
- it adds the same arrow-key camera binding to the Cinemachine input-provider action used by the inherited FreeLook/target camera stack;
- it removes the runtime Space binding from `Jump` and adds Space to `LightAttack`;
- it leaves the upstream `Controller.inputactions` and generated `Controllers.cs` files unchanged on disk.

The adapter never invokes attack events itself, never writes movement vectors, never moves the player transform, and never changes camera priorities. Input still enters the inherited Dragon Souls input/action/state pipeline.

## Native verification

After materializing the exact branch head and launching:

`Mindforge -> World V0.33 -> PLAY BCI INTEGRATION`

confirm the Console contains:

```text
[Mindforge:V33:INPUT] Laptop keyboard profile ready: WASD=move, Arrow Keys=view, Space=light attack.
```

Then verify in this order:

1. `W/A/S/D` produces ordinary camera-relative locomotion.
2. Holding each arrow key rotates the view continuously and releasing it stops camera motion.
3. Arrow controls work in normal FreeLook and target-oriented gameplay.
4. `Space` produces exactly one light-attack input rather than the old Jump binding.
5. Movement plus camera can be held simultaneously, for example `W + Left Arrow`.
6. Movement plus attack can overlap naturally, subject to the inherited Dragon Souls combat state machine.
7. Existing gamepad behavior remains unchanged if a controller is connected.

Finally run `Mindforge -> World V0.33 -> Audit BCI Integration`. The audit fails if the requested keyboard profile cannot be installed or if the pinned `_controls` input seam has drifted.

## Next ergonomic bindings

Do not overload the first profile before the core playthrough is stable. Once movement/view/attack are native-green, the next candidates should be assigned from observed gameplay need, likely roll/dodge, target lock, heal, sprint and heavy attack. Those should be added as an explicit second tranche with a visible control legend rather than accumulated as undocumented shortcuts.
