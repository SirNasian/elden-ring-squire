using EldenRingSquire.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EldenRingSquire.Backend.Controllers;

[ApiController]
[Route("api/scrape")]
public class WikiScraperController(IWikiScraperService scraper) : ControllerBase
{
	[HttpPost("graces")]
	public async Task<IActionResult> ScrapeGraces(CancellationToken ct) =>
		Ok(await scraper.ScrapeGraces(ct));
}
