export interface GetChecklistResponse {
	categories: ChecklistCategory[]
	items: ChecklistItem[]
}

export interface ChecklistCategory {
	id: string;
	label: string;
	groupCaption: string;
	statusLabels: [string, string];
}

export interface ChecklistItem {
	id: string;
	category: string;
	name: string;
	group: string;
	url?: string;
	dlc: boolean;
}
