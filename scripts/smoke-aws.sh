#!/usr/bin/env bash
set -euo pipefail

: "${API_BASE_URL:?API_BASE_URL is required}"
: "${SMOKE_ACCESS_TOKEN:?SMOKE_ACCESS_TOKEN is required}"

api_base_url="${API_BASE_URL%/}"
household_name="${SMOKE_HOUSEHOLD_NAME:-HouseKeeper AWS smoke household}"

curl --fail --silent --show-error "${api_base_url}/health/live" >/dev/null
curl --fail --silent --show-error "${api_base_url}/health/ready" >/dev/null

current_user="$(curl --fail --silent --show-error \
  -H "Authorization: Bearer ${SMOKE_ACCESS_TOKEN}" \
  "${api_base_url}/api/me")"

if ! jq --exit-status '.subject | strings | length > 0' <<<"${current_user}" >/dev/null; then
  echo 'Authenticated AWS smoke check did not return a current user subject.' >&2
  exit 1
fi

if [[ "${SMOKE_EXPECT_EXISTING:-false}" == "true" ]]; then
  households="$(curl --fail --silent --show-error \
    -H "Authorization: Bearer ${SMOKE_ACCESS_TOKEN}" \
    "${api_base_url}/api/households")"
  jq --exit-status --arg household_name "${household_name}" \
    'any(.[]; .name == $household_name)' <<<"${households}" >/dev/null
else
  created="$(curl --fail --silent --show-error \
    -X POST \
    -H "Authorization: Bearer ${SMOKE_ACCESS_TOKEN}" \
    -H "Content-Type: application/json" \
    --data "{\"name\":\"${household_name}\"}" \
    "${api_base_url}/api/households")"
  jq --exit-status --arg household_name "${household_name}" \
    '.name == $household_name' <<<"${created}" >/dev/null
fi

echo 'Authenticated AWS smoke check passed.'
