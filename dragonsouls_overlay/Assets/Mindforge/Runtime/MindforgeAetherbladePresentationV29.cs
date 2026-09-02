using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Presentation-only replacement for Dragon Souls' visible sword mesh.
    ///
    /// The upstream Sword root remains authoritative for hand/sheath attachment,
    /// Rigidbody throw/recall, hit control, damage, audio timing, and its existing
    /// TrailRenderer. V0.29 only retires the child MeshRenderer and creates a
    /// collider-free emissive Aetherblade under that exact same animated root.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeAetherbladePresentationV29 : MonoBehaviour
    {
        public const string PresentationRootName = "Mindforge_Aetherblade_V29";
        public const string UpstreamMeshName = "Sword1_1_3";

        [Header("Blade")]
        [SerializeField] private float bladeLength = 1.03f;
        [SerializeField] private float bladeRadius = 0.028f;
        [SerializeField] private float bladeStartY = 0.17f;
        [SerializeField] private Color bladeColor = new Color(0.20f, 0.92f, 1.00f, 1f);
        [SerializeField] private float emission = 7.5f;

        [Header("Hilt")]
        [SerializeField] private float hiltLength = 0.24f;
        [SerializeField] private float hiltRadius = 0.047f;

        private Transform _presentationRoot;
        private Renderer _retiredUpstreamRenderer;
        private TrailRenderer _trail;
        private Material _bladeMaterial;
        private Material _glowMaterial;
        private Material _hiltMaterial;
        private Material _trailMaterial;

        public bool Installed => _presentationRoot != null;
        public Renderer RetiredUpstreamRenderer => _retiredUpstreamRenderer;

        private void Awake()
        {
            Install();
        }

        private void OnEnable()
        {
            Install();
        }

        private void OnDestroy()
        {
            DestroyRuntimeMaterial(_bladeMaterial);
            DestroyRuntimeMaterial(_glowMaterial);
            DestroyRuntimeMaterial(_hiltMaterial);
            DestroyRuntimeMaterial(_trailMaterial);
        }

        public void Install()
        {
            if (_presentationRoot != null) return;

            Transform old = transform.Find(PresentationRootName);
            if (old != null)
            {
                _presentationRoot = old;
                return;
            }

            Transform upstreamMesh = FindDeep(transform, UpstreamMeshName);
            if (upstreamMesh != null)
            {
                _retiredUpstreamRenderer = upstreamMesh.GetComponent<Renderer>();
                if (_retiredUpstreamRenderer != null)
                    _retiredUpstreamRenderer.enabled = false;
            }

            _trail = GetComponentInChildren<TrailRenderer>(true);
            BuildPresentation();
            RethemeTrail();
        }

        private void BuildPresentation()
        {
            GameObject root = new GameObject(PresentationRootName);
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            _presentationRoot = root.transform;

            _bladeMaterial = CreateEmissionMaterial(
                "MF_V29_Aetherblade_Core",
                bladeColor,
                emission,
                transparent: false);
            _glowMaterial = CreateEmissionMaterial(
                "MF_V29_Aetherblade_Glow",
                new Color(bladeColor.r, bladeColor.g, bladeColor.b, 0.23f),
                emission * 0.45f,
                transparent: true);
            _hiltMaterial = CreateLitMaterial(
                "MF_V29_Aetherblade_Hilt",
                new Color(0.055f, 0.065f, 0.075f, 1f),
                metallic: 0.82f,
                smoothness: 0.72f);

            float bladeCenterY = bladeStartY + bladeLength * 0.5f;
            CreateCylinder(
                "Blade_Core",
                _presentationRoot,
                new Vector3(0f, bladeCenterY, 0f),
                bladeRadius,
                bladeLength,
                _bladeMaterial);
            CreateCylinder(
                "Blade_Glow",
                _presentationRoot,
                new Vector3(0f, bladeCenterY, 0f),
                bladeRadius * 2.25f,
                bladeLength * 1.015f,
                _glowMaterial);

            float hiltCenterY = bladeStartY - hiltLength * 0.43f;
            CreateCylinder(
                "Hilt",
                _presentationRoot,
                new Vector3(0f, hiltCenterY, 0f),
                hiltRadius,
                hiltLength,
                _hiltMaterial);

            CreateGuardRing(bladeStartY - 0.015f);
            CreatePommel(hiltCenterY - hiltLength * 0.56f);
            CreateLocalBladeLight(bladeStartY + 0.34f);
        }

        private void CreateGuardRing(float y)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Emitter_Guard";
            ring.transform.SetParent(_presentationRoot, false);
            ring.transform.localPosition = new Vector3(0f, y, 0f);
            ring.transform.localRotation = Quaternion.identity;
            ring.transform.localScale = new Vector3(hiltRadius * 2.7f, 0.018f, hiltRadius * 2.7f);
            RemovePhysics(ring);
            Renderer renderer = ring.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = _bladeMaterial;
        }

        private void CreatePommel(float y)
        {
            GameObject pommel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pommel.name = "Pommel";
            pommel.transform.SetParent(_presentationRoot, false);
            pommel.transform.localPosition = new Vector3(0f, y, 0f);
            pommel.transform.localScale = Vector3.one * hiltRadius * 1.35f;
            RemovePhysics(pommel);
            Renderer renderer = pommel.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = _hiltMaterial;
        }

        private void CreateLocalBladeLight(float y)
        {
            GameObject lightObject = new GameObject("Aetherblade_LocalLight");
            lightObject.transform.SetParent(_presentationRoot, false);
            lightObject.transform.localPosition = new Vector3(0f, y, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = bladeColor;
            light.range = 2.15f;
            light.intensity = 1.15f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
        }

        private void RethemeTrail()
        {
            if (_trail == null) return;
            _trail.time = Mathf.Clamp(_trail.time, 0.075f, 0.16f);
            _trail.widthMultiplier = 0.78f;
            _trail.minVertexDistance = Mathf.Min(_trail.minVertexDistance, 0.045f);

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(bladeColor, 0.18f),
                    new GradientColorKey(bladeColor * 0.55f, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0.82f, 0f),
                    new GradientAlphaKey(0.48f, 0.34f),
                    new GradientAlphaKey(0f, 1f),
                });
            _trail.colorGradient = gradient;

            _trailMaterial = CreateEmissionMaterial(
                "MF_V29_Aetherblade_Trail",
                new Color(bladeColor.r, bladeColor.g, bladeColor.b, 0.72f),
                emission * 0.55f,
                transparent: true);
            _trail.sharedMaterial = _trailMaterial;
        }

        private static void CreateCylinder(
            string name,
            Transform parent,
            Vector3 localPosition,
            float radius,
            float length,
            Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localRotation = Quaternion.identity;
            // Unity's built-in cylinder is 2 units tall along local Y.
            cylinder.transform.localScale = new Vector3(radius, length * 0.5f, radius);
            RemovePhysics(cylinder);
            Renderer renderer = cylinder.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static void RemovePhysics(GameObject go)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Rigidbody body = go.GetComponent<Rigidbody>();
            if (body != null) Destroy(body);
        }

        private static Material CreateEmissionMaterial(
            string name,
            Color color,
            float intensity,
            bool transparent)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { name = name };

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * Mathf.Max(1f, intensity));
            }

            if (transparent)
            {
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            return material;
        }

        private static Material CreateLitMaterial(
            string name,
            Color color,
            float metallic,
            float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            return material;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (string.Equals(child.name, name, StringComparison.Ordinal)) return child;
                Transform nested = FindDeep(child, name);
                if (nested != null) return nested;
            }
            return null;
        }

        private static void DestroyRuntimeMaterial(Material material)
        {
            if (material != null) Destroy(material);
        }
    }

    /// <summary>
    /// Runtime scene hook that decorates the upstream Sword prefab without modifying
    /// its serialized source. This keeps the local external checkout pristine.
    /// </summary>
    public static class MindforgeAetherbladeInstallerV29
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterFirstScene()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            InstallInActiveScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InstallInActiveScene();
        }

        private static void InstallInActiveScene()
        {
            GameObject[] all = UnityEngine.Object.FindObjectsOfType<GameObject>(true);
            for (int i = 0; i < all.Length; i++)
            {
                GameObject go = all[i];
                if (go == null || go.name != "Sword") continue;
                if (go.transform.Find(MindforgeAetherbladePresentationV29.UpstreamMeshName) == null) continue;
                if (go.GetComponent<MindforgeAetherbladePresentationV29>() == null)
                    go.AddComponent<MindforgeAetherbladePresentationV29>();
            }
        }
    }
}
