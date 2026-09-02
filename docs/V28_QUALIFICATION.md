# V0.28 qualification snapshot

Candidate head: `28d1a1deba60b7813559e6534f8a21d09057d0e1`

## Observed

- exact-head software workflow: `33595129977`
- pytest: **708 passed**, 0 failed, 0 errors, 0 skipped
- software promotion gate: **P0 PASS**
- Python demo/qualification tooling: PASS
- Content Foundry validation: PASS
- browser combat module syntax: PASS
- exact-head software evidence artifact: `9833123876`
- artifact digest: `sha256:ecb5143a2c17f9ebfd7d141501587e77549733eefcc7c84e869067b1e44bab21`

## Not observed

Unity lifecycle workflow `33595129915` completed its wrapper successfully, but the actual **Unity 2022.3.62f3 lifecycle smoke-test step was skipped** because no Unity CI license is configured. Therefore:

- P1 clean-checkout Unity assembly/validation: **UNOBSERVED**
- imported Gobkit creature orientation/animation: **UNOBSERVED**
- UnityGLTF import on the canonical project: **UNOBSERVED**
- V0.28 KayKit scene dressing scale/grounding: **UNOBSERVED**
- runtime separation/camera feel: **UNOBSERVED**
- full controller-only encounter: **UNOBSERVED**

A green workflow wrapper must not be cited as native Unity compile/import evidence.

## Promotion decision

Keep PR #53 **draft** until the focused V0.28 local playtest has been completed with `Mindforge → Latest → PLAY LATEST (BCI Simulation)` and the resulting Unity Console plus gameplay recording have been reviewed.

Use `docs/V28_PLAYTEST_CARD.md` for the intended short qualification route.
