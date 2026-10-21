# .NET 10 迁移说明

本次迁移以原 SQL Server 备份为基准，迁移到 PostgreSQL，保留表名和现有前端接口路径，并让 .NET 10 访问层使用 Npgsql。

## 配置

数据库连接字符串不再写入仓库。部署时通过环境变量或密钥管理系统提供：

```bash
connectionString__ScientificResearch_Development="Host=...;Port=5432;Database={0};Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=False"
connectionString__ScientificResearch_Release="Host=...;Port=5432;Database={0};Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=False"
```

`allowedDatabaseKeys` 是数据库白名单。新增租户数据库前，必须显式加入白名单并确认数据库账号权限。

EPPlus 已升级到 `8.7.0`。部署环境必须通过 `EPPlusLicenseContext` 提供合法授权，例如 `Commercial:<license-key>`；非商业组织只能使用符合 EPPlus 许可条款的 `NonCommercialOrganization:<name>` 配置，不能把授权信息写入仓库。

## Keycloak 统一人员管理

系统已经加入 Keycloak OIDC Bearer Token 校验，配置项位于 `Keycloak` 节。默认 `Enabled=false`，保持原有本地登录兼容；切换为 `true` 后，API 使用 `Authorization: Bearer <access_token>`，不再接受 `/Access/Login` 的本地密码登录。

Keycloak 的 `preferred_username`（可通过 `UserIdentifierClaim` 调整）必须对应 PostgreSQL `人员.工号`，且本地人员 `是否启用=true`。系统不会根据 Keycloak 账号自动创建科研人员；映射成功后继续使用本地部门、角色和权限表，因此现有业务控制器无需改写。前端登录完成后调用 `GET /Access/Me` 获取当前人员和权限树，密码修改转到 Keycloak 账户中心。旧版 AJAX 封装会读取当前标签页的 access token 自动添加 Bearer 头，收到一次 401 时使用 refresh token 重试，退出时清除全部令牌。

切换前应在 Keycloak 创建 realm 和 public client，配置 API audience 为 `scientificresearch-api`，导入或绑定人员账号，并先用测试账号验证 `/Access/Me`、部门查询和一个受保护业务接口。Keycloak 客户端应配置 `ClientId`、Authorization Code + PKCE `S256`、关闭 direct access grants/service account、登记唯一 HTTPS 回调地址和 `scientificresearch-api` audience；应用同时校验令牌的 `azp` 必须等于该客户端。HTTPS 元数据校验在生产环境应保持开启；需要 MFA 门禁时，在验收环境设置 `KEYCLOAK_REQUIRE_MFA=true` 并核对令牌 `amr`。

## 本地编译

在安装 .NET 10 SDK 的机器上执行：

```bash
dotnet restore ScientificResearch/ScientificResearch/ScientificResearch.csproj
dotnet build ScientificResearch/ScientificResearch/ScientificResearch.csproj -c Release
dotnet publish ScientificResearch/ScientificResearch/ScientificResearch.csproj -c Release -r linux-x64 --self-contained false -o publish/linux-x64
```

## Linux 运行

应用监听 `63739` 端口。生产环境应由 Nginx 终止 HTTPS，并由 systemd 管理 Kestrel 进程；上传目录需要对应用账户授予写权限。发布前必须验证登录、项目申报、审批、报销、Excel 导入和附件上传。

systemd、Nginx 和环境变量模板位于 `deploy/`：先复制 `scientificresearch.env.example` 为服务器上的私密环境文件，再按 [deploy/README.md](deploy/README.md) 执行发布、验证和回滚。

仓库根目录同时提供 `Dockerfile`、`docker-compose.yml` 和 `.env.docker.example`。Compose 会自动初始化 PostgreSQL 迁移脚本；没有 NuGet 网络时，可以先发布 `publish/linux-x64`，再用 `Dockerfile.published` 和 `docker-compose.published.yml` 构建运行镜像。

## 当前验证

- 已通过 `appsettings.json`、项目文件和发布配置的 JSON/XML 静态解析检查。
- 已清除旧的 `bin/obj` 构建产物和旧 FTP 发布凭据文件，防止历史依赖或凭据进入新发布包。
- 已使用 .NET SDK `10.0.401` 完成 NuGet 还原、Release 编译；Npgsql 连接层编译通过。
- 发布目录为 `publish/linux-x64`，约 54 MB，目标服务器需安装 .NET 10 ASP.NET Core Runtime。
- 已从原 SQL Server 备份恢复并导入 PostgreSQL 18：原始 58 张业务表、37,241 条数据、51 个视图通过容器执行验证；加上操作审计、权限代理、项目治理、预算异常对账处理、外部系统同步队列、项目申报入口、论文认领入口、HR 人员映射、伦理同步记录和迁移版本表后，当前 schema 为 94 张表、55 个视图，另有历史数据完整性异常清单。
- 已用 PostgreSQL 18 与 .NET 10 运行时容器完成启动验证：生产默认关闭 Swagger，健康检查、Keycloak 登录和受保护接口均通过；需要调试时可显式设置 `SWAGGER_ENABLED=true`。当前主机是 ARM 架构，因此使用 `linux/amd64` 仿真验证 `linux-x64` 发布包；Linux x64 服务器可直接运行。
- 多阶段 Dockerfile 已加入 BuildKit NuGet 缓存、120 秒超时和 4 次重试，恢复与发布阶段共用缓存，避免 `NETSDK1064` 找不到 `StackExchange.Redis`。若构建服务器仍遇到 NuGet 下载连接提前结束，离线构建可使用已验证的 `Dockerfile.published`，完整源码构建需保证 Docker 构建环境可访问 NuGet。
- PostgreSQL 的 `date` 类型由 Npgsql 映射为 `DateOnly`；运行时已注册兼容处理器，将其安全转换为旧模型使用的 `DateTime/DateTime?`，不需要修改原始 58 张业务表和既有视图。
- Keycloak 接入已通过 .NET 10 Release 编译检查；本机已用真实 Keycloak `scientific-research` Realm、`scientificresearch-api` 客户端和本地启用人员 `string22` 完成令牌签发、issuer/audience/azp 校验、`/Access/Me` 与工作台/预算接口验收；另用临时其他客户端附加相同 audience 进行负向测试，应用按预期返回 401；前端保留当前标签页 refresh token，并在旧版接口收到一次 401 时自动刷新重试；目标生产环境仍需替换为自己的 Realm、客户端和测试账号。
- 已增加 `/health/live`、`/health/ready` 和 `X-Request-ID`，用于容器编排、反向代理和故障追踪；生产验收应确认 readiness 在数据库不可用时返回非 2xx。
- 已补充生产 P0 运行保障：ForwardedHeaders 仅信任配置的代理 IP，生产默认关闭 Swagger，Docker 应用 healthcheck 复用 `/health/ready`，并统一返回基础安全响应头。

## 兼容性边界

- `Npgsql` 已替换 SQL Server 客户端连接层。
- EPPlus 已升级到 `8.7.0`，Excel 导入导出需在真实环境验证授权和文件兼容性。
- 旧版 `Microsoft.AspNetCore.All` 和旧版 Swagger 已移除。
- 成功响应继续保留原有 `{ data: ... }` 包装，错误响应使用 HTTP 状态码和 `{ error: ... }`。
- 原库的 82 个存储过程中，74 个已转换为 PostgreSQL 函数（包括项目变更审批、角色权限、通知接收、字典维护、人员导入、论文导入、登录和角色人员兼容过程）；38 个内联表值函数、4 个标量函数和 2 个表值函数均已有 PostgreSQL 实现，剩余 8 个动态脚本生成器和旧分页过程已由 `fn_迁移遗留例程状态()` 明确登记为安全退休。原有 35 个重复数据拦截触发器已转换为 PostgreSQL 行级校验，并通过 `v1_数据完整性异常` 暴露历史重复键。完整应用接口仍需在目标 Linux/PostgreSQL 环境做登录、权限、流程、报销和附件回归。
- 已增加统一工作台摘要、追加式操作审计和项目级访问判断；项目详情接口会按负责人、参与人、流程发起人或科研管理角色进行授权过滤。登录页已接入 Authorization Code + PKCE，回调后调用 `/Access/Me` 建立本地人员上下文，退出时清除浏览器令牌；新增“权限代理与预算对账”页面作为代理和异常处理入口。
- 权限代理接口对客户端 ISO 8601 时间显式转换为 PostgreSQL 无时区时间，UTC 请求可正常创建代理；脚本支持直接使用短期 `KEYCLOAK_ACCESS_TOKEN`，或用临时测试客户端账号取得 token，并检查 issuer、audience、人员映射和 `/Access/Me`。时间边界权限代理支持撤销原因、人员禁用自动回收和管理角色批量回收；预算异常对账支持处理状态、责任人和处理说明。本机验收覆盖层为 `deploy/docker-compose.keycloak.local.yml`；目标环境仍需执行同一脚本并使用生产 Realm、客户端和测试账号。
- PostgreSQL 迁移脚本位于 `database/postgresql/migrations/`，仓库目标/登记 schema 版本为 88（运行库最近一次稳定证据仍为 Schema 82，须在 Docker 恢复后补做 083–088 迁移验收）；迁移 042 为科研管理员补齐科研业务菜单权限，迁移 043 为统一工作台增加角色、下一步责任人和办理提示，迁移 044 为申报建立版本快照、评审意见和未获资助复盘，迁移 045 增加项目延期、暂停、终止申请、研究报告和档案材料留痕，迁移 046 增加预算分层台账、实际付款和合同付款节点，迁移 047 增加成果关联、作者身份核验和治理状态，迁移 048 增加人员科研能力、团队、导师关系和项目分工画像，迁移 049 增加科研评价规则版本、可解释结果、申诉和奖励兑现，迁移 050 增加科研瓶颈事件、资源使用记录和决策汇总，迁移 051 增加流程版本、数据导出任务和接口失败告警，迁移 052 增加科研单位规范字典和成果作者单位绑定，迁移 053 修复项目和经费菜单路径，迁移 054 增加 HR、财务、采购、伦理等外部系统注册和幂等同步任务队列，迁移 055 增加纵向项目申报预检查、提交和评审汇总前端入口，迁移 056 增加论文采集可认领列表和认领审核入口，迁移 057 增加 HR 人员外部映射和幂等同步基础表，迁移 058 增加伦理审批结果同步审计和项目伦理字段回写，迁移 059 补齐成果、统计、学术活动和流程菜单页面路径，迁移 060 增加匿名专家评审工作台菜单和授权，迁移 061 增加论文采集结果幂等落库与认领状态审计，迁移 062 增加字段级授权和权限变更通知，迁移 063 增加字段授权系统管理菜单入口，迁移 064 增加纵向项目申报计划发布窗口和菜单入口，迁移 065 增加工作台缺件提示，迁移 066 增加专家遴选页面，同时保留组织架构和系统管理边界；迁移 067 增加科研统计指标口径版本管理，068 修复纵向申报存储过程，069 增加配套项目和 HRP 中间表，070 增加项目节点预警和伦理续展，071 增加论文主动推送和署名消歧，072 增加生物样本台账与伦理阻断，073 增加移动推送和绩效公式引擎，074 增加伦理审查流程，075 增加成果转化和奖励财务回写，076 补齐科研伦理审查工作台菜单权限，077 增加项目申报资格预检、任务书版本、内部确认和电子档案清单，078 增加发票查重、预算调整、年度封账和付款回写约束，079 扩展人员能力画像、可用时间、导师关系和团队匹配，080 增加年度评价公示/正式发布/锁定/结算和奖励支付凭证，081 增加伦理委员会会议、议程、纪要和独立电子档案，082 将移动推送改为真实通道验证、排队与失败状态并清理示例凭据，083 增加成果治理不可变历史与重复成果合并审计，084 增加专家可用时间与外部专家邀请队列，085 将 HRP 中间库双向协同收敛为只推送待同步记录的安全过程，拒绝 replay/push_and_replay 和示例付款写入；086 增加成果撤稿/更正状态来源凭证与不可变核验审计；087 增加科研评价奖励支付批次、财务回写流水、失败重试和对账约束；088 增加横向项目延期/暂停/终止、研究报告、合同付款节点和档案材料留痕。`/health/ready` 会校验 `Postgres:ExpectedSchemaVersion`，`database/postgresql/verify_migration.sh` 默认只读，只有显式指定 `FRESH_DATABASE_URL` 才会在空库重放迁移。源对象清单位于 `database/postgresql/source-sqlserver/`。
