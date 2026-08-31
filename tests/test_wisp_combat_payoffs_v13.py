from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def test_sight_is_opening_exploitation_not_automatic_offense():
    buffs = read("unity/Assets/Mindforge/SoulWisp/AuraBuffController.cs")
    combat = read("unity/Assets/Mindforge/Combat/GuardianCombatController.cs")

    assert "sightDamageMultiplier = 1.30f" in buffs
    assert "sightReachMultiplier = 1.16f" in buffs
    assert "sightPoiseMultiplier = 1.45f" in buffs
    assert "SightReachMultiplier" in combat
    assert "SightPoiseMultiplier" in combat
    assert "RiftCleave(Vector3 aimDirection)" in combat
    assert "FirePulse(Vector3 aimDirection)" in combat

    # Sight alters the consequence of conventional attacks. It must not manufacture them.
    assert "Input.Get" not in buffs
    assert "FirePulse(" not in buffs
    assert "RiftCleave(" not in buffs
    assert "ReceiveDamage(" not in buffs


def test_guard_improves_player_executed_counter_opportunity_without_auto_defense():
    buffs = read("unity/Assets/Mindforge/SoulWisp/AuraBuffController.cs")
    combat = read("unity/Assets/Mindforge/Combat/GuardianCombatController.cs")

    assert "guardCounterWindowMultiplier = 1.28f" in buffs
    assert "guardCounterRadiusMultiplier = 1.10f" in buffs
    assert "guardSuccessfulCounterHeal = 3.2f" in buffs
    assert "auras.GuardCounterWindowMultiplier" in combat
    assert "auras.GuardCounterRadiusMultiplier" in combat
    assert "auras.GuardSuccessfulCounterHeal" in combat

    baseline = combat.index("_counterUntilTick = now + SecondsToTicks(tuning.counterWindow)")
    guard_extension = combat.index("auras.GuardCounterWindowMultiplier")
    action = combat.index('ActionAccepted?.Invoke("COUNTER_PULSE")')
    assert baseline < guard_extension < action

    # Guard still requires the physical BeginCounter call and cannot become passive invulnerability.
    forbidden = ("invulnerable", "autoCounter", "AutoCounter", "damageReduction", "DamageReduction")
    assert all(token not in buffs for token in forbidden)


def test_concord_stays_sequence_mastery_with_physical_release_elsewhere():
    buffs = read("unity/Assets/Mindforge/SoulWisp/AuraBuffController.cs")
    combat = read("unity/Assets/Mindforge/Combat/GuardianCombatController.cs")

    assert "now < _sightUntil && now < _guardUntil" in buffs
    assert "ConcordTriggered" in buffs
    assert "ConcordActive" in combat
    assert "ReflectTowards" in combat
    assert "Input.Get" not in buffs
