<template>
  <div class="horizontal-contract-container">
    <el-card shadow="never" class="header-card">
      <div class="page-header">
        <div>
          <h2>横向科研项目与产学研合同全周期管理</h2>
          <p class="subtitle">覆盖横向课题申报、企业合作方资信、产学研合同登记、技术交易认定、变更与结题归档</p>
        </div>
        <el-button type="primary" :icon="Plus" @click="handleOpenApplyDialog">
          新增横向项目立项申请
        </el-button>
      </div>
    </el-card>

    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab">
        <!-- 标签页 1：横向项目与合同列表 -->
        <el-tab-pane label="横向项目台账" name="projects">
          <!-- 检索过滤栏 -->
          <el-form :inline="true" :model="queryForm" class="filter-form">
            <el-form-item label="合同编号">
              <el-input v-model="queryForm.like合同编号" placeholder="模糊检索合同编号" clearable />
            </el-form-item>
            <el-form-item label="合同名称">
              <el-input v-model="queryForm.like合同名称" placeholder="模糊检索合同或课题名称" clearable />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
              <el-button :icon="Refresh" @click="handleReset">重置</el-button>
            </el-form-item>
          </el-form>

          <!-- 汇总指标栏 -->
          <el-row :gutter="16" class="stats-row">
            <el-col :span="6">
              <div class="stat-box">
                <div class="stat-label">横向项目总数</div>
                <div class="stat-value">{{ totalCount }} 项</div>
              </div>
            </el-col>
            <el-col :span="6">
              <div class="stat-box">
                <div class="stat-label">合同总金额</div>
                <div class="stat-value highlight">¥ {{ formatMoney(totalContractAmount) }}</div>
              </div>
            </el-col>
            <el-col :span="6">
              <div class="stat-box">
                <div class="stat-label">技术交易认定数</div>
                <div class="stat-value">{{ certifiedCount }} 项</div>
              </div>
            </el-col>
            <el-col :span="6">
              <div class="stat-box">
                <div class="stat-label">在研进行中</div>
                <div class="stat-value success">{{ ongoingCount }} 项</div>
              </div>
            </el-col>
          </el-row>

          <!-- 表格 -->
          <el-table v-loading="loading" :data="projectList" border stripe style="width: 100%">
            <el-table-column prop="合同编号" label="合同编号" width="160" />
            <el-table-column prop="合同名称" label="合同/项目名称" min-width="200" show-overflow-tooltip />
            <el-table-column prop="合作方单位名称" label="合作方/委托单位" min-width="180" show-overflow-tooltip />
            <el-table-column prop="项目负责人姓名" label="负责人" width="110">
              <template #default="{ row }">
                {{ row.项目负责人姓名 || ('工号: ' + row.项目负责人编号) }}
              </template>
            </el-table-column>
            <el-table-column prop="合同金额" label="合同金额 (元)" width="150" align="right">
              <template #default="{ row }">
                <span class="money-text">¥ {{ formatMoney(row.合同金额) }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="签订时间" label="签订日期" width="120">
              <template #default="{ row }">
                {{ formatDate(row.签订时间) }}
              </template>
            </el-table-column>
            <el-table-column label="合同认定" width="100" align="center">
              <template #default="{ row }">
                <el-tag v-if="row.是否申请技术合同认定" type="success" size="small">已认定</el-tag>
                <el-tag v-else type="info" size="small">未认定</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="140" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="handleViewDetail(row)">
                  详情画像
                </el-button>
                <el-button link type="warning" size="small" @click="handleApplyChange(row)">
                  变更申请
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-container">
            <el-pagination
              v-model:current-page="paging.pageIndex"
              v-model:page-size="paging.pageSize"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              :total="totalCount"
              @size-change="fetchProjects"
              @current-change="fetchProjects"
            />
          </div>
        </el-tab-pane>

        <!-- 标签页 2：横向项目变更追踪 -->
        <el-tab-pane label="合同变更留痕" name="changes">
          <el-table v-loading="changeLoading" :data="changeList" border stripe style="width: 100%">
            <el-table-column prop="合同编号" label="合同编号" width="160" />
            <el-table-column prop="合同名称" label="合同名称" min-width="200" show-overflow-tooltip />
            <el-table-column prop="变更事由" label="变更事由" min-width="240" show-overflow-tooltip />
            <el-table-column prop="变更时间" label="变更发起时间" width="160">
              <template #default="{ row }">
                {{ formatDateTime(row.变更时间) }}
              </template>
            </el-table-column>
            <el-table-column label="变更审核状态" width="120" align="center">
              <template #default="{ row }">
                <el-tag :type="getStatusTag(row.状态)" size="small">
                  {{ getStatusText(row.状态) }}
                </el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签页 3：结题与成果验收 -->
        <el-tab-pane label="项目结题归档" name="closures">
          <el-table v-loading="closureLoading" :data="closureList" border stripe style="width: 100%">
            <el-table-column prop="合同编号" label="合同编号" width="160" />
            <el-table-column prop="合同名称" label="合同名称" min-width="200" show-overflow-tooltip />
            <el-table-column prop="结题时间" label="申请结题日期" width="130">
              <template #default="{ row }">
                {{ formatDate(row.结题时间) }}
              </template>
            </el-table-column>
            <el-table-column prop="验收结果" label="验收结论" min-width="180">
              <template #default="{ row }">
                <el-tag v-if="row.验收结果" type="success" size="small">{{ row.验收结果 }}</el-tag>
                <span v-else class="empty-state">未提供验收结论</span>
              </template>
            </el-table-column>
            <el-table-column label="归档留存" width="120" align="center">
              <template #default="{ row }">
                <el-tag v-if="row.归档状态" type="info" size="small">{{ row.归档状态 }}</el-tag>
                <span v-else class="empty-state">未提供档案状态</span>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="合同治理" name="governance">
          <el-form :inline="true" class="filter-form">
            <el-form-item label="选择项目">
              <el-select v-model="governanceProjectId" filterable clearable placeholder="选择横向项目" style="width: 360px">
                <el-option v-for="item in projectList" :key="item.编号" :label="`${item.合同编号 || item.编号} · ${item.合同名称 || ''}`" :value="item.编号" />
              </el-select>
            </el-form-item>
            <el-form-item><el-button type="primary" :disabled="!governanceProjectId" @click="fetchGovernance">查询治理记录</el-button></el-form-item>
          </el-form>
          <el-alert title="签审、合作方资质和收入认定状态均来自数据库；未取得正式供应商回执时不会显示为已完成。" type="info" :closable="false" show-icon />
          <el-divider content-position="left">合同版本与签审</el-divider>
          <el-table :data="governance.合同版本" border stripe>
            <el-table-column prop="版本号" label="版本" width="80" />
            <el-table-column prop="版本类型" label="类型" width="110" />
            <el-table-column prop="签审状态" label="签审状态" width="110" />
            <el-table-column prop="文件校验和" label="校验和" min-width="180" show-overflow-tooltip />
            <el-table-column prop="变更摘要" label="变更摘要" min-width="220" show-overflow-tooltip />
          </el-table>
          <el-divider content-position="left">合作方资质</el-divider>
          <el-table :data="governance.合作方资质" border stripe>
            <el-table-column prop="单位名称" label="单位名称" min-width="180" />
            <el-table-column prop="资质类型" label="资质类型" width="140" />
            <el-table-column prop="有效期至" label="有效期至" width="120" />
            <el-table-column prop="核验状态" label="核验状态" width="110" />
          </el-table>
          <el-divider content-position="left">收入/技术合同认定</el-divider>
          <el-table :data="governance.收入认定" border stripe>
            <el-table-column prop="认定类型" label="认定类型" width="140" />
            <el-table-column prop="申请金额" label="申请金额" width="130" />
            <el-table-column prop="认定金额" label="认定金额" width="130" />
            <el-table-column prop="状态" label="状态" width="110" />
            <el-table-column prop="认定编号" label="认定编号" min-width="160" />
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 详情抽屉 -->
    <el-drawer v-model="detailDrawerVisible" title="横向科研项目与合同详情" size="650px">
      <div v-if="currentProject" class="project-detail">
        <el-descriptions title="合同基本信息" :column="2" border>
          <el-descriptions-item label="合同编号">{{ currentProject.合同编号 }}</el-descriptions-item>
          <el-descriptions-item label="合同名称">{{ currentProject.合同名称 }}</el-descriptions-item>
          <el-descriptions-item label="合作方单位" :span="2">{{ currentProject.合作方单位名称 }}</el-descriptions-item>
          <el-descriptions-item label="合同总金额">
            <span class="money-text">¥ {{ formatMoney(currentProject.合同金额) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="技术交易额">
            ¥ {{ formatMoney(currentProject.合同技术交易额 || 0) }}
          </el-descriptions-item>
          <el-descriptions-item label="合作方式">{{ currentProject.合作方式 || '未提供合作方式' }}</el-descriptions-item>
          <el-descriptions-item label="技术领域">{{ currentProject.技术领域 || '未提供技术领域' }}</el-descriptions-item>
          <el-descriptions-item label="合同起止日期" :span="2">
            {{ formatDate(currentProject.合同开始时间) }} 至 {{ formatDate(currentProject.合同结束时间) }}
          </el-descriptions-item>
          <el-descriptions-item label="知识产权归属" :span="2">
            {{ currentProject.知识产权 || '未提供知识产权归属' }}
          </el-descriptions-item>
        </el-descriptions>

        <div style="margin-top: 24px">
          <h4>产学研经费执行与合同节点</h4>
          <el-progress
            v-if="expenseProgress !== null"
            :percentage="expenseProgress"
            :stroke-width="18"
            status="success"
            :text-inside="true"
            style="margin: 12px 0 20px"
          />
          <el-empty v-else :image-size="56" description="尚未取得真实经费执行数据" />
          <el-alert
            title="合规提示：横向项目经费预算总额须与实际合同签署金额严格一致，到账后方可按合同付款节点进行直接经费开支核销。"
            type="info"
            :closable="false"
            show-icon
          />
        </div>
      </div>
    </el-drawer>

    <!-- 新增申请向导弹窗 -->
    <el-dialog v-model="applyDialogVisible" title="新增横向项目立项申请" width="750px">
      <el-form ref="applyFormRef" :model="applyForm" :rules="applyRules" label-width="130px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="合同编号" prop="合同编号">
              <el-input v-model="applyForm.合同编号" placeholder="如: HX-2026-001" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="合同名称" prop="合同名称">
              <el-input v-model="applyForm.合同名称" placeholder="请输入合同或项目全称" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="合作方单位" prop="合作方单位名称">
              <el-input v-model="applyForm.合作方单位名称" placeholder="企业/事业单位名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="合同金额 (元)" prop="合同金额">
              <el-input-number v-model="applyForm.合同金额" :min="0" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="合作方式">
              <el-select v-model="applyForm.合作方式" placeholder="选择合作类型" style="width: 100%">
                <el-option label="委托开发" value="委托开发" />
                <el-option label="合作研发" value="合作研发" />
                <el-option label="技术转让/许可" value="技术转让" />
                <el-option label="技术咨询与服务" value="技术咨询" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="技术合同认定">
              <el-switch v-model="applyForm.是否申请技术合同认定" active-text="申请登记" inactive-text="不申请" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="合同开始日期">
              <el-date-picker v-model="applyForm.合同开始时间" type="date" placeholder="起始日期" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="合同终止日期">
              <el-date-picker v-model="applyForm.合同结束时间" type="date" placeholder="截止日期" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="知识产权归属">
          <el-input v-model="applyForm.知识产权" type="textarea" :rows="2" placeholder="约定双方知识产权与专利归属条款" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="applyDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmitApply">
          提交立项审核
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { Plus, Search, Refresh } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import {
  getHorizontalProjects,
  getHorizontalProjectChanges,
  getHorizontalProjectClosures,
  createHorizontalProject,
  type HorizontalProjectItem,
  type HorizontalProjectChangeItem,
  type HorizontalProjectClosureItem
} from '@/api/horizontal-project'
import { getContractGovernance } from '@/api/horizontal-contract-governance'
import dayjs from 'dayjs'

const activeTab = ref('projects')
const loading = ref(false)
const changeLoading = ref(false)
const closureLoading = ref(false)
const submitLoading = ref(false)

const projectList = ref<HorizontalProjectItem[]>([])
const changeList = ref<HorizontalProjectChangeItem[]>([])
const closureList = ref<HorizontalProjectClosureItem[]>([])
const currentProject = ref<HorizontalProjectItem | null>(null)
const detailDrawerVisible = ref(false)
const applyDialogVisible = ref(false)
const governanceProjectId = ref<number | null>(null)
const governance = reactive<{ 合同版本: any[]; 合作方资质: any[]; 收入认定: any[] }>({ 合同版本: [], 合作方资质: [], 收入认定: [] })

const paging = reactive({
  pageIndex: 1,
  pageSize: 10
})

const queryForm = reactive({
  like合同编号: '',
  like合同名称: ''
})

const totalCount = ref(0)

const applyForm = reactive({
  合同编号: '',
  合同名称: '',
  合作方单位名称: '',
  合同金额: null as unknown as number,
  合作方式: '委托开发',
  是否申请技术合同认定: true,
  合同开始时间: null as unknown as Date,
  合同结束时间: null as unknown as Date,
  知识产权: ''
})

const applyRules = {
  合同编号: [{ required: true, message: '请输入合同编号', trigger: 'blur' }],
  合同名称: [{ required: true, message: '请输入合同名称', trigger: 'blur' }],
  合作方单位名称: [{ required: true, message: '请输入合作方单位名称', trigger: 'blur' }],
  合同金额: [{ required: true, message: '请输入合同金额', trigger: 'blur' }]
}

// 统计计算
const totalContractAmount = computed(() => {
  return projectList.value.reduce((sum, item) => sum + (Number(item.合同金额) || 0), 0)
})

const certifiedCount = computed(() => {
  return projectList.value.filter(item => item.是否申请技术合同认定).length
})

const ongoingCount = computed(() => {
  return projectList.value.length
})

// 只有服务端返回批准预算/实际支出后才显示进度；缺失数据时显示空状态，避免用固定百分比误导用户。
const expenseProgress = computed<number | null>(() => {
  const project = currentProject.value as (HorizontalProjectItem & {
    批准预算?: number
    已审核支出?: number
    已付款金额?: number
  }) | null
  if (!project) return null
  const total = Number(project.批准预算)
  const spent = Number(project.已审核支出 ?? project.已付款金额)
  if (!Number.isFinite(total) || total <= 0 || !Number.isFinite(spent) || spent < 0) return null
  return Math.min(100, Math.max(0, Math.round((spent / total) * 100)))
})

function formatMoney(val: number | string | undefined) {
  if (val === undefined || val === null) return '0.00'
  return Number(val).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatDate(val: string | undefined) {
  if (!val) return '-'
  return dayjs(val).format('YYYY-MM-DD')
}

function formatDateTime(val: string | undefined) {
  if (!val) return '-'
  return dayjs(val).format('YYYY-MM-DD HH:mm')
}

function getStatusTag(status: any): 'success' | 'warning' | 'info' | 'danger' {
  if (status === 1 || status === '通过') return 'success'
  if (status === 0 || status === '待审') return 'warning'
  return 'info'
}

function getStatusText(status: any) {
  if (status === 1 || status === '通过') return '审核通过'
  if (status === 0 || status === '待审') return '流程流转中'
  return '已留存'
}

async function fetchProjects() {
  loading.value = true
  try {
    const res = await getHorizontalProjects(paging, queryForm)
    if (res && res.data) {
      projectList.value = res.data.list || res.data || []
      totalCount.value = res.data.total || projectList.value.length
    }
  } catch (err: any) {
    ElMessage.error(err.message || '获取横向项目列表失败')
  } finally {
    loading.value = false
  }
}

async function fetchChanges() {
  changeLoading.value = true
  try {
    const res = await getHorizontalProjectChanges({ pageIndex: 1, pageSize: 20 })
    if (res && res.data) {
      changeList.value = res.data.list || res.data || []
    }
  } catch {
    // 降级使用空列表
  } finally {
    changeLoading.value = false
  }
}

async function fetchClosures() {
  closureLoading.value = true
  try {
    const res = await getHorizontalProjectClosures({ pageIndex: 1, pageSize: 20 })
    if (res && res.data) {
      closureList.value = res.data.list || res.data || []
    }
  } catch {
    // 降级使用空列表
  } finally {
    closureLoading.value = false
  }
}

async function fetchGovernance() {
  if (!governanceProjectId.value) return
  try {
    const res = await getContractGovernance(governanceProjectId.value)
    const payload = res?.data || res || {}
    governance.合同版本 = payload.合同版本 || []
    governance.合作方资质 = payload.合作方资质 || []
    governance.收入认定 = payload.收入认定 || []
  } catch (err: any) {
    ElMessage.error(err.message || '获取合同治理记录失败')
  }
}

function handleSearch() {
  paging.pageIndex = 1
  fetchProjects()
}

function handleReset() {
  queryForm.like合同编号 = ''
  queryForm.like合同名称 = ''
  paging.pageIndex = 1
  fetchProjects()
}

function handleViewDetail(row: HorizontalProjectItem) {
  currentProject.value = row
  detailDrawerVisible.value = true
}

function handleApplyChange(row: HorizontalProjectItem) {
  ElMessage.info(`发起合同 [${row.合同编号}] 的变更申请流程`)
}

function handleOpenApplyDialog() {
  applyDialogVisible.value = true
}

async function handleSubmitApply() {
  submitLoading.value = true
  try {
    await createHorizontalProject({
      基本资料: {
        合同编号: applyForm.合同编号,
        合同名称: applyForm.合同名称,
        合作方单位名称: applyForm.合作方单位名称,
        合同金额: applyForm.合同金额,
        合作方式: applyForm.合作方式,
        是否申请技术合同认定: applyForm.是否申请技术合同认定,
        合同开始时间: dayjs(applyForm.合同开始时间).toISOString(),
        合同结束时间: dayjs(applyForm.合同结束时间).toISOString(),
        知识产权: applyForm.知识产权
      },
      参与人列表: [],
      经费预算列表: [],
      isHold: false
    })
    ElMessage.success('横向项目立项申请已成功提交审核流程！')
    applyDialogVisible.value = false
    fetchProjects()
  } catch (err: any) {
    ElMessage.error(err.message || '提交失败')
  } finally {
    submitLoading.value = false
  }
}

onMounted(() => {
  fetchProjects()
  fetchChanges()
  fetchClosures()
})
</script>

<style scoped lang="scss">
.horizontal-contract-container {
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
    }
  }

  .main-card {
    .filter-form {
      margin-bottom: 16px;
    }

    .stats-row {
      margin-bottom: 16px;

      .stat-box {
        background: #f8f9fa;
        border-radius: 6px;
        padding: 12px 16px;
        border: 1px solid #ebeef5;

        .stat-label {
          font-size: 13px;
          color: #909399;
          margin-bottom: 4px;
        }

        .stat-value {
          font-size: 20px;
          font-weight: 600;
          color: #303133;

          &.highlight {
            color: #409eff;
          }
          &.success {
            color: #67c23a;
          }
        }
      }
    }

    .money-text {
      font-weight: 600;
      color: #e6a23c;
    }

    .pagination-container {
      margin-top: 16px;
      display: flex;
      justify-content: flex-end;
    }
  }
}
</style>
