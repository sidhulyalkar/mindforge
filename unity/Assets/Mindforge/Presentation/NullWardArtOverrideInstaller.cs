using System.Collections;
using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Binds optional production room art onto editor-authored Null Ward presentation
    /// anchors. Imported art is aggressively presentation-only: physics and every
    /// Mindforge runtime MonoBehaviour are removed from the instantiated hierarchy.
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
                GameObject root = GameObject.Find(WardRootName);
                if (root != null) ward = root.transform;
                if (ward == null) yield return null;
            }
            if (ward == null)
            {
                Debug.LogWarning("[Mindforge:NullWardArt] Art profile found but Null Ward root is absent.");
                yield break;
            }

            bool anyBound = false;
            anyBound |= BindZone(ward, "NullWard_ArtAnchor_MemoryForge", "AuthoredMemoryForge", _profile.memoryForge);
            anyBound |= BindZone(ward, "NullWard_ArtAnchor_Causeway", "AuthoredSynapseCauseway", _profile.synapseCauseway);
            anyBound |= BindZone(ward, "NullWard_ArtAnchor_Market", "AuthoredNullMarket", _profile.nullMarket);
            anyBound |= BindZone(ward, "NullWard_ArtAnchor_Maintenance", "AuthoredMaintenanceLoop", _profile.maintenanceLoop);
            anyBound |= BindZone(ward, "NullWard_ArtAnchor_Cathedral", "AuthoredSignalCathedral", _profile.signalCathedral);

            if (anyBound && _profile.hideProceduralDetailWhenAnyZoneIsBound)
            {
                Transform detail = ward.Find(DetailRootName);
                if (detail != null) detail.gameObject.SetActive(false);
            }
        }

        private static bool BindZone(
            Transform ward,
            string anchorName,
            string instanceName,
            NullWardArtProfile.ZoneBinding binding)
        {
            if (ward == null || binding == null || binding.visualPrefab == null) return false;
            Transform anchor = ward.Find(anchorName);
            if (anchor == null)
            {
                Debug.LogWarning($"[Mindforge:NullWardArt] Missing presentation anchor {anchorName}; rebuild the cinematic Showcase.");
                return false;
            }
            if (anchor.Find(instanceName) != null) return true;

            GameObject visual = Instantiate(binding.visualPrefab, anchor);
            visual.name = instanceName;
            visual.transform.localPosition = binding.localPosition;
            visual.transform.localRotation = Quaternion.Euler(binding.localEuler);
            visual.transform.localScale = binding.localScale == Vector3.zero ? Vector3.one : binding.localScale;
            StripAuthority(visual);
            return true;
        }

        private static void StripAuthority(GameObject visualRoot)
        {
            foreach (Rigidbody body in visualRoot.GetComponentsInChildren<Rigidbody>(true))
                Destroy(body);
            foreach (Collider collider in visualRoot.GetComponentsInChildren<Collider>(true))
                Destroy(collider);
            foreach (Joint joint in visualRoot.GetComponentsInChildren<Joint>(true))
                Destroy(joint);

            // Art prefabs are a rendering payload. Any script from the Mindforge runtime
            // namespace is authority or game-specific presentation that must be bound by
            // the host scene rather than imported accidentally inside room art.
            foreach (MonoBehaviour behaviour in visualRoot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                string ns = behaviour.GetType().Namespace ?? string.Empty;
                if (ns == "Mindforge" || ns.StartsWith("Mindforge."))
                    Destroy(behaviour);
            }
        }
    }
}
