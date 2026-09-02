using States;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Runtime installer for the V0.31 production slice. It uses the inherited
    /// Dragon Souls world and state machines, then installs camera, crowd-spacing,
    /// hit-feedback, HUD and restrained world-look presentation on top.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    public sealed class MindforgeVerticalSliceRuntimeV31 : MonoBehaviour
    {
        public const string ProductVersion = "V0.31 Production Vertical Slice";

        [Header("World look")]
        [SerializeField] private float terrainDetailDensity = 0.22f;
        [SerializeField] private float terrainDetailDistance = 18f;
        [SerializeField] private float terrainTreeDistance = 190f;
        [SerializeField] private float shadowDistance = 72f;

        private Volume _volume;
        private VolumeProfile _profile;

        public bool Installed { get; private set; }
        public int EnemiesConfigured { get; private set; }
        public int FeedbackOwnersConfigured { get; private set; }
        public int TerrainsCurated { get; private set; }

        private void Start()
        {
            InstallCamera();
            InstallCombatPresentation();
            InstallHud();
            CurateTerrains();
            InstallWorldLook();
            Installed = true;
        }

        private void OnDestroy()
        {
            if (_profile != null) Destroy(_profile);
        }

        private void InstallCamera()
        {
            if (GetComponent<MindforgeProductionCameraV31>() == null)
                gameObject.AddComponent<MindforgeProductionCameraV31>();
        }

        private void InstallHud()
        {
            if (GetComponent<MindforgeHudPresentationV31>() == null)
                gameObject.AddComponent<MindforgeHudPresentationV31>();
        }

        private void InstallCombatPresentation()
        {
            PlayerStateMachine player = FindObjectOfType<PlayerStateMachine>();
            if (player != null && player.GetComponent<MindforgeCombatFeedbackV31>() == null)
            {
                player.gameObject.AddComponent<MindforgeCombatFeedbackV31>();
                FeedbackOwnersConfigured++;
            }

            EnemyStateMachine[] enemies = FindObjectsOfType<EnemyStateMachine>();
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyStateMachine enemy = enemies[i];
                if (enemy == null || enemy.GetComponentInChildren<EnemyNightmareDragonController>(true) != null)
                    continue;

                if (enemy.GetComponent<MindforgeEnemyFormationV31>() == null)
                    enemy.gameObject.AddComponent<MindforgeEnemyFormationV31>();
                if (enemy.GetComponent<MindforgeCombatFeedbackV31>() == null)
                {
                    enemy.gameObject.AddComponent<MindforgeCombatFeedbackV31>();
                    FeedbackOwnersConfigured++;
                }
                EnemiesConfigured++;
            }
        }

        private void CurateTerrains()
        {
            Terrain[] terrains = FindObjectsOfType<Terrain>();
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null) continue;
                terrain.detailObjectDensity = Mathf.Min(terrain.detailObjectDensity, terrainDetailDensity);
                terrain.detailObjectDistance = Mathf.Min(terrain.detailObjectDistance, terrainDetailDistance);
                terrain.treeDistance = Mathf.Min(terrain.treeDistance, terrainTreeDistance);
                terrain.treeBillboardDistance = Mathf.Min(terrain.treeBillboardDistance, 70f);
                terrain.basemapDistance = Mathf.Max(terrain.basemapDistance, 850f);
                TerrainsCurated++;
            }
        }

        private void InstallWorldLook()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0085f;
            RenderSettings.fogColor = new Color(0.115f, 0.155f, 0.225f, 1f);
            RenderSettings.ambientIntensity = Mathf.Clamp(RenderSettings.ambientIntensity * 0.88f, 0.72f, 1.35f);
            RenderSettings.reflectionIntensity = Mathf.Clamp(RenderSettings.reflectionIntensity * 0.86f, 0.28f, 0.90f);
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, shadowDistance);

            _volume = GetComponent<Volume>();
            if (_volume == null) _volume = gameObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 80f;
            _volume.weight = 1f;

            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "Mindforge_V31_Runtime_PostFX";
            _volume.profile = _profile;

            ColorAdjustments color = _profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(-0.10f);
            color.contrast.Override(14f);
            color.saturation.Override(-18f);
            color.colorFilter.Override(new Color(0.93f, 0.975f, 1.055f, 1f));

            WhiteBalance balance = _profile.Add<WhiteBalance>(true);
            balance.temperature.Override(-7f);
            balance.tint.Override(-1f);

            Bloom bloom = _profile.Add<Bloom>(true);
            bloom.intensity.Override(0.34f);
            bloom.threshold.Override(1.08f);
            bloom.scatter.Override(0.48f);

            Tonemapping tone = _profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            Vignette vignette = _profile.Add<Vignette>(true);
            vignette.intensity.Override(0.14f);
            vignette.smoothness.Override(0.28f);
            vignette.rounded.Override(false);
        }
    }
}
