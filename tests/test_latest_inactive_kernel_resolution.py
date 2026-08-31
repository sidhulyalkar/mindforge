from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "unity" / "Assets" / "Mindforge" / "Editor" / "MindforgeDemoV11Builder.cs"
ASSEMBLER = ROOT / "unity" / "Assets" / "Mindforge" / "Editor" / "CompetitionSceneAssembler.cs"
CALIBRATION = ROOT / "unity" / "Assets" / "Mindforge" / "Calibration" / "AwakeningCalibrationDirector.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def test_latest_builder_resolves_intentionally_inactive_competition_kernel():
    builder = read(BUILDER)
    assembler = read(ASSEMBLER)
    calibration = read(CALIBRATION)

    assert "arena.SetActive(false);" in assembler
    assert "if (arenaRoot != null) arenaRoot.SetActive(false);" in calibration
    assert "if (arenaRoot != null) arenaRoot.SetActive(true);" in calibration

    assert 'GameObject.Find("Fractured_Signal_Arena")' not in builder
    assert 'GameObject.Find("The_Fractured_Signal")' not in builder
    assert 'FindSingleSceneComponent<GuardianMotor>("GuardianMotor")' in builder
    assert 'FindSingleSceneComponent<FracturedSignalDirector>("FracturedSignalDirector")' in builder
    assert 'FindSingleSceneComponent<AwakeningCalibrationDirector>("AwakeningCalibrationDirector")' in builder
    assert "Resources.FindObjectsOfTypeAll<T>()" in builder
    assert "candidate.gameObject.scene != activeScene" in builder
    assert 'serialized.FindProperty("arenaRoot")' in builder
    assert "boss.transform.IsChildOf(arena.transform)" in builder

    # Construction must preserve the calibration/controller-only lifecycle instead of
    # making the combat arena globally active just to satisfy an Editor lookup.
    assert "arena.SetActive(true)" not in builder
