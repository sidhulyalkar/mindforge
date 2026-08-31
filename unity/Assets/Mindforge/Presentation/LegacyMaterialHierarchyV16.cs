using System;
using System.Collections;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Rebalances legacy blockout color hierarchy without replacing meshes, colliders or
    /// authored PBR materials. The recording showed large near-black architectural masses
    /// collapsing into one silhouette. This pass uses MaterialPropertyBlock so the change is
    /// local, reversible, allocation-light, and cannot create gameplay authority.
    ///
    /// The authored V0.9 production-art root is deliberately excluded. Its PBR base colors
    /// are material truth, not legacy blockout values, and V0.16 must never recolor them.
    /// Periodic/emissive neural targets are explicit exclusions as well. The one-time restyle
    /// waits until no baseline/calibration/resonance epoch owns the retinal field.
    /// </summary>
    public sealed class LegacyMaterialHierarchyV16 : MonoBehaviour
    {
        private static readonly string[] RootNames =
        {
            // The clean V0.11 world is the canonical scene assembled by Mindforge > Latest.
            // Keep it first so recording-driven restyling always targets the actually played scene.
            "Mindforge_Demo_World_V11",
            "Mindforge_AetheriaWorld_V1",
            "Mindforge_GroundedWorld_V1",
            "Mindforge_Demo_Environment_V15",
        };

        private static readonly string[] PreserveTokens =
        {
            "SightVepCore",
            "GuardVepCore",
            "Photodiode",
            "Wisp",
            "Signal",
            "Rune",
            "Core",
            "Energy",
            "Visor",
            "Blade",
            "Halo",
        };

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private int _restyled;

        public int RestyledRendererCount => _restyled;

        public void Configure(AwakeningCalibrationDirector calibration, SoulWispController wisp)
        {
            _calibration = calibration;
            _wisp = wisp;
        }

        private IEnumerator Start()
        {
            while (NeuralEvidenceOwnsVisualField()) yield return null;
            ApplyHierarchy();
        }

        private bool NeuralEvidenceOwnsVisualField()
        {
            if (_calibration == null) _calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            if (_wisp == null) _wisp = FindObjectOfType<SoulWispController>(true);
            return (_calibration != null && _calibration.CalibrationInProgress) ||
                   (_wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive));
        }

        private void ApplyHierarchy()
        {
            _restyled = 0;
            for (int r = 0; r < RootNames.Length; r++)
            {
                GameObject root = VisualIdentityV16Installer.FindSceneObject(RootNames[r]);
                if (root == null) continue;
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null || ShouldPreserve(renderer.gameObject.name)) continue;
                    Material material = renderer.sharedMaterial;
                    if (material == null) continue;
                    if (material.IsKeywordEnabled("_EMISSION")) continue;

                    Color source = ReadBaseColor(material);
                    if (!ShouldRestyle(renderer.gameObject.name, source)) continue;
                    Color target = PaletteFor(renderer.gameObject.name, source.a);

                    renderer.GetPropertyBlock(_block);
                    if (material.HasProperty(BaseColorId)) _block.SetColor(BaseColorId, target);
                    else if (material.HasProperty(ColorId)) _block.SetColor(ColorId, target);
                    else continue;
                    renderer.SetPropertyBlock(_block);
                    _restyled++;
                }
            }

            Debug.Log($"[Mindforge:V16] Material hierarchy restyled {_restyled} legacy renderers; authored production PBR and coded/emissive signals preserved.");
        }

        private static bool ShouldPreserve(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return false;
            for (int i = 0; i < PreserveTokens.Length; i++)
                if (objectName.IndexOf(PreserveTokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        private static bool ShouldRestyle(string objectName, Color source)
        {
            float luminance = source.r * 0.2126f + source.g * 0.7152f + source.b * 0.0722f;
            if (luminance <= 0.18f) return true;
            return HasAny(objectName,
                "Wall", "Tower", "Monolith", "Pillar", "Column", "Arch", "Rib", "Canopy",
                "Bridge", "Road", "Promenade", "Floor", "Ground", "Plate", "Facade", "Spire");
        }

        private static Color PaletteFor(string objectName, float alpha)
        {
            if (HasAny(objectName, "Floor", "Ground", "Road", "Promenade", "Bridge", "Plate"))
                return new Color(0.16f, 0.19f, 0.23f, alpha);
            if (HasAny(objectName, "Arch", "Rib", "Column", "Pillar", "Facade"))
                return new Color(0.46f, 0.47f, 0.45f, alpha);
            if (HasAny(objectName, "Crown", "Rail", "Pylon", "Trim", "Metal"))
                return new Color(0.29f, 0.34f, 0.39f, alpha);
            if (HasAny(objectName, "Tower", "Monolith", "Wall", "Spire", "Canopy"))
                return new Color(0.105f, 0.125f, 0.16f, alpha);
            return new Color(0.13f, 0.15f, 0.19f, alpha);
        }

        private static Color ReadBaseColor(Material material)
        {
            if (material.HasProperty(BaseColorId)) return material.GetColor(BaseColorId);
            if (material.HasProperty(ColorId)) return material.GetColor(ColorId);
            return Color.white;
        }

        private static bool HasAny(string source, params string[] tokens)
        {
            if (string.IsNullOrEmpty(source)) return false;
            for (int i = 0; i < tokens.Length; i++)
                if (source.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }
    }
}
