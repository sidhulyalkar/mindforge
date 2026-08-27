using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Optional authored-art seam. Drop a Resources/Cinematic/MindforgeArtProfile asset
    /// in the project and assign production prefabs to replace procedural presentation
    /// without changing authoritative combat objects or BCI code.
    /// </summary>
    [CreateAssetMenu(menuName = "Mindforge/Cinematic Art Profile", fileName = "MindforgeArtProfile")]
    public sealed class CinematicArtProfile : ScriptableObject
    {
        [Header("Optional authored visual prefabs")]
        public GameObject guardianVisualPrefab;
        public GameObject fracturedSignalVisualPrefab;
        public GameObject arenaSetDressPrefab;

        [Header("Visual-only placement")]
        public Vector3 guardianLocalPosition;
        public Vector3 guardianLocalEuler;
        public Vector3 guardianLocalScale = Vector3.one;
        public Vector3 bossLocalPosition;
        public Vector3 bossLocalEuler;
        public Vector3 bossLocalScale = Vector3.one;

        [Header("Transition")]
        public bool hideProceduralGuardianWhenBound = true;
        public bool hideProceduralBossWhenBound = true;
    }
}
