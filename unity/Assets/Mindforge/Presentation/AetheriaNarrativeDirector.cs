using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only world title/transmission layer. Story beats are proximity-read
    /// from the existing Guardian transform and never gate, pause, spawn, move, damage, or
    /// touch neural evidence. The antagonist stays serious; character motion supplies humor.
    /// </summary>
    public sealed class AetheriaNarrativeDirector : MonoBehaviour
    {
        [SerializeField] private Transform guardian;
        [SerializeField] private float cardSeconds = 4.2f;

        private readonly float[] _z = { -56f, -44f, -29f, -10f, 6.5f, 18f };
        private readonly float[] _radius = { 12f, 8f, 8f, 8f, 8f, 9f };
        private readonly string[] _titles =
        {
            "PRISM BASTION",
            "THE NEON CAUSEWAY",
            "MARKET OF BROKEN MOMENTUM",
            "CHOIR OF RUINED TOWERS",
            "HALL OF EXCESSIVE GRAVITAS",
            "MENAGERIE CRUCIBLE",
        };
        private readonly string[] _subtitles =
        {
            "Last guild signal detected. Knight posture: regrettably enthusiastic.",
            "Aetheria's bridge engines are still beating beneath the occupation.",
            "Stolen momentum drives, RGB salvage, and one extremely offended machine army.",
            "The old harmonic towers still answer movement with light.",
            "MALATRACT // Motion is error. I will grant this realm the mercy of stillness.",
            "Ten cyber-mythic combat identities. Three waves. No dignity guaranteed.",
        };

        private bool[] _visited;
        private int _active = -1;
        private float _cardUntil;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;

        private void Awake()
        {
            ResolveGuardian();
            _visited = new bool[_titles.Length];
        }

        private void Update()
        {
            ResolveGuardian();
            if (guardian == null) return;

            for (int i = 0; i < _titles.Length; i++)
            {
                if (_visited[i]) continue;
                if (Mathf.Abs(guardian.position.z - _z[i]) > _radius[i]) continue;
                _visited[i] = true;
                _active = i;
                _cardUntil = Time.unscaledTime + Mathf.Max(1.5f, cardSeconds);
                break;
            }
        }

        private void OnGUI()
        {
            if (_active < 0 || _active >= _titles.Length || Time.unscaledTime >= _cardUntil) return;
            EnsureStyles();

            float width = Mathf.Min(760f, Screen.width * 0.76f);
            float x = (Screen.width - width) * 0.5f;
            Rect titleRect = new Rect(x, 42f, width, 38f);
            Rect subtitleRect = new Rect(x + 24f, 80f, width - 48f, 58f);
            GUI.Label(titleRect, _titles[_active], _titleStyle);
            GUI.Label(subtitleRect, _subtitles[_active], _subtitleStyle);
        }

        private void ResolveGuardian()
        {
            if (guardian != null) return;
            GameObject go = GameObject.Find("Guardian");
            if (go != null) guardian = go.transform;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.88f, 0.96f, 1f) },
            };
            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.72f, 0.80f, 0.92f) },
            };
        }
    }
}
