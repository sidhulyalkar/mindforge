#!/usr/bin/env bash
set -euo pipefail

# Mindforge V0.29 playable-chassis bootstrap.
#
# This materializes the complete upstream Dragon Souls Unity project locally at
# an exact immutable commit. The checkout is intentionally git-ignored because
# the upstream repository contains a large collection of third-party art/audio
# whose individual redistribution terms must be audited separately from the
# upstream project's MIT-licensed source code.

UPSTREAM_URL="https://github.com/btuhany/DragonSouls-Unity3D.git"
UPSTREAM_COMMIT="f54824255517801d5d3443848e1e4275d8d5066d"
EXPECTED_UNITY="2021.3.20f1"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
CHECKOUT_ROOT="${REPO_ROOT}/external/DragonSouls-Unity3D"
PROJECT_ROOT="${CHECKOUT_ROOT}/ThirdPersonCombat"
OVERLAY_TOOL="${REPO_ROOT}/tools/apply_dragonsouls_overlay.py"

REFRESH=0
APPLY_OVERLAY=1
PRINT_ONLY=0

usage() {
  cat <<'EOF'
Usage: tools/bootstrap_dragonsouls_chassis.sh [options]

Options:
  --refresh       Discard the existing external checkout and materialize it again.
  --no-overlay    Do not copy the tracked Mindforge overlay into the chassis project.
  --print-path    Print the Unity project path and exit after validation.
  -h, --help      Show this help.

The resulting Unity project is:
  external/DragonSouls-Unity3D/ThirdPersonCombat

Open that directory with Unity 2021.3.20f1. Do not upgrade the project on the
first qualification run; V0.29 deliberately preserves the upstream known-good
engine/package combination before we modernize anything.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --refresh) REFRESH=1 ;;
    --no-overlay) APPLY_OVERLAY=0 ;;
    --print-path) PRINT_ONLY=1 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1" >&2; usage >&2; exit 2 ;;
  esac
  shift
done

command -v git >/dev/null 2>&1 || { echo "git is required" >&2; exit 1; }
command -v python3 >/dev/null 2>&1 || { echo "python3 is required" >&2; exit 1; }

if [[ ${REFRESH} -eq 1 && -d "${CHECKOUT_ROOT}" ]]; then
  echo "[Mindforge:V29] Removing existing chassis checkout..."
  rm -rf "${CHECKOUT_ROOT}"
fi

if [[ ! -d "${CHECKOUT_ROOT}/.git" ]]; then
  echo "[Mindforge:V29] Materializing Dragon Souls at ${UPSTREAM_COMMIT}..."
  mkdir -p "$(dirname "${CHECKOUT_ROOT}")"
  git clone --filter=blob:none --no-checkout "${UPSTREAM_URL}" "${CHECKOUT_ROOT}"
  git -C "${CHECKOUT_ROOT}" fetch --depth=1 origin "${UPSTREAM_COMMIT}"
  git -C "${CHECKOUT_ROOT}" checkout --detach "${UPSTREAM_COMMIT}"
else
  current="$(git -C "${CHECKOUT_ROOT}" rev-parse HEAD)"
  if [[ "${current}" != "${UPSTREAM_COMMIT}" ]]; then
    echo "[Mindforge:V29] Existing chassis is ${current}; restoring pinned ${UPSTREAM_COMMIT}."
    git -C "${CHECKOUT_ROOT}" fetch --depth=1 origin "${UPSTREAM_COMMIT}"
    git -C "${CHECKOUT_ROOT}" checkout --detach "${UPSTREAM_COMMIT}"
  fi
fi

actual="$(git -C "${CHECKOUT_ROOT}" rev-parse HEAD)"
if [[ "${actual}" != "${UPSTREAM_COMMIT}" ]]; then
  echo "Pinned checkout verification failed: expected ${UPSTREAM_COMMIT}, got ${actual}" >&2
  exit 1
fi

if [[ ! -f "${CHECKOUT_ROOT}/LICENSE" ]] || ! grep -q "MIT License" "${CHECKOUT_ROOT}/LICENSE"; then
  echo "Dragon Souls MIT license notice is missing from the pinned checkout." >&2
  exit 1
fi

version_file="${PROJECT_ROOT}/ProjectSettings/ProjectVersion.txt"
if [[ ! -f "${version_file}" ]]; then
  echo "Dragon Souls Unity project is incomplete: ${version_file} missing." >&2
  exit 1
fi

actual_unity="$(sed -n 's/^m_EditorVersion: //p' "${version_file}" | head -n 1)"
if [[ "${actual_unity}" != "${EXPECTED_UNITY}" ]]; then
  echo "Unexpected Dragon Souls Unity version: expected ${EXPECTED_UNITY}, got ${actual_unity}" >&2
  exit 1
fi

if [[ ${APPLY_OVERLAY} -eq 1 ]]; then
  python3 "${OVERLAY_TOOL}" --project "${PROJECT_ROOT}" --source-commit "${UPSTREAM_COMMIT}"
fi

python3 - "${PROJECT_ROOT}" "${UPSTREAM_URL}" "${UPSTREAM_COMMIT}" "${EXPECTED_UNITY}" <<'PY'
import json
import pathlib
import sys
from datetime import datetime, timezone

project = pathlib.Path(sys.argv[1])
record = {
    "schema": "mindforge.dragonsouls_chassis.v1",
    "upstream": sys.argv[2],
    "source_commit": sys.argv[3],
    "unity_version": sys.argv[4],
    "project_subdir": "ThirdPersonCombat",
    "materialized_utc": datetime.now(timezone.utc).isoformat(),
    "third_party_art_status": "local_upstream_checkout_requires_individual_license_audit",
}
(project / ".mindforge_chassis.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
PY

if [[ ${PRINT_ONLY} -eq 1 ]]; then
  printf '%s\n' "${PROJECT_ROOT}"
  exit 0
fi

cat <<EOF

[Mindforge:V29] Dragon Souls chassis ready.

Project: ${PROJECT_ROOT}
Commit:  ${UPSTREAM_COMMIT}
Unity:   ${EXPECTED_UNITY}

Open the project in Unity Hub with Unity ${EXPECTED_UNITY}, then use:
  Mindforge > Chassis > PLAY MAIN GAME

The first objective is to prove the untouched chassis plays correctly before
we replace its presentation and attach Mindforge BCI semantics.
EOF
