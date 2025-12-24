<template>
   <div class="min-h-screen bg-gray-50">
     <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
       <!-- 页面标题 -->
       <div class="mb-8 text-center">
         <h1 class="text-3xl font-bold text-gray-900">上传包</h1>
         <p class="mt-2 text-gray-600">
           {{ userStore.isAuthenticated ? `欢迎回来，${userStore.displayName}！` : '请先登录后再上传包' }}
         </p>
       </div>

       <!-- 未登录提示 -->
       <div v-if="!userStore.isAuthenticated" class="text-center py-12">
         <n-empty description="需要登录才能上传包">
           <template #extra>
             <n-button type="primary" @click="$router.push('/login')">
               立即登录
             </n-button>
           </template>
         </n-empty>
       </div>
      
       <!-- 上传表单 -->
       <div v-if="userStore.isAuthenticated" class="grid grid-cols-1 lg:grid-cols-3 gap-8">
         <div class="lg:col-span-2">
           <n-card>
            <n-form
              ref="formRef"
              :model="formData"
              :rules="rules"
              label-placement="top"
              class="space-y-6"
            >
              <!-- 文件上传 -->
              <n-form-item label="选择包文件" path="file">
                <n-upload
                  v-model:file-list="fileList"
                  :max="1"
                  accept=".o8pkg,.zip"
                  :before-upload="beforeUpload"
                  @change="handleFileChange"
                >
                  <n-button>选择文件</n-button>
                  <template #tip>
                    支持 .o8pkg 格式的包文件，最大 100MB
                  </template>
                </n-upload>
              </n-form-item>
              
              <!-- 包语言 -->
              <n-form-item label="包语言" path="language">
                <n-select
                  v-model:value="formData.language"
                  placeholder="选择包语言"
                >
                  <n-option value="old8lang" label="Old8Lang" />
                  <n-option value="python" label="Python" />
                  <n-option value="javascript" label="JavaScript" />
                  <n-option value="typescript" label="TypeScript" />
                </n-select>
              </n-form-item>
              
              <!-- API 密钥 -->
              <n-form-item label="API 密钥" path="apiKey">
                <n-input
                  v-model:value="formData.apiKey"
                  type="password"
                  placeholder="输入发布密钥（可选）"
                  show-password-on="mousedown"
                />
              </n-form-item>
              
              <!-- 提交按钮 -->
              <n-form-item>
                <n-button
                  type="primary"
                  size="large"
                  :loading="uploading"
                  :disabled="!canUpload"
                  @click="handleSubmit"
                  block
                >
                  上传包
                </n-button>
              </n-form-item>
            </n-form>
          </n-card>
        </div>
        
        <!-- 说明信息 -->
        <div class="space-y-6">
          <!-- 包格式说明 -->
          <n-card title="包格式说明" size="small">
            <div class="space-y-3 text-sm">
              <div>
                <strong>.o8pkg 格式</strong>
                <p class="text-gray-600 mt-1">标准包格式，包含代码、文档和元数据</p>
              </div>
              <div>
                <strong>结构要求</strong>
                <div class="bg-gray-100 p-3 rounded mt-2">
                  <pre class="text-xs">MyPackage.1.0.0.o8pkg
├── package.json
├── lib/
│   └── old8lang-1.0/
│       └── MyPackage.o8
├── docs/
└── examples/</pre>
                </div>
              </div>
            </div>
          </n-card>
          
          <!-- 最佳实践 -->
          <n-card title="最佳实践" size="small">
            <div class="space-y-3 text-sm">
              <div>
                <strong>版本管理</strong>
                <p class="text-gray-600 mt-1">使用语义化版本控制（如 1.0.0, 1.2.3）</p>
              </div>
              <div>
                <strong>文档完整</strong>
                <p class="text-gray-600 mt-1">包含 README、API 文档和使用示例</p>
              </div>
              <div>
                <strong>依赖声明</strong>
                <p class="text-gray-600 mt-1">在 package.json 中明确声明所有依赖</p>
              </div>
              <div>
                <strong>测试覆盖</strong>
                <p class="text-gray-600 mt-1">确保包有充分的测试覆盖</p>
              </div>
            </div>
          </n-card>
          
          <!-- 相关链接 -->
          <n-card title="相关链接" size="small">
            <div class="space-y-2 text-sm">
              <a href="#" class="text-blue-600 hover:text-blue-700 block">
                包开发指南 →
              </a>
              <a href="#" class="text-blue-600 hover:text-blue-700 block">
                API 参考文档 →
              </a>
              <a href="#" class="text-blue-600 hover:text-blue-700 block">
                示例项目模板 →
              </a>
            </div>
          </n-card>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useMessage, type FormInst, type FormRules } from 'naive-ui'
import type { PackageApi } from '@/api/package'
import type { UploadFileInfo } from 'naive-ui'
import { useUserStore } from '@/stores/user'
import { useRouter } from 'vue-router'

const message = useMessage()
const userStore = useUserStore()
const router = useRouter()

// 表单数据
const formRef = ref<FormInst | null>(null)
const uploading = ref(false)
const fileList = ref<UploadFileInfo[]>([])

const formData = ref({
  file: null as File | null,
  language: '',
  apiKey: '',
})

// 表单验证规则
const rules: FormRules = {
  file: [
    {
      required: true,
      message: '请选择包文件',
      trigger: ['change', 'blur']
    }
  ],
  language: [
    {
      required: true,
      message: '请选择包语言',
      trigger: ['change', 'blur']
    }
  ]
}

// 是否可以上传
const canUpload = computed(() => {
  return formData.value.file !== null && formData.value.language !== ''
})

// 文件上传前检查
const beforeUpload = (data: { file: File }) => {
  const file = data.file
  
  // 检查文件大小（100MB）
  if (file.size > 100 * 1024 * 1024) {
    message.error('文件大小不能超过 100MB')
    return false
  }
  
  // 检查文件类型
  const allowedTypes = ['.o8pkg', '.zip']
  const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase()
  if (!allowedTypes.includes(fileExtension)) {
    message.error('只支持 .o8pkg 和 .zip 格式的文件')
    return false
  }
  
  return true
}

// 处理文件变化
const handleFileChange = (options: { file: File, fileList: UploadFileInfo[] }) => {
  if (options.file) {
    formData.value.file = options.file
    fileList.value = [options.fileList[0]]
  } else {
    formData.value.file = null
    fileList.value = []
  }
}

// 处理提交
const handleSubmit = async () => {
  if (!formRef.value) return
  
  try {
    await formRef.value.validate()
  } catch {
    return
  }
  
  if (!formData.value.file) {
    message.error('请选择要上传的文件')
    return
  }
  
  uploading.value = true
  
  try {
    const formDataToSend = new FormData()
    formDataToSend.append('file', formData.value.file)
    formDataToSend.append('language', formData.value.language)
    
    if (formData.value.apiKey) {
      formDataToSend.append('apiKey', formData.value.apiKey)
    }
    
    const result = await PackageApi.uploadPackage(formDataToSend, (progressEvent) => {
      const percent = Math.round(
        (progressEvent.loaded * 100) / progressEvent.total
      )
      message.info(`上传进度：${percent}%`)
    })
    
    if (result.success) {
      message.success(`包 ${result.packageId} 上传成功！`)
      // 重置表单
      formData.value.file = null
      formData.value.language = ''
      formData.value.apiKey = ''
      fileList.value = []
    } else {
      message.error(`上传失败：${result.message}`)
    }
  } catch (error: any) {
    message.error(`上传失败：${error.message}`)
  } finally {
    uploading.value = false
  }
}
</script>

<style scoped>
.n-card :deep(.n-card__content) {
  padding: 24px;
}

pre {
  white-space: pre-wrap;
  word-break: break-all;
}
</style>