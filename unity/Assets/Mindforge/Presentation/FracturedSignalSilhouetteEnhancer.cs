using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Additive render-only silhouette for the Fractured Signal. It creates an
    /// asymmetric crown of fracture spires around the existing avatar without adding
    /// colliders or changing boss attack authority.
    /// </summary>
    public sealed class FracturedSignalSilhouetteEnhancer : MonoBehaviour
    {
        [SerializeField] private FracturedSignalDirector director;
        private Transform _root;
        private Transform[] _spires;
        private Transform[] _satellites;
        private Material _spineMaterial;
        private Material _satelliteMaterial;
        private int _phase = 1;
        private float _charge;
        private float _release;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            FracturedSignalDirector boss = UnityEngine.Object.FindObjectOfType<FracturedSignalDirector>(true);
            if (boss == null || boss.GetComponent<FracturedSignalSilhouetteEnhancer>() != null) return;
            boss.gameObject.AddComponent<FracturedSignalSilhouetteEnhancer>();
        }

        private void Awake()
        {
            if (director == null) director = GetComponent<FracturedSignalDirector>();
            Build();
        }

        private void OnEnable()
        {
            if (director == null) director = GetComponent<FracturedSignalDirector>();
            if (director != null)
            {
                _phase = director.Phase;
                director.PhaseChanged += OnPhaseChanged;
                director.AttackTelegraphed += OnTelegraph;
                director.AttackFired += OnFired;
            }
        }

        private void OnDisable()
        {
            if (director == null) return;
            director.PhaseChanged -= OnPhaseChanged;
            director.AttackTelegraphed -= OnTelegraph;
            director.AttackFired -= OnFired;
        }

        private void Build()
        {
            Transform existing = transform.Find("FracturedSignalThreatSilhouette");
            if (existing != null)
            {
                _root = existing;
                return;
            }

            _spineMaterial = Material("FracturedThreatSpine", new Color(0.035f, 0.012f, 0.050f), 0.90f, 0.48f, new Color(1f, 0.08f, 0.30f) * 0.85f);
            _satelliteMaterial = Material("FracturedThreatSatellite", new Color(0.05f, 0.015f, 0.09f), 0.82f, 0.62f, new Color(0.54f, 0.10f, 1f) * 1.35f);
            _root = new GameObject("FracturedSignalThreatSilhouette").transform;
            _root.SetParent(transform, false);
            _root.localPosition = new Vector3(0f, 0.18f, 0f);

            _spires = new Transform[7];
            for (int i = 0; i < _spires.Length; i++)
            {
                float a = i / (float)_spires.Length * Mathf.PI * 2f + 0.23f;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"ThreatSpire_{i:00}";
                go.transform.SetParent(_root, false);
                go.transform.localPosition = radial * (1.08f + (i % 3) * 0.09f) + Vector3.up * (-0.02f + (i % 4) * 0.13f);
                go.transform.localScale = new Vector3(0.14f + (i % 2) * 0.05f, 0.20f, 1.28f + (i % 3) * 0.24f);
                go.transform.localRotation = Quaternion.LookRotation(radial) * Quaternion.Euler(i % 2 == 0 ? -18f : 14f, 0f, i * 13f);
                DisableCollider(go);
                Renderer r = go.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = _spineMaterial;
                _spires[i] = go.transform;
            }

            _satellites = new Transform[8];
            for (int i = 0; i < _satellites.Length; i++)
            {
                float a = i / (float)_satellites.Length * Mathf.PI * 2f;
                GameObject go = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube);
                go.name = $"ThreatSatellite_{i:00}";
                go.transform.SetParent(_root, false);
                go.transform.localPosition = new Vector3(Mathf.Cos(a) * 2.28f, Mathf.Sin(a * 2f) * 0.48f, Mathf.Sin(a) * 2.28f);
                go.transform.localScale = i % 3 == 0 ? Vector3.one * 0.20f : new Vector3(0.12f, 0.38f + (i % 2) * 0.18f, 0.16f);
                go.transform.localRotation = Quaternion.Euler(i * 29f, i * 17f, i * 41f);
                DisableCollider(go);
                Renderer r = go.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = _satelliteMaterial;
                _satellites[i] = go.transform;
            }
        }

        private void LateUpdate()
        {
            if (_root == null || _spires == null || _satellites == null) return;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            float time = Time.unscaledTime;
            _charge = Mathf.MoveTowards(_charge, 0f, dt * 1.6f);
            _release = Mathf.MoveTowards(_release, 0f, dt * 4.5f);
            float phase = _phase == 1 ? 0.25f : _phase == 2 ? 0.62f : 1f;

            for (int i = 0; i < _spires.Length; i++)
            {
                Transform s = _spires[i];
                if (s == null) continue;
                float a = i / (float)_spires.Length * Mathf.PI * 2f + 0.23f + Mathf.Sin(time * 0.28f + i) * 0.05f;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float radius = 1.06f + phase * 0.30f + _charge * 0.42f;
                s.localPosition = radial * radius + Vector3.up * (-0.02f + (i % 4) * 0.13f);
                s.localRotation = Quaternion.LookRotation(radial) * Quaternion.Euler(i % 2 == 0 ? -18f : 14f, 0f, Mathf.Sin(time + i) * 15f);
                Vector3 scale = s.localScale;
                scale.z = (1.28f + (i % 3) * 0.24f) * (1f + phase * 0.16f + _charge * 0.26f + _release * 0.10f);
                s.localScale = scale;
            }

            for (int i = 0; i < _satellites.Length; i++)
            {
                Transform satellite = _satellites[i];
                if (satellite == null) continue;
                float a = i / (float)_satellites.Length * Mathf.PI * 2f + time * (0.18f + phase * 0.13f);
                float radius = 2.12f + phase * 0.34f + Mathf.Sin(time * 1.3f + i) * 0.12f + _charge * 0.30f;
                satellite.localPosition = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a * 2f + i) * (0.44f + phase * 0.18f), Mathf.Sin(a) * radius);
                satellite.Rotate(new Vector3(21f, 34f, 17f) * dt * (1f + phase), Space.Self);
            }
        }

        private void OnPhaseChanged(int phase) { _phase = Mathf.Clamp(phase, 1, 3); _release = 1f; }
        private void OnTelegraph(string pattern, int count, bool heavy) => _charge = heavy ? 1f : 0.72f;
        private void OnFired(string pattern, int count, bool heavy) => _release = heavy ? 1f : 0.70f;

        private static void DisableCollider(GameObject go)
        {
            Collider c = go != null ? go.GetComponent<Collider>() : null;
            if (c != null) c.enabled = false;
        }

        private static Material Material(string name, Color color, float metallic, float smoothness, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            return material;
        }
    }
}
