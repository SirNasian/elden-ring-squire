<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue"
import { useVirtualizer } from "@tanstack/vue-virtual"

interface ChecklistItem {
	id: string
	name: string
	group: string
	url: string | null
	dlc: boolean
	category: string
	completed: boolean
}

const STORAGE_KEY = "elden-ring-squire-completed"

const bosses = ref<ChecklistItem[]>([])
const graces = ref<ChecklistItem[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

const load = async () => {
	loading.value = true
	error.value = null
	try {
		const [bossRes, graceRes] = await Promise.all([
			fetch("/api/scrape/bosses", { method: "POST" }),
			fetch("/api/scrape/graces", { method: "POST" }),
		])
		if (!bossRes.ok || !graceRes.ok)
			throw new Error("Failed to load data from the backend.")
		const [bossData, graceData]: [Omit<ChecklistItem, "completed">[], Omit<ChecklistItem, "completed">[]] =
			await Promise.all([bossRes.json(), graceRes.json()])
		const completed = new Set<string>(JSON.parse(localStorage.getItem(STORAGE_KEY) ?? "[]"))
		const mapCompleted = (x: Omit<ChecklistItem, "completed">) => ({ ...x, completed: completed.has(x.id) })
		const sortDlc = (a: ChecklistItem, b: ChecklistItem) => (a.dlc ? 1 : 0) - (b.dlc ? 1 : 0)
		bosses.value = bossData.map(mapCompleted).sort(sortDlc)
		graces.value = graceData.map(mapCompleted).sort(sortDlc)
	} catch (e) {
		error.value = e instanceof Error ? e.message : "An unexpected error occurred."
	} finally {
		loading.value = false
	}
}

const save = (): void => {
	const completed = [
		...bosses.value.filter(x => x.completed).map(x => x.id),
		...graces.value.filter(x => x.completed).map(x => x.id),
	]
	localStorage.setItem(STORAGE_KEY, JSON.stringify(completed))
}

const bossCount = computed(() => bosses.value.filter(x => x.completed).length)
const graceCount = computed(() => graces.value.filter(x => x.completed).length)

type CompletionFilter = "all" | "completed" | "incomplete"

const nameFilter = ref("")

const completionFilter = ref<CompletionFilter>("all")
const completionOptions: { label: string; value: CompletionFilter }[] = [
	{ label: "All", value: "all" },
	{ label: "Completed", value: "completed" },
	{ label: "Incomplete", value: "incomplete" },
]

function matchesFilters(item: ChecklistItem): boolean {
	if (nameFilter.value.trim()) {
		const lower = item.name.toLowerCase()
		if (!nameFilter.value.trim().toLowerCase().split(/\s+/).every(w => lower.includes(w)))
			return false
	}
	if (completionFilter.value === "completed") return item.completed
	if (completionFilter.value === "incomplete") return !item.completed
	return true
}

const filteredBosses = computed(() => bosses.value.filter(matchesFilters))
const filteredGraces = computed(() => graces.value.filter(matchesFilters))

const activeTab = ref<"bosses" | "graces">("bosses")

watch([filteredBosses, filteredGraces], () => {
	const currentEmpty = activeTab.value === "bosses"
		? filteredBosses.value.length === 0
		: filteredGraces.value.length === 0
	if (!currentEmpty) return
	if (activeTab.value === "bosses" && filteredGraces.value.length > 0) activeTab.value = "graces"
	else if (activeTab.value === "graces" && filteredBosses.value.length > 0) activeTab.value = "bosses"
})

const activeItems = computed(() =>
	activeTab.value === "bosses" ? filteredBosses.value : filteredGraces.value
)

const scrollRef = ref<HTMLElement | null>(null)

const virtualizer = useVirtualizer(computed(() => ({
	count: activeItems.value.length,
	getScrollElement: () => scrollRef.value,
	estimateSize: () => 42,
	overscan: 10,
})))

const virtualRows = computed(() =>
	virtualizer.value.getVirtualItems().map(vRow => ({
		...vRow,
		item: activeItems.value[vRow.index],
	}))
)

watch(activeTab, () => scrollRef.value?.scrollTo({ top: 0 }))

onMounted(load)
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

		<Toolbar class="checklist-toolbar">
			<template #start>
				<IconField>
					<InputIcon class="pi pi-search" />
					<InputText v-model="nameFilter" placeholder="Search by name..." />
				</IconField>
			</template>
			<template #end>
				<SelectButton v-model="completionFilter" :options="completionOptions" option-label="label"
					option-value="value" :allow-empty="false" />
			</template>
		</Toolbar>

		<Tabs v-model:value="activeTab">
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
		</Tabs>

		<div class="table-header table-grid">
			<div></div>
			<div>Name</div>
			<div>{{ activeTab === "bosses" ? "Location" : "Area" }}</div>
			<div></div>
			<div></div>
		</div>

		<div ref="scrollRef" class="table-scroll">
			<div v-if="loading" class="table-empty">
				<i class="pi pi-spin pi-spinner" /> Loading...
			</div>
			<div v-else :style="{ height: `${virtualizer.getTotalSize()}px`, position: 'relative' }">
				<div v-for="row in virtualRows" :key="row.item.id" class="table-row table-grid"
					:class="{ 'row-stripe': row.index % 2 !== 0, 'row-done': row.item.completed }"
					:style="{ position: 'absolute', top: 0, width: '100%', transform: `translateY(${row.start}px)` }">
					<div class="cell">
						<Checkbox v-model="row.item.completed" :binary="true" @change="save()" />
					</div>
					<div class="cell">
						<a v-if="row.item.url" :href="row.item.url" target="_blank" rel="noopener noreferrer">{{
							row.item.name
						}}</a>
						<span v-else>{{ row.item.name }}</span>
					</div>
					<div class="cell">{{ row.item.group }}</div>
					<div class="cell">
						<Tag v-if="row.item.dlc" value="DLC" severity="warn" />
					</div>
					<div class="cell">
						<Tag :value="row.item.completed ? (activeTab === 'bosses' ? 'Defeated' : 'Found') : (activeTab === 'bosses' ? 'Alive' : 'Not Found')"
							:severity="row.item.completed ? 'success' : 'danger'" />
					</div>
				</div>
			</div>
		</div>
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

.checklist-toolbar {
	margin-bottom: 0.75rem;
}

.tab-count {
	margin-left: 0.5rem;
	font-size: 0.75rem;
}

.table-grid {
	display: grid;
	grid-template-columns: 3.5rem 1fr auto 5rem 8rem;
	align-items: center;
}

.table-header {
	font-weight: 600;
	font-size: 0.875rem;
	padding: 0.5rem 0.75rem;
	border-bottom: 2px solid var(--p-surface-border);
	background: var(--p-surface-ground);
}

.table-header > div {
	padding: 0 0.25rem;
}

.table-scroll {
	overflow-y: auto;
	max-height: calc(100vh - 280px);
}

.table-row {
	padding: 0.5rem 0.75rem;
	border-bottom: 1px solid var(--p-surface-border);
	background: var(--p-surface-card);
}

.row-stripe {
	background: var(--p-surface-ground);
}

.row-done {
	opacity: 0.5;
}

.cell {
	padding: 0 0.25rem;
}

.table-empty {
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 0.5rem;
	height: 6rem;
	color: var(--p-text-muted-color);
}

a {
	color: var(--p-primary-color);
	text-decoration: none;
}

a:hover {
	text-decoration: underline;
}
</style>
