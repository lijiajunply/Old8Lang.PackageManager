import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Package, PackageSearchRequest, SearchResult, PopularPackage } from '@/types/package'
import { PackageApi } from '@/api/package'

export const usePackageStore = defineStore('package', () => {
  // 状态
  const searchResult = ref<SearchResult>({
    packages: [],
    totalCount: 0,
    currentPage: 1,
    pageSize: 20,
    totalPages: 0,
  })
  
  const popularPackages = ref<PopularPackage[]>([])
  const currentPackage = ref<Package | null>(null)
  const searchLoading = ref(false)
  const popularLoading = ref(false)
  const packageLoading = ref(false)
  const searchHistory = ref<string[]>([])
  
  // 计算属性
  const hasSearchResults = computed(() => searchResult.value.packages.length > 0)
  const searchQuery = ref('')
  const selectedLanguage = ref<string | null>(null)
  const sortBy = ref<'relevance' | 'name' | 'created' | 'updated' | 'downloads'>('relevance')
  const sortOrder = ref<'asc' | 'desc'>('desc')
  const currentPage = ref(1)
  const pageSize = ref(20)
  
  // 搜索包
  const searchPackages = async (query?: string) => {
    if (query !== undefined) {
      searchQuery.value = query
    }
    
    if (query?.trim()) {
      // 添加到搜索历史
      if (!searchHistory.value.includes(query.trim())) {
        searchHistory.value.unshift(query.trim())
        if (searchHistory.value.length > 10) {
          searchHistory.value = searchHistory.value.slice(0, 10)
        }
      }
    }
    
    searchLoading.value = true
    try {
      const params: PackageSearchRequest = {
        q: searchQuery.value || undefined,
        language: selectedLanguage.value || undefined,
        skip: (currentPage.value - 1) * pageSize.value,
        take: pageSize.value,
        sortBy: sortBy.value,
        sortOrder: sortOrder.value,
      }
      
      const result = await PackageApi.searchPackages(params)
      searchResult.value = result
    } catch (error) {
      console.error('搜索包失败:', error)
      throw error
    } finally {
      searchLoading.value = false
    }
  }
  
  // 获取热门包
  const getPopularPackages = async (language?: string) => {
    popularLoading.value = true
    try {
      const packages = await PackageApi.getPopularPackages(language, 50)
      popularPackages.value = packages
    } catch (error) {
      console.error('获取热门包失败:', error)
      throw error
    } finally {
      popularLoading.value = false
    }
  }
  
  // 获取包详情
  const getPackage = async (packageId: string, version?: string) => {
    packageLoading.value = true
    try {
      const pkg = await PackageApi.getPackage(packageId, version)
      currentPackage.value = pkg
      return pkg
    } catch (error) {
      console.error('获取包详情失败:', error)
      throw error
    } finally {
      packageLoading.value = false
    }
  }
  
  // 下载包
  const downloadPackage = async (packageId: string, version: string) => {
    try {
      const blob = await PackageApi.downloadPackage(packageId, version)
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `${packageId}-${version}.o8pkg`
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      window.URL.revokeObjectURL(url)
    } catch (error) {
      console.error('下载包失败:', error)
      throw error
    }
  }
  
  // 重置搜索
  const resetSearch = () => {
    searchQuery.value = ''
    searchResult.value = {
      packages: [],
      totalCount: 0,
      currentPage: 1,
      pageSize: 20,
      totalPages: 0,
    }
    currentPage.value = 1
  }
  
  // 更新搜索参数
  const updateSearchParams = (params: {
    language?: string | null
    sortBy?: typeof sortBy.value
    sortOrder?: typeof sortOrder.value
    pageSize?: number
  }) => {
    if (params.language !== undefined) selectedLanguage.value = params.language
    if (params.sortBy !== undefined) sortBy.value = params.sortBy
    if (params.sortOrder !== undefined) sortOrder.value = params.sortOrder
    if (params.pageSize !== undefined) pageSize.value = params.pageSize
    
    // 重新搜索
    if (searchQuery.value) {
      searchPackages()
    }
  }
  
  // 切换页面
  const goToPage = (page: number) => {
    currentPage.value = page
    if (searchQuery.value) {
      searchPackages()
    }
  }
  
  return {
    // 状态
    searchResult,
    popularPackages,
    currentPackage,
    searchLoading,
    popularLoading,
    packageLoading,
    searchHistory,
    searchQuery,
    selectedLanguage,
    sortBy,
    sortOrder,
    currentPage,
    pageSize,
    
    // 计算属性
    hasSearchResults,
    
    // 方法
    searchPackages,
    getPopularPackages,
    getPackage,
    downloadPackage,
    resetSearch,
    updateSearchParams,
    goToPage,
  }
})