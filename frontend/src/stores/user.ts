import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi, type User, type AuthProvider } from '@/api/auth'
import { useRouter } from 'vue-router'
import { useMessage } from 'naive-ui'

export const useUserStore = defineStore('user', () => {
  const router = useRouter()
  const message = useMessage()

  // 状态
  const user = ref<User | null>(null)
  const loading = ref(false)
  const authProviders = ref<AuthProvider[]>([])

  // 计算属性
  const isAuthenticated = computed(() => !!user.value)
  const isAdmin = computed(() => user.value?.isAdmin || false)
  const displayName = computed(() => 
    user.value?.displayName || user.value?.username || '未知用户'
  )
  const avatarUrl = computed(() => 
    user.value?.avatarUrl || `https://ui-avatars.com/api/?name=${encodeURIComponent(displayName.value)}&background=4f46e5&color=fff&size=128`
  )

  // 方法
  /**
   * 初始化用户状态
   */
  const initAuth = async () => {
    if (user.value) return // 已经登录

    loading.value = true
    try {
      const response = await authApi.getCurrentUser()
      if (response.success && response.data) {
        user.value = response.data
      }
    } catch (error) {
      console.error('获取用户信息失败:', error)
    } finally {
      loading.value = false
    }
  }

  /**
   * 获取认证提供商
   */
  const loadAuthProviders = async () => {
    if (authProviders.value.length > 0) return

    try {
      const response = await authApi.getAuthProviders()
      if (response.success && response.data) {
        authProviders.value = response.data
      }
    } catch (error) {
      console.error('获取认证提供商失败:', error)
    }
  }

  /**
   * 使用外部提供商登录
   */
  const loginWithProvider = async (provider: string, returnUrl?: string) => {
    try {
      await authApi.externalLogin(provider, returnUrl)
    } catch (error) {
      message.error('登录失败，请重试')
      console.error('外部登录失败:', error)
    }
  }

  /**
   * 登出
   */
  const logout = async () => {
    try {
      await authApi.logout()
      user.value = null
      message.success('已成功登出')
      
      // 重定向到首页
      router.push('/')
    } catch (error) {
      message.error('登出失败，请重试')
      console.error('登出失败:', error)
    }
  }

  /**
   * 强制刷新用户信息
   */
  const refreshUser = async () => {
    if (!user.value) return

    try {
      const response = await authApi.getCurrentUser()
      if (response.success && response.data) {
        user.value = response.data
      } else {
        // 如果获取失败，可能已登出
        user.value = null
      }
    } catch (error) {
      user.value = null
      console.error('刷新用户信息失败:', error)
    }
  }

  /**
   * 检查用户权限
   */
  const hasPermission = (permission: string): boolean => {
    if (!user.value) return false

    switch (permission) {
      case 'admin':
        return user.value.isAdmin
      case 'upload':
        return true // 所有已登录用户都可以上传
      case 'verified':
        return user.value.isEmailVerified
      default:
        return false
    }
  }

  return {
    // 状态
    user: readonly(user),
    loading: readonly(loading),
    authProviders: readonly(authProviders),
    
    // 计算属性
    isAuthenticated,
    isAdmin,
    displayName,
    avatarUrl,
    
    // 方法
    initAuth,
    loadAuthProviders,
    loginWithProvider,
    logout,
    refreshUser,
    hasPermission
  }
})

// 类型声明
declare module '@vue/runtime-core' {
  interface ComponentCustomProperties {
    $user: ReturnType<typeof useUserStore>
  }
}