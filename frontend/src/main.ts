import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createRouter, createWebHistory } from 'vue-router'

import App from './App.vue'

// 导入 Naive UI
import {
  create,
  NButton,
  NInput,
  NCard,
  NLayout,
  NLayoutHeader,
  NLayoutContent,
  NLayoutSider,
  NMenu,
  NIcon,
  NSpin,
  NTag,
  NPagination,
  NDropdown,
  NModal,
  NForm,
  NFormItem,
  NSelect,
  NUpload,
  NMessageProvider,
  NNotificationProvider,
  NResult,
  NEmpty,
  NSpace,
  NDivider,
  NTooltip,
  NProgress,
  NRate,
  NConfigProvider,
  darkTheme,
  zhCN,
  enUS,
  dateZhCN,
  dateEnUS,
} from 'naive-ui'

// 导入路由
import routes from './router'

// 导入样式
import 'tailwindcss/tailwind.css'
import './assets/styles/main.css'

// 创建路由
const router = createRouter({
  history: createWebHistory(),
  routes,
})

// 创建 Naive UI 实例
const naive = create({
  components: [
    NButton,
    NInput,
    NCard,
    NLayout,
    NLayoutHeader,
    NLayoutContent,
    NLayoutSider,
    NMenu,
    NIcon,
    NSpin,
    NTag,
    NPagination,
    NDropdown,
    NModal,
    NForm,
    NFormItem,
    NSelect,
    NUpload,
    NMessageProvider,
    NNotificationProvider,
    NResult,
    NEmpty,
    NSpace,
    NDivider,
    NTooltip,
    NProgress,
    NRate,
    NConfigProvider,
  ],
})

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(naive)

app.mount('#app')