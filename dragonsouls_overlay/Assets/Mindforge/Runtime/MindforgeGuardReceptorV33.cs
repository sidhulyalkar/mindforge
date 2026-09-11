using States;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Chassis
{
    /// <summary>
    /// First production Guard receptor. A validated Guard intent creates a temporary
    /// collider-free stabilization field around the player. V0.33 deliberately does
    /// not grant invulnerability or rewrite incoming damage; later hazards may query
    /// GuardActive as a semantic condition.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeGuardReceptorV33 : MonoBehaviour
    {
        [SerializeField] private float stabilizationSeconds = 5f;
        [SerializeField] private Color guardColor = new Color(0.92f, 0.76f, 0.34f, 1f);
        [SerializeField, Range(4, 12)] private int orbitNodeCount = 8;
        [SerializeField] private float orbitRadius = 0.85f;

        private Transform _player;
        private GameObject _fieldRoot;
        private Material _material;
        private float _until;

        public int ActivationCount { get; private set; }
        public bool GuardActive => _fieldRoot != null && Time.unscaledTime <= _until;
        public float GuardRemainingSeconds => GuardActive ? Mathf.Max(0f, _until - Time.unscaledTime) : 0f;

        private void Start()
        {
            PlayerStateMachine player = FindObjectOfType<PlayerStateMachine>(true);
            _player = player != null ? player.transform : null;
            MindforgeIntentBusV29.IntentPublished += HandleIntent;
        }

        private void OnDestroy()
        {
            MindforgeIntentBusV29.IntentPublished -= HandleIntent;
            if (_fieldRoot != null) Destroy(_fieldRoot);
            if (_material != null) Destroy(_material);
        }

        private void Update()
        {
            if (_fieldRoot == null) return;
            if (Time.unscaledTime > _until)
            {
                Destroy(_fieldRoot);
                _fieldRoot = null;
                return;
            }

            _fieldRoot.transform.Rotate(Vector3.up, 55f * Time.unscaledDeltaTime, Space.Self);
            float breathe = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f);
            _fieldRoot.transform.localScale = Vector3.one * breathe;
        }

        private void HandleIntent(MindforgeIntentEventV29 evt)
        {
            if (evt.Intent != MindforgeIntentV29.Guard) return;
            EnsurePlayer();
            if (_player == null) return;

            if (_fieldRoot != null) Destroy(_fieldRoot);
            if (_material == null) _material = CreateMaterial();

            _fieldRoot = new GameObject("Mindforge_Guard_Field_V33");
            _fieldRoot.transform.SetParent(_player, false);
            _fieldRoot.transform.localPosition = Vector3.up * 1.0f;

            int count = Mathf.Clamp(orbitNodeCount, 4, 12);
            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i) / count;
                Vector3 local = new Vector3(Mathf.Cos(angle), 0.10f * Mathf.Sin(angle * 2f), Mathf.Sin(angle)) * orbitRadius;
                GameObject node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                node.name = $"GuardNode_{i:00}";
                node.transform.SetParent(_fieldRoot.transform, false);
                node.transform.localPosition = local;
                node.transform.localScale = Vector3.one * 0.10f;
                Collider collider = node.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                Renderer renderer = node.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = _material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }

            _until = Time.unscaledTime + stabilizationSeconds;
            ActivationCount++;
        }

        private void EnsurePlayer()
        {
            if (_player != null) return;
            PlayerStateMachine player = FindObjectOfType<PlayerStateMachine>(true);
            _player = player != null ? player.transform : null;
        }

        private Material CreateMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { name = "MF_V33_GuardField" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", guardColor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", guardColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", guardColor * 2.5f);
            }
            return material;
        }
    }
}
