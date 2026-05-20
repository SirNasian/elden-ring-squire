using EldenRingSquire.Backend.Models.Checklist;

namespace EldenRingSquire.Backend.Services.Interfaces;

public interface IWikiScraperService
{
	public Task<IList<Boss>> ScrapeBosses(CancellationToken ct = default);
	public Task<IList<Grace>> ScrapeGraces(CancellationToken ct = default);
}
