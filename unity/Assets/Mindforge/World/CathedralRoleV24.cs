using UnityEngine;

namespace Mindforge.World
{
    /// <summary>
    /// Semantic role marker for V0.24 cathedral-authored world geometry.
    ///
    /// This component carries no runtime behaviour. It exists so editor authoring and
    /// validation can distinguish walkable surface, load-bearing structure, boundary,
    /// foundation, ornament and intentionally levitating signal art instead of treating
    /// every mesh as an anonymous prop.
    /// </summary>
    public sealed class CathedralRoleV24 : MonoBehaviour
    {
        public enum StructuralRole
        {
            WalkableFloor,
            StructuralSupport,
            BoundaryWall,
            VaultCeiling,
            RetainingSubstructure,
            DecorativePatina,
            MysticAccent,
        }

        [SerializeField] private StructuralRole role = StructuralRole.DecorativePatina;

        public StructuralRole Role => role;

        public void Configure(StructuralRole value)
        {
            role = value;
        }
    }
}
