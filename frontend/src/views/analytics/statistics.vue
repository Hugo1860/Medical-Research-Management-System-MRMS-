<template>
  <div class="page-container">
    <el-card shadow="never" class="main-card">
      <div class="screen-header">
        <div class="header-left">
          <h2>全院科研大数据与决策分析中心</h2>
          <p class="header-sub">
            多维统计指标版本化治理 · 实时汇聚立项、经费、成果与人才全链路
          </p>
        </div>
        <div class="header-right">
          <el-radio-group v-model="dimension" @change="onDimensionChange">
            <el-radio-button value="全院总览">全院总览</el-radio-button>
            <el-radio-button value="纵向项目">纵向项目</el-radio-button>
            <el-radio-button value="横向攻关">横向攻关</el-radio-button>
            <el-radio-button value="成果论文">成果论文</el-radio-button>
          </el-radio-group>
          <el-button type="success" :icon="Download" style="margin-left: 12px;" @click="handleExport">
            导出决策报表 (CSV)
          </el-button>
        </div>
      </div>

      <!-- 核心指标摘要 -->
      <el-row :gutter="16" class="metric-row" style="margin-top: 16px;">
        <el-col :xs="12" :sm="6">
          <div class="m-card c-blue">
            <span class="m-title">在研科研项目总数</span>
            <span class="m-number">{{ projectSummary.count }} <small>项</small></span>
            <span class="m-tip">{{ projectSummary.tip }}</span>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="m-card c-green">
            <span class="m-title">科研经费总盘子</span>
            <span class="m-number">{{ financeSummary.amount }} <small>万元</small></span>
            <span class="m-tip">{{ financeSummary.tip }}</span>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="m-card c-purple">
            <span class="m-title">高水平学术论文 (SCI/EI)</span>
            <span class="m-number">{{ resultSummary.count }} <small>篇/项</small></span>
            <span class="m-tip">{{ resultSummary.tip }}</span>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="m-card c-orange">
            <span class="m-title">科研领军与专家人才</span>
            <span class="m-number">{{ peopleSummary.count }} <small>人</small></span>
            <span class="m-tip">{{ peopleSummary.tip }}</span>
          </div>
        </el-col>
      </el-row>

      <!-- 运行风险指标：直接连接项目卡点、预算对账和伦理治理接口。 -->
      <el-row :gutter="16" class="metric-row risk-row" style="margin-top: 16px;">
        <el-col v-for="card in riskCards" :key="card.key" :xs="12" :sm="6">
          <el-card v-loading="riskLoading" shadow="never" class="risk-card" :class="`risk-${card.tone}`" @click="showRiskDetails(card)">
            <div class="risk-card-header">
              <span class="m-title">{{ card.title }}</span>
              <el-icon><WarningFilled /></el-icon>
            </div>
            <span class="m-number">{{ card.value }} <small>{{ card.unit }}</small></span>
            <span class="m-tip">{{ card.tip }}</span>
            <el-button type="primary" link size="small" class="risk-link">查看风险明细</el-button>
          </el-card>
        </el-col>
      </el-row>

      <!-- 双图表分析区域 -->
      <el-row :gutter="16" style="margin-top: 16px;">
        <el-col :xs="24" :lg="14">
          <el-card shadow="never" class="chart-card">
            <template #header>
            <span class="chart-title">项目经费与数量分布</span>
            </template>
            <div ref="deptChartRef" class="chart-box"></div>
          </el-card>
        </el-col>

        <el-col :xs="24" :lg="10">
          <el-card shadow="never" class="chart-card">
            <template #header>
            <span class="chart-title">科研指标数量分布</span>
            </template>
            <div ref="jcrChartRef" class="chart-box"></div>
          </el-card>
        </el-col>
      </el-row>

      <!-- 指标口径与明细下钻表格 -->
      <el-card shadow="never" style="margin-top: 16px;">
        <template #header>
          <span class="chart-title">核心指标统计口径与明细钻取</span>
        </template>
        <el-table v-loading="loading" :data="metricDetails" stripe style="width: 100%;">
          <el-table-column prop="指标名称" label="科研核心指标" width="180" />
          <el-table-column prop="当前数值" label="统计数值" width="130" align="right">
            <template #default="{ row }">
              <span style="font-weight: 700; color: #409eff;">{{ row.当前数值 }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="口径版本" label="核算口径版本" width="140">
            <template #default="{ row }">
              <el-tag size="small" type="info">{{ row.口径版本 }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="计算说明" label="口径定义与包含范围" min-width="260" show-overflow-tooltip />
          <el-table-column label="操作" width="100" align="center">
            <template #default="{ row }">
              <el-button type="primary" link size="small" @click="drillDown(row)">下钻明细</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>

      <el-dialog v-model="drillDialogVisible" :title="`指标明细 · ${drillTitle}`" width="80%" destroy-on-close>
        <el-alert v-if="drillDefinition" :title="drillDefinition" type="info" :closable="false" show-icon style="margin-bottom: 12px;" />
        <el-table v-loading="drillLoading" :data="drillRows" stripe max-height="420">
          <el-table-column v-for="column in (drillRows.length ? Object.keys(drillRows[0]) : [])" :key="column" :prop="column" :label="column" min-width="140" show-overflow-tooltip />
          <template #empty>
            <el-empty description="暂无可展示的真实明细" :image-size="72" />
          </template>
        </el-table>
      </el-dialog>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick } from 'vue'
import { getAnalyticsOverview } from '@/api/workbench'
import {
  getAnalyticsDrilldown,
  getBottleneckSummary,
  getBudgetAnomalies,
  getEthicsExpiryReminders,
  getEthicsFollowupSummary,
  type EthicsFollowupSummary
} from '@/api/analytics'
import * as echarts from 'echarts'
import { ElMessage } from 'element-plus'
import { Download, WarningFilled } from '@element-plus/icons-vue'

const dimension = ref('全院总览')
const deptChartRef = ref<HTMLDivElement | null>(null)
const jcrChartRef = ref<HTMLDivElement | null>(null)
const overviewMetrics = ref<any[]>([])
const overviewDefinitions = ref<any[]>([])
const loading = ref(false)
const riskLoading = ref(false)
const drillLoading = ref(false)
const drillDialogVisible = ref(false)
const drillRows = ref<any[]>([])
const drillTitle = ref('')
const drillDefinition = ref('')
const riskSignals = ref({
  bottlenecks: [] as any[],
  budgetAnomalies: [] as any[],
  ethicsReminders: { 批件提醒: [] as any[], 持续审查提醒: [] as any[] },
  ethicsFollowupSummary: null as EthicsFollowupSummary | null,
  severeAdverseEvents: [] as any[]
})

const metricDetails = ref<any[]>([])

const metricByName = (name: string) => overviewMetrics.value.find((item) => item?.指标 === name)
const projectSummary = computed(() => {
  const vertical = metricByName('纵向项目')
  const horizontal = metricByName('横向项目')
  return {
    count: Number(vertical?.数量 || 0) + Number(horizontal?.数量 || 0),
    tip: `纵向 ${Number(vertical?.数量 || 0)} 项 / 横向 ${Number(horizontal?.数量 || 0)} 项`
  }
})
const financeSummary = computed(() => {
  const amount = Number(metricByName('纵向项目')?.数值 || 0) + Number(metricByName('横向项目')?.数值 || 0)
  return {
    amount: amount ? (amount / 10000).toFixed(1) : '—',
    tip: amount ? '按当前统计口径汇总登记金额' : '暂无可用经费统计数据'
  }
})
const resultSummary = computed(() => {
  const count = Number(metricByName('科研成果')?.数量 || 0)
  return { count, tip: count ? '论文、著作、专利和获奖成果总数' : '暂无可用成果统计数据' }
})
const peopleSummary = computed(() => {
  const count = Number(metricByName('科研人员')?.数量 || 0)
  return { count, tip: count ? '人员主数据总数' : '暂无可用人员统计数据' }
})

const riskCards = computed(() => {
  const bottlenecks = riskSignals.value.bottlenecks || []
  const budget = riskSignals.value.budgetAnomalies || []
  const reminders = riskSignals.value.ethicsReminders || {}
  const expiry = (reminders.批件提醒 || []).length
  const followupSummary = riskSignals.value.ethicsFollowupSummary?.汇总 || {}
  const followup = Math.max((reminders.持续审查提醒 || []).length, Number(followupSummary.逾期持续审查数 || 0))
  const severe = riskSignals.value.severeAdverseEvents || []
  const severeCount = Number(followupSummary.严重不良事件数 ?? severe.length)
  return [
    {
      key: 'bottleneck',
      title: '项目卡点与延期',
      value: bottlenecks.reduce((sum, item) => sum + Number(item?.事件数 || 0), 0),
      unit: '项',
      tip: bottlenecks.length ? '按事件类型、原因代码和严重程度聚合' : '暂无管理权限或暂无卡点数据',
      tone: 'blue'
    },
    {
      key: 'budget',
      title: '经费异常对账',
      value: budget.length,
      unit: '项',
      tip: budget.length ? '包含预算、到账、报销差额和处理状态' : '暂无预算异常',
      tone: 'orange'
    },
    {
      key: 'ethics',
      title: '伦理逾期/持续审查',
      value: expiry + followup,
      unit: '项',
      tip: `批件 ${expiry} 项 · 逾期持续审查 ${followup} 项`,
      tone: 'purple'
    },
    {
      key: 'adverse',
      title: '严重不良事件',
      value: severeCount,
      unit: '项',
      tip: severeCount ? '仅统计已登记的严重/危及生命事件' : '暂无严重不良事件',
      tone: 'red'
    }
  ]
})

const modeForDimension = () => {
  if (dimension.value === '纵向项目' || dimension.value === '横向攻关') return 'project'
  if (dimension.value === '成果论文') return 'result'
  return 'overview'
}

function formatMetricValue(row: any) {
  const value = Number(row?.数值 || 0)
  if (!value) return `${Number(row?.数量 || 0)} ${row?.指标 === '科研人员' ? '人' : '项'}`
  return `¥${(value / 10000).toFixed(1)} 万元`
}

async function onDimensionChange() {
  ElMessage.info(`已切换统计维度为【${dimension.value}】`)
  await loadOverview()
  await nextTick()
  initCharts()
}

function handleExport() {
  if (!metricDetails.value.length) {
    ElMessage.warning('当前没有可导出的真实统计数据')
    return
  }
  const rows = metricDetails.value.map((row) => [row.指标名称, row.当前数值, row.口径版本, row.计算说明].map((v) => `"${String(v ?? '').replace(/"/g, '""')}"`).join(','))
  const csv = '\uFEFF指标,统计数值,口径版本,统计说明\r\n' + rows.join('\r\n')
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `科研统计-${modeForDimension()}-${new Date().toISOString().slice(0, 10)}.csv`
  link.click()
  URL.revokeObjectURL(url)
  ElMessage.success('真实统计数据已导出')
}

function drillDown(row: any) {
  drillTitle.value = row.指标名称
  drillDefinition.value = row.计算说明 || ''
  drillRows.value = []
  drillDialogVisible.value = true
  drillLoading.value = true
  getAnalyticsDrilldown(row.指标名称)
    .then((result) => {
      drillRows.value = Array.isArray(result?.明细) ? result.明细 : []
      drillDefinition.value = result?.口径说明 || drillDefinition.value
      if (!drillRows.value.length) ElMessage.info('该指标当前没有可下钻的明细')
    })
    .catch(() => {
      drillRows.value = []
    })
    .finally(() => {
      drillLoading.value = false
    })
}

function riskRows(key: string) {
  if (key === 'bottleneck') return riskSignals.value.bottlenecks
  if (key === 'budget') return riskSignals.value.budgetAnomalies
  if (key === 'ethics') {
    return [
      ...(riskSignals.value.ethicsReminders.批件提醒 || []).map((item) => ({ ...item, 风险类型: '批件到期' })),
      ...(riskSignals.value.ethicsReminders.持续审查提醒 || []).map((item) => ({ ...item, 风险类型: '持续审查' })),
      ...(riskSignals.value.ethicsFollowupSummary?.汇总 ? [{ ...riskSignals.value.ethicsFollowupSummary.汇总, 风险类型: '持续记录汇总' }] : [])
    ]
  }
  return riskSignals.value.severeAdverseEvents
}

function showRiskDetails(card: any) {
  drillTitle.value = card.title
  drillDefinition.value = card.tip
  drillRows.value = riskRows(card.key)
  drillDialogVisible.value = true
}

function initCharts() {
  const metrics = overviewMetrics.value.filter((item) => item && item.指标)
  if (deptChartRef.value) {
    const chart = echarts.init(deptChartRef.value)
    chart.setOption({
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
      xAxis: {
        type: 'category',
        data: metrics.map((item) => item.指标)
      },
      yAxis: { type: 'value', name: '经费金额 (万元)' },
      series: [
        { name: '登记金额', type: 'bar', data: metrics.map((item) => Number(item.数值 || 0) / 10000), itemStyle: { color: '#409eff' } }
      ]
    })
  }

  if (jcrChartRef.value) {
    const chart = echarts.init(jcrChartRef.value)
    chart.setOption({
      tooltip: { trigger: 'item' },
      legend: { bottom: '0', left: 'center' },
      series: [
        {
          name: '指标数量',
          type: 'pie',
          radius: ['40%', '70%'],
          avoidLabelOverlap: false,
          itemStyle: { borderRadius: 6, borderColor: '#fff', borderWidth: 2 },
          label: { show: false, position: 'center' },
          emphasis: { label: { show: true, fontSize: 16, fontWeight: 'bold' } },
          data: metrics.map((item, index) => ({ value: Number(item.数量 || 0), name: item.指标, itemStyle: { color: ['#3b82f6', '#10b981', '#f59e0b', '#94a3b8'][index % 4] } }))
        }
      ]
    })
  }
}

async function loadOverview() {
  loading.value = true
  try {
    const result = await getAnalyticsOverview(modeForDimension())
    overviewMetrics.value = Array.isArray(result?.指标) ? result.指标 : []
    overviewDefinitions.value = Array.isArray(result?.口径) ? result.口径 : []
    metricDetails.value = overviewMetrics.value.map((row) => {
      const definition = overviewDefinitions.value.find((item) => item.指标名称 === row.指标 || item.指标代码 === row.指标)
      return {
        指标名称: row.指标,
        当前数值: formatMetricValue(row),
        口径版本: definition?.版本号 ? `v${definition.版本号}` : '实时数据',
        计算说明: definition?.口径说明 || row.说明 || '以当前 PostgreSQL 统计视图为准'
      }
    })
  } catch {
    overviewMetrics.value = []
    overviewDefinitions.value = []
    metricDetails.value = []
  } finally {
    loading.value = false
  }
}

async function loadRiskSignals() {
  riskLoading.value = true
  const [bottlenecks, budgetAnomalies, ethicsReminders, severeAdverseEvents] = await Promise.allSettled([
    getBottleneckSummary(),
    getBudgetAnomalies(),
    getEthicsExpiryReminders(60),
    getEthicsFollowupSummary()
  ])
  riskSignals.value = {
    bottlenecks: bottlenecks.status === 'fulfilled' && Array.isArray(bottlenecks.value) ? bottlenecks.value : [],
    budgetAnomalies: budgetAnomalies.status === 'fulfilled' && Array.isArray(budgetAnomalies.value) ? budgetAnomalies.value : [],
    ethicsReminders: ethicsReminders.status === 'fulfilled' && ethicsReminders.value
      ? {
          批件提醒: ethicsReminders.value.批件提醒 || [],
          持续审查提醒: ethicsReminders.value.持续审查提醒 || []
        }
      : { 批件提醒: [], 持续审查提醒: [] },
    ethicsFollowupSummary: severeAdverseEvents.status === 'fulfilled' ? severeAdverseEvents.value : null,
    severeAdverseEvents: severeAdverseEvents.status === 'fulfilled' && Array.isArray(severeAdverseEvents.value?.严重事件) ? severeAdverseEvents.value.严重事件 : []
  }
  riskLoading.value = false
}

onMounted(async () => {
  await Promise.all([loadOverview(), loadRiskSignals()])
  await nextTick()
  initCharts()
})
</script>

<style scoped lang="scss">
.page-container {
  padding: 16px;

  .screen-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding-bottom: 16px;
    border-bottom: 1px solid #f1f5f9;
    flex-wrap: wrap;
    gap: 12px;

    .header-left {
      h2 {
        font-size: 18px;
        color: #0f172a;
        margin-bottom: 4px;
      }
      .header-sub {
        font-size: 13px;
        color: #64748b;
      }
    }

    .header-right {
      display: flex;
      align-items: center;
    }
  }

  .metric-row {
    .m-card {
      padding: 16px;
      border-radius: 8px;
      color: #ffffff;
      display: flex;
      flex-direction: column;

      .m-title {
        font-size: 12px;
        opacity: 0.9;
        margin-bottom: 4px;
      }

      .m-number {
        font-size: 24px;
        font-weight: 700;
        line-height: 1.2;

        small {
          font-size: 14px;
          font-weight: 400;
        }
      }

      .m-tip {
        font-size: 12px;
        opacity: 0.85;
        margin-top: 6px;
      }

      &.c-blue {
        background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%);
      }
      &.c-green {
        background: linear-gradient(135deg, #10b981 0%, #047857 100%);
      }
      &.c-purple {
        background: linear-gradient(135deg, #8b5cf6 0%, #6d28d9 100%);
      }
      &.c-orange {
        background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
      }
    }

    &.risk-row {
      .risk-card {
        position: relative;
        min-height: 138px;
        border: 1px solid #e2e8f0;
        cursor: pointer;
        transition: box-shadow 0.2s ease, transform 0.2s ease;

        &:hover {
          box-shadow: 0 6px 18px rgba(15, 23, 42, 0.1);
          transform: translateY(-1px);
        }

        .risk-card-header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          color: #334155;
          margin-bottom: 5px;
        }

        .m-title {
          font-size: 12px;
          color: #475569;
        }

        .m-number {
          display: block;
          font-size: 24px;
          color: #0f172a;
          font-weight: 700;
          line-height: 1.2;

          small {
            font-size: 13px;
            font-weight: 400;
          }
        }

        .m-tip {
          display: block;
          min-height: 32px;
          margin-top: 5px;
          color: #64748b;
          font-size: 11px;
          line-height: 1.45;
        }

        .risk-link {
          padding: 0;
          font-size: 11px;
        }

        &.risk-blue .el-icon { color: #2563eb; }
        &.risk-orange .el-icon { color: #d97706; }
        &.risk-purple .el-icon { color: #7c3aed; }
        &.risk-red .el-icon { color: #dc2626; }
      }
    }
  }

  .chart-card {
    background-color: #ffffff;

    .chart-title {
      font-weight: 600;
      font-size: 14px;
      color: #1e293b;
    }

    .chart-box {
      width: 100%;
      height: 260px;
    }
  }

  @media (max-width: 768px) {
    padding: 8px;

    .screen-header {
      align-items: stretch;

      .header-left h2 {
        font-size: 16px;
      }

      .header-right {
        width: 100%;
        flex-wrap: wrap;

        .el-radio-group {
          width: 100%;
          display: flex;

          .el-radio-button {
            flex: 1;

            :deep(.el-radio-button__inner) {
              width: 100%;
              padding-inline: 4px;
              font-size: 12px;
            }
          }
        }

        .el-button {
          width: 100%;
          margin: 8px 0 0 !important;
        }
      }
    }

    .risk-row .risk-card {
      min-height: 130px;
      padding: 12px;
    }

    .chart-card .chart-box {
      height: 220px;
    }
  }
}
</style>
