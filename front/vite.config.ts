import {getDefaultConfig} from "@elyspio/vite-eslint-config/lib/vite.config";
import {defineConfig} from "vite";

const config = getDefaultConfig({
	basePath: __dirname,

});

export default defineConfig({
	...config,
	base: "/react-api-template",
});
