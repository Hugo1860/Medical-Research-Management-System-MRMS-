// var getApiHost = 'http://192.168.0.126:63739';
var getApiHost = '';
var getUserImG = 'http://api3.meinolf.net.cn:9003';

var Code = {
    //用户登录
    URL_POST_LOGIN: getApiHost + '/Access/Login',

    //修改密码
    URL_POST_CHANGE_PASSWORD: getApiHost + '/Access/ChangePassword',

    //获取登录日志
    URL_GET_LOGIN_LOG: getApiHost + '/Access/获取登录日志',

    //获取角色列表
    URL_GET_ROLE_LIST: getApiHost + '/Permission/获取角色列表',

    //新增角色
    URL_POST_ADD_ROLE: getApiHost + '/Permission/新增角色',

    //修改某个角色
    URL_POST_EDIT_ROLE: getApiHost + '/Permission/修改角色',

    //分页获取角色中的人员
    URL_GET_ROLE_USERS: getApiHost + '/Permission/分页获取角色中的人员',

    //分页获取角色中没有的人员
    URL_GET_ROLE_NOT_USERS: getApiHost + '/Permission/分页获取角色中没有的人员',

    //保存修改角色中的人员
    URL_POST_EDIT_ROLE_USERS: getApiHost + '/Permission/保存角色中的人员',

    //增删角色人员
    URL_POST_ADD_AND_DEL_ROLE_USERS: getApiHost + '/Permission/增删角色人员',

    //获取某个角色的权限
    URL_GET_ROLE_JURISDICTION: getApiHost + '/Permission/获取角色的权限',

    //修改某个角色中的权限
    URL_POST_EDIT_ROLE_JURISDICTION: getApiHost + '/Permission/保存角色的权限',

    //获取启用的部门列表
    URL_GET_ENABLE_DEPARTMENT_LIST: getApiHost + '/BaseInfo/获取启用的部门列表',

    //获取全部部门列表
    URL_GET_DEPARTMENT_LIST: getApiHost + '/BaseInfo/获取部门列表',

    //新增或修改部门
    URL_POST_ADD_OR_EDIT_DEPARTMENT: getApiHost + '/BaseInfo/增改部门',

    //启用部门
    URL_POST_ENABLE_DEPARTMENT: getApiHost + '/BaseInfo/启用部门',

    //禁用部门
    URL_POST_DISABLE_DEPARTMENT: getApiHost + '/BaseInfo/禁用部门',

    //获取全部人员列表
    URL_GET_USERS_LIST: getApiHost + '/People/获取所有人员列表',

    //分页获取全部人员列表
    URL_GET_PAGING_USERS_LIST: getApiHost + '/People/分页获取人员列表',

    //分页获取启用人员列表
    URL_GET_PAGING_ENABLE_USERS_LIST: getApiHost + '/People/分页获取启用的人员列表',

    //新增或修改人员
    URL_POST_ADD_OR_EDIT_USERS: getApiHost + '/People/增改人员',

    //启用人员
    URL_POST_ENABLE_USERS: getApiHost + '/People/启用人员',

    //禁用人员
    URL_POST_DISABLE_USERS: getApiHost + '/People/禁用人员',

    //获取某人员详情
    URL_GET_USER_DETAILS: getApiHost + '/People/获取某人员详情',

    //获取人员详情辅助数据
    URL_GET_USER_DETAILS_AUXILIARY_DATA: getApiHost + '/People/获取人员详情辅助数据',

    //分页获取学术任职
    URL_GET_PAGING_LEARNING: getApiHost + '/People/分页获取学术任职',

    //获取学术任职详情
    URL_GET_LEARNING_DETAILS: getApiHost + '/People/获取学术任职详情',

    //新增学术任职
    URL_POST_ADD_LEARNING_USER:getApiHost + '/People/新增学术任职',

    //修改学术任职
    URL_POST_EDIT_LEARNING_USER:getApiHost + '/People/完善学术任职',

    //上传学术任职相关文件
    URL_POST_UPLOAD_LEARNING_FILE:getApiHost + '/People/上传学术任职相关文件',

    //分页获取科研人员
    URL_GET_PAGING_SCIENTIFICRESARCH_USER: getApiHost + '/People/分页获取科研人员',

    //新增科研人员
    URL_POST_ADD_SCIENTIFICRESARCH_USER:getApiHost + '/People/新增科研人员',

    //删除科研人员
    URL_POST_DEL_SCIENTIFICRESARCH_USER:getApiHost + '/People/删除科研人员',

    //分页获取专家
    URL_GET_PAGING_EXPERTALENTS_USER: getApiHost + '/People/分页获取专家',

    //新增专家
    URL_POST_ADD_EXPERTALENTS_USER:getApiHost + '/People/新增专家',

    //删除专家
    URL_POST_DEL_EXPERTALENTS_USER:getApiHost + '/People/删除专家',

    //人才与团队能力画像
    URL_GET_PEOPLE_CAPABILITY_PROFILE: getApiHost + '/People/获取人员科研能力画像',
    URL_POST_SAVE_PEOPLE_CAPABILITY_PROFILE: getApiHost + '/People/保存人员科研能力画像',
    URL_POST_SAVE_MENTOR_RELATION: getApiHost + '/People/保存人员导师关系',
    URL_GET_RESEARCH_COLLABORATORS: getApiHost + '/People/查找科研合作人',
    URL_POST_SAVE_RESEARCH_TEAM: getApiHost + '/People/保存科研团队',
    URL_POST_SAVE_RESEARCH_TEAM_MEMBER: getApiHost + '/People/保存科研团队成员',
    URL_POST_SAVE_PEOPLE_PROJECT_ASSIGNMENT: getApiHost + '/People/保存人员项目分工',

    //科研评价与奖励
    URL_GET_EVALUATION_RULES: getApiHost + '/Evaluation/获取科研评价规则',
    URL_GET_EVALUATION_RULE_DETAIL: getApiHost + '/Evaluation/获取科研评价规则详情',
    URL_POST_SAVE_EVALUATION_RULE: getApiHost + '/Evaluation/保存科研评价规则',
    URL_POST_SAVE_EVALUATION_RULE_ITEM: getApiHost + '/Evaluation/保存科研评价规则项',
    URL_POST_GENERATE_EVALUATION: getApiHost + '/Evaluation/生成科研评价',
    URL_GET_EVALUATION_RESULTS: getApiHost + '/Evaluation/查询科研评价结果',
    URL_GET_EVALUATION_DETAIL: getApiHost + '/Evaluation/获取科研评价详情',
    URL_POST_REVIEW_EVALUATION: getApiHost + '/Evaluation/复核科研评价',
    URL_POST_APPEAL_EVALUATION: getApiHost + '/Evaluation/提交科研评价申诉',
    URL_POST_HANDLE_EVALUATION_APPEAL: getApiHost + '/Evaluation/处理科研评价申诉',
    URL_POST_REGISTER_EVALUATION_REWARD: getApiHost + '/Evaluation/登记科研评价奖励',
    URL_GET_EVALUATION_RANKING: getApiHost + '/Evaluation/获取科研评价排名',

    //科研瓶颈与决策分析
    URL_GET_BOTTLENECK_EVENTS: getApiHost + '/Analytics/查询科研瓶颈事件',
    URL_POST_SAVE_BOTTLENECK_EVENT: getApiHost + '/Analytics/保存科研瓶颈事件',
    URL_GET_BOTTLENECK_SUMMARY: getApiHost + '/Analytics/获取科研瓶颈汇总',
    URL_GET_RESOURCE_USAGE_SUMMARY: getApiHost + '/Analytics/获取科研资源使用汇总',
    URL_POST_SAVE_RESOURCE_USAGE: getApiHost + '/Analytics/保存科研资源使用',
    URL_GET_DECISION_SUMMARY: getApiHost + '/Analytics/获取科研决策摘要',

    //运行治理、流程版本、失败告警和数据导出
    URL_GET_INTERFACE_FAILURE_ALERTS: getApiHost + '/Operations/获取接口失败告警',
    URL_GET_FLOW_VERSIONS: getApiHost + '/Operations/获取流程版本',
    URL_POST_SAVE_FLOW_VERSION: getApiHost + '/Operations/保存流程版本',
    URL_POST_PUBLISH_FLOW_VERSION: getApiHost + '/Operations/发布流程版本',
    URL_POST_CREATE_DATA_EXPORT: getApiHost + '/Operations/创建数据导出任务',
    URL_GET_DATA_EXPORT_TASKS: getApiHost + '/Operations/查询数据导出任务',
    URL_POST_UPDATE_DATA_EXPORT: getApiHost + '/Operations/更新数据导出任务',

    // 外部系统集成、幂等同步任务和重试
    URL_GET_EXTERNAL_SYSTEMS: getApiHost + '/Operations/获取外部系统',
    URL_POST_SAVE_EXTERNAL_SYSTEM: getApiHost + '/Operations/保存外部系统',
    URL_GET_EXTERNAL_SYNC_TASKS: getApiHost + '/Operations/查询外部同步任务',
    URL_POST_CREATE_EXTERNAL_SYNC_TASK: getApiHost + '/Operations/创建外部同步任务',
    URL_POST_RETRY_EXTERNAL_SYNC_TASK: getApiHost + '/Operations/重试外部同步任务',
    URL_POST_CANCEL_EXTERNAL_SYNC_TASK: getApiHost + '/Operations/取消外部同步任务',

    //科研单位名称规范
    URL_GET_RESEARCH_INSTITUTION_NORMALIZATION: getApiHost + '/Fruit/获取科研单位规范',
    URL_POST_SAVE_RESEARCH_INSTITUTION: getApiHost + '/Fruit/保存科研单位规范',
    URL_POST_SAVE_RESEARCH_INSTITUTION_ALIAS: getApiHost + '/Fruit/保存科研单位别名',
    URL_POST_NORMALIZE_RESULT_AUTHOR_INSTITUTION: getApiHost + '/Fruit/规范科研成果作者单位',

    //获取获取某上级编号下字典
    URL_GET_DICTIONARY_CHILD: getApiHost + '/BaseInfo/获取某上级编号下字典',

    //分页获取获取某上级编号下字典
    URL_GET_PAGING_DICTIONARY_CHILD: getApiHost + '/BaseInfo/分页获取某上级编号下字典',

    //分页获取某类字典
    URL_GET_PAGING_CATEGORY_DICTIONARY: getApiHost + '/BaseInfo/分页获取某类字典',

    //获取某类字典
    URL_GET_CATEGORY_DICTIONARY: getApiHost + '/BaseInfo/获取某类字典',

    //新增字典
    URL_POST_ADD_DICTIONARY: getApiHost + '/BaseInfo/新增字典',

    //修改某个字典
    URL_POST_EDIT_DICTIONARY: getApiHost + '/BaseInfo/修改字典',

    //批量删除字典
    URL_POST_DEL_DICTIONARY: getApiHost + '/BaseInfo/删除字典',

    //获取学科列表
    URL_GET_SUBJECT_LIST: getApiHost + '/BaseInfo/获取学科',

    //新增学科
    URL_POST_ADD_SUBJECT: getApiHost + '/BaseInfo/新增学科',

    //修改某个学科
    URL_POST_EDIT_SUBJECT: getApiHost + '/BaseInfo/修改学科',

    //批量删除学科
    URL_POST_DEL_SUBJECT: getApiHost + '/BaseInfo/删除学科',

    //获取项目分类列表
    URL_GET_PROJECT_TYPE_LIST: getApiHost + '/BaseInfo/获取项目分类',

    //增改项目分类
    URL_POST_ADD_OR_EDIT_PROJECT_TYPE: getApiHost + '/BaseInfo/增改项目分类',

    //批量删除项目分类
    URL_POST_DEL_PROJECT_TYPE: getApiHost + '/BaseInfo/删除项目分类',

    //获取国民行业代码列表
    URL_GET_INDUSTRY_CODE_LIST: getApiHost + '/BaseInfo/获取国民行业代码',

    //新增国民行业代码
    URL_POST_ADD_INDUSTRY_CODE: getApiHost + '/BaseInfo/新增国民行业代码',

    //修改某个国民行业代码
    URL_POST_EDIT_INDUSTRY_CODE: getApiHost + '/BaseInfo/修改国民行业代码',

    //批量删除国民行业代码
    URL_POST_DEL_INDUSTRY_CODE: getApiHost + '/BaseInfo/删除国民行业代码',

    //获取合同买方性质列表
    URL_GET_CONTRACT_BUYER_LIST: getApiHost + '/BaseInfo/获取合同买方性质',

    //新增合同买方性质
    URL_POST_ADD_CONTRACT_BUYER: getApiHost + '/BaseInfo/新增合同买方性质',

    //修改某个合同买方性质
    URL_POST_EDIT_CONTRACT_BUYER: getApiHost + '/BaseInfo/修改合同买方性质',

    //批量删除合同买方性质
    URL_POST_DEL_CONTRACT_BUYER: getApiHost + '/BaseInfo/删除合同买方性质',

    //获取技术领域列表
    URL_GET_TECHNICAL_FIELD_LIST: getApiHost + '/BaseInfo/获取技术领域',

    //新增技术领域
    URL_POST_ADD_TECHNICAL_FIELD: getApiHost + '/BaseInfo/新增技术领域',

    //修改某个技术领域
    URL_POST_EDIT_TECHNICAL_FIELD: getApiHost + '/BaseInfo/修改技术领域',

    //批量删除技术领域
    URL_POST_DEL_TECHNICAL_FIELD: getApiHost + '/BaseInfo/删除技术领域',

    //获取区域列表
    URL_GET_AREA_LIST: getApiHost + '/BaseInfo/获取区域',

    //新增区域
    URL_POST_ADD_AREA: getApiHost + '/BaseInfo/新增区域',

    //修改某个区域
    URL_POST_EDIT_AREA: getApiHost + '/BaseInfo/修改区域',

    //批量删除区域
    URL_POST_DEL_AREA: getApiHost + '/BaseInfo/删除区域',

    //获取经费模版列表
    URL_GET_EXPENDITURE_TEMPLATE_LIST: getApiHost + '/BaseInfo/获取经费模版',

    //增改经费模版
    URL_POST_ADD_OR_EDIT_EXPENDITURE_TEMPLATE: getApiHost + '/BaseInfo/增改经费模版',

    //批量删除删除经费模版
    URL_POST_DEL_EXPENDITURE_TEMPLATE: getApiHost + '/BaseInfo/删除经费模版',

    //获取经费模版项目支出类型列表
    URL_GET_EXPENDITURE_TYPE_LIST: getApiHost + '/BaseInfo/获取经费模版项目支出类型列表',

    //增改经费模版项目支出类型
    URL_POST_ADD_OR_EDIT_EXPENDITURE_TYPE: getApiHost + '/BaseInfo/增改经费模版项目支出类型',

    //批量删除经费模版项目支出类型
    URL_POST_DEL_EXPENDITURE_TYPE: getApiHost + '/BaseInfo/删除经费模版项目支出类型',

    //批量删除经费模版项目支出类型
    URL_GET_EXPENDITURE_TYPE_DETAIL: getApiHost + '/BaseInfo/获取经费模版项目支出类型详情',

    //分页获取期刊列表
    URL_GET_PERIODICAL_LIST: getApiHost + '/BaseInfo/分页获取期刊',

    //增改期刊
    URL_POST_ADD_OR_EDIT_PERIODICAL: getApiHost + '/BaseInfo/增改期刊',

    //批量删除期刊
    URL_POST_DEL_PERIODICAL: getApiHost + '/BaseInfo/删除期刊',

    //审核步骤
    URL_POST_WORKFLOW: getApiHost + '/WorkFlow/审核步骤',

    //获取科研详情
    URL_GET_SCIENTIFICRESARCH_DETAILS: getApiHost + '/Office/获取科研详情',

    //分页获取科研待办
    URL_GET_SCIENTIFICRESARCH_UNTREATED: getApiHost + '/Office/获取待办流程',

    //获取统一工作台摘要
    URL_GET_WORKBENCH_SUMMARY: getApiHost + '/Office/获取科研工作台摘要',

    //纵向项目申报预检查（只读）
    URL_POST_VERTICAL_PROJECT_PREFLIGHT: getApiHost + '/YProject/预检查纵向项目申报',

    //纵向项目生命周期预检查（只读）
    URL_GET_VERTICAL_MIDTERM_PREFLIGHT: getApiHost + '/YProject/预检查纵向项目中检',
    URL_GET_VERTICAL_CHANGE_PREFLIGHT: getApiHost + '/YProject/预检查纵向项目变更',
    URL_GET_VERTICAL_CLOSEOUT_PREFLIGHT: getApiHost + '/YProject/预检查纵向项目结题',

    //专家评审利益冲突预检查（只读）
    URL_GET_REVIEWER_CONFLICT_PREFLIGHT: getApiHost + '/WorkFlow/预检查专家利益冲突',

    //纵向项目申报评审追踪
    URL_GET_VERTICAL_APPLICATION_REVIEW_SUMMARY: getApiHost + '/YProject/获取纵向项目申报评审汇总',
    URL_GET_VERTICAL_APPLICATION_VERSION_COMPARE: getApiHost + '/YProject/比较纵向项目申报版本',
    URL_POST_VERTICAL_REVIEW_COMMENT: getApiHost + '/YProject/新增纵向项目评审意见',
    URL_POST_VERTICAL_UNFUNDED_REVIEW: getApiHost + '/YProject/新增未获资助复盘',

    //纵向项目生命周期治理
    URL_GET_VERTICAL_LIFECYCLE_ARCHIVE: getApiHost + '/YProject/获取纵向项目生命周期档案',
    URL_POST_VERTICAL_LIFECYCLE_REQUEST: getApiHost + '/YProject/申请纵向项目延期暂停终止',
    URL_POST_VERTICAL_LIFECYCLE_REVIEW: getApiHost + '/YProject/审核纵向项目生命周期申请',
    URL_POST_VERTICAL_RESEARCH_REPORT: getApiHost + '/YProject/提交纵向项目研究报告',
    URL_POST_VERTICAL_RESEARCH_REPORT_REVIEW: getApiHost + '/YProject/审核纵向项目研究报告',
    URL_POST_VERTICAL_ARCHIVE_MATERIAL: getApiHost + '/YProject/归档纵向项目材料',
    URL_POST_VERTICAL_ARCHIVE_UPLOAD: getApiHost + '/YProject/上传项目档案材料',

    //预算分层与合同付款节点
    URL_GET_PROJECT_BUDGET_LAYERED: getApiHost + '/Expense/获取项目经费分层',
    URL_GET_PROJECT_PAYMENT_MILESTONES: getApiHost + '/Expense/获取项目合同付款节点',
    URL_POST_PROJECT_LEDGER: getApiHost + '/Expense/登记项目经费台账',
    URL_POST_PROJECT_PAYMENT_MILESTONE: getApiHost + '/Expense/保存项目合同付款节点',
    URL_POST_SYNC_FINANCE_TRANSACTIONS: getApiHost + '/Expense/同步财务流水',

    //成果治理
    URL_GET_RESULT_GOVERNANCE: getApiHost + '/Fruit/获取成果治理详情',
    URL_POST_RESULT_LINK: getApiHost + '/Fruit/新增科研成果关联',
    URL_POST_RESULT_AUTHOR_VERIFY: getApiHost + '/Fruit/核验科研成果作者',
    URL_POST_RESULT_GOVERNANCE_STATUS: getApiHost + '/Fruit/更新科研成果治理状态',

    //分页获取某人员可查看的通知公告
    URL_GET_PAGING_USER_NOTICE_LIST: getApiHost + '/Office/分页获取某人员可查看的通知公告',

    //分页获取通知公告
    URL_GET_PAGING_NOTICE_LIST: getApiHost + '/Office/分页获取通知公告',

    //获取通知公告详情
    URL_GET_NOTICE_DETAILS: getApiHost + '/Office/获取通知公告详情',

    //启禁通知公告
    URL_POST_ENABLE_OR_DISABLE_NOTICE: getApiHost + '/Office/启禁通知公告',

    //新增通知公告
    URL_POST_ADD_NOTICE: getApiHost + '/Office/新增通知公告',

    //上传通知公告文件
    URL_POST_UPLOAD_NOTICE_FILE: getApiHost + '/Office/上传通知公告文件',

    //上传下载文件
    URL_POST_UPLOAD_DOWNLOAD_FILE: getApiHost + '/Office/上传下载文件',

    //分页获取下载中心
    URL_GET_DOWNLOAD_CENTER: getApiHost + '/Office/分页获取下载中心',

    //分页获取可查看的下载中心
    URL_GET_ENABLE_DOWNLOAD_CENTER: getApiHost + '/Office/分页获取可查看的下载中心',

    //增改下载
    URL_POST_ADD_OR_DOWNLOAD: getApiHost + '/Office/增改下载',

    //启禁下载
    URL_POST_ENABLE_OR_DISABLE_DOWNLOAD: getApiHost + '/Office/启禁下载',

    //分页获取主办讲座
    URL_GET_HOST_LECTURE_LIST: getApiHost + '/AcademicActivities/分页获取主办讲座',

    //获取主办讲座详情
    URL_GET_HOST_LECTURE_DETAILS: getApiHost + '/AcademicActivities/获取主办讲座详情',

    //发起一个主办讲座流程
    URL_POST_ADD_HOST_LECTURE: getApiHost + '/AcademicActivities/发起一个主办讲座流程',

    //完善主办讲座申请资料
    URL_POST_EDIT_HOST_LECTURE: getApiHost + '/AcademicActivities/完善主办讲座申请资料',

    //上传主讲人简历
    URL_POST_UPLOAD_SPONSOR_RESUME: getApiHost + '/AcademicActivities/上传主讲人简历',

    //分页获取讲座反馈
    URL_GET_FEEDBACK_LECTURE: getApiHost + '/AcademicActivities/分页获取讲座反馈',

    //获取讲座反馈详情
    URL_GET_LECTURE_FEEDBACK_DETAILS: getApiHost + '/AcademicActivities/获取讲座反馈详情',

    //发起一个讲座反馈流程
    URL_POST_ADD_LECTURE_FEEDBACK: getApiHost + '/AcademicActivities/发起一个讲座反馈流程',

    //完善讲座反馈申请资料
    URL_POST_EDIT_LECTURE_FEEDBACK: getApiHost + '/AcademicActivities/完善讲座反馈申请资料',

    //上传讲座附件
    URL_POST_UPLOAD_LECTURE_ENCLOSURE: getApiHost + '/AcademicActivities/上传讲座附件',

    //分页获取参加会议
    URL_GET_MEETING_LIST: getApiHost + '/AcademicActivities/分页获取参加会议',

    //获取参加会议详情
    URL_GET_MEETING_DETAILS: getApiHost + '/AcademicActivities/获取参加会议详情',

    //发起一个参加会议流程
    URL_POST_ADD_MEETING: getApiHost + '/AcademicActivities/发起一个参加会议流程',

    //完善参加会议申请资料
    URL_POST_EDIT_MEETING: getApiHost + '/AcademicActivities/完善参加会议申请资料',

    //上传参会相关文件
    URL_POST_UPLOAD_MEETING_FILE: getApiHost + '/AcademicActivities/上传参会相关文件',

    //分页获取参加会议反馈
    URL_GET_MEETING_FEEDBACK_LIST: getApiHost + '/AcademicActivities/分页获取参加会议反馈',

    //获取参加会议反馈详情
    URL_GET_MEETING_FEEDBACK_DETAILS: getApiHost + '/AcademicActivities/获取参加会议反馈详情',

    //发起一个参加会议反馈流程
    URL_POST_ADD_MEETING_FEEDBACK: getApiHost + '/AcademicActivities/发起一个参加会议反馈流程',

    //完善参加会议反馈申请资料
    URL_POST_EDIT_MEETING_FEEDBACK: getApiHost + '/AcademicActivities/完善参加会议反馈申请资料',

    //上传会议反馈文件
    URL_POST_UPLOAD_MEETING_FEEDBACK_FILE: getApiHost + '/AcademicActivities/上传会议反馈文件',

    //分页获取论文
    URL_GET_PAPER_LIST: getApiHost + '/Fruit/分页获取论文',

    //获取论文详情
    URL_GET_PAPER_DETAILS: getApiHost + '/Fruit/获取论文详情',
    URL_GET_CLAIMABLE_PAPER_LIST: getApiHost + '/Fruit/分页获取可领取论文',
    URL_POST_CLAIM_PAPER: getApiHost + '/Fruit/认领论文',
    URL_GET_PAPER_JOURNAL_METADATA: getApiHost + '/Fruit/识别论文期刊信息',

    //新增论文
    URL_POST_ADD_PAPER: getApiHost + '/Fruit/新增论文',

    //完善论文资料
    URL_POST_EDIT_PAPER: getApiHost + '/Fruit/完善论文资料',

    //上传论文正文
    URL_POST_UPLOAD_PAPER: getApiHost + '/Fruit/上传论文正文',

    //上传论文其他相关文件
    URL_POST_UPLOAD_PAPER_FILE: getApiHost + '/Fruit/上传论文其他相关文件',

    //上传检索信息页相关文件
    URL_POST_UPLOAD_RETRIEVAL_INFO: getApiHost + '/Fruit/上传检索信息页相关文件',

    //导入自定义论文
    URL_POST_IMPORT_CUSTOM_PAPER: getApiHost + '/Fruit/导入自定义论文',

    //导入知网论文
    URL_POST_IMPORT_HOWNET_PAPER: getApiHost + '/Fruit/导入知网论文',
}
