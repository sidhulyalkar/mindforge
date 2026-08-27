using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Visual-only body for The Fractured Signal. The original collider/vitals and
    /// scheduler remain authoritative. Phase, telegraph and damage events only drive
    /// silhouette, light, motion and emission.
    /// </summary>
    public sealed class FracturedSignalAvatar : MonoBehaviour
    {
        [SerializeField] private FracturedSignalDirector director;
        [SerializeField] private CombatantVitals vitals;

        private Transform _root;
        private Transform _core;
        private Transform[] _shards;
        private LineRenderer[] _rings;
        private Light _coreLight;
        private Material _coreMaterial;
        private Material _shellMaterial;
        private Material _ringMaterial;
        private MaterialPropertyBlock _block;
        private float _charge;
        private float _firePulse;
        private float _damageFlash;
        private int _phase = 1;
        private string _pattern = "";

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            if (director == null) director = GetComponent<FracturedSignalDirector>();
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
            BuildVisuals();
        }

        private void OnEnable()
        {
            if (director != null)
            {
                director.PhaseChanged += OnPhaseChanged;
                director.AttackTelegraphed += OnTelegraph;
                director.AttackFired += OnFired;
                _phase = director.Phase;
            }
            if (vitals != null) vitals.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.PhaseChanged -= OnPhaseChanged;
                director.AttackTelegraphed -= OnTelegraph;
                director.AttackFired -= OnFired;
            }
            if (vitals != null) vitals.Damaged -= OnDamaged;
        }

        private void BuildVisuals()
        {
            if (transform.Find("FracturedSignalShowcaseAvatar") != null) return;
            Renderer legacy = GetComponent<Renderer>();
            if (legacy != null) legacy.enabled = false;

            _coreMaterial = CreateMaterial("FracturedCore", new Color(0.18f, 0.01f, 0.05f), 0.72f, 0.80f, new Color(1f, 0.08f, 0.22f) * 3.0f);
            _shellMaterial = CreateMaterial("FracturedShard", new Color(0.08f, 0.02f, 0.12f), 0.86f, 0.66f, new Color(0.48f, 0.08f, 1f) * 1.3f);
            _ringMaterial = CreateMaterial("FracturedRing", new Color(0.18f, 0.04f, 0.24f), 0.45f, 0.74f, new Color(0.55f, 0.14f, 1f) * 2.2f);
            _block = new MaterialPropertyBlock();

            _root = Node("FracturedSignalShowcaseAvatar", transform, Vector3.zero);
            _core = Part("FracturedHeart", PrimitiveType.Sphere, _root, new Vector3(0f, 0.15f, 0f), Vector3.one * 1.18f, _coreMaterial);
            Part("CoreLens", PrimitiveType.Sphere, _core, new Vector3(0f, 0.08f, -0.48f), new Vector3(0.42f, 0.30f, 0.18f), _ringMaterial);

            _rings = new LineRenderer[3];
            for (int i = 0; i < _rings.Length; i++)
            {
                Transform ringRoot = Node($"FractureRing_{i:00}", _root, new Vector3(0f, 0.12f, 0f));
                ringRoot.localRotation = i == 0 ? Quaternion.Euler(0f, 0f, 0f) : i == 1 ? Quaternion.Euler(64f, 0f, 23f) : Quaternion.Euler(18f, 72f, 0f);
                LineRenderer ring = ringRoot.gameObject.AddComponent<LineRenderer>();
                ring.sharedMaterial = _ringMaterial;
                ring.useWorldSpace = false;
                ring.loop = true;
                ring.positionCount = 72;
                ring.widthMultiplier = 0.055f - i * 0.009f;
                float radius = 1.35f + i * 0.32f;
                for (int p = 0; p < ring.positionCount; p++)
                {
                    float a = p / (float)ring.positionCount * Mathf.PI * 2f;
                    ring.SetPosition(p, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
                }
                _rings[i] = ring;
            }

            _shards = new Transform[10];
            for (int i = 0; i < _shards.Length; i++)
            {
                float a = i / (float)_shards.Length * Mathf.PI * 2f;
                Transform shard = Part($"OrbitShard_{i:00}", PrimitiveType.Cube, _root,
                    new Vector3(Mathf.Cos(a) * 1.85f, 0.10f + Mathf.Sin(a * 2f) * 0.35f, Mathf.Sin(a) * 1.85f),
                    new Vector3(0.18f + (i % 3) * 0.04f, 0.48f + (i % 2) * 0.18f, 0.22f),
                    _shellMaterial);
                shard.localRotation = Quaternion.Euler(i * 17f, i * 31f, i * 23f);
                _shards[i] = shard;
            }

            GameObject lightGo = new GameObject("FracturedCoreLight");
            lightGo.transform.SetParent(_root, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            _coreLight = lightGo.AddComponent<Light>();
            _coreLight.type = LightType.Point;
            _coreLight.range = 7.5f;
            _coreLight.intensity = 2.2f;
            _coreLight.shadows = LightShadows.Soft;
        }

        private void LateUpdate()
        {
            if (_root == null) return;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            float time = Time.unscaledTime;
            _charge = Mathf.MoveTowards(_charge, 0f, dt * 1.55f);
            _firePulse = Mathf.MoveTowards(_firePulse, 0f, dt * 4.8f);
            _damageFlash = Mathf.MoveTowards(_damageFlash, 0f, dt * 5.4f);

            float phaseEnergy = _phase == 1 ? 0.30f : _phase == 2 ? 0.62f : 1f;
            float breathing = 1f + Mathf.Sin(time * (1.7f + phaseEnergy)) * 0.035f + _charge * 0.08f + _firePulse * 0.10f;
            _core.localScale = Vector3.one * 1.18f * breathing;

            for (int i = 0; i < _rings.Length; i++)
            {
                if (_rings[i] == null) continue;
                float direction = i % 2 == 0 ? 1f : -1f;
                _rings[i].transform.Rotate(Vector3.up, direction * (18f + i * 11f + phaseEnergy * 24f) * dt, Space.Self);
                _rings[i].widthMultiplier = (0.045f + _charge * 0.045f + _firePulse * 0.025f) * (1f - i * 0.10f);
            }

            for (int i = 0; i < _shards.Length; i++)
            {
                Transform shard = _shards[i];
                if (shard == null) continue;
                float a = i / (float)_shards.Length * Mathf.PI * 2f + time * (0.24f + phaseEnergy * 0.17f);
                float radius = 1.75f + Mathf.Sin(time * 1.4f + i) * 0.16f + _charge * 0.42f;
                shard.localPosition = new Vector3(
                    Mathf.Cos(a) * radius,
                    0.10f + Mathf.Sin(a * 2f + i * 0.3f) * (0.32f + phaseEnergy * 0.20f),
                    Mathf.Sin(a) * radius);
                shard.Rotate(new Vector3(19f, 31f, 13f) * dt * (1f + phaseEnergy), Space.Self);
            }

            Color phaseColor = _phase == 1
                ? new Color(0.78f, 0.06f, 0.22f)
                : _phase == 2
                    ? new Color(0.72f, 0.12f, 1f)
                    : new Color(1f, 0.16f, 0.08f);
            float intensity = 1.6f + phaseEnergy * 2.1f + _charge * 3.2f + _firePulse * 4.0f;
            if (_coreLight != null)
            {
                _coreLight.color = Color.Lerp(phaseColor, Color.white, _firePulse * 0.45f);
                _coreLight.intensity = intensity;
                _coreLight.range = 6.0f + phaseEnergy * 2.3f + _charge * 1.2f;
            }
            ApplyCoreEmission(phaseColor, intensity, _damageFlash);
        }

        private void OnPhaseChanged(int phase)
        {
            _phase = Mathf.Clamp(phase, 1, 3);
            _firePulse = 1f;
        }

        private void OnTelegraph(string pattern, int count, bool heavy)
        {
            _pattern = pattern ?? "";
            _charge = heavy ? 1f : 0.72f;
        }

        private void OnFired(string pattern, int count, bool heavy)
        {
            _pattern = pattern ?? _pattern;
            _firePulse = heavy ? 1f : 0.72f;
        }

        private void OnDamaged(DamagePacket packet)
        {
            if (packet.Damage > 0f) _damageFlash = 1f;
        }

        private void ApplyCoreEmission(Color color, float intensity, float damageFlash)
        {
            Renderer renderer = _core != null ? _core.GetComponent<Renderer>() : null;
            if (renderer == null || _block == null) return;
            Color final = Color.Lerp(color, Color.white, damageFlash * 0.75f);
            renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, final * 0.35f);
            _block.SetColor(ColorProperty, final * 0.35f);
            _block.SetColor(EmissionColor, final * intensity);
            renderer.SetPropertyBlock(_block);
        }

        private static Transform Node(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static Transform Part(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return go.transform;
        }

        private static Material CreateMaterial(string name, Color color, float metallic, float smoothness, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty(BaseColor)) material.SetColor(BaseColor, color);
            else if (material.HasProperty(ColorProperty)) material.SetColor(ColorProperty, color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty(EmissionColor))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor(EmissionColor, emission);
            }
            return material;
        }
    }
}
