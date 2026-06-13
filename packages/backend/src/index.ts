import { serveStatic } from "@hono/node-server/serve-static";
import { serve } from "@hono/node-server";
import { Hono } from "hono";

import { config } from "./config";
import { logger } from "./logging";
import { ChecklistRouter } from "./routers";

const app = new Hono();

app.use("/*", serveStatic({ root: "./public" }));
app.route("/api/checklist", ChecklistRouter);

const server = serve({
	fetch: app.fetch,
	hostname: config.HOST,
	port: config.PORT
}, () => logger.log("server started", config));

process.on("SIGINT", () => {
	server.close();
	process.exit(0);
});

process.on("SIGTERM", () => {
	server.close((error) => {
		if (error) {
			console.error(error)
			process.exit(1)
		}
		process.exit(0)
	})
});
