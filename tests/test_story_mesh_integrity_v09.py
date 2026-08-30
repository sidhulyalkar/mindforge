from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
MESHES = ROOT / "unity/Assets/Mindforge/Editor/ProductionStoryMeshLibraryV09.cs"


def text() -> str:
    assert MESHES.exists()
    return MESHES.read_text(encoding="utf-8")


def test_convex_story_meshes_self_validate_outward_winding():
    src = text()
    assert 'AssertConvexOutward(vertices, triangles, "BrokenSlab")' in src
    assert 'AssertConvexOutward(vertices, triangles, "SignalShard")' in src
    assert "Vector3.Dot(face, centroid - center) <= 0f" in src
    assert "degenerate or inward-facing triangle" in src


def test_broken_slab_top_bottom_and_side_winding_are_explicitly_outward():
    src = text()
    assert "triangles.Add(topCenter); triangles.Add(next); triangles.Add(i);" in src
    assert "triangles.Add(bottomCenter); triangles.Add(5 + i); triangles.Add(5 + next);" in src
    assert "AddQuad(triangles, i, next, 5 + next, 5 + i);" in src


def test_signal_shard_winding_is_the_outward_orientation():
    src = text()
    assert "0,2,1, 0,3,2, 0,4,3, 0,1,4" in src
    assert "5,1,2, 5,2,3, 5,3,4, 5,4,1" in src


def test_ribbon_uses_real_thickness_and_independent_front_back_vertices():
    src = text()
    ribbon = src.split("private static Mesh BuildHangingRibbon", 1)[1].split(
        "private static Mesh BuildCableArc", 1
    )[0]
    assert "const float thickness = 0.018f" in ribbon
    assert "(verticalSegments + 1) * 4" in ribbon
    assert "z - thickness" in ribbon
    assert "z + thickness" in ribbon
    assert "AddQuad(triangles, fl, fr, nfr, nfl)" in ribbon
    assert "AddQuad(triangles, bl, nbl, nbr, br)" in ribbon
    assert "Double-sided without requiring" not in ribbon


def test_all_generated_story_mesh_normals_are_fail_closed():
    src = text()
    assert "mesh.RecalculateNormals();" in src
    assert "Vector3[] normals = mesh.normals;" in src
    assert "!IsFinite(normals[i]) || normals[i].sqrMagnitude < 0.25f" in src
    assert "Generated story mesh contains an invalid normal" in src
    assert "float.IsNaN" in src
    assert "float.IsInfinity" in src
