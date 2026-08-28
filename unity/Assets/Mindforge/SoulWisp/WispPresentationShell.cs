using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Presentation;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Non-coded visual shell around the Soul Wisp.
    ///
    /// This layer reacts only to already-accepted AuraBuffController state. It never
    /// reads decoder scores, never touches VepAuraStimulus, and never changes coded
    /// target luminance/frequency. Geometry motion and one-shot accents provide fantasy
    /// feedback while the scientific stimulus remains an independent child renderer.
    /// </summary>
    [RequireComponent(typeof(SoulWispController))]
    public sealed class WispPresentationShell : MonoBehaviour
    {
        [SerializeField] private AuraBuffController buffs;
        [SerializeField] private float responseSharpness = 6.5f;
        [SerializeField] private float baseRadius = 0.48f;
        [SerializeField] private float lineWidth = 0.020f;

        private Transform _visualRoot;
        private LineRenderer _neutralRing;
        private LineRenderer _sightRing;
        private LineRenderer _guardRing;
        private LineRenderer _concordRing;
        private Material _lineMaterial;
        private float _sight;
        private float _guard;
        private float _concord;
        private float _accent;
        private bool _subscribed;

        private static readonly Color NeutralColor = new Color(0.58f, 0.44f, 0.86f, 1f);
        private static readonly Color SightColor = new Color(0.20f, 0.60f, 1f, 1f);
        private static readonly Color GuardColor = new Color(0.18f, 1f, 0.52f, 1f);
        private static readonly Color ConcordColor = new Color(0.90f, 0.82f, 1f, 1f);

        private void Awake()
        {
            ResolveBuffs();
            BuildShell();
        }

        private void OnEnable()
        {
            ResolveBuffs();
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void OnDestroy()
        {
            if (_lineMaterial != null) Destroy(_lineMaterial);
        }

        private void Update()
        {
            ResolveBuffs();
            Subscribe();

            float dt = Time.unscaledDeltaTime;
            float response = 1f - Mathf.Exp(-Mathf.Max(0.01f, responseSharpness) * dt);
            _sight = Mathf.Lerp(_sight, buffs != null && buffs.SightActive ? 1f : 0f, response);
            _guard = Mathf.Lerp(_guard, buffs != null && buffs.GuardActive ? 1f : 0f, response);
            _concord = Mathf.Lerp(_concord, buffs != null && buffs.ConcordActive ? 1f : 0f, response);
            _accent = Mathf.MoveTowards(_accent, 0f, dt * 2.4f);

            float now = Time.unscaledTime;
            float active = Mathf.Max(_sight, _guard);
            float accentScale = 1f + _accent * 0.14f;

            if (_visualRoot != null)
            {
                _visualRoot.localPosition = Vector3.zero;
                _visualRoot.localRotation = Quaternion.identity;
                _visualRoot.localScale = Vector3.one * accentScale;
            }

            UpdateRing(
                _neutralRing,
                NeutralColor,
                0.18f + (1f - active) * 0.16f,
                baseRadius * (1f + active * 0.06f),
                Quaternion.Euler(68f, now * 18f, 12f),
                lineWidth);

            UpdateRing(
                _sightRing,
                SightColor,
                0.04f + _sight * 0.82f,
                baseRadius * (1.05f + _sight * 0.20f),
                Quaternion.Euler(24f, now * (24f + _sight * 18f), 58f),
                lineWidth * (0.85f + _sight * 0.55f));

            UpdateRing(
                _guardRing,
                GuardColor,
                0.04f + _guard * 0.82f,
                baseRadius * (1.08f + _guard * 0.24f),
                Quaternion.Euler(112f, -now * (21f + _guard * 15f), 18f),
                lineWidth * (0.85f + _guard * 0.55f));

            bool showConcord = _concord > 0.015f && PresentationQualityGovernor.OptionalShellDetail;
            if (_concordRing != null)
            {
                _concordRing.enabled = showConcord;
                if (showConcord)
                {
                    UpdateRing(
                        _concordRing,
                        ConcordColor,
                        Mathf.Clamp01(_concord * 0.72f + _accent * 0.20f),
                        baseRadius * (1.34f + _concord * 0.22f),
                        Quaternion.Euler(42f, now * 11f, now * 7f),
                        lineWidth * (0.75f + _concord * 0.65f));
                }
            }
        }

        private void ResolveBuffs()
        {
            if (buffs == null) buffs = FindObjectOfType<AuraBuffController>(true);
        }

        private void Subscribe()
        {
            if (_subscribed || buffs == null) return;
            buffs.AuraApplied += OnAuraApplied;
            buffs.ConcordTriggered += OnConcordTriggered;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || buffs == null) return;
            buffs.AuraApplied -= OnAuraApplied;
            buffs.ConcordTriggered -= OnConcordTriggered;
            _subscribed = false;
        }

        private void OnAuraApplied(string aura)
        {
            _accent = Mathf.Max(_accent, 0.72f);
        }

        private void OnConcordTriggered()
        {
            _accent = 1f;
        }

        private void BuildShell()
        {
            Transform existing = transform.Find("MindforgeWispPresentationShell");
            if (existing != null)
            {
                _visualRoot = existing;
                _neutralRing = FindLine("NeutralRing");
                _sightRing = FindLine("SightRing");
                _guardRing = FindLine("GuardRing");
                _concordRing = FindLine("ConcordRing");
                return;
            }

            _visualRoot = new GameObject("MindforgeWispPresentationShell").transform;
            _visualRoot.SetParent(transform, false);
            _neutralRing = CreateRing("NeutralRing", 40);
            _sightRing = CreateRing("SightRing", 40);
            _guardRing = CreateRing("GuardRing", 40);
            _concordRing = CreateRing("ConcordRing", 48);
        }

        private LineRenderer FindLine(string childName)
        {
            if (_visualRoot == null) return null;
            Transform child = _visualRoot.Find(childName);
            return child != null ? child.GetComponent<LineRenderer>() : null;
        }

        private LineRenderer CreateRing(string childName, int segments)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(_visualRoot, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = LineMaterial();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = Mathf.Clamp(segments, 16, 64);
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.textureMode = LineTextureMode.Stretch;

            int count = line.positionCount;
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
            }
            return line;
        }

        private Material LineMaterial()
        {
            if (_lineMaterial != null) return _lineMaterial;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            _lineMaterial = new Material(shader) { name = "MindforgeWispShellMaterial" };
            return _lineMaterial;
        }

        private static void UpdateRing(
            LineRenderer line,
            Color color,
            float alpha,
            float radius,
            Quaternion rotation,
            float width)
        {
            if (line == null) return;
            line.enabled = alpha > 0.005f;
            if (!line.enabled) return;

            Transform t = line.transform;
            t.localRotation = rotation;
            t.localScale = Vector3.one * Mathf.Max(0.01f, radius);
            Color c = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
            line.startColor = c;
            line.endColor = c;
            line.widthMultiplier = Mathf.Max(0.001f, width);
        }
    }

    /// <summary>
    /// Merge-friendly bootstrap: attaches the shell without requiring edits to the
    /// authoritative SoulWispController or scene assembler.
    /// </summary>
    public sealed class WispPresentationShellBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            new GameObject("MindforgeWispShellBootstrap")
                .AddComponent<WispPresentationShellBootstrap>();
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 240; frame++)
            {
                SoulWispController[] wisps = FindObjectsOfType<SoulWispController>(true);
                if (wisps.Length > 0)
                {
                    foreach (SoulWispController wisp in wisps)
                    {
                        if (wisp != null && wisp.GetComponent<WispPresentationShell>() == null)
                            wisp.gameObject.AddComponent<WispPresentationShell>();
                    }
                    Destroy(gameObject);
                    yield break;
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
