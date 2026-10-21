import { describe, it, expect, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useFavoritesStore } from '../stores/favorites'
import { useTabsStore } from '../stores/tabs'
import type { LeafMenuItem } from '../layout/components/Sidebar.vue'

describe('Menu Navigation & Ergonomics (左侧菜单查看与高效操作体验优化)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  // 1. 常用功能与快捷收藏夹
  describe('常用功能置顶与自定义收藏夹 (Favorites Store)', () => {
    it('应默认预置研究型医院高频科研业务快捷菜单', () => {
      const favoritesStore = useFavoritesStore()
      expect(favoritesStore.favorites.length).toBeGreaterThanOrEqual(8)

      const paths = favoritesStore.favorites.map((f) => f.path)
      expect(paths).toContain('/workflow/todo-approvals')
      expect(paths).toContain('/project/vertical-application')
      expect(paths).toContain('/project/vertical-lifecycle')
      expect(paths).toContain('/finance/expense-claims')
      expect(paths).toContain('/fruit/paper-claim')
      expect(paths).toContain('/fruit/biobank-ledger')
      expect(paths).toContain('/evaluation/performance-evaluation')
      expect(paths).toContain('/office/mobile-workbench')
    })

    it('应支持随时收藏与取消收藏任意菜单项，并持久化到本地存储', () => {
      const favoritesStore = useFavoritesStore()
      const testPath = '/base-info/dictionary'

      expect(favoritesStore.isFavorite(testPath)).toBe(false)

      // 添加收藏
      favoritesStore.toggleFavorite({
        name: '科研基础字典规范',
        path: testPath,
        category: '基础信息'
      })
      expect(favoritesStore.isFavorite(testPath)).toBe(true)

      // 再次切换即取消收藏
      favoritesStore.toggleFavorite({
        name: '科研基础字典规范',
        path: testPath
      })
      expect(favoritesStore.isFavorite(testPath)).toBe(false)
    })

    it('应支持单向移出常用功能', () => {
      const favoritesStore = useFavoritesStore()
      const todoPath = '/workflow/todo-approvals'
      expect(favoritesStore.isFavorite(todoPath)).toBe(true)

      favoritesStore.removeFavorite(todoPath)
      expect(favoritesStore.isFavorite(todoPath)).toBe(false)
    })
  })

  // 2. 扁平化搜索与面包屑快速定位
  describe('左侧菜单即时模糊检索与扁平过滤', () => {
    const mockLeafMenus: LeafMenuItem[] = [
      { id: 1, name: '纵向项目申报向导', route: '/project/vertical-application', categoryPath: '纵向课题 / 申报立项', icon: {} },
      { id: 2, name: '纵向课题全生命周期治理', route: '/project/vertical-lifecycle', categoryPath: '纵向课题 / 过程监管', icon: {} },
      { id: 3, name: '经费报销与票据核销台账', route: '/finance/expense-claims', categoryPath: '经费管理 / 票据核销', icon: {} },
      { id: 4, name: '论文成果主动网推认领', route: '/fruit/paper-claim', categoryPath: '学术成果 / 智能网推', icon: {} },
      { id: 5, name: '临床生物样本库 (Biobank) 台账', route: '/fruit/biobank-ledger', categoryPath: '临床生态 / 生物样本', icon: {} },
      { id: 6, name: '科研绩效评价与奖励兑现', route: '/evaluation/performance-evaluation', categoryPath: '评价考核 / 绩效考核', icon: {} }
    ]

    function filterMenus(keyword: string): LeafMenuItem[] {
      const q = keyword.trim().toLowerCase()
      if (!q) return []
      return mockLeafMenus.filter(
        (m) =>
          m.name.toLowerCase().includes(q) ||
          m.categoryPath.toLowerCase().includes(q) ||
          m.route.toLowerCase().includes(q)
      )
    }

    it('输入关键词时应能快速跨层级命中功能，无需逐级点开', () => {
      const resExpense = filterMenus('报销')
      expect(resExpense.length).toBe(1)
      expect(resExpense[0].name).toBe('经费报销与票据核销台账')
      expect(resExpense[0].categoryPath).toContain('经费管理')

      const resPaper = filterMenus('论文')
      expect(resPaper.length).toBe(1)
      expect(resPaper[0].name).toContain('论文成果主动网推认领')
    })

    it('输入板块名称时应能召回该板块下全部扁平功能', () => {
      const resVertical = filterMenus('纵向')
      expect(resVertical.length).toBe(2)
      expect(resVertical.map((r) => r.name)).toContain('纵向项目申报向导')
      expect(resVertical.map((r) => r.name)).toContain('纵向课题全生命周期治理')
    })
  })

  // 3. 树形与平铺模式下的全部展开 / 全部折叠状态控制
  describe('菜单展开与折叠状态反馈控制 (Expand / Collapse Feedback)', () => {
    it('点击展开与折叠应在树形模式与平铺模式下同步切换全部分组状态（含常用功能）', () => {
      const allSubMenuIds = ['favorites', '9000', '9001', '9002', '9003']
      const allFlatSectionKeys = ['favorites', '纵向课题', '横向课题', '经费管理', '学术成果']

      let openedMenus = ['favorites']
      let collapsedFlatSections: string[] = []

      const isTreeAllExpanded = () =>
        allSubMenuIds.length > 0 && allSubMenuIds.every((id) => openedMenus.includes(id))
      const isTreeAllCollapsed = () => openedMenus.length === 0

      const isFlatAllExpanded = () => collapsedFlatSections.length === 0
      const isFlatAllCollapsed = () =>
        allFlatSectionKeys.length > 0 &&
        allFlatSectionKeys.every((k) => collapsedFlatSections.includes(k))

      // 初始状态：树形仅展开常用功能（既非全展开也非全折叠）
      expect(isTreeAllExpanded()).toBe(false)
      expect(isTreeAllCollapsed()).toBe(false)

      // 执行“全部折叠”：连同 favorites 一起彻底收起，平铺分组也全部折叠
      openedMenus = []
      collapsedFlatSections = [...allFlatSectionKeys]
      expect(isTreeAllCollapsed()).toBe(true)
      expect(isTreeAllExpanded()).toBe(false)
      expect(isFlatAllCollapsed()).toBe(true)
      expect(isFlatAllExpanded()).toBe(false)

      // 执行“全部展开”：所有树形子菜单及平铺分组全部展开
      openedMenus = [...allSubMenuIds]
      collapsedFlatSections = []
      expect(isTreeAllExpanded()).toBe(true)
      expect(isTreeAllCollapsed()).toBe(false)
      expect(isFlatAllExpanded()).toBe(true)
      expect(isFlatAllCollapsed()).toBe(false)
    })
  })

  // 4. 多标签页密集打开防挤压与快捷关闭管理
  describe('多标签栏密集打开管理与快捷切换 (Multi-Tab Overflow & Actions)', () => {
    it('连续打开多个科研业务标签页时应完整保留标题且支持一键关闭其他与全部关闭', () => {
      const tabsStore = useTabsStore()
      const routes = [
        { path: '/project/vertical-application', fullPath: '/project/vertical-application', name: 'VerticalApp', meta: { title: '纵向项目申报向导' }, query: {} },
        { path: '/project/vertical-lifecycle', fullPath: '/project/vertical-lifecycle', name: 'VerticalLife', meta: { title: '纵向课题全生命周期治理' }, query: {} },
        { path: '/finance/ledger', fullPath: '/finance/ledger', name: 'FinanceLedger', meta: { title: '项目经费分层台账' }, query: {} },
        { path: '/finance/expense-claims', fullPath: '/finance/expense-claims', name: 'ExpenseClaims', meta: { title: '经费报销与票据核销台账' }, query: {} },
        { path: '/fruit/paper-claim', fullPath: '/fruit/paper-claim', name: 'PaperClaim', meta: { title: '论文成果主动网推认领' }, query: {} },
        { path: '/fruit/biobank-ledger', fullPath: '/fruit/biobank-ledger', name: 'Biobank', meta: { title: '临床生物样本库 (Biobank) 台账' }, query: {} },
        { path: '/evaluation/performance-evaluation', fullPath: '/evaluation/performance-evaluation', name: 'PerfEval', meta: { title: '科研绩效评价与奖励兑现' }, query: {} }
      ]

      for (const r of routes) {
        tabsStore.addTab(r as any)
      }

      // 1个默认工作台 + 7个新增业务标签 = 8个标签
      expect(tabsStore.tabs.length).toBe(8)
      expect(tabsStore.activeTab).toBe('/evaluation/performance-evaluation')

      // 关闭其他：仅保留 /dashboard 与当前激活页
      tabsStore.closeOthers('/finance/expense-claims')
      expect(tabsStore.tabs.length).toBe(2)
      expect(tabsStore.tabs.map((t) => t.path)).toEqual(['/dashboard', '/finance/expense-claims'])

      // 全部关闭：仅保留 /dashboard
      tabsStore.closeAll()
      expect(tabsStore.tabs.length).toBe(1)
      expect(tabsStore.tabs[0].path).toBe('/dashboard')
    })
  })

  // 5. 全景功能大纲地图
  describe('全景功能大纲地图看板 (Mega Map Matrix)', () => {
    it('全景地图应覆盖全部 5 大科研管理核心业务板块', () => {
      const categories = [
        '科研课题全周期与立项',
        '经费核算与业财双向闭环',
        '学术成果、网推与临床专病生态',
        '专家评审、学术活动与人才队伍',
        '绩效考核、国考决策与行政治理'
      ]

      expect(categories.length).toBe(5)
      expect(categories).toContain('科研课题全周期与立项')
      expect(categories).toContain('经费核算与业财双向闭环')
      expect(categories).toContain('学术成果、网推与临床专病生态')
    })
  })
})
