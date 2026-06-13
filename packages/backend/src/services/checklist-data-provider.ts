import { ChecklistCategory, ChecklistItem } from "@ersquire/shared";

export interface ChecklistDataProvider {
	getCategories: () => Promise<ChecklistCategory[]>;
	getItems: () => Promise<ChecklistItem[]>;
}
