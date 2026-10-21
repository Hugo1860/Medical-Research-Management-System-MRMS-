<template>
  <div class="office-notice-container">
    <el-card shadow="never" class="header-card">
      <div class="page-header">
        <div>
          <h2>科研办公与通知公告中心</h2>
          <p class="subtitle">统一发布全院科研项目申报指南、评审结果公示、科研管理规章制度及学术交流公告</p>
        </div>
        <div class="header-actions">
          <el-button type="primary" :icon="Plus" @click="handleOpenCreateNotice">
            发布科研通知公告
          </el-button>
        </div>
      </div>
    </el-card>

    <!-- KPI 状态摘要 -->
    <el-row :gutter="16" class="kpi-row">
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="kpi-card">
          <div class="kpi-label">已发布通知公告总数</div>
          <div class="kpi-value text-primary">{{ noticeTotal }} <span class="unit">条</span></div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="kpi-card">
          <div class="kpi-label">现行生效执行中</div>
          <div class="kpi-value text-success">{{ activeNoticeCount }} <span class="unit">条</span></div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="kpi-card">
          <div class="kpi-label">全院公开发布类</div>
          <div class="kpi-value text-warning">{{ publicNoticeCount }} <span class="unit">项</span></div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="kpi-card">
          <div class="kpi-label">附件资料齐备率</div>
          <div class="kpi-value text-purple">{{ attachmentRate }} <span class="unit">%</span></div>
        </el-card>
      </el-col>
    </el-row>

    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab">
        <!-- 标签页 1：通知公告 -->
        <el-tab-pane label="通知公告列表" name="notices">
          <el-form :inline="true" :model="noticeQuery" class="filter-form">
            <el-form-item label="公告标题">
              <el-input v-model="noticeQuery.like通知名称" placeholder="检索通知公告标题/关键字" clearable />
            </el-form-item>
            <el-form-item label="类型">
              <el-select v-model="noticeQuery.通知类型" placeholder="全部类型" clearable style="width: 140px">
                <el-option label="全部类型" value="" />
                <el-option label="公告" value="公告" />
                <el-option label="普通通知" value="普通通知" />
              </el-select>
            </el-form-item>
            <el-form-item label="发布人">
              <el-input v-model="noticeQuery.like发送人姓名" placeholder="发布人姓名" clearable style="width: 140px" />
            </el-form-item>
            <el-form-item label="生效状态">
              <el-select v-model="statusSelect" placeholder="全部状态" clearable style="width: 120px">
                <el-option label="全部" value="" />
                <el-option label="生效中" value="true" />
                <el-option label="已停用" value="false" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :icon="Search" @click="fetchNotices">查询</el-button>
              <el-button :icon="Refresh" @click="handleResetNoticeQuery">重置</el-button>
            </el-form-item>
          </el-form>

          <el-table v-loading="noticeLoading" :data="noticeList" border stripe style="width: 100%">
            <el-table-column type="index" label="序号" width="60" align="center" />
            <el-table-column prop="通知名称" label="公告标题" min-width="260">
              <template #default="{ row }">
                <div class="notice-title-cell">
                  <el-tag
                    :type="getNoticeTagType(row.通知类型)"
                    size="small"
                    style="margin-right: 8px; flex-shrink: 0;"
                  >
                    {{ row.通知类型 || '通知' }}
                  </el-tag>
                  <el-tag
                    v-if="row.通知标识 && row.通知标识 !== 'string'"
                    type="info"
                    size="small"
                    effect="plain"
                    style="margin-right: 8px; flex-shrink: 0;"
                  >
                    {{ row.通知标识 }}
                  </el-tag>
                  <el-link type="primary" :underline="false" @click="handleReadNotice(row)">
                    {{ row.通知名称 || row.标题 || '未命名公告' }}
                  </el-link>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="发送人姓名" label="发布人" width="130">
              <template #default="{ row }">
                <span>{{ row.发送人姓名 || row.发布人姓名 || '科研管理处' }}</span>
                <span v-if="row.发送人部门名称" class="dept-sub"> ({{ row.发送人部门名称 }})</span>
              </template>
            </el-table-column>
            <el-table-column prop="建立时间" label="发布日期" width="160">
              <template #default="{ row }">
                {{ formatDateTime(row.建立时间 || row.发布时间) }}
              </template>
            </el-table-column>
            <el-table-column label="接收情况" width="120" align="center">
              <template #default="{ row }">
                <span v-if="row.接收人数 !== undefined">
                  {{ row.已接收人数 || 0 }} / {{ row.接收人数 || 0 }} 人
                </span>
                <span v-else class="text-muted">全院人员</span>
              </template>
            </el-table-column>
            <el-table-column label="附件" width="80" align="center">
              <template #default="{ row }">
                <el-icon v-if="row.相关文件路径 && row.相关文件路径 !== 'string'" color="#409eff"><Paperclip /></el-icon>
                <span v-else class="text-muted">-</span>
              </template>
            </el-table-column>
            <el-table-column label="状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag v-if="row.是否启用" type="success" size="small">生效中</el-tag>
                <el-tag v-else type="info" size="small">已下线</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="160" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="handleReadNotice(row)">
                  阅读详情
                </el-button>
                <el-button
                  link
                  :type="row.是否启用 ? 'danger' : 'success'"
                  size="small"
                  @click="handleToggleStatus(row)"
                >
                  {{ row.是否启用 ? '下线' : '恢复' }}
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-container">
            <el-pagination
              v-model:current-page="noticePaging.pageIndex"
              v-model:page-size="noticePaging.pageSize"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              :total="noticeTotal"
              @size-change="fetchNotices"
              @current-change="fetchNotices"
            />
          </div>
        </el-tab-pane>

        <!-- 标签页 2：制度与资料下载直通 -->
        <el-tab-pane label="规章制度与资料下载直通" name="downloads">
          <div class="downloads-pane-header">
            <span class="pane-tip">查看常用科研政策法规、立项申报书模板及财务填报指南，支持一键跳转下载中心全量库</span>
            <el-button type="primary" link :icon="Right" @click="jumpToDownloadCenter">
              进入科研规章制度与下载中心完整工作台 &rarr;
            </el-button>
          </div>
          <el-table v-loading="downloadLoading" :data="downloadList" border stripe style="width: 100%">
            <el-table-column type="index" label="序号" width="60" align="center" />
            <el-table-column prop="文件名称" label="文件资料名称" min-width="260" show-overflow-tooltip />
            <el-table-column prop="文件类型" label="分类属性" width="140" align="center">
              <template #default="{ row }">
                <el-tag :type="row.文件类型 === '政策法规类' ? 'danger' : 'warning'" size="small">
                  {{ row.文件类型 || '普通文件类' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="上传人姓名" label="发布人" width="130" />
            <el-table-column prop="建立时间" label="更新时间" width="160">
              <template #default="{ row }">
                {{ formatDateTime(row.建立时间) }}
              </template>
            </el-table-column>
            <el-table-column label="操作" width="120" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="handleDownloadFile(row)">
                  下载文件
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 阅读公告抽屉 -->
    <el-drawer v-model="readDrawerVisible" title="通知公告详情" size="680px">
      <div v-if="currentNotice" class="notice-detail">
        <h3 class="detail-title">{{ currentNotice.通知名称 || currentNotice.标题 }}</h3>
        <div class="detail-meta">
          <span><el-icon><User /></el-icon> 发布人：{{ currentNotice.发送人姓名 || currentNotice.发布人姓名 || '科研管理处' }}</span>
          <span><el-icon><Calendar /></el-icon> 发布时间：{{ formatDateTime(currentNotice.建立时间 || currentNotice.发布时间) }}</span>
          <span v-if="currentNotice.关闭时间"><el-icon><Clock /></el-icon> 截止时间：{{ formatDateTime(currentNotice.关闭时间) }}</span>
        </div>
        <el-divider />
        <div class="detail-content" v-html="currentNotice.通知内容 || currentNotice.内容 || '无内容正文'"></div>

        <div v-if="noticeAttachments.length > 0" class="detail-attachment">
          <div class="attachment-header">
            <el-icon><Paperclip /></el-icon> <strong>附件材料列表 ({{ noticeAttachments.length }} 个)</strong>
          </div>
          <div v-for="(file, idx) in noticeAttachments" :key="idx" class="attachment-item">
            <el-link type="primary" :href="file" target="_blank" :icon="Download">
              {{ getFileName(file) }}
            </el-link>
          </div>
        </div>
      </div>
    </el-drawer>

    <!-- 发布公告弹窗 -->
    <el-dialog v-model="createDialogVisible" title="发布科研通知公告" width="720px">
      <el-form ref="createFormRef" :model="createForm" :rules="createRules" label-width="100px">
        <el-form-item label="公告标题" prop="通知名称">
          <el-input v-model="createForm.通知名称" placeholder="请输入通知公告全称（例如：关于申报2026年度国家自然科学基金的通知）" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="通知类型" prop="通知类型">
              <el-select v-model="createForm.通知类型" style="width: 100%">
                <el-option label="公告" value="公告" />
                <el-option label="普通通知" value="普通通知" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="业务分类">
              <el-input v-model="createForm.通知标识" placeholder="如：科研动态、规章制度、申报指南" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="有效截止时间">
              <el-date-picker
                v-model="createForm.关闭时间"
                type="datetime"
                placeholder="选择通知下线时间"
                format="YYYY-MM-DD HH:mm:ss"
                value-format="YYYY-MM-DD HH:mm:ss"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="生效状态">
              <el-switch v-model="createForm.是否启用" active-text="立即生效" inactive-text="暂存草稿" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="公告正文" prop="通知内容">
          <el-input
            v-model="createForm.通知内容"
            type="textarea"
            :rows="7"
            placeholder="支持输入正文说明，或包含简单 HTML 格式内容"
          />
        </el-form-item>
        <el-form-item label="附件路径">
          <el-input
            v-model="createForm.相关文件路径"
            placeholder="多份附件可使用英文逗号分隔（如：/upload/下载中心/sample.pdf）"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmitCreate">确认发布</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Plus, Search, Refresh, Paperclip, Download, User, Calendar, Clock, Right } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import {
  getNotices,
  getNoticeDetail,
  createNotice,
  toggleNoticeStatus,
  getDownloads,
  type NoticeItem,
  type DownloadItem,
  type NoticeFilter
} from '@/api/office'
import { formatDateTime } from '@/utils/format'

const route = useRoute()
const router = useRouter()

const activeTab = ref('notices')
const noticeLoading = ref(false)
const downloadLoading = ref(false)
const submitLoading = ref(false)

const noticeList = ref<NoticeItem[]>([])
const downloadList = ref<DownloadItem[]>([])
const noticeTotal = ref(0)
const currentNotice = ref<NoticeItem | null>(null)

const readDrawerVisible = ref(false)
const createDialogVisible = ref(false)

const noticePaging = reactive({ pageIndex: 1, pageSize: 10 })
const noticeQuery = reactive({
  like通知名称: '',
  like发送人姓名: '',
  通知类型: ''
})
const statusSelect = ref('')

const now = new Date()
const defaultCloseDate = `${now.getFullYear() + 2}-12-31 23:59:59`

const createForm = reactive({
  通知名称: '',
  通知类型: '公告',
  通知标识: '科研动态',
  通知内容: '',
  关闭时间: defaultCloseDate,
  相关文件路径: '',
  是否启用: true
})

const createRules = {
  通知名称: [{ required: true, message: '请输入公告标题', trigger: 'blur' }],
  通知内容: [{ required: true, message: '请输入公告正文', trigger: 'blur' }],
  通知类型: [{ required: true, message: '请选择通知类型', trigger: 'change' }]
}

// KPI 统计
const activeNoticeCount = computed(() => {
  return noticeList.value.filter(item => item.是否启用).length
})

const publicNoticeCount = computed(() => {
  return noticeList.value.filter(item => item.通知类型 === '公告').length
})

const attachmentRate = computed(() => {
  if (noticeList.value.length === 0) return '0'
  const hasAttach = noticeList.value.filter(item => item.相关文件路径 && item.相关文件路径 !== 'string').length
  return ((hasAttach / noticeList.value.length) * 100).toFixed(0)
})

const noticeAttachments = computed(() => {
  if (!currentNotice.value?.相关文件路径) return []
  return currentNotice.value.相关文件路径
    .split(',')
    .map(s => s.trim())
    .filter(s => s && s !== 'string')
})

function getNoticeTagType(type?: string): '' | 'success' | 'warning' | 'danger' | 'info' {
  if (type === '公告') return 'danger'
  if (type === '普通通知') return 'warning'
  return 'info'
}

function getFileName(filePath: string): string {
  const parts = filePath.replace(/\\/g, '/').split('/')
  return parts[parts.length - 1] || filePath
}

async function fetchNotices() {
  noticeLoading.value = true
  try {
    const filter: NoticeFilter = {
      like通知名称: noticeQuery.like通知名称 || undefined,
      like发送人姓名: noticeQuery.like发送人姓名 || undefined,
      通知类型: noticeQuery.通知类型 || undefined,
      是否启用: statusSelect.value !== '' ? statusSelect.value === 'true' : undefined
    }
    const res: any = await getNotices(noticePaging, filter)
    if (res && res.data) {
      noticeList.value = res.data.list || res.data || []
      noticeTotal.value = res.data.total || noticeList.value.length
    }
  } catch {
    // 降级使用空列表
  } finally {
    noticeLoading.value = false
  }
}

async function fetchDownloads() {
  downloadLoading.value = true
  try {
    const res: any = await getDownloads({ pageIndex: 1, pageSize: 10 }, { 是否启用: true })
    if (res && res.data) {
      downloadList.value = res.data.list || res.data || []
    }
  } catch {
    // 降级使用空列表
  } finally {
    downloadLoading.value = false
  }
}

function handleResetNoticeQuery() {
  noticeQuery.like通知名称 = ''
  noticeQuery.like发送人姓名 = ''
  noticeQuery.通知类型 = ''
  statusSelect.value = ''
  noticePaging.pageIndex = 1
  fetchNotices()
}

async function handleReadNotice(row: NoticeItem) {
  try {
    const res: any = await getNoticeDetail(row.编号)
    if (res && res.data) {
      currentNotice.value = res.data.通知公告 || res.data
    } else {
      currentNotice.value = row
    }
  } catch {
    currentNotice.value = row
  }
  readDrawerVisible.value = true
}

async function handleToggleStatus(row: NoticeItem) {
  try {
    await toggleNoticeStatus(row.编号, !row.是否启用, row.发送人编号)
    ElMessage.success(`公告 [${row.通知名称 || row.标题}] 状态已更新！`)
    fetchNotices()
  } catch (err: any) {
    ElMessage.error(err.message || '操作失败')
  }
}

function handleDownloadFile(row: DownloadItem) {
  if (row.文件路径) {
    ElMessage.success(`开始下载资料文档: [${row.文件名称}]`)
    window.open(row.文件路径, '_blank')
  } else {
    ElMessage.warning(`文档 [${row.文件名称}] 暂无下载链接`)
  }
}

function handleOpenCreateNotice() {
  createForm.通知名称 = ''
  createForm.通知内容 = ''
  createForm.通知类型 = '公告'
  createForm.通知标识 = '科研动态'
  createForm.相关文件路径 = ''
  createForm.关闭时间 = defaultCloseDate
  createForm.是否启用 = true
  createDialogVisible.value = true
}

async function handleSubmitCreate() {
  if (!createForm.通知名称 || !createForm.通知内容) {
    ElMessage.warning('请填写通知公告标题与正文内容')
    return
  }
  submitLoading.value = true
  try {
    await createNotice(createForm)
    ElMessage.success('通知公告已成功发布！')
    createDialogVisible.value = false
    fetchNotices()
  } catch (err: any) {
    ElMessage.error(err.message || '发布失败')
  } finally {
    submitLoading.value = false
  }
}

function jumpToDownloadCenter() {
  router.push('/office/downloads')
}

onMounted(() => {
  fetchNotices()
  fetchDownloads()

  if (route.query.action === 'add') {
    handleOpenCreateNotice()
  }
})
</script>

<style scoped lang="scss">
.office-notice-container {
  padding: 16px;

  .header-card {
    margin-bottom: 16px;
    background: #ffffff;

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
        font-weight: 600;

        .unit {
          font-size: 13px;
          font-weight: normal;
          color: #909399;
          margin-left: 4px;
        }
      }
      .text-primary { color: #409eff; }
      .text-success { color: #67c23a; }
      .text-warning { color: #e6a23c; }
      .text-purple { color: #8e44ad; }
    }
  }

  .main-card {
    background: #ffffff;

    .filter-form {
      margin-bottom: 16px;
    }

    .notice-title-cell {
      display: flex;
      align-items: center;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .dept-sub {
      font-size: 12px;
      color: #909399;
    }

    .pagination-container {
      margin-top: 16px;
      display: flex;
      justify-content: flex-end;
    }

    .downloads-pane-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
      padding: 10px 14px;
      background: #f8fafc;
      border-radius: 4px;

      .pane-tip {
        font-size: 13px;
        color: #606266;
      }
    }
  }

  .notice-detail {
    padding: 12px;

    .detail-title {
      margin: 0 0 12px 0;
      font-size: 20px;
      color: #303133;
      line-height: 1.4;
    }
    .detail-meta {
      display: flex;
      flex-wrap: wrap;
      gap: 16px;
      font-size: 13px;
      color: #909399;
    }
    .detail-content {
      line-height: 1.8;
      font-size: 15px;
      color: #303133;
      min-height: 200px;
      word-break: break-word;
    }
    .detail-attachment {
      margin-top: 24px;
      padding-top: 16px;
      border-top: 1px dashed #ebeef5;

      .attachment-header {
        margin-bottom: 12px;
        font-size: 14px;
        color: #303133;
      }

      .attachment-item {
        margin-bottom: 8px;
      }
    }
  }
}
</style>
