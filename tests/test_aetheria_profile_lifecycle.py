from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_post_menagerie_aetheria_attack_mutations_are_recaptured_into_wave_profiles():
    horde = read("Editor", "AetheriaHordeBossV1Builder.cs")
    profile = read("World", "ArenaMenagerieRoleProfile.cs")
    waves = read("World", "ArenaMenagerieDirector.cs")

    # Menagerie profiles are restored after every activation because enemy OnEnable
    # reapplies base-archetype defaults. Any later Aetheria mutation must therefore
    # refresh the serialized profile or it disappears when its wave begins.
    assert "RefreshRoleProfile(stalker)" in horde
    assert "RefreshRoleProfile(gargoyle)" in horde
    assert "profile.CaptureFromCurrent(enemy)" in horde
    assert "ArenaMenagerieRoleProfile" in horde

    assert "public void CaptureFromCurrent" in profile
    assert "profile?.Apply()" in waves
    assert waves.index("enemy.gameObject.SetActive(true)") < waves.index("profile?.Apply()") < waves.index("enemy.Arm()")


def test_gargoyle_profile_is_refreshed_even_when_dive_was_already_authored():
    horde = read("Editor", "AetheriaHordeBossV1Builder.cs")
    method = horde[horde.index("private static void ConfigureAeroGargoyleDive"):horde.index("private static void RefreshRoleProfile")]

    assert 'if (FindAttack(source, "gargoyle_dive") == null)' in method
    assert "RebuildCooldownState(gargoyle);" in method
    assert "RefreshRoleProfile(gargoyle);" in method
    assert method.index("RefreshRoleProfile(gargoyle);") > method.index('if (FindAttack(source, "gargoyle_dive") == null)')
