#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Consolidates the accumulated opening-light stack into one global sun plus two restrained,
    /// shadow-free architectural accents. Earlier showcase generations are still authored before
    /// V0.9, but their overlapping Sanctum lights no longer flatten the production PBR forms.
    /// Arena-local lighting outside the opening is deliberately left alone.
    /// </summary>
    public static class ProductionLightingV09Builder
    {
        public const string RootName = "Production_Lighting_Consolidation_V09";
        public const int MaxOpeningAccentLights = 3;

        private static readonly string[] OpeningLightNames =
        {
            "SanctumFillA",
            "SanctumFillB",
            "ThresholdFill",
            "CourtFill",
        };

        [MenuItem("Mindforge/Legacy/Showcase/Apply Production Lighting Consolidation V0.9", priority = 46)]
        public static void ApplyOpenScene()
        {
            GameObject production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
            GameObject sanctum = EditorSceneLookup.FindIncludingInactive(SanctumOnboardingV08Builder.RootName);
            if (production == null || sanctum == null)
                throw new InvalidOperationException("Production lighting V0.9 requires Production Art and Sanctum V0.8.");

            Transform previous = production.transform.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            GameObject marker = new GameObject(RootName);
            marker.transform.SetParent(production.transform, false);

            Light sun = ResolveProductionSun();
            int disabledDirectionals = EnforceSingleDirectionalSun(sun);
            int openingLights = ConfigureOpeningAccents();
            ConfigureForgeAccent();
            ValidateLighting(sun, openingLights);

            EditorUtility.SetDirty(marker);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[Mindforge:V09:Lighting] Production light hierarchy consolidated: primary sun={sun.name}; " +
                $"disabled duplicate directionals={disabledDirectionals}; active opening accents={openingLights}/{MaxOpeningAccentLights}. " +
                "No gameplay, neural timing, collision or later arena-light authority changed.");
        }

        private static Light ResolveProductionSun()
        {
            Light sun = RenderSettings.sun;
            if (sun != null && sun.type == LightType.Directional)
            {
                sun.enabled = true;
                ConfigureSun(sun);
                return sun;
            }

            Light[] lights = UnityEngine.Object.FindObjectsOfType<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                Light candidate = lights[i];
                if (candidate == null || candidate.type != LightType.Directional) continue;
                candidate.enabled = true;
                ConfigureSun(candidate);
                RenderSettings.sun = candidate;
                return candidate;
            }

            GameObject go = new GameObject("ProductionSunV09");
            Light created = go.AddComponent<Light>();
            created.type = LightType.Directional;
            ConfigureSun(created);
            RenderSettings.sun = created;
            return created;
        }

        private static void ConfigureSun(Light sun)
        {
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.intensity = 1.16f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.88f;
            sun.shadowBias = 0.035f;
            sun.shadowNormalBias = 0.24f;
            sun.useColorTemperature = true;
            sun.colorTemperature = 5350f;
            sun.transform.rotation = Quaternion.Euler(43f, -37f, 0f);
            EditorUtility.SetDirty(sun);
        }

        private static int EnforceSingleDirectionalSun(Light sun)
        {
            int disabled = 0;
            Light[] lights = UnityEngine.Object.FindObjectsOfType<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null || light == sun || light.type != LightType.Directional) continue;
                if (!light.enabled) continue;
                light.enabled = false;
                EditorUtility.SetDirty(light);
                disabled++;
            }
            RenderSettings.sun = sun;
            return disabled;
        }

        private static int ConfigureOpeningAccents()
        {
            int active = 0;
            for (int i = 0; i < OpeningLightNames.Length; i++)
            {
                GameObject go = EditorSceneLookup.FindIncludingInactive(OpeningLightNames[i]);
                Light light = go != null ? go.GetComponent<Light>() : null;
                if (light == null) continue;

                bool keep = string.Equals(light.name, "SanctumFillA", StringComparison.Ordinal) ||
                            string.Equals(light.name, "ThresholdFill", StringComparison.Ordinal);
                light.enabled = keep;
                light.shadows = LightShadows.None;
                if (keep)
                {
                    if (string.Equals(light.name, "SanctumFillA", StringComparison.Ordinal))
                    {
                        light.color = new Color(0.55f, 0.72f, 0.92f);
                        light.intensity = 0.42f;
                        light.range = 15f;
                    }
                    else
                    {
                        light.color = new Color(1f, 0.78f, 0.52f);
                        light.intensity = 0.50f;
                        light.range = 14f;
                    }
                    active++;
                }
                EditorUtility.SetDirty(light);
            }
            return active;
        }

        private static void ConfigureForgeAccent()
        {
            GameObject altar = EditorSceneLookup.FindIncludingInactive("Memory_Forge_Sanctum_Altar_V08");
            if (altar == null) return;
            Light[] lights = altar.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null) continue;
                light.enabled = true;
                light.type = LightType.Point;
                light.color = new Color(0.36f, 0.76f, 0.92f);
                light.intensity = 0.52f;
                light.range = 5.5f;
                light.shadows = LightShadows.None;
                EditorUtility.SetDirty(light);
            }
        }

        private static void ValidateLighting(Light sun, int openingAccents)
        {
            if (sun == null || !sun.enabled || sun.type != LightType.Directional || sun.shadows == LightShadows.None)
                throw new InvalidOperationException("Production lighting requires one enabled shadowed directional sun.");
            if (RenderSettings.sun != sun)
                throw new InvalidOperationException("RenderSettings.sun must reference the production sun.");
            if (openingAccents > MaxOpeningAccentLights)
                throw new InvalidOperationException($"Opening accent-light budget exceeded: {openingAccents}/{MaxOpeningAccentLights}.");

            int enabledDirectionals = 0;
            Light[] lights = UnityEngine.Object.FindObjectsOfType<Light>(true);
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null && lights[i].enabled && lights[i].type == LightType.Directional)
                    enabledDirectionals++;
            if (enabledDirectionals != 1)
                throw new InvalidOperationException("Production lighting requires exactly one enabled directional light; found " + enabledDirectionals + ".");
        }
    }
}
#endif
