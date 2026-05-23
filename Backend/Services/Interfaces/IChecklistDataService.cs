using EldenRingSquire.Backend.Models.Checklist;

namespace EldenRingSquire.Backend.Services.Interfaces;

public interface IChecklistDataService
{
	public Task<IList<ChecklistCategory>> GetCategories(CancellationToken ct = default);
	public Task<IList<ChecklistItem>> GetItems(CancellationToken ct = default);
}
