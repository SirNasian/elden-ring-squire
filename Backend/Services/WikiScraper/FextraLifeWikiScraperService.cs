using AngleSharp.Dom;
using EldenRingSquire.Backend.Models.Checklist;
using Microsoft.Extensions.Caching.Memory;

namespace EldenRingSquire.Backend.Services.WikiScraper;

public class FextraLifeWikiScraperService(HttpClient http, IMemoryCache cache) : BaseWikiScraperService(http)
{
	private static readonly TimeSpan cacheExpiry = TimeSpan.FromHours(1);

	private const string URL_ROOT    = "https://eldenring.wiki.fextralife.com";
	private const string URL_BOSSES  = "https://eldenring.wiki.fextralife.com/Bosses";
	private const string URL_GRACES  = "https://eldenring.wiki.fextralife.com/Sites+of+Grace";
	private const string URL_WEAPONS = "https://eldenring.wiki.fextralife.com/Weapons";
	private const string URL_SHIELDS = "https://eldenring.wiki.fextralife.com/Shields";

	public override async Task<IList<Boss>> GetBosses(CancellationToken ct = default)
	{
		static Func<IElement, Boss> ConstructItem(string? area, bool dlc) => x => new()
		{
			Id = FormatToId($"{area}-{x.TextContent}"),
			Name = x.TextContent,
			Group = area ?? "",
			Url = $"{URL_ROOT}{x.QuerySelector("a")?.GetAttribute("href")}",
			Dlc = dlc,
		};

		if (cache.TryGetValue(URL_BOSSES, out List<Boss>? bosses))
			if (bosses is not null)
				return bosses;

		bosses = [];
		var document = await GetParsedDocumentAsync(URL_BOSSES, ct);
		var tabContent = document.QuerySelector("div .tabcontent.tutorial-tab");
		if (tabContent is not null)
			foreach (var sectionRow in tabContent.QuerySelectorAll("div .row"))
				foreach (var sectionColumn in sectionRow.QuerySelectorAll("div"))
					foreach (var heading in sectionColumn.QuerySelectorAll("h4"))
					{
						var item = heading.NextElementSibling;
						var dlc = item?.TagName.Equals(TagNames.P, StringComparison.OrdinalIgnoreCase) ?? false;
						if (dlc) item = item?.NextElementSibling;
						if (item is not null)
							bosses.AddRange(
								item
									.QuerySelectorAll("li")
									.Select(ConstructItem(heading.QuerySelector("a")?.TextContent, dlc))
								?? []
							);
					}

		bosses = [.. bosses.OrderBy(x => x.Dlc)];
		return cache.Set(URL_BOSSES, bosses, cacheExpiry);
	}

	public override async Task<IList<Grace>> GetGraces(CancellationToken ct = default)
	{
		static string ExtractName(IElement x) =>
			string.Concat(x.ChildNodes.Where(x => x.NodeType == NodeType.Text).Select(x => x.TextContent)).Trim(['[', ']', ' ']);

		static Func<IElement, Grace> ConstructItem(string area, bool dlc) => x => new()
		{
			Id = $"{FormatToId(area)}:{FormatToId(ExtractName(x))}",
			Name = ExtractName(x),
			Group = area.Split('(')[0].Trim(),
			Url = $"{URL_ROOT}{x.QuerySelector("a")?.GetAttribute("href")}",
			Dlc = dlc,
		};

		if (cache.TryGetValue(URL_GRACES, out List<Grace>? graces))
			if (graces is not null)
				return graces;

		graces = [];
		var document = await GetParsedDocumentAsync(URL_GRACES, ct);
		foreach (var tabContent in document.QuerySelectorAll("div .tabcontent"))
			foreach (var sectionRow in tabContent.QuerySelectorAll("div .row"))
				foreach (var sectionColumn in sectionRow.QuerySelectorAll("div"))
					foreach (var heading in sectionColumn.QuerySelectorAll("h4"))
						graces.AddRange(
							heading
								.NextElementSibling
								?.QuerySelectorAll("li")
								.Select(ConstructItem(heading.TextContent, tabContent.ClassList.Contains("1-tab")))
							?? []
						);

		graces = [.. graces.OrderBy(x => x.Dlc)];
		return cache.Set(URL_GRACES, graces, cacheExpiry);
	}

	public override async Task<IList<Weapon>> GetWeapons(CancellationToken ct = default)
	{
		static IElement? GetAnchor(IElement x) => x.QuerySelector("p a");
		static string ExtractName(IElement x) => GetAnchor(x)?.GetAttribute("title")?[10..].Trim() ?? "";

		static Func<IElement, Weapon> ConstructItem(string header) => x => new()
		{
			Id = $"weapon:{FormatToId(ExtractName(x))}",
			Name = ExtractName(x),
			Group = header,
			Url = $"{URL_ROOT}{GetAnchor(x)?.GetAttribute("href")}",
			Dlc = x.QuerySelector("img[title=\"sote-new\"]") is not null,
		};

		if (cache.TryGetValue(URL_WEAPONS, out List<Weapon>? weapons))
			if (weapons is not null)
				return weapons;

		weapons = [];
		var document = await GetParsedDocumentAsync(URL_WEAPONS, ct);
		foreach (var header in document.QuerySelectorAll("#wiki-content-block h3[style=\"text-align: center;\"]"))
		{
			var row = header.NextElementSibling;
			while (row is not null && row.ClassList.Contains("row"))
			{
				weapons.AddRange(
					row
						.QuerySelectorAll("div.col-xs-6.col-sm-2")
						.Where(x => GetAnchor(x) is not null)
						.Select(ConstructItem(header.TextContent))
					?? []
				);
				row = row.NextElementSibling;
			}
		}

		weapons = [.. weapons.OrderBy(x => x.Group).ThenBy(x => x.Name)];
		return cache.Set(URL_WEAPONS, weapons, cacheExpiry);
	}

	public override async Task<IList<Shield>> GetShields(CancellationToken ct = default)
	{
		static IElement? GetRowHeader(IElement? x) =>
			(x is null || x.TagName.Equals(TagNames.H3, StringComparison.OrdinalIgnoreCase))
				? x : GetRowHeader(x.PreviousElementSibling);

		static string ExtractName(IElement x) =>
			x.GetAttribute("title")?[10..].Trim() ?? "";

		if (cache.TryGetValue(URL_WEAPONS, out List<Shield>? shields))
			if (shields is not null)
				return shields;

		shields = [];
		var document = await GetParsedDocumentAsync(URL_SHIELDS, ct);
		foreach (var row in document.QuerySelectorAll("#wiki-content-block > h3 ~ div[class=\"row\"]"))
			foreach (var column in row.QuerySelectorAll("div[class=\"col-xs-6 col-sm-2\"]"))
			{
				var x = column.QuerySelector("a[class=\"wiki_link wiki_tooltip\"]");
				if (x is not null)
					shields.Add(new()
					{
						Id = $"shield:{FormatToId(ExtractName(x))}",
						Name = ExtractName(x),
						Group = GetRowHeader(row)?.TextContent ?? "",
						Url = $"{URL_ROOT}{x.GetAttribute("href")}",
						Dlc = column.QuerySelector("img[title=\"sote-new\"]") is not null,
					});
			}

		return cache.Set(URL_SHIELDS, shields, cacheExpiry);
	}

	private static string FormatToId(string name) => name
		.ToLowerInvariant()
		.Replace(" ", "-").Replace(",", "-").Replace(":", "-")
		.Replace("'", "").Replace(".", "").Replace(" ", "")
		.Trim();
}
