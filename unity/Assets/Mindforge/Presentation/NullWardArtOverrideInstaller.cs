using System;
using System.Collections;
using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Binds optional production room art onto editor-authored Null Ward presentation
    /// anchors. Imported art is aggressively presentation-only: physics, cameras,
    /// listeners and custom MonoBehaviours are removed from the instantiated hierarchy.
    /// The generated world/collision layer remains authoritative underneath.
    /// </summary>
    public sealed class NullWardArtOverrideInstaller : MonoBehaviour
    {
        private const string WardRootName = "Mindforge_Null_Ward_V1";
        private const string DetailRootName = "Mindforge_NullWard_StaticDetail_V2";
        private NullWardArtProfile _profile;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<NullWardArtOverrideInstaller>(true) != null) return;
            new GameObject("MindforgeNullWardArtBinding").AddComponent<NullWardArtOverrideInstaller>();
        }

        private IEnumerator Start()
        {
            _profile = Resources.Load<NullWardArtProfile>("Cinematic/NullWardArtProfile");
            if (_profile == null) yield break;

            Transform ward = null;
            for (int frame = 0; frame < 120 && ward == null; frame++)
            {
                ward = FindWardRootIncludingInactive();
                if (ward == null) yield return null;
            }
            if (ward == null)
            {
                Debug.LogWarning("[Mindforge:NullWardArt] Art profile found but Null Ward root is absent.");
                yield break;
            }

            BindZone(ward, "NullWard_ArtAnchor_MemoryForge", "AuthoredMemoryForge", "Detail_MemoryForge", _profile.memoryForge);
            BindZone(ward, "NullWard_ArtAnchor_Causeway", "AuthoredSynapseCauseway", "Detail_Causeway", _profile.synapseCauseway);
            BindZone(ward, "NullWard_ArtAnchor_Market", "AuthoredNullMarket", "Detail_Market", _profile.nullMarket);
            BindZone(ward, "NullWard_ArtAnchor_Maintenance", "AuthoredMaintenanceLoop", "Detail_Maintenance", _profile.maintenanceLoop);
            BindZone(ward, "NullWard_ArtAnchor_Cathedral", "AuthoredSignalCathedral", "Detail_Cathedral", _profile.signalCathedral);
        }

        private bool BindZone(
            Transform ward,
            string anchorName,
            string instanceName,
            string proceduralDetailName,
            NullWardArtProfile.ZoneBinding binding)
        {
            if (ward == null || binding == null || binding.visualPrefab == null) return false;
            Transform anchor = ward.Find(anchorName);
            if (anchor == null)
            {
                Debug.LogWarning($"[Mindforge:NullWardArt] Missing presentation anchor {anchorName}; rebuild the cinematic Showcase.");
                return false;
            }

            if (anchor.Find(instanceName) == null)
            {
                GameObject visual = Instantiate(binding.visualPrefab, anchor);
                visual.name = instanceName;
                visual.transform.localPosition = binding.localPosition;
                visual.transform.localRotation = Quaternion.Euler(binding.localEuler);
                visual.transform.localScale = binding.localScale == Vector3.zero ? Vector3.one : binding.localScale;
                StripAuthority(visual);
            }

            if (_profile.hideProceduralDetailForBoundZones)
            {
                Transform detailRoot = ward.Find(DetailRootName);
                Transform zoneDetail = detailRoot != null ? detailRoot.Find(proceduralDetailName) : null;
                if (zoneDetail != null) zoneDetail.gameObject.SetActive(false);
            }
            return true;
        }

        private static Transform FindWardRootIncludingInactive()
        {
            Transform[] transforms = FindObjectsOfType<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null && candidate.name == WardRootName)
                    return candidate;
            }
            return null;
        }

        private static void StripAuthority(GameObject visualRoot)
        {
            foreach (Rigidbody body in visualRoot.GetComponentsInChildren<Rigidbody>(true))
                Destroy(body);
            foreach (Collider collider in visualRoot.GetComponentsInChildren<Collider>(true))
                Destroy(collider);
            foreach (Joint joint in visualRoot.GetComponentsInChildren<Joint>(true))
                Destroy(joint);

            // Physics2D is an optional Unity module in this project. Directly naming
            // Rigidbody2D / Collider2D / Joint2D creates a compile-time dependency on
            // UnityEngine.Physics2DModule, which is exactly the opposite of what an
            // optional art-import firewall should do. Inspect the inheritance chain by
            // fully-qualified type name instead so 2D physics is stripped when present
            // while a pure 3D project compiles without the Physics2D module installed.
            Component[] components = visualRoot.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component != null && IsOptionalPhysics2DComponent(component.GetType()))
                    Destroy(component);
            }

            foreach (Camera camera in visualRoot.GetComponentsInChildren<Camera>(true))
                Destroy(camera);
            foreach (AudioListener listener in visualRoot.GetComponentsInChildren<AudioListener>(true))
                Destroy(listener);

            // Room prefabs are rendering payloads. Custom scripts, even from third-party
            // art packages, are removed so imported assets cannot move transforms, spawn
            // gameplay objects or mutate global state behind Mindforge's authority layer.
            // Animator, Renderer, Light, Cloth, ParticleSystem, AudioSource and pure VFX
            // components are not MonoBehaviours and remain available for presentation.
            foreach (MonoBehaviour behaviour in visualRoot.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour != null) Destroy(behaviour);
        }

        private static bool IsOptionalPhysics2DComponent(Type type)
        {
            for (Type cursor = type; cursor != null; cursor = cursor.BaseType)
            {
                string fullName = cursor.FullName;
                if (fullName == "UnityEngine.Rigidbody2D" ||
                    fullName == "UnityEngine.Collider2D" ||
                    fullName == "UnityEngine.Joint2D")
                {
                    return true;
                }
            }
            return false;
        }
    }
}
