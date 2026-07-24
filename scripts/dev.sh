#!/usr/bin/env bash
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
compose_file="${root_dir}/deploy/local/compose.yaml"

cd "${root_dir}"

docker compose -f "${compose_file}" up -d postgres

dotnet tool restore
dotnet restore HouseKeeper.slnx

printf 'Waiting for PostgreSQL'
until docker compose -f "${compose_file}" exec -T postgres \
  pg_isready -U housekeeper -d housekeeper >/dev/null 2>&1; do
  printf '.'
  sleep 1
done
printf ' ready\n'

dotnet ef database update \
  --project src/Modules/HouseKeeper.Modules.Households \
  --startup-project src/HouseKeeper.Api \
  --context HouseholdsDbContext

dotnet run --project src/HouseKeeper.Api --launch-profile http &
api_pid=$!

dotnet run --project src/HouseKeeper.Web --launch-profile http &
web_pid=$!

cleanup() {
  kill "${api_pid}" "${web_pid}" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

printf '\nHouseKeeper is starting:\n'
printf '  Web: http://localhost:5136\n'
printf '  API: http://localhost:5287\n\n'

wait "${api_pid}" "${web_pid}"
