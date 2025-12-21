export interface Package {
  id: string
  version: string
  name: string
  description: string
  author: string
  license: string
  homepage?: string
  repository?: {
    type: string
    url: string
  }
  keywords: string[]
  dependencies: PackageDependency[]
  frameworks: Record<string, any>
  publishedAt: string
  checksum: string
  size: number
  downloadCount: number
  language: 'old8lang' | 'python' | 'javascript' | 'typescript'
  isListed: boolean
  isPrerelease: boolean
  tags: string[]
}

export interface PackageDependency {
  id: string
  version: string
  targetFramework: string
  isDevelopmentDependency?: boolean
}

export interface PackageVersion {
  version: string
  packageContent: string
  packageHash: string
  publishedAt: string
  isPrerelease: boolean
  downloadCount: number
}

export interface SearchResult {
  packages: Package[]
  totalCount: number
  currentPage: number
  pageSize: number
  totalPages: number
}

export interface PackageSearchRequest {
  q?: string
  language?: string
  skip?: number
  take?: number
  sortBy?: 'relevance' | 'name' | 'created' | 'updated' | 'downloads'
  sortOrder?: 'asc' | 'desc'
  prerelease?: boolean
}

export interface UploadResponse {
  packageId: string
  version: string
  language: string
  success: boolean
  message: string
  warnings?: string[]
}

export interface ApiError {
  message: string
  statusCode: number
  details?: any
}

export interface PopularPackage {
  packageId: string
  name: string
  description: string
  downloadCount: number
  language: string
  latestVersion: string
  updatedAt: string
}