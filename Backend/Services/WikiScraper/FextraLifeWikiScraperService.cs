using AngleSharp.Dom;
using EldenRingSquire.Backend.Models.Checklist;

namespace EldenRingSquire.Backend.Services.WikiScraper;

public class FextraLifeWikiScraperService(HttpClient http) : BaseWikiScraperService(http)
{
	private const string URL_ROOT   = "https://eldenring.wiki.fextralife.com";
	private const string URL_BOSSES = "https://eldenring.wiki.fextralife.com/Bosses";
	private const string URL_GRACES = "https://eldenring.wiki.fextralife.com/Sites+of+Grace";

	public override async Task<IList<Boss>> ScrapeBosses(CancellationToken ct = default)
	{
		var bosses = new List<Boss>();

		static Func<IElement, Boss> ConstructItem(string? area, bool dlc) => x => new()
		{
			Id = ConvertNameToId($"{area}-{x.TextContent}"),
			Name = x.TextContent,
			Group = area ?? "",
			Url = $"{URL_ROOT}{x.GetAttribute("href")}",
			Dlc = dlc,
		};

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

		return [.. bosses.OrderBy(x => x.Dlc)];
	}

	public override async Task<IList<Grace>> ScrapeGraces(CancellationToken ct = default)
	{
		static string ExtractName(IElement x) =>
			string.Concat(x.ChildNodes.Where(x => x.NodeType == NodeType.Text).Select(x => x.TextContent)).Trim(['[', ']', ' ']);

		static Func<IElement, Grace> ConstructItem(string area, bool dlc) => x => new()
		{
			Id = ConvertNameToId(ExtractName(x)),
			Name = ExtractName(x),
			Group = area.Split('(')[0].Trim(),
			Url = $"{URL_ROOT}{x.QuerySelector("a")?.GetAttribute("href")}",
			Dlc = dlc,
		};

		var graces = new List<Grace>();
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

		return [.. graces.OrderBy(x => x.Dlc)];
	}

	private static string ConvertNameToId(string name) => name
		.ToLowerInvariant()
		.Replace(" ", "-").Replace(",", "-").Replace(":", "-")
		.Replace("'", "").Replace(".", "").Replace(" ", "")
		.Trim();
}
