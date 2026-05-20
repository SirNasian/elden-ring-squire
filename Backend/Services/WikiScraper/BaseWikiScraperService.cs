using AngleSharp;
using AngleSharp.Dom;
using EldenRingSquire.Backend.Models.Checklist;
using EldenRingSquire.Backend.Services.Interfaces;

namespace EldenRingSquire.Backend.Services.WikiScraper;

public abstract class BaseWikiScraperService(HttpClient httpClient) : IWikiScraperService
{
	public abstract Task<IList<Boss>> ScrapeBosses(CancellationToken ct = default);
	public abstract Task<IList<Grace>> ScrapeGraces(CancellationToken ct = default);

	protected async Task<IDocument> GetParsedDocumentAsync(string url, CancellationToken cancellationToken = default)
	{
		var html = await httpClient.GetStringAsync(url, cancellationToken);
		var context = BrowsingContext.New(Configuration.Default);
		return await context.OpenAsync(req => req.Content(html), cancellationToken);
	}
}
