<template>
  <div class="further-education-page">
    <el-card shadow="never" class="page-header-card">
      <div class="page-header">
        <div>
          <h2>人员进修管理</h2>
          <p class="subtitle">年度计划、个人申请、可配置审批、预算预占与审计归档</p>
        </div>
        <div class="header-actions">
          <el-button type="primary" :icon="Plus" @click="openApplication">发起进修申请</el-button>
          <el-button :icon="Refresh" :loading="loading" @click="refreshAll">刷新</el-button>
        </div>
      </div>
    </el-card>

    <el-row :gutter="16" class="kpi-row">
      <el-col :xs="12" :sm="6"><el-card shadow="hover" class="kpi-card"><span>年度计划</span><strong>{{ plans.length }}</strong><small>当前查询年度</small></el-card></el-col>
      <el-col :xs="12" :sm="6"><el-card shadow="hover" class="kpi-card"><span>我的申请</span><strong>{{ applications.length }}</strong><small>草稿及流程中</small></el-card></el-col>
      <el-col :xs="12" :sm="6"><el-card shadow="hover" class="kpi-card"><span>待办理</span><strong>{{ todoList.length }}</strong><small>按当前账号权限</small></el-card></el-col>
      <el-col :xs="12" :sm="6"><el-card shadow="hover" class="kpi-card"><span>预算可用</span><strong>¥ {{ money(budget?.可用余额 || 0) }}</strong><small>服务端台账</small></el-card></el-col>
    </el-row>

    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab" @tab-change="onTabChange">
        <el-tab-pane label="年度计划" name="plans">
          <div class="toolbar">
            <el-form :inline="true" :model="planQuery">
              <el-form-item label="年度"><el-input-number v-model="planQuery.计划年度" :min="2020" :max="2100" :controls="false" /></el-form-item>
              <el-form-item label="状态"><el-select v-model="planQuery.状态" clearable placeholder="全部" style="width: 130px"><el-option label="草稿" value="草稿" /><el-option label="已发布" value="已发布" /><el-option label="已锁定" value="已锁定" /></el-select></el-form-item>
              <el-form-item><el-button type="primary" :icon="Search" @click="fetchPlans">查询</el-button></el-form-item>
            </el-form>
            <el-button type="success" :icon="Plus" @click="openPlan">新增年度计划</el-button>
          </div>
          <el-table v-loading="loadingPlans" :data="plans" border stripe>
            <el-table-column prop="计划年度" label="年度" width="85" />
            <el-table-column prop="科室名称" label="科室" width="150" />
            <el-table-column prop="专业方向" label="专业方向" min-width="150" />
            <el-table-column prop="目标医院" label="目标医院" min-width="180" />
            <el-table-column prop="拟派人数" label="人数" width="80" align="center" />
            <el-table-column label="总预算（元）" width="130" align="right"><template #default="{ row }">¥ {{ money(totalPlanBudget(row)) }}</template></el-table-column>
            <el-table-column prop="状态" label="状态" width="100"><template #default="{ row }"><el-tag :type="statusType(row.状态)">{{ row.状态 || '草稿' }}</el-tag></template></el-table-column>
            <el-table-column label="操作" width="155" fixed="right"><template #default="{ row }"><el-button link type="primary" @click="openPlan(row)">编辑</el-button><el-button v-if="row.状态 === '草稿' || !row.状态" link type="success" @click="publishPlan(row)">发布</el-button></template></el-table-column>
          </el-table>
          <el-empty v-if="!loadingPlans && !plans.length" description="暂无年度计划，请先新增或等待科教科发布" />
        </el-tab-pane>

        <el-tab-pane label="我的申请" name="applications">
          <div class="toolbar"><el-form :inline="true"><el-form-item label="状态"><el-select v-model="applicationQuery.状态" clearable placeholder="全部" style="width: 140px"><el-option label="草稿" value="草稿" /><el-option label="审批中" value="审批中" /><el-option label="已通过" value="已通过" /><el-option label="已退回" value="已退回" /></el-select></el-form-item><el-form-item><el-button type="primary" :icon="Search" @click="fetchApplications">查询</el-button></el-form-item></el-form><el-button type="primary" :icon="Plus" @click="openApplication">新建申请</el-button></div>
          <el-table v-loading="loadingApplications" :data="applications" border stripe>
            <el-table-column prop="编号" label="编号" width="80" />
            <el-table-column prop="进修医院" label="进修医院" min-width="180" />
            <el-table-column prop="专业方向" label="专业方向" min-width="140" />
            <el-table-column label="进修时间" width="205"><template #default="{ row }">{{ row.开始日期 || '-' }} 至 {{ row.结束日期 || '-' }}</template></el-table-column>
            <el-table-column prop="预算金额" label="申请预算（元）" width="130" align="right"><template #default="{ row }">¥ {{ money(row.预算金额 || calcApplicationBudget(row)) }}</template></el-table-column><el-table-column prop="预算状态" label="预算状态" width="100"><template #default="{ row }"><el-tag :type="row.预算状态 === '已占用' ? 'warning' : row.预算状态 === '已释放' ? 'info' : 'success'">{{ row.预算状态 || '未占用' }}</el-tag></template></el-table-column>
            <el-table-column prop="当前步骤" label="当前节点" min-width="135" />
            <el-table-column prop="状态" label="状态" width="100"><template #default="{ row }"><el-tag :type="statusType(row.状态)">{{ row.状态 || '草稿' }}</el-tag></template></el-table-column>
            <el-table-column label="操作" width="220" fixed="right"><template #default="{ row }"><el-button link type="primary" :disabled="row.状态 && row.状态 !== '草稿'" @click="openApplication(row)">编辑</el-button><el-button v-if="!row.状态 || row.状态 === '草稿'" link type="success" @click="submitApplication(row)">提交</el-button><el-button v-if="row.预算状态 === '未占用' && row.编号" link type="warning" @click="lockBudget(row)">预占</el-button><el-button v-if="row.预算状态 === '已占用' && row.编号" link type="danger" @click="releaseBudget(row)">释放</el-button><el-button link @click="viewAudit(row)">审计</el-button></template></el-table-column>
          </el-table>
          <el-empty v-if="!loadingApplications && !applications.length" description="暂无申请记录" />
        </el-tab-pane>

        <el-tab-pane label="我的待办与通知" name="todo">
          <div class="notice-line">待办由服务端按角色和数据范围返回，不能在前端伪造审批状态。</div>
          <el-table v-loading="loadingTodo" :data="todoList" border stripe>
            <el-table-column prop="流程编号" label="流程" width="90" /><el-table-column prop="流程名称" label="流程名称" min-width="180" /><el-table-column prop="步骤名称" label="当前步骤" min-width="150" /><el-table-column prop="流程创建时间" label="提交时间" width="170" /><el-table-column prop="是否可处理" label="可处理" width="90"><template #default="{ row }"><el-tag :type="row.是否可处理 ? 'warning' : 'info'">{{ row.是否可处理 ? '待办理' : '只读' }}</el-tag></template></el-table-column>
          </el-table><el-empty v-if="!loadingTodo && !todoList.length" description="暂无待办理事项" />
          <h3 class="section-title">进修通知</h3>
          <el-table :data="notifications" border stripe>
            <el-table-column prop="标题" label="通知" min-width="220" />
            <el-table-column prop="通知类型" label="类型" width="100" />
            <el-table-column prop="创建时间" label="时间" width="175" />
            <el-table-column label="状态" width="100"><template #default="{ row }"><el-tag :type="row.是否已读 ? 'info' : 'warning'">{{ row.是否已读 ? '已读' : '未读' }}</el-tag></template></el-table-column>
            <el-table-column label="操作" width="90"><template #default="{ row }"><el-button v-if="!row.是否已读" link type="primary" @click="markNotificationRead(row)">标为已读</el-button></template></el-table-column>
          </el-table>
          <el-empty v-if="!notifications.length" description="暂无进修通知" />
        </el-tab-pane>

        <el-tab-pane label="审批流程配置" name="workflow">
          <div class="notice-line">流程模板发布后，新申请使用新版本；已运行申请继续使用原版本。</div>
          <div v-for="template in workflows" :key="template.编号 || template.名称" class="workflow-card">
            <div class="workflow-head"><div><strong>{{ template.名称 }}</strong><el-tag size="small" :type="template.是否已发布 ? 'success' : 'info'" class="ml-8">{{ template.是否已发布 ? '已发布' : '草稿' }}</el-tag><span class="version">v{{ template.版本号 || 1 }}</span></div><div><el-button link type="primary" @click="editWorkflow(template)">编辑节点</el-button><el-button v-if="template.编号 && !template.是否已发布" link type="success" @click="publishWorkflow(template)">发布</el-button></div></div>
            <el-steps :active="(template.节点列表 || []).length" finish-status="success" simple><el-step v-for="node in template.节点列表 || []" :key="node.编号 || node.顺序" :title="node.节点名称" :description="node.审批角色" /></el-steps>
          </div>
          <el-empty v-if="!workflows.length" description="服务端尚未返回流程模板" />
        </el-tab-pane>

        <el-tab-pane label="预算与审计" name="budget">
          <div class="budget-summary" v-if="budget"><div><span>批准预算</span><strong>¥ {{ money(budget.批准预算) }}</strong></div><div><span>审批中</span><strong>¥ {{ money(budget.审批中金额) }}</strong></div><div><span>已预占</span><strong>¥ {{ money(budget.已预占金额) }}</strong></div><div><span>已核销</span><strong>¥ {{ money(budget.已核销金额) }}</strong></div><div class="available"><span>可用余额</span><strong>¥ {{ money(budget.可用余额) }}</strong></div></div><el-empty v-else description="暂无预算台账" />
          <h3 class="section-title">最近审计记录</h3><el-table v-loading="loadingAudit" :data="auditItems" border stripe><el-table-column prop="操作时间" label="时间" width="175" /><el-table-column prop="操作类型" label="操作" width="130" /><el-table-column prop="操作人姓名" label="操作人" width="120" /><el-table-column prop="原状态" label="原状态" width="100" /><el-table-column prop="新状态" label="新状态" width="100" /><el-table-column prop="说明" label="说明" min-width="220" show-overflow-tooltip /></el-table><el-empty v-if="!loadingAudit && !auditItems.length" description="暂无审计记录" />
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <el-dialog v-model="planDialog.visible" :title="planDialog.form.编号 ? '编辑年度计划' : '新增年度计划'" width="680px" destroy-on-close>
      <el-form ref="planFormRef" :model="planDialog.form" :rules="planRules" label-width="115px"><el-row :gutter="12"><el-col :span="12"><el-form-item label="计划年度" prop="计划年度"><el-input-number v-model="planDialog.form.计划年度" :min="2020" :max="2100" /></el-form-item></el-col><el-col :span="12"><el-form-item label="拟派人数" prop="拟派人数"><el-input-number v-model="planDialog.form.拟派人数" :min="1" /></el-form-item></el-col></el-row><el-form-item label="科室编号" prop="科室编号"><el-input-number v-model="planDialog.form.科室编号" :min="1" :controls="false" placeholder="管理人员填写" /></el-form-item><el-form-item label="科室名称"><el-input v-model="planDialog.form.科室名称" placeholder="可由服务端按账号带出" /></el-form-item><el-form-item label="专业方向" prop="专业方向"><el-input v-model="planDialog.form.专业方向" /></el-form-item><el-form-item label="目标医院"><el-input v-model="planDialog.form.目标医院" /></el-form-item><el-row :gutter="12"><el-col :span="12"><el-form-item label="培训费预算"><el-input-number v-model="planDialog.form.培训费预算" :min="0" :precision="2" /></el-form-item></el-col><el-col :span="12"><el-form-item label="交通费预算"><el-input-number v-model="planDialog.form.交通费预算" :min="0" :precision="2" /></el-form-item></el-col><el-col :span="12"><el-form-item label="住宿费预算"><el-input-number v-model="planDialog.form.住宿费预算" :min="0" :precision="2" /></el-form-item></el-col><el-col :span="12"><el-form-item label="补助预算"><el-input-number v-model="planDialog.form.补助预算" :min="0" :precision="2" /></el-form-item></el-col></el-row><el-form-item label="允许计划外"><el-switch v-model="planDialog.form.是否允许计划外" /></el-form-item><el-form-item label="备注"><el-input v-model="planDialog.form.备注" type="textarea" :rows="2" /></el-form-item></el-form>
      <template #footer><el-button @click="planDialog.visible = false">取消</el-button><el-button type="primary" :loading="saving" @click="savePlan">保存</el-button></template>
    </el-dialog>

    <el-dialog v-model="applicationDialog.visible" :title="applicationDialog.form.编号 ? '编辑进修申请' : '新建进修申请'" width="760px" destroy-on-close>
      <!-- 管理人员代办时可指定申请科室；普通申请人留空，由后端从当前账号解析部门。 -->
      <el-form ref="applicationFormRef" :model="applicationDialog.form" :rules="applicationRules" label-width="120px"><el-row :gutter="12"><el-col :span="12"><el-form-item label="关联年度计划"><el-select v-model="applicationDialog.form.计划编号" clearable placeholder="选择已发布计划" style="width: 100%"><el-option v-for="plan in plans.filter(p => p.状态 === '已发布' || !p.状态)" :key="plan.编号" :label="`${plan.计划年度}｜${plan.科室名称 || '全院'}｜${plan.专业方向 || '-'}`" :value="plan.编号" /></el-select></el-form-item></el-col><el-col :span="12"><el-form-item label="是否计划外"><el-switch v-model="applicationDialog.form.是否计划外" /></el-form-item></el-col></el-row><el-form-item label="申请科室编号"><el-input-number v-model="applicationDialog.form.部门编号" :min="0" :controls="false" placeholder="管理人员代办时填写" /></el-form-item><el-form-item label="进修医院" prop="进修医院"><el-input v-model="applicationDialog.form.进修医院" /></el-form-item><el-row :gutter="12"><el-col :span="12"><el-form-item label="进修基地"><el-input v-model="applicationDialog.form.进修基地" /></el-form-item></el-col><el-col :span="12"><el-form-item label="专业方向" prop="专业方向"><el-input v-model="applicationDialog.form.专业方向" /></el-form-item></el-col></el-row><el-form-item label="导师"><el-input v-model="applicationDialog.form.导师" /></el-form-item><el-row :gutter="12"><el-col :span="12"><el-form-item label="开始日期" prop="开始日期"><el-date-picker v-model="applicationDialog.form.开始日期" value-format="YYYY-MM-DD" type="date" style="width: 100%" /></el-form-item></el-col><el-col :span="12"><el-form-item label="结束日期" prop="结束日期"><el-date-picker v-model="applicationDialog.form.结束日期" value-format="YYYY-MM-DD" type="date" style="width: 100%" /></el-form-item></el-col></el-row><el-row :gutter="12"><el-col :span="12"><el-form-item label="培训费"><el-input-number v-model="applicationDialog.form.培训费" :min="0" :precision="2" /></el-form-item></el-col><el-col :span="12"><el-form-item label="交通费"><el-input-number v-model="applicationDialog.form.交通费" :min="0" :precision="2" /></el-form-item></el-col><el-col :span="12"><el-form-item label="住宿费"><el-input-number v-model="applicationDialog.form.住宿费" :min="0" :precision="2" /></el-form-item></el-col><el-col :span="12"><el-form-item label="补助费"><el-input-number v-model="applicationDialog.form.补助费" :min="0" :precision="2" /></el-form-item></el-col></el-row><el-form-item v-if="applicationDialog.form.是否计划外" label="计划外原因" prop="计划外原因"><el-input v-model="applicationDialog.form.计划外原因" type="textarea" /></el-form-item><el-form-item label="申请理由" prop="申请理由"><el-input v-model="applicationDialog.form.申请理由" type="textarea" :rows="3" /></el-form-item><div class="budget-preview">本次申请预算：<strong>¥ {{ money(calcApplicationBudget(applicationDialog.form)) }}</strong>；保存后由服务端执行预算校验和预占。</div></el-form>
      <template #footer><el-button @click="applicationDialog.visible = false">取消</el-button><el-button type="primary" :loading="saving" @click="saveApplication(false)">保存草稿</el-button><el-button type="success" :loading="saving" @click="saveApplication(true)">保存并提交</el-button></template>
    </el-dialog>

    <el-dialog v-model="workflowDialog.visible" title="编辑审批节点" width="780px" destroy-on-close>
      <el-form :model="workflowDialog.form" label-width="105px"><el-form-item label="模板名称"><el-input v-model="workflowDialog.form.名称" /></el-form-item><el-form-item label="说明"><el-input v-model="workflowDialog.form.说明" /></el-form-item><div v-for="(node, index) in workflowDialog.form.节点列表" :key="node.编号 || index" class="node-row"><el-input-number v-model="node.顺序" :min="1" :controls="false" /><el-input v-model="node.节点名称" placeholder="节点名称" /><el-select v-model="node.节点类型" style="width: 125px"><el-option label="审批" value="审批" /><el-option label="备案" value="备案" /><el-option label="自动校验" value="自动" /></el-select><el-input v-model="node.审批角色" placeholder="审批角色" /><el-input-number v-model="node.办理期限天数" :min="1" placeholder="天数" /><el-switch v-model="node.是否启用" /><el-button circle text type="danger" :icon="Delete" @click="workflowDialog.form.节点列表.splice(index, 1)" /></div><el-button type="primary" link :icon="Plus" @click="addWorkflowNode">增加节点</el-button></el-form>
      <template #footer><el-button @click="workflowDialog.visible = false">取消</el-button><el-button type="primary" :loading="saving" @click="saveWorkflow">保存模板</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Delete, Plus, Refresh, Search } from '@element-plus/icons-vue'
import type { WorkflowTaskItem } from '@/api/workflow'
import {
  getFurtherEducationBudgetLedger,
  lockFurtherEducationBudget,
  listFurtherEducationApplications,
  listFurtherEducationAudit,
  listFurtherEducationNotifications,
  listFurtherEducationTodos,
  listFurtherEducationPlans,
  listFurtherEducationWorkflowTemplates,
  publishFurtherEducationPlan,
  publishFurtherEducationWorkflowTemplate,
  releaseFurtherEducationBudget,
  saveFurtherEducationApplication,
  saveFurtherEducationPlan,
  saveFurtherEducationWorkflowTemplate,
  submitFurtherEducationApplication,
  markFurtherEducationNotificationRead,
  type FurtherEducationApplication,
  type FurtherEducationAuditItem,
  type FurtherEducationBudgetLedger,
  type FurtherEducationPlan,
  type FurtherEducationNotification,
  type FurtherEducationTodo,
  type FurtherEducationWorkflowNode,
  type FurtherEducationWorkflowTemplate
} from '@/api/further-education'

const activeTab = ref('plans')
const route = useRoute()
const loading = ref(false)
const saving = ref(false)
const loadingPlans = ref(false)
const loadingApplications = ref(false)
const loadingTodo = ref(false)
const loadingAudit = ref(false)
const plans = ref<FurtherEducationPlan[]>([])
const applications = ref<FurtherEducationApplication[]>([])
const todoList = ref<WorkflowTaskItem[]>([])
const notifications = ref<FurtherEducationNotification[]>([])
const workflows = ref<FurtherEducationWorkflowTemplate[]>([])
const budget = ref<FurtherEducationBudgetLedger | null>(null)
const auditItems = ref<FurtherEducationAuditItem[]>([])
const planFormRef = ref<FormInstance>()
const applicationFormRef = ref<FormInstance>()
const planQuery = reactive({ 计划年度: new Date().getFullYear() + 1, 状态: '' })
const applicationQuery = reactive<{ 状态?: string }>({ 状态: '' })
const planDialog = reactive<{ visible: boolean; form: FurtherEducationPlan }>({ visible: false, form: emptyPlan() })
const applicationDialog = reactive<{ visible: boolean; form: FurtherEducationApplication }>({ visible: false, form: emptyApplication() })
const workflowDialog = reactive<{ visible: boolean; form: FurtherEducationWorkflowTemplate }>({ visible: false, form: emptyWorkflow() })
const planRules: FormRules = { 计划年度: [{ required: true, message: '请选择年度', trigger: 'change' }], 科室编号: [{ required: true, message: '请输入科室编号', trigger: 'change' }], 专业方向: [{ required: true, message: '请输入专业方向', trigger: 'blur' }] }
const applicationRules: FormRules = { 进修医院: [{ required: true, message: '请输入进修医院', trigger: 'blur' }], 专业方向: [{ required: true, message: '请输入专业方向', trigger: 'blur' }], 开始日期: [{ required: true, message: '请选择开始日期', trigger: 'change' }], 结束日期: [{ required: true, message: '请选择结束日期', trigger: 'change' }], 申请理由: [{ required: true, message: '请填写申请理由', trigger: 'blur' }], 计划外原因: [{ required: true, message: '计划外申请需要填写原因', trigger: 'blur' }] }

function emptyPlan(): FurtherEducationPlan { return { 计划年度: planQuery.计划年度, 拟派人数: 1, 培训费预算: 0, 交通费预算: 0, 住宿费预算: 0, 补助预算: 0, 是否允许计划外: false, 专业方向: '' } }
function emptyApplication(): FurtherEducationApplication { return { 进修医院: '', 进修基地: '', 专业方向: '', 导师: '', 开始日期: '', 结束日期: '', 培训费: 0, 交通费: 0, 住宿费: 0, 补助费: 0, 是否计划外: false, 申请理由: '' } }
function emptyWorkflow(): FurtherEducationWorkflowTemplate { return { 名称: '人员进修默认审批流程', 是否启用: true, 是否已发布: false, 节点列表: [{ 顺序: 1, 节点名称: '科主任审批', 节点类型: '审批', 审批角色: '科主任', 是否启用: true, 办理期限天数: 5 }, { 顺序: 2, 节点名称: '分管领导审批', 节点类型: '审批', 审批角色: '分管领导', 是否启用: true, 办理期限天数: 5 }, { 顺序: 3, 节点名称: '科教科备案', 节点类型: '备案', 审批角色: '科教科', 是否启用: true, 办理期限天数: 3 }] } }
function money(value: unknown) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function totalPlanBudget(row: FurtherEducationPlan) { return row.总预算 ?? Number(row.培训费预算 || 0) + Number(row.交通费预算 || 0) + Number(row.住宿费预算 || 0) + Number(row.补助预算 || 0) }
function calcApplicationBudget(row: Partial<FurtherEducationApplication>) { return Number(row.培训费 || 0) + Number(row.交通费 || 0) + Number(row.住宿费 || 0) + Number(row.补助费 || 0) }
function statusType(status?: string) { return status === '已通过' || status === '已发布' ? 'success' : status === '已退回' ? 'danger' : status === '审批中' ? 'warning' : 'info' }
function unwrapList<T>(result: any): T[] { return Array.isArray(result) ? result : (result?.list || result?.List || result?.items || []) }
function normalizePlan(row: any): FurtherEducationPlan {
  return {
    ...row,
    计划年度: row.计划年度 ?? row.年度,
    科室编号: row.科室编号 ?? row.部门编号,
    科室名称: row.科室名称 ?? row.部门名称,
    拟派人数: row.拟派人数 ?? row.计划人数,
    总预算: row.总预算 ?? row.预算总额
  }
}
function normalizeApplication(row: any): FurtherEducationApplication {
  return {
    ...row,
    计划编号: row.计划编号 ?? row.年度计划编号,
    进修医院: row.进修医院 ?? row.目标医院,
    专业方向: row.专业方向 ?? row.进修专业,
    开始日期: row.开始日期 ?? row.进修开始日期,
    结束日期: row.结束日期 ?? row.进修结束日期,
    申请理由: row.申请理由 ?? row.申请说明
  }
}

async function fetchPlans() { loadingPlans.value = true; try { plans.value = unwrapList<any>(await listFurtherEducationPlans(planQuery)).map(normalizePlan) } catch { plans.value = [] } finally { loadingPlans.value = false } }
async function fetchApplications() { loadingApplications.value = true; try { applications.value = unwrapList<any>(await listFurtherEducationApplications({ ...applicationQuery, Index: 1, Size: 100 })).map(normalizeApplication) } catch { applications.value = [] } finally { loadingApplications.value = false } }
async function fetchTodo() { loadingTodo.value = true; try { const [todoResult, notificationResult] = await Promise.all([listFurtherEducationTodos(true), listFurtherEducationNotifications(true)]); todoList.value = unwrapList<any>(todoResult).map((item: FurtherEducationTodo) => ({ 流程编号: item.进修申请编号, 流程模板编号: 0, 流程名称: item.标题 || '人员进修审批', 流程相关项目路径: '', 项目编号: item.进修申请编号, 状态: 0, 流程创建时间: item.创建时间 || '', 步骤编号: item.编号, 步骤名称: item.接收角色 || '进修审批', 步骤状态: 0, 是否可处理: item.状态 === '待处理' } as WorkflowTaskItem)); notifications.value = unwrapList<FurtherEducationNotification>(notificationResult) } catch { todoList.value = []; notifications.value = [] } finally { loadingTodo.value = false } }
async function fetchWorkflows() { try { workflows.value = unwrapList<FurtherEducationWorkflowTemplate>(await listFurtherEducationWorkflowTemplates()) } catch { workflows.value = [] } }
async function fetchBudgetAndAudit() { loadingAudit.value = true; try { budget.value = await getFurtherEducationBudgetLedger({ 计划年度: planQuery.计划年度 }) || null; auditItems.value = unwrapList<FurtherEducationAuditItem>(await listFurtherEducationAudit({ Limit: 30 })) } catch { budget.value = null; auditItems.value = [] } finally { loadingAudit.value = false } }
async function refreshAll() { loading.value = true; await Promise.allSettled([fetchPlans(), fetchApplications(), fetchTodo(), fetchWorkflows(), fetchBudgetAndAudit()]); loading.value = false }
async function onTabChange(name: string | number) { if (name === 'budget') await fetchBudgetAndAudit(); if (name === 'todo') await fetchTodo(); if (name === 'workflow') await fetchWorkflows() }
function openPlan(row?: FurtherEducationPlan) { planDialog.form = row ? { ...row } : emptyPlan(); planDialog.visible = true }
async function savePlan() { if (!(await planFormRef.value?.validate().catch(() => false))) return; saving.value = true; try { await saveFurtherEducationPlan(planDialog.form); ElMessage.success('年度计划已保存'); planDialog.visible = false; await fetchPlans() } catch { /* request interceptor displays server error */ } finally { saving.value = false } }
async function publishPlan(row: FurtherEducationPlan) { if (!row.编号) return; try { await publishFurtherEducationPlan(row.编号); ElMessage.success('年度计划已发布'); await fetchPlans() } catch { /* keep server state */ } }
function openApplication(row?: FurtherEducationApplication) { applicationDialog.form = row ? { ...row } : emptyApplication(); applicationDialog.visible = true }
async function saveApplication(andSubmit: boolean) { if (!(await applicationFormRef.value?.validate().catch(() => false))) return; saving.value = true; try { const result = await saveFurtherEducationApplication({ ...applicationDialog.form, 预算金额: calcApplicationBudget(applicationDialog.form) }); const id = applicationDialog.form.编号 || result?.编号; if (andSubmit && id) await submitFurtherEducationApplication(id); ElMessage.success(andSubmit ? '申请已保存并提交' : '申请草稿已保存'); applicationDialog.visible = false; await Promise.all([fetchApplications(), fetchTodo()]) } catch { /* do not report success when API failed */ } finally { saving.value = false } }
async function submitApplication(row: FurtherEducationApplication) { if (!row.编号) return; try { await submitFurtherEducationApplication(row.编号); ElMessage.success('申请已提交审批'); await Promise.all([fetchApplications(), fetchTodo()]) } catch { /* server error */ } }
async function lockBudget(row: FurtherEducationApplication) { if (!row.编号) return; try { await lockFurtherEducationBudget(row.编号); ElMessage.success('预算已预占'); await Promise.all([fetchApplications(), fetchBudgetAndAudit()]) } catch { /* server error */ } }
async function releaseBudget(row: FurtherEducationApplication) { if (!row.编号) return; try { await releaseFurtherEducationBudget(row.编号); ElMessage.success('预算已释放'); await Promise.all([fetchApplications(), fetchBudgetAndAudit()]) } catch { /* server error */ } }
async function markNotificationRead(row: FurtherEducationNotification) { if (!row.编号) return; try { await markFurtherEducationNotificationRead(row.编号); row.是否已读 = true } catch { /* server error */ } }
function editWorkflow(template: FurtherEducationWorkflowTemplate) { workflowDialog.form = JSON.parse(JSON.stringify(template)); workflowDialog.visible = true }
function addWorkflowNode() { workflowDialog.form.节点列表.push({ 顺序: workflowDialog.form.节点列表.length + 1, 节点名称: '', 节点类型: '审批', 审批角色: '', 是否启用: true, 办理期限天数: 3 } as FurtherEducationWorkflowNode) }
async function saveWorkflow() { saving.value = true; try { await saveFurtherEducationWorkflowTemplate(workflowDialog.form); ElMessage.success('流程模板已保存'); workflowDialog.visible = false; await fetchWorkflows() } catch { /* server error */ } finally { saving.value = false } }
async function publishWorkflow(template: FurtherEducationWorkflowTemplate) { if (!template.编号) return; try { await publishFurtherEducationWorkflowTemplate(template.编号); ElMessage.success('流程模板已发布'); await fetchWorkflows() } catch { /* server error */ } }
async function viewAudit(row: FurtherEducationApplication) { activeTab.value = 'budget'; await fetchBudgetAndAudit(); if (!auditItems.value.length) ElMessage.info(`申请 ${row.编号 || ''} 暂无审计记录`) }

onMounted(() => {
  const queryTab = String(route.query.tab || '')
  if (['plans', 'applications', 'todo', 'workflow', 'budget'].includes(queryTab)) activeTab.value = queryTab
  refreshAll()
})
</script>

<style scoped lang="scss">
.further-education-page { padding: 20px; min-height: 100%; background: #f5f7fa; }
.page-header-card, .main-card { border: 0; border-radius: 10px; margin-bottom: 16px; }
.page-header { display: flex; justify-content: space-between; align-items: center; gap: 16px; }
h2 { margin: 0 0 8px; font-size: 22px; color: #1f2937; } .subtitle { margin: 0; color: #7b8794; }
.header-actions { display: flex; gap: 8px; flex-wrap: wrap; }
.kpi-row { margin-bottom: 16px; } .kpi-card { border: 0; text-align: center; display: flex; flex-direction: column; gap: 4px; } .kpi-card span { color: #718096; font-size: 13px; } .kpi-card strong { font-size: 23px; color: #243b53; } .kpi-card small { color: #9aa5b1; }
.toolbar { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 10px; margin-bottom: 14px; } .toolbar .el-form { margin-bottom: 0; }
.notice-line { padding: 10px 14px; margin-bottom: 14px; color: #52606d; background: #f0f4f8; border-radius: 5px; font-size: 13px; }
.workflow-card { border: 1px solid #e4e7ed; border-radius: 8px; padding: 16px; margin-bottom: 12px; } .workflow-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; gap: 8px; } .version { margin-left: 8px; color: #9aa5b1; font-size: 12px; } .ml-8 { margin-left: 8px; }
.budget-summary { display: grid; grid-template-columns: repeat(5, minmax(120px, 1fr)); gap: 12px; margin-bottom: 25px; } .budget-summary > div { padding: 16px; background: #f8fafc; border-radius: 8px; } .budget-summary span, .budget-summary strong { display: block; } .budget-summary span { color: #718096; font-size: 13px; } .budget-summary strong { margin-top: 6px; color: #243b53; font-size: 20px; } .budget-summary .available { background: #ecfdf5; } .budget-summary .available strong { color: #059669; }
.section-title { color: #334e68; font-size: 16px; margin: 15px 0 10px; }
.budget-preview { padding: 8px 12px; background: #f0f4f8; border-radius: 4px; color: #52606d; } .budget-preview strong { color: #0f766e; }
.node-row { display: grid; grid-template-columns: 72px 1.4fr 120px 1.2fr 82px 45px 35px; gap: 8px; align-items: center; margin-bottom: 9px; }
@media (max-width: 800px) { .page-header { align-items: flex-start; flex-direction: column; } .budget-summary { grid-template-columns: repeat(2, 1fr); } .node-row { grid-template-columns: 1fr 1fr; } .further-education-page { padding: 10px; } }
</style>
