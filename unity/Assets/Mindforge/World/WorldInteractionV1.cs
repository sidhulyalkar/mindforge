using System;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.Traversal;

namespace Mindforge.World
{
    public interface IWorldInteractionV1
    {
        string InteractionId { get; }
        string Prompt { get; }
        Transform Anchor { get; }
        float Radius { get; }
        int Priority { get; }
        bool CanInteract(Transform actor);
        bool TryInteract(Transform actor);
    }

    /// <summary>
    /// Small registry-backed interaction source. Sources publish an offer only; they do not
    /// sample input. GuardianInteractionRouterV1 owns context selection and explicit input,
    /// while the concrete source remains authoritative for the action it performs.
    /// </summary>
    public abstract class WorldInteractionSourceV1 : MonoBehaviour, IWorldInteractionV1
    {
        private static readonly List<WorldInteractionSourceV1> Active = new List<WorldInteractionSourceV1>(32);

        public abstract string InteractionId { get; }
        public abstract string Prompt { get; }
        public virtual Transform Anchor => transform;
        public virtual float Radius => 3f;
        public virtual int Priority => 0;
        public abstract bool CanInteract(Transform actor);
        public abstract bool TryInteract(Transform actor);

        protected virtual void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
        }

        protected virtual void OnDisable()
        {
            Active.Remove(this);
        }

        public static WorldInteractionSourceV1 FindBest(
            Transform actor,
            Camera camera,
            float maximumRadius,
            out float score)
        {
            score = float.PositiveInfinity;
            if (actor == null) return null;

            WorldInteractionSourceV1 best = null;
            Vector3 forward = camera != null
                ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up)
                : Vector3.ProjectOnPlane(actor.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            for (int i = Active.Count - 1; i >= 0; i--)
            {
                WorldInteractionSourceV1 source = Active[i];
                if (source == null)
                {
                    Active.RemoveAt(i);
                    continue;
                }
                if (!source.isActiveAndEnabled || !source.gameObject.activeInHierarchy || !source.CanInteract(actor)) continue;

                Transform anchor = source.Anchor;
                if (anchor == null) continue;
                Vector3 delta = Vector3.ProjectOnPlane(anchor.position - actor.position, Vector3.up);
                float distance = delta.magnitude;
                float radius = Mathf.Min(Mathf.Max(0.5f, source.Radius), Mathf.Max(0.5f, maximumRadius));
                if (distance > radius) continue;

                float angle = delta.sqrMagnitude > 0.001f ? Vector3.Angle(forward, delta.normalized) : 0f;
                // Priority dominates, then distance, then camera-facing intent. This makes
                // authored high-priority interactions predictable without requiring colliders.
                float candidate = -source.Priority * 100f + distance + angle * 0.018f;
                if (candidate >= score) continue;
                score = candidate;
                best = source;
            }
            return best;
        }
    }

    /// <summary>Context adapter for the existing Memory Forge physical checkpoint authority.</summary>
    public sealed class MemoryForgeInteractionV1 : WorldInteractionSourceV1
    {
        [SerializeField] private MemoryForgeCheckpoint checkpoint;

        public override string InteractionId => "checkpoint.memory_forge.rest";
        public override string Prompt => "Reconstruct at Memory Forge";
        public override Transform Anchor => checkpoint != null ? checkpoint.InteractionPoint : transform;
        public override float Radius => checkpoint != null ? checkpoint.InteractionRadius : 2.35f;
        public override int Priority => 30;

        public void ConfigureRuntime(MemoryForgeCheckpoint value)
        {
            checkpoint = value;
            if (checkpoint != null) checkpoint.SetExternalInteractionOwned(true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (checkpoint == null) checkpoint = GetComponent<MemoryForgeCheckpoint>();
            if (checkpoint != null) checkpoint.SetExternalInteractionOwned(true);
        }

        protected override void OnDisable()
        {
            if (checkpoint != null) checkpoint.SetExternalInteractionOwned(false);
            base.OnDisable();
        }

        public override bool CanInteract(Transform actor)
            => checkpoint != null && checkpoint.CanRestNow;

        public override bool TryInteract(Transform actor)
        {
            if (!CanInteract(actor)) return false;
            checkpoint.RestAndReconstruct();
            return true;
        }
    }

    /// <summary>
    /// Context adapter for a parked Prism hoverbike. The adapter never moves the Guardian;
    /// it forwards the explicit context request to GuardianHoverbikeController, which remains
    /// the sole mounted locomotion and mount/dismount physics authority.
    /// </summary>
    public sealed class HoverbikeInteractionV1 : WorldInteractionSourceV1
    {
        [SerializeField] private AetherHoverbikeMount mount;
        [SerializeField] private GuardianHoverbikeController controller;

        public override string InteractionId => "vehicle.prism_hoverbike.mount";
        public override string Prompt => "Ride Prism Hoverbike";
        public override Transform Anchor => mount != null ? mount.transform : transform;
        public override float Radius => 3.2f;
        public override int Priority => 10;

        public void ConfigureRuntime(AetherHoverbikeMount bike, GuardianHoverbikeController guardianController)
        {
            mount = bike;
            controller = guardianController;
        }

        public override bool CanInteract(Transform actor)
        {
            Resolve(actor);
            return mount != null && controller != null && controller.CanMount(mount);
        }

        public override bool TryInteract(Transform actor)
        {
            Resolve(actor);
            return mount != null && controller != null && controller.TryMount(mount);
        }

        private void Resolve(Transform actor)
        {
            if (mount == null) mount = GetComponent<AetherHoverbikeMount>();
            if (controller == null && actor != null)
                controller = actor.GetComponent<GuardianHoverbikeController>();
        }
    }
}
