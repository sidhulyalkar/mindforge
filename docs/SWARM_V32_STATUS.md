# V0.32 Implementation Status

This file is the coordination surface for parallel development of the showcase chapter.

## Dependency graph

`V0.31 native boundary fix`
→ `V0.32 showcase scene builds`
→ `chapter flow + tutorial + BCI reveal`
→ `encounter socket resolver`
→ `first encounter`
→ `Sight puzzle`
→ `vertical traversal chunk`
→ `elite encounter`
→ `boss mechanics`
→ `reward + world reveal`
→ `extract reusable additive chunk pipeline`
→ `Fracture Caverns + Memory Gardens`

No downstream gameplay-critical phase should be promoted while its upstream dependency is natively broken.

## Current work

| Area | Branch | Owned files | Status | Gate |
| --- | --- | --- | --- | --- |
| V0.31 route blocker | `feat/v31-production-vertical-slice` | `MindforgeVerticalSliceBuilderV31.cs`, projection tests | software complete | native rebuild required |
| Showcase flow | `feat/v32-showcase-intro` | `MindforgeShowcaseFlowV32.cs` | implemented | Unity compile/play |
| Chapter checkpoints | `feat/v32-showcase-intro` | `MindforgeShowcaseStageCheckpointV32.cs`, builder | implemented | Unity path traversal |
| Tutorial presentation | `feat/v32-showcase-intro` | `MindforgeShowcaseTutorialV32.cs` | implemented | native visual review |
| Region grammar | `feat/v32-showcase-intro` | `MindforgeWorldGrammarV32.cs` | implemented | source + future chunk audit |
| Chunk/socket metadata | `feat/v32-showcase-intro` | `MindforgeChunkMetadataV32.cs`, grammar audit | implemented | first authored chunk |
| Encounter recipes | `feat/v32-showcase-intro` | `MindforgeEncounterLibraryV32.cs` | implemented | resolver not yet implemented |
| Showcase readiness | `feat/v32-showcase-intro` | `MindforgeShowcaseReadinessV32.cs` | implemented | native play mode |
| First encounter resolver | future | TBD | blocked | V0.32 native scene |
| Sight puzzle | future | TBD | blocked | semantic BCI adapter |
| Traversal chunk | future | TBD | blocked | chunk metadata native validation |
| Elite encounter | future | TBD | blocked | encounter resolver |
| Boss phase mechanics | future | TBD | blocked | baseline boss native capture |
| World reveal | future | TBD | blocked | boss defeat signal |

## Latest known evidence

V0.31 route-placement fix head:

`faa5d463fce8ea0051f005623a20677ad54d0b1b`

Software gate:
- 755 tests passed;
- 0 failures;
- 0 errors;
- 0 skipped;
- P0 PASS;
- run `33679362153`;
- evidence artifact `9865630036`.

Native Unity evidence remains required for the fix because the original failure was observed only in the imported Dragon Souls scene with its real prefab pivots/bounds.

## Integration rules

- one authority file owner at a time;
- preserve Dragon Souls movement/combat/damage authority;
- never weaken route/geometry validation merely to build;
- no new solid world module without definite collision;
- no random gameplay topology;
- no third-party asset redistribution without source-level license verification;
- each tranche ends with exact-head software evidence and a focused native capture before merge.

## Immediate next native action

On the user's Mac, update `feat/v31-production-vertical-slice`, reapply the overlay, then run:

`Mindforge -> World V0.31 -> Build + Open Vertical Slice`

The previous `V31_Station_00_R innerEdge=-130.48m` failure should be gone. If a different boundary fails, preserve the 7 m requirement and capture the exact new diagnostic.

Once V0.31 builds, switch to `feat/v32-showcase-intro`, reapply the overlay, then run:

`Mindforge -> World V0.32 -> Build + Open Showcase Intro`

and

`Mindforge -> World V0.32 -> Audit Showcase Intro`.
