<template>
  <el-drawer
    v-model="visible"
    title="医院科研管理系统 · 全景功能大纲地图"
    size="880px"
    destroy-on-close
    class="mega-map-drawer"
  >
    <div class="mega-map-container">
      <!-- 顶部搜索与统计提示 -->
      <div class="map-header">
        <div class="desc-text">
          <span class="badge">全景业务矩阵</span>
          <span>全院 152 项科研管理业务功能全景视图，支持一键直达，告别逐级点开菜单。</span>
        </div>
        <div class="search-row">
          <el-input
            v-model="searchQuery"
            placeholder="输入功能名称、业务类型或拼音首字母进行实时过滤..."
            clearable
            :prefix-icon="Search"
            class="filter-input"
          />
        </div>
      </div>

      <!-- 快速筛选与定位标签栏 -->
      <div class="category-tabs">
        <span
          class="cat-chip"
          :class="{ active: selectedCategory === '' }"
          @click="selectedCategory = ''"
        >
          全部模块 ({{ totalFunctionsCount }})
        </span>
        <span
          v-for="cat in megaCategories"
          :key="cat.name"
          class="cat-chip"
          :class="{ active: selectedCategory === cat.name }"
          @click="selectedCategory = cat.name"
        >
          {{ cat.icon }} {{ cat.name }} ({{ cat.items.length }})
        </span>
      </div>

      <!-- 全景功能卡片网格 -->
      <el-scrollbar class="map-scrollbar">
        <div class="cards-grid">
          <div
            v-for="cat in filteredCategories"
            :key="cat.name"
            class="mega-category-card"
          >
            <div class="card-header">
              <div class="header-left">
                <span class="cat-icon">{{ cat.icon }}</span>
                <span class="cat-title">{{ cat.name }}</span>
              </div>
              <el-tag size="small" type="info">{{ cat.items.length }} 项业务</el-tag>
            </div>
            <div class="card-desc">{{ cat.description }}</div>

            <div class="function-list">
              <div
                v-for="item in cat.items"
                :key="item.path"
                class="function-item"
                :class="{ active: currentPath === item.path }"
                @click="jumpTo(item.path)"
              >
                <div class="func-main">
                  <el-icon class="func-icon"><component :is="item.icon" /></el-icon>
                  <span class="func-name">{{ item.name }}</span>
                  <el-tag
                    v-if="item.tag"
                    size="small"
                    :type="item.tagType || 'primary'"
                    class="func-tag"
                  >
                    {{ item.tag }}
                  </el-tag>
                </div>
                <div class="func-actions">
                  <el-icon
                    class="fav-btn"
                    :class="{ starred: isFavorite(item.path) }"
                    :title="isFavorite(item.path) ? '取消常用' : '设为常用'"
                    @click.stop="toggleFavorite(item)"
                  >
                    <StarFilled v-if="isFavorite(item.path)" />
                    <Star v-else />
                  </el-icon>
                  <el-icon class="jump-arrow"><Right /></el-icon>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div v-if="filteredCategories.length === 0" class="empty-state">
          <el-empty description="未检索到匹配的科研功能模块，请更换关键词检索" />
        </div>
      </el-scrollbar>
    </div>
  </el-drawer>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  Search,
  Star,
  StarFilled,
  Right,
  TrendCharts,
  Collection,
  Files,
  Histogram,
  Money,
  Reading,
  Document,
  User,
  Medal,
  Download,
  Bell,
  Setting,
  Key,
  Lock,
  House,
  School
} from '@element-plus/icons-vue'

export interface MegaFunctionItem {
  name: string
  path: string
  icon: any
  tag?: string
  tagType?: 'primary' | 'success' | 'warning' | 'danger' | 'info'
  keywords?: string
}

export interface MegaCategory {
  name: string
  icon: string
  description: string
  items: MegaFunctionItem[]
}

const props = defineProps<{
  modelValue: boolean
  favorites: string[]
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', val: boolean): void
  (e: 'toggle-favorite', item: MegaFunctionItem): void
}>()

const visible = computed({
  get: () => props.modelValue,
  set: (val: boolean) => emit('update:modelValue', val)
})

const route = useRoute()
const router = useRouter()
const currentPath = computed(() => route.path)

const searchQuery = ref('')
const selectedCategory = ref('')

const megaCategories: MegaCategory[] = [
  {
    name: '科研课题全周期与立项',
    icon: '🔬',
    description: '国家自然科学基金、省市级纵向申报向导，横向产学研合同，2秒一键配套与结余基金管理',
    items: [
      { name: '纵向项目申报向导', path: '/project/vertical-application', icon: TrendCharts, tag: '向导', tagType: 'primary', keywords: '纵向 申报 立项 国自然 基金 课题' },
      { name: '纵向课题全生命周期治理', path: '/project/vertical-lifecycle', icon: Files, tag: '合规中枢', tagType: 'danger', keywords: '纵向 课题 治理 中检 变更 结题 验收' },
      { name: '纵向项目申报计划发布', path: '/project/vertical-application?tab=plans', icon: TrendCharts, tag: '计划', tagType: 'info', keywords: '计划 申报批次 申报指南' },
      { name: '院级项目批次与评审方案', path: '/project/vertical-application?tab=plans', icon: TrendCharts, tag: '院级', tagType: 'warning', keywords: '院级项目 批次 限额 资格 评审方案' },
      { name: '横向项目与产学研合同', path: '/project/horizontal-contract', icon: Collection, tag: '产学研', tagType: 'success', keywords: '横向 合同 企业 委托 合作' },
      { name: '2秒一键配套项目立项', path: '/project/vertical-lifecycle?tab=approvals', icon: TrendCharts, tag: '高频', tagType: 'warning', keywords: '配套 资助 院级 配套项目' },
      { name: '项目结余转结可持续基金', path: '/project/vertical-lifecycle?tab=closeouts', icon: Files, tag: '结余', tagType: 'success', keywords: '结余 结转 结题' }
    ]
  },
  {
    name: '经费核算与业财双向闭环',
    icon: '💵',
    description: '分批到账预算模式、直接间接经费分层台账、票据报销核销与 HRP 财务中间库双向协同',
    items: [
      { name: '项目经费分层台账', path: '/finance/ledger', icon: Histogram, tag: '预算执行', tagType: 'primary', keywords: '经费 台账 分层 预算' },
      { name: '纵向经费到账管理与认领', path: '/finance/incoming', icon: Money, tag: '到账', tagType: 'success', keywords: '到账 认领 拨款 支票 纵向' },
      { name: '横向经费到账管理与认领', path: '/finance/incoming?tab=horizontal', icon: Money, tag: '横向', tagType: 'info', keywords: '横向 到账 认领 企业回款' },
      { name: '经费报销与票据核销台账', path: '/finance/expense-claims', icon: Money, tag: '报销', tagType: 'danger', keywords: '报销 发票 公务卡 核销 支出 冲账' },
      { name: '预算异常对账与处理', path: '/governance/proxy-reconciliation', icon: Key, tag: '对账', tagType: 'warning', keywords: '对账 异常 预算 财务 hrp' },
      { name: '单据检索与财务账单查找', path: '/finance/expense-claims?tab=bills', icon: Document, tag: '单据', tagType: 'info', keywords: '单据 账单 检索 查询 凭证' },
      { name: '外部财务HRP中间库同步监控', path: '/operations/external-sync', icon: Setting, tag: 'HRP', tagType: 'info', keywords: '外部 接口 hrp 财务 同步 任务' }
    ]
  },
  {
    name: '学术成果、网推与临床专病生态',
    icon: '📚',
    description: '主动大数据论文网推与机构别名消歧、JCR/中科院分区自动打标、Biobank 样本库资产借还核销',
    items: [
      { name: '论文成果主动网推认领中心', path: '/fruit/paper-claim', icon: Reading, tag: '智能网推', tagType: 'danger', keywords: '论文 认领 网推 消歧 中科院 分区 影响因子' },
      { name: '学术论文全生命周期管理', path: '/fruit/paper-management', icon: Document, tag: '成果', tagType: 'primary', keywords: '论文 SCI 期刊 crossref pubmed' },
      { name: '论文投稿事前备案与期刊风控', path: '/fruit/paper-submission', icon: Document, tag: '风控闭环', tagType: 'danger', keywords: '论文 投稿 备案 期刊 预警 黑名单 风控 诚信 版面费' },
      { name: '成果综合业务台账 (专利/著作/奖励)', path: '/fruit/achievement-ledger', icon: Reading, tag: '综合台账', tagType: 'success', keywords: '专利 著作 奖励 成果 转化 知识产权' },
      { name: '临床生物样本库 (Biobank) 资产台账', path: '/fruit/biobank-ledger', icon: Reading, tag: 'Biobank', tagType: 'warning', keywords: '生物样本 样本库 冻存 标本 队列 biobank' },
      { name: '生物样本借还与实验消耗核销', path: '/fruit/biobank-ledger?tab=ledger', icon: Files, tag: '样本借还', tagType: 'info', keywords: '借还 出库 消耗 核销 伦理' },
      { name: '我的科研全景档案与数字名片', path: '/office/research-portfolio', icon: User, tag: '个人画卷', tagType: 'primary', keywords: '档案 个人 资产 名片 成果盘点' }
    ]
  },
  {
    name: '专家评审、学术活动与人才队伍',
    icon: '👥',
    description: '专家盲审双向脱敏、学术委员会利益冲突回避、国际国内学术会议讲座与学术任职遴选',
    items: [
      { name: '专家匿名盲审工作台', path: '/evaluation/expert-workbench', icon: Medal, tag: '双盲脱敏', tagType: 'danger', keywords: '专家 评审 盲审 回避 脱敏 打分' },
      { name: '学术活动与会议讲座管理', path: '/activities/academic-activities', icon: Reading, tag: '学术活动', tagType: 'primary', keywords: '讲座 会议 反馈 专家 继教' },
      { name: '继续医学教育 (CME) 学分', path: '/office/cme-credits', icon: School, tag: 'CME授分', tagType: 'success', keywords: '继续医学教育 CME 学分 继教 达标 讲座 签到 催办' },
      { name: '学术任职与专家人才中心', path: '/organization/academic-appointments', icon: Medal, tag: '任职遴选', tagType: 'success', keywords: '任职 专家 遴选 人才 学会' },
      { name: '部门人员与课题团队维护', path: '/organization/people', icon: User, tag: '组织架构', tagType: 'info', keywords: '组织 部门 人员 员工 架构' }
    ]
  },
  {
    name: '绩效考核、国考决策与行政治理',
    icon: '⚖️',
    description: '动态绩效公式计算器、多作者位次拆分、卫健委国考年报、移动协同推送与权限安全审计',
    items: [
      { name: '科研绩效评价与奖励兑现', path: '/evaluation/performance-evaluation', icon: Medal, tag: '动态算分', tagType: 'danger', keywords: '绩效 评价 积分 奖励 考核 算分' },
      { name: '动态绩效公式与多作者位次拆分', path: '/evaluation/performance-evaluation?tab=formula_designer', icon: Setting, tag: '公式引擎', tagType: 'warning', keywords: '公式 位次 一作 通讯 拆分' },
      { name: '自定义多维科研报表与卫健委国考年报', path: '/evaluation/performance-evaluation?tab=custom_reports', icon: Histogram, tag: '国考直报', tagType: 'success', keywords: '报表 自定义 国考 卫健委 年报 统计' },
      { name: '科研待办流转审核中心', path: '/workflow/todo-approvals', icon: Files, tag: '待办中心', tagType: 'danger', keywords: '待办 审核 流程 批注 审批' },
      { name: '移动科研协同工作台与推送网关', path: '/office/mobile-workbench', icon: House, tag: '企业微信/钉钉', tagType: 'primary', keywords: '移动 手机 工作台 企业微信 钉钉 推送' },
      { name: '科研统计决策分析大屏', path: '/analytics/statistics', icon: Histogram, tag: '决策大屏', tagType: 'primary', keywords: '统计 分析 大屏 报表 看板 态势' },
      { name: '权限代理委托与离岗回收', path: '/governance/proxy-reconciliation', icon: Key, tag: '等保合规', tagType: 'info', keywords: '代理 授权 对账 离岗 委托' },
      { name: '科研通知公告', path: '/office/notice', icon: Bell, tag: '通知', tagType: 'info', keywords: '通知 公告 新闻 院发文' },
      { name: '科研制度与资料下载中心', path: '/office/downloads', icon: Download, tag: '资料库', tagType: 'info', keywords: '下载 制度 模板 规范 申请表' },
      { name: '角色授权与数据脱敏规则配置', path: '/system/roles', icon: Lock, tag: '角色权限', tagType: 'info', keywords: '角色 权限 字段 授权 脱敏' },
      { name: '等保三级登录审计日志', path: '/system/login-logs', icon: Document, tag: '等保审计', tagType: 'info', keywords: '日志 登录 审计 安全 ip' },
      { name: '科研基础字典规范', path: '/base-info/dictionary', icon: Files, tag: '标准字典', tagType: 'info', keywords: '字典 编码 规范 国标' },
      { name: '科研基础信息标准配置', path: '/base-info/system-configs', icon: Setting, tag: '系统配置', tagType: 'info', keywords: '分类 模版 期刊 行业 学科 配置' },
      { name: '医院运营模式（三级综合/二级精简）', path: '/base-info/system-configs?tab=hospitalMode', icon: Setting, tag: '模式切换', tagType: 'primary', keywords: '医院 运营模式 三级 综合 二级 精简 模式 基层' }
    ]
  }
]

const totalFunctionsCount = computed(() => {
  return megaCategories.reduce((acc, cat) => acc + cat.items.length, 0)
})

const filteredCategories = computed(() => {
  let list = megaCategories
  if (selectedCategory.value) {
    list = list.filter((cat) => cat.name === selectedCategory.value)
  }

  const q = searchQuery.value.trim().toLowerCase()
  if (!q) return list

  return list
    .map((cat) => ({
      ...cat,
      items: cat.items.filter(
        (item) =>
          item.name.toLowerCase().includes(q) ||
          item.path.toLowerCase().includes(q) ||
          (item.keywords && item.keywords.toLowerCase().includes(q))
      )
    }))
    .filter((cat) => cat.items.length > 0)
})

function isFavorite(path: string): boolean {
  return props.favorites.includes(path)
}

function toggleFavorite(item: MegaFunctionItem) {
  emit('toggle-favorite', item)
}

function jumpTo(path: string) {
  visible.value = false
  router.push(path)
}
</script>

<style scoped lang="scss">
.mega-map-container {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.map-header {
  margin-bottom: 14px;

  .desc-text {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 13px;
    color: #475569;
    margin-bottom: 12px;

    .badge {
      background: #eff6ff;
      color: #2563eb;
      font-weight: 600;
      padding: 2px 8px;
      border-radius: 4px;
      font-size: 12px;
      white-space: nowrap;
      flex-shrink: 0;
    }
  }

  .filter-input {
    width: 100%;
  }
}

.category-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 16px;
  padding-bottom: 12px;
  border-bottom: 1px solid #e2e8f0;

  .cat-chip {
    padding: 4px 12px;
    border-radius: 16px;
    background: #f1f5f9;
    color: #475569;
    font-size: 12px;
    cursor: pointer;
    transition: all 0.2s;
    user-select: none;
    white-space: nowrap;
    flex-shrink: 0;

    &:hover {
      background: #e2e8f0;
      color: #0f172a;
    }

    &.active {
      background: #3b82f6;
      color: #ffffff;
      font-weight: 600;
    }
  }
}

.map-scrollbar {
  flex: 1;
  padding-right: 8px;
}

.cards-grid {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.mega-category-card {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  padding: 16px;
  transition: box-shadow 0.2s;

  &:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  }

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 4px;

    .header-left {
      display: flex;
      align-items: center;
      gap: 8px;

      .cat-icon {
        font-size: 18px;
      }

      .cat-title {
        font-size: 15px;
        font-weight: 700;
        color: #1e293b;
      }
    }
  }

  .card-desc {
    font-size: 12px;
    color: #64748b;
    margin-bottom: 12px;
  }

  .function-list {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
    gap: 8px;
  }

  .function-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 8px 12px;
    background: #ffffff;
    border: 1px solid #e2e8f0;
    border-radius: 6px;
    cursor: pointer;
    transition: all 0.15s ease-in-out;

    &:hover {
      border-color: #3b82f6;
      background: #eff6ff;
      transform: translateY(-1px);

      .jump-arrow {
        color: #3b82f6;
        transform: translateX(3px);
      }
    }

    &.active {
      border-color: #3b82f6;
      background: #eff6ff;

      .func-name {
        color: #1d4ed8;
        font-weight: 600;
      }
    }

    .func-main {
      display: flex;
      align-items: center;
      gap: 8px;
      overflow: hidden;
      min-width: 0;
      flex: 1;

      .func-icon {
        color: #64748b;
        font-size: 15px;
        flex-shrink: 0;
      }

      .func-name {
        font-size: 13px;
        color: #334155;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        min-width: 0;
      }

      .func-tag {
        font-size: 10px;
        padding: 0 4px;
        height: 18px;
        line-height: 18px;
        flex-shrink: 0;
        white-space: nowrap;
      }
    }

    .func-actions {
      display: flex;
      align-items: center;
      gap: 6px;
      flex-shrink: 0;
      margin-left: 6px;

      .fav-btn {
        color: #cbd5e1;
        font-size: 15px;
        cursor: pointer;
        transition: color 0.2s;

        &:hover {
          color: #f59e0b;
        }

        &.starred {
          color: #f59e0b;
        }
      }

      .jump-arrow {
        color: #94a3b8;
        font-size: 13px;
        transition: all 0.2s;
      }
    }
  }
}

.empty-state {
  padding: 40px 0;
}
</style>
