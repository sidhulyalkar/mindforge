using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Scene-level marker for the clean V0.11 presentation path. Runtime systems use this
    /// marker to avoid installing legacy showcase presentation layers on top of the demo.
    /// </summary>
    public sealed class MindforgeDemoV11Marker : MonoBehaviour
    {
        [SerializeField] private bool controllerOnlyByDefault = true;
        [SerializeField] private Vector3 guardianSpawn = new Vector3(0f, 0.7f, -18f);
        [SerializeField] private Vector3 bossSpawn = new Vector3(0f, 5.0f, 94f);

        public bool ControllerOnlyByDefault => controllerOnlyByDefault;
        public Vector3 GuardianSpawn => guardianSpawn;
        public Vector3 BossSpawn => bossSpawn;

        public void Configure(bool controllerOnly, Vector3 guardianPosition, Vector3 bossPosition)
        {
            controllerOnlyByDefault = controllerOnly;
            guardianSpawn = guardianPosition;
            bossSpawn = bossPosition;
        }
    }
}
