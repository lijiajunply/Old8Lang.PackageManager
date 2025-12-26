<template>
  <div class="quality-badge">
    <n-tooltip v-if="qualityScore" placement="top">
      <template #trigger>
        <n-tag
          :type="getScoreType(qualityScore.qualityScore)"
          :bordered="false"
          round
          size="small"
          class="quality-tag"
        >
          <template #icon>
            <n-icon>
              <shield-checkmark-outline v-if="qualityScore.qualityScore >= 80" />
              <shield-half-outline v-else-if="qualityScore.qualityScore >= 60" />
              <shield-outline v-else />
            </n-icon>
          </template>
          {{ Math.round(qualityScore.qualityScore) }}%
        </n-tag>
      </template>
      <div class="quality-tooltip">
        <div class="tooltip-title">Quality Score: {{ Math.round(qualityScore.qualityScore) }}%</div>
        <div class="score-breakdown">
          <div class="score-item">
            <span>Completeness:</span>
            <span class="score-value">{{ Math.round(qualityScore.completenessScore) }}%</span>
          </div>
          <div class="score-item">
            <span>Stability:</span>
            <span class="score-value">{{ Math.round(qualityScore.stabilityScore) }}%</span>
          </div>
          <div class="score-item">
            <span>Maintenance:</span>
            <span class="score-value">{{ Math.round(qualityScore.maintenanceScore) }}%</span>
          </div>
          <div class="score-item">
            <span>Security:</span>
            <span class="score-value">{{ Math.round(qualityScore.securityScore) }}%</span>
          </div>
          <div class="score-item">
            <span>Community:</span>
            <span class="score-value">{{ Math.round(qualityScore.communityScore) }}%</span>
          </div>
          <div class="score-item">
            <span>Documentation:</span>
            <span class="score-value">{{ Math.round(qualityScore.documentationScore) }}%</span>
          </div>
        </div>
      </div>
    </n-tooltip>
    <n-tag v-else :bordered="false" round size="small" type="info">
      No Score
    </n-tag>
  </div>
</template>

<script setup lang="ts">
import { NTag, NTooltip, NIcon } from 'naive-ui'
import {
  ShieldCheckmarkOutline,
  ShieldHalfOutline,
  ShieldOutline
} from '@vicons/ionicons5'
import type { PackageQualityScore } from '../types/package'

interface Props {
  qualityScore?: PackageQualityScore | null
}

defineProps<Props>()

const getScoreType = (score: number) => {
  if (score >= 80) return 'success'
  if (score >= 60) return 'warning'
  return 'error'
}
</script>

<style scoped>
.quality-badge {
  display: inline-flex;
  align-items: center;
}

.quality-tag {
  cursor: help;
  font-weight: 600;
}

.quality-tooltip {
  max-width: 250px;
}

.tooltip-title {
  font-weight: 600;
  font-size: 14px;
  margin-bottom: 8px;
  padding-bottom: 8px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.score-breakdown {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.score-item {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
  line-height: 1.5;
}

.score-value {
  font-weight: 600;
  margin-left: 12px;
}
</style>
