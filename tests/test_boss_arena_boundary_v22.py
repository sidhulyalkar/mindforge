from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"
BOUNDARY = UNITY / "Combat" / "FracturedSignalArenaBoundaryV22.cs"
V11 = UNITY / "Editor" / "MindforgeDemoV11Builder.cs"
V21 = UNITY / "Editor" / "WorldCohesionV21Builder.cs"
SMOKE = UNITY / "Tests" / "Editor" / "FracturedSignalArenaBoundaryV22SmokeTests.cs"


def read(path: Path) -> str:
    assert path.exists(), f"missing arena-boundary source: {path}"
    return path.read_text(encoding="utf-8")


def test_v22_replaces_accidental_closed_ring_with_one_south_doorway():
    source = read(BOUNDARY)
    v11 = read(V11)
    v21 = read(V21)

    assert "const int segments = 14" in v11
    assert 'Block($"FractureWall_{i:00}"' in v11
    assert "ArenaWallRadius = 18.3f" in v21
    assert 'private const float SouthDoorZ = 75.70f' in source
    assert 'child.name.StartsWith("FractureWall_"' in source
    assert "if (segment == 7)" in source
    assert "child.gameObject.SetActive(false)" in source
    assert "scale.y = Mathf.Max(scale.y, wallHeight)" in source
    assert "position.y = FloorTopY + scale.y * 0.5f" in source


def test_v22_gate_is_visible_opaque_world_geometry_backed_by_one_matching_collision_plane():
    source = read(BOUNDARY)
    for token in (
        'GateName = "V22_Arena_Entrance_Gate"',
        '"GatePillarL"',
        '"GatePillarR"',
        '"GateLintel"',
        '"GateBar_',
        '"GateCrossbar_',
        '"GateSealCollision"',
        "GameObject.CreatePrimitive(PrimitiveType.Cube)",
        "renderer.sharedMaterial = _wallMaterial",
        "BoxCollider",
        "_sealCollider.enabled = false",
    ):
        assert token in source

    # Cosmetic bars never become a picket-fence collision exploit. One full-width collider
    # matches the visible closed gate and is enabled only once the bars arrive.
    assert "cosmeticCollider.enabled = false" in source
    assert "doorwayHalfWidth * 1.90f" in source
    assert "Mathf.Abs(nextY - gateClosedCenterY) <= 0.18f" in source


def test_v22_gate_closes_only_after_encounter_entry_and_reopens_on_boss_death():
    source = read(BOUNDARY)
    for token in (
        "encounterReleaseZ = 82f",
        "_guardian.position.z >= encounterReleaseZ",
        "_vitals.IsAlive",
        "_encounterEntered = true",
        "bool shouldClose = _encounterEntered && _vitals.IsAlive",
        "_vitals.Died += OnBossDied",
        "_vitals.Died -= OnBossDied",
        "_encounterEntered = false",
        "Fractured Signal defeated; chamber entrance reopened",
    ):
        assert token in source

    assert "NeuralEvent" not in source
    assert "UdpNeuralReceiver" not in source
    assert "SetExternalPause(" not in source
    assert "ReceiveDamage(" not in source


def test_v22_gate_moves_as_one_group_instead_of_only_moving_first_bar():
    source = read(BOUNDARY)
    assert "float currentY = _bars[0].localPosition.y" in source
    assert "float deltaY = nextY - currentY" in source
    assert "p.y += deltaY" in source
    bad = "p.y += nextY - _bars[0].localPosition.y"
    assert bad not in source


def test_v22_arena_boundary_has_unity_construction_smoke_and_pinned_guid():
    smoke = read(SMOKE)
    assert "V22ArenaBoundary_CanBeConstructedByUnity" in smoke
    assert "AddComponent<FracturedSignalArenaBoundaryV22>()" in smoke

    meta = read(BOUNDARY.with_suffix(".cs.meta"))
    assert "fileFormatVersion: 2" in meta
    guid = next(line.split(":", 1)[1].strip() for line in meta.splitlines() if line.startswith("guid: "))
    assert len(guid) == 32
