using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Finalizes the procedural Guardian presentation hierarchy after motion wrappers
    /// are created. Every direct visible body piece inherits the same center-of-mass
    /// motion while gameplay objects remain outside the visual subtree.
    /// </summary>
    [DefaultExecutionOrder(470)]
    public sealed class GuardianPresentationHierarchyBinder : MonoBehaviour
    {
        private bool _bound;

        private void LateUpdate()
        {
            if (_bound) return;
            Transform visualRoot = transform.Find("GuardianShowcaseAvatar");
            if (visualRoot == null) return;
            Transform bodyMotion = visualRoot.Find("Motion_Body");
            if (bodyMotion == null) return;

            // Iterate backwards because SetParent changes the direct-child collection.
            for (int i = visualRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = visualRoot.GetChild(i);
                if (child == bodyMotion) continue;
                child.SetParent(bodyMotion, true);
            }
            _bound = true;
        }
    }
}
