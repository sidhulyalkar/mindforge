using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only ground dodge roll. GuardianMotor remains the sole movement and
    /// invulnerability authority; this component inserts a wrapper above the procedural
    /// avatar and rotates that wrapper while the authoritative ground dash is active.
    /// Air dashes keep their existing aerial pose.
    /// </summary>
    [DefaultExecutionOrder(800)]
    public sealed class GuardianDodgeRollPresentation : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private float visualRollSeconds = 0.28f;
        [SerializeField] private float tuckScale = 0.90f;
        [SerializeField] private float verticalDip = 0.10f;

        private Transform _rollRoot;
        private bool _rolling;
        private float _rollElapsed;

        private void Awake()
        {
            if (motor == null) motor = GetComponent<GuardianMotor>();
        }

        private void Start()
        {
            BindAvatar();
            if (motor != null)
            {
                motor.DashStarted -= OnDashStarted;
                motor.DashStarted += OnDashStarted;
            }
        }

        private void OnDestroy()
        {
            if (motor != null) motor.DashStarted -= OnDashStarted;
        }

        private void BindAvatar()
        {
            if (_rollRoot != null) return;
            Transform avatar = transform.Find("GuardianShowcaseAvatar");
            if (avatar == null) return;

            GameObject wrapper = new GameObject("Motion_DodgeRollRoot");
            _rollRoot = wrapper.transform;
            _rollRoot.SetParent(transform, false);
            _rollRoot.localPosition = avatar.localPosition;
            _rollRoot.localRotation = avatar.localRotation;
            _rollRoot.localScale = Vector3.one;

            avatar.SetParent(_rollRoot, true);
        }

        private void OnDashStarted()
        {
            if (motor == null || motor.IsAirDashing || !motor.IsGrounded) return;
            if (_rollRoot == null) BindAvatar();
            _rolling = _rollRoot != null;
            _rollElapsed = 0f;
        }

        private void LateUpdate()
        {
            if (_rollRoot == null) BindAvatar();
            if (_rollRoot == null) return;

            if (!_rolling)
            {
                _rollRoot.localPosition = Vector3.Lerp(_rollRoot.localPosition, Vector3.zero,
                    1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                _rollRoot.localRotation = Quaternion.Slerp(_rollRoot.localRotation, Quaternion.identity,
                    1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
                _rollRoot.localScale = Vector3.Lerp(_rollRoot.localScale, Vector3.one,
                    1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                return;
            }

            _rollElapsed += Mathf.Min(0.05f, Time.unscaledDeltaTime);
            float duration = Mathf.Max(0.10f, visualRollSeconds);
            float t = Mathf.Clamp01(_rollElapsed / duration);
            float eased = t * t * (3f - 2f * t);
            float angle = -360f * eased;
            float dip = Mathf.Sin(t * Mathf.PI) * Mathf.Max(0f, verticalDip);
            float tuck = Mathf.Lerp(1f, Mathf.Clamp(tuckScale, 0.75f, 1f), Mathf.Sin(t * Mathf.PI));

            _rollRoot.localPosition = Vector3.down * dip;
            _rollRoot.localRotation = Quaternion.Euler(angle, 0f, 0f);
            _rollRoot.localScale = new Vector3(1f, tuck, 1f);

            if (t >= 1f || motor == null || !motor.IsDashing || motor.IsAirDashing)
            {
                _rolling = false;
                _rollRoot.localPosition = Vector3.zero;
                _rollRoot.localRotation = Quaternion.identity;
                _rollRoot.localScale = Vector3.one;
            }
        }
    }
}
