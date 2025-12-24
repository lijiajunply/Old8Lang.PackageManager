<template>
  <div class="min-h-screen bg-gray-50 py-8">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- 页面标题 -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-gray-900">个人中心</h1>
        <p class="mt-2 text-gray-600">管理您的个人信息和包</p>
      </div>

      <!-- 加载状态 -->
      <div v-if="userStore.loading" class="flex justify-center py-12">
        <n-spin size="large" />
      </div>

      <!-- 用户信息 -->
      <template v-else-if="userStore.user">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <!-- 左侧用户信息卡片 -->
          <div class="lg:col-span-1">
            <div class="bg-white overflow-hidden shadow rounded-lg">
              <!-- 用户头像 -->
              <div class="p-6 text-center">
                <img 
                  :src="userStore.avatarUrl" 
                  :alt="userStore.displayName"
                  class="mx-auto h-32 w-32 rounded-full"
                />
                <h2 class="mt-4 text-xl font-semibold text-gray-900">
                  {{ userStore.displayName }}
                </h2>
                <p class="text-sm text-gray-500">@{{ userStore.user.username }}</p>
                <p class="text-sm text-gray-500">{{ userStore.user.email }}</p>
                
                <!-- 验证状态 -->
                <div class="mt-4 flex justify-center">
                  <n-tag v-if="userStore.user.isEmailVerified" type="success" size="small">
                    <template #icon>
                      <svg class="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
                      </svg>
                    </template>
                    已验证
                  </n-tag>
                  <n-tag v-else type="warning" size="small">
                    <template #icon>
                      <svg class="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
                      </svg>
                    </template>
                    未验证
                  </n-tag>
                </div>

                <!-- 管理员标识 -->
                <div v-if="userStore.isAdmin" class="mt-2">
                  <n-tag type="error" size="small">
                    <template #icon>
                      <svg class="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                        <path d="M9 2a1 1 0 000 2h2a1 1 0 100-2H9z" />
                        <path fill-rule="evenodd" d="M4 5a2 2 0 012-2 1 1 0 000 2H6a2 2 0 00-2 2v6a2 2 0 002 2h8a2 2 0 002-2V7a2 2 0 00-2-2h-2a1 1 0 100-2h2a4 4 0 014 4v6a4 4 0 01-4 4H6a4 4 0 01-4-4V7a4 4 0 014-4z" clip-rule="evenodd" />
                      </svg>
                    </template>
                    管理员
                  </n-tag>
                </div>
              </div>

              <!-- 用户详细信息 -->
              <div class="border-t border-gray-200 px-6 py-4">
                <dl class="space-y-3">
                  <div v-if="userStore.user.company">
                    <dt class="text-sm font-medium text-gray-500">公司</dt>
                    <dd class="text-sm text-gray-900">{{ userStore.user.company }}</dd>
                  </div>
                  <div v-if="userStore.user.location">
                    <dt class="text-sm font-medium text-gray-500">位置</dt>
                    <dd class="text-sm text-gray-900">{{ userStore.user.location }}</dd>
                  </div>
                  <div>
                    <dt class="text-sm font-medium text-gray-500">注册时间</dt>
                    <dd class="text-sm text-gray-900">{{ formatDate(userStore.user.createdAt) }}</dd>
                  </div>
                  <div>
                    <dt class="text-sm font-medium text-gray-500">最后登录</dt>
                    <dd class="text-sm text-gray-900">{{ formatDate(userStore.user.lastLoginAt) }}</dd>
                  </div>
                </dl>
              </div>

              <!-- 操作按钮 -->
              <div class="border-t border-gray-200 px-6 py-4">
                <div class="space-y-3">
                  <n-button block @click="handleLogout" type="error" secondary>
                    退出登录
                  </n-button>
                </div>
              </div>
            </div>

            <!-- 外部登录关联 -->
            <div v-if="userStore.user.externalLogins?.length > 0" class="mt-6 bg-white overflow-hidden shadow rounded-lg">
              <div class="px-6 py-4 border-b border-gray-200">
                <h3 class="text-lg font-medium text-gray-900">关联的账户</h3>
              </div>
              <div class="px-6 py-4">
                <div class="space-y-3">
                  <div 
                    v-for="login in userStore.user.externalLogins" 
                    :key="login.provider"
                    class="flex items-center justify-between p-3 bg-gray-50 rounded-md"
                  >
                    <div class="flex items-center">
                      <div class="flex-shrink-0">
                        <!-- GitHub 图标 -->
                        <svg v-if="login.provider === 'GitHub'" class="h-6 w-6 text-gray-600" viewBox="0 0 24 24">
                          <path fill="currentColor" d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
                        </svg>
                        <!-- Google 图标 -->
                        <svg v-else-if="login.provider === 'Google'" class="h-6 w-6 text-gray-600" viewBox="0 0 24 24">
                          <path fill="currentColor" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
                          <path fill="currentColor" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
                          <path fill="currentColor" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/>
                          <path fill="currentColor" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
                        </svg>
                        <!-- 默认图标 -->
                        <svg v-else class="h-6 w-6 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        </svg>
                      </div>
                      <div>
                        <p class="text-sm font-medium text-gray-900">{{ login.providerDisplayName }}</p>
                        <p class="text-xs text-gray-500">关联于 {{ formatDate(login.createdAt) }}</p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- 右侧内容区域 -->
          <div class="lg:col-span-2 space-y-6">
            <!-- 统计信息 -->
            <div class="bg-white shadow rounded-lg">
              <div class="px-6 py-4 border-b border-gray-200">
                <h3 class="text-lg font-medium text-gray-900">统计信息</h3>
              </div>
              <div class="p-6">
                <dl class="grid grid-cols-1 gap-5 sm:grid-cols-3">
                  <div class="text-center">
                    <dt class="text-2xl font-bold text-indigo-600">
                      {{ userStore.user.packageCount }}
                    </dt>
                    <dd class="mt-1 text-sm text-gray-500">已发布包</dd>
                  </div>
                  <div class="text-center">
                    <dt class="text-2xl font-bold text-green-600">
                      {{ formatNumber(userStore.user.totalDownloads) }}
                    </dt>
                    <dd class="mt-1 text-sm text-gray-500">总下载量</dd>
                  </div>
                  <div class="text-center">
                    <dt class="text-2xl font-bold text-yellow-600">
                      {{ formatBytes(userStore.user.usedStorage) }}
                    </dt>
                    <dd class="mt-1 text-sm text-gray-500">已用存储</dd>
                  </div>
                </dl>
              </div>
            </div>

            <!-- 我的包 -->
            <div class="bg-white shadow rounded-lg">
              <div class="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
                <h3 class="text-lg font-medium text-gray-900">我的包</h3>
                <n-button type="primary" @click="$router.push('/upload')">
                  上传新包
                </n-button>
              </div>
              <div class="p-6">
                <n-spin v-if="loadingPackages" />
                <div v-else-if="userPackages.length === 0" class="text-center py-8">
                  <n-empty description="您还没有发布任何包">
                    <template #extra>
                      <n-button @click="$router.push('/upload')" type="primary">
                        立即上传
                      </n-button>
                    </template>
                  </n-empty>
                </div>
                <div v-else class="space-y-4">
                  <div 
                    v-for="pkg in userPackages" 
                    :key="`${pkg.id}-${pkg.version}`"
                    class="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
                  >
                    <div class="flex justify-between items-start">
                      <div class="flex-1">
                        <h4 class="text-lg font-medium text-gray-900 hover:text-indigo-600 cursor-pointer">
                          {{ pkg.id }}
                        </h4>
                        <p class="text-sm text-gray-600">{{ pkg.description }}</p>
                        <div class="mt-2 flex items-center space-x-4 text-sm text-gray-500">
                          <span>版本: {{ pkg.version }}</span>
                          <span>语言: {{ pkg.language }}</span>
                          <span>下载: {{ formatNumber(pkg.downloadCount) }}</span>
                        </div>
                      </div>
                      <div class="flex space-x-2">
                        <n-button size="small" @click="viewPackage(pkg)">
                          查看
                        </n-button>
                        <n-button size="small" type="error" @click="deletePackage(pkg)">
                          删除
                        </n-button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </template>

      <!-- 未登录状态 -->
      <template v-else>
        <div class="text-center py-12">
          <n-empty description="请先登录">
            <template #extra>
              <n-button type="primary" @click="$router.push('/login')">
                立即登录
              </n-button>
            </template>
          </n-empty>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useMessage, useDialog } from 'naive-ui'
import type { Package } from '@/types/package'

const router = useRouter()
const userStore = useUserStore()
const message = useMessage()
const dialog = useDialog()

const userPackages = ref<Package[]>([])
const loadingPackages = ref(false)

onMounted(async () => {
  if (!userStore.isAuthenticated) {
    router.push('/login')
    return
  }
  
  await loadUserPackages()
})

const handleLogout = () => {
  dialog.warning({
    title: '确认退出',
    content: '您确定要退出登录吗？',
    positiveText: '确定',
    negativeText: '取消',
    onPositiveClick: async () => {
      await userStore.logout()
    }
  })
}

const loadUserPackages = async () => {
  if (!userStore.user) return
  
  loadingPackages.value = true
  try {
    // TODO: 实现获取用户包的 API
    // const response = await packageApi.getUserPackages(userStore.user.id)
    // userPackages.value = response.data || []
  } catch (error) {
    message.error('加载用户包失败')
  } finally {
    loadingPackages.value = false
  }
}

const viewPackage = (pkg: Package) => {
  router.push(`/package/${pkg.id}`)
}

const deletePackage = (pkg: Package) => {
  dialog.warning({
    title: '确认删除',
    content: `您确定要删除包 ${pkg.id} 吗？此操作不可恢复。`,
    positiveText: '确定删除',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        // TODO: 实现删除包的 API
        // await packageApi.deletePackage(pkg.id, pkg.version)
        message.success('包已删除')
        await loadUserPackages()
      } catch (error) {
        message.error('删除包失败')
      }
    }
  })
}

const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleString('zh-CN')
}

const formatNumber = (num: number) => {
  if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M'
  if (num >= 1000) return (num / 1000).toFixed(1) + 'K'
  return num.toString()
}

const formatBytes = (bytes: number) => {
  if (bytes >= 1073741824) return (bytes / 1073741824).toFixed(1) + 'GB'
  if (bytes >= 1048576) return (bytes / 1048576).toFixed(1) + 'MB'
  if (bytes >= 1024) return (bytes / 1024).toFixed(1) + 'KB'
  return bytes + 'B'
}
</script>