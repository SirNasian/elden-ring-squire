namespace EldenRingSquire.Backend.Models.Checklist;

public class ChecklistItem
{
	public required string Id { get; set; }
	public required string Category { get; set; }
	public required string Name { get; set; }
	public required string Group { get; set; }
	public required string? Url { get; set; }
	public required bool Dlc { get; set; }
}
