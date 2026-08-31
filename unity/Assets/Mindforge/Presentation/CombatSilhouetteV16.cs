using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Small collider-free shape accents that keep the Guardian and current enemy target
    /// readable against the world at normal camera distance. This is not a replacement for
    /// production character art; it is a deterministic fallback layer for the demo build.
    /// No periodic luminance animation is used, and one-time construction waits until no
    /// neural evidence epoch owns the visual field.
    /// </summary>
    public sealed class CombatSilhouetteV16 : MonoBehaviour
    {
        private Transform _guardian;
        private GuardianTargetLock _targetLock;
        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private Transform _lastEnemy;
        private GameObject _guardianRoot;
        private GameObject _enemyRoot;
        private readonly List<Material> _materials = new List<Material>(5);
        private Material _guardianSteel;
        private Material _guardianIvory;
        private Material _guardianDark;
        private Material _enemyShell;
        private Material _enemyTrim;
        private bool _ready;

        public void Configure(
            Transform guardian,
            GuardianTargetLock targetLock,
            AwakeningCalibrationDirector calibration,
            SoulWispController wisp)
        {
            _guardian = guardian;
            _targetLock = targetLock;
            _calibration = calibration;
            _wisp = wisp;
        }

        private IEnumerator Start()
        {
            while (NeuralEvidenceOwnsVisualField()) yield return null;
            CreateMaterials();
            BuildGuardianAccents();
            RefreshEnemyAccents(true);
            _ready = true;
        }

        private void Update()
        {
            if (!_ready || NeuralEvidenceOwnsVisualField()) return;
            RefreshEnemyAccents(false);
        }

        private bool NeuralEvidenceOwnsVisualField()
        {
            if (_calibration == null) _calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            if (_wisp == null) _wisp = FindObjectOfType<SoulWispController>(true);
            return (_calibration != null && _calibration.CalibrationInProgress) ||
                   (_wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive));
        }

        private void CreateMaterials()
        {
            _guardianSteel = CreateLit("V16_GuardianSteel", new Color(0.36f, 0.42f, 0.49f), 0.78f, 0.62f);
            _guardianIvory = CreateLit("V16_GuardianIvory", new Color(0.66f, 0.67f, 0.64f), 0.38f, 0.48f);
            _guardianDark = CreateLit("V16_GuardianDark", new Color(0.045f, 0.060f, 0.082f), 0.55f, 0.40f);
            _enemyShell = CreateLit("V16_EnemyShell", new Color(0.095f, 0.055f, 0.12f), 0.52f, 0.36f);
            _enemyTrim = CreateLit("V16_EnemyTrim", new Color(0.46f, 0.10f, 0.22f), 0.42f, 0.48f);
        }

        private void BuildGuardianAccents()
        {
            if (_guardian == null || _guardian.Find("GuardianReadabilityV16") != null) return;
            _guardianRoot = new GameObject("GuardianReadabilityV16");
            _guardianRoot.transform.SetParent(_guardian, false);

            AddPart("GuardianReadabilityChest", PrimitiveType.Cube, _guardianRoot.transform,
                new Vector3(0f, 1.18f, 0.01f), new Vector3(0.68f, 0.52f, 0.34f), _guardianSteel, Vector3.zero);
            AddPart("GuardianReadabilityChestInset", PrimitiveType.Cube, _guardianRoot.transform,
                new Vector3(0f, 1.19f, 0.185f), new Vector3(0.38f, 0.27f, 0.035f), _guardianIvory, Vector3.zero);
            AddPart("GuardianReadabilityShoulderL", PrimitiveType.Sphere, _guardianRoot.transform,
                new Vector3(-0.48f, 1.34f, 0f), new Vector3(0.33f, 0.24f, 0.36f), _guardianIvory, new Vector3(0f, 0f, -8f));
            AddPart("GuardianReadabilityShoulderR", PrimitiveType.Sphere, _guardianRoot.transform,
                new Vector3(0.48f, 1.34f, 0f), new Vector3(0.33f, 0.24f, 0.36f), _guardianIvory, new Vector3(0f, 0f, 8f));
            AddPart("GuardianReadabilityHelmet", PrimitiveType.Sphere, _guardianRoot.transform,
                new Vector3(0f, 1.82f, 0f), new Vector3(0.39f, 0.33f, 0.40f), _guardianSteel, Vector3.zero);
            AddPart("GuardianReadabilityCrest", PrimitiveType.Cube, _guardianRoot.transform,
                new Vector3(0f, 2.12f, -0.03f), new Vector3(0.07f, 0.48f, 0.30f), _guardianIvory, new Vector3(-8f, 0f, 0f));
            AddPart("GuardianReadabilityWaist", PrimitiveType.Cube, _guardianRoot.transform,
                new Vector3(0f, 0.77f, 0f), new Vector3(0.50f, 0.22f, 0.28f), _guardianDark, Vector3.zero);
        }

        private void RefreshEnemyAccents(bool force)
        {
            Transform enemy = ResolveEnemy();
            if (!force && enemy == _lastEnemy) return;
            _lastEnemy = enemy;

            if (_enemyRoot != null) Destroy(_enemyRoot);
            _enemyRoot = null;
            if (enemy == null) return;
            if (enemy.Find("EnemyReadabilityV16") != null) return;

            _enemyRoot = new GameObject("EnemyReadabilityV16");
            _enemyRoot.transform.SetParent(enemy, false);

            AddPart("EnemyReadabilityShardL", PrimitiveType.Cube, _enemyRoot.transform,
                new Vector3(-0.78f, 1.08f, 0.02f), new Vector3(0.24f, 1.36f, 0.28f), _enemyShell, new Vector3(0f, 0f, -24f));
            AddPart("EnemyReadabilityShardR", PrimitiveType.Cube, _enemyRoot.transform,
                new Vector3(0.72f, 1.22f, -0.08f), new Vector3(0.22f, 1.58f, 0.25f), _enemyShell, new Vector3(8f, 0f, 29f));
            AddPart("EnemyReadabilityShardRear", PrimitiveType.Cube, _enemyRoot.transform,
                new Vector3(0.05f, 1.55f, -0.58f), new Vector3(0.20f, 1.10f, 0.22f), _enemyTrim, new Vector3(18f, 22f, -7f));
            AddPart("EnemyReadabilityCoreFrame", PrimitiveType.Cube, _enemyRoot.transform,
                new Vector3(0f, 0.90f, 0.12f), new Vector3(0.70f, 0.12f, 0.48f), _enemyTrim, new Vector3(0f, 22f, 0f));
        }

        private Transform ResolveEnemy()
        {
            if (_targetLock != null && _targetLock.Target != null) return _targetLock.Target;

            CombatantVitals[] vitals = FindObjectsOfType<CombatantVitals>(true);
            CombatantVitals strongest = null;
            for (int i = 0; i < vitals.Length; i++)
            {
                CombatantVitals candidate = vitals[i];
                if (candidate == null || candidate.Team != CombatTeam.Enemy || !candidate.IsAlive || !candidate.gameObject.activeInHierarchy)
                    continue;
                if (strongest == null || candidate.MaxHealth > strongest.MaxHealth) strongest = candidate;
            }
            return strongest != null ? strongest.transform : null;
        }

        private Material CreateLit(string name, Color color, float metallic, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            _materials.Add(material);
            return material;
        }

        private static GameObject AddPart(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Vector3 localEuler)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEuler);
            part.transform.localScale = localScale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            return part;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _materials.Count; i++)
                if (_materials[i] != null) Destroy(_materials[i]);
        }
    }
}
