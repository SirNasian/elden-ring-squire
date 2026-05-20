using AngleSharp.Dom;
using EldenRingSquire.Backend.Models.Checklist;

namespace EldenRingSquire.Backend.Services.WikiScraper;

public class FextraLifeWikiScraperService(HttpClient http) : BaseWikiScraperService(http)
{
	private const string URL_GRACES = "https://eldenring.wiki.fextralife.com/Sites+of+Grace";

	public override async Task<IList<Grace>> ScrapeGraces(CancellationToken ct = default)
	{
		static string ExtractGraceName(IElement x) =>
			string.Concat(x.ChildNodes.Where(x => x.NodeType == NodeType.Text).Select(x => x.TextContent)).Trim(['[', ']', ' ']);

		static string ExtractGraceId(IElement x) =>
			ExtractGraceName(x).ToLowerInvariant()
				.Replace(" ", "-").Replace(",", "-").Replace(":", "-")
				.Replace("'", "").Replace(".", "").Replace(" ", "")
				.Trim();

		static Func<IElement, Grace> ConstructGrace(string area, bool dlc) => x => new()
		{
			Id = ExtractGraceId(x),
			Name = ExtractGraceName(x),
			Group = area.Split('(')[0].Trim(),
			Url = x.QuerySelector("a")?.GetAttribute("href"),
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
								.Select(ConstructGrace(heading.TextContent, tabContent.ClassList.Contains("1-tab")))
							?? []
						);

		return graces;
	}
}
