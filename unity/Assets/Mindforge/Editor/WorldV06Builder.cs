#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// V0.6 integration pass. It keeps the authored Memory Forge -> Cathedral spine intact,
    /// then adds one deterministic procedural annex plus representative persistent world
    /// content. Every interactive object publishes through the existing contextual E router,
    /// owns a stable world id and participates in the profile-v2 persistence architecture.
    /// </summary>
    public static class WorldV06Builder
    {
        public const string RootName = "Mindforge_Persistent_World_V06";
        public const string Revision = "PERSISTENT_WORLD_V06";
        private const string DialoguePath = "Assets/Mindforge/Generated/WorldV06/ArchivistDialogue.asset";

        [MenuItem("Mindforge/Legacy/Showcase/Apply Persistent World V0.6", priority = 34)]
        public static void ApplyOpenScene()
        {
            GameObject guardian = EditorSceneLookup.FindIncludingInactive("Guardian");
            MemoryForgeCheckpoint checkpoint = UnityEngine.Object.FindObjectOfType<MemoryForgeCheckpoint>(true);
            WorldSignalBus bus = UnityEngine.Object.FindObjectOfType<WorldSignalBus>(true);
            WorldStateLedger ledger = UnityEngine.Object.FindObjectOfType<WorldStateLedger>(true);
            PlayerProgressionLedger progression = UnityEngine.Object.FindObjectOfType<PlayerProgressionLedger>(true);
            WorldShortcut shortcut = UnityEngine.Object.FindObjectOfType<WorldShortcut>(true);

            if (guardian == null || checkpoint == null || bus == null || ledger == null || progression == null || shortcut == null)
                throw new InvalidOperationException(
                    "Persistent World V0.6 requires Guardian, Memory Forge, Game Foundation V1, UX V0.5 and the Null Ward shortcut.");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);

            GameObject root = new GameObject(RootName);
            root.SetActive(false);

            GameObject foundationRoot = bus.gameObject;
            PlayerInventoryV06 inventory = foundationRoot.GetComponent<PlayerInventoryV06>();
            if (inventory == null) inventory = foundationRoot.AddComponent<PlayerInventoryV06>();
            inventory.ConfigureRuntime(bus);

            PlayerProfileSaveV06 profile = foundationRoot.GetComponent<PlayerProfileSaveV06>();
            if (profile == null) profile = foundationRoot.AddComponent<PlayerProfileSaveV06>();
            profile.ConfigureRuntime(ledger, progression, inventory, checkpoint, bus);

            // V0.6 is the sole active disk persistence authority. Keeping the V0.5 component
            // present makes migration/inspection easy without allowing two writers to race.
            PlayerProfileSaveV05 legacy = foundationRoot.GetComponent<PlayerProfileSaveV05>();
            if (legacy != null) legacy.enabled = false;

            WorldShortcutInteractionV06 shortcutOffer = shortcut.GetComponent<WorldShortcutInteractionV06>();
            if (shortcutOffer == null) shortcutOffer = shortcut.gameObject.AddComponent<WorldShortcutInteractionV06>();
            shortcutOffer.ConfigureRuntime(
                "memory_forge_market_loop",
                "Memory Conduit",
                shortcut,
                ledger,
                bus);

            Material basalt = FindMaterial("ArenaBasalt");
            Material obsidian = FindMaterial("ObsidianArchitecture");
            Material metal = FindMaterial("GuardianMetal");
            Material cyan = FindMaterial("AetherCyan");
            Material green = FindMaterial("WispVerdant");

            BuildNeuralCloister(root.transform, basalt, obsidian, cyan);
            BuildPersistentLoot(root.transform, inventory, ledger, bus, cyan, green, metal);
            BuildShrine(root.transform, inventory, profile, ledger, bus, cyan, metal);
            BuildArchivist(root.transform, ledger, cyan, metal);
            BuildRegionDiscovery(root.transform, inventory, ledger);

            root.SetActive(true);

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(foundationRoot);
            EditorUtility.SetDirty(inventory);
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(shortcut);
            EditorUtility.SetDirty(shortcutOffer);
            if (legacy != null) EditorUtility.SetDirty(legacy);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Mindforge:WorldV06] Profile-v2 + stable persistent shortcut + two idempotent loot nodes + Signal Shrine + Archivist dialogue + " +
                "Neural Cloister region discovery + deterministic constraint-generated architecture installed. Authored landmarks remain authoritative; " +
                "generated geometry fills a bounded east-side annex inside the existing Grounded World collision basin.");
        }

        private static void BuildNeuralCloister(Transform parent, Material floor, Material wall, Material accent)
        {
            GameObject annex = new GameObject("Neural_Cloister_Procedural_Annex");
            annex.transform.SetParent(parent, false);
            annex.transform.localPosition = new Vector3(27f, 0.20f, -35f);

            ModularWorldAssemblerV06 assembler = annex.AddComponent<ModularWorldAssemblerV06>();
            SerializedObject serialized = new SerializedObject(assembler);
            serialized.FindProperty("gridSize").vector2IntValue = new Vector2Int(3, 5);
            serialized.FindProperty("cellSize").floatValue = 5.2f;
            serialized.FindProperty("heightStepMeters").floatValue = 1.05f;
            serialized.FindProperty("seed").intValue = 60613;
            serialized.FindProperty("retryCount").intValue = 12;
            serialized.FindProperty("buildOnStart").boolValue = false;
            // The Grounded World V1 perimeter is already the collision shell. A second outer
            // wall here would make the annex feel like a box rather than part of Aetheria.
            serialized.FindProperty("enclosePerimeter").boolValue = false;
            serialized.FindProperty("floorMaterial").objectReferenceValue = floor;
            serialized.FindProperty("wallMaterial").objectReferenceValue = wall;
            serialized.FindProperty("accentMaterial").objectReferenceValue = accent;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            assembler.Generate();

            // Fixed authored threshold makes the generated annex legible from the main route.
            Primitive("Cloister_Threshold", PrimitiveType.Cube, parent,
                new Vector3(19.4f, 0.12f, -35f), new Vector3(5.6f, 0.28f, 4.0f), floor, true);
            Primitive("Cloister_Threshold_Rib_L", PrimitiveType.Cube, parent,
                new Vector3(19.4f, 1.45f, -37.0f), new Vector3(0.30f, 3.1f, 0.30f), accent, false);
            Primitive("Cloister_Threshold_Rib_R", PrimitiveType.Cube, parent,
                new Vector3(19.4f, 1.45f, -33.0f), new Vector3(0.30f, 3.1f, 0.30f), accent, false);
            CreateLine("Cloister_Signal_Trace", parent,
                new Vector3(17.0f, 0.30f, -35f), new Vector3(22.0f, 0.30f, -35f), 0.055f, accent);
        }

        private static void BuildPersistentLoot(
            Transform parent,
            PlayerInventoryV06 inventory,
            WorldStateLedger ledger,
            WorldSignalBus bus,
            Material cyan,
            Material green,
            Material metal)
        {
            GameObject shard = Primitive("Loot_Forge_MemoryShard_01", PrimitiveType.Sphere, parent,
                new Vector3(0.2f, 0.85f, -56.6f), Vector3.one * 0.52f, cyan, true);
            AddOrbitRings(shard.transform, metal, 0.72f);
            PersistentPickupInteractionV06 shardPickup = shard.AddComponent<PersistentPickupInteractionV06>();
            shardPickup.ConfigureRuntime(
                "forge.memory_shard.01",
                "memory_shard",
                "Memory Shard",
                1,
                inventory,
                ledger,
                bus);

            GameObject lens = Primitive("Loot_Cloister_AetherLens_01", PrimitiveType.Sphere, parent,
                new Vector3(23.4f, 1.05f, -28.5f), new Vector3(0.62f, 0.28f, 0.62f), green, true);
            AddOrbitRings(lens.transform, cyan, 0.82f);
            PersistentPickupInteractionV06 lensPickup = lens.AddComponent<PersistentPickupInteractionV06>();
            lensPickup.ConfigureRuntime(
                "cloister.aether_lens.01",
                "aether_lens",
                "Aether Lens",
                1,
                inventory,
                ledger,
                bus,
                "focus");
        }

        private static void BuildShrine(
            Transform parent,
            PlayerInventoryV06 inventory,
            PlayerProfileSaveV06 profile,
            WorldStateLedger ledger,
            WorldSignalBus bus,
            Material signal,
            Material metal)
        {
            GameObject shrine = new GameObject("Signal_Shrine_Neural_Cloister");
            shrine.transform.SetParent(parent, false);
            shrine.transform.localPosition = new Vector3(30.4f, 0f, -26.8f);
            Primitive("Shrine_Base", PrimitiveType.Cylinder, shrine.transform,
                new Vector3(0f, 0.18f, 0f), new Vector3(2.2f, 0.20f, 2.2f), metal, true);
            Primitive("Shrine_Spine", PrimitiveType.Cylinder, shrine.transform,
                new Vector3(0f, 1.05f, 0f), new Vector3(0.34f, 1.55f, 0.34f), metal, false);
            Primitive("Shrine_Core", PrimitiveType.Sphere, shrine.transform,
                new Vector3(0f, 1.85f, 0f), Vector3.one * 0.54f, signal, false);
            AddOrbitRings(shrine.transform, signal, 1.10f);

            PersistentShrineInteractionV06 interaction = shrine.AddComponent<PersistentShrineInteractionV06>();
            interaction.ConfigureRuntime(
                "cloister.signal_shrine.01",
                "Neural Cloister Shrine",
                "neural_cloister",
                inventory,
                profile,
                ledger,
                bus);
        }

        private static void BuildArchivist(Transform parent, WorldStateLedger ledger, Material signal, Material metal)
        {
            DialogueGraphV06 graph = EnsureArchivistDialogue();
            GameObject npc = new GameObject("NPC_NullMarket_Archivist");
            npc.transform.SetParent(parent, false);
            npc.transform.localPosition = new Vector3(-5.1f, 0f, -33.8f);

            Primitive("Body", PrimitiveType.Capsule, npc.transform,
                new Vector3(0f, 1.05f, 0f), new Vector3(0.78f, 1.05f, 0.78f), metal, true);
            Primitive("FaceSignal", PrimitiveType.Sphere, npc.transform,
                new Vector3(0f, 1.92f, 0.34f), Vector3.one * 0.25f, signal, false);
            Primitive("Pack", PrimitiveType.Cube, npc.transform,
                new Vector3(0f, 1.05f, -0.44f), new Vector3(0.62f, 0.86f, 0.28f), metal, false);

            NpcDialogueInteractionV06 interaction = npc.AddComponent<NpcDialogueInteractionV06>();
            interaction.ConfigureRuntime("null_market.archivist.01", "the Archivist", graph, ledger);
        }

        private static void BuildRegionDiscovery(Transform parent, PlayerInventoryV06 inventory, WorldStateLedger ledger)
        {
            GameObject trigger = new GameObject("Region_NeuralCloister_Discovery");
            trigger.transform.SetParent(parent, false);
            trigger.transform.localPosition = new Vector3(27f, 1.4f, -35f);
            BoxCollider collider = trigger.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(17f, 4.0f, 29f);
            RegionDiscoveryV06 region = trigger.AddComponent<RegionDiscoveryV06>();
            region.ConfigureRuntime("neural_cloister", inventory, ledger);
        }

        private static DialogueGraphV06 EnsureArchivistDialogue()
        {
            EnsureFolder("Assets/Mindforge/Generated/WorldV06");
            DialogueGraphV06 graph = AssetDatabase.LoadAssetAtPath<DialogueGraphV06>(DialoguePath);
            if (graph == null)
            {
                graph = ScriptableObject.CreateInstance<DialogueGraphV06>();
                AssetDatabase.CreateAsset(graph, DialoguePath);
            }

            graph.graph_id = "null_market.archivist.01";
            graph.start_node_id = "start";
            graph.nodes = new List<DialogueNodeV06>
            {
                new DialogueNodeV06
                {
                    id = "start",
                    speaker = "ARCHIVIST",
                    text = "The Forge remembers actions better than stories. Gates, relics, shrines... give each thing one name and the city stops contradicting itself.",
                    set_bool_fact = "story.archivist.met",
                    set_bool_value = true,
                    choices = new List<DialogueChoiceV06>
                    {
                        new DialogueChoiceV06
                        {
                            label = "Ask about the Forge",
                            next_node_id = "forge",
                            set_bool_fact = "story.archivist.asked_about_forge",
                            set_bool_value = true,
                        },
                        new DialogueChoiceV06
                        {
                            label = "Ask about the Cloister",
                            next_node_id = "cloister",
                            set_bool_fact = "story.archivist.asked_about_cloister",
                            set_bool_value = true,
                        },
                    },
                },
                new DialogueNodeV06
                {
                    id = "forge",
                    speaker = "ARCHIVIST",
                    text = "Rest there when you want the world to commit. A claimed relic stays claimed. An opened conduit stays open. Memory should change consequences, not invent them.",
                },
                new DialogueNodeV06
                {
                    id = "cloister",
                    speaker = "ARCHIVIST",
                    text = "East of the Market. The halls recompose from a small architectural grammar, but the shrine and every meaningful object keep their identity.",
                },
            };
            EditorUtility.SetDirty(graph);
            return graph;
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

        private static Material FindMaterial(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Material", new[] { "Assets/Mindforge" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null && string.Equals(material.name, name, StringComparison.OrdinalIgnoreCase)) return material;
            }
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }

        private static GameObject Primitive(
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
            if (shape != null && !collider) UnityEngine.Object.DestroyImmediate(shape);
            return go;
        }

        private static void AddOrbitRings(Transform parent, Material material, float radius)
        {
            for (int i = 0; i < 2; i++)
            {
                GameObject ring = Primitive("SignalRing_" + i, PrimitiveType.Cylinder, parent,
                    new Vector3(0f, 0.0f, 0f), new Vector3(radius, 0.025f, radius), material, false);
                ring.transform.localRotation = Quaternion.Euler(90f, i * 55f, i * 31f);
            }
        }

        private static void CreateLine(string name, Transform parent, Vector3 a, Vector3 b, float width, Material material)
        {
            GameObject line = new GameObject(name);
            line.transform.SetParent(parent, false);
            LineRenderer lr = line.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.startWidth = width;
            lr.endWidth = width;
            if (material != null) lr.sharedMaterial = material;
        }
    }
}
#endif
