# Mindforge v7.8 Current Platform Audit

## Executive verdict

The recovered Mindforge v7.8 codebase is an unusually large and disciplined prototype, but it is not yet a competition-ready BCI game.

Its strongest work is in deterministic combat contracts, safety boundaries, fallback behavior, replay/evidence infrastructure, accessibility, and explicit separation between simulated and observed claims.

Its weakest work is exactly what BR41N.IO will care about most: a small number of scientifically grounded, physically validated, player-visible BCI mechanics that work reliably on real hardware and create an unforgettable game experience.

The platform became broader faster than the core experience became deeper.

For BR41N.IO 2026, Mindforge should therefore be **salvaged, not continued linearly**.

We should preserve the best contracts and rewrite the product around one polished vertical slice.

---

## Recovered source census

The v7.8 archive contains approximately:

- **7,434 files**
- **72.6 MB** uncompressed source/evidence content
- **4,118 JSON files**
- **982 Python files**
- **669 C# files**
- **551 Markdown files**
- **352 CSV files**
- **267 web demo files**
- **685 Unity files**
- **845 research files**
- hundreds of generated output/evidence directories

This is far beyond the surface area required for a hackathon game.

The quantity itself is not the problem. The problem is that it dilutes engineering attention and makes it difficult to identify which code owns the actual player experience.

---

# What is genuinely strong

## 1. Deterministic gameplay authority

The v7.8 combat design correctly separates gameplay truth from presentation.

World-space swept collision is authoritative. Screen-space projection is used for readability rather than damage. Render frame rate, resolution, VFX, accessibility settings, and camera framing are not allowed to silently change collision outcomes.

This is excellent and should survive.

## 2. Controller-complete design

The project refuses to make neural input responsible for every action.

The 180 ms Counter window remains controller-owned, dash has no hidden invulnerability, and the game can continue Controller-Only.

This is an excellent foundation for a hybrid BCI game.

## 3. Raw EEG boundary

The Unity layer is intentionally designed to consume derived neural features/events rather than raw EEG.

That is scientifically cleaner, more testable, and better for privacy.

This should become one of the new architecture's central invariants.

## 4. Evidence discipline

The historical code repeatedly distinguishes simulation/fixture evidence from observed hardware evidence.

The real-hardware adapter module explicitly marks itself `bench_only` and `observed_eligible: false` until physical qualification exists.

That conservative claims policy is a competitive advantage.

## 5. Failure handling philosophy

The project contains concepts for:

- stale-frame detection,
- packet gaps,
- reconnect attempts,
- signal-quality gating,
- fallback to Controller-Only,
- artifact-aware suppression,
- deterministic replay.

These are exactly the kinds of details that make a live BCI demo trustworthy.

## 6. Accessibility

The project consistently protects Controller-Only completion and keeps accessibility settings from mutating authoritative combat rules.

That should remain.

---

# What is not competition-ready

## P0: the physical Unicorn path is not authoritative

The top-level `hardware_abstraction/adapters/unicorn_adapter.py` currently returns a `SimulatedFeatureAdapter`.

A deeper research module, `research/src/mindforge_analysis/gtec_unicorn_adapter_v31.py`, contains a much better boundary:

- expected 250 Hz acquisition,
- canonical 8-channel montage,
- injectable vendor backend,
- optional LSL backend,
- packet-gap accounting,
- stale-frame detection,
- reconnect behavior,
- impedance snapshot hooks.

But it explicitly remains bench-only and not observed-eligible.

### Consequence

Mindforge currently has a hardware architecture, not a physically demonstrated hardware product.

### Action

Rewrite this into one canonical `neuro/acquisition/unicorn.py` implementation and qualify it on real hardware immediately.

---

## P0: the game-facing neural mechanic is too generic

`MindforgeFocusAdapterV70.cs` receives a `NeuralFrame` and forwards `frame.intentProbability` into a mock manager as “focus.”

This is elegant as an abstraction test, but scientifically weak as the centerpiece of a hackathon game.

It does not answer:

- What task is the participant performing?
- What EEG phenomenon is being decoded?
- What classifier generates `intentProbability`?
- What makes the feature identifiable from artifacts?
- Why does that neural variable belong in the game?

### Action

Replace the generic focus scalar with explicit derived event types:

- `ATTUNE_TARGET`
- `NEURAL_GUARD_READY`
- `RESONANCE_DELTA`
- `ABSTAIN`
- `BCI_LOST`
- `BCI_RECOVERED`

Each event must include paradigm, confidence, quality, and timestamp metadata.

---

## P0: no observed end-to-end live BCI loop

The recovered release itself states that it does not establish:

- physical consumer hardware timing,
- real setup time,
- observed fallback p95,
- Unity Player execution,
- external human fun/balance outcomes.

### Action

The next milestone must not be another architecture layer.

It must be:

```text
real Unicorn
→ real EEG
→ real decoder
→ derived neural event
→ Unity
→ visible game-state change
```

with video and telemetry evidence.

---

## P1: platform scope overwhelms the game

The repository contains major systems for:

- creator packages,
- Guardian Studio,
- federation,
- external governance,
- trusted exchange,
- co-op research,
- ecosystem observability,
- publication tooling,
- distribution,
- migration,
- longitudinal personalization,
- many version-specific output trees.

These are interesting future directions but mostly irrelevant to winning BR41N.IO 2026.

### Action

Move them out of the competition core.

The new codebase should have four obvious authorities:

1. `unity/` — game.
2. `neuro/` — EEG.
3. `shared/` — event protocol.
4. `experiments/` — evidence.

Everything else must justify its existence.

---

## P1: too many game systems before one exceptional encounter

v7.8 has:

- five canonical weapons,
- multiple Guardians,
- multiple worlds,
- campaign infrastructure,
- rematches,
- creator-authored content,
- traversal systems,
- environmental interactions.

This is a production-game roadmap, not the optimal hackathon scope.

### Action

Build one boss to an absurdly high standard.

A jury should remember **The Fractured Signal**, not a list of 30 systems.

---

## P1: the old scientific language risks semantic overreach

The game frequently uses “focus” as a generic neural control variable.

A scalar EEG feature can be useful operationally without being a valid universal measure of attention or focus.

### Action

Name the paradigm, not the fantasy interpretation.

The game can call the resulting diegetic mechanic “Resonance,” but the research layer should say something precise such as:

- P300 attended-target posterior,
- FBCCA target score,
- motor-imagery classifier margin,
- participant-calibrated spectral-control score.

---

## P1: test volume is not the same as product evidence

The historical validation suite is huge and valuable, but thousands of passing contract tests do not establish:

- fun,
- intuitiveness,
- physical EEG reliability,
- setup burden,
- live demo stability,
- audiovisual quality.

### Action

Preserve unit/integration tests, but shift the release gate toward observed evidence:

- real Unity builds,
- multi-user headset sessions,
- calibration outcomes,
- recorded decoder metrics,
- gameplay comprehension,
- full jury-length rehearsals.

---

# Component disposition

## KEEP

### Combat authority

- swept world-space collision principles
- explicit Counter timing
- deterministic state ownership
- input priority
- no neural auto-aim

### Neural boundary

- `INeuralProvider` concept
- derived feature/event boundary
- bounded buffering
- replay provider concept
- uncertainty-aware policy

### Hardware reliability concepts

- stale detection
- packet-gap accounting
- disconnect/reconnect
- fallback
- signal-quality gates

### Evidence

- deterministic replay
- session manifests
- explicit source classification
- observed-vs-simulated evidence labels

### Accessibility

- Controller-Only completeness
- reduced motion
- high contrast
- no accessibility-dependent collision changes

---

## SIMPLIFY

### Weapons

Keep one primary weapon and perhaps one alternate stance for the hackathon build.

Five weapons are unnecessary unless playtesting proves they improve the demo.

### Campaign

Reduce to:

1. calibration awakening,
2. tutorial room,
3. Fractured Signal boss,
4. post-run evidence screen.

### Telemetry

One beautiful competition telemetry overlay is more valuable than dozens of evidence reports.

### Build/release tooling

Keep only what protects reproducibility and demo reliability.

---

## REWRITE

### Unicorn acquisition

Create one physically validated production path.

### Decoder layer

Implement explicit P300/SSVEP/MI paradigms instead of generic focus.

### Unity neural bridge

Use typed derived events, monotonic timestamps, sequence numbers, and explicit abstention.

### Calibration

Make calibration both scientifically valid and diegetically integrated.

### Boss design

Build BCI mechanics into the boss's information structure rather than adding an optional buff on top of conventional combat.

---

## ARCHIVE

Move the following historical systems to a legacy/reference location rather than deleting their intellectual value:

- Creator Preview / Guardian Studio
- federation/exchange/governance systems
- co-op research
- long campaign content
- longitudinal personalization
- ecosystem systems
- large versioned evidence trees

They may become useful after the competition.

---

## DELETE FROM THE NEW CORE

Do not migrate:

- generated output directories that can be reproduced,
- duplicated version-specific manifests,
- obsolete web demos,
- simulated adapters masquerading behind device names,
- release scaffolding for versions no longer shipped,
- any feature with no direct competition or research justification.

---

# New product definition

The new Mindforge is not:

> a general BCI platform with a game attached.

It is:

> **a beautiful game whose architecture happens to make its BCI claims reproducible and trustworthy.**

That inversion should govern every implementation decision through October 5, 2026.
