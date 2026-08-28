from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_vitals_never_issue_velocity_or_force_commands_to_kinematic_bodies():
    vitals = read("Combat", "CombatantVitals.cs")

    assert "!body.isKinematic && packet.Impulse.sqrMagnitude > 0.001f" in vitals
    assert "if (body != null && !body.isKinematic)" in vitals
    assert "body.AddForce(packet.Impulse, ForceMode.VelocityChange)" in vitals
    assert "body.velocity = Vector3.zero" in vitals
    assert "body.angularVelocity = Vector3.zero" in vitals

    # The fix is a guard around unsupported commands, not a change to damage/poise or
    # checkpoint reconstruction semantics.
    assert "poise?.Apply(packet.PoiseDamage)" in vitals
    assert "if (restoreHealth) Health = Mathf.Max(0f, maxHealth)" in vitals
