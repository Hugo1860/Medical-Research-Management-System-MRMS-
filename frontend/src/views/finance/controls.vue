<template>
  <div class="finance-controls-page">
    <el-card shadow="never" class="header-card">
      <div class="page-header">
        <div>
          <h2>科研经费控制中心</h2>
          <p class="subtitle">发票查重、预算调整版本和年度封账。系统尚未接入 OCR，发票号码、金额及供应商必须人工核对录入。</p>
        </div>
        <el-tag type="warning" effect="plain">人工录入 · 审批留痕</el-tag>
      </div>
    </el-card>

    <el-card shadow="never" class="scope-card">
      <el-form :inline="true" :model="scope" label-width="86px">
        <el-form-item label="流程模板">
          <el-input-number v-model="scope.流程模板编号" :min="1" :controls="false" />
        </el-form-item>
        <el-form-item label="项目编号">
          <el-input-number v-model="scope.项目编号" :min="1" :controls="false" />
        </el-form-item>
        <el-form-item label="结算年度">
          <el-input-number v-model="scope.结算年度" :min="2000" :max="2200" :controls="false" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="loading" @click="loadProjectData">加载项目控制数据</el-button>
        </el-form-item>
      </el-form>
      <el-alert v-if="!scope.项目编号" title="请输入项目编号后加载真实数据，页面不会填充演示台账。" type="info" :closable="false" show-icon />
    </el-card>

    <el-row :gutter="16">
      <el-col :xs="24" :lg="12">
        <el-card shadow="never" class="section-card">
          <template #header><div class="section-title">发票查重与人工录入</div></template>
          <el-form :model="invoiceForm" label-width="96px" @submit.prevent>
            <el-form-item label="发票号码" required><el-input v-model="invoiceForm.发票号码" maxlength="100" /></el-form-item>
            <el-form-item label="发票代码"><el-input v-model="invoiceForm.发票代码" maxlength="50" /></el-form-item>
            <el-form-item label="开票日期"><el-date-picker v-model="invoiceForm.开票日期" type="date" value-format="YYYY-MM-DD" style="width: 100%" /></el-form-item>
            <el-form-item label="金额(元)" required><el-input-number v-model="invoiceForm.金额" :min="0.01" :precision="2" style="width: 100%" /></el-form-item>
            <el-form-item label="供应商" required><el-input v-model="invoiceForm.供应商名称" maxlength="300" /></el-form-item>
            <el-form-item label="供应商税号"><el-input v-model="invoiceForm.供应商税号" maxlength="100" /></el-form-item>
            <el-form-item label="附件路径"><el-input v-model="invoiceForm.附件路径" placeholder="人工核对的电子票据路径" /></el-form-item>
            <el-form-item>
              <el-button :loading="invoiceChecking" @click="checkInvoice">先查重</el-button>
              <el-button type="primary" :loading="invoiceSaving" @click="saveInvoice">保存发票</el-button>
            </el-form-item>
          </el-form>
          <el-alert v-if="invoiceDuplicates.length" type="warning" :closable="false" show-icon title="发现相同号码或供应商金额组合，请人工核对。允许保存后记录会标记为疑似重复。" />
          <el-table v-if="invoiceDuplicates.length" :data="invoiceDuplicates" size="small" border style="margin-top: 12px">
            <el-table-column prop="发票号码" label="发票号码" min-width="130" />
            <el-table-column prop="供应商名称" label="供应商" min-width="150" />
            <el-table-column prop="金额" label="金额" width="110" align="right" />
            <el-table-column prop="重复原因" label="重复原因" min-width="160" />
          </el-table>
        </el-card>
      </el-col>

      <el-col :xs="24" :lg="12">
        <el-card shadow="never" class="section-card">
          <template #header><div class="section-title">年度结算与封账</div></template>
          <div class="inline-actions">
            <el-button type="primary" :loading="settlementLoading" @click="loadSettlements">刷新结算状态</el-button>
            <el-button type="danger" :disabled="!scope.项目编号" @click="closeSettlement">封账 {{ scope.结算年度 }} 年</el-button>
            <el-button type="warning" :disabled="!scope.项目编号" @click="reopenSettlement">财务负责人重开</el-button>
          </div>
          <el-table :data="settlements" size="small" border style="margin-top: 14px">
            <el-table-column prop="结算年度" label="年度" width="70" />
            <el-table-column prop="状态" label="状态" width="90"><template #default="{ row }"><el-tag :type="settlementTag(row.状态)">{{ row.状态 }}</el-tag></template></el-table-column>
            <el-table-column prop="批准预算" label="批准预算" align="right" min-width="110" />
            <el-table-column prop="到账金额" label="到账" align="right" min-width="100" />
            <el-table-column prop="支出金额" label="报销支出" align="right" min-width="110" />
            <el-table-column prop="付款金额" label="实际付款" align="right" min-width="110" />
          </el-table>
          <el-empty v-if="!settlements.length" description="尚未登记该项目年度结算" :image-size="60" />
        </el-card>

        <el-card shadow="never" class="section-card">
          <template #header><div class="section-title">付款状态回写约束</div></template>
          <el-form :model="paymentForm" :inline="true" label-width="82px">
            <el-form-item label="台账编号"><el-input-number v-model="paymentForm.台账编号" :min="1" :controls="false" /></el-form-item>
            <el-form-item label="状态"><el-select v-model="paymentForm.状态" style="width: 130px"><el-option v-for="item in paymentStatuses" :key="item" :label="item" :value="item" /></el-select></el-form-item>
            <el-form-item label="付款日期"><el-date-picker v-model="paymentForm.付款日期" type="date" value-format="YYYY-MM-DD" style="width: 145px" /></el-form-item>
            <el-form-item><el-button type="success" :loading="paymentSaving" @click="savePayment">回写</el-button></el-form-item>
          </el-form>
          <p class="muted">已封账年度的发票和付款台账由数据库触发器拒绝修改；回写凭证号请在原财务流水同步中保留。</p>
        </el-card>
      </el-col>
    </el-row>

    <el-card shadow="never" class="section-card">
      <template #header>
        <div class="section-header"><span class="section-title">预算调整申请与版本历史</span><el-button type="primary" size="small" @click="addBudgetLine">添加预算科目</el-button></div>
      </template>
      <el-form :model="budgetForm" label-width="90px">
        <el-form-item label="调整原因" required><el-input v-model="budgetForm.申请原因" type="textarea" :rows="2" maxlength="2000" show-word-limit /></el-form-item>
      </el-form>
      <el-table :data="budgetForm.申请预算" border size="small">
        <el-table-column label="支出类型" min-width="130"><template #default="{ row }"><el-input v-model="row.项目支出类型" /></template></el-table-column>
        <el-table-column label="支出内容" min-width="160"><template #default="{ row }"><el-input v-model="row.项目支出内容" /></template></el-table-column>
        <el-table-column label="财务科目" min-width="140"><template #default="{ row }"><el-input v-model="row.财务科目" /></template></el-table-column>
        <el-table-column label="批准经费" width="145"><template #default="{ row }"><el-input-number v-model="row.批准经费" :min="0" :precision="2" controls-position="right" /></template></el-table-column>
        <el-table-column label="配套经费" width="145"><template #default="{ row }"><el-input-number v-model="row.配套经费" :min="0" :precision="2" controls-position="right" /></template></el-table-column>
        <el-table-column label="操作" width="70"><template #default="{ $index }"><el-button link type="danger" @click="removeBudgetLine($index)">移除</el-button></template></el-table-column>
      </el-table>
      <div class="section-actions"><el-button type="primary" :loading="budgetSaving" @click="submitBudget">提交预算调整审批</el-button><el-button @click="loadBudgetHistory">刷新申请与版本</el-button></div>
      <el-divider content-position="left">申请记录</el-divider>
      <el-table :data="adjustments" size="small" border>
        <el-table-column prop="编号" label="申请号" width="90" />
        <el-table-column prop="申请时间" label="申请时间" width="170" />
        <el-table-column prop="申请原因" label="原因" min-width="220" show-overflow-tooltip />
        <el-table-column prop="状态" label="状态" width="90" />
        <el-table-column prop="审批意见" label="审批意见" min-width="180" show-overflow-tooltip />
      </el-table>
      <el-divider content-position="left">版本历史</el-divider>
      <el-table :data="versions" size="small" border>
        <el-table-column prop="版本号" label="版本" width="80" />
        <el-table-column prop="版本状态" label="状态" width="90" />
        <el-table-column prop="来源申请编号" label="来源申请" width="100" />
        <el-table-column prop="建立时间" label="建立时间" width="170" />
        <el-table-column prop="变更说明" label="变更说明" min-width="220" show-overflow-tooltip />
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  checkFinanceInvoice,
  closeYearSettlement,
  getBudgetAdjustments,
  getYearSettlements,
  registerFinanceInvoice,
  reopenYearSettlement,
  submitBudgetAdjustment,
  writebackPayment,
  type FinanceBudgetAdjustmentItem,
  type FinanceBudgetVersionItem,
  type FinanceInvoiceItem,
  type FinanceYearSettlementItem
} from '@/api/finance'

const loading = ref(false)
const scope = reactive({ 流程模板编号: 410, 项目编号: 0, 结算年度: new Date().getFullYear() })
const invoiceChecking = ref(false)
const invoiceSaving = ref(false)
const invoiceDuplicates = ref<FinanceInvoiceItem[]>([])
const invoiceForm = reactive({ 发票号码: '', 发票代码: '', 开票日期: '', 金额: 0, 供应商名称: '', 供应商税号: '', 附件路径: '' })
const settlementLoading = ref(false)
const settlements = ref<FinanceYearSettlementItem[]>([])
const paymentSaving = ref(false)
const paymentStatuses = ['待审核', '已审批', '已付款', '已驳回', '已冲销']
const paymentForm = reactive({ 台账编号: 0, 状态: '已付款', 付款日期: '' })
const budgetSaving = ref(false)
const adjustments = ref<FinanceBudgetAdjustmentItem[]>([])
const versions = ref<FinanceBudgetVersionItem[]>([])
const budgetForm = reactive<{ 申请原因: string; 申请预算: Array<{ 项目支出类型: string; 项目支出内容: string; 财务科目: string; 批准经费: number; 配套经费: number }> }>({
  申请原因: '',
  申请预算: [{ 项目支出类型: '直接经费', 项目支出内容: '', 财务科目: '', 批准经费: 0, 配套经费: 0 }]
})

function ensureScope() {
  if (!scope.项目编号) { ElMessage.warning('请先输入项目编号'); return false }
  return true
}
async function loadProjectData() {
  if (!ensureScope()) return
  loading.value = true
  try { await Promise.all([loadSettlements(), loadBudgetHistory()]) } finally { loading.value = false }
}
async function checkInvoice() {
  if (!ensureScope() || !invoiceForm.发票号码 || !invoiceForm.供应商名称 || invoiceForm.金额 <= 0) { ElMessage.warning('请填写发票号码、供应商和金额'); return }
  invoiceChecking.value = true
  try { invoiceDuplicates.value = await checkFinanceInvoice({ ...scope, ...invoiceForm }) || []; if (!invoiceDuplicates.value.length) ElMessage.success('未发现重复票据') } catch (error: any) { ElMessage.error(error.message || '发票查重失败') } finally { invoiceChecking.value = false }
}
async function saveInvoice() {
  if (!ensureScope()) return
  if (invoiceDuplicates.value.length) {
    try { await ElMessageBox.confirm('当前存在疑似重复发票，确认仍以疑似重复状态保存吗？', '重复票据确认', { type: 'warning' }) } catch { return }
  }
  invoiceSaving.value = true
  try { await registerFinanceInvoice({ ...scope, ...invoiceForm, 允许重复: invoiceDuplicates.value.length > 0 }); ElMessage.success('发票已保存，OCR 状态为人工录入'); invoiceDuplicates.value = [] } catch (error: any) { ElMessage.error(error.message || '保存发票失败') } finally { invoiceSaving.value = false }
}
async function loadSettlements() {
  if (!ensureScope()) return
  settlementLoading.value = true
  try { settlements.value = await getYearSettlements(scope.流程模板编号, scope.项目编号) || [] } catch (error: any) { ElMessage.error(error.message || '加载年度结算失败') } finally { settlementLoading.value = false }
}
function settlementTag(status: string) { return status === '已封账' ? 'danger' : status === '已重开' ? 'warning' : 'success' }
async function closeSettlement() {
  if (!ensureScope()) return
  try { await ElMessageBox.confirm(`封账后将阻止 ${scope.结算年度} 年发票和付款流水写入，确认继续？`, '年度封账', { type: 'warning' }); await closeYearSettlement({ ...scope }); ElMessage.success('年度封账成功'); await loadSettlements() } catch (error: any) { if (error !== 'cancel') ElMessage.error(error.message || '封账失败') }
}
async function reopenSettlement() {
  if (!ensureScope()) return
  try { await ElMessageBox.confirm('重开操作只允许财务负责人或系统管理员，确认继续？', '重开年度结算', { type: 'warning' }); await reopenYearSettlement({ ...scope }); ElMessage.success('已提交年度重开'); await loadSettlements() } catch (error: any) { if (error !== 'cancel') ElMessage.error(error.message || '重开失败') }
}
async function savePayment() {
  if (!paymentForm.台账编号) { ElMessage.warning('请输入台账编号'); return }
  paymentSaving.value = true
  try { await writebackPayment({ ...paymentForm }); ElMessage.success('付款状态回写成功') } catch (error: any) { ElMessage.error(error.message || '付款状态回写失败') } finally { paymentSaving.value = false }
}
function addBudgetLine() { budgetForm.申请预算.push({ 项目支出类型: '直接经费', 项目支出内容: '', 财务科目: '', 批准经费: 0, 配套经费: 0 }) }
function removeBudgetLine(index: number) { if (budgetForm.申请预算.length > 1) budgetForm.申请预算.splice(index, 1) }
async function loadBudgetHistory() {
  if (!ensureScope()) return
  try { const result = await getBudgetAdjustments(scope); adjustments.value = result.申请列表 || []; versions.value = result.版本列表 || [] } catch (error: any) { ElMessage.error(error.message || '加载预算版本失败') }
}
async function submitBudget() {
  if (!ensureScope() || !budgetForm.申请原因.trim()) { ElMessage.warning('请填写预算调整原因'); return }
  if (budgetForm.申请预算.some(row => !row.项目支出内容 || !row.财务科目 || row.批准经费 < 0)) { ElMessage.warning('预算明细的内容、财务科目和金额必须完整'); return }
  budgetSaving.value = true
  try { await submitBudgetAdjustment({ ...scope, 申请原因: budgetForm.申请原因, 申请预算: budgetForm.申请预算 }); ElMessage.success('预算调整申请已提交'); budgetForm.申请原因 = ''; await loadBudgetHistory() } catch (error: any) { ElMessage.error(error.message || '提交预算调整失败') } finally { budgetSaving.value = false }
}
</script>

<style scoped>
.finance-controls-page { padding: 16px; background: #f5f7fa; min-height: 100%; }
.header-card, .scope-card, .section-card { margin-bottom: 16px; }
.page-header, .section-header, .inline-actions, .section-actions { display: flex; align-items: center; gap: 12px; }
.page-header, .section-header { justify-content: space-between; }
.page-header h2 { margin: 0 0 6px; font-size: 22px; color: #1f2d3d; }
.subtitle, .muted { color: #6b7280; font-size: 13px; margin: 0; }
.section-title { font-weight: 600; color: #1f2d3d; }
.section-actions { justify-content: flex-end; margin-top: 14px; }
.scope-card :deep(.el-form-item) { margin-bottom: 0; }
.scope-card :deep(.el-input-number) { width: 130px; }
.inline-actions { flex-wrap: wrap; }
.muted { margin-top: 12px; line-height: 1.6; }
@media (max-width: 900px) { .finance-controls-page { padding: 8px; } .page-header { align-items: flex-start; flex-direction: column; } }
</style>
