#!/usr/bin/env bash
set -euo pipefail

# Refresh a cached HouseKeeper Codex environment after another branch is checked
# out. Heavy installation belongs in codex-setup.sh.

readonly PNPM_VERSION="11.23.0"
readonly POSTGRES_MAJOR_VERSION="18"
readonly POSTGRES_CLUSTER="main"
readonly POSTGRES_DB="housekeeper"
readonly POSTGRES_USER="housekeeper"
readonly POSTGRES_PASSWORD="housekeeper_codex"
readonly DATABASE_URL="postgres://${POSTGRES_USER}:${POSTGRES_PASSWORD}@127.0.0.1:5432/${POSTGRES_DB}"

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

start_postgresql() {
  if ! pg_lsclusters --no-header 2>/dev/null \
    | awk -v version="${POSTGRES_MAJOR_VERSION}" -v cluster="${POSTGRES_CLUSTER}" \
      '$1 == version && $2 == cluster { found = 1 } END { exit(found ? 0 : 1) }'; then
    echo "PostgreSQL ${POSTGRES_MAJOR_VERSION} is missing; reset the Codex environment cache." >&2
    exit 1
  fi

  if ! pg_lsclusters --no-header \
    | awk -v version="${POSTGRES_MAJOR_VERSION}" -v cluster="${POSTGRES_CLUSTER}" \
      '$1 == version && $2 == cluster && $4 == "online" { found = 1 } END { exit(found ? 0 : 1) }'; then
    run_root pg_ctlcluster "${POSTGRES_MAJOR_VERSION}" "${POSTGRES_CLUSTER}" start
  fi

  pg_isready --host 127.0.0.1 --port 5432

  run_as_postgres psql --set ON_ERROR_STOP=1 --command \
    "ALTER ROLE ${POSTGRES_USER} WITH LOGIN PASSWORD '${POSTGRES_PASSWORD}';"

  if ! run_as_postgres psql --tuples-only --no-align --command \
    "SELECT 1 FROM pg_database WHERE datname = '${POSTGRES_DB}'" | grep -qx '1'; then
    run_as_postgres createdb --owner "${POSTGRES_USER}" "${POSTGRES_DB}"
  fi
}

main() {
  corepack enable
  corepack prepare "pnpm@${PNPM_VERSION}" --activate
  start_postgresql

  export DATABASE_URL

  # Cached environments are intentionally network-independent. If a branch adds
  # uncached dependencies, reset the environment so setup can provision them.
  pnpm install --offline --no-frozen-lockfile
  pnpm db:push

  printf '\nHouseKeeper cached Codex environment refreshed.\n'
  node --version
  pnpm --version
  psql --version
}

main "$@"
