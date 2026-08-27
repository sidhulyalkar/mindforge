using System.Collections;
using UnityEngine;
using Mindforge.Neural;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Post-decision haptic confirmation only.
    ///
    /// We intentionally do NOT ramp controller rumble while FBCCA evidence is being
    /// accumulated. Continuous vibration can induce hand/arm movement and EMG that
    /// then contaminates the EEG we are trying to classify. Short haptic echoes are
    /// emitted only after an accepted neural event or Concord transition.
    /// </summary>
    public sealed class NeuralHapticFeedback : MonoBehaviour
    {
        [SerializeField] private UdpNeuralReceiver receiver;
        [SerializeField] private AuraBuffController buffs;
        [SerializeField] private float selectionDurationSeconds = 0.055f;
        [SerializeField] private float concordDurationSeconds = 0.095f;

        private Coroutine _pulse;

        private void OnEnable()
        {
            if (receiver != null) receiver.EventReceived += OnNeuralEvent;
            if (buffs != null) buffs.ConcordTriggered += OnConcord;
        }

        private void OnDisable()
        {
            if (receiver != null) receiver.EventReceived -= OnNeuralEvent;
            if (buffs != null) buffs.ConcordTriggered -= OnConcord;
            if (_pulse != null) StopCoroutine(_pulse);
            _pulse = null;
            SetMotors(0f, 0f);
        }

        private void OnNeuralEvent(NeuralEvent evt)
        {
            if (evt == null || !evt.IsSelection) return;
            if (evt.Target == AuraTarget.Sight) Pulse(0.10f, 0.24f, selectionDurationSeconds);
            else if (evt.Target == AuraTarget.Guard) Pulse(0.22f, 0.10f, selectionDurationSeconds);
        }

        private void OnConcord()
        {
            Pulse(0.22f, 0.62f, concordDurationSeconds);
        }

        private void Pulse(float low, float high, float realSeconds)
        {
            if (_pulse != null) StopCoroutine(_pulse);
            SetMotors(0f, 0f);
            _pulse = StartCoroutine(PulseRoutine(low, high, realSeconds));
        }

        private IEnumerator PulseRoutine(float low, float high, float realSeconds)
        {
            SetMotors(low, high);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, realSeconds));
            SetMotors(0f, 0f);
            _pulse = null;
        }

        private static void SetMotors(float low, float high)
        {
#if ENABLE_INPUT_SYSTEM
            if (Gamepad.current != null)
                Gamepad.current.SetMotorSpeeds(Mathf.Clamp01(low), Mathf.Clamp01(high));
#endif
        }
    }
}
