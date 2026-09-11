using States;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Chassis
{
    /// <summary>
    /// First production Sight receptor. A validated Sight intent reveals the nearest
    /// encounter actor with a temporary collider-free resonance marker. This changes
    /// information available to the player, not enemy health, AI, navigation or damage.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeSightReceptorV33 : MonoBehaviour
    {
        [SerializeField] private float revealRange = 48f;
        [SerializeField] private float revealSeconds = 6f;
        [SerializeField] private Color revealColor = new Color(0.18f, 0.92f, 1.0f, 1f);

        private Transform _player;
        private GameObject _marker;
        private Material _material;
        private float _until;

        public int ActivationCount { get; private set; }
        public Transform LastRevealedTarget { get; private set; }
        public bool RevealActive => _marker != null && Time.unscaledTime <= _until;

        private void Start()
        {
            PlayerStateMachine player = FindObjectOfType<PlayerStateMachine>(true);
            _player = player != null ? player.transform : null;
            MindforgeIntentBusV29.IntentPublished += HandleIntent;
        }

        private void OnDestroy()
        {
            MindforgeIntentBusV29.IntentPublished -= HandleIntent;
            if (_marker != null) Destroy(_marker);
            if (_material != null) Destroy(_material);
        }

        private void Update()
        {
            if (_marker == null) return;
            if (Time.unscaledTime > _until)
            {
                Destroy(_marker);
                _marker = null;
                LastRevealedTarget = null;
                return;
            }

            float pulse = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 3f);
            _marker.transform.localScale = Vector3.one * 0.22f * pulse;
            _marker.transform.Rotate(Vector3.up, 70f * Time.unscaledDeltaTime, Space.Self);
        }

        private void HandleIntent(MindforgeIntentEventV29 evt)
        {
            if (evt.Intent != MindforgeIntentV29.Sight) return;
            Transform target = FindNearestEncounterTarget();
            if (target == null)
            {
                Debug.Log("[Mindforge:V33] Sight accepted but no encounter target is within reveal range.");
                return;
            }

            if (_marker != null) Destroy(_marker);
            if (_material == null) _material = CreateMaterial();

            _marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _marker.name = "Mindforge_Sight_Reveal_V33";
            _marker.transform.SetParent(target, false);
            _marker.transform.localPosition = Vector3.up * 2.2f;
            _marker.transform.localScale = Vector3.one * 0.22f;
            Collider collider = _marker.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Renderer renderer = _marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            LastRevealedTarget = target;
            _until = Time.unscaledTime + revealSeconds;
            ActivationCount++;
        }

        private Transform FindNearestEncounterTarget()
        {
            if (_player == null)
            {
                PlayerStateMachine player = FindObjectOfType<PlayerStateMachine>(true);
                _player = player != null ? player.transform : null;
            }
            if (_player == null) return null;

            EnemyStateMachine[] enemies = FindObjectsOfType<EnemyStateMachine>(true);
            Transform best = null;
            float bestSq = revealRange * revealRange;
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyStateMachine enemy = enemies[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                Vector3 delta = enemy.transform.position - _player.position;
                float sq = delta.sqrMagnitude;
                if (sq >= bestSq) continue;
                bestSq = sq;
                best = enemy.transform;
            }
            return best;
        }

        private Material CreateMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { name = "MF_V33_SightReveal" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", revealColor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", revealColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", revealColor * 3.0f);
            }
            return material;
        }
    }
}
