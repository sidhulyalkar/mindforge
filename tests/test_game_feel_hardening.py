from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_dynamic_combatants_cannot_become_ground_for_jump_state():
    motor = read("Combat", "GuardianMotor.cs")

    assert "IsDynamicCombatantCollider(hit.collider)" in motor
    assert "candidate.GetComponentInParent<CombatantVitals>() != null" in motor
    assert "Physics.SphereCastNonAlloc" in motor


def test_motor_owns_yaw_instead_of_accepting_contact_induced_spin():
    motor = read("Combat", "GuardianMotor.cs")

    assert "_body.angularVelocity = Vector3.zero" in motor
    assert "Yaw is an explicit player/lock-on state" in motor
    assert "_body.MoveRotation" in motor

    # The dynamic player can clear contact torque safely, but it must never become
    # kinematic or teleport itself each fixed tick to solve collision response.
    assert "_body.isKinematic = true" not in motor
    assert "_body.position =" not in motor


def test_persistent_combat_hud_teaches_current_aerial_mapping_without_space_dodge_regression():
    hud = read("Presentation", "CombatStateHud.cs")

    assert "F SWORD · SPACE ×2 · SHIFT DODGE" in hud
    assert "SPACE ×2 / HOLD HOVER · SHIFT AIR DASH" in hud
    assert "SPACE  DODGE" not in hud

    for forbidden in ("RequestJump(", "RequestDash(", "TryApply(", "ReceiveDamage("):
        assert forbidden not in hud
