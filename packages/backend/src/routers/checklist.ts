import { Hono } from "hono";
import { GetChecklistResponse } from "@ersquire/shared";
import { scraper } from "../services/fextralife-wiki-scraper";

const router = new Hono;

router.get("/", async (c) => c.json<GetChecklistResponse>({
	categories: await scraper.getCategories(),
	items: await scraper.getItems(),
}));

export const ChecklistRouter = router;
