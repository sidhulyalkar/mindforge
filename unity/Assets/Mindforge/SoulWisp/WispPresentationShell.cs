using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Presentation;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Non-coded fantasy presentation shell around the Soul Wisp.
    ///
    /// The shell is deliberately a drifting flame/tendril language rather than orbital
    /// rings. It reacts only to already-accepted AuraBuffController state, never reads
    /// decoder scores, never touches VepAuraStimulus, and never changes coded target
    /// luminance/frequency. The authoritative Wisp controller owns companion position;
    /// this class only shapes the visual wake around that position.
    /// </summary>
    [RequireComponent(typeof(SoulWispController))]
    public sealed class WispPresentationShell : MonoBehaviour
    {
        [SerializeField] private AuraBuffController buffs;
        [SerializeField] private float responseSharpness = 6.5f;
        [SerializeField] private float tendrilLength = 0.82f;
        [SerializeField] private float lineWidth = 0.045f;
        [SerializeField] private float tendrilSway = 0.16f;
        [SerializeField] private float velocityResponse = 5.5f;

        private Transform _visualRoot;
        private LineRenderer _neutralTendril;
        private LineRenderer _sightTendril;
        private LineRenderer _guardTendril;
        private LineRenderer _concordTendril;
        private Material _lineMaterial;
        private float _sight;
        private float _guard;
        private float _concord;
        private float _accent;
        private bool _subscribed;
        private Vector3 _previousWorldPosition;
        private Vector3 _presentationVelocity;

        private static readonly Color NeutralColor = new Color(0.66f, 0.46f, 1.00f, 1f);
        private static readonly Color SightColor = new Color(0.20f, 0.60f, 1f, 1f);
        private static readonly Color GuardColor = new Color(0.18f, 1f, 0.52f, 1f);
        private static readonly Color ConcordColor = new Color(0.94f, 0.76f, 1f, 1f);

        private void Awake()
        {
            ResolveBuffs();
            BuildShell();
            _previousWorldPosition = transform.position;
        }

        private void OnEnable()
        {
            ResolveBuffs();
            Subscribe();
            _previousWorldPosition = transform.position;
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

            float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            float response = 1f - Mathf.Exp(-Mathf.Max(0.01f, responseSharpness) * dt);
            _sight = Mathf.Lerp(_sight, buffs != null && buffs.SightActive ? 1f : 0f, response);
            _guard = Mathf.Lerp(_guard, buffs != null && buffs.GuardActive ? 1f : 0f, response);
            _concord = Mathf.Lerp(_concord, buffs != null && buffs.ConcordActive ? 1f : 0f, response);
            _accent = Mathf.MoveTowards(_accent, 0f, dt * 2.4f);

            Vector3 rawVelocity = (transform.position - _previousWorldPosition) / dt;
            _previousWorldPosition = transform.position;
            rawVelocity = Vector3.ClampMagnitude(rawVelocity, 9f);
            float velocityBlend = 1f - Mathf.Exp(-Mathf.Max(0.1f, velocityResponse) * dt);
            _presentationVelocity = Vector3.Lerp(_presentationVelocity, rawVelocity, velocityBlend);

            Vector3 trailWorld = ResolveTrailDirectionWorld();
            Vector3 trailLocal = transform.InverseTransformDirection(trailWorld).normalized;
            float speedStretch = Mathf.Clamp01(_presentationVelocity.magnitude / 7.5f);
            float now = Time.unscaledTime;
            float active = Mathf.Max(_sight, _guard);
            float accentScale = 1f + _accent * 0.18f;

            if (_visualRoot != null)
            {
                _visualRoot.localPosition = Vector3.zero;
                _visualRoot.localRotation = Quaternion.identity;
                _visualRoot.localScale = Vector3.one * accentScale;
            }

            UpdateTendril(
                _neutralTendril,
                NeutralColor,
                0.46f + (1f - active) * 0.16f,
                tendrilLength * (0.92f + speedStretch * 0.65f),
                lineWidth,
                trailLocal,
                now,
                0.0f,
                1.9f);

            UpdateTendril(
                _sightTendril,
                SightColor,
                0.02f + _sight * 0.88f,
                tendrilLength * (0.84f + _sight * 0.34f + speedStretch * 0.34f),
                lineWidth * (0.72f + _sight * 0.42f),
                trailLocal,
                now,
                1.8f,
                2.55f);

            UpdateTendril(
                _guardTendril,
                GuardColor,
                0.02f + _guard * 0.88f,
                tendrilLength * (0.88f + _guard * 0.38f + speedStretch * 0.30f),
                lineWidth * (0.72f + _guard * 0.42f),
                trailLocal,
                now,
                3.7f,
                2.25f);

            bool showConcord = _concord > 0.015f && PresentationQualityGovernor.OptionalShellDetail;
            if (_concordTendril != null)
            {
                _concordTendril.enabled = showConcord;
                if (showConcord)
                {
                    UpdateTendril(
                        _concordTendril,
                        ConcordColor,
                        Mathf.Clamp01(_concord * 0.74f + _accent * 0.24f),
                        tendrilLength * (1.04f + _concord * 0.48f + speedStretch * 0.28f),
                        lineWidth * (0.62f + _concord * 0.48f),
                        trailLocal,
                        now,
                        5.2f,
                        1.55f);
                }
            }
        }

        private Vector3 ResolveTrailDirectionWorld()
        {
            if (_presentationVelocity.sqrMagnitude > 0.04f)
                return -_presentationVelocity.normalized;

            Camera cam = Camera.main;
            Vector3 cameraBack = cam != null
                ? -Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up)
                : -transform.forward;
            if (cameraBack.sqrMagnitude < 0.001f) cameraBack = Vector3.back;
            return (cameraBack.normalized * 0.32f + Vector3.down * 0.68f).normalized;
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
            if (existing != null) Destroy(existing.gameObject);

            _visualRoot = new GameObject("MindforgeWispPresentationShell").transform;
            _visualRoot.SetParent(transform, false);
            _neutralTendril = CreateTendril("NeutralTendril", 12);
            _sightTendril = CreateTendril("SightTendril", 12);
            _guardTendril = CreateTendril("GuardTendril", 12);
            _concordTendril = CreateTendril("ConcordTendril", 14);
        }

        private LineRenderer CreateTendril(string childName, int segments)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(_visualRoot, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = LineMaterial();
            line.useWorldSpace = false;
            line.loop = false;
            line.positionCount = Mathf.Clamp(segments, 8, 20);
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.widthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.42f, 0.72f),
                new Keyframe(1f, 0f));
            return line;
        }

        private Material LineMaterial()
        {
            if (_lineMaterial != null) return _lineMaterial;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            _lineMaterial = new Material(shader) { name = "MindforgeWispTendrilMaterial" };
            return _lineMaterial;
        }

        private void UpdateTendril(
            LineRenderer line,
            Color color,
            float alpha,
            float length,
            float width,
            Vector3 trailDirection,
            float now,
            float phase,
            float frequency)
        {
            if (line == null) return;
            line.enabled = alpha > 0.005f;
            if (!line.enabled) return;

            Vector3 direction = trailDirection.sqrMagnitude > 0.001f ? trailDirection.normalized : Vector3.down;
            Vector3 side = Vector3.Cross(direction, Vector3.up);
            if (side.sqrMagnitude < 0.001f) side = Vector3.right;
            side.Normalize();
            Vector3 bendUp = Vector3.Cross(side, direction).normalized;

            int count = line.positionCount;
            for (int i = 0; i < count; i++)
            {
                float u = count <= 1 ? 0f : i / (float)(count - 1);
                float envelope = Mathf.Sin(u * Mathf.PI) * tendrilSway * (0.45f + u * 0.90f);
                float wave = Mathf.Sin(now * frequency + phase + u * 7.4f) * envelope;
                float curl = Mathf.Cos(now * (frequency * 0.73f) + phase * 1.31f + u * 5.2f) * envelope * 0.72f;
                Vector3 point = direction * Mathf.Max(0.05f, length) * u
                    + side * wave
                    + bendUp * curl
                    + Vector3.down * (u * u * 0.06f);
                line.SetPosition(i, point);
            }

            Color start = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
            Color end = new Color(color.r, color.g, color.b, 0f);
            line.startColor = start;
            line.endColor = end;
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
