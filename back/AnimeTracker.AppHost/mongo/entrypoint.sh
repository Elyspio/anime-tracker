#!/bin/sh
# Brings the one-member replica set up, then hands over to the stock entrypoint.
#
# The initiation cannot live in /docker-entrypoint-initdb.d: the stock entrypoint strips
# --replSet for the temporary instance it runs those scripts against, precisely so the root user
# can be created before the set exists. So it is done here, against the real server, in the
# background while mongod starts in the foreground as PID 1.
set -e

(
	# Idempotent: rs.status() succeeds once the set is configured, and a data volume carries that
	# configuration across restarts, so a restarted container simply finds nothing to do.
	attempt=0
	while [ "$attempt" -lt 150 ]; do
		attempt=$((attempt + 1))
		sleep 2

		if mongosh --quiet \
			-u "$MONGO_INITDB_ROOT_USERNAME" -p "$MONGO_INITDB_ROOT_PASSWORD" \
			--authenticationDatabase admin \
			--eval 'try { rs.status().ok } catch (e) { rs.initiate({ _id: "rs0", members: [{ _id: 0, host: "localhost:27017" }] }) }' \
			>/dev/null 2>&1; then
			exit 0
		fi
	done

	echo "replica set rs0 could not be initiated" >&2
) &

exec docker-entrypoint.sh "$@"
