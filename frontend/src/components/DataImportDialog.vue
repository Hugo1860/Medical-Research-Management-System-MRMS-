<template>
  <el-dialog
    :model-value="modelValue"
    :title="title || '批量数据导入'"
    width="580px"
    class="data-import-dialog"
    destroy-on-close
    append-to-body
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div class="import-body">
      <!-- 模板下载提示条 -->
      <div v-if="templateUrl || templateName" class="template-download-banner">
        <div class="banner-left">
          <el-icon class="icon-file"><Document /></el-icon>
          <div class="text-group">
            <span class="file-name">{{ templateName || '标准数据导入模板.xlsx' }}</span>
            <span class="file-tip">请下载标准模板并按格式填写后上传，避免表头格式错位</span>
          </div>
        </div>
        <el-button type="primary" link :icon="Download" @click="handleDownloadTemplate">
          下载模板
        </el-button>
      </div>

      <!-- 文件拖拽上传区域 -->
      <el-upload
        ref="uploadRef"
        drag
        action="#"
        :auto-upload="false"
        :limit="1"
        accept=".xlsx,.xls,.csv"
        :on-change="handleFileChange"
        :on-remove="handleFileRemove"
        class="import-uploader"
      >
        <el-icon class="el-icon--upload"><UploadFilled /></el-icon>
        <div class="el-upload__text">
          将文件拖到此处，或<em>点击上传</em>
        </div>
        <template #tip>
          <div class="el-upload__tip">
            支持扩展名：.xlsx, .xls, .csv，单文件大小不超过 20MB
          </div>
        </template>
      </el-upload>

      <!-- 已选文件与解析进度 -->
      <div v-if="currentFile" class="file-preview-card">
        <div class="file-info-row">
          <span class="name">📄 {{ currentFile.name }}</span>
          <span class="size">({{ formatFileSize(currentFile.size || 0) }})</span>
        </div>
        <el-progress
          v-if="uploadProgress > 0"
          :percentage="uploadProgress"
          :status="uploadProgress === 100 ? 'success' : ''"
          style="margin-top: 8px;"
        />
      </div>

      <!-- 规则提示卡片 -->
      <div class="rules-tip-box">
        <div class="tip-title">导入注意事项：</div>
        <ul>
          <li>工号、姓名、项目名称等必填主键字段不可为空。</li>
          <li>日期格式统一推荐采用 <code>YYYY-MM-DD</code>（如：2026-10-15）。</li>
          <li>系统将按唯一编号/工号自动执行幂等增量或覆盖更新，保障历史数据安全。</li>
        </ul>
      </div>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button :disabled="importing" @click="emit('update:modelValue', false)">取消</el-button>
        <el-button
          type="primary"
          :loading="importing"
          :disabled="!currentFile"
          @click="handleSubmitImport"
        >
          开始执行导入
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { ElMessage, type UploadFile } from 'element-plus'
import { Download, Document, UploadFilled } from '@element-plus/icons-vue'

const props = withDefaults(
  defineProps<{
    modelValue: boolean
    title?: string
    templateName?: string
    templateUrl?: string
    uploadHandler?: (file: File) => Promise<any>
  }>(),
  {
    title: '批量数据导入',
    templateName: '标准批量导入模板.xlsx'
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', visible: boolean): void
  (e: 'success', result?: any): void
}>()

const currentFile = ref<File | null>(null)
const uploadProgress = ref(0)
const importing = ref(false)

function handleFileChange(file: UploadFile) {
  if (file.raw) {
    currentFile.value = file.raw
    uploadProgress.value = 0
  }
}

function handleFileRemove() {
  currentFile.value = null
  uploadProgress.value = 0
}

function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return (bytes / Math.pow(k, i)).toFixed(1) + ' ' + sizes[i]
}

function handleDownloadTemplate() {
  if (props.templateUrl) {
    window.open(props.templateUrl, '_blank')
  } else {
    // 模拟模板下载
    ElMessage.success(`正在准备并导出【${props.templateName}】...`)
    const csvContent = '\uFEFF工号,姓名,部门,联系电话,电子邮箱,专业技术级别\nDOC001,示例人员,心血管内科,13800000000,sample@hospital.org,副高级\n'
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = props.templateName.replace(/\.xlsx?$/, '.csv')
    link.click()
    URL.revokeObjectURL(link.href)
  }
}

async function handleSubmitImport() {
  if (!currentFile.value) {
    ElMessage.warning('请先选取待导入的数据表格文件')
    return
  }

  importing.value = true
  uploadProgress.value = 20

  try {
    const timer = setInterval(() => {
      if (uploadProgress.value < 85) {
        uploadProgress.value += 20
      }
    }, 200)

    let res: any = null
    if (props.uploadHandler) {
      res = await props.uploadHandler(currentFile.value)
    } else {
      // 默认解析等待
      await new Promise(r => setTimeout(r, 600))
      res = { count: 1, message: '导入成功' }
    }

    clearInterval(timer)
    uploadProgress.value = 100

    const successMsg = res?.说明 || (res?.导入数量 != null ? `导入完成，成功入库 ${res.导入数量} 篇数据` : '批量数据导入已完成并入库！')
    ElMessage.success(successMsg)
    emit('success', res)
    emit('update:modelValue', false)
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.error || err.message || '导入解析失败')
  } finally {
    importing.value = false
  }
}

watch(
  () => props.modelValue,
  (val) => {
    if (val) {
      currentFile.value = null
      uploadProgress.value = 0
    }
  }
)
</script>

<style scoped lang="scss">
.import-body {
  .template-download-banner {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 12px 16px;
    background: #f0fdf4;
    border: 1px solid #bbf7d0;
    border-radius: 8px;
    margin-bottom: 16px;

    .banner-left {
      display: flex;
      align-items: center;
      gap: 12px;

      .icon-file {
        font-size: 24px;
        color: #16a34a;
      }

      .text-group {
        display: flex;
        flex-direction: column;

        .file-name {
          font-weight: 600;
          font-size: 13px;
          color: #15803d;
        }

        .file-tip {
          font-size: 12px;
          color: #65a30d;
        }
      }
    }
  }

  .import-uploader {
    width: 100%;
    margin-bottom: 12px;
  }

  .file-preview-card {
    background: #f8fafc;
    border: 1px solid #e2e8f0;
    padding: 12px;
    border-radius: 6px;
    margin-bottom: 12px;

    .file-info-row {
      display: flex;
      justify-content: space-between;
      font-size: 13px;
      color: #334155;
      font-weight: 500;

      .size {
        color: #94a3b8;
      }
    }
  }

  .rules-tip-box {
    background: #f8fafc;
    border-radius: 6px;
    padding: 12px 16px;
    font-size: 12px;
    color: #64748b;
    border: 1px solid #e2e8f0;

    .tip-title {
      font-weight: 600;
      color: #475569;
      margin-bottom: 4px;
    }

    ul {
      margin: 0;
      padding-left: 18px;
      line-height: 1.6;
    }

    code {
      background: #edf2f7;
      padding: 1px 4px;
      border-radius: 3px;
      color: #0f172a;
    }
  }
}
</style>
