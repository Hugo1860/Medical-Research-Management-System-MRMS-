<template>
  <div class="fund-execution-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span class="title">科研经费执行治理</span>
          <el-tag type="success">外拨经费 / 跨年结转 / 工作量与绩效分割 / 结项结余</el-tag>
        </div>
      </template>

      <div class="project-selector" style="margin-bottom: 20px; display: flex; gap: 15px; align-items: center">
        <span>当前项目编号：</span>
        <el-input-number v-model="currentProjectId" :min="1" style="width: 180px" />
        <el-select v-model="currentTemplateId" style="width: 180px">
          <el-option label="纵向项目立项 (模板401)" :value="401" />
          <el-option label="横向项目申请 (模板500)" :value="500" />
        </el-select>
        <el-button type="primary" @click="refreshAll">加载台账</el-button>
      </div>

      <el-tabs v-model="activeTab">
        <!-- 外拨经费 -->
        <el-tab-pane label="外拨经费管理" name="outsource">
          <div class="action-bar" style="margin-bottom: 12px">
            <el-button type="primary" @click="handleOpenOutsourceDialog">申请外拨经费</el-button>
          </div>
          <el-table :data="outsourceList" border style="width: 100%">
            <el-table-column prop="合作单位名称" label="合作单位" min-width="180" />
            <el-table-column prop="统一社会信用代码" label="信用代码" width="160" />
            <el-table-column prop="外拨金额" label="外拨申请金额(元)" width="150" align="right">
              <template #default="{ row }">
                {{ Number(row.外拨金额).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
              </template>
            </el-table-column>
            <el-table-column prop="已拨付金额" label="已拨付(元)" width="130" align="right">
              <template #default="{ row }">
                {{ Number(row.已拨付金额).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
              </template>
            </el-table-column>
            <el-table-column prop="状态" label="状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="row.状态 === '已批准' ? 'success' : row.状态 === '已驳回' ? 'danger' : 'info'">
                  {{ row.状态 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="160" align="center">
              <template #default="{ row }">
                <el-button v-if="row.状态 === '草稿' || row.状态 === '审核中'" link type="primary" @click="handleAuditOutsource(row, '已批准')">
                  批准
                </el-button>
                <el-button v-if="row.状态 === '草稿' || row.状态 === '审核中'" link type="danger" @click="handleAuditOutsource(row, '已驳回')">
                  驳回
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 经费结转 -->
        <el-tab-pane label="跨年度经费结转" name="carryover">
          <div class="action-bar" style="margin-bottom: 12px">
            <el-button type="primary" @click="handleOpenCarryoverDialog">申请跨年结转</el-button>
          </div>
          <el-table :data="carryoverList" border style="width: 100%">
            <el-table-column prop="结转来源年度" label="来源年度" width="100" align="center" />
            <el-table-column prop="结转目标年度" label="目标年度" width="100" align="center" />
            <el-table-column prop="可结转余额快照" label="可结转余额(元)" width="150" align="right">
              <template #default="{ row }">
                {{ Number(row.可结转余额快照).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
              </template>
            </el-table-column>
            <el-table-column prop="结转金额" label="结转金额(元)" width="150" align="right">
              <template #default="{ row }">
                {{ Number(row.结转金额).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
              </template>
            </el-table-column>
            <el-table-column prop="状态" label="状态" width="100" align="center" />
            <el-table-column prop="申请说明" label="结转说明" min-width="200" />
          </el-table>
        </el-tab-pane>

        <!-- 经费分割方案 -->
        <el-tab-pane label="工作量与绩效分割" name="split">
          <div class="action-bar" style="margin-bottom: 12px">
            <el-button type="primary" @click="handleOpenSplitDialog">新建分割方案</el-button>
          </div>
          <el-table :data="splitList" border style="width: 100%">
            <el-table-column prop="分割类型" label="分割类型" width="120" />
            <el-table-column prop="期间或年度" label="执行期间" width="120" />
            <el-table-column prop="基数金额" label="分割基数(元)" width="140" align="right">
              <template #default="{ row }">
                {{ Number(row.基数金额).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
              </template>
            </el-table-column>
            <el-table-column prop="状态" label="状态" width="100" align="center" />
            <el-table-column label="分割明细预览" min-width="250">
              <template #default="{ row }">
                <div v-for="d in (row.明细 || [])" :key="d.编号">
                  {{ d.人员姓名 }} ({{ d.部门名称 }}): {{ d.分割比例 }}% -> {{ d.分割金额 }}元
                </div>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 结项结余处理 -->
        <el-tab-pane label="结项结余处理" name="balance">
          <div class="action-bar" style="margin-bottom: 12px">
            <el-button type="primary" @click="handleOpenBalanceDialog">登记结余处理</el-button>
          </div>
          <el-table :data="balanceList" border style="width: 100%">
            <el-table-column prop="结余金额快照" label="结余金额(元)" width="150" align="right">
              <template #default="{ row }">
                {{ Number(row.结余金额快照).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
              </template>
            </el-table-column>
            <el-table-column prop="处理方式" label="处理方式" width="150" />
            <el-table-column prop="财务凭证号" label="财务凭证号" width="160" />
            <el-table-column prop="状态" label="状态" width="100" align="center" />
            <el-table-column prop="处理说明" label="说明" min-width="200" />
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 外拨申请弹窗 -->
    <el-dialog v-model="outsourceDialogVisible" title="外拨经费申请" width="550px">
      <el-form :model="outsourceForm" label-width="120px">
        <el-form-item label="合作单位名称" required>
          <el-input v-model="outsourceForm.合作单位名称" />
        </el-form-item>
        <el-form-item label="统一信用代码">
          <el-input v-model="outsourceForm.统一社会信用代码" />
        </el-form-item>
        <el-form-item label="外拨合同编号">
          <el-input v-model="outsourceForm.外拨合同编号" />
        </el-form-item>
        <el-form-item label="申请外拨金额" required>
          <el-input-number v-model="outsourceForm.外拨金额" :min="100" :step="1000" style="width: 100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="outsourceDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitOutsource">提交保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import {
  getOutsourceFunds,
  saveOutsourceFund,
  auditOutsourceFund,
  getFundCarryovers,
  getFundSplitSchemes,
  getProjectBalanceDisposals
} from '@/api/fund-execution'

const activeTab = ref('outsource')
const currentProjectId = ref(1)
const currentTemplateId = ref(401)

const outsourceList = ref<any[]>([])
const carryoverList = ref<any[]>([])
const splitList = ref<any[]>([])
const balanceList = ref<any[]>([])

const outsourceDialogVisible = ref(false)
const outsourceForm = ref<any>({
  流程模板编号: 401,
  项目编号: 1,
  合作单位名称: '',
  统一社会信用代码: '',
  外拨合同编号: '',
  外拨金额: 10000
})

async function refreshAll() {
  loadOutsource()
  loadCarryover()
  loadSplit()
  loadBalance()
}

async function loadOutsource() {
  try {
    const res = await getOutsourceFunds(currentTemplateId.value, currentProjectId.value)
    outsourceList.value = res || []
  } catch (err: any) {
    ElMessage.error(err.message || '加载外拨经费失败')
  }
}

async function loadCarryover() {
  try {
    const res = await getFundCarryovers(currentTemplateId.value, currentProjectId.value)
    carryoverList.value = res || []
  } catch (err: any) {
    ElMessage.error(err.message || '加载结转记录失败')
  }
}

async function loadSplit() {
  try {
    const res = await getFundSplitSchemes(currentTemplateId.value, currentProjectId.value)
    splitList.value = res || []
  } catch (err: any) {
    ElMessage.error(err.message || '加载分割方案失败')
  }
}

async function loadBalance() {
  try {
    const res = await getProjectBalanceDisposals(currentTemplateId.value, currentProjectId.value)
    balanceList.value = res || []
  } catch (err: any) {
    ElMessage.error(err.message || '加载结项结余失败')
  }
}

function handleOpenOutsourceDialog() {
  outsourceForm.value = {
    流程模板编号: currentTemplateId.value,
    项目编号: currentProjectId.value,
    合作单位名称: '',
    统一社会信用代码: '',
    外拨合同编号: '',
    外拨金额: 10000
  }
  outsourceDialogVisible.value = true
}

async function submitOutsource() {
  if (!outsourceForm.value.合作单位名称) {
    ElMessage.warning('合作单位名称必填')
    return
  }
  try {
    await saveOutsourceFund(outsourceForm.value)
    ElMessage.success('外拨申请保存成功')
    outsourceDialogVisible.value = false
    loadOutsource()
  } catch (err: any) {
    ElMessage.error(err.message || '保存失败')
  }
}

async function handleAuditOutsource(row: any, result: string) {
  try {
    await auditOutsourceFund({
      编号: row.编号,
      审核结果: result,
      审核意见: result === '已批准' ? '审核通过' : '退回修改'
    })
    ElMessage.success(`操作完成：${result}`)
    loadOutsource()
  } catch (err: any) {
    ElMessage.error(err.message || '审核失败')
  }
}

function handleOpenCarryoverDialog() {
  ElMessage.info('跨年度结转支持核验余额快照并流转财务')
}

function handleOpenSplitDialog() {
  ElMessage.info('方案支持按人员工作量或绩效比例进行100%刚性校验')
}

function handleOpenBalanceDialog() {
  ElMessage.info('结余处理支持按规定上缴、留归统筹或结转后续项目')
}

onMounted(() => {
  refreshAll()
})
</script>

<style scoped>
.fund-execution-container {
  padding: 16px;
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.title {
  font-size: 16px;
  font-weight: 600;
}
</style>
