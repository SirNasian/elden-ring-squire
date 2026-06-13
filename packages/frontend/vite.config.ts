import { defineConfig } from "vite"
import vue from "@vitejs/plugin-vue"
import Components from "unplugin-vue-components/vite"
import { PrimeVueResolver } from "@primevue/auto-import-resolver"

export default defineConfig({
	build: {
		outDir: "../../dist/public",
		emptyOutDir: true,
	},
	plugins: [
		vue(),
		Components({
			resolvers: [PrimeVueResolver()],
			dts: "src/components.d.ts",
		}),
	],
	server: {
		proxy: {
			"/api": {
				target: "http://localhost:5000",
				changeOrigin: true,
			},
		},
	},
})
