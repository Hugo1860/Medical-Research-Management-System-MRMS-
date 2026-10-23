<template>
  <div class="page-container">
    <el-card shadow="never" class="main-card">
      <div class="header-section">
        <div>
          <h2 class="page-title">继续医学教育（CME）学分管理</h2>
          <p class="page-subtitle">
            基层与二级医院科教科核心业务：支持I类/II类继教学分申报与审核、院内讲座扫码授分、年度达标进度跟踪与智能催办。
          </p>
        </div>
        <div class="header-actions">
          <el-button type="primary" :icon="Plus" @click="openApplyDialog">
            申报个人学分
          </el-button>
        </div>
      </div>

      <el-tabs v-model="activeTab" class="cme-tabs" @tab-change="onTabChange">
        <!-- 标签 1：我的学分档案 -->
        <el-tab-pane label="我的学分" name="my">
          <!-- 学分达标看板卡片 -->
          <div class="stats-overview" v-if="summary">
            <el-card shadow="hover" class="stat-card">
              <div class="stat-label">年度总学分</div>
              <div class="stat-value" :class="{ 'is-qualified': summary.已认定总分 >= summary.要求总分 }">
                {{ summary.已认定总分 }} <span class="stat-unit">/ {{ summary.要求总分 }}分</span>
              </div>
              <el-progress
                :percentage="Math.min(100, Math.round((summary.已认定总分 / summary.要求总分) * 100))"
                :status="summary.已认定总分 >= summary.要求总分 ? 'success' : undefined"
                style="margin-top: 8px;"
              />
              <div class="stat-tip">
                <el-tag size="small" :type="summary.是否达标 ? 'success' : 'danger'">
                  {{ summary.是否达标 ? '已达标' : `还差 ${summary.总分差额} 分达标` }}
                </el-tag>
              </div>
            </el-card>

            <el-card shadow="hover" class="stat-card">
              <div class="stat-label">I 类学分（硬性要求）</div>
              <div class="stat-value" :class="{ 'is-qualified': summary.已认定I类分 >= summary.要求I类分 }">
                {{ summary.已认定I类分 }} <span class="stat-unit">/ {{ summary.要求I类分 }}分</span>
              </div>
              <el-progress
                :percentage="Math.min(100, Math.round((summary.已认定I类分 / summary.要求I类分) * 100))"
                :status="summary.已认定I类分 >= summary.要求I类分 ? 'success' : 'exception'"
                style="margin-top: 8px;"
              />
              <div class="stat-tip">
                <span v-if="summary.已认定I类分 >= summary.要求I类分" style="color: #67C23A; font-size: 12px;">I类分已满足</span>
                <span v-else style="color: #F56C6C; font-size: 12px;">I类缺口: {{ summary.I类差额 }} 分</span>
              </div>
            </el-card>

            <el-card shadow="hover" class="stat-card">
              <div class="stat-label">II 类学分已认定</div>
              <div class="stat-value" style="color: #409EFF;">
                {{ summary.已认定II类分 }} <span class="stat-unit">分</span>
              </div>
              <div style="font-size: 12px; color: #909399; margin-top: 14px;">
                主要来源于院内主办学术讲座、自修与院内交流
              </div>
            </el-card>

            <el-card shadow="hover" class="stat-card">
              <div class="stat-label">待审核学分</div>
              <div class="stat-value" style="color: #E6A23C;">
                {{ summary.待审核学分 }} <span class="stat-unit">分</span>
              </div>
              <div style="font-size: 12px; color: #909399; margin-top: 14px;">
                科教科已收到申报，请耐心等待集中认定
              </div>
            </el-card>
          </div>

          <div class="table-header" style="margin-top: 20px;">
            <div style="font-weight: 600; font-size: 15px;">{{ currentYear }}年度学分明细记录</div>
            <el-button :icon="Refresh" size="small" @click="loadMyCredits">刷新</el-button>
          </div>

          <el-table :data="summary?.明细列表 || []" border stripe style="width: 100%; margin-top: 10px;">
            <el-table-column prop="项目名称" label="继教项目 / 讲座名称" min-width="220" show-overflow-tooltip />
            <el-table-column prop="学分类别" label="学分类别" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="row.学分类别 === 'I类' ? 'primary' : 'success'" size="small">
                  {{ row.学分类别 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="学分" label="学分值" width="90" align="right">
              <template #default="{ row }">
                <span style="font-weight: 600; color: #303133;">{{ row.学分 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="来源" label="学分来源" width="130" align="center">
              <template #default="{ row }">
                <el-tag effect="plain" size="small">{{ row.来源 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="授予单位" label="授予单位" min-width="160" show-overflow-tooltip />
            <el-table-column prop="获得日期" label="获得日期" width="110">
              <template #default="{ row }">
                {{ row.获得日期 ? row.获得日期.substring(0, 10) : '-' }}
              </template>
            </el-table-column>
            <el-table-column prop="审核状态" label="状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="row.审核状态 === '已认定' ? 'success' : row.审核状态 === '已驳回' ? 'danger' : 'warning'" size="small">
                  {{ row.审核状态 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="审核意见" label="审核意见" min-width="140" show-overflow-tooltip />
            <el-table-column label="操作" width="100" fixed="right">
              <template #default="{ row }">
                <el-button
                  v-if="row.审核状态 === '待审核'"
                  link type="danger" size="small"
                  @click="handleWithdraw(row)"
                >
                  撤回
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签 2：讲座签到 -->
        <el-tab-pane label="讲座签到" name="signin">
          <div class="signin-box">
            <el-card shadow="never" style="max-width: 520px; margin: 20px auto; border-radius: 8px;">
              <template #header>
                <div style="font-weight: 600; font-size: 16px; text-align: center;">
                  院内学术讲座签到授分
                </div>
              </template>
              <div style="text-align: center; margin-bottom: 20px;">
                <p style="color: #606266; font-size: 14px;">
                  请在讲座现场扫码，或输入主持人公布的 6-32 位<b>签到令牌 (Token)</b>完成签到：
                </p>
                <el-input
                  v-model="inputToken"
                  placeholder="请输入或粘贴签到令牌..."
                  size="large"
                  clearable
                  style="margin-bottom: 16px;"
                />
                <el-button type="primary" size="large" :loading="signingIn" style="width: 100%;" @click="handleSignIn">
                  立即签到授分
                </el-button>
              </div>

              <el-alert
                title="温馨提示"
                type="info"
                description="讲座签到需在设定的有效时间窗口内完成；签到成功后，科教科将在讲座结束核准后统一授分并计入个人学分档案。"
                :closable="false"
              />
            </el-card>
          </div>
        </el-tab-pane>

        <!-- 标签 3：学分审核（科教科管理员） -->
        <el-tab-pane label="学分审核" name="audit">
          <div class="toolbar">
            <el-input
              v-model="auditQuery.关键词"
              placeholder="搜索项目名称、申请人或工号..."
              clearable
              style="width: 280px;"
              @keyup.enter="loadPendingAudits"
            />
            <el-button type="primary" :icon="Search" @click="loadPendingAudits">查询</el-button>
            <el-button
              type="success"
              :disabled="selectedAuditIds.length === 0"
              @click="handleBatchAudit(true)"
            >
              批量通过 ({{ selectedAuditIds.length }})
            </el-button>
            <el-button
              type="danger"
              :disabled="selectedAuditIds.length === 0"
              @click="handleBatchAudit(false)"
            >
              批量驳回
            </el-button>
          </div>

          <el-table
            :data="pendingAudits"
            border stripe
            v-loading="auditLoading"
            style="width: 100%; margin-top: 14px;"
            @selection-change="onAuditSelectionChange"
          >
            <el-table-column type="selection" width="45" />
            <el-table-column prop="申请人姓名" label="申请人" width="120">
              <template #default="{ row }">
                <div>{{ row.申请人姓名 }}</div>
                <div style="font-size: 11px; color: #909399;">{{ row.部门名称 }}</div>
              </template>
            </el-table-column>
            <el-table-column prop="项目名称" label="申报项目名称" min-width="220" show-overflow-tooltip />
            <el-table-column prop="学分类别" label="类别" width="80" align="center">
              <template #default="{ row }">
                <el-tag size="small">{{ row.学分类别 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="学分" label="申报分值" width="90" align="right">
              <template #default="{ row }">
                <span style="font-weight: 600;">{{ row.学分 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="授予单位" label="授予单位" min-width="160" show-overflow-tooltip />
            <el-table-column prop="证书附件路径" label="凭证附件" width="100" align="center">
              <template #default="{ row }">
                <el-button v-if="row.证书附件路径" link type="primary" size="small" @click="openAttachment(row.证书附件路径)">
                  查看
                </el-button>
                <span v-else style="color: #909399; font-size: 12px;">无</span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="140" fixed="right">
              <template #default="{ row }">
                <el-button link type="success" size="small" @click="handleSingleAudit(row.编号, true)">通过</el-button>
                <el-button link type="danger" size="small" @click="handleSingleAudit(row.编号, false)">驳回</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签 4：年度达标统计与催办 -->
        <el-tab-pane label="全院达标统计与催办" name="stats">
          <div v-if="annualStats">
            <div class="stats-overview" style="margin-bottom: 20px;">
              <el-card shadow="hover" class="stat-card">
                <div class="stat-label">在岗卫技总人数</div>
                <div class="stat-value">{{ annualStats.总卫技人数 }} <span class="stat-unit">人</span></div>
              </el-card>
              <el-card shadow="hover" class="stat-card">
                <div class="stat-label">达标总人数</div>
                <div class="stat-value is-qualified">{{ annualStats.达标总人数 }} <span class="stat-unit">人</span></div>
              </el-card>
              <el-card shadow="hover" class="stat-card">
                <div class="stat-label">未达标人数</div>
                <div class="stat-value" style="color: #F56C6C;">{{ annualStats.未达标总人数 }} <span class="stat-unit">人</span></div>
              </el-card>
              <el-card shadow="hover" class="stat-card">
                <div class="stat-label">全院达标率</div>
                <div class="stat-value" style="color: #409EFF;">{{ annualStats.全院达标率 }}%</div>
                <el-progress :percentage="annualStats.全院达标率" :status="annualStats.全院达标率 >= 90 ? 'success' : undefined" />
              </el-card>
            </div>

            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px;">
              <h4 style="margin: 0;">未达标人员名单（共 {{ annualStats.未达标总人数 }} 人）</h4>
              <div style="display: flex; gap: 10px;">
                <el-button
                  type="warning"
                  :disabled="selectedRemindIds.length === 0"
                  @click="handleSendBatchRemind"
                >
                  批量发送催办通知 ({{ selectedRemindIds.length }})
                </el-button>
                <el-button :icon="Download" @click="handleExportCsv">
                  导出统计报表 (CSV)
                </el-button>
              </div>
            </div>

            <el-table
              :data="annualStats.未达标人员名单 || []"
              border stripe size="small"
              max-height="400"
              @selection-change="onRemindSelectionChange"
            >
              <el-table-column type="selection" width="45" />
              <el-table-column prop="工号" label="工号" width="100" />
              <el-table-column prop="姓名" label="姓名" width="110" />
              <el-table-column prop="部门名称" label="科室" min-width="140" />
              <el-table-column prop="职称" label="职称" width="100" />
              <el-table-column prop="I类分" label="已获I类分" width="90" align="right" />
              <el-table-column prop="总分" label="已获总分" width="90" align="right" />
              <el-table-column label="达标要求" width="140">
                <template #default>
                  总分≥25 (I类≥10)
                </template>
              </el-table-column>
            </el-table>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 个人学分申报弹窗 -->
    <el-dialog v-model="applyDialogVisible" title="申报继续教育学分" width="600px">
      <el-form ref="applyFormRef" :model="applyModel" :rules="applyRules" label-width="110px">
        <el-form-item label="项目名称" prop="项目名称">
          <el-input v-model="applyModel.项目名称" placeholder="如: 省级急危重症护理新进展培训班" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="学分类别" prop="学分类别">
              <el-select v-model="applyModel.学分类别" style="width: 100%;">
                <el-option label="I类学分" value="I类" />
                <el-option label="II类学分" value="II类" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="学分值" prop="学分">
              <el-input-number v-model="applyModel.学分" :min="0.5" :max="25" :step="0.5" style="width: 100%;" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="授予单位" prop="授予单位">
          <el-input v-model="applyModel.授予单位" placeholder="如: 省医学会 / 国家卫健委" />
        </el-form-item>
        <el-form-item label="获得日期" prop="获得日期">
          <el-date-picker v-model="applyModel.获得日期" type="date" placeholder="选择结业或获得学分日期" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="凭证证书附件">
          <el-input v-model="applyModel.证书附件路径" placeholder="可上传或填写附件URL" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="applyDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="savingApply" @click="confirmApply">提交申报</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { Plus, Refresh, Search, Download } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  getMyCmeCredits,
  applyCmeCredit,
  withdrawCmeApply,
  auditCmeCredits,
  getPendingCmeAudits,
  signInLecture,
  getCmeAnnualStats,
  sendBatchCmeRemind,
  type MyCmeSummary
} from '@/api/cme-credits'

const route = useRoute()
const activeTab = ref('my')
const currentYear = ref(new Date().getFullYear())

// 1. 我的学分
const summary = ref<MyCmeSummary | null>(null)

async function loadMyCredits() {
  try {
    const res = await getMyCmeCredits()
    if (res) summary.value = res as any
  } catch {}
}

// 2. 签到逻辑
const inputToken = ref('')
const signingIn = ref(false)

async function handleSignIn() {
  if (!inputToken.value.trim()) {
    ElMessage.warning('请输入签到令牌')
    return
  }
  signingIn.value = true
  try {
    const res: any = await signInLecture(inputToken.value.trim())
    ElMessage.success(res?.message || '签到成功！')
    inputToken.value = ''
    loadMyCredits()
  } finally {
    signingIn.value = false
  }
}

// 3. 学分审核
const pendingAudits = ref<any[]>([])
const auditLoading = ref(false)
const selectedAuditIds = ref<number[]>([])
const auditQuery = reactive({ pageIndex: 1, pageSize: 20, 关键词: '' })

async function loadPendingAudits() {
  auditLoading.value = true
  try {
    const res: any = await getPendingCmeAudits(auditQuery)
    if (res) pendingAudits.value = res.list || []
  } finally {
    auditLoading.value = false
  }
}

function onAuditSelectionChange(selection: any[]) {
  selectedAuditIds.value = selection.map(s => s.编号)
}

async function handleSingleAudit(id: number, pass: boolean) {
  await auditCmeCredits({ 编号列表: [id], 通过: pass })
  ElMessage.success(pass ? '已认定' : '已驳回')
  loadPendingAudits()
}

async function handleBatchAudit(pass: boolean) {
  if (!selectedAuditIds.value.length) return
  await auditCmeCredits({ 编号列表: selectedAuditIds.value, 通过: pass })
  ElMessage.success(`批量处理完成`)
  loadPendingAudits()
}

// 4. 统计与催办
const annualStats = ref<any>(null)
const selectedRemindIds = ref<number[]>([])

async function loadAnnualStats() {
  const res: any = await getCmeAnnualStats({ 年度: currentYear.value })
  if (res) annualStats.value = res
}

function onRemindSelectionChange(selection: any[]) {
  selectedRemindIds.value = selection.map(s => s.人员编号)
}

async function handleSendBatchRemind() {
  if (!selectedRemindIds.value.length) return
  await ElMessageBox.confirm(`确定向选中的 ${selectedRemindIds.value.length} 位未达标人员推送系统催办通知吗？`, '催办确认', {
    type: 'warning'
  })
  await sendBatchCmeRemind({
    年度: currentYear.value,
    人员编号列表: selectedRemindIds.value
  })
  ElMessage.success('催办通知已批量发送')
}

function handleExportCsv() {
  window.open(`/api/CmeCredit/导出达标统计CSV?年度=${currentYear.value}`, '_blank')
}

// 申报弹窗
const applyDialogVisible = ref(false)
const savingApply = ref(false)
const applyFormRef = ref()
const applyModel = reactive({
  项目名称: '',
  学分类别: 'II类',
  学分: 1.0,
  授予单位: '',
  获得日期: '',
  证书附件路径: ''
})
const applyRules = {
  项目名称: [{ required: true, message: '请输入项目名称', trigger: 'blur' }],
  授予单位: [{ required: true, message: '请输入授予单位', trigger: 'blur' }]
}

function openApplyDialog() {
  Object.assign(applyModel, {
    项目名称: '',
    学分类别: 'II类',
    学分: 1.0,
    授予单位: '',
    获得日期: '',
    证书附件路径: ''
  })
  applyDialogVisible.value = true
}

async function confirmApply() {
  await applyFormRef.value?.validate()
  savingApply.value = true
  try {
    await applyCmeCredit(applyModel as any)
    ElMessage.success('学分申报已提交')
    applyDialogVisible.value = false
    loadMyCredits()
  } finally {
    savingApply.value = false
  }
}

async function handleWithdraw(row: any) {
  await withdrawCmeApply(row.编号)
  ElMessage.success('已撤回')
  loadMyCredits()
}

function openAttachment(path: string) {
  window.open(path, '_blank')
}

function onTabChange(tab: string) {
  if (tab === 'my') loadMyCredits()
  else if (tab === 'audit') loadPendingAudits()
  else if (tab === 'stats') loadAnnualStats()
}

onMounted(() => {
  if (route.query.signin) {
    inputToken.value = String(route.query.signin)
    activeTab.value = 'signin'
    handleSignIn()
  } else {
    loadMyCredits()
  }
})
</script>

<style scoped lang="scss">
.page-container {
  padding: 16px;
  background-color: #f0f2f5;
  min-height: calc(100vh - 84px);
}

.main-card {
  border-radius: 8px;
}

.header-section {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 16px;
  border-bottom: 1px solid #ebeef5;
  padding-bottom: 12px;

  .page-title {
    font-size: 20px;
    font-weight: 600;
    color: #303133;
    margin: 0 0 6px 0;
  }

  .page-subtitle {
    font-size: 13px;
    color: #909399;
    margin: 0;
  }
}

.stats-overview {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
  margin-top: 10px;

  .stat-card {
    border-radius: 8px;

    .stat-label {
      font-size: 13px;
      color: #909399;
      margin-bottom: 8px;
    }

    .stat-value {
      font-size: 26px;
      font-weight: 700;
      color: #303133;

      &.is-qualified {
        color: #67c23a;
      }

      .stat-unit {
        font-size: 13px;
        font-weight: normal;
        color: #909399;
      }
    }

    .stat-tip {
      margin-top: 8px;
      display: flex;
      justify-content: flex-end;
    }
  }
}

.table-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.toolbar {
  display: flex;
  gap: 12px;
  align-items: center;
}
</style>
