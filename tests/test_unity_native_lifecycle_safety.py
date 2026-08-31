import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY_RUNTIME = ROOT / "unity/Assets/Mindforge"

# UnityEngine native wrappers may invoke CreateImpl/native allocation. Constructing these from
# MonoBehaviour instance field initializers is unsafe because Unity can deserialize/construct the
# managed object outside Awake/OnEnable/Start. Keep allocation inside explicit lifecycle methods.
_NATIVE_TYPES = (
    "MaterialPropertyBlock",
    "Material",
    "Texture2D",
    "RenderTexture",
    "Mesh",
    "GameObject",
    "ComputeBuffer",
    "GraphicsBuffer",
)
_NATIVE_FIELD_INITIALIZER = re.compile(
    r"^\s*(?:private|protected|public|internal)\s+"
    r"(?:(?:static|readonly|volatile)\s+)*"
    r"[\w<>,.?\[\]]+\s+\w+\s*=\s*new\s+"
    r"(" + "|".join(_NATIVE_TYPES) + r")\s*\("
)


def runtime_sources():
    for path in UNITY_RUNTIME.rglob("*.cs"):
        if "Editor" in path.parts:
            continue
        yield path


def test_runtime_sources_do_not_construct_native_unity_objects_in_field_initializers():
    offenders = []
    for path in runtime_sources():
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
            match = _NATIVE_FIELD_INITIALIZER.match(line)
            if match:
                offenders.append(f"{path.relative_to(ROOT)}:{number}: {line.strip()}")

    assert not offenders, (
        "Native Unity objects must be allocated in Awake/OnEnable/Start or an explicit runtime "
        "build method, never from instance/static field initialization:\n" + "\n".join(offenders)
    )


def test_v16_material_hierarchy_allocates_property_block_in_unity_lifecycle():
    path = UNITY_RUNTIME / "Presentation/LegacyMaterialHierarchyV16.cs"
    text = path.read_text(encoding="utf-8")
    assert "private MaterialPropertyBlock _block;" in text
    assert "private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();" not in text
    awake = text.split("private void Awake()", 1)[1].split("public void Configure", 1)[0]
    assert "_block = new MaterialPropertyBlock();" in awake
