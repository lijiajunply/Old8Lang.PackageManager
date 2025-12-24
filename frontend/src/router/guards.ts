import type { Router } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useMessage } from 'naive-ui'

/**
 * 创建认证守卫
 */
export function createAuthGuard(router: Router) {
  const userStore = useUserStore()
  const message = useMessage()

  router.beforeEach(async (to, from, next) => {
    // 初始化用户认证状态
    if (!userStore.user) {
      await userStore.initAuth()
    }

    // 检查是否需要认证
    const requiresAuth = to.meta?.requiresAuth as boolean
    const hideWhenAuthenticated = to.meta?.hideWhenAuthenticated as boolean

    // 如果页面需要认证但用户未登录
    if (requiresAuth && !userStore.isAuthenticated) {
      message.warning('请先登录')
      next({
        path: '/login',
        query: { returnUrl: to.fullPath }
      })
      return
    }

    // 如果用户已登录但页面应该隐藏（如登录页面）
    if (hideWhenAuthenticated && userStore.isAuthenticated) {
      next('/')
      return
    }

    // 检查管理员权限
    const requiresAdmin = to.meta?.requiresAdmin as boolean
    if (requiresAdmin && !userStore.isAdmin) {
      message.error('需要管理员权限')
      next(from)
      return
    }

    // 检查特定权限
    const requiredPermission = to.meta?.permission as string
    if (requiredPermission && !userStore.hasPermission(requiredPermission)) {
      message.error('权限不足')
      next(from)
      return
    }

    next()
  })

  // 路由错误处理
  router.onError((error) => {
    console.error('路由错误:', error)
    message.error('页面加载失败，请重试')
  })
}

/**
 * 创建权限检查函数
 */
export function createPermissionChecker(userStore: any) {
  return {
    /**
     * 检查用户是否有权限访问特定路由
     */
    canAccess: (route: any): boolean => {
      const requiresAuth = route.meta?.requiresAuth as boolean
      const requiresAdmin = route.meta?.requiresAdmin as boolean
      const requiredPermission = route.meta?.permission as string

      if (requiresAuth && !userStore.isAuthenticated) {
        return false
      }

      if (requiresAdmin && !userStore.isAdmin) {
        return false
      }

      if (requiredPermission && !userStore.hasPermission(requiredPermission)) {
        return false
      }

      return true
    },

    /**
     * 检查用户是否有特定权限
     */
    hasPermission: (permission: string): boolean => {
      return userStore.hasPermission(permission)
    },

    /**
     * 检查用户是否已登录
     */
    isAuthenticated: (): boolean => {
      return userStore.isAuthenticated
    },

    /**
     * 检查用户是否为管理员
     */
    isAdmin: (): boolean => {
      return userStore.isAdmin
    }
  }
}