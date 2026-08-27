using UnityEngine;
using Mindforge.Presentation;

namespace Mindforge.Combat
{
    /// <summary>
    /// Procedural telegraph renderer for The Fractured Signal.
    /// Telegraphs use hostile crimson/orange only and remain visually distinct from
    /// the smooth blue/green BCI targets.
    ///
    /// A telegraph is a promise. Fan attacks preview their actual launch lanes and
    /// radial attacks do the same instead of showing only an ambiguous ring.
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
            int requested = Mathf.Max(0, count);
            EnsureRayCapacity(requested);
            Clear();
            if (rays == null) return;
            Color color = Hostile(heavy);
            int shown = Mathf.Min(requested, rays.Length);
            for (int i = 0; i < shown; i++)
            {
                LineRenderer line = rays[i];
                if (line == null) continue;
                float offset = (i - (shown - 1) * 0.5f) * spreadDegrees;
                Vector3 direction = Quaternion.AngleAxis(offset, Vector3.up) * centerDirection.normalized;
                ShowRay(line, origin, direction, color);
            }
        }

        public void ShowRadial(Vector3 origin, int projectileCount, bool heavy = false)
        {
            int count = Mathf.Max(1, projectileCount);
            EnsureRayCapacity(count);
            Clear();
            Color color = Hostile(heavy);

            // Preview the exact angular lattice SpawnRadial uses. This keeps the
            // warning truthful even as phase three increases radial density.
            if (rays != null)
            {
                for (int i = 0; i < count && i < rays.Length; i++)
                {
                    LineRenderer line = rays[i];
                    if (line == null) continue;
                    float angle = i / (float)count * 360f;
                    Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                    ShowRay(line, origin, direction, color);
                }
            }

            if (radialRing == null) return;
            int segments = Mathf.Max(12, ringSegments);
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

        private void EnsureRayCapacity(int requested)
        {
            requested = Mathf.Max(0, requested);
            if (requested == 0 || (rays != null && rays.Length >= requested)) return;

            int existing = rays != null ? rays.Length : 0;
            LineRenderer[] expanded = new LineRenderer[requested];
            for (int i = 0; i < existing; i++) expanded[i] = rays[i];

            LineRenderer template = null;
            for (int i = 0; i < existing && template == null; i++)
                if (rays[i] != null) template = rays[i];

            for (int i = existing; i < requested; i++)
            {
                GameObject go = new GameObject($"TelegraphRay_Runtime_{i:00}");
                go.transform.SetParent(transform, false);
                LineRenderer line = go.AddComponent<LineRenderer>();
                if (template != null)
                {
                    line.sharedMaterial = template.sharedMaterial;
                    line.widthMultiplier = template.widthMultiplier;
                    line.useWorldSpace = template.useWorldSpace;
                    line.textureMode = template.textureMode;
                    line.numCapVertices = template.numCapVertices;
                }
                else
                {
                    line.widthMultiplier = 0.055f;
                    line.useWorldSpace = true;
                }
                line.gameObject.SetActive(false);
                expanded[i] = line;
            }
            rays = expanded;
        }

        private void ShowRay(LineRenderer line, Vector3 origin, Vector3 direction, Color color)
        {
            line.gameObject.SetActive(true);
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, origin + direction.normalized * rayLength);
            line.startColor = new Color(color.r, color.g, color.b, 0.15f);
            line.endColor = new Color(color.r, color.g, color.b, 0.85f);
        }

        private Color Hostile(bool heavy)
        {
            if (palette != null) return heavy ? palette.hostileHeavy : palette.hostilePrimary;
            return heavy ? new Color(1f, 0.42f, 0.12f) : new Color(1f, 0.18f, 0.34f);
        }
    }
}
