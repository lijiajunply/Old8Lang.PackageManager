import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'home',
    component: () => import('@/views/HomeView.vue'),
    meta: {
      title: 'Old8Lang 包管理器',
      description: '发现和下载 Old8Lang 包',
    },
  },
  {
    path: '/search',
    name: 'search',
    component: () => import('@/views/SearchView.vue'),
    meta: {
      title: '搜索包',
      description: '搜索 Old8Lang 包',
    },
  },
  {
    path: '/package/:id/:version?',
    name: 'package',
    component: () => import('@/views/PackageView.vue'),
    meta: {
      title: '包详情',
      description: '查看包的详细信息',
    },
  },
  {
    path: '/upload',
    name: 'upload',
    component: () => import('@/views/UploadView.vue'),
    meta: {
      title: '上传包',
      description: '上传你的包到仓库',
    },
  },
  {
    path: '/docs',
    name: 'docs',
    component: () => import('@/views/DocsView.vue'),
    meta: {
      title: '文档',
      description: 'Old8Lang 包管理器文档',
    },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/views/NotFoundView.vue'),
    meta: {
      title: '页面未找到',
    },
  },
]

export default routes