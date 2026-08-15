#!/usr/bin/env bash
# Entry point from deploy.ps1 (SCP + SSH) or manual run on the server.
# Ensures git, wipes SRC_DIR, clones REPO_URL, runs local install.sh.
#
# Usage:
#   remote-install.sh [server-host-or-ip]
#
# Optional env:
#   REPO_URL  SRC_DIR  REPO_BRANCH
set -euo pipefail

export DEBIAN_FRONTEND=noninteractive

REPO_URL="${REPO_URL:-https://github.com/kgonsovskii/power-weather.git}"
SRC_DIR="${SRC_DIR:-/opt/power-weather-src}"
REPO_BRANCH="${REPO_BRANCH:-main}"
SERVER_HOST="${1:-}"

echo "== Power Weather remote-install =="
echo "REPO_URL=${REPO_URL}"
echo "SRC_DIR=${SRC_DIR}"
echo "BRANCH=${REPO_BRANCH}"

if ! command -v git >/dev/null 2>&1; then
  echo "git not found — installing..."
  apt-get update -y
  apt-get install -y --no-install-recommends git ca-certificates
fi
echo "git: $(git --version)"

echo "Cleaning ${SRC_DIR} and cloning ${REPO_URL} (${REPO_BRANCH})..."
rm -rf "${SRC_DIR}"
mkdir -p "$(dirname "${SRC_DIR}")"
git clone --depth 1 --branch "${REPO_BRANCH}" "${REPO_URL}" "${SRC_DIR}"

INSTALL_SH="${SRC_DIR}/deploy/install.sh"
if [[ ! -f "${INSTALL_SH}" ]]; then
  echo "install.sh missing after clone: ${INSTALL_SH}" >&2
  exit 1
fi

echo "Handing off to ${INSTALL_SH}"

set +e
if [[ -n "${SERVER_HOST}" ]]; then
  "${INSTALL_SH}" "${SERVER_HOST}"
else
  "${INSTALL_SH}"
fi
code=$?
set -e

echo "---- install.log (tail) ----"
tail -n 80 /var/log/power-weather-install.log 2>/dev/null || true
exit "${code}"
