#!/usr/bin/env bash
set -euo pipefail

# Refresh a cached HouseKeeper Codex cloud environment after Codex checks out the
# branch being reviewed. Heavy tool/browser installation belongs in setup; this
# script only re-establishes runtime state and restores branch-specific packages.

readonly DOTNET_INSTALL_DIR="${HOME}/.dotnet"
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

configure_dotnet_shell() {
  export DOTNET_ROOT="${DOTNET_INSTALL_DIR}"
  export PATH="${DOTNET_ROOT}:${DOTNET_ROOT}/tools:${PATH}"
}

start_postgresql() {
  if ! pg_lsclusters --no-header 2>/dev/null \
    | awk -v version="${POSTGRES_MAJOR_VERSION}" -v cluster="${POSTGRES_CLUSTER}" \
      '$1 == version && $2 == cluster { found = 1 } END { exit(found ? 0 : 1) }'; then
    echo "PostgreSQL ${POSTGRES_MAJOR_VERSION} cluster is missing; reset the Codex environment cache." >&2
    exit 1
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

  # Reassert the disposable review database contract in case a previous task
  # altered local role state. This is never a production credential.
  run_as_postgres psql --set ON_ERROR_STOP=1 --command \
    "ALTER ROLE ${POSTGRES_USER} WITH LOGIN PASSWORD '${POSTGRES_PASSWORD}';"

  if ! run_as_postgres psql --tuples-only --no-align --command \
    "SELECT 1 FROM pg_database WHERE datname = '${POSTGRES_DB}'" \
    | grep -qx '1'; then
    run_as_postgres createdb --owner "${POSTGRES_USER}" "${POSTGRES_DB}"
  fi
}

main() {
  configure_dotnet_shell
  start_postgresql

  dotnet tool restore
  dotnet restore HouseKeeper.slnx

  printf '\nHouseKeeper cached Codex environment refreshed.\n'
  dotnet --version
  psql --version
}

main "$@"
