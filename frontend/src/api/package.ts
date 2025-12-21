import axios, { AxiosResponse, AxiosError } from 'axios'
import type { 
  Package, 
  PackageSearchRequest, 
  SearchResult, 
  UploadResponse, 
  PopularPackage,
  ApiError 
} from '@/types/package'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api'

// 创建 axios 实例
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
})

// 请求拦截器
apiClient.interceptors.request.use(
  (config) => {
    // 可以在这里添加认证 token
    // const token = localStorage.getItem('auth-token')
    // if (token) {
    //   config.headers.Authorization = `Bearer ${token}`
    // }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// 响应拦截器
apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    return response
  },
  (error: AxiosError) => {
    const apiError: ApiError = {
      message: error.response?.data?.message || error.message || '请求失败',
      statusCode: error.response?.status || 500,
      details: error.response?.data?.details,
    }
    return Promise.reject(apiError)
  }
)

export class PackageApi {
  // 搜索包
  static async searchPackages(params: PackageSearchRequest = {}): Promise<SearchResult> {
    const response = await apiClient.get<SearchResult>('/packages/search', { params })
    return response.data
  }

  // 获取包详情
  static async getPackage(packageId: string, version?: string): Promise<Package> {
    const url = version 
      ? `/packages/${packageId}/versions/${version}`
      : `/packages/${packageId}`
    const response = await apiClient.get<Package>(url)
    return response.data
  }

  // 获取包的所有版本
  static async getPackageVersions(packageId: string): Promise<string[]> {
    const response = await apiClient.get<string[]>(`/packages/${packageId}/versions`)
    return response.data
  }

  // 获取热门包
  static async getPopularPackages(language?: string, count = 20): Promise<PopularPackage[]> {
    const params: any = { count }
    if (language) {
      params.language = language
    }
    const response = await apiClient.get<PopularPackage[]>('/packages/popular', { params })
    return response.data
  }

  // 下载包
  static async downloadPackage(packageId: string, version: string): Promise<Blob> {
    const response = await apiClient.get(`/packages/${packageId}/${version}/download`, {
      responseType: 'blob',
    })
    return response.data
  }

  // 上传包
  static async uploadPackage(
    formData: FormData,
    onProgress?: (progressEvent: any) => void
  ): Promise<UploadResponse> {
    const response = await apiClient.post<UploadResponse>('/packages/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: onProgress,
    })
    return response.data
  }

  // 删除包
  static async deletePackage(packageId: string, version: string): Promise<void> {
    await apiClient.delete(`/packages/${packageId}/${version}`)
  }

  // 获取包下载统计
  static async getDownloadStats(packageId: string, version: string): Promise<number> {
    const response = await apiClient.get<{ count: number }>(`/packages/${packageId}/${version}/stats`)
    return response.data.count
  }
}

// PyPI 兼容性 API
export class PyPIApi {
  // 获取简单索引
  static async getSimpleIndex(): Promise<string> {
    const response = await apiClient.get('/simple/', {
      headers: {
        'Accept': 'text/html',
      },
    })
    return response.data
  }

  // 获取包的版本列表 (PyPI 格式)
  static async getPyPIPackageVersions(packageName: string): Promise<any> {
    const response = await apiClient.get(`/pypi/${packageName}/json`)
    return response.data
  }

  // 搜索包 (PyPI 兼容格式)
  static async searchPyPIPackages(params: {
    q?: string
    page?: number
    per_page?: number
  }): Promise<any> {
    const response = await apiClient.get('/pypi/search', { params })
    return response.data
  }
}

// NPM 兼容性 API
export class NPMApi {
  // 搜索包 (NPM 兼容格式)
  static async searchNPMPackages(params: {
    text?: string
    from?: number
    size?: number
  }): Promise<any> {
    const response = await apiClient.get('/npm/search', { params })
    return response.data
  }

  // 获取包信息 (NPM 兼容格式)
  static async getNPMPackage(packageName: string): Promise<any> {
    const response = await apiClient.get(`/npm/${packageName}`)
    return response.data
  }
}