<template>
  <div class="min-h-screen bg-gray-50">
    <!-- 搜索头部 -->
    <div class="bg-white shadow-sm border-b border-gray-200 sticky top-0 z-10">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
        <div class="flex flex-col space-y-4 lg:flex-row lg:space-y-0 lg:space-x-4">
          <!-- 搜索框 -->
          <div class="flex-1">
            <n-input
              v-model:value="packageStore.searchQuery"
              size="large"
              placeholder="搜索包..."
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
                  :loading="packageStore.searchLoading"
                  @click="handleSearch"
                >
                  搜索
                </n-button>
              </template>
            </n-input>
          </div>
          
          <!-- 筛选选项 -->
          <div class="flex items-center space-x-4">
            <!-- 语言筛选 -->
            <n-select
              v-model:value="packageStore.selectedLanguage"
              placeholder="选择语言"
              clearable
              style="width: 120px"
              @update:value="handleLanguageChange"
            >
              <n-option value="old8lang" label="Old8Lang" />
              <n-option value="python" label="Python" />
              <n-option value="javascript" label="JavaScript" />
              <n-option value="typescript" label="TypeScript" />
            </n-select>
            
            <!-- 排序方式 -->
            <n-select
              v-model:value="packageStore.sortBy"
              style="width: 120px"
              @update:value="handleSortChange"
            >
              <n-option value="relevance" label="相关性" />
              <n-option value="name" label="名称" />
              <n-option value="created" label="创建时间" />
              <n-option value="updated" label="更新时间" />
              <n-option value="downloads" label="下载量" />
            </n-select>
            
            <!-- 排序顺序 -->
            <n-select
              v-model:value="packageStore.sortOrder"
              style="width: 100px"
              @update:value="handleSortOrderChange"
            >
              <n-option value="desc" label="降序" />
              <n-option value="asc" label="升序" />
            </n-select>
          </div>
        </div>
      </div>
    </div>

    <!-- 主要内容 -->
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <!-- 搜索结果头部 -->
      <div v-if="packageStore.searchQuery" class="mb-6">
        <div class="flex items-center justify-between">
          <div class="flex items-center space-x-4">
            <h2 class="text-2xl font-bold text-gray-900">搜索结果</h2>
            <n-tag v-if="packageStore.selectedLanguage" :type="getLanguageColor(packageStore.selectedLanguage)" size="small">
              {{ getLanguageLabel(packageStore.selectedLanguage) }}
            </n-tag>
            <span class="text-gray-600">
              找到 {{ packageStore.searchResult.totalCount }} 个包
            </span>
          </div>
          
          <!-- 搜索历史 -->
          <n-dropdown v-if="packageStore.searchHistory.length > 0" trigger="click">
            <n-button text>
              <template #icon>
                <n-icon>
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                </n-icon>
              </template>
              搜索历史
            </n-button>
            <template #dropdown>
              <n-menu>
                <n-menu-item 
                  v-for="item in packageStore.searchHistory" 
                  :key="item"
                  @click="selectHistoryItem(item)"
                >
                  {{ item }}
                </n-menu-item>
              </n-menu>
            </template>
          </n-dropdown>
        </div>
      </div>

      <!-- 加载状态 -->
      <div v-if="packageStore.searchLoading" class="flex justify-center items-center py-12">
        <n-spin size="large" />
        <span class="ml-3 text-gray-600">搜索中...</span>
      </div>

      <!-- 搜索结果 -->
      <div v-else-if="packageStore.hasSearchResults" class="space-y-6">
        <!-- 包列表 -->
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <div 
            v-for="pkg in packageStore.searchResult.packages" 
            :key="`${pkg.id}-${pkg.version}`"
            class="card-shadow bg-white rounded-lg p-6 hover:shadow-xl transition-all cursor-pointer"
            @click="goToPackage(pkg.id, pkg.version)"
          >
            <div class="flex items-start justify-between mb-3">
              <div>
                <h3 class="text-lg font-semibold text-gray-900 truncate mb-1">{{ pkg.id }}</h3>
                <span class="text-sm text-gray-500">{{ pkg.version }}</span>
              </div>
              <n-tag :type="getLanguageColor(pkg.language)" size="small">
                {{ getLanguageLabel(pkg.language) }}
              </n-tag>
            </div>
            
            <p class="text-gray-600 text-sm mb-4 line-clamp-2">{{ pkg.description }}</p>
            
            <div class="flex items-center justify-between text-sm text-gray-500">
              <span class="truncate">{{ pkg.author }}</span>
              <div class="flex items-center space-x-3">
                <div class="flex items-center">
                  <n-icon size="14" class="mr-1">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"></path>
                    </svg>
                  </n-icon>
                  {{ formatNumber(pkg.downloadCount) }}
                </div>
                <span>{{ formatDate(pkg.publishedAt) }}</span>
              </div>
            </div>
            
            <!-- 关键词标签 -->
            <div v-if="pkg.keywords.length > 0" class="mt-3">
              <div class="flex flex-wrap gap-1">
                <n-tag 
                  v-for="keyword in pkg.keywords.slice(0, 3)" 
                  :key="keyword"
                  type="info" 
                  size="tiny"
                >
                  {{ keyword }}
                </n-tag>
                <n-tag 
                  v-if="pkg.keywords.length > 3"
                  type="default" 
                  size="tiny"
                >
                  +{{ pkg.keywords.length - 3 }}
                </n-tag>
              </div>
            </div>
          </div>
        </div>

        <!-- 分页 -->
        <div class="flex justify-center mt-8">
          <n-pagination
            v-model:page="packageStore.currentPage"
            :page-count="packageStore.searchResult.totalPages"
            :page-size="packageStore.pageSize"
            show-size-picker
            :page-sizes="[10, 20, 50, 100]"
            @update:page="handlePageChange"
            @update:page-size="handlePageSizeChange"
          />
        </div>
      </div>

      <!-- 无结果 -->
      <div v-else-if="packageStore.searchQuery" class="text-center py-12">
        <n-empty 
          description="没有找到匹配的包"
          size="large"
        >
          <template #extra>
            <div class="space-y-3">
              <p class="text-gray-600">尝试：</p>
              <ul class="text-left text-gray-600 space-y-1">
                <li>• 检查拼写是否正确</li>
                <li>• 使用更通用的关键词</li>
                <li>• 尝试不同的语言筛选</li>
                <li>• 减少筛选条件</li>
              </ul>
              <n-button @click="packageStore.resetSearch">重新搜索</n-button>
            </div>
          </template>
        </n-empty>
      </div>

      <!-- 初始状态 -->
      <div v-else class="text-center py-12">
        <n-empty description="输入关键词开始搜索" size="large">
          <template #extra>
            <div class="space-y-4">
              <h3 class="text-lg font-semibold text-gray-900">热门搜索</h3>
              <div class="flex flex-wrap justify-center gap-2">
                <n-tag 
                  v-for="tag in popularTags" 
                  :key="tag"
                  checkable
                  @click="searchTag(tag)"
                >
                  {{ tag }}
                </n-tag>
              </div>
            </div>
          </template>
        </n-empty>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { usePackageStore } from '@/stores/package'

const router = useRouter()
const route = useRoute()
const packageStore = usePackageStore()

const popularTags = [
  'utils', 'tools', 'http', 'async', 'logger', 'validation', 
  'database', 'testing', 'web', 'api', 'cli', 'ui'
]

// 处理搜索
const handleSearch = () => {
  if (packageStore.searchQuery.trim()) {
    packageStore.searchPackages()
  }
}

// 语言筛选变化
const handleLanguageChange = () => {
  packageStore.updateSearchParams({ language: packageStore.selectedLanguage })
}

// 排序方式变化
const handleSortChange = () => {
  packageStore.updateSearchParams({ sortBy: packageStore.sortBy })
}

// 排序顺序变化
const handleSortOrderChange = () => {
  packageStore.updateSearchParams({ sortOrder: packageStore.sortOrder })
}

// 页面变化
const handlePageChange = (page: number) => {
  packageStore.goToPage(page)
}

// 页面大小变化
const handlePageSizeChange = (pageSize: number) => {
  packageStore.updateSearchParams({ pageSize })
}

// 选择历史记录项
const selectHistoryItem = (item: string) => {
  packageStore.searchQuery = item
  handleSearch()
}

// 搜索标签
const searchTag = (tag: string) => {
  packageStore.searchQuery = tag
  handleSearch()
}

// 跳转到包详情
const goToPackage = (packageId: string, version: string) => {
  router.push({
    name: 'package',
    params: { id: packageId, version }
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
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  const days = Math.floor(diff / (1000 * 60 * 60 * 24))
  
  if (days === 0) {
    return '今天'
  } else if (days === 1) {
    return '昨天'
  } else if (days < 7) {
    return `${days}天前`
  } else if (days < 30) {
    return `${Math.floor(days / 7)}周前`
  } else if (days < 365) {
    return `${Math.floor(days / 30)}个月前`
  } else {
    return `${Math.floor(days / 365)}年前`
  }
}

// 初始化搜索参数
onMounted(() => {
  const { q, language, sortBy, sortOrder } = route.query as any
  
  if (q) {
    packageStore.searchQuery = q
  }
  if (language) {
    packageStore.selectedLanguage = language
  }
  if (sortBy) {
    packageStore.sortBy = sortBy
  }
  if (sortOrder) {
    packageStore.sortOrder = sortOrder
  }
  
  // 如果有搜索查询，立即搜索
  if (q) {
    packageStore.searchPackages()
  }
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