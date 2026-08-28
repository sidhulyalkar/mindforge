using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only reticle for the conventional GuardianTargetLock. It observes
    /// the selected target and never creates, cycles or releases lock state.
    /// </summary>
    public sealed class LockOnIndicator : MonoBehaviour
    {
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private float targetHeight = 1.1f;
        [SerializeField] private float size = 25f;
        [SerializeField] private float thickness = 2f;
        [SerializeField] private Color color = new Color(0.90f, 0.94f, 1f, 0.88f);

        private Transform _target;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<LockOnIndicator>(true) != null) return;
            GuardianTargetLock lockState = FindObjectOfType<GuardianTargetLock>(true);
            if (lockState == null) return;
            LockOnIndicator indicator = new GameObject("MindforgeLockOnIndicator").AddComponent<LockOnIndicator>();
            indicator.targetLock = lockState;
        }

        private void OnEnable()
        {
            Resolve();
            Subscribe();
            _target = targetLock != null ? targetLock.Target : null;
        }

        private void OnDisable() => Unsubscribe();

        private void Update()
        {
            if (targetLock == null)
            {
                Unsubscribe();
                Resolve();
                Subscribe();
            }
            _target = targetLock != null ? targetLock.Target : null;
        }

        private void Resolve()
        {
            if (targetLock == null) targetLock = FindObjectOfType<GuardianTargetLock>(true);
            if (gameplayCamera == null) gameplayCamera = Camera.main;
        }

        private void Subscribe()
        {
            if (targetLock == null) return;
            targetLock.TargetChanged -= OnTargetChanged;
            targetLock.TargetChanged += OnTargetChanged;
        }

        private void Unsubscribe()
        {
            if (targetLock != null) targetLock.TargetChanged -= OnTargetChanged;
        }

        private void OnTargetChanged(Transform target) => _target = target;

        private void OnGUI()
        {
            if (_target == null || gameplayCamera == null || targetLock == null || !targetLock.Locked) return;
            Vector3 screen = gameplayCamera.WorldToScreenPoint(_target.position + Vector3.up * targetHeight);
            if (screen.z <= 0f) return;

            float s = Mathf.Max(12f, size);
            float t = Mathf.Max(1f, thickness);
            float x = screen.x - s * 0.5f;
            float y = Screen.height - screen.y - s * 0.5f;
            Color before = GUI.color;
            GUI.color = color;

            float arm = s * 0.34f;
            Draw(new Rect(x, y, arm, t));
            Draw(new Rect(x, y, t, arm));
            Draw(new Rect(x + s - arm, y, arm, t));
            Draw(new Rect(x + s - t, y, t, arm));
            Draw(new Rect(x, y + s - t, arm, t));
            Draw(new Rect(x, y + s - arm, t, arm));
            Draw(new Rect(x + s - arm, y + s - t, arm, t));
            Draw(new Rect(x + s - t, y + s - arm, t, arm));

            GUI.color = before;
        }

        private static void Draw(Rect rect) => GUI.DrawTexture(rect, Texture2D.whiteTexture);
    }
}
