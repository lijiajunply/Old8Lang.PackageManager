<template>
  <n-config-provider :theme="theme" :locale="locale">
    <n-notification-provider>
      <n-message-provider>
        <router-view />
      </n-message-provider>
    </n-notification-provider>
  </n-config-provider>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import { darkTheme, zhCN, enUS } from 'naive-ui'
import dayjs from 'dayjs'
import 'dayjs/locale/zh-cn'
import 'dayjs/locale/en'

const route = useRoute()

// 主题设置
const isDark = ref(false)
const theme = computed(() => isDark.value ? darkTheme : null)

// 语言设置
const locale = ref(zhCN)

// 检查系统主题偏好
const checkThemePreference = () => {
  if (localStorage.getItem('theme')) {
    isDark.value = localStorage.getItem('theme') === 'dark'
  } else if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
    isDark.value = true
  }
}

// 检查语言偏好
const checkLanguagePreference = () => {
  const savedLocale = localStorage.getItem('locale')
  if (savedLocale) {
    locale.value = savedLocale === 'en' ? enUS : zhCN
  } else if (navigator.language.startsWith('en')) {
    locale.value = enUS
  }
}

// 设置 dayjs 语言
const updateDayjsLocale = () => {
  const dayjsLocale = locale.value === zhCN ? 'zh-cn' : 'en'
  dayjs.locale(dayjsLocale)
}

// 监听主题变化
watch(isDark, (newValue) => {
  localStorage.setItem('theme', newValue ? 'dark' : 'light')
  if (newValue) {
    document.documentElement.classList.add('dark')
  } else {
    document.documentElement.classList.remove('dark')
  }
})

// 监听语言变化
watch(locale, (newValue) => {
  localStorage.setItem('locale', newValue === zhCN ? 'zh' : 'en')
  updateDayjsLocale()
})

// 监听路由变化，更新页面标题
watch(
  () => route.meta,
  (meta) => {
    if (meta?.title) {
      document.title = `${meta.title} - Old8Lang Package Manager`
    }
    if (meta?.description) {
      const metaDescription = document.querySelector('meta[name="description"]')
      if (metaDescription) {
        metaDescription.setAttribute('content', meta.description)
      }
    }
  },
  { immediate: true }
)

// 初始化
checkThemePreference()
checkLanguagePreference()
updateDayjsLocale()

// 监听系统主题变化
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
  if (!localStorage.getItem('theme')) {
    isDark.value = e.matches
  }
})
</script>

<style>
#app {
  min-height: 100vh;
}

/* 深色模式样式 */
.dark {
  color-scheme: dark;
}
</style>