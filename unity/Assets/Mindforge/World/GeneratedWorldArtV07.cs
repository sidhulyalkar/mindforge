using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.World
{
    /// <summary>
    /// Read-only semantic metadata attached to every generated world cell. It lets later
    /// art passes add local structure without re-parsing object names or taking topology
    /// authority away from ModularWorldAssemblerV06.
    /// </summary>
    public sealed class GeneratedWorldCellV07 : MonoBehaviour
    {
        [SerializeField] private Vector2Int grid;
        [SerializeField] private string tileId;
        [SerializeField] private string north;
        [SerializeField] private string east;
        [SerializeField] private string south;
        [SerializeField] private string west;
        [SerializeField] private int heightSteps;
        [SerializeField] private float cellSize;

        public Vector2Int Grid => grid;
        public string TileId => tileId ?? string.Empty;
        public string North => north ?? string.Empty;
        public string East => east ?? string.Empty;
        public string South => south ?? string.Empty;
        public string West => west ?? string.Empty;
        public int HeightSteps => heightSteps;
        public float CellSize => Mathf.Max(2f, cellSize);

        public void Configure(
            int x,
            int y,
            string id,
            string northSocket,
            string eastSocket,
            string southSocket,
            string westSocket,
            int steps,
            float size)
        {
            grid = new Vector2Int(x, y);
            tileId = Normalize(id);
            north = Normalize(northSocket);
            east = Normalize(eastSocket);
            south = Normalize(southSocket);
            west = Normalize(westSocket);
            heightSteps = Mathf.Max(0, steps);
            cellSize = Mathf.Max(2f, size);
        }

        public bool IsOpen(int direction)
        {
            string socket;
            switch (direction)
            {
                case 0: socket = North; break;
                case 1: socket = East; break;
                case 2: socket = South; break;
                default: socket = West; break;
            }
            return socket == "path" || socket == "open" || socket == "door";
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "sealed" : value.Trim().ToLowerInvariant();
    }

    [Serializable]
    public sealed class NeuralGothicArtBudgetV07
    {
        [Range(0, 8)] public int cornerButtresses = 4;
        [Range(0, 8)] public int wallRibsPerClosedSide = 2;
        [Range(0, 8)] public int archSegmentsPerOpenSide = 3;
        [Range(0, 8)] public int floorInlays = 2;
        [Range(0, 6)] public int propClusters = 2;
        [Range(0, 6)] public int overheadStructures = 1;
        [Range(0, 8)] public int signalAccents = 2;
        [Range(0, 96)] public int maxDecorativePrimitivesPerCell = 34;
    }

    /// <summary>
    /// Deterministic, presentation-only local module pass inspired by the per-module
    /// generation pattern used by permissively licensed modular Unity generators. The
    /// assembler owns topology; this component only gives each solved cell architectural
    /// hierarchy, silhouette and material rhythm.
    ///
    /// No interaction, collision boundary, quest, combat or BCI authority lives here.
    /// Decorative colliders are intentionally disabled. Existing cell floor/wall collision
    /// remains the only generated traversal authority.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NeuralGothicWorldDetailerV07 : MonoBehaviour
    {
        [SerializeField] private int detailSeed = 70731;
        [SerializeField] private Material stone;
        [SerializeField] private Material darkStone;
        [SerializeField] private Material metal;
        [SerializeField] private Material patina;
        [SerializeField] private Material cyan;
        [SerializeField] private Material green;
        [SerializeField] private Material violet;
        [SerializeField] private NeuralGothicArtBudgetV07 budget = new NeuralGothicArtBudgetV07();

        public const string DetailRootName = "NeuralGothicDetail_V07";
        private int _createdThisCell;

        public void ConfigureRuntime(
            int seed,
            Material primaryStone,
            Material secondaryStone,
            Material structuralMetal,
            Material patinaMetal,
            Material cyanSignal,
            Material greenSignal,
            Material violetSignal)
        {
            detailSeed = seed;
            stone = primaryStone;
            darkStone = secondaryStone;
            metal = structuralMetal;
            patina = patinaMetal;
            cyan = cyanSignal;
            green = greenSignal;
            violet = violetSignal;
        }

        [ContextMenu("Rebuild Neural-Gothic Detail V0.7")]
        public int Rebuild()
        {
            GeneratedWorldCellV07[] cells = GetComponentsInChildren<GeneratedWorldCellV07>(true);
            Array.Sort(cells, CompareCells);
            int detailed = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null) continue;
                DetailCell(cells[i]);
                detailed++;
            }
            return detailed;
        }

        public void Clear()
        {
            GeneratedWorldCellV07[] cells = GetComponentsInChildren<GeneratedWorldCellV07>(true);
            for (int i = 0; i < cells.Length; i++)
            {
                Transform root = cells[i] != null ? cells[i].transform.Find(DetailRootName) : null;
                if (root == null) continue;
                if (Application.isPlaying) Destroy(root.gameObject);
                else DestroyImmediate(root.gameObject);
            }
        }

        private void DetailCell(GeneratedWorldCellV07 cell)
        {
            Transform existing = cell.transform.Find(DetailRootName);
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            GameObject detailObject = new GameObject(DetailRootName);
            detailObject.transform.SetParent(cell.transform, false);
            Transform root = detailObject.transform;
            _createdThisCell = 0;

            int hash = StableHash(cell.TileId + ":" + cell.Grid.x + ":" + cell.Grid.y + ":" + detailSeed);
            System.Random random = new System.Random(hash);
            float half = cell.CellSize * 0.5f;
            float bodyHeight = 2.9f + cell.HeightSteps * 0.34f;
            Material cellStone = ((hash >> 2) & 1) == 0 ? stone : darkStone;
            Material signal = SignalFor(hash);

            BuildFoundation(root, cell, cellStone, metal, signal, random);
            BuildCorners(root, half, bodyHeight, cellStone, metal, signal, random);
            for (int direction = 0; direction < 4; direction++)
            {
                if (cell.IsOpen(direction)) BuildOpenSide(root, direction, half, bodyHeight, cellStone, metal, signal, random);
                else BuildClosedSide(root, direction, half, bodyHeight, cellStone, metal, signal, random);
            }
            BuildVerticalSilhouette(root, cell, half, bodyHeight, cellStone, metal, signal, random);
            BuildProps(root, cell, half, cellStone, signal, random);
            BuildSignals(root, cell, half, bodyHeight, signal, random);
        }

        private void BuildFoundation(
            Transform root,
            GeneratedWorldCellV07 cell,
            Material primary,
            Material structural,
            Material signal,
            System.Random random)
        {
            float half = cell.CellSize * 0.5f;
            int inlays = Mathf.Min(budget.floorInlays, 4);
            for (int i = 0; i < inlays; i++)
            {
                float axis = i % 2 == 0 ? 1f : -1f;
                float x = i % 2 == 0 ? 0f : Mathf.Lerp(-half * 0.55f, half * 0.55f, (float)random.NextDouble());
                float z = i % 2 == 0 ? Mathf.Lerp(-half * 0.55f, half * 0.55f, (float)random.NextDouble()) : 0f;
                Vector3 scale = i % 2 == 0
                    ? new Vector3(cell.CellSize * 0.72f, 0.025f, 0.055f)
                    : new Vector3(0.055f, 0.025f, cell.CellSize * 0.72f);
                Create("floor_inlay_" + i, PrimitiveType.Cube, root,
                    new Vector3(x, 0.035f, z), scale, signal, Quaternion.identity);
            }

            if ((StableHash(cell.TileId) & 2) != 0)
            {
                Create("center_plinth", PrimitiveType.Cylinder, root,
                    new Vector3(0f, 0.13f, 0f), new Vector3(1.42f, 0.13f, 1.42f), primary,
                    Quaternion.Euler(0f, (cell.Grid.x + cell.Grid.y) * 11f, 0f));
                Create("center_ring", PrimitiveType.Cylinder, root,
                    new Vector3(0f, 0.29f, 0f), new Vector3(0.94f, 0.045f, 0.94f), structural,
                    Quaternion.identity);
            }
        }

        private void BuildCorners(
            Transform root,
            float half,
            float height,
            Material primary,
            Material structural,
            Material signal,
            System.Random random)
        {
            int count = Mathf.Min(4, budget.cornerButtresses);
            Vector2[] corners =
            {
                new Vector2(-1f, -1f), new Vector2(-1f, 1f),
                new Vector2(1f, -1f), new Vector2(1f, 1f),
            };
            for (int i = 0; i < count; i++)
            {
                Vector2 corner = corners[i];
                float inset = 0.34f;
                Vector3 p = new Vector3(corner.x * (half - inset), height * 0.5f, corner.y * (half - inset));
                float taper = 0.52f + (float)random.NextDouble() * 0.14f;
                Create("corner_pier_" + i, PrimitiveType.Cube, root, p,
                    new Vector3(taper, height, taper), primary,
                    Quaternion.Euler(0f, i * 90f + 45f, 0f));
                Create("corner_cap_" + i, PrimitiveType.Cube, root,
                    p + Vector3.up * (height * 0.5f + 0.16f),
                    new Vector3(taper * 1.55f, 0.22f, taper * 1.55f), structural,
                    Quaternion.Euler(0f, 45f, 0f));
                if ((i & 1) == 0)
                {
                    Create("corner_signal_" + i, PrimitiveType.Cube, root,
                        p + new Vector3(-corner.x * 0.31f, 0.2f, -corner.y * 0.31f),
                        new Vector3(0.055f, height * 0.58f, 0.055f), signal, Quaternion.identity);
                }
            }
        }

        private void BuildOpenSide(
            Transform root,
            int direction,
            float half,
            float height,
            Material primary,
            Material structural,
            Material signal,
            System.Random random)
        {
            int segments = Mathf.Clamp(budget.archSegmentsPerOpenSide, 0, 4);
            if (segments == 0) return;
            float opening = Mathf.Clamp(half * 0.68f, 1.5f, 2.3f);
            float side = Mathf.Max(0.38f, (half - opening) * 0.72f);

            // Two jamb towers frame each navigable socket without adding collision.
            for (int i = 0; i < 2; i++)
            {
                float sign = i == 0 ? -1f : 1f;
                Vector3 local = SidePosition(direction, half - 0.17f, sign * (opening + side * 0.34f), height * 0.46f);
                Vector3 scale = SideScale(direction, side, height * 0.92f, 0.34f);
                Create($"open_{direction}_jamb_{i}", PrimitiveType.Cube, root, local, scale,
                    primary, Quaternion.identity);
            }

            float lintelY = height * 0.91f;
            Create($"open_{direction}_lintel", PrimitiveType.Cube, root,
                SidePosition(direction, half - 0.18f, 0f, lintelY),
                SideScale(direction, opening * 2.2f, 0.32f, 0.40f), structural, Quaternion.identity);

            if (segments >= 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    float sign = i == 0 ? -1f : 1f;
                    Create($"open_{direction}_fin_{i}", PrimitiveType.Cube, root,
                        SidePosition(direction, half - 0.25f, sign * opening * 0.76f, height * 0.67f),
                        SideScale(direction, 0.12f, height * 0.42f, 0.12f), signal,
                        Quaternion.Euler(0f, direction * 90f, sign * 8f));
                }
            }

            if (segments >= 3 && random.NextDouble() > 0.35)
            {
                Create($"open_{direction}_crown", PrimitiveType.Cube, root,
                    SidePosition(direction, half - 0.16f, 0f, height + 0.40f),
                    SideScale(direction, opening * 1.08f, 0.68f, 0.32f), primary,
                    Quaternion.Euler(0f, direction * 90f, 0f));
            }
        }

        private void BuildClosedSide(
            Transform root,
            int direction,
            float half,
            float height,
            Material primary,
            Material structural,
            Material signal,
            System.Random random)
        {
            int ribs = Mathf.Clamp(budget.wallRibsPerClosedSide, 0, 4);
            for (int i = 0; i < ribs; i++)
            {
                float t = ribs == 1 ? 0f : Mathf.Lerp(-0.48f, 0.48f, i / (float)(ribs - 1));
                Vector3 p = SidePosition(direction, half - 0.23f, t * half * 1.35f, height * 0.50f);
                Create($"closed_{direction}_rib_{i}", PrimitiveType.Cube, root, p,
                    SideScale(direction, 0.23f, height * 0.92f, 0.31f),
                    i % 2 == 0 ? structural : primary, Quaternion.identity);
            }

            if (random.NextDouble() > 0.44)
            {
                float y = height * Mathf.Lerp(0.45f, 0.72f, (float)random.NextDouble());
                Create($"closed_{direction}_signal", PrimitiveType.Cube, root,
                    SidePosition(direction, half - 0.30f, 0f, y),
                    SideScale(direction, half * 1.05f, 0.055f, 0.055f), signal, Quaternion.identity);
            }
        }

        private void BuildVerticalSilhouette(
            Transform root,
            GeneratedWorldCellV07 cell,
            float half,
            float height,
            Material primary,
            Material structural,
            Material signal,
            System.Random random)
        {
            int overhead = Mathf.Clamp(budget.overheadStructures, 0, 3);
            if (overhead <= 0) return;

            int openCount = 0;
            for (int d = 0; d < 4; d++) if (cell.IsOpen(d)) openCount++;

            if (openCount >= 3 && random.NextDouble() > 0.32)
            {
                float towerHeight = 2.1f + (float)random.NextDouble() * 1.8f;
                Create("signal_spire", PrimitiveType.Cylinder, root,
                    new Vector3(0f, height + towerHeight * 0.5f, 0f),
                    new Vector3(0.34f, towerHeight * 0.5f, 0.34f), structural, Quaternion.identity);
                Create("signal_spire_core", PrimitiveType.Sphere, root,
                    new Vector3(0f, height + towerHeight + 0.18f, 0f),
                    Vector3.one * 0.34f, signal, Quaternion.identity);
            }
            else if (openCount == 2 && random.NextDouble() > 0.28)
            {
                bool northSouth = cell.IsOpen(0) && cell.IsOpen(2);
                Vector3 scale = northSouth
                    ? new Vector3(cell.CellSize * 0.72f, 0.20f, 0.28f)
                    : new Vector3(0.28f, 0.20f, cell.CellSize * 0.72f);
                Create("overhead_crossbeam", PrimitiveType.Cube, root,
                    new Vector3(0f, height + 0.68f, 0f), scale, structural, Quaternion.identity);

                for (int i = -1; i <= 1; i += 2)
                {
                    Vector3 p = northSouth
                        ? new Vector3(i * half * 0.62f, height + 0.42f, 0f)
                        : new Vector3(0f, height + 0.42f, i * half * 0.62f);
                    Create("beam_drop_" + i, PrimitiveType.Cube, root, p,
                        new Vector3(0.12f, 0.80f, 0.12f), signal, Quaternion.identity);
                }
            }
            else if (cell.HeightSteps > 0 && random.NextDouble() > 0.36)
            {
                float side = random.NextDouble() > 0.5 ? -1f : 1f;
                Create("vertical_marker", PrimitiveType.Cube, root,
                    new Vector3(side * half * 0.63f, height + 0.78f, -side * half * 0.48f),
                    new Vector3(0.42f, 1.55f + cell.HeightSteps * 0.32f, 0.42f), primary,
                    Quaternion.Euler(0f, 45f, 0f));
                Create("vertical_marker_cap", PrimitiveType.Sphere, root,
                    new Vector3(side * half * 0.63f, height + 1.65f + cell.HeightSteps * 0.25f, -side * half * 0.48f),
                    Vector3.one * 0.28f, signal, Quaternion.identity);
            }
        }

        private void BuildProps(
            Transform root,
            GeneratedWorldCellV07 cell,
            float half,
            Material primary,
            Material signal,
            System.Random random)
        {
            int clusters = Mathf.Clamp(budget.propClusters, 0, 4);
            for (int i = 0; i < clusters; i++)
            {
                float x = Mathf.Lerp(-half * 0.58f, half * 0.58f, (float)random.NextDouble());
                float z = Mathf.Lerp(-half * 0.58f, half * 0.58f, (float)random.NextDouble());
                if (Mathf.Abs(x) < 0.85f && Mathf.Abs(z) < 0.85f) x += x >= 0f ? 1.1f : -1.1f;

                int type = random.Next(0, 4);
                if (type == 0)
                {
                    Create("prop_plinth_" + i, PrimitiveType.Cylinder, root,
                        new Vector3(x, 0.18f, z), new Vector3(0.62f, 0.18f, 0.62f), primary,
                        Quaternion.Euler(0f, (float)random.NextDouble() * 180f, 0f));
                    Create("prop_relic_" + i, PrimitiveType.Sphere, root,
                        new Vector3(x, 0.62f, z), Vector3.one * 0.22f, signal, Quaternion.identity);
                }
                else if (type == 1)
                {
                    Create("prop_crate_" + i, PrimitiveType.Cube, root,
                        new Vector3(x, 0.24f, z), new Vector3(0.62f, 0.48f, 0.62f), patina != null ? patina : metal,
                        Quaternion.Euler(0f, (float)random.NextDouble() * 25f - 12.5f, 0f));
                }
                else if (type == 2)
                {
                    Create("prop_shard_a_" + i, PrimitiveType.Cube, root,
                        new Vector3(x, 0.26f, z), new Vector3(0.16f, 0.52f, 0.20f), primary,
                        Quaternion.Euler((float)random.NextDouble() * 22f, (float)random.NextDouble() * 180f, (float)random.NextDouble() * 22f));
                    Create("prop_shard_b_" + i, PrimitiveType.Cube, root,
                        new Vector3(x + 0.28f, 0.18f, z - 0.20f), new Vector3(0.12f, 0.36f, 0.16f), primary,
                        Quaternion.Euler(14f, (float)random.NextDouble() * 180f, -9f));
                }
                else
                {
                    Create("prop_terminal_" + i, PrimitiveType.Cube, root,
                        new Vector3(x, 0.62f, z), new Vector3(0.48f, 1.24f, 0.38f), metal, Quaternion.identity);
                    Create("prop_terminal_signal_" + i, PrimitiveType.Cube, root,
                        new Vector3(x, 0.83f, z + 0.205f), new Vector3(0.27f, 0.36f, 0.025f), signal, Quaternion.identity);
                }
            }
        }

        private void BuildSignals(
            Transform root,
            GeneratedWorldCellV07 cell,
            float half,
            float height,
            Material signal,
            System.Random random)
        {
            int accents = Mathf.Clamp(budget.signalAccents, 0, 4);
            for (int i = 0; i < accents; i++)
            {
                float angle = ((cell.Grid.x * 37 + cell.Grid.y * 19 + i * 83 + detailSeed) % 360) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * half * 0.70f;
                p.y = height * Mathf.Lerp(0.24f, 0.70f, (float)random.NextDouble());
                Create("signal_node_" + i, PrimitiveType.Sphere, root, p,
                    Vector3.one * Mathf.Lerp(0.12f, 0.20f, (float)random.NextDouble()), signal, Quaternion.identity);
            }
        }

        private GameObject Create(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Quaternion localRotation)
        {
            int cap = Mathf.Max(0, budget.maxDecorativePrimitivesPerCell);
            if (_createdThisCell >= cap) return null;
            _createdThisCell++;

            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.transform.localRotation = localRotation;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            go.isStatic = true;
            return go;
        }

        private Material SignalFor(int hash)
        {
            int index = Mathf.Abs(hash % 3);
            if (index == 0 && cyan != null) return cyan;
            if (index == 1 && green != null) return green;
            if (violet != null) return violet;
            return cyan != null ? cyan : green;
        }

        private static Vector3 SidePosition(int direction, float edge, float lateral, float y)
        {
            switch (direction)
            {
                case 0: return new Vector3(lateral, y, edge);
                case 1: return new Vector3(edge, y, -lateral);
                case 2: return new Vector3(-lateral, y, -edge);
                default: return new Vector3(-edge, y, lateral);
            }
        }

        private static Vector3 SideScale(int direction, float lateral, float vertical, float depth)
        {
            return direction == 0 || direction == 2
                ? new Vector3(lateral, vertical, depth)
                : new Vector3(depth, vertical, lateral);
        }

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

        private static int CompareCells(GeneratedWorldCellV07 a, GeneratedWorldCellV07 b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            int y = a.Grid.y.CompareTo(b.Grid.y);
            return y != 0 ? y : a.Grid.x.CompareTo(b.Grid.x);
        }
    }
}
