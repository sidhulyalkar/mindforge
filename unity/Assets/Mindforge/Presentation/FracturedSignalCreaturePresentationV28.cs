using System;
using Mindforge.Combat;
using Mindforge.SoulWisp;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation adapter for the V0.28 imported Fractured Signal creature.
    ///
    /// The imported rig supplies authored anatomy and animation. Existing Mindforge movement,
    /// attack, damage and neural owners remain authoritative. This component only samples the
    /// imported clips from those existing states and applies restrained corruption tinting.
    /// </summary>
    [DefaultExecutionOrder(905)]
    [RequireComponent(typeof(FracturedSignalDirector))]
    [RequireComponent(typeof(CombatantVitals))]
    public sealed class FracturedSignalCreaturePresentationV28 : MonoBehaviour
    {
        public const string RootName = "V28_ReliquaryBeast";
        public const string ModelName = "V28_ReliquaryBeast_Model";

        [SerializeField] private Transform modelRoot;
        [SerializeField] private AnimationClip idleClip;
        [SerializeField] private AnimationClip walkClip;
        [SerializeField] private AnimationClip attackClip;
        [SerializeField] private AnimationClip deathClip;
        [SerializeField] private FracturedSignalDirector director;
        [SerializeField] private FracturedSignalFirstBossV19 movement;
        [SerializeField] private CombatantVitals vitals;
        [SerializeField] private SoulWispController wisp;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private Vector3 _modelBasePosition;
        private Quaternion _modelBaseRotation;
        private Vector3 _modelBaseScale;
        private float _stateTime;
        private float _attackUntil;
        private float _damagePulse;
        private float _deathTime;
        private bool _dead;
        private bool _configured;
        private int _phase = 1;
        private VisualState _state;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private enum VisualState
        {
            Idle,
            Walk,
            Attack,
            Dead,
        }

        public Transform ModelRoot => modelRoot;
        public bool Configured => _configured && modelRoot != null;

        public void Configure(
            Transform importedModelRoot,
            AnimationClip idle,
            AnimationClip walk,
            AnimationClip attack,
            AnimationClip dead)
        {
            modelRoot = importedModelRoot;
            idleClip = idle;
            walkClip = walk;
            attackClip = attack;
            deathClip = dead;
            Resolve();
            CaptureModelBase();
            PrepareRenderers();
            _configured = modelRoot != null && idleClip != null && walkClip != null && attackClip != null;
        }

        private void Awake()
        {
            Resolve();
            CaptureModelBase();
            PrepareRenderers();
            HideRetiredBossVisuals();
        }

        private void OnEnable()
        {
            Resolve();
            if (director != null)
            {
                _phase = Mathf.Clamp(director.Phase, 1, 3);
                director.PhaseChanged += OnPhaseChanged;
                director.AttackTelegraphed += OnAttackTelegraphed;
                director.AttackFired += OnAttackFired;
            }
            if (vitals != null)
            {
                vitals.Damaged += OnDamaged;
                vitals.Died += OnDied;
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
            if (vitals != null)
            {
                vitals.Damaged -= OnDamaged;
                vitals.Died -= OnDied;
            }
        }

        private void Resolve()
        {
            if (director == null) director = GetComponent<FracturedSignalDirector>();
            if (movement == null) movement = GetComponent<FracturedSignalFirstBossV19>();
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
            if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
            if (modelRoot == null)
            {
                Transform root = transform.Find(RootName);
                if (root != null) modelRoot = FindDeep(root, ModelName) ?? root;
            }
        }

        private void CaptureModelBase()
        {
            if (modelRoot == null) return;
            _modelBasePosition = modelRoot.localPosition;
            _modelBaseRotation = modelRoot.localRotation;
            _modelBaseScale = modelRoot.localScale;
        }

        private void PrepareRenderers()
        {
            if (modelRoot == null) return;
            _renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            _block = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer renderer = _renderers[i];
                if (renderer == null) continue;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            Animator animator = modelRoot.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.enabled = false;
            }
        }

        private void LateUpdate()
        {
            Resolve();
            if (modelRoot == null) return;
            HideRetiredBossVisuals();

            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            _damagePulse = Mathf.MoveTowards(_damagePulse, 0f, dt * 3.8f);

            if (NeuralVisualFieldActive())
            {
                Sample(idleClip, 0f);
                RestoreModelRootTransform();
                ApplySurface(0f, true);
                return;
            }

            VisualState next = ResolveState();
            if (next != _state)
            {
                _state = next;
                _stateTime = 0f;
            }
            else
            {
                _stateTime += dt;
            }

            switch (_state)
            {
                case VisualState.Dead:
                    _deathTime += dt;
                    Sample(deathClip != null ? deathClip : idleClip, _deathTime, false);
                    break;
                case VisualState.Attack:
                    Sample(attackClip, _stateTime, false);
                    break;
                case VisualState.Walk:
                    Sample(walkClip, _stateTime, true);
                    break;
                default:
                    Sample(idleClip, _stateTime, true);
                    break;
            }

            RestoreModelRootTransform();
            ApplySurface(Time.unscaledTime, false);
        }

        private VisualState ResolveState()
        {
            if (_dead || (vitals != null && !vitals.IsAlive)) return VisualState.Dead;
            if (Time.unscaledTime < _attackUntil) return VisualState.Attack;
            if (movement != null && movement.MovementActive) return VisualState.Walk;
            return VisualState.Idle;
        }

        private void Sample(AnimationClip clip, float time, bool loop = true)
        {
            if (clip == null || modelRoot == null) return;
            float length = Mathf.Max(0.001f, clip.length);
            float sample = loop ? Mathf.Repeat(time, length) : Mathf.Min(time, Mathf.Max(0f, length - 0.001f));
            clip.SampleAnimation(modelRoot.gameObject, sample);
        }

        private void RestoreModelRootTransform()
        {
            // Imported clips may contain root translation. Mindforge locomotion owns world motion,
            // so clip sampling is allowed to animate bones but never the boss/model root transform.
            modelRoot.localPosition = _modelBasePosition;
            modelRoot.localRotation = _modelBaseRotation;
            modelRoot.localScale = _modelBaseScale;
        }

        private void ApplySurface(float time, bool neutral)
        {
            if (_renderers == null || _block == null) return;
            float phase01 = Mathf.InverseLerp(1f, 3f, _phase);
            float pulse = neutral ? 0f : 0.5f + 0.5f * Mathf.Sin(time * (1.25f + phase01 * 0.35f));
            float damage = neutral ? 0f : _damagePulse;
            Color stone = Color.Lerp(new Color(0.32f, 0.30f, 0.31f), new Color(0.18f, 0.14f, 0.19f), phase01);
            stone = Color.Lerp(stone, new Color(0.40f, 0.19f, 0.32f), damage * 0.28f);
            Color emission = new Color(0.82f, 0.055f, 0.78f) * ((0.10f + phase01 * 0.18f) * (0.55f + pulse * 0.45f) + damage * 0.32f);

            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer renderer = _renderers[i];
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_block);
                _block.SetColor(BaseColor, stone);
                _block.SetColor(ColorProperty, stone);
                _block.SetColor(EmissionColor, emission);
                renderer.SetPropertyBlock(_block);
            }
        }

        private void HideRetiredBossVisuals()
        {
            FracturedSignalBeastV27 v27 = GetComponent<FracturedSignalBeastV27>();
            if (v27 != null && v27.enabled) v27.enabled = false;
            DisableChild(FracturedSignalBeastV27.RootName);
            DisableChild(FracturedSignalCharacterV19.RootName);
            DisableChild("V11BossVisual");
            DisableChild("FracturedSignalShowcaseAvatar");
            DisableChild("FracturedSignalThreatSilhouette");

            Renderer legacy = GetComponent<Renderer>();
            if (legacy != null) legacy.enabled = false;
        }

        private void DisableChild(string childName)
        {
            Transform child = transform.Find(childName);
            if (child != null && child != modelRoot && child.gameObject.activeSelf) child.gameObject.SetActive(false);
        }

        private void OnPhaseChanged(int phase) => _phase = Mathf.Clamp(phase, 1, 3);

        private void OnAttackTelegraphed(string pattern, int count, bool heavy)
        {
            float duration = attackClip != null ? Mathf.Clamp(attackClip.length, 0.32f, 1.25f) : 0.72f;
            _attackUntil = Mathf.Max(_attackUntil, Time.unscaledTime + duration * (heavy ? 1.0f : 0.78f));
        }

        private void OnAttackFired(string pattern, int count, bool heavy)
        {
            float tail = attackClip != null ? Mathf.Clamp(attackClip.length * 0.42f, 0.18f, 0.55f) : 0.28f;
            _attackUntil = Mathf.Max(_attackUntil, Time.unscaledTime + tail);
        }

        private void OnDamaged(DamagePacket packet) => _damagePulse = 1f;

        private void OnDied()
        {
            _dead = true;
            _deathTime = 0f;
            _state = VisualState.Dead;
        }

        private bool NeuralVisualFieldActive()
        {
            if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
            return wisp != null && (wisp.CalibrationStimuliActive || wisp.ResonanceWindowActive);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (string.Equals(root.name, name, StringComparison.Ordinal)) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
