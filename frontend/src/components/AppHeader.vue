<template>
  <header class="bg-white shadow-sm border-b border-gray-200">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <div class="flex justify-between items-center h-16">
        <!-- Logo 和导航 -->
        <div class="flex items-center">
          <!-- Logo -->
          <router-link to="/" class="flex items-center space-x-2">
            <div class="h-8 w-8 bg-indigo-600 rounded-lg flex items-center justify-center">
              <svg class="h-5 w-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
              </svg>
            </div>
            <span class="text-xl font-bold text-gray-900">Old8Lang PM</span>
          </router-link>

          <!-- 主导航 -->
          <nav class="hidden md:flex ml-10 space-x-8">
            <router-link
              v-for="item in navigation"
              :key="item.name"
              :to="item.to"
              class="text-gray-500 hover:text-gray-900 px-3 py-2 rounded-md text-sm font-medium transition-colors"
              :class="{ 'text-gray-900 bg-gray-100': isActiveRoute(item.to) }"
            >
              {{ item.name }}
            </router-link>
          </nav>
        </div>

        <!-- 右侧用户菜单 -->
        <div class="flex items-center space-x-4">
          <!-- 搜索按钮 -->
          <button
            @click="$emit('openSearch')"
            class="p-2 text-gray-500 hover:text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
          >
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
          </button>

          <!-- 主题切换 -->
          <button
            @click="toggleTheme"
            class="p-2 text-gray-500 hover:text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
          >
            <svg v-if="!isDark" class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
            </svg>
            <svg v-else class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" />
            </svg>
          </button>

          <!-- 用户菜单 -->
          <div v-if="userStore.isAuthenticated" class="relative">
            <!-- 用户头像和下拉菜单 -->
            <n-dropdown
              trigger="click"
              :options="userMenuOptions"
              @select="handleMenuSelect"
              placement="bottom-end"
            >
              <button class="flex items-center space-x-2 p-2 rounded-full hover:bg-gray-100 transition-colors">
                <img 
                  :src="userStore.avatarUrl" 
                  :alt="userStore.displayName"
                  class="h-8 w-8 rounded-full"
                />
                <svg class="h-4 w-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                </svg>
              </button>
            </n-dropdown>
          </div>

          <!-- 登录按钮 -->
          <n-button v-else type="primary" @click="$router.push('/login')">
            登录
          </n-button>

          <!-- 移动端菜单按钮 -->
          <button
            @click="toggleMobileMenu"
            class="md:hidden p-2 text-gray-500 hover:text-gray-700 rounded-md hover:bg-gray-100"
          >
            <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
        </div>
      </div>

      <!-- 移动端导航 -->
      <div v-if="mobileMenuOpen" class="md:hidden">
        <div class="px-2 pt-2 pb-3 space-y-1 border-t border-gray-200 mt-2">
          <router-link
            v-for="item in navigation"
            :key="item.name"
            :to="item.to"
            class="block text-gray-500 hover:text-gray-900 hover:bg-gray-50 px-3 py-2 rounded-md text-base font-medium"
            @click="closeMobileMenu"
          >
            {{ item.name }}
          </router-link>
        </div>
      </div>
    </div>
  </header>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useMessage, useDialog } from 'naive-ui'

defineEmits<{
  openSearch: []
}>()

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const message = useMessage()
const dialog = useDialog()

const mobileMenuOpen = ref(false)
const isDark = ref(false)

// 导航菜单
const navigation = ref([
  { name: '首页', to: '/' },
  { name: '搜索', to: '/search' },
  { name: '上传', to: '/upload' },
  { name: '文档', to: '/docs' }
])

// 用户下拉菜单选项
const userMenuOptions = computed(() => [
  {
    label: '个人中心',
    key: 'profile',
    icon: () => h('svg', { class: 'h-4 w-4', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
      h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z' })
    ])
  },
  {
    label: '我的包',
    key: 'packages',
    icon: () => h('svg', { class: 'h-4 w-4', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
      h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4' })
    ])
  },
  { type: 'divider' },
  {
    label: '设置',
    key: 'settings',
    icon: () => h('svg', { class: 'h-4 w-4', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
      h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z' })
    ])
  },
  { type: 'divider' },
  {
    label: '退出登录',
    key: 'logout',
    icon: () => h('svg', { class: 'h-4 w-4', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
      h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1' })
    ])
  }
])

// 检查路由是否激活
const isActiveRoute = (path: string) => {
  return route.path === path
}

// 切换移动端菜单
const toggleMobileMenu = () => {
  mobileMenuOpen.value = !mobileMenuOpen.value
}

// 关闭移动端菜单
const closeMobileMenu = () => {
  mobileMenuOpen.value = false
}

// 切换主题
const toggleTheme = () => {
  isDark.value = !isDark.value
  // 这里可以触发全局主题切换
  const event = new CustomEvent('theme-toggle', { detail: { dark: isDark.value } })
  window.dispatchEvent(event)
}

// 处理用户菜单选择
const handleMenuSelect = (key: string) => {
  switch (key) {
    case 'profile':
      router.push('/profile')
      break
    case 'packages':
      router.push('/profile?tab=packages')
      break
    case 'settings':
      // TODO: 实现设置页面
      message.info('设置功能正在开发中')
      break
    case 'logout':
      dialog.warning({
        title: '确认退出',
        content: '您确定要退出登录吗？',
        positiveText: '确定',
        negativeText: '取消',
        onPositiveClick: async () => {
          await userStore.logout()
        }
      })
      break
  }
}

// 初始化主题状态
const initTheme = () => {
  const savedTheme = localStorage.getItem('theme')
  isDark.value = savedTheme === 'dark' || (!savedTheme && window.matchMedia('(prefers-color-scheme: dark)').matches)
}

initTheme()
</script>

<style scoped>
/* 添加一些过渡效果 */
.transition-colors {
  transition: all 0.2s ease-in-out;
}

/* 激活的导航项样式 */
.router-link-active {
  color: #1f2937 !important;
  background-color: #f3f4f6;
}
</style>