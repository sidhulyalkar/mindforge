using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only binder that upgrades the procedural vertical slice to the
    /// editor-authored PBR material library after runtime bootstrap has created the
    /// character, boss and physical armament visuals. It never touches colliders,
    /// combat state, damage, input or neural authority.
    /// </summary>
    public sealed class CinematicRuntimeMaterialOverride : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // Runtime avatar + physical arsenal bootstraps execute after scene load.
            // Allow them to build their render-only geometry before replacing materials.
            for (int frame = 0; frame < 12; frame++) yield return null;
            Apply();
        }

        public void Apply()
        {
            Material armor = Resources.Load<Material>("Cinematic/GuardianArmor");
            Material cloth = Resources.Load<Material>("Cinematic/GuardianCloth");
            Material aether = Resources.Load<Material>("Cinematic/GuardianAether");
            Material shard = Resources.Load<Material>("Cinematic/FracturedShard");
            Material core = Resources.Load<Material>("Cinematic/FracturedCore");
            Material ring = Resources.Load<Material>("Cinematic/FracturedRing");
            Material sight = Resources.Load<Material>("Cinematic/AetherCyan");
            Material guard = Resources.Load<Material>("Cinematic/WispVerdant");

            Renderer[] renderers = FindObjectsOfType<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                string name = renderer.gameObject.name;
                Material selected = null;

                if (name == "Pelvis" || name == "Torso" || name == "Head" || name == "CrownFin" ||
                    name == "LeftPauldron" || name == "RightPauldron" || name == "Upper" || name == "Greave")
                    selected = armor;
                else if (name == "Neck" || name == "Mantle" || name == "Boot")
                    selected = cloth;
                else if (name == "ChestAether" || name == "Visor" || name == "MantleMark" || name == "Gauntlet")
                    selected = aether;
                else if (name == "FracturedHeart")
                    selected = core;
                else if (name.StartsWith("OrbitShard_"))
                    selected = shard;
                else if (name == "Aetherblade" || name == "AetherbladeHilt")
                    selected = sight;
                else if (name == "VerdantWard")
                    selected = guard;

                if (selected != null) renderer.sharedMaterial = selected;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            }

            foreach (LineRenderer line in FindObjectsOfType<LineRenderer>(true))
            {
                if (line.gameObject.name.StartsWith("FractureRing_") && ring != null)
                    line.sharedMaterial = ring;
            }

            Debug.Log("[Mindforge:Cinematic] Runtime presentation rebound to generated PBR surface library.");
        }
    }
}
