using AngleSharp.Dom;
using EldenRingSquire.Backend.Models.Checklist;
using Microsoft.Extensions.Caching.Memory;

namespace EldenRingSquire.Backend.Services.WikiScraper;

public class FextraLifeWikiScraperService(HttpClient http, IMemoryCache cache) : BaseWikiScraperService(http)
{
	private static readonly TimeSpan cacheDuration = TimeSpan.FromHours(1);

	private const string URL_ROOT    = "https://eldenring.wiki.fextralife.com";
	private const string URL_BOSSES  = "https://eldenring.wiki.fextralife.com/Bosses";
	private const string URL_GRACES  = "https://eldenring.wiki.fextralife.com/Sites+of+Grace";
	private const string URL_WEAPONS = "https://eldenring.wiki.fextralife.com/Weapons";
	private const string URL_SHIELDS = "https://eldenring.wiki.fextralife.com/Shields";

	public override async Task<IList<ChecklistCategory>> GetCategories(CancellationToken ct = default) =>
	[
		new() { Id = "bosses",  Label = "Bosses",  GroupCaption = "Location", StatusLabels = ["Alive",       "Defeated"] },
		new() { Id = "graces",  Label = "Graces",  GroupCaption = "Area",     StatusLabels = ["Not Found",   "Found"] },
		new() { Id = "weapons", Label = "Weapons", GroupCaption = "Type",     StatusLabels = ["Not Obtained","Obtained"] },
		new() { Id = "shields", Label = "Shields", GroupCaption = "Type",     StatusLabels = ["Not Obtained","Obtained"] }
	];

	public override async Task<IList<ChecklistItem>> GetItems(CancellationToken ct = default) =>
		[.. (await Task.WhenAll(GetBosses(ct), GetGraces(ct), GetWeapons(ct), GetShields(ct))).SelectMany(x => x)];

	private static string FormatToId(string name) => name
		.ToLowerInvariant()
		.Replace(" ", "-").Replace(",", "-").Replace(":", "-")
		.Replace("'", "").Replace(".", "").Replace(" ", "")
		.Trim();

	private async Task<IList<ChecklistItem>> GetBosses(CancellationToken ct = default)
	{
		static IElement? GetHeader(IElement? x) =>
			(x is null || x.TagName.Equals(TagNames.H4, StringComparison.OrdinalIgnoreCase))
				? x : GetHeader(x.PreviousElementSibling);

		static string ExtractGroup(IElement x) =>
			GetHeader(x)?.QuerySelector("a")?.TextContent ?? "";

		if (cache.TryGetValue(URL_BOSSES, out List<ChecklistItem>? items))
			if (items is not null)
				return items;

		items = [];
		var document = await GetParsedDocumentAsync(URL_BOSSES, ct);
		foreach (var list in document.QuerySelectorAll("div.tabcontent.tutorial-tab h4[class=\"special\"] ~ ul"))
			items.AddRange(
				list
					.QuerySelectorAll("li")
					.Select(x => new ChecklistItem
					{
						Id = $"boss:{FormatToId($"{ExtractGroup(list)}{x.TextContent}")}",
						Category = "bosses",
						Name = x.TextContent.Trim(),
						Group = ExtractGroup(list),
						Url = $"{URL_ROOT}{x.QuerySelector("a")?.GetAttribute("href")}",
						Dlc = list.PreviousElementSibling?.QuerySelector("img[title=\"sote-new\"]") is not null,
					})
			);

		items = [.. items.OrderBy(x => x.Dlc)];
		return cache.Set(URL_BOSSES, items, cacheDuration);
	}

	private async Task<IList<ChecklistItem>> GetGraces(CancellationToken ct = default)
	{
		static IElement? GetHeader(IElement? x) =>
			(x is null || x.TagName.Equals(TagNames.H4, StringComparison.OrdinalIgnoreCase))
				? x : GetHeader(x.PreviousElementSibling);

		static string ExtractGroup(IElement x) =>
			GetHeader(x)?.TextContent.Split('(').First().Trim() ?? "";

		static string ExtractName(IElement x) =>
			string.Concat(x.TextContent.Split('[').First().Trim());

		if (cache.TryGetValue(URL_GRACES, out List<ChecklistItem>? items))
			if (items is not null)
				return items;

		items = [];
		var document = await GetParsedDocumentAsync(URL_GRACES, ct);
		foreach (var tab in new List<int>() { 2, 1 })
			foreach (var list in document.QuerySelectorAll($"div.tabcontent.\\3{tab}-tab h4 ~ ul"))
				items.AddRange(
					list
						.QuerySelectorAll("li")
						.Select(x => new ChecklistItem
						{
							Id = $"grace:{FormatToId($"{ExtractGroup(list)}-{ExtractName(x)}")}",
							Category = "graces",
							Name = ExtractName(x),
							Group = ExtractGroup(list),
							Url = $"{URL_ROOT}{x.QuerySelector("a")?.GetAttribute("href")}",
							Dlc = tab == 1,
						})
				);

		return cache.Set(URL_GRACES, items, cacheDuration);
	}

	private async Task<IList<ChecklistItem>> GetWeapons(CancellationToken ct = default)
	{
		static IElement? GetHeader(IElement? x) =>
			(x is null || x.TagName.Equals(TagNames.H3, StringComparison.OrdinalIgnoreCase))
				? x : GetHeader(x.PreviousElementSibling);

		static string ExtractName(IElement x) =>
			x.QuerySelector("a")?.GetAttribute("title")?[10..].Trim() ?? "";

		if (cache.TryGetValue(URL_WEAPONS, out List<ChecklistItem>? items))
			if (items is not null)
				return items;

		items = [];
		var document = await GetParsedDocumentAsync(URL_WEAPONS, ct);
		var query = "#wiki-content-block h3[style=\"text-align: center;\"] ~ div.row.gallery";
		foreach (var row in document.QuerySelectorAll(query))
			items.AddRange(
				row
					.QuerySelectorAll("div.col-xs-6.col-sm-2")
					.Where(x => x.QuerySelector("img") is not null)
					.Select(x => new ChecklistItem
					{
						Id = $"weapon:{FormatToId(ExtractName(x))}",
						Category = "weapons",
						Name = ExtractName(x),
						Group = GetHeader(row)?.TextContent ?? "",
						Url = $"{URL_ROOT}{x.QuerySelector("a")?.GetAttribute("href")}",
						Dlc = x.QuerySelector("img[title=\"sote-new\"]") is not null,
					})
				);

		return cache.Set(URL_WEAPONS, items, cacheDuration);
	}

	private async Task<IList<ChecklistItem>> GetShields(CancellationToken ct = default)
	{
		static IElement? GetHeader(IElement? x) =>
			(x is null || x.TagName.Equals(TagNames.H3, StringComparison.OrdinalIgnoreCase))
				? x : GetHeader(x.PreviousElementSibling);

		static string ExtractName(IElement x) =>
			x.QuerySelector("a")?.GetAttribute("title")?[10..].Trim() ?? "";

		if (cache.TryGetValue(URL_SHIELDS, out List<ChecklistItem>? items))
			if (items is not null)
				return items;

		items = [];
		var document = await GetParsedDocumentAsync(URL_SHIELDS, ct);
		foreach (var row in document.QuerySelectorAll("#wiki-content-block > h3 ~ div.row"))
			items.AddRange(
				row
					.QuerySelectorAll("div.col-xs-6.col-sm-2")
					.Select(x => new ChecklistItem
					{
						Id = $"shield:",
						Category = "shields",
						Name = ExtractName(x),
						Group = GetHeader(row)?.TextContent ?? "",
						Url = $"{URL_ROOT}{x.QuerySelector("a")?.GetAttribute("href")}",
						Dlc = x.QuerySelector("img[title=\"sote-new\"]") is not null,
					})
			);

		return cache.Set(URL_SHIELDS, items, cacheDuration);
	}
}
