# Mindforge Unity First-Journey Showcase

This is the shortest path from a fresh checkout to the current third-person Mindforge vertical slice.

The showcase has two deliberately separate modes:

1. **Controller-only preview** for controls, camera, traversal, combat feel, enemy readability, environment and boss validation.
2. **Calibrated neural mode** for real Sight/Guard SSVEP authority and BCI evidence.

The controller-only path never fabricates calibration success or neural evidence.

## Required Unity version

Open the repository's `unity/` directory with:

- **Unity 2022.3.62f3 LTS**
- Universal Render Pipeline **14.0.11**, pinned by the project

The `ProjectVersion.txt` and qualification runner now use the same editor version that has been observed compiling the project on the development Mac. Do not migrate the project to Unity 6 during qualification.

## Fresh import or update

From the repository root:

```bash
git switch main
git pull --ff-only origin main
git rev-parse HEAD
```

Then return to Unity and allow package/script compilation to finish. If Unity enters Safe Mode or shows a red Console error, stop and capture the **first** compiler error. Fix source rather than editing generated scene objects by hand.

## One-click preview

Stop Play Mode, clear the Console, then choose:

**Mindforge → Showcase → Build + Play Cinematic Showcase**

The command performs the full current authoring pipeline:

```text
cinematic URP configuration
        ↓
deterministic PBR material authoring
        ↓
competition scene assembly
        ↓
showcase + cinematic detail passes
        ↓
Arena Environment V3
        ↓
First Journey authoring
        ↓
scene validation
        ↓
Play Mode
        ↓
explicit controller-only preview
        ↓
Game view receives keyboard/mouse focus
```

The generated scene remains `Assets/Mindforge/Scenes/Mindforge_Competition.unity`.

## Current physical control grammar

| Input | Action |
|---|---|
| WASD | Camera-relative movement |
| Mouse / trackpad | Free third-person camera orbit |
| Arrow keys, unlocked | Laptop camera-orbit fallback |
| T | Conventional target lock / unlock |
| Left / Right arrow, locked | Cycle target |
| Mouse wheel, locked | Cycle target |
| Space | Unlimited directional dodge / dash |
| F or Left Mouse | Aetherblade light attack / queue combo / blade parry |
| E or Right Mouse (hold) | Verdant Ward shield |
| Shift | Pulse Shot |
| Q | Rift Cleave |
| C | Counter Pulse |
| R | Gravity Bloom / Twin Eclipse when eligible |
| Tab | Warden Loadout |
| Esc | Release mouse cursor in Editor |
| F8 | Explicit controller-only qualification |
| F9 | Photodiode patch toggle |
| F10 | Judge Lens |
| F11 | Photodiode source Sight / Guard |
| F12 | Display qualification |

Ordinary movement, sword attacks and directional dashes are **not stamina-gated** in the current design. Defensive shield pressure uses **Guard Integrity**.

The authority boundary remains strict: neural evidence cannot move, orbit the camera, target-lock, attack, guard, dodge or fire for the player.

## The authored journey

The current controller-only Showcase opens the combat world and places the Guardian at the beginning of a short directed expedition:

```text
The Listening Cavern
        ↓
The Ruined House
        ↓
The Cellar
        ↓
The Signal Warden
        ↓
Fractured Signal threshold
        ↓
Arena V3 boss fight
```

The journey is authored under the combat arena root so Awakening/calibration remains untouched. The final Arena V3 stays at its existing qualified location; the journey extends backward from it.

### The Listening Cavern

Purpose: teach third-person locomotion without a tutorial wall.

Validate:

- `W` follows camera-forward;
- rotating the camera changes the direction represented by `W`;
- `T` locks the first Hollow;
- locked `A/D` strafes while the Guardian continues facing the enemy;
- `Space` dodges in held WASD direction;
- `F` produces only one sword step unless another input is queued;
- the first Hollow behaves as a readable 1v1;
- the second Hollow waits farther toward the cavern exit rather than immediately collapsing the lesson into a 2v1;
- the exit seal cannot be bypassed before both required enemies are defeated.

### The Ruined House

Purpose: introduce mixed melee/ranged pressure and target switching.

Validate:

- Hollow and Shardcaster are visually distinct;
- target cycling selects only living active enemies;
- line-of-sight/cover causes projectiles to hit the environment rather than pass through it;
- RMB/E shield behaves directionally;
- perfect guard can reflect a projectile;
- an Aetherblade contact-window slash can physically parry a hostile projectile;
- reflected projectiles follow the **currently locked conventional enemy**, not a hidden boss reference.

### The Cellar

Purpose: ask the player to combine movement, target choice and reflection mechanics.

Validate:

- three enemies remain readable rather than attacking as an incoherent simultaneous wall;
- telegraphs visibly precede authority;
- dodge i-frames allow a projectile to continue through rather than consuming it as a fake hit;
- `C` Counter Pulse reflects nearby hostile projectiles;
- Flux builds from intended actions;
- `R` Gravity Bloom captures and returns projectiles when eligible;
- target switching remains stable in the tighter camera space.

### The Signal Warden

Purpose: physical mastery check before the boss.

Validate:

- the Warden alternates readable melee and ranged burst pressure;
- only one pending enemy attack authority exists at a time;
- attack windup, contact and recovery are visually distinguishable;
- the player can intentionally dodge, block, perfect-guard or punish recovery;
- killing the Warden opens the final path and provides a short pacing breath before the boss.

### Boss threshold

The Fractured Signal is authored inactive until the journey is complete.

Validate:

- no boss projectile or boss HUD appears during earlier rooms;
- the approach is open only after the Warden stage is cleared;
- crossing the final threshold activates the boss and closes the arena seal **behind** the Guardian;
- the boss bar appears only when boss authority is active;
- target lock and Soul Wisp anchor cleanly transfer to The Fractured Signal.

## Third-person camera validation

Unlocked camera:

- remains behind and slightly above the Guardian;
- supports full horizontal orbit;
- ignores the Guardian's own colliders during camera collision;
- retracts for actual world geometry;
- recovers smoothly after leaving a wall;
- preserves a readable Guardian silhouette.

Locked camera:

- keeps Guardian and current enemy coherently framed;
- supports forward, backpedal and strafe combat;
- does not secretly create target lock;
- does not consume arrows as camera orbit while they are being used to cycle targets;
- maintains enough enemy screen stability for future gaze/SSVEP placement.

## BCI target presentation validation

The Soul Wisp follows conventional target selection.

Unlocked or without a valid combat target, it returns to its non-combat behavior. While `T` lock is active, Sight and Guard settle into stable camera-relative positions around the currently locked enemy.

The two coded targets remain:

- **Sight / blue / 10 Hz**
- **Guard / green / 12 Hz**

Target lock changes **position**, not coded VEP frequency. The luminance timing remains owned by `VepAuraStimulus`.

In controller-only mode the game may show the visual language, but no EEG/neural authority is accepted. The qualification banner must remain explicit.

## Combat validation

### Aetherblade

Validate the three-step chain:

- step 1: fast opening sweep;
- step 2: reverse-direction sweep;
- step 3: wider/heavier finisher with more recovery commitment.

Also validate:

- movement and ordinary sword use remain unrestricted by stamina;
- one swing cannot repeatedly damage the same enemy;
- the active swept capsule agrees with the rendered swing direction;
- hostile projectiles are reflected only when they intersect the active blade volume;
- hit stop is crisp and does not make camera control feel sticky;
- Sight can amplify bounded reach/damage only after accepted Sight authority.

### Verdant Ward

Validate:

- shield coverage is directional;
- movement is slower while guarding;
- Guard Integrity is spent by absorbed pressure rather than by ordinary locomotion;
- insufficient integrity causes guard break;
- flanking attacks can bypass coverage;
- perfect projectile guard reflects;
- perfect melee guard applies poise pressure rather than invented direct damage;
- Guard neural state only amplifies bounded shield properties after accepted Guard authority.

### Directional dodge

Validate:

- held WASD has dash-direction priority;
- stationary dash falls back to combat heading;
- dash commands can be chained without a stamina or arbitrary cooldown tax;
- the bounded i-frame is shorter than the whole visible movement;
- attack commitment prevents impossible mid-contact teleporting;
- movement after the dash resumes naturally.

## What counts as a successful pull test

Do **not** call the new slice Unity-qualified merely because source tests are green.

The first real Editor gate is:

1. Unity 2022.3.62f3 imports the pulled head;
2. Console reaches zero red compiler errors;
3. the one-click Showcase completes scene authoring;
4. no red runtime errors appear on entering Play Mode;
5. Guardian appears at the Listening Cavern start;
6. the route can progress cavern → house → cellar → Warden → boss;
7. locks/gates do not soft-lock the run;
8. the boss remains dormant until the final threshold.

If any step fails, capture the **first red Console error** or a short recording of the behavioral failure and fix that exact head in source.

## Next quality gates after the route works

Once the journey is mechanically traversable in real Unity, tune in this order:

1. third-person movement/camera feel;
2. enemy telegraph/recovery timing;
3. room width, cover and encounter pacing;
4. Guardian locomotion/combat animation;
5. enemy visual silhouettes/animation;
6. weapon and contact VFX;
7. environment material/lighting polish;
8. BCI orb placement under real gaze constraints;
9. calibrated synthetic/physical BCI qualification.

That order prevents a beautiful scene from hiding a controller that still feels wrong.
