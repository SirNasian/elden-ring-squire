import * as cheerio from "cheerio";
import type { CheerioAPI } from "cheerio";
import type { AnyNode, Element } from "domhandler";

import { ChecklistCategory, ChecklistItem } from "@ersquire/shared";
import { ChecklistDataProvider } from "./checklist-data-provider";

const URL_ROOT          = "https://eldenring.wiki.fextralife.com";
const URL_BOSSES        = "https://eldenring.wiki.fextralife.com/Bosses";
const URL_GRACES        = "https://eldenring.wiki.fextralife.com/Sites+of+Grace";
const URL_WEAPONS       = "https://eldenring.wiki.fextralife.com/Weapons";
const URL_SHIELDS       = "https://eldenring.wiki.fextralife.com/Shields";
const URL_SPIRIT_ASHES  = "https://eldenring.wiki.fextralife.com/Spirit+Ashes";
const URL_SORCERIES     = "https://eldenring.wiki.fextralife.com/Sorceries";
const URL_INCANTATIONS  = "https://eldenring.wiki.fextralife.com/Incantations";
const URL_TALISMANS     = "https://eldenring.wiki.fextralife.com/Talismans";
const URL_ASHES_OF_WAR  = "https://eldenring.wiki.fextralife.com/Ashes+of+War";
const URL_COOKBOOKS     = "https://eldenring.wiki.fextralife.com/Cookbooks";
const URL_BALL_BEARINGS = "https://eldenring.wiki.fextralife.com/Bell+Bearings";
const URL_CRYSTAL_TEARS = "https://eldenring.wiki.fextralife.com/Crystal+Tears";
const URL_GREAT_RUNES   = "https://eldenring.wiki.fextralife.com/Great+Runes";
const URL_TOOLS         = "https://eldenring.wiki.fextralife.com/Tools";

const CACHE_DURATION_MS = 60 * 60 * 1000;

interface CacheEntry<T> { value: T; expiresAt: number; }
const cache = new Map<string, CacheEntry<ChecklistItem[]>>();

const cacheGet = (key: string): ChecklistItem[] | undefined => {
	const entry = cache.get(key);
	if (entry && entry.expiresAt > Date.now()) return entry.value;
	cache.delete(key);
	return undefined;
}

const cacheSet = (key: string, value: ChecklistItem[]): ChecklistItem[] => {
	cache.set(key, { value, expiresAt: Date.now() + CACHE_DURATION_MS });
	return value;
}

const fetchDocument = async (url: string): Promise<CheerioAPI> => {
	const res = await fetch(url);
	const html = await res.text();
	return cheerio.load(html);
}

const formatId = (name: string) => name
	.toLowerCase()
	.replace(/ /g, "-").replace(/,/g, "-").replace(/:/g, "-")
	.replace(/'/g, "").replace(/\./g, "")
	.trim();

const findHeader = ($: CheerioAPI, el: AnyNode | null | undefined, tags: string[]): Element | null => {
	if (!el || !("tagName" in el)) return null;
	const elem = el as Element;
	if (tags.map(t => t.toLowerCase()).includes(elem.tagName)) return elem;
	const prev = $(elem).prev().get(0);
	if (prev && "tagName" in prev) return findHeader($, prev, tags);
	return findHeader($, $(elem).parent().get(0), tags);
}

const getCategories = async (): Promise<ChecklistCategory[]> => [
	{ id: "bosses",        label: "Bosses",        groupCaption: "Location", statusLabels: ["Alive",        "Defeated"] },
	{ id: "graces",        label: "Graces",        groupCaption: "Area",     statusLabels: ["Not Found",    "Found"] },
	{ id: "weapons",       label: "Weapons",       groupCaption: "Type",     statusLabels: ["Not Obtained", "Obtained"] },
	{ id: "shields",       label: "Shields",       groupCaption: "Type",     statusLabels: ["Not Obtained", "Obtained"] },
	{ id: "spirit-ashes",  label: "Spirit Ashes",  groupCaption: "Type",     statusLabels: ["Not Obtained", "Obtained"] },
	{ id: "sorceries",     label: "Sorceries",     groupCaption: "School",   statusLabels: ["Not Known",    "Learned"] },
	{ id: "incantations",  label: "Incantations",  groupCaption: "School",   statusLabels: ["Not Known",    "Learned"] },
	{ id: "talismans",     label: "Talismans",     groupCaption: "",         statusLabels: ["Not Obtained", "Obtained"] },
	{ id: "ashes-of-war",  label: "Ashes of War",  groupCaption: "Affinity", statusLabels: ["Not Obtained", "Obtained"] },
	{ id: "cookbooks",     label: "Cookbooks",     groupCaption: "",         statusLabels: ["Not Obtained", "Obtained"] },
	{ id: "ball-bearings", label: "Ball Bearings", groupCaption: "",         statusLabels: ["Not Obtained", "Obtained"] },
	{ id: "crystal-tears", label: "Crystal Tears", groupCaption: "",         statusLabels: ["Not Obtained", "Obtained"] },
	{ id: "great-runes",   label: "Great Runes",   groupCaption: "",         statusLabels: ["Not Obtained", "Obtained"] },
	{ id: "tools",         label: "Tools",         groupCaption: "",         statusLabels: ["Not Obtained", "Obtained"] },
];

const getBosses = async (): Promise<ChecklistItem[]> => {
	const cached = cacheGet(URL_BOSSES);
	if (cached) return cached;

	const $ = await fetchDocument(URL_BOSSES);
	const items: ChecklistItem[] = [];

	for (const list of $("div.tabcontent.tutorial-tab h4.special ~ ul")) {
		const header = findHeader($, list, ["H4"]);
		const group = header ? $("a", header).text() : "";
		const dlc = $(list).prev().find("img[title=\"sote-new\"]").length > 0;

		for (const li of $("li", list)) {
			items.push({
				id: `boss:${formatId(`${group}${$(li).text()}`)}`,
				category: "bosses",
				name: $(li).text().trim(),
				group,
				url: `${URL_ROOT}${$("a", li).attr("href") ?? ""}`,
				dlc,
			});
		}
	}

	items.sort((a, b) => Number(a.dlc) - Number(b.dlc));
	return cacheSet(URL_BOSSES, items);
}

const getGraces = async (): Promise<ChecklistItem[]> => {
	const cached = cacheGet(URL_GRACES);
	if (cached) return cached;

	const $ = await fetchDocument(URL_GRACES);
	const items: ChecklistItem[] = [];

	const extractGroup = (el: Element) => {
		const header = findHeader($, el, ["H3", "H4"]);
		return header ? $(header).text().split("(")[0].trim() : "";
	};
	const extractName = (el: Element) => $(el).text().split(/[\[(]/)[0].trim();

	for (const tab of [2, 1]) {
		const escapedTab = tab.toString().split("").map(d => `\\3${d} `).join("").trim();
		for (const list of $(`div.tabcontent.${escapedTab}-tab ul`)) {
			for (const li of $("li", list)) {
				items.push({
					id: `grace:${formatId(`${extractGroup(list)}-${extractName(li)}`)}`,
					category: "graces",
					name: extractName(li),
					group: extractGroup(list),
					url: `${URL_ROOT}${$("a", li).attr("href") ?? ""}`,
					dlc: tab === 1,
				});
			}
		}
	}

	return cacheSet(URL_GRACES, items);
}

const getWeapons = async (): Promise<ChecklistItem[]> => {
	const cached = cacheGet(URL_WEAPONS);
	if (cached) return cached;

	const $ = await fetchDocument(URL_WEAPONS);
	const items: ChecklistItem[] = [];

	const extractName = (el: Element) => {
		const title = $("a", el).attr("title") ?? "";
		return title.startsWith("Elden Ring ") ? title.slice(11).trim() : title.trim();
	};

	for (const row of $("#wiki-content-block h3[style=\"text-align: center;\"] ~ div.row")) {
		if ($("h3", row).length > 0) continue;
		const header = findHeader($, row, ["H3"]);
		const group = header ? $(header).text().trim() : "";
		if (group === "Tools") continue;

		for (const cell of $("div.col-xs-6.col-sm-2", row)) {
			if ($("img", cell).length === 0) continue;
			const name = extractName(cell);
			items.push({
				id: `weapon:${formatId(name)}`,
				category: "weapons",
				name,
				group,
				url: `${URL_ROOT}${$("a", cell).attr("href") ?? ""}`,
				dlc: $("img[title=\"sote-new\"]", cell).length > 0,
			});
		}
	}

	return cacheSet(URL_WEAPONS, items);
}

const getShields = async (): Promise<ChecklistItem[]> => {
	const cached = cacheGet(URL_SHIELDS);
	if (cached) return cached;

	const $ = await fetchDocument(URL_SHIELDS);
	const items: ChecklistItem[] = [];

	const extractName = (el: Element) => {
		const title = $("a", el).attr("title") ?? "";
		return title.startsWith("Elden Ring ") ? title.slice(11).trim() : title.trim();
	};

	for (const row of $("#wiki-content-block > h3 ~ div.row")) {
		const header = findHeader($, row, ["H3"]);
		const group = header ? $(header).text().trim() : "";
		for (const cell of $("div.col-xs-6.col-sm-2", row)) {
			const name = extractName(cell);
			items.push({
				id: `shield:${formatId(name)}`,
				category: "shields",
				name,
				group,
				url: `${URL_ROOT}${$("a", cell).attr("href") ?? ""}`,
				dlc: $("img[title=\"sote-new\"]", cell).length > 0,
			});
		}
	}

	return cacheSet(URL_SHIELDS, items);
}

const getSpiritAshes = async (): Promise<ChecklistItem[]> => {
	const cached = cacheGet(URL_SPIRIT_ASHES);
	if (cached) return cached;

	const $ = await fetchDocument(URL_SPIRIT_ASHES);
	const items: ChecklistItem[] = [];

	const extractName = (el: Element) => {
		const title = $("a", el).attr("title") ?? "";
		return title.startsWith("Elden Ring ") ? title.slice(11).trim() : title.trim();
	};

	for (const row of $("div.tabcontent.\\30-tab h3 ~ div.row")) {
		const headerEl = findHeader($, row, ["H3"]);
		const text = headerEl ? $(headerEl).text() : "";
		const group = text.includes("Renowned") ? "Renowned" : "Normal";

		for (const cell of $("div.col-xs-6.col-sm-3", row)) {
			const name = extractName(cell);
			items.push({
				id: `spirit-ash:${formatId(name)}`,
				category: "spirit-ashes",
				name,
				group,
				url: `${URL_ROOT}${$("a", cell).attr("href") ?? ""}`,
				dlc: $("img[title=\"sote-new\"]", cell).length > 0,
			});
		}
	}

	return cacheSet(URL_SPIRIT_ASHES, items);
}

const getSorceries = async (): Promise<ChecklistItem[]> => {
	const cached = cacheGet(URL_SORCERIES);
	if (cached) return cached;

	const $ = await fetchDocument(URL_SORCERIES);
	const items: ChecklistItem[] = [];

	const extractName = (el: Element) => {
		const title = $("a", el).attr("title") ?? "";
		return title.startsWith("Elden Ring ") ? title.slice(11).trim() : title.trim();
	};

	for (const row of $("div.tabcontent.\\32-tab h3 ~ div.row")) {
		const header = findHeader($, row, ["H3"]);
		const group = header ? $(header).text().replace("Sorceries", "").trim() : "";

		for (const cell of $("div.col-xs-6.col-sm-3", row)) {
			const name = extractName(cell);
			items.push({
				id: `sorcery:${formatId(name)}`,
				category: "sorceries",
				name,
				group,
				url: `${URL_ROOT}${$("a", cell).attr("href") ?? ""}`,
				dlc: $("img[title=\"sote-new\"]", cell).length > 0,
			});
		}
	}

	return cacheSet(URL_SORCERIES, items);
}

const getIncantations = async (): Promise<ChecklistItem[]> => {
	const cached = cacheGet(URL_INCANTATIONS);
	if (cached) return cached;

	const $ = await fetchDocument(URL_INCANTATIONS);
	const items: ChecklistItem[] = [];

	const extractName = (el: Element) => {
		const title = $("a", el).attr("title") ?? "";
		return title.startsWith("Elden Ring ") ? title.slice(11).trim() : title.trim();
	};

	for (const row of $("div.tabcontent.\\32-tab h3 ~ div.row.gallery")) {
		const header = findHeader($, row, ["H3"]);
		const group = header ? $(header).text().replace("Incantations", "").trim() : "";

		for (const cell of $("div.col-xs-6.col-sm-3", row)) {
			const name = extractName(cell);
			items.push({
				id: `incantation:${formatId(name)}`,
				category: "incantations",
				name,
				group,
				url: `${URL_ROOT}${$("a", cell).attr("href") ?? ""}`,
				dlc: $("img[title=\"sote-new\"]", cell).length > 0,
			});
		}
	}

	return cacheSet(URL_INCANTATIONS, items);
}

const getTalismans = async (): Promise<ChecklistItem[]> => {
	const cached = cacheGet(URL_TALISMANS);
	if (cached) return cached;

	const $ = await fetchDocument(URL_TALISMANS);
	const items: ChecklistItem[] = [];

	const extractName = (el: Element) => {
		const title = $("a", el).attr("title") ?? "";
		return title.startsWith("Elden Ring ") ? title.slice(11).trim() : title.trim();
	};

	for (const row of $("div.tabcontent.\\30-tab h3 ~ div.row.gallery")) {
		for (const cell of $("div.col-xs-6.col-sm-3", row)) {
			const name = extractName(cell);
			items.push({
				id: `talisman:${formatId(name)}`,
				category: "talismans",
				name,
				group: "",
				url: `${URL_ROOT}${$("a", cell).attr("href") ?? ""}`,
				dlc: $("img[title=\"sote-new\"]", cell).length > 0,
			});
		}
	}

	return cacheSet(URL_TALISMANS, items);
}

const getAshesOfWar = async (): Promise<ChecklistItem[]> => {
	const cached = cacheGet(URL_ASHES_OF_WAR);
	if (cached) return cached;

	const $ = await fetchDocument(URL_ASHES_OF_WAR);

	const extractGroup = (el: Element) => {
		const header = findHeader($, $(el).parent().get(0), ["H4"]);
		return header ? $(header).text().replace("Ashes of War", "").trim() : "";
	};
	const extractName = (el: Element) =>
		($("a", el).attr("title") ?? "").replace("Elden Ring Ash of War:", "").trim();

	const items: ChecklistItem[] = [...$("div.tabcontent.\\33-tab h4 ~ ul li")].map(li => ({
		id: `ash-of-war:${formatId(extractName(li))}`,
		category: "ashes-of-war",
		name: extractName(li),
		group: extractGroup(li),
		url: `${URL_ROOT}${$("a", li).attr("href") ?? ""}`,
		dlc: false,
	}));

	return cacheSet(URL_ASHES_OF_WAR, items);
}

const getCookbooks = async (): Promise<ChecklistItem[]> => {
	const cached = cacheGet(URL_COOKBOOKS);
	if (cached) return cached;

	const $ = await fetchDocument(URL_COOKBOOKS);

	const extractName = (el: Element) =>
		($("a", el).attr("title") ?? "").replace("Elden Ring", "").trim();

	const items: ChecklistItem[] = [...$("div.tabcontent.\\31-tab h4")]
		.filter(h => $("a", h).length > 0)
		.map(h => ({
			id: `cookbook:${formatId(extractName(h))}`,
			category: "cookbooks",
			name: extractName(h),
			group: "",
			url: `${URL_ROOT}${$("a", h).attr("href") ?? ""}`,
			dlc: $("img[title=\"sote-new\"]", h).length > 0,
		}));

	return cacheSet(URL_COOKBOOKS, items);
}

const getBallBearings = async (): Promise<ChecklistItem[]> => {
	const cached = cacheGet(URL_BALL_BEARINGS);
	if (cached) return cached;

	const $ = await fetchDocument(URL_BALL_BEARINGS);

	const extractName = (el: Element) =>
		($("a", el).attr("title") ?? "").replace("Elden Ring", "").trim();

	const items: ChecklistItem[] = [...$("div.tabcontent.\\31-tab h4")]
		.filter(h => $("a", h).length > 0)
		.map(h => ({
			id: `ball-bearing:${formatId(extractName(h))}`,
			category: "ball-bearings",
			name: extractName(h),
			group: "",
			url: `${URL_ROOT}${$("a", h).attr("href") ?? ""}`,
			dlc: $("img[title=\"sote-new\"]", h).length > 0,
		}));

	return cacheSet(URL_BALL_BEARINGS, items);
}

const getCrystalTears = async (): Promise<ChecklistItem[]> => {
	const cached = cacheGet(URL_CRYSTAL_TEARS);
	if (cached) return cached;

	const $ = await fetchDocument(URL_CRYSTAL_TEARS);

	const extractName = (el: Element) =>
		($("a", el).attr("title") ?? "").replace("Elden Ring", "").trim();

	const items: ChecklistItem[] = [...$("div.tabcontent.\\31-tab h4")]
		.filter(h => $("a", h).length > 0)
		.map(h => ({
			id: `crystal-tear:${formatId(extractName(h))}`,
			category: "crystal-tears",
			name: extractName(h),
			group: "",
			url: `${URL_ROOT}${$("a", h).attr("href") ?? ""}`,
			dlc: $("img[title=\"sote-new\"]", h).length > 0,
		}));

	return cacheSet(URL_CRYSTAL_TEARS, items);
}

const getGreatRunes = async (): Promise<ChecklistItem[]> => {
	const cached = cacheGet(URL_GREAT_RUNES);
	if (cached) return cached;

	const $ = await fetchDocument(URL_GREAT_RUNES);

	const extractName = (el: Element) =>
		($("a", el).attr("title") ?? "").replace("Elden Ring", "").trim();

	const items: ChecklistItem[] = [...$("div.tabcontent.\\30-tab h4")]
		.filter(h => $("a", h).length > 0)
		.map(h => ({
			id: `great-rune:${formatId(extractName(h))}`,
			category: "great-runes",
			name: extractName(h),
			group: "",
			url: `${URL_ROOT}${$("a", h).attr("href") ?? ""}`,
			dlc: $(h).parent().find("img[alt=\"sote new\"]").length > 0,
		}));

	return cacheSet(URL_GREAT_RUNES, items);
}

const getTools = async (): Promise<ChecklistItem[]> => {
	const cached = cacheGet(URL_TOOLS);
	if (cached) return cached;

	const $ = await fetchDocument(URL_TOOLS);

	const extractName = (el: Element) =>
		($("a", el).attr("title") ?? "").replace("Elden Ring", "").trim();

	const items: ChecklistItem[] = [...$("div.tabcontent.\\31-tab h4")]
		.filter(h => $("a", h).length > 0)
		.map(h => ({
			id: `tool:${formatId(extractName(h))}`,
			category: "tools",
			name: extractName(h),
			group: "",
			url: `${URL_ROOT}${$("a", h).attr("href") ?? ""}`,
			dlc: $("img[title=\"sote-new\"]", h).length > 0,
		}));

	return cacheSet(URL_TOOLS, items);
}

const getItems = async (): Promise<ChecklistItem[]> => {
	const results = await Promise.all([
		getBosses(),
		getGraces(),
		getWeapons(),
		getShields(),
		getSpiritAshes(),
		getSorceries(),
		getIncantations(),
		getTalismans(),
		getAshesOfWar(),
		getCookbooks(),
		getBallBearings(),
		getCrystalTears(),
		getGreatRunes(),
		getTools(),
	]);
	return results.flat();
};

export const scraper: ChecklistDataProvider = { getCategories, getItems };
export default scraper;
