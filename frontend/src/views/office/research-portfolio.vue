<template>
  <div class="research-portfolio-container">
    <!-- 头部卡片 -->
    <div class="header-card">
      <div class="header-info">
        <h2 class="page-title">
          <el-icon class="title-icon"><DataBoard /></el-icon>
          我的科研全景档案与成果资产
        </h2>
        <p class="page-subtitle">
          汇聚纵向课题、横向产学研项目、知识产权专利、专著论文与科技奖励，呈现个人学术资产沉淀与多维科研履历。
        </p>
      </div>
      <div class="header-actions">
        <el-button type="primary" :icon="User" @click="goToProfile">
          个人资料维护
        </el-button>
        <el-button :icon="Refresh" @click="loadPortfolio">刷新全景</el-button>
      </div>
    </div>

    <!-- 个人名片与学术画像卡片 -->
    <el-card shadow="never" class="profile-hero-card">
      <div class="hero-layout">
        <div class="avatar-wrap">
          <el-avatar :size="76" class="user-avatar">
            {{ userProfile?.姓名 ? userProfile.姓名.substring(0, 1) : '科' }}
          </el-avatar>
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
          </div>

          <div class="meta-row">
            <span><strong class="text-muted">工号:</strong> {{ userProfile?.工号 || '-' }}</span>
            <span><strong class="text-muted">学历/学位:</strong> {{ userProfile?.学历 || '-' }} / {{ userProfile?.学位 || '-' }}</span>
            <span><strong class="text-muted">学科门类:</strong> {{ userProfile?.学科门类 || '-' }}</span>
            <span><strong class="text-muted">办公电话:</strong> {{ userProfile?.手机 || '-' }}</span>
            <span><strong class="text-muted">电子邮箱:</strong> {{ userProfile?.邮箱 || '-' }}</span>
          </div>

          <div v-if="userProfile?.研究方向 || userProfile?.学术特长" class="spec-row">
            <div class="spec-item">
              <span class="spec-label">核心方向:</span>
              <span class="spec-val">{{ userProfile?.研究方向 || '未登记研究方向' }}</span>
            </div>
            <div class="spec-item">
              <span class="spec-label">学术特长:</span>
              <span class="spec-val">{{ userProfile?.学术特长 || '未登记特长' }}</span>
            </div>
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
            <span class="stat-label">资助项目总经费</span>
            <span class="stat-value text-purple">¥ {{ formatMoney(totalFunding) }}</span>
          </div>
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
      </el-tabs>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import {
  DataBoard,
  User,
  Refresh,
  TrendCharts,
  Collection,
  Reading,
  Money
} from '@element-plus/icons-vue'
import { getResearchPortfolio } from '@/api/office'
import { useUserStore } from '@/stores/user'
import { formatMoney, formatDate } from '@/utils/format'

const router = useRouter()
const userStore = useUserStore()

const loading = ref(false)
const activeTab = ref('vertical')

const userProfile = ref<any>(null)
const verticalList = ref<any[]>([])
const horizontalList = ref<any[]>([])
const fruitList = ref<any[]>([])

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

async function loadPortfolio() {
  loading.value = true
  try {
    const currentUserId = userProfile.value?.编号 || userStore.userInfo?.编号
    const res = await getResearchPortfolio(currentUserId)
    const data = (res as any)?.data || res
    userProfile.value = data?.个人资料 || userStore.userInfo || null
    verticalList.value = Array.isArray(data?.纵向项目总览) ? data.纵向项目总览 : []
    horizontalList.value = Array.isArray(data?.横向项目总览) ? data.横向项目总览 : []
    fruitList.value = Array.isArray(data?.成果总览) ? data.成果总览 : []
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
  loadPortfolio()
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
  padding: 18px 24px;
  border-radius: 8px;
  border: 1px solid #ebeef5;
  margin-bottom: 16px;
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

.profile-hero-card {
  border-radius: 8px;
  border: 1px solid #ebeef5;
  margin-bottom: 16px;
  background: #ffffff;
}

.hero-layout {
  display: flex;
  align-items: flex-start;
  gap: 20px;
}

.user-avatar {
  background: linear-gradient(135deg, #409eff, #1d68c4);
  color: #ffffff;
  font-size: 30px;
  font-weight: 600;
}

.hero-content {
  flex: 1;
}

.name-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 8px;
}

.user-name {
  font-size: 20px;
  font-weight: 700;
  color: #303133;
}

.meta-row {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  font-size: 13px;
  color: #606266;
  margin-bottom: 10px;
}

.spec-row {
  border-top: 1px dashed #ebeef5;
  padding-top: 8px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.spec-item {
  font-size: 12px;
  color: #606266;
}

.spec-label {
  color: #909399;
  margin-right: 6px;
}

.stats-row {
  margin-bottom: 16px;
}

.stat-card {
  border-radius: 8px;
  display: flex;
  align-items: center;
  padding: 8px 12px;
}

:deep(.stat-card .el-card__body) {
  display: flex;
  align-items: center;
  width: 100%;
  padding: 10px 14px;
}

.stat-icon-wrap {
  width: 46px;
  height: 46px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
  margin-right: 14px;
}

.stat-icon-wrap.blue {
  background: #ecf5ff;
  color: #409eff;
}
.stat-icon-wrap.green {
  background: #f0f9eb;
  color: #67c23a;
}
.stat-icon-wrap.orange {
  background: #fdf6ec;
  color: #e6a23c;
}
.stat-icon-wrap.purple {
  background: #f4f0ff;
  color: #8e44ad;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 12px;
  color: #909399;
  margin-bottom: 4px;
}

.stat-value {
  font-size: 20px;
  font-weight: 700;
  color: #303133;
}

.stat-value small {
  font-size: 12px;
  font-weight: normal;
}

.text-green {
  color: #67c23a;
}
.text-orange {
  color: #e6a23c;
}
.text-purple {
  color: #8e44ad;
}
.text-muted {
  color: #909399;
}
.font-medium {
  font-weight: 500;
}

.main-card {
  border-radius: 8px;
  border: 1px solid #ebeef5;
}
</style>
