using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Truthful presentation for FracturedSignalMeleeDirector. The wedge/radius comes
    /// directly from the authority event and remains visible until that same attack
    /// resolves. This component has no damage or timing authority.
    /// </summary>
    public sealed class FracturedSignalMeleePresentation : MonoBehaviour
    {
        [SerializeField] private FracturedSignalMeleeDirector melee;

        private GameObject _root;
        private LineRenderer[] _rays;
        private LineRenderer _arc;
        private LineRenderer _ring;
        private Material _material;
        private string _pattern;
        private Vector3 _direction;
        private float _range;
        private float _arcDegrees;
        private bool _heavy;
        private bool _active;
        private float _phase;

        private static readonly Color LightThreat = new Color(1f, 0.12f, 0.30f, 0.90f);
        private static readonly Color HeavyThreat = new Color(1f, 0.42f, 0.08f, 0.95f);

        private void OnEnable()
        {
            Resolve();
            Subscribe();
        }

        private void OnDisable()
        {
            if (melee != null)
            {
                melee.MeleeTelegraphed -= OnTelegraph;
                melee.MeleeResolved -= OnResolved;
            }
            Clear();
        }

        private void Update()
        {
            if (melee == null)
            {
                Resolve();
                Subscribe();
            }
            if (!_active || _root == null) return;

            _phase += Time.unscaledDeltaTime * (_heavy ? 10f : 7f);
            float pulse = 0.72f + (0.5f + 0.5f * Mathf.Sin(_phase)) * 0.28f;
            Color baseColor = _heavy ? HeavyThreat : LightThreat;
            Color color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * pulse);
            Draw(color);
        }

        private void Resolve()
        {
            if (melee == null) melee = FindObjectOfType<FracturedSignalMeleeDirector>(true);
            if (_root == null) Build();
        }

        private void Subscribe()
        {
            if (melee == null) return;
            melee.MeleeTelegraphed -= OnTelegraph;
            melee.MeleeResolved -= OnResolved;
            melee.MeleeTelegraphed += OnTelegraph;
            melee.MeleeResolved += OnResolved;
        }

        private void Build()
        {
            if (_root != null) return;
            _material = CreateMaterial();
            _root = new GameObject("FracturedSignalMeleeTelegraph");

            _rays = new LineRenderer[9];
            for (int i = 0; i < _rays.Length; i++)
                _rays[i] = NewLine($"CleaveRay_{i:00}", 2, 0.032f);

            _arc = NewLine("CleaveArc", 32, 0.055f);
            _ring = NewLine("SlamRing", 72, 0.072f);
            _ring.loop = true;
            Clear();
        }

        private LineRenderer NewLine(string name, int points, float width)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = _material;
            line.useWorldSpace = true;
            line.positionCount = points;
            line.widthMultiplier = width;
            line.numCornerVertices = 3;
            line.numCapVertices = 3;
            return line;
        }

        private void OnTelegraph(string pattern, Vector3 direction, float range, float arcDegrees, bool heavy)
        {
            _pattern = (pattern ?? string.Empty).ToUpperInvariant();
            _direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (_direction.sqrMagnitude < 0.001f) _direction = Vector3.forward;
            _direction.Normalize();
            _range = Mathf.Max(0.1f, range);
            _arcDegrees = Mathf.Clamp(arcDegrees, 1f, 360f);
            _heavy = heavy;
            _active = true;
            _phase = 0f;
            if (_root != null) _root.SetActive(true);
            Draw(_heavy ? HeavyThreat : LightThreat);
        }

        private void OnResolved(string pattern, string outcome, float damage)
        {
            Clear();
        }

        private void Draw(Color color)
        {
            if (!_active || melee == null) return;
            Vector3 origin = melee.transform.position + Vector3.up * 0.07f;

            bool slam = _pattern == "SLAM";
            for (int i = 0; i < _rays.Length; i++)
                if (_rays[i] != null) _rays[i].gameObject.SetActive(!slam);
            if (_arc != null) _arc.gameObject.SetActive(!slam);
            if (_ring != null) _ring.gameObject.SetActive(slam);

            if (slam)
            {
                _ring.startColor = color;
                _ring.endColor = color;
                for (int i = 0; i < _ring.positionCount; i++)
                {
                    float a = i / (float)_ring.positionCount * Mathf.PI * 2f;
                    _ring.SetPosition(i, origin + new Vector3(Mathf.Cos(a) * _range, 0f, Mathf.Sin(a) * _range));
                }
                return;
            }

            for (int i = 0; i < _rays.Length; i++)
            {
                float t = _rays.Length <= 1 ? 0.5f : i / (float)(_rays.Length - 1);
                float angle = Mathf.Lerp(-_arcDegrees * 0.5f, _arcDegrees * 0.5f, t);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * _direction;
                _rays[i].startColor = new Color(color.r, color.g, color.b, color.a * (i == 0 || i == _rays.Length - 1 ? 1f : 0.40f));
                _rays[i].endColor = _rays[i].startColor;
                _rays[i].SetPosition(0, origin + direction * 0.34f);
                _rays[i].SetPosition(1, origin + direction * _range);
            }

            _arc.startColor = color;
            _arc.endColor = color;
            for (int i = 0; i < _arc.positionCount; i++)
            {
                float t = _arc.positionCount <= 1 ? 0.5f : i / (float)(_arc.positionCount - 1);
                float angle = Mathf.Lerp(-_arcDegrees * 0.5f, _arcDegrees * 0.5f, t);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * _direction;
                _arc.SetPosition(i, origin + direction * _range);
            }
        }

        private void Clear()
        {
            _active = false;
            if (_root != null) _root.SetActive(false);
        }

        private static Material CreateMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            return new Material(shader) { name = "MindforgeMeleeTelegraphMaterial" };
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root);
            if (_material != null) Destroy(_material);
        }
    }
}
