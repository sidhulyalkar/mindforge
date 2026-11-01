using UnityEngine;

namespace Mindforge.LivingWorld
{
    /// <summary>A small debug overlay installed only in the explicitly generated demo scene.</summary>
    public sealed class LivingWorldDemoHud : MonoBehaviour
    {
        [SerializeField] private LivingWorldPassiveReceiver receiver;
        public void Configure(LivingWorldPassiveReceiver value) { receiver = value; }

        private void OnGUI()
        {
            if (receiver == null) return;
            Rect rect = new Rect(22f, 22f, Mathf.Min(Screen.width - 44f, 410f), 122f);
            if (rect.width < 180f) return;
            GUI.Box(rect, "MINDFORGE  /  LIVING WORLD V3");
            string kind = receiver.Fresh ? receiver.LastSourceMode : "disconnected · neutral fallback";
            GUI.Label(new Rect(rect.x + 18f, rect.y + 32f, rect.width - 36f, 78f),
                "PASSIVE NEURAL FEATURE · " + kind + "\n" +
                "Posterior alpha relative power: " + receiver.CurrentValue.ToString("F3") + "\n" +
                "Accepted: " + receiver.AcceptedCount + "  Rejected: " + receiver.RejectedCount + "\n" +
                "Synthetic visual demonstration; not a brain-state diagnosis.");
        }
    }
}
