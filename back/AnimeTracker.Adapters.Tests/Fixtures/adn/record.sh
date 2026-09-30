#!/usr/bin/env bash
# Re-records the ADN fixtures by replaying the adapter's own requests (AdnAdapter, AdnClient) through
# the dub egress proxy — never from the home network's address.
#   PROXY=http://10.0.1.123:8888 ./record.sh
set -euo pipefail
cd "$(dirname "$0")"

PROXY="${PROXY:?set PROXY to the dub egress proxy}"
UA='Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36'
API=https://gw.api.animationdigitalnetwork.com
SHOW=1331      # Hero Without a Class — one season, French on every episode
GROUPED=1350   # HELL MODE — two seasons in one show, the second numbered 13 to 25

get() { curl -sS --http1.1 -x "$PROXY" -A "$UA" -H 'X-Target-Distribution: fr' -H 'Accept: application/json' "$@"; sleep 0.5; }

get -o search-hero-without-a-class.json "$API/show/catalog?search=hero%20without%20a%20class&limit=6&offset=0"
get -o "show-$SHOW.json" "$API/show/$SHOW"
get -o "videos-$SHOW.json" "$API/video/show/$SHOW?offset=0&limit=100&order=asc"
get -o "videos-$GROUPED.json" "$API/video/show/$GROUPED?offset=0&limit=100&order=asc"
get -o show-999999.json "$API/show/999999"
