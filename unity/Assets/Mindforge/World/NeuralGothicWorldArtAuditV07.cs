using System;
using UnityEngine;

namespace Mindforge.World
{
    /// <summary>
    /// Cheap scene-level art budget guard. This only reports visual density; it never changes
    /// quality, frame timing, gameplay authority or coded neural stimulus behavior.
    /// Kept outside an Editor folder because the component is serialized into the showcase.
    /// </summary>
    public sealed class NeuralGothicWorldArtAuditV07 : MonoBehaviour
    {
        [Serializable]
        public struct ArtCounts
        {
            public int renderers;
            public int lights;
            public int lines;
        }

        [SerializeField] private Transform generatedAnnex;
        [SerializeField] private Transform heroArt;
        [SerializeField, Min(64)] private int rendererBudget = 760;
        [SerializeField, Min(1)] private int lightBudget = 10;
        [SerializeField, Min(1)] private int lineBudget = 48;

        public ArtCounts LastCounts { get; private set; }
        public bool LastPassed { get; private set; }

        public void ConfigureRuntime(Transform annex, Transform authoredHeroArt)
        {
            generatedAnnex = annex;
            heroArt = authoredHeroArt;
        }

        [ContextMenu("Audit Neural-Gothic World V0.7")]
        public bool Evaluate(bool log = true)
        {
            ArtCounts counts = new ArtCounts();
            Accumulate(generatedAnnex, ref counts);
            if (heroArt != null && heroArt != generatedAnnex) Accumulate(heroArt, ref counts);
            LastCounts = counts;
            LastPassed = counts.renderers <= Mathf.Max(64, rendererBudget) &&
                         counts.lights <= Mathf.Max(1, lightBudget) &&
                         counts.lines <= Mathf.Max(1, lineBudget);

            if (log)
            {
                string status = LastPassed ? "PASS" : "WARN";
                Debug.Log($"[Mindforge:WorldV07:{status}] renderers={counts.renderers}/{rendererBudget} lights={counts.lights}/{lightBudget} lines={counts.lines}/{lineBudget}");
            }
            return LastPassed;
        }

        private static void Accumulate(Transform root, ref ArtCounts counts)
        {
            if (root == null) return;
            counts.renderers += root.GetComponentsInChildren<Renderer>(true).Length;
            counts.lights += root.GetComponentsInChildren<Light>(true).Length;
            counts.lines += root.GetComponentsInChildren<LineRenderer>(true).Length;
        }
    }
}
