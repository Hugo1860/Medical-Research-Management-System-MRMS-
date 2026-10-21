<template>
  <div class="academic-exchange-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span class="title">学术交流与会议治理</span>
          <el-tag type="warning">主办会议申请 / 派出与接收 / 统计分析</el-tag>
        </div>
      </template>

      <el-tabs v-model="activeTab">
        <!-- 主办会议 -->
        <el-tab-pane label="主办学术会议" name="conf">
          <div class="action-bar" style="margin-bottom: 12px">
            <el-button type="primary" @click="handleOpenConfDialog">申请主办会议</el-button>
            <el-button type="success" @click="handleExportCsv">导出统计 CSV</el-button>
          </div>
          <el-table :data="confList" border style="width: 100%">
            <el-table-column prop="会议名称" label="会议名称" min-width="180" />
            <el-table-column prop="会议级别" label="级别" width="90" align="center" />
            <el-table-column prop="主办单位" label="主办单位" min-width="150" />
            <el-table-column prop="负责人姓名" label="负责人" width="100" />
            <el-table-column prop="开始日期" label="开始日期" width="120">
              <template #default="{ row }">
                {{ row.开始日期 ? row.开始日期.substring(0, 10) : '' }}
              </template>
            </el-table-column>
            <el-table-column prop="预计参会人数" label="预计人数" width="100" align="center" />
            <el-table-column prop="经费预算" label="预算(元)" width="120" align="right">
              <template #default="{ row }">
                {{ Number(row.经费预算).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
              </template>
            </el-table-column>
            <el-table-column prop="状态" label="状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="row.状态 === '已批准' ? 'success' : row.状态 === '已驳回' ? 'danger' : 'info'">
                  {{ row.状态 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="150" align="center">
              <template #default="{ row }">
                <el-button v-if="row.状态 === '待审核'" link type="primary" @click="handleAuditConf(row, '已批准')">
                  批准
                </el-button>
                <el-button v-if="row.状态 === '待审核'" link type="danger" @click="handleAuditConf(row, '已驳回')">
                  驳回
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 派出与接收 -->
        <el-tab-pane label="人员派出与来访接收" name="person">
          <div class="action-bar" style="margin-bottom: 12px">
            <el-button type="primary" @click="handleOpenPersonDialog">登记交流人员</el-button>
          </div>
          <el-table :data="personList" border style="width: 100%">
            <el-table-column prop="交流方向" label="方向" width="90" align="center">
              <template #default="{ row }">
                <el-tag :type="row.交流方向 === '派出' ? 'primary' : 'success'">
                  {{ row.交流方向 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="人员姓名" width="130">
              <template #default="{ row }">
                {{ row.交流方向 === '派出' ? row.本院人员姓名 : row.外来人员姓名 }}
              </template>
            </el-table-column>
            <el-table-column prop="外来人员单位" label="单位/组织" min-width="150" />
            <el-table-column prop="交流类型" label="交流类型" width="120" />
            <el-table-column prop="目的地或来访地" label="地标" width="140" />
            <el-table-column prop="开始日期" label="开始日期" width="120">
              <template #default="{ row }">
                {{ row.开始日期 ? row.开始日期.substring(0, 10) : '' }}
              </template>
            </el-table-column>
            <el-table-column prop="状态" label="状态" width="100" align="center" />
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 会议申请弹窗 -->
    <el-dialog v-model="confDialogVisible" title="主办学术会议申请" width="600px">
      <el-form :model="confForm" label-width="120px">
        <el-form-item label="会议名称" required>
          <el-input v-model="confForm.会议名称" />
        </el-form-item>
        <el-form-item label="会议级别">
          <el-select v-model="confForm.会议级别" style="width: 100%">
            <el-option label="院级" value="院级" />
            <el-option label="市级" value="市级" />
            <el-option label="省级" value="省级" />
            <el-option label="全国" value="全国" />
            <el-option label="国际" value="国际" />
          </el-select>
        </el-form-item>
        <el-form-item label="主办单位" required>
          <el-input v-model="confForm.主办单位" />
        </el-form-item>
        <el-form-item label="举办地点">
          <el-input v-model="confForm.举办地点" />
        </el-form-item>
        <el-form-item label="预计人数">
          <el-input-number v-model="confForm.预计参会人数" :min="10" style="width: 100%" />
        </el-form-item>
        <el-form-item label="经费预算">
          <el-input-number v-model="confForm.经费预算" :min="0" :step="1000" style="width: 100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="confDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitConf">提交申请</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import {
  getHostedConferences,
  saveHostedConference,
  auditHostedConference,
  getAcademicExchangePersons
} from '@/api/academic-exchange'

const activeTab = ref('conf')
const confList = ref<any[]>([])
const personList = ref<any[]>([])

const confDialogVisible = ref(false)
const confForm = ref<any>({
  会议名称: '',
  会议级别: '院级',
  主办单位: '本院',
  举办地点: '学术会议中心',
  预计参会人数: 60,
  经费预算: 15000,
  开始日期: new Date().toISOString().substring(0, 10),
  结束日期: new Date().toISOString().substring(0, 10)
})

async function loadConferences() {
  try {
    const res = await getHostedConferences()
    confList.value = res || []
  } catch (err: any) {
    ElMessage.error(err.message || '加载学术会议失败')
  }
}

async function loadPersons() {
  try {
    const res = await getAcademicExchangePersons()
    personList.value = res || []
  } catch (err: any) {
    ElMessage.error(err.message || '加载交流人员失败')
  }
}

function handleOpenConfDialog() {
  confDialogVisible.value = true
}

async function submitConf() {
  if (!confForm.value.会议名称) {
    ElMessage.warning('会议名称必填')
    return
  }
  try {
    await saveHostedConference(confForm.value)
    ElMessage.success('会议申请提交成功')
    confDialogVisible.value = false
    loadConferences()
  } catch (err: any) {
    ElMessage.error(err.message || '提交失败')
  }
}

async function handleAuditConf(row: any, result: string) {
  try {
    await auditHostedConference({
      编号: row.编号,
      审核结果: result,
      审核意见: result === '已批准' ? '同意办会' : '退回修改'
    })
    ElMessage.success(`操作完成：${result}`)
    loadConferences()
  } catch (err: any) {
    ElMessage.error(err.message || '审核失败')
  }
}

function handleOpenPersonDialog() {
  ElMessage.info('人员派出与接收支持外宾资质及回执追溯')
}

function handleExportCsv() {
  window.open('/AcademicExchange/导出学术交流CSV', '_blank')
}

onMounted(() => {
  loadConferences()
  loadPersons()
})
</script>

<style scoped>
.academic-exchange-container {
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
