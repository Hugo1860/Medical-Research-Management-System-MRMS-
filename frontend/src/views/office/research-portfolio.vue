<template>
  <div class="research-portfolio-container">
    <!-- 头部卡片与全局操作栏 -->
    <div class="header-card no-print">
      <div class="header-info">
        <h2 class="page-title">
          <el-icon class="title-icon"><DataBoard /></el-icon>
          科研人员全景学术画像与成果档案
        </h2>
        <p class="page-subtitle">
          汇聚纵向课题、横向产学研项目、知识产权专利、专著论文与科技奖励，呈现个人学术资产沉淀与多维科研履历。
        </p>
      </div>
      <div class="header-actions">
        <!-- 切换查看人员（支持按科室与姓名/工号多维筛选） -->
        <div class="person-selector-group">
          <el-select
            v-model="selectedDeptId"
            placeholder="筛选科室"
            clearable
            filterable
            size="default"
            class="dept-select"
            @change="handleDeptChange"
          >
            <el-option
              v-for="dept in departmentList"
              :key="dept.部门编号 || dept.编号"
              :label="dept.部门名称 || dept.名称"
              :value="dept.部门编号 || dept.编号"
            />
          </el-select>

          <el-select
            v-model="targetPersonId"
            placeholder="搜索人员/工号"
            filterable
            remote
            :remote-method="searchPeople"
            :loading="peopleSearching"
            size="default"
            class="person-select"
            @change="handlePersonChange"
          >
            <el-option
              v-for="person in peopleOptions"
              :key="person.编号"
              :label="`${person.姓名} (${person.工号 || '无工号'}${person.部门名称 ? ' · ' + person.部门名称 : ''})`"
              :value="person.编号"
            />
          </el-select>
        </div>

        <el-button
          v-if="targetPersonId !== myUserId"
          type="info"
          plain
          :icon="User"
          @click="resetToMyPortfolio"
        >
          查看我的画像
        </el-button>
        <el-button
          type="success"
          plain
          :icon="Printer"
          @click="handlePrintReport"
        >
          导出 / 打印学术档案
        </el-button>
        <el-button
          v-if="targetPersonId === myUserId"
          type="primary"
          :icon="User"
          @click="goToProfile"
        >
          个人资料维护
        </el-button>
        <el-button :icon="Refresh" @click="loadPortfolio">刷新全景</el-button>
      </div>
    </div>

    <!-- 打印专用顶部抬头 (仅在打印时呈现) -->
    <div class="print-header print-only">
      <h1 class="print-title">医疗科研管理系统 · 科研人员个人学术档案与业绩报告</h1>
      <div class="print-meta">
        <span>档案对象: <strong>{{ userProfile?.姓名 || '-' }}</strong> (工号: {{ userProfile?.工号 || '-' }})</span>
        <span>所属部门: {{ userProfile?.部门名称 || '-' }}</span>
        <span>技术职称: {{ userProfile?.技术职称 || '-' }}</span>
        <span>生成时间: {{ printDate }}</span>
      </div>
    </div>

    <!-- 个人名片与学术画像卡片 -->
    <el-card shadow="never" class="profile-hero-card">
      <div class="hero-layout">
        <div class="avatar-wrap">
          <el-avatar :size="80" class="user-avatar">
            {{ userProfile?.姓名 ? userProfile.姓名.substring(0, 1) : '研' }}
          </el-avatar>
          <div v-if="capabilityScores?.评级称号" class="rank-badge-wrap">
            <el-tag size="small" effect="dark" type="warning" class="rank-badge">
              {{ capabilityScores.评级称号 }}
            </el-tag>
          </div>
        </div>

        <div class="hero-content">
          <div class="name-row">
            <span class="user-name">{{ userProfile?.姓名 || '科研人员' }}</span>
            <el-tag size="small" type="primary" effect="dark" class="role-badge">
              {{ userProfile?.部门名称 || '未分配科室' }}
            </el-tag>
            <el-tag v-if="userProfile?.技术职称" size="small" type="success" effect="plain">
              {{ userProfile?.技术职称 }}
            </el-tag>
            <el-tag v-if="userProfile?.岗位名称" size="small" type="info" effect="plain">
              {{ userProfile?.岗位名称 }}
            </el-tag>
            <el-tag
              v-if="capabilityInfo?.合作意愿"
              size="small"
              :type="getWillingnessTag(capabilityInfo.合作意愿)"
              effect="light"
            >
              {{ capabilityInfo.合作意愿 }}
            </el-tag>
          </div>

          <div class="meta-row">
            <span><strong class="text-muted">工号:</strong> {{ userProfile?.工号 || '-' }}</span>
            <span><strong class="text-muted">学历/学位:</strong> {{ userProfile?.学历 || '-' }} / {{ userProfile?.学位 || '-' }}</span>
            <span><strong class="text-muted">学科门类:</strong> {{ userProfile?.学科门类 || '-' }}</span>
            <span><strong class="text-muted">毕业院校:</strong> {{ userProfile?.获得最高学历的院校 || '-' }}</span>
            <span><strong class="text-muted">联系方式:</strong> {{ userProfile?.手机 || '-' }}</span>
            <span><strong class="text-muted">电子邮箱:</strong> {{ userProfile?.邮箱 || '-' }}</span>
          </div>

          <div class="spec-row">
            <div class="spec-item">
              <span class="spec-label">核心方向:</span>
              <span class="spec-val highlight">{{ userProfile?.研究方向 || capabilityInfo?.研究专长 || '暂未维护核心方向' }}</span>
            </div>
            <div class="spec-item">
              <span class="spec-label">学术特长:</span>
              <span class="spec-val">{{ userProfile?.学术特长 || capabilityInfo?.技术服务能力 || '暂未登记学术特长' }}</span>
            </div>
            <div v-if="capabilityTags?.length" class="spec-item tags-item">
              <span class="spec-label">能力标签:</span>
              <el-tag
                v-for="tag in capabilityTags"
                :key="tag.编号 || tag.标签"
                size="small"
                class="cap-tag"
                effect="plain"
              >
                {{ tag.标签类型 }} · {{ tag.标签 }}
              </el-tag>
            </div>
          </div>
        </div>

        <!-- 综合科研指数大字展示区 -->
        <div class="hero-score-badge">
          <div class="score-circle">
            <span class="score-num">{{ capabilityScores?.综合科研指数 ?? '--' }}</span>
            <span class="score-sub">综合科研指数</span>
          </div>
          <div class="score-hint">
            <el-progress
              :percentage="Math.min(100, capabilityScores?.综合科研指数 || 0)"
              :show-text="false"
              status="success"
              style="width: 120px; margin-top: 6px;"
            />
          </div>
        </div>
      </div>
    </el-card>

    <!-- 资产宏观指标卡片 -->
    <el-row :gutter="16" class="stats-row">
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap blue"><el-icon><TrendCharts /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">纵向科研课题</span>
            <span class="stat-value">{{ verticalList.length }} <small>项</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap green"><el-icon><Collection /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">横向产学研合作</span>
            <span class="stat-value text-green">{{ horizontalList.length }} <small>项</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap orange"><el-icon><Reading /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">学术成果全集</span>
            <span class="stat-value text-orange">{{ fruitList.length }} <small>项</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap purple"><el-icon><Money /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">立项与资助总经费</span>
            <span class="stat-value text-purple">¥ {{ formatMoney(totalFunding) }}</span>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 核心可视化画像看板：五维能力雷达图 + 历年产出趋势图 -->
    <el-row :gutter="16" class="charts-row">
      <!-- 左栏：五维学术能力量化雷达图 -->
      <el-col :xs="24" :md="10">
        <el-card shadow="never" class="chart-card">
          <template #header>
            <div class="card-header-bar">
              <div class="header-left">
                <el-icon class="mr-1 text-primary"><Trophy /></el-icon>
                <span class="card-title">五维学术能力量化雷达画像</span>
              </div>
              <el-tag size="small" type="success" effect="plain">综合评分模型</el-tag>
            </div>
          </template>
          <div class="radar-content-wrap">
            <div ref="radarChartRef" class="echarts-box radar-chart"></div>
            <!-- 指标得分细项列表 -->
            <div class="radar-score-grid">
              <div class="score-metric-item">
                <span class="metric-name">课题主持力</span>
                <span class="metric-val text-primary">{{ capabilityScores?.课题主持得分 ?? '--' }} 分</span>
              </div>
              <div class="score-metric-item">
                <span class="metric-name">经费规模度</span>
                <span class="metric-val text-warning">{{ capabilityScores?.经费规模得分 ?? '--' }} 分</span>
              </div>
              <div class="score-metric-item">
                <span class="metric-name">论文发表力</span>
                <span class="metric-val text-success">{{ capabilityScores?.论文能力得分 ?? '--' }} 分</span>
              </div>
              <div class="score-metric-item">
                <span class="metric-name">成果转化度</span>
                <span class="metric-val text-danger">{{ capabilityScores?.成果转化得分 ?? '--' }} 分</span>
              </div>
              <div class="score-metric-item">
                <span class="metric-name">团队引领度</span>
                <span class="metric-val text-purple">{{ capabilityScores?.团队指导得分 ?? '--' }} 分</span>
              </div>
            </div>
          </div>
        </el-card>
      </el-col>

      <!-- 右栏：历年科研学术产出走势图 -->
      <el-col :xs="24" :md="14">
        <el-card shadow="never" class="chart-card">
          <template #header>
            <div class="card-header-bar">
              <div class="header-left">
                <el-icon class="mr-1 text-warning"><TrendCharts /></el-icon>
                <span class="card-title">历年科研学术产出与经费演进</span>
              </div>
              <span class="text-muted text-xs">近 5~6 年立项、成果与到账经费走势</span>
            </div>
          </template>
          <div ref="trendChartRef" class="echarts-box trend-chart"></div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 资产全景数据选项卡 -->
    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab" class="portfolio-tabs">
        <!-- Tab 1: 纵向项目资产 -->
        <el-tab-pane label="纵向科研课题" name="vertical">
          <el-table
            v-loading="loading"
            :data="verticalList"
            stripe
            border
            style="width: 100%"
          >
            <el-table-column type="index" label="序号" width="65" align="center" />
            <el-table-column prop="项目中文名称" label="课题名称" min-width="240" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="font-medium">{{ row.项目中文名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="项目级别" label="课题级别" width="130" align="center">
              <template #default="{ row }">
                <el-tag size="small">{{ row.项目级别 || '-' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="分类名称" label="课题分类" width="140" show-overflow-tooltip />
            <el-table-column prop="负责人姓名" label="负责人" width="110" />
            <el-table-column label="批准经费 (元)" width="130" align="right">
              <template #default="{ row }">
                <span class="font-medium text-green">¥ {{ formatMoney(row.批准经费) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="起止周期" width="200" align="center">
              <template #default="{ row }">
                <span>{{ formatDate(row.计划开始日期 || row.开始时间) }} ~ {{ formatDate(row.计划完成日期 || row.结束时间) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="审核流转" width="110" align="center">
              <template #default="{ row }">
                <el-tag size="small" type="success" effect="plain">{{ row.审核进度 || '执行中' }}</el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- Tab 2: 横向项目与合同 -->
        <el-tab-pane label="横向项目与产学研合同" name="horizontal">
          <el-table
            v-loading="loading"
            :data="horizontalList"
            stripe
            border
            style="width: 100%"
          >
            <el-table-column type="index" label="序号" width="65" align="center" />
            <el-table-column prop="项目名称" label="横向项目 / 合同名称" min-width="240" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="font-medium">{{ row.合同名称 || row.项目名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="委托单位" label="委托方 / 合作企业" min-width="180" show-overflow-tooltip>
              <template #default="{ row }">
                <span>{{ row.委托单位 || row.项目来源单位 || '-' }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="项目类型" label="合作模式" width="130">
              <template #default="{ row }">
                <el-tag size="small" effect="plain">{{ row.项目类型 || row.名称 || '-' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="合同经费 (元)" width="140" align="right">
              <template #default="{ row }">
                <span class="font-medium text-green">¥ {{ formatMoney(row.合同金额 || row.合同经费) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="签订日期" width="120" align="center">
              <template #default="{ row }">{{ formatDate(row.签订时间 || row.建立时间) }}</template>
            </el-table-column>
            <el-table-column label="状态" width="130" align="center">
              <template #default="{ row }">
                <el-tag size="small" type="primary">{{ row.审核进度 || row.状态 || '履行中' }}</el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- Tab 3: 学术成果总集 -->
        <el-tab-pane label="学术成果全景" name="fruits">
          <el-table
            v-loading="loading"
            :data="fruitList"
            stripe
            border
            style="width: 100%"
          >
            <el-table-column type="index" label="序号" width="65" align="center" />
            <el-table-column prop="成果名称" label="成果名称" min-width="260" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="font-medium">{{ row.成果名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="成果类型" label="成果类别" width="120" align="center">
              <template #default="{ row }">
                <el-tag :type="getTypeTagType(row.成果类型)" size="small">
                  {{ row.成果类型 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="发起人姓名" label="署名 / 发起人" width="120" />
            <el-table-column prop="发起人部门名称" label="所属科室" min-width="160" show-overflow-tooltip />
          </el-table>
        </el-tab-pane>

        <!-- Tab 4: 团队与导师合作画像 -->
        <el-tab-pane label="团队与师承指导" name="collaboration">
          <el-row :gutter="16">
            <el-col :xs="24" :md="12">
              <div class="sub-table-block">
                <h4 class="sub-block-title">
                  <el-icon class="mr-1 text-primary"><Briefcase /></el-icon>
                  参与/领衔科研团队
                </h4>
                <el-table :data="teamList" border size="small" empty-text="暂无团队记录">
                  <el-table-column prop="团队名称" label="科研团队" min-width="150" />
                  <el-table-column prop="成员角色" label="分工角色" width="100" />
                  <el-table-column prop="研究方向" label="团队方向" min-width="140" show-overflow-tooltip />
                  <el-table-column prop="成员状态" label="状态" width="80" align="center" />
                </el-table>
              </div>
            </el-col>
            <el-col :xs="24" :md="12">
              <div class="sub-table-block">
                <h4 class="sub-block-title">
                  <el-icon class="mr-1 text-success"><User /></el-icon>
                  导师与带教学员关系
                </h4>
                <el-table :data="mentorList" border size="small" empty-text="暂无师承或带教记录">
                  <el-table-column prop="导师姓名" label="导师" width="100" />
                  <el-table-column prop="学员姓名" label="学员/研究生" width="110" />
                  <el-table-column prop="关系类型" label="类型" width="90" />
                  <el-table-column prop="状态" label="状态" width="80" align="center" />
                  <el-table-column label="周期" min-width="140" show-overflow-tooltip>
                    <template #default="{ row }">
                      {{ formatDate(row.开始日期) }} ~ {{ formatDate(row.结束日期) }}
                    </template>
                  </el-table-column>
                </el-table>
              </div>
            </el-col>
          </el-row>
        </el-tab-pane>
      </el-tabs>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, nextTick, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import * as echarts from 'echarts'
import {
  DataBoard,
  User,
  Refresh,
  TrendCharts,
  Collection,
  Reading,
  Money,
  Printer,
  Trophy,
  Briefcase
} from '@element-plus/icons-vue'
import { getResearchPortfolio, type CapabilityScores, type YearTrendItem } from '@/api/office'
import { getDepartmentList, getPeopleList } from '@/api/organization'
import { useUserStore } from '@/stores/user'
import { formatMoney, formatDate } from '@/utils/format'

const router = useRouter()
const userStore = useUserStore()

const myUserId = computed(() => userStore.userInfo?.编号 || 0)
const targetPersonId = ref<number>(myUserId.value)
const selectedDeptId = ref<number | undefined>(undefined)

const loading = ref(false)
const peopleSearching = ref(false)
const activeTab = ref('vertical')

const userProfile = ref<any>(null)
const capabilityInfo = ref<any>(null)
const capabilityTags = ref<any[]>([])
const capabilityScores = ref<CapabilityScores | null>(null)
const yearTrends = ref<YearTrendItem[]>([])
const verticalList = ref<any[]>([])
const horizontalList = ref<any[]>([])
const fruitList = ref<any[]>([])
const teamList = ref<any[]>([])
const mentorList = ref<any[]>([])

const departmentList = ref<any[]>([])
const peopleOptions = ref<any[]>([])

const radarChartRef = ref<HTMLElement | null>(null)
const trendChartRef = ref<HTMLElement | null>(null)
let radarChart: echarts.ECharts | null = null
let trendChart: echarts.ECharts | null = null

const printDate = computed(() => {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')} ${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`
})

const totalFunding = computed(() => {
  let sum = 0
  for (const v of verticalList.value) {
    sum += Number(v.批准经费 || 0)
  }
  for (const h of horizontalList.value) {
    sum += Number(h.合同金额 || h.合同经费 || 0)
  }
  return sum
})

function getTypeTagType(type?: string) {
  if (!type) return 'info'
  if (type.includes('论文')) return 'primary'
  if (type.includes('著作')) return 'success'
  if (type.includes('专利')) return 'warning'
  if (type.includes('获奖')) return 'danger'
  return 'info'
}

function getWillingnessTag(w?: string) {
  if (w === '可合作') return 'success'
  if (w === '需邀约') return 'warning'
  return 'info'
}

async function searchPeople(query: string) {
  peopleSearching.value = true
  try {
    const res = await getPeopleList(
      { index: 1, size: 25 },
      {
        Like姓名: query ? query.trim() : undefined,
        部门编号: selectedDeptId.value,
        是否启用: true
      }
    )
    const list = (res as any)?.rows || (res as any)?.List || (res as any)?.data || []
    peopleOptions.value = Array.isArray(list) ? list : []
  } catch {
    // ignore
  } finally {
    peopleSearching.value = false
  }
}

async function loadDepartments() {
  try {
    const res = await getDepartmentList()
    const list = (res as any)?.data || res || []
    departmentList.value = Array.isArray(list) ? list : []
  } catch {
    // ignore
  }
}

function handleDeptChange() {
  searchPeople('')
}

function handlePersonChange(val: number) {
  if (val) {
    loadPortfolio(val)
  }
}

function resetToMyPortfolio() {
  targetPersonId.value = myUserId.value
  selectedDeptId.value = undefined
  loadPortfolio(myUserId.value)
}

function handlePrintReport() {
  window.print()
}

function initRadarChart() {
  if (!radarChartRef.value) return
  if (!radarChart) {
    radarChart = echarts.init(radarChartRef.value)
  }

  const scores = capabilityScores.value || {
    课题主持得分: 20,
    经费规模得分: 20,
    论文能力得分: 20,
    成果转化得分: 20,
    团队指导得分: 20
  }

  const option: echarts.EChartsOption = {
    tooltip: {
      trigger: 'item'
    },
    radar: {
      indicator: [
        { name: '课题主持力', max: 100 },
        { name: '经费规模度', max: 100 },
        { name: '论文发表力', max: 100 },
        { name: '成果转化度', max: 100 },
        { name: '团队引领度', max: 100 }
      ],
      radius: '68%',
      splitNumber: 4,
      axisName: {
        color: '#475569',
        fontSize: 12,
        fontWeight: 500
      },
      splitArea: {
        areaStyle: {
          color: ['#f8fafc', '#f1f5f9', '#f8fafc', '#eff6ff']
        }
      },
      splitLine: {
        lineStyle: {
          color: '#cbd5e1'
        }
      }
    },
    series: [
      {
        name: '五维学术能力量化',
        type: 'radar',
        data: [
          {
            value: [
              Number(scores.课题主持得分) || 0,
              Number(scores.经费规模得分) || 0,
              Number(scores.论文能力得分) || 0,
              Number(scores.成果转化得分) || 0,
              Number(scores.团队指导得分) || 0
            ],
            name: `${userProfile.value?.姓名 || '科研人员'} 学术画像`,
            areaStyle: {
              color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                { offset: 0, color: 'rgba(64, 158, 255, 0.45)' },
                { offset: 1, color: 'rgba(64, 158, 255, 0.1)' }
              ])
            },
            lineStyle: {
              color: '#409eff',
              width: 2.5
            },
            itemStyle: {
              color: '#409eff'
            }
          }
        ]
      }
    ]
  }

  radarChart.setOption(option)
}

function initTrendChart() {
  if (!trendChartRef.value) return
  if (!trendChart) {
    trendChart = echarts.init(trendChartRef.value)
  }

  const trends = yearTrends.value && yearTrends.value.length > 0
    ? yearTrends.value
    : [
        { 年度: '2022', 纵向课题数: 0, 纵向经费: 0, 横向合同数: 0, 横向经费: 0, 论文数: 0, 专利数: 0, 著作数: 0, 获奖数: 0 },
        { 年度: '2023', 纵向课题数: 0, 纵向经费: 0, 横向合同数: 0, 横向经费: 0, 论文数: 0, 专利数: 0, 著作数: 0, 获奖数: 0 },
        { 年度: '2024', 纵向课题数: 0, 纵向经费: 0, 横向合同数: 0, 横向经费: 0, 论文数: 0, 专利数: 0, 著作数: 0, 获奖数: 0 },
        { 年度: '2025', 纵向课题数: 0, 纵向经费: 0, 横向合同数: 0, 横向经费: 0, 论文数: 0, 专利数: 0, 著作数: 0, 获奖数: 0 },
        { 年度: '2026', 纵向课题数: 0, 纵向经费: 0, 横向合同数: 0, 横向经费: 0, 论文数: 0, 专利数: 0, 著作数: 0, 获奖数: 0 }
      ]

  const years = trends.map((t) => `${t.年度}年`)
  const verticalCounts = trends.map((t) => t.纵向课题数)
  const horizontalCounts = trends.map((t) => t.横向合同数)
  const paperAndIpCounts = trends.map((t) => (t.论文数 || 0) + (t.专利数 || 0))
  const fundTotals = trends.map((t) => Number((Number(t.纵向经费 || 0) + Number(t.横向经费 || 0)).toFixed(1)))

  const option: echarts.EChartsOption = {
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'cross' }
    },
    legend: {
      data: ['纵向课题', '横向合同', '论文专利产出', '总经费 (万元)'],
      bottom: 0
    },
    grid: {
      left: '3%',
      right: '4%',
      top: '12%',
      bottom: '12%',
      containLabel: true
    },
    xAxis: {
      type: 'category',
      data: years,
      axisPointer: { type: 'shadow' }
    },
    yAxis: [
      {
        type: 'value',
        name: '项目与成果数 (项/篇)',
        minInterval: 1
      },
      {
        type: 'value',
        name: '经费 (万元)',
        axisLabel: { formatter: '{value} 万' }
      }
    ],
    series: [
      {
        name: '纵向课题',
        type: 'bar',
        data: verticalCounts,
        itemStyle: { color: '#409eff' }
      },
      {
        name: '横向合同',
        type: 'bar',
        data: horizontalCounts,
        itemStyle: { color: '#67c23a' }
      },
      {
        name: '论文专利产出',
        type: 'bar',
        data: paperAndIpCounts,
        itemStyle: { color: '#e6a23c' }
      },
      {
        name: '总经费 (万元)',
        type: 'line',
        yAxisIndex: 1,
        smooth: true,
        data: fundTotals,
        lineStyle: { width: 3, color: '#8e44ad' },
        itemStyle: { color: '#8e44ad' }
      }
    ]
  }

  trendChart.setOption(option)
}

function handleResize() {
  radarChart?.resize()
  trendChart?.resize()
}

async function loadPortfolio(userId?: number) {
  loading.value = true
  const queryId = userId || targetPersonId.value || myUserId.value
  try {
    const res = await getResearchPortfolio(queryId)
    const data = (res as any)?.data || res
    userProfile.value = data?.个人资料 || null
    verticalList.value = Array.isArray(data?.纵向项目总览) ? data.纵向项目总览 : []
    horizontalList.value = Array.isArray(data?.横向项目总览) ? data.横向项目总览 : []
    fruitList.value = Array.isArray(data?.成果总览) ? data.成果总览 : []
    capabilityInfo.value = data?.科研能力 || null
    capabilityTags.value = Array.isArray(data?.能力标签) ? data.能力标签 : []
    teamList.value = Array.isArray(data?.团队) ? data.团队 : []
    mentorList.value = Array.isArray(data?.导师关系) ? data.导师关系 : []
    capabilityScores.value = data?.能力画像量化 || null
    yearTrends.value = Array.isArray(data?.历年产出趋势) ? data.历年产出趋势 : []

    // Ensure targetPersonId reflects loaded person
    if (userProfile.value?.编号) {
      targetPersonId.value = userProfile.value.编号
    }

    await nextTick()
    initRadarChart()
    initTrendChart()
  } catch (err: any) {
    ElMessage.error(err.message || '加载个人科研档案失败')
  } finally {
    loading.value = false
  }
}

function goToProfile() {
  router.push('/office/profile')
}

onMounted(() => {
  loadDepartments()
  searchPeople('')
  loadPortfolio()
  window.addEventListener('resize', handleResize)
})

onBeforeUnmount(() => {
  radarChart?.dispose()
  trendChart?.dispose()
  window.removeEventListener('resize', handleResize)
})
</script>

<style scoped>
.research-portfolio-container {
  padding: 16px 20px 30px;
  background-color: #f6f8fa;
  min-height: calc(100vh - 84px);
}

.header-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #ffffff;
  padding: 16px 20px;
  border-radius: 8px;
  border: 1px solid #ebeef5;
  margin-bottom: 16px;
  flex-wrap: wrap;
  gap: 12px;
}

.page-title {
  margin: 0 0 6px 0;
  font-size: 20px;
  font-weight: 600;
  color: #1e293b;
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
  color: #64748b;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.person-selector-group {
  display: flex;
  gap: 6px;
}

.dept-select {
  width: 140px;
}

.person-select {
  width: 200px;
}

.profile-hero-card {
  border-radius: 8px;
  border: 1px solid #ebeef5;
  margin-bottom: 16px;
  background: #ffffff;
}

.hero-layout {
  display: flex;
  align-items: center;
  gap: 22px;
}

.avatar-wrap {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
}

.user-avatar {
  background: linear-gradient(135deg, #3b82f6, #1d4ed8);
  color: #ffffff;
  font-size: 32px;
  font-weight: 600;
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.25);
}

.rank-badge-wrap {
  margin-top: 2px;
}

.hero-content {
  flex: 1;
}

.name-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 8px;
  flex-wrap: wrap;
}

.user-name {
  font-size: 22px;
  font-weight: 700;
  color: #0f172a;
}

.meta-row {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  font-size: 13px;
  color: #475569;
  margin-bottom: 10px;
}

.spec-row {
  border-top: 1px dashed #e2e8f0;
  padding-top: 10px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.spec-item {
  font-size: 13px;
  color: #475569;
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 4px;
}

.spec-label {
  color: #94a3b8;
  font-weight: 500;
}

.spec-val.highlight {
  color: #2563eb;
  font-weight: 500;
}

.cap-tag {
  margin-right: 4px;
}

.hero-score-badge {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 12px 20px;
  background: #f8fafc;
  border-radius: 12px;
  border: 1px solid #e2e8f0;
}

.score-circle {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.score-num {
  font-size: 34px;
  font-weight: 800;
  color: #2563eb;
  line-height: 1.1;
}

.score-sub {
  font-size: 12px;
  color: #64748b;
  font-weight: 500;
}

.stats-row {
  margin-bottom: 16px;
}

.stat-card {
  border-radius: 8px;
}

:deep(.stat-card .el-card__body) {
  display: flex;
  align-items: center;
  width: 100%;
  padding: 12px 16px;
}

.stat-icon-wrap {
  width: 48px;
  height: 48px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
  margin-right: 14px;
}

.stat-icon-wrap.blue {
  background: #eff6ff;
  color: #3b82f6;
}
.stat-icon-wrap.green {
  background: #f0fdf4;
  color: #16a34a;
}
.stat-icon-wrap.orange {
  background: #fffbeb;
  color: #d97706;
}
.stat-icon-wrap.purple {
  background: #faf5ff;
  color: #7c3aed;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 13px;
  color: #64748b;
  margin-bottom: 4px;
}

.stat-value {
  font-size: 22px;
  font-weight: 700;
  color: #0f172a;
}

.stat-value small {
  font-size: 12px;
  font-weight: normal;
}

.charts-row {
  margin-bottom: 16px;
}

.chart-card {
  border-radius: 8px;
  height: 380px;
  display: flex;
  flex-direction: column;
}

:deep(.chart-card .el-card__body) {
  flex: 1;
  padding: 10px 16px;
  display: flex;
  flex-direction: column;
}

.card-header-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-title {
  font-size: 15px;
  font-weight: 600;
  color: #1e293b;
}

.radar-content-wrap {
  display: flex;
  height: 100%;
  align-items: center;
}

.echarts-box.radar-chart {
  width: 60%;
  height: 290px;
}

.radar-score-grid {
  width: 40%;
  padding-left: 10px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.score-metric-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 6px 10px;
  background: #f8fafc;
  border-radius: 6px;
  border: 1px solid #f1f5f9;
}

.metric-name {
  font-size: 12px;
  color: #475569;
}

.metric-val {
  font-size: 14px;
  font-weight: 700;
}

.echarts-box.trend-chart {
  width: 100%;
  height: 290px;
}

.main-card {
  border-radius: 8px;
  border: 1px solid #ebeef5;
}

.sub-table-block {
  padding: 6px 0;
}

.sub-block-title {
  margin: 0 0 10px 0;
  font-size: 14px;
  font-weight: 600;
  color: #1e293b;
  display: flex;
  align-items: center;
}

.text-green {
  color: #16a34a;
}
.text-orange {
  color: #d97706;
}
.text-purple {
  color: #7c3aed;
}
.text-primary {
  color: #2563eb;
}
.text-warning {
  color: #d97706;
}
.text-danger {
  color: #dc2626;
}
.text-success {
  color: #16a34a;
}
.text-muted {
  color: #94a3b8;
}
.text-xs {
  font-size: 12px;
}
.font-medium {
  font-weight: 500;
}
.mr-1 {
  margin-right: 4px;
}

/* 打印专有样式 */
.print-only {
  display: none;
}

@media print {
  .no-print {
    display: none !important;
  }
  .print-only {
    display: block !important;
  }
  .research-portfolio-container {
    padding: 0;
    background-color: #ffffff;
  }
  .print-header {
    margin-bottom: 20px;
    border-bottom: 2px solid #0f172a;
    padding-bottom: 12px;
  }
  .print-title {
    font-size: 20px;
    font-weight: 700;
    margin: 0 0 8px 0;
    color: #0f172a;
    text-align: center;
  }
  .print-meta {
    display: flex;
    justify-content: space-around;
    font-size: 13px;
    color: #475569;
  }
  .chart-card {
    height: auto !important;
    border: 1px solid #cbd5e1 !important;
  }
  .profile-hero-card,
  .main-card,
  .stat-card {
    border: 1px solid #cbd5e1 !important;
  }
}
</style>
