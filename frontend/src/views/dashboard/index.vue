<template>
  <div class="dashboard-container">
    <!-- 欢迎横幅 -->
    <el-card class="welcome-card" shadow="never">
      <div class="welcome-content">
        <div class="user-greeting">
          <el-avatar :size="64" class="greeting-avatar">
            {{ userStore.userInfo?.姓名?.substring(0, 1) || '科' }}
          </el-avatar>
          <div class="greeting-text">
            <h2>早安，{{ userStore.userInfo?.姓名 || '科研人员' }}！欢迎使用科研信息管理系统</h2>
            <p class="greeting-sub">
              工号：{{ userStore.userInfo?.工号 || '—' }} &nbsp;|&nbsp; 部门：{{
                userStore.userInfo?.部门名称 || '科研院所'
              }}
              &nbsp;|&nbsp; 当前角色：{{ workbenchSummary?.角色名称 || userStore.userInfo?.角色名称 || '当前用户' }} &nbsp;|&nbsp; {{ todayStr }}
            </p>
            <p v-if="workbenchSummary?.下一步流程名称 || workbenchSummary?.办理提示" class="next-step-hint">
              下一步：{{ workbenchSummary?.下一步流程名称 || '待办事项' }}<span v-if="workbenchSummary?.下一步办理人"> · {{ workbenchSummary.下一步办理人 }}</span>
              <span v-if="workbenchSummary?.办理提示"> · {{ workbenchSummary.办理提示 }}</span>
            </p>
          </div>
        </div>
        <div class="quick-actions">
          <el-button type="primary" :icon="Plus" @click="navigateTo('/project/vertical-application', '纵向申报')">
            发起项目申报
          </el-button>
          <el-button type="success" :icon="DocumentChecked" @click="navigateTo('/fruit/paper-claim', '成果认领')">
            论文成果认领
          </el-button>
          <el-button type="warning" :icon="Money" @click="navigateTo('/finance/expense-claims', '经费报销')">
            经费报销申请
          </el-button>
        </div>
      </div>
    </el-card>

    <!-- 关键指标卡片 -->
    <el-row :gutter="16" class="metric-row">
      <el-col :xs="24" :sm="12" :md="6">
        <el-card shadow="hover" class="metric-card card-blue">
          <div class="metric-body">
            <div class="metric-info">
              <span class="metric-label">已登记科研项目</span>
              <span class="metric-value">{{ projectMetric.value }} <small>项</small></span>
              <span class="metric-foot">{{ projectMetric.foot }}</span>
            </div>
            <el-icon class="metric-icon"><TrendCharts /></el-icon>
          </div>
        </el-card>
      </el-col>

      <el-col :xs="24" :sm="12" :md="6">
        <el-card shadow="hover" class="metric-card card-green">
          <div class="metric-body">
            <div class="metric-info">
              <span class="metric-label">已登记科研经费</span>
              <span class="metric-value">{{ financeMetric.value }} <small>万元</small></span>
              <span class="metric-foot">{{ financeMetric.foot }}</span>
            </div>
            <el-icon class="metric-icon"><Money /></el-icon>
          </div>
        </el-card>
      </el-col>

      <el-col :xs="24" :sm="12" :md="6">
        <el-card shadow="hover" class="metric-card card-purple">
          <div class="metric-body">
            <div class="metric-info">
              <span class="metric-label">学术成果总数</span>
              <span class="metric-value">{{ resultMetric.value }} <small>篇/项</small></span>
              <span class="metric-foot">{{ resultMetric.foot }}</span>
            </div>
            <el-icon class="metric-icon"><Medal /></el-icon>
          </div>
        </el-card>
      </el-col>

      <el-col :xs="24" :sm="12" :md="6">
        <el-card shadow="hover" class="metric-card card-orange">
          <div class="metric-body">
            <div class="metric-info">
              <span class="metric-label">待办审批流程</span>
              <span class="metric-value">{{ pendingTasks.length }} <small>件</small></span>
              <span class="metric-foot">{{ pendingTasks.length ? '请按办理提示处理' : '当前没有待办事项' }}</span>
            </div>
            <el-icon class="metric-icon"><Bell /></el-icon>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 图表与待办区域 -->
    <el-row :gutter="16" class="content-row">
      <!-- 左侧：图表统计 -->
      <el-col :xs="24" :lg="14">
        <el-card shadow="never" class="chart-card">
          <template #header>
            <div class="card-header">
              <span class="header-title">科研统计指标概览</span>
              <el-tag size="small" type="success">来自统计接口</el-tag>
            </div>
          </template>
          <div ref="lineChartRef" class="chart-container"></div>
        </el-card>

        <el-card shadow="never" class="chart-card" style="margin-top: 16px;">
          <template #header>
            <div class="card-header">
              <span class="header-title">科研项目与成果类型分布</span>
            </div>
          </template>
          <div ref="pieChartRef" class="chart-container"></div>
        </el-card>
      </el-col>

      <!-- 右侧：待办事项与流程中心 -->
      <el-col :xs="24" :lg="10">
        <el-card shadow="never" class="todo-card">
          <template #header>
            <div class="card-header">
              <span class="header-title">待处理科研事务</span>
              <el-badge :value="pendingTasks.length" type="danger" />
            </div>
          </template>

          <el-table :data="pendingTasks" style="width: 100%" stripe empty-text="当前暂无待办事项">
            <el-table-column prop="分类" label="分类" width="90">
              <template #default="{ row }">
                <el-tag size="small" :type="getTagType(row.分类)">{{ row.分类 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="标题" label="事务摘要" show-overflow-tooltip />
            <el-table-column prop="办理提示" label="办理要求" width="110" show-overflow-tooltip />
            <el-table-column label="责任人/角色" width="130" show-overflow-tooltip>
              <template #default="{ row }">
                {{ row.办理人 || row.办理角色 || '服务端未返回责任人' }}
              </template>
            </el-table-column>
            <el-table-column label="操作" width="80" align="center">
              <template #default="{ row }">
                <el-button type="primary" link size="small" @click="handleTask(row)">
                  办理
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>

        <!-- 科研通知与快捷通道 -->
        <el-card shadow="never" class="quick-card" style="margin-top: 16px;">
          <template #header>
            <div class="card-header">
              <span class="header-title">快捷入口与科研治理</span>
            </div>
          </template>
          <div class="quick-grid">
            <div class="quick-item" @click="navigateTo('/operations/external-sync', '科研治理中心')">
              <el-icon class="quick-icon" style="color: #409eff"><Odometer /></el-icon>
              <span>科研治理中心</span>
            </div>
            <div class="quick-item" @click="navigateTo('/governance/proxy-reconciliation', '预算异常对账')">
              <el-icon class="quick-icon" style="color: #67c23a"><Money /></el-icon>
              <span>预算异常对账</span>
            </div>
            <div class="quick-item" @click="navigateTo('/evaluation/expert-workbench', '专家评审工作台')">
              <el-icon class="quick-icon" style="color: #e6a23c"><UserFilled /></el-icon>
              <span>专家评审工作台</span>
            </div>
            <div class="quick-item" @click="navigateTo('/analytics/statistics', '成果统计看板')">
              <el-icon class="quick-icon" style="color: #f56c6c"><Histogram /></el-icon>
              <span>成果统计看板</span>
            </div>
          </div>
        </el-card>

        <el-card v-if="configuredComponents.length" shadow="never" class="config-card" style="margin-top: 16px;">
          <template #header>
            <div class="card-header">
              <span class="header-title">{{ configuredComponents[0]?.角色名称 === '默认' ? '科研服务入口' : '角色专属科研入口' }}</span>
              <el-tag size="small" type="info">按角色配置</el-tag>
            </div>
          </template>
          <div class="configured-entry-grid">
            <div v-for="component in configuredComponents" :key="component.编号" class="configured-entry" @click="navigateConfigured(component)">
              <span class="configured-entry-title">{{ component.组件标题 }}</span>
              <span class="configured-entry-type">{{ component.组件类型 }}</span>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { getWorkbenchSummary, getAnalyticsOverview, type WorkbenchSummary, type WorkbenchSummaryItem } from '@/api/workbench'
import { getDashboardComponents, type DashboardComponent } from '@/api/dashboard'
import { getTodoList, type WorkflowTaskItem } from '@/api/workflow'
import { resolveModernRoute } from '@/utils/route-resolver'
import * as echarts from 'echarts'
import dayjs from 'dayjs'
import {
  Plus,
  DocumentChecked,
  Money,
  TrendCharts,
  Medal,
  Bell,
  Odometer,
  UserFilled,
  Histogram
} from '@element-plus/icons-vue'

const router = useRouter()
const userStore = useUserStore()

const todayStr = dayjs().format('YYYY年MM月DD日 星期' + ['日','一','二','三','四','五','六'][dayjs().day()])

const pendingTasks = ref<WorkbenchSummaryItem[]>([])
const workbenchSummary = ref<WorkbenchSummary | null>(null)
const overviewMetrics = ref<any[]>([])
const configuredComponents = ref<DashboardComponent[]>([])
const lineChartRef = ref<HTMLDivElement | null>(null)
const pieChartRef = ref<HTMLDivElement | null>(null)

const metricByName = (name: string) => overviewMetrics.value.find((item) => item?.指标 === name)
const projectMetric = computed(() => {
  const vertical = metricByName('纵向项目')
  const horizontal = metricByName('横向项目')
  const count = Number(vertical?.数量 || 0) + Number(horizontal?.数量 || 0)
  return {
    value: count,
    foot: `纵向 ${Number(vertical?.数量 || 0)} 项 / 横向 ${Number(horizontal?.数量 || 0)} 项`
  }
})
const financeMetric = computed(() => {
  const vertical = Number(metricByName('纵向项目')?.数值 || 0)
  const horizontal = Number(metricByName('横向项目')?.数值 || 0)
  const total = vertical + horizontal
  return {
    value: total ? (total / 10000).toFixed(1) : '—',
    foot: total ? '按已登记项目批准经费/合同金额汇总' : '暂无可用经费统计数据'
  }
})
const resultMetric = computed(() => {
  const result = metricByName('科研成果')
  const count = Number(result?.数量 || 0)
  return {
    value: count,
    foot: count ? '论文、著作、专利和获奖成果总数' : '暂无可用成果统计数据'
  }
})

function getTagType(category: string): 'primary' | 'success' | 'warning' | 'info' | 'danger' {
  if (category?.includes('项目') || category?.includes('纵向')) return 'primary'
  if (category?.includes('成果') || category?.includes('论文')) return 'success'
  if (category?.includes('经费') || category?.includes('报销')) return 'warning'
  return 'info'
}

function navigateTo(url: string, title?: string) {
  if (!url) return
  if (url.startsWith('/')) {
    const modern = resolveModernRoute(url)
    if (modern) {
      router.push(modern)
      return
    }
    // If it's already an internal modern route like /project/...
    if (!url.startsWith('/IMIS') && !url.includes('.html')) {
      router.push(url)
      return
    }
  }

  const modern = resolveModernRoute(url)
  if (modern) {
    router.push(modern)
    return
  }

  router.push({
    path: '/iframe',
    query: {
      url,
      title: title || ''
    }
  })
}

function handleTask(task: WorkbenchSummaryItem) {
  if (task.步骤链接路径) {
    navigateTo(task.步骤链接路径, task.标题)
    return
  }
  router.push('/workflow/todo-approvals')
}

function navigateConfigured(component: DashboardComponent) {
  const path = typeof component.配置?.路径 === 'string' ? component.配置.路径 : ''
  if (path) navigateTo(path, component.组件标题)
}

async function fetchSummary() {
  const [summary, todo, overview, components] = await Promise.all([
    getWorkbenchSummary().catch(() => null),
    getTodoList({ index: 1, size: 8 }).catch(() => ({ total: 0, list: [] })),
    getAnalyticsOverview('overview').catch(() => null),
    getDashboardComponents().catch(() => [])
  ])
  workbenchSummary.value = summary
  const todoPayload = todo as any
  const todoRows: WorkflowTaskItem[] = todoPayload?.list || todoPayload?.List || []
  pendingTasks.value = todoRows
    .filter((row) => Boolean(row.流程编号 || row.项目编号) && Boolean(row.流程名称 || row.步骤名称))
    .map((row) => ({
    类型: row.流程名称 || row.步骤名称,
    分类: row.流程名称 || row.步骤名称,
    标题: row.流程名称 || row.步骤名称,
    时间: row.流程创建时间 || '',
    下一步: row.步骤名称 || '',
    办理提示: row.步骤状态说明 || '',
    业务编号: row.项目编号 || row.流程编号,
    步骤链接路径: row.步骤链接路径,
    办理人: row.步骤操作人姓名 || row.步骤操作人部门名称
  }))
  const metrics = overview?.指标 || overview?.指标列表 || []
  overviewMetrics.value = Array.isArray(metrics) ? metrics : []
  configuredComponents.value = Array.isArray(components) ? components : []
}

function initCharts() {
  const metrics = overviewMetrics.value.filter((item) => item && item.指标)
  if (lineChartRef.value) {
    const lineChart = echarts.init(lineChartRef.value)
    lineChart.setOption({
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
      xAxis: {
        type: 'category',
        data: metrics.map((item) => item.指标)
      },
      yAxis: { type: 'value', name: '登记金额 (万元)' },
      series: [{ name: '登记金额', type: 'bar', data: metrics.map((item) => Number(item.数值 || 0) / 10000), itemStyle: { color: '#409eff' } }],
      graphic: metrics.length ? undefined : [{ type: 'text', left: 'center', top: 'middle', style: { text: '暂无真实统计数据', fill: '#909399', fontSize: 14 } }]
    })
  }

  if (pieChartRef.value) {
    const pieChart = echarts.init(pieChartRef.value)
    pieChart.setOption({
      tooltip: { trigger: 'item', formatter: '{a} <br/>{b}: {c} ({d}%)' },
      legend: { orient: 'vertical', left: 'left' },
      series: [
        {
          name: '科研产出分布',
          type: 'pie',
          radius: ['45%', '70%'],
          avoidLabelOverlap: false,
          itemStyle: {
            borderRadius: 6,
            borderColor: '#fff',
            borderWidth: 2
          },
          label: { show: false, position: 'center' },
          emphasis: {
            label: { show: true, fontSize: 16, fontWeight: 'bold' }
          },
          data: metrics.map((item, index) => ({ value: Number(item.数量 || 0), name: item.指标, itemStyle: { color: ['#409eff', '#67c23a', '#e6a23c', '#f56c6c', '#909399'][index % 5] } }))
        }
      ],
      graphic: metrics.length ? undefined : [{ type: 'text', left: 'center', top: 'middle', style: { text: '暂无真实统计数据', fill: '#909399', fontSize: 14 } }]
    })
  }
}

onMounted(async () => {
  await fetchSummary()
  await nextTick()
  initCharts()
})
</script>

<style scoped lang="scss">
.dashboard-container {
  padding: 16px;

  .welcome-card {
    margin-bottom: 16px;
    background: linear-gradient(135deg, #ffffff 0%, #f6f9fc 100%);
    border: 1px solid #e2e8f0;

    .welcome-content {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: 16px;

      .user-greeting {
        display: flex;
        align-items: center;
        gap: 16px;

        .greeting-avatar {
          background-color: #3b82f6;
          color: #ffffff;
          font-size: 24px;
          font-weight: 600;
        }

        .greeting-text {
          h2 {
            font-size: 18px;
            font-weight: 600;
            color: #1e293b;
            margin-bottom: 4px;
          }

          .greeting-sub {
            font-size: 13px;
            color: #64748b;
          }

          .next-step-hint {
            margin: 6px 0 0;
            font-size: 13px;
            color: #2563eb;
          }
        }
      }

      .quick-actions {
        display: flex;
        gap: 8px;
      }
    }
  }

  .metric-row {
    margin-bottom: 16px;

    .metric-card {
      border-radius: 8px;
      border: none;
      color: #ffffff;

      .metric-body {
        display: flex;
        justify-content: space-between;
        align-items: center;

        .metric-info {
          display: flex;
          flex-direction: column;

          .metric-label {
            font-size: 13px;
            opacity: 0.9;
            margin-bottom: 4px;
          }

          .metric-value {
            font-size: 26px;
            font-weight: 700;
            line-height: 1.2;

            small {
              font-size: 14px;
              font-weight: 400;
            }
          }

          .metric-foot {
            font-size: 12px;
            opacity: 0.85;
            margin-top: 6px;
          }
        }

        .metric-icon {
          font-size: 48px;
          opacity: 0.25;
        }
      }

      &.card-blue {
        background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%);
      }

      &.card-green {
        background: linear-gradient(135deg, #10b981 0%, #047857 100%);
      }

      &.card-purple {
        background: linear-gradient(135deg, #8b5cf6 0%, #6d28d9 100%);
      }

      &.card-orange {
        background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
      }
    }
  }

  .content-row {
    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;

      .header-title {
        font-weight: 600;
        font-size: 15px;
        color: #1e293b;
      }
    }

    .chart-container {
      width: 100%;
      height: 240px;
    }

    .quick-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 12px;

      .quick-item {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 16px;
        background-color: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 6px;
        cursor: pointer;
        transition: all 0.2s;

        &:hover {
          background-color: #eff6ff;
          border-color: #3b82f6;
          transform: translateY(-2px);
        }

        .quick-icon {
          font-size: 28px;
          margin-bottom: 8px;
        }

        span {
          font-size: 13px;
          color: #334155;
          font-weight: 500;
        }
      }
    }

    .configured-entry-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 10px;

      .configured-entry {
        display: flex;
        flex-direction: column;
        gap: 4px;
        padding: 12px;
        border: 1px solid #dbeafe;
        border-radius: 6px;
        background: #f8fbff;
        cursor: pointer;

        &:hover { border-color: #60a5fa; background: #eff6ff; }
        .configured-entry-title { color: #1e3a8a; font-weight: 600; }
        .configured-entry-type { color: #64748b; font-size: 12px; }
      }
    }
  }
}
</style>
