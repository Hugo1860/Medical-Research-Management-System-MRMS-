<template>
  <div class="attachment-management">
    <el-card class="header-card" shadow="hover">
      <div class="header-content">
        <div>
          <h2>系统附件与对象存储管理 (MinIO / S3)</h2>
          <p class="subtitle">
            方案 B 工业级对象存储驱动，支持客户端安全预签名直传、SHA256 秒传去重、原中文文件名受控还原与等保三级不可变下载审计。
          </p>
        </div>
        <div class="actions">
          <el-button type="warning" :icon="Refresh" :loading="migrating" @click="handleMigrate">
            一键迁移存量历史附件
          </el-button>
          <el-button type="primary" :icon="RefreshRight" @click="loadData">
            刷新统计
          </el-button>
        </div>
      </div>
    </el-card>

    <!-- 统计指标卡片 -->
    <el-row :gutter="20" class="stat-row">
      <el-col :xs="24" :sm="12" :md="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-label">存储驱动与桶</div>
          <div class="stat-val highlight">{{ stats?.driver || 'MinIO' }} / {{ stats?.defaultBucket || 'private' }}</div>
          <div class="stat-desc">S3 协议兼容 / 信创集群支持</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-label">总附件数</div>
          <div class="stat-val">{{ stats?.summary?.附件总数 || 0 }} 份</div>
          <div class="stat-desc">有效绑定: {{ stats?.summary?.有效附件数 || 0 }} 份</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-label">存储占用容量</div>
          <div class="stat-val">{{ formatSize(stats?.summary?.总存储字节 || 0) }}</div>
          <div class="stat-desc">SHA256 内容级自动去重</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-label">临时草稿未绑定</div>
          <div class="stat-val warn">{{ stats?.summary?.未绑定草稿数 || 0 }} 份</div>
          <div class="stat-desc">超过24小时自动触发GC回收</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 上传体验与审计中心 -->
    <el-row :gutter="20" class="main-row">
      <el-col :span="24">
        <el-card shadow="hover">
          <template #header>
            <div class="card-title">
              <span>对象存储安全直传体验区 (Presigned Direct Upload)</span>
              <el-tag type="success">安全双通道</el-tag>
            </div>
          </template>

          <div class="upload-zone">
            <el-upload
              drag
              :auto-upload="false"
              :on-change="handleFileSelected"
              :show-file-list="false"
              action="#"
            >
              <el-icon class="el-icon--upload"><upload-filled /></el-icon>
              <div class="el-upload__text">
                拖拽科研文档至此处，或 <em>点击选择文件上传</em>
              </div>
              <template #tip>
                <div class="el-upload__tip">
                  支持 PDF、Word、Excel、图片等常用格式；本地先行计算 SHA256 指纹，未发生变化时瞬间完成秒传。
                </div>
              </template>
            </el-upload>

            <div v-if="uploading" class="upload-progress">
              <p>正在处理：{{ currentFileName }} ({{ uploadStatusText }})</p>
              <el-progress :percentage="uploadPercent" :status="uploadPercent === 100 ? 'success' : ''" />
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 等保三级不可变下载与访问审计日志 -->
    <el-card shadow="hover" class="audit-card">
      <template #header>
        <div class="card-title">
          <span>等保三级访问审计日志 (不可变触发器保护)</span>
          <el-tag type="info">只读防篡改</el-tag>
        </div>
      </template>

      <el-table :data="stats?.recentAudits || []" stripe border style="width: 100%">
        <el-table-column prop="编号" label="日志编号" width="90" align="center" />
        <el-table-column prop="原始文件名" label="目标附件" min-width="200" show-overflow-tooltip />
        <el-table-column prop="访问动作" label="操作" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="row.访问动作 === '下载' ? 'success' : 'primary'">{{ row.访问动作 }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="操作人姓名" label="操作人" width="110" align="center" />
        <el-table-column prop="客户端IP" label="客户端 IP" width="140" align="center" />
        <el-table-column prop="访问时间" label="访问时间" width="180">
          <template #default="{ row }">
            {{ formatTime(row.访问时间) }}
          </template>
        </el-table-column>
        <el-table-column prop="鉴权结果" label="鉴权状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.鉴权结果 === '成功' ? 'success' : 'danger'">{{ row.鉴权结果 }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="摘要" label="操作明细摘要" min-width="200" show-overflow-tooltip />
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Refresh, RefreshRight, UploadFilled } from '@element-plus/icons-vue'
import {
  getStorageStats,
  requestUpload,
  completeUpload,
  uploadStream,
  migrateLegacyUploads,
  type StorageStatsResult
} from '@/api/attachment'

const stats = ref<StorageStatsResult | null>(null)
const loading = ref(false)
const migrating = ref(false)

const uploading = ref(false)
const currentFileName = ref('')
const uploadPercent = ref(0)
const uploadStatusText = ref('')

async function loadData() {
  loading.value = true
  try {
    stats.value = await getStorageStats()
  } catch (err: any) {
    ElMessage.error(err.message || '获取存储统计失败')
  } finally {
    loading.value = false
  }
}

function formatSize(bytes: number): string {
  if (!bytes || bytes <= 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return (bytes / Math.pow(k, i)).toFixed(2) + ' ' + sizes[i]
}

function formatTime(val: string): string {
  if (!val) return ''
  const d = new Date(val)
  return d.toLocaleString()
}

// 计算文件的 SHA256 哈希
async function computeSha256(file: File): Promise<string> {
  const buffer = await file.arrayBuffer()
  const hashBuffer = await crypto.subtle.digest('SHA-256', buffer)
  const hashArray = Array.from(new Uint8Array(hashBuffer))
  return hashArray.map(b => b.toString(16).padStart(2, '0')).join('')
}

async function handleFileSelected(uploadFile: any) {
  const rawFile: File = uploadFile.raw
  if (!rawFile) return

  currentFileName.value = rawFile.name
  uploading.value = true
  uploadPercent.value = 10
  uploadStatusText.value = '正在计算本地指纹 (SHA256)...'

  try {
    const sha = await computeSha256(rawFile)
    uploadPercent.value = 30
    uploadStatusText.value = '申请上传凭据或秒传检测...'

    const reqResult = await requestUpload({
      businessType: '测试附件',
      fileName: rawFile.name,
      fileSize: rawFile.size,
      sha256: sha,
      contentType: rawFile.type || 'application/octet-stream'
    })

    if (reqResult.fastUpload) {
      uploadPercent.value = 100
      uploadStatusText.value = '秒传成功！'
      ElMessage.success(`命中内容级去重，已瞬间完成秒传: ${rawFile.name}`)
      await loadData()
      return
    }

    // 预签名直传 MinIO
    if (reqResult.uploadUrl) {
      uploadPercent.value = 60
      uploadStatusText.value = '正在直传至 MinIO 对象存储...'

      try {
        const resp = await fetch(reqResult.uploadUrl, {
          method: reqResult.method || 'PUT',
          headers: reqResult.headers || {},
          body: rawFile
        })

        if (!resp.ok) {
          throw new Error(`MinIO 直传响应错误: ${resp.status}`)
        }

        uploadPercent.value = 85
        uploadStatusText.value = '校验文件完整性与落盘确认...'

        await completeUpload({
          attachmentId: reqResult.attachmentId,
          sha256: sha
        })

        uploadPercent.value = 100
        uploadStatusText.value = '上传成功！'
        ElMessage.success(`MinIO 直传完成并确认落盘: ${rawFile.name}`)
      } catch (directErr) {
        // 自动降级为服务端代理流式上传
        uploadStatusText.value = '直连受限，自动切换服务端流式代理兜底...'
        await uploadStream(rawFile, '测试附件', '')
        uploadPercent.value = 100
        uploadStatusText.value = '流式代理上传成功！'
        ElMessage.success(`通过服务端代理通道上传成功: ${rawFile.name}`)
      }
    }

    await loadData()
  } catch (err: any) {
    ElMessage.error(err.message || '上传失败')
  } finally {
    setTimeout(() => {
      uploading.value = false
      uploadPercent.value = 0
    }, 1500)
  }
}

async function handleMigrate() {
  try {
    await ElMessageBox.confirm(
      '将自动扫描 wwwroot/upload 历史附件目录并同步至 MinIO 存储桶，历史数据较大时可能需要数分钟，是否继续？',
      '提示',
      { confirmButtonText: '确定开始', cancelButtonText: '取消', type: 'warning' }
    )

    migrating.value = true
    const res = await migrateLegacyUploads()
    ElMessageBox.alert(
      `迁移扫描完成！\n总扫描: ${res.totalScanned}\n成功同步: ${res.totalMigrated}\n已跳过(已存在): ${res.totalSkipped}\n异常: ${res.totalErrors}`,
      '存量迁移报告'
    )
    await loadData()
  } catch (err: any) {
    if (err !== 'cancel') {
      ElMessage.error(err.message || '迁移失败')
    }
  } finally {
    migrating.value = false
  }
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.attachment-management {
  padding: 20px;
}

.header-card {
  margin-bottom: 20px;
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-content h2 {
  margin: 0 0 8px 0;
  font-size: 20px;
  color: #303133;
}

.subtitle {
  margin: 0;
  font-size: 13px;
  color: #909399;
}

.stat-row {
  margin-bottom: 20px;
}

.stat-card {
  border-left: 4px solid #409eff;
}

.stat-label {
  font-size: 13px;
  color: #909399;
  margin-bottom: 8px;
}

.stat-val {
  font-size: 22px;
  font-weight: bold;
  color: #303133;
  margin-bottom: 6px;
}

.stat-val.highlight {
  color: #409eff;
  font-size: 18px;
}

.stat-val.warn {
  color: #e6a23c;
}

.stat-desc {
  font-size: 12px;
  color: #c0c4cc;
}

.main-row {
  margin-bottom: 20px;
}

.card-title {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-weight: 600;
}

.upload-zone {
  padding: 10px 0;
}

.upload-progress {
  margin-top: 15px;
  font-size: 13px;
  color: #606266;
}

.audit-card {
  margin-top: 20px;
}
</style>
