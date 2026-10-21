<template>
  <div class="page-container">
    <!-- 八类核心外联系统状态看板 -->
    <el-card shadow="never" class="overview-card">
      <template #header>
        <div class="card-header">
          <span class="header-title">医院/高校 8 大核心外部系统对接状态看板</span>
          <el-button type="primary" :icon="Refresh" :loading="pinging" @click="testAllConnections">
            刷新配置预检
          </el-button>
        </div>
      </template>

      <el-row :gutter="12">
        <el-col v-for="sys in externalSystems" :key="sys.系统代码" :xs="12" :sm="6" :md="3">
          <div class="system-item" :class="{ online: isSystemReady(sys), disabled: !sys.是否启用 }">
            <div class="sys-dot"></div>
            <div class="sys-name">{{ sys.系统名称 }}</div>
            <div class="sys-type">{{ sys.系统类型 }}</div>
            <div class="sys-status">{{ systemStatus(sys) }}</div>
          </div>
        </el-col>
      </el-row>
      <el-empty v-if="!loadingSystems && externalSystems.length === 0" description="尚未登记外部系统；请由系统管理员登记地址、认证引用和重试参数" />
      <el-alert v-if="loadError" :title="loadError" type="warning" :closable="false" show-icon />
    </el-card>

    <el-card shadow="never" class="main-card" style="margin-top: 16px;">
      <template #header>
        <div class="card-header">
          <span class="header-title">八类外部系统配置预检</span>
          <span class="muted">仅检查本地登记和部署密钥引用，不发起供应商请求</span>
        </div>
      </template>
      <el-table :data="configChecks" stripe size="small">
        <el-table-column prop="系统类型" label="系统类型" width="120" />
        <el-table-column label="登记" width="70" align="center">
          <template #default="{ row }"><el-tag :type="row.已登记 ? 'success' : 'danger'" size="small">{{ row.已登记 ? '是' : '否' }}</el-tag></template>
        </el-table-column>
        <el-table-column label="地址" width="70" align="center">
          <template #default="{ row }"><el-tag :type="row.已配置地址 ? 'success' : 'danger'" size="small">{{ row.已配置地址 ? '通过' : '缺失' }}</el-tag></template>
        </el-table-column>
        <el-table-column label="认证" width="70" align="center">
          <template #default="{ row }"><el-tag :type="row.已配置认证 ? 'success' : 'danger'" size="small">{{ row.已配置认证 ? '通过' : '缺失' }}</el-tag></template>
        </el-table-column>
        <el-table-column label="密钥" width="70" align="center">
          <template #default="{ row }"><el-tag :type="row.认证密钥已注入 ? 'success' : 'warning'" size="small">{{ row.认证密钥已注入 ? '已注入' : '未注入' }}</el-tag></template>
        </el-table-column>
        <el-table-column label="启用" width="70" align="center">
          <template #default="{ row }"><el-tag :type="row.已启用 ? 'success' : 'info'" size="small">{{ row.已启用 ? '是' : '否' }}</el-tag></template>
        </el-table-column>
        <el-table-column label="结论" min-width="260">
          <template #default="{ row }"><el-tag :type="row.通过 ? 'success' : 'warning'" size="small">{{ row.通过 ? '可作为联调候选' : row.提示 }}</el-tag></template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 异步同步任务队列监控 -->
    <el-card shadow="never" class="main-card" style="margin-top: 16px;">
      <template #header>
        <div class="card-header">
          <span class="header-title">外部系统数据同步任务队列监控</span>
          <div class="right-tools">
            <el-select v-model="statusFilter" placeholder="任务状态" clearable style="width: 120px; margin-right: 8px;" @change="fetchTasks">
              <el-option label="待处理" value="待处理" />
              <el-option label="处理中" value="处理中" />
              <el-option label="成功" value="成功" />
              <el-option label="失败" value="失败" />
            </el-select>
            <el-button :icon="Refresh" @click="fetchTasks">刷新队列</el-button>
          </div>
        </div>
      </template>

      <el-table
        v-loading="loading"
        :data="syncTasks"
        stripe
        style="width: 100%;"
      >
        <el-table-column prop="幂等键" label="任务幂等键" width="180" show-overflow-tooltip />
        <el-table-column prop="系统名称" label="目标外部系统" width="140" />
        <el-table-column prop="业务类型" label="同步业务类型" width="130">
          <template #default="{ row }">
            <el-tag size="small">{{ row.业务类型 }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="任务状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusTag(row.状态)" size="small">{{ row.状态 }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="重试次数" width="100" align="center">
          <template #default="{ row }">{{ row.尝试次数 }} / {{ row.最大重试次数 }}</template>
        </el-table-column>
        <el-table-column prop="最后错误" label="异常捕获详情" min-width="180" show-overflow-tooltip>
          <template #default="{ row }">
            <span v-if="row.最后错误" style="color: #f56c6c; font-size: 12px;">{{ row.最后错误 }}</span>
            <span v-else style="color: #94a3b8;">—</span>
          </template>
        </el-table-column>
        <el-table-column prop="更新时间" label="最后流转时间" width="160" />
        <el-table-column label="运维操作" width="140" fixed="right" align="center">
          <template #default="{ row }">
            <el-button
              v-if="row.状态 === '失败'"
              type="primary"
              link
              size="small"
              @click="handleRetry(row)"
            >
              重新投递
            </el-button>
            <el-button
              v-if="row.状态 === '待处理' || row.状态 === '失败'"
              type="danger"
              link
              size="small"
              @click="handleCancel(row)"
            >
              取消
            </el-button>
            <span v-if="row.状态 === '成功'" style="color: #67c23a; font-size: 13px;">正常</span>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-card v-if="failAlerts.length" shadow="never" class="main-card alert-card" style="margin-top: 16px;">
      <template #header>
        <div class="card-header"><span class="header-title">近 24 小时接口失败告警</span><el-tag type="danger">{{ failAlerts.length }} 类</el-tag></div>
      </template>
      <el-table :data="failAlerts" stripe size="small">
        <el-table-column prop="请求路径" label="请求路径" min-width="220" show-overflow-tooltip />
        <el-table-column prop="请求方法" label="方法" width="70" />
        <el-table-column prop="响应状态" label="状态码" width="80" />
        <el-table-column prop="失败次数" label="失败次数" width="90" />
        <el-table-column prop="最近失败时间" label="最近失败" width="180" />
      </el-table>
    </el-card>

    <el-card v-if="queueFailAlerts.length" shadow="never" class="main-card alert-card" style="margin-top: 16px;">
      <template #header>
        <div class="card-header"><span class="header-title">外部同步失败任务</span><el-tag type="danger">{{ queueFailAlerts.length }} 条</el-tag></div>
      </template>
      <el-table :data="queueFailAlerts" stripe size="small">
        <el-table-column prop="系统名称" label="外部系统" width="150" />
        <el-table-column prop="业务类型" label="业务类型" width="150" />
        <el-table-column prop="幂等键" label="幂等键" min-width="200" show-overflow-tooltip />
        <el-table-column prop="尝试次数" label="尝试次数" width="90" />
        <el-table-column prop="最后错误" label="最后错误" min-width="260" show-overflow-tooltip />
        <el-table-column prop="更新时间" label="最后失败时间" width="180" />
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import {
  getExternalSystems,
  checkExternalConfigs,
  getSyncTasks,
  retrySyncTask,
  cancelSyncTask,
  getFailAlerts,
  getExternalSyncFailAlerts,
  type ExternalSystemItem,
  type SyncTaskItem
} from '@/api/operations'
import { ElMessage } from 'element-plus'
import { Refresh } from '@element-plus/icons-vue'

const loading = ref(false)
const loadingSystems = ref(false)
const pinging = ref(false)
const statusFilter = ref('')
const loadError = ref('')
const configChecks = ref<any[]>([])
const failAlerts = ref<any[]>([])
const queueFailAlerts = ref<any[]>([])

const externalSystems = ref<ExternalSystemItem[]>([])

const syncTasks = ref<SyncTaskItem[]>([])

function getStatusTag(status?: string): 'primary' | 'success' | 'warning' | 'info' | 'danger' {
  if (status === '成功') return 'success'
  if (status === '处理中') return 'warning'
  if (status === '待处理') return 'info'
  if (status === '失败') return 'danger'
  return 'info'
}

function isSystemReady(system: ExternalSystemItem) {
  const check = configChecks.value.find((item) => item.系统类型 === system.系统类型)
  return Boolean(system.是否启用 && check?.通过)
}

function systemStatus(system: ExternalSystemItem) {
  const check = configChecks.value.find((item) => item.系统类型 === system.系统类型)
  if (!system.是否启用) return '已登记，未启用'
  if (!check) return '等待配置预检'
  return check.通过 ? '配置通过，待真实联调' : '配置不完整'
}

async function fetchSystems() {
  loadingSystems.value = true
  try {
    const list = await getExternalSystems()
    externalSystems.value = Array.isArray(list) ? list : []
  } catch (error: any) {
    externalSystems.value = []
    loadError.value = error?.response?.data?.error || error?.message || '外部系统登记读取失败'
  } finally {
    loadingSystems.value = false
  }
}

async function fetchTasks() {
  loading.value = true
  try {
    const list = await getSyncTasks({ 状态: statusFilter.value || undefined, 数量: 50 })
    syncTasks.value = Array.isArray(list) ? list : []
  } finally {
    loading.value = false
  }
}

async function testAllConnections() {
  pinging.value = true
  try {
    const result = await checkExternalConfigs()
    configChecks.value = Array.isArray(result?.必需系统) ? result.必需系统 : []
    ElMessage.success('配置预检完成；未发起外部网络请求')
  } catch (error: any) {
    ElMessage.error(error?.response?.data?.error || error?.message || '配置预检失败')
  } finally {
    pinging.value = false
  }
}

async function handleRetry(task: SyncTaskItem) {
  try {
    await retrySyncTask(task.编号)
    ElMessage.success(`任务【${task.幂等键}】已重新加入调度队列`)
    task.状态 = '待处理'
    task.最后错误 = undefined
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '重试失败')
  }
}

async function handleCancel(task: SyncTaskItem) {
  try {
    await cancelSyncTask(task.编号, '管理员手动终止')
    ElMessage.success('任务已取消')
    task.状态 = '已取消'
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '取消失败')
  }
}

onMounted(async () => {
  await fetchSystems()
  await Promise.all([
    fetchTasks(),
    testAllConnections(),
    getFailAlerts().then((list) => { failAlerts.value = Array.isArray(list) ? list : [] }).catch(() => { failAlerts.value = [] }),
    getExternalSyncFailAlerts({ 数量: 100 }).then((list) => { queueFailAlerts.value = Array.isArray(list) ? list : [] }).catch(() => { queueFailAlerts.value = [] })
  ])
})
</script>

<style scoped lang="scss">
.page-container {
  padding: 16px;

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;

    .header-title {
      font-weight: 600;
      font-size: 15px;
      color: #0f172a;
    }

    .right-tools {
      display: flex;
      align-items: center;
    }
  }

  .overview-card {
    background-color: #ffffff;
    border-radius: 8px;

    .system-item {
      background-color: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      padding: 12px;
      margin-bottom: 8px;
      position: relative;

      .sys-dot {
        position: absolute;
        top: 10px;
        right: 10px;
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background-color: #94a3b8;
      }

      .sys-name {
        font-weight: 600;
        font-size: 13px;
        color: #1e293b;
      }

      .sys-type {
        font-size: 11px;
        color: #64748b;
        margin: 2px 0 6px;
      }

      .sys-status {
        font-size: 11px;
        color: #94a3b8;
      }

      &.online {
        border-color: #bbf7d0;
        background-color: #f0fdf4;

        .sys-dot {
          background-color: #22c55e;
          box-shadow: 0 0 6px rgba(34, 197, 94, 0.4);
        }

        .sys-status {
          color: #16a34a;
          font-weight: 500;
        }
      }

      &.disabled {
        opacity: 0.72;
      }
    }
  }

  .main-card {
    background-color: #ffffff;
    border-radius: 8px;
  }

  .muted {
    color: #64748b;
    font-size: 12px;
  }
}
</style>
