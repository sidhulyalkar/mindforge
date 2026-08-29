from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_checkpoint_normalizes_mount_before_snapshotting_and_suspending_foot_authority():
    checkpoint = read("World", "MemoryForgeCheckpoint.cs")
    bike = read("Traversal", "GuardianHoverbikeController.cs")

    assert "GuardianHoverbikeController hoverbike" in checkpoint
    assert "hoverbike?.PrepareForAuthoritySuspension()" in checkpoint
    assert "public void PrepareForAuthoritySuspension()" in bike
    assert "if (_mounted) Dismount(true);" in bike

    death = checkpoint[checkpoint.index("private void OnPlayerDied()"):checkpoint.index("private void RestoreGuardian")]
    assert death.index("NormalizeMountedAuthority();") < death.index("SuspendGuardianAuthority();")

    suspend = checkpoint[checkpoint.index("private void SuspendGuardianAuthority()"):checkpoint.index("private void ResumeGuardianAuthority()")]
    assert suspend.index("_inputWasEnabled =") < suspend.index("playerInput.enabled = false")
    assert suspend.index("_motorWasEnabled =") < suspend.index("playerMotor.enabled = false")


def test_checkpoint_rest_also_exits_mount_before_reconstruction():
    checkpoint = read("World", "MemoryForgeCheckpoint.cs")
    rest = checkpoint[checkpoint.index("public void RestAndReconstruct()"):checkpoint.index("private void OnPlayerDied()")]
    assert rest.index("NormalizeMountedAuthority();") < rest.index("RestoreGuardian(false);")


def test_authority_normalization_cannot_create_new_bci_or_combat_authority():
    checkpoint = read("World", "MemoryForgeCheckpoint.cs")
    bike = read("Traversal", "GuardianHoverbikeController.cs")

    method = bike[bike.index("public void PrepareForAuthoritySuspension()"):bike.index("private void TryMountNearest()")]
    for forbidden in (
        "Input.Get",
        "ReceiveDamage(",
        "TryLightAttack(",
        "RequestDash(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
    ):
        assert forbidden not in method

    assert "NeuralEvent" not in checkpoint
    assert "VepAuraStimulus" not in checkpoint
