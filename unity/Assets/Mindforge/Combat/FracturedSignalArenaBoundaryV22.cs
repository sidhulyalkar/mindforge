using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Turns the V0.11/V0.21 Fractured Signal wall ring into a deliberate boss-room boundary.
    ///
    /// The original arena authored fourteen nearly contiguous wall segments, including the
    /// south approach. V0.21 enlarged that ring but did not create an actual doorway or a fight
    /// seal. With a double-jump/air-capable Guardian this made entry and escape feel accidental.
    ///
    /// V0.22 creates one authored south doorway, raises the remaining low wall ring, and adds an
    /// opaque stone portcullis that closes behind the Guardian only after encounter release and
    /// reopens when the boss dies. The gate is ordinary world collision, never neural authority.
    /// </summary>
    [DefaultExecutionOrder(-91)]
    [RequireComponent(typeof(FracturedSignalDirector))]
    [RequireComponent(typeof(CombatantVitals))]
    public sealed class FracturedSignalArenaBoundaryV22 : MonoBehaviour
    {
        private const string ArenaName = "V11_Fractured_Signal_Arena";
        private const string GateName = "V22_Arena_Entrance_Gate";
        private const float SouthDoorZ = 75.70f;
        private const float FloorTopY = 4.08f;

        [Header("Encounter boundary")]
        [SerializeField] private float encounterReleaseZ = 82f;
        [SerializeField] private float wallHeight = 7.8f;
        [SerializeField] private float doorwayHalfWidth = 5.0f;
        [SerializeField] private float gateTravelSpeed = 9.5f;
        [SerializeField] private float gateClosedCenterY = 7.45f;
        [SerializeField] private float gateOpenCenterY = 15.35f;

        private readonly List<Transform> _bars = new List<Transform>(11);
        private CombatantVitals _vitals;
        private Transform _guardian;
        private BoxCollider _sealCollider;
        private Material _wallMaterial;
        private bool _encounterEntered;
        private bool _built;

        public bool Sealed => _sealCollider != null && _sealCollider.enabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            FracturedSignalDirector[] bosses = FindObjectsOfType<FracturedSignalDirector>(true);
            for (int i = 0; i < bosses.Length; i++)
            {
                FracturedSignalDirector boss = bosses[i];
                if (boss != null && boss.GetComponent<FracturedSignalArenaBoundaryV22>() == null)
                    boss.gameObject.AddComponent<FracturedSignalArenaBoundaryV22>();
            }
        }

        private void Awake()
        {
            _vitals = GetComponent<CombatantVitals>();
        }

        private void Start()
        {
            ResolveGuardian();
            BuildBoundary();
            if (_vitals != null) _vitals.Died += OnBossDied;
        }

        private void OnDestroy()
        {
            if (_vitals != null) _vitals.Died -= OnBossDied;
        }

        private void FixedUpdate()
        {
            if (!_built) BuildBoundary();
            ResolveGuardian();
            if (!_built || _guardian == null || _vitals == null) return;

            if (!_encounterEntered && _guardian.position.z >= encounterReleaseZ && _vitals.IsAlive)
            {
                _encounterEntered = true;
                Debug.Log("[Mindforge:BossV22] Fractured Signal chamber sealed behind the Guardian.");
            }

            bool shouldClose = _encounterEntered && _vitals.IsAlive;
            MoveGate(shouldClose ? gateClosedCenterY : gateOpenCenterY);
        }

        private void ResolveGuardian()
        {
            if (_guardian != null) return;
            GuardianCombatInput input = FindObjectOfType<GuardianCombatInput>(true);
            if (input != null) _guardian = input.transform;
        }

        private void BuildBoundary()
        {
            if (_built) return;
            GameObject arenaObject = GameObject.Find(ArenaName);
            if (arenaObject == null) return;
            Transform arena = arenaObject.transform;

            // Segment 07 is the exact south radial segment (angle = PI) in the 14-piece ring.
            // Remove it to make a real route-aligned door, then make the remaining arena wall
            // tall enough to communicate a chamber boundary to an aerial Guardian.
            for (int i = 0; i < arena.childCount; i++)
            {
                Transform child = arena.GetChild(i);
                if (child == null || !child.name.StartsWith("FractureWall_", StringComparison.Ordinal)) continue;
                string suffix = child.name.Substring("FractureWall_".Length);
                if (!int.TryParse(suffix, out int segment)) continue;

                Renderer renderer = child.GetComponent<Renderer>();
                if (_wallMaterial == null && renderer != null && renderer.sharedMaterial != null)
                    _wallMaterial = renderer.sharedMaterial;

                if (segment == 7)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }

                Vector3 scale = child.localScale;
                scale.y = Mathf.Max(scale.y, wallHeight);
                child.localScale = scale;
                Vector3 position = child.position;
                position.y = FloorTopY + scale.y * 0.5f;
                child.position = position;
            }

            Transform existing = arena.Find(GateName);
            if (existing != null) Destroy(existing.gameObject);

            GameObject gate = new GameObject(GateName);
            gate.transform.SetParent(arena, false);
            gate.transform.position = new Vector3(0f, 0f, SouthDoorZ);

            BuildGateFrame(gate.transform);
            BuildPortcullis(gate.transform);
            BuildSealCollider(gate.transform);
            MoveGateImmediate(gateOpenCenterY);
            _built = true;
        }

        private void BuildGateFrame(Transform parent)
        {
            float frameHeight = Mathf.Max(7.8f, wallHeight);
            float centerY = FloorTopY + frameHeight * 0.5f;
            CreateStoneBlock("GatePillarL", parent,
                new Vector3(-doorwayHalfWidth - 0.58f, centerY, 0f),
                new Vector3(1.18f, frameHeight, 1.55f), true);
            CreateStoneBlock("GatePillarR", parent,
                new Vector3(doorwayHalfWidth + 0.58f, centerY, 0f),
                new Vector3(1.18f, frameHeight, 1.55f), true);
            CreateStoneBlock("GateLintel", parent,
                new Vector3(0f, FloorTopY + frameHeight - 0.48f, 0f),
                new Vector3(doorwayHalfWidth * 2f + 2.35f, 0.96f, 1.62f), true);
        }

        private void BuildPortcullis(Transform parent)
        {
            const int count = 9;
            float span = doorwayHalfWidth * 1.72f;
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(-span * 0.5f, span * 0.5f, i / (float)(count - 1));
                Transform bar = CreateStoneBlock($"GateBar_{i:00}", parent,
                    new Vector3(x, gateOpenCenterY, 0.02f),
                    new Vector3(0.48f, 6.55f, 0.64f), false);
                Collider cosmeticCollider = bar.GetComponent<Collider>();
                if (cosmeticCollider != null) cosmeticCollider.enabled = false;
                _bars.Add(bar);
            }

            // Crossbars visually explain the single full-width collision plane used below.
            for (int i = 0; i < 2; i++)
            {
                Transform cross = CreateStoneBlock($"GateCrossbar_{i:00}", parent,
                    new Vector3(0f, gateOpenCenterY + (i == 0 ? -1.55f : 1.55f), 0.05f),
                    new Vector3(doorwayHalfWidth * 1.84f, 0.34f, 0.72f), false);
                Collider cosmeticCollider = cross.GetComponent<Collider>();
                if (cosmeticCollider != null) cosmeticCollider.enabled = false;
                _bars.Add(cross);
            }
        }

        private void BuildSealCollider(Transform parent)
        {
            GameObject seal = new GameObject("GateSealCollision");
            seal.transform.SetParent(parent, false);
            seal.transform.localPosition = new Vector3(0f, gateClosedCenterY, 0f);
            _sealCollider = seal.AddComponent<BoxCollider>();
            _sealCollider.size = new Vector3(doorwayHalfWidth * 1.90f, 6.65f, 0.78f);
            _sealCollider.enabled = false;
        }

        private Transform CreateStoneBlock(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            bool keepCollider)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = localScale;
            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null && _wallMaterial != null) renderer.sharedMaterial = _wallMaterial;
            Collider collider = block.GetComponent<Collider>();
            if (!keepCollider && collider != null) collider.enabled = false;
            return block.transform;
        }

        private void MoveGate(float targetCenterY)
        {
            if (_bars.Count == 0) return;
            float currentY = _bars[0].localPosition.y;
            float nextY = Mathf.MoveTowards(currentY, targetCenterY, gateTravelSpeed * Time.fixedDeltaTime);
            float deltaY = nextY - currentY;
            for (int i = 0; i < _bars.Count; i++)
            {
                Transform part = _bars[i];
                if (part == null) continue;
                Vector3 p = part.localPosition;
                p.y += deltaY;
                part.localPosition = p;
            }

            if (_sealCollider != null)
                _sealCollider.enabled = Mathf.Abs(nextY - gateClosedCenterY) <= 0.18f && _encounterEntered && _vitals != null && _vitals.IsAlive;
        }

        private void MoveGateImmediate(float centerY)
        {
            if (_bars.Count == 0) return;
            float delta = centerY - _bars[0].localPosition.y;
            for (int i = 0; i < _bars.Count; i++)
            {
                Transform part = _bars[i];
                if (part == null) continue;
                Vector3 p = part.localPosition;
                p.y += delta;
                part.localPosition = p;
            }
            if (_sealCollider != null) _sealCollider.enabled = false;
        }

        private void OnBossDied()
        {
            _encounterEntered = false;
            if (_sealCollider != null) _sealCollider.enabled = false;
            Debug.Log("[Mindforge:BossV22] Fractured Signal defeated; chamber entrance reopened.");
        }
    }
}
