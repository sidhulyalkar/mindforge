#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Journey;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// V0.6 proof slice for reusable persistent world content. It adds one optional side
    /// Resonance Cache containing a persistent JourneyGate and idempotent pickup, then binds
    /// WorldSaveCoordinatorV1 to Memory Forge rest. The main route remains unobstructed.
    /// </summary>
    public static class WorldPersistenceContentV06Builder
    {
        public const string RootName = "Mindforge_WorldPersistence_Content_V06";
        public const string Revision = "WORLD_PERSISTENCE_CONTENT_V06";

        private static readonly StaticEditorFlags PhysicalStatic =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        [MenuItem("Mindforge/Showcase/Apply World Persistence + Content V0.6", priority = 34)]
        public static void ApplyOpenScene()
        {
            GameObject ux = EditorSceneLookup.FindIncludingInactive(UxInteractionSaveV05Builder.RootName);
            WorldSignalBus bus = UnityEngine.Object.FindObjectOfType<WorldSignalBus>(true);
            WorldStateLedger ledger = UnityEngine.Object.FindObjectOfType<WorldStateLedger>(true);
            PlayerProgressionLedger progression = UnityEngine.Object.FindObjectOfType<PlayerProgressionLedger>(true);
            PlayerProfileSaveV05 profile = UnityEngine.Object.FindObjectOfType<PlayerProfileSaveV05>(true);
            MemoryForgeCheckpoint checkpoint = UnityEngine.Object.FindObjectOfType<MemoryForgeCheckpoint>(true);

            if (ux == null || bus == null || ledger == null || progression == null || profile == null || checkpoint == null)
                throw new InvalidOperationException(
                    "World Persistence V0.6 requires UX V0.5, Game Foundation, player progression/profile and Memory Forge.");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);

            CinematicMaterialAuthoring.EnsureAuthored();
            Material obsidian = RequireMaterial("ObsidianArchitecture");
            Material metal = RequireMaterial("GuardianMetal");
            Material cyan = RequireMaterial("AetherCyan");
            Material green = RequireMaterial("WispVerdant");

            GameObject root = new GameObject(RootName);
            BuildResonanceCache(root.transform, bus, ledger, progression, obsidian, metal, cyan, green);

            WorldSaveCoordinatorV1 coordinator = bus.GetComponent<WorldSaveCoordinatorV1>();
            if (coordinator == null) coordinator = bus.gameObject.AddComponent<WorldSaveCoordinatorV1>();
            coordinator.ConfigureRuntime(checkpoint, profile, bus, "aetheria.v06");

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(coordinator);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Mindforge:WorldV06] Added optional Market Resonance Cache with stable persistent gate + receipt-safe Resonance pickup. " +
                "Memory Forge rest now captures validated safe-boundary physical authority snapshots after profile persistence. Main route remains open; " +
                "arbitrary mid-combat resume is intentionally outside V0.6 proof scope.");
        }

        private static void BuildResonanceCache(
            Transform parent,
            WorldSignalBus bus,
            WorldStateLedger ledger,
            PlayerProgressionLedger progression,
            Material obsidian,
            Material metal,
            Material cyan,
            Material green)
        {
            GameObject cache = new GameObject("Market_ResonanceCache_V06");
            cache.transform.SetParent(parent, false);

            // Optional side-space east of Broken Momentum Market. Nothing here intersects
            // the primary north/south route, so persistence content can fail closed safely.
            PhysicalPart("CacheFloor", cache.transform, new Vector3(24.0f, -0.22f, -28.5f), new Vector3(8.0f, 0.44f, 7.0f), obsidian);
            PhysicalPart("CacheNorthWall", cache.transform, new Vector3(24.0f, 1.35f, -32.0f), new Vector3(8.0f, 2.7f, 0.45f), obsidian);
            PhysicalPart("CacheSouthWall", cache.transform, new Vector3(24.0f, 1.35f, -25.0f), new Vector3(8.0f, 2.7f, 0.45f), obsidian);
            PhysicalPart("CacheEastWall", cache.transform, new Vector3(28.0f, 1.35f, -28.5f), new Vector3(0.45f, 2.7f, 7.0f), obsidian);

            // Visual breadcrumbs from the market toward the optional cache. These have no
            // colliders and therefore cannot accidentally create a new traversal authority.
            for (int i = 0; i < 4; i++)
            {
                float x = 13.8f + i * 1.9f;
                DecorativePart($"CacheSignalPaver_{i}", cache.transform, new Vector3(x, 0.04f, -28.5f), new Vector3(1.2f, 0.04f, 0.28f), i % 2 == 0 ? cyan : green);
            }

            GameObject gateRoot = new GameObject("PersistentGate_MarketResonanceCache");
            gateRoot.transform.SetParent(cache.transform, false);
            gateRoot.transform.position = new Vector3(20.05f, 0f, -28.5f);

            GameObject seal = new GameObject("GateSealVisual");
            seal.transform.SetParent(gateRoot.transform, false);
            seal.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            DecorativePart("GateSealMass", seal.transform, Vector3.zero, new Vector3(0.58f, 2.75f, 4.75f), metal);
            DecorativePart("GateSignalA", seal.transform, new Vector3(-0.31f, 0.20f, 0f), new Vector3(0.05f, 2.1f, 3.85f), cyan);
            DecorativePart("GateSignalB", seal.transform, new Vector3(0.31f, -0.20f, 0f), new Vector3(0.05f, 2.1f, 3.85f), green);

            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "GateCollisionBlocker";
            blocker.transform.SetParent(gateRoot.transform, false);
            blocker.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            blocker.transform.localScale = new Vector3(0.72f, 2.9f, 4.9f);
            Renderer blockerRenderer = blocker.GetComponent<Renderer>();
            if (blockerRenderer != null) blockerRenderer.enabled = false;
            Collider blockerCollider = blocker.GetComponent<Collider>();

            JourneyGate gate = gateRoot.AddComponent<JourneyGate>();
            gate.ConfigureRuntime(seal.transform, blockerCollider != null ? new[] { blockerCollider } : Array.Empty<Collider>());
            gate.SetOpen(false, true);

            PersistentWorldGateV1 persistentGate = gateRoot.AddComponent<PersistentWorldGateV1>();
            persistentGate.ConfigureRuntime(
                "gate.market_resonance_cache",
                gate,
                ledger,
                bus,
                "Open Resonance Cache");

            GameObject pickupRoot = new GameObject("PersistentPickup_MarketResonancePrism");
            pickupRoot.transform.SetParent(cache.transform, false);
            pickupRoot.transform.position = new Vector3(24.35f, 0f, -28.5f);
            GameObject pickupVisual = new GameObject("ResonancePrismVisual");
            pickupVisual.transform.SetParent(pickupRoot.transform, false);
            pickupVisual.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            DecorativePart("ResonanceCore", pickupVisual.transform, Vector3.zero, new Vector3(0.62f, 1.15f, 0.62f), cyan);
            DecorativePart("ResonanceHaloA", pickupVisual.transform, Vector3.zero, new Vector3(1.35f, 0.08f, 0.18f), green);
            DecorativePart("ResonanceHaloB", pickupVisual.transform, Vector3.zero, new Vector3(0.18f, 0.08f, 1.35f), green);

            PersistentWorldPickupV1 pickup = pickupRoot.AddComponent<PersistentWorldPickupV1>();
            pickup.ConfigureRuntime(
                "pickup.market_resonance_prism",
                pickupVisual.transform,
                progression,
                ledger,
                bus,
                WorldRewardKind.Resonance,
                25,
                null,
                "Claim Resonance Prism · +25");

            EditorUtility.SetDirty(gateRoot);
            EditorUtility.SetDirty(gate);
            EditorUtility.SetDirty(persistentGate);
            EditorUtility.SetDirty(pickupRoot);
            EditorUtility.SetDirty(pickup);
        }

        private static GameObject PhysicalPart(
            string name,
            Transform parent,
            Vector3 worldPosition,
            Vector3 scale,
            Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPosition;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(go, PhysicalStatic);
            return go;
        }

        private static GameObject DecorativePart(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = name == "ResonanceCore" ? Quaternion.Euler(45f, 45f, 0f) : Quaternion.identity;
            go.transform.localScale = localScale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return go;
        }

        private static Material RequireMaterial(string key)
        {
            Material material = CinematicMaterialAuthoring.Load(key);
            if (material == null)
                throw new InvalidOperationException($"World Persistence V0.6 missing cinematic material '{key}'.");
            return material;
        }
    }
}
#endif
