using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.World;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only architectural dressing for deterministic V0.6 world cells.
    ///
    /// This layer deliberately has no gameplay authority: it never creates colliders,
    /// changes the WFC observation, writes world state, owns input, or animates coded
    /// neural stimuli. It turns the generated traversal grammar into a more authored
    /// neural-gothic place while leaving the underlying level truth untouched.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NeuralGothicWorldKitV07 : MonoBehaviour
    {
        public const string Revision = "NEURAL_GOTHIC_WORLD_V07";
        public const string DecorRootName = "Mindforge_V07_Neural_Gothic_Visuals";

        [SerializeField] private ModularWorldAssemblerV06 sourceAssembler;
        [SerializeField] private Material architectureMaterial;
        [SerializeField] private Material metalMaterial;
        [SerializeField] private Material routeMaterial;
        [SerializeField] private Material secondarySignalMaterial;
        [SerializeField] private int deterministicSeed = 70713;
        [SerializeField, Range(0, 3)] private int detailTier = 2;

        private readonly struct SocketSet
        {
            public readonly bool north;
            public readonly bool east;
            public readonly bool south;
            public readonly bool west;

            public SocketSet(bool north, bool east, bool south, bool west)
            {
                this.north = north;
                this.east = east;
                this.south = south;
                this.west = west;
            }

            public int OpenCount =>
                (north ? 1 : 0) + (east ? 1 : 0) + (south ? 1 : 0) + (west ? 1 : 0);
        }

        public int DetailTier => detailTier;
        public int DeterministicSeed => deterministicSeed;

        public void ConfigureRuntime(
            ModularWorldAssemblerV06 assembler,
            Material architecture,
            Material metal,
            Material route,
            Material secondarySignal,
            int seed = 70713,
            int tier = 2)
        {
            sourceAssembler = assembler;
            architectureMaterial = architecture;
            metalMaterial = metal;
            routeMaterial = route;
            secondarySignalMaterial = secondarySignal;
            deterministicSeed = seed;
            detailTier = Mathf.Clamp(tier, 0, 3);
        }

        [ContextMenu("Rebuild Neural Gothic World V0.7")]
        public bool Rebuild()
        {
            if (sourceAssembler == null)
            {
                Debug.LogWarning("[Mindforge:WorldV07] No V0.6 modular world assembler is assigned.", this);
                return false;
            }

            List<Transform> cells = CollectGeneratedCells();
            if (cells.Count == 0)
            {
                Debug.LogWarning("[Mindforge:WorldV07] No generated V0.6 cells were found to dress.", this);
                return false;
            }

            cells.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            float cellSize = ResolveCellSize(cells);
            Transform root = RecreateDecorRoot();

            Bounds localBounds = new Bounds(
                sourceAssembler.transform.InverseTransformPoint(cells[0].position),
                Vector3.zero);

            for (int i = 0; i < cells.Count; i++)
            {
                Transform cell = cells[i];
                Vector3 localPosition = sourceAssembler.transform.InverseTransformPoint(cell.position);
                localBounds.Encapsulate(localPosition);
                SocketSet sockets = ResolveSockets(cell);
                int hash = StableHash(cell.name, deterministicSeed);
                BuildCellVisuals(root, localPosition, cellSize, sockets, hash);
            }

            BuildCloisterCrown(root, localBounds, cellSize);
            Debug.Log(
                $"[Mindforge:WorldV07] Dressed {cells.Count} generated cells at detail tier {detailTier}. " +
                "Traversal, collision, persistent IDs and neural stimulus timing remain unchanged.",
                this);
            return true;
        }

        [ContextMenu("Clear Neural Gothic World V0.7")]
        public void ClearDecor()
        {
            if (sourceAssembler == null) return;
            Transform child = sourceAssembler.transform.Find(DecorRootName);
            if (child == null) return;
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }

        private List<Transform> CollectGeneratedCells()
        {
            List<Transform> cells = new List<Transform>();
            Transform[] descendants = sourceAssembler.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform candidate = descendants[i];
                if (candidate == sourceAssembler.transform) continue;
                if (!candidate.name.StartsWith("cell_", StringComparison.Ordinal)) continue;
                cells.Add(candidate);
            }
            return cells;
        }

        private Transform RecreateDecorRoot()
        {
            ClearDecor();
            GameObject root = new GameObject(DecorRootName);
            root.transform.SetParent(sourceAssembler.transform, false);
            return root.transform;
        }

        private void BuildCellVisuals(
            Transform root,
            Vector3 cell,
            float cellSize,
            SocketSet sockets,
            int hash)
        {
            float half = cellSize * 0.5f;

            // A thin, dim route trace gives the procedural annex visual continuity without
            // competing with combat, lock-on, or coded neural targets in the lighting hierarchy.
            if (sockets.north) BuildRouteTrace(root, cell, Vector3.forward, half);
            if (sockets.east) BuildRouteTrace(root, cell, Vector3.right, half);
            if (sockets.south) BuildRouteTrace(root, cell, Vector3.back, half);
            if (sockets.west) BuildRouteTrace(root, cell, Vector3.left, half);

            if (detailTier == 0) return;

            // Only north/east boundaries are authored here so neighboring cells never receive
            // duplicate arches on the same shared seam.
            if (sockets.north && ((hash >> 1) & 1) == 0)
                BuildPointedThreshold(root, cell + Vector3.forward * half, Vector3.right, cell.y, cellSize);
            if (sockets.east && ((hash >> 2) & 1) == 0)
                BuildPointedThreshold(root, cell + Vector3.right * half, Vector3.forward, cell.y, cellSize);

            if (detailTier >= 2)
            {
                if (!sockets.north) BuildButtressPair(root, cell, Vector3.forward, Vector3.right, half);
                if (!sockets.east) BuildButtressPair(root, cell, Vector3.right, Vector3.forward, half);
                if (!sockets.south && (hash & 4) != 0) BuildButtressPair(root, cell, Vector3.back, Vector3.right, half);
                if (!sockets.west && (hash & 8) != 0) BuildButtressPair(root, cell, Vector3.left, Vector3.forward, half);

                if (sockets.OpenCount >= 3 && Mathf.Abs(hash % 3) == 0)
                {
                    Vector3 horizontal = ((hash & 16) == 0) ? Vector3.right : Vector3.forward;
                    BuildVerticalOculus(root, cell + Vector3.up * 4.45f, horizontal, cellSize * 0.58f, routeMaterial, 10);
                }
            }

            if (detailTier >= 3 && sockets.OpenCount <= 2 && (hash & 32) != 0)
            {
                Vector3 finPosition = cell + Vector3.up * 3.45f;
                BuildSpire(root, finPosition, 2.8f + Mathf.Abs(hash % 5) * 0.22f, metalMaterial, routeMaterial);
            }
        }

        private void BuildRouteTrace(Transform root, Vector3 cell, Vector3 direction, float half)
        {
            Vector3 start = cell + Vector3.up * 0.055f;
            Vector3 end = start + direction * (half * 0.92f);
            CreateBeam("RouteTrace", root, start, end, 0.055f, routeMaterial, false);
        }

        private void BuildPointedThreshold(
            Transform root,
            Vector3 center,
            Vector3 widthAxis,
            float floorY,
            float cellSize)
        {
            float halfOpening = Mathf.Min(1.62f, cellSize * 0.31f);
            float shoulderY = floorY + 2.35f;
            float apexY = floorY + 3.38f;
            float baseY = floorY + 0.08f;
            float thickness = 0.16f;

            Vector3 leftBase = new Vector3(center.x, baseY, center.z) - widthAxis * halfOpening;
            Vector3 rightBase = new Vector3(center.x, baseY, center.z) + widthAxis * halfOpening;
            Vector3 leftShoulder = new Vector3(center.x, shoulderY, center.z) - widthAxis * halfOpening;
            Vector3 rightShoulder = new Vector3(center.x, shoulderY, center.z) + widthAxis * halfOpening;
            Vector3 apex = new Vector3(center.x, apexY, center.z);

            CreateBeam("ThresholdPillar", root, leftBase, leftShoulder, thickness, architectureMaterial, true);
            CreateBeam("ThresholdPillar", root, rightBase, rightShoulder, thickness, architectureMaterial, true);
            CreateBeam("ThresholdArch", root, leftShoulder, apex, thickness, architectureMaterial, true);
            CreateBeam("ThresholdArch", root, rightShoulder, apex, thickness, architectureMaterial, true);

            Vector3 signalLeft = Vector3.Lerp(leftShoulder, apex, 0.57f);
            Vector3 signalRight = Vector3.Lerp(rightShoulder, apex, 0.57f);
            CreateBeam("ThresholdSignal", root, signalLeft, signalRight, 0.045f, routeMaterial, false);
        }

        private void BuildButtressPair(
            Transform root,
            Vector3 cell,
            Vector3 outward,
            Vector3 tangent,
            float half)
        {
            float edge = half * 0.91f;
            float tangentOffset = Mathf.Min(1.55f, half * 0.58f);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 basePoint = cell + outward * edge + tangent * tangentOffset * side + Vector3.up * 0.08f;
                Vector3 shoulder = cell + outward * (edge - 0.45f) + tangent * tangentOffset * side + Vector3.up * 3.42f;
                CreateBeam("FlyingButtress", root, basePoint, shoulder, 0.19f, architectureMaterial, true);

                Vector3 signalStart = Vector3.Lerp(basePoint, shoulder, 0.23f);
                Vector3 signalEnd = Vector3.Lerp(basePoint, shoulder, 0.78f);
                CreateBeam("ButtressSignal", root, signalStart, signalEnd, 0.040f, secondarySignalMaterial, false);
            }
        }

        private void BuildVerticalOculus(
            Transform root,
            Vector3 center,
            Vector3 horizontalAxis,
            float radius,
            Material material,
            int segments)
        {
            int safeSegments = Mathf.Max(6, segments);
            for (int i = 0; i < safeSegments; i++)
            {
                float a0 = Mathf.PI * 2f * i / safeSegments;
                float a1 = Mathf.PI * 2f * (i + 1) / safeSegments;
                Vector3 p0 = center + horizontalAxis * (Mathf.Cos(a0) * radius) + Vector3.up * (Mathf.Sin(a0) * radius);
                Vector3 p1 = center + horizontalAxis * (Mathf.Cos(a1) * radius) + Vector3.up * (Mathf.Sin(a1) * radius);
                CreateBeam("OculusSegment", root, p0, p1, 0.075f, material, false);
            }
        }

        private void BuildSpire(
            Transform root,
            Vector3 baseCenter,
            float height,
            Material body,
            Material signal)
        {
            Vector3 top = baseCenter + Vector3.up * height;
            CreateBeam("NeuralSpire", root, baseCenter, top, 0.22f, body, true);
            CreateBeam(
                "NeuralSpireSignal",
                root,
                Vector3.Lerp(baseCenter, top, 0.54f),
                Vector3.Lerp(baseCenter, top, 0.96f),
                0.050f,
                signal,
                false);
        }

        private void BuildCloisterCrown(Transform root, Bounds bounds, float cellSize)
        {
            // The annex is entered from its west threshold, so the crown occupies the far-east
            // skyline. It is intentionally above traversable space and entirely non-colliding.
            float farX = bounds.max.x + cellSize * 0.46f;
            float midZ = bounds.center.z;
            float baseY = bounds.min.y + 0.20f;
            float sideOffset = cellSize * 0.62f;

            Vector3 leftBase = new Vector3(farX, baseY, midZ - sideOffset);
            Vector3 centerBase = new Vector3(farX + 0.12f, baseY, midZ);
            Vector3 rightBase = new Vector3(farX, baseY, midZ + sideOffset);
            BuildSpire(root, leftBase, 6.25f, metalMaterial, secondarySignalMaterial);
            BuildSpire(root, centerBase, 8.10f, architectureMaterial, routeMaterial);
            BuildSpire(root, rightBase, 6.25f, metalMaterial, secondarySignalMaterial);

            Vector3 crownCenter = new Vector3(farX - 0.06f, baseY + 4.95f, midZ);
            BuildVerticalOculus(root, crownCenter, Vector3.forward, cellSize * 0.58f, routeMaterial, 14);
            CreateBeam(
                "CrownLintel",
                root,
                new Vector3(farX, baseY + 3.18f, midZ - sideOffset),
                new Vector3(farX, baseY + 3.18f, midZ + sideOffset),
                0.20f,
                architectureMaterial,
                true);

            if (detailTier >= 2)
            {
                float outer = sideOffset * 1.42f;
                CreateBeam(
                    "CrownFlyingRib",
                    root,
                    new Vector3(farX - 0.24f, baseY + 0.35f, midZ - outer),
                    new Vector3(farX, baseY + 5.45f, midZ - sideOffset),
                    0.16f,
                    architectureMaterial,
                    true);
                CreateBeam(
                    "CrownFlyingRib",
                    root,
                    new Vector3(farX - 0.24f, baseY + 0.35f, midZ + outer),
                    new Vector3(farX, baseY + 5.45f, midZ + sideOffset),
                    0.16f,
                    architectureMaterial,
                    true);
            }
        }

        private SocketSet ResolveSockets(Transform cell)
        {
            bool hasNamedFallbackWalls =
                cell.Find("wall_n") != null ||
                cell.Find("wall_e") != null ||
                cell.Find("wall_s") != null ||
                cell.Find("wall_w") != null;

            if (hasNamedFallbackWalls)
            {
                return new SocketSet(
                    cell.Find("wall_n") == null,
                    cell.Find("wall_e") == null,
                    cell.Find("wall_s") == null,
                    cell.Find("wall_w") == null);
            }

            // Prefab-authored cells may not retain fallback wall names. The deterministic cell
            // id still contains its tile grammar, so presentation can recover the same openings
            // without reaching into the assembler's private generation state.
            string id = cell.name.ToLowerInvariant();
            if (id.Contains("corridor_ns")) return new SocketSet(true, false, true, false);
            if (id.Contains("corridor_ew")) return new SocketSet(false, true, false, true);
            if (id.Contains("corner_ne")) return new SocketSet(true, true, false, false);
            if (id.Contains("corner_es")) return new SocketSet(false, true, true, false);
            if (id.Contains("corner_sw")) return new SocketSet(false, false, true, true);
            if (id.Contains("corner_wn")) return new SocketSet(true, false, false, true);
            return new SocketSet(true, true, true, true);
        }

        private float ResolveCellSize(IReadOnlyList<Transform> cells)
        {
            float best = float.MaxValue;
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3 a = sourceAssembler.transform.InverseTransformPoint(cells[i].position);
                for (int j = i + 1; j < cells.Count; j++)
                {
                    Vector3 b = sourceAssembler.transform.InverseTransformPoint(cells[j].position);
                    float dx = Mathf.Abs(a.x - b.x);
                    float dz = Mathf.Abs(a.z - b.z);
                    if (dx > 0.1f && dz < 0.1f) best = Mathf.Min(best, dx);
                    if (dz > 0.1f && dx < 0.1f) best = Mathf.Min(best, dz);
                }
            }
            return best < float.MaxValue ? best : 5.5f;
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
            if (length < 0.001f) return null;

            GameObject beam = CreateVisualPrimitive(name, PrimitiveType.Cube, parent, (start + end) * 0.5f, material, castShadows);
            beam.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta / length);
            beam.transform.localScale = new Vector3(thickness, length, thickness);
            return beam;
        }

        private GameObject CreateVisualPrimitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Material material,
            bool castShadows)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (material != null) renderer.sharedMaterial = material;
                renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = castShadows;
            }

            Collider shape = go.GetComponent<Collider>();
            if (shape != null)
            {
                if (Application.isPlaying) Destroy(shape);
                else DestroyImmediate(shape);
            }

            go.isStatic = true;
            return go;
        }

        private static int StableHash(string value, int seed)
        {
            unchecked
            {
                int hash = seed ^ 0x5f3759df;
                string safe = value ?? string.Empty;
                for (int i = 0; i < safe.Length; i++) hash = hash * 31 + safe[i];
                return hash;
            }
        }
    }
}
