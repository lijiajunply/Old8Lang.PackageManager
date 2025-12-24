import { api } from './package'

export interface User {
  id: number
  username: string
  email: string
  displayName?: string
  avatarUrl?: string
  bio?: string
  websiteUrl?: string
  company?: string
  location?: string
  isEmailVerified: boolean
  isActive: boolean
  isAdmin: boolean
  createdAt: string
  lastLoginAt: string
  packageCount: number
  totalDownloads: number
  usedStorage: number
  preferredLanguage: string
  emailNotificationsEnabled: boolean
  externalLogins: Array<{
    provider: string
    providerDisplayName: string
    createdAt: string
  }>
}

export interface AuthProvider {
  name: string
  displayName: string
  callbackPath?: string
  scopes: string[]
}

export interface LoginResponse {
  message: string
  user?: User
}

export interface ApiResponse<T = any> {
  success: boolean
  message: string
  data?: T
  error?: string
}

export const authApi = {
  /**
   * 获取当前用户信息
   */
  getCurrentUser: async (): Promise<ApiResponse<User>> => {
    try {
      const response = await api.get('/api/v1/auth/me')
      return {
        success: true,
        message: '获取用户信息成功',
        data: response.data
      }
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || '获取用户信息失败',
        error: error.response?.data?.error || 'UNKNOWN_ERROR'
      }
    }
  },

  /**
   * 用户登出
   */
  logout: async (): Promise<ApiResponse> => {
    try {
      const response = await api.post('/api/v1/auth/logout')
      return {
        success: true,
        message: response.data.message || '登出成功'
      }
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || '登出失败',
        error: error.response?.data?.error || 'LOGOUT_FAILED'
      }
    }
  },

  /**
   * 获取可用的认证提供商
   */
  getAuthProviders: async (): Promise<ApiResponse<AuthProvider[]>> => {
    try {
      const response = await api.get('/api/v1/auth/providers')
      return {
        success: true,
        message: '获取认证提供商成功',
        data: response.data.providers
      }
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || '获取认证提供商失败',
        error: error.response?.data?.error || 'GET_PROVIDERS_FAILED'
      }
    }
  },

  /**
   * 启动外部登录
   */
  externalLogin: async (provider: string, returnUrl?: string): Promise<void> => {
    try {
      const params = new URLSearchParams()
      if (returnUrl) {
        params.append('returnUrl', returnUrl)
      }
      
      const url = `/api/v1/auth/login/${provider}${params.toString() ? '?' + params.toString() : ''}`
      window.location.href = url
    } catch (error: any) {
      console.error('启动外部登录失败:', error)
    }
  },

  /**
   * 检查用户是否已登录
   */
  checkAuthStatus: async (): Promise<boolean> => {
    try {
      const response = await authApi.getCurrentUser()
      return response.success
    } catch (error) {
      return false
    }
  }
}

export default authApi