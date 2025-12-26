import { createI18n } from 'vue-i18n'
import en from './locales/en'
import zhCN from './locales/zh-CN'

// 获取浏览器语言
function getBrowserLanguage(): string {
  const lang = navigator.language.toLowerCase()
  if (lang.startsWith('zh')) {
    return 'zh-CN'
  }
  return 'en'
}

// 获取存储的语言设置
function getStoredLanguage(): string | null {
  return localStorage.getItem('language')
}

// 存储语言设置
export function setStoredLanguage(lang: string): void {
  localStorage.setItem('language', lang)
}

// 确定初始语言
const initialLanguage = getStoredLanguage() || getBrowserLanguage()

const i18n = createI18n({
  legacy: false,
  locale: initialLanguage,
  fallbackLocale: 'en',
  messages: {
    en,
    'zh-CN': zhCN,
  },
})

export default i18n
