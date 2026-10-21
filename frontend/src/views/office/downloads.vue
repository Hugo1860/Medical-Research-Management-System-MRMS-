<template>
  <div class="downloads-container">
    <el-card shadow="never" class="header-card">
      <div class="page-header">
        <div>
          <h2>科研规章制度与资料下载中心</h2>
          <p class="subtitle">统一归口发布科技政策、申报指南、经费管理办法、伦理审查规范及官方申报书表格模板</p>
        </div>
        <div class="header-actions">
          <el-button :icon="Download" @click="handleExportCsv">
            导出清单
          </el-button>
          <el-button type="primary" :icon="Plus" @click="handleOpenAddDialog">
            上传发布新文件
          </el-button>
        </div>
      </div>
    </el-card>

    <!-- KPI 统计卡片 -->
    <el-row :gutter="16" class="kpi-row">
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="kpi-card">
          <div class="kpi-label">全院制度与模板总数</div>
          <div class="kpi-value text-primary">{{ totalDownloads }} <span class="unit">份</span></div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="kpi-card">
          <div class="kpi-label">国家与地方政策法规</div>
          <div class="kpi-value text-danger">{{ policyCount }} <span class="unit">部</span></div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="kpi-card">
          <div class="kpi-label">项目申报与表格模板</div>
          <div class="kpi-value text-warning">{{ templateCount }} <span class="unit">套</span></div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="kpi-card">
          <div class="kpi-label">现行生效有效比例</div>
          <div class="kpi-value text-success">{{ activeRate }} <span class="unit">%</span></div>
        </el-card>
      </el-col>
    </el-row>

    <el-card shadow="never" class="main-card">
      <!-- 过滤表单 -->
      <el-form :inline="true" :model="queryForm" class="filter-form">
        <el-form-item label="文件名称">
          <el-input v-model="queryForm.like文件名称" placeholder="输入文件全称或关键字" clearable />
        </el-form-item>
        <el-form-item label="文件类型">
          <el-select v-model="queryForm.like文件类型" placeholder="全部类型" clearable style="width: 160px">
            <el-option label="全部类型" value="" />
            <el-option label="政策法规类" value="政策法规类" />
            <el-option label="申报模板类" value="申报模板类" />
            <el-option label="财务经费类" value="财务经费类" />
            <el-option label="伦理审查类" value="伦理审查类" />
            <el-option label="普通文件类" value="普通文件类" />
          </el-select>
        </el-form-item>
        <el-form-item label="启用状态">
          <el-select v-model="queryForm.statusString" placeholder="全部状态" clearable style="width: 130px">
            <el-option label="全部" value="" />
            <el-option label="正常生效" value="true" />
            <el-option label="已停用" value="false" />
          </el-select>
        </el-form-item>
        <el-form-item label="上传发布人">
          <el-input v-model="queryForm.like上传人姓名" placeholder="发布人姓名" clearable style="width: 140px" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
          <el-button :icon="Refresh" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <!-- 文件列表表格 -->
      <el-table v-loading="loading" :data="downloadList" border stripe style="width: 100%">
        <el-table-column type="index" label="序号" width="60" align="center" />
        <el-table-column prop="文件名称" label="文件资料名称" min-width="260">
          <template #default="{ row }">
            <div class="file-name-cell">
              <el-icon class="file-icon"><Document /></el-icon>
              <el-link type="primary" :underline="false" @click="handleOpenDetail(row)">
                {{ row.文件名称 }}
              </el-link>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="文件类型" label="分类属性" width="130" align="center">
          <template #default="{ row }">
            <el-tag :type="getTypeTag(row.文件类型)" size="small">
              {{ row.文件类型 || '普通文件类' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.是否启用" type="success" size="small">正常</el-tag>
            <el-tag v-else type="info" size="small">已禁用</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="上传人姓名" label="发布人" width="120" />
        <el-table-column prop="上传人部门名称" label="归属部门" width="160" show-overflow-tooltip />
        <el-table-column prop="建立时间" label="发布/更新日期" width="160">
          <template #default="{ row }">
            {{ formatDateTime(row.建立时间) }}
          </template>
        </el-table-column>
        <el-table-column prop="备注" label="说明与适用范围" min-width="180" show-overflow-tooltip />
        <el-table-column label="操作" width="220" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleOpenDetail(row)">
              详情
            </el-button>
            <el-button link type="success" size="small" @click="handleDownload(row)">
              下载
            </el-button>
            <el-button link type="primary" size="small" @click="handleEdit(row)">
              编辑
            </el-button>
            <el-button
              link
              :type="row.是否启用 ? 'danger' : 'warning'"
              size="small"
              @click="handleToggleStatus(row)"
            >
              {{ row.是否启用 ? '停用' : '启用' }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页栏 -->
      <div class="pagination-container">
        <el-pagination
          v-model:current-page="paging.pageIndex"
          v-model:page-size="paging.pageSize"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next, jumper"
          :total="totalDownloads"
          @size-change="fetchDownloads"
          @current-change="fetchDownloads"
        />
      </div>
    </el-card>

    <!-- 资料详情抽屉 -->
    <el-drawer v-model="detailDrawerVisible" title="科研制度与下载文件详情" size="600px">
      <div v-if="selectedItem" class="detail-container">
        <el-descriptions title="基础元数据信息" :column="1" border>
          <el-descriptions-item label="文件全称">
            <strong>{{ selectedItem.文件名称 }}</strong>
          </el-descriptions-item>
          <el-descriptions-item label="所属类别">
            <el-tag :type="getTypeTag(selectedItem.文件类型)" size="small">
              {{ selectedItem.文件类型 }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="发布状态">
            <el-tag v-if="selectedItem.是否启用" type="success" size="small">生效启用中</el-tag>
            <el-tag v-else type="info" size="small">已停用下架</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="上传发布人">
            {{ selectedItem.上传人姓名 || '科研管理处' }}
          </el-descriptions-item>
          <el-descriptions-item label="发布科室/部门">
            {{ selectedItem.上传人部门名称 || '科技处' }}
          </el-descriptions-item>
          <el-descriptions-item label="发布日期">
            {{ formatDateTime(selectedItem.建立时间) }}
          </el-descriptions-item>
          <el-descriptions-item label="文件存储标识">
            <code>{{ selectedItem.文件路径 || '未绑定云端存储' }}</code>
          </el-descriptions-item>
          <el-descriptions-item label="文档说明/备注">
            <p class="remarks-box">{{ selectedItem.备注 || '暂无特别说明' }}</p>
          </el-descriptions-item>
        </el-descriptions>

        <div class="detail-actions">
          <el-button type="success" :icon="Download" @click="handleDownload(selectedItem)">
            立即下载文档
          </el-button>
          <el-button type="primary" :icon="Edit" @click="handleEdit(selectedItem)">
            编辑文件资料
          </el-button>
          <el-button
            :type="selectedItem.是否启用 ? 'danger' : 'warning'"
            @click="handleToggleStatus(selectedItem)"
          >
            {{ selectedItem.是否启用 ? '下线停用' : '恢复启用' }}
          </el-button>
        </div>
      </div>
    </el-drawer>

    <!-- 新增 / 编辑下载文件弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEditMode ? '编辑下载文件资料' : '上传发布新科研文件'"
      width="640px"
      :close-on-click-modal="false"
    >
      <el-form ref="formRef" :model="formData" :rules="formRules" label-width="110px">
        <el-form-item label="文件全称" prop="文件名称">
          <el-input v-model="formData.文件名称" placeholder="输入文件官方标题全称" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="文件类别" prop="文件类型">
              <el-select v-model="formData.文件类型" placeholder="选择文件属性" style="width: 100%">
                <el-option label="政策法规类" value="政策法规类" />
                <el-option label="申报模板类" value="申报模板类" />
                <el-option label="财务经费类" value="财务经费类" />
                <el-option label="伦理审查类" value="伦理审查类" />
                <el-option label="普通文件类" value="普通文件类" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="生效状态">
              <el-switch v-model="formData.是否启用" active-text="正常启用" inactive-text="暂存禁用" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="文件附件" prop="文件路径">
          <div class="upload-wrapper">
            <el-input v-model="formData.文件路径" placeholder="文件上传后自动生成相对路径，也可手动填写">
              <template #append>
                <el-upload
                  action=""
                  :show-file-list="false"
                  :http-request="customUploadFile"
                >
                  <el-button type="primary">选择本地文件</el-button>
                </el-upload>
              </template>
            </el-input>
          </div>
        </el-form-item>
        <el-form-item label="说明与备注">
          <el-input
            v-model="formData.备注"
            type="textarea"
            :rows="3"
            placeholder="填写适用学科、面向科室或申报批次等说明"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          {{ isEditMode ? '保存修改' : '确认发布' }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { Download, Plus, Search, Refresh, Document, Edit } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox, type FormInstance } from 'element-plus'
import {
  getDownloads,
  saveDownload,
  toggleDownloadStatus,
  uploadDownloadFile,
  type DownloadItem,
  type DownloadFilter
} from '@/api/office'
import { formatDateTime } from '@/utils/format'
import { exportToCsv, type ExportColumn } from '@/utils/export'

const route = useRoute()

const loading = ref(false)
const submitLoading = ref(false)
const downloadList = ref<DownloadItem[]>([])
const totalDownloads = ref(0)

const paging = reactive({ pageIndex: 1, pageSize: 10 })
const queryForm = reactive({
  like文件名称: '',
  like文件类型: '',
  statusString: '',
  like上传人姓名: ''
})

const detailDrawerVisible = ref(false)
const selectedItem = ref<DownloadItem | null>(null)

const dialogVisible = ref(false)
const isEditMode = ref(false)
const formRef = ref<FormInstance>()
const formData = reactive<DownloadItem>({
  编号: undefined,
  文件名称: '',
  文件类型: '普通文件类',
  文件路径: '',
  是否启用: true,
  备注: ''
})

const formRules = {
  文件名称: [{ required: true, message: '请输入文件全称', trigger: 'blur' }],
  文件类型: [{ required: true, message: '请选择文件类型', trigger: 'change' }],
  文件路径: [{ required: true, message: '请上传或提供文件相对路径', trigger: 'blur' }]
}

// 统计分析计算属性
const policyCount = computed(() => {
  return downloadList.value.filter(item => item.文件类型 === '政策法规类').length
})

const templateCount = computed(() => {
  return downloadList.value.filter(item => item.文件类型 === '申报模板类').length
})

const activeRate = computed(() => {
  if (downloadList.value.length === 0) return '100'
  const activeCount = downloadList.value.filter(item => item.是否启用).length
  return ((activeCount / downloadList.value.length) * 100).toFixed(1)
})

function getTypeTag(type?: string): 'primary' | 'success' | 'warning' | 'danger' | 'info' {
  switch (type) {
    case '政策法规类':
      return 'danger'
    case '申报模板类':
      return 'warning'
    case '财务经费类':
      return 'success'
    case '伦理审查类':
      return 'primary'
    default:
      return 'info'
  }
}

async function fetchDownloads() {
  loading.value = true
  try {
    const filter: DownloadFilter = {
      like文件名称: queryForm.like文件名称 || undefined,
      like文件类型: queryForm.like文件类型 || undefined,
      like上传人姓名: queryForm.like上传人姓名 || undefined,
      是否启用: queryForm.statusString !== '' ? queryForm.statusString === 'true' : undefined
    }
    const res: any = await getDownloads(paging, filter)
    if (res && res.data) {
      downloadList.value = res.data.list || res.data || []
      totalDownloads.value = res.data.total || downloadList.value.length
    }
  } catch {
    // 降级为空
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  paging.pageIndex = 1
  fetchDownloads()
}

function handleReset() {
  queryForm.like文件名称 = ''
  queryForm.like文件类型 = ''
  queryForm.statusString = ''
  queryForm.like上传人姓名 = ''
  paging.pageIndex = 1
  fetchDownloads()
}

function handleOpenDetail(row: DownloadItem) {
  selectedItem.value = row
  detailDrawerVisible.value = true
}

function handleDownload(row: DownloadItem) {
  if (!row.文件路径) {
    ElMessage.warning(`文件 [${row.文件名称}] 暂无有效下载链接`)
    return
  }
  ElMessage.success(`开始下载 [${row.文件名称}]`)
  window.open(row.文件路径, '_blank')
}

function handleOpenAddDialog() {
  isEditMode.value = false
  formData.编号 = undefined
  formData.文件名称 = ''
  formData.文件类型 = '申报模板类'
  formData.文件路径 = ''
  formData.是否启用 = true
  formData.备注 = ''
  dialogVisible.value = true
}

function handleEdit(row: DownloadItem) {
  isEditMode.value = true
  formData.编号 = row.编号
  formData.文件名称 = row.文件名称
  formData.文件类型 = row.文件类型 || '普通文件类'
  formData.文件路径 = row.文件路径 || ''
  formData.是否启用 = row.是否启用 ?? true
  formData.备注 = row.备注 || ''
  dialogVisible.value = true
}

async function customUploadFile(options: any) {
  try {
    const res: any = await uploadDownloadFile(options.file)
    const filePaths = res?.data || res
    if (Array.isArray(filePaths) && filePaths.length > 0) {
      formData.文件路径 = filePaths[0]
      if (!formData.文件名称) {
        formData.文件名称 = options.file.name
      }
      ElMessage.success('文件上传成功！')
    } else {
      formData.文件路径 = `/upload/下载中心/${options.file.name}`
      ElMessage.success('文件已就绪')
    }
  } catch (err: any) {
    ElMessage.error(err.message || '上传文件失败')
  }
}

async function handleSubmit() {
  if (!formRef.value) return
  await formRef.value.validate(async valid => {
    if (!valid) return
    submitLoading.value = true
    try {
      await saveDownload(formData)
      ElMessage.success(isEditMode.value ? '修改成功！' : '文件发布成功！')
      dialogVisible.value = false
      fetchDownloads()
    } catch (err: any) {
      ElMessage.error(err.message || '提交失败')
    } finally {
      submitLoading.value = false
    }
  })
}

async function handleToggleStatus(row: DownloadItem) {
  const nextStatus = !row.是否启用
  const actionText = nextStatus ? '启用' : '禁用下线'
  try {
    await ElMessageBox.confirm(
      `确定要【${actionText}】文件资料《${row.文件名称}》吗？`,
      '状态调整确认',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: nextStatus ? 'info' : 'warning'
      }
    )
    await toggleDownloadStatus({
      编号: row.编号 || 0,
      是否启用: nextStatus,
      上传人编号: row.上传人编号
    })
    ElMessage.success(`已成功${actionText}！`)
    row.是否启用 = nextStatus
  } catch {
    // 用户取消
  }
}

function handleExportCsv() {
  if (downloadList.value.length === 0) {
    ElMessage.warning('当前列表无数据可导出')
    return
  }
  const columns: ExportColumn[] = [
    { label: '编号', prop: '编号' },
    { label: '文件名称', prop: '文件名称' },
    { label: '文件类型', prop: '文件类型' },
    { label: '状态', prop: '是否启用', formatter: (val: any) => (val ? '正常' : '已禁用') },
    { label: '发布人', prop: '上传人姓名' },
    { label: '归属部门', prop: '上传人部门名称' },
    { label: '发布时间', prop: '建立时间', formatter: (val: any) => formatDateTime(val) },
    { label: '说明与备注', prop: '备注' }
  ]
  exportToCsv('科研制度与下载中心资料清单', columns, downloadList.value)
  ElMessage.success('已导出 CSV 清单！')
}

watch(
  () => route.query.action,
  (action) => {
    if (action === 'add') {
      handleOpenAddDialog()
    }
  }
)

onMounted(() => {
  fetchDownloads()
  if (route.query.action === 'add') {
    handleOpenAddDialog()
  }
})
</script>

<style scoped lang="scss">
.downloads-container {
  .header-card {
    margin-bottom: 16px;
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;

      h2 {
        margin: 0 0 6px 0;
        font-size: 18px;
        color: #303133;
      }
      .subtitle {
        margin: 0;
        font-size: 13px;
        color: #909399;
      }

      .header-actions {
        display: flex;
        gap: 12px;
      }
    }
  }

  .kpi-row {
    margin-bottom: 16px;
    .kpi-card {
      .kpi-label {
        font-size: 13px;
        color: #909399;
        margin-bottom: 8px;
      }
      .kpi-value {
        font-size: 24px;
        font-weight: 700;
        .unit {
          font-size: 14px;
          font-weight: normal;
          color: #909399;
        }
      }
      .text-primary { color: #409eff; }
      .text-danger { color: #f56c6c; }
      .text-warning { color: #e6a23c; }
      .text-success { color: #67c23a; }
    }
  }

  .main-card {
    .filter-form {
      margin-bottom: 16px;
    }

    .file-name-cell {
      display: flex;
      align-items: center;
      gap: 6px;
      .file-icon {
        color: #409eff;
        font-size: 16px;
      }
    }

    .pagination-container {
      margin-top: 16px;
      display: flex;
      justify-content: flex-end;
    }
  }

  .detail-container {
    .remarks-box {
      margin: 0;
      color: #606266;
      line-height: 1.5;
    }
    .detail-actions {
      margin-top: 24px;
      display: flex;
      gap: 12px;
    }
  }

  .upload-wrapper {
    width: 100%;
  }
}
</style>
