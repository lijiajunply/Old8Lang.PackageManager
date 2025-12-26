<template>
  <div class="min-h-screen bg-gray-50">
    <!-- 加载状态 -->
    <div v-if="packageStore.packageLoading" class="flex justify-center items-center py-20">
      <n-spin size="large" />
      <span class="ml-3 text-gray-600">加载包信息中...</span>
    </div>

    <!-- 包详情 -->
    <div v-else-if="packageStore.currentPackage" class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <!-- 面包屑导航 -->
      <nav class="text-sm text-gray-500 mb-6">
        <router-link to="/" class="hover:text-gray-700">首页</router-link>
        <span class="mx-2">/</span>
        <router-link to="/search" class="hover:text-gray-700">搜索</router-link>
        <span class="mx-2">/</span>
        <span class="text-gray-900">{{ packageStore.currentPackage.id }}</span>
      </nav>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <!-- 主要信息 -->
        <div class="lg:col-span-2 space-y-6">
          <!-- 包标题 -->
          <div class="bg-white rounded-lg shadow-sm p-6">
            <div class="flex items-start justify-between mb-4">
              <div>
                <h1 class="text-3xl font-bold text-gray-900 mb-2">
                  {{ packageStore.currentPackage.id }}
                </h1>
                <div class="flex items-center space-x-3">
                  <n-tag :type="getLanguageColor(packageStore.currentPackage.language)" size="medium">
                    {{ getLanguageLabel(packageStore.currentPackage.language) }}
                  </n-tag>
                  <span class="text-lg font-semibold text-gray-700">
                    {{ packageStore.currentPackage.version }}
                  </span>
                  <n-tag v-if="packageStore.currentPackage.isPrerelease" type="warning" size="small">
                    预发布版
                  </n-tag>
                </div>
              </div>
              
              <n-dropdown trigger="click">
                <n-button circle type="primary" size="large" class="btn-hover-lift">
                  <template #icon>
                    <n-icon size="20">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"></path>
                      </svg>
                    </n-icon>
                  </template>
                </n-button>
                <template #dropdown>
                  <n-menu>
                    <n-menu-item @click="downloadPackage()">
                      <template #icon>
                        <n-icon>
                          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"></path>
                          </svg>
                        </n-icon>
                      </template>
                      下载包
                    </n-menu-item>
                    <n-menu-item @click="copyInstallCommand()">
                      <template #icon>
                        <n-icon>
                          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"></path>
                          </svg>
                        </n-icon>
                      </template>
                      复制安装命令
                    </n-menu-item>
                  </n-menu>
                </template>
              </n-dropdown>
            </div>

            <p class="text-gray-600 text-lg leading-relaxed">
              {{ packageStore.currentPackage.description }}
            </p>

            <!-- 基本信息 -->
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mt-6">
              <div>
                <span class="text-sm text-gray-500">作者</span>
                <p class="font-medium text-gray-900">{{ packageStore.currentPackage.author }}</p>
              </div>
              <div>
                <span class="text-sm text-gray-500">许可证</span>
                <p class="font-medium text-gray-900">{{ packageStore.currentPackage.license }}</p>
              </div>
              <div>
                <span class="text-sm text-gray-500">包大小</span>
                <p class="font-medium text-gray-900">{{ formatFileSize(packageStore.currentPackage.size) }}</p>
              </div>
              <div>
                <span class="text-sm text-gray-500">发布时间</span>
                <p class="font-medium text-gray-900">{{ formatDate(packageStore.currentPackage.publishedAt) }}</p>
              </div>
            </div>

            <!-- 链接 -->
            <div v-if="packageStore.currentPackage.homepage || packageStore.currentPackage.repository" class="flex items-center space-x-4 mt-6">
              <a 
                v-if="packageStore.currentPackage.homepage"
                :href="packageStore.currentPackage.homepage" 
                target="_blank"
                class="text-blue-600 hover:text-blue-700 font-medium flex items-center"
              >
                <n-icon size="16" class="mr-1">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"></path>
                  </svg>
                </n-icon>
                主页
              </a>
              <a 
                v-if="packageStore.currentPackage.repository"
                :href="packageStore.currentPackage.repository.url" 
                target="_blank"
                class="text-blue-600 hover:text-blue-700 font-medium flex items-center"
              >
                <n-icon size="16" class="mr-1">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"></path>
                  </svg>
                </n-icon>
                源码
              </a>
            </div>
          </div>

          <!-- 关键词 -->
          <div v-if="packageStore.currentPackage.keywords.length > 0" class="bg-white rounded-lg shadow-sm p-6">
            <h3 class="text-lg font-semibold text-gray-900 mb-4">关键词</h3>
            <div class="flex flex-wrap gap-2">
              <n-tag 
                v-for="keyword in packageStore.currentPackage.keywords" 
                :key="keyword"
                type="info" 
                size="small"
                checkable
                @click="searchKeyword(keyword)"
              >
                {{ keyword }}
              </n-tag>
            </div>
          </div>

          <!-- 依赖关系 -->
          <div v-if="packageStore.currentPackage.dependencies.length > 0" class="bg-white rounded-lg shadow-sm p-6">
            <h3 class="text-lg font-semibold text-gray-900 mb-4">依赖关系</h3>
            <div class="space-y-3">
              <div 
                v-for="dep in packageStore.currentPackage.dependencies" 
                :key="`${dep.id}-${dep.version}`"
                class="flex items-center justify-between p-3 border border-gray-200 rounded-md"
              >
                <div class="flex items-center space-x-3">
                  <span class="font-medium text-gray-900">{{ dep.id }}</span>
                  <n-tag type="default" size="small">{{ dep.version }}</n-tag>
                  <n-tag v-if="dep.targetFramework" type="info" size="small">
                    {{ dep.targetFramework }}
                  </n-tag>
                </div>
                <n-tag v-if="dep.isDevelopmentDependency" type="warning" size="small">
                  开发依赖
                </n-tag>
              </div>
            </div>
          </div>

          <!-- 支持的框架 -->
          <div v-if="packageStore.currentPackage.frameworks" class="bg-white rounded-lg shadow-sm p-6">
            <h3 class="text-lg font-semibold text-gray-900 mb-4">支持的框架</h3>
            <div class="flex flex-wrap gap-2">
              <n-tag 
                v-for="(framework, name) in packageStore.currentPackage.frameworks" 
                :key="name"
                type="success" 
                size="small"
              >
                {{ name }}
              </n-tag>
            </div>
          </div>
        </div>

        <!-- 侧边栏 -->
        <div class="space-y-6">
          <!-- 下载统计 -->
          <div class="bg-white rounded-lg shadow-sm p-6">
            <h3 class="text-lg font-semibold text-gray-900 mb-4">下载统计</h3>
            <div class="text-center">
              <div class="text-3xl font-bold text-gray-900">
                {{ formatNumber(packageStore.currentPackage.downloadCount) }}
              </div>
              <p class="text-sm text-gray-500">总下载量</p>
            </div>
          </div>

          <!-- 版本信息 -->
          <div class="bg-white rounded-lg shadow-sm p-6">
            <h3 class="text-lg font-semibold text-gray-900 mb-4">包信息</h3>
            <div class="space-y-3">
              <div class="flex justify-between">
                <span class="text-sm text-gray-500">校验和</span>
                <n-tag type="default" size="small">SHA256</n-tag>
              </div>
              <div class="mt-2">
                <code class="text-xs bg-gray-100 text-gray-800 p-2 rounded block break-all">
                  {{ packageStore.currentPackage.checksum }}
                </code>
              </div>
            </div>
          </div>

          <!-- 质量评分 -->
          <PackageQualityDetails :quality-score="packageStore.currentPackage.qualityScore" />

          <!-- 相关包 -->
          <div class="bg-white rounded-lg shadow-sm p-6">
            <h3 class="text-lg font-semibold text-gray-900 mb-4">相关包</h3>
            <div v-if="relatedPackagesLoading" class="text-center py-4">
              <n-spin />
            </div>
            <div v-else-if="relatedPackages.length > 0" class="space-y-3">
              <div 
                v-for="pkg in relatedPackages" 
                :key="pkg.id"
                class="p-3 border border-gray-200 rounded-md hover:bg-gray-50 cursor-pointer transition-colors"
                @click="goToPackage(pkg.id)"
              >
                <div class="flex items-center justify-between mb-2">
                  <span class="font-medium text-gray-900 text-sm">{{ pkg.id }}</span>
                  <n-tag :type="getLanguageColor(pkg.language)" size="tiny">
                    {{ getLanguageLabel(pkg.language) }}
                  </n-tag>
                </div>
                <p class="text-xs text-gray-600 line-clamp-2">{{ pkg.description }}</p>
              </div>
            </div>
            <div v-else class="text-center py-4">
              <n-empty description="暂无相关包" size="small" />
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 未找到包 -->
    <div v-else class="flex justify-center items-center py-20">
      <n-result status="404" title="包未找到" description="请检查包名是否正确">
        <template #footer>
          <n-button @click="$router.push('/')">返回首页</n-button>
        </template>
      </n-result>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useMessage } from 'naive-ui'
import { usePackageStore } from '@/stores/package'
import PackageQualityDetails from '@/components/PackageQualityDetails.vue'
import type { Package } from '@/types/package'

const route = useRoute()
const router = useRouter()
const message = useMessage()
const packageStore = usePackageStore()

const relatedPackages = ref<Package[]>([])
const relatedPackagesLoading = ref(false)

// 下载包
const downloadPackage = async () => {
  if (!packageStore.currentPackage) return
  
  try {
    await packageStore.downloadPackage(
      packageStore.currentPackage.id,
      packageStore.currentPackage.version
    )
    message.success('包开始下载')
  } catch (error) {
    message.error('下载失败：' + error)
  }
}

// 复制安装命令
const copyInstallCommand = () => {
  if (!packageStore.currentPackage) return
  
  const command = `o8pm add ${packageStore.currentPackage.id}@${packageStore.currentPackage.version}`
  navigator.clipboard.writeText(command)
  message.success('安装命令已复制到剪贴板')
}

// 搜索关键词
const searchKeyword = (keyword: string) => {
  router.push({
    name: 'search',
    query: { q: keyword }
  })
}

// 跳转到包
const goToPackage = (packageId: string) => {
  router.push({
    name: 'package',
    params: { id: packageId }
  })
}

// 获取语言颜色
const getLanguageColor = (language: string) => {
  const colors = {
    old8lang: 'primary',
    python: 'success',
    javascript: 'warning',
    typescript: 'info'
  }
  return colors[language] || 'default'
}

// 获取语言标签
const getLanguageLabel = (language: string) => {
  const labels = {
    old8lang: 'Old8Lang',
    python: 'Python',
    javascript: 'JavaScript',
    typescript: 'TypeScript'
  }
  return labels[language] || language
}

// 格式化文件大小
const formatFileSize = (bytes: number) => {
  if (bytes === 0) return '0 Bytes'
  const k = 1024
  const sizes = ['Bytes', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

// 格式化数字
const formatNumber = (num: number) => {
  if (num >= 1000000) {
    return (num / 1000000).toFixed(1) + 'M'
  } else if (num >= 1000) {
    return (num / 1000).toFixed(1) + 'K'
  }
  return num.toString()
}

// 格式化日期
const formatDate = (dateString: string) => {
  const date = new Date(dateString)
  return date.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

// 加载相关包
const loadRelatedPackages = async () => {
  if (!packageStore.currentPackage) return
  
  relatedPackagesLoading.value = true
  try {
    // 使用包的关键词搜索相关包
    const keyword = packageStore.currentPackage.keywords[0]
    if (keyword) {
      const result = await packageStore.searchPackages(keyword)
      // 过滤掉当前包
      relatedPackages.value = result.packages
        .filter(pkg => pkg.id !== packageStore.currentPackage.id)
        .slice(0, 5)
    }
  } catch (error) {
    console.error('加载相关包失败:', error)
  } finally {
    relatedPackagesLoading.value = false
  }
}

// 监听路由参数变化
watch(
  () => [route.params.id, route.params.version],
  async ([id, version]) => {
    if (id) {
      await packageStore.getPackage(id as string, version as string)
      if (packageStore.currentPackage) {
        loadRelatedPackages()
      }
    }
  },
  { immediate: true }
)
</script>

<style scoped>
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>