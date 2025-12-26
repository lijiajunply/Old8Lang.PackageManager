<template>
  <n-dropdown :options="languageOptions" @select="handleLanguageChange">
    <n-button text>
      <n-icon size="20">
        <component :is="LanguageIcon" />
      </n-icon>
      <span class="ml-2">{{ currentLanguageLabel }}</span>
    </n-button>
  </n-dropdown>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { NDropdown, NButton, NIcon } from 'naive-ui'
import { Language as LanguageIcon } from '@vicons/ionicons5'
import { setStoredLanguage } from '../i18n'

const { locale } = useI18n()

interface LanguageOption {
  label: string
  key: string
}

const languageOptions = computed<LanguageOption[]>(() => [
  {
    label: 'English',
    key: 'en',
  },
  {
    label: '简体中文',
    key: 'zh-CN',
  },
])

const currentLanguageLabel = computed(() => {
  const option = languageOptions.value.find(opt => opt.key === locale.value)
  return option?.label || 'English'
})

const handleLanguageChange = (key: string) => {
  locale.value = key
  setStoredLanguage(key)

  // 重新加载页面以确保所有组件都使用新语言
  window.location.reload()
}
</script>

<style scoped>
.ml-2 {
  margin-left: 0.5rem;
}
</style>
