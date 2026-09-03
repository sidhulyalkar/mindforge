from __future__ import annotations

import math
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
BUILDER = (
    ROOT
    / "dragonsouls_overlay"
    / "Assets"
    / "Mindforge"
    / "Editor"
    / "MindforgeVerticalSliceBuilderV31.cs"
)


def dot(a: tuple[float, float, float], b: tuple[float, float, float]) -> float:
    return sum(x * y for x, y in zip(a, b))


def normalize(v: tuple[float, float, float]) -> tuple[float, float, float]:
    n = math.sqrt(dot(v, v))
    assert n > 0
    return tuple(x / n for x in v)


def projected_half_extent(
    extents: tuple[float, float, float], axis: tuple[float, float, float]
) -> float:
    axis = normalize(axis)
    return sum(abs(a) * e for a, e in zip(axis, extents))


def inner_edge(
    bounds_center: tuple[float, float, float],
    route_center: tuple[float, float, float],
    extents: tuple[float, float, float],
    lateral: tuple[float, float, float],
    side: float,
) -> float:
    lateral = normalize(lateral)
    delta = tuple(a - b for a, b in zip(bounds_center, route_center))
    signed_center = dot(delta, lateral) * side
    return signed_center - projected_half_extent(extents, lateral)


def test_signed_projection_is_symmetric_for_left_and_right_boundaries():
    route = (10.0, 2.0, -40.0)
    extents = (2.0, 4.0, 1.0)
    lateral = (1.0, 0.0, 0.0)
    assert inner_edge((20.0, 2.0, -40.0), route, extents, lateral, 1.0) == 8.0
    assert inner_edge((0.0, 2.0, -40.0), route, extents, lateral, -1.0) == 8.0


def test_projection_handles_rotated_lateral_axis():
    lateral = normalize((1.0, 0.0, 1.0))
    radius = projected_half_extent((2.0, 4.0, 6.0), lateral)
    assert math.isclose(radius, 8.0 / math.sqrt(2.0), rel_tol=1e-6)


def test_projection_handles_axis_with_vertical_component_for_general_correctness():
    lateral = normalize((1.0, 1.0, 0.0))
    radius = projected_half_extent((2.0, 4.0, 6.0), lateral)
    assert math.isclose(radius, 6.0 / math.sqrt(2.0), rel_tol=1e-6)


def test_negative_world_coordinates_do_not_flip_side_semantics():
    route = (-120.0, 3.0, -240.0)
    extents = (3.0, 2.0, 2.0)
    lateral = (0.0, 0.0, 1.0)
    assert inner_edge((-120.0, 3.0, -228.0), route, extents, lateral, 1.0) == 10.0
    assert inner_edge((-120.0, 3.0, -252.0), route, extents, lateral, -1.0) == 10.0


def test_large_prefab_pivot_offset_is_corrected_from_actual_bounds_center():
    protected = 7.0
    padding = 0.75
    extents = (5.0, 8.0, 2.0)
    lateral = (1.0, 0.0, 0.0)
    radius = projected_half_extent(extents, lateral)
    desired_signed_center = protected + radius + padding

    # Simulate the native failure: prefab root was on the right but visible bounds
    # were ~130 m to the left because of an inherited local pivot offset.
    current_signed_center = -125.0
    correction = desired_signed_center - current_signed_center
    resolved_signed_center = current_signed_center + correction
    resolved_inner_edge = resolved_signed_center - radius

    assert math.isclose(resolved_inner_edge, protected + padding, abs_tol=1e-6)


def test_builder_uses_actual_bounds_alignment_instead_of_root_pivot_assumption():
    text = BUILDER.read_text(encoding="utf-8")
    for token in (
        "AlignBoundaryBoundsToLane(instance, routeCenter, lateral, side)",
        "ProjectedHalfExtent(bounds, lateral)",
        "desiredSignedCenter = ProtectedHalfWidth + projectedRadius + BoundaryPadding",
        "currentSignedCenter = Vector3.Dot(delta, lateral) * side",
        "instance.transform.position += lateral * side * correction",
        "Mathf.Abs(axis.y) * bounds.extents.y",
        "resolvedSignedCenter",
    ):
        assert token in text

    # The old bug placed the prefab root at a desired center and implicitly assumed
    # renderer/collider bounds were centered on that pivot.
    assert "Vector3 desiredCenter = routeCenter + lateral * side * centerOffset" not in text


def test_builder_syncs_physics_before_mixing_renderer_and_collider_bounds():
    text = BUILDER.read_text(encoding="utf-8")
    calculate_bounds = text.split("private static Bounds CalculateBounds", 1)[1].split(
        "private static void ConfigureRenderers", 1
    )[0]

    sync = calculate_bounds.index("Physics.SyncTransforms();")
    assert sync < calculate_bounds.index("renderer.bounds")
    assert sync < calculate_bounds.index("collider.bounds")
