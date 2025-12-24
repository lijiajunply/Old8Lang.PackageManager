<template>
  <n-config-provider :theme="theme" :locale="locale">
    <n-notification-provider>
      <n-message-provider>
        <div class="min-h-screen bg-gray-50">
          <AppHeader @open-search="handleOpenSearch" />
          <main class="flex-1">
            <router-view />
          </main>
          <!-- 搜索模态框 -->
          <n-modal
            v-model:show="searchModalVisible"
            :mask-closable="true"
            preset="card"
            title="搜索包"
            style="width: 600px"
          >
            <div class="space-y-4">
              <n-input
                v-model:value="searchQuery"
                placeholder="输入包名或关键词..."
                size="large"
                @keyup.enter="handleSearch"
              >
                <template #prefix>
                  <svg class="h-5 w-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </template>
              </n-input>
              
              <div class="flex justify-end space-x-2">
                <n-button @click="searchModalVisible = false">
                  取消
                </n-button>
                <n-button type="primary" @click="handleSearch" :disabled="!searchQuery.trim()">
                  搜索
                </n-button>
              </div>
            </div>
          </n-modal>
        </div>
      </n-message-provider>
    </n-notification-provider>
  </n-config-provider>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { darkTheme, zhCN, enUS } from 'naive-ui'
import dayjs from 'dayjs'
import 'dayjs/locale/zh-cn'
import 'dayjs/locale/en'
import { useUserStore } from '@/stores/user'
import AppHeader from '@/components/AppHeader.vue'

const route = useRoute()

// 主题设置
const isDark = ref(false)
const theme = computed(() => isDark.value ? darkTheme : null)

// 语言设置
const locale = ref(zhCN)

// 搜索相关
const searchModalVisible = ref(false)
const searchQuery = ref('')

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

// 处理搜索
const handleOpenSearch = () => {
  searchModalVisible.value = true
  searchQuery.value = ''
}

const handleSearch = () => {
  if (searchQuery.value.trim()) {
    searchModalVisible.value = false
    router.push({
      name: 'search',
      query: { q: searchQuery.value.trim() }
    })
  }
}

// 初始化
onMounted(async () => {
  checkThemePreference()
  checkLanguagePreference()
  updateDayjsLocale()
  
  // 初始化用户认证状态
  await userStore.initAuth()
})

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