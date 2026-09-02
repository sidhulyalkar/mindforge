using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Marks a Dragon Souls-derived scene as a Mindforge-owned production slice.
    /// The source upstream scene remains untouched; this marker exists only in the
    /// copied Assets/Mindforge scene that we are free to recompose aggressively.
    /// </summary>
    public sealed class MindforgeCombatSliceMarkerV29 : MonoBehaviour
    {
        [SerializeField] private string sourceScene = "Assets/Levels/Scenes/GameplayTestScene.unity";
        [SerializeField] private string productVersion = "V0.29 Dragon Souls Chassis";
        [SerializeField] private float minimumCombatHallWidth = 14f;
        [SerializeField] private float minimumTraversalCorridorWidth = 8f;
        [SerializeField] private float decorativeShoulderExclusion = 2f;
        [SerializeField] private float minimumBossArenaDiameter = 32f;

        public string SourceScene => sourceScene;
        public string ProductVersion => productVersion;
        public float MinimumCombatHallWidth => minimumCombatHallWidth;
        public float MinimumTraversalCorridorWidth => minimumTraversalCorridorWidth;
        public float DecorativeShoulderExclusion => decorativeShoulderExclusion;
        public float MinimumBossArenaDiameter => minimumBossArenaDiameter;
    }
}
