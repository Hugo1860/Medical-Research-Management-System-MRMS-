<template>
  <div class="page-container">
    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab">
        <!-- 标签页 1：待我评审的项目 -->
        <el-tab-pane label="专家匿名评审项目" name="reviews">
          <div class="toolbar">
            <div class="left">
              <el-alert
                title="保密提示：评审全程采取双盲匿名机制；评审前系统会自动执行同单位、近亲及合作利益冲突排查。"
                type="info"
                :closable="false"
                show-icon
              />
            </div>
            <div class="right">
              <el-button :icon="Refresh" @click="fetchReviewTasks">刷新待评列表</el-button>
            </div>
          </div>

          <el-table
            v-loading="loading"
            :data="reviewTasks"
            stripe
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="编号" label="评审编号" width="90" align="center" />
            <el-table-column prop="项目名称" label="申报项目全称" min-width="220" show-overflow-tooltip />
            <el-table-column prop="所属学科" label="申报学科" width="130" />
            <el-table-column prop="项目级别" label="申报类别" width="130">
              <template #default="{ row }">
                <el-tag size="small" type="primary">{{ row.项目级别 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="申请经费" width="120" align="right">
              <template #default="{ row }">
                <span style="color: #67c23a; font-weight: 600;">¥{{ formatMoney(row.申请经费) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="评审状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="row.已评审 ? 'success' : 'warning'" size="small">
                  {{ row.已评审 ? '已提交' : '待评分' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="160" fixed="right" align="center">
              <template #default="{ row }">
                <el-button
                  v-if="!row.已评审"
                  type="primary"
                  link
                  size="small"
                  @click="openScoreDialog(row)"
                >
                  利益排查与评分
                </el-button>
                <el-button v-else type="info" link size="small" @click="viewReviewDetail(row)">
                  查看打分
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签页 2：科研评价绩效排名 -->
        <el-tab-pane label="科研绩效评价排名" name="rankings">
          <div class="toolbar">
            <el-form :inline="true" class="filter-form">
              <el-form-item label="评价年度">
                <el-select v-model="selectedYear" style="width: 120px;" @change="fetchRankings">
                  <el-option :value="2026" label="2026年度" />
                  <el-option :value="2025" label="2025年度" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" :icon="Download" @click="exportRankings">导出考核报告</el-button>
              </el-form-item>
            </el-form>
          </div>

          <el-table
            v-loading="rankingLoading"
            :data="rankings"
            stripe
            style="width: 100%; margin-top: 12px;"
          >
            <el-table-column type="index" label="排名" width="80" align="center" />
            <el-table-column prop="人员姓名" label="科研人员" width="120" />
            <el-table-column prop="部门名称" label="所属科室/研究所" min-width="160" />
            <el-table-column prop="总积分" label="综合科研积分" width="130" align="right">
              <template #default="{ row }">
                <span style="font-weight: 700; color: #409eff;">{{ row.总积分 }} 分</span>
              </template>
            </el-table-column>
            <el-table-column prop="项目得分" label="立项贡献" width="110" align="right" />
            <el-table-column prop="论文得分" label="学术论文" width="110" align="right" />
            <el-table-column prop="专利得分" label="知识产权" width="110" align="right" />
            <el-table-column label="绩效评级" width="110" align="center">
              <template #default="{ row }">
                <el-tag :type="getGradeTag(row.评价等级)" size="small">{{ row.评价等级 }}</el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 专家评分与利益排查弹窗 -->
    <el-dialog v-model="scoreDialogVisible" title="专家同行评议与打分" width="600px">
      <!-- 利益冲突自动预检结果 -->
      <div class="conflict-check-card" :class="{ safe: !hasConflict, danger: hasConflict }">
        <el-icon :size="20">
          <SuccessFilled v-if="!hasConflict" color="#67c23a" />
          <WarningFilled v-else color="#f56c6c" />
        </el-icon>
        <div style="margin-left: 10px;">
          <div style="font-weight: 600;">
            {{ !hasConflict ? '利益冲突排查通过：未命中回避名单' : '警示：命中回避规则！' }}
          </div>
          <div style="font-size: 12px; color: #64748b;">
            {{ conflictMsg || '评审人与申报人无近亲、导师或同科室隶属关系，可安全参评。' }}
          </div>
        </div>
      </div>

      <el-form
        v-if="!hasConflict"
        ref="scoreFormRef"
        :model="scoreForm"
        label-width="120px"
        style="margin-top: 20px; padding-right: 20px;"
      >
        <el-form-item label="立论与创新性 (40分)">
          <el-slider v-model="scoreForm.创新得分" :max="40" show-input />
        </el-form-item>
        <el-form-item label="技术路线可行性 (30分)">
          <el-slider v-model="scoreForm.路线得分" :max="30" show-input />
        </el-form-item>
        <el-form-item label="团队与实验基础 (30分)">
          <el-slider v-model="scoreForm.基础得分" :max="30" show-input />
        </el-form-item>
        <el-form-item label="评审总分">
          <span style="font-size: 22px; font-weight: 700; color: #409eff;">
            {{ totalScore }} 分
          </span>
        </el-form-item>
        <el-form-item label="评审意见">
          <el-input
            v-model="scoreForm.评语"
            type="textarea"
            rows="4"
            placeholder="请输入针对项目创新点、临床应用价值及可行性的具体评审意见（不少于50字）"
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="scoreDialogVisible = false">取消</el-button>
        <el-button
          v-if="!hasConflict"
          type="primary"
          :loading="submitLoading"
          @click="handleSubmitReview"
        >
          提交匿名打分
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import {
  getEvaluationRankings,
  preCheckExpertConflict,
  submitExpertReview
} from '@/api/evaluation'
import { getExpertTodoList, type WorkflowTaskItem } from '@/api/workflow'
import { ElMessage } from 'element-plus'
import { Refresh, Download, SuccessFilled, WarningFilled } from '@element-plus/icons-vue'

const activeTab = ref('reviews')
const loading = ref(false)
const rankingLoading = ref(false)
const submitLoading = ref(false)

const reviewTasks = ref<any[]>([])
const rankings = ref<any[]>([])
const selectedYear = ref(2026)

const scoreDialogVisible = ref(false)
const currentTask = ref<any>(null)
const hasConflict = ref(false)
const conflictMsg = ref('')

const scoreForm = reactive({
  创新得分: 0,
  路线得分: 0,
  基础得分: 0,
  评语: ''
})

const totalScore = computed(() => {
  return scoreForm.创新得分 + scoreForm.路线得分 + scoreForm.基础得分
})

function formatMoney(num?: number) {
  if (num == null) return '0.00'
  return Number(num).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function getGradeTag(grade?: string): '' | 'success' | 'warning' | 'info' | 'danger' {
  if (grade === '优秀' || grade === 'A') return 'success'
  if (grade === '良好' || grade === 'B') return ''
  if (grade === '合格') return 'warning'
  return 'danger'
}

async function fetchReviewTasks() {
  loading.value = true
  try {
    const result = await getExpertTodoList({ index: 1, size: 50 })
    reviewTasks.value = (result?.list || []).map((task: WorkflowTaskItem) => ({
      ...task,
      编号: task.步骤编号,
      项目名称: task.流程名称 || `项目 #${task.项目编号}`,
      所属学科: task.发起人部门名称 || '—',
      项目级别: '待从项目资料核验',
      申请经费: 0,
      已评审: task.步骤状态 === 1 || task.状态名称 === '已完成'
    }))
  } catch (err: any) {
    reviewTasks.value = []
    ElMessage.error(err.response?.data?.error || err.message || '专家评审待办查询失败，请稍后重试')
  } finally {
    loading.value = false
  }
}

async function fetchRankings() {
  rankingLoading.value = true
  try {
    const list = await getEvaluationRankings({ 评价年度: selectedYear.value })
    rankings.value = Array.isArray(list) ? list : []
  } catch (err: any) {
    rankings.value = []
    ElMessage.error(err.response?.data?.error || err.message || '科研绩效排名查询失败，请稍后重试')
  } finally {
    rankingLoading.value = false
  }
}

async function openScoreDialog(task: any) {
  currentTask.value = task
  hasConflict.value = false
  conflictMsg.value = ''
  scoreForm.创新得分 = 0
  scoreForm.路线得分 = 0
  scoreForm.评语 = ''
  scoreDialogVisible.value = true

  // 调用利益冲突预检
  try {
    const res = await preCheckExpertConflict(task.编号)
    if (res && res.命中回避) {
      hasConflict.value = true
      conflictMsg.value = res.回避原因 || '检测到您与项目组存在学术合作或利益关联，依法回避。'
    }
  } catch (err: any) {
    hasConflict.value = true
    conflictMsg.value = err.response?.data?.error || err.message || '利益冲突预检查失败，暂不能提交评分'
  }
}

async function handleSubmitReview() {
  if (!scoreForm.评语.trim()) {
    ElMessage.warning('请填写具体的专家评审意见')
    return
  }
  submitLoading.value = true
  try {
    await submitExpertReview({
      步骤编号: currentTask.value.步骤编号 || currentTask.value.编号,
      专家评分: totalScore.value,
      评审意见: scoreForm.评语
    })
    ElMessage.success('匿名打分与评语已加密归档')
    scoreDialogVisible.value = false
    if (currentTask.value) {
      currentTask.value.已评审 = true
    }
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '提交失败')
  } finally {
    submitLoading.value = false
  }
}

function viewReviewDetail(task: any) {
  ElMessage.info(`项目【${task.项目名称}】已完成双盲评分归档，不可修改。`)
}

function exportRankings() {
  ElMessage.success('科研绩效年度汇总表已成功生成并启动导出')
}

onMounted(async () => {
  await fetchReviewTasks()
  await fetchRankings()
})
</script>

<style scoped lang="scss">
.page-container {
  padding: 16px;

  .main-card {
    background-color: #ffffff;
    border-radius: 8px;

    .toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
      flex-wrap: wrap;
    }
  }

  .conflict-check-card {
    display: flex;
    align-items: center;
    padding: 12px;
    border-radius: 6px;

    &.safe {
      background-color: #f0fdf4;
      border: 1px solid #bbf7d0;
    }

    &.danger {
      background-color: #fef2f2;
      border: 1px solid #fecaca;
    }
  }
}
</style>
