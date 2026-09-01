# V0.24 White Cathedral + World Reformation

## Why V0.24 exists

V0.23 made the world physically trustworthy, but the latest playtest still exposed a different failure: **the environment did not read as one designed place**. It read as a stack of successful local fixes. Dark slabs, procedural rock scatter, market props, old skyline masses, later retaining pieces, columns, fracture scenery and route underlays were all individually understandable, yet together they produced visual mush.

V0.24 changes the question from "how do we hide this seam?" to **"what is the architectural system of this world?"**

The answer is the original white-cathedral direction: an ordered sacred structure carved into a darker cavern, with the Fractured Signal acting as an invasion of that order rather than the entire visual language.

## The core art contract

The foreground route is now governed by a deliberately narrow palette:

- pale ivory limestone for most load-bearing architecture;
- slightly cooler white marble for focal arches, capitals and processional inlays;
- pale worn stone for the authoritative route floors;
- cool shadow stone only for cavern geology, foundations and recessed backing structure;
- bronze and restrained sacred gold for trim;
- cyan for benign lumen guidance;
- magenta for Fractured Signal corruption.

The intended screen-space balance is cathedral-first. Dark stone should frame and recess the architecture, not swallow the playable foreground.

## One floor authority

The route still obeys the V0.11/V0.23 physical collision contract. V0.24 does **not** create a second collision floor on top of it.

Canonical visible/collision owners remain:

- `SanctumFloor`
- `CausewayRoad`
- `MarketFloor`
- `AscentRamp`
- `FractureFloor`

V0.24 re-materializes those surfaces with the same pale-floor material and adds extremely thin, collider-free processional skins for aisle inlays and trim. The Choir ramp skin is derived from the exact transform and top normal of the canonical `AscentRamp`, so V0.24 cannot recreate the old V0.22 `+6.5°` crossing-floor bug.

**One floor authority** means:

1. gameplay collision remains on canonical floor owners;
2. presentation skins never become hidden higher floors;
3. district changes use explicit threshold bands instead of accidental rectangle seams;
4. the same pale-floor grammar continues from sanctuary to boss chamber.

## The cathedral building kit

`CathedralModuleLibraryV24` replaces one-off world decoration with a small reusable vocabulary:

- `FloorSkin`
- `Column`
- `PointedArch`
- `Buttress`
- `WallPanel`
- `LumenSconce`
- `RetainingBlock`
- `BoundaryBlock`
- `BeamBetween`
- `Trim`

Every rendered V0.24 module carries a `CathedralRoleV24` semantic role:

- `WalkableFloor`
- `StructuralSupport`
- `BoundaryWall`
- `VaultCeiling`
- `RetainingSubstructure`
- `DecorativePatina`
- `MysticAccent`

This is deliberately boring infrastructure. It prevents future passes from returning to anonymous primitives with no declared relationship to traversal or architecture.

## Cleanup before addition

V0.24 does not solve clutter by adding more clutter.

The following foreground grammars are disabled after they have served their earlier prototyping purpose:

- `WorldSoul_Natural_Rock`
- `WorldSoul_Sanctum_Grove`
- `WorldSoul_Causeway_Banks`
- `WorldSoul_Market_Ruins`
- `WorldSoul_Ascent_Geology`
- `V21_Surface_Transitions`
- `V21_Foreground_Ecology`
- `V21_Near_City_Facades`
- `V21_Landmark_Composition`
- `V22_Route_Luminance_Anchors`
- the blocky `V11_Skyline`
- old market stalls and garden boxes

V0.20 terrain, V0.22 cavern containment, V0.23 physical foundations and the boss fracture-history layer remain. The world is simplified before it is rebuilt.

## Zone architecture

### Narthex — Memory Forge Sanctum

The opening zone becomes a proper cathedral narthex. Three column/arch bays establish the visual rules immediately. Side wall panels and cyan sconces frame the Memory Forge without competing with it.

The important change is repetition: columns are not scattered props. They form a measured architectural cadence.

### Nave — Causeway

The Causeway becomes the primary processional nave:

- repeated fluted column pairs;
- repeated pointed transverse arches;
- wall panels between structural bays;
- a continuous white-marble center aisle;
- bronze edge trim;
- restrained alternating sconces.

This should be the visual sentence that teaches the player what the world is.

### Cloister / transept — Market

The old mixed market clutter is replaced with a larger quiet cloister composition:

- perimeter columns;
- exterior buttresses;
- broad transverse arches;
- a white-marble aisle/transept cross;
- a small sacred-gold medallion axis.

The Market remains mechanically open, but it stops reading as an empty room filled with miscellaneous boxes.

### Choir — Tower ascent

The ascent becomes a ceremonial choir rise rather than a ramp with geology scattered around it.

Arch heights follow the same route-elevation function used by the world. Buttresses step upward along the sides. Gold trim follows the exact canonical ramp transform. V0.23 remains responsible for the physical surface beneath it.

### Apse — Fractured Signal

The boss chamber becomes a white cathedral apse being invaded by the signal anomaly.

Architecture is deliberately placed **outside** the existing enlarged combat floor/wall ring so V0.24 does not compress the V0.22 fight:

- ten pale perimeter columns;
- deeper shadow buttresses behind them;
- a segmented pale sanctum floor ring;
- a north triptych of pointed apse arches;
- cyan/magenta lumen contrast;
- one magenta fracture axis through the floor.

This creates the intended contrast: ordered pale architecture vs. unstable magenta corruption.

## Lighting

V0.24 lifts the scene out of the near-black presentation that was flattening material differences.

The pass uses only static lighting:

- a warmer, stronger directional key;
- brighter cool ambient fill;
- longer, cleaner fog falloff;
- six fixed point lights supporting zone readability.

There is no flicker, pulsing, neural-state modulation or time-varying visual effect. The BCI visual-control boundary remains intact.

## Material generation

`CathedralMaterialLibraryV24` generates its pale stone surfaces deterministically under the ignored `Assets/Mindforge/Generated/V24` tree.

It reuses the maintained `Mindforge/ProductionTriplanarLitV09` shader so scaled architectural modules preserve world-space texture density. Pale limestone, marble, floor stone, cool shadow stone and fracture stone receive generated albedo and normal maps from deterministic `WorldSoulNoiseV20` recipes.

No new external art package or binary texture dependency is introduced by this pass.

## Structural validation

V0.24 fails closed if the authored world escapes its design grammar.

The builder checks:

- every canonical primary floor is still a visible collision owner;
- every canonical primary floor uses the same V0.24 pale-floor material;
- the V0.24 kit contains a minimum number of walkable skins, structural supports and mystic accents;
- every V0.24 renderer has a semantic `CathedralRoleV24` marker;
- selected old clutter layers remain disabled;
- the contradictory V0.22 `AscentUnderlay` has not returned.

These checks are not a substitute for visual playtesting, but they prevent several categories of regression from silently re-entering the scene.

## Playtest gate

Use **Mindforge → Latest → PLAY LATEST (BCI Simulation)** and evaluate the world as architecture rather than as a sequence of mechanics.

1. Stand at spawn and look toward the route. The foreground should immediately read as pale cathedral architecture, not dark procedural scenery.
2. Walk the entire center aisle. Floor language should remain consistent from the narthex through the nave and cloister.
3. Inspect every district threshold. The transition should look intentionally framed rather than like two rectangles happened to overlap.
4. Strafe close to columns and arches. Repetition should create rhythm without reducing route readability.
5. Re-test the Choir ascent with jump, double jump, hover and air dash. There must be one readable ramp surface and no floating/crossing floor illusion.
6. Rotate the camera through the Market. Old stall boxes, noisy scatter, near-city facade clutter and skyline blocks should no longer dominate the image.
7. Enter the boss chamber. The arena should remain mechanically spacious, with pale apse architecture outside the fight and magenta corruption concentrated near the Fractured Signal.
8. Look upward. The darker cavern should frame the white vault rhythm rather than making the whole environment disappear into black.
9. Test Wisp/SSVEP flows. V0.24 adds no temporal lighting or neural-driven graphics and must not change the established pause/stimulus contracts.
10. Run **Mindforge → Latest → Validate Latest Readiness** after the visual pass.

The acceptance criterion is simple to say and hard to fake: **the player should be able to describe the place as a white cathedral inside a cavern, not as a collection of Unity pieces.**
