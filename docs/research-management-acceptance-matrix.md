# 科研管理系统验收矩阵

| 模块 | 当前实现 | 当前证据 | 仍需完成 |
|---|---|---|---|
| 统一认证 | Keycloak Realm discovery（issuer、authorization/token endpoint、JWKS、可选 `scopes_supported`）、OIDC 客户端 PKCE/回调配置检查、可选 Realm/client 角色、client scope 和 token claims 检查、六角色本地人员映射与只读范围矩阵、MFA `amr` 预检、HR 人员同步回放、全量名单缺席人员禁用与代理回收、未登录 401 边界 | `verify-keycloak.sh`（`KEYCLOAK_DISCOVERY_ONLY=true` 为只读 discovery；普通模式无真实 token 退出码 2；支持多接口旧令牌和代理撤销探针）、`verify-auth-boundaries.sh`、`verify-auth-uat-contract.sh`、治理中心 HR 回放入口、HR 映射幂等逻辑、`/health/keycloak` | 真实多角色登录、MFA、真实 HR 批次和离岗回收验收；claims 预检不能替代 JwtBearer 签名验证，旧令牌失效与代理撤销仍需使用专用验收账号回放 |
| 科研首页 | 角色、下一步办理人、办理提示、移动摘要 | 工作台接口、移动工作台 HTTP 200 | 真实用户待办和缺件提示验收 |
| 项目申报评审 | 预检查、版本、评审汇总、冲突检查、专家评分 | 纵向申报页面、业务回归脚本 | 真实专家评分和匿名评审流程验收 |
| 项目全过程 | 纵向生命周期预检查、延期/暂停/终止、报告、档案；横向变更/延期暂停终止/研究报告/合同付款节点/结题档案 | 生命周期和横向流程页面 HTTP 200；`verify-project-lifecycle-contract.sh`、`verify-horizontal-lifecycle-contract.sh` | 真实审批、附件上传、合同付款回写和横向字段验收 |
| 科研伦理 | 独立伦理申请、秘书受理、委员会排期/议程、委员意见、正式决定、年度跟踪、修正案、不良事件、方案偏离、会议纪要确认和电子档案归档；持续记录审核要求服务端处理结论，不良事件必须有发生日期和严重程度 | `EthicsController` 全流程接口、伦理工作台、迁移 074/081、`verify-ethics-workflow-contract.sh`；详情页提供委员会审核按钮，审核结果写入审核人/时间 | 真实伦理委员会账号、会议排期与决定书档案回放；不良事件升级、持续审查提醒和正式档案服务仍需运行库验收 |
| 菜单路径 | 153 个权限路径全部对应 HTML | `verify-menu-paths.sh`：源码存在、HTTP 全部 200 | 新增菜单继续纳入门禁 |
| 经费与外部同步 | 预算、到账、报销、异常对账、预算异常处理、经费分层、付款节点、发票查重/OCR 状态、预算调整、年度结算和付款回写、幂等队列、worker、队列创建/重试/取消；worker 按外部系统配置继承超时和重试次数，对网络错误、超时、408、429、5xx 重试，其他 4xx 和本地配置错误终止；治理中心和发布门禁均要求 HR、财务、采购、伦理、合同、人才库、生物样本库、论文采集八类系统满足启用条件 | 仓库与运行库当前均为 Schema 88、治理校验、预算异常处理页面、auth boundary（已覆盖异常查询/处理和经费分层）、配置检查接口、`verify-finance-controls-contract.sh`、`verify-external-adapter-contract.sh`、`verify-adapter-readiness-contract.sh`、`verify-finance-adapter-contract.sh`（发票 OCR/采购订单/HRP 示例逻辑专项预检）、`verify-external-worker-contract.sh`（幂等键、请求追踪、租约回收、重试分类和队列锁顺序静态检查）、worker 默认关闭、治理中心操作入口、八类适配器字段模板、发布编译验证；HRP `push_and_replay`/`replay` 已服务端阻断，只允许 `push_only`；迁移085进一步让数据库过程只写入待同步队列，不读取或写入示例付款流水；迁移087补充奖励支付批次、财务回写流水和失败/对账约束 | 财务/采购等供应商协议和双向联调；真实 OCR、采购订单关联字段、付款回写和 HRP 回放仍需真实地址、密钥及脱敏账号验收 |
| 成果与论文 | 论文认领、期刊识别、Crossref/PubMed 采集、采集任务队列、采集结果幂等落库、作者核验、单位规范、专利代理通知队列、成果治理历史与重复合并审计、撤稿/更正来源凭证、成果转化、收益分配和财务回写；迁移097扩展著作/研究报告/鉴定成果/艺术作品等成果类型，知识产权全过程登记/事件/缴费提醒，获奖申报批次/专家评审/认定 | 论文页面、采集任务页面 HTTP 200、治理/转化接口编译通过；`verify-achievement-status-evidence-contract.sh`、`verify-achievement-ip-award-contract.sh`；迁移097表结构已建立 | 使用真实管理员账号和论文采集系统编号执行 Crossref/PubMed 回放；转化合同、收益分配和财务凭证需按脱敏数据验收；CNKI/万方/WoS/EI/JCR供应商适配器仍需联调；期刊分区自动匹配算法待补齐 |
| 专家管理 | 人员能力、导师关系、团队匹配、专家遴选筛选、冲突检查、评分提交、专家可用时间、外部邀请队列 | 专家遴选页面、组织架构/学术任职页面和接口；仓库与运行库当前均为 Schema 100；`verify-people-expertise-contract.sh`、`verify-expert-invitation-contract.sh` 检查能力画像、项目访问范围、幂等邀请和真实回执门禁 | 多专家真实遴选、匿名评审、评分汇总、真实邮件/身份服务邀请和离岗回收验收 |
| 权限管理 | 项目范围、代理授权、回收、字段级授权、权限变更通知、未登录边界和六角色范围门禁 | 治理 smoke、auth boundary（已覆盖代理授权/撤销/人员回收、字段规则读写、权限变更通知；仓库与运行库当前均为 Schema 100）；`AUTH_BOUNDARIES_ROLE_TESTS=true` 验证普通角色 403/管理角色 200；`verify-auth-uat-contract.sh` 与 `AUTH_BOUNDARIES_SIX_ROLE_TESTS=true` 提供六类真实令牌矩阵、`/Access/Me` 本地映射和可选 claims/MFA 检查；`verify-keycloak.sh` 提供旧令牌多接口和代理撤销回放探针 | 使用真实六角色账号完成字段授权、离岗禁用后的旧令牌失效、代理撤销后的旧令牌和 `X-Proxy-Grantor-Id` 回放 |
| 绩效考核 | 规则、规则项、评价生成、结果解释、复核、申诉、奖励登记、奖励发放确认、财务回写、汇总 CSV 和明细考核报告导出 | 绩效页面已覆盖生成/复核/奖励/明细/申诉/导出入口；支付批次 API 已提供创建、提交财务受理凭据、单笔成功/失败回写和批次对账；`verify-reward-finance-closure-contract.sh` 检查支付批次、幂等键、不可变流水和凭证约束；`verify-evaluation-reward-contract.sh` 要求“已支付”同时具备真实财务回写和财务/支付凭证；`verify-auth-boundaries.sh` 覆盖评价详情、复核、申诉、奖励登记和奖励发放确认的未登录 401 | 真实规则配置、结果复核、工资/财务系统回写、失败重试/对账和支付凭证报告验收；支付 API 仍只登记真实外部回执，不代替供应商连接器 |
| 决策统计 | 总览、项目/成果/人员维度、可视化、CSV 导出、按指标钻取明细和版本化口径管理；迁移096增加教育部科技统计任务、数据包、校验、调整留痕与导出 | 统计页面、钻取接口、导出接口、指标口径表与维护接口、未登录 401；`verify-education-statistics-contract.sh` | 真实管理员口径确认、历史期间对比和正式报表模板验收 |
| 移动科研 | 待办、项目、经费、成果摘要、论文认领入口 | 移动工作台 HTTP 200 | 真机浏览器布局和多角色数据范围 |
| 数据迁移 | 001–100 迁移、逐表差异、逐项目主键核对、预算批准金额核对、绑定检查 | 仓库与运行库当前均为 Schema 100；迁移序列连续，199 张表、56 个视图和 320 组完整性检查通过；差异报告保留治理扩展、目标库追加、运行时追加和 1 条孤立参与人绑定证据 | 治理扩展授权确认、孤立参与人记录人工确认、全量历史业务核对 |
| 院级项目批次 | 院级项目申报批次、申报限额、资格规则、评审方案与评审计划管理（迁移094） | 纵向申报页面集成院级批次 tab，`YProjectController` 已集成批次与资格校验；`verify-internal-batch-contract` | 真实院级项目申报与评审全流程业务演练 |
| 经费到账认领 | 银行到款批量登记、负责人认领、认领分拆、经费卡生成与拆卡、费用提取方案与规则（迁移095） | `IncomingController` 接口完备；`incoming.vue` 交互；`verify-finance-incoming-contract.sh` | 银行对账单 Excel 批量导入解析、通知单打印与真实财务凭证回写 |
| 科研平台与服务 | 科研平台/中心档案、平台成员、服务事项目录、材料模板、办理时限、服务申请与反馈评价（迁移093） | `ResearchPlatformController`、`research-platform.vue`、`verify-research-platform-service-contract.sh` | 仪器设备台账、开放预约日历、机时计费与平台年报功能补充 |
| 文档共享与消息 | 文档分类管理、版本留痕、下载授权、下载审计日志、导入导出日志撤销；短信/邮件/微信消息通道与模板配置（迁移098） | `DocumentMessagingController`、`document-sharing.vue`、`verify-document-messaging-contract.sh` | 真实短信/邮件网关联调；企业微信生产对接 |
| 首页组件与催办 | 按角色组件布局配置、预警催办发起与处理、配置审计日志（迁移099） | `DashboardController`、`index.vue`、`verify-dashboard-component-contract.sh` | 用户自定义统计模型配置向导与前端拖拽排版扩展 |
| 横向合同治理 | 合同版本签审、合作单位资质核验、技术合同收入认定、合同模板库（迁移100） | `HorizontalContractGovernanceController`、`horizontal-contract.vue`、`verify-horizontal-contract-governance-contract.sh` | 合同在线预览与套红打印排版 |

## 当前发布门禁

本轮已重新验收 Docker Desktop、应用、PostgreSQL 和 Keycloak；`/health/live`、`/health/ready` 与 Keycloak discovery 均为 200。下面的结论基于本轮运行证据，但真实外部联调和角色回放仍需单独完成。

- `./deploy/verify-local-release.sh` 可一次性执行本地全量只读验收。
- .NET 10 Linux 发布包编译通过。
- 静态文件与迁移序列一致，当前 801 个静态文件、迁移 001–088；`verify-no-demo-fallback.sh` 阻止首页统计、登录审计、角色、权限代理和成果/样本写入使用伪造数据；移动工作台提供可安装 manifest 和离线静态壳。
- 本轮 `/health/live`、`/health/ready`、Keycloak discovery 均返回 200；后续每次重建后仍需重新执行，不得用历史结果替代当前证据。
- 本轮 153 个菜单路径源码存在且运行中 HTTP 返回 200；发布契约额外检查申报计划、专家遴选和项目统计页面。
- 未登录管理、同步、统计和评价接口按预期返回 401。
- 治理 smoke 和发布验收脚本通过。
- `verify-performance-load-contract.sh` 已登记有界只读并发烟测；正式容量测试仍需在验收环境按医院并发模型执行。

真实外部系统地址、认证密钥、供应商字段协议和多角色测试账号属于上线前输入；在这些条件提供前，系统保持外部 worker 默认关闭，不把本地技术验收表述为生产联调完成。

## 角色范围门禁

未登录门禁不会伪造身份，默认只验证管理接口返回 401。准备好由真实 Keycloak 签发的普通角色和系统管理员短期令牌后，可执行：

```bash
AUTH_BOUNDARIES_ROLE_TESTS=true \
AUTH_BOUNDARIES_NON_ADMIN_TOKEN="$NON_ADMIN_TOKEN" \
AUTH_BOUNDARIES_ADMIN_TOKEN="$ADMIN_TOKEN" \
APP_BASE_URL=https://research.example.org \
KEYCLOAK_BASE_URL=https://sso.example.org \
./deploy/verify-auth-boundaries.sh
```

脚本只对三个无写入 GET 管理接口做角色范围检查：普通角色应为 403，系统管理员应为 200；令牌缺失时直接失败，不生成或记录令牌。启用六角色开关后会先验证每个角色的 `/Access/Me` 映射，并可按部署要求检查 JWT claims 与 MFA `amr`。代理撤销、人员离岗回收、字段授权变更和旧令牌失效仍需使用专用验收账号按 `docs/production-uat-runbook.md` 做业务回放，再用撤销探针确认 401/403。
