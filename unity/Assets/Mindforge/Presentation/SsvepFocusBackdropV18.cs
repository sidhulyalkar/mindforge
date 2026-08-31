using System.Collections;
using Mindforge.SoulWisp;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Non-coded local contrast control for the two SSVEP targets.
    ///
    /// Each coded core receives a dark circular plate slightly behind it in camera depth only
    /// while calibration or a player-armed resonance window is visible. The plate never emits,
    /// animates, pulses, reads neural evidence, changes frequency, or owns input. Its purpose is
    /// to reduce uncontrolled local-background contrast variance across the game world while
    /// preserving the existing camera-relative angular stimulus geometry.
    /// </summary>
    [DefaultExecutionOrder(820)]
    public sealed class SsvepFocusBackdropV18 : MonoBehaviour
    {
        public const string RootName = "Mindforge_SsvepFocusBackdrop_V18";

        [SerializeField] private SoulWispController wisp;
        [SerializeField] private Camera targetCamera;
        [SerializeField, Range(1.05f, 2.5f)] private float diameterScale = 1.72f;
        [SerializeField] private float depthBehindCore = 0.035f;
        [SerializeField] private Color neutralColor = new Color(0.025f, 0.035f, 0.050f, 1f);

        private VepAuraStimulus[] _stimuli;
        private Transform[] _plates;
        private Renderer[] _plateRenderers;
        private Material _material;
        private Mesh _discMesh;

        public bool Active { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<MindforgeDemoV11Marker>(true) == null) return;
            if (FindObjectOfType<SsvepFocusBackdropV18>(true) != null) return;
            new GameObject(RootName).AddComponent<SsvepFocusBackdropV18>();
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 240; frame++)
            {
                Resolve();
                if (wisp != null && targetCamera != null && _stimuli != null && _stimuli.Length >= 2)
                {
                    Build();
                    yield break;
                }
                yield return null;
            }

            Debug.LogError("[Mindforge:SSVEP] Focus backdrop could not resolve Wisp/camera/stimulus pair; disabled.");
            enabled = false;
        }

        private void Resolve()
        {
            if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
            if (targetCamera == null) targetCamera = Camera.main;
            if (_stimuli == null || _stimuli.Length < 2)
                _stimuli = FindObjectsOfType<VepAuraStimulus>(true);
        }

        private void Build()
        {
            if (_plates != null || _stimuli == null) return;
            _discMesh = BuildDiscMesh(48);
            _material = BuildNeutralMaterial();
            _plates = new Transform[_stimuli.Length];
            _plateRenderers = new Renderer[_stimuli.Length];

            for (int i = 0; i < _stimuli.Length; i++)
            {
                VepAuraStimulus stimulus = _stimuli[i];
                if (stimulus == null) continue;
                GameObject plate = new GameObject($"SSVEP_LocalContrastPlate_{i:00}_{stimulus.FrequencyHz:0.##}Hz");
                plate.transform.SetParent(transform, false);
                MeshFilter filter = plate.AddComponent<MeshFilter>();
                MeshRenderer renderer = plate.AddComponent<MeshRenderer>();
                filter.sharedMesh = _discMesh;
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.enabled = false;
                _plates[i] = plate.transform;
                _plateRenderers[i] = renderer;
            }
        }

        private void LateUpdate()
        {
            if (_plates == null || wisp == null || targetCamera == null) return;
            bool shouldShow = wisp.CalibrationStimuliActive || wisp.ResonanceWindowActive;
            Active = shouldShow;

            for (int i = 0; i < _plates.Length; i++)
            {
                Transform plate = _plates[i];
                Renderer plateRenderer = _plateRenderers[i];
                VepAuraStimulus stimulus = i < _stimuli.Length ? _stimuli[i] : null;
                if (plate == null || plateRenderer == null || stimulus == null)
                    continue;

                if (!shouldShow || !stimulus.gameObject.activeInHierarchy)
                {
                    plateRenderer.enabled = false;
                    continue;
                }

                Vector3 fromCamera = stimulus.transform.position - targetCamera.transform.position;
                float distance = fromCamera.magnitude;
                if (distance <= targetCamera.nearClipPlane + 0.02f)
                {
                    plateRenderer.enabled = false;
                    continue;
                }

                Vector3 direction = fromCamera / distance;
                plate.position = stimulus.transform.position + direction * Mathf.Max(0.005f, depthBehindCore);
                plate.rotation = Quaternion.LookRotation(-direction, targetCamera.transform.up);

                float coreDiameter = ResolveWorldDiameter(stimulus);
                float plateDiameter = Mathf.Max(0.05f, coreDiameter * Mathf.Max(1.05f, diameterScale));
                plate.localScale = new Vector3(plateDiameter, plateDiameter, 1f);
                plateRenderer.enabled = true;
            }
        }

        private static float ResolveWorldDiameter(VepAuraStimulus stimulus)
        {
            Renderer renderer = stimulus.GetComponent<Renderer>();
            if (renderer == null) renderer = stimulus.GetComponentInChildren<Renderer>(true);
            if (renderer != null)
            {
                Vector3 size = renderer.bounds.size;
                return Mathf.Max(size.x, size.y, size.z);
            }

            Vector3 scale = stimulus.transform.lossyScale;
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }

        private Material BuildNeutralMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;

            Material material = new Material(shader) { name = "Mindforge_SSVEP_LocalContrast_V18" };
            int baseColor = Shader.PropertyToID("_BaseColor");
            int color = Shader.PropertyToID("_Color");
            if (material.HasProperty(baseColor)) material.SetColor(baseColor, neutralColor);
            if (material.HasProperty(color)) material.SetColor(color, neutralColor);
            return material;
        }

        private static Mesh BuildDiscMesh(int segments)
        {
            int count = Mathf.Clamp(segments, 12, 96);
            Vector3[] vertices = new Vector3[count + 1];
            Vector2[] uv = new Vector2[count + 1];
            int[] triangles = new int[count * 3];
            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                float x = Mathf.Cos(a) * 0.5f;
                float y = Mathf.Sin(a) * 0.5f;
                vertices[i + 1] = new Vector3(x, y, 0f);
                uv[i + 1] = new Vector2(x + 0.5f, y + 0.5f);

                int tri = i * 3;
                triangles[tri] = 0;
                triangles[tri + 1] = i + 1;
                triangles[tri + 2] = (i + 1) % count + 1;
            }

            Mesh mesh = new Mesh { name = "Mindforge_SSVEP_Disc_V18" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
            if (_discMesh != null) Destroy(_discMesh);
        }
    }
}
