using EldenRingSquire.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EldenRingSquire.Backend.Controllers;

[ApiController]
[Route("api/checklist")]
public class ChecklistController(IChecklistDataService checklist) : ControllerBase
{
	[HttpGet("bosses")]
	public async Task<IActionResult> GetBosses(CancellationToken ct) =>
		Ok(await checklist.GetBosses(ct));

	[HttpGet("graces")]
	public async Task<IActionResult> GetGraces(CancellationToken ct) =>
		Ok(await checklist.GetGraces(ct));
}
