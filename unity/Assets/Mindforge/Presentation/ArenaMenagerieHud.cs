using System.Text;
using UnityEngine;
using Mindforge.Journey;
using Mindforge.World;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Tiny demo-only identity strip for the Menagerie Crucible. It names only the enemies
    /// currently alive in the active wave so players can attach names to silhouettes. It is
    /// presentation-only and intentionally does not expose health bars, timing internals,
    /// neural evidence, or any input surface.
    /// </summary>
    public sealed class ArenaMenagerieHud : MonoBehaviour
    {
        [SerializeField] private ArenaMenagerieDirector director;
        private GUIStyle _header;
        private GUIStyle _roles;
        private Texture2D _panel;
        private readonly StringBuilder _buffer = new StringBuilder(128);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            ArenaMenagerieDirector director = Object.FindObjectOfType<ArenaMenagerieDirector>(true);
            if (director == null || director.GetComponent<ArenaMenagerieHud>() != null) return;
            ArenaMenagerieHud hud = director.gameObject.AddComponent<ArenaMenagerieHud>();
            hud.director = director;
        }

        private void Awake()
        {
            if (director == null) director = GetComponent<ArenaMenagerieDirector>();
        }

        private void OnGUI()
        {
            if (director == null || !director.Started) return;
            EnsureStyles();

            float width = Mathf.Min(470f, Screen.width - 36f);
            float x = (Screen.width - width) * 0.5f;
            Rect panel = new Rect(x, 14f, width, director.Complete ? 48f : 66f);
            GUI.DrawTexture(panel, _panel, ScaleMode.StretchToFill);

            if (director.Complete)
            {
                GUI.Label(new Rect(x + 12f, 22f, width - 24f, 28f), "MENAGERIE CRUCIBLE · CLEAR", _header);
                return;
            }

            GUI.Label(
                new Rect(x + 12f, 19f, width - 24f, 24f),
                $"MENAGERIE CRUCIBLE · WAVE {director.WaveIndex + 1}/{Mathf.Max(1, director.WaveCount)}",
                _header);
            GUI.Label(new Rect(x + 12f, 42f, width - 24f, 20f), BuildActiveRoleLine(), _roles);
        }

        private string BuildActiveRoleLine()
        {
            _buffer.Length = 0;
            JourneyEnemyController[] enemies = Object.FindObjectsOfType<JourneyEnemyController>(true);
            for (int i = 0; i < enemies.Length; i++)
            {
                JourneyEnemyController enemy = enemies[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy || !enemy.IsAlive) continue;
                if (!enemy.name.StartsWith("Menagerie_")) continue;
                if (_buffer.Length > 0) _buffer.Append("   ·   ");
                _buffer.Append(ReadableName(enemy.name.Substring("Menagerie_".Length)));
            }
            return _buffer.Length > 0 ? _buffer.ToString() : "SIGNAL QUIET · NEXT WAVE FORMING";
        }

        private static string ReadableName(string compact)
        {
            if (string.IsNullOrEmpty(compact)) return "UNKNOWN SIGNAL";
            StringBuilder text = new StringBuilder(compact.Length + 8);
            for (int i = 0; i < compact.Length; i++)
            {
                char c = compact[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(compact[i - 1])) text.Append(' ');
                text.Append(char.ToUpperInvariant(c));
            }
            return text.ToString();
        }

        private void EnsureStyles()
        {
            if (_panel == null)
            {
                _panel = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "MenagerieHudPanel",
                    hideFlags = HideFlags.HideAndDontSave,
                };
                _panel.SetPixel(0, 0, new Color(0.018f, 0.026f, 0.045f, 0.82f));
                _panel.Apply(false, true);
            }

            if (_header == null)
            {
                _header = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                };
                _header.normal.textColor = new Color(0.72f, 0.93f, 1f, 0.96f);
            }

            if (_roles == null)
            {
                _roles = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    clipping = TextClipping.Clip,
                };
                _roles.normal.textColor = new Color(0.92f, 0.94f, 1f, 0.88f);
            }
        }

        private void OnDestroy()
        {
            if (_panel != null) Destroy(_panel);
        }
    }
}
