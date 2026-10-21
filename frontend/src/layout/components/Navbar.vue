<template>
  <div class="navbar">
    <div class="left-panel">
      <el-icon class="hamburger" :size="20" @click="$emit('toggle-collapse')">
        <Fold v-if="!isCollapse" />
        <Expand v-else />
      </el-icon>

      <el-breadcrumb separator="/" class="breadcrumb">
        <el-breadcrumb-item :to="{ path: '/dashboard' }">首页</el-breadcrumb-item>
        <el-breadcrumb-item v-if="currentTitle && currentTitle !== '科研工作台'">
          {{ currentTitle }}
        </el-breadcrumb-item>
      </el-breadcrumb>
    </div>

    <div class="right-panel">
      <!-- 全景功能大纲地图快捷入口 -->
      <div class="nav-action-btn map-trigger" title="科研管理全景功能大纲地图" @click="megaMapVisible = true">
        <el-icon :size="16"><Grid /></el-icon>
        <span class="search-tip">功能全景</span>
      </div>

      <!-- 全局快捷搜索 / 命令面板按钮 -->
      <div class="nav-action-btn search-trigger" @click="openSearchDialog">
        <el-icon :size="16"><Search /></el-icon>
        <span class="search-tip">全局搜索</span>
        <span class="shortcut-badge">⌘K</span>
      </div>

      <!-- 待办与消息通知微标 -->
      <el-popover
        placement="bottom-end"
        :width="360"
        trigger="click"
        popper-class="notice-popover"
      >
        <template #reference>
          <div class="nav-action-btn badge-btn">
            <el-badge :value="pendingCount" :max="99" :hidden="pendingCount === 0">
              <el-icon :size="18"><Bell /></el-icon>
            </el-badge>
          </div>
        </template>
        <div class="notice-container">
          <div class="notice-header">
            <span class="title">科研待办与通知提醒</span>
            <el-tag size="small" type="danger" v-if="pendingCount > 0">{{ pendingCount }} 条待办</el-tag>
          </div>
          <el-scrollbar max-height="260px">
            <div v-if="pendingTasks.length === 0" class="empty-notice">
              暂无待处理的科研审批任务
            </div>
            <div
              v-for="task in pendingTasks"
              :key="task.流程编号"
              class="notice-item"
              @click="jumpToTask"
            >
              <div class="item-title">{{ task.流程名称 }}</div>
              <div class="item-meta">
                <el-tag size="small" type="warning">{{ task.步骤名称 }}</el-tag>
                <span class="applicant">{{ task.发起人姓名 || '申请人' }}</span>
              </div>
            </div>
          </el-scrollbar>
          <div class="notice-footer">
            <el-button type="primary" link size="small" @click="jumpToTask">
              前往科研待办审核工作台 &rarr;
            </el-button>
          </div>
        </div>
      </el-popover>

      <!-- 全屏切换 -->
      <div class="nav-action-btn" title="全屏切换" @click="toggleFullscreen">
        <el-icon :size="18"><FullScreen /></el-icon>
      </div>

      <!-- 租户/数据库标识 -->
      <el-tag size="small" type="info" class="db-tag">
        {{ userStore.userInfo?.DbKey || '科研主库' }}
      </el-tag>

      <!-- 用户信息下拉菜单 -->
      <el-dropdown trigger="click" @command="handleCommand">
        <div class="user-profile">
          <el-avatar :size="32" class="avatar">
            {{ userStore.userInfo?.姓名?.substring(0, 1) || '用' }}
          </el-avatar>
          <div class="user-meta">
            <span class="user-name">{{ userStore.userInfo?.姓名 || '科研用户' }}</span>
            <span class="user-dept">{{ userStore.userInfo?.部门名称 || '科研人员' }}</span>
          </div>
          <el-icon class="el-icon--right"><CaretBottom /></el-icon>
        </div>

        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="profile">
              <el-icon><User /></el-icon>个人科研档案 (工号: {{ userStore.userInfo?.工号 }})
            </el-dropdown-item>
            <el-dropdown-item command="proxy">
              <el-icon><Key /></el-icon>权限代理与预算对账
            </el-dropdown-item>
            <el-dropdown-item command="governance">
              <el-icon><Odometer /></el-icon>外部系统同步监控
            </el-dropdown-item>
            <el-dropdown-item command="security">
              <el-icon><Lock /></el-icon>安全设置与密码
            </el-dropdown-item>
            <el-dropdown-item divided command="logout">
              <el-icon><SwitchButton /></el-icon>安全退出
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>

    <!-- 全局科研命令与模块快速跳转面板 (Ctrl+K / Cmd+K) -->
    <el-dialog
      v-model="searchDialogVisible"
      title="科研业务全局快速检索与导航"
      width="640px"
      class="global-search-dialog"
      destroy-on-close
      append-to-body
    >
      <div class="search-dialog-body">
        <el-input
          ref="searchInputRef"
          v-model="searchQuery"
          placeholder="输入模块名称、首字母或业务关键字（如：纵向、经费、论文、待办）..."
          prefix-icon="Search"
          clearable
          size="large"
          class="search-main-input"
        />

        <div class="quick-nav-list">
          <div
            v-for="group in filteredGroups"
            :key="group.category"
            class="nav-group"
          >
            <div class="group-title">{{ group.category }}</div>
            <div class="group-items">
              <div
                v-for="item in group.items"
                :key="item.path"
                class="nav-item"
                @click="handleNavigate(item.path)"
              >
                <div class="item-left">
                  <el-icon class="item-icon"><component :is="item.icon" /></el-icon>
                  <span class="item-name">{{ item.name }}</span>
                </div>
                <span class="item-path">{{ item.path }}</span>
              </div>
            </div>
          </div>
          <div v-if="filteredGroups.length === 0" class="empty-search">
            未检索到匹配的科研业务模块，换个关键字试试
          </div>
        </div>
      </div>
    </el-dialog>

    <!-- 全景功能大纲地图抽屉 -->
    <MegaMapDrawer
      v-model="megaMapVisible"
      :favorites="favoritePaths"
      @toggle-favorite="onToggleFavorite"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useFavoritesStore } from '@/stores/favorites'
import MegaMapDrawer from './MegaMapDrawer.vue'
import { getTodoList, type WorkflowTaskItem } from '@/api/workflow'
import { ElMessageBox } from 'element-plus'
import {
  Fold,
  Expand,
  CaretBottom,
  User,
  Key,
  Odometer,
  SwitchButton,
  Search,
  Bell,
  FullScreen,
  Lock,
  TrendCharts,
  Collection,
  Histogram,
  Reading,
  Money,
  Files,
  Medal,
  Download,
  Setting,
  House,
  Document,
  Grid
} from '@element-plus/icons-vue'

defineProps<{
  isCollapse: boolean
}>()

defineEmits<{
  (e: 'toggle-collapse'): void
}>()

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const favoritesStore = useFavoritesStore()

const megaMapVisible = ref(false)
const favoritePaths = computed(() => favoritesStore.favorites.map((f) => f.path))

function onToggleFavorite(item: any) {
  favoritesStore.toggleFavorite({ name: item.name, path: item.path })
}

const currentTitle = computed(() => {
  return (route.query.title as string) || (route.meta?.title as string) || ''
})

// 待办通知
const pendingCount = ref(0)
const pendingTasks = ref<WorkflowTaskItem[]>([])

async function fetchPendingTasks() {
  try {
    const res = await getTodoList({ index: 1, size: 5 })
    if (res && Array.isArray(res.list)) {
      pendingTasks.value = res.list
      pendingCount.value = res.total || res.list.length
    }
  } catch {
    pendingTasks.value = []
    pendingCount.value = 0
  }
}

function jumpToTask() {
  router.push('/workflow/todo-approvals')
}

// 全屏切换
function toggleFullscreen() {
  if (!document.fullscreenElement) {
    document.documentElement.requestFullscreen().catch(() => {})
  } else {
    document.exitFullscreen().catch(() => {})
  }
}

// 全局快捷搜索面板
const searchDialogVisible = ref(false)
const searchQuery = ref('')
const searchInputRef = ref()

interface NavModule {
  name: string
  path: string
  icon: any
  keywords: string
}

interface NavGroup {
  category: string
  items: NavModule[]
}

const moduleCatalog: NavGroup[] = [
  {
    category: '科研项目申报与立项',
    items: [
      { name: '纵向项目申报向导', path: '/project/vertical-application', icon: TrendCharts, keywords: '纵向 申报 立项 基金' },
      { name: '横向项目与产学研合同', path: '/project/horizontal-contract', icon: Collection, keywords: '横向 合同 企业 委托' },
      { name: '纵向课题全生命周期治理', path: '/project/vertical-lifecycle', icon: Files, keywords: '纵向 中检 变更 结题 验收' }
    ]
  },
  {
    category: '经费核算与财务治理',
    items: [
      { name: '项目经费分层台账', path: '/finance/ledger', icon: Histogram, keywords: '经费 台账 分层 预算' },
      { name: '经费到账管理与认领', path: '/finance/incoming', icon: Money, keywords: '到账 认领 拨款 支票' },
      { name: '经费报销与票据核销台账', path: '/finance/expense-claims', icon: Money, keywords: '报销 发票 公务卡 核销' }
    ]
  },
  {
    category: '学术成果与知识产权',
    items: [
      { name: '论文成果认领中心', path: '/fruit/paper-claim', icon: Reading, keywords: '论文 认领 成果 作者' },
      { name: '学术论文全周期管理', path: '/fruit/paper-management', icon: Document, keywords: '论文 SCI 期刊 crossref pubmed' },
      { name: '成果综合业务台账', path: '/fruit/achievement-ledger', icon: Reading, keywords: '专利 著作 奖励 成果 转化' },
      { name: '我的科研全景档案', path: '/office/research-portfolio', icon: User, keywords: '档案 个人 资产 名片' }
    ]
  },
  {
    category: '学术活动与人才队伍',
    items: [
      { name: '学术活动与讲座会议', path: '/activities/academic-activities', icon: Reading, keywords: '讲座 会议 反馈 专家' },
      { name: '学术任职与专家人才中心', path: '/organization/academic-appointments', icon: Medal, keywords: '任职 专家 遴选 人才' },
      { name: '部门人员信息维护', path: '/organization/people', icon: User, keywords: '组织 部门 人员 员工 架构' }
    ]
  },
  {
    category: '评审考核与行政办公',
    items: [
      { name: '专家匿名评审工作台', path: '/evaluation/expert-workbench', icon: Medal, keywords: '专家 评审 盲审 回避' },
      { name: '科研待办流转审核', path: '/workflow/todo-approvals', icon: Files, keywords: '待办 审核 流程 批注' },
      { name: '科研绩效评价与奖励兑现', path: '/evaluation/performance-evaluation', icon: Medal, keywords: '绩效 评价 积分 奖励' },
      { name: '科研统计决策分析大屏', path: '/analytics/statistics', icon: Histogram, keywords: '统计 分析 大屏 报表' },
      { name: '移动科研工作台', path: '/office/mobile-workbench', icon: House, keywords: '移动 手机 工作台' },
      { name: '科研通知公告', path: '/office/notice', icon: Bell, keywords: '通知 公告 新闻' },
      { name: '科研制度与资料下载中心', path: '/office/downloads', icon: Download, keywords: '下载 制度 模板 规范' },
      { name: '权限代理与预算对账', path: '/governance/proxy-reconciliation', icon: Key, keywords: '代理 授权 对账 预算' },
      { name: '外部系统同步监控', path: '/operations/external-sync', icon: Setting, keywords: '外部 接口 同步 hr 财务' },
      { name: '科研基础字典规范', path: '/base-info/dictionary', icon: Files, keywords: '字典 编码 规范' },
      { name: '科研基础信息标准配置', path: '/base-info/system-configs', icon: Setting, keywords: '分类 模版 期刊 行业 学科' },
      { name: '角色权限管理', path: '/system/roles', icon: Lock, keywords: '角色 权限 字段 授权' },
      { name: '等保三级登录审计日志', path: '/system/login-logs', icon: Document, keywords: '日志 登录 审计 安全' }
    ]
  }
]

const filteredGroups = computed(() => {
  const q = searchQuery.value.trim().toLowerCase()
  if (!q) return moduleCatalog
  return moduleCatalog
    .map(g => ({
      category: g.category,
      items: g.items.filter(i =>
        i.name.toLowerCase().includes(q) ||
        i.path.toLowerCase().includes(q) ||
        i.keywords.toLowerCase().includes(q)
      )
    }))
    .filter(g => g.items.length > 0)
})

function openSearchDialog() {
  searchQuery.value = ''
  searchDialogVisible.value = true
  nextTick(() => {
    searchInputRef.value?.focus()
  })
}

function handleNavigate(path: string) {
  searchDialogVisible.value = false
  router.push(path)
}

function handleKeydown(e: KeyboardEvent) {
  if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
    e.preventDefault()
    openSearchDialog()
  }
}

async function handleCommand(command: string) {
  if (command === 'logout') {
    try {
      await ElMessageBox.confirm('确定要退出当前系统吗？', '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      })
      const keycloakLogoutUrl = await userStore.logout()
      // Complete the OIDC RP-initiated logout in the browser so the Keycloak
      // SSO cookie is removed as well as the local application session.
      if (keycloakLogoutUrl) {
        window.location.assign(keycloakLogoutUrl)
      } else {
        router.replace('/login')
      }
    } catch {
      // canceled
    }
  } else if (command === 'proxy') {
    router.push('/governance/proxy-reconciliation')
  } else if (command === 'governance') {
    router.push('/operations/external-sync')
  } else if (command === 'profile') {
    router.push('/office/profile')
  } else if (command === 'security') {
    router.push('/office/profile?tab=security')
  }
}

onMounted(() => {
  fetchPendingTasks()
  window.addEventListener('keydown', handleKeydown)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeydown)
})
</script>

<style scoped lang="scss">
.navbar {
  height: 50px;
  background-color: #ffffff;
  border-bottom: 1px solid #e6e6e6;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0 16px;
  box-shadow: 0 1px 4px rgba(0, 21, 41, 0.08);
  overflow: hidden;
  gap: 12px;

  .left-panel {
    display: flex;
    align-items: center;
    flex: 1;
    min-width: 0;
    overflow: hidden;

    .hamburger {
      cursor: pointer;
      color: #606266;
      margin-right: 16px;
      flex-shrink: 0;

      &:hover {
        color: #409eff;
      }
    }

    .breadcrumb {
      font-size: 14px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;

      :deep(.el-breadcrumb__item) {
        float: none;
        display: inline-flex;
        align-items: center;
        white-space: nowrap;
      }
    }
  }

  .right-panel {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-shrink: 0;
    white-space: nowrap;

    .nav-action-btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      white-space: nowrap;
      cursor: pointer;
      color: #64748b;
      padding: 6px 8px;
      border-radius: 6px;
      transition: all 0.2s;

      &:hover {
        background-color: #f1f5f9;
        color: #0f172a;
      }

      &.map-trigger {
        background-color: #fef3c7;
        color: #b45309;
        padding: 4px 10px;
        gap: 6px;
        font-size: 12px;
        border: 1px solid #fde68a;

        .search-tip {
          color: #b45309;
          font-weight: 600;
          white-space: nowrap;
        }

        &:hover {
          background-color: #fde68a;
          color: #92400e;
        }
      }

      &.search-trigger {
        background-color: #f1f5f9;
        padding: 4px 10px;
        gap: 6px;
        font-size: 12px;

        .search-tip {
          color: #64748b;
          white-space: nowrap;
        }

        .shortcut-badge {
          background: #e2e8f0;
          padding: 1px 5px;
          border-radius: 4px;
          font-family: monospace;
          font-size: 11px;
          color: #475569;
          white-space: nowrap;
        }

        &:hover {
          background-color: #e2e8f0;
        }
      }

      &.badge-btn {
        padding: 6px;
      }
    }

    .db-tag {
      font-family: monospace;
      flex-shrink: 0;
      white-space: nowrap;
    }

    .user-profile {
      display: flex;
      align-items: center;
      cursor: pointer;
      user-select: none;
      flex-shrink: 0;
      white-space: nowrap;

      .avatar {
        background-color: #409eff;
        color: #ffffff;
        font-weight: 600;
        flex-shrink: 0;
      }

      .user-meta {
        display: flex;
        flex-direction: column;
        margin: 0 8px;
        text-align: left;

        .user-name {
          font-size: 14px;
          font-weight: 500;
          color: #303133;
          line-height: 1.2;
          white-space: nowrap;
        }

        .user-dept {
          font-size: 12px;
          color: #909399;
          line-height: 1.2;
          white-space: nowrap;
        }
      }
    }
  }
}

.notice-container {
  .notice-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding-bottom: 8px;
    border-bottom: 1px solid #f1f5f9;
    font-size: 13px;
    font-weight: 600;
    color: #1e293b;
  }

  .notice-item {
    padding: 8px 6px;
    border-bottom: 1px dashed #f1f5f9;
    cursor: pointer;
    border-radius: 4px;

    &:hover {
      background-color: #f8fafc;
    }

    .item-title {
      font-size: 13px;
      color: #334155;
      font-weight: 500;
      margin-bottom: 4px;
    }

    .item-meta {
      display: flex;
      align-items: center;
      justify-content: space-between;

      .applicant {
        font-size: 12px;
        color: #94a3b8;
      }
    }
  }

  .empty-notice {
    text-align: center;
    color: #94a3b8;
    padding: 24px 0;
    font-size: 12px;
  }

  .notice-footer {
    text-align: center;
    padding-top: 8px;
    border-top: 1px solid #f1f5f9;
  }
}

.search-dialog-body {
  .search-main-input {
    margin-bottom: 16px;
  }

  .quick-nav-list {
    max-height: 380px;
    overflow-y: auto;

    .nav-group {
      margin-bottom: 16px;

      .group-title {
        font-size: 12px;
        font-weight: 600;
        color: #94a3b8;
        margin-bottom: 6px;
        text-transform: uppercase;
      }

      .group-items {
        display: flex;
        flex-direction: column;
        gap: 4px;

        .nav-item {
          display: flex;
          align-items: center;
          justify-content: space-between;
          padding: 8px 12px;
          border-radius: 6px;
          background: #f8fafc;
          cursor: pointer;
          transition: all 0.2s;

          &:hover {
            background: #eff6ff;
            color: #2563eb;

            .item-path {
              color: #3b82f6;
            }
          }

          .item-left {
            display: flex;
            align-items: center;
            gap: 10px;

            .item-icon {
              font-size: 16px;
              color: #409eff;
            }

            .item-name {
              font-size: 13px;
              font-weight: 500;
            }
          }

          .item-path {
            font-size: 11px;
            color: #94a3b8;
            font-family: monospace;
          }
        }
      }
    }

    .empty-search {
      text-align: center;
      color: #94a3b8;
      padding: 32px 0;
      font-size: 13px;
    }
  }
}
</style>
