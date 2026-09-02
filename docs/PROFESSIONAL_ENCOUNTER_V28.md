# V0.28 Professional Creature + World Staging

V0.28 is a replacement pass, not another polishing layer on top of the V0.27 procedural beast.

The recording-driven goals are straightforward: the Guardian and boss must remain visually separable, the boss must read as an authored animal rather than generated primitive anatomy, sword contact must agree with the visible body, and additional world detail must strengthen the white-cathedral composition without consuming traversal space.

## Authority map

V0.28 deliberately keeps the existing ownership boundaries:

- `FracturedSignalFirstBossV19` remains the only first-boss locomotion owner.
- `FracturedSignalDirector` and the existing melee/projectile systems remain attack authority.
- `GuardianSwordShieldController` remains sword contact/damage authority.
- V0.23 remains world collision/foundation authority.
- V0.17 remains ordinary camera orbit, FOV and framing authority.
- calibration/Wisp systems remain neural visual-field authority.

The new systems supply presentation, a derived trigger-only hurt envelope, a minimum separation rule inside the existing movement owner, and deterministic decorative staging.

## 1. Minimum separation instead of interpenetration

The boss locomotion now carries an explicit `minimumSeparationDistance` of 2.75 m. If the Guardian penetrates this envelope, the same authoritative boss movement controller backs the boss away before ordinary orbit/attack recovery holds are honored.

This is intentionally not a second push component. It avoids two movement writers fighting over the same Rigidbody and keeps separation behavior inside the controller that already owns boss movement.

The separation response still freezes during calibration or Wisp resonance windows.

## 2. Authored creature instead of procedural dinosaur

V0.27's generated body is retired when V0.28 is present.

The new creature starts from Gobkit's CC0 `Rhino.glb`, pinned to upstream commit:

`0d654ab3306515b1b63621a5c6548554034482dc`

The exact source Git blob SHA-1 is:

`f638b1cf00a6472192beb85b1a4162535bfc189e`

The asset contains rigged/animated quadruped anatomy and authored idle, walk, attack and death motion. Mindforge normalizes the model to a roughly 3.7 m encounter length and grounds it from its actual renderer bounds. Imported root motion is disabled: the source rig animates anatomy while Mindforge remains responsible for world movement.

`FracturedSignalCreaturePresentationV28` samples the imported clips from existing movement, attack and death state. During neural visual fields the creature is sampled at a static neutral idle frame.

## 3. Render-derived hurt envelope

The previous humanoid trigger capsule was too small for a broad quadruped. V0.28 derives local boss-space bounds from the imported renderers and builds four trigger-only anatomical sword-contact volumes:

- head,
- chest,
- midbody,
- rear body.

These children contain no Rigidbody and do not deal damage. Guardian sword contact still comes from the existing swept capsule in `GuardianSwordShieldController`, which resolves `CombatantVitals` from the hit collider and deduplicates each receiver per swing.

The old V0.22 boss trigger hull is disabled once the V0.28 hurt envelope is authored.

## 4. Actor occlusion guard

`MindforgeActorOcclusionGuardV28` is a bounded post-resolver behind the V0.17 gameplay camera.

It does not own FOV, target lock, orbit input or normal camera distance. It only activates when the locked target's actual renderer bounds intersect the camera-to-Guardian sight corridor, or when the camera enters those bounds. The correction is capped to a small lateral and upward displacement and never intentionally moves the camera closer to the target.

The guard is disabled for the entire neural visual-field interval.

## 5. CC0 socketed world staging

V0.28 uses a deliberately tiny subset of KayKit Dungeon Remastered rather than importing the whole pack. The source is CC0 and pinned to:

`b0ca9bd96a8072ab36a3a5464f00ed1e06a16d07`

The selected pieces are a white banner, mounted torch, chair, decorated small table and gold chest.

They are placed only from explicit authored sockets:

- Memory Forge side chapels,
- processional nave wall rhythm,
- Market/cloister side alcoves.

No random scatter is used. The processional center maintains a 3.15 m half-width clear corridor and the Fractured Signal arena keeps a 14.4 m clear radius. Pairwise prop-overlap validation prevents decorative stacks from collapsing into visual clutter.

Decorative imported props are collider-free and use Mindforge's existing cathedral materials, so V0.23 remains world collision authority and the new objects inherit the white-cathedral palette rather than bringing a second incompatible material language.

## 6. Reproducible public-art acquisition

`PublicAssetAcquisitionV28` performs Editor-only acquisition from immutable raw GitHub commit URLs. Each response is checked with the same SHA-1 object format Git uses for blobs before it is admitted to `Assets/Mindforge/Generated/V28/ThirdParty`.

KayKit OBJ sources are verified first, then their `mtllib` reference is removed and material import is disabled because Mindforge supplies its own materials.

The generated third-party cache is build output rather than hand-authored source. The complete source commit, source path, Git blob hash, generated path, role and license are recorded in `third_party/manifest.json`.

UnityGLTF is pinned to Khronos `release/2.20.0` under its MIT license and is used only to import glTF art. It does not supply gameplay, BCI, collision or boss logic.

## Validation gate

The V0.28 builder fails closed if any of these contracts are broken:

- imported authored creature is missing or has implausible normalized dimensions;
- required idle/walk/attack clips are unavailable;
- the four anatomical hurt boxes are missing, non-trigger, or introduce a Rigidbody;
- minimum boss-player separation is below the readability threshold;
- actor occlusion guard is absent;
- decorative staging contains a Collider or Rigidbody;
- fewer than sixteen staged detail objects are present;
- a staged prop enters the protected processional corridor or boss clear radius;
- staged props overlap excessively.

## Focused playtest

The next local Unity capture should specifically test front/flank/rear sword contact, walking directly into the boss, boss retreat during an attack commitment, target-lock camera behavior at point-blank range, full arena circulation, and a Wisp window. Then traverse the Memory Forge, nave and Market slowly enough to judge prop rhythm and whitespace.

The acceptance criterion is not simply “more detail.” The world should read as one authored cathedral with deliberate negative space, and the boss should read as a physically present corrupted quadruped whose visible anatomy, hit response and camera behavior agree.
