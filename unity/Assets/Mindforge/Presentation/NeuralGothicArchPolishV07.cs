using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.World;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Adds a thin pointed-arch silhouette over shared open seams in the generated Cloister.
    /// Existing V0.7 jambs/lintels remain the structural visual grammar; this pass gives those
    /// openings a genuinely gothic crown without adding colliders or changing topology.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NeuralGothicArchPolishV07 : MonoBehaviour
    {
        public const string Revision = "NEURAL_GOTHIC_ARCH_POLISH_V07";
        public const string RootName = "NeuralGothicArchPolish_V07";

        [SerializeField] private Material architectureMaterial;
        [SerializeField] private Material signalMaterial;
        [SerializeField, Range(0, 64)] private int maxSharedArches = 24;
        [SerializeField, Range(0.02f, 0.30f)] private float architectureThickness = 0.14f;
        [SerializeField, Range(0.01f, 0.12f)] private float signalThickness = 0.035f;

        public void ConfigureRuntime(
            Material architecture,
            Material signal,
            int archBudget = 24)
        {
            architectureMaterial = architecture;
            signalMaterial = signal;
            maxSharedArches = Mathf.Clamp(archBudget, 0, 64);
        }

        [ContextMenu("Rebuild Pointed Arch Polish V0.7")]
        public int Rebuild()
        {
            Clear();
            GeneratedWorldCellV07[] cells = GetComponentsInChildren<GeneratedWorldCellV07>(true);
            if (cells == null || cells.Length == 0) return 0;

            Dictionary<Vector2Int, GeneratedWorldCellV07> byGrid = new Dictionary<Vector2Int, GeneratedWorldCellV07>();
            for (int i = 0; i < cells.Length; i++)
            {
                GeneratedWorldCellV07 cell = cells[i];
                if (cell != null) byGrid[cell.Grid] = cell;
            }

            GameObject rootObject = new GameObject(RootName);
            rootObject.transform.SetParent(transform, false);
            Transform root = rootObject.transform;

            int built = 0;
            for (int i = 0; i < cells.Length && built < maxSharedArches; i++)
            {
                GeneratedWorldCellV07 cell = cells[i];
                if (cell == null) continue;

                // North and east own shared seams so each pair gets exactly one arch.
                if (TryBuildSharedArch(root, cell, byGrid, 0, new Vector2Int(0, 1))) built++;
                if (built >= maxSharedArches) break;
                if (TryBuildSharedArch(root, cell, byGrid, 1, new Vector2Int(1, 0))) built++;
            }

            rootObject.isStatic = true;
            return built;
        }

        [ContextMenu("Clear Pointed Arch Polish V0.7")]
        public void Clear()
        {
            Transform existing = transform.Find(RootName);
            if (existing == null) return;
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        private bool TryBuildSharedArch(
            Transform root,
            GeneratedWorldCellV07 cell,
            IReadOnlyDictionary<Vector2Int, GeneratedWorldCellV07> byGrid,
            int direction,
            Vector2Int neighborOffset)
        {
            if (!cell.IsOpen(direction)) return false;
            if (!byGrid.TryGetValue(cell.Grid + neighborOffset, out GeneratedWorldCellV07 neighbor) || neighbor == null)
                return false;

            int opposite = direction == 0 ? 2 : 3;
            if (!neighbor.IsOpen(opposite)) return false;

            float half = cell.CellSize * 0.5f;
            float bodyHeight = 2.9f + cell.HeightSteps * 0.34f;
            float halfOpening = Mathf.Clamp(half * 0.52f, 1.25f, 1.72f);
            float shoulderY = bodyHeight * 0.84f;
            float apexY = bodyHeight + 1.12f;

            Vector3 edgeCenter;
            Vector3 widthAxis;
            if (direction == 0)
            {
                edgeCenter = cell.transform.position + cell.transform.forward * half;
                widthAxis = cell.transform.right;
            }
            else
            {
                edgeCenter = cell.transform.position + cell.transform.right * half;
                widthAxis = cell.transform.forward;
            }

            // Convert world-space seam geometry into the polish root's local space. This keeps
            // the component correct if the generated annex is moved or rotated as a whole.
            Vector3 shoulderCenter = edgeCenter + Vector3.up * shoulderY;
            Vector3 apexWorld = edgeCenter + Vector3.up * apexY;
            Vector3 leftWorld = shoulderCenter - widthAxis * halfOpening;
            Vector3 rightWorld = shoulderCenter + widthAxis * halfOpening;

            Vector3 left = root.InverseTransformPoint(leftWorld);
            Vector3 right = root.InverseTransformPoint(rightWorld);
            Vector3 apex = root.InverseTransformPoint(apexWorld);

            CreateBeam("PointedArch_Left", root, left, apex, architectureThickness, architectureMaterial, true);
            CreateBeam("PointedArch_Right", root, right, apex, architectureThickness, architectureMaterial, true);

            // The inner signal line is deliberately thinner than the structural rib. It reads
            // as circuitry embedded in architecture rather than becoming another gameplay cue.
            Vector3 innerLeft = Vector3.Lerp(left, apex, 0.16f);
            Vector3 innerRight = Vector3.Lerp(right, apex, 0.16f);
            Vector3 innerApex = apex - Vector3.up * 0.10f;
            CreateBeam("PointedArchSignal_Left", root, innerLeft, innerApex, signalThickness, signalMaterial, false);
            CreateBeam("PointedArchSignal_Right", root, innerRight, innerApex, signalThickness, signalMaterial, false);

            GameObject key = CreateVisual(
                "PointedArch_Key",
                PrimitiveType.Sphere,
                root,
                apex,
                Vector3.one * 0.18f,
                signalMaterial,
                false);
            if (key != null) key.transform.localScale = Vector3.one * 0.18f;
            return true;
        }

        private GameObject CreateBeam(
            string name,
            Transform parent,
            Vector3 start,
            Vector3 end,
            float thickness,
            Material material,
            bool castShadows)
        {
            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length <= 0.001f) return null;

            GameObject beam = CreateVisual(
                name,
                PrimitiveType.Cube,
                parent,
                (start + end) * 0.5f,
                new Vector3(thickness, length, thickness),
                material,
                castShadows);
            if (beam != null) beam.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta / length);
            return beam;
        }

        private static GameObject CreateVisual(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool castShadows)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (material != null) renderer.sharedMaterial = material;
                renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = castShadows;
            }

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            go.isStatic = true;
            return go;
        }
    }
}
