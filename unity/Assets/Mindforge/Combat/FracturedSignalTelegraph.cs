using UnityEngine;
using Mindforge.Presentation;

namespace Mindforge.Combat
{
    /// <summary>
    /// Optional procedural telegraph renderer for The Fractured Signal.
    /// Telegraphs use hostile crimson/orange only and remain visually distinct from
    /// the smooth blue/green BCI targets.
    /// </summary>
    public sealed class FracturedSignalTelegraph : MonoBehaviour
    {
        [SerializeField] private CombatVisualPalette palette;
        [SerializeField] private LineRenderer[] rays;
        [SerializeField] private LineRenderer radialRing;
        [SerializeField] private float rayLength = 16f;
        [SerializeField] private float ringRadius = 2.2f;
        [SerializeField] private int ringSegments = 64;

        private void Awake() => Clear();

        public void ShowFan(Vector3 origin, Vector3 centerDirection, int count, float spreadDegrees, bool heavy = false)
        {
            Clear();
            if (rays == null) return;
            Color color = Hostile(heavy);
            int shown = Mathf.Min(count, rays.Length);
            for (int i = 0; i < shown; i++)
            {
                LineRenderer line = rays[i];
                if (line == null) continue;
                float offset = (i - (shown - 1) * 0.5f) * spreadDegrees;
                Vector3 direction = Quaternion.AngleAxis(offset, Vector3.up) * centerDirection.normalized;
                line.gameObject.SetActive(true);
                line.positionCount = 2;
                line.SetPosition(0, origin);
                line.SetPosition(1, origin + direction * rayLength);
                line.startColor = new Color(color.r, color.g, color.b, 0.15f);
                line.endColor = new Color(color.r, color.g, color.b, 0.85f);
            }
        }

        public void ShowRadial(Vector3 origin, bool heavy = false)
        {
            Clear();
            if (radialRing == null) return;
            int segments = Mathf.Max(12, ringSegments);
            Color color = Hostile(heavy);
            radialRing.gameObject.SetActive(true);
            radialRing.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                radialRing.SetPosition(i, origin + new Vector3(Mathf.Cos(a), 0.05f, Mathf.Sin(a)) * ringRadius);
            }
            radialRing.startColor = color;
            radialRing.endColor = color;
        }

        public void Clear()
        {
            if (rays != null)
                foreach (LineRenderer line in rays)
                    if (line != null) line.gameObject.SetActive(false);
            if (radialRing != null) radialRing.gameObject.SetActive(false);
        }

        private Color Hostile(bool heavy)
        {
            if (palette != null) return heavy ? palette.hostileHeavy : palette.hostilePrimary;
            return heavy ? new Color(1f, 0.42f, 0.12f) : new Color(1f, 0.18f, 0.34f);
        }
    }
}
