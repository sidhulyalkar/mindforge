using System;

namespace Mindforge.World
{
    [Serializable]
    internal sealed class PersistentGateStateV1
    {
        public bool open;
    }

    [Serializable]
    internal sealed class PersistentPickupStateV1
    {
        public bool collected;
    }
}
