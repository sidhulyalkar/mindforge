from pathlib import Path
import runpy

import numpy as np


ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Runtime"
RUNNER = ROOT / "tools" / "run_unity_calibrated_decoder.py"


def test_v34_enables_repeated_blocks_without_changing_standalone_v33_default():
    director = (RUNTIME / "MindforgeBciCalibrationDirectorV33.cs").read_text(encoding="utf-8")
    runtime = (RUNTIME / "MindforgeAdaptiveBciRuntimeV34.cs").read_text(encoding="utf-8")

    assert "private bool _adaptiveRepeatedBlocks;" in director
    assert "public void SetAdaptiveRepeatedBlocks(bool enabled)" in director
    assert "if (_adaptiveRepeatedBlocks)" in director
    assert 'SendCalibrationStage(_calibrationId, "protocol", "begin"' in director
    assert 'SendCalibrationStage(_calibrationId, "protocol", "complete"' in director
    assert "MindforgeIntentV29.Sight" in director
    assert "MindforgeIntentV29.Guard" in director
    assert "calibration.SetAdaptiveRepeatedBlocks(true);" in runtime


def test_python_waits_for_protocol_complete_and_reserves_final_target_blocks():
    text = RUNNER.read_text(encoding="utf-8")
    for token in (
        "adaptive_protocol = False",
        "protocol_complete = False",
        'if stage == "protocol":',
        'action == "complete"',
        "ready_to_fit = enough_stage_data and (not adaptive_protocol or protocol_complete)",
        "train_segments = segments[:-1]",
        "heldout_segments = segments[-1:]",
        "cfg.window_samples,\n                                    cfg.window_samples,",
        "validation = heldout_validation(decoder, validation_trials)",
        '"adaptive_repeated_blocks_v34"',
        '"heldout_validation": validation',
        '"promotion_accuracy": promotion_accuracy',
    ):
        assert token in text


def test_heldout_validation_requires_clean_windows_from_both_targets():
    namespace = runpy.run_path(str(RUNNER))
    heldout_validation = namespace["heldout_validation"]

    class Quality:
        artifact = False
        score = 1.0

    class Decision:
        def __init__(self, target):
            self.quality = Quality()
            self.accepted = True
            self.target = target

    class Config:
        min_quality = 0.1

    class Decoder:
        config = Config()

        def decide(self, eeg):
            # Positive marker -> Sight, negative -> Guard.
            target = namespace["AuraTarget"].SIGHT if float(eeg[0, 0]) > 0 else namespace["AuraTarget"].GUARD
            return Decision(target)

        def score(self, eeg):
            sight = float(eeg[0, 0]) > 0
            return {
                namespace["AuraTarget"].SIGHT: 0.9 if sight else 0.1,
                namespace["AuraTarget"].GUARD: 0.1 if sight else 0.9,
            }

    AuraTarget = namespace["AuraTarget"]
    trials = [
        (AuraTarget.SIGHT, np.ones((8, 8))),
        (AuraTarget.SIGHT, np.ones((8, 8))),
        (AuraTarget.GUARD, -np.ones((8, 8))),
        (AuraTarget.GUARD, -np.ones((8, 8))),
    ]
    result = heldout_validation(Decoder(), trials)
    assert result["balanced_accuracy"] == 1.0
    assert result["accepted_correct_fraction"] == 1.0
    assert result["usable_sight_windows"] == 2
    assert result["usable_guard_windows"] == 2
