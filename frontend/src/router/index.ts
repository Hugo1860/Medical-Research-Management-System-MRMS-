import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useTabsStore } from '@/stores/tabs'
import Layout from '@/layout/index.vue'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/login/index.vue'),
    meta: { title: '系统登录' }
  },
  {
    path: '/',
    component: Layout,
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/dashboard/index.vue'),
        meta: { title: '科研工作台', icon: 'House' }
      },
      {
        path: 'project/vertical-application',
        name: 'VerticalApplication',
        component: () => import('@/views/project/vertical-application.vue'),
        meta: { title: '纵向项目申报', icon: 'TrendCharts' }
      },
      {
        path: 'organization/people',
        name: 'OrganizationPeople',
        component: () => import('@/views/organization/people.vue'),
        meta: { title: '部门人员信息维护', icon: 'User' }
      },
      {
        path: 'governance/proxy-reconciliation',
        name: 'ProxyReconciliation',
        component: () => import('@/views/governance/proxy-reconciliation.vue'),
        meta: { title: '权限代理与预算对账', icon: 'Key' }
      },
      {
        path: 'base-info/dictionary',
        name: 'DictionaryManagement',
        component: () => import('@/views/base-info/dictionary.vue'),
        meta: { title: '科研字典规范维护', icon: 'Files' }
      },
      {
        path: 'evaluation/expert-workbench',
        name: 'ExpertWorkbench',
        component: () => import('@/views/evaluation/expert-workbench.vue'),
        meta: { title: '专家评审工作台', icon: 'Medal' }
      },
      {
        path: 'finance/ledger',
        name: 'FinanceLedger',
        component: () => import('@/views/finance/ledger.vue'),
        meta: { title: '项目经费分层台账', icon: 'Histogram' }
      },
      {
        path: 'fruit/paper-claim',
        name: 'PaperClaim',
        component: () => import('@/views/fruit/paper-claim.vue'),
        meta: { title: '论文成果认领', icon: 'Reading' }
      },
      {
        path: 'operations/external-sync',
        name: 'ExternalSync',
        component: () => import('@/views/operations/external-sync.vue'),
        meta: { title: '外部系统同步监控', icon: 'Setting' }
      },
      {
        path: 'analytics/statistics',
        name: 'AnalyticsStatistics',
        component: () => import('@/views/analytics/statistics.vue'),
        meta: { title: '科研统计决策分析', icon: 'Histogram' }
      },
      {
        path: 'project/horizontal-contract',
        name: 'HorizontalContract',
        component: () => import('@/views/project/horizontal-contract.vue'),
        meta: { title: '横向项目与合同', icon: 'Collection' }
      },
      {
        path: 'activities/academic-activities',
        name: 'AcademicActivities',
        component: () => import('@/views/activities/academic-activities.vue'),
        meta: { title: '学术活动与讲座会议', icon: 'Reading' }
      },
      {
        path: 'finance/incoming',
        name: 'IncomingFunds',
        component: () => import('@/views/finance/incoming.vue'),
        meta: { title: '经费到账与认领', icon: 'Money' }
      },
      {
        path: 'office/notice',
        name: 'OfficeNotice',
        component: () => import('@/views/office/notice.vue'),
        meta: { title: '科研办公与通知公告', icon: 'Bell' }
      },
      {
        path: 'system/roles',
        name: 'RoleManagement',
        component: () => import('@/views/system/role-management.vue'),
        meta: { title: '角色权限管理', icon: 'Lock' }
      },
      {
        path: 'system/login-logs',
        name: 'LoginLogs',
        component: () => import('@/views/system/login-log.vue'),
        meta: { title: '系统登录日志', icon: 'DocumentChecked' }
      },
      {
        path: 'system/attachments',
        name: 'AttachmentManagement',
        component: () => import('@/views/system/attachment-management.vue'),
        meta: { title: '附件与对象存储管理', icon: 'FolderOpened' }
      },
      {
        path: 'workflow/todo-approvals',
        name: 'WorkflowApprovals',
        component: () => import('@/views/workflow/todo-approvals.vue'),
        meta: { title: '科研待办与审核', icon: 'Stamp' }
      },
      {
        path: 'project/vertical-lifecycle',
        name: 'VerticalLifecycle',
        component: () => import('@/views/project/vertical-lifecycle.vue'),
        meta: { title: '纵向课题全周期', icon: 'Files' }
      },
      {
        path: 'office/profile',
        name: 'UserProfile',
        component: () => import('@/views/office/profile.vue'),
        meta: { title: '个人资料与安全', icon: 'UserFilled' }
      },
      {
        path: 'organization/academic-appointments',
        name: 'AcademicAppointments',
        component: () => import('@/views/organization/academic-appointments.vue'),
        meta: { title: '学术任职与专家人才', icon: 'UserFilled' }
      },
      {
        path: 'fruit/achievement-ledger',
        name: 'AchievementLedger',
        component: () => import('@/views/fruit/achievement-ledger.vue'),
        meta: { title: '成果综合业务台账', icon: 'Reading' }
      },
      {
        path: 'fruit/achievement-ip-awards',
        name: 'AchievementIpAwards',
        component: () => import('@/views/fruit/achievement-ip-awards.vue'),
        meta: { title: '成果、知识产权与获奖申报', icon: 'Medal' }
      },
      {
        path: 'fruit/paper-management',
        name: 'PaperManagement',
        component: () => import('@/views/fruit/paper-management.vue'),
        meta: { title: '学术论文管理', icon: 'Document' }
      },
      {
        path: 'office/research-portfolio',
        name: 'ResearchPortfolio',
        component: () => import('@/views/office/research-portfolio.vue'),
        meta: { title: '我的科研全景档案', icon: 'DataBoard' }
      },
      {
        path: 'finance/expense-claims',
        name: 'ExpenseClaims',
        component: () => import('@/views/finance/expense-claims.vue'),
        meta: { title: '经费报销与票据核销台账', icon: 'Money' }
      },
      {
        path: 'finance/controls',
        name: 'FinanceControls',
        component: () => import('@/views/finance/controls.vue'),
        meta: { title: '经费控制与年度结算', icon: 'SetUp' }
      },
      {
        path: 'base-info/system-configs',
        name: 'SystemConfigs',
        component: () => import('@/views/base-info/system-configs.vue'),
        meta: { title: '科研基础信息与标准规范', icon: 'Setting' }
      },
      {
        path: 'evaluation/performance-evaluation',
        name: 'PerformanceEvaluation',
        component: () => import('@/views/evaluation/performance-evaluation.vue'),
        meta: { title: '科研绩效评价与奖励', icon: 'Medal' }
      },
      {
        path: 'office/mobile-workbench',
        name: 'MobileWorkbench',
        component: () => import('@/views/office/mobile-workbench.vue'),
        meta: { title: '移动科研工作台', icon: 'Iphone' }
      },
      {
        path: 'office/further-education',
        name: 'FurtherEducation',
        component: () => import('@/views/office/further-education.vue'),
        meta: { title: '人员进修管理', icon: 'School' }
      },
      {
        path: 'office/downloads',
        name: 'OfficeDownloads',
        component: () => import('@/views/office/downloads.vue'),
        meta: { title: '科研制度与下载中心', icon: 'Download' }
      },
      {
        path: 'office/biobank-ledger',
        name: 'BiobankLedger',
        component: () => import('@/views/office/biobank-ledger.vue'),
        meta: { title: '生物样本库与科研借还台账', icon: 'Box' }
      },
      {
        path: 'office/research-platform',
        name: 'ResearchPlatform',
        component: () => import('@/views/office/research-platform.vue'),
        meta: { title: '网上科研服务', icon: 'Service' }
      },
      {
        path: 'office/education-statistics',
        name: 'EducationStatistics',
        component: () => import('@/views/office/education-statistics.vue'),
        meta: { title: '教育部科研统计', icon: 'DataAnalysis' }
      },
      {
        path: 'office/document-sharing',
        name: 'DocumentSharing',
        component: () => import('@/views/office/document-sharing.vue'),
        meta: { title: '科研文档共享与消息中心', icon: 'FolderOpened' }
      },
      {
        path: 'project/project-configuration',
        name: 'ProjectConfiguration',
        component: () => import('@/views/project/project-configuration.vue'),
        meta: { title: '项目分类与预算模板', icon: 'Operation' }
      },
      {
        path: 'project/vertical-import',
        name: 'VerticalProjectImport',
        component: () => import('@/views/project/vertical-import.vue'),
        meta: { title: '纵向项目导入', icon: 'Upload' }
      },
      {
        path: 'finance/fund-execution',
        name: 'FundExecution',
        component: () => import('@/views/finance/fund-execution.vue'),
        meta: { title: '科研经费执行治理', icon: 'Money' }
      },
      {
        path: 'activities/academic-exchange',
        name: 'AcademicExchange',
        component: () => import('@/views/activities/academic-exchange.vue'),
        meta: { title: '学术交流治理', icon: 'Reading' }
      },
      {
        path: 'fruit/journal-partition',
        name: 'JournalPartition',
        component: () => import('@/views/fruit/journal-partition.vue'),
        meta: { title: '期刊分区库与匹配', icon: 'Notebook' }
      },
      {
        path: 'fruit/paper-submission',
        name: 'PaperSubmission',
        component: () => import('@/views/fruit/paper-submission.vue'),
        meta: { title: '论文投稿备案与期刊风控', icon: 'Document' }
      },
      {
        path: 'office/cme-credits',
        name: 'CmeCredits',
        component: () => import('@/views/office/cme-credits.vue'),
        meta: { title: '继续医学教育学分', icon: 'Medal' }
      },
      {
        path: 'office/new-technology',
        name: 'OfficeNewTechnology',
        component: () => import('@/views/office/new-technology.vue'),
        meta: { title: '医疗新技术与适宜技术', icon: 'TrendCharts' }
      },
      {
        path: 'fruit/popular-science',
        name: 'FruitPopularScience',
        component: () => import('@/views/fruit/popular-science.vue'),
        meta: { title: '科普成果与案例库', icon: 'Reading' }
      },
      {
        path: 'office/community-healthcare',
        name: 'OfficeCommunityHealthcare',
        component: () => import('@/views/office/community-healthcare.vue'),
        meta: { title: '医共体科教协同', icon: 'Share' }
      },
      {
        path: 'ethics/review-workflow',
        name: 'EthicsReviewWorkflow',
        component: () => import('@/views/ethics/review-workflow.vue'),
        meta: { title: '科研伦理审查工作台', icon: 'DocumentChecked' }
      },
      {
        path: 'error/404',
        name: 'Error404',
        component: () => import('@/views/error/404.vue'),
        meta: { title: '404 资源未找到' }
      },
      {
        path: 'error/403',
        name: 'Error403',
        component: () => import('@/views/error/403.vue'),
        meta: { title: '403 权限不足' }
      },
      {
        path: 'iframe',
        name: 'IframeView',
        component: () => import('@/views/iframe/IframeView.vue'),
        meta: { title: '业务页面' }
      }
    ]
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/error/404'
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由全局守卫
router.beforeEach(async (to, _from, next) => {
  document.title = `${to.meta.title ? to.meta.title + ' - ' : ''}科研信息管理系统`

  const userStore = useUserStore()
  const hasToken = !!userStore.token || !!sessionStorage.getItem('userInfo')

  if (to.path === '/login') {
    if (hasToken) {
      next('/dashboard')
    } else {
      next()
    }
  } else {
    if (!hasToken) {
      next(`/login?redirect=${encodeURIComponent(to.fullPath)}`)
    } else {
      if (!userStore.userInfo) {
        try {
          await userStore.fetchUserInfo()
        } catch {
          // ignore error to let user access cached session
        }
      }
      next()
    }
  }
})

router.afterEach((to) => {
  const tabsStore = useTabsStore()
  tabsStore.addTab(to)
})

export default router
