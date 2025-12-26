<template>
  <n-card title="Dependencies Tree" size="small" class="dependency-tree-card">
    <template #header-extra>
      <n-space>
        <n-tag v-if="hasCircularDependencies" type="error" size="small">
          <template #icon>
            <n-icon><warning-outline /></n-icon>
          </template>
          Circular Dependencies
        </n-tag>
        <n-tag type="info" size="small">
          {{ totalDependencies }} dependencies
        </n-tag>
        <n-tag type="info" size="small">
          Max depth: {{ maxDepth }}
        </n-tag>
      </n-space>
    </template>

    <div v-if="loading" class="loading-container">
      <n-spin size="medium" />
      <n-text depth="3">Loading dependency tree...</n-text>
    </div>

    <div v-else-if="error" class="error-container">
      <n-alert type="error" title="Failed to load dependencies">
        {{ error }}
      </n-alert>
    </div>

    <div v-else-if="treeData" class="tree-container">
      <n-tree
        :data="treeData"
        block-line
        expand-on-click
        selectable
        :default-expanded-keys="defaultExpandedKeys"
        :render-label="renderLabel"
        :render-prefix="renderPrefix"
      />

      <div v-if="circularPaths.length > 0" class="circular-warnings">
        <n-divider />
        <n-text strong>Circular Dependencies Detected:</n-text>
        <n-list>
          <n-list-item v-for="(path, index) in circularPaths" :key="index">
            <n-text depth="3" style="font-family: monospace; font-size: 12px;">
              {{ path }}
            </n-text>
          </n-list-item>
        </n-list>
      </div>
    </div>
  </n-card>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, h } from 'vue'
import {
  NCard,
  NTree,
  NSpin,
  NAlert,
  NText,
  NTag,
  NSpace,
  NIcon,
  NDivider,
  NList,
  NListItem,
  type TreeOption
} from 'naive-ui'
import {
  WarningOutline,
  GitBranchOutline,
  AlertCircleOutline,
  CheckmarkCircleOutline
} from '@vicons/ionicons5'
import type { DependencyTreeNode, DependencyTreeResponse } from '../types/package'
import { apiClient } from '../api'

interface Props {
  packageId: string
  version: string
  maxDepth?: number
}

const props = withDefaults(defineProps<Props>(), {
  maxDepth: 10
})

const loading = ref(false)
const error = ref<string | null>(null)
const treeResponse = ref<DependencyTreeResponse | null>(null)
const treeData = ref<TreeOption[]>([])
const defaultExpandedKeys = ref<string[]>([])

const totalDependencies = computed(() => treeResponse.value?.totalDependencies ?? 0)
const maxDepth = computed(() => treeResponse.value?.maxDepth ?? 0)
const hasCircularDependencies = computed(() => treeResponse.value?.hasCircularDependencies ?? false)
const circularPaths = computed(() => treeResponse.value?.circularPaths ?? [])

onMounted(async () => {
  await loadDependencyTree()
})

async function loadDependencyTree() {
  loading.value = true
  error.value = null

  try {
    const response = await apiClient.get<{ data: DependencyTreeResponse }>(
      `/v3/package/${props.packageId}/${props.version}/dependencies/tree`,
      { params: { maxDepth: props.maxDepth } }
    )

    treeResponse.value = response.data.data
    if (treeResponse.value) {
      treeData.value = [buildTreeOption(treeResponse.value.rootNode)]
      defaultExpandedKeys.value = [getNodeKey(treeResponse.value.rootNode)]
    }
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to load dependency tree'
  } finally {
    loading.value = false
  }
}

function buildTreeOption(node: DependencyTreeNode): TreeOption {
  const key = getNodeKey(node)
  const label = `${node.packageId}@${node.version}`

  return {
    key,
    label,
    children: node.dependencies.length > 0
      ? node.dependencies.map(dep => buildTreeOption(dep))
      : undefined,
    isLeaf: node.dependencies.length === 0,
    prefix: () => renderPrefix({ option: { node } } as any),
    node // Store the original node data
  }
}

function getNodeKey(node: DependencyTreeNode): string {
  return `${node.packageId}@${node.version}`
}

function renderLabel({ option }: { option: TreeOption }) {
  const node = (option as any).node as DependencyTreeNode

  return h('div', { class: 'tree-node-label' }, [
    h('span', { class: 'package-name' }, node.packageId),
    h('span', { class: 'package-version' }, `@${node.version}`),
    node.versionRange && node.versionRange !== node.version
      ? h(NTag, { size: 'tiny', type: 'default', style: { marginLeft: '8px' } }, () => node.versionRange)
      : null,
    !node.isResolved
      ? h(NTag, { size: 'tiny', type: 'warning', style: { marginLeft: '8px' } }, () => 'Unresolved')
      : null,
    node.isCircular
      ? h(NTag, { size: 'tiny', type: 'error', style: { marginLeft: '8px' } }, () => 'Circular')
      : null
  ])
}

function renderPrefix({ option }: { option: TreeOption }) {
  const node = (option as any).node as DependencyTreeNode

  if (node.isCircular) {
    return h(NIcon, { color: '#d03050', size: 16 }, () => h(AlertCircleOutline))
  } else if (!node.isResolved) {
    return h(NIcon, { color: '#f0a020', size: 16 }, () => h(WarningOutline))
  } else {
    return h(NIcon, { color: '#18a058', size: 16 }, () => h(GitBranchOutline))
  }
}
</script>

<style scoped>
.dependency-tree-card {
  height: 100%;
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  padding: 40px 0;
}

.error-container {
  padding: 16px 0;
}

.tree-container {
  max-height: 600px;
  overflow-y: auto;
}

.tree-node-label {
  display: flex;
  align-items: center;
  gap: 4px;
}

.package-name {
  font-weight: 600;
  font-size: 13px;
}

.package-version {
  color: var(--n-text-color-3);
  font-size: 12px;
}

.circular-warnings {
  margin-top: 16px;
}

.circular-warnings .n-list {
  max-height: 200px;
  overflow-y: auto;
}
</style>
