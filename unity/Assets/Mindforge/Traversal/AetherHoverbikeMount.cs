using UnityEngine;

namespace Mindforge.Traversal
{
    /// <summary>
    /// Parked/attached presentation object for a Prism hoverbike. The bike deliberately
    /// owns no Rigidbody locomotion and no damage authority. GuardianHoverbikeController
    /// keeps the existing Guardian Rigidbody as the sole mounted player body.
    /// </summary>
    public sealed class AetherHoverbikeMount : MonoBehaviour
    {
        [SerializeField] private Transform mountPoint;
        [SerializeField] private Transform presentationRoot;
        [SerializeField, Min(0.5f)] private float interactionRadius = 2.6f;
        [SerializeField] private Vector3 mountedLocalPosition = new Vector3(0f, -0.78f, -0.04f);
        [SerializeField] private Vector3 mountedLocalEuler = Vector3.zero;

        private Transform _parkedParent;
        private bool _occupied;

        public bool Occupied => _occupied;
        public float InteractionRadius => Mathf.Max(0.5f, interactionRadius);
        public Vector3 MountWorldPoint => mountPoint != null ? mountPoint.position : transform.position + Vector3.up * 1.25f;
        public Transform PresentationRoot => presentationRoot != null ? presentationRoot : transform;

        private void Awake()
        {
            _parkedParent = transform.parent;
            EnsurePresentationColliderFree();
        }

        public bool InRange(Vector3 worldPoint)
            => !_occupied && Vector3.SqrMagnitude(worldPoint - MountWorldPoint) <= InteractionRadius * InteractionRadius;

        public bool AttachTo(Transform rider)
        {
            if (_occupied || rider == null) return false;
            _occupied = true;
            _parkedParent = transform.parent;
            transform.SetParent(rider, false);
            transform.localPosition = mountedLocalPosition;
            transform.localRotation = Quaternion.Euler(mountedLocalEuler);
            EnsurePresentationColliderFree();
            return true;
        }

        public void DetachTo(Vector3 worldPosition, Quaternion worldRotation)
        {
            transform.SetParent(_parkedParent, true);
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            _occupied = false;
            EnsurePresentationColliderFree();
        }

        private void EnsurePresentationColliderFree()
        {
            Transform root = PresentationRoot;
            if (root == null) return;
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider != null) collider.enabled = false;
            }
        }
    }
}
