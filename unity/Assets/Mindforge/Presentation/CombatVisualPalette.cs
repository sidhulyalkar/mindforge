using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// One visual-language authority for the competition build.
    /// Sight blue and Guard green are reserved for BCI targets and their immediate
    /// acceptance tether. Combat ordnance deliberately uses a different palette.
    /// </summary>
    [CreateAssetMenu(menuName = "Mindforge/Combat Visual Palette", fileName = "MindforgeVisualPalette")]
    public sealed class CombatVisualPalette : ScriptableObject
    {
        [Header("Reserved neural target colors")]
        public Color sightTarget = new Color(0.29f, 0.62f, 1.00f, 1f);
        public Color guardTarget = new Color(0.27f, 0.95f, 0.60f, 1f);

        [Header("Combat colors")]
        public Color guardianPrimary = new Color(0.94f, 0.95f, 1.00f, 1f);
        public Color hostilePrimary = new Color(1.00f, 0.18f, 0.34f, 1f);
        public Color hostileHeavy = new Color(1.00f, 0.42f, 0.12f, 1f);
        public Color reflected = new Color(0.73f, 0.38f, 1.00f, 1f);
        public Color concord = new Color(0.94f, 0.46f, 1.00f, 1f);
    }
}
