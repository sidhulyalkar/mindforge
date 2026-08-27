using System.Collections;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Binds optional production art to the existing authoritative transforms. Prefabs
    /// supplied through CinematicArtProfile are presentation children only: they never
    /// replace Guardian/Boss Rigidbody, collider, vitals, input, attack or neural logic.
    /// </summary>
    public sealed class CinematicArtOverrideInstaller : MonoBehaviour
    {
        private CinematicArtProfile _profile;

        private IEnumerator Start()
        {
            _profile = Resources.Load<CinematicArtProfile>("Cinematic/MindforgeArtProfile");
            if (_profile == null) yield break;

            GuardianCombatInput guardian = null;
            FracturedSignalDirector boss = null;
            for (int frame = 0; frame < 60; frame++)
            {
                if (guardian == null) guardian = FindObjectOfType<GuardianCombatInput>(true);
                if (boss == null) boss = FindObjectOfType<FracturedSignalDirector>(true);
                if (guardian != null && boss != null) break;
                yield return null;
            }

            if (guardian != null && _profile.guardianVisualPrefab != null)
                BindGuardian(guardian.transform);
            if (boss != null && _profile.fracturedSignalVisualPrefab != null)
                BindBoss(boss.transform);
            if (_profile.arenaSetDressPrefab != null)
                BindArenaSet();
        }

        private void BindGuardian(Transform authority)
        {
            if (_profile.hideProceduralGuardianWhenBound)
            {
                Transform procedural = authority.Find("GuardianShowcaseAvatar");
                if (procedural != null) procedural.gameObject.SetActive(false);
            }

            GameObject visual = Instantiate(_profile.guardianVisualPrefab, authority);
            visual.name = "GuardianAuthoredVisual";
            ApplyTransform(visual.transform, _profile.guardianLocalPosition, _profile.guardianLocalEuler, _profile.guardianLocalScale);
            RemoveAuthorityComponents(visual);
        }

        private void BindBoss(Transform authority)
        {
            if (_profile.hideProceduralBossWhenBound)
            {
                Transform procedural = authority.Find("FracturedSignalShowcaseAvatar");
                if (procedural != null) procedural.gameObject.SetActive(false);
            }

            GameObject visual = Instantiate(_profile.fracturedSignalVisualPrefab, authority);
            visual.name = "FracturedSignalAuthoredVisual";
            ApplyTransform(visual.transform, _profile.bossLocalPosition, _profile.bossLocalEuler, _profile.bossLocalScale);
            RemoveAuthorityComponents(visual);
        }

        private void BindArenaSet()
        {
            GameObject arena = GameObject.Find("Fractured_Signal_Arena");
            if (arena == null) return;
            GameObject set = Instantiate(_profile.arenaSetDressPrefab, arena.transform);
            set.name = "AuthoredArenaSetDress";
            RemoveAuthorityComponents(set);
        }

        private static void ApplyTransform(Transform target, Vector3 position, Vector3 euler, Vector3 scale)
        {
            target.localPosition = position;
            target.localRotation = Quaternion.Euler(euler);
            target.localScale = scale == Vector3.zero ? Vector3.one : scale;
        }

        private static void RemoveAuthorityComponents(GameObject visualRoot)
        {
            // Art prefabs are never allowed to smuggle a second physics/gameplay body
            // into the scene. Renderers, Animators, cloth and VFX remain untouched.
            foreach (Rigidbody body in visualRoot.GetComponentsInChildren<Rigidbody>(true))
                Destroy(body);
            foreach (Collider collider in visualRoot.GetComponentsInChildren<Collider>(true))
                Destroy(collider);
            foreach (CombatantVitals vitals in visualRoot.GetComponentsInChildren<CombatantVitals>(true))
                Destroy(vitals);
            foreach (GuardianCombatInput input in visualRoot.GetComponentsInChildren<GuardianCombatInput>(true))
                Destroy(input);
            foreach (FracturedSignalDirector director in visualRoot.GetComponentsInChildren<FracturedSignalDirector>(true))
                Destroy(director);
        }
    }
}
