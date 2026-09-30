#!/usr/bin/env bash
# Re-records the Crunchyroll fixtures by replaying the adapter's own requests (CrunchyrollAdapter,
# CrunchyrollClient), through the dub egress proxy — never from the home network's address.
#   PROXY=http://10.0.1.123:8888 ./record.sh
# curl, not PowerShell: Cloudflare challenges .NET on Windows, and Invoke-WebRequest is .NET.
# The only edit made to a reply is the anonymous token's value, replaced so no credential is committed.
set -euo pipefail
cd "$(dirname "$0")"

PROXY="${PROXY:?set PROXY to the dub egress proxy}"
UA='Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36'
BASE=https://www.crunchyroll.com
SERIES=G24H1N3MP          # Mushoku Tensei: Jobless Reincarnation — three seasons, French on part of the third
SEASON=GS00374452JAJP     # its third season, summer 2026
MISSING=GZZZZZZZZ

get() { curl -sS --http1.1 -x "$PROXY" -A "$UA" "$@"; sleep 0.5; }

get -o home.html "$BASE/"
CLIENT=$(grep -oE '"accountAuthClientId":"[^"]+"' home.html | cut -d'"' -f4)
get -o token.json -X POST "$BASE/auth/v1/token" -H "Authorization: Basic $(printf '%s:' "$CLIENT" | base64 | tr -d '\n')" \
	-d 'grant_type=client_id&scope=offline_access'
TOKEN=$(grep -oE '"access_token":"[^"]+"' token.json | cut -d'"' -f4)
sed -i -E 's/"access_token":"[^"]+"/"access_token":"recorded-token"/' token.json

auth=(-H "Authorization: Bearer $TOKEN")
get -o search-mushoku-tensei.json "${auth[@]}" "$BASE/content/v2/discover/search?q=mushoku%20tensei&n=6&type=series&locale=fr-FR"
get -o "series-$SERIES.json" "${auth[@]}" "$BASE/content/v2/cms/series/$SERIES?locale=fr-FR"
get -o "seasons-$SERIES.json" "${auth[@]}" "$BASE/content/v2/cms/series/$SERIES/seasons?locale=fr-FR"
get -o "episodes-$SEASON.json" "${auth[@]}" "$BASE/content/v2/cms/seasons/$SEASON/episodes?locale=fr-FR"
get -o "series-$MISSING.json" "${auth[@]}" "$BASE/content/v2/cms/series/$MISSING?locale=fr-FR"
