<template>
  <div class="page-container">
    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab" class="governance-tabs">
        <!-- 标签页 1：权限代理管理 -->
        <el-tab-pane label="权限代理管理" name="proxy">
          <div class="toolbar">
            <div class="left">
              <el-button type="primary" :icon="Plus" @click="openGrantDialog">
                发起代理授权
              </el-button>
              <el-button :icon="Refresh" @click="fetchProxies">刷新</el-button>
            </div>
            <div class="right">
              <el-alert
                title="代理人可代表授权人执行白名单内审批流，过期或人员禁用将自动回收"
                type="info"
                :closable="false"
                show-icon
              />
            </div>
          </div>

          <el-table
            v-loading="proxyLoading"
            :data="proxyList"
            stripe
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="编号" label="代理编号" width="90" align="center" />
            <el-table-column prop="授权人姓名" label="授权人" width="110" />
            <el-table-column prop="被代理人姓名" label="被代理人" width="110" />
            <el-table-column prop="代理范围" label="代理业务范围" width="130">
              <template #default="{ row }">
                <el-tag size="small" type="primary">{{ row.代理范围 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="生效起止时间" min-width="260">
              <template #default="{ row }">
                <span>{{ row.开始时间 }} 至 {{ row.结束时间 }}</span>
              </template>
            </el-table-column>
            <el-table-column label="代理状态" width="110" align="center">
              <template #default="{ row }">
                <el-tag v-if="row.是否撤销" type="danger" size="small">已撤销</el-tag>
                <el-tag v-else-if="isExpired(row.结束时间)" type="info" size="small">已过期</el-tag>
                <el-tag v-else type="success" size="small">生效中</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="撤销原因" label="撤销记录" min-width="150" show-overflow-tooltip>
              <template #default="{ row }">
                <span v-if="row.是否撤销" class="revoke-meta">
                  {{ row.撤销人姓名 ? `[${row.撤销人姓名}] ` : '' }}{{ row.撤销原因 || '未填写原因' }}
                </span>
                <span v-else class="text-muted">—</span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="100" fixed="right" align="center">
              <template #default="{ row }">
                <el-button
                  v-if="!row.是否撤销 && !isExpired(row.结束时间)"
                  type="danger"
                  link
                  size="small"
                  @click="openRevokeDialog(row)"
                >
                  撤销授权
                </el-button>
                <span v-else class="text-muted">不可操作</span>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签页 2：预算异常对账 -->
        <el-tab-pane label="预算异常对账" name="reconciliation">
          <div class="toolbar">
            <div class="left">
              <el-button :icon="Refresh" @click="fetchReconciliations">刷新对账结果</el-button>
            </div>
            <div class="right">
              <el-alert
                title="系统自动对账项目批复预算、实际到账与报销审核金额，异常将在此处汇聚预警"
                type="warning"
                :closable="false"
                show-icon
              />
            </div>
          </div>

          <el-table
            v-loading="reconcileLoading"
            :data="reconcileList"
            stripe
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="项目名称" label="项目名称" min-width="180" show-overflow-tooltip />
            <el-table-column prop="负责人姓名" label="负责人" width="100" />
            <el-table-column label="批复预算" width="120" align="right">
              <template #default="{ row }">
                <span>¥{{ formatMoney(row.预算批复金额) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="经费到账" width="120" align="right">
              <template #default="{ row }">
                <span style="color: #67c23a;">¥{{ formatMoney(row.经费到账金额) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="已报销支出" width="120" align="right">
              <template #default="{ row }">
                <span style="color: #e6a23c;">¥{{ formatMoney(row.已报销金额) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="待审报销" width="110" align="right">
              <template #default="{ row }">
                <span>¥{{ formatMoney(row.待审报销金额) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="异常差额" width="120" align="right">
              <template #default="{ row }">
                <span style="color: #f56c6c; font-weight: 600;">
                  ¥{{ formatMoney(row.对账异常差额) }}
                </span>
              </template>
            </el-table-column>
            <el-table-column label="对账状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="getStatusTag(row.处理状态)" size="small">
                  {{ row.处理状态 || '待核实' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="100" fixed="right" align="center">
              <template #default="{ row }">
                <el-button type="primary" link size="small" @click="openProcessDialog(row)">
                  处理对账
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 发起代理授权弹窗 -->
    <el-dialog v-model="grantDialogVisible" title="发起权限代理授权" width="500px">
      <el-form
        ref="grantFormRef"
        :model="grantForm"
        :rules="grantRules"
        label-width="110px"
        style="padding-right: 20px;"
      >
        <el-form-item label="被代理人工号" prop="被代理人工号">
          <el-input
            v-model="grantForm.被代理人工号"
            placeholder="请输入有效人员工号（如 1 张三）"
          />
        </el-form-item>

        <el-form-item label="代理业务范围" prop="代理范围">
          <el-select v-model="grantForm.代理范围" placeholder="请选择白名单授权业务" style="width: 100%;">
            <el-option
              v-for="item in allowedScopes"
              :key="item"
              :label="item"
              :value="item"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="有效期区间" prop="dateRange">
          <el-date-picker
            v-model="grantForm.dateRange"
            type="datetimerange"
            range-separator="至"
            start-placeholder="开始时间"
            end-placeholder="结束时间"
            value-format="YYYY-MM-DD HH:mm:ss"
            style="width: 100%;"
          />
        </el-form-item>

        <el-form-item label="授权备注" prop="备注">
          <el-input
            v-model="grantForm.备注"
            type="textarea"
            rows="3"
            placeholder="请输入本次代理授权事由（如外出公干委托审批）"
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="grantDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleGrantSubmit">
          确认授权
        </el-button>
      </template>
    </el-dialog>

    <!-- 撤销授权弹窗 -->
    <el-dialog v-model="revokeDialogVisible" title="撤销代理授权" width="450px">
      <p style="margin-bottom: 12px; color: #606266;">
        确定撤销对 <strong>{{ currentRevokeItem?.被代理人姓名 }}</strong> 的【{{ currentRevokeItem?.代理范围 }}】代理授权吗？
      </p>
      <el-input
        v-model="revokeReason"
        type="textarea"
        rows="3"
        placeholder="请输入撤销原因（必填）"
      />
      <template #footer>
        <el-button @click="revokeDialogVisible = false">取消</el-button>
        <el-button type="danger" :loading="submitLoading" @click="handleRevokeSubmit">
          确认撤销
        </el-button>
      </template>
    </el-dialog>

    <!-- 处理预算异常对账弹窗 -->
    <el-dialog v-model="processDialogVisible" title="登记对账处理记录" width="500px">
      <el-form :model="processForm" label-width="100px" style="padding-right: 20px;">
        <el-form-item label="项目名称">
          <span>{{ currentReconcileItem?.项目名称 }}</span>
        </el-form-item>
        <el-form-item label="异常差额">
          <span style="color: #f56c6c; font-weight: 600;">
            ¥{{ formatMoney(currentReconcileItem?.对账异常差额 || 0) }}
          </span>
        </el-form-item>
        <el-form-item label="处理状态">
          <el-radio-group v-model="processForm.处理状态">
            <el-radio value="处理中">核查中</el-radio>
            <el-radio value="已对账">已调账归一</el-radio>
            <el-radio value="免处理">无需调整</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="处理说明">
          <el-input
            v-model="processForm.处理说明"
            type="textarea"
            rows="3"
            placeholder="请输入核对结果与调账处理说明"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="processDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleProcessSubmit">
          保存处理记录
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import {
  getProxyScopes,
  getPersonProxies,
  grantProxy,
  revokeProxy,
  getBudgetReconciliations,
  handleBudgetReconciliation,
  type PermissionProxyItem,
  type BudgetReconciliationItem
} from '@/api/governance'
import { ElMessage } from 'element-plus'
import dayjs from 'dayjs'
import { Plus, Refresh } from '@element-plus/icons-vue'

const activeTab = ref('proxy')
const proxyLoading = ref(false)
const reconcileLoading = ref(false)
const submitLoading = ref(false)

const proxyList = ref<PermissionProxyItem[]>([])
const reconcileList = ref<BudgetReconciliationItem[]>([])
const allowedScopes = ref<string[]>(['项目审批'])

const grantDialogVisible = ref(false)
const revokeDialogVisible = ref(false)
const processDialogVisible = ref(false)

const grantForm = reactive({
  被代理人工号: '',
  代理范围: '项目审批',
  dateRange: [] as string[],
  备注: ''
})

const grantRules = {
  被代理人工号: [{ required: true, message: '请输入被代理人工号', trigger: 'blur' }],
  代理范围: [{ required: true, message: '请选择代理范围', trigger: 'change' }],
  dateRange: [{ required: true, message: '请选择有效期起止时间', trigger: 'change' }]
}

const currentRevokeItem = ref<PermissionProxyItem | null>(null)
const revokeReason = ref('')

const currentReconcileItem = ref<BudgetReconciliationItem | null>(null)
const processForm = reactive({
  处理状态: '已对账',
  处理说明: ''
})

function formatMoney(num?: number) {
  if (num == null) return '0.00'
  return Number(num).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function isExpired(dateStr?: string) {
  if (!dateStr) return false
  return dayjs(dateStr).isBefore(dayjs())
}

function getStatusTag(status?: string): '' | 'success' | 'warning' | 'info' | 'danger' {
  if (status === '已对账') return 'success'
  if (status === '处理中') return 'warning'
  if (status === '免处理') return 'info'
  return 'danger'
}

async function fetchProxies() {
  proxyLoading.value = true
  try {
    const list = await getPersonProxies()
    proxyList.value = Array.isArray(list) ? list : []
  } catch {
    proxyList.value = []
  } finally {
    proxyLoading.value = false
  }
}

async function fetchReconciliations() {
  reconcileLoading.value = true
  try {
    const list = await getBudgetReconciliations()
    reconcileList.value = Array.isArray(list) ? list : []
  } catch {
    reconcileList.value = []
  } finally {
    reconcileLoading.value = false
  }
}

function openGrantDialog() {
  grantForm.被代理人工号 = ''
  grantForm.代理范围 = allowedScopes.value[0] || '项目审批'
  grantForm.dateRange = [
    dayjs().format('YYYY-MM-DD HH:mm:ss'),
    dayjs().add(7, 'day').format('YYYY-MM-DD HH:mm:ss')
  ]
  grantForm.备注 = ''
  grantDialogVisible.value = true
}

async function handleGrantSubmit() {
  if (!grantForm.被代理人工号 || !grantForm.dateRange || grantForm.dateRange.length < 2) {
    ElMessage.warning('请补齐完整授权信息')
    return
  }
  submitLoading.value = true
  try {
    await grantProxy({
      被代理人编号: parseInt(grantForm.被代理人工号, 10),
      代理范围: grantForm.代理范围,
      开始时间: grantForm.dateRange[0],
      结束时间: grantForm.dateRange[1],
      备注: grantForm.备注
    })
    ElMessage.success('权限代理授权成功')
    grantDialogVisible.value = false
    await fetchProxies()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '授权失败')
  } finally {
    submitLoading.value = false
  }
}

function openRevokeDialog(item: PermissionProxyItem) {
  currentRevokeItem.value = item
  revokeReason.value = ''
  revokeDialogVisible.value = true
}

async function handleRevokeSubmit() {
  if (!currentRevokeItem.value) return
  if (!revokeReason.value.trim()) {
    ElMessage.warning('请输入撤销原因')
    return
  }
  submitLoading.value = true
  try {
    await revokeProxy({
      代理编号: currentRevokeItem.value.编号,
      撤销原因: revokeReason.value
    })
    ElMessage.success('代理授权已撤销')
    revokeDialogVisible.value = false
    await fetchProxies()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '撤销失败')
  } finally {
    submitLoading.value = false
  }
}

function openProcessDialog(item: BudgetReconciliationItem) {
  currentReconcileItem.value = item
  processForm.处理状态 = '已对账'
  processForm.处理说明 = ''
  processDialogVisible.value = true
}

async function handleProcessSubmit() {
  if (!currentReconcileItem.value) return
  submitLoading.value = true
  try {
    await handleBudgetReconciliation({
      流程模板编号: currentReconcileItem.value.流程模板编号,
      项目编号: currentReconcileItem.value.项目编号,
      处理状态: processForm.处理状态,
      处理说明: processForm.处理说明
    })
    ElMessage.success('预算异常对账记录已保存')
    processDialogVisible.value = false
    await fetchReconciliations()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '保存失败')
  } finally {
    submitLoading.value = false
  }
}

onMounted(async () => {
  try {
    const scopes = await getProxyScopes()
    if (Array.isArray(scopes) && scopes.length > 0) {
      allowedScopes.value = scopes
    }
  } catch {
    // default
  }
  await fetchProxies()
  await fetchReconciliations()
})
</script>

<style scoped lang="scss">
.page-container {
  padding: 16px;

  .main-card {
    background: #ffffff;
    border-radius: 8px;

    .toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
      flex-wrap: wrap;
      gap: 12px;

      .left {
        display: flex;
        gap: 8px;
      }
    }

    .revoke-meta {
      font-size: 12px;
      color: #f56c6c;
    }

    .text-muted {
      color: #909399;
    }
  }
}
</style>
