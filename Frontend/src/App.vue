<script setup lang="ts">
import { ref, computed, onMounted } from "vue"

interface ChecklistItem {
	id: string
	name: string
	group: string
	url: string | null
	dlc: boolean
	category: string
	completed: boolean
}

const STORAGE_KEY = "elden-ring-squire-completed";

const bosses = ref<ChecklistItem[]>([]);
const graces = ref<ChecklistItem[]>([]);

const loading = ref(false);
const error = ref<string | null>(null);

const load = async () => {
	loading.value = true;
	error.value = null;

	try {
		const [bossRes, graceRes] = await Promise.all([
			fetch("/api/scrape/bosses", { method: "POST" }),
			fetch("/api/scrape/graces", { method: "POST" }),
		]);

		if (!bossRes.ok || !graceRes.ok)
			throw new Error("Failed to load data from the backend.");

		const [bossData, graceData]: [Omit<ChecklistItem, "completed">[], Omit<ChecklistItem, "completed">[]] =
			await Promise.all([bossRes.json(), graceRes.json()]);

		const completed = new Set<string>(JSON.parse(localStorage.getItem(STORAGE_KEY) ?? "[]"));
		const mapCompleted = (x: Omit<ChecklistItem, "completed">) => ({ ...x, completed: completed.has(x.id) });
		const sortDlc = (a: ChecklistItem, b: ChecklistItem) => ((a.dlc ? 1 : 0) - (b.dlc ? 1 : 0));
		bosses.value = bossData.map(mapCompleted).sort(sortDlc);
		graces.value = graceData.map(mapCompleted).sort(sortDlc);

	} catch (e) {
		error.value = e instanceof Error ? e.message : "An unexpected error occurred.";
	} finally {
		loading.value = false;
	}
}

const save = async (): Promise<void> => {
	const completed: string[] = [];
	completed.push(...bosses.value.filter(x => x.completed).map(x => x.id));
	completed.push(...graces.value.filter(x => x.completed).map(x => x.id));
	localStorage.setItem(STORAGE_KEY, JSON.stringify(completed));
}

const bossCount = computed(() => bosses.value.filter(x => x.completed).length);
const graceCount = computed(() => graces.value.filter(x => x.completed).length);

onMounted(load);
</script>

<template>
	<div class="app-wrapper">
		<header class="app-header">
			<h1>Elden Ring Squire</h1>
			<Button icon="pi pi-refresh" label="Refresh" :loading="loading" @click="load" />
		</header>

		<div v-if="error" class="app-error">
			<i class="pi pi-exclamation-triangle" />
			{{ error }}
		</div>

		<Tabs value="bosses">
			<TabList>
				<Tab value="bosses">
					Bosses
					<Tag :value="`${bossCount} / ${bosses.length}`" severity="secondary" class="tab-count" />
				</Tab>
				<Tab value="graces">
					Sites of Grace
					<Tag :value="`${graceCount} / ${graces.length}`" severity="secondary" class="tab-count" />
				</Tab>
			</TabList>

			<TabPanels>
				<TabPanel value="bosses">
					<DataTable :value="bosses" :loading="loading" data-key="id" striped-rows scrollable
						scroll-height="calc(100vh - 280px)">
						<Column header="" style="width: 3.5rem">
							<template #body="{ data }">
								<Checkbox v-model="data.completed" :binary="true" @change="save()" />
							</template>
						</Column>
						<Column field="name" header="Name">
							<template #body="{ data }">
								<a v-if="data.url" :href="data.url" target="_blank" rel="noopener noreferrer">{{
									data.name }}</a>
								<span v-else>{{ data.name }}</span>
							</template>
						</Column>
						<Column field="group" header="Location" />
						<Column field="dlc" header="" style="width: 5rem">
							<template #body="{ data }">
								<Tag v-if="data.dlc" value="DLC" severity="warn" />
							</template>
						</Column>
						<Column field="completed" header="" style="width: 8rem">
							<template #body="{ data }">
								<Tag :value="data.completed ? 'Defeated' : 'Alive'"
									:severity="data.completed ? 'success' : 'danger'" />
							</template>
						</Column>
					</DataTable>
				</TabPanel>

				<TabPanel value="graces">
					<DataTable :value="graces" :loading="loading" data-key="id" striped-rows scrollable
						scroll-height="calc(100vh - 280px)">
						<Column header="" style="width: 3.5rem">
							<template #body="{ data }">
								<Checkbox v-model="data.completed" :binary="true" @change="save()" />
							</template>
						</Column>
						<Column field="name" header="Name">
							<template #body="{ data }">
								<a v-if="data.url" :href="data.url" target="_blank" rel="noopener noreferrer">{{
									data.name }}</a>
								<span v-else>{{ data.name }}</span>
							</template>
						</Column>
						<Column field="group" header="Location" />
						<Column field="dlc" header="" style="width: 5rem">
							<template #body="{ data }">
								<Tag v-if="data.dlc" value="DLC" severity="warn" />
							</template>
						</Column>
						<Column field="completed" header="" style="width: 8rem">
							<template #body="{ data }">
								<Tag :value="data.completed ? 'Found' : 'Not Found'"
									:severity="data.completed ? 'success' : 'danger'" />
							</template>
						</Column>
					</DataTable>
				</TabPanel>
			</TabPanels>
		</Tabs>
	</div>
</template>

<style>
*,
*::before,
*::after {
	box-sizing: border-box;
}

body {
	margin: 0;
	font-family: system-ui, sans-serif;
	background: var(--p-surface-ground);
	color: var(--p-text-color);
}
</style>

<style scoped>
.app-wrapper {
	max-width: 1200px;
	margin: 0 auto;
	padding: 1.5rem;
}

.app-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 1.5rem;
}

.app-header h1 {
	margin: 0;
	font-size: 1.75rem;
}

.app-error {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	padding: 0.75rem 1rem;
	margin-bottom: 1rem;
	border-radius: 6px;
	background: var(--p-red-100);
	color: var(--p-red-700);
}

.tab-count {
	margin-left: 0.5rem;
	font-size: 0.75rem;
}

.progress-row {
	display: flex;
	align-items: center;
	gap: 1rem;
	padding: 0.75rem 0 1rem;
}

.progress-label {
	white-space: nowrap;
	font-size: 0.875rem;
	color: var(--p-text-muted-color);
	min-width: 9rem;
}

.progress-bar {
	flex: 1;
}

a {
	color: var(--p-primary-color);
	text-decoration: none;
}

a:hover {
	text-decoration: underline;
}
</style>
