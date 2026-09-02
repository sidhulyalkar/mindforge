using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Presentation-only styling for inherited Dragon Souls HUD widgets. Existing
    /// health/stamina/boss scripts remain the sole owners of values and visibility.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeHudPresentationV31 : MonoBehaviour
    {
        [SerializeField] private Color healthColor = new Color(0.78f, 0.24f, 0.46f, 1f);
        [SerializeField] private Color staminaColor = new Color(0.26f, 0.72f, 0.78f, 1f);
        [SerializeField] private Color bossColor = new Color(0.72f, 0.25f, 0.66f, 1f);
        [SerializeField] private Color neutralColor = new Color(0.70f, 0.78f, 0.84f, 1f);
        [SerializeField] private Color backgroundColor = new Color(0.025f, 0.035f, 0.055f, 0.78f);

        public int SlidersStyled { get; private set; }
        public int TextElementsStyled { get; private set; }
        public bool Installed { get; private set; }

        private void Start()
        {
            StyleSliders();
            StyleText();
            Installed = true;
        }

        private void StyleSliders()
        {
            Slider[] sliders = FindObjectsOfType<Slider>(true);
            for (int i = 0; i < sliders.Length; i++)
            {
                Slider slider = sliders[i];
                if (slider == null || !LooksLikeGameplayMeter(slider.transform)) continue;
                string key = HierarchyKey(slider.transform);
                Color fill = ResolveMeterColor(key);

                if (slider.fillRect != null)
                {
                    Image fillImage = slider.fillRect.GetComponent<Image>();
                    if (fillImage != null) fillImage.color = fill;
                }

                Transform background = slider.transform.Find("Background");
                if (background != null)
                {
                    Image image = background.GetComponent<Image>();
                    if (image != null) image.color = backgroundColor;
                }

                Image rootImage = slider.GetComponent<Image>();
                if (rootImage != null && rootImage != (slider.fillRect == null ? null : slider.fillRect.GetComponent<Image>()))
                    rootImage.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, Mathf.Min(rootImage.color.a, 0.55f));

                SlidersStyled++;
            }
        }

        private void StyleText()
        {
            TextMeshProUGUI[] labels = FindObjectsOfType<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TextMeshProUGUI label = labels[i];
                if (label == null || !LooksLikeGameplayLabel(label.transform)) continue;
                label.color = new Color(0.89f, 0.94f, 0.98f, label.color.a);
                label.enableWordWrapping = false;
                TextElementsStyled++;
            }
        }

        private Color ResolveMeterColor(string key)
        {
            if (key.Contains("boss")) return bossColor;
            if (key.Contains("stamina")) return staminaColor;
            if (key.Contains("health") || key.Contains("hp")) return healthColor;
            return neutralColor;
        }

        private static bool LooksLikeGameplayMeter(Transform transform)
        {
            string key = HierarchyKey(transform);
            return key.Contains("health") || key.Contains("stamina") || key.Contains("boss") || key.Contains("hp");
        }

        private static bool LooksLikeGameplayLabel(Transform transform)
        {
            string key = HierarchyKey(transform);
            return key.Contains("health") || key.Contains("stamina") || key.Contains("boss") ||
                   key.Contains("soul") || key.Contains("heal") || key.Contains("potion") || key.Contains("flask");
        }

        private static string HierarchyKey(Transform transform)
        {
            string key = string.Empty;
            Transform current = transform;
            for (int depth = 0; current != null && depth < 6; depth++, current = current.parent)
                key += "/" + current.name.ToLowerInvariant();
            return key;
        }
    }
}
