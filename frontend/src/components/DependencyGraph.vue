<template>
  <n-card title="Dependencies Graph" size="small" class="dependency-graph-card">
    <template #header-extra>
      <n-space>
        <n-tag v-if="hasCircularDependencies" type="error" size="small">
          <template #icon>
            <n-icon><warning-outline /></n-icon>
          </template>
          Circular Dependencies
        </n-tag>
        <n-tag type="info" size="small">
          {{ totalNodes }} nodes
        </n-tag>
        <n-tag type="info" size="small">
          {{ totalEdges }} edges
        </n-tag>
      </n-space>
    </template>

    <div v-if="loading" class="loading-container">
      <n-spin size="medium" />
      <n-text depth="3">Loading dependency graph...</n-text>
    </div>

    <div v-else-if="error" class="error-container">
      <n-alert type="error" title="Failed to load dependencies">
        {{ error }}
      </n-alert>
    </div>

    <div v-else-if="graphData" class="graph-container">
      <svg :width="svgWidth" :height="svgHeight" class="graph-svg">
        <!-- Edges -->
        <g class="edges">
          <line
            v-for="edge in positionedEdges"
            :key="`${edge.from}-${edge.to}`"
            :x1="edge.x1"
            :y1="edge.y1"
            :x2="edge.x2"
            :y2="edge.y2"
            :class="{ 'dev-dependency': edge.isDevelopmentDependency }"
            stroke="#d0d0d0"
            stroke-width="1.5"
            :stroke-dasharray="edge.isDevelopmentDependency ? '5,5' : 'none'"
          />
          <text
            v-for="edge in positionedEdges"
            :key="`label-${edge.from}-${edge.to}`"
            :x="(edge.x1 + edge.x2) / 2"
            :y="(edge.y1 + edge.y2) / 2"
            text-anchor="middle"
            class="edge-label"
          >
            {{ edge.versionRange }}
          </text>
        </g>

        <!-- Nodes -->
        <g class="nodes">
          <g
            v-for="node in positionedNodes"
            :key="node.id"
            :transform="`translate(${node.x}, ${node.y})`"
            :class="{ 'root-node': node.isRoot, 'circular-node': node.isCircular }"
          >
            <circle
              :r="node.isRoot ? 35 : 30"
              :fill="getNodeColor(node)"
              :stroke="node.isCircular ? '#d03050' : node.isRoot ? '#18a058' : '#666'"
              :stroke-width="node.isCircular ? 3 : node.isRoot ? 3 : 2"
            />
            <text
              text-anchor="middle"
              dy="-5"
              class="node-label-package"
            >
              {{ node.packageId }}
            </text>
            <text
              text-anchor="middle"
              dy="10"
              class="node-label-version"
            >
              {{ node.version }}
            </text>
          </g>
        </g>
      </svg>

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
import { ref, onMounted, computed } from 'vue'
import {
  NCard,
  NSpin,
  NAlert,
  NText,
  NTag,
  NSpace,
  NIcon,
  NDivider,
  NList,
  NListItem
} from 'naive-ui'
import { WarningOutline } from '@vicons/ionicons5'
import type { DependencyGraphNode, DependencyGraphEdge, DependencyGraphResponse } from '../types/package'
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
const graphData = ref<DependencyGraphResponse | null>(null)
const svgWidth = ref(800)
const svgHeight = ref(600)

const totalNodes = computed(() => graphData.value?.totalNodes ?? 0)
const totalEdges = computed(() => graphData.value?.totalEdges ?? 0)
const hasCircularDependencies = computed(() => graphData.value?.hasCircularDependencies ?? false)
const circularPaths = computed(() => graphData.value?.circularPaths ?? [])

interface PositionedNode extends DependencyGraphNode {
  x: number
  y: number
}

interface PositionedEdge extends DependencyGraphEdge {
  x1: number
  y1: number
  x2: number
  y2: number
}

const positionedNodes = computed<PositionedNode[]>(() => {
  if (!graphData.value) return []

  const nodes = graphData.value.nodes
  const nodePositions = new Map<string, { x: number; y: number }>()

  // Group nodes by level
  const nodesByLevel = new Map<number, DependencyGraphNode[]>()
  nodes.forEach(node => {
    if (!nodesByLevel.has(node.level)) {
      nodesByLevel.set(node.level, [])
    }
    nodesByLevel.get(node.level)!.push(node)
  })

  // Calculate positions
  const maxLevel = Math.max(...nodes.map(n => n.level))
  const levelHeight = svgHeight.value / (maxLevel + 2)

  nodesByLevel.forEach((levelNodes, level) => {
    const nodeWidth = svgWidth.value / (levelNodes.length + 1)
    levelNodes.forEach((node, index) => {
      nodePositions.set(node.id, {
        x: nodeWidth * (index + 1),
        y: levelHeight * (level + 1)
      })
    })
  })

  return nodes.map(node => ({
    ...node,
    ...nodePositions.get(node.id)!
  }))
})

const positionedEdges = computed<PositionedEdge[]>(() => {
  if (!graphData.value) return []

  const nodePositionMap = new Map<string, { x: number; y: number }>()
  positionedNodes.value.forEach(node => {
    nodePositionMap.set(node.id, { x: node.x, y: node.y })
  })

  return graphData.value.edges.map(edge => {
    const fromPos = nodePositionMap.get(edge.from)
    const toPos = nodePositionMap.get(edge.to)

    return {
      ...edge,
      x1: fromPos?.x ?? 0,
      y1: fromPos?.y ?? 0,
      x2: toPos?.x ?? 0,
      y2: toPos?.y ?? 0
    }
  })
})

onMounted(async () => {
  await loadDependencyGraph()
})

async function loadDependencyGraph() {
  loading.value = true
  error.value = null

  try {
    const response = await apiClient.get<{ data: DependencyGraphResponse }>(
      `/v3/package/${props.packageId}/${props.version}/dependencies/graph`,
      { params: { maxDepth: props.maxDepth } }
    )

    graphData.value = response.data.data
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to load dependency graph'
  } finally {
    loading.value = false
  }
}

function getNodeColor(node: DependencyGraphNode): string {
  if (node.isCircular) return '#fce7ef'
  if (node.isRoot) return '#e8f5e9'
  return '#f5f5f5'
}
</script>

<style scoped>
.dependency-graph-card {
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

.graph-container {
  overflow-x: auto;
}

.graph-svg {
  border: 1px solid var(--n-border-color);
  border-radius: 4px;
  background: var(--n-color);
}

.node-label-package {
  font-size: 11px;
  font-weight: 600;
  fill: var(--n-text-color-1);
  pointer-events: none;
}

.node-label-version {
  font-size: 9px;
  fill: var(--n-text-color-3);
  pointer-events: none;
}

.edge-label {
  font-size: 10px;
  fill: var(--n-text-color-3);
  pointer-events: none;
}

.circular-warnings {
  margin-top: 16px;
}

.circular-warnings .n-list {
  max-height: 200px;
  overflow-y: auto;
}

.root-node circle {
  filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.1));
}

.circular-node circle {
  filter: drop-shadow(0 0 6px rgba(208, 48, 80, 0.3));
}
</style>
