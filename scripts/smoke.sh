#!/usr/bin/env bash
set -euo pipefail

api_base_url="${API_BASE_URL:-http://127.0.0.1:5287}"
subject="hk14-smoke-user"
display_name="HK-14 Smoke User"
household_name="HK-14 Smoke Household ${GITHUB_RUN_ID:-local}-$(date +%s)"

headers=(
  -H "X-HouseKeeper-Subject: ${subject}"
  -H "X-HouseKeeper-Display-Name: ${display_name}"
)

curl --fail --silent --show-error "${api_base_url}/health/live" >/dev/null
curl --fail --silent --show-error "${api_base_url}/health/ready" >/dev/null

current_user="$(curl --fail --silent --show-error \
  "${headers[@]}" \
  "${api_base_url}/api/me")"

grep --fixed-strings --quiet '"subject":"hk14-smoke-user"' <<<"${current_user}"

created="$(curl --fail --silent --show-error \
  -X POST \
  "${headers[@]}" \
  -H "Content-Type: application/json" \
  --data "{\"name\":\"${household_name}\"}" \
  "${api_base_url}/api/households")"

grep --fixed-strings --quiet "\"name\":\"${household_name}\"" <<<"${created}"

listed="$(curl --fail --silent --show-error \
  "${headers[@]}" \
  "${api_base_url}/api/households")"

grep --fixed-strings --quiet "\"name\":\"${household_name}\"" <<<"${listed}"

printf 'HK-14 smoke flow passed for household: %s\n' "${household_name}"
