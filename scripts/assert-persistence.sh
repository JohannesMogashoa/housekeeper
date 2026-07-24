#!/usr/bin/env bash
set -euo pipefail

api_base_url="${API_BASE_URL:-http://127.0.0.1:5287}"
household_name="${SMOKE_HOUSEHOLD_NAME:?SMOKE_HOUSEHOLD_NAME is required}"

listed="$(curl --fail --silent --show-error \
  -H "X-HouseKeeper-Subject: hk14-smoke-user" \
  -H "X-HouseKeeper-Display-Name: HK-14 Smoke User" \
  "${api_base_url}/api/households")"

grep --fixed-strings --quiet "\"name\":\"${household_name}\"" <<<"${listed}"

printf 'HK-14 restart persistence passed for household: %s\n' "${household_name}"
