<template>
  <div class="min-h-screen bg-gray-50 flex items-center justify-center">
    <div class="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
      <div class="mb-8">
        <div class="w-24 h-24 bg-gray-200 rounded-full flex items-center justify-center mx-auto mb-4">
          <n-icon size="48" class="text-gray-400">
            <svg class="w-12 h-12" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.172 16.172a4 4 0 015.656 0L9 10.343l7.071 7.071a1 1 0 001.414 1.414l-7.071-7.071a1 1 0 00-1.414 0L2.586 8.828a4 4 0 00-5.657 0L1.172 6.828a4 4 0 005.656 0L7 14.657l2.829 2.829a1 1 0 001.414 1.414l-2.829-2.829a1 1 0 00-1.414 0L5.172 13.828a4 4 0 015.657 0L12.828 6.172a1 1 0 011.414-1.414L16.414 6.828A1 1 0 0116 6.828L13.172 9.657a1 1 0 00-1.414 0L7.586 14.243a4 4 0 00-5.657 0L1.172 8.828a4 4 0 005.656 0L7 14.657l2.829 2.829a1 1 0 001.414 1.414l-2.829-2.829a1 1 0 00-1.414 0L5.172 13.828a4 4 0 015.657 0L12.828 6.172a1 1 0 011.414-1.414L16.414 6.828A1 1 0 0116 6.828L13.172 9.657a1 1 0 00-1.414 0z"></path>
            </svg>
          </n-icon>
        </div>
        <h1 class="text-3xl font-bold text-gray-900 mb-2">页面未找到</h1>
        <p class="text-lg text-gray-600 mb-6">抱歉，您访问的页面不存在</p>
      </div>
      
      <div class="bg-white rounded-lg shadow-sm p-8">
        <h2 class="text-xl font-semibold text-gray-900 mb-4">您可以尝试：</h2>
        <div class="space-y-4">
          <div class="flex items-start space-x-3">
            <n-icon size="20" class="text-gray-400 mt-0.5">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3a1 1 0 001-1v-4M4 10h4m4-4h4m4 4v4m0 0h-3a1 1 0 01-1-1v-4"></path>
              </svg>
            </n-icon>
            <div>
              <h3 class="font-medium text-gray-900">检查网址</h3>
              <p class="text-gray-600 text-sm">确保输入的网址正确</p>
            </div>
          </div>
          
          <div class="flex items-start space-x-3">
            <n-icon size="20" class="text-gray-400 mt-0.5">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path>
              </svg>
            </n-icon>
            <div>
              <h3 class="font-medium text-gray-900">搜索包</h3>
              <p class="text-gray-600 text-sm">使用搜索功能查找您需要的包</p>
            </div>
          </div>
          
          <div class="flex items-start space-x-3">
            <n-icon size="20" class="text-gray-400 mt-0.5">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3a1 1 0 001-1v-4M4 10h4m4-4h4m4 4v4m0 0h-3a1 1 0 01-1-1v-4"></path>
              </svg>
            </n-icon>
            <div>
              <h3 class="font-medium text-gray-900">返回首页</h3>
              <p class="text-gray-600 text-sm">回到主页查看热门包和功能</p>
            </div>
          </div>
        </div>
        
        <div class="mt-6 flex flex-col sm:flex-row gap-4">
          <n-button size="large" @click="$router.push('/search')">
            <template #icon>
              <n-icon>
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path>
                </svg>
              </n-icon>
            </template>
            搜索包
          </n-button>
          
          <n-button size="large" @click="$router.push('/')">
            <template #icon>
              <n-icon>
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3a1 1 0 001-1v-4M4 10h4m4-4h4m4 4v4m0 0h-3a1 1 0 01-1-1v-4"></path>
                </svg>
              </n-icon>
            </template>
            返回首页
          </n-button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// 404 页面不需要额外的逻辑
</script>