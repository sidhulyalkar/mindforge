#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Materializes the strongest procedural motifs as small collider-free prefabs that can
    /// be hand-tuned by an artist and later fed back into WorldTileDefinitionV06 catalogs.
    /// The bake is deterministic and presentation-only; it never rewrites an existing scene.
    /// </summary>
    public static class NeuralGothicPrefabBakerV07
    {
        public const string PrefabFolder = "Assets/Mindforge/Generated/WorldV07/Prefabs";

        private sealed class Piece
        {
            public string id;
            public Action<Transform, Material, Material, Material> build;
        }

        [MenuItem("Mindforge/Showcase/Bake Neural-Gothic Prefab Kit V0.7", priority = 23)]
        public static void EnsureBakedKit()
        {
            NeuralGothicMaterialAuthoringV07.EnsureAuthored();
            EnsureFolder(PrefabFolder);

            Material stone = Require(NeuralGothicMaterialAuthoringV07.Stone);
            Material metal = Require(NeuralGothicMaterialAuthoringV07.Metal);
            Material signal = Require("AetherCyan");

            Piece[] pieces = BuildCatalog();
            for (int i = 0; i < pieces.Length; i++) Bake(pieces[i], stone, metal, signal);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Mindforge:WorldV07] Baked {pieces.Length} collider-free neural-gothic art-kit prefabs under {PrefabFolder}.");
        }

        private static Piece[] BuildCatalog()
        {
            return new[]
            {
                PieceOf("NG_FloorPlinth", (r, s, m, e) =>
                {
                    P("body", PrimitiveType.Cylinder, r, new Vector3(0f, 0.12f, 0f), new Vector3(1.8f, 0.12f, 1.8f), s);
                    P("rim", PrimitiveType.Cylinder, r, new Vector3(0f, 0.27f, 0f), new Vector3(1.35f, 0.045f, 1.35f), m);
                }),
                PieceOf("NG_CornerPier", (r, s, m, e) =>
                {
                    P("pier", PrimitiveType.Cube, r, new Vector3(0f, 1.45f, 0f), new Vector3(0.58f, 2.9f, 0.58f), s, new Vector3(0f, 45f, 0f));
                    P("cap", PrimitiveType.Cube, r, new Vector3(0f, 3.02f, 0f), new Vector3(0.92f, 0.22f, 0.92f), m, new Vector3(0f, 45f, 0f));
                    P("signal", PrimitiveType.Cube, r, new Vector3(0.30f, 1.55f, 0f), new Vector3(0.045f, 1.55f, 0.045f), e);
                }),
                PieceOf("NG_ArchJamb", (r, s, m, e) =>
                {
                    P("jamb", PrimitiveType.Cube, r, new Vector3(0f, 1.30f, 0f), new Vector3(0.62f, 2.6f, 0.72f), s);
                    P("fin", PrimitiveType.Cube, r, new Vector3(0.37f, 1.55f, 0f), new Vector3(0.08f, 1.55f, 0.16f), e);
                    P("cap", PrimitiveType.Cube, r, new Vector3(0f, 2.72f, 0f), new Vector3(0.86f, 0.22f, 0.94f), m);
                }),
                PieceOf("NG_ArchLintel", (r, s, m, e) =>
                {
                    P("beam", PrimitiveType.Cube, r, new Vector3(0f, 0f, 0f), new Vector3(3.4f, 0.42f, 0.54f), m);
                    P("stoneCap", PrimitiveType.Cube, r, new Vector3(0f, 0.34f, 0f), new Vector3(2.75f, 0.28f, 0.72f), s);
                    P("trace", PrimitiveType.Cube, r, new Vector3(0f, -0.23f, 0.28f), new Vector3(2.15f, 0.045f, 0.045f), e);
                }),
                PieceOf("NG_WallRib", (r, s, m, e) =>
                {
                    P("rib", PrimitiveType.Cube, r, new Vector3(0f, 1.5f, 0f), new Vector3(0.30f, 3.0f, 0.48f), m);
                    P("foot", PrimitiveType.Cube, r, new Vector3(0f, 0.15f, 0f), new Vector3(0.72f, 0.30f, 0.72f), s, new Vector3(0f, 45f, 0f));
                }),
                PieceOf("NG_SignalFin", (r, s, m, e) =>
                {
                    P("frame", PrimitiveType.Cube, r, new Vector3(0f, 0.85f, 0f), new Vector3(0.22f, 1.7f, 0.34f), m, new Vector3(0f, 0f, 8f));
                    P("core", PrimitiveType.Cube, r, new Vector3(0.13f, 0.92f, 0.02f), new Vector3(0.045f, 1.15f, 0.055f), e, new Vector3(0f, 0f, 8f));
                }),
                PieceOf("NG_Terminal", (r, s, m, e) =>
                {
                    P("body", PrimitiveType.Cube, r, new Vector3(0f, 0.62f, 0f), new Vector3(0.72f, 1.24f, 0.48f), m);
                    P("face", PrimitiveType.Cube, r, new Vector3(0f, 0.80f, 0.26f), new Vector3(0.42f, 0.42f, 0.035f), e);
                    P("base", PrimitiveType.Cube, r, new Vector3(0f, 0.08f, 0f), new Vector3(0.95f, 0.16f, 0.72f), s);
                }),
                PieceOf("NG_RelicPlinth", (r, s, m, e) =>
                {
                    P("plinth", PrimitiveType.Cylinder, r, new Vector3(0f, 0.16f, 0f), new Vector3(0.86f, 0.16f, 0.86f), s);
                    P("ring", PrimitiveType.Cylinder, r, new Vector3(0f, 0.38f, 0f), new Vector3(0.62f, 0.06f, 0.62f), m);
                    P("relic", PrimitiveType.Sphere, r, new Vector3(0f, 0.88f, 0f), Vector3.one * 0.30f, e);
                }),
                PieceOf("NG_BrokenShardCluster", (r, s, m, e) =>
                {
                    P("shardA", PrimitiveType.Cube, r, new Vector3(-0.15f, 0.32f, 0.05f), new Vector3(0.18f, 0.64f, 0.24f), s, new Vector3(12f, 22f, -8f));
                    P("shardB", PrimitiveType.Cube, r, new Vector3(0.24f, 0.22f, -0.16f), new Vector3(0.14f, 0.44f, 0.18f), s, new Vector3(-8f, 67f, 15f));
                    P("shardC", PrimitiveType.Cube, r, new Vector3(0.02f, 0.16f, 0.30f), new Vector3(0.11f, 0.32f, 0.14f), m, new Vector3(18f, 112f, 6f));
                }),
                PieceOf("NG_Crossbeam", (r, s, m, e) =>
                {
                    P("beam", PrimitiveType.Cube, r, Vector3.zero, new Vector3(4.0f, 0.28f, 0.34f), m);
                    P("dropL", PrimitiveType.Cube, r, new Vector3(-1.45f, -0.42f, 0f), new Vector3(0.10f, 0.82f, 0.10f), e);
                    P("dropR", PrimitiveType.Cube, r, new Vector3(1.45f, -0.42f, 0f), new Vector3(0.10f, 0.82f, 0.10f), e);
                }),
                PieceOf("NG_SignalSpire", (r, s, m, e) =>
                {
                    P("base", PrimitiveType.Cylinder, r, new Vector3(0f, 0.18f, 0f), new Vector3(0.72f, 0.18f, 0.72f), s);
                    P("shaft", PrimitiveType.Cylinder, r, new Vector3(0f, 1.65f, 0f), new Vector3(0.20f, 1.45f, 0.20f), m);
                    P("core", PrimitiveType.Sphere, r, new Vector3(0f, 3.20f, 0f), Vector3.one * 0.30f, e);
                }),
                PieceOf("NG_GateCrown", (r, s, m, e) =>
                {
                    P("crown", PrimitiveType.Cube, r, new Vector3(0f, 0f, 0f), new Vector3(1.7f, 0.42f, 1.7f), s, new Vector3(0f, 45f, 0f));
                    P("needle", PrimitiveType.Cylinder, r, new Vector3(0f, 0.72f, 0f), new Vector3(0.10f, 0.72f, 0.10f), m);
                    P("node", PrimitiveType.Sphere, r, new Vector3(0f, 1.50f, 0f), Vector3.one * 0.18f, e);
                }),
            };
        }

        private static Piece PieceOf(string id, Action<Transform, Material, Material, Material> build)
            => new Piece { id = id, build = build };

        private static void Bake(Piece piece, Material stone, Material metal, Material signal)
        {
            GameObject root = new GameObject(piece.id);
            try
            {
                piece.build(root.transform, stone, metal, signal);
                string path = $"{PrefabFolder}/{piece.id}.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject P(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Vector3? localEuler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.transform.localEulerAngles = localEuler ?? Vector3.zero;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return go;
        }

        private static Material Require(string name)
        {
            Material material = CinematicMaterialAuthoring.Load(name);
            if (material == null) throw new InvalidOperationException("Prefab bake requires material: " + name);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
