<template>
  <div class="community-healthcare-container">
    <div class="header-card">
      <div class="header-info">
        <h2 class="page-title">
          <el-icon class="title-icon"><Share /></el-icon>
          县域医共体科教协同与基层帮扶
        </h2>
        <p class="page-subtitle">
          支持总院（县人民医院/二级公立牵头医院）统一管理下属乡镇卫生院、社区卫生服务中心及分院区：进修接收（满6个月规范化考核）、适宜技术下乡指导、对口专家下沉帮扶与科教合作。
        </p>
      </div>
      <div class="header-actions">
        <el-button type="primary" :icon="Plus" @click="openAddTrainingDialog">
          登记进修 / 帮扶记录
        </el-button>
        <el-button type="success" plain :icon="OfficeBuilding" @click="openAddPartnerDialog">
          新增医共体合作单位
        </el-button>
        <el-button :icon="Refresh" @click="refreshAll">刷新</el-button>
      </div>
    </div>

    <!-- 顶部概览卡片 -->
    <el-row :gutter="16" class="metrics-row">
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="metric-card">
          <div class="metric-label">医共体合作单位</div>
          <div class="metric-value color-primary">{{ partners.length || 0 }} <span class="unit">家</span></div>
          <div class="metric-sub">乡镇卫生院 / 社区中心</div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="metric-card">
          <div class="metric-label">基层来院进修</div>
          <div class="metric-value color-success">{{ trainingSummary.进修人数 || 0 }} <span class="unit">人次</span></div>
          <div class="metric-sub">满6个月: {{ trainingSummary.满六个月人数 || 0 }} 人</div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="metric-card">
          <div class="metric-label">总院下乡帮扶</div>
          <div class="metric-value color-warning">{{ trainingSummary.帮扶人次 || 0 }} <span class="unit">人次</span></div>
          <div class="metric-sub">专家坐诊与技术下沉</div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="metric-card">
          <div class="metric-label">进修考核合格率</div>
          <div class="metric-value color-cyan">100%</div>
          <div class="metric-sub">规范化鉴定留痕</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 工作台 Tabs -->
    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab" @tab-change="handleTabChange">
        <!-- 标签页 1：进修与帮扶台账 -->
        <el-tab-pane label="基层进修与下乡帮扶台账" name="trainings">
          <div class="table-toolbar">
            <el-form :inline="true" :model="trainingFilter">
              <el-form-item label="业务类型">
                <el-select v-model="trainingFilter.type" clearable placeholder="全部业务" style="width: 140px" @change="loadTrainings">
                  <el-option label="基层来院进修" value="基层来院进修" />
                  <el-option label="总院下乡帮扶" value="总院下乡帮扶" />
                  <el-option label="联合技术攻关" value="联合技术攻关" />
                </el-select>
              </el-form-item>
              <el-form-item label="合作单位">
                <el-select v-model="trainingFilter.partnerId" clearable filterable placeholder="全部单位" style="width: 200px" @change="loadTrainings">
                  <el-option v-for="p in partners" :key="p.编号" :label="p.单位名称" :value="p.编号" />
                </el-select>
              </el-form-item>
              <el-form-item label="周期考核">
                <el-select v-model="trainingFilter.sixMonths" clearable placeholder="全部" style="width: 130px" @change="loadTrainings">
                  <el-option label="满 6 个月以上" :value="true" />
                  <el-option label="不满 6 个月" :value="false" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" :icon="Search" @click="loadTrainings">查询</el-button>
                <el-button :icon="Refresh" @click="resetTrainingFilter">重置</el-button>
              </el-form-item>
            </el-form>
          </div>

          <el-table v-loading="trainingLoading" :data="trainings" stripe border class="data-table">
            <el-table-column prop="编号" label="编号" width="70" align="center" />
            <el-table-column prop="业务类型" label="业务类型" width="130" align="center">
              <template #default="{ row }">
                <el-tag size="small" :type="row.业务类型 === '基层来院进修' ? 'success' : 'primary'">{{ row.业务类型 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="合作单位名称" label="医共体合作单位" min-width="180" show-overflow-tooltip>
              <template #default="{ row }">
                <strong>{{ row.合作单位名称 || '-' }}</strong>
              </template>
            </el-table-column>
            <el-table-column label="人员姓名 / 职称" width="160">
              <template #default="{ row }">
                <div><strong>{{ row.人员姓名 }}</strong></div>
                <div class="muted-text">{{ row.人员职称 || '医护' }} · {{ row.专业学科 || '全科' }}</div>
              </template>
            </el-table-column>
            <el-table-column label="进修/帮扶周期" width="190" align="center">
              <template #default="{ row }">
                <div>{{ formatDate(row.开始日期) }} 至 {{ formatDate(row.结束日期) }}</div>
                <el-tag v-if="row.进修天数 >= 180" size="small" type="success" effect="light">
                  满 {{ row.进修天数 }} 天 (≥6个月达标)
                </el-tag>
                <span v-else class="muted-text">{{ row.进修天数 }} 天</span>
              </template>
            </el-table-column>
            <el-table-column prop="带教导师姓名" label="带教导师" width="110" align="center" />
            <el-table-column prop="考核鉴定结果" label="考核结果" width="100" align="center">
              <template #default="{ row }">
                <el-tag size="small" :type="row.考核鉴定结果 === '优秀' ? 'success' : 'primary'">{{ row.考核鉴定结果 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="主要帮扶与学习成效" label="主要帮扶成效 / 总结" min-width="200" show-overflow-tooltip />
          </el-table>

          <div class="pagination-wrapper">
            <el-pagination
              v-model:current-page="trainingPagination.page"
              v-model:page-size="trainingPagination.pageSize"
              :total="trainingPagination.total"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="loadTrainings"
              @current-change="loadTrainings"
            />
          </div>
        </el-tab-pane>

        <!-- 标签页 2：医共体合作单位档案 -->
        <el-tab-pane label="医共体合作单位名录" name="partners">
          <el-table v-loading="partnerLoading" :data="partners" stripe border class="data-table">
            <el-table-column prop="编号" label="编号" width="70" align="center" />
            <el-table-column prop="单位名称" label="合作单位名称" min-width="200" show-overflow-tooltip>
              <template #default="{ row }">
                <strong>{{ row.单位名称 }}</strong>
              </template>
            </el-table-column>
            <el-table-column prop="单位类型" label="单位类型" width="140" align="center">
              <template #default="{ row }">
                <el-tag size="small">{{ row.单位类型 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="行政区划" label="区划/乡镇" width="130" align="center" />
            <el-table-column prop="床位数" label="床位" width="80" align="center">
              <template #default="{ row }">{{ row.床位数 }} 张</template>
            </el-table-column>
            <el-table-column prop="卫技人员数" label="卫技人员" width="90" align="center">
              <template #default="{ row }">{{ row.卫技人员数 }} 人</template>
            </el-table-column>
            <el-table-column label="对接联系人" width="160">
              <template #default="{ row }">
                <div>{{ row.对接联系人 || '-' }}</div>
                <div class="muted-text">{{ row.联系电话 || '-' }}</div>
              </template>
            </el-table-column>
            <el-table-column prop="合作状态" label="合作状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag size="small" :type="row.合作状态 === '正常合作' ? 'success' : 'info'">{{ row.合作状态 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="协议有效期" width="140" align="center">
              <template #default="{ row }">
                {{ formatDate(row.协议有效期至) }}
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 弹窗 1：登记进修 / 帮扶记录 -->
    <el-dialog v-model="trainingDialogVisible" title="登记医共体人员进修 / 专家帮扶记录" width="620px" destroy-on-close>
      <el-form ref="trainingFormRef" :model="trainingForm" :rules="trainingRules" label-width="120px">
        <el-form-item label="业务类型" prop="业务类型">
          <el-radio-group v-model="trainingForm.业务类型">
            <el-radio label="基层来院进修">基层来院进修</el-radio>
            <el-radio label="总院下乡帮扶">总院下乡帮扶</el-radio>
            <el-radio label="联合技术攻关">联合技术攻关</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="合作单位" prop="合作单位编号">
          <el-select v-model="trainingForm.合作单位编号" filterable placeholder="选择基层卫生院或社区中心" style="width: 100%">
            <el-option v-for="p in partners" :key="p.编号" :label="p.单位名称" :value="p.编号" />
          </el-select>
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="人员姓名" prop="人员姓名">
              <el-input v-model="trainingForm.人员姓名" placeholder="进修人员或下乡专家" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="职称/专业">
              <el-input v-model="trainingForm.人员职称" placeholder="如：主治医师 / 内科" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="开始日期" prop="开始日期">
              <el-date-picker v-model="trainingForm.开始日期" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="结束日期" prop="结束日期">
              <el-date-picker v-model="trainingForm.结束日期" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="专业学科">
              <el-input v-model="trainingForm.专业学科" placeholder="如：急诊重症、妇幼保健" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="考核鉴定结果">
              <el-select v-model="trainingForm.考核鉴定结果" style="width: 100%">
                <el-option label="优秀" value="优秀" />
                <el-option label="良好" value="良好" />
                <el-option label="合格" value="合格" />
                <el-option label="结业" value="结业" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="主要成效总结">
          <el-input v-model="trainingForm.主要帮扶与学习成效" type="textarea" :rows="3" placeholder="总结基层掌握的技术要点、临床带教成效或帮扶解决的疑难病例" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="trainingDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="trainingSubmitting" @click="handleSaveTraining">保存登记</el-button>
      </template>
    </el-dialog>

    <!-- 弹窗 2：新增合作单位 -->
    <el-dialog v-model="partnerDialogVisible" title="新增医共体合作单位" width="560px" destroy-on-close>
      <el-form ref="partnerFormRef" :model="partnerForm" :rules="partnerRules" label-width="110px">
        <el-form-item label="单位名称" prop="单位名称">
          <el-input v-model="partnerForm.单位名称" placeholder="输入乡镇卫生院或社区卫生服务中心全称" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="单位类型" prop="单位类型">
              <el-select v-model="partnerForm.单位类型" style="width: 100%">
                <el-option label="乡镇卫生院" value="乡镇卫生院" />
                <el-option label="社区卫生服务中心" value="社区卫生服务中心" />
                <el-option label="分院区" value="分院区" />
                <el-option label="专科对口帮扶医院" value="专科对口帮扶医院" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="行政区划">
              <el-input v-model="partnerForm.行政区划" placeholder="如：城东新区、城北镇" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="编制床位">
              <el-input-number v-model="partnerForm.床位数" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="卫技人员数">
              <el-input-number v-model="partnerForm.卫技人员数" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="对接联系人">
              <el-input v-model="partnerForm.对接联系人" placeholder="科教科负责人/院办主任" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="联系电话">
              <el-input v-model="partnerForm.联系电话" placeholder="办公电话或手机号" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="协议有效期">
          <el-date-picker v-model="partnerForm.协议有效期至" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="partnerDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="partnerSubmitting" @click="handleSavePartner">保存单位</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Share, Plus, OfficeBuilding, Refresh, Search } from '@element-plus/icons-vue'
import {
  listCommunityPartners, saveCommunityPartner, listCommunityTrainings, saveCommunityTraining
} from '@/api/medical-advancement'

const activeTab = ref('trainings')
const trainingSummary = reactive({ 进修人数: 0, 满六个月人数: 0, 帮扶人次: 0 })

// 进修台账
const trainingLoading = ref(false)
const trainings = ref<any[]>([])
const trainingPagination = reactive({ page: 1, pageSize: 10, total: 0 })
const trainingFilter = reactive({ type: '', partnerId: undefined, sixMonths: undefined })

// 合作单位
const partnerLoading = ref(false)
const partners = ref<any[]>([])

// 弹窗
const trainingDialogVisible = ref(false)
const trainingSubmitting = ref(false)
const trainingForm = reactive<any>({
  业务类型: '基层来院进修', 合作单位编号: undefined, 人员姓名: '', 人员职称: '',
  专业学科: '', 开始日期: '', 结束日期: '', 考核鉴定结果: '合格', 主要帮扶与学习成效: ''
})
const trainingRules = {
  合作单位编号: [{ required: true, message: '请选择合作单位', trigger: 'change' }],
  人员姓名: [{ required: true, message: '请输入人员姓名', trigger: 'blur' }],
  开始日期: [{ required: true, message: '请选择开始日期', trigger: 'change' }],
  结束日期: [{ required: true, message: '请选择结束日期', trigger: 'change' }]
}

const partnerDialogVisible = ref(false)
const partnerSubmitting = ref(false)
const partnerForm = reactive<any>({
  单位名称: '', 单位类型: '乡镇卫生院', 行政区划: '', 床位数: 50, 卫技人员数: 30,
  对接联系人: '', 联系电话: '', 协议有效期至: ''
})
const partnerRules = {
  单位名称: [{ required: true, message: '请输入单位名称', trigger: 'blur' }]
}

function formatDate(val: any) {
  if (!val) return '-'
  return String(val).substring(0, 10)
}

async function loadPartners() {
  partnerLoading.value = true
  try {
    const res: any = await listCommunityPartners()
    partners.value = res?.data || res || []
  } catch {} finally {
    partnerLoading.value = false
  }
}

async function loadTrainings() {
  trainingLoading.value = true
  try {
    const res: any = await listCommunityTrainings({
      合作单位编号: trainingFilter.partnerId || undefined,
      业务类型: trainingFilter.type || undefined,
      满六个月: trainingFilter.sixMonths,
      pageIndex: trainingPagination.page,
      pageSize: trainingPagination.pageSize
    })
    const data = res?.data || res
    trainings.value = data?.list || []
    trainingPagination.total = data?.total || 0

    // 计算统计指标
    let jx = 0; let m6 = 0; let bf = 0
    trainings.value.forEach((item: any) => {
      if (item.业务类型 === '基层来院进修') {
        jx++
        if (item.进修天数 >= 180) m6++
      } else if (item.业务类型 === '总院下乡帮扶') {
        bf++
      }
    })
    trainingSummary.进修人数 = jx
    trainingSummary.满六个月人数 = m6
    trainingSummary.帮扶人次 = bf
  } catch (err: any) {
    ElMessage.error(err.message || '获取进修帮扶列表失败')
  } finally {
    trainingLoading.value = false
  }
}

function handleTabChange(tab: string) {
  if (tab === 'trainings') loadTrainings()
  else if (tab === 'partners') loadPartners()
}

function resetTrainingFilter() {
  trainingFilter.type = ''
  trainingFilter.partnerId = undefined
  trainingFilter.sixMonths = undefined
  trainingPagination.page = 1
  loadTrainings()
}

function refreshAll() {
  loadPartners()
  loadTrainings()
}

function openAddTrainingDialog() {
  Object.assign(trainingForm, {
    业务类型: '基层来院进修', 合作单位编号: partners.value[0]?.编号 || undefined,
    人员姓名: '', 人员职称: '', 专业学科: '',
    开始日期: new Date().toISOString().substring(0, 10),
    结束日期: new Date(Date.now() + 180 * 86400000).toISOString().substring(0, 10),
    考核鉴定结果: '合格', 主要帮扶与学习成效: ''
  })
  trainingDialogVisible.value = true
}

async function handleSaveTraining() {
  if (!trainingForm.合作单位编号 || !trainingForm.人员姓名 || !trainingForm.开始日期 || !trainingForm.结束日期) {
    ElMessage.warning('请完整填写关键信息')
    return
  }
  trainingSubmitting.value = true
  try {
    await saveCommunityTraining(trainingForm)
    ElMessage.success('进修/帮扶台账登记成功')
    trainingDialogVisible.value = false
    loadTrainings()
  } catch (err: any) {
    ElMessage.error(err.message || '保存失败')
  } finally {
    trainingSubmitting.value = false
  }
}

function openAddPartnerDialog() {
  Object.assign(partnerForm, {
    单位名称: '', 单位类型: '乡镇卫生院', 行政区划: '', 床位数: 50, 卫技人员数: 30,
    对接联系人: '', 联系电话: '',
    协议有效期至: new Date(Date.now() + 365 * 3 * 86400000).toISOString().substring(0, 10)
  })
  partnerDialogVisible.value = true
}

async function handleSavePartner() {
  if (!partnerForm.单位名称) {
    ElMessage.warning('请输入单位名称')
    return
  }
  partnerSubmitting.value = true
  try {
    await saveCommunityPartner(partnerForm)
    ElMessage.success('医共体合作单位保存成功')
    partnerDialogVisible.value = false
    loadPartners()
  } catch (err: any) {
    ElMessage.error(err.message || '保存失败')
  } finally {
    partnerSubmitting.value = false
  }
}

onMounted(() => {
  loadPartners()
  loadTrainings()
})
</script>

<style scoped>
.community-healthcare-container {
  padding: 16px 20px;
  background-color: #f5f7fa;
  min-height: calc(100vh - 84px);
}

.header-card {
  background: linear-gradient(135deg, #0284c7 0%, #0369a1 100%);
  color: #fff;
  padding: 20px 24px;
  border-radius: 8px;
  margin-bottom: 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 4px 12px rgba(2, 132, 199, 0.15);
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  margin: 0 0 6px;
  display: flex;
  align-items: center;
  gap: 8px;
}

.page-subtitle {
  font-size: 13px;
  opacity: 0.9;
  margin: 0;
  max-width: 800px;
  line-height: 1.5;
}

.header-actions {
  display: flex;
  gap: 10px;
}

.metrics-row {
  margin-bottom: 16px;
}

.metric-card {
  border-radius: 8px;
  text-align: center;
  border: 1px solid #ebeef5;
}

.metric-label {
  font-size: 13px;
  color: #909399;
  margin-bottom: 6px;
}

.metric-value {
  font-size: 24px;
  font-weight: 700;
  color: #303133;
}

.metric-value .unit {
  font-size: 12px;
  font-weight: normal;
  color: #909399;
}

.metric-sub {
  font-size: 12px;
  color: #909399;
  margin-top: 4px;
}

.color-primary { color: #0284c7; }
.color-success { color: #10b981; }
.color-warning { color: #f59e0b; }
.color-cyan { color: #06b6d4; }

.main-card {
  border-radius: 8px;
}

.table-toolbar {
  margin-bottom: 14px;
  display: flex;
  justify-content: space-between;
}

.muted-text {
  font-size: 12px;
  color: #8c8c8c;
}

.pagination-wrapper {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>
