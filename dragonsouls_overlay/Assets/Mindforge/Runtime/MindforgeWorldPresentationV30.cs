using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Presentation-only identity layer for the Mindforge-owned copy of Dragon Souls'
    /// complete MainGameScene. It never owns movement, collision, AI, health, damage,
    /// progression or BCI authority. The source game's authored geometry and baked
    /// navigation remain untouched.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeWorldPresentationV30 : MonoBehaviour
    {
        public const string ProductVersion = "V0.30 Production Combat World";

        [SerializeField, Range(0f, 0.35f)] private float environmentTintStrength = 0.16f;
        [SerializeField] private bool installPostProcessing = true;
        [SerializeField] private bool rethemeStaticEnvironment = true;
        [SerializeField] private bool rethemeStandardEnemies = true;

        private Volume _volume;
        private VolumeProfile _runtimeProfile;

        public bool Installed { get; private set; }
        public int EnvironmentRenderersRethemed { get; private set; }
        public int EnemiesRethemed { get; private set; }

        private void Start()
        {
            ApplyAtmosphere();
            if (installPostProcessing) InstallPostProcessing();
            if (rethemeStaticEnvironment) EnvironmentRenderersRethemed = RethemeEnvironment();
            if (rethemeStandardEnemies) EnemiesRethemed = InstallEnemyPresentations();
            Installed = true;
        }

        private void OnDestroy()
        {
            if (_runtimeProfile != null)
                Destroy(_runtimeProfile);
        }

        private static void ApplyAtmosphere()
        {
            // Keep the upstream fog/skybox model and only steer its color response.
            // This avoids invalidating baked lighting or replacing scene illumination.
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.205f, 0.255f, 0.355f, 1f);
            RenderSettings.ambientIntensity = Mathf.Clamp(RenderSettings.ambientIntensity * 0.94f, 0.85f, 1.65f);
            RenderSettings.reflectionIntensity = Mathf.Clamp(RenderSettings.reflectionIntensity * 0.92f, 0.35f, 1.0f);
        }

        private void InstallPostProcessing()
        {
            _volume = GetComponent<Volume>();
            if (_volume == null) _volume = gameObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 40f;
            _volume.weight = 1f;

            _runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            _runtimeProfile.name = "Mindforge_V30_Runtime_PostFX";
            _volume.profile = _runtimeProfile;

            Bloom bloom = _runtimeProfile.Add<Bloom>(true);
            bloom.intensity.Override(0.30f);
            bloom.threshold.Override(1.05f);
            bloom.scatter.Override(0.54f);

            ColorAdjustments color = _runtimeProfile.Add<ColorAdjustments>(true);
            color.postExposure.Override(-0.04f);
            color.contrast.Override(7f);
            color.saturation.Override(-6f);
            color.colorFilter.Override(new Color(0.94f, 0.98f, 1.05f, 1f));

            Tonemapping tone = _runtimeProfile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            Vignette vignette = _runtimeProfile.Add<Vignette>(true);
            vignette.intensity.Override(0.11f);
            vignette.smoothness.Override(0.32f);
            vignette.rounded.Override(false);
        }

        private int RethemeEnvironment()
        {
            MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>(true);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            int changed = 0;

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (!renderer.enabled || !LooksLikeEnvironment(renderer.transform)) continue;
                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length != 1 || materials[0] == null) continue;

                Material material = materials[0];
                string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" :
                    material.HasProperty("_Color") ? "_Color" : null;
                if (colorProperty == null) continue;

                Color original = material.GetColor(colorProperty);
                Color target = PaletteFor(renderer.transform);
                target.a = original.a;
                Color adjusted = Color.Lerp(original, target, environmentTintStrength);

                renderer.GetPropertyBlock(block);
                block.SetColor(colorProperty, adjusted);
                renderer.SetPropertyBlock(block);
                changed++;
            }

            return changed;
        }

        private static int InstallEnemyPresentations()
        {
            EnemyStateMachine[] enemies = Object.FindObjectsOfType<EnemyStateMachine>(true);
            int installed = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyStateMachine enemy = enemies[i];
                if (enemy == null) continue;
                if (enemy.GetComponentInChildren<EnemyNightmareDragonController>(true) != null) continue;
                if (enemy.GetComponent<MindforgeEnemyPresentationV30>() == null)
                    enemy.gameObject.AddComponent<MindforgeEnemyPresentationV30>();
                installed++;
            }
            return installed;
        }

        private static bool LooksLikeEnvironment(Transform transform)
        {
            bool included = false;
            Transform current = transform;
            for (int depth = 0; current != null && depth < 7; depth++, current = current.parent)
            {
                string n = current.name.ToLowerInvariant();
                if (ContainsAny(n, "player", "enemy", "dragon", "weapon", "sword", "canvas", "ui", "camera"))
                    return false;
                if (ContainsAny(n, "environment", "terrain", "ground", "floor", "wall", "pillar", "column", "ruin", "castle", "rock", "cliff", "arch", "bridge", "stairs", "path", "road"))
                    included = true;
            }
            return included;
        }

        private static Color PaletteFor(Transform transform)
        {
            string n = transform.name.ToLowerInvariant();
            if (ContainsAny(n, "terrain", "ground", "path", "road"))
                return new Color(0.34f, 0.38f, 0.43f, 1f);
            if (ContainsAny(n, "rock", "cliff", "ruin"))
                return new Color(0.28f, 0.32f, 0.39f, 1f);
            return new Color(0.40f, 0.43f, 0.50f, 1f);
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
                if (value.Contains(tokens[i])) return true;
            return false;
        }
    }
}
