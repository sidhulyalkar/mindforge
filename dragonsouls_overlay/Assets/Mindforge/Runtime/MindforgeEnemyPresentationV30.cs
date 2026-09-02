using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Visual-only treatment for standard Dragon Souls enemies inside the V0.30
    /// production world. The existing EnemyStateMachine remains the sole owner of
    /// AI, locomotion, attacks, health, hit reactions and death.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeEnemyPresentationV30 : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.45f)] private float tintStrength = 0.22f;

        public bool Installed { get; private set; }
        public int RenderersRethemed { get; private set; }

        private void Start()
        {
            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            Color target = ArchetypeColor(gameObject.name);
            int changed = 0;

            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = renderers[i];
                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0 || materials[0] == null) continue;

                Material material = materials[0];
                string property = material.HasProperty("_BaseColor") ? "_BaseColor" :
                    material.HasProperty("_Color") ? "_Color" : null;
                if (property == null) continue;

                Color original = material.GetColor(property);
                Color adjusted = Color.Lerp(original, target, tintStrength);
                adjusted.a = original.a;

                renderer.GetPropertyBlock(block);
                block.SetColor(property, adjusted);
                renderer.SetPropertyBlock(block);
                changed++;
            }

            RenderersRethemed = changed;
            Installed = true;
        }

        private static Color ArchetypeColor(string objectName)
        {
            string n = objectName.ToLowerInvariant();
            if (n.Contains("mage") || n.Contains("wizard"))
                return new Color(0.48f, 0.22f, 0.58f, 1f);
            if (n.Contains("archer") || n.Contains("range"))
                return new Color(0.25f, 0.48f, 0.58f, 1f);
            if (n.Contains("heavy") || n.Contains("knight") || n.Contains("brute"))
                return new Color(0.30f, 0.30f, 0.39f, 1f);
            return new Color(0.34f, 0.38f, 0.46f, 1f);
        }
    }
}
