# Mindforge engineering foundation and public toolbase policy

Mindforge should reuse mature public tools aggressively **at the edges** and keep gameplay/neural authority small, deterministic, and auditable **at the center**.

This document defines the development foundation after V0.17 and the first Unity-native lifecycle failures found during real Editor testing.

## The three qualification gates

A green result in one layer never implies a pass in another.

### 1. Repository / authority gate

Owned by `.github/workflows/test-neuro.yml` and the Python/source contract suite.

This gate validates, among other things:

- exact source revision and promotion evidence;
- neural-event schema and stimulus-epoch authority;
- Wisp/locomotion authority separation;
- no pre-window EEG replay into a new Wisp decision;
- presentation code cannot silently take gameplay/neural authority;
- public-data qualification tooling;
- Content Foundry and browser-combat contracts;
- static Unity lifecycle hazards that can be recognized safely from source.

This gate is fast and mandatory, but it is **not a Unity compiler/runtime**.

### 2. Unity-native lifecycle gate

The project already depends on Unity Test Framework (`com.unity.test-framework`).
`unity/Assets/Mindforge/Tests/Editor/MindforgeUnityLifecycleSmokeTests.cs` contains the first native Editor smoke tests.

`.github/workflows/unity-lifecycle-smoke.yml` integrates the maintained GameCI `unity-test-runner` on the project-pinned Unity version, `2022.3.62f3`.

Until Unity CI credentials are configured, the workflow must report **SKIPPED**, never PASS. A local Unity Editor run remains the native authority in that state.

The first native smoke target protects against the exact class of failure where a `MonoBehaviour` constructs a native Unity object such as `MaterialPropertyBlock` from an instance field initializer instead of `Awake`, `OnEnable`, `Start`, or an explicit runtime build method.

Future native tests should remain small and high-value:

- canonical scene can compile/open;
- latest runtime components can be instantiated without lifecycle exceptions;
- one canonical camera/HUD owner after handoff;
- VEP pair exists with intended software frequency/refresh contract;
- readiness audit can run without exceptions;
- controller-only demo reaches a stable first playable frame.

### 3. Physical SSVEP/display gate

This gate is intentionally impossible to satisfy in ordinary CI.

It requires the final display and real acquisition hardware to validate:

- actual photon timing with a photodiode;
- physical 10/12 Hz presentation and dropped-frame behavior;
- EEG synchronization and post-onset evidence ownership;
- participant calibration and target-specific normalization;
- accepted-command precision, latency, abstention and false-activation rate;
- natural-combat performance.

`physical_ssvep_qualified` must remain false until those measurements exist.

## Public toolbases we should use

### Adopt now

**Unity Test Framework**

Official Unity-native tests. Already installed. Use it for lifecycle, serialization and canonical-scene smoke tests that Python cannot prove.

**GameCI**

Use its maintained GitHub Actions for Unity test/build execution instead of maintaining a custom Docker/Unity activation stack. Pin major action versions and the repository's exact Unity version. Keep credentials in GitHub secrets, never the repository.

**URP**

Continue using the official Universal Render Pipeline already pinned by the project. Prefer its supported rendering features over custom render infrastructure unless a measured requirement demands otherwise.

**MNE / MOABB / SciPy stack**

Use maintained scientific tooling for public EEG datasets, filtering/reference implementations and reproducible dataset adapters. Do not copy research notebook code into gameplay authority when an isolated adapter/benchmark can consume it instead.

### Evaluate deliberately, not automatically

**Cinemachine**

Potentially valuable for ordinary third-person framing, damping, collision and authored camera sequences. It should only replace custom normal-combat camera code after an A/B integration proves it improves maintainability. During calibration/Wisp evidence, Mindforge still requires a fixed-FOV, controlled camera pose, so no camera framework may own retinal geometry during a neural epoch.

**Unity Input System**

Useful for controller/device abstraction and remapping, but migrating a stable control layer during BCI qualification creates unnecessary churn. Adopt when multi-device/remapping requirements justify the migration, not because it is newer.

**Open-source art/VFX/navigation/tool packages**

Prefer small, permissively licensed packages with clear ownership boundaries. Treat imported visual code as presentation authority only. Do not permit third-party update loops to write movement, combat, target-lock, stimulus phase, or neural selection state without an explicit adapter and contract test.

## Import policy

Before incorporating public code or assets:

1. Verify the license and preserve required attribution/notices.
2. Prefer dependency use or a thin adapter over copying source wholesale.
3. Pin versions/commit SHAs for anything that affects deterministic builds.
4. Keep external packages outside the BCI/combat authority kernel whenever possible.
5. Add a regression test for the ownership boundary the dependency crosses.
6. Benchmark before replacing working code. A dependency is not an improvement merely because it has more stars.
7. Never let an imported visual system alter coded VEP luminance, timing, geometry or camera pose during an evidence window.

## Canonical development loop

The ordinary loop should remain boring:

1. Pull `main`.
2. Open the project with Unity `2022.3.62f3`.
3. Require a clean compile before Play Mode.
4. Run `Mindforge > Latest > PLAY LATEST (BCI Simulation)`.
5. Run `Mindforge > Latest > Validate Latest Readiness` during Play Mode.
6. Treat any red Unity Console entry as a release blocker and add a native/static regression for its class before continuing feature development.
7. Use the neural-hardware variant only after the software/runtime path is clean and the display/hardware qualification plan is ready.

The goal is not zero dependencies. The goal is a **small trustworthy core surrounded by replaceable, well-tested public tooling**.
