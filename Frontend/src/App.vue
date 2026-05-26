<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from "vue"
import { useVirtualizer } from "@tanstack/vue-virtual"

interface ChecklistCategory {
	id: string
	label: string
	groupCaption: string
	statusLabels: [string, string]
}

interface ChecklistItem {
	id: string
	name: string
	group: string
	url: string | null
	dlc: boolean
	category: string
	completed: boolean
}

interface ChecklistResponse {
	categories: ChecklistCategory[]
	items: Omit<ChecklistItem, "completed">[]
}

const STORAGE_KEY = "checklist-completed"
const CACHE_KEY = "checklist-cache"

const categories = ref<ChecklistCategory[]>([])
const items = ref<ChecklistItem[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

const activeTab = ref("")

const load = async (force = false) => {
	loading.value = true
	error.value = null
	try {
		let data: ChecklistResponse | null = null

		if (!force) {
			const cached = localStorage.getItem(CACHE_KEY)
			if (cached) data = JSON.parse(cached) as ChecklistResponse
		}

		if (!data) {
			const res = await fetch("/api/checklist")
			if (!res.ok) throw new Error("Failed to load data from the backend.")
			data = await res.json() as ChecklistResponse
			localStorage.setItem(CACHE_KEY, JSON.stringify(data))
		}

		const completed = new Set<string>(JSON.parse(localStorage.getItem(STORAGE_KEY) ?? "[]"))

		categories.value = data!.categories
		items.value = data!.items.map(x => ({ ...x, completed: completed.has(x.id) }))

		if (!activeTab.value || !categories.value.some(x => x.id === activeTab.value))
			activeTab.value = categories.value[0]?.id ?? ""
	} catch (e) {
		error.value = e instanceof Error ? e.message : "An unexpected error occurred."
	} finally {
		loading.value = false
	}
}

const save = (): void => {
	const completed = items.value.filter(x => x.completed).map(x => x.id)
	localStorage.setItem(STORAGE_KEY, JSON.stringify(completed))
}

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
		const words = nameFilter.value.trim().toLowerCase().split(/\s+/)
		const name = item.name.toLowerCase()
		const group = item.group.toLowerCase()
		if (!words.every(x => name.includes(x) || group.includes(x)))
			return false
	}
	if (dlcFilter.value === "dlc" && !item.dlc) return false
	if (dlcFilter.value === "base" && item.dlc) return false
	if (completionFilter.value === "completed") return item.completed
	if (completionFilter.value === "incomplete") return !item.completed
	return true
}

const filteredItems = computed(() => items.value.filter(matchesFilters))

const categoryCounts = computed(() => {
	const counts: Record<string, { completed: number; total: number }> = {}
	for (const cat of categories.value) {
		const catItems = filteredItems.value.filter(x => x.category === cat.id)
		counts[cat.id] = {
			completed: catItems.filter(x => x.completed).length,
			total: catItems.length,
		}
	}
	return counts
})

watch(categoryCounts, () => {
	if (categoryCounts.value[activeTab.value]?.total > 0) return
	const first = categories.value.find(c => categoryCounts.value[c.id]?.total > 0)
	if (first) activeTab.value = first.id
})

const visibleTabs = computed(() => categories.value.filter(c => categoryCounts.value[c.id]?.total > 0))

const navigateTabLeft = () => {
	const idx = visibleTabs.value.findIndex(c => c.id === activeTab.value)
	if (idx > 0) activeTab.value = visibleTabs.value[idx - 1].id
}

const navigateTabRight = () => {
	const idx = visibleTabs.value.findIndex(c => c.id === activeTab.value)
	if (idx < visibleTabs.value.length - 1) activeTab.value = visibleTabs.value[idx + 1].id
}

const activeConfig = computed(() => categories.value.find(x => x.id === activeTab.value))
const groupCaption = computed(() => activeConfig.value?.groupCaption ?? "")
const completedCaption = computed(() => activeConfig.value?.statusLabels ?? ["", ""] as [string, string])

const activeItems = computed(() => filteredItems.value.filter(x => x.category === activeTab.value))

const scrollRef = ref<HTMLElement | null>(null)
const searchRef = ref<{ $el: HTMLElement } | null>(null)

const onGlobalKeydown = (e: KeyboardEvent) => {
	if (e.ctrlKey || e.metaKey || e.altKey) return
	const inputEl = searchRef.value?.$el
	if (document.activeElement === inputEl) return
	switch (e.key) {
		case 'ArrowUp':    e.preventDefault(); navigateUp(); break
		case 'ArrowDown':  e.preventDefault(); navigateDown(); break
		case 'ArrowLeft':  e.preventDefault(); navigateTabLeft(); break
		case 'ArrowRight': e.preventDefault(); navigateTabRight(); break
		case 'Enter':      toggleSelected(); break
		case 'Backspace':  inputEl?.focus(); break
		default:           (e.key.length === 1) && inputEl?.focus()
	}
}

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

const selectedIndex = ref(0)

watch(activeItems, () => {
	if (selectedIndex.value >= activeItems.value.length)
		selectedIndex.value = 0
})

watch(activeTab, () => {
	selectedIndex.value = 0
	scrollRef.value?.scrollTo({ top: 0 })
})

const scrollToSelected = () => {
	virtualizer.value.scrollToIndex(selectedIndex.value)
}

const navigateUp = () => {
	if (!activeItems.value.length) return
	if (selectedIndex.value > 0) selectedIndex.value--
	scrollToSelected()
}

const navigateDown = () => {
	if (!activeItems.value.length) return
	if (selectedIndex.value < activeItems.value.length - 1) selectedIndex.value++
	scrollToSelected()
}

const toggleSelected = () => {
	const item = activeItems.value[selectedIndex.value]
	if (!item) return
	item.completed = !item.completed
	save()
}

onMounted(() => {
	load()
	document.addEventListener('keydown', onGlobalKeydown)
})

onUnmounted(() => {
	document.removeEventListener('keydown', onGlobalKeydown)
})
</script>

<template>
	<div class="app-wrapper">
		<header class="app-header">
			<h1>Elden Ring Squire</h1>
			<Button icon="pi pi-refresh" label="Refresh" :loading="loading" @click="load(true)" />
		</header>

		<div v-if="error" class="app-error">
			<i class="pi pi-exclamation-triangle" />
			{{ error }}
		</div>

		<Toolbar class="checklist-toolbar">
			<template #start>
				<IconField>
					<InputIcon class="pi pi-search" />
					<InputText ref="searchRef" v-model="nameFilter" placeholder="Search..."
							@keydown.enter="toggleSelected"
							@keydown.up.prevent="navigateUp"
							@keydown.down.prevent="navigateDown"
							@keydown.left.prevent="navigateTabLeft"
							@keydown.right.prevent="navigateTabRight" />
				</IconField>
			</template>
			<template #end>
				<div class="filter-buttons">
					<SelectButton v-model="dlcFilter" :options="dlcOptions" option-label="label"
						option-value="value" :allow-empty="false" />
					<SelectButton v-model="completionFilter" :options="completionOptions" option-label="label"
						option-value="value" :allow-empty="false" />
				</div>
			</template>
		</Toolbar>

		<div v-if="loading" class="table-loading table-background">
			<i class="pi pi-spin pi-spinner" /> Loading...
		</div>
		<div v-else-if="!Object.values(categoryCounts).find(x => x.total)" class="table-loading table-background">
			¯\_(ツ)_/¯
		</div>
		<div v-else class="table-wrapper table-background">
			<Tabs v-model:value="activeTab">
				<TabList>
					<template v-for="cat in categories">
						<Tab v-if="categoryCounts[cat.id]?.total > 0" :key="cat.id" :value="cat.id">
							<span>{{ cat.label }}</span>
							<span class="tab-count">
								{{ categoryCounts[cat.id]?.completed ?? 0 }} / {{ categoryCounts[cat.id]?.total ?? 0 }}
							</span>
						</Tab>
					</template>
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
				<div :style="{ height: `${virtualizer.getTotalSize()}px`, position: 'relative' }">
					<div v-for="row in virtualRows" :key="row.item.id" class="table-row table-grid"
						:class="{ 'row-done': row.item.completed, 'row-selected': row.index === selectedIndex }"
						:style="{ position: 'absolute', top: 0, width: '100%', transform: `translateY(${row.start}px)` }">
						<div class="cell">
							<Checkbox v-model="row.item.completed" :binary="true" @change="save()" />
						</div>
						<div class="cell">
							<a v-if="row.item.url" :href="row.item.url" target="_blank" rel="noopener noreferrer">
								{{ row.item.name }}
							</a>
							<span v-else>
								{{ row.item.name }}
							</span>
						</div>
						<div class="cell">
							{{ row.item.group }}
						</div>
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
	height: 100%;
	display: flex;
	flex-direction: column;
	gap: 1rem;
	padding: 1rem;
}

.app-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
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
	border-radius: 6px;
	background: var(--p-red-100);
	color: var(--p-red-700);
}

.checklist-toolbar {
	border: 0;
}

.p-inputtext {
	border: 0;
}

.tab-count {
	margin-left: 0.5rem;
	font-size: 0.75rem;
	padding: 0.15rem 0.4rem;
	border-radius: var(--p-tag-border-radius, 4px);
	background: var(--p-tag-secondary-background);
	color: var(--p-tag-secondary-color);
	white-space: nowrap;
}

.table-grid {
	display: grid;
	grid-template-columns: auto 1fr auto 4rem 8rem;
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

.table-row {
	padding: 0.5rem 0.75rem;
	border-bottom: 1px solid var(--p-surface-border);
	background: var(--p-surface-card);
}

.table-scroll {
	flex: 1;
	min-height: 0;
	overflow-y: scroll;
}

.table-wrapper {
	flex: 1;
	display: flex;
	flex-direction: column;
	min-height: 0;
}

.row-done {
	opacity: 0.5;
}

.row-selected {
	outline: 1px solid var(--p-primary-color);
	outline-offset: -1px;
}

.cell {
	padding: 0 0.25rem;
}

.table-background {
	padding: var(--p-toolbar-padding);
	background: var(--p-toolbar-background);
	color: var(--p-toolbar-color);
	border-radius: var(--p-toolbar-border-radius);
}

.table-grid > :nth-child(4) {
	display: flex;
	justify-content: center;
}

.table-loading {
	flex: 1;
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 0.5rem;
	color: var(--p-text-muted-color);
}

a {
	color: var(--p-primary-color);
	text-decoration: none;
}

a:hover {
	text-decoration: underline;
}

.p-tab {
	padding-top: 0;
}

.filter-buttons {
	display: flex;
	gap: 1rem;
	align-items: center;
}

@media (pointer: coarse) {
	.row-selected {
		outline: none;
	}
}

@media (max-width: 640px) {
	.app-wrapper {
		padding: 0.5rem;
		gap: 0.5rem;
	}

	.app-header h1 {
		font-size: 1.25rem;
	}

	.checklist-toolbar :deep(.p-toolbar) {
		flex-wrap: wrap;
		gap: 0.5rem;
	}

	.checklist-toolbar :deep(.p-toolbar-start),
	.checklist-toolbar :deep(.p-toolbar-end) {
		width: 100%;
	}

	.checklist-toolbar :deep(.p-iconfield) {
		width: 100%;
	}

	.checklist-toolbar :deep(.p-inputtext) {
		width: 100%;
	}

	.filter-buttons {
		flex-wrap: wrap;
		gap: 0.5rem;
	}

	.table-wrapper :deep(.p-tablist-content) {
		overflow-x: auto;
		scrollbar-width: none;
	}

	.table-grid {
		grid-template-columns: auto 1fr auto;
	}

	.table-grid > :nth-child(3),
	.table-grid > :nth-child(4) {
		display: none;
	}
}
</style>
