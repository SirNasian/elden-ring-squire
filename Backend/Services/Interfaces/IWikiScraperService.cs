using EldenRingSquire.Backend.Models.Checklist;

namespace EldenRingSquire.Backend.Services.Interfaces;

public interface IWikiScraperService
{
	public Task<IList<Grace>> ScrapeGraces(CancellationToken ct = default);
}
