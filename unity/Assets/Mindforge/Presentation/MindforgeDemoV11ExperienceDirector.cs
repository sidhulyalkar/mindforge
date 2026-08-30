using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only authored experience layer for the clean V0.11 route.
    ///
    /// The director reads Guardian position, existing enemy vitality and boss phase to shape
    /// atmosphere, landmark emphasis and visual enemy silhouettes. It never creates attacks,
    /// changes damage, schedules combat, changes target lock, emits neural events, alters BCI
    /// stimuli or mutates persistence. Neural-hardware builds keep the conservative static
    /// lighting authored by MindforgeDemoV11Builder; the richer spatial atmosphere is enabled
    /// only for the explicitly labelled controller-only presentation demo.
    /// </summary>
    [DefaultExecutionOrder(700)]
    public sealed class MindforgeDemoV11ExperienceDirector : MonoBehaviour
    {
        private readonly struct DistrictProfile
        {
            public readonly string id;
            public readonly float maxZ;
            public readonly Color ambient;
            public readonly Color fog;
            public readonly float fogStart;
            public readonly float fogEnd;

            public DistrictProfile(
                string profileId,
                float maximumZ,
                Color ambientColor,
                Color fogColor,
                float start,
                float end)
            {
                id = profileId;
                maxZ = maximumZ;
                ambient = ambientColor;
                fog = fogColor;
                fogStart = start;
                fogEnd = end;
            }
        }

        private sealed class AccentBinding
        {
            public Renderer renderer;
            public Color emission;
            public float radius;
            public float baseIntensity;
            public float focusIntensity;
            public MaterialPropertyBlock block;
        }

        private static readonly DistrictProfile[] Districts =
        {
            new DistrictProfile(
                "sanctum", -2f,
                new Color(0.27f, 0.30f, 0.34f),
                new Color(0.17f, 0.21f, 0.24f),
                62f, 175f),
            new DistrictProfile(
                "causeway", 32f,
                new Color(0.20f, 0.27f, 0.31f),
                new Color(0.11f, 0.18f, 0.22f),
                54f, 166f),
            new DistrictProfile(
                "market", 58f,
                new Color(0.29f, 0.27f, 0.23f),
                new Color(0.20f, 0.19f, 0.17f),
                50f, 154f),
            new DistrictProfile(
                "ascent", 83f,
                new Color(0.21f, 0.23f, 0.29f),
                new Color(0.13f, 0.16f, 0.22f),
                48f, 148f),
            new DistrictProfile(
                "fracture", float.PositiveInfinity,
                new Color(0.25f, 0.19f, 0.22f),
                new Color(0.18f, 0.11f, 0.14f),
                44f, 138f),
        };

        private readonly List<AccentBinding> _accents = new List<AccentBinding>(12);
        private MindforgeDemoV11Marker _marker;
        private Transform _guardian;
        private FracturedSignalDirector _boss;
        private CombatantVitals _bossVitals;
        private MindforgeDemoV11BossStaging _bossStaging;
        private Light _sanctumLight;
        private Light _marketLight;
        private Light _fractureLight;
        private int _district = -1;
        private bool _controllerPresentation;
        private bool _ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            MindforgeDemoV11Marker marker = UnityEngine.Object.FindObjectOfType<MindforgeDemoV11Marker>(true);
            if (marker == null || marker.GetComponent<MindforgeDemoV11ExperienceDirector>() != null) return;
            marker.gameObject.AddComponent<MindforgeDemoV11ExperienceDirector>();
        }

        private IEnumerator Start()
        {
            _marker = GetComponent<MindforgeDemoV11Marker>();
            _controllerPresentation = _marker != null && _marker.ControllerOnlyByDefault;

            GuardianCombatInput input = null;
            for (int frame = 0; frame < 180; frame++)
            {
                if (input == null) input = UnityEngine.Object.FindObjectOfType<GuardianCombatInput>(true);
                if (_boss == null) _boss = UnityEngine.Object.FindObjectOfType<FracturedSignalDirector>(true);
                if (input != null && _boss != null) break;
                yield return null;
            }

            if (input == null || _boss == null)
            {
                Debug.LogError("[Mindforge:V11Experience] Missing Guardian or Fractured Signal; authored presentation not installed.");
                yield break;
            }

            _guardian = input.transform;
            _bossVitals = _boss.GetComponent<CombatantVitals>();

            // Allow the base V0.11 runtime one frame to install its compatibility visuals.
            // We then replace only the Echo presentation shells, never their gameplay roots.
            yield return null;
            yield return null;

            InstallEchoArchetypes();
            InstallBossStaging();
            BindLandmarkAccents();
            BuildAccentLights();
            UpdateDistrict(true);
            _ready = true;

            Debug.Log(
                "[Mindforge:V11Experience] Authored presentation active: district atmosphere, " +
                "proximity-reactive landmarks, three visual Echo archetypes and phase-readable boss staging. " +
                (_controllerPresentation
                    ? "Controller-only spatial atmosphere enabled."
                    : "Neural-hardware conservative lighting retained."));
        }

        private void Update()
        {
            if (!_ready || _guardian == null) return;
            UpdateDistrict(false);
            UpdateSpatialAtmosphere();
            UpdateLandmarkAccents();
            UpdateAccentLights();
        }

        private void UpdateDistrict(bool force)
        {
            int next = DistrictIndexFor(_guardian.position.z);
            if (!force && next == _district) return;
            _district = next;
            Debug.Log($"[Mindforge:V11Experience] Entered district profile '{Districts[_district].id}'.");
        }

        private static int DistrictIndexFor(float z)
        {
            for (int i = 0; i < Districts.Length; i++)
                if (z < Districts[i].maxZ) return i;
            return Districts.Length - 1;
        }

        private void UpdateSpatialAtmosphere()
        {
            // Do not introduce global luminance changes into the neural-hardware presentation.
            // The static builder lighting remains authoritative for that review path.
            if (!_controllerPresentation || _district < 0) return;

            DistrictProfile profile = Districts[_district];
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float blend = 1f - Mathf.Exp(-2.2f * dt);
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, profile.ambient, blend);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, profile.fog, blend);
            RenderSettings.fogStartDistance = Mathf.Lerp(RenderSettings.fogStartDistance, profile.fogStart, blend);
            RenderSettings.fogEndDistance = Mathf.Lerp(RenderSettings.fogEndDistance, profile.fogEnd, blend);
        }

        private void BindLandmarkAccents()
        {
            _accents.Clear();
            BindAccent("MemoryForgeCore", new Color(0.16f, 0.76f, 1f), 14f, 1.7f, 4.0f);
            BindAccent("CausewayAetherSpine", new Color(0.12f, 0.64f, 1f), 18f, 1.35f, 2.6f);
            BindAccent("MarketSignalOrb", new Color(0.20f, 0.84f, 1f), 15f, 1.6f, 4.4f);
            BindAccent("AscentAetherGuide", new Color(0.18f, 0.72f, 1f), 17f, 1.3f, 2.8f);
            BindAccent("SkylineAetherBeacon", new Color(0.20f, 0.82f, 1f), 36f, 1.4f, 2.2f);
            for (int i = 0; i < 4; i++)
                BindAccent($"FractureSpire_{i}", new Color(1f, 0.10f, 0.28f), 20f, 1.25f, 3.2f);
        }

        private void BindAccent(string objectName, Color emission, float radius, float baseIntensity, float focusIntensity)
        {
            Renderer renderer = FindSceneRenderer(objectName);
            if (renderer == null) return;
            _accents.Add(new AccentBinding
            {
                renderer = renderer,
                emission = emission,
                radius = Mathf.Max(1f, radius),
                baseIntensity = Mathf.Max(0f, baseIntensity),
                focusIntensity = Mathf.Max(baseIntensity, focusIntensity),
                block = new MaterialPropertyBlock(),
            });
        }

        private void UpdateLandmarkAccents()
        {
            if (_accents.Count == 0 || _guardian == null) return;
            Vector3 guardianPosition = _guardian.position;
            for (int i = 0; i < _accents.Count; i++)
            {
                AccentBinding accent = _accents[i];
                if (accent == null || accent.renderer == null) continue;

                // Spatially reactive rather than periodic. No sine/flicker clock is used.
                float distance = Vector3.Distance(guardianPosition, accent.renderer.bounds.center);
                float proximity = _controllerPresentation
                    ? 1f - Mathf.SmoothStep(accent.radius * 0.22f, accent.radius, distance)
                    : 0f;
                float intensity = Mathf.Lerp(accent.baseIntensity, accent.focusIntensity, proximity);
                accent.renderer.GetPropertyBlock(accent.block);
                accent.block.SetColor("_EmissionColor", accent.emission * intensity);
                accent.renderer.SetPropertyBlock(accent.block);
            }
        }

        private void BuildAccentLights()
        {
            _sanctumLight = EnsureAccentLight(
                "V11ExperienceLight_Sanctum", "MemoryForgeCore",
                new Color(0.18f, 0.72f, 1f), 8.5f);
            _marketLight = EnsureAccentLight(
                "V11ExperienceLight_Market", "MarketSignalOrb",
                new Color(0.20f, 0.78f, 1f), 7.0f);
            _fractureLight = EnsureAccentLight(
                "V11ExperienceLight_Fracture", "FractureInnerDais",
                new Color(1f, 0.10f, 0.25f), 10.5f);

            UpdateAccentLights();
        }

        private Light EnsureAccentLight(string lightName, string anchorName, Color color, float range)
        {
            GameObject existing = FindSceneObject(lightName);
            if (existing != null)
            {
                Light existingLight = existing.GetComponent<Light>();
                if (existingLight != null) return existingLight;
            }

            Renderer anchor = FindSceneRenderer(anchorName);
            if (anchor == null) return null;
            GameObject go = new GameObject(lightName);
            go.transform.SetParent(transform, true);
            go.transform.position = anchor.bounds.center + Vector3.up * 1.2f;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            return light;
        }

        private void UpdateAccentLights()
        {
            if (!_controllerPresentation || _guardian == null)
            {
                if (_sanctumLight != null) _sanctumLight.intensity = 0f;
                if (_marketLight != null) _marketLight.intensity = 0f;
                if (_fractureLight != null) _fractureLight.intensity = 0f;
                return;
            }

            float z = _guardian.position.z;
            SetLightIntensity(_sanctumLight, SpatialWeight(z, -16f, 24f) * 1.05f);
            SetLightIntensity(_marketLight, SpatialWeight(z, 49f, 24f) * 0.90f);
            SetLightIntensity(_fractureLight, SpatialWeight(z, 94f, 30f) * 1.15f);
        }

        private static float SpatialWeight(float value, float center, float width)
        {
            float half = Mathf.Max(1f, width * 0.5f);
            float distance = Mathf.Abs(value - center);
            return 1f - Mathf.SmoothStep(half * 0.20f, half, distance);
        }

        private static void SetLightIntensity(Light light, float value)
        {
            if (light != null) light.intensity = Mathf.Max(0f, value);
        }

        private void InstallEchoArchetypes()
        {
            FracturedEchoNode[] echoes = UnityEngine.Object.FindObjectsOfType<FracturedEchoNode>(true);
            Array.Sort(echoes, (a, b) => string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty));
            for (int i = 0; i < echoes.Length; i++)
            {
                FracturedEchoNode echo = echoes[i];
                if (echo == null || !echo.name.StartsWith("V11Echo_", StringComparison.Ordinal)) continue;
                MindforgeDemoV11EchoPresentation visual = echo.GetComponent<MindforgeDemoV11EchoPresentation>();
                if (visual == null) visual = echo.gameObject.AddComponent<MindforgeDemoV11EchoPresentation>();
                visual.Configure(Mathf.Clamp(i, 0, 2));
            }
        }

        private void InstallBossStaging()
        {
            if (_boss == null) return;
            _bossStaging = _boss.GetComponent<MindforgeDemoV11BossStaging>();
            if (_bossStaging == null) _bossStaging = _boss.gameObject.AddComponent<MindforgeDemoV11BossStaging>();
            _bossStaging.Configure(_boss, _bossVitals, _controllerPresentation);
        }

        private static Renderer FindSceneRenderer(string name)
        {
            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.gameObject.scene.IsValid()) continue;
                if (string.Equals(renderer.name, name, StringComparison.Ordinal)) return renderer;
            }
            return null;
        }

        private static GameObject FindSceneObject(string name)
        {
            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (t == null || !t.gameObject.scene.IsValid()) continue;
                if (string.Equals(t.name, name, StringComparison.Ordinal)) return t.gameObject;
            }
            return null;
        }
    }

    /// <summary>
    /// Replaces the three compatibility Echo shells with visually distinct silhouettes while
    /// preserving the exact FracturedEchoNode gameplay root, collider, vitality and fire cadence.
    /// The differences are visual progression only; they do not imply different mechanics yet.
    /// </summary>
    internal sealed class MindforgeDemoV11EchoPresentation : MonoBehaviour
    {
        private int _archetype = -1;

        public void Configure(int archetype)
        {
            int next = Mathf.Clamp(archetype, 0, 2);
            if (_archetype == next && transform.Find("V11EchoArchetype") != null) return;
            _archetype = next;
            DisableCompatibilityShell();
            Rebuild();
        }

        private void DisableCompatibilityShell()
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour == this) continue;
                if (string.Equals(behaviour.GetType().Name, "MindforgeDemoEchoV11", StringComparison.Ordinal))
                    behaviour.enabled = false;
            }

            Transform legacy = transform.Find("V11EchoVisual");
            if (legacy != null) Destroy(legacy.gameObject);
            Transform existing = transform.Find("V11EchoArchetype");
            if (existing != null) Destroy(existing.gameObject);
        }

        private void Rebuild()
        {
            Transform root = new GameObject("V11EchoArchetype").transform;
            root.SetParent(transform, false);

            if (_archetype == 0) BuildNeedle(root);
            else if (_archetype == 1) BuildBastion(root);
            else BuildChoir(root);
        }

        private static void BuildNeedle(Transform root)
        {
            Primitive("NeedleCore", PrimitiveType.Sphere, root, Vector3.zero, Vector3.one * 0.30f, RuntimeMaterialV11.Hostile);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f;
                Vector3 p = Quaternion.Euler(0f, angle, 0f) * new Vector3(0.42f, 0f, 0f);
                Primitive("NeedleFin_" + i, PrimitiveType.Cube, root, p,
                    new Vector3(0.09f, 0.62f, 0.16f),
                    i == 0 ? RuntimeMaterialV11.Gold : RuntimeMaterialV11.Dark,
                    new Vector3(0f, -angle, 24f));
            }
        }

        private static void BuildBastion(Transform root)
        {
            Primitive("BastionCore", PrimitiveType.Sphere, root, Vector3.zero, Vector3.one * 0.40f, RuntimeMaterialV11.Hostile);
            Primitive("BastionBody", PrimitiveType.Cube, root, Vector3.zero, new Vector3(0.66f, 0.66f, 0.66f), RuntimeMaterialV11.Dark, new Vector3(18f, 32f, 8f));
            Primitive("BastionShieldL", PrimitiveType.Cube, root, new Vector3(-0.68f, 0f, 0f), new Vector3(0.17f, 0.82f, 0.64f), RuntimeMaterialV11.Gold, new Vector3(0f, 0f, -10f));
            Primitive("BastionShieldR", PrimitiveType.Cube, root, new Vector3(0.68f, 0f, 0f), new Vector3(0.17f, 0.82f, 0.64f), RuntimeMaterialV11.Gold, new Vector3(0f, 0f, 10f));
        }

        private static void BuildChoir(Transform root)
        {
            Primitive("ChoirCore", PrimitiveType.Sphere, root, new Vector3(0f, 0.10f, 0f), Vector3.one * 0.34f, RuntimeMaterialV11.Hostile);
            Primitive("ChoirSpine", PrimitiveType.Cube, root, Vector3.zero, new Vector3(0.28f, 1.15f, 0.28f), RuntimeMaterialV11.Dark, new Vector3(0f, 15f, 0f));
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f;
                Vector3 p = Quaternion.Euler(0f, angle, 0f) * new Vector3(0.58f, 0.28f, 0f);
                Primitive("ChoirWing_" + i, PrimitiveType.Cube, root, p,
                    new Vector3(0.14f, 0.82f, 0.34f), RuntimeMaterialV11.Gold,
                    new Vector3(0f, -angle, i % 2 == 0 ? 18f : -18f));
            }
            Primitive("ChoirCrown", PrimitiveType.Sphere, root, new Vector3(0f, 0.92f, 0f), Vector3.one * 0.18f, RuntimeMaterialV11.Hostile);
        }

        private static GameObject Primitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 scale,
            Material material,
            Vector3? euler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return go;
        }
    }

    /// <summary>
    /// Phase-readable presentation attached to the existing FracturedSignalDirector. Phase
    /// activation follows the boss' already-authoritative health thresholds and never changes
    /// those thresholds or schedules an action. Neural-hardware mode snaps to state without
    /// animated luminance transitions.
    /// </summary>
    internal sealed class MindforgeDemoV11BossStaging : MonoBehaviour
    {
        private FracturedSignalDirector _director;
        private CombatantVitals _vitals;
        private Transform _phaseTwo;
        private Transform _phaseThree;
        private Light _coreLight;
        private int _phase;
        private bool _controllerPresentation;

        public void Configure(FracturedSignalDirector director, CombatantVitals vitals, bool controllerPresentation)
        {
            _director = director;
            _vitals = vitals;
            _controllerPresentation = controllerPresentation;
            BuildIfNeeded();
            ApplyPhase(true);
        }

        private void Start()
        {
            if (_director == null) _director = GetComponent<FracturedSignalDirector>();
            if (_vitals == null) _vitals = GetComponent<CombatantVitals>();
            BuildIfNeeded();
            ApplyPhase(true);
        }

        private void Update()
        {
            ApplyPhase(false);
            UpdateCoreLight();
        }

        private void BuildIfNeeded()
        {
            if (_phaseTwo != null && _phaseThree != null) return;
            Transform existing = transform.Find("V11BossPhaseStaging");
            if (existing != null) Destroy(existing.gameObject);

            Transform root = new GameObject("V11BossPhaseStaging").transform;
            root.SetParent(transform, false);
            _phaseTwo = new GameObject("PhaseTwoFractureRing").transform;
            _phaseTwo.SetParent(root, false);
            _phaseThree = new GameObject("PhaseThreeFractureCrown").transform;
            _phaseThree.SetParent(root, false);

            for (int i = 0; i < 4; i++)
            {
                float angle = 45f + i * 90f;
                Vector3 p = Quaternion.Euler(0f, angle, 0f) * new Vector3(2.15f, 1.55f, 0f);
                Primitive("PhaseTwoShard_" + i, _phaseTwo, p,
                    new Vector3(0.20f, 1.05f, 0.34f),
                    i % 2 == 0 ? RuntimeMaterialV11.Hostile : RuntimeMaterialV11.Gold,
                    new Vector3(0f, -angle, 32f));
            }

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f;
                Vector3 p = Quaternion.Euler(0f, angle, 0f) * new Vector3(2.9f, 2.05f, 0f);
                Primitive("PhaseThreeBlade_" + i, _phaseThree, p,
                    new Vector3(0.16f, 1.55f, 0.40f), RuntimeMaterialV11.Hostile,
                    new Vector3(0f, -angle, i % 2 == 0 ? 40f : -40f));
            }

            GameObject lightObject = new GameObject("V11BossCoreLight");
            lightObject.transform.SetParent(root, false);
            lightObject.transform.localPosition = new Vector3(0f, 1.65f, 0.45f);
            _coreLight = lightObject.AddComponent<Light>();
            _coreLight.type = LightType.Point;
            _coreLight.color = new Color(1f, 0.10f, 0.27f);
            _coreLight.range = 7.0f;
            _coreLight.shadows = LightShadows.None;
            _coreLight.intensity = 0f;
        }

        private void ApplyPhase(bool force)
        {
            if (_director == null) return;
            int next = Mathf.Clamp(_director.Phase, 1, 3);
            if (!force && next == _phase) return;
            _phase = next;
            if (_phaseTwo != null) _phaseTwo.gameObject.SetActive(_phase >= 2);
            if (_phaseThree != null) _phaseThree.gameObject.SetActive(_phase >= 3);
            Debug.Log($"[Mindforge:V11Experience] Fractured Signal presentation advanced to phase {_phase}.");
        }

        private void UpdateCoreLight()
        {
            if (_coreLight == null) return;
            bool broken = _vitals != null && _vitals.Poise != null && _vitals.Poise.Broken;
            float phaseIntensity = _phase == 1 ? 0.45f : _phase == 2 ? 0.72f : 0.98f;
            if (broken) phaseIntensity *= 0.45f;

            if (!_controllerPresentation)
            {
                _coreLight.intensity = 0f;
                return;
            }

            float blend = 1f - Mathf.Exp(-7f * Mathf.Min(Time.unscaledDeltaTime, 0.05f));
            _coreLight.intensity = Mathf.Lerp(_coreLight.intensity, phaseIntensity, blend);
        }

        private static GameObject Primitive(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 scale,
            Material material,
            Vector3 euler)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return go;
        }
    }
}
