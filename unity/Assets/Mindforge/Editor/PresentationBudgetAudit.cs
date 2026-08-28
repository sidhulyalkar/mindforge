using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Mindforge.SoulWisp;

namespace Mindforge.EditorTools
{
    /// <summary>
    /// Read-only scene audit for presentation cost. It makes rendering/VFX growth
    /// observable without changing quality settings or gameplay authority.
    /// </summary>
    public static class PresentationBudgetAudit
    {
        private const string MenuPath = "Mindforge/Showcase/Audit Presentation Budget";

        [Serializable]
        private sealed class AuditReport
        {
            public string schema = "mindforge.presentation_budget.v1";
            public string generated_utc;
            public int renderer_count;
            public int material_slots;
            public int unique_shared_materials;
            public int apparent_material_instances;
            public int particle_systems;
            public int particle_capacity;
            public int trail_renderers;
            public int line_renderers;
            public int realtime_shadow_lights;
            public int cameras;
            public int realtime_reflection_probes;
            public int wisp_shells;
            public int vep_stimuli;
            public List<string> warnings = new List<string>();
        }

        [MenuItem(MenuPath)]
        public static void Run()
        {
            AuditReport report = BuildReport();
            string output = ReportPath();
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? string.Empty);
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            AssetDatabase.Refresh();

            string warningText = report.warnings.Count == 0
                ? "none"
                : string.Join(" | ", report.warnings);
            Debug.Log(
                $"[Mindforge:PresentationAudit] renderers={report.renderer_count}, " +
                $"materials={report.unique_shared_materials}/{report.material_slots} slots, " +
                $"particles={report.particle_systems} capacity={report.particle_capacity}, " +
                $"shadowLights={report.realtime_shadow_lights}, cameras={report.cameras}, " +
                $"realtimeProbes={report.realtime_reflection_probes}, wispShells={report.wisp_shells}, " +
                $"vepStimuli={report.vep_stimuli}. warnings={warningText}. Report: {output}");
        }

        private static AuditReport BuildReport()
        {
            AuditReport report = new AuditReport
            {
                generated_utc = DateTime.UtcNow.ToString("O"),
            };

            Renderer[] renderers = UnityEngine.Object.FindObjectsOfType<Renderer>(true);
            HashSet<Material> sharedMaterials = new HashSet<Material>();
            HashSet<string> apparentInstances = new HashSet<string>();
            report.renderer_count = renderers.Length;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                Material[] materials = renderer.sharedMaterials;
                report.material_slots += materials.Length;
                foreach (Material material in materials)
                {
                    if (material == null) continue;
                    sharedMaterials.Add(material);
                    if (material.name.IndexOf("(Instance)", StringComparison.OrdinalIgnoreCase) >= 0)
                        apparentInstances.Add(material.name);
                }
            }
            report.unique_shared_materials = sharedMaterials.Count;
            report.apparent_material_instances = apparentInstances.Count;

            ParticleSystem[] particles = UnityEngine.Object.FindObjectsOfType<ParticleSystem>(true);
            report.particle_systems = particles.Length;
            foreach (ParticleSystem particle in particles)
                if (particle != null) report.particle_capacity += particle.main.maxParticles;

            report.trail_renderers = UnityEngine.Object.FindObjectsOfType<TrailRenderer>(true).Length;
            report.line_renderers = UnityEngine.Object.FindObjectsOfType<LineRenderer>(true).Length;
            report.cameras = UnityEngine.Object.FindObjectsOfType<Camera>(true).Length;
            report.wisp_shells = UnityEngine.Object.FindObjectsOfType<WispPresentationShell>(true).Length;
            report.vep_stimuli = UnityEngine.Object.FindObjectsOfType<VepAuraStimulus>(true).Length;

            Light[] lights = UnityEngine.Object.FindObjectsOfType<Light>(true);
            foreach (Light light in lights)
            {
                if (light != null && light.shadows != LightShadows.None)
                    report.realtime_shadow_lights++;
            }

            ReflectionProbe[] probes = UnityEngine.Object.FindObjectsOfType<ReflectionProbe>(true);
            foreach (ReflectionProbe probe in probes)
            {
                if (probe != null && probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime)
                    report.realtime_reflection_probes++;
            }

            if (report.apparent_material_instances > 0)
                report.warnings.Add("Apparent runtime/editor material instances detected; prefer shared materials or deliberate property overrides.");
            if (report.realtime_shadow_lights > 6)
                report.warnings.Add("More than six shadow-casting lights are active; profile shadow atlas and additional-light cost.");
            if (report.particle_capacity > 2400)
                report.warnings.Add("Aggregate ParticleSystem max-particle capacity exceeds 2400; verify actual overdraw and burst concurrency.");
            if (report.cameras > 2)
                report.warnings.Add("More than two cameras are active; verify stacking/teleport/debug cameras are not rendering simultaneously.");
            if (report.realtime_reflection_probes > 2)
                report.warnings.Add("Multiple realtime reflection probes are active; prefer deliberate update cadence or baked probes where possible.");
            if (report.vep_stimuli != 0 && report.vep_stimuli != 2)
                report.warnings.Add("Expected exactly two coded VEP stimuli in the reference encounter.");

            return report;
        }

        private static string ReportPath()
        {
            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            return Path.Combine(repoRoot, "experiments", "reports", "presentation-budget-latest.json");
        }
    }
}
