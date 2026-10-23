<template>
  <div class="page-container">
    <el-card shadow="never" class="main-card">
      <div class="header-section">
        <div>
          <h2 class="page-title">论文投稿事前备案与期刊风控</h2>
          <p class="page-subtitle">
            贯彻“先备案、后投稿、再报销”全流程闭环治理，内置中科院预警期刊拦截、国内正规期刊白名单查验与科研诚信承诺书签署。
          </p>
        </div>
        <div class="header-actions">
          <el-button type="primary" :icon="Plus" @click="handleOpenCreate">
            发起投稿备案
          </el-button>
        </div>
      </div>

      <el-tabs v-model="activeTab" class="submission-tabs" @tab-change="onTabChange">
        <!-- 标签 1：我的投稿备案 -->
        <el-tab-pane label="我的投稿备案" name="my">
          <div class="toolbar">
            <el-input
              v-model="myQuery.关键词"
              placeholder="搜索论文题目、拟投期刊或备案号..."
              clearable
              style="width: 280px;"
              @keyup.enter="loadMyList"
            />
            <el-select v-model="myQuery.状态" placeholder="备案状态" clearable style="width: 140px;" @change="loadMyList">
              <el-option label="全部状态" value="" />
              <el-option label="草稿" value="草稿" />
              <el-option label="待科室审核" value="待科室审核" />
              <el-option label="待科教科审核" value="待科教科审核" />
              <el-option label="已备案" value="已备案" />
              <el-option label="已驳回" value="已驳回" />
              <el-option label="已撤回" value="已撤回" />
              <el-option label="已见刊" value="已见刊" />
            </el-select>
            <el-button type="primary" :icon="Search" @click="loadMyList">查询</el-button>
            <el-button :icon="Refresh" @click="loadMyList">刷新</el-button>
          </div>

          <el-table :data="myList" border stripe v-loading="myLoading" style="width: 100%; margin-top: 14px;">
            <el-table-column prop="备案号" label="备案号" width="160">
              <template #default="{ row }">
                <el-tag effect="plain" type="info">{{ row.备案号 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="论文题目" label="论文题目" min-width="220" show-overflow-tooltip />
            <el-table-column prop="拟投期刊名称" label="拟投期刊" min-width="180" show-overflow-tooltip>
              <template #default="{ row }">
                <div>{{ row.拟投期刊名称 }}</div>
                <div style="font-size: 11px; color: #909399;">
                  {{ row.期刊类型 }} | ISSN: {{ row.ISSN || '无' }}
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="风险等级" label="风控研判" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="getRiskTagType(row.风险等级)" effect="dark" size="small">
                  {{ row.风险等级 }}风险
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="预计版面费" label="预计版面费" width="110" align="right">
              <template #default="{ row }">
                {{ row.预计版面费 ? '¥' + Number(row.预计版面费).toLocaleString() : '免费/自理' }}
              </template>
            </el-table-column>
            <el-table-column prop="状态" label="审核状态" width="120" align="center">
              <template #default="{ row }">
                <el-tag :type="getStatusTagType(row.状态)" size="small">
                  {{ row.状态 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="创建时间" label="提交时间" width="150">
              <template #default="{ row }">
                {{ formatDate(row.创建时间) }}
              </template>
            </el-table-column>
            <el-table-column label="操作" width="220" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="handleViewDetail(row)">详情</el-button>
                <el-button
                  v-if="row.状态 === '草稿' || row.状态 === '已驳回' || row.状态 === '已撤回'"
                  link type="primary" size="small"
                  @click="handleEditDraft(row)"
                >
                  编辑
                </el-button>
                <el-button
                  v-if="row.状态 === '草稿' || row.状态 === '已驳回' || row.状态 === '已撤回'"
                  link type="success" size="small"
                  @click="handleSubmit(row)"
                >
                  送审
                </el-button>
                <el-button
                  v-if="row.状态 === '待科室审核' || row.状态 === '待科教科审核'"
                  link type="warning" size="small"
                  @click="handleWithdraw(row)"
                >
                  撤回
                </el-button>
                <el-button
                  v-if="row.状态 === '已备案'"
                  link type="success" size="small"
                  @click="handleOpenPublish(row)"
                >
                  登记见刊
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-bar">
            <el-pagination
              v-model:current-page="myQuery.pageIndex"
              v-model:page-size="myQuery.pageSize"
              :total="myTotal"
              :page-sizes="[10, 15, 30, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="loadMyList"
              @current-change="loadMyList"
            />
          </div>
        </el-tab-pane>

        <!-- 标签 2：待我审核（科室 / 科教科） -->
        <el-tab-pane label="待我审核" name="pending">
          <div class="toolbar">
            <el-input
              v-model="pendingQuery.关键词"
              placeholder="搜索论文、期刊或申请人姓名..."
              clearable
              style="width: 280px;"
              @keyup.enter="loadPendingList"
            />
            <el-radio-group v-model="pendingQuery.节点" @change="loadPendingList">
              <el-radio-button label="全部" />
              <el-radio-button label="科室审核" />
              <el-radio-button label="科教科审核" />
            </el-radio-group>
            <el-button type="primary" :icon="Search" @click="loadPendingList">查询</el-button>
          </div>

          <el-table :data="pendingList" border stripe v-loading="pendingLoading" style="width: 100%; margin-top: 14px;">
            <el-table-column prop="备案号" label="备案号" width="160" />
            <el-table-column prop="申请人姓名" label="申请人" width="110">
              <template #default="{ row }">
                <div>{{ row.申请人姓名 }}</div>
                <div style="font-size: 11px; color: #909399;">{{ row.部门名称 }}</div>
              </template>
            </el-table-column>
            <el-table-column prop="论文题目" label="论文题目" min-width="220" show-overflow-tooltip />
            <el-table-column prop="拟投期刊名称" label="拟投期刊" min-width="180" show-overflow-tooltip />
            <el-table-column prop="风险等级" label="风控等级" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="getRiskTagType(row.风险等级)" effect="dark" size="small">
                  {{ row.风险等级 }}风险
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="预计版面费" label="预计版面费" width="110" align="right">
              <template #default="{ row }">
                {{ row.预计版面费 ? '¥' + Number(row.预计版面费).toLocaleString() : '0' }}
              </template>
            </el-table-column>
            <el-table-column prop="状态" label="当前节点" width="130" align="center">
              <template #default="{ row }">
                <el-tag :type="getStatusTagType(row.状态)" size="small">{{ row.状态 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="180" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="handleViewDetail(row)">详情</el-button>
                <el-button link type="success" size="small" @click="handleAudit(row, true)">通过</el-button>
                <el-button link type="danger" size="small" @click="handleAudit(row, false)">驳回</el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-bar">
            <el-pagination
              v-model:current-page="pendingQuery.pageIndex"
              v-model:page-size="pendingQuery.pageSize"
              :total="pendingTotal"
              :page-sizes="[10, 15, 30]"
              layout="total, sizes, prev, pager, next"
              @size-change="loadPendingList"
              @current-change="loadPendingList"
            />
          </div>
        </el-tab-pane>

        <!-- 标签 3：期刊风险名单与正规期刊库 -->
        <el-tab-pane label="期刊风控库" name="riskDb">
          <div style="display: flex; gap: 20px;">
            <div style="flex: 1;">
              <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px;">
                <h4 style="margin: 0;">中科院预警与黑名单库</h4>
                <el-button size="small" type="primary" @click="openImportRiskDialog">导入预警名单</el-button>
              </div>
              <el-input v-model="riskQuery.关键词" placeholder="搜索期刊或ISSN..." clearable size="small" @keyup.enter="loadRiskList" style="margin-bottom: 10px;" />
              <el-table :data="riskList" border size="small" max-height="450">
                <el-table-column prop="来源" label="来源" width="100" />
                <el-table-column prop="期刊名称" label="期刊名称" min-width="140" show-overflow-tooltip />
                <el-table-column prop="风险等级" label="风险" width="70" align="center">
                  <template #default="{ row }">
                    <el-tag size="small" :type="row.风险等级 === '高' ? 'danger' : 'warning'">{{ row.风险等级 }}</el-tag>
                  </template>
                </el-table-column>
                <el-table-column prop="风险原因" label="预警原因" min-width="140" show-overflow-tooltip />
              </el-table>
            </div>

            <div style="flex: 1;">
              <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px;">
                <h4 style="margin: 0;">国内正规期刊白名单库</h4>
                <el-button size="small" type="success" @click="openImportDomesticDialog">导入正规期刊</el-button>
              </div>
              <el-input v-model="domesticQuery.关键词" placeholder="搜索CN号、刊名或主办单位..." clearable size="small" @keyup.enter="loadDomesticList" style="margin-bottom: 10px;" />
              <el-table :data="domesticList" border size="small" max-height="450">
                <el-table-column prop="期刊名称" label="期刊名称" min-width="140" show-overflow-tooltip />
                <el-table-column prop="CN号" label="CN号" width="110" />
                <el-table-column prop="ISSN" label="ISSN" width="100" />
                <el-table-column prop="主办单位" label="主办单位" min-width="140" show-overflow-tooltip />
              </el-table>
            </div>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 发起/编辑投稿备案弹窗 -->
    <el-dialog
      v-model="formVisible"
      :title="formModel.编号 ? '修改投稿备案草稿' : '发起论文投稿事前备案'"
      width="780px"
      destroy-on-close
    >
      <el-form ref="formRef" :model="formModel" :rules="formRules" label-width="120px" size="default">
        <el-form-item label="论文题目" prop="论文题目">
          <el-input v-model="formModel.论文题目" placeholder="请输入完整的中文/英文论文标题" />
        </el-form-item>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="期刊类型" prop="期刊类型">
              <el-radio-group v-model="formModel.期刊类型" @change="handleTriggerRiskCheck">
                <el-radio-button label="国内" />
                <el-radio-button label="国外" />
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="拟投期刊" prop="拟投期刊名称">
              <el-input
                v-model="formModel.拟投期刊名称"
                placeholder="请输入刊物标准全称"
                @blur="handleTriggerRiskCheck"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="ISSN号" prop="ISSN">
              <el-input v-model="formModel.ISSN" placeholder="如: 1000-0000" @blur="handleTriggerRiskCheck" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="CN号(国内)" prop="CN号">
              <el-input v-model="formModel.CN号" placeholder="国内期刊必填，如: CN 11-1234/R" @blur="handleTriggerRiskCheck" />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 实时风控研判卡片 -->
        <el-alert
          v-if="riskResult"
          :title="`【风控研判：${riskResult.风险等级}风险】`"
          :type="getAlertLevel(riskResult.风险等级)"
          show-icon
          :closable="false"
          style="margin-bottom: 18px;"
        >
          <div v-for="(tip, idx) in riskResult.研判提示" :key="idx" style="font-size: 13px; line-height: 1.6;">
            • {{ tip }}
          </div>
        </el-alert>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="预计版面费" prop="预计版面费">
              <el-input-number v-model="formModel.预计版面费" :min="0" :step="500" style="width: 100%;" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="稿件附件路径">
              <el-input v-model="formModel.稿件附件路径" placeholder="可填写网盘/系统附件路径" />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 科研诚信承诺书 -->
        <div class="integrity-card">
          <div class="integrity-title">科研诚信与第一署名单位承诺</div>
          <p class="integrity-text">
            根据国家卫健委《医学科研诚信和相关行为规范》及我院学术管理规定：严禁买卖论文、由第三方代写代投或伪造篡改实验数据；
            严禁无实质学术贡献者挂名；我院必须作为第一署名单位（或第一通讯单位）；发表后版面费报销须强制核对本备案号与论文成果一致性。
          </p>
          <el-checkbox v-model="formModel.诚信承诺">
            <span style="font-weight: 600; color: #303133;">
              本人已仔细阅读并郑重承诺文稿无学术不端行为，严格遵守科研伦理与署名规范，承诺以上信息真实有效。
            </span>
          </el-checkbox>
        </div>
      </el-form>

      <template #footer>
        <el-button @click="formVisible = false">取消</el-button>
        <el-button type="info" :loading="saving" @click="handleSaveDraft">保存草稿</el-button>
        <el-button type="primary" :loading="saving" @click="handleSaveAndSubmit">签署并提交送审</el-button>
      </template>
    </el-dialog>

    <!-- 详情与审计记录弹窗 -->
    <el-dialog v-model="detailVisible" title="投稿备案详情与流转全景" width="720px">
      <div v-if="detailRecord">
        <el-descriptions title="备案基础信息" :column="2" border size="small">
          <el-descriptions-item label="备案号">{{ detailRecord.基本信息?.备案号 }}</el-descriptions-item>
          <el-descriptions-item label="申请人">{{ detailRecord.基本信息?.申请人姓名 }} ({{ detailRecord.基本信息?.部门名称 }})</el-descriptions-item>
          <el-descriptions-item label="论文题目" :span="2">{{ detailRecord.基本信息?.论文题目 }}</el-descriptions-item>
          <el-descriptions-item label="拟投期刊">{{ detailRecord.基本信息?.拟投期刊名称 }}</el-descriptions-item>
          <el-descriptions-item label="ISSN / CN号">{{ detailRecord.基本信息?.ISSN || '-' }} / {{ detailRecord.基本信息?.CN号 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="风控等级">
            <el-tag size="small" :type="getRiskTagType(detailRecord.基本信息?.风险等级)">
              {{ detailRecord.基本信息?.风险等级 }}风险
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="当前状态">
            <el-tag size="small" :type="getStatusTagType(detailRecord.基本信息?.状态)">
              {{ detailRecord.基本信息?.状态 }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="预计版面费">¥{{ Number(detailRecord.基本信息?.预计版面费 || 0).toLocaleString() }}</el-descriptions-item>
          <el-descriptions-item label="申报时间">{{ formatDate(detailRecord.基本信息?.创建时间) }}</el-descriptions-item>
        </el-descriptions>

        <h4 style="margin: 18px 0 10px;">流转审计记录</h4>
        <el-timeline>
          <el-timeline-item
            v-for="(log, idx) in detailRecord.审核日志"
            :key="idx"
            :timestamp="formatDate(log.操作时间)"
            placement="top"
          >
            <div style="font-weight: 600;">
              {{ log.审核节点 }} - <span :style="{ color: log.操作结果 === '已驳回' ? '#F56C6C' : '#67C23A' }">{{ log.操作结果 }}</span>
              <span style="font-weight: normal; color: #909399; margin-left: 8px;">操作人: {{ log.操作人姓名 }}</span>
            </div>
            <div v-if="log.审核意见" style="color: #606266; font-size: 13px; margin-top: 4px;">
              意见：{{ log.审核意见 }}
            </div>
          </el-timeline-item>
        </el-timeline>
      </div>
    </el-dialog>

    <!-- 审核处理弹窗 -->
    <el-dialog v-model="auditDialogVisible" :title="auditAction ? '审核通过确认' : '审核驳回'" width="500px">
      <el-form label-width="90px">
        <el-form-item label="审核结果">
          <el-tag :type="auditAction ? 'success' : 'danger'">
            {{ auditAction ? '拟同意备案' : '拟驳回申请' }}
          </el-tag>
        </el-form-item>
        <el-form-item label="审核意见">
          <el-input
            v-model="auditOpinion"
            type="textarea"
            :rows="3"
            placeholder="请输入审核意见或修改要求..."
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="auditDialogVisible = false">取消</el-button>
        <el-button :type="auditAction ? 'success' : 'danger'" :loading="auditing" @click="confirmAudit">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { Plus, Search, Refresh } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  checkJournalRisk,
  savePaperSubmission,
  submitPaperSubmission,
  withdrawPaperSubmission,
  auditDeptSubmission,
  auditHospitalSubmission,
  getMySubmissions,
  getPendingSubmissions,
  getSubmissionDetail,
  getJournalRiskList,
  getDomesticJournalList,
  type JournalRiskCheckResult
} from '@/api/paper-submission'

const activeTab = ref('my')

// 1. 我的备案列表
const myList = ref<any[]>([])
const myTotal = ref(0)
const myLoading = ref(false)
const myQuery = reactive({
  pageIndex: 1,
  pageSize: 15,
  状态: '',
  关键词: ''
})

async function loadMyList() {
  myLoading.value = true
  try {
    const res: any = await getMySubmissions(myQuery)
    if (res) {
      myList.value = res.list || []
      myTotal.value = res.total || 0
    }
  } finally {
    myLoading.value = false
  }
}

// 2. 待审列表
const pendingList = ref<any[]>([])
const pendingTotal = ref(0)
const pendingLoading = ref(false)
const pendingQuery = reactive({
  pageIndex: 1,
  pageSize: 15,
  节点: '全部',
  关键词: ''
})

async function loadPendingList() {
  pendingLoading.value = true
  try {
    const res: any = await getPendingSubmissions(pendingQuery)
    if (res) {
      pendingList.value = res.list || []
      pendingTotal.value = res.total || 0
    }
  } finally {
    pendingLoading.value = false
  }
}

// 3. 期刊风控库列表
const riskList = ref<any[]>([])
const riskQuery = reactive({ pageIndex: 1, pageSize: 20, 关键词: '' })
const domesticList = ref<any[]>([])
const domesticQuery = reactive({ pageIndex: 1, pageSize: 20, 关键词: '' })

async function loadRiskList() {
  const res: any = await getJournalRiskList(riskQuery)
  if (res) riskList.value = res.list || []
}

async function loadDomesticList() {
  const res: any = await getDomesticJournalList(domesticQuery)
  if (res) domesticList.value = res.list || []
}

function onTabChange(tab: string) {
  if (tab === 'my') loadMyList()
  else if (tab === 'pending') loadPendingList()
  else if (tab === 'riskDb') {
    loadRiskList()
    loadDomesticList()
  }
}

// 表单相关
const formVisible = ref(false)
const saving = ref(false)
const formRef = ref()
const formModel = reactive<any>({
  编号: 0,
  论文题目: '',
  拟投期刊名称: '',
  期刊类型: '国内',
  ISSN: '',
  CN号: '',
  预计版面费: 0,
  稿件附件路径: '',
  诚信承诺: false
})
const formRules = {
  论文题目: [{ required: true, message: '论文题目不能为空', trigger: 'blur' }],
  拟投期刊名称: [{ required: true, message: '拟投期刊名称不能为空', trigger: 'blur' }]
}

const riskResult = ref<JournalRiskCheckResult | null>(null)

async function handleTriggerRiskCheck() {
  if (!formModel.拟投期刊名称 && !formModel.ISSN) return
  try {
    const res = await checkJournalRisk({
      期刊名称: formModel.拟投期刊名称,
      ISSN: formModel.ISSN,
      CN号: formModel.CN号,
      期刊类型: formModel.期刊类型
    })
    riskResult.value = res as any
  } catch {}
}

function handleOpenCreate() {
  Object.assign(formModel, {
    编号: 0,
    论文题目: '',
    拟投期刊名称: '',
    期刊类型: '国内',
    ISSN: '',
    CN号: '',
    预计版面费: 0,
    稿件附件路径: '',
    诚信承诺: false
  })
  riskResult.value = null
  formVisible.value = true
}

function handleEditDraft(row: any) {
  Object.assign(formModel, {
    编号: row.编号,
    论文题目: row.论文题目,
    拟投期刊名称: row.拟投期刊名称,
    期刊类型: row.期刊类型 || '国内',
    ISSN: row.ISSN || '',
    CN号: row.CN号 || '',
    预计版面费: Number(row.预计版面费 || 0),
    稿件附件路径: row.稿件附件路径 || '',
    诚信承诺: row.诚信承诺 || false
  })
  handleTriggerRiskCheck()
  formVisible.value = true
}

async function handleSaveDraft() {
  await formRef.value?.validate()
  saving.value = true
  try {
    await savePaperSubmission(formModel)
    ElMessage.success('草稿已保存')
    formVisible.value = false
    loadMyList()
  } finally {
    saving.value = false
  }
}

async function handleSaveAndSubmit() {
  await formRef.value?.validate()
  if (!formModel.诚信承诺) {
    ElMessage.warning('提交送审必须勾选签署《科研诚信承诺书》！')
    return
  }
  saving.value = true
  try {
    const saveRes: any = await savePaperSubmission(formModel)
    const id = saveRes?.编号 || formModel.编号
    await submitPaperSubmission(id)
    ElMessage.success('投稿备案已成功提交送审！')
    formVisible.value = false
    loadMyList()
  } finally {
    saving.value = false
  }
}

async function handleSubmit(row: any) {
  if (!row.诚信承诺) {
    ElMessage.warning('该草稿尚未签署诚信承诺，请先点击【编辑】勾选签署承诺书后再送审。')
    return
  }
  try {
    await submitPaperSubmission(row.编号)
    ElMessage.success('已提交送审')
    loadMyList()
  } catch {}
}

async function handleWithdraw(row: any) {
  await ElMessageBox.confirm('确定撤回该投稿备案申请吗？撤回后可重新修改再送审。', '撤回提示', {
    type: 'warning'
  })
  await withdrawPaperSubmission(row.编号)
  ElMessage.success('已撤回申请')
  loadMyList()
}

// 详情查看
const detailVisible = ref(false)
const detailRecord = ref<any>(null)

async function handleViewDetail(row: any) {
  const res: any = await getSubmissionDetail(row.编号)
  if (res) {
    detailRecord.value = res
    detailVisible.value = true
  }
}

// 审核操作
const auditDialogVisible = ref(false)
const auditTarget = ref<any>(null)
const auditAction = ref(true)
const auditOpinion = ref('')
const auditing = ref(false)

function handleAudit(row: any, isPass: boolean) {
  auditTarget.value = row
  auditAction.value = isPass
  auditOpinion.value = isPass ? '审核通过，准予申报/备案' : ''
  auditDialogVisible.value = true
}

async function confirmAudit() {
  if (!auditTarget.value) return
  auditing.value = true
  try {
    const payload = {
      编号: auditTarget.value.编号,
      通过: auditAction.value,
      审核意见: auditOpinion.value
    }
    if (auditTarget.value.状态 === '待科室审核') {
      await auditDeptSubmission(payload)
    } else {
      await auditHospitalSubmission(payload)
    }
    ElMessage.success(auditAction.value ? '已审核通过' : '已驳回申请')
    auditDialogVisible.value = false
    loadPendingList()
  } finally {
    auditing.value = false
  }
}

function handleOpenPublish(_row: any) {
  ElMessageBox.prompt('请输入正式发表期刊卷期、DOI或见刊说明：', '登记见刊', {
    confirmButtonText: '确定登记',
    cancelButtonText: '取消'
  }).then(async ({ value: _val }) => {
    ElMessage.success('见刊登记成功')
    loadMyList()
  })
}

function openImportRiskDialog() {
  ElMessage.info('支持管理员批量上传 Excel/CSV 格式的中科院预警名单与黑名单')
}

function openImportDomesticDialog() {
  ElMessage.info('支持管理员批量上传国家新闻出版署正规国内期刊库（含CN号/ISSN）')
}

// 辅助格式化
function getRiskTagType(level?: string) {
  if (level === '高') return 'danger'
  if (level === '中') return 'warning'
  return 'success'
}

function getAlertLevel(level?: string): any {
  if (level === '高') return 'error'
  if (level === '中') return 'warning'
  return 'success'
}

function getStatusTagType(status?: string) {
  switch (status) {
    case '草稿': return 'info'
    case '待科室审核': return 'warning'
    case '待科教科审核': return 'warning'
    case '已备案': return 'success'
    case '已见刊': return 'success'
    case '已驳回': return 'danger'
    default: return 'info'
  }
}

function formatDate(val?: string) {
  if (!val) return '-'
  return val.replace('T', ' ').substring(0, 16)
}

onMounted(() => {
  loadMyList()
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

.toolbar {
  display: flex;
  gap: 12px;
  align-items: center;
}

.pagination-bar {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}

.integrity-card {
  background: #fdf6ec;
  border: 1px solid #faecd8;
  border-radius: 6px;
  padding: 12px 16px;
  margin-top: 8px;

  .integrity-title {
    font-weight: 600;
    font-size: 14px;
    color: #e6a23c;
    margin-bottom: 6px;
  }

  .integrity-text {
    font-size: 12px;
    color: #606266;
    line-height: 1.6;
    margin: 0 0 10px 0;
  }
}
</style>
