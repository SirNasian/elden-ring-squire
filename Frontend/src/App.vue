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
const weapons = ref<ChecklistItem[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

const load = async () => {
	loading.value = true
	error.value = null
	try {
		const [bossRes, graceRes, weaponRes] = await Promise.all([
			fetch("/api/checklist/bosses"),
			fetch("/api/checklist/graces"),
			fetch("/api/checklist/weapons"),
		])

		if (!bossRes.ok || !graceRes.ok || !weaponRes.ok)
			throw new Error("Failed to load data from the backend.")

		const [bossData, graceData, weaponData]: [Omit<ChecklistItem, "completed">[], Omit<ChecklistItem, "completed">[], Omit<ChecklistItem, "completed">[]] =
			await Promise.all([bossRes.json(), graceRes.json(), weaponRes.json()])

		const completed = new Set<string>(JSON.parse(localStorage.getItem(STORAGE_KEY) ?? "[]"))
		const mapCompleted = (x: Omit<ChecklistItem, "completed">) => ({ ...x, completed: completed.has(x.id) })
		bosses.value = bossData.map(mapCompleted)
		graces.value = graceData.map(mapCompleted)
		weapons.value = weaponData.map(mapCompleted)
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
		...weapons.value.filter(x => x.completed).map(x => x.id),
	]
	localStorage.setItem(STORAGE_KEY, JSON.stringify(completed))
}

const applyDlcFilter = (items: ChecklistItem[]) =>
	dlcFilter.value === "dlc" ? items.filter(x => x.dlc) :
	dlcFilter.value === "base" ? items.filter(x => !x.dlc) :
	items

const dlcFilterBosses = computed(() => applyDlcFilter(bosses.value))
const dlcFilterGraces = computed(() => applyDlcFilter(graces.value))
const dlcFilterWeapons = computed(() => applyDlcFilter(weapons.value))

const bossCount = computed(() => dlcFilterBosses.value.filter(x => x.completed).length)
const graceCount = computed(() => dlcFilterGraces.value.filter(x => x.completed).length)
const weaponCount = computed(() => dlcFilterWeapons.value.filter(x => x.completed).length)

type CompletionFilter = "all" | "completed" | "incomplete"
type DlcFilter = "all" | "dlc" | "base"

const nameFilter = ref("")

const completionFilter = ref<CompletionFilter>("all")
const completionOptions: { label: string; value: CompletionFilter }[] = [
	{ label: "All", value: "all" },
	{ label: "Completed", value: "completed" },
	{ label: "Incomplete", value: "incomplete" },
]

const dlcFilter = ref<DlcFilter>("all")
const dlcOptions: { label: string; value: DlcFilter }[] = [
	{ label: "All", value: "all" },
	{ label: "Base", value: "base" },
	{ label: "DLC", value: "dlc" },
]

function matchesFilters(item: ChecklistItem): boolean {
	if (nameFilter.value.trim()) {
		const lower = item.name.toLowerCase()
		if (!nameFilter.value.trim().toLowerCase().split(/\s+/).every(w => lower.includes(w)))
			return false
	}
	if (dlcFilter.value === "dlc" && !item.dlc) return false
	if (dlcFilter.value === "base" && item.dlc) return false
	if (completionFilter.value === "completed") return item.completed
	if (completionFilter.value === "incomplete") return !item.completed
	return true
}

const filteredBosses = computed(() => bosses.value.filter(matchesFilters))
const filteredGraces = computed(() => graces.value.filter(matchesFilters))
const filteredWeapons = computed(() => weapons.value.filter(matchesFilters))

const activeTab = ref<"bosses" | "graces" | "weapons">("bosses")

watch([filteredBosses, filteredGraces, filteredWeapons], () => {
	const currentEmpty =
		activeTab.value === "bosses" ? filteredBosses.value.length === 0 :
		activeTab.value === "graces" ? filteredGraces.value.length === 0 :
		filteredWeapons.value.length === 0
	if (!currentEmpty) return

	if (filteredBosses.value.length > 0) activeTab.value = "bosses"
	else if (filteredGraces.value.length > 0) activeTab.value = "graces"
	else if (filteredWeapons.value.length > 0) activeTab.value = "weapons"
})

const activeItems = computed(() =>
	activeTab.value === "bosses" ? filteredBosses.value :
	activeTab.value === "graces" ? filteredGraces.value :
	filteredWeapons.value
)

const groupCaption = computed(() =>
	activeTab.value === "bosses" ? "Location" :
	activeTab.value === "graces" ? "Area" :
	"Type"
)

const completedCaption = computed(() =>
	activeTab.value === "bosses" ? ["Alive", "Defeated"] :
	activeTab.value === "graces" ? ["Not Found", "Found"] :
	["Not Obtained", "Obtained"]
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

const toggleFirst = () => {
	const first = activeItems.value[0]
	if (!first) return
	first.completed = !first.completed
	save()
}

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
					<InputText v-model="nameFilter" placeholder="Search by name..." @keydown.enter="toggleFirst" />
				</IconField>
			</template>
			<template #end>
				<SelectButton v-model="dlcFilter" :options="dlcOptions" option-label="label"
					option-value="value" :allow-empty="false" style="margin-right: 1rem" />
				<SelectButton v-model="completionFilter" :options="completionOptions" option-label="label"
					option-value="value" :allow-empty="false" />
			</template>
		</Toolbar>

		<Tabs v-model:value="activeTab">
			<TabList>
				<Tab value="bosses">
					Bosses
					<Tag :value="`${bossCount} / ${dlcFilterBosses.length}`" severity="secondary" class="tab-count" />
				</Tab>
				<Tab value="graces">
					Graces
					<Tag :value="`${graceCount} / ${dlcFilterGraces.length}`" severity="secondary" class="tab-count" />
				</Tab>
				<Tab value="weapons">
					Weapons
					<Tag :value="`${weaponCount} / ${dlcFilterWeapons.length}`" severity="secondary" class="tab-count" />
				</Tab>
			</TabList>
		</Tabs>

		<div class="table-header table-grid">
			<div></div>
			<div>Name</div>
			<div>{{ groupCaption }}</div>
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
						<Tag :value="completedCaption[row.item.completed ? 1 : 0]"
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

.table-grid > :nth-child(4),
.table-grid > :nth-child(5) {
	display: flex;
	justify-content: center;
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
