using System;
using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Optional authored-art seams for the five Null Ward visual districts.
    ///
    /// Prefabs bound through this profile are presentation overlays only. The generated
    /// collision/world-authority geometry remains intact underneath them, so replacing
    /// room art never changes encounter triggers, checkpoints, shortcuts or BCI state.
    /// </summary>
    [CreateAssetMenu(menuName = "Mindforge/Null Ward Art Profile", fileName = "NullWardArtProfile")]
    public sealed class NullWardArtProfile : ScriptableObject
    {
        [Serializable]
        public sealed class ZoneBinding
        {
            public GameObject visualPrefab;
            public Vector3 localPosition;
            public Vector3 localEuler;
            public Vector3 localScale = Vector3.one;
        }

        [Header("Optional authored zone overlays")]
        public ZoneBinding memoryForge = new ZoneBinding();
        public ZoneBinding synapseCauseway = new ZoneBinding();
        public ZoneBinding nullMarket = new ZoneBinding();
        public ZoneBinding maintenanceLoop = new ZoneBinding();
        public ZoneBinding signalCathedral = new ZoneBinding();

        [Header("Transition")]
        [Tooltip("Hides only the collider-free V2 detail hierarchy when authored room art is supplied. Base structural renderers/colliders stay intact.")]
        public bool hideProceduralDetailWhenAnyZoneIsBound = true;
    }
}
