using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Phase-readable staging layered over LordMalatractPresentationV1. The existing boss
    /// director still owns phase thresholds and every attack. This component only expands
    /// crown/mantle silhouette, blade scale and restrained halo geometry after those facts.
    /// </summary>
    [DefaultExecutionOrder(1450)]
    public sealed class LordMalatractPhaseStagingV2 : MonoBehaviour
    {
        [SerializeField] private FracturedSignalDirector director;
        [SerializeField] private float response = 5.8f;
        [SerializeField] private float telegraphPulseDecay = 2.6f;

        private Transform _presentationRoot;
        private Transform _crownL;
        private Transform _crownR;
        private Transform _blade;
        private Transform[] _mantle = System.Array.Empty<Transform>();
        private Transform _phaseHalo;
        private Vector3 _bladeBaseScale;
        private Vector3 _crownLBaseEuler;
        private Vector3 _crownRBaseEuler;
        private Vector3[] _mantleBaseScale;
        private float _telegraphPulse;
        private int _phase = 1;
        private bool _bound;

        private void Awake() => Resolve();

        private void OnEnable()
        {
            Resolve();
            if (director != null)
            {
                director.PhaseChanged += OnPhaseChanged;
                director.AttackTelegraphed += OnAttackTelegraphed;
                director.AttackFired += OnAttackFired;
                _phase = director.Phase;
            }
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.PhaseChanged -= OnPhaseChanged;
                director.AttackTelegraphed -= OnAttackTelegraphed;
                director.AttackFired -= OnAttackFired;
            }
        }

        private void LateUpdate()
        {
            Resolve();
            BindIfReady();
            if (!_bound) return;

            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            _telegraphPulse = Mathf.MoveTowards(_telegraphPulse, 0f, dt * Mathf.Max(0.1f, telegraphPulseDecay));
            float phase01 = Mathf.Clamp01((_phase - 1) * 0.5f);
            float t = 1f - Mathf.Exp(-Mathf.Max(0.1f, response) * dt);

            float crownSpread = Mathf.Lerp(0f, 17f, phase01) + _telegraphPulse * 3f;
            if (_crownL != null)
            {
                Vector3 e = _crownLBaseEuler;
                e.z -= crownSpread;
                _crownL.localRotation = Quaternion.Slerp(_crownL.localRotation, Quaternion.Euler(e), t);
            }
            if (_crownR != null)
            {
                Vector3 e = _crownRBaseEuler;
                e.z += crownSpread;
                _crownR.localRotation = Quaternion.Slerp(_crownR.localRotation, Quaternion.Euler(e), t);
            }

            if (_blade != null)
            {
                Vector3 scale = _bladeBaseScale;
                scale.z *= 1f + phase01 * 0.24f + _telegraphPulse * 0.08f;
                scale.x *= 1f + phase01 * 0.10f;
                _blade.localScale = Vector3.Lerp(_blade.localScale, scale, t);
            }

            for (int i = 0; i < _mantle.Length; i++)
            {
                Transform cable = _mantle[i];
                if (cable == null || i >= _mantleBaseScale.Length) continue;
                Vector3 scale = _mantleBaseScale[i];
                scale.y *= 1f + phase01 * (0.10f + (i % 3) * 0.035f);
                cable.localScale = Vector3.Lerp(cable.localScale, scale, t);
            }

            if (_phaseHalo != null)
            {
                float haloScale = _phase <= 1 ? 0.001f : 1f + phase01 * 0.36f + _telegraphPulse * 0.10f;
                _phaseHalo.localScale = Vector3.Lerp(_phaseHalo.localScale, Vector3.one * haloScale, t);
                _phaseHalo.localRotation *= Quaternion.Euler(0f, (7f + _phase * 3f) * dt, 0f);
            }
        }

        private void Resolve()
        {
            if (director == null) director = GetComponent<FracturedSignalDirector>();
        }

        private void BindIfReady()
        {
            if (_bound) return;
            _presentationRoot = transform.Find(LordMalatractPresentationV1.RootName);
            if (_presentationRoot == null) return;

            _crownL = FindDeep(_presentationRoot, "MalatractCrownL");
            _crownR = FindDeep(_presentationRoot, "MalatractCrownR");
            _blade = FindDeep(_presentationRoot, "OrderedRuinBlade");
            if (_blade != null) _bladeBaseScale = _blade.localScale;
            if (_crownL != null) _crownLBaseEuler = _crownL.localEulerAngles;
            if (_crownR != null) _crownRBaseEuler = _crownR.localEulerAngles;

            System.Collections.Generic.List<Transform> mantle = new System.Collections.Generic.List<Transform>();
            Transform[] all = _presentationRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].name.StartsWith("MantleCable_", System.StringComparison.Ordinal)) mantle.Add(all[i]);
            _mantle = mantle.ToArray();
            _mantleBaseScale = new Vector3[_mantle.Length];
            for (int i = 0; i < _mantle.Length; i++) _mantleBaseScale[i] = _mantle[i].localScale;

            BuildHalo();
            _bound = true;
        }

        private void BuildHalo()
        {
            Transform existing = _presentationRoot.Find("MalatractPhaseHaloV2");
            if (existing != null)
            {
                _phaseHalo = existing;
                return;
            }

            Renderer source = FindDeep(_presentationRoot, "CrownSignalL")?.GetComponent<Renderer>();
            GameObject haloGo = new GameObject("MalatractPhaseHaloV2");
            haloGo.transform.SetParent(_presentationRoot, false);
            haloGo.transform.localPosition = new Vector3(0f, 2.25f, -0.28f);
            _phaseHalo = haloGo.transform;
            _phaseHalo.localScale = Vector3.one * 0.001f;

            LineRenderer line = haloGo.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 32;
            line.widthMultiplier = 0.028f;
            line.numCornerVertices = 2;
            if (source != null) line.sharedMaterial = source.sharedMaterial;
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * 1.12f, Mathf.Sin(a) * 0.28f, 0f));
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && string.Equals(all[i].name, name, System.StringComparison.Ordinal)) return all[i];
            return null;
        }

        private void OnPhaseChanged(int phase)
        {
            _phase = Mathf.Clamp(phase, 1, 3);
            _telegraphPulse = 1f;
        }

        private void OnAttackTelegraphed(string pattern, int count, bool heavy)
            => _telegraphPulse = Mathf.Max(_telegraphPulse, heavy ? 0.82f : 0.48f);

        private void OnAttackFired(string pattern, int count, bool heavy)
            => _telegraphPulse = Mathf.Max(_telegraphPulse, heavy ? 0.55f : 0.28f);
    }
}
