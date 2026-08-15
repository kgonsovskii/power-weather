#!/usr/bin/env bash
# Local install for Power Weather (on the Ubuntu host).
# Works the same when run manually from a checkout or after remote-install.sh.
# Always: stop service → Let's Encrypt (certbot) → hard recompile → override publish/unit → start.
#
# Let's Encrypt lives ONLY here (not in remote-install.sh / deploy.ps1).
#
# Usage (from any clone):
#   ./deploy/install.sh [server-host-or-ip]
#
# Optional env:
#   APP_DIR  CFG_DIR  SERVICE_NAME  DOTNET_CHANNEL
#   LETSENCRYPT_DOMAIN  LETSENCRYPT_EMAIL  INSTALL_LOG
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

INSTALL_LOG="${INSTALL_LOG:-/var/log/power-weather-install.log}"
mkdir -p "$(dirname "${INSTALL_LOG}")"
touch "${INSTALL_LOG}"
chmod 644 "${INSTALL_LOG}" || true

exec > >(tee -a "${INSTALL_LOG}") 2>&1
echo "==== $(date -u +'%Y-%m-%dT%H:%M:%SZ') install start ===="
echo "Log file: ${INSTALL_LOG}"
echo "REPO_ROOT=${REPO_ROOT}"

SERVER_HOST="${1:-}"
if [[ -z "${SERVER_HOST}" ]]; then
  SERVER_HOST="$(hostname -I 2>/dev/null | awk '{print $1}')"
fi
if [[ -z "${SERVER_HOST}" ]]; then
  echo "server host / IP is required (arg1) or detectable via hostname -I" >&2
  exit 1
fi

APP_DIR="${APP_DIR:-/opt/power-weather}"
CFG_DIR="${CFG_DIR:-/etc/power-weather}"
SERVICE_NAME="${SERVICE_NAME:-power-weather}"
DOTNET_CHANNEL="${DOTNET_CHANNEL:-10.0}"
RID="linux-x64"

LETSENCRYPT_DOMAIN="${LETSENCRYPT_DOMAIN:-${SERVER_HOST}.sslip.io}"
LETSENCRYPT_EMAIL="${LETSENCRYPT_EMAIL:-power-weather@${LETSENCRYPT_DOMAIN}}"

ENV_FILE="${CFG_DIR}/power-weather.env"
UNIT_SRC="${REPO_ROOT}/deploy/power-weather.service"
PROJECT_PATH="${REPO_ROOT}/src/Power.Weather.Web/Power.Weather.Web.csproj"
LE_LIVE="/etc/letsencrypt/live/${LETSENCRYPT_DOMAIN}"
LE_FULLCHAIN="${LE_LIVE}/fullchain.pem"
LE_PRIVKEY="${LE_LIVE}/privkey.pem"

export DEBIAN_FRONTEND=noninteractive

if [[ ! -f "${PROJECT_PATH}" ]]; then
  echo "Project not found: ${PROJECT_PATH}" >&2
  echo "Run via remote-install.sh or from a full repo checkout." >&2
  exit 1
fi
if [[ ! -f "${UNIT_SRC}" ]]; then
  echo "systemd unit not found: ${UNIT_SRC}" >&2
  exit 1
fi

stop_service() {
  echo "Stopping ${SERVICE_NAME} (override install always stops first)..."
  systemctl stop "${SERVICE_NAME}" 2>/dev/null || true
  # Ensure ports are free for certbot / new process.
  sleep 1
}

ensure_packages() {
  apt-get update -y
  apt-get install -y --no-install-recommends ca-certificates curl wget openssl
}

dotnet_has_channel() {
  local channel="$1"
  command -v dotnet >/dev/null 2>&1 || return 1
  dotnet --list-sdks 2>/dev/null | grep -E "^${channel/./\\.}" >/dev/null 2>&1
}

ensure_dotnet_sdk() {
  export DOTNET_ROOT="${DOTNET_ROOT:-/usr/share/dotnet}"
  export PATH="${DOTNET_ROOT}:${PATH}"

  if [[ -x "${DOTNET_ROOT}/dotnet" && ! -x /usr/local/bin/dotnet ]]; then
    ln -sf "${DOTNET_ROOT}/dotnet" /usr/local/bin/dotnet
  fi

  if dotnet_has_channel "${DOTNET_CHANNEL}"; then
    echo "dotnet SDK ${DOTNET_CHANNEL}.x present: $(dotnet --list-sdks | tr '\n' ' ')"
    return
  fi

  echo "dotnet SDK ${DOTNET_CHANNEL} not found — installing..."
  local installer="/tmp/dotnet-install.sh"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "${installer}"
  bash "${installer}" --channel "${DOTNET_CHANNEL}" --install-dir "${DOTNET_ROOT}"
  ln -sf "${DOTNET_ROOT}/dotnet" /usr/local/bin/dotnet
  export PATH="${DOTNET_ROOT}:${PATH}"

  if ! dotnet_has_channel "${DOTNET_CHANNEL}"; then
    echo "Failed to install dotnet SDK ${DOTNET_CHANNEL}" >&2
    dotnet --info || true
    exit 1
  fi
  echo "dotnet SDK installed: $(dotnet --list-sdks | tr '\n' ' ')"
}

ensure_certbot() {
  if command -v certbot >/dev/null 2>&1; then
    echo "certbot: $(certbot --version 2>&1 | head -n1)"
    return
  fi
  echo "certbot not found — installing..."
  apt-get install -y certbot
  echo "certbot: $(certbot --version 2>&1 | head -n1)"
}

write_app_env() {
  mkdir -p "${CFG_DIR}"
  cat > "${ENV_FILE}" <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:80;https://0.0.0.0:443
ASPNETCORE_Kestrel__Certificates__Default__Path=${LE_FULLCHAIN}
ASPNETCORE_Kestrel__Certificates__Default__KeyPath=${LE_PRIVKEY}
DOTNET_ROOT=${DOTNET_ROOT:-/usr/share/dotnet}
DOTNET_PRINT_TELEMETRY_MESSAGE=false
EOF
  chmod 600 "${ENV_FILE}"
  echo "Wrote ${ENV_FILE} (Let's Encrypt PEM for ${LETSENCRYPT_DOMAIN})"
}

ensure_letsencrypt() {
  echo "Ensuring Let's Encrypt certificate for ${LETSENCRYPT_DOMAIN}..."
  rm -rf /etc/power-weather/certs
  ensure_certbot

  if [[ -f "${LE_FULLCHAIN}" && -f "${LE_PRIVKEY}" ]]; then
    echo "Existing LE cert found — keep/renew if needed..."
    certbot renew --cert-name "${LETSENCRYPT_DOMAIN}" --standalone \
      --preferred-challenges http \
      --non-interactive \
      --deploy-hook "systemctl try-restart ${SERVICE_NAME}" \
      || true
  else
    certbot certonly --standalone --preferred-challenges http \
      -d "${LETSENCRYPT_DOMAIN}" \
      --agree-tos --email "${LETSENCRYPT_EMAIL}" --non-interactive \
      --keep-until-expiring \
      --deploy-hook "systemctl try-restart ${SERVICE_NAME}"
  fi

  if [[ ! -f "${LE_FULLCHAIN}" || ! -f "${LE_PRIVKEY}" ]]; then
    echo "Let's Encrypt files missing under ${LE_LIVE}" >&2
    exit 1
  fi

  systemctl enable --now certbot.timer 2>/dev/null || true
  write_app_env
}

clean_and_publish() {
  echo "Hard recompile + override publish → ${APP_DIR}"
  export DOTNET_ROOT="${DOTNET_ROOT:-/usr/share/dotnet}"
  export PATH="${DOTNET_ROOT}:${PATH}"

  # Wipe previous published bits and local build outputs so the new version fully replaces the old one.
  rm -rf "${APP_DIR}"
  mkdir -p "${APP_DIR}"
  find "${REPO_ROOT}/src" -type d \( -name bin -o -name obj \) -print0 2>/dev/null \
    | xargs -0 -r rm -rf

  dotnet publish "${PROJECT_PATH}" \
    --configuration Release \
    --runtime "${RID}" \
    --self-contained true \
    --force \
    -p:PublishSingleFile=false \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    --output "${APP_DIR}"

  if [[ ! -x "${APP_DIR}/Power.Weather.Web" && ! -f "${APP_DIR}/Power.Weather.Web" ]]; then
    echo "Publish output missing executable: ${APP_DIR}/Power.Weather.Web" >&2
    exit 1
  fi

  chmod +x "${APP_DIR}/Power.Weather.Web"
  find "${APP_DIR}" -type f -name "*.so" -exec chmod 755 {} \; || true
  find "${APP_DIR}" -type d -exec chmod 755 {} \;
  echo "Publish override complete."
}

install_service() {
  echo "Overriding systemd unit from ${UNIT_SRC}..."
  cp -f "${UNIT_SRC}" "/etc/systemd/system/${SERVICE_NAME}.service"
  chmod 644 "/etc/systemd/system/${SERVICE_NAME}.service"

  systemctl daemon-reload
  systemctl enable "${SERVICE_NAME}"
  systemctl restart "${SERVICE_NAME}"
  sleep 2
  systemctl --no-pager --full status "${SERVICE_NAME}" || true

  if command -v ufw >/dev/null 2>&1; then
    ufw allow 80/tcp || true
    ufw allow 443/tcp || true
  fi
}

smoke_check() {
  curl -sS -o /dev/null -w "HTTP  %{http_code}\n" --max-time 8 "http://127.0.0.1/" || true
  curl -sS -o /dev/null -w "HTTPS %{http_code}\n" --max-time 8 "https://${LETSENCRYPT_DOMAIN}/" || true
  echo "Install finished."
  echo "  http://${SERVER_HOST}/"
  echo "  https://${LETSENCRYPT_DOMAIN}/  (Let's Encrypt)"
  echo "==== $(date -u +'%Y-%m-%dT%H:%M:%SZ') install done (log: ${INSTALL_LOG}) ===="
}

echo "== Power Weather install (local) =="
stop_service
ensure_packages
ensure_dotnet_sdk
ensure_letsencrypt
clean_and_publish
install_service
smoke_check
