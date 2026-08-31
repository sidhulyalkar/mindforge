using System;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Adds distant, collider-free architectural depth around the playable world.
    /// The recording exposed an empty grey horizon that made every arena edge feel like
    /// a level boundary. These forms sit well outside the playable shell and are intentionally
    /// low-frequency, non-emissive silhouettes. They never animate or participate in physics.
    /// </summary>
    public sealed class WorldDepthBackdropV16 : MonoBehaviour
    {
        public const string RootName = "Mindforge_WorldDepthBackdrop_V16";

        private static readonly string[] SurveyRootNames =
        {
            "Mindforge_AetheriaWorld_V1",
            "Mindforge_GroundedWorld_V1",
            "Mindforge_Production_Art_V09",
            "Mindforge_Demo_Environment_V15",
        };

        private Transform _guardian;
        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private readonly List<Material> _ownedMaterials = new List<Material>(4);
        private readonly List<GameObject> _ownedObjects = new List<GameObject>(64);

        public int BackdropPieceCount => _ownedObjects.Count;

        public void Configure(Transform guardian, AwakeningCalibrationDirector calibration, SoulWispController wisp)
        {
            _guardian = guardian;
            _calibration = calibration;
            _wisp = wisp;
        }

        private void Start()
        {
            if (VisualIdentityV16Installer.FindSceneObject(RootName) != null) return;
            BuildBackdrop();
        }

        private void BuildBackdrop()
        {
            Bounds world = SurveyWorldBounds();
            if (world.size.sqrMagnitude < 1f)
            {
                Vector3 center = _guardian != null ? _guardian.position : Vector3.zero;
                world = new Bounds(center, new Vector3(76f, 18f, 109f));
            }

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(transform, false);
            _ownedObjects.Add(root);

            Material farDark = CreateLit("V16_FarSlate", new Color(0.070f, 0.085f, 0.115f), 0.05f, 0.20f);
            Material farMid = CreateLit("V16_FarStone", new Color(0.13f, 0.16f, 0.20f), 0.02f, 0.16f);
            Material farLight = CreateLit("V16_FarIvory", new Color(0.31f, 0.32f, 0.31f), 0.03f, 0.18f);
            Material hazeBand = CreateLit("V16_HorizonBand", new Color(0.10f, 0.14f, 0.18f), 0f, 0.08f);

            Vector3 centerWorld = world.center;
            float halfX = Mathf.Max(36f, world.extents.x);
            float halfZ = Mathf.Max(50f, world.extents.z);
            float outerX = halfX + 28f;
            float outerZ = halfZ + 34f;

            // Broad low horizon shelves remove the hard "platform against nothing" read.
            AddPart("HorizonShelfNorth", PrimitiveType.Cube, root.transform,
                new Vector3(centerWorld.x, world.min.y - 1.0f, centerWorld.z + outerZ),
                new Vector3(outerX * 2.7f, 2.0f, 24f), hazeBand, Vector3.zero);
            AddPart("HorizonShelfSouth", PrimitiveType.Cube, root.transform,
                new Vector3(centerWorld.x, world.min.y - 1.2f, centerWorld.z - outerZ),
                new Vector3(outerX * 2.7f, 2.2f, 22f), farDark, Vector3.zero);
            AddPart("HorizonShelfWest", PrimitiveType.Cube, root.transform,
                new Vector3(centerWorld.x - outerX, world.min.y - 1.1f, centerWorld.z),
                new Vector3(18f, 2.1f, outerZ * 2.15f), farDark, Vector3.zero);
            AddPart("HorizonShelfEast", PrimitiveType.Cube, root.transform,
                new Vector3(centerWorld.x + outerX, world.min.y - 1.1f, centerWorld.z),
                new Vector3(18f, 2.1f, outerZ * 2.15f), farDark, Vector3.zero);

            // Three depth planes of skyline. Deterministic spacing avoids procedural noise
            // that changes between test runs while still breaking the repeated-cube look.
            for (int layer = 0; layer < 3; layer++)
            {
                float z = centerWorld.z + outerZ + 10f + layer * 18f;
                float layerScale = 1f + layer * 0.22f;
                Material layerMaterial = layer == 0 ? farLight : (layer == 1 ? farMid : farDark);
                for (int i = -7; i <= 7; i++)
                {
                    float x = centerWorld.x + i * 9.5f * layerScale + ((i & 1) == 0 ? 1.8f : -1.6f);
                    float normalized = Mathf.Abs(i) / 7f;
                    float height = (12f + ((i * i + layer * 5) % 7) * 2.35f) * layerScale;
                    float width = (3.6f + ((Mathf.Abs(i) + layer) % 3) * 1.2f) * layerScale;
                    float depth = 3.8f + layer * 1.7f;
                    Vector3 p = new Vector3(x, world.min.y + height * 0.5f - 0.25f, z + normalized * 4f);
                    GameObject tower = AddPart($"Skyline_L{layer}_T{i + 7:00}", PrimitiveType.Cube, root.transform,
                        p, new Vector3(width, height, depth), layerMaterial,
                        new Vector3(0f, i * 2.8f + layer * 7f, (i % 3 - 1) * 1.2f));

                    if ((i + layer) % 3 == 0)
                    {
                        float crownHeight = 4.5f + layer * 1.4f;
                        AddPart($"SkylineCrown_L{layer}_T{i + 7:00}", PrimitiveType.Cube, tower.transform,
                            new Vector3(0f, height * 0.54f, 0f),
                            new Vector3(width * 0.38f, crownHeight, depth * 0.42f), farLight,
                            new Vector3(0f, 0f, i % 2 == 0 ? -13f : 13f), true);
                    }
                }
            }

            // Side silhouettes create parallax when the player turns and stop the camera from
            // revealing a single empty horizon outside the authored forward vista.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = centerWorld.x + side * (outerX + 15f);
                for (int i = 0; i < 8; i++)
                {
                    float z = centerWorld.z - halfZ * 0.75f + i * (halfZ * 1.5f / 7f);
                    float height = 9f + (i % 4) * 3.8f;
                    AddPart($"SideSpire_{(side < 0 ? "W" : "E")}_{i:00}",
                        i % 2 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube,
                        root.transform,
                        new Vector3(x + side * (i % 2) * 4f, world.min.y + height * 0.5f, z),
                        new Vector3(2.2f + (i % 3) * 0.6f, height, 2.2f),
                        i % 3 == 0 ? farLight : farMid,
                        new Vector3(i % 2 == 0 ? 0f : 2f, side * (8f + i * 5f), i % 3 - 1));
                }
            }

            Debug.Log($"[Mindforge:V16] Added {_ownedObjects.Count - 1} collider-free backdrop pieces around surveyed world bounds {world.size}.");
        }

        private Bounds SurveyWorldBounds()
        {
            bool has = false;
            Bounds result = default;
            for (int r = 0; r < SurveyRootNames.Length; r++)
            {
                GameObject root = VisualIdentityV16Installer.FindSceneObject(SurveyRootNames[r]);
                if (root == null) continue;
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null) continue;
                    if (!has)
                    {
                        result = renderer.bounds;
                        has = true;
                    }
                    else result.Encapsulate(renderer.bounds);
                }
            }
            return result;
        }

        private Material CreateLit(string materialName, Color color, float metallic, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;
            Material material = new Material(shader) { name = materialName };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            _ownedMaterials.Add(material);
            return material;
        }

        private GameObject AddPart(
            string objectName,
            PrimitiveType type,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Vector3 euler,
            bool localPosition = false)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            if (localPosition) part.transform.localPosition = position;
            else part.transform.position = position;
            part.transform.localRotation = Quaternion.Euler(euler);
            part.transform.localScale = scale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            _ownedObjects.Add(part);
            return part;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _ownedMaterials.Count; i++)
                if (_ownedMaterials[i] != null) Destroy(_ownedMaterials[i]);
        }
    }
}
