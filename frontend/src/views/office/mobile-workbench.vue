<template>
  <div class="mobile-container">
    <!-- 移动端顶部状态栏卡片 -->
    <header class="mobile-header">
      <div class="header-top">
        <div>
          <h2 class="workbench-title">移动科研工作台</h2>
          <div class="user-greeting">
            <span>{{ userStore.userInfo?.姓名 || '科研学者' }}</span>
            <span class="user-dept">{{ userStore.userInfo?.部门名称 || '临床医学院' }}</span>
          </div>
        </div>
        <div class="header-badges">
          <div class="dmz-badge" title="DMZ 架构反向代理隔离，保护核心业务数据库不直通外网">
            <span class="dmz-dot"></span>
            <span>DMZ安全隔离</span>
          </div>
          <div class="network-badge" :class="{ offline: !isOnline }">
            <span class="dot"></span>
            <span>{{ isOnline ? '实时在线' : '离线只读' }}</span>
          </div>
        </div>
      </div>

      <!-- 三大核心待办数据指标 -->
      <div class="kpi-grid">
        <div class="kpi-box" @click="router.push('/workflow/todo-approvals')">
          <span class="kpi-num">{{ workbenchData?.待办数量 ?? todoList.length }}</span>
          <span class="kpi-title">待办审批</span>
        </div>
        <div class="kpi-box" @click="router.push('/office/notice')">
          <span class="kpi-num">{{ workbenchData?.未读通知数量 ?? 0 }}</span>
          <span class="kpi-title">未读通知</span>
        </div>
        <div class="kpi-box" @click="router.push('/project/vertical-lifecycle')">
          <span class="kpi-num">{{ workbenchData?.已发起流程数量 ?? 0 }}</span>
          <span class="kpi-title">在办流程</span>
        </div>
      </div>
    </header>

    <!-- 缺件提示警报条 -->
    <div v-if="workbenchData?.缺件数 && workbenchData.缺件数 > 0" class="missing-alert">
      <el-alert
        type="warning"
        show-icon
        :closable="false"
        :title="`缺件提醒：${workbenchData.缺件提示}（共 ${workbenchData.缺件数} 项关键材料待补交）`"
      />
    </div>

    <!-- 角色与下一步事项必须来自服务端工作台摘要，不使用演示数据。 -->
    <section class="next-step-card" aria-label="角色下一步办理事项">
      <div class="next-step-heading">
        <div>
          <h3>角色下一步</h3>
          <p v-if="workbenchData?.角色名称">当前角色：{{ workbenchData.角色名称 }}</p>
          <p v-else>未读取到角色范围</p>
        </div>
        <el-tag v-if="workbenchData?.下一步办理角色" size="small" type="info">
          {{ workbenchData.下一步办理角色 }}
        </el-tag>
      </div>
      <div v-if="workbenchData?.下一步流程名称" class="next-step-body">
        <strong>{{ workbenchData.下一步流程名称 }}</strong>
        <span>{{ workbenchData.下一步办理人 || '按流程规则办理' }}</span>
        <span class="next-step-hint">{{ workbenchData.办理提示 || workbenchData.下一步状态说明 || '请打开事项查看办理要求' }}</span>
        <el-button size="small" type="primary" plain @click="openNextStep">打开事项</el-button>
      </div>
      <div v-else class="next-step-empty">暂无可执行的角色待办事项</div>
    </section>

    <!-- 伦理审查移动摘要：只显示服务端返回的当前账号可见数据。 -->
    <section class="section-card ethics-mobile-card" aria-label="科研伦理审查移动摘要">
      <div class="card-header">
        <div>
          <h3 class="section-title">伦理审查移动摘要</h3>
          <p class="section-caption">初审、修正案、持续审查和合规提醒</p>
        </div>
        <div class="ethics-header-actions">
          <el-tag size="small" type="info">{{ ethicsAsOf || '未同步' }}</el-tag>
          <el-button link type="primary" size="small" :loading="ethicsLoading" @click="loadEthicsData">
            刷新
          </el-button>
        </div>
      </div>

      <el-alert
        v-if="ethicsError"
        type="warning"
        :closable="false"
        show-icon
        :title="ethicsError"
      />
      <el-alert
        v-else-if="ethicsDataNotice"
        type="info"
        :closable="false"
        :title="ethicsDataNotice"
      />
      <div v-else-if="ethicsLoading" class="loading-placeholder">
        <el-skeleton :rows="2" animated />
      </div>
      <template v-else-if="ethicsLoaded">
        <div class="ethics-kpi-grid">
          <button class="ethics-kpi" type="button" @click="router.push('/ethics/review-workflow')">
            <strong>{{ ethicsSummary.initialReview }}</strong>
            <span>初审待办</span>
          </button>
          <button class="ethics-kpi" type="button" @click="router.push('/ethics/review-workflow')">
            <strong>{{ ethicsSummary.amendments }}</strong>
            <span>修正案</span>
          </button>
          <button class="ethics-kpi" type="button" @click="router.push('/ethics/review-workflow')">
            <strong>{{ ethicsSummary.followups }}</strong>
            <span>持续审查</span>
          </button>
          <button class="ethics-kpi danger" type="button" @click="router.push('/ethics/review-workflow')">
            <strong>{{ ethicsSummary.seriousEvents }}</strong>
            <span>严重不良事件</span>
          </button>
        </div>
        <div class="ethics-alert-list">
          <button
            class="ethics-alert-item"
            type="button"
            @click="router.push('/ethics/review-workflow')"
          >
            <span>批件/持续审查逾期提醒</span>
            <el-tag size="small" :type="ethicsSummary.expiryReminders > 0 ? 'danger' : 'success'">
              {{ ethicsSummary.expiryReminders }} 项
            </el-tag>
          </button>
          <button
            class="ethics-alert-item"
            type="button"
            @click="router.push('/ethics/review-workflow')"
          >
            <span>委员回避/匿名投票意见</span>
            <el-tag size="small" type="warning">{{ ethicsSummary.reviewerActions }} 项</el-tag>
          </button>
          <button
            class="ethics-alert-item"
            type="button"
            @click="router.push('/ethics/review-workflow')"
          >
            <span>决定书归档待核验</span>
            <el-tag size="small" :type="ethicsSummary.archivePending > 0 ? 'warning' : 'success'">
              {{ ethicsSummary.archivePending }} 项
            </el-tag>
          </button>
        </div>
        <p v-if="ethicsSummary.visibleApplications === 0" class="empty-placeholder">当前账号暂无可见伦理申请</p>
        <p class="ethics-scope-hint">数据范围由服务端按当前账号权限计算，移动端不扩大可见范围。</p>
      </template>
      <div v-else class="empty-placeholder">尚未同步伦理审查数据</div>
    </section>

    <!-- 企业微信/钉钉移动通道状态栏 -->
    <div class="channel-bar">
      <div class="channel-info">
        <el-icon class="channel-icon"><ChatDotSquare /></el-icon>
        <span class="channel-text">移动协同通道：{{ activeChannelName || '未配置真实通道' }}</span>
      </div>
      <el-button size="small" type="primary" plain @click="openChannelDrawer">
        通道管理与测试
      </el-button>
    </div>

    <!-- 快捷入口宫格 -->
    <section class="section-card">
      <h3 class="section-title">高频科研快捷入口</h3>
      <div class="shortcut-grid">
        <div class="shortcut-item" @click="router.push('/project/vertical-application')">
          <el-icon class="icon-wrap color-blue"><DocumentAdd /></el-icon>
          <span>申报纵向</span>
        </div>
        <div class="shortcut-item" @click="router.push('/project/horizontal-contract')">
          <el-icon class="icon-wrap color-green"><Briefcase /></el-icon>
          <span>横向合同</span>
        </div>
        <div class="shortcut-item" @click="router.push('/finance/expense-claims')">
          <el-icon class="icon-wrap color-orange"><Money /></el-icon>
          <span>经费报销</span>
        </div>
        <div class="shortcut-item" @click="router.push('/fruit/paper-claim')">
          <el-icon class="icon-wrap color-purple"><DocumentChecked /></el-icon>
          <span>论文认领</span>
        </div>
        <div class="shortcut-item" @click="router.push('/office/biobank-ledger')">
          <el-icon class="icon-wrap color-teal"><Box /></el-icon>
          <span>样本资产</span>
        </div>
        <div class="shortcut-item" @click="router.push('/evaluation/performance-evaluation')">
          <el-icon class="icon-wrap color-indigo"><Trophy /></el-icon>
          <span>绩效考核</span>
        </div>
      </div>
    </section>

    <!-- 下一步办理与待办流程 -->
    <section class="section-card">
      <div class="card-header">
        <h3 class="section-title">我的待办科研事项</h3>
        <el-button link type="primary" size="small" @click="router.push('/workflow/todo-approvals')">
          查看全部
        </el-button>
      </div>

      <div v-if="loading" class="loading-placeholder">
        <el-skeleton :rows="3" animated />
      </div>
      <div v-else-if="todoList.length" class="item-list">
        <div
          v-for="item in todoList.slice(0, 5)"
          :key="item.编号"
          class="todo-item"
        >
          <div class="item-left" @click="router.push('/workflow/todo-approvals')">
            <span class="todo-title">{{ item.流程名称 || item.项目名称 || item.步骤名称 || '服务端未返回事项名称' }}</span>
            <span class="todo-sub">
              {{ item.步骤名称 || '当前步骤未返回' }}
              &nbsp;·&nbsp;
              责任人：{{ item.步骤操作人姓名 || item.步骤操作人部门名称 || '服务端未返回责任人' }}
            </span>
          </div>
          <div class="item-actions">
            <el-button
              size="small"
              type="primary"
              plain
              :disabled="!hasVerifiedChannel"
              :title="hasVerifiedChannel ? '排队到已验证的真实移动通道' : '尚未配置并验证真实移动通道'"
              @click="handlePushItem(item)"
            >
              {{ hasVerifiedChannel ? '排队推送' : '未配置通道' }}
            </el-button>
            <el-tag size="small" type="warning" style="margin-left: 6px;">
              {{ item.是否可处理 ? '当前账号可办理' : '按服务端权限办理' }}
            </el-tag>
          </div>
        </div>
      </div>
      <div v-else class="empty-placeholder">暂无待办事项</div>
    </section>

    <!-- 待认领成果提醒 -->
    <section class="section-card">
      <div class="card-header">
        <h3 class="section-title">智能匹配待认领论文</h3>
        <el-button link type="primary" size="small" @click="router.push('/fruit/paper-claim')">
          去认领
        </el-button>
      </div>

      <div v-if="claimablePapers.length" class="item-list">
        <div
          v-for="paper in claimablePapers.slice(0, 3)"
          :key="paper.编号"
          class="todo-item"
        >
          <div class="item-left">
            <span class="todo-title">{{ paper.中文名称 }}</span>
            <span class="todo-sub">
              {{ paper.刊物名称 || '期刊' }} &nbsp;·&nbsp; 第一作者: {{ paper.第一作者 || '-' }}
            </span>
          </div>
          <el-button size="small" type="success" plain @click="router.push('/fruit/paper-claim')">
            认领
          </el-button>
        </div>
      </div>
      <div v-else class="empty-placeholder">暂无待认领论文成果</div>
    </section>

    <!-- 移动待办协同推送流水 -->
    <section class="section-card">
      <div class="card-header">
        <h3 class="section-title">移动协同推送流水</h3>
        <el-button link type="primary" size="small" @click="fetchPushLogs">
          刷新流水
        </el-button>
      </div>

      <div v-if="pushLogs.length" class="log-list">
        <div v-for="log in pushLogs.slice(0, 4)" :key="log.编号" class="log-item">
          <div class="log-top">
            <el-tag size="small" :type="pushStatusType(log.推送状态)">
              {{ log.推送状态 }}
            </el-tag>
            <el-tag size="small" type="info">
              {{ log.通道类型 || '企微应用' }}
            </el-tag>
            <span class="log-title">{{ log.消息标题 }}</span>
            <span class="log-time">{{ log.推送时间 }}</span>
          </div>
          <p class="log-desc">{{ log.消息摘要 }}</p>
        </div>
      </div>
      <div v-else class="empty-placeholder">暂无移动待办推送流水</div>
    </section>

    <!-- 移动协同通道抽屉 -->
    <el-drawer
      v-model="channelDrawerVisible"
      title="移动协同消息网关与通道测试"
      size="480px"
    >
      <div class="drawer-content">
        <div class="dmz-banner">
          <el-alert
            type="info"
            :closable="false"
            show-icon
            title="DMZ 反向代理安全隔离架构"
            description="本系统严格实施内外网物理与逻辑隔离。外部企业微信/钉钉仅与 DMZ 网关握手，核心科研与患者敏感数据不向公网开放。"
          />
        </div>

        <h4 style="margin: 16px 0 8px 0; font-size: 15px;">已注册移动推送通道</h4>
        <el-table :data="channelList" border size="small" style="width: 100%;">
          <el-table-column prop="通道名称" label="通道名称" min-width="150" show-overflow-tooltip />
          <el-table-column prop="通道类型" label="类型" width="110" />
        <el-table-column prop="配置状态" label="配置状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag size="small" :type="channelStatusType(row)">
                {{ row.配置状态 || (row.是否启用 ? '待验证' : '已停用') }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="真实发送" width="86" align="center">
            <template #default="{ row }">
              <el-tag size="small" :type="row.允许发送 ? 'success' : 'info'">
                {{ row.允许发送 ? '可排队' : '不可用' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="连通性测试" width="110" align="center">
            <template #default="{ row }">
              <el-button
                size="small"
                type="warning"
                plain
                :loading="testingChannelId === row.编号"
                @click="handleTestChannel(row.编号)"
              >
                测试握手
              </el-button>
            </template>
          </el-table-column>
        </el-table>

        <h4 style="margin: 20px 0 8px 0; font-size: 15px;">真实通道发送请求</h4>
        <el-alert
          type="warning"
          :closable="false"
          show-icon
          title="系统不会模拟成功；只有外部适配器返回真实回执后才会显示已发送。"
          style="margin-bottom: 12px;"
        />
        <el-form label-position="top" size="small">
          <el-form-item label="消息标题">
            <el-input v-model="testSendForm.消息标题" placeholder="填写真实待办标题" />
          </el-form-item>
          <el-form-item label="消息摘要">
            <el-input
              v-model="testSendForm.消息摘要"
              type="textarea"
              :rows="2"
              placeholder="填写真实待办摘要（选填）"
            />
          </el-form-item>
          <el-button
            type="primary"
            style="width: 100%;"
            :loading="sendingPush"
            :disabled="!hasVerifiedChannel"
            @click="handleSendCustomPush"
          >
            {{ hasVerifiedChannel ? '提交真实通道发送请求' : '完成真实通道验证后可发送' }}
          </el-button>
        </el-form>
      </div>
    </el-drawer>

    <!-- 底部操作与同步刷新 -->
    <footer class="mobile-footer">
      <el-button :icon="Refresh" @click="loadData">刷新工作台</el-button>
      <el-button type="info" plain @click="router.push('/dashboard')">
        进入完整桌面端
      </el-button>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { resolveModernRoute } from '@/utils/route-resolver'
import { ElMessage } from 'element-plus'
import {
  Refresh,
  DocumentAdd,
  Briefcase,
  Money,
  DocumentChecked,
  Box,
  Trophy,
  ChatDotSquare
} from '@element-plus/icons-vue'
import { getWorkbenchSummary, type WorkbenchSummary } from '@/api/workbench'
import { getTodoList } from '@/api/workflow'
import { getClaimablePapers, type ClaimablePaperItem } from '@/api/fruit'
import {
  getEthicsApplications,
  getEthicsApplicationDetail,
  getEthicsExpiryReminders,
  type EthicsApplication
} from '@/api/ethics'
import { buildEthicsMobileSummary } from '@/utils/ethics-mobile'
import {
  getMobilePushConfigs,
  testMobilePushChannel,
  sendMobilePushNotification,
  getMobilePushLogs,
  type MobilePushConfig,
  type MobilePushLogItem
} from '@/api/mobile-push'

const router = useRouter()
const userStore = useUserStore()

const loading = ref(false)
const isOnline = ref(navigator.onLine)
const workbenchData = ref<WorkbenchSummary | null>(null)
const todoList = ref<any[]>([])
const claimablePapers = ref<ClaimablePaperItem[]>([])
const channelList = ref<MobilePushConfig[]>([])
const pushLogs = ref<MobilePushLogItem[]>([])
const activeChannelName = ref('')
const channelDrawerVisible = ref(false)
const testingChannelId = ref<number | null>(null)
const sendingPush = ref(false)
const ethicsLoading = ref(false)
const ethicsLoaded = ref(false)
const ethicsError = ref('')
const ethicsDataNotice = ref('')
const ethicsAsOf = ref('')
const ethicsApplications = ref<EthicsApplication[]>([])
const ethicsExpiryReminders = ref<{ 批件提醒: any[]; 持续审查提醒: any[] } | null>(null)
const ethicsFollowups = ref<any[]>([])
const ethicsReviewerActions = ref(0)

const ethicsSummary = computed(() => buildEthicsMobileSummary({
  applications: ethicsApplications.value,
  expiryReminders: ethicsExpiryReminders.value,
  followups: ethicsFollowups.value,
  reviewerActions: ethicsReviewerActions.value
}))

const hasVerifiedChannel = computed(() => channelList.value.some((channel) => channel.允许发送 === true))

const testSendForm = ref({
  消息标题: '',
  消息摘要: '',
  业务模块: '',
  业务编号: ''
})

function updateOnlineStatus() {
  isOnline.value = navigator.onLine
}

function formatSyncTime() {
  const now = new Date()
  return `同步于 ${now.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })}`
}

/**
 * 移动端伦理摘要只从受权限过滤的真实接口读取，不使用本地演示数据。
 * 详情接口返回的持续记录和委员意见用于补充严重事件及委员动作计数。
 */
async function loadEthicsData() {
  ethicsLoading.value = true
  ethicsError.value = ''
  ethicsDataNotice.value = ''
  try {
    const [applicationsResult, remindersResult] = await Promise.allSettled([
      getEthicsApplications({ 数量: 20 }),
      getEthicsExpiryReminders(60)
    ])
    if (applicationsResult.status === 'rejected' && remindersResult.status === 'rejected') {
      throw applicationsResult.reason || remindersResult.reason || new Error('伦理审查数据暂时无法读取')
    }
    ethicsApplications.value = applicationsResult.status === 'fulfilled' ? (applicationsResult.value || []) : []
    ethicsExpiryReminders.value = remindersResult.status === 'fulfilled'
      ? (remindersResult.value || { 批件提醒: [], 持续审查提醒: [] })
      : { 批件提醒: [], 持续审查提醒: [] }
    if (applicationsResult.status === 'rejected' || remindersResult.status === 'rejected') {
      ethicsDataNotice.value = '部分伦理摘要暂不可见，服务端已按当前账号权限返回可用数据。'
    }

    const detailResults = await Promise.allSettled(
      ethicsApplications.value.slice(0, 20).map((item) => getEthicsApplicationDetail(item.编号))
    )
    const followups: any[] = []
    let reviewerActions = 0
    detailResults.forEach((result) => {
      if (result.status !== 'fulfilled' || !result.value) return
      followups.push(...(result.value.持续记录 || []))
      reviewerActions += (result.value.委员意见 || []).length
    })
    ethicsFollowups.value = followups
    ethicsReviewerActions.value = reviewerActions
    ethicsAsOf.value = formatSyncTime()
    ethicsLoaded.value = true
  } catch (error: any) {
    ethicsApplications.value = []
    ethicsExpiryReminders.value = { 批件提醒: [], 持续审查提醒: [] }
    ethicsFollowups.value = []
    ethicsReviewerActions.value = 0
    ethicsDataNotice.value = ''
    ethicsError.value = error?.message || '伦理审查数据暂时无法读取，请稍后重试'
    ethicsLoaded.value = false
  } finally {
    ethicsLoading.value = false
  }
}

async function fetchPushChannels() {
  try {
    const list = await getMobilePushConfigs()
    channelList.value = list || []
    const enabled = channelList.value.find((c) => c.允许发送) || channelList.value.find((c) => c.是否启用)
    if (enabled) {
      activeChannelName.value = `${enabled.通道名称}（${enabled.配置状态 || '待验证'}）`
    } else {
      activeChannelName.value = ''
    }
  } catch (err) {
    console.error('获取移动推送通道失败:', err)
    channelList.value = []
    activeChannelName.value = ''
  }
}

async function fetchPushLogs() {
  try {
    const logs = await getMobilePushLogs({ limit: 10 })
    pushLogs.value = logs || []
  } catch (err) {
    console.error('获取推送流水失败:', err)
    pushLogs.value = []
  }
}

async function openChannelDrawer() {
  channelDrawerVisible.value = true
  await Promise.all([fetchPushChannels(), fetchPushLogs()])
}

async function handleTestChannel(id?: number) {
  if (!id) return
  testingChannelId.value = id
  try {
    const res = await testMobilePushChannel(id)
    if (res.成功) {
      ElMessage.success(res.提示消息 || '通道连通性测试通过')
    } else {
      ElMessage.warning(res.提示消息 || '测试未通过')
    }
    await fetchPushLogs()
  } catch (err: any) {
    ElMessage.error(err.message || '通道握手异常')
  } finally {
    testingChannelId.value = null
  }
}

async function handlePushItem(item: any) {
  if (!hasVerifiedChannel.value) {
    ElMessage.warning('当前没有完成真实验证的移动通道，未提交推送请求')
    return
  }
  const workflowId = item.流程编号 ?? item.项目编号
  const workflowTitle = item.流程名称 || item.项目名称 || item.步骤名称
  if (!workflowId || !workflowTitle) {
    ElMessage.warning('待办缺少真实流程编号或名称，未提交推送请求')
    return
  }
  try {
    const res = await sendMobilePushNotification({
      消息类型: '待办审批',
      业务模块: '工作流待办',
      业务编号: String(workflowId),
      消息标题: `【待办提醒】${workflowTitle}`,
      消息摘要: `步骤：${item.步骤名称 || workflowTitle}；责任人：${item.步骤操作人姓名 || item.步骤操作人部门名称 || '服务端未返回责任人'}。请在移动端查阅处理。`,
      卡片链接: '/workflow/todo-approvals'
    })
    if (res.是否已真实发送) ElMessage.success(res.提示消息 || '真实移动通道已发送')
    else ElMessage.warning(res.提示消息 || `请求状态：${res.推送状态 || '未发送'}`)
    await fetchPushLogs()
  } catch (err: any) {
    ElMessage.error(err.message || '推送失败')
  }
}

async function handleSendCustomPush() {
  if (!testSendForm.value.消息标题) {
    ElMessage.warning('请输入标题')
    return
  }
  if (!hasVerifiedChannel.value) {
    ElMessage.warning('当前没有完成真实验证的移动通道，未提交推送请求')
    return
  }
  sendingPush.value = true
  try {
    const res = await sendMobilePushNotification({
      消息类型: '移动提醒',
      业务模块: testSendForm.value.业务模块 || undefined,
      业务编号: testSendForm.value.业务编号 || undefined,
      消息标题: testSendForm.value.消息标题,
      消息摘要: testSendForm.value.消息摘要,
      卡片链接: '/office/mobile-workbench'
    })
    if (res.是否已真实发送) ElMessage.success(res.提示消息 || '真实移动通道已发送')
    else ElMessage.warning(res.提示消息 || `请求状态：${res.推送状态 || '未发送'}`)
    await fetchPushLogs()
  } catch (err: any) {
    ElMessage.error(err.message || '推送失败')
  } finally {
    sendingPush.value = false
  }
}

function openNextStep() {
  const rawPath = workbenchData.value?.最近待办路径 || '/workflow/todo-approvals'
  router.push(resolveModernRoute(rawPath) || rawPath)
}

function channelStatusType(channel: MobilePushConfig) {
  if (channel.允许发送) return 'success'
  if (channel.配置状态 === '验证失败' || channel.配置状态 === '已停用') return 'danger'
  return 'warning'
}

function pushStatusType(status: MobilePushLogItem['推送状态']) {
  if (status === '已发送') return 'success'
  if (status === '发送失败' || status === '未配置') return 'danger'
  if (status === '通道未验证') return 'warning'
  return 'info'
}

async function loadData() {
  loading.value = true
  try {
    const [summaryRes, todoRes, papersRes] = await Promise.all([
      getWorkbenchSummary().catch(() => null),
      getTodoList({ index: 1, size: 5 }).catch(() => ({ total: 0, list: [] })),
      getClaimablePapers({ index: 1, size: 5 }).catch(() => ({ total: 0, list: [] })),
      fetchPushChannels().catch(() => null),
      fetchPushLogs().catch(() => null),
      loadEthicsData().catch(() => null)
    ])
    workbenchData.value = summaryRes
    todoList.value = todoRes?.list || []
    claimablePapers.value = papersRes?.list || []
  } catch (error) {
    console.error('移动科研工作台数据同步失败:', error)
    workbenchData.value = null
    todoList.value = []
    claimablePapers.value = []
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  window.addEventListener('online', updateOnlineStatus)
  window.addEventListener('offline', updateOnlineStatus)
  loadData()
})

onUnmounted(() => {
  window.removeEventListener('online', updateOnlineStatus)
  window.removeEventListener('offline', updateOnlineStatus)
})
</script>

<style scoped lang="scss">
.mobile-container {
  max-width: 680px;
  margin: 0 auto;
  padding: 16px;
  background-color: #f8fafc;
  min-height: calc(100vh - 84px);
}

.mobile-header {
  background: linear-gradient(135deg, #1e40af 0%, #3b82f6 100%);
  color: #ffffff;
  border-radius: 12px;
  padding: 20px;
  box-shadow: 0 4px 12px rgba(37, 99, 235, 0.15);

  .header-top {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;

    .workbench-title {
      font-size: 20px;
      font-weight: 700;
      margin: 0 0 6px 0;
    }

    .user-greeting {
      font-size: 13px;
      opacity: 0.9;

      .user-dept {
        margin-left: 8px;
        opacity: 0.8;
      }
    }
  }

  .header-badges {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 6px;
  }

  .dmz-badge {
    display: flex;
    align-items: center;
    background: rgba(16, 185, 129, 0.25);
    border: 1px solid rgba(16, 185, 129, 0.4);
    padding: 2px 8px;
    border-radius: 12px;
    font-size: 11px;
    font-weight: 600;

    .dmz-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background-color: #34d399;
      margin-right: 6px;
    }
  }

  .network-badge {
    display: flex;
    align-items: center;
    background: rgba(255, 255, 255, 0.2);
    padding: 3px 8px;
    border-radius: 20px;
    font-size: 11px;

    .dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background-color: #4ade80;
      margin-right: 6px;
    }

    &.offline {
      background: rgba(239, 68, 68, 0.3);
      .dot {
        background-color: #f87171;
      }
    }
  }

  .kpi-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 12px;
    margin-top: 20px;

    .kpi-box {
      background: rgba(255, 255, 255, 0.12);
      border-radius: 8px;
      padding: 12px 8px;
      text-align: center;
      cursor: pointer;
      transition: all 0.2s;

      &:hover {
        background: rgba(255, 255, 255, 0.2);
      }

      .kpi-num {
        display: block;
        font-size: 22px;
        font-weight: 700;
        margin-bottom: 4px;
      }

      .kpi-title {
        font-size: 12px;
        opacity: 0.85;
      }
    }
  }
}

.channel-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #ffffff;
  border-radius: 8px;
  padding: 10px 14px;
  margin-top: 12px;
  border: 1px solid #e2e8f0;

  .channel-info {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 13px;
    color: #334155;

    .channel-icon {
      font-size: 16px;
      color: #3b82f6;
    }

    .channel-text {
      font-weight: 600;
    }
  }
}

.missing-alert {
  margin-top: 12px;
}

.next-step-card {
  background: #eff6ff;
  border: 1px solid #bfdbfe;
  border-radius: 10px;
  padding: 14px 16px;
  margin-top: 12px;

  .next-step-heading {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;

    h3 {
      margin: 0;
      color: #1e3a8a;
      font-size: 15px;
    }

    p {
      margin: 5px 0 0;
      color: #475569;
      font-size: 12px;
    }
  }

  .next-step-body {
    display: flex;
    flex-direction: column;
    gap: 5px;
    margin-top: 12px;
    color: #334155;
    font-size: 13px;

    strong { color: #0f172a; }
    .next-step-hint { color: #64748b; font-size: 12px; }
    .el-button { align-self: flex-start; margin-top: 4px; }
  }

  .next-step-empty {
    margin-top: 10px;
    color: #64748b;
    font-size: 13px;
  }
}

.section-card {
  background: #ffffff;
  border-radius: 10px;
  padding: 16px;
  margin-top: 16px;
  border: 1px solid #e2e8f0;

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 12px;
  }

  .section-title {
    font-size: 15px;
    font-weight: 700;
    color: #1e293b;
    margin: 0;
  }

  .section-caption {
    margin: 4px 0 0;
    color: #64748b;
    font-size: 12px;
  }
}

.ethics-mobile-card {
  .ethics-header-actions {
    display: flex;
    align-items: center;
    gap: 4px;
  }

  .ethics-kpi-grid {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 8px;
    margin-top: 12px;
  }

  .ethics-kpi {
    border: 1px solid #dbeafe;
    border-radius: 8px;
    padding: 10px 4px;
    background: #eff6ff;
    color: #1e3a8a;
    cursor: pointer;
    text-align: center;

    strong,
    span {
      display: block;
    }

    strong {
      font-size: 20px;
      line-height: 1.2;
    }

    span {
      margin-top: 4px;
      font-size: 11px;
      color: #475569;
      white-space: nowrap;
    }

    &.danger {
      border-color: #fecaca;
      background: #fff1f2;
      color: #be123c;
    }
  }

  .ethics-alert-list {
    display: flex;
    flex-direction: column;
    gap: 6px;
    margin-top: 12px;
  }

  .ethics-alert-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    padding: 8px 10px;
    border: 1px solid #e2e8f0;
    border-radius: 7px;
    background: #f8fafc;
    color: #334155;
    font-size: 12px;
    text-align: left;
    cursor: pointer;
  }

  .ethics-scope-hint {
    margin: 10px 0 0;
    color: #94a3b8;
    font-size: 11px;
    line-height: 1.5;
  }
}

.shortcut-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
  margin-top: 12px;

  .shortcut-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 12px 6px;
    border-radius: 8px;
    background: #f8fafc;
    cursor: pointer;
    transition: all 0.2s;

    &:hover {
      background: #f1f5f9;
      transform: translateY(-1px);
    }

    .icon-wrap {
      font-size: 24px;
      margin-bottom: 6px;

      &.color-blue { color: #3b82f6; }
      &.color-green { color: #10b981; }
      &.color-orange { color: #f59e0b; }
      &.color-purple { color: #8b5cf6; }
      &.color-teal { color: #0d9488; }
      &.color-indigo { color: #6366f1; }
    }

    span {
      font-size: 12px;
      color: #475569;
      font-weight: 500;
    }
  }
}

.item-list {
  display: flex;
  flex-direction: column;
  gap: 10px;

  .todo-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 12px;
    background: #f8fafc;
    border-radius: 8px;
    border: 1px solid #f1f5f9;
    cursor: pointer;

    &:hover {
      background: #f1f5f9;
    }

    .item-left {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 4px;
      margin-right: 8px;

      .todo-title {
        font-size: 14px;
        font-weight: 600;
        color: #1e293b;
      }

      .todo-sub {
        font-size: 12px;
        color: #64748b;
      }
    }

    .item-actions {
      display: flex;
      align-items: center;
    }
  }
}

.log-list {
  display: flex;
  flex-direction: column;
  gap: 8px;

  .log-item {
    padding: 10px;
    background: #f8fafc;
    border-radius: 6px;
    border: 1px solid #f1f5f9;

    .log-top {
      display: flex;
      align-items: center;
      gap: 6px;
      margin-bottom: 4px;

      .log-title {
        font-size: 13px;
        font-weight: 600;
        color: #1e293b;
        flex: 1;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .log-time {
        font-size: 11px;
        color: #94a3b8;
      }
    }

    .log-desc {
      font-size: 12px;
      color: #64748b;
      margin: 0;
      line-height: 1.4;
    }
  }
}

.empty-placeholder {
  padding: 24px;
  text-align: center;
  color: #94a3b8;
  font-size: 13px;
}

.mobile-footer {
  display: flex;
  justify-content: space-between;
  margin-top: 24px;
  padding-bottom: 24px;

  .el-button {
    flex: 1;
    margin: 0 6px;
  }
}

.drawer-content {
  padding: 8px 0;
}

@media (max-width: 480px) {
  .mobile-container {
    padding: 10px;
  }

  .mobile-header {
    padding: 16px;

    .header-top {
      flex-direction: column;
      gap: 10px;
    }

    .header-badges {
      width: 100%;
      flex-direction: row;
      align-items: center;
      justify-content: flex-start;
      flex-wrap: wrap;
    }

    .kpi-grid {
      gap: 6px;

      .kpi-box {
        padding: 10px 4px;

        .kpi-num {
          font-size: 20px;
        }
      }
    }
  }

  .channel-bar {
    align-items: flex-start;
    flex-direction: column;
    gap: 8px;

    .channel-info {
      width: 100%;
    }

    .el-button {
      width: 100%;
    }
  }

  .section-card {
    padding: 12px;
  }

  .shortcut-grid {
    gap: 6px;

    .shortcut-item {
      padding: 10px 3px;
    }
  }

  .ethics-mobile-card {
    .card-header {
      align-items: flex-start;
    }

    .ethics-header-actions {
      flex-direction: column;
      align-items: flex-end;
    }

    .ethics-kpi-grid {
      grid-template-columns: repeat(2, minmax(0, 1fr));
    }
  }

  .item-list .todo-item {
    align-items: flex-start;
    flex-direction: column;
    gap: 8px;

    .item-left {
      width: 100%;
    }

    .item-actions {
      width: 100%;
      justify-content: flex-end;
    }
  }

  .mobile-footer {
    flex-direction: column;
    gap: 8px;

    .el-button {
      margin: 0;
    }
  }
}
</style>
