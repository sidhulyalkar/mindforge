using System;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.ThirdParty.Wfc;

namespace Mindforge.World
{
    [Serializable]
    public sealed class WorldTileDefinitionV06
    {
        public string id;
        public GameObject prefab;
        [Min(0.01f)] public float weight = 1f;
        public string north = "path";
        public string east = "path";
        public string south = "path";
        public string west = "path";
        [Range(0, 4)] public int heightSteps;
    }

    /// <summary>
    /// Deterministic modular architecture generator for non-critical world dressing and
    /// traversal annexes. Authored landmarks remain fixed; this fills the negative space with
    /// socket-compatible cells, terraced elevation and sealed perimeter geometry.
    ///
    /// The collapse solver is adapted from the MIT WaveFunctionCollapse project. Generated
    /// visual content is entirely Mindforge-authored or supplied through local prefabs.
    /// </summary>
    [DefaultExecutionOrder(-760)]
    public sealed class ModularWorldAssemblerV06 : MonoBehaviour
    {
        [SerializeField] private Vector2Int gridSize = new Vector2Int(9, 12);
        [SerializeField, Min(2f)] private float cellSize = 5.5f;
        [SerializeField, Min(0.5f)] private float heightStepMeters = 1.25f;
        [SerializeField] private int seed = 60601;
        [SerializeField, Range(1, 24)] private int retryCount = 8;
        [SerializeField] private bool buildOnStart;
        [SerializeField] private bool enclosePerimeter = true;
        [SerializeField, Min(1f)] private float perimeterWallHeight = 6f;
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material accentMaterial;
        [SerializeField] private List<WorldTileDefinitionV06> tiles = new List<WorldTileDefinitionV06>();

        private const string GeneratedRootName = "Mindforge_V06_Generated_World";

        public int Seed => seed;
        public Vector2Int GridSize => gridSize;
        public Transform GeneratedRoot => transform.Find(GeneratedRootName);

        private void Start()
        {
            if (buildOnStart) Generate();
        }

        [ContextMenu("Generate V0.6 Modular World")]
        public bool Generate()
        {
            EnsureDefaultCatalog();
            if (tiles.Count == 0) return false;

            int width = Mathf.Max(2, gridSize.x);
            int height = Mathf.Max(2, gridSize.y);
            double[] weights = new double[tiles.Count];
            bool[,,] adjacency = new bool[4, tiles.Count, tiles.Count];
            for (int i = 0; i < tiles.Count; i++) weights[i] = Mathf.Max(0.01f, tiles[i].weight);

            for (int a = 0; a < tiles.Count; a++)
            {
                for (int b = 0; b < tiles.Count; b++)
                {
                    bool heightCompatible = Mathf.Abs(tiles[a].heightSteps - tiles[b].heightSteps) <= 1;
                    adjacency[0, a, b] = heightCompatible && SocketMatches(tiles[a].north, tiles[b].south);
                    adjacency[1, a, b] = heightCompatible && SocketMatches(tiles[a].east, tiles[b].west);
                    adjacency[2, a, b] = heightCompatible && SocketMatches(tiles[a].south, tiles[b].north);
                    adjacency[3, a, b] = heightCompatible && SocketMatches(tiles[a].west, tiles[b].east);
                }
            }

            int[] observed = null;
            bool solved = false;
            for (int attempt = 0; attempt < Mathf.Max(1, retryCount); attempt++)
            {
                MindforgeConstraintCollapse solver = new MindforgeConstraintCollapse(width, height, weights, adjacency);
                if (!solver.Run(seed + attempt * 7919, out observed)) continue;
                solved = true;
                break;
            }
            if (!solved || observed == null)
            {
                Debug.LogWarning("[Mindforge:WorldV06] Constraint collapse failed for the current tile catalog.");
                return false;
            }

            Transform root = RecreateGeneratedRoot();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    int tileIndex = Mathf.Clamp(observed[index], 0, tiles.Count - 1);
                    WorldTileDefinitionV06 tile = tiles[tileIndex];
                    Vector3 position = new Vector3(
                        (x - (width - 1) * 0.5f) * cellSize,
                        tile.heightSteps * heightStepMeters,
                        (y - (height - 1) * 0.5f) * cellSize);
                    CreateTile(root, tile, position, x, y);
                }
            }

            BuildVerticalConnectors(root, observed, width, height);
            if (enclosePerimeter) BuildPerimeter(root, width, height);
            Debug.Log($"[Mindforge:WorldV06] Generated {width * height} modular cells with seed {seed}.");
            return true;
        }

        [ContextMenu("Clear V0.6 Modular World")]
        public void ClearGenerated()
        {
            Transform child = transform.Find(GeneratedRootName);
            if (child == null) return;
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }

        private Transform RecreateGeneratedRoot()
        {
            ClearGenerated();
            GameObject root = new GameObject(GeneratedRootName);
            root.transform.SetParent(transform, false);
            return root.transform;
        }

        private void CreateTile(Transform parent, WorldTileDefinitionV06 tile, Vector3 localPosition, int x, int y)
        {
            GameObject instance;
            if (tile.prefab != null)
            {
                instance = Instantiate(tile.prefab, parent);
                instance.name = $"cell_{x:00}_{y:00}_{Safe(tile.id)}";
                instance.transform.localPosition = localPosition;
                instance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                instance = new GameObject($"cell_{x:00}_{y:00}_{Safe(tile.id)}");
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = localPosition;
                BuildFallbackTile(instance.transform, tile);
            }

            GeneratedWorldCellV07 metadata = instance.GetComponent<GeneratedWorldCellV07>();
            if (metadata == null) metadata = instance.AddComponent<GeneratedWorldCellV07>();
            metadata.Configure(
                x,
                y,
                tile.id,
                tile.north,
                tile.east,
                tile.south,
                tile.west,
                tile.heightSteps,
                cellSize);
        }

        private void BuildFallbackTile(Transform parent, WorldTileDefinitionV06 tile)
        {
            CreateCube("floor", parent, new Vector3(0f, -0.25f, 0f), new Vector3(cellSize, 0.5f, cellSize), floorMaterial, true);
            float wallThickness = 0.28f;
            float wallHeight = 2.8f;
            float half = cellSize * 0.5f;

            if (!IsPath(tile.north)) CreateCube("wall_n", parent, new Vector3(0f, wallHeight * 0.5f, half), new Vector3(cellSize, wallHeight, wallThickness), wallMaterial, true);
            if (!IsPath(tile.south)) CreateCube("wall_s", parent, new Vector3(0f, wallHeight * 0.5f, -half), new Vector3(cellSize, wallHeight, wallThickness), wallMaterial, true);
            if (!IsPath(tile.east)) CreateCube("wall_e", parent, new Vector3(half, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, cellSize), wallMaterial, true);
            if (!IsPath(tile.west)) CreateCube("wall_w", parent, new Vector3(-half, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, cellSize), wallMaterial, true);

            int hash = StableHash(tile.id);
            if ((hash & 1) == 0)
            {
                CreateCube("rib_a", parent, new Vector3(-half * 0.62f, 1.25f, 0f), new Vector3(0.22f, 2.5f, 0.22f), accentMaterial, false);
                CreateCube("rib_b", parent, new Vector3(half * 0.62f, 1.25f, 0f), new Vector3(0.22f, 2.5f, 0.22f), accentMaterial, false);
            }
            if ((hash & 2) != 0)
            {
                GameObject core = CreatePrimitive("signal_core", PrimitiveType.Cylinder, parent, new Vector3(0f, 0.32f, 0f), new Vector3(0.48f, 0.32f, 0.48f), accentMaterial, false);
                core.transform.localRotation = Quaternion.Euler(0f, (hash % 4) * 45f, 0f);
            }
        }

        private void BuildVerticalConnectors(Transform parent, int[] observed, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    WorldTileDefinitionV06 current = tiles[observed[index]];
                    if (x + 1 < width)
                        BuildConnector(parent, x, y, current, tiles[observed[index + 1]], true);
                    if (y + 1 < height)
                        BuildConnector(parent, x, y, current, tiles[observed[index + width]], false);
                }
            }
        }

        private void BuildConnector(Transform parent, int x, int y, WorldTileDefinitionV06 a, WorldTileDefinitionV06 b, bool east)
        {
            int delta = b.heightSteps - a.heightSteps;
            if (Mathf.Abs(delta) != 1) return;
            string sourceSocket = east ? a.east : a.north;
            string targetSocket = east ? b.west : b.south;
            if (!IsPath(sourceSocket) || !IsPath(targetSocket)) return;

            float baseY = Mathf.Min(a.heightSteps, b.heightSteps) * heightStepMeters;
            Vector3 center = new Vector3(
                (x - (Mathf.Max(2, gridSize.x) - 1) * 0.5f) * cellSize,
                baseY,
                (y - (Mathf.Max(2, gridSize.y) - 1) * 0.5f) * cellSize);
            center += east ? Vector3.right * cellSize * 0.5f : Vector3.forward * cellSize * 0.5f;

            int steps = 4;
            for (int i = 0; i < steps; i++)
            {
                float t = (i + 0.5f) / steps;
                float h = heightStepMeters * (i + 1) / steps;
                Vector3 offset = east
                    ? new Vector3((t - 0.5f) * cellSize * 0.72f, h * 0.5f, 0f)
                    : new Vector3(0f, h * 0.5f, (t - 0.5f) * cellSize * 0.72f);
                if (delta < 0) offset = -new Vector3(offset.x, -offset.y, offset.z);
                Vector3 scale = east
                    ? new Vector3(cellSize * 0.72f / steps, h, cellSize * 0.42f)
                    : new Vector3(cellSize * 0.42f, h, cellSize * 0.72f / steps);
                CreateCube($"step_{x:00}_{y:00}_{(east ? "e" : "n")}_{i:00}", parent, center + offset, scale, floorMaterial, true);
            }
        }

        private void BuildPerimeter(Transform parent, int width, int height)
        {
            float worldWidth = width * cellSize;
            float worldDepth = height * cellSize;
            float thickness = 0.45f;
            float y = perimeterWallHeight * 0.5f - 0.1f;
            CreateCube("perimeter_n", parent, new Vector3(0f, y, worldDepth * 0.5f), new Vector3(worldWidth + thickness, perimeterWallHeight, thickness), wallMaterial, true);
            CreateCube("perimeter_s", parent, new Vector3(0f, y, -worldDepth * 0.5f), new Vector3(worldWidth + thickness, perimeterWallHeight, thickness), wallMaterial, true);
            CreateCube("perimeter_e", parent, new Vector3(worldWidth * 0.5f, y, 0f), new Vector3(thickness, perimeterWallHeight, worldDepth + thickness), wallMaterial, true);
            CreateCube("perimeter_w", parent, new Vector3(-worldWidth * 0.5f, y, 0f), new Vector3(thickness, perimeterWallHeight, worldDepth + thickness), wallMaterial, true);
        }

        private void EnsureDefaultCatalog()
        {
            if (tiles != null && tiles.Count > 0) return;
            tiles = new List<WorldTileDefinitionV06>();
            string[] ids = { "plaza", "corridor_ns", "corridor_ew", "corner_ne", "corner_es", "corner_sw", "corner_wn" };
            string[,] sockets =
            {
                { "path", "path", "path", "path" },
                { "path", "sealed", "path", "sealed" },
                { "sealed", "path", "sealed", "path" },
                { "path", "path", "sealed", "sealed" },
                { "sealed", "path", "path", "sealed" },
                { "sealed", "sealed", "path", "path" },
                { "path", "sealed", "sealed", "path" },
            };
            for (int h = 0; h < 3; h++)
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    tiles.Add(new WorldTileDefinitionV06
                    {
                        id = ids[i] + "_h" + h,
                        weight = i == 0 ? 1.6f : 1f,
                        north = sockets[i, 0],
                        east = sockets[i, 1],
                        south = sockets[i, 2],
                        west = sockets[i, 3],
                        heightSteps = h,
                    });
                }
            }
        }

        private static bool SocketMatches(string a, string b)
        {
            string left = Safe(a);
            string right = Safe(b);
            if (left == "*" || right == "*") return true;
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        private static bool IsPath(string socket) => Safe(socket) == "path";
        private static string Safe(string value) => string.IsNullOrWhiteSpace(value) ? "sealed" : value.Trim().ToLowerInvariant();

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 17;
                string safe = value ?? string.Empty;
                for (int i = 0; i < safe.Length; i++) hash = hash * 31 + safe[i];
                return hash;
            }
        }

        private GameObject CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool collider)
            => CreatePrimitive(name, PrimitiveType.Cube, parent, localPosition, localScale, material, collider);

        private static GameObject CreatePrimitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            Collider shape = go.GetComponent<Collider>();
            if (shape != null && !collider)
            {
                if (Application.isPlaying) Destroy(shape);
                else DestroyImmediate(shape);
            }
            return go;
        }
    }
}
