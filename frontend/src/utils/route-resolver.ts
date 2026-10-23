/**
 * 路由解析与现代前端接管中心 (Route Resolver)
 * 将数据库/旧版系统中的历史菜单路径（含 URL 编码、参数化路径及旧版 views 相对路径）
 * 映射接管至 Vue 3 现代 SPA 路由。
 */

export const modernRouteMap: Record<string, string> = {
  // 科研办公与个人中心 (Office)
  '科研办公/科研办公/通知公告.html': '/office/notice',
  '科研办公/科研办公/添加通知公告.html': '/office/notice?action=add',
  '科研办公/科研办公/通知公告详情.html': '/office/notice',
  '科研办公/科研办公/下载中心.html': '/office/downloads',
  '科研办公/科研办公/添加下载.html': '/office/downloads?action=add',
  '科研办公/科研办公/下载详情.html': '/office/downloads',
  '科研办公/科研办公/个人资料.html': '/office/profile',
  '科研办公/科研办公/修改密码.html': '/office/profile?tab=security',
  '科研办公/我的科研/科研详情.html': '/office/research-portfolio',
  '科研办公/我的科研/科研待办.html': '/workflow/todo-approvals',
  '科研办公/科研办公/移动科研工作台.html': '/office/mobile-workbench',
  '科研办公/科研办公/人员进修管理.html': '/office/further-education',
  '科研办公/科研办公/进修年度计划.html': '/office/further-education?tab=plans',
  '科研办公/科研办公/进修申请.html': '/office/further-education?tab=applications',
  '科研办公/科研办公/进修待办.html': '/office/further-education?tab=todo',
  '进修管理/年度计划.html': '/office/further-education?tab=plans',
  '进修管理/个人申请.html': '/office/further-education?tab=applications',
  '科研办公/科研办公/科研治理中心.html': '/operations/external-sync',
  '科研办公/科研办公/权限代理与预算对账.html': '/governance/proxy-reconciliation',
  '科研办公/科研办公/选择用户.html': '/organization/people?tab=users',
  '科研办公/科研办公/科研评价管理.html': '/evaluation/performance-evaluation',
  '科研办公/科研办公/横向项目流程.html': '/project/horizontal-contract',
  '科研办公/科研办公/项目生命周期.html': '/project/vertical-lifecycle',
  '科研办公/科研办公/科研伦理审查.html': '/ethics/review-workflow',
  '伦理审查/科研伦理审查.html': '/ethics/review-workflow',
  '科研办公/科研办公/纵向项目申报.html': '/project/vertical-application',
  '科研办公/科研办公/纵向项目申报.html?category=1': '/project/vertical-application',
  '科研办公/科研办公/纵向项目申报.html?category=2': '/project/vertical-application',
  '科研办公/科研办公/纵向项目申报.html?category=3': '/project/vertical-application',
  '科研办公/科研办公/纵向项目申报计划.html': '/project/vertical-application?tab=plans',

  // 扩展科研办公与专项治理模块
  '科研办公/科研办公/科研平台与服务目录.html': '/office/research-platform',
  '科研办公/科研办公/网上科研服务.html': '/office/research-platform',
  '科研办公/科研办公/院级项目批次.html': '/project/vertical-application?tab=plans',
  '科研办公/科研办公/教育部科研统计.html': '/office/education-statistics',
  '科研办公/科研办公/成果与知识产权.html': '/fruit/achievement-ip-awards',
  '科研办公/科研办公/首页组件与业务催办.html': '/dashboard',
  '科研办公/科研办公/科研文档共享与消息中心.html': '/office/document-sharing',
  '科研办公/科研办公/横向合同治理.html': '/project/horizontal-contract?tab=governance',
  '科研办公/科研办公/项目分类与预算模板.html': '/project/project-configuration',
  '科研办公/科研办公/纵向项目导入.html': '/project/vertical-import',
  '科研办公/科研办公/经费执行治理.html': '/finance/fund-execution',
  '科研办公/科研办公/仪器设备与开放预约.html': '/office/research-platform?tab=instruments',
  '科研办公/科研办公/学术交流治理.html': '/activities/academic-exchange',
  '科研办公/科研办公/期刊分区库.html': '/fruit/journal-partition',
  '科研办公/科研办公/论文投稿备案.html': '/fruit/paper-submission',
  '科研办公/科研办公/继续医学教育学分.html': '/office/cme-credits',

  // 统一业务台账参数化接管
  '科研办公/科研办公/科研业务台账.html': '/project/vertical-lifecycle',
  '科研办公/科研办公/科研业务台账.html?module=y-application': '/project/vertical-application',
  '科研办公/科研办公/科研业务台账.html?module=y-approval': '/project/vertical-lifecycle?tab=approvals',
  '科研办公/科研办公/科研业务台账.html?module=y-midterm': '/project/vertical-lifecycle?tab=midterms',
  '科研办公/科研办公/科研业务台账.html?module=y-change': '/project/vertical-lifecycle?tab=changes',
  '科研办公/科研办公/科研业务台账.html?module=y-close': '/project/vertical-lifecycle?tab=closeouts',
  '科研办公/科研办公/科研业务台账.html?module=y-finished': '/project/vertical-lifecycle?tab=closeouts',
  '科研办公/科研办公/科研业务台账.html?module=x-list': '/project/horizontal-contract',
  '科研办公/科研办公/科研业务台账.html?module=x-change': '/project/horizontal-contract',
  '科研办公/科研办公/科研业务台账.html?module=x-close': '/project/horizontal-contract',
  '科研办公/科研办公/科研业务台账.html?module=y-income': '/finance/incoming',
  '科研办公/科研办公/科研业务台账.html?module=x-income': '/finance/incoming?tab=horizontal',
  '科研办公/科研办公/科研业务台账.html?module=y-expense': '/finance/expense-claims?tab=y-expense',
  '科研办公/科研办公/科研业务台账.html?module=x-expense': '/finance/expense-claims?tab=x-expense',
  '科研办公/科研办公/科研业务台账.html?module=bill-search': '/finance/expense-claims?tab=bills',
  '科研办公/科研办公/生物样本库.html': '/office/biobank-ledger',
  '科研办公/科研办公/生物样本台账.html': '/office/biobank-ledger',
  '科研办公/科研办公/科研业务台账.html?module=biobank': '/office/biobank-ledger',

  // 组织架构与人事
  '组织架构/部门人员信息维护.html': '/organization/people',
  '组织架构/科研人员.html': '/organization/people',
  '组织架构/添加部门.html': '/organization/people?action=add-dept',
  '组织架构/添加用户.html': '/organization/people?action=add-user',
  '组织架构/专家评审工作台.html': '/evaluation/expert-workbench',
  '组织架构/员工学术任职情况.html': '/organization/academic-appointments?tab=appointments',
  '组织架构/专家人才.html': '/organization/academic-appointments?tab=experts',
  '组织架构/专家遴选.html': '/organization/academic-appointments?tab=selection',
  '组织架构/人员任职申请.html': '/organization/academic-appointments?tab=my_appointments',
  '组织架构/添加学术任职.html': '/organization/academic-appointments?tab=my_appointments',
  '组织架构/学术任职详情.html': '/organization/academic-appointments?tab=appointments',

  // 纵向项目
  '纵向项目/项目申报立项/纵向项目申报.html': '/project/vertical-application',
  '纵向项目/在研项目/项目中检.html': '/project/vertical-lifecycle?tab=midterms',
  '纵向项目/在研项目/项目变更.html': '/project/vertical-lifecycle?tab=changes',
  '纵向项目/在研项目/项目结题.html': '/project/vertical-lifecycle?tab=closeouts',
  '纵向项目/已结项目/已结项目.html': '/project/vertical-lifecycle?tab=closeouts',

  // 横向项目
  '横向项目/项目立项/横向项目申报.html': '/project/horizontal-contract',
  '横向项目/横向项目管理/横向项目列表.html': '/project/horizontal-contract',
  '横向项目/横向项目管理/横向项目变更.html': '/project/horizontal-contract?tab=changes',
  '横向项目/横向项目管理/横向项目结题.html': '/project/horizontal-contract?tab=closeouts',

  // 项目经费管理
  '项目经费管理/纵向经费管理/纵向项目经费合计.html': '/finance/ledger',
  '项目经费管理/纵向经费管理/经费到账管理.html': '/finance/incoming',
  '项目经费管理/纵向经费管理/经费到账列表.html': '/finance/incoming',
  '项目经费管理/纵向经费管理/经费报销管理.html': '/finance/expense-claims?tab=y-expense',
  '项目经费管理/纵向经费管理/纵向项目报销.html': '/finance/expense-claims?tab=y-expense',
  '项目经费管理/纵向经费管理/经费报销统计.html': '/finance/expense-claims?tab=y-expense',
  '项目经费管理/横向经费管理/经费到账管理.html': '/finance/incoming?tab=horizontal',
  '项目经费管理/横向经费管理/经费到账认领.html': '/finance/incoming?tab=horizontal',
  '项目经费管理/横向经费管理/经费支出申请.html': '/finance/expense-claims?tab=x-expense',
  '项目经费管理/横向经费管理/经费预算管理.html': '/finance/ledger',
  '项目经费管理/横向经费管理/经费报销管理.html': '/finance/expense-claims?tab=x-expense',
  '项目经费管理/经费浮动调整/纵向预算浮动.html': '/governance/proxy-reconciliation',
  '项目经费管理/经费浮动调整/横向预算浮动.html': '/governance/proxy-reconciliation',
  '项目经费管理/单据检索/账单查找.html': '/finance/expense-claims?tab=bills',

  // 成果管理
  '成果管理/成果业务台账.html': '/fruit/achievement-ledger',
  '成果管理/成果业务台账.html?module=overview': '/fruit/achievement-ledger?tab=overview',
  '成果管理/成果业务台账.html?module=work': '/fruit/achievement-ledger?tab=work',
  '成果管理/成果业务台账.html?module=patent': '/fruit/achievement-ledger?tab=patent',
  '成果管理/成果业务台账.html?module=award': '/fruit/achievement-ledger?tab=award',
  '成果管理/论文/认领论文.html': '/fruit/paper-claim',
  '成果管理/论文/论文认领.html': '/fruit/paper-claim',
  '成果管理/论文/论文管理.html': '/fruit/paper-management',
  '成果管理/论文/论文详情.html': '/fruit/paper-management',
  '成果管理/论文/论文采集任务.html': '/fruit/paper-management?tab=sync',
  '成果管理/成果/成果登记.html': '/fruit/achievement-ledger?action=add-achievement',
  '成果管理/成果/鉴定成果.html': '/fruit/achievement-ledger?tab=overview',
  '成果管理/著作/著作管理.html': '/fruit/achievement-ledger?tab=work',
  '成果管理/专利/专利管理.html': '/fruit/achievement-ledger?tab=patent',
  '成果管理/专利/软件专利管理.html': '/fruit/achievement-ledger?tab=patent',
  '成果管理/文体/文体成果管理.html': '/fruit/achievement-ledger?tab=overview',
  '成果管理/获奖/获奖管理.html': '/fruit/achievement-ledger?tab=award',
  '成果管理/成果总览/成果总览.html': '/fruit/achievement-ledger?tab=overview',
  '成果管理/成果导入/成果导入.html': '/fruit/achievement-ledger?action=import',

  // 学术活动
  '学术活动/申请学术讲座/主办讲座.html': '/activities/academic-activities?tab=lectures',
  '学术活动/申请学术讲座/主办讲座申请.html': '/activities/academic-activities?action=apply-lecture',
  '学术活动/申请学术讲座/主办讲座详情.html': '/activities/academic-activities?tab=lectures',
  '学术活动/申请学术讲座/讲座反馈.html': '/activities/academic-activities?tab=lecture-feedbacks',
  '学术活动/申请学术讲座/讲座反馈申请.html': '/activities/academic-activities?action=lecture-feedback',
  '学术活动/申请学术讲座/讲座反馈详情.html': '/activities/academic-activities?tab=lecture-feedbacks',
  '学术活动/参加学术会议/参会信息管理.html': '/activities/academic-activities?tab=conferences',
  '学术活动/参加学术会议/参加会议申请.html': '/activities/academic-activities?action=apply-conf',
  '学术活动/参加学术会议/参加会议详情.html': '/activities/academic-activities?tab=conferences',
  '学术活动/参加学术会议/参加会议反馈.html': '/activities/academic-activities?tab=conf-feedbacks',
  '学术活动/参加学术会议/参加会议反馈申请.html': '/activities/academic-activities?action=conf-feedback',
  '学术活动/参加学术会议/参加会议反馈详情.html': '/activities/academic-activities?tab=conf-feedbacks',
  '学术活动/参加学术会议/添加会议反馈.html': '/activities/academic-activities?action=conf-feedback',
  '学术活动/参加学术会议/参会经费报销管理.html': '/activities/academic-activities?tab=ledger',
  '学术活动/参加学术会议/学术活动台账.html': '/activities/academic-activities?tab=ledger',

  // 科研统计
  'statistics.html': '/analytics/statistics',
  '科研统计/成果统计.html': '/analytics/statistics',
  '科研统计/纵向项目统计.html': '/analytics/statistics',
  '科研统计/科研统计台账.html?module=people': '/analytics/statistics?tab=people',
  '科研统计/科研统计台账.html?module=project': '/analytics/statistics?tab=project',
  '科研统计/科研统计台账.html?module=result': '/analytics/statistics?tab=result',

  // 基础信息维护与字典
  '基础信息维护/字典.html': '/base-info/dictionary',
  '基础信息维护/add_dictionary.html': '/base-info/dictionary?action=add',
  '基础信息维护/child_dictionary.html': '/base-info/dictionary?action=child',
  '基础信息维护/纵向基本信息维护/项目分类维护.html': '/base-info/system-configs?tab=categories',
  '基础信息维护/纵向基本信息维护/经费模版配置.html': '/base-info/system-configs?tab=templates',
  '基础信息维护/纵向基本信息维护/经费模版支出类型.html': '/base-info/system-configs?tab=templates',
  '基础信息维护/纵向基本信息维护/添加经费模版.html': '/base-info/system-configs?tab=templates&action=add',
  '基础信息维护/纵向基本信息维护/添加经费模版支出类型.html': '/base-info/system-configs?tab=templates',
  '基础信息维护/成果基本信息维护/刊物论文集信息维护.html': '/base-info/system-configs?tab=journals',
  '基础信息维护/成果基本信息维护/刊物论文集详情.html': '/base-info/system-configs?tab=journals',
  '基础信息维护/成果基本信息维护/添加刊物论文集.html': '/base-info/system-configs?tab=journals&action=add',
  '基础信息维护/通用信息维护/区域信息维护.html': '/base-info/system-configs?tab=regions',
  '基础信息维护/通用信息维护/add_area.html': '/base-info/system-configs?tab=regions&action=add',
  '基础信息维护/通用信息维护/国民行业代码维护.html': '/base-info/system-configs?tab=industry',
  '基础信息维护/通用信息维护/add_addIndustry_code.html': '/base-info/system-configs?tab=industry&action=add',
  '基础信息维护/通用信息维护/技术领域维护.html': '/base-info/system-configs?tab=disciplines',
  '基础信息维护/通用信息维护/add_technical_field.html': '/base-info/system-configs?tab=disciplines&action=add',
  '基础信息维护/通用信息维护/合同买方性质维护.html': '/base-info/system-configs?tab=regions',
  '基础信息维护/通用信息维护/add_contract_buyer.html': '/base-info/system-configs?tab=regions&action=add',
  '基础信息维护/通用信息维护/学科名称维护.html': '/base-info/system-configs?tab=disciplines',
  '基础信息维护/通用信息维护/add_subject.html': '/base-info/system-configs?tab=disciplines&action=add',

  // 系统管理与流程一览
  '系统管理/权限管理/角色管理.html': '/system/roles',
  '系统管理/权限管理/字段授权.html': '/system/roles?tab=fields',
  '系统管理/权限管理/add_role.html': '/system/roles?action=add',
  '系统管理/权限管理/role_jurisdiction.html': '/system/roles?tab=permissions',
  '系统管理/权限管理/role_user.html': '/system/roles?tab=users',
  '系统管理/权限管理/流程一览.html': '/workflow/todo-approvals',
  '系统管理/登录日志.html': '/system/login-logs',
  '系统管理/系统管理/附件与对象存储管理.html': '/system/attachments',
  '科研办公/科研办公/附件与对象存储管理.html': '/system/attachments',
  '流程/审核.html': '/workflow/todo-approvals',
  '流程/无终止审核.html': '/workflow/todo-approvals?mode=no-term',
  '流程/流程/待办事务.html': '/workflow/todo-approvals'
}

/**
 * 智能解析路径，将任何旧版 HTML / URL 统一接管转换为现代 Vue 路由。
 * 若已支持现代路由，返回目标路由字符串（例如 '/office/notice'）；
 * 若未能接管（如外链或未迁移的纯旧页面），返回 null。
 */
export function resolveModernRoute(rawPath: string | null | undefined): string | null {
  if (!rawPath) return null

  // 1. 先进行多层 URL 解码，防止后台 Uri.EscapeUriString 或前端 encodeURIComponent 导致中文匹配失败
  let decoded = rawPath.trim()
  try {
    while (decoded.includes('%')) {
      const nextDecoded = decodeURIComponent(decoded)
      if (nextDecoded === decoded) break
      decoded = nextDecoded
    }
  } catch {
    // 解码异常时继续使用当前字符串
  }

  // 2. 清除前导斜杠及 /IMIS/views/ 前缀
  const cleanPath = decoded.replace(/^\/?(IMIS\/views\/)?/, '')

  // 3. 优先精确匹配现代路由映射表
  if (modernRouteMap[cleanPath]) {
    return modernRouteMap[cleanPath]
  }

  // 4. 去除可能的前置参数（保留原始 query）进行二次匹配
  const [basePath, queryString] = cleanPath.split('?')

  // 4.1 字典管理参数化动态接管 (56+ 字典项无缝直达)
  if (basePath === '基础信息维护/字典.html' || basePath.endsWith('/字典.html')) {
    if (queryString) {
      const params = new URLSearchParams(queryString)
      const name = params.get('name')
      if (name) {
        return `/base-info/dictionary?name=${encodeURIComponent(name)}`
      }
    }
    return '/base-info/dictionary'
  }

  // 4.2 统一台账基础路由兜底
  if (basePath === '科研办公/科研办公/科研业务台账.html') {
    if (queryString) {
      const params = new URLSearchParams(queryString)
      const module = params.get('module')
      if (module) {
        const fullKey = `科研办公/科研办公/科研业务台账.html?module=${module}`
        if (modernRouteMap[fullKey]) {
          return modernRouteMap[fullKey]
        }
      }
    }
    return '/project/vertical-lifecycle'
  }

  // 4.3 基础路径如果在映射表中有定义
  if (modernRouteMap[basePath]) {
    const target = modernRouteMap[basePath]
    if (queryString && !target.includes('?')) {
      return `${target}?${queryString}`
    }
    return target
  }

  return null
}
