# Development MongoDB: a one-member replica set, so change streams and transactions behave the
# way they do in a real deployment. Not a deployment artefact — the API talks to whatever MongoDB
# the environment points it at.
FROM mongo:8.0.4

# A replica set with authentication requires an internal-auth keyfile shared by its members, and
# mongod refuses to start if anyone but its own user can read it. One member is still a member.
# Baking the key into the image is safe here and only here: this container is local, bound to
# loopback, and holds nothing but scraped public listings.
RUN head -c 756 /dev/urandom | base64 | tr -d '\n' > /etc/mongo-keyfile \
    && chmod 400 /etc/mongo-keyfile \
    && chown mongodb:mongodb /etc/mongo-keyfile

COPY entrypoint.sh /usr/local/bin/replica-set-entrypoint.sh
RUN chmod +x /usr/local/bin/replica-set-entrypoint.sh

ENTRYPOINT ["/usr/local/bin/replica-set-entrypoint.sh"]
CMD ["mongod", "--replSet", "rs0", "--keyFile", "/etc/mongo-keyfile", "--bind_ip_all"]
