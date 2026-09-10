using TMPro;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Compact developer-facing neural status readout. It exposes link/calibration/
    /// window state so a tester never has to guess whether the closed loop is armed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeBciStatusHudV33 : MonoBehaviour
    {
        [SerializeField] private Vector3 cameraLocalPosition = new Vector3(-0.66f, 0.34f, 2.15f);
        [SerializeField] private float refreshSeconds = 0.10f;

        private MindforgeUdpNeuralReceiverV33 _receiver;
        private MindforgeBciCalibrationDirectorV33 _calibration;
        private MindforgeNeuralWindowControllerV33 _windows;
        private MindforgeNeuralIntentBridgeV33 _bridge;
        private MindforgeDisplayTimingMonitorV33 _timing;
        private TextMeshPro _text;
        private float _nextRefresh;

        private void Start()
        {
            ResolveDependencies();
            Camera camera = Camera.main;
            if (camera == null) return;

            GameObject root = new GameObject("Mindforge_BCI_Status_V33", typeof(RectTransform));
            root.transform.SetParent(camera.transform, false);
            root.transform.localPosition = cameraLocalPosition;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one * 0.10f;

            _text = root.AddComponent<TextMeshPro>();
            _text.fontSize = 0.52f;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.enableWordWrapping = false;
            _text.rectTransform.sizeDelta = new Vector2(5.8f, 2.2f);
            _text.color = new Color(0.78f, 0.88f, 0.96f, 0.92f);
            UpdateText();
        }

        private void Update()
        {
            if (_text == null) return;
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + refreshSeconds;
            ResolveDependencies();
            UpdateText();
        }

        private void ResolveDependencies()
        {
            if (_receiver == null) _receiver = GetComponent<MindforgeUdpNeuralReceiverV33>();
            if (_calibration == null) _calibration = GetComponent<MindforgeBciCalibrationDirectorV33>();
            if (_windows == null) _windows = GetComponent<MindforgeNeuralWindowControllerV33>();
            if (_bridge == null) _bridge = GetComponent<MindforgeNeuralIntentBridgeV33>();
            if (_timing == null) _timing = GetComponent<MindforgeDisplayTimingMonitorV33>();
        }

        private void UpdateText()
        {
            if (_text == null) return;
            string link = _receiver != null && _receiver.IsConnected ? "LINKED" : "OFFLINE";
            string calibration = _calibration != null ? _calibration.State.ToString().ToUpperInvariant() : "NO CAL";
            string window = _windows != null && _windows.IsListening
                ? $"LISTEN #{_windows.ActiveEpoch} {_windows.RemainingSeconds:F1}s"
                : "WINDOW IDLE";
            string last = _bridge != null && _bridge.LastAcceptedIntent != MindforgeIntentV29.None
                ? $"LAST {_bridge.LastAcceptedIntent.ToString().ToUpperInvariant()}"
                : "LAST -";
            string timing = _timing != null && _timing.HasMeasurement
                ? $"{_timing.ObservedRefreshHz:F0} Hz / long {_timing.LongFrameFraction:P0}"
                : "timing warming";

            _text.text =
                $"BCI V0.33  {link}\n" +
                $"{calibration}  |  {window}\n" +
                $"{last}  |  {timing}\n" +
                "C calibrate  N listen  1 Sight  2 Guard  B pause";
        }
    }
}
