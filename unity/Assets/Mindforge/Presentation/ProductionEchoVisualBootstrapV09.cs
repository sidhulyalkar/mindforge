using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only runtime installer for Fractured Echo visuals. Boss Echoes are spawned
    /// after the scene is authored, so an editor-only pass cannot see every future instance.
    /// This component periodically discovers live Echo authority objects and adds only the
    /// production visual shell. It never changes combat cadence, health, collision or targeting.
    /// </summary>
    public sealed class ProductionEchoVisualBootstrapV09 : MonoBehaviour
    {
        [SerializeField] private Material shell;
        [SerializeField] private Material hostile;
        [SerializeField] private Material trim;
        [SerializeField] private float scanIntervalSeconds = 0.35f;

        private float _nextScan;

        public void ConfigureRuntime(Material shellMaterial, Material hostileMaterial, Material trimMaterial)
        {
            shell = shellMaterial;
            hostile = hostileMaterial;
            trim = trimMaterial;
        }

        private void OnEnable()
        {
            _nextScan = 0f;
            ApplyToExistingEchoes();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + Mathf.Max(0.1f, scanIntervalSeconds);
            ApplyToExistingEchoes();
        }

        private void ApplyToExistingEchoes()
        {
            if (shell == null || hostile == null || trim == null) return;
            FracturedEchoNode[] echoes = FindObjectsOfType<FracturedEchoNode>(true);
            for (int i = 0; i < echoes.Length; i++)
            {
                FracturedEchoNode echo = echoes[i];
                if (echo == null) continue;
                ProductionEchoVisualV09 visual = echo.GetComponent<ProductionEchoVisualV09>();
                if (visual == null) visual = echo.gameObject.AddComponent<ProductionEchoVisualV09>();
                visual.ConfigureRuntime(shell, hostile, trim);
            }
        }
    }
}
