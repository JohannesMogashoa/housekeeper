#!/usr/bin/env bash
set -euo pipefail

# Bootstrap the Linux-based Codex review environment while provisioning still
# has internet access. Review-time agent internet access can remain disabled.

readonly NODE_MAJOR_VERSION="24"
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

ensure_bashrc_line() {
  local line="$1"
  touch "${HOME}/.bashrc"
  grep -Fqx "$line" "${HOME}/.bashrc" || printf '%s\n' "$line" >> "${HOME}/.bashrc"
}

configure_node() {
  local current_major=""

  if command -v node >/dev/null 2>&1; then
    current_major="$(node --version | sed -E 's/^v([0-9]+).*/\1/')"
  fi

  if [[ "${current_major}" != "${NODE_MAJOR_VERSION}" ]]; then
    if ! command -v mise >/dev/null 2>&1; then
      echo "Node.js ${NODE_MAJOR_VERSION} is required and mise is unavailable." >&2
      echo "Pin Node.js ${NODE_MAJOR_VERSION} in the Codex environment settings." >&2
      exit 1
    fi

    mise use --global "node@${NODE_MAJOR_VERSION}"
    hash -r
  fi

  if [[ "$(node --version | sed -E 's/^v([0-9]+).*/\1/')" != "${NODE_MAJOR_VERSION}" ]]; then
    echo "Unable to activate Node.js ${NODE_MAJOR_VERSION}." >&2
    exit 1
  fi
}

configure_pnpm() {
  corepack enable
  corepack prepare "pnpm@${PNPM_VERSION}" --activate

  if [[ "$(pnpm --version)" != "${PNPM_VERSION}" ]]; then
    echo "Unable to activate pnpm ${PNPM_VERSION}." >&2
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
    "SELECT 1 FROM pg_roles WHERE rolname = '${POSTGRES_USER}'" | grep -qx '1'; then
    run_as_postgres createuser --login "${POSTGRES_USER}"
  fi

  run_as_postgres psql --set ON_ERROR_STOP=1 --command \
    "ALTER ROLE ${POSTGRES_USER} WITH LOGIN PASSWORD '${POSTGRES_PASSWORD}';"

  if ! run_as_postgres psql --tuples-only --no-align --command \
    "SELECT 1 FROM pg_database WHERE datname = '${POSTGRES_DB}'" | grep -qx '1'; then
    run_as_postgres createdb --owner "${POSTGRES_USER}" "${POSTGRES_DB}"
  fi
}

prepare_repository() {
  export DATABASE_URL
  ensure_bashrc_line "export DATABASE_URL='${DATABASE_URL}'"

  pnpm install --no-frozen-lockfile
  pnpm db:generate
  pnpm db:push
  pnpm typecheck
  pnpm test
}

main() {
  configure_node
  configure_pnpm
  install_postgresql
  start_postgresql
  prepare_repository

  printf '\nHouseKeeper Codex environment ready.\n'
  node --version
  pnpm --version
  psql --version
  pg_isready --host 127.0.0.1 --port 5432
}

main "$@"
