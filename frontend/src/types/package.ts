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
  qualityScore?: PackageQualityScore
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

export interface PackageQualityScore {
  qualityScore: number
  completenessScore: number
  stabilityScore: number
  maintenanceScore: number
  securityScore: number
  communityScore: number
  documentationScore: number
  lastCalculatedAt: string
}

export interface DependencyTreeNode {
  packageId: string
  version: string
  language: string
  versionRange: string
  isResolved: boolean
  isCircular: boolean
  isDevelopmentDependency: boolean
  depth: number
  dependencies: DependencyTreeNode[]
}

export interface DependencyTreeResponse {
  packageId: string
  version: string
  language: string
  totalDependencies: number
  maxDepth: number
  hasCircularDependencies: boolean
  circularPaths: string[]
  rootNode: DependencyTreeNode
}

export interface DependencyGraphNode {
  id: string
  packageId: string
  version: string
  language: string
  label: string
  level: number
  isRoot: boolean
  isCircular: boolean
  isDevelopmentDependency: boolean
}

export interface DependencyGraphEdge {
  from: string
  to: string
  versionRange: string
  isDevelopmentDependency: boolean
}

export interface DependencyGraphResponse {
  nodes: DependencyGraphNode[]
  edges: DependencyGraphEdge[]
  totalNodes: number
  totalEdges: number
  hasCircularDependencies: boolean
  circularPaths: string[]
}
