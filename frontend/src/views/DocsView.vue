<template>
  <div class="min-h-screen bg-gray-50">
    <div class="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <h1 class="text-4xl font-bold text-gray-900 mb-8">文档中心</h1>
      
      <div class="grid grid-cols-1 lg:grid-cols-4 gap-6">
        <!-- 侧边导航 -->
        <div class="lg:col-span-1">
          <n-card title="文档导航" size="small">
            <n-menu>
              <n-menu-item @click="scrollToSection('getting-started')">
                <template #icon>
                  <n-icon>
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332-.477 4.5-1.253z"></path>
                    </svg>
                  </n-icon>
                </template>
                快速开始
              </n-menu-item>
              
              <n-menu-item @click="scrollToSection('package-development')">
                <template #icon>
                  <n-icon>
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4-6 6"></path>
                    </svg>
                  </n-icon>
                </template>
                包开发
              </n-menu-item>
              
              <n-menu-item @click="scrollToSection('api-reference')">
                <template #icon>
                  <n-icon>
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"></path>
                    </svg>
                  </n-icon>
                </template>
                API 参考
              </n-menu-item>
              
              <n-menu-item @click="scrollToSection('examples')">
                <template #icon>
                  <n-icon>
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4-6 6"></path>
                    </svg>
                  </n-icon>
                </template>
                示例
              </n-menu-item>
            </n-menu>
          </n-card>
        </div>
        
        <!-- 主要内容 -->
        <div class="lg:col-span-3 space-y-8">
          <!-- 快速开始 -->
          <section id="getting-started" ref="gettingStartedRef">
            <n-card title="快速开始">
              <div class="prose prose-lg max-w-none">
                <h3>安装 Old8Lang 包管理器</h3>
                <div class="code-block">
                  <pre><code># 下载并安装 o8pm
curl -fsSL https://get.o8pm.dev/install.sh | sh

# 或使用包管理器安装
npm install -g @old8lang/o8pm</code></pre>
                </div>
                
                <h3>初始化项目</h3>
                <div class="code-block">
                  <pre><code># 创建新项目
mkdir my-o8-project
cd my-o8-project

# 初始化包配置
o8pm init

# 添加依赖
o8pm add utils@1.0.0

# 安装依赖
o8pm install</code></pre>
                </div>
                
                <h3>基本命令</h3>
                <div class="code-block">
                  <pre><code># 搜索包
o8pm search utils

# 查看包信息
o8pm info utils

# 下载包
o8pm download utils@1.0.0

# 发布包
o8pm publish ./my-package-1.0.0.o8pkg</code></pre>
                </div>
              </div>
            </n-card>
          </section>
          
          <!-- 包开发 -->
          <section id="package-development" ref="packageDevRef">
            <n-card title="包开发指南">
              <div class="prose prose-lg max-w-none">
                <h3>包结构</h3>
                <p>每个 Old8Lang 包都遵循标准的目录结构：</p>
                <div class="code-block">
                  <pre><code>my-package.1.0.0.o8pkg
├── package.json          # 包元数据
├── lib/                  # 编译后的代码
│   ├── old8lang-1.0/
│   │   └── package.o8
│   └── old8lang-1.1/
│       └── package.o8
├── docs/                 # 文档
│   ├── README.md
│   └── api.md
├── examples/             # 示例代码
└── tools/                # 工具脚本</code></pre>
                </div>
                
                <h3>package.json 格式</h3>
                <div class="code-block">
                  <pre><code>{
  "id": "my-package",
  "version": "1.0.0",
  "name": "My Package",
  "description": "一个有用的 Old8Lang 包",
  "author": "Your Name",
  "license": "MIT",
  "keywords": ["utility", "tools"],
  "frameworks": {
    "old8lang-1.0": {},
    "old8lang-1.1": {}
  },
  "dependencies": [
    {
      "id": "another-package",
      "version": "^1.0.0",
      "targetFramework": "old8lang-1.0"
    }
  ],
  "repository": {
    "type": "git",
    "url": "https://github.com/username/my-package.git"
  },
  "publishedAt": "2024-01-01T00:00:00Z",
  "checksum": "sha256:abc123...",
  "size": 1024000
}</code></pre>
                </div>
              </div>
            </n-card>
          </section>
          
          <!-- API 参考 -->
          <section id="api-reference" ref="apiRef">
            <n-card title="API 参考">
              <div class="prose prose-lg max-w-none">
                <h3>REST API 端点</h3>
                
                <h4>搜索包</h4>
                <div class="code-block">
                  <pre><code>GET /api/packages/search?q=utils&language=old8lang&skip=0&take=20</code></pre>
                </div>
                
                <h4>获取包详情</h4>
                <div class="code-block">
                  <pre><code>GET /api/packages/{packageId}
GET /api/packages/{packageId}/versions/{version}</code></pre>
                </div>
                
                <h4>下载包</h4>
                <div class="code-block">
                  <pre><code>GET /api/packages/{packageId}/{version}/download</code></pre>
                </div>
                
                <h4>上传包</h4>
                <div class="code-block">
                  <pre><code>POST /api/packages/upload
Content-Type: multipart/form-data</code></pre>
                </div>
              </div>
            </n-card>
          </section>
          
          <!-- 示例 -->
          <section id="examples" ref="examplesRef">
            <n-card title="示例项目">
              <div class="prose prose-lg max-w-none">
                <h3>工具包示例</h3>
                <p>创建一个简单的字符串工具包：</p>
                <div class="code-block">
                  <pre><code>// package.json
{
  "id": "string-utils",
  "version": "1.0.0",
  "name": "String Utilities",
  "description": "字符串处理工具集",
  "main": "lib/old8lang-1.0/string-utils.o8"
}</code></pre>
                </div>
                
                <h3>主文件代码</h3>
                <div class="code-block">
                  <pre><code>// lib/old8lang-1.0/string-utils.o8
package string.utils

// 检查字符串是否为空
func isEmpty(str: string): bool {
    return length(str) == 0
}

// 首字母大写
func capitalize(str: string): string {
    if (isEmpty(str)) {
        return str
    }
    return toUpper(substring(str, 0, 1)) + substring(str, 1)
}

// 反转字符串
func reverse(str: string): string {
    var result = ""
    var i = length(str) - 1
    
    while (i >= 0) {
        result = result + substring(str, i, i + 1)
        i = i - 1
    }
    
    return result
}</code></pre>
                </div>
              </div>
            </n-card>
          </section>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const gettingStartedRef = ref<HTMLElement | null>(null)
const packageDevRef = ref<HTMLElement | null>(null)
const apiRef = ref<HTMLElement | null>(null)
const examplesRef = ref<HTMLElement | null>(null)

// 滚动到指定区域
const scrollToSection = (sectionId: string) => {
  const refs: Record<string, { value: HTMLElement | null }> = {
    'getting-started': gettingStartedRef,
    'package-development': packageDevRef,
    'api-reference': apiRef,
    'examples': examplesRef,
  }
  
  const element = refs[sectionId]?.value
  if (element) {
    element.scrollIntoView({ behavior: 'smooth' })
  }
}
</script>

<style scoped>
.prose {
  color: #374151;
  line-height: 1.75;
}

.prose h3 {
  margin: 2rem 0 1rem;
  font-size: 1.25rem;
  font-weight: 600;
  color: #111827;
}

.prose h4 {
  margin: 1.5rem 0 0.5rem;
  font-size: 1.125rem;
  font-weight: 600;
  color: #111827;
}

.prose p {
  margin-bottom: 1rem;
}

.prose pre {
  background-color: #1f2937;
  color: #f3f4f6;
  border-radius: 0.5rem;
  padding: 1rem;
  overflow-x: auto;
  font-size: 0.875rem;
  line-height: 1.5;
}
</style>