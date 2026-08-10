#!/usr/bin/env bash
set -euo pipefail

# HouseKeeper Codex cloud environment bootstrap.
# This script is intentionally Linux/Bash-only because Codex cloud environments
# run in the universal Ubuntu image. Local developer workflows remain covered by
# the existing Bash/PowerShell scripts.

readonly DOTNET_SDK_VERSION="10.0.300"
readonly DOTNET_INSTALL_DIR="${HOME}/.dotnet"
readonly NODE_MAJOR_VERSION="22"
readonly CDK_CLI_VERSION="2.1132.1"
readonly POSTGRES_MAJOR_VERSION="18"
readonly POSTGRES_CLUSTER="main"
readonly POSTGRES_DB="housekeeper"
readonly POSTGRES_USER="housekeeper"
readonly POSTGRES_PASSWORD="housekeeper_codex"

run_root() {
  if [[ "$(id -u)" -eq 0 ]]; then
    "$@"
  else
    sudo "$@"
  fi
}

run_as_postgres() {
  if [[ "$(id -u)" -eq 0 ]]; then
    runuser -u postgres -- "$@"
  else
    sudo -u postgres "$@"
  fi
}

ensure_bashrc_line() {
  local line="$1"
  touch "${HOME}/.bashrc"
  grep -Fqx "$line" "${HOME}/.bashrc" || printf '%s\n' "$line" >> "${HOME}/.bashrc"
}

install_dotnet() {
  if [[ -x "${DOTNET_INSTALL_DIR}/dotnet" ]] \
    && "${DOTNET_INSTALL_DIR}/dotnet" --list-sdks | grep -q "^${DOTNET_SDK_VERSION//./\\.} "; then
    return
  fi

  curl --fail --silent --show-error --location \
    https://dot.net/v1/dotnet-install.sh \
    --output /tmp/dotnet-install.sh

  bash /tmp/dotnet-install.sh \
    --version "${DOTNET_SDK_VERSION}" \
    --install-dir "${DOTNET_INSTALL_DIR}" \
    --no-path
}

configure_dotnet_shell() {
  export DOTNET_ROOT="${DOTNET_INSTALL_DIR}"
  export PATH="${DOTNET_ROOT}:${DOTNET_ROOT}/tools:${PATH}"

  ensure_bashrc_line 'export DOTNET_ROOT="$HOME/.dotnet"'
  ensure_bashrc_line 'export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"'
}

configure_node() {
  local current_major=""

  if command -v node >/dev/null 2>&1; then
    current_major="$(node --version | sed -E 's/^v([0-9]+).*/\1/')"
  fi

  if [[ "${current_major}" == "${NODE_MAJOR_VERSION}" ]]; then
    return
  fi

  if ! command -v mise >/dev/null 2>&1; then
    echo "Node.js ${NODE_MAJOR_VERSION} is required and mise is unavailable." >&2
    echo "Pin Node.js ${NODE_MAJOR_VERSION} in the Codex environment package-version settings." >&2
    exit 1
  fi

  mise use --global "node@${NODE_MAJOR_VERSION}"
  hash -r

  current_major="$(node --version | sed -E 's/^v([0-9]+).*/\1/')"
  if [[ "${current_major}" != "${NODE_MAJOR_VERSION}" ]]; then
    echo "Unable to activate Node.js ${NODE_MAJOR_VERSION}." >&2
    exit 1
  fi
}

install_postgresql() {
  if command -v pg_config >/dev/null 2>&1 \
    && [[ "$(pg_config --version | awk '{print $2}' | cut -d. -f1)" == "${POSTGRES_MAJOR_VERSION}" ]]; then
    return
  fi

  run_root apt-get update
  run_root apt-get install -y --no-install-recommends \
    ca-certificates \
    curl \
    gnupg \
    postgresql-common

  # Ubuntu 24.04 does not ship PostgreSQL 18. Add the official PGDG repository.
  run_root /usr/share/postgresql-common/pgdg/apt.postgresql.org.sh -y
  run_root apt-get update
  run_root apt-get install -y --no-install-recommends "postgresql-${POSTGRES_MAJOR_VERSION}"
}

start_postgresql() {
  if ! pg_lsclusters --no-header 2>/dev/null \
    | awk -v version="${POSTGRES_MAJOR_VERSION}" -v cluster="${POSTGRES_CLUSTER}" \
      '$1 == version && $2 == cluster { found = 1 } END { exit(found ? 0 : 1) }'; then
    run_root pg_createcluster "${POSTGRES_MAJOR_VERSION}" "${POSTGRES_CLUSTER}"
  fi

  if ! pg_lsclusters --no-header \
    | awk -v version="${POSTGRES_MAJOR_VERSION}" -v cluster="${POSTGRES_CLUSTER}" \
      '$1 == version && $2 == cluster && $4 == "online" { found = 1 } END { exit(found ? 0 : 1) }'; then
    run_root pg_ctlcluster "${POSTGRES_MAJOR_VERSION}" "${POSTGRES_CLUSTER}" start
  fi

  for _ in {1..30}; do
    if pg_isready --host 127.0.0.1 --port 5432 >/dev/null 2>&1; then
      break
    fi
    sleep 1
  done

  pg_isready --host 127.0.0.1 --port 5432

  if ! run_as_postgres psql --tuples-only --no-align --command \
    "SELECT 1 FROM pg_roles WHERE rolname = '${POSTGRES_USER}'" \
    | grep -qx '1'; then
    run_as_postgres createuser --login "${POSTGRES_USER}"
  fi

  run_as_postgres psql --set ON_ERROR_STOP=1 --command \
    "ALTER ROLE ${POSTGRES_USER} WITH LOGIN PASSWORD '${POSTGRES_PASSWORD}';"

  if ! run_as_postgres psql --tuples-only --no-align --command \
    "SELECT 1 FROM pg_database WHERE datname = '${POSTGRES_DB}'" \
    | grep -qx '1'; then
    run_as_postgres createdb --owner "${POSTGRES_USER}" "${POSTGRES_DB}"
  fi
}

install_powershell() {
  if command -v pwsh >/dev/null 2>&1; then
    return
  fi

  # Playwright's .NET installer is distributed as a PowerShell script.
  . /etc/os-release
  curl --fail --silent --show-error --location \
    "https://packages.microsoft.com/config/ubuntu/${VERSION_ID}/packages-microsoft-prod.deb" \
    --output /tmp/packages-microsoft-prod.deb

  run_root dpkg -i /tmp/packages-microsoft-prod.deb
  run_root apt-get update
  run_root apt-get install -y --no-install-recommends powershell
}

install_cdk() {
  local current=""

  if command -v cdk >/dev/null 2>&1; then
    current="$(cdk --version | awk '{print $1}')"
  fi

  if [[ "${current}" != "${CDK_CLI_VERSION}" ]]; then
    npm install --global "aws-cdk@${CDK_CLI_VERSION}"
  fi
}

prepare_repository_dependencies() {
  dotnet tool restore
  dotnet restore HouseKeeper.slnx

  # Pre-build the E2E project so Playwright's pinned installer exists while the
  # setup phase still has internet access. Browser downloads are then cached for
  # later network-disabled review tasks.
  dotnet build tests/HouseKeeper.EndToEndTests/HouseKeeper.EndToEndTests.csproj \
    --configuration Release \
    --no-restore

  pwsh tests/HouseKeeper.EndToEndTests/bin/Release/net10.0/playwright.ps1 \
    install --with-deps chromium
}

main() {
  install_dotnet
  configure_dotnet_shell
  configure_node
  install_postgresql
  start_postgresql
  install_powershell
  install_cdk
  prepare_repository_dependencies

  printf '\nHouseKeeper Codex environment ready.\n'
  dotnet --version
  node --version
  cdk --version
  psql --version
  pwsh --version
}

main "$@"
