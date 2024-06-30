const {spawnSync} = require("child_process");

spawnSync("docker", ["buildx", "bake", "--push"], {cwd: __dirname, stdio: "inherit"});
spawnSync("ssh", ["elyspio@192.168.0.59", "cd /apps/own/react-api-template && docker compose pull && docker compose up -d"], {cwd: __dirname, stdio: "inherit"});
