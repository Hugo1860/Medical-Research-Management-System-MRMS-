<template>
  <div class="sidebar-container" :class="{ collapsed: isCollapse }">
    <!-- 顶部 Logo 栏 -->
    <div class="logo">
      <el-icon :size="24" class="logo-icon"><Management /></el-icon>
      <span v-show="!isCollapse" class="logo-title">科研管理系统</span>
      <el-tag
        v-if="!isCollapse && hospitalModeStore.isGrassroots"
        size="small"
        type="success"
        effect="dark"
        style="margin-left: 8px; font-size: 11px; padding: 0 6px;"
      >
        精简版
      </el-tag>
    </div>

    <!-- 展开状态下的效率操作工具栏 (搜索、全部展开/折叠、平铺/树形切换、全景地图) -->
    <div v-show="!isCollapse" class="sidebar-toolbar">
      <!-- 1. 实时快捷搜索框 -->
      <div class="search-box">
        <el-input
          v-model="searchKeyword"
          placeholder="快速查找功能 (如: 报销/论文/申报)..."
          clearable
          size="small"
          :prefix-icon="Search"
          class="menu-search-input"
        />
      </div>

      <!-- 2. 便捷操作按钮栏 -->
      <div class="tools-row">
        <el-tooltip content="展开所有层级菜单分组" placement="top" :show-after="300">
          <button
            type="button"
            class="tool-btn"
            :class="{ active: isAllExpanded }"
            data-testid="btn-expand-all"
            @click="handleExpandAll"
          >
            <el-icon><Expand /></el-icon>
            <span>展开</span>
          </button>
        </el-tooltip>

        <el-tooltip content="收起折叠所有菜单分组" placement="top" :show-after="300">
          <button
            type="button"
            class="tool-btn"
            :class="{ active: isAllCollapsed }"
            data-testid="btn-collapse-all"
            @click="handleCollapseAll"
          >
            <el-icon><Fold /></el-icon>
            <span>折叠</span>
          </button>
        </el-tooltip>

        <el-tooltip
          :content="viewMode === 'tree' ? '切换为平铺大纲模式（无需点开文件夹）' : '切换为传统树形折叠模式'"
          placement="top"
          :show-after="300"
        >
          <button
            type="button"
            class="tool-btn"
            :class="{ active: viewMode === 'flat' }"
            data-testid="btn-toggle-view-mode"
            @click="toggleViewMode"
          >
            <el-icon><Operation /></el-icon>
            <span>{{ viewMode === 'tree' ? '平铺' : '树形' }}</span>
          </button>
        </el-tooltip>

        <el-tooltip content="打开全景功能地图大纲 (152项业务全览)" placement="top" :show-after="300">
          <button
            type="button"
            class="tool-btn map-btn"
            data-testid="btn-mega-map"
            @click="megaMapVisible = true"
          >
            <el-icon><Grid /></el-icon>
            <span>全景图</span>
          </button>
        </el-tooltip>

        <el-tooltip content="自定义调整菜单显示顺序 (个人偏好)" placement="top" :show-after="300">
          <button
            type="button"
            class="tool-btn sort-btn"
            :class="{ active: menuOrderStore.hasCustomOrder }"
            data-testid="btn-menu-sort"
            @click="menuSortDialogVisible = true"
          >
            <el-icon><Sort /></el-icon>
            <span>排序</span>
          </button>
        </el-tooltip>
      </div>
    </div>

    <!-- 菜单滚动区域 -->
    <el-scrollbar class="menu-scrollbar">
      <!-- A. 搜索结果模式：当输入搜索关键字时，直接扁平化展示匹配的功能列表 -->
      <div v-if="searchKeyword.trim() && !isCollapse" class="search-results-list">
        <div class="results-header">
          <span>找到 {{ filteredLeafMenus.length }} 个相关功能</span>
          <el-button link size="small" type="primary" @click="searchKeyword = ''">清空</el-button>
        </div>

        <div
          v-for="item in filteredLeafMenus"
          :key="String(item.id)"
          class="flat-nav-item"
          :class="{ active: activeMenu === item.route }"
          @click="onItemClick(item.route)"
        >
          <div class="item-main">
            <el-icon class="item-icon"><component :is="item.icon" /></el-icon>
            <div class="item-text">
              <span class="item-name" :title="item.name">{{ item.name }}</span>
              <span class="item-crumbs" :title="item.categoryPath">{{ item.categoryPath }}</span>
            </div>
          </div>
          <el-icon
            class="star-fav-btn"
            :class="{ starred: favoritesStore.isFavorite(item.route) }"
            :title="favoritesStore.isFavorite(item.route) ? '取消常用' : '设为常用'"
            @click.stop="favoritesStore.toggleFavorite({ name: item.name, path: item.route, category: item.categoryPath })"
          >
            <StarFilled v-if="favoritesStore.isFavorite(item.route)" />
            <Star v-else />
          </el-icon>
        </div>

        <div v-if="filteredLeafMenus.length === 0" class="no-result">
          未检索到匹配的菜单，换个关键词试试
        </div>
      </div>

      <!-- B. 平铺大纲模式：无需逐级点开文件夹，按业务板块直接平铺所有可用功能（支持分组展开/折叠） -->
      <div v-else-if="viewMode === 'flat' && !isCollapse" class="flat-outline-container">
        <!-- 首页科研工作台 -->
        <div
          class="outline-leaf-item dashboard-item"
          :class="{ active: activeMenu === '/dashboard' }"
          @click="onItemClick('/dashboard')"
        >
          <el-icon class="leaf-icon"><House /></el-icon>
          <span class="leaf-title">科研工作台</span>
        </div>

        <!-- 置顶 ⭐ 常用功能 -->
        <div v-if="favoritesStore.favorites.length > 0" class="outline-section">
          <div class="section-title clickable" @click="toggleFlatSection('favorites')">
            <div class="section-title-left">
              <el-icon class="section-arrow" :class="{ collapsed: isFlatSectionCollapsed('favorites') }">
                <CaretBottom />
              </el-icon>
              <el-icon color="#f59e0b"><StarFilled /></el-icon>
              <span class="section-name">常用功能</span>
            </div>
            <span class="section-badge">{{ favoritesStore.favorites.length }}</span>
          </div>
          <div v-show="!isFlatSectionCollapsed('favorites')" class="section-body">
            <div
              v-for="fav in favoritesStore.favorites"
              :key="fav.path"
              class="outline-leaf-item"
              :class="{ active: activeMenu === fav.path }"
              :title="fav.name"
              @click="onItemClick(fav.path)"
            >
              <el-icon class="leaf-icon"><component :is="getIconByName(fav.iconName)" /></el-icon>
              <span class="leaf-title">{{ fav.name }}</span>
              <el-icon
                class="unpin-icon"
                title="移出常用"
                @click.stop="favoritesStore.removeFavorite(fav.path)"
              >
                <Close />
              </el-icon>
            </div>
          </div>
        </div>

        <!-- 业务大板块平铺分组 -->
        <div
          v-for="grp in groupedFlatCatalog"
          :key="grp.category"
          class="outline-section"
        >
          <div class="section-title clickable" @click="toggleFlatSection(grp.category)">
            <div class="section-title-left">
              <el-icon class="section-arrow" :class="{ collapsed: isFlatSectionCollapsed(grp.category) }">
                <CaretBottom />
              </el-icon>
              <span class="section-name">{{ grp.category }}</span>
            </div>
            <span class="section-badge">{{ grp.items.length }}</span>
          </div>
          <div v-show="!isFlatSectionCollapsed(grp.category)" class="section-body">
            <div
              v-for="leaf in grp.items"
              :key="String(leaf.id)"
              class="outline-leaf-item"
              :class="{ active: activeMenu === leaf.route }"
              :title="leaf.name"
              @click="onItemClick(leaf.route)"
            >
              <el-icon class="leaf-icon"><component :is="leaf.icon" /></el-icon>
              <span class="leaf-title">{{ leaf.name }}</span>
              <el-icon
                class="star-fav-btn"
                :class="{ starred: favoritesStore.isFavorite(leaf.route) }"
                :title="favoritesStore.isFavorite(leaf.route) ? '取消常用' : '设为常用'"
                @click.stop="favoritesStore.toggleFavorite({ name: leaf.name, path: leaf.route, category: leaf.categoryPath })"
              >
                <StarFilled v-if="favoritesStore.isFavorite(leaf.route)" />
                <Star v-else />
              </el-icon>
            </div>
          </div>
        </div>
      </div>

      <!-- C. 传统树形折叠菜单模式 (含置顶常用功能与一键全部展开/折叠控制) -->
      <el-menu
        v-else
        :key="menuKey"
        :default-active="activeMenu"
        :default-openeds="openedMenus"
        :collapse="isCollapse"
        :collapse-transition="false"
        background-color="#304156"
        text-color="#bfcbd9"
        active-text-color="#409eff"
        @open="onSubMenuOpen"
        @close="onSubMenuClose"
      >
        <!-- 固定首页工作台 -->
        <el-menu-item index="/dashboard" @click="onItemClick('/dashboard')">
          <el-icon><House /></el-icon>
          <template #title>科研工作台</template>
        </el-menu-item>

        <!-- 置顶 ⭐ 常用功能子菜单 -->
        <el-sub-menu
          v-if="!isCollapse && favoritesStore.favorites.length > 0"
          index="favorites"
          class="favorites-submenu"
        >
          <template #title>
            <el-icon color="#f59e0b"><StarFilled /></el-icon>
            <span class="sub-menu-label" style="font-weight: 600; color: #f59e0b;">
              常用功能 ({{ favoritesStore.favorites.length }})
            </span>
          </template>

          <el-menu-item
            v-for="fav in favoritesStore.favorites"
            :key="'fav-' + fav.path"
            :index="'fav:' + fav.path"
            :class="{ 'is-active-fav': activeMenu === fav.path }"
            @click="onItemClick(fav.path)"
          >
            <el-icon><component :is="getIconByName(fav.iconName)" /></el-icon>
            <template #title>
              <div class="fav-menu-inner" :title="fav.name">
                <span class="menu-item-text">{{ fav.name }}</span>
                <el-icon
                  class="remove-fav-btn"
                  title="移出常用"
                  @click.stop="favoritesStore.removeFavorite(fav.path)"
                >
                  <Close />
                </el-icon>
              </div>
            </template>
          </el-menu-item>
        </el-sub-menu>

        <!-- 动态权限菜单树 (支持未加载时自动兜底完整科研目录树) -->
        <template v-for="item in effectivePermissions" :key="item.菜单.编号">
          <!-- 有子菜单 -->
          <el-sub-menu
            v-if="item.子级菜单 && item.子级菜单.length > 0"
            :index="String(item.菜单.编号)"
          >
            <template #title>
              <el-icon><component :is="getMenuIcon(item.菜单.名称)" /></el-icon>
              <span class="sub-menu-label" :title="getMenuDisplayName(item.菜单)">{{ getMenuDisplayName(item.菜单) }}</span>
            </template>

            <template v-for="sub in item.子级菜单" :key="sub.菜单.编号">
              <!-- 二级仍有子菜单 -->
              <el-sub-menu
                v-if="sub.子级菜单 && sub.子级菜单.length > 0"
                :index="String(sub.菜单.编号)"
              >
                <template #title>
                  <el-icon><Folder /></el-icon>
                  <span class="sub-menu-label" :title="getMenuDisplayName(sub.菜单)">{{ getMenuDisplayName(sub.菜单) }}</span>
                </template>
                <el-menu-item
                  v-for="third in sub.子级菜单"
                  :key="third.菜单.编号"
                  :index="getMenuRoute(third.菜单)"
                  @click="onMenuSelect(third.菜单)"
                >
                  <el-icon><Document /></el-icon>
                  <template #title>
                    <div class="tree-menu-title-row" :title="getMenuDisplayName(third.菜单)">
                      <span class="menu-item-text">{{ getMenuDisplayName(third.菜单) }}</span>
                      <el-icon
                        class="tree-star-icon"
                        :class="{ starred: favoritesStore.isFavorite(getMenuRoute(third.菜单)) }"
                        :title="favoritesStore.isFavorite(getMenuRoute(third.菜单)) ? '取消常用' : '设为常用'"
                        @click.stop="favoritesStore.toggleFavorite({ name: getMenuDisplayName(third.菜单), path: getMenuRoute(third.菜单), category: getMenuDisplayName(sub.菜单) })"
                      >
                        <StarFilled v-if="favoritesStore.isFavorite(getMenuRoute(third.菜单))" />
                        <Star v-else />
                      </el-icon>
                    </div>
                  </template>
                </el-menu-item>
              </el-sub-menu>

              <!-- 二级叶子菜单 -->
              <el-menu-item
                v-else
                :index="getMenuRoute(sub.菜单)"
                @click="onMenuSelect(sub.菜单)"
              >
                <el-icon><Document /></el-icon>
                <template #title>
                  <div class="tree-menu-title-row" :title="getMenuDisplayName(sub.菜单)">
                    <span class="menu-item-text">{{ getMenuDisplayName(sub.菜单) }}</span>
                    <el-icon
                      class="tree-star-icon"
                      :class="{ starred: favoritesStore.isFavorite(getMenuRoute(sub.菜单)) }"
                      :title="favoritesStore.isFavorite(getMenuRoute(sub.菜单)) ? '取消常用' : '设为常用'"
                    @click.stop="favoritesStore.toggleFavorite({ name: getMenuDisplayName(sub.菜单), path: getMenuRoute(sub.菜单), category: getMenuDisplayName(item.菜单) })"
                    >
                      <StarFilled v-if="favoritesStore.isFavorite(getMenuRoute(sub.菜单))" />
                      <Star v-else />
                    </el-icon>
                  </div>
                </template>
              </el-menu-item>
            </template>
          </el-sub-menu>

          <!-- 一级叶子菜单 -->
          <el-menu-item
            v-else
            :index="getMenuRoute(item.菜单)"
            @click="onMenuSelect(item.菜单)"
          >
            <el-icon><component :is="getMenuIcon(item.菜单.名称)" /></el-icon>
            <template #title>
              <div class="tree-menu-title-row" :title="getMenuDisplayName(item.菜单)">
                <span class="menu-item-text">{{ getMenuDisplayName(item.菜单) }}</span>
                <el-icon
                  class="tree-star-icon"
                  :class="{ starred: favoritesStore.isFavorite(getMenuRoute(item.菜单)) }"
                  @click.stop="favoritesStore.toggleFavorite({ name: getMenuDisplayName(item.菜单), path: getMenuRoute(item.菜单) })"
                >
                  <StarFilled v-if="favoritesStore.isFavorite(getMenuRoute(item.菜单))" />
                  <Star v-else />
                </el-icon>
              </div>
            </template>
          </el-menu-item>
        </template>
      </el-menu>
    </el-scrollbar>

    <!-- 全景功能大纲地图抽屉 -->
    <MegaMapDrawer
      v-model="megaMapVisible"
      :favorites="favoritePaths"
      @toggle-favorite="onToggleFavorite"
    />

    <!-- 自定义菜单顺序设置弹窗 -->
    <MenuSortDialog
      v-model="menuSortDialogVisible"
      :raw-permissions="rawPermissionsList"
      @order-changed="menuKey++"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useFavoritesStore } from '@/stores/favorites'
import { useMenuOrderStore } from '@/stores/menuOrder'
import { useHospitalModeStore } from '@/stores/hospitalMode'
import type { MenuItem, PermissionNode } from '@/api/auth'
import MegaMapDrawer from './MegaMapDrawer.vue'
import MenuSortDialog from './MenuSortDialog.vue'
import { resolveModernRoute } from '@/utils/route-resolver'
import { ElMessage } from 'element-plus'
import {
  House,
  Management,
  Folder,
  Document,
  Briefcase,
  TrendCharts,
  Collection,
  Reading,
  Histogram,
  User,
  Setting,
  Files,
  Medal,
  Money,
  Search,
  Expand,
  Fold,
  Grid,
  Operation,
  Sort,
  Star,
  StarFilled,
  Close,
  Key,
  Bell,
  Download,
  Lock,
  CaretBottom,
  School
} from '@element-plus/icons-vue'

defineProps<{
  isCollapse: boolean
}>()

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const favoritesStore = useFavoritesStore()
const menuOrderStore = useMenuOrderStore()
const hospitalModeStore = useHospitalModeStore()

// 1. 状态与配置
const searchKeyword = ref('')
const viewMode = ref<'tree' | 'flat'>(
  (localStorage.getItem('scientific_research_menu_view_mode') as 'tree' | 'flat') || 'tree'
)
const megaMapVisible = ref(false)
const menuSortDialogVisible = ref(false)
const menuKey = ref(1)
const collapsedFlatSections = ref<string[]>([])

const favoritePaths = computed(() => {
  return favoritesStore.favorites.map((f) => f.path)
})

const activeMenu = computed(() => {
  if (route.path === '/iframe') {
    return route.fullPath
  }
  return route.path
})

// 2. 提取扁平叶子菜单与默认兜底树，保障树形与平铺模式均立即可用
export interface LeafMenuItem {
  id: string | number
  name: string
  route: string
  categoryPath: string
  icon: any
}

const defaultCatalogLeafMenus: LeafMenuItem[] = [
  { id: 'v-app', name: '纵向项目申报向导', route: '/project/vertical-application', categoryPath: '纵向课题 / 申报立项', icon: TrendCharts },
  { id: 'v-life', name: '纵向课题全生命周期治理', route: '/project/vertical-lifecycle', categoryPath: '纵向课题 / 过程监管', icon: Files },
  { id: 'v-plan', name: '纵向项目申报计划发布', route: '/project/vertical-application?tab=plans', categoryPath: '纵向课题 / 计划指南', icon: TrendCharts },
  { id: 'internal-plan', name: '院级项目批次与评审方案', route: '/project/vertical-application?tab=plans', categoryPath: '院级课题 / 批次评审', icon: TrendCharts },
  { id: 'h-con', name: '横向项目与产学研合同', route: '/project/horizontal-contract', categoryPath: '横向课题 / 产学研', icon: Collection },
  { id: 'f-led', name: '项目经费分层台账', route: '/finance/ledger', categoryPath: '经费管理 / 预算分层', icon: Histogram },
  { id: 'f-inc', name: '经费到账管理与认领', route: '/finance/incoming', categoryPath: '经费管理 / 经费到账', icon: Money },
  { id: 'f-exp', name: '经费报销与票据核销台账', route: '/finance/expense-claims', categoryPath: '经费管理 / 票据核销', icon: Money },
  { id: 'p-claim', name: '论文成果主动网推认领', route: '/fruit/paper-claim', categoryPath: '学术成果 / 智能网推', icon: Reading },
  { id: 'p-mgmt', name: '学术论文全生命周期管理', route: '/fruit/paper-management', categoryPath: '学术成果 / 论文管理', icon: Document },
  { id: 'ach-led', name: '成果综合业务台账 (专利/奖励)', route: '/fruit/achievement-ledger', categoryPath: '学术成果 / 综合台账', icon: Reading },
  { id: 'biobank', name: '临床生物样本库 (Biobank) 台账', route: '/fruit/biobank-ledger', categoryPath: '临床生态 / 生物样本', icon: Reading },
  { id: 'portfolio', name: '我的科研全景档案', route: '/office/research-portfolio', categoryPath: '科研办公 / 学术画像', icon: User },
  { id: 'eval', name: '科研绩效评价与奖励兑现', route: '/evaluation/performance-evaluation', categoryPath: '评价考核 / 绩效考核', icon: Medal },
  { id: 'todo', name: '科研待办流转审核中心', route: '/workflow/todo-approvals', categoryPath: '科研办公 / 我的待办', icon: Files },
  { id: 'mobile', name: '移动科研协同工作台', route: '/office/mobile-workbench', categoryPath: '科研办公 / 移动协同', icon: House },
  { id: 'further-education', name: '人员进修管理', route: '/office/further-education', categoryPath: '科研办公 / 人员进修', icon: School },
  { id: 'stat', name: '科研统计决策分析大屏', route: '/analytics/statistics', categoryPath: '统计决策 / 决策大屏', icon: Histogram },
  { id: 'expert', name: '专家匿名盲审工作台', route: '/evaluation/expert-workbench', categoryPath: '专家评审 / 盲审工作台', icon: Medal },
  { id: 'appt', name: '学术任职与专家人才中心', route: '/organization/academic-appointments', categoryPath: '学术人才 / 任职专家', icon: Medal },
  { id: 'act', name: '学术活动与会议讲座管理', route: '/activities/academic-activities', categoryPath: '学术活动 / 讲座会议', icon: Reading },
  { id: 'people', name: '部门人员信息维护', route: '/organization/people', categoryPath: '组织架构 / 部门人员', icon: User },
  { id: 'proxy', name: '权限代理与预算对账', route: '/governance/proxy-reconciliation', categoryPath: '行政治理 / 权限对账', icon: Key },
  { id: 'notice', name: '科研通知公告', route: '/office/notice', categoryPath: '科研办公 / 院级发文', icon: Bell },
  { id: 'down', name: '科研制度与资料下载中心', route: '/office/downloads', categoryPath: '科研办公 / 规范文件', icon: Download },
  { id: 'sync', name: '外部系统同步监控', route: '/operations/external-sync', categoryPath: '系统集成 / 外部接口', icon: Setting },
  { id: 'role', name: '角色权限管理', route: '/system/roles', categoryPath: '系统管理 / 权限脱敏', icon: Lock },
  { id: 'log', name: '等保三级登录审计日志', route: '/system/login-logs', categoryPath: '系统管理 / 等保审计', icon: Document },
  { id: 'dict', name: '科研基础字典规范', route: '/base-info/dictionary', categoryPath: '基础信息 / 标准字典', icon: Files },
  { id: 'cfg', name: '科研基础信息标准配置', route: '/base-info/system-configs', categoryPath: '基础信息 / 系统配置', icon: Setting },
  { id: 'research-platform', name: '网上科研服务', route: '/office/research-platform', categoryPath: '科研平台 / 网上科研服务', icon: Briefcase },
  { id: 'edu-stat', name: '教育部科研统计', route: '/office/education-statistics', categoryPath: '统计决策 / 教育部统计', icon: Histogram },
  { id: 'ach-ip', name: '成果与知识产权', route: '/fruit/achievement-ip-awards', categoryPath: '学术成果 / 知识产权', icon: Medal },
  { id: 'doc-share', name: '科研文档共享与消息中心', route: '/office/document-sharing', categoryPath: '科研办公 / 文档消息', icon: Files },
  { id: 'h-gov', name: '横向合同治理', route: '/project/horizontal-contract?tab=governance', categoryPath: '横向课题 / 合同治理', icon: Collection },
  { id: 'proj-cfg', name: '项目分类与预算模板', route: '/project/project-configuration', categoryPath: '基础信息 / 项目分类与预算模板', icon: Operation },
  { id: 'vert-import', name: '纵向项目导入', route: '/project/vertical-import', categoryPath: '纵向课题 / 计划书导入', icon: TrendCharts },
  { id: 'fund-exec', name: '经费执行治理', route: '/finance/fund-execution', categoryPath: '经费管理 / 执行治理', icon: Money },
  { id: 'inst-book', name: '仪器设备与开放预约', route: '/office/research-platform?tab=instruments', categoryPath: '科研平台 / 仪器预约', icon: Briefcase },
  { id: 'acad-exch', name: '学术交流治理', route: '/activities/academic-exchange', categoryPath: '学术活动 / 交流治理', icon: Reading },
  { id: 'jour-part', name: '期刊分区库', route: '/fruit/journal-partition', categoryPath: '学术成果 / 期刊分区', icon: Document },
  { id: 'paper-sub', name: '论文投稿事前备案', route: '/fruit/paper-submission', categoryPath: '学术成果 / 投稿备案', icon: Document },
  { id: 'cme-credit', name: '继续医学教育学分', route: '/office/cme-credits', categoryPath: '科研办公 / 继教学分', icon: School },
  { id: 'hosp-mode', name: '医院运营模式配置', route: '/base-info/system-configs?tab=hospitalMode', categoryPath: '基础信息 / 运营模式', icon: Setting }
]

const defaultTreePermissions = computed<PermissionNode[]>(() => {
  const map = new Map<string, LeafMenuItem[]>()
  for (const item of defaultCatalogLeafMenus) {
    const mainCat = item.categoryPath.split('/')[0].trim() || '科研业务'
    if (!map.has(mainCat)) {
      map.set(mainCat, [])
    }
    map.get(mainCat)!.push(item)
  }

  let catIdx = 9000
  let leafIdx = 9100
  const tree: PermissionNode[] = []
  for (const [catName, items] of map.entries()) {
    const parentId = catIdx++
    tree.push({
      菜单: { 编号: parentId, 名称: catName, 路径: '' },
      子级菜单: items.map((leaf) => ({
        菜单: {
          编号: leafIdx++,
          名称: leaf.name,
          路径: leaf.route,
          上级编号: parentId
        },
        子级菜单: []
      }))
    })
  }
  return tree
})

function filterTreeByHospitalMode(nodes: PermissionNode[]): PermissionNode[] {
  if (!hospitalModeStore.isGrassroots) return nodes
  return nodes
    .map((node) => {
      const children = node.子级菜单 || []
      if (children.length > 0) {
        const filtered = filterTreeByHospitalMode(children)
        return { ...node, 子级菜单: filtered }
      } else {
        const route = getMenuRoute(node.菜单)
        if (hospitalModeStore.isRouteHidden(route)) {
          return null
        }
        return node
      }
    })
    .filter((n): n is PermissionNode => {
      if (!n) return false
      if (n.子级菜单 && n.子级菜单.length === 0 && !getMenuRoute(n.菜单)) {
        return false
      }
      return true
    })
}

const rawPermissionsList = computed<PermissionNode[]>(() => {
  const base = Array.isArray(userStore.permissions) && userStore.permissions.length > 0
    ? userStore.permissions
    : defaultTreePermissions.value
  return filterTreeByHospitalMode(base)
})

const effectivePermissions = computed<PermissionNode[]>(() => {
  return menuOrderStore.sortPermissionNodes(rawPermissionsList.value)
})

const allLeafMenus = computed<LeafMenuItem[]>(() => {
  const result: LeafMenuItem[] = []

  function traverse(nodes: any[], parentPath: string) {
    if (!Array.isArray(nodes)) return
    for (const node of nodes) {
      const menu = node.菜单 || node
      const displayName = getMenuDisplayName(menu)
      const currentPath = parentPath ? `${parentPath} / ${displayName}` : displayName
      const children = node.子级菜单 || []

      if (children.length === 0) {
        const targetRoute = getMenuRoute(menu)
        if (targetRoute && targetRoute !== '#') {
          result.push({
            id: menu.编号 || targetRoute,
            name: displayName,
            route: targetRoute,
            categoryPath: parentPath || '全局功能',
            icon: getMenuIcon(menu.名称)
          })
        }
      } else {
        traverse(children, currentPath)
      }
    }
  }

  traverse(userStore.permissions, '')

  // 若权限树尚未加载完成，提供完整兜底目录保证立即可查
  if (result.length === 0) {
    return defaultCatalogLeafMenus
  }

  // 去重
  const seen = new Set<string>()
  const merged: LeafMenuItem[] = []
  for (const item of [...result, ...defaultCatalogLeafMenus]) {
    if (!seen.has(item.route)) {
      seen.add(item.route)
      merged.push(item)
    }
  }
  return merged.filter((item) => !hospitalModeStore.isRouteHidden(item.route))
  })

const filteredLeafMenus = computed(() => {
  const q = searchKeyword.value.trim().toLowerCase()
  if (!q) return []
  return allLeafMenus.value.filter(
    (item) =>
      item.name.toLowerCase().includes(q) ||
      item.categoryPath.toLowerCase().includes(q) ||
      item.route.toLowerCase().includes(q)
  )
})

// 平铺大纲视图的分组
const groupedFlatCatalog = computed(() => {
  const groups: Record<string, LeafMenuItem[]> = {}
  for (const item of allLeafMenus.value) {
    const mainCat = item.categoryPath.split('/')[0].trim() || '通用业务'
    if (!groups[mainCat]) {
      groups[mainCat] = []
    }
    groups[mainCat].push(item)
  }

  const categories = Object.keys(groups)
  const rootOrder = menuOrderStore.customOrderMap['_root']
  if (rootOrder && Array.isArray(rootOrder) && rootOrder.length > 0) {
    const rankMap = new Map<string, number>()
    rootOrder.forEach((name, idx) => rankMap.set(name, idx))
    categories.sort((a, b) => {
      const rankA = rankMap.has(a) ? rankMap.get(a)! : 9999
      const rankB = rankMap.has(b) ? rankMap.get(b)! : 9999
      return rankA - rankB
    })
  }

  return categories.map((key) => ({
    category: key,
    items: groups[key]
  }))
})

// 3. 收集所有可展开的子菜单 ID（树形模式）与分组 Key（平铺模式）
const allSubMenuIds = computed<string[]>(() => {
  const ids: string[] = []
  if (favoritesStore.favorites.length > 0) {
    ids.push('favorites')
  }
  function collect(nodes: any[]) {
    if (!Array.isArray(nodes)) return
    for (const node of nodes) {
      if (node.子级菜单 && node.子级菜单.length > 0) {
        ids.push(String(node.菜单.编号))
        collect(node.子级菜单)
      }
    }
  }
  collect(effectivePermissions.value)
  return ids
})

const allFlatSectionKeys = computed<string[]>(() => {
  const keys: string[] = []
  if (favoritesStore.favorites.length > 0) {
    keys.push('favorites')
  }
  for (const grp of groupedFlatCatalog.value) {
    keys.push(grp.category)
  }
  return keys
})

const openedMenus = ref<string[]>(['favorites'])

const isAllExpanded = computed<boolean>(() => {
  if (viewMode.value === 'flat') {
    return collapsedFlatSections.value.length === 0
  }
  return (
    allSubMenuIds.value.length > 0 &&
    allSubMenuIds.value.every((id) => openedMenus.value.includes(id))
  )
})

const isAllCollapsed = computed<boolean>(() => {
  if (viewMode.value === 'flat') {
    return (
      allFlatSectionKeys.value.length > 0 &&
      allFlatSectionKeys.value.every((k) => collapsedFlatSections.value.includes(k))
    )
  }
  return openedMenus.value.length === 0
})

function handleExpandAll() {
  if (searchKeyword.value.trim()) {
    searchKeyword.value = ''
  }
  openedMenus.value = [...allSubMenuIds.value]
  collapsedFlatSections.value = []
  menuKey.value++
  ElMessage.success({
    message: `已展开全部菜单分组 (${viewMode.value === 'flat' ? allFlatSectionKeys.value.length : allSubMenuIds.value.length} 个分组)`,
    duration: 1400,
    grouping: true
  })
}

function handleCollapseAll() {
  if (searchKeyword.value.trim()) {
    searchKeyword.value = ''
  }
  openedMenus.value = []
  collapsedFlatSections.value = [...allFlatSectionKeys.value]
  menuKey.value++
  ElMessage.info({
    message: '已折叠收起全部菜单分组',
    duration: 1400,
    grouping: true
  })
}

function onSubMenuOpen(index: string) {
  if (!openedMenus.value.includes(index)) {
    openedMenus.value.push(index)
  }
}

function onSubMenuClose(index: string) {
  openedMenus.value = openedMenus.value.filter((item) => item !== index)
}

function isFlatSectionCollapsed(sectionKey: string): boolean {
  return collapsedFlatSections.value.includes(sectionKey)
}

function toggleFlatSection(sectionKey: string) {
  if (collapsedFlatSections.value.includes(sectionKey)) {
    collapsedFlatSections.value = collapsedFlatSections.value.filter((k) => k !== sectionKey)
  } else {
    collapsedFlatSections.value.push(sectionKey)
  }
}

function toggleViewMode() {
  viewMode.value = viewMode.value === 'tree' ? 'flat' : 'tree'
  localStorage.setItem('scientific_research_menu_view_mode', viewMode.value)
  ElMessage.success({
    message: viewMode.value === 'flat' ? '已切换为平铺大纲模式' : '已切换为树形折叠模式',
    duration: 1200,
    grouping: true
  })
}

// 根据图标名称反查组件
function getIconByName(iconName?: string) {
  switch (iconName) {
    case 'Files': return Files
    case 'TrendCharts': return TrendCharts
    case 'Collection': return Collection
    case 'Histogram': return Histogram
    case 'Money': return Money
    case 'Reading': return Reading
    case 'Document': return Document
    case 'User': return User
    case 'Medal': return Medal
    case 'House': return House
    case 'Setting': return Setting
    case 'Bell': return Bell
    case 'Download': return Download
    case 'Key': return Key
    default: return Document
  }
}

// 根据菜单名称匹配合适的 Element 图标
function getMenuIcon(name: string) {
  if (name.includes('办公')) return Briefcase
  if (name.includes('纵向')) return TrendCharts
  if (name.includes('横向')) return Collection
  if (name.includes('经费')) return Histogram
  if (name.includes('成果')) return Reading
  if (name.includes('活动')) return Reading
  if (name.includes('统计')) return Histogram
  if (name.includes('组织')) return User
  if (name.includes('系统')) return Setting
  if (name.includes('基础')) return Files
  return Folder
}

// 格式化菜单路径，全面接管转换为现代路由
function getMenuDisplayName(menu: MenuItem): string {
  return getMenuRoute(menu) === '/office/research-platform' ? '网上科研服务' : menu.名称
}

function getMenuRoute(menu: MenuItem): string {
  if (!menu.路径) return '#'

  const modern = resolveModernRoute(menu.路径)
  if (modern) {
    return modern
  }

  if (menu.路径.startsWith('http') || menu.路径.includes('.html')) {
    const rawPath = menu.路径.startsWith('/') ? menu.路径 : `/IMIS/views/${menu.路径}`
    return `/iframe?url=${encodeURIComponent(rawPath)}&title=${encodeURIComponent(menu.名称)}`
  }
  return menu.路径.startsWith('/') ? menu.路径 : `/${menu.路径}`
}

function onMenuSelect(menu: MenuItem) {
  if (!menu.路径) return
  const targetRoute = getMenuRoute(menu)
  if (targetRoute !== '#') {
    router.push(targetRoute)
  }
}

function onItemClick(routePath: string) {
  if (!routePath || routePath === '#') return
  router.push(routePath)
}

function onToggleFavorite(item: any) {
  favoritesStore.toggleFavorite({
    name: item.name,
    path: item.path
  })
}

onMounted(() => {
  hospitalModeStore.loadConfig()
  // Refresh the server-filtered permission tree whenever the main layout is
  // mounted.  A role/menu grant can change while an OIDC session remains in
  // sessionStorage; only refreshing when the tree is empty would leave a
  // stale sidebar until the user manually cleared browser storage.
  if (userStore.token) userStore.fetchUserInfo().catch(() => {})
})
</script>

<style scoped lang="scss">
.sidebar-container {
  width: 240px;
  height: 100%;
  background-color: #304156;
  display: flex;
  flex-direction: column;
  transition: width 0.28s;
  user-select: none;

  &.collapsed {
    width: 64px;
  }

  .logo {
    height: 56px;
    display: flex;
    align-items: center;
    padding: 0 16px;
    background-color: #2b2f3a;
    color: #ffffff;
    overflow: hidden;

    .logo-icon {
      color: #409eff;
      flex-shrink: 0;
    }

    .logo-title {
      margin-left: 12px;
      font-size: 16px;
      font-weight: 600;
      white-space: nowrap;
    }
  }

  .sidebar-toolbar {
    padding: 8px 10px;
    background: #263445;
    border-bottom: 1px solid #1f2d3d;
    display: flex;
    flex-direction: column;
    gap: 6px;

    .menu-search-input {
      :deep(.el-input__wrapper) {
        background-color: #1f2d3d;
        box-shadow: none;
        border: 1px solid #3b4d61;

        &:hover,
        &.is-focus {
          border-color: #409eff;
        }

        .el-input__inner {
          color: #e2e8f0;
          font-size: 12px;

          &::placeholder {
            color: #94a3b8;
          }
        }
      }
    }

    .tools-row {
      display: flex;
      justify-content: space-between;
      gap: 4px;
      flex-wrap: nowrap;

      .tool-btn {
        flex: 1;
        min-width: 0;
        white-space: nowrap;
        flex-shrink: 0;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: 2px;
        background: #1f2d3d;
        border: 1px solid #3b4d61;
        color: #cbd5e1;
        font-size: 11px;
        height: 26px;
        padding: 0 4px;
        border-radius: 4px;
        cursor: pointer;
        transition: all 0.2s;

        &:hover {
          color: #ffffff;
          background: #334155;
          border-color: #60a5fa;
        }

        &:active {
          transform: scale(0.96);
        }

        &.active {
          background: #2563eb;
          color: #ffffff;
          border-color: #60a5fa;
          font-weight: 600;
          box-shadow: 0 0 0 1px rgba(96, 165, 250, 0.3);
        }

        &.map-btn {
          color: #fbbf24;
          border-color: #d97706;

          &:hover {
            background: #b45309;
            color: #ffffff;
          }
        }

        &.sort-btn {
          color: #38bdf8;
          border-color: #0284c7;

          &:hover {
            background: #0369a1;
            color: #ffffff;
          }

          &.active {
            background: #0284c7;
            color: #ffffff;
            border-color: #38bdf8;
            box-shadow: 0 0 0 1px rgba(56, 189, 248, 0.4);
          }
        }
      }
    }
  }

  .menu-scrollbar {
    flex: 1;

    :deep(.el-menu) {
      border-right: none;
    }

    :deep(.el-sub-menu__title),
    :deep(.el-menu-item) {
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    :deep(.el-menu-item.is-active-fav) {
      color: #409eff !important;
      background-color: #263445 !important;
      font-weight: 600;
    }
  }
}

.sub-menu-label {
  display: inline-block;
  max-width: 145px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  vertical-align: middle;
}

// 搜索结果列表样式
.search-results-list {
  padding: 8px;

  .results-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 4px 8px 8px;
    font-size: 11px;
    color: #94a3b8;
    border-bottom: 1px solid #374151;
    margin-bottom: 6px;
    white-space: nowrap;
  }

  .flat-nav-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 8px 10px;
    border-radius: 4px;
    cursor: pointer;
    margin-bottom: 4px;
    transition: background-color 0.2s;

    &:hover {
      background-color: #263445;

      .star-fav-btn {
        opacity: 1;
      }
    }

    &.active {
      background-color: #1e3a5f;

      .item-name {
        color: #60a5fa;
        font-weight: 600;
      }
    }

    .item-main {
      display: flex;
      align-items: center;
      gap: 8px;
      overflow: hidden;
      min-width: 0;
      flex: 1;

      .item-icon {
        color: #94a3b8;
        font-size: 15px;
        flex-shrink: 0;
      }

      .item-text {
        display: flex;
        flex-direction: column;
        overflow: hidden;
        min-width: 0;

        .item-name {
          font-size: 13px;
          color: #e2e8f0;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }

        .item-crumbs {
          font-size: 10px;
          color: #94a3b8;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }
      }
    }

    .star-fav-btn {
      color: #64748b;
      font-size: 14px;
      flex-shrink: 0;
      cursor: pointer;
      opacity: 0.6;
      transition: all 0.2s;

      &:hover {
        color: #f59e0b;
        opacity: 1;
      }

      &.starred {
        color: #f59e0b;
        opacity: 1;
      }
    }
  }

  .no-result {
    padding: 30px 10px;
    text-align: center;
    font-size: 12px;
    color: #94a3b8;
  }
}

// 平铺大纲视图样式
.flat-outline-container {
  padding: 6px 8px;

  .dashboard-item {
    background: #1f2d3d;
    border-radius: 4px;
    margin-bottom: 8px;
  }

  .outline-section {
    margin-bottom: 8px;

    .section-title {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 6px;
      font-size: 11px;
      font-weight: 700;
      color: #94a3b8;
      padding: 6px 8px;
      letter-spacing: 0.5px;
      border-bottom: 1px solid #2d3748;
      margin-bottom: 4px;
      border-radius: 4px;
      white-space: nowrap;
      transition: background-color 0.15s, color 0.15s;

      &.clickable {
        cursor: pointer;

        &:hover {
          background-color: #263445;
          color: #e2e8f0;
        }
      }

      .section-title-left {
        display: flex;
        align-items: center;
        gap: 5px;
        min-width: 0;
        overflow: hidden;

        .section-arrow {
          font-size: 12px;
          transition: transform 0.2s ease;
          flex-shrink: 0;

          &.collapsed {
            transform: rotate(-90deg);
          }
        }

        .section-name {
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }
      }

      .section-badge {
        font-size: 10px;
        background: #374151;
        color: #e2e8f0;
        padding: 0 5px;
        border-radius: 8px;
        flex-shrink: 0;
      }
    }
  }

  .outline-leaf-item {
    display: flex;
    align-items: center;
    padding: 7px 10px;
    border-radius: 4px;
    color: #cbd5e1;
    font-size: 12px;
    cursor: pointer;
    margin-bottom: 2px;
    transition: all 0.15s;
    white-space: nowrap;

    &:hover {
      background-color: #263445;
      color: #ffffff;

      .unpin-icon,
      .star-fav-btn {
        opacity: 1;
      }
    }

    &.active {
      background-color: #1e3a5f;
      color: #60a5fa;
      font-weight: 600;
    }

    .leaf-icon {
      font-size: 14px;
      margin-right: 8px;
      flex-shrink: 0;
    }

    .leaf-title {
      flex: 1;
      min-width: 0;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .unpin-icon {
      font-size: 12px;
      color: #94a3b8;
      opacity: 0.4;
      cursor: pointer;
      flex-shrink: 0;
      margin-left: 4px;

      &:hover {
        color: #ef4444;
      }
    }

    .star-fav-btn {
      font-size: 13px;
      color: #64748b;
      opacity: 0.3;
      cursor: pointer;
      flex-shrink: 0;
      margin-left: 4px;

      &:hover {
        color: #f59e0b;
        opacity: 1;
      }

      &.starred {
        color: #f59e0b;
        opacity: 1;
      }
    }
  }
}

// 传统树形菜单下的常用功能与星星
.fav-menu-inner {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
  min-width: 0;
  gap: 6px;

  .menu-item-text {
    flex: 1;
    min-width: 0;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .remove-fav-btn {
    font-size: 12px;
    color: #94a3b8;
    margin-right: 4px;
    cursor: pointer;
    flex-shrink: 0;

    &:hover {
      color: #ef4444;
    }
  }
}

.tree-menu-title-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
  min-width: 0;
  gap: 6px;

  .menu-item-text {
    flex: 1;
    min-width: 0;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .tree-star-icon {
    font-size: 13px;
    color: #64748b;
    margin-right: 4px;
    opacity: 0.25;
    transition: opacity 0.2s;
    flex-shrink: 0;

    &:hover {
      color: #f59e0b;
      opacity: 1;
    }

    &.starred {
      color: #f59e0b;
      opacity: 1;
    }
  }

  &:hover {
    .tree-star-icon {
      opacity: 0.85;
    }
  }
}
</style>
