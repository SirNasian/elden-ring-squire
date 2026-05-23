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

	[HttpGet("weapons")]
	public async Task<IActionResult> GetWeapons(CancellationToken ct) =>
		Ok(await checklist.GetWeapons(ct));

	[HttpGet("shields")]
	public async Task<IActionResult> GetShields(CancellationToken ct) =>
		Ok(await checklist.GetShields(ct));
}
