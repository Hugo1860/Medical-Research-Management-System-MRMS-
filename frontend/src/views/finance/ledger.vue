<template>
  <div class="page-container">
    <el-card shadow="never" class="main-card">
      <!-- 顶部项目概要与概览 -->
      <div class="project-header">
        <div class="header-left">
          <h2>{{ currentProjectName }}</h2>
          <p class="header-meta">
            项目立项编号：待选择 &nbsp;|&nbsp; 负责人：待确认 &nbsp;|&nbsp;
            归口学科：待确认 &nbsp;|&nbsp; 预算周期：待确认
          </p>
        </div>
        <div class="header-right">
          <el-button type="primary" :icon="Plus" @click="openLedgerDialog">登记经费支出</el-button>
          <el-button :icon="Refresh" @click="fetchLedgers">刷新台账</el-button>
        </div>
      </div>

      <!-- 核心经费四柱卡片 -->
      <el-row :gutter="16" class="finance-cards">
        <el-col :xs="12" :sm="6">
          <div class="f-card f-blue">
            <span class="f-label">总批复预算</span>
            <span class="f-val">¥{{ formatMoney(totalBudget) }}</span>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="f-card f-green">
            <span class="f-label">累计经费到账</span>
            <span class="f-val">¥{{ formatMoney(totalArrival) }}</span>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="f-card f-orange">
            <span class="f-label">已审核报销支出</span>
            <span class="f-val">¥{{ formatMoney(totalSpent) }}</span>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="f-card f-purple">
            <span class="f-label">可用预算净余额</span>
            <span class="f-val">¥{{ formatMoney(totalAvailable) }}</span>
          </div>
        </el-col>
      </el-row>

      <el-tabs v-model="activeTab" style="margin-top: 20px;">
        <!-- 标签页 1：分层科目明细台账 -->
        <el-tab-pane label="科目分层明细台账" name="ledger">
          <el-table
            v-loading="loading"
            :data="ledgerList"
            stripe
            style="width: 100%;"
            row-key="编号"
            default-expand-all
          >
            <el-table-column prop="科目层级" label="科目层级" width="120" />
            <el-table-column prop="科目名称" label="预算支出科目名称" min-width="160">
              <template #default="{ row }">
                <span style="font-weight: 600; color: #1e293b;">{{ row.科目名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column label="批复预算" width="130" align="right">
              <template #default="{ row }">
                <span>¥{{ formatMoney(row.批复预算) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="已报销支出" width="130" align="right">
              <template #default="{ row }">
                <span style="color: #e6a23c; font-weight: 600;">¥{{ formatMoney(row.已支出金额) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="可用预算余额" width="130" align="right">
              <template #default="{ row }">
                <span style="color: #67c23a; font-weight: 600;">¥{{ formatMoney(row.可用余额) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="预算执行率" min-width="180">
              <template #default="{ row }">
                <el-progress
                  :percentage="getExecutionRate(row.已支出金额, row.批复预算)"
                  :status="getRateStatus(row.已支出金额, row.批复预算)"
                />
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签页 2：合同约定付款节点 -->
        <el-tab-pane label="合同付款里程碑节点" name="payment">
          <el-table :data="paymentNodes" stripe style="width: 100%;">
            <el-table-column prop="节点名称" label="里程碑付款节点" min-width="160" />
            <el-table-column label="约定比例" width="110" align="center">
              <template #default="{ row }">
                <el-tag size="small">{{ row.约定比例 }}%</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="约定金额" width="130" align="right">
              <template #default="{ row }">
                <span>¥{{ formatMoney(row.约定金额) }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="计划付款日期" label="计划执行时间" width="130" />
            <el-table-column label="付款状态" width="110" align="center">
              <template #default="{ row }">
                <el-tag :type="row.是否已付款 ? 'success' : 'warning'" size="small">
                  {{ row.是否已付款 ? '已到账/付款' : '未满足节点' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="实际付款日期" label="实际到账凭据日期" width="140" />
          </el-table>
        </el-tab-pane>

        <!-- 标签页 3：到账批次预算维护与执行 (文献实践: 双模式预算控制) -->
        <el-tab-pane label="到账批次预算维护与执行" name="arrivalBudget">
          <div class="arrival-budget-box">
            <div class="mode-bar">
              <div class="mode-left">
                <span class="mode-label">预算管理模式：</span>
                <el-radio-group v-model="budgetMode" size="default" @change="onBudgetModeChange">
                  <el-radio-button value="按项目立项">模式一：按项目立项维护预算</el-radio-button>
                  <el-radio-button value="按到账金额">模式二：按到账金额维护预算 (清华长庚医院模式)</el-radio-button>
                </el-radio-group>
              </div>
              <div class="mode-right">
                <el-button type="primary" :icon="Plus" @click="addArrivalBudgetRow">添加预算科目</el-button>
                <el-button type="success" :icon="Check" :loading="savingBudgetLoading" @click="handleSaveArrivalBudget">保存批次预算分配</el-button>
                <el-button :icon="Refresh" @click="fetchArrivalBudgets">刷新批次预算</el-button>
              </div>
            </div>

            <!-- 批次选择卡片 -->
            <el-card shadow="never" class="batch-selector-card" style="margin-top: 14px; background: #f8fafc;">
              <el-row :gutter="16" align="middle">
                <el-col :xs="24" :sm="10">
                  <span style="font-weight: 600; margin-right: 8px;">选择到账批次：</span>
                  <el-select v-model="selectedArrivalId" placeholder="选择批次" style="width: 260px;" @change="onArrivalChange">
                    <el-option
                      v-for="item in arrivalBatches"
                      :key="item.id"
                      :label="item.label"
                      :value="item.id"
                    />
                  </el-select>
                </el-col>
                <el-col :xs="24" :sm="14">
                  <div class="batch-summary-chips">
                    <el-tag type="info" size="large">批次到账：¥{{ formatMoney(currentBatchAmount) }}</el-tag>
                    <el-tag type="primary" size="large">批次已分配：¥{{ formatMoney(currentBatchAllocated) }}</el-tag>
                    <el-tag :type="currentBatchDiff === 0 ? 'success' : 'danger'" size="large">
                      差额：¥{{ formatMoney(currentBatchDiff) }}
                      <span v-if="currentBatchDiff === 0">（已足额合规匹配）</span>
                      <span v-else>（未平衡）</span>
                    </el-tag>
                  </div>
                </el-col>
              </el-row>
            </el-card>

            <!-- 预算列表表格 -->
            <el-table
              v-loading="arrivalBudgetLoading"
              :data="arrivalBudgetRows"
              stripe
              style="width: 100%; margin-top: 14px;"
            >
              <el-table-column label="预算科目名称" min-width="180">
                <template #default="{ row }">
                  <el-input v-model="row.预算科目名称" placeholder="输入或选择预算科目" />
                </template>
              </el-table-column>
              <el-table-column label="批复 / 分配金额 (元)" width="200" align="right">
                <template #default="{ row }">
                  <el-input-number
                    v-model="row.批复金额"
                    :min="0"
                    :step="1000"
                    style="width: 100%"
                  />
                </template>
              </el-table-column>
              <el-table-column label="管理模式" width="140" align="center">
                <template #default>
                  <el-tag size="small" :type="budgetMode === '按到账金额' ? 'success' : 'info'">
                    {{ budgetMode }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="到账批次号" width="160" align="center">
                <template #default>
                  <span class="code-font">{{ currentBatchCode }}</span>
                </template>
              </el-table-column>
              <el-table-column label="备注说明" min-width="180">
                <template #default="{ row }">
                  <el-input v-model="row.备注" placeholder="科目测算说明或执行重点" />
                </template>
              </el-table-column>
              <el-table-column label="操作" width="90" align="center">
                <template #default="{ $index }">
                  <el-button type="danger" link size="small" @click="removeArrivalBudgetRow($index)">删除</el-button>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </el-tab-pane>

        <!-- 标签页 4：HRP 财务中间库双向协同 (文献四大中间表协同) -->
        <el-tab-pane label="HRP 财务中间库双向协同" name="hrpSync">
          <div class="hrp-sync-box">
            <el-alert
              title="科研与 HRP 财务系统双向中间库协同机制"
              type="info"
              :closable="false"
              show-icon
              description="清华长庚医院实践方案：科研管理系统与院内 HRP 财务系统通过4张标准业务中间表（项目表 hrp_sync_project、预算表 hrp_sync_budget、到账流水表 hrp_sync_incoming、支出核销流水表 hrp_sync_expense）进行松耦合交互，实现立项预算主动推送与财务到账/支出自动回写入账。"
              style="margin-bottom: 16px;"
            />

            <!-- 操作动作卡片 -->
            <div class="sync-action-toolbar">
              <el-button type="primary" :icon="Upload" :loading="syncPushing" @click="handleHrpPush">
                一键推送立项与预算至 HRP
              </el-button>
              <el-button type="success" :icon="Download" :loading="syncPulling" @click="handleHrpPull">
                一键拉取 HRP 到账与实际支出
              </el-button>
              <el-button :icon="Refresh" :loading="hrpLoading" @click="fetchHrpStatus">
                刷新协同状态
              </el-button>
            </div>

            <!-- 四大中间表多标签页 -->
            <el-tabs v-model="hrpSubTab" style="margin-top: 16px;">
              <el-tab-pane label="1. 立项推送表 (hrp_sync_project)" name="sync_project">
                <el-table :data="hrpProjectList" stripe style="width: 100%;">
                  <el-table-column prop="project_id" label="课题ID" width="90" align="center" />
                  <el-table-column prop="project_code" label="项目编码" width="160" />
                  <el-table-column prop="project_name" label="课题名称" min-width="200" />
                  <el-table-column prop="pi_name" label="负责人" width="110" />
                  <el-table-column prop="dept_name" label="部门" width="120" />
                  <el-table-column label="核定资助金额" width="140" align="right">
                    <template #default="{ row }">
                      <span>¥{{ formatMoney(row.approved_budget) }}</span>
                    </template>
                  </el-table-column>
                  <el-table-column label="中间表同步状态" width="130" align="center">
                    <template #default="{ row }">
                      <el-tag :type="row.sync_status === 1 ? 'success' : row.sync_status === 2 ? 'danger' : 'warning'" size="small">
                        {{ row.sync_status === 1 ? '已同步成功' : row.sync_status === 2 ? '同步异常' : '待HRP拉取' }}
                      </el-tag>
                    </template>
                  </el-table-column>
                  <el-table-column prop="sync_time" label="同步时间" width="160" />
                </el-table>
              </el-tab-pane>

              <el-tab-pane label="2. 预算明细推送表 (hrp_sync_budget)" name="sync_budget">
                <el-table :data="hrpBudgetList" stripe style="width: 100%;">
                  <el-table-column prop="id" label="记录ID" width="90" align="center" />
                  <el-table-column prop="project_code" label="项目编码" width="150" />
                  <el-table-column prop="subject_name" label="预算科目名称" min-width="160" />
                  <el-table-column label="批复金额" width="140" align="right">
                    <template #default="{ row }">
                      <span>¥{{ formatMoney(row.amount) }}</span>
                    </template>
                  </el-table-column>
                  <el-table-column prop="budget_mode" label="预算模式" width="130" align="center" />
                  <el-table-column prop="arrival_batch" label="对应到账批次" width="140" align="center" />
                  <el-table-column label="状态" width="110" align="center">
                    <template #default="{ row }">
                      <el-tag :type="row.sync_status === 1 ? 'success' : 'warning'" size="small">
                        {{ row.sync_status === 1 ? '已生效' : '待处理' }}
                      </el-tag>
                    </template>
                  </el-table-column>
                </el-table>
              </el-tab-pane>

              <el-tab-pane label="3. 到账流水回写表 (hrp_sync_incoming)" name="sync_incoming">
                <el-table :data="hrpIncomingList" stripe style="width: 100%;">
                  <el-table-column prop="voucher_no" label="财务到账凭据号" width="160" />
                  <el-table-column prop="arrival_batch" label="到账批次" width="140" align="center" />
                  <el-table-column label="到账金额" width="140" align="right">
                    <template #default="{ row }">
                      <span class="money-font text-success">¥{{ formatMoney(row.amount) }}</span>
                    </template>
                  </el-table-column>
                  <el-table-column prop="arrival_date" label="到账日期" width="130" />
                  <el-table-column prop="source_account" label="拨付来源渠道" min-width="180" />
                  <el-table-column label="回写科研系统状态" width="140" align="center">
                    <template #default="{ row }">
                      <el-tag :type="row.sync_status === 1 ? 'success' : 'info'" size="small">
                        {{ row.sync_status === 1 ? '已入账至科研' : '待处理' }}
                      </el-tag>
                    </template>
                  </el-table-column>
                </el-table>
              </el-tab-pane>

              <el-tab-pane label="4. 支出核销流水表 (hrp_sync_expense)" name="sync_expense">
                <el-table :data="hrpExpenseList" stripe style="width: 100%;">
                  <el-table-column prop="expense_no" label="科研报销单号" width="150" />
                  <el-table-column prop="voucher_no" label="HRP凭证号" width="150" />
                  <el-table-column prop="subject_name" label="支出科目" width="150" />
                  <el-table-column label="核销入账金额" width="140" align="right">
                    <template #default="{ row }">
                      <span class="money-font text-warning">¥{{ formatMoney(row.amount) }}</span>
                    </template>
                  </el-table-column>
                  <el-table-column prop="payee" label="收款人/供应商" min-width="140" />
                  <el-table-column prop="expense_date" label="实际扣账日期" width="130" />
                  <el-table-column label="状态" width="120" align="center">
                    <template #default="{ row }">
                      <el-tag :type="row.sync_status === 1 ? 'success' : 'warning'" size="small">
                        {{ row.sync_status === 1 ? '已核销记账' : '待入账' }}
                      </el-tag>
                    </template>
                  </el-table-column>
                </el-table>
              </el-tab-pane>
            </el-tabs>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 登记支出弹窗 -->
    <el-dialog v-model="ledgerDialogVisible" title="登记科研经费分层支出" width="480px">
      <el-form :model="ledgerForm" label-width="100px" style="padding-right: 20px;">
        <el-form-item label="支出科目" required>
          <el-select v-model="ledgerForm.科目名称" placeholder="请选择科目" style="width: 100%;">
            <el-option label="直接经费 - 材料与试剂耗材" value="材料与试剂耗材" />
            <el-option label="直接经费 - 测试化验加工费" value="测试化验加工费" />
            <el-option label="直接经费 - 差旅学术会议费" value="差旅学术会议费" />
            <el-option label="直接经费 - 学生劳务与专家咨询" value="学生劳务与专家咨询" />
            <el-option label="直接经费 - 仪器设备购置费" value="仪器设备购置费" />
            <el-option label="间接经费 - 绩效支出与管理费" value="绩效支出与管理费" />
          </el-select>
        </el-form-item>
        <el-form-item label="支出金额(元)" required>
          <el-input-number v-model="ledgerForm.金额" :min="1" :step="1000" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="报销事项说明" required>
          <el-input
            v-model="ledgerForm.说明"
            type="textarea"
            rows="3"
            placeholder="请详细说明单据凭证、用途及发票编号"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="ledgerDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleLedgerSubmit">
          确认登记
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import {
  getExpenseLayers,
  saveLedgerEntry,
  saveArrivalBudget,
  getArrivalBudgetList,
  syncHrpIntermediate,
  getHrpSyncStatus,
  type ExpenseLayerItem,
  type PaymentNodeItem,
  type ArrivalBudgetItem
} from '@/api/finance'
import { ElMessage } from 'element-plus'
import { Plus, Refresh, Upload, Download, Check } from '@element-plus/icons-vue'

const activeTab = ref('ledger')
const loading = ref(false)
const submitLoading = ref(false)
const route = useRoute()
const projectId = computed(() => {
  const raw = route.query.projectId ?? route.query.项目编号
  const parsed = Number(raw)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : 0
})

const currentProjectName = ref('未选择项目（请从项目台账进入）')
const ledgerList = ref<ExpenseLayerItem[]>([])
const paymentNodes = ref<PaymentNodeItem[]>([])
const ledgerDialogVisible = ref(false)

const ledgerForm = reactive({
  项目编号: 0,
  科目名称: '材料与试剂耗材',
  金额: 15000,
  说明: ''
})

const totalBudget = computed(() => {
  return ledgerList.value.reduce((acc, cur) => acc + (Number(cur.批复预算) || 0), 0)
})

const totalSpent = computed(() => {
  return ledgerList.value.reduce((acc, cur) => acc + (Number(cur.已支出金额) || 0), 0)
})

const totalArrival = computed(() => ledgerList.value.reduce((acc, cur) => acc + (Number((cur as any).到账金额) || 0), 0))
const totalAvailable = computed(() => totalBudget.value - totalSpent.value)

// 到账批次预算管理 (Tab 3: 文献清华长庚医院实践)
const budgetMode = ref<'按项目立项' | '按到账金额'>('按到账金额')
const arrivalBudgetLoading = ref(false)
const savingBudgetLoading = ref(false)
const selectedArrivalId = ref<number | undefined>(undefined)

const arrivalBatches = ref<Array<{ id: number; batch: string; label: string; amount: number }>>([])

const arrivalBudgetRows = ref<ArrivalBudgetItem[]>([])

const currentBatchAmount = computed(() => {
  const found = arrivalBatches.value.find(b => b.id === selectedArrivalId.value)
  return found?.amount || 0
})

const currentBatchCode = computed(() => {
  const found = arrivalBatches.value.find(b => b.id === selectedArrivalId.value)
  return found?.batch || '-'
})

const currentBatchAllocated = computed(() => {
  return arrivalBudgetRows.value.reduce((acc, cur) => acc + (Number(cur.批复金额) || 0), 0)
})

const currentBatchDiff = computed(() => {
  return currentBatchAmount.value - currentBatchAllocated.value
})

function onBudgetModeChange(mode: any) {
  ElMessage.info(`已切换至【${mode}】维护模式`)
}

async function onArrivalChange(id: number) {
  selectedArrivalId.value = id
  await fetchArrivalBudgets()
}

function addArrivalBudgetRow() {
  arrivalBudgetRows.value.push({
    预算科目编号: 100 + arrivalBudgetRows.value.length + 1,
    预算科目名称: '其他直接科研支出',
    批复金额: 0,
    备注: ''
  })
}

function removeArrivalBudgetRow(index: number) {
  arrivalBudgetRows.value.splice(index, 1)
}

async function fetchArrivalBudgets() {
  arrivalBudgetLoading.value = true
  try {
    if (!projectId.value) {
      arrivalBudgetRows.value = []
      return
    }
    const list = await getArrivalBudgetList(projectId.value, selectedArrivalId.value)
    arrivalBudgetRows.value = Array.isArray(list) ? list : []
  } catch (e: any) {
    arrivalBudgetRows.value = []
    ElMessage.error(e.response?.data?.error || e.message || '到账批次预算查询失败')
  } finally {
    arrivalBudgetLoading.value = false
  }
}

async function handleSaveArrivalBudget() {
  if (!projectId.value || !selectedArrivalId.value) {
    ElMessage.warning('请从项目台账进入并选择有效的到账批次')
    return
  }
  savingBudgetLoading.value = true
  try {
    await saveArrivalBudget({
      项目编号: projectId.value,
      到账编号: selectedArrivalId.value,
      到账批次: currentBatchCode.value,
      预算列表: arrivalBudgetRows.value.map(r => ({
        预算科目编号: r.预算科目编号 || 101,
        预算科目名称: r.预算科目名称,
        批复金额: Number(r.批复金额) || 0,
        备注: r.备注
      }))
    })
    ElMessage.success(`批次【${currentBatchCode.value}】预算细化分配保存成功！`)
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '保存批次预算失败')
  } finally {
    savingBudgetLoading.value = false
  }
}

// HRP 中间库协同 (Tab 4: 文献四大业务中间表)
const hrpSubTab = ref('sync_project')
const hrpLoading = ref(false)
const syncPushing = ref(false)
const syncPulling = ref(false)

const hrpProjectList = ref<any[]>([])
const hrpBudgetList = ref<any[]>([])
const hrpIncomingList = ref<any[]>([])
const hrpExpenseList = ref<any[]>([])

async function fetchHrpStatus() {
  hrpLoading.value = true
  try {
    if (!projectId.value) {
      hrpProjectList.value = []
      hrpBudgetList.value = []
      hrpIncomingList.value = []
      hrpExpenseList.value = []
      return
    }
    const res = await getHrpSyncStatus(projectId.value)
    if (res) {
      hrpProjectList.value = Array.isArray(res.项目列表) ? res.项目列表 : []
      hrpBudgetList.value = Array.isArray(res.预算列表) ? res.预算列表 : []
      hrpIncomingList.value = Array.isArray(res.到账列表) ? res.到账列表 : []
      hrpExpenseList.value = Array.isArray(res.支出列表) ? res.支出列表 : []
    }
  } catch (e: any) {
    hrpProjectList.value = []
    hrpBudgetList.value = []
    hrpIncomingList.value = []
    hrpExpenseList.value = []
    ElMessage.error(e.response?.data?.error || e.message || 'HRP 中间库状态查询失败')
  } finally {
    hrpLoading.value = false
  }
}

async function handleHrpPush() {
  if (!projectId.value) {
    ElMessage.warning('请从项目台账进入后再推送 HRP')
    return
  }
  syncPushing.value = true
  try {
    await syncHrpIntermediate({ 协同模式: '推送立项与预算', 项目编号: projectId.value })
    ElMessage.success('已成功将课题立项信息与预算明细推送至 HRP 中间库！')
    await fetchHrpStatus()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '推送失败')
  } finally {
    syncPushing.value = false
  }
}

async function handleHrpPull() {
  if (!projectId.value) {
    ElMessage.warning('请从项目台账进入后再拉取 HRP')
    return
  }
  syncPulling.value = true
  try {
    await syncHrpIntermediate({ 协同模式: '拉取到账与支出', 项目编号: projectId.value })
    ElMessage.success('已成功从 HRP 中间库拉取最新财务到账与实际支出流水！')
    await fetchHrpStatus()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '拉取失败')
  } finally {
    syncPulling.value = false
  }
}

function formatMoney(num?: number) {
  if (num == null) return '0.00'
  return Number(num).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function getExecutionRate(spent: number, budget: number): number {
  if (!budget || budget <= 0) return 0
  const rate = Math.round((spent / budget) * 100)
  return Math.min(rate, 100)
}

function getRateStatus(spent: number, budget: number): '' | 'success' | 'warning' | 'exception' {
  const rate = (spent / (budget || 1)) * 100
  if (rate >= 90) return 'warning'
  if (rate >= 50) return 'success'
  return ''
}

async function fetchLedgers() {
  loading.value = true
  try {
    ledgerForm.项目编号 = projectId.value
    if (!projectId.value) {
      ledgerList.value = []
      paymentNodes.value = []
      return
    }
    const list = await getExpenseLayers({ 项目编号: projectId.value })
    ledgerList.value = Array.isArray(list) ? list : []
    // 付款节点由后端项目合同接口提供；没有真实数据时保持空状态，禁止展示演示节点。
    paymentNodes.value = []
  } finally {
    loading.value = false
  }
}

function openLedgerDialog() {
  if (!projectId.value) {
    ElMessage.warning('请从项目台账进入后登记经费支出')
    return
  }
  ledgerForm.项目编号 = projectId.value
  ledgerForm.说明 = ''
  ledgerDialogVisible.value = true
}

async function handleLedgerSubmit() {
  if (!projectId.value) {
    ElMessage.warning('请从项目台账进入后登记经费支出')
    return
  }
  if (!ledgerForm.说明.trim()) {
    ElMessage.warning('请填写支出事项说明')
    return
  }
  submitLoading.value = true
  try {
    await saveLedgerEntry(ledgerForm)
    ElMessage.success('科研经费支出台账登记成功')
    ledgerDialogVisible.value = false
    await fetchLedgers()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '登记失败')
  } finally {
    submitLoading.value = false
  }
}

onMounted(async () => {
  await fetchLedgers()
  await fetchArrivalBudgets()
  await fetchHrpStatus()
})
</script>

<style scoped lang="scss">
.page-container {
  padding: 16px;

  .main-card {
    background-color: #ffffff;
    border-radius: 8px;

    .project-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-bottom: 16px;
      border-bottom: 1px solid #f1f5f9;
      flex-wrap: wrap;
      gap: 12px;

      .header-left {
        h2 {
          font-size: 18px;
          color: #0f172a;
          margin-bottom: 6px;
        }

        .header-meta {
          font-size: 13px;
          color: #64748b;
        }
      }

      .header-right {
        display: flex;
        gap: 8px;
      }
    }

    .finance-cards {
      margin-top: 16px;

      .f-card {
        padding: 16px;
        border-radius: 8px;
        color: #ffffff;
        display: flex;
        flex-direction: column;

        .f-label {
          font-size: 12px;
          opacity: 0.9;
          margin-bottom: 4px;
        }

        .f-val {
          font-size: 22px;
          font-weight: 700;
        }

        &.f-blue {
          background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%);
        }
        &.f-green {
          background: linear-gradient(135deg, #10b981 0%, #047857 100%);
        }
        &.f-orange {
          background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
        }
        &.f-purple {
          background: linear-gradient(135deg, #8b5cf6 0%, #6d28d9 100%);
        }
      }
    }

    .arrival-budget-box {
      .mode-bar {
        display: flex;
        justify-content: space-between;
        align-items: center;
        flex-wrap: wrap;
        gap: 12px;

        .mode-label {
          font-weight: 600;
          color: #1e293b;
          margin-right: 8px;
        }

        .mode-right {
          display: flex;
          gap: 8px;
        }
      }

      .batch-summary-chips {
        display: flex;
        gap: 10px;
        flex-wrap: wrap;
        justify-content: flex-end;
      }
    }

    .hrp-sync-box {
      .sync-action-toolbar {
        display: flex;
        gap: 12px;
        margin-bottom: 8px;
        flex-wrap: wrap;
      }
    }
  }
}
</style>
