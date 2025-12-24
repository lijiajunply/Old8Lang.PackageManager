<template>
  <div class="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center px-4">
    <div class="max-w-md w-full space-y-8">
      <!-- 头部 -->
      <div class="text-center">
        <div class="mx-auto h-16 w-16 bg-indigo-600 rounded-full flex items-center justify-center">
          <svg class="h-8 w-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
          </svg>
        </div>
        <h2 class="mt-6 text-3xl font-extrabold text-gray-900">
          欢迎来到 Old8Lang Package Manager
        </h2>
        <p class="mt-2 text-sm text-gray-600">
          使用您喜爱的账户登录，开始管理您的包
        </p>
      </div>

      <!-- 登录卡片 -->
      <div class="bg-white py-8 px-6 shadow-xl rounded-lg">
        <!-- OAuth 登录按钮 -->
        <div class="space-y-4">
          <h3 class="text-sm font-medium text-gray-700 text-center">
            选择登录方式
          </h3>
          
          <!-- 加载状态 -->
          <div v-if="userStore.loading" class="flex justify-center py-8">
            <n-spin size="large" />
          </div>

          <!-- 认证提供商按钮 -->
          <template v-else-if="userStore.authProviders.length > 0">
            <div class="space-y-3">
              <button
                v-for="provider in userStore.authProviders"
                :key="provider.name"
                @click="handleLogin(provider.name)"
                class="w-full flex items-center justify-center px-4 py-3 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 transition-colors"
                :disabled="loading"
              >
                <!-- GitHub 图标 -->
                <svg v-if="provider.name === 'GitHub'" class="h-5 w-5 mr-2" viewBox="0 0 24 24">
                  <path fill="currentColor" d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
                </svg>

                <!-- Google 图标 -->
                <svg v-else-if="provider.name === 'Google'" class="h-5 w-5 mr-2" viewBox="0 0 24 24">
                  <path fill="currentColor" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
                  <path fill="currentColor" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
                  <path fill="currentColor" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/>
                  <path fill="currentColor" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
                </svg>

                <!-- 自定义提供商图标 -->
                <svg v-else class="h-5 w-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>

                {{ provider.displayName }}
              </button>
            </div>
          </template>

          <!-- 无可用提供商时显示 -->
          <div v-else class="text-center py-8">
            <n-empty description="暂无可用的登录方式">
              <template #extra>
                <n-button @click="loadProviders">重新加载</n-button>
              </template>
            </n-empty>
          </div>
        </div>

        <!-- 分隔线 -->
        <div class="mt-6">
          <div class="relative">
            <div class="absolute inset-0 flex items-center">
              <div class="w-full border-t border-gray-300" />
            </div>
            <div class="relative flex justify-center text-sm">
              <span class="px-2 bg-white text-gray-500">或</span>
            </div>
          </div>
        </div>

        <!-- 功能介绍 -->
        <div class="mt-6">
          <h4 class="text-sm font-medium text-gray-900 mb-3">为什么选择 Old8Lang Package Manager？</h4>
          <ul class="space-y-2 text-sm text-gray-600">
            <li class="flex items-center">
              <svg class="h-4 w-4 text-green-500 mr-2" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
              </svg>
              多语言包支持（Old8Lang、Python、JavaScript）
            </li>
            <li class="flex items-center">
              <svg class="h-4 w-4 text-green-500 mr-2" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
              </svg>
              安全可靠的企业级认证
            </li>
            <li class="flex items-center">
              <svg class="h-4 w-4 text-green-500 mr-2" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
              </svg>
              智能依赖解析和版本管理
            </li>
          </ul>
        </div>
      </div>

      <!-- 页脚链接 -->
      <div class="text-center text-sm text-gray-600">
        <router-link to="/" class="text-indigo-600 hover:text-indigo-500">
          返回首页
        </router-link>
      </div>
    </div>

    <!-- 登录中遮罩 -->
    <div v-if="loading" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div class="bg-white rounded-lg p-6 shadow-xl">
        <n-spin size="large" />
        <p class="mt-4 text-gray-600">正在跳转到 {{ currentProvider }}...</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useMessage, useLoadingBar } from 'naive-ui'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const message = useMessage()
const loadingBar = useLoadingBar()

const loading = ref(false)
const currentProvider = ref('')

// 如果已经登录，重定向到首页
onMounted(async () => {
  await userStore.initAuth()
  if (userStore.isAuthenticated) {
    router.push('/')
    return
  }

  // 加载认证提供商
  await loadProviders()
})

const loadProviders = async () => {
  try {
    await userStore.loadAuthProviders()
  } catch (error) {
    message.error('加载登录方式失败，请刷新页面重试')
  }
}

const handleLogin = async (provider: string) => {
  loading.value = true
  currentProvider.value = provider
  loadingBar.start()

  try {
    // 获取返回地址
    const returnUrl = (route.query.returnUrl as string) || '/'
    await userStore.loginWithProvider(provider, returnUrl)
  } catch (error) {
    message.error('登录失败，请重试')
    loading.value = false
    loadingBar.error()
  }
}
</script>

<style scoped>
/* 添加一些动画效果 */
.transition-colors {
  transition: all 0.2s ease-in-out;
}

.transition-colors:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
}

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}
</style>