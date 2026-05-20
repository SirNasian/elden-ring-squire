using EldenRingSquire.Backend.Models;

namespace EldenRingSquire.Backend.Services.Interfaces;

public interface IWikiScraperService
{
	public Task<IList<Grace>> ScrapeGraces(CancellationToken ct = default);
}
