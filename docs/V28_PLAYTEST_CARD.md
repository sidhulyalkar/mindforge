# V0.28 focused playtest card

Use the canonical Unity entry point only:

**Mindforge → Latest → PLAY LATEST (BCI Simulation)**

V0.28 is intentionally still a draft until this pass is observed in Unity 2022.3. The software gate proves source-level contracts, not imported-model orientation, animation, camera feel or scene composition.

## 1. First rebuild / asset ingestion

- Let Unity Package Manager resolve the pinned UnityGLTF package.
- On an empty V0.28 generated cache, allow the Editor to download the pinned Gobkit/KayKit assets.
- Treat any hash/import/build failure as a real failure. Do not manually drag substitute art into the scene.
- Run **Mindforge → Latest → Validate Latest Readiness** after the scene opens.

## 2. Fractured Signal body and spacing

Approach the boss head-on, from both flanks and from behind.

Pass criteria:

- the visible creature is the authored quadruped, never the old generated V0.27 beast;
- no V0.19 shard-character or V0.11 boss proxy flashes or persists;
- the Guardian never disappears into the creature's body;
- at very close distance the boss backs out smoothly rather than teleporting or vibrating;
- the boss can still enter its normal melee danger band and cleave/slam remains threatening.

Capture a failure if you see repeated separation oscillation, a dead zone where neither actor can move, or the boss being pushed through arena architecture.

## 3. Sword contact

Attack the visible head, chest, both flanks and rear over several combo chains.

Pass criteria:

- sword hits register where visible anatomy exists;
- broad visible body regions do not behave like empty air;
- one sword swing does not multiply damage because it crosses multiple anatomical trigger boxes;
- attacks just outside the body do not register as obvious phantom hits.

## 4. Camera visibility

Lock onto the boss and orbit clockwise/counter-clockwise at close, medium and long range. Repeat near columns and the north apse.

Pass criteria:

- the Guardian remains readable when the creature crosses the camera sightline;
- camera correction is small and temporary;
- FOV remains fixed;
- there is no lateral jitter or sudden shoulder swap;
- the correction never places the camera inside a wall, column, ceiling or prop.

## 5. Authored animation

Observe idle, movement, attack telegraph/release and death.

Pass criteria:

- the imported skeleton animates rather than sliding as a rigid statue;
- walk animation does not move the gameplay root independently of Mindforge locomotion;
- attack animation reinforces the existing telegraph rather than firing late or hiding it;
- death does not drag the root through the floor;
- the model stays grounded and consistently faces the intended encounter forward direction.

## 6. Cathedral spacing and detail

Traverse Memory Forge → Causeway → Market/cloister → complete choir ascent → boss apse slowly, including camera orbit near both side walls.

Pass criteria:

- the main processional lane always reads wider and calmer than the dressing bands;
- choir lights/banners create repeated scale rhythm rather than a wall of props;
- prayer seats read as side alcoves and never snag movement/camera;
- far-apase reliquaries deepen the composition while remaining obviously outside the fight floor;
- no floating, buried, z-fighting or giant mis-scaled imported props;
- no new decorative object has gameplay collision.

## 7. Neural visual field

Trigger calibration/Wisp resonance after ordinary combat is understood.

Pass criteria:

- V0.28 camera correction neutralizes;
- creature presentation settles to the intended neutral visual state;
- coded neural stimuli remain the dominant controlled visual signal;
- no new animated prop or creature effect becomes an uncontrolled competing flicker.

## Recording request

A useful V0.28 recording is 90–150 seconds and contains, in this order:

1. ten seconds walking through the choir ascent;
2. one slow orbit of the north apse before combat;
3. direct approach into the boss until minimum separation engages;
4. one close lock-on orbit each direction near a column;
5. head/chest/flank/rear sword contacts;
6. at least one boss melee attack and one projectile fan/radial attack;
7. death or one phase transition if practical.

The next graphics/combat tranche should be selected from this recording rather than adding another speculative visual layer.
