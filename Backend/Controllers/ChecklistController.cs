using EldenRingSquire.Backend.Models.Checklist;
using EldenRingSquire.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EldenRingSquire.Backend.Controllers;

[ApiController]
[Route("api/checklist")]
public class ChecklistController(IChecklistDataService checklist) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> Get(CancellationToken ct) =>
		Ok(new ChecklistResponse()
		{
			Categories = await checklist.GetCategories(ct),
			Items = await checklist.GetItems(ct),
		});
}
