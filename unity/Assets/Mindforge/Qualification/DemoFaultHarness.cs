using System.Diagnostics;
using UnityEngine;

namespace Mindforge.Qualification
{
    /// <summary>
    /// Qualification-only main-thread fault injector. It deliberately stalls Unity
    /// so the threaded/bounded neural transport can be observed under ugly frame
    /// pacing. Never enable automatic injection in a participant run.
    /// </summary>
    public sealed class DemoFaultHarness : MonoBehaviour
    {
        [SerializeField] private KeyCode fiftyMillisecondStallKey = KeyCode.F6;
        [SerializeField] private KeyCode oneHundredTwentyMillisecondStallKey = KeyCode.F7;
        [SerializeField] private bool allowKeyboardInjection = true;

        public int LastInjectedMilliseconds { get; private set; }
        public double LastInjectionRealtime { get; private set; }

        private void Update()
        {
            if (!allowKeyboardInjection) return;
            if (Input.GetKeyDown(fiftyMillisecondStallKey)) InjectMainThreadStall(50);
            if (Input.GetKeyDown(oneHundredTwentyMillisecondStallKey)) InjectMainThreadStall(120);
        }

        public void InjectMainThreadStall(int milliseconds)
        {
            milliseconds = Mathf.Clamp(milliseconds, 1, 500);
            LastInjectedMilliseconds = milliseconds;
            LastInjectionRealtime = Time.realtimeSinceStartupAsDouble;
            long targetTicks = (long)(Stopwatch.Frequency * (milliseconds / 1000.0));
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedTicks < targetTicks) { }
        }
    }
}
