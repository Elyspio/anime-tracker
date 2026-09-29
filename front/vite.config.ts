import { getDefaultConfig } from "@elyspio/vite-eslint-config";
import { defineConfig } from "vite-plus";

// Plain HTTP: Aspire publishes the dev server un-proxied, and the realm's redirect URIs name http://localhost:5173.
const config = getDefaultConfig({ basePath: import.meta.dirname, port: 5173, useMkcert: false });

// The .NET API serves the SPA in production; in dev, /api is proxied to the local API.
const API_TARGET = process.env.VITE_API_TARGET ?? "https://localhost:7281";

export default defineConfig({
	...config,
	server: {
		...config.server,
		// Stable OIDC origin: fail rather than fall back to another port, since the redirect
		// URIs registered in Keycloak are pinned to 5173.
		strictPort: true,
		proxy: {
			"/api": { target: API_TARGET, changeOrigin: true, secure: false },
		},
	},
	build: {
		outDir: "dist",
		sourcemap: true,
	},
});
