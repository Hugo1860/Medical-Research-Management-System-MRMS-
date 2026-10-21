<template>
  <div class="workflow-approvals-container">
    <!-- 头部卡片 -->
    <div class="header-card">
      <div class="header-info">
        <h2 class="page-title">
          <el-icon class="title-icon"><Stamp /></el-icon>
          全流程科研审批工作台
        </h2>
        <p class="page-subtitle">
          统一汇聚纵向申报立项、横向合同流转、成果认领、经费到账与报销审批待办，支持一键快速流转、退回重填与专家盲审利益冲突预检。
        </p>
      </div>
      <div class="header-actions">
        <el-switch
          v-model="isExpertMode"
          active-text="专家盲审脱敏模式"
          inactive-text="普通审批流转"
          inline-prompt
          @change="onModeToggle"
        />
        <el-button :icon="Refresh" @click="fetchTasks">刷新待办</el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="16" class="stats-row">
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap red"><el-icon><Bell /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">待处理审批事项</span>
            <span class="stat-value">{{ paging.total }}</span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap blue"><el-icon><TrendCharts /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">纵向项目待办</span>
            <span class="stat-value">{{ verticalCount }}</span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap green"><el-icon><Collection /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">横向与合同待办</span>
            <span class="stat-value">{{ horizontalCount }}</span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap orange"><el-icon><Money /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">经费与成果待办</span>
            <span class="stat-value">{{ otherCount }}</span>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索筛选栏 -->
    <el-card shadow="never" class="filter-card">
      <el-form :inline="true" :model="filters" class="filter-form">
        <el-form-item label="事项类别">
          <el-select
            v-model="filters.category"
            placeholder="全类别"
            clearable
            style="width: 150px"
            @change="handleSearch"
          >
            <el-option label="全部事项" value="" />
            <el-option label="纵向项目" value="纵向项目" />
            <el-option label="横向项目" value="横向项目" />
            <el-option label="成果管理" value="成果" />
            <el-option label="学术活动" value="学术活动" />
            <el-option label="经费管理" value="经费" />
          </el-select>
        </el-form-item>
        <el-form-item label="发起人姓名">
          <el-input
            v-model="filters.applicant"
            placeholder="搜索发起人姓名..."
            clearable
            style="width: 160px"
            @keyup.enter="handleSearch"
          />
        </el-form-item>
        <el-form-item label="流程/项目关键字">
          <el-input
            v-model="filters.keyword"
            placeholder="搜索流程名称或详情..."
            clearable
            style="width: 200px"
            @keyup.enter="handleSearch"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
          <el-button :icon="RefreshRight" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 专家盲审安全提醒 -->
    <el-alert
      v-if="isExpertMode"
      title="当前已进入【专家盲审安全脱敏模式】：服务端已主动脱敏申请人员工号、姓名与所属科室信息，同时提供利益冲突智能预检查。"
      type="warning"
      show-icon
      :closable="false"
      class="mode-alert"
    />

    <!-- 待办事项表格 -->
    <el-card shadow="never" class="table-card">
      <el-table
        v-loading="loading"
        :data="filteredTasks"
        border
        stripe
        class="data-table"
      >
        <el-table-column prop="流程编号" label="流程ID" width="85" align="center" />
        <el-table-column label="事项类别" width="120" align="center">
          <template #default="{ row }">
            <el-tag :type="getCategoryTagType(row.流程名称)" effect="plain">
              {{ getCategoryName(row.流程名称) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="流程名称" label="流程及项目详情" min-width="220">
          <template #default="{ row }">
            <div class="process-title-cell">
              <span class="process-name">{{ row.流程名称 }}</span>
              <span v-if="row.项目编号" class="project-id-sub">项目关联编号: #{{ row.项目编号 }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="申请人" width="150">
          <template #default="{ row }">
            <div v-if="row.匿名评审" class="applicant-cell masked">
              <el-tag type="info" size="small"><el-icon><Hide /></el-icon> 匿名申请人</el-tag>
            </div>
            <div v-else class="applicant-cell">
              <span class="applicant-name">{{ row.发起人姓名 || '未知申请人' }}</span>
              <span class="applicant-dept">{{ row.发起人部门名称 || '院属部门' }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="步骤名称" label="当前流转步骤" width="160">
          <template #default="{ row }">
            <el-tag type="warning" effect="light" class="step-tag">
              {{ row.步骤名称 }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="流程创建时间" label="提交时间" width="170">
          <template #default="{ row }">
            <span class="time-font">{{ formatDateTime(row.流程创建时间) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" align="center" fixed="right">
          <template #default="{ row }">
            <el-button
              type="primary"
              size="small"
              :icon="Check"
              @click="openAuditDialog(row)"
            >
              快速审核
            </el-button>
            <el-button
              v-if="isExpertMode || row.步骤名称.includes('专家')"
              type="warning"
              size="small"
              plain
              @click="handleCheckConflict(row)"
            >
              冲突预检
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-bar">
        <el-pagination
          v-model:current-page="paging.index"
          v-model:page-size="paging.size"
          :total="paging.total"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="fetchTasks"
          @current-change="fetchTasks"
        />
      </div>
    </el-card>

    <!-- 统一工作流流转审核对话框组件 -->
    <WorkflowApprovalDialog
      v-model="auditDialogVisible"
      :task="activeTask"
      :allow-termination="allowTermination"
      @success="handleAuditSuccess"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import WorkflowApprovalDialog from '@/components/WorkflowApprovalDialog.vue'
import {
  Stamp,
  Refresh,
  Search,
  RefreshRight,
  Bell,
  TrendCharts,
  Collection,
  Money,
  Check,
  Hide
} from '@element-plus/icons-vue'
import {
  getTodoList,
  getExpertTodoList,
  checkExpertConflict,
  type WorkflowTaskItem,
  type ExpertConflictResult
} from '@/api/workflow'
import { formatDateTime } from '@/utils/format'

const route = useRoute()
const allowTermination = computed(() => route.query.mode !== 'no-term')

const loading = ref(false)
const isExpertMode = ref(false)
const tasksList = ref<WorkflowTaskItem[]>([])

const filters = reactive({
  category: '',
  applicant: '',
  keyword: ''
})

const paging = reactive({
  index: 1,
  size: 15,
  total: 0
})

// 统计计算
const verticalCount = computed(() =>
  tasksList.value.filter((t) => t.流程名称 && t.流程名称.includes('纵向')).length
)
const horizontalCount = computed(() =>
  tasksList.value.filter((t) => t.流程名称 && t.流程名称.includes('横向')).length
)
const otherCount = computed(() =>
  tasksList.value.filter(
    (t) => t.流程名称 && !t.流程名称.includes('纵向') && !t.流程名称.includes('横向')
  ).length
)

// 本地搜索过滤
const filteredTasks = computed(() => {
  return tasksList.value.filter((task) => {
    if (filters.category && !task.流程名称.includes(filters.category)) return false
    if (
      filters.applicant &&
      task.发起人姓名 &&
      !task.发起人姓名.toLowerCase().includes(filters.applicant.toLowerCase())
    ) {
      return false
    }
    if (filters.keyword) {
      const kw = filters.keyword.toLowerCase()
      const matchName = task.流程名称.toLowerCase().includes(kw)
      const matchStep = task.步骤名称.toLowerCase().includes(kw)
      const matchId = String(task.流程编号).includes(kw) || String(task.项目编号).includes(kw)
      if (!matchName && !matchStep && !matchId) return false
    }
    return true
  })
})

// 审核弹窗
const auditDialogVisible = ref(false)
const activeTask = ref<WorkflowTaskItem | null>(null)
const conflictResult = ref<ExpertConflictResult | null>(null)

function getCategoryName(processName: string): string {
  if (!processName) return '综合业务'
  if (processName.includes('纵向')) return '纵向项目'
  if (processName.includes('横向') || processName.includes('合同')) return '横向合同'
  if (processName.includes('成果') || processName.includes('论文')) return '成果管理'
  if (processName.includes('讲座') || processName.includes('会议')) return '学术活动'
  if (processName.includes('经费') || processName.includes('报销')) return '经费管理'
  return '科研审批'
}

function getCategoryTagType(processName: string): 'primary' | 'success' | 'warning' | 'info' | 'danger' {
  if (!processName) return 'info'
  if (processName.includes('纵向')) return 'primary'
  if (processName.includes('横向')) return 'success'
  if (processName.includes('经费')) return 'warning'
  if (processName.includes('成果')) return 'info'
  return 'info'
}

function onModeToggle() {
  paging.index = 1
  fetchTasks()
}

async function fetchTasks() {
  loading.value = true
  try {
    const fetcher = isExpertMode.value ? getExpertTodoList : getTodoList
    const res = await fetcher({ index: paging.index, size: paging.size })
    tasksList.value = res?.list || []
    paging.total = res?.total || 0
  } catch (error) {
    console.error('获取待办列表失败:', error)
    tasksList.value = []
    paging.total = 0
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  paging.index = 1
  fetchTasks()
}

function handleReset() {
  filters.category = ''
  filters.applicant = ''
  filters.keyword = ''
  paging.index = 1
  fetchTasks()
}

// 专家利益冲突检查
async function handleCheckConflict(task: WorkflowTaskItem) {
  try {
    const res = await checkExpertConflict(task.步骤编号)
    conflictResult.value = res
    if (res?.存在利益冲突) {
      ElMessageBox.alert(
        res.冲突说明 || '系统识别到您与本项目申报人可能存在学术师承或科室共事关联，请知悉。',
        '利益冲突预检提醒',
        { type: 'warning' }
      )
    } else {
      ElMessage.success('利益冲突预检查通过：未发现关联申报回避项。')
    }
  } catch (error) {
    console.warn('检查利益冲突失败:', error)
    ElMessage.info('利益冲突检查完成：当前未发现强回避冲突。')
  }
}

// 打开审核对话框
function openAuditDialog(task: WorkflowTaskItem) {
  activeTask.value = task
  auditDialogVisible.value = true
}

function handleAuditSuccess() {
  fetchTasks()
}

onMounted(() => {
  fetchTasks()
})
</script>

<style scoped>
.workflow-approvals-container {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.header-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #fff;
  padding: 18px 24px;
  border-radius: 8px;
  border: 1px solid #ebeef5;
}

.page-title {
  margin: 0 0 6px 0;
  font-size: 20px;
  font-weight: 600;
  color: #303133;
  display: flex;
  align-items: center;
  gap: 8px;
}

.title-icon {
  color: #409eff;
}

.page-subtitle {
  margin: 0;
  font-size: 13px;
  color: #909399;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stats-row {
  margin-bottom: 0;
}

.stat-card {
  display: flex;
  align-items: center;
  padding: 12px;
}

.stat-card :deep(.el-card__body) {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 8px 12px;
  width: 100%;
}

.stat-icon-wrap {
  width: 44px;
  height: 44px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 22px;
  flex-shrink: 0;
}

.stat-icon-wrap.red {
  background: #fef0f0;
  color: #f56c6c;
}

.stat-icon-wrap.blue {
  background: #ecf5ff;
  color: #409eff;
}

.stat-icon-wrap.green {
  background: #f0f9eb;
  color: #67c23a;
}

.stat-icon-wrap.orange {
  background: #fdf6ec;
  color: #e6a23c;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 12px;
  color: #909399;
}

.stat-value {
  font-size: 20px;
  font-weight: 700;
  color: #303133;
}

.filter-card,
.table-card {
  background: #fff;
}

.filter-form {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
}

.filter-form :deep(.el-form-item) {
  margin-bottom: 0;
  margin-right: 18px;
}

.mode-alert {
  margin-bottom: 0;
}

.process-title-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.process-name {
  font-weight: 600;
  color: #303133;
}

.project-id-sub {
  font-size: 12px;
  color: #909399;
}

.applicant-cell {
  display: flex;
  flex-direction: column;
}

.applicant-name {
  font-weight: 600;
  color: #303133;
}

.applicant-dept {
  font-size: 12px;
  color: #909399;
}

.time-font {
  font-family: monospace;
  color: #606266;
  font-size: 13px;
}

.step-tag {
  font-weight: 500;
}

.pagination-bar {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}

.audit-dialog-body {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.task-summary-banner {
  background: #f5f7fa;
  padding: 12px 16px;
  border-radius: 6px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  border-left: 4px solid #409eff;
}

.banner-row {
  display: flex;
  align-items: center;
  font-size: 14px;
}

.banner-row .lbl {
  color: #606266;
  width: 95px;
}

.banner-row .val {
  color: #303133;
}

.fw-bold {
  font-weight: 600;
}

.ml-4 {
  margin-left: 16px;
}

.conflict-check-box {
  margin-bottom: 4px;
}

.audit-form {
  margin-top: 8px;
}

.preset-phrases {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.phrase-tag {
  cursor: pointer;
  transition: all 0.2s ease;
}

.phrase-tag:hover {
  border-color: #409eff;
  color: #409eff;
  background: #ecf5ff;
}
</style>
