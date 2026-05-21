using EldenRingSquire.Backend.Models.Checklist;

namespace EldenRingSquire.Backend.Services.Interfaces;

public interface IChecklistDataService
{
	public Task<IList<Boss>> GetBosses(CancellationToken ct = default);
	public Task<IList<Grace>> GetGraces(CancellationToken ct = default);
}
