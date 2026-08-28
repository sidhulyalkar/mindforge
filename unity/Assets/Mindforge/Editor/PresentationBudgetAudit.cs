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
        private sealed class ZoneBudget
        {
            public string id;
            public int renderer_count;
            public int material_slots;
            public long estimated_triangles;
            public int transparent_material_slots;
            public int batching_static_renderers;
            public int particle_systems;
            public int particle_capacity;
            public int line_renderers;
            public int lights;
            public int shadow_lights;
        }

        [Serializable]
        private sealed class AuditReport
        {
            public string schema = "mindforge.presentation_budget.v1";
            public string generated_utc;
            public int renderer_count;
            public int material_slots;
            public int unique_shared_materials;
            public int apparent_material_instances;
            public long estimated_triangles;
            public int transparent_material_slots;
            public int batching_static_renderers;
            public int particle_systems;
            public int particle_capacity;
            public int trail_renderers;
            public int line_renderers;
            public int realtime_shadow_lights;
            public int cameras;
            public int realtime_reflection_probes;
            public int wisp_shells;
            public int vep_stimuli;
            public List<ZoneBudget> null_ward_zones = new List<ZoneBudget>();
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
                $"triangles~={report.estimated_triangles}, batchingStatic={report.batching_static_renderers}, " +
                $"materials={report.unique_shared_materials}/{report.material_slots} slots, " +
                $"transparentSlots={report.transparent_material_slots}, " +
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
            Dictionary<string, ZoneBudget> zones = CreateZoneBudgets(report);

            Renderer[] renderers = UnityEngine.Object.FindObjectsOfType<Renderer>(true);
            HashSet<Material> sharedMaterials = new HashSet<Material>();
            HashSet<string> apparentInstances = new HashSet<string>();
            report.renderer_count = renderers.Length;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                Material[] materials = renderer.sharedMaterials;
                long triangles = EstimateTriangles(renderer);
                int transparentSlots = 0;
                bool batchingStatic = (GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.BatchingStatic) != 0;

                report.material_slots += materials.Length;
                report.estimated_triangles += triangles;
                if (batchingStatic) report.batching_static_renderers++;

                foreach (Material material in materials)
                {
                    if (material == null) continue;
                    sharedMaterials.Add(material);
                    if (material.renderQueue >= 3000) transparentSlots++;
                    if (material.name.IndexOf("(Instance)", StringComparison.OrdinalIgnoreCase) >= 0)
                        apparentInstances.Add(material.name);
                }
                report.transparent_material_slots += transparentSlots;

                ZoneBudget zone = ResolveZone(renderer.transform, zones);
                if (zone != null)
                {
                    zone.renderer_count++;
                    zone.material_slots += materials.Length;
                    zone.estimated_triangles += triangles;
                    zone.transparent_material_slots += transparentSlots;
                    if (batchingStatic) zone.batching_static_renderers++;
                    if (renderer is LineRenderer) zone.line_renderers++;
                }
            }
            report.unique_shared_materials = sharedMaterials.Count;
            report.apparent_material_instances = apparentInstances.Count;

            ParticleSystem[] particles = UnityEngine.Object.FindObjectsOfType<ParticleSystem>(true);
            report.particle_systems = particles.Length;
            foreach (ParticleSystem particle in particles)
            {
                if (particle == null) continue;
                int capacity = particle.main.maxParticles;
                report.particle_capacity += capacity;
                ZoneBudget zone = ResolveZone(particle.transform, zones);
                if (zone != null)
                {
                    zone.particle_systems++;
                    zone.particle_capacity += capacity;
                }
            }

            report.trail_renderers = UnityEngine.Object.FindObjectsOfType<TrailRenderer>(true).Length;
            report.line_renderers = UnityEngine.Object.FindObjectsOfType<LineRenderer>(true).Length;
            report.cameras = UnityEngine.Object.FindObjectsOfType<Camera>(true).Length;
            report.wisp_shells = UnityEngine.Object.FindObjectsOfType<WispPresentationShell>(true).Length;
            report.vep_stimuli = UnityEngine.Object.FindObjectsOfType<VepAuraStimulus>(true).Length;

            Light[] lights = UnityEngine.Object.FindObjectsOfType<Light>(true);
            foreach (Light light in lights)
            {
                if (light == null) continue;
                bool shadowed = light.shadows != LightShadows.None;
                if (shadowed) report.realtime_shadow_lights++;
                ZoneBudget zone = ResolveZone(light.transform, zones);
                if (zone != null)
                {
                    zone.lights++;
                    if (shadowed) zone.shadow_lights++;
                }
            }

            ReflectionProbe[] probes = UnityEngine.Object.FindObjectsOfType<ReflectionProbe>(true);
            foreach (ReflectionProbe probe in probes)
            {
                if (probe != null && probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime)
                    report.realtime_reflection_probes++;
            }

            AddWarnings(report);
            return report;
        }

        private static Dictionary<string, ZoneBudget> CreateZoneBudgets(AuditReport report)
        {
            Dictionary<string, ZoneBudget> zones = new Dictionary<string, ZoneBudget>(StringComparer.OrdinalIgnoreCase);
            string[] ids = { "memory_forge", "synapse_causeway", "null_market", "maintenance_loop", "signal_cathedral" };
            for (int i = 0; i < ids.Length; i++)
            {
                ZoneBudget zone = new ZoneBudget { id = ids[i] };
                report.null_ward_zones.Add(zone);
                zones.Add(ids[i], zone);
            }
            return zones;
        }

        private static ZoneBudget ResolveZone(Transform transform, Dictionary<string, ZoneBudget> zones)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                string name = current.name ?? string.Empty;
                if (Contains(name, "MemoryForge") || Contains(name, "Memory_Forge")) return zones["memory_forge"];
                if (Contains(name, "Causeway")) return zones["synapse_causeway"];
                if (Contains(name, "Market")) return zones["null_market"];
                if (Contains(name, "Maintenance") || Contains(name, "MemoryConduit")) return zones["maintenance_loop"];
                if (Contains(name, "Cathedral") || Contains(name, "Protocol")) return zones["signal_cathedral"];
            }
            return null;
        }

        private static bool Contains(string value, string token)
            => value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        private static long EstimateTriangles(Renderer renderer)
        {
            Mesh mesh = null;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null) mesh = filter.sharedMesh;
            if (mesh == null && renderer is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
            if (mesh == null) return 0;

            ulong indices = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                indices += mesh.GetIndexCount(i);
            return (long)(indices / 3UL);
        }

        private static void AddWarnings(AuditReport report)
        {
            if (report.apparent_material_instances > 0)
                report.warnings.Add("Apparent runtime/editor material instances detected; prefer shared materials or deliberate property overrides.");
            if (report.realtime_shadow_lights > 6)
                report.warnings.Add("More than six shadow-casting lights are active; profile shadow atlas and additional-light cost.");
            if (report.particle_capacity > 2400)
                report.warnings.Add("Aggregate ParticleSystem max-particle capacity exceeds 2400; verify actual overdraw and burst concurrency.");
            if (report.transparent_material_slots > 48)
                report.warnings.Add("Transparent material-slot pressure is high; inspect overdraw before adding more neural-field layers.");
            if (report.cameras > 2)
                report.warnings.Add("More than two cameras are active; verify stacking/teleport/debug cameras are not rendering simultaneously.");
            if (report.realtime_reflection_probes > 2)
                report.warnings.Add("Multiple realtime reflection probes are active; prefer deliberate update cadence or baked probes where possible.");
            if (report.vep_stimuli != 0 && report.vep_stimuli != 2)
                report.warnings.Add("Expected exactly two coded VEP stimuli in the reference encounter.");

            for (int i = 0; i < report.null_ward_zones.Count; i++)
            {
                ZoneBudget zone = report.null_ward_zones[i];
                if (zone.renderer_count > 100)
                    report.warnings.Add($"{zone.id} exceeds 100 renderers; profile batching and visibility before adding detail.");
                if (zone.particle_capacity > 700)
                    report.warnings.Add($"{zone.id} particle capacity exceeds 700; inspect local overdraw.");
                if (zone.shadow_lights > 2)
                    report.warnings.Add($"{zone.id} has more than two shadowed lights; verify they earn their atlas cost.");
            }
        }

        private static string ReportPath()
        {
            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            return Path.Combine(repoRoot, "experiments", "reports", "presentation-budget-latest.json");
        }
    }
}
