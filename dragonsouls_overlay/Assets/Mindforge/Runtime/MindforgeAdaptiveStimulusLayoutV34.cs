using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Converts the gaze tutorial's interaction metrics into a deliberately conservative
    /// two-target presentation layout, then freezes that layout before EEG calibration.
    /// Gaze never changes neural class labels or decoder output. The adaptation is limited
    /// to bounded visual spacing plus gaze-AOI/acquisition recommendations.
    /// </summary>
    [DefaultExecutionOrder(1230)]
    [DisallowMultipleComponent]
    public sealed class MindforgeAdaptiveStimulusLayoutV34 : MonoBehaviour
    {
        private MindforgeBciStimulusV33 _stimulus;
        private MindforgeGazeProfilerV34 _profiler;

        public bool Frozen { get; private set; }
        public bool UsedDefaultLayout { get; private set; }
        public string LayoutId { get; private set; } = "unfrozen";
        public float RecommendedAoiRadius { get; private set; } = 0.06f;
        public int RecommendedAcquisitionLeadMs { get; private set; } = 350;
        public float TargetSeparation { get; private set; } = 0.21f;

        private void Start()
        {
            ResolveDependencies();
        }

        public bool FreezeFromProfile()
        {
            ResolveDependencies();
            if (Frozen || _stimulus == null || !_stimulus.Installed) return false;

            MindforgeGazeProfilerV34.ProfileSnapshot profile = _profiler != null ? _profiler.Snapshot() : null;
            bool usable = profile != null &&
                          profile.usable_samples >= 20 &&
                          profile.prompt_count >= 3 &&
                          profile.usable_fraction >= 0.55f;

            if (!usable)
                return FreezeDefault("insufficient_gaze_evidence");

            RecommendedAoiRadius = Mathf.Clamp(profile.recommended_aoi_radius, 0.045f, 0.11f);
            RecommendedAcquisitionLeadMs = Mathf.Clamp(profile.recommended_acquisition_lead_ms, 250, 800);

            // Preserve symmetry and only increase spacing modestly when measured gaze
            // dispersion is wider. This avoids converting screen-mapping bias into a
            // moving neural target while still adapting readability to the participant.
            float extra = Mathf.Max(0f, profile.dispersion_p90 - 0.035f) * 0.80f;
            TargetSeparation = Mathf.Clamp(0.21f + extra, 0.20f, 0.26f);
            float half = TargetSeparation * 0.5f;
            Vector3 sight = new Vector3(-half, 0f, -0.025f);
            Vector3 guard = new Vector3(half, 0f, -0.025f);
            LayoutId = string.Format(
                "v34-s{0:000}-a{1:000}-l{2}",
                Mathf.RoundToInt(TargetSeparation * 1000f),
                Mathf.RoundToInt(RecommendedAoiRadius * 1000f),
                RecommendedAcquisitionLeadMs
            );

            bool applied = _stimulus.TryConfigureLayout(sight, guard, LayoutId, freeze: true);
            if (!applied) return false;

            Frozen = true;
            UsedDefaultLayout = false;
            Debug.Log(
                $"[Mindforge:V34] Gaze-informed BCI layout frozen id={LayoutId} " +
                $"separation={TargetSeparation:F3} aoi={RecommendedAoiRadius:F3} " +
                $"lead={RecommendedAcquisitionLeadMs}ms."
            );
            return true;
        }

        public bool FreezeDefault(string reason)
        {
            ResolveDependencies();
            if (Frozen || _stimulus == null || !_stimulus.Installed) return false;

            TargetSeparation = 0.21f;
            RecommendedAoiRadius = 0.06f;
            RecommendedAcquisitionLeadMs = 350;
            LayoutId = "v34-default-s210-a060-l350";
            bool applied = _stimulus.TryConfigureLayout(
                MindforgeBciStimulusV33.DefaultSightLocalPosition,
                MindforgeBciStimulusV33.DefaultGuardLocalPosition,
                LayoutId,
                freeze: true
            );
            if (!applied) return false;

            Frozen = true;
            UsedDefaultLayout = true;
            Debug.Log($"[Mindforge:V34] Default BCI layout frozen id={LayoutId} reason={reason}.");
            return true;
        }

        private void ResolveDependencies()
        {
            if (_stimulus == null) _stimulus = GetComponent<MindforgeBciStimulusV33>();
            if (_profiler == null) _profiler = GetComponent<MindforgeGazeProfilerV34>();
        }
    }
}
