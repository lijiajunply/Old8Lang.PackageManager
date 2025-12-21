<template>
  <div class="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50">
    <!-- 头部导航 -->
    <header class="bg-white shadow-sm border-b border-gray-100">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between items-center h-16">
          <div class="flex items-center space-x-3">
            <div class="w-8 h-8 bg-gradient-to-br from-blue-600 to-purple-600 rounded-lg flex items-center justify-center">
              <span class="text-white font-bold text-sm">O8</span>
            </div>
            <h1 class="text-xl font-bold text-gray-900">Old8Lang Package Manager</h1>
          </div>
          
          <nav class="flex items-center space-x-6">
            <router-link 
              to="/search" 
              class="text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md text-sm font-medium transition-colors"
            >
              搜索
            </router-link>
            <router-link 
              to="/upload" 
              class="text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md text-sm font-medium transition-colors"
            >
              上传
            </router-link>
            <router-link 
              to="/docs" 
              class="text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md text-sm font-medium transition-colors"
            >
              文档
            </router-link>
          </nav>
        </div>
      </div>
    </header>

    <!-- 主要内容 -->
    <main>
      <!-- Hero 区域 -->
      <section class="py-20 px-4 sm:px-6 lg:px-8">
        <div class="max-w-7xl mx-auto text-center">
          <h2 class="text-4xl sm:text-5xl lg:text-6xl font-bold text-gray-900 mb-6">
            发现和管理
            <span class="text-transparent bg-clip-text bg-gradient-to-r from-blue-600 to-purple-600">
              Old8Lang 包
            </span>
          </h2>
          <p class="text-xl text-gray-600 mb-8 max-w-2xl mx-auto">
            现代化的包生态系统，为 Old8Lang 语言提供完整的包管理解决方案
          </p>
          
          <!-- 搜索框 -->
          <div class="max-w-2xl mx-auto mb-12">
            <div class="relative">
              <n-input
                v-model:value="searchQuery"
                size="large"
                placeholder="搜索包名、描述或关键词..."
                class="search-input"
                @keydown.enter="handleSearch"
              >
                <template #prefix>
                  <n-icon size="20" class="text-gray-400">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path>
                    </svg>
                  </n-icon>
                </template>
                <template #suffix>
                  <n-button 
                    type="primary" 
                    size="large"
                    :loading="searchLoading"
                    @click="handleSearch"
                  >
                    搜索
                  </n-button>
                </template>
              </n-input>
            </div>
          </div>

          <!-- 快速链接 -->
          <div class="flex flex-col sm:flex-row gap-4 justify-center items-center">
            <router-link 
              to="/search?language=old8lang"
              class="btn-hover-lift bg-white px-6 py-3 rounded-lg border border-gray-200 text-gray-700 hover:text-gray-900 hover:border-gray-300 transition-all shadow-sm"
            >
              <n-icon size="18" class="mr-2">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4"></path>
                </svg>
              </n-icon>
              Old8Lang 包
            </router-link>
            <router-link 
              to="/search?language=python"
              class="btn-hover-lift bg-white px-6 py-3 rounded-lg border border-gray-200 text-gray-700 hover:text-gray-900 hover:border-gray-300 transition-all shadow-sm"
            >
              <n-icon size="18" class="mr-2">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4"></path>
                </svg>
              </n-icon>
              Python 包
            </router-link>
          </div>
        </div>
      </section>

      <!-- 热门包 -->
      <section class="py-16 px-4 sm:px-6 lg:px-8 bg-white">
        <div class="max-w-7xl mx-auto">
          <div class="flex justify-between items-center mb-8">
            <h3 class="text-2xl font-bold text-gray-900">热门包</h3>
            <router-link 
              to="/search" 
              class="text-blue-600 hover:text-blue-700 font-medium"
            >
              查看更多 →
            </router-link>
          </div>
          
          <div v-if="popularLoading" class="text-center py-12">
            <n-spin size="large" />
          </div>
          
          <div v-else-if="popularPackages.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            <div 
              v-for="pkg in popularPackages" 
              :key="pkg.packageId"
              class="card-shadow bg-white rounded-lg p-6 hover:shadow-xl transition-all cursor-pointer"
              @click="goToPackage(pkg.packageId)"
            >
              <div class="flex items-start justify-between mb-3">
                <h4 class="text-lg font-semibold text-gray-900 truncate">{{ pkg.name }}</h4>
                <n-tag :type="getLanguageColor(pkg.language)" size="small">
                  {{ getLanguageLabel(pkg.language) }}
                </n-tag>
              </div>
              
              <p class="text-gray-600 text-sm mb-4 line-clamp-2">{{ pkg.description }}</p>
              
              <div class="flex items-center justify-between text-sm text-gray-500">
                <span>{{ pkg.latestVersion }}</span>
                <div class="flex items-center">
                  <n-icon size="14" class="mr-1">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"></path>
                    </svg>
                  </n-icon>
                  {{ formatNumber(pkg.downloadCount) }}
                </div>
              </div>
            </div>
          </div>
          
          <div v-else class="text-center py-12">
            <n-empty description="暂无热门包" />
          </div>
        </div>
      </section>

      <!-- 特性介绍 -->
      <section class="py-20 px-4 sm:px-6 lg:px-8">
        <div class="max-w-7xl mx-auto">
          <h3 class="text-3xl font-bold text-center text-gray-900 mb-12">为什么选择 Old8Lang Package Manager？</h3>
          
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
            <div class="text-center">
              <div class="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <n-icon size="24" class="text-blue-600">
                  <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path>
                  </svg>
                </n-icon>
              </div>
              <h4 class="text-xl font-semibold text-gray-900 mb-2">快速搜索</h4>
              <p class="text-gray-600">强大的搜索功能，支持按语言、关键词、版本等多维度筛选</p>
            </div>
            
            <div class="text-center">
              <div class="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <n-icon size="24" class="text-green-600">
                  <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"></path>
                  </svg>
                </n-icon>
              </div>
              <h4 class="text-xl font-semibold text-gray-900 mb-2">安全可靠</h4>
              <p class="text-gray-600">包完整性验证，依赖关系检查，确保包的安全性</p>
            </div>
            
            <div class="text-center">
              <div class="w-16 h-16 bg-purple-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <n-icon size="24" class="text-purple-600">
                  <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4"></path>
                  </svg>
                </n-icon>
              </div>
              <h4 class="text-xl font-semibold text-gray-900 mb-2">多源支持</h4>
              <p class="text-gray-600">支持本地和远程包源，灵活配置包来源</p>
            </div>
            
            <div class="text-center">
              <div class="w-16 h-16 bg-orange-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <n-icon size="24" class="text-orange-600">
                  <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                </n-icon>
              </div>
              <h4 class="text-xl font-semibold text-gray-900 mb-2">智能依赖</h4>
              <p class="text-gray-600">自动解析依赖关系，智能选择最佳版本</p>
            </div>
            
            <div class="text-center">
              <div class="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <n-icon size="24" class="text-red-600">
                  <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6"></path>
                  </svg>
                </n-icon>
              </div>
              <h4 class="text-xl font-semibold text-gray-900 mb-2">版本管理</h4>
              <p class="text-gray-600">语义化版本控制，支持预发布版和版本约束</p>
            </div>
            
            <div class="text-center">
              <div class="w-16 h-16 bg-indigo-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <n-icon size="24" class="text-indigo-600">
                  <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c-.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426-1.756-2.924-1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"></path>
                  </svg>
                </n-icon>
              </div>
              <h4 class="text-xl font-semibold text-gray-900 mb-2">兼容性</h4>
              <p class="text-gray-600">兼容 PyPI 和 NPM 接口，无缝迁移现有工具</p>
            </div>
          </div>
        </div>
      </section>
    </main>

    <!-- 页脚 -->
    <footer class="bg-gray-900 text-white py-12">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="grid grid-cols-1 md:grid-cols-4 gap-8">
          <div>
            <h5 class="text-lg font-semibold mb-4">Old8Lang Package Manager</h5>
            <p class="text-gray-400 text-sm">现代化的包管理解决方案，为 Old8Lang 语言提供完整的包生态。</p>
          </div>
          <div>
            <h5 class="text-lg font-semibold mb-4">快速链接</h5>
            <ul class="space-y-2 text-sm">
              <li><router-link to="/search" class="text-gray-400 hover:text-white">搜索包</router-link></li>
              <li><router-link to="/upload" class="text-gray-400 hover:text-white">上传包</router-link></li>
              <li><router-link to="/docs" class="text-gray-400 hover:text-white">文档</router-link></li>
            </ul>
          </div>
          <div>
            <h5 class="text-lg font-semibold mb-4">支持</h5>
            <ul class="space-y-2 text-sm">
              <li><a href="#" class="text-gray-400 hover:text-white">使用指南</a></li>
              <li><a href="#" class="text-gray-400 hover:text-white">常见问题</a></li>
              <li><a href="#" class="text-gray-400 hover:text-white">社区论坛</a></li>
            </ul>
          </div>
          <div>
            <h5 class="text-lg font-semibold mb-4">关于</h5>
            <ul class="space-y-2 text-sm">
              <li><a href="#" class="text-gray-400 hover:text-white">项目主页</a></li>
              <li><a href="#" class="text-gray-400 hover:text-white">GitHub</a></li>
              <li><a href="#" class="text-gray-400 hover:text-white">许可证</a></li>
            </ul>
          </div>
        </div>
        <div class="border-t border-gray-800 mt-8 pt-8 text-center text-sm text-gray-400">
          <p>&copy; 2024 Old8Lang Package Manager. All rights reserved.</p>
        </div>
      </div>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { usePackageStore } from '@/stores/package'

const router = useRouter()
const packageStore = usePackageStore()

const searchQuery = ref('')
const popularLoading = ref(false)
const popularPackages = ref([])

// 获取热门包
const loadPopularPackages = async () => {
  popularLoading.value = true
  try {
    await packageStore.getPopularPackages()
    popularPackages.value = packageStore.popularPackages
  } catch (error) {
    console.error('加载热门包失败:', error)
  } finally {
    popularLoading.value = false
  }
}

// 搜索处理
const handleSearch = () => {
  if (searchQuery.value.trim()) {
    router.push({
      name: 'search',
      query: { q: searchQuery.value.trim() }
    })
  }
}

// 跳转到包详情
const goToPackage = (packageId: string) => {
  router.push({
    name: 'package',
    params: { id: packageId }
  })
}

// 获取语言标签颜色
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

// 格式化数字
const formatNumber = (num: number) => {
  if (num >= 1000000) {
    return (num / 1000000).toFixed(1) + 'M'
  } else if (num >= 1000) {
    return (num / 1000).toFixed(1) + 'K'
  }
  return num.toString()
}

onMounted(() => {
  loadPopularPackages()
})
</script>

<style scoped>
.search-input {
  max-width: 100%;
}

.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>