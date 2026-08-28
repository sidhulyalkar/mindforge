using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only readability layer for the combat arena. It preserves the
    /// midnight/cyan/copper palette while preventing silhouettes and floor structure
    /// from collapsing into black during gameplay. It never changes combat colliders,
    /// player/boss state, neural evidence, or stimulus timing.
    /// </summary>
    public sealed class ArenaVisibilityDirector : MonoBehaviour
    {
        [Header("Ambient readability")]
        [SerializeField] private Color ambientSky = new Color(0.075f, 0.12f, 0.24f);
        [SerializeField] private Color ambientEquator = new Color(0.055f, 0.075f, 0.15f);
        [SerializeField] private Color ambientGround = new Color(0.018f, 0.025f, 0.055f);
        [SerializeField] private Color fogColor = new Color(0.012f, 0.025f, 0.058f);
        [SerializeField] private float fogDensity = 0.0042f;
        [SerializeField] private float reflectionIntensity = 1.0f;

        [Header("Combat readability light")]
        [SerializeField] private Color readabilityColor = new Color(0.72f, 0.82f, 1.0f);
        [SerializeField] private float readabilityIntensity = 1.65f;
        [SerializeField] private float readabilityRange = 28f;
        [SerializeField] private float readabilitySpotAngle = 76f;

        private Light _readabilityLight;

        private void Start()
        {
            ApplyAmbientReadability();
            TuneAuthoredLights();
            EnsureReadabilityLight();
        }

        private void ApplyAmbientReadability()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = Mathf.Clamp(fogDensity, 0f, 0.02f);
            RenderSettings.fogColor = fogColor;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.reflectionIntensity = Mathf.Max(0f, reflectionIntensity);
        }

        private void TuneAuthoredLights()
        {
            Light key = FindLight("KeyLight");
            if (key != null)
            {
                key.intensity = Mathf.Max(key.intensity, 1.42f);
                key.color = new Color(0.96f, 0.88f, 0.78f);
                key.shadowStrength = 0.78f;
            }

            Light fill = FindLight("ArenaV3IndigoFill");
            if (fill != null)
            {
                fill.intensity = Mathf.Max(fill.intensity, 2.85f);
                fill.color = new Color(0.22f, 0.28f, 0.82f);
            }

            Light rim = FindLight("ArenaV3CyanRim");
            if (rim != null)
            {
                rim.intensity = Mathf.Max(rim.intensity, 3.05f);
                rim.color = new Color(0.04f, 0.68f, 1f);
            }
        }

        private void EnsureReadabilityLight()
        {
            GameObject existing = GameObject.Find("ArenaCombatReadabilityLight");
            GameObject go = existing != null ? existing : new GameObject("ArenaCombatReadabilityLight");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, 13.5f, -2.2f);
            go.transform.rotation = Quaternion.Euler(68f, 0f, 0f);

            _readabilityLight = go.GetComponent<Light>();
            if (_readabilityLight == null) _readabilityLight = go.AddComponent<Light>();
            _readabilityLight.type = LightType.Spot;
            _readabilityLight.color = readabilityColor;
            _readabilityLight.intensity = readabilityIntensity;
            _readabilityLight.range = readabilityRange;
            _readabilityLight.spotAngle = readabilitySpotAngle;
            _readabilityLight.innerSpotAngle = 54f;
            _readabilityLight.shadows = LightShadows.None;
        }

        private static Light FindLight(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            return go != null ? go.GetComponent<Light>() : null;
        }
    }
}
