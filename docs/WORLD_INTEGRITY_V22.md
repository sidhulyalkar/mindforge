# V0.22 World Integrity + Boss Duel

## Why this tranche exists

V0.21 corrected a real arena geometry problem, but the next gameplay report exposed a more fundamental failure: Mindforge still read as several procedural layers placed near each other instead of one enclosed place. The symptoms were transparent/ghosted world surfaces, open sightlines into empty sky or void, no convincing cavern roof, weak wall-to-roof continuity, exploration revealing unfinished edges, and a first boss that could still stall or feel unreliable at sword range.

V0.22 therefore treats **world integrity** and **duel integrity** as contracts rather than polish.

## World integrity

`WorldIntegrityV22Builder` is the final editor-authored stage after V0.20 and V0.21.

### Render-state repair

Structural materials are forced back to an opaque depth-writing state. This resets URP surface/blend/Z-write/render-queue state and clears stale transparency keywords. World Soul water is also opaque in the stylized cavern so its read comes from color, normals and smoothness rather than depth-sorted alpha. Real glass and signal/BCI presentation surfaces remain exempt.

This addresses a Unity-specific failure mode where generated or reused material assets can retain serialized render-state values across revisions even after a later recipe changes shader or appearance properties.

### Complete cavern envelope

The canonical route now has:

- broad opaque ground underlay beneath authoritative walkable geometry, removing visible void through seams;
- a continuous procedural cavern vault spanning the authored route;
- double-sided opaque vault rendering for a reliable underside;
- a high mesh-collider ceiling so repeated aerial movement cannot leave through the roof;
- continuous west/east/north/south dark cavern backing volumes;
- irregular rock shoulders and cathedral ribs in front of those backing volumes;
- distant perimeter safety colliders that prevent falling into un-authored infinity without cluttering normal traversal.

The ceiling and perimeter are deliberately outside ordinary movement. They are world-boundary safety, not a competing traversal system.

### Architectural continuity

The shell inherits the route's architectural language through pointed vault ribs, stone shoulders, static luminance anchors and a dedicated Fractured Signal chamber crown. The boss arena should read as a carved chamber inside the same geology rather than a circular platform pasted onto the end of a corridor.

## Duel integrity

`FracturedSignalDuelStabilityV22` composes over the maintained V0.19 locomotion owner and `FracturedSignalDirector`; it does not create a second ordinary attack loop.

### Use the room we built

V0.21 enlarged the wall ring to 18.3 m but retained only a 9 m boss home leash. V0.22 expands that leash to 14.2 m, shortens orbit-side commitment, lowers the collision probe radius and preserves readable sword/dodge spacing. The boss should now traverse most of the actual chamber instead of orbiting inside an invisible smaller circle.

### Less projectile soup, clearer melee

The first encounter is retuned toward learning the physical combat loop:

- slightly longer phase intervals;
- longer telegraphs;
- radial count reduced to 6;
- only one concurrent echo;
- narrower cleaves;
- slightly smaller slam radius;
- longer melee tells.

A trigger-only `V22_BossCombatHull` enlarges reliable sword contact without becoming movement collision. Guardian sword sweeps already resolve `CombatantVitals` through parent colliders, so this improves contact reliability without changing damage authority.

### Stall recovery

V0.19 remains the normal locomotion owner. V0.22 intervenes only when the boss is outside the authored chamber envelope or at an impossible vertical position, or when it has remained effectively stationary for a sustained post-attack window while not paused or poise-broken.

Recovery reverses orbit preference, advances the next orbit decision, clears a stale post-attack hold only after commitment has ended, and may perform a small collision-checked inward/tangential nudge.

### Pause ownership

A stale boss external pause may be repaired after the player has entered the encounter, but only when neither legitimate safety owner is active:

- `WispCombatIntermissionV19`; or
- `NeuralLinkContingency` degraded/participant-stop state.

The Wisp ceasefire and neural safety contract remain authoritative.

## Local gameplay gate

Use only **Mindforge → Latest → PLAY LATEST (BCI Simulation)**.

1. Traverse slowly and quickly through Sanctum, Causeway, Market and Ascent. Look specifically for ghosted floors, see-through rocks, transparent masonry, z-sorting seams, sky-colored cracks and missing geometry below the route.
2. Jump, double-jump, hover and air-dash aggressively at route edges. The world should remain visually enclosed and the cavern roof should stay present above the camera. You should not be able to escape through the top or fall into an un-authored void.
3. Inspect long horizontal sightlines. There should be no large gaps between terrain and cavern roof exposing an empty backdrop.
4. Approach the Fractured Signal. The arena should read as a chamber in the same cavern, with crown/buttress/rib composition around it.
5. Target-lock and circle through every quadrant. The boss should use substantially more of the arena than V0.21 and should not settle against a wall for long periods.
6. Repeatedly attack near the edge of sword reach. The trigger hull should make visually plausible swings register consistently without creating an invisible physical blocker.
7. Test dodge, jump-over-cleave, spacing, perfect guard and sword projectile parry. The encounter should feel readable rather than saturated with radial projectiles.
8. Hold `V`. Confirm the deliberate Wisp ceasefire still pauses both sides. End the window and confirm combat resumes.
9. In controller-only simulation, verify the boss never remains externally paused after ordinary encounter entry without a visible Wisp/safety reason.
10. Run **Mindforge → Latest → Validate Latest Readiness** and inspect the Console for `[Mindforge:V22]` or `[Mindforge:BossV22]` warnings.

If a boss stop remains, capture the exact moment including HUD/Wisp state and Console. V0.22 now separates ordinary movement, attack commitment, legitimate pause ownership and exceptional stall recovery, making any remaining failure materially easier to isolate.
