<template>
  <n-card v-if="qualityScore" title="Package Quality" size="small" class="quality-card">
    <div class="quality-content">
      <div class="overall-score">
        <n-progress
          type="circle"
          :percentage="Math.round(qualityScore.qualityScore)"
          :color="getScoreColor(qualityScore.qualityScore)"
          :stroke-width="12"
          :show-indicator="false"
        >
          <div class="score-text">
            <div class="score-number">{{ Math.round(qualityScore.qualityScore) }}</div>
            <div class="score-label">Score</div>
          </div>
        </n-progress>
      </div>

      <n-divider />

      <div class="score-details">
        <div class="detail-item">
          <div class="detail-label">
            <n-icon><document-text-outline /></n-icon>
            <span>Completeness</span>
          </div>
          <n-progress
            :percentage="Math.round(qualityScore.completenessScore)"
            :color="getScoreColor(qualityScore.completenessScore)"
            :show-indicator="true"
            :height="8"
          />
        </div>

        <div class="detail-item">
          <div class="detail-label">
            <n-icon><checkmark-circle-outline /></n-icon>
            <span>Stability</span>
          </div>
          <n-progress
            :percentage="Math.round(qualityScore.stabilityScore)"
            :color="getScoreColor(qualityScore.stabilityScore)"
            :show-indicator="true"
            :height="8"
          />
        </div>

        <div class="detail-item">
          <div class="detail-label">
            <n-icon><construct-outline /></n-icon>
            <span>Maintenance</span>
          </div>
          <n-progress
            :percentage="Math.round(qualityScore.maintenanceScore)"
            :color="getScoreColor(qualityScore.maintenanceScore)"
            :show-indicator="true"
            :height="8"
          />
        </div>

        <div class="detail-item">
          <div class="detail-label">
            <n-icon><shield-checkmark-outline /></n-icon>
            <span>Security</span>
          </div>
          <n-progress
            :percentage="Math.round(qualityScore.securityScore)"
            :color="getScoreColor(qualityScore.securityScore)"
            :show-indicator="true"
            :height="8"
          />
        </div>

        <div class="detail-item">
          <div class="detail-label">
            <n-icon><people-outline /></n-icon>
            <span>Community</span>
          </div>
          <n-progress
            :percentage="Math.round(qualityScore.communityScore)"
            :color="getScoreColor(qualityScore.communityScore)"
            :show-indicator="true"
            :height="8"
          />
        </div>

        <div class="detail-item">
          <div class="detail-label">
            <n-icon><book-outline /></n-icon>
            <span>Documentation</span>
          </div>
          <n-progress
            :percentage="Math.round(qualityScore.documentationScore)"
            :color="getScoreColor(qualityScore.documentationScore)"
            :show-indicator="true"
            :height="8"
          />
        </div>
      </div>

      <n-text depth="3" class="last-calculated">
        Last calculated: {{ formatDate(qualityScore.lastCalculatedAt) }}
      </n-text>
    </div>
  </n-card>
</template>

<script setup lang="ts">
import { NCard, NProgress, NDivider, NIcon, NText } from 'naive-ui'
import {
  ShieldCheckmarkOutline,
  DocumentTextOutline,
  CheckmarkCircleOutline,
  ConstructOutline,
  PeopleOutline,
  BookOutline
} from '@vicons/ionicons5'
import type { PackageQualityScore } from '../types/package'

interface Props {
  qualityScore?: PackageQualityScore | null
}

defineProps<Props>()

const getScoreColor = (score: number) => {
  if (score >= 80) return '#18a058'
  if (score >= 60) return '#f0a020'
  return '#d03050'
}

const formatDate = (dateString: string) => {
  const date = new Date(dateString)
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  const days = Math.floor(diff / (1000 * 60 * 60 * 24))

  if (days === 0) return 'Today'
  if (days === 1) return 'Yesterday'
  if (days < 7) return `${days} days ago`
  if (days < 30) return `${Math.floor(days / 7)} weeks ago`
  return date.toLocaleDateString()
}
</script>

<style scoped>
.quality-card {
  height: 100%;
}

.quality-content {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.overall-score {
  display: flex;
  justify-content: center;
  padding: 16px 0;
}

.score-text {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.score-number {
  font-size: 32px;
  font-weight: 700;
  line-height: 1;
}

.score-label {
  font-size: 12px;
  color: var(--n-text-color-3);
  margin-top: 4px;
}

.score-details {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.detail-item {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.detail-label {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 500;
}

.last-calculated {
  font-size: 12px;
  text-align: center;
  margin-top: 8px;
}
</style>
