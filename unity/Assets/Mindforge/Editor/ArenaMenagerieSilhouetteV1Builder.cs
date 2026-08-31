#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Journey;

namespace Mindforge.Editor
{
    /// <summary>
    /// Collider-free identity pass for the ten Menagerie Crucible roles. These are not
    /// skins over one mannequin: each role has a different mass distribution, negative
    /// space, weapon geometry and luminous focal point. Gameplay colliders remain on the
    /// JourneyEnemyController root and are never altered here.
    /// </summary>
    public static class ArenaMenagerieSilhouetteV1Builder
    {
        public const string RootName = "MenagerieIdentityV1";

        [MenuItem("Mindforge/Legacy/Showcase/Apply Arena Menagerie Silhouettes V1", priority = 28)]
        public static void ApplyOpenScene()
        {
            GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
            if (ward == null) throw new InvalidOperationException("Menagerie silhouettes require Null Ward.");

            Material hostile = Require("FracturedCore");
            Material fractured = Require("FracturedRing");
            Material metal = Require("GuardianMetal");
            Material obsidian = Require("ObsidianArchitecture");
            Material cyan = Require("AetherCyan");
            Material green = Require("WispVerdant");

            JourneyEnemyController[] enemies = ward.GetComponentsInChildren<JourneyEnemyController>(true);
            int rebuilt = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                JourneyEnemyController enemy = enemies[i];
                if (enemy == null || !enemy.name.StartsWith("Menagerie_", StringComparison.Ordinal)) continue;
                Transform visuals = enemy.transform.Find("Visuals");
                if (visuals == null) continue;

                DestroyChild(visuals, RootName);
                DestroyChild(visuals, NullWardEnemySilhouetteV3Builder.RootName);
                Renderer legacy = visuals.Find("Body")?.GetComponent<Renderer>();
                if (legacy != null) legacy.enabled = false;
                Transform core = visuals.Find("Core");
                Renderer coreRenderer = core != null ? core.GetComponent<Renderer>() : null;
                Material signal = coreRenderer != null && coreRenderer.sharedMaterial != null ? coreRenderer.sharedMaterial : hostile;
                Material body = legacy != null && legacy.sharedMaterial != null ? legacy.sharedMaterial : obsidian;
                float s = EstimateScale(core);

                GameObject root = new GameObject(RootName);
                root.transform.SetParent(visuals, false);
                string n = enemy.name;
                if (n.Contains("RiftHollow")) BuildRiftHollow(root.transform, s, body, signal);
                else if (n.Contains("Shardsinger")) BuildShardsinger(root.transform, s, body, signal, fractured);
                else if (n.Contains("SignalWarden")) BuildSignalWarden(root.transform, s, body, signal, metal);
                else if (n.Contains("NullSentry")) BuildNullSentry(root.transform, s, body, signal);
                else if (n.Contains("ChromePenitent")) BuildChromePenitent(root.transform, s, body, signal, metal);
                else if (n.Contains("RiftStalker")) BuildRiftStalker(root.transform, s, body, signal, green);
                else if (n.Contains("ChoirDrone")) BuildChoirDrone(root.transform, s, body, signal, cyan);
                else if (n.Contains("PrismMaw")) BuildPrismMaw(root.transform, s, body, signal, fractured);
                else if (n.Contains("VeilReaper")) BuildVeilReaper(root.transform, s, body, signal, hostile);
                else if (n.Contains("OrbitSeraph")) BuildOrbitSeraph(root.transform, s, body, signal, cyan);
                rebuilt++;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[Mindforge:MenagerieSilhouettes] Rebuilt {rebuilt} enemies with ten distinct collider-free silhouettes.");
        }

        private static void BuildRiftHollow(Transform p, float s, Material body, Material signal)
        {
            Part("Hollow_Wedge", PrimitiveType.Cube, p, V(0,.42f,.10f,s), V(.66f,.24f,.82f,s), new Vector3(12,0,0), body, true);
            Part("Hollow_Muzzle", PrimitiveType.Cube, p, V(0,.48f,.58f,s), V(.40f,.18f,.38f,s), new Vector3(-20,0,0), body, true);
            Part("Hollow_KnifeL", PrimitiveType.Cube, p, V(-.38f,.24f,.22f,s), V(.09f,.13f,.82f,s), new Vector3(0,-18,-22), body, true);
            Part("Hollow_KnifeR", PrimitiveType.Cube, p, V(.38f,.24f,.22f,s), V(.09f,.13f,.82f,s), new Vector3(0,18,22), body, true);
            Part("Hollow_Tail", PrimitiveType.Cube, p, V(0,.38f,-.60f,s), V(.08f,.10f,.68f,s), new Vector3(0,0,8), body, true);
            Part("Hollow_Eye", PrimitiveType.Cube, p, V(0,.51f,.79f,s), V(.26f,.045f,.025f,s), Vector3.zero, signal, false);
        }

        private static void BuildShardsinger(Transform p, float s, Material body, Material signal, Material accent)
        {
            Part("Singer_Stem", PrimitiveType.Cube, p, V(0,.78f,0,s), V(.20f,1.20f,.20f,s), new Vector3(0,0,45), body, true);
            Part("Singer_ForkL", PrimitiveType.Cube, p, V(-.30f,1.12f,0,s), V(.09f,.78f,.12f,s), new Vector3(0,-12,20), accent, false);
            Part("Singer_ForkR", PrimitiveType.Cube, p, V(.30f,1.12f,0,s), V(.09f,.78f,.12f,s), new Vector3(0,12,-20), accent, false);
            Part("Singer_Bow", PrimitiveType.Cube, p, V(0,.73f,-.20f,s), V(.86f,.08f,.12f,s), new Vector3(0,16,0), body, true);
            Part("Singer_Lens", PrimitiveType.Sphere, p, V(0,.88f,.22f,s), Vector3.one*.24f*s, Vector3.zero, signal, false);
            Part("Singer_Rune", PrimitiveType.Cube, p, V(0,.48f,.15f,s), V(.035f,.42f,.025f,s), Vector3.zero, signal, false);
        }

        private static void BuildSignalWarden(Transform p, float s, Material body, Material signal, Material metal)
        {
            Material hard = metal != null ? metal : body;
            Part("Warden_Gate", PrimitiveType.Cube, p, V(0,.90f,0,s), V(.98f,.88f,.52f,s), Vector3.zero, body, true);
            Part("Warden_TowerL", PrimitiveType.Cube, p, V(-.57f,1.02f,0,s), V(.20f,1.42f,.36f,s), new Vector3(0,0,-5), hard, true);
            Part("Warden_TowerR", PrimitiveType.Cube, p, V(.57f,1.02f,0,s), V(.20f,1.42f,.36f,s), new Vector3(0,0,5), hard, true);
            Part("Warden_Crown", PrimitiveType.Cube, p, V(0,1.62f,0,s), V(.82f,.18f,.48f,s), Vector3.zero, hard, true);
            Part("Warden_HornL", PrimitiveType.Cube, p, V(-.30f,1.93f,0,s), V(.09f,.58f,.14f,s), new Vector3(0,0,-18), hard, true);
            Part("Warden_HornR", PrimitiveType.Cube, p, V(.30f,1.93f,0,s), V(.09f,.58f,.14f,s), new Vector3(0,0,18), hard, true);
            Part("Warden_Sigil", PrimitiveType.Cube, p, V(0,.92f,.31f,s), V(.38f,.38f,.035f,s), new Vector3(0,0,45), signal, false);
        }

        private static void BuildNullSentry(Transform p, float s, Material body, Material signal)
        {
            Part("Sentry_Diamond", PrimitiveType.Cube, p, V(0,.82f,0,s), V(.56f,.78f,.42f,s), new Vector3(0,0,45), body, true);
            Part("Sentry_FinL", PrimitiveType.Cube, p, V(-.44f,.82f,-.02f,s), V(.10f,.80f,.22f,s), new Vector3(0,-10,28), body, true);
            Part("Sentry_FinR", PrimitiveType.Cube, p, V(.44f,.82f,-.02f,s), V(.10f,.80f,.22f,s), new Vector3(0,10,-28), body, true);
            Part("Sentry_Gun", PrimitiveType.Cube, p, V(0,.78f,.48f,s), V(.18f,.18f,.66f,s), Vector3.zero, body, true);
            Part("Sentry_Tail", PrimitiveType.Cube, p, V(0,.28f,-.20f,s), V(.08f,.54f,.16f,s), new Vector3(10,0,0), body, true);
            Part("Sentry_Visor", PrimitiveType.Cube, p, V(0,1.05f,.34f,s), V(.42f,.05f,.03f,s), Vector3.zero, signal, false);
        }

        private static void BuildChromePenitent(Transform p, float s, Material body, Material signal, Material metal)
        {
            Material hard = metal != null ? metal : body;
            Part("Penitent_Torso", PrimitiveType.Cube, p, V(-.06f,.76f,0,s), V(.78f,.90f,.52f,s), new Vector3(-5,0,0), hard, true);
            Part("Penitent_Helm", PrimitiveType.Cube, p, V(-.05f,1.30f,.02f,s), V(.54f,.38f,.48f,s), Vector3.zero, body, true);
            Part("Penitent_Shoulder", PrimitiveType.Cube, p, V(-.52f,.98f,0,s), V(.36f,.28f,.60f,s), new Vector3(0,0,-10), hard, true);
            Part("Penitent_Cleaver", PrimitiveType.Cube, p, V(.66f,.55f,.18f,s), V(.23f,.90f,.34f,s), new Vector3(-20,0,-14), hard, true);
            Part("Penitent_BackBlade", PrimitiveType.Cube, p, V(-.02f,.68f,-.38f,s), V(.58f,.66f,.12f,s), Vector3.zero, body, true);
            Part("Penitent_Visor", PrimitiveType.Cube, p, V(-.05f,1.30f,.285f,s), V(.36f,.06f,.03f,s), Vector3.zero, signal, false);
        }

        private static void BuildRiftStalker(Transform p, float s, Material body, Material signal, Material accent)
        {
            Part("Stalker_Spine", PrimitiveType.Cube, p, V(0,.52f,0,s), V(.42f,.26f,1.08f,s), new Vector3(8,0,0), body, true);
            Part("Stalker_Head", PrimitiveType.Cube, p, V(0,.62f,.62f,s), V(.34f,.24f,.36f,s), new Vector3(-24,0,0), body, true);
            for (int side=-1; side<=1; side+=2)
            {
                Part($"Stalker_Fore_{side}", PrimitiveType.Cube, p, V(.38f*side,.28f,.30f,s), V(.09f,.16f,.72f,s), new Vector3(0,side*22,side*34), accent, true);
                Part($"Stalker_Hind_{side}", PrimitiveType.Cube, p, V(.42f*side,.24f,-.34f,s), V(.09f,.14f,.68f,s), new Vector3(0,side*-18,side*28), body, true);
            }
            Part("Stalker_MandibleL", PrimitiveType.Cube, p, V(-.16f,.52f,.86f,s), V(.07f,.08f,.42f,s), new Vector3(0,-12,-22), accent, false);
            Part("Stalker_MandibleR", PrimitiveType.Cube, p, V(.16f,.52f,.86f,s), V(.07f,.08f,.42f,s), new Vector3(0,12,22), accent, false);
            Part("Stalker_Eye", PrimitiveType.Cube, p, V(0,.68f,.82f,s), V(.20f,.04f,.025f,s), Vector3.zero, signal, false);
        }

        private static void BuildChoirDrone(Transform p, float s, Material body, Material signal, Material accent)
        {
            Part("Choir_CoreCage", PrimitiveType.Sphere, p, V(0,.90f,0,s), Vector3.one*.46f*s, Vector3.zero, body, true);
            Part("Choir_ForkL", PrimitiveType.Cube, p, V(-.38f,1.18f,0,s), V(.09f,.82f,.12f,s), new Vector3(0,-10,12), accent, false);
            Part("Choir_ForkR", PrimitiveType.Cube, p, V(.38f,1.18f,0,s), V(.09f,.82f,.12f,s), new Vector3(0,10,-12), accent, false);
            Part("Choir_HaloA", PrimitiveType.Cube, p, V(0,.90f,0,s), V(1.10f,.06f,.10f,s), new Vector3(0,28,0), accent, false);
            Part("Choir_HaloB", PrimitiveType.Cube, p, V(0,.90f,0,s), V(1.10f,.06f,.10f,s), new Vector3(0,-28,0), accent, false);
            Part("Choir_Downstem", PrimitiveType.Cube, p, V(0,.35f,0,s), V(.08f,.62f,.08f,s), Vector3.zero, body, true);
            Part("Choir_Mouth", PrimitiveType.Cube, p, V(0,.90f,.43f,s), V(.28f,.055f,.025f,s), Vector3.zero, signal, false);
        }

        private static void BuildPrismMaw(Transform p, float s, Material body, Material signal, Material accent)
        {
            Part("Maw_Carapace", PrimitiveType.Cube, p, V(0,.62f,0,s), V(.76f,.58f,.72f,s), new Vector3(0,45,0), body, true);
            Part("Maw_JawTop", PrimitiveType.Cube, p, V(0,.78f,.48f,s), V(.54f,.12f,.58f,s), new Vector3(-22,0,0), accent, true);
            Part("Maw_JawBottom", PrimitiveType.Cube, p, V(0,.44f,.48f,s), V(.54f,.12f,.58f,s), new Vector3(22,0,0), accent, true);
            Part("Maw_JawL", PrimitiveType.Cube, p, V(-.38f,.60f,.44f,s), V(.10f,.48f,.50f,s), new Vector3(0,-18,12), body, true);
            Part("Maw_JawR", PrimitiveType.Cube, p, V(.38f,.60f,.44f,s), V(.10f,.48f,.50f,s), new Vector3(0,18,-12), body, true);
            Part("Maw_PrismEye", PrimitiveType.Sphere, p, V(0,.65f,.31f,s), Vector3.one*.18f*s, Vector3.zero, signal, false);
            Part("Maw_BackSpine", PrimitiveType.Cube, p, V(0,.86f,-.36f,s), V(.10f,.66f,.16f,s), new Vector3(24,0,0), accent, false);
        }

        private static void BuildVeilReaper(Transform p, float s, Material body, Material signal, Material accent)
        {
            Part("Reaper_Stem", PrimitiveType.Cube, p, V(0,.92f,0,s), V(.26f,1.38f,.30f,s), new Vector3(0,0,4), body, true);
            Part("Reaper_Hood", PrimitiveType.Cube, p, V(0,1.55f,.06f,s), V(.58f,.42f,.52f,s), new Vector3(-10,0,0), body, true);
            Part("Reaper_ScytheL", PrimitiveType.Cube, p, V(-.52f,1.02f,.16f,s), V(.10f,1.20f,.18f,s), new Vector3(-28,-8,-22), accent, true);
            Part("Reaper_ScytheR", PrimitiveType.Cube, p, V(.52f,1.02f,.16f,s), V(.10f,1.20f,.18f,s), new Vector3(-28,8,22), accent, true);
            Part("Reaper_LegL", PrimitiveType.Cube, p, V(-.22f,.24f,-.04f,s), V(.11f,.72f,.14f,s), new Vector3(8,0,-12), body, true);
            Part("Reaper_LegR", PrimitiveType.Cube, p, V(.22f,.24f,-.04f,s), V(.11f,.72f,.14f,s), new Vector3(8,0,12), body, true);
            Part("Reaper_FaceSlit", PrimitiveType.Cube, p, V(0,1.55f,.35f,s), V(.24f,.045f,.025f,s), Vector3.zero, signal, false);
        }

        private static void BuildOrbitSeraph(Transform p, float s, Material body, Material signal, Material accent)
        {
            Part("Seraph_CoreShell", PrimitiveType.Sphere, p, V(0,.92f,0,s), Vector3.one*.42f*s, Vector3.zero, body, true);
            Part("Seraph_HaloH", PrimitiveType.Cube, p, V(0,1.20f,0,s), V(1.28f,.07f,.12f,s), new Vector3(0,22,0), accent, false);
            Part("Seraph_HaloV", PrimitiveType.Cube, p, V(0,1.20f,0,s), V(.12f,.07f,1.28f,s), new Vector3(0,22,0), accent, false);
            for (int i=0; i<4; i++)
            {
                float a=i*Mathf.PI*.5f;
                Vector3 q=new Vector3(Mathf.Cos(a)*.72f,.88f,Mathf.Sin(a)*.72f)*s;
                Part($"Seraph_OrbitBlade_{i}", PrimitiveType.Cube, p, q, V(.08f,.58f,.16f,s), new Vector3(0,-a*Mathf.Rad2Deg,34), body, true);
            }
            Part("Seraph_Eye", PrimitiveType.Sphere, p, V(0,.92f,.36f,s), Vector3.one*.18f*s, Vector3.zero, signal, false);
            Part("Seraph_DownRune", PrimitiveType.Cube, p, V(0,.36f,0,s), V(.04f,.52f,.04f,s), Vector3.zero, signal, false);
        }

        private static Vector3 V(float x,float y,float z,float s) => new Vector3(x,y,z)*s;
        private static float EstimateScale(Transform core) => core == null ? 1f : Mathf.Clamp(core.localScale.x/.30f,.50f,1.80f);

        private static GameObject Part(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Vector3 euler, Material material, bool shadows)
        {
            GameObject go=GameObject.CreatePrimitive(type);
            go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=pos;
            go.transform.localRotation=Quaternion.Euler(euler); go.transform.localScale=scale;
            Collider c=go.GetComponent<Collider>(); if(c!=null) UnityEngine.Object.DestroyImmediate(c);
            Renderer r=go.GetComponent<Renderer>(); if(r!=null){r.sharedMaterial=material;r.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;r.receiveShadows=shadows;}
            return go;
        }

        private static Material Require(string name)
        {
            Material material=CinematicMaterialAuthoring.Load(name);
            if(material==null) throw new InvalidOperationException($"Missing material '{name}'.");
            return material;
        }

        private static void DestroyChild(Transform parent,string name)
        {
            Transform child=parent!=null?parent.Find(name):null;
            if(child!=null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }
}
#endif
