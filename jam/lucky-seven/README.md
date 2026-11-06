# Lucky Seven: Little Minds (2026 Unity Jam prototype)

> Newly created during the 2026 Unity Game Jam. Standalone Unity project, separate from the pre-existing Mindforge game. Not yet a submitted, compiled, hardware-qualified build.

**Jam theme:** Lucky Number. A tiny flying squirrel must find seven luminous fortune seeds to return to its nest. Seven is a mechanical win condition: the nest stays closed until the seventh seed. The game has two mutually exclusive animal senses, **Wind** (cyan air bridges) and **Root** (lavender bark bridges). Neither is an emotion decoder or generic "attention score": in a future live session they correspond to two independently calibrated SSVEP visual targets. Movement, jumping, gliding and landing stay with the hands.

## Quick start

1. Open this directory itself as a **separate project** in Unity 2022.3.62f3 (the version currently pinned by Mindforge), not the parent `unity/` directory.
2. Choose **Tools > Lucky Seven > Create Playable Prototype Scene**.
3. Open `Assets/Scenes/LuckySeven.unity`, then press **Play**.
4. Install **WebGL Build Support** in Unity Hub, then use **Tools > Lucky Seven > Build WebGL Demo** and test `Build/WebGL` via a local HTTP server before packaging for itch.io.
5. Move with **A/D** or arrow keys, **Space** to jump or hold to glide on descent, **1** for Wind and **2** for Root, **R** to restart after winning. The seven golden dots are collectible; the glowing nest at the far right opens on 7/7.

The project generates colored geometric placeholder art at runtime, intentionally avoiding pre-jam game content. Its current goal is a testable gameplay foundation, not final art, sound, or tuning.

## Mode and BCI truth table

| Mode | Availability | What it proves |
|---|---|---|
| Keyboard demo (default) | Editor, WebGL, native player | Platformer and sense-switching are playable without equipment. **Not BCI evidence.** |
| Native live test (`-lucky-seven-live`) | Native player only | Reads Mindforge-derived NeuralEvent v2 events on localhost UDP :19742. A calibrated, live, evidence-bearing Sight decision selects Wind; Guard selects Root. Never accepts manual/simulation/replay sources for live control. |
| Hardware qualification | Not completed | Requires device, separately synchronized stimulus render timing, artifact validation, movement/gaze contamination tests and measured false accept/reject/latency performance. |

There is **no built-in flickering stimulus yet**. A real hardware session must use an independently validated stimulus/decoder stack; a UDP test by itself is not proof of SSVEP control. This prototype does not flash targets at potentially unsafe or unmeasured frequencies. Stop or lost-signal events disarm new live selections; the last platform arrangement persists until the player is safe.

## What makes the BCI meaningful

- The bridges over three long gaps are activated by a **sense selection**. In strict native live mode, only a calibrated EEG decision can select a sense. No keyboard fallback.
- The selection is made at **safe resting perches**, never as a frame-perfect reflex. BCI latency should create anticipation, not a delayed jump button.
- Wind and Root are exclusive; taking one route can hide the other, creating intentional world-state choices.
- If the decoder abstains, glitches, or disconnects, no new choice is granted. Its uncertainty is not interpreted as a player's intention.
- The WebGL version is intentionally controller-accessible for jam judges. It is clearly labelled a non-neural demo.

## Next deliverables and gates

1. **Unity import / compile** and generated scene opening, then WebGL export verified in an actual browser. No Unity run has been performed by this change.
2. Movement feel: coyote time, jump buffering, forgiving platform edges, glide animation, camera leading and coherent hand-drawn creature art.
3. Real affordances: visibly distinct Wind/Root perches and all seven seeds reachable only under their intended sense/route conditions.
4. Integration: the existing Mindforge two-class decoder, actual display timing and independent signal/artifact controls.
5. Native hardware playtest, then balancing by observed attempts, error types and neural-versus-manual interactions.
6. Sound, tutorial, credits, jam page and browser build, before Nov 14 12:00 UTC. Do not modify the submitted game during judging.

## Jam integrity

The submission should be a **new original game**, not a reskin of the prior Mindforge game. Keep its source and asset credits, record provenance, disclose any generative AI use on the itch.io page, and verify official eligibility. A WebGL build running directly on itch.io is required for prize eligibility. A player must not need an unbundled sidecar to try the browser demo.

The parent repository is used as a development workspace only; for release, package this child Unity project independently. Do not include `../unity/Assets/Mindforge`, the prior game's scenes, or unlicensed assets.
