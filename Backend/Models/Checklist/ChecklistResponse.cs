namespace EldenRingSquire.Backend.Models.Checklist;

public class ChecklistCategory
{
	public required string Id { get; set; }
	public required string Label { get; set; }
	public required string GroupCaption { get; set; }
	public required string[] StatusLabels { get; set; }
}

public class ChecklistResponse
{
	public required IList<ChecklistCategory> Categories { get; set; }
	public required IList<ChecklistItem> Items { get; set; }
}
