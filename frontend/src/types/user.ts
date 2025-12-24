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

export interface UserSession {
  isAuthenticated: boolean
  user?: User
  loading: boolean
}

export interface UserPreferences {
  theme: 'light' | 'dark' | 'auto'
  language: string
  notificationsEnabled: boolean
}