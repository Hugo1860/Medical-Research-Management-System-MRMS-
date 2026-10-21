<template>
  <div class="login-log-container">
    <!-- 头部卡片 -->
    <div class="header-card">
      <div class="header-info">
        <h2 class="page-title">
          <el-icon class="title-icon"><DocumentChecked /></el-icon>
          系统登录与安全审计日志
        </h2>
        <p class="page-subtitle">
          全量追踪科研管理系统访问行为、记录工号、姓名、登录时间戳及客户端 IP，满足等保三级安全审计规范。
        </p>
      </div>
      <div class="header-actions">
        <el-button :icon="Refresh" @click="fetchLogs">刷新日志</el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="16" class="stats-row">
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap blue"><el-icon><DataAnalysis /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">总计登录记录</span>
            <span class="stat-value">{{ paging.total }}</span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap green"><el-icon><Calendar /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">今日审计记录</span>
            <span class="stat-value">{{ todayCount }}</span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap purple"><el-icon><User /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">活跃登录人员</span>
            <span class="stat-value">{{ uniqueUsersCount }}</span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap orange"><el-icon><Monitor /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">独立接入 IP</span>
            <span class="stat-value">{{ uniqueIpsCount }}</span>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索筛选栏 -->
    <el-card shadow="never" class="filter-card">
      <el-form :inline="true" :model="filters" class="filter-form">
        <el-form-item label="人员工号">
          <el-input
            v-model="filters.工号"
            placeholder="支持精确工号..."
            clearable
            style="width: 160px"
            @keyup.enter="handleSearch"
          />
        </el-form-item>
        <el-form-item label="人员姓名">
          <el-input
            v-model="filters.Like姓名"
            placeholder="支持模糊姓名..."
            clearable
            style="width: 160px"
            @keyup.enter="handleSearch"
          />
        </el-form-item>
        <el-form-item label="登录时间范围">
          <el-date-picker
            v-model="dateRange"
            type="datetimerange"
            range-separator="至"
            start-placeholder="开始时间"
            end-placeholder="结束时间"
            value-format="YYYY-MM-DD HH:mm:ss"
            :shortcuts="dateShortcuts"
            style="width: 360px"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
          <el-button :icon="RefreshRight" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 日志数据表格 -->
    <el-card shadow="never" class="table-card">
      <el-table
        v-loading="loading"
        :data="logList"
        border
        stripe
        class="data-table"
      >
        <el-table-column prop="编号" label="日志序号" width="90" align="center" />
        <el-table-column prop="工号" label="登录人工号" width="130" />
        <el-table-column prop="姓名" label="登录人姓名" width="130">
          <template #default="{ row }">
            <span class="fw-bold">{{ row.姓名 }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="登录时间" label="登录时间戳" min-width="180">
          <template #default="{ row }">
            <span class="timestamp-font">{{ formatDateTime(row.登录时间) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="IP" label="客户端接入 IP" min-width="160">
          <template #default="{ row }">
            <el-tag size="small" type="info" effect="plain" class="ip-tag">
              {{ row.IP || '未知' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="网络区域" width="130" align="center">
          <template #default="{ row }">
            <el-tag
              size="small"
              :type="row.IP ? (isInternalIp(row.IP) ? 'success' : 'warning') : 'info'"
              effect="light"
            >
              {{ row.IP ? (isInternalIp(row.IP) ? '院内局域网' : '专网/公网访问') : '未知' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="认证结果" width="110" align="center">
          <template #default>
            <el-tag type="success" size="small">认证通过</el-tag>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-bar">
        <el-pagination
          v-model:current-page="paging.index"
          v-model:page-size="paging.size"
          :total="paging.total"
          :page-sizes="[15, 30, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="fetchLogs"
          @current-change="fetchLogs"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import {
  DocumentChecked,
  Refresh,
  Search,
  RefreshRight,
  DataAnalysis,
  Calendar,
  User,
  Monitor
} from '@element-plus/icons-vue'
import { getLoginLogs, type LoginLogItem } from '@/api/system'
import { formatDateTime } from '@/utils/format'

const loading = ref(false)
const logList = ref<LoginLogItem[]>([])
const dateRange = ref<[string, string] | null>(null)

const filters = reactive({
  工号: '',
  Like姓名: ''
})

const paging = reactive({
  index: 1,
  size: 15,
  total: 0
})

const dateShortcuts = [
  {
    text: '今天',
    value: () => {
      const start = new Date()
      start.setHours(0, 0, 0, 0)
      const end = new Date()
      end.setHours(23, 59, 59, 999)
      return [start, end]
    }
  },
  {
    text: '最近一周',
    value: () => {
      const end = new Date()
      const start = new Date()
      start.setTime(start.getTime() - 3600 * 1000 * 24 * 7)
      return [start, end]
    }
  },
  {
    text: '最近一个月',
    value: () => {
      const end = new Date()
      const start = new Date()
      start.setTime(start.getTime() - 3600 * 1000 * 24 * 30)
      return [start, end]
    }
  }
]

// 统计计算
const todayCount = computed(() => {
  const todayStr = new Date().toISOString().slice(0, 10)
  return logList.value.filter((l) => l.登录时间 && l.登录时间.includes(todayStr)).length
})

const uniqueUsersCount = computed(() => {
  const set = new Set(logList.value.map((l) => l.工号))
  return set.size
})

const uniqueIpsCount = computed(() => {
  const set = new Set(logList.value.map((l) => l.IP).filter(Boolean))
  return set.size
})

// 判断是否为内网 IP
function isInternalIp(ip: string): boolean {
  if (!ip) return true
  if (ip === '127.0.0.1' || ip === '::1' || ip.startsWith('10.') || ip.startsWith('192.168.')) return true
  if (ip.startsWith('172.')) {
    const parts = ip.split('.')
    const second = parseInt(parts[1], 10)
    if (second >= 16 && second <= 31) return true
  }
  return false
}

async function fetchLogs() {
  loading.value = true
  try {
    const res = await getLoginLogs(
      { index: paging.index, size: paging.size },
      {
        工号: filters.工号.trim() || undefined,
        Like姓名: filters.Like姓名.trim() || undefined,
        Begin登录时间: dateRange.value?.[0],
        End登录时间: dateRange.value?.[1]
      }
    )
    logList.value = res?.list || []
    paging.total = res?.total || 0
  } catch (error: any) {
    console.warn('获取登录日志失败，已清空当前列表:', error)
    logList.value = []
    paging.total = 0
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  paging.index = 1
  fetchLogs()
}

function handleReset() {
  filters.工号 = ''
  filters.Like姓名 = ''
  dateRange.value = null
  paging.index = 1
  fetchLogs()
}

onMounted(() => {
  fetchLogs()
})
</script>

<style scoped>
.login-log-container {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.header-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #fff;
  padding: 18px 24px;
  border-radius: 8px;
  border: 1px solid #ebeef5;
}

.page-title {
  margin: 0 0 6px 0;
  font-size: 20px;
  font-weight: 600;
  color: #303133;
  display: flex;
  align-items: center;
  gap: 8px;
}

.title-icon {
  color: #409eff;
}

.page-subtitle {
  margin: 0;
  font-size: 13px;
  color: #909399;
}

.stats-row {
  margin-bottom: 0;
}

.stat-card {
  display: flex;
  align-items: center;
  padding: 12px;
}

.stat-card :deep(.el-card__body) {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 8px 12px;
  width: 100%;
}

.stat-icon-wrap {
  width: 44px;
  height: 44px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 22px;
  flex-shrink: 0;
}

.stat-icon-wrap.blue {
  background: #ecf5ff;
  color: #409eff;
}

.stat-icon-wrap.green {
  background: #f0f9eb;
  color: #67c23a;
}

.stat-icon-wrap.purple {
  background: #f4f4f5;
  color: #909399;
}

.stat-icon-wrap.orange {
  background: #fdf6ec;
  color: #e6a23c;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 12px;
  color: #909399;
}

.stat-value {
  font-size: 20px;
  font-weight: 700;
  color: #303133;
}

.filter-card {
  background: #fff;
}

.filter-form {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
}

.filter-form :deep(.el-form-item) {
  margin-bottom: 0;
  margin-right: 18px;
}

.table-card {
  background: #fff;
}

.fw-bold {
  font-weight: 600;
}

.timestamp-font {
  font-family: monospace;
  color: #606266;
}

.ip-tag {
  font-family: monospace;
}

.pagination-bar {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}
</style>
