import { fileURLToPath } from "node:url";
import tailwindcss from "@tailwindcss/vite";
import { tanstackRouter } from "@tanstack/router-plugin/vite";
import { defineConfig } from "vite";
import solidPlugin from "vite-plugin-solid";

const apiBaseUrl = process.env.VITE_API_BASE_URL ?? "https://localhost:7198";

export default defineConfig({
	plugins: [
		tanstackRouter({
			target: "solid",
			autoCodeSplitting: true,
		}),
		tailwindcss(),
		solidPlugin(),
	],
	build: {
		outDir: "../RealWorldApi.Core/wwwroot",
		emptyOutDir: true,
	},
	resolve: {
		alias: {
			"~": fileURLToPath(new URL("./src", import.meta.url)),
		},
		tsconfigPaths: true,
	},
	server: {
		port: 5173,
		strictPort: true,
		proxy: {
			"/api": {
				target: apiBaseUrl,
				changeOrigin: true,
				secure: false,
			},
		},
	},
});
