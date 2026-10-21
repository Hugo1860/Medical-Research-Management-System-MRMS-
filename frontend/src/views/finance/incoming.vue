<template>
  <div class="incoming-funds-container">
    <el-card shadow="never" class="header-card">
      <div class="page-header">
        <div>
          <h2>科研经费到账管理与认领中心</h2>
          <p class="subtitle">跟踪纵向财政拨款、横向企业委托研发经费到账、拨付批次、到账认领与凭证核销</p>
        </div>
        <div class="header-actions">
          <el-button type="primary" :icon="Plus" @click="handleOpenRegisterDialog('纵向')">
            登记纵向项目到账
          </el-button>
          <el-button type="success" :icon="Plus" @click="handleOpenRegisterDialog('横向')">
            认领横向项目到账
          </el-button>
        </div>
      </div>
    </el-card>

    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab">
        <!-- 标签页 1：纵向项目经费到账 -->
        <el-tab-pane label="纵向项目经费到账" name="vertical">
          <el-form :inline="true" :model="verticalQuery" class="filter-form">
            <el-form-item label="项目名称">
              <el-input v-model="verticalQuery.like项目名称" placeholder="检索纵向项目全称" clearable />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :icon="Search" @click="fetchVerticalList">查询</el-button>
              <el-button :icon="Refresh" @click="handleResetVerticalQuery">重置</el-button>
            </el-form-item>
          </el-form>

          <!-- 统计指标 -->
          <el-row :gutter="16" class="stats-row">
            <el-col :span="6">
              <div class="stat-box">
                <div class="stat-label">纵向项目总数</div>
                <div class="stat-value">{{ verticalTotal }} 项</div>
              </div>
            </el-col>
            <el-col :span="6">
              <div class="stat-box">
                <div class="stat-label">批准资助总额</div>
                <div class="stat-value">¥ {{ formatMoney(totalApprovedFunds) }}</div>
              </div>
            </el-col>
            <el-col :span="6">
              <div class="stat-box">
                <div class="stat-label">累计实收到账</div>
                <div class="stat-value highlight">¥ {{ formatMoney(totalIncomingFunds) }}</div>
              </div>
            </el-col>
            <el-col :span="6">
              <div class="stat-box">
                <div class="stat-label">到账达成率</div>
                <div class="stat-value success">{{ incomingRate }}%</div>
              </div>
            </el-col>
          </el-row>

          <el-table v-loading="verticalLoading" :data="verticalList" border stripe style="width: 100%">
            <el-table-column prop="项目编号" label="项目编号" width="100" />
            <el-table-column prop="项目名称" label="纵向项目名称" min-width="220" show-overflow-tooltip />
            <el-table-column prop="负责人姓名" label="负责人" width="120" />
            <el-table-column prop="批准经费" label="批准经费 (元)" width="140" align="right">
              <template #default="{ row }">
                ¥ {{ formatMoney(row.批准经费) }}
              </template>
            </el-table-column>
            <el-table-column prop="配套经费" label="配套经费 (元)" width="130" align="right">
              <template #default="{ row }">
                ¥ {{ formatMoney(row.配套经费) }}
              </template>
            </el-table-column>
            <el-table-column prop="到账总金额" label="已到账金额 (元)" width="150" align="right">
              <template #default="{ row }">
                <span class="money-text">¥ {{ formatMoney(row.到账总金额) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="到账进度" width="160">
              <template #default="{ row }">
                <el-progress
                  :percentage="calcPercent(row.到账总金额, row.批准经费)"
                  :status="calcPercent(row.到账总金额, row.批准经费) >= 100 ? 'success' : ''"
                  :stroke-width="14"
                  :text-inside="true"
                />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="130" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="handleViewRecords(row, '纵向')">
                  到账明细
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-container">
            <el-pagination
              v-model:current-page="verticalPaging.pageIndex"
              v-model:page-size="verticalPaging.pageSize"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              :total="verticalTotal"
              @size-change="fetchVerticalList"
              @current-change="fetchVerticalList"
            />
          </div>
        </el-tab-pane>

        <!-- 标签页 2：横向项目经费认领 -->
        <el-tab-pane label="横向项目经费认领" name="horizontal">
          <el-form :inline="true" :model="horizontalQuery" class="filter-form">
            <el-form-item label="合同编号">
              <el-input v-model="horizontalQuery.like合同编号" placeholder="检索合同编号" clearable />
            </el-form-item>
            <el-form-item label="合同名称">
              <el-input v-model="horizontalQuery.like合同名称" placeholder="检索合同名称" clearable />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :icon="Search" @click="fetchHorizontalList">查询</el-button>
              <el-button :icon="Refresh" @click="handleResetHorizontalQuery">重置</el-button>
            </el-form-item>
          </el-form>

          <el-table v-loading="horizontalLoading" :data="horizontalList" border stripe style="width: 100%">
            <el-table-column prop="合同编号" label="合同编号" width="150" />
            <el-table-column prop="合同名称" label="横向合同/项目名称" min-width="220" show-overflow-tooltip />
            <el-table-column prop="负责人姓名" label="项目负责人" width="120" />
            <el-table-column prop="合同金额" label="合同总金额 (元)" width="150" align="right">
              <template #default="{ row }">
                ¥ {{ formatMoney(row.合同金额) }}
              </template>
            </el-table-column>
            <el-table-column prop="到账总金额" label="累计到账 (元)" width="150" align="right">
              <template #default="{ row }">
                <span class="money-text">¥ {{ formatMoney(row.到账总金额) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="认领执行进度" width="160">
              <template #default="{ row }">
                <el-progress
                  :percentage="calcPercent(row.到账总金额, row.合同金额)"
                  :status="calcPercent(row.到账总金额, row.合同金额) >= 100 ? 'success' : ''"
                  :stroke-width="14"
                  :text-inside="true"
                />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="140" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="handleViewRecords(row, '横向')">
                  认领记录
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-container">
            <el-pagination
              v-model:current-page="horizontalPaging.pageIndex"
              v-model:page-size="horizontalPaging.pageSize"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              :total="horizontalTotal"
              @size-change="fetchHorizontalList"
              @current-change="fetchHorizontalList"
            />
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 历次到账明细抽屉 -->
    <el-drawer v-model="recordsDrawerVisible" :title="currentRecordTitle" size="600px">
      <el-timeline v-if="recordsList.length > 0" style="padding: 16px">
        <el-timeline-item
          v-for="item in recordsList"
          :key="item.编号"
          :timestamp="formatDate(item.到账日期)"
          placement="top"
          type="primary"
        >
          <el-card shadow="hover">
            <div style="display: flex; justify-content: space-between; align-items: center">
              <h4>到账金额：<span class="money-text">¥ {{ formatMoney(item.到账金额) }}</span></h4>
              <el-tag size="small" type="success">已核销入账</el-tag>
            </div>
            <p><strong>拨款单位：</strong>{{ item.拨款单位 || '财政拨付/企业委托方' }}</p>
            <p v-if="item.凭证号"><strong>记账凭证号：</strong>{{ item.凭证号 }}</p>
            <p v-if="item.备注"><strong>备注说明：</strong>{{ item.备注 }}</p>
          </el-card>
        </el-timeline-item>
      </el-timeline>
      <el-empty v-else description="暂无历史到账入账批次" />
    </el-drawer>

    <!-- 登记到账弹窗 -->
    <el-dialog v-model="registerDialogVisible" :title="registerDialogTitle" width="600px">
      <el-form ref="registerFormRef" :model="registerForm" :rules="registerRules" label-width="120px">
        <el-form-item label="关联项目编号" prop="项目编号">
          <el-input-number v-model="registerForm.项目编号" :min="1" style="width: 100%" placeholder="输入项目立项编号" />
        </el-form-item>
        <el-form-item label="拨款单位" prop="拨款单位">
          <el-input v-model="registerForm.拨款单位" placeholder="如: 科技部/省基金委/合作企业" />
        </el-form-item>
        <el-form-item label="到账金额 (元)" prop="到账金额">
          <el-input-number v-model="registerForm.到账金额" :min="0" :precision="2" style="width: 100%" />
        </el-form-item>
        <el-form-item label="到账日期" prop="到账日期">
          <el-date-picker v-model="registerForm.到账日期" type="date" style="width: 100%" />
        </el-form-item>
        <el-form-item label="财务凭证号">
          <el-input v-model="registerForm.凭证号" placeholder="财务系统入账流水或凭证号" />
        </el-form-item>
        <el-form-item label="备注说明">
          <el-input v-model="registerForm.备注" type="textarea" :rows="2" placeholder="经费批次或说明" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="registerDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmitRegister">确认登记</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { Plus, Search, Refresh } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import {
  getVerticalIncomingList,
  getHorizontalIncomingList,
  getVerticalIncomingRecords,
  getHorizontalIncomingRecords,
  addVerticalIncoming,
  addHorizontalIncoming,
  type VerticalIncomingSummaryItem,
  type HorizontalIncomingSummaryItem,
  type IncomingRecordItem
} from '@/api/incoming'
import { formatMoney, formatDate } from '@/utils/format'
import dayjs from 'dayjs'

const activeTab = ref('vertical')
const verticalLoading = ref(false)
const horizontalLoading = ref(false)
const submitLoading = ref(false)

const verticalList = ref<VerticalIncomingSummaryItem[]>([])
const horizontalList = ref<HorizontalIncomingSummaryItem[]>([])
const recordsList = ref<IncomingRecordItem[]>([])

const verticalTotal = ref(0)
const horizontalTotal = ref(0)
const currentRecordTitle = ref('')
const recordsDrawerVisible = ref(false)
const registerDialogVisible = ref(false)
const registerType = ref<'纵向' | '横向'>('纵向')

const verticalPaging = reactive({ pageIndex: 1, pageSize: 10 })
const horizontalPaging = reactive({ pageIndex: 1, pageSize: 10 })
const verticalQuery = reactive({ like项目名称: '' })
const horizontalQuery = reactive({ like合同编号: '', like合同名称: '' })

const registerForm = reactive({
  项目编号: 1,
  拨款单位: '',
  到账金额: 50000,
  到账日期: new Date(),
  凭证号: '',
  备注: ''
})

const registerRules = {
  项目编号: [{ required: true, message: '请输入项目编号', trigger: 'blur' }],
  拨款单位: [{ required: true, message: '请输入拨款单位', trigger: 'blur' }],
  到账金额: [{ required: true, message: '请输入到账金额', trigger: 'blur' }]
}

const registerDialogTitle = computed(() => {
  return registerType.value === '纵向' ? '登记纵向科研经费到账' : '认领横向项目经费到账'
})

const totalApprovedFunds = computed(() => {
  return verticalList.value.reduce((sum, item) => sum + (Number(item.批准经费) || 0), 0)
})

const totalIncomingFunds = computed(() => {
  return verticalList.value.reduce((sum, item) => sum + (Number(item.到账总金额) || 0), 0)
})

const incomingRate = computed(() => {
  if (totalApprovedFunds.value === 0) return 0
  return Math.min(100, Math.round((totalIncomingFunds.value / totalApprovedFunds.value) * 100))
})

function calcPercent(income: number | undefined, total: number | undefined) {
  if (!total || total <= 0) return 0
  return Math.min(100, Math.round(((income || 0) / total) * 100))
}

async function fetchVerticalList() {
  verticalLoading.value = true
  try {
    const res = await getVerticalIncomingList(verticalPaging, verticalQuery)
    if (res && res.data) {
      verticalList.value = res.data.list || res.data || []
      verticalTotal.value = res.data.total || verticalList.value.length
    }
  } catch {
    // 降级使用空列表
  } finally {
    verticalLoading.value = false
  }
}

async function fetchHorizontalList() {
  horizontalLoading.value = true
  try {
    const res = await getHorizontalIncomingList(horizontalPaging, horizontalQuery)
    if (res && res.data) {
      horizontalList.value = res.data.list || res.data || []
      horizontalTotal.value = res.data.total || horizontalList.value.length
    }
  } catch {
    // 降级使用空列表
  } finally {
    horizontalLoading.value = false
  }
}

function handleResetVerticalQuery() {
  verticalQuery.like项目名称 = ''
  verticalPaging.pageIndex = 1
  fetchVerticalList()
}

function handleResetHorizontalQuery() {
  horizontalQuery.like合同编号 = ''
  horizontalQuery.like合同名称 = ''
  horizontalPaging.pageIndex = 1
  fetchHorizontalList()
}

async function handleViewRecords(row: any, type: '纵向' | '横向') {
  currentRecordTitle.value = `${type}项目 [${row.项目名称 || row.合同名称}] 历次到账明细`
  recordsList.value = []
  recordsDrawerVisible.value = true
  try {
    const res = type === '纵向'
      ? await getVerticalIncomingRecords(row.项目编号)
      : await getHorizontalIncomingRecords(row.项目编号)
    if (res && res.data) {
      recordsList.value = Array.isArray(res.data) ? res.data : [res.data]
    }
  } catch {
    // 降级
  }
}

function handleOpenRegisterDialog(type: '纵向' | '横向') {
  registerType.value = type
  registerDialogVisible.value = true
}

async function handleSubmitRegister() {
  submitLoading.value = true
  try {
    const payload = {
      基本资料: {
        项目编号: registerForm.项目编号,
        拨款单位: registerForm.拨款单位,
        到账金额: registerForm.到账金额,
        到账日期: dayjs(registerForm.到账日期).toISOString(),
        凭证号: registerForm.凭证号,
        备注: registerForm.备注
      },
      isHold: false
    }

    if (registerType.value === '纵向') {
      await addVerticalIncoming(payload)
      ElMessage.success('纵向经费到账登记已成功提交！')
      fetchVerticalList()
    } else {
      await addHorizontalIncoming(payload)
      ElMessage.success('横向经费到账认领已成功提交！')
      fetchHorizontalList()
    }
    registerDialogVisible.value = false
  } catch (err: any) {
    ElMessage.error(err.message || '登记失败')
  } finally {
    submitLoading.value = false
  }
}

onMounted(() => {
  fetchVerticalList()
  fetchHorizontalList()
})
</script>

<style scoped lang="scss">
.incoming-funds-container {
  .header-card {
    margin-bottom: 16px;
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;

      h2 {
        margin: 0 0 6px 0;
        font-size: 18px;
        color: #303133;
      }
      .subtitle {
        margin: 0;
        font-size: 13px;
        color: #909399;
      }

      .header-actions {
        display: flex;
        gap: 12px;
      }
    }
  }

  .main-card {
    .filter-form {
      margin-bottom: 16px;
    }

    .stats-row {
      margin-bottom: 16px;

      .stat-box {
        background: #f8f9fa;
        border-radius: 6px;
        padding: 12px 16px;
        border: 1px solid #ebeef5;

        .stat-label {
          font-size: 13px;
          color: #909399;
          margin-bottom: 4px;
        }

        .stat-value {
          font-size: 20px;
          font-weight: 600;
          color: #303133;

          &.highlight {
            color: #409eff;
          }
          &.success {
            color: #67c23a;
          }
        }
      }
    }

    .money-text {
      font-weight: 600;
      color: #e6a23c;
    }

    .pagination-container {
      margin-top: 16px;
      display: flex;
      justify-content: flex-end;
    }
  }
}
</style>
