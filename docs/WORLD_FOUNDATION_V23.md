# V0.23 World Foundation + Coherence

## Why this tranche exists

The post-V0.22 gameplay recording showed that closing the visible void is not the same as building a physically coherent world. A player can still lose trust in the environment when the surface they see and the surface physics knows are different objects with different shapes.

The clearest example was the Choir Tower ascent. The canonical V0.11 collision ramp is rotated **-8.1 degrees**, while V0.22 added a broad visual-only `AscentUnderlay` rotated **+6.5 degrees**. Those slabs physically cross in space. The Guardian can therefore follow the correct collision ramp while appearing to pass through the later visual slab. That is the "weird piece of floor" visible in the recording.

The audit also found a real traversal seam: `CausewayRoad` ends at z=32 while `MarketFloor` begins at z=33. The V0.22 underlay hides that one-metre gap visually but owns no collision. In addition, the V0.22 cavern ceiling reused an ordinary upward-facing terrain mesh. A double-sided material made the ceiling visible from underneath, but its geometric normals still faced out of the cavern. Finally, many rocks, columns and buttresses looked solid while remaining invisible to both Guardian contact and the existing camera collision sphere cast.

V0.23 fixes these as one world-foundation problem: **visible solidity, collision solidity and camera solidity should agree.**

## Recording-driven fixes

### 1. Remove the crossing fake ascent floor

`WorldFoundationV23Builder.RepairAscentVisualAuthority(...)` deletes the V0.22 `AscentUnderlay` and replaces it with a foundation skin aligned to the canonical **-8.1 degree** ramp slope. The replacement stays below the authoritative traversal surface and owns no gameplay collision.

The result is intentionally boring in the best way: when the player jumps on the ascent, there should no longer be a second stone plane intersecting their body.

### 2. Bridge route seams below the visible floor

V0.23 creates exactly three thin, invisible reconciliation colliders:

- `LowerRouteSeamGuard` beneath the sanctum/causeway/market path, including the z=32 to z=33 gap;
- `AscentSeamGuard` beneath and parallel to the canonical ramp;
- `BossArenaSeamGuard` immediately below the widened Fractured Signal floor.

These are not replacement floors. They are recessed catchers underneath the normal contact surfaces. Ordinary V0.11/V0.21 collision remains the first surface the Guardian touches.

### 3. Make obvious solid scenery actually solid

V0.20 and V0.21 deliberately kept decorative procedural meshes collider-free. That was useful while establishing visual grammar, but it produces an increasingly strange world once those meshes become large foreground architecture.

V0.23 adds conservative inset `BoxCollider` proxies only to visually obvious solids such as:

- major sanctum, causeway, market and ascent columns;
- fracture spires and chamber buttresses;
- foreground field/bank/crater rocks;
- ascent-to-geology rocks and wall shoulders.

The proxies are inset relative to the render bounds so collision communicates mass without snagging the Guardian on every visual corner. This also improves camera behavior because `MindforgeGameplayCameraV17` already sphere-casts against scene colliders. No second camera system is introduced.

### 4. Author the cavern ceiling from the inside

`WorldFoundationMeshLibraryV23` generates the cavern ceiling with reversed triangle winding relative to an ordinary terrain patch. `RecalculateNormals()` therefore creates downward/inward normals appropriate for a ceiling viewed from inside the cavern.

The same generated mesh is assigned to both the `MeshFilter` and `MeshCollider`. This follows a useful principle from Sebastian Lague's MIT-licensed `Procedural-Cave-Generation`: the rendered cave boundary and physical cave boundary should come from the same topology rather than unrelated approximations. Mindforge's V0.23 implementation is project-authored and specialized to the existing deterministic height-field cavern.

`aadebdeb/ProceduralMesh` remains the MIT-licensed workflow reference for storing deterministic construction recipes instead of committing opaque generated mesh binaries.

### 5. Close the high north/south cavern wedges

V0.22's side walls meet the low roof edges well, but its north/south backing walls do not reach the high center of the vault. V0.23 adds upper backing volumes plus irregular rock masks in front of them. This closes sky wedges without making the player stare at a rectangular box ceiling.

### 6. Give the route visible foundations

The causeway, market and ascent receive retaining/foundation geometry below or outside normal traversal. This is not decorative clutter for its own sake. It makes platforms look embedded in geology instead of floating blocks placed inside a larger cavern.

## Public-code policy

V0.23 continues the repository's license-first policy.

- `SebLague/Procedural-Cave-Generation` is MIT licensed and is used as a **reference** for deriving visible and collision cave boundaries from shared generated topology. No upstream script, scene, mesh or texture is copied into Mindforge.
- `aadebdeb/ProceduralMesh` is MIT licensed and remains a **reference** for deterministic procedural-mesh authoring. V0.23's inward patch implementation is project-authored.

Both references are recorded in `third_party/manifest.json`.

Directly importing public code can be useful when it buys a maintained subsystem and the license/provenance remain clear. For Mindforge world building, narrow adaptation is currently better than importing a general dungeon framework because the game already has a canonical traversal route, combat authority, BCI visual constraints and deterministic editor build stack. A large external generator would create two competing world authorities rather than fix the one we have.

## Authority boundary

V0.23 is editor-authored world construction. It does not add `Update`, `LateUpdate`, `FixedUpdate`, runtime randomization, input handling, damage, neural consumers, persistence, combat scheduling or temporal effects.

The only new gameplay-facing authority is static collision whose purpose is to make visible solidity truthful:

- three recessed route seam guards;
- conservative proxies on obvious solid scenery;
- the existing cavern roof collider now shares the corrected inward-facing render topology.

V0.11/V0.21 remain the ordinary route collision owners and existing combat/neural systems remain unchanged.

## Validation gate

Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)** and test the world as something to explore, not just a corridor to finish.

1. Revisit the entire Choir Tower ascent. Jump and double-jump repeatedly where the previous recording showed floor intersection. The Guardian should never visually pass through a second stone slab.
2. Walk and jump across the Causeway to Market transition around z=32 to z=33 from center and both lateral edges. There should be no fall-through or invisible step.
3. Jump onto/off the Market perches, ascent edges and boss threshold. The recessed seam guards must never feel like higher phantom floors.
4. Run directly into major columns, foreground rocks, fracture spires and chamber buttresses. Objects that read as large solid masses should no longer be ghost geometry, while small foliage/patina remains non-blocking.
5. Rotate the camera tightly around those objects. Camera collision should now respect the same large solids instead of entering them.
6. Look upward throughout the route, especially near the north/south ends. The cavern ceiling should shade as an interior surface and no sky wedge should appear between the high vault and end walls.
7. Explore lateral edges with jump, double jump, hover and air dash. Existing V0.22 perimeter/roof containment still applies.
8. Fight the Fractured Signal through all phases after aggressive arena traversal. V0.23 must not alter V0.22 boss scheduler, Wisp pause or neural-safety authority.
9. Run **Mindforge → Latest → Validate Latest Readiness** after the playtest.

The most important acceptance criterion is perceptual: the player should stop thinking about which surfaces are real. The cavern should read as one continuous place with one physical vocabulary.
