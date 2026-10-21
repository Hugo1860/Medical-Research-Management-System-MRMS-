# Linux 部署模板

这些文件按“PostgreSQL、Linux 运行 .NET 10 应用”的路线准备。模板不会自动连接或修改服务器。

## 首次安装

```bash
sudo useradd --system --home /opt/scientificresearch --shell /usr/sbin/nologin scientificresearch
sudo install -d -o scientificresearch -g scientificresearch /opt/scientificresearch/releases
sudo install -d -o scientificresearch -g scientificresearch /opt/scientificresearch/current/wwwroot/upload
sudo install -d -m 700 /etc/scientificresearch
sudo install -m 600 scientificresearch.env /etc/scientificresearch/scientificresearch.env
sudo install -m 644 scientificresearch.service /etc/systemd/system/scientificresearch.service
sudo install -m 644 nginx-scientificresearch-tls.conf.example /etc/nginx/sites-available/scientificresearch.conf
sudo ln -s /etc/nginx/sites-available/scientificresearch.conf /etc/nginx/sites-enabled/scientificresearch.conf
```

生产环境必须先把 TLS 模板中的证书路径替换为有效证书；`nginx-scientificresearch.conf` 仅用于本地或内网 HTTP 测试。

将 `publish/linux-x64` 上传到 `/opt/scientificresearch/releases/<版本号>/`，再切换 `current` 软链接：

```bash
sudo ln -sfn /opt/scientificresearch/releases/<版本号> /opt/scientificresearch/current
sudo chown -R scientificresearch:scientificresearch /opt/scientificresearch/releases/<版本号>
sudo systemctl daemon-reload
sudo systemctl enable --now scientificresearch
sudo nginx -t && sudo systemctl reload nginx
```

## 验证和回滚

```bash
systemctl --no-pager --full status scientificresearch
curl -f http://127.0.0.1:63739/health/live
curl -f https://research.example.org/health/ready
```

回滚时将 `current` 指向上一个发布目录，再执行：

```bash
sudo systemctl restart scientificresearch
```

生产验收还需要使用真实 PostgreSQL 验证登录、权限、项目流程、报销、Excel 导入和附件上传。迁移完成后可执行 `DATABASE_CONTAINER=2018-7-24-db-1 ./database/postgresql/verify_migration.sh`；脚本默认只读校验 schema 版本 106、表/视图数量和关键数据，只有明确设置 `FRESH_DATABASE_URL` 才会对空库重放迁移。

人员进修 P0 已接入独立业务路由 `/FurtherEducation/*`（兼容 `/ContinuingEducation/*`），覆盖年度计划、个人申请、科主任/分管领导/科教科默认审批、预算预占与释放、进修待办、通知和审计。可执行 `./deploy/verify-further-education-contract.sh` 与 `./deploy/verify-further-education-p0-contract.sh` 做静态检查；真实流程仍需使用已认证的进修申请人、科主任、分管领导和科教科账号回放。

项目申报提交前可调用 `POST /YProject/预检查纵向项目申报` 获取只读检查结果。结果按检查项返回 `通过`、`材料完整`、`阻断提交` 和提示；暂存允许缺少最终材料，正式提交接口会复用同一检查并拒绝项目名称、日期、经费、伦理材料、申报书、摘要或参与人状态不合规的请求。`verify-business-contract.sh` 在提供真实 Keycloak 测试令牌后会同时验收该预检查接口。

项目中检、项目变更和项目结题发起前分别调用 `GET /YProject/预检查纵向项目中检`、`GET /YProject/预检查纵向项目变更`、`GET /YProject/预检查纵向项目结题`。检查会确认立项审批、项目访问权、结题状态、重复记录和未完成流程；结题还会检查中检与变更流程是否已完成。三个正式发起接口复用同一检查结果，避免绕过流程状态约束。

迁移 044 为纵向项目申报建立版本快照，并提供 `GET /YProject/获取纵向项目申报评审汇总`、`GET /YProject/比较纵向项目申报版本`、`POST /YProject/新增纵向项目评审意见` 和 `POST /YProject/新增未获资助复盘`。申报表每次新增或修改都会自动留存快照；评审意见与版本差异可按项目追踪，只有流程已完成且明确未立项的申报才能登记复盘。迁移 045 增加延期、暂停、终止申请、研究报告审核和档案材料留痕，并提供 `GET /YProject/获取纵向项目生命周期档案`、`POST /YProject/申请纵向项目延期暂停终止`、`POST /YProject/审核纵向项目生命周期申请`、`POST /YProject/提交纵向项目研究报告`、`POST /YProject/审核纵向项目研究报告` 和 `POST /YProject/归档纵向项目材料`。迁移 046 增加预算分层台账和合同付款节点，并提供 `GET /Expense/获取项目经费分层`、`GET /Expense/获取项目合同付款节点`、`POST /Expense/登记项目经费台账` 和 `POST /Expense/保存项目合同付款节点`。迁移 047 增加成果与研究、资助、伦理、数据关联、作者身份核验和成果治理状态，并提供 `GET /Fruit/获取成果治理详情`、`GET /Fruit/获取成果治理审计`、`POST /Fruit/新增科研成果关联`、`POST /Fruit/核验科研成果作者` 和 `POST /Fruit/更新科研成果治理状态`。迁移 081 补齐独立伦理委员会排期、议程、会议纪要确认和电子档案归档；迁移 082 将移动推送改为真实通道验证、排队和失败状态，清除演示凭据并禁止模拟成功；迁移 083 增加成果治理不可变历史和重复成果合并审计，迁移 084 增加专家可用时间和外部专家邀请的真实回执门禁，迁移 085 将 HRP 中间库双向协同限制为只推送待同步记录并拒绝 replay；迁移 086 增加成果状态来源凭证和不可变核验审计，迁移 087 增加奖励支付批次、财务回写流水和失败/对账约束；迁移 088 增加横向项目延期/暂停/终止、研究报告和合同档案接口。上述接口只登记真实外部受理/回执，不在浏览器内伪造支付或项目状态。CNKI 当前只解析用户上传的导出文件，万方接口尚未接入，不把文件导入结果表述为供应商实时同步。

伦理工作台的持续记录审核已形成闭环：项目负责人可提交年度跟踪、方案修正案、知情同意修订、不良事件和方案偏离，委员会管理角色在申请详情中审核并登记处理结论；不良事件必须填写发生日期和严重程度，审核结果、审核人和时间由服务端写入。`verify-ethics-workflow-contract.sh` 只读检查申请、受理、排期、会议意见、决定、持续记录审核、纪要确认、电子档案接口与前端入口，不连接数据库，也不把页面打开当作运行库验收。

专家可用时间与外部邀请契约可执行 `./deploy/verify-expert-invitation-contract.sh`；邀请默认只登记“未配置”队列，幂等键唯一，只有真实邮件/身份适配器回执才允许“已发送”。该检查不发送外部邀请。

任务书的“内部签署”只表示系统内阅读/确认留痕，不构成法律电子签章；项目档案清单的本地上传和核验也不等同于正式电子档案托管。`GET /YProject/获取纵向项目电子签章服务状态`、`GET /YProject/获取纵向项目正式档案服务状态` 以及两个正式提交接口在供应商未配置、HTTPS 端点未登记或真实适配器未验收时返回 `503 DOCUMENT_PROVIDER_NOT_READY`，不会写入“已签章/已托管”状态。部署方需先配置 `ElectronicSignature__*`、`ElectronicArchive__*`，完成供应商合同、协议版本、端点和多角色联调验收，再由实现方补齐适配器；可用 `./deploy/verify-document-provider-contract.sh` 做静态门禁，带真实管理员令牌并设置 `CHECK_HTTP=true` 可额外验证运行时 `503`。该脚本不调用供应商，也不上传或修改业务数据。


首页的“科研治理中心”（`/IMIS/views/科研办公/科研办公/科研治理中心.html`）汇总科研评价规则、瓶颈与资源等待、接口失败告警、数据导出任务和科研单位规范；单位规范维护仍由系统管理员或科研管理员授权，页面只显示当前账号可访问的数据。

专家评审前可调用 `GET /WorkFlow/预检查专家利益冲突?步骤编号=...`。系统会根据当前待评审流程识别纵向/横向项目负责人、申报人和参与人；命中项目团队时禁止提交评分，评审接口与预检查使用同一服务端判断，避免只依赖前端提示。

项目中检、项目变更、项目结题和三类报销汇总接口可用 `deploy/verify-business-contract.sh` 做只读回归；它使用 Keycloak 测试令牌调用查询接口，不创建流程、报销记录或附件。也可以在 `verify-release.sh` 中设置 `VERIFY_BUSINESS_CONTRACT=true` 一并执行：

```bash
APP_BASE_URL=http://127.0.0.1:16379 \
KEYCLOAK_AUTHORITY=http://keycloak.localhost:8180/realms/scientific-research \
KEYCLOAK_CLIENT_ID=scientificresearch-api \
KEYCLOAK_USERNAME=<启用的人员工号> \
KEYCLOAK_PASSWORD=<临时测试密码> \
BUSINESS_Y_PROJECT_ID=<真实纵向项目立项编号> \
BUSINESS_X_PROJECT_ID=<真实横向项目编号> \
BUSINESS_Y_FLOW_TEMPLATE_ID=<真实纵向项目流程模板编号> \
./deploy/verify-business-contract.sh
```

上线前应至少做一次可恢复性验收。先用 `EXPORT_FILE=/secure/backup/scientificresearch-$(date +%Y%m%d-%H%M%S).dump ./deploy/export-postgres-data.sh` 生成完整导出，再把同一个文件传给 `BACKUP_FILE=/secure/backup/scientificresearch-...dump EXPECTED_SCHEMA_VERSION=106 ./deploy/verify-postgres-backup.sh` 做隔离恢复验收。导出脚本以临时文件原子提交 dump、`.sha256` 和 `.json` 元数据，默认拒绝覆盖已有备份；如确实需要覆盖，必须显式设置 `ALLOW_BACKUP_OVERWRITE=true`。验收脚本只读取已有备份，不会默默覆盖它；会先核对 sidecar 校验和和元数据，再在唯一命名的 PostgreSQL 18 临时容器中使用一次性 tmpfs（也可显式传入 `RESTORE_VOLUME`）执行 `pg_restore`，检查 schema 版本、表/视图数量、退休例程、预算处理函数、代理并发触发器及 077–090 新增特征表，最后写出 `${BACKUP_FILE}.restore-report.json`，作为本次恢复演练报告。源数据库和正式数据卷不会被修改，临时资源只清理由本次运行创建的对象；若指定的恢复容器或卷已存在，脚本会安全失败。Docker 操作和恢复容器等待均有超时，分别可用 `DOCKER_COMMAND_TIMEOUT_SECONDS`、`RESTORE_READY_TIMEOUT_SECONDS` 调整。运行时门禁可附加 `CHECK_BACKUP_ARTIFACTS=true BACKUP_FILE=... ./deploy/check-runtime.sh`，只读核对备份、校验和及 schema 元数据，不代替隔离恢复演练。

迁移数据核对可执行 `DATABASE_CONTAINER=2018-7-24-db-1 ./deploy/verify-migration-data-diff.sh`。脚本只读比较 `source-sqlserver/data/001-058.json` 与 PostgreSQL 对应表的行数，逐项核对项目主键和预算批准金额，并检查预算、到账、报销和项目参与人的流程绑定；默认同时更新 `reports/migration-diff-current.tsv`、`reports/migration-diff-current.summary.json` 并保留带时间戳的差异归档和摘要。报告包含 `difference_class`、`review_action` 和 `.summary.json` 摘要（含运行库 schema 版本），用于区分治理扩展、运行时追加、目标库追加与阻断性源数据/绑定/金额差异；不自动删除或改写业务数据。默认 `STRICT_MIGRATION_DIFF=true` 仅阻断源数据缺失、项目主键/预算金额不一致、孤立绑定和移动通道策略违规；如需把所有数量差异都作为门禁，设置 `STRICT_MIGRATION_DIFF_ALL=true`。

迁移窗口 068–073 可在运行库恢复后单独执行 `DATABASE_CONTAINER=2018-7-24-db-1 ./deploy/verify-migration-window.sh`；该脚本只读检查申报、HRP、过程预警、论文采集、生物样本、移动推送和绩效公式关键对象，不应用迁移、不写入业务数据。


外部同步 worker 默认关闭。完成外部系统注册、认证引用和适配器端点验收后，才设置 `ExternalSync__Enabled=true`；认证密钥通过 `ExternalSystems__Secrets__<认证引用>` 注入，不写入数据库。八类系统各只能有一个启用且配置完整的适配器，重复启用会阻断 worker。worker 领取任务时使用行锁和幂等键，失败按指数退避并在达到最大次数后标记失败；`ExternalSync__LeaseTimeoutSeconds`（默认 900 秒）会回收进程崩溃后遗留的 `处理中` 任务，终态失败可由 `/Operations/获取外部同步失败告警` 查看。

外部适配器发布前可执行 `DATABASE_CONTAINER=2018-7-24-db-1 ./deploy/verify-external-adapter-contract.sh`。脚本只读检查 HR、财务、采购、伦理、合同、人才库、生物样本库和论文采集八类系统的登记、唯一启用、地址、认证引用、超时/重试参数以及待处理队列幂等键，不发起供应商请求；worker 已启用但核心系统未完成配置时会阻断验收。

P2 外部集成静态门禁可执行 `./deploy/verify-external-p2-contract.sh`。它检查八类适配器的字段映射和真实回执清单、`X-Request-ID`/`Idempotency-Key`、2xx 成功与 408/429/5xx 重试分类、租约回收、响应敏感字段脱敏和回调签名边界；不连接数据库、不读取密钥、不调用供应商。worker 将供应商响应摘要写入队列前统一把 token、密码、Cookie、Authorization、API key 等替换为 `[REDACTED]`。P2 真实验收仍必须由运行管理员使用脱敏测试账号逐项验证 2xx、4xx、5xx、超时、幂等重放和业务回执，并在八类系统全部完成前保持 `ExternalSync__Enabled=false`。

Docker 恢复后，HR/财务首批登记可先执行 `HR_ADAPTER_ENDPOINT=https://<hr-adapter-host>/research FINANCE_ADAPTER_ENDPOINT=https://<finance-adapter-host>/research ./deploy/register-core-adapters.sh --dry-run`。该脚本默认不访问数据库、不读取密钥、不启用 worker；受控写入必须显式增加 `--apply CONFIRM_EXTERNAL_ADAPTER_WRITE=YES ADAPTER_OPERATOR_ID=<启用人员编号>`，并默认将两行登记为停用。认证引用默认分别为 `HIS_HR_TOKEN`、`FINANCE_API_TOKEN`，实际值只由部署环境注入；生产地址必须使用 HTTPS，HTTP 仅能显式用于 localhost 联调。登记后仍需执行外部适配器、经费/HRP 只读预检和“测试外部系统连接”，以供应商 2xx、幂等重放和业务回执作为真实联调证据，不能把登记成功或预检通过当作接口已联通。静态边界由 `./deploy/verify-core-adapter-config-contract.sh` 检查。

经费控制静态契约可执行 `./deploy/verify-finance-controls-contract.sh`，检查发票查重/OCR 状态、预算调整审批、年度结算封账和付款回写接口。经费与 HRP 专项契约可执行 `DATABASE_CONTAINER=2018-7-24-db-1 ./deploy/verify-finance-adapter-contract.sh`；默认会阻断缺失结构、历史示例付款逻辑和疑似示例流水，使用 `--strict` 时还会把适配器唯一启用和采购订单关联字段缺失作为门禁。该脚本只读核对 HR/财务/采购适配器、HRP 四类中间表、发票 OCR 状态、预算调整/年度结算/付款回写结构和采购订单关联字段，并检测历史 HRP 过程中的示例付款逻辑及疑似示例流水；它不发起网络请求、不写入队列或业务表。通过专项预检仍需供应商字段协议、真实 OCR/采购订单接口和脱敏财务账号回放。

菜单路径回归可执行 `DATABASE_CONTAINER=2018-7-24-db-1 APP_BASE_URL=http://127.0.0.1:16379 CHECK_HTTP=true ./deploy/verify-menu-paths.sh`，它会比较 PostgreSQL `权限.路径` 与 `wwwroot/IMIS/views`，并逐个验证运行中的页面返回 200。

发布后的只读响应时间烟测可执行 `PERF_ITERATIONS=8 ./deploy/verify-performance-baseline.sh`。它会测量 live/ready、两个现代 SPA 路由和一个旧版生命周期页面的 p95；脚本带上浏览器的 `Accept: text/html` 请求头，以验证 SPA 回退实际返回页面。该脚本是轻量基线，不替代正式压力测试。

需要在验收环境执行有界并发只读压力烟测时，可使用 `LOAD_REQUESTS=200 LOAD_CONCURRENCY=20 LOAD_MAX_P95_MS=1000 LOAD_MAX_ERROR_RATE=0 APP_BASE_URL=https://research.example.org ./deploy/verify-performance-load.sh`。脚本只访问健康探针和公开 HTML 路由，输出请求数、并发度、p95、错误率及前十条错误；它不调用登录后接口，也不代表正式容量、稳定性或峰值压力测试。

统一认证和权限边界可执行 `APP_BASE_URL=http://127.0.0.1:16379 KEYCLOAK_BASE_URL=http://127.0.0.1:8180 ./deploy/verify-auth-boundaries.sh`。脚本只读检查 live/ready、Realm discovery，以及代理、字段权限、外部系统、评价奖励、统计、HR、财务、成果治理/转化和伦理接口的未登录 401 响应。准备真实 Keycloak 令牌后设置 `AUTH_BOUNDARIES_ROLE_TESTS=true AUTH_BOUNDARIES_NON_ADMIN_TOKEN=... AUTH_BOUNDARIES_ADMIN_TOKEN=...`，还会对字段授权、外部系统和预算异常三个无写入管理接口验证普通角色 403、管理角色 200；脚本不会生成或记录令牌。

六角色本地验收可先运行 `./deploy/prepare-scientificresearch-uat-tokens-watch.sh` 收集六个现代登录页令牌，再执行 `./deploy/run-scientificresearch-uat.sh`。封装脚本从权限为 600 的 `/private/tmp/scientificresearch-uat-tokens.env` 读取令牌，自动映射科研人员、科室负责人、科研秘书、专家、财务管理员和系统管理员，执行只读权限矩阵和伦理 P2 探针；令牌内容不会打印，也不会写入报告。令牌过期时会明确返回 401，不会把静态契约当作真实验收。

需要独立伦理账号时，可使用 `KEYCLOAK_ADMIN_USERNAME=... KEYCLOAK_ADMIN_PASSWORD=... ./deploy/provision-ethics-accounts.sh` 创建或幂等更新 `uat-ethics-secretary`（伦理秘书）和 `uat-ethics-committee`（伦理委员）。脚本同时写入 PostgreSQL 的人员/角色映射，并在 Keycloak 绑定同名 Realm 角色；临时密码只写入权限为 600 的 `/private/tmp/scientificresearch-ethics-accounts.env`，首次登录必须修改密码并配置 TOTP。该脚本不会把管理员令牌或密码写入仓库或验收报告。

提供真实 Keycloak 测试账号和真实只读验收项目后，可执行 `APP_BASE_URL=http://127.0.0.1:16379 KEYCLOAK_AUTHORITY=http://keycloak.localhost:8180/realms/scientific-research KEYCLOAK_CLIENT_ID=scientificresearch-api KEYCLOAK_USERNAME=... KEYCLOAK_PASSWORD=... BUSINESS_Y_PROJECT_ID=... BUSINESS_X_PROJECT_ID=... BUSINESS_Y_FLOW_TEMPLATE_ID=... ./deploy/verify-business-contract.sh`，脚本会回归项目申报/任务书、纵向/横向生命周期、经费控制与年度结算、伦理会议与档案、人员团队、成果转化/网推、绩效奖励和统计接口；脚本只读，不创建或修改业务数据。三个业务编号现在必须显式提供，不再回退到示例项目。
权限和预算治理可在运行库上执行 `./deploy/verify-governance.sh`；脚本在单个事务中验证代理授权、有效性、单条撤销、按人员批量回收、并发触发器和预算异常登记，最后回滚全部测试写入。

应用提供 `/health/live`（进程存活）和 `/health/ready`（PostgreSQL 可连接）两个探针；反向代理和容器编排应使用这两个地址区分重启与流量接入。每个响应都会返回 `X-Request-ID`，排查问题时请保留该编号。启用 Keycloak 后，登录页会通过 `/Access/OidcConfig` 发起 Authorization Code + PKCE 登录，回调后调用 `/Access/Me` 建立本地人员上下文；前端只在当前标签页保存短期 access token 和 refresh token，不保存密码，旧版接口收到一次 401 时会自动刷新令牌并重试。
默认使用进程内缓存以保持单容器本地启动简单；多实例部署时应使用强密码启用 `docker-compose.redis.yml` 覆盖层，让会话进入 Redis：`REDIS_PASSWORD=... docker compose -f docker-compose.yml -f docker-compose.published.yml -f docker-compose.redis.yml up -d`。Redis 密码只通过环境文件或密钥注入，不写入仓库。

生产环境的 `ForwardedHeaders:TrustedProxies` 只填写实际反向代理地址；应用会拒绝未列入白名单的转发头。生产配置默认关闭 Swagger，容器同时以 `/health/ready` 作为 Docker healthcheck，并返回 `X-Content-Type-Options`、`X-Frame-Options`、`Referrer-Policy` 和 `Permissions-Policy` 安全响应头。

启用统一身份时，在环境文件中配置 `Keycloak__Enabled=true`、`Keycloak__Authority`、`Keycloak__Audience` 和公开客户端 `Keycloak__ClientId`。生产客户端参数可参考 [`keycloak-client-scientificresearch.production.example.json`](keycloak-client-scientificresearch.production.example.json)：使用 Authorization Code + PKCE（S256），关闭 direct access grants 和 service account，只登记正式 HTTPS 回调。Keycloak 用户名必须与 `人员.工号` 一致；应用只映射已启用的本地人员，不自动开户。前端完成 OIDC/PKCE 回调后将令牌写入当前标签页的 `sessionStorage`，现有 AJAX 封装会自动添加 Bearer 头并在一次 401 时刷新令牌，再调用 `/Access/Me`。上线前应使用测试账号完成令牌校验、人员映射和受保护接口验收。

Keycloak 切换前可执行 `KEYCLOAK_AUTHORITY=... APP_BASE_URL=... KEYCLOAK_ACCESS_TOKEN=... ./deploy/verify-keycloak.sh`。脚本先验证 Realm discovery、issuer 和 token endpoint；也可临时提供 `KEYCLOAK_CLIENT_ID`、`KEYCLOAK_USERNAME`、`KEYCLOAK_PASSWORD`（confidential client 再提供 `KEYCLOAK_CLIENT_SECRET`）取得测试令牌。脚本会检查 audience、issuer、`azp` 客户端归属、人员标识、`/health/ready`、`/Access/Me`、工作台和预算异常对账接口，不输出令牌；未提供令牌或测试客户端凭据时以退出码 2 结束。若同时提供 Keycloak 管理员短期 token，设置 `KEYCLOAK_ADMIN_TOKEN` 后脚本还会核对真实 Realm 客户端的标准流程、public client、service account、implicit flow、direct access grants、回调地址、Web Origin 和 PKCE 方法；生产默认要求 `KEYCLOAK_EXPECT_DIRECT_ACCESS_GRANTS=false`。正式 HTTPS 验收会自动要求 `S256`、精确单一 HTTPS 回调和对应 Web Origin。需要把 MFA 纳入上线门禁时，增加 `KEYCLOAK_REQUIRE_MFA=true KEYCLOAK_MFA_AMR=otp,mfa`，脚本会要求令牌的 `amr` 包含配置的方法。应用的 `/health/keycloak` 在启用 Keycloak 后也会检查 discovery，失败时 `/health/ready` 不应接收流量。

正式发布前可直接执行统一只读门禁 `./deploy/verify-local-release.sh`；它会依次检查 Docker/应用运行时、发布包与迁移序列、068–090 迁移特征契约（静态）、068–073 迁移窗口安全契约（静态，禁止示例金额、默认身份、论文/样本种子、模拟移动回执和 HRP 付款回放）、示例数据回退契约、绩效奖励发放契约、153 个菜单路径 HTTP、发布安全契约、未登录认证边界、PostgreSQL Schema 106、治理 smoke、逐项目迁移差异报告、六角色/MFA/权限回收静态 UAT 契约、科研伦理流程静态契约、成果状态凭证契约、奖励财务闭环契约、八类外部适配器契约、`verify-adapter-readiness-contract.sh`（真实地址/认证引用/2xx 回执/幂等重放与 worker 开关边界）和外部 worker 静态契约（幂等键、请求追踪、租约回收、重试分类以及 PostgreSQL 队列锁顺序）、备份恢复静态契约（Schema 106、custom dump 校验、隔离恢复与演练报告）。首页统计、登录审计、角色列表、权限代理和成果/样本写入接口不得以示例记录替代真实数据；奖励标记为“已支付”前必须完成财务回写并保留财务和支付凭证。迁移特征契约只证明仓库文件包含目标对象，不能替代运行库迁移验收。该脚本不会重启容器、写入业务数据或向供应商发起请求。需要单项诊断时再分别执行 `check-runtime.sh`、`verify-publish-parity.sh`、`verify-migration-feature-contract.sh`、`verify-no-demo-fallback.sh`、`verify-evaluation-reward-contract.sh`、`verify-release.sh` 或对应专项脚本。该门禁默认要求应用 `/Access/OidcConfig` 返回 `enabled=true`，并执行 `verify-startup-contract.sh`；如果确需密码应急模式，必须显式设置 `REQUIRE_KEYCLOAK=false CHECK_KEYCLOAK_DISCOVERY=false`，其结果只能作为应急维护验收，不能作为统一认证验收。
统一门禁还会执行 `verify-performance-baseline.sh`，对 live/ready、现代 SPA 和旧版生命周期页面做只读 p95 响应时间烟测；它是发布基线，不替代正式压力测试。

Docker 主机空间不足时可先运行 `./deploy/prune-docker-cache.sh` 只盘点可回收空间；确认后使用 `DRY_RUN=false ./deploy/prune-docker-cache.sh` 清理悬空镜像和指定时间以前的 BuildKit 缓存。脚本不会删除容器或卷，因此不会触碰 PostgreSQL、上传文件、密钥、Redis 或 Keycloak 数据；清理后仍应重新执行 `./deploy/check-runtime.sh`。
正式发布前可先用短期管理员令牌幂等导入客户端：`KEYCLOAK_AUTHORITY=https://sso.example.org/realms/scientific-research KEYCLOAK_ADMIN_TOKEN=... ./deploy/apply-keycloak-client.sh`。脚本只接受生产客户端模板中声明的单一 HTTPS 回调、对应 Web Origin、PKCE `S256` 和关闭的高风险流程；`KEYCLOAK_DRY_RUN=true` 可先检查目标 Realm 而不写入，HTTP 仅能通过显式 `ALLOW_HTTP=true` 用于本地测试。随后执行发布验收命令；生产模式还会要求真实 Realm 客户端核对通过，默认拒绝 direct access grants，并检查客户端回调与 OIDC 公共配置一致。`KEYCLOAK_ACCESS_TOKEN` 或测试客户端凭据沿用同一组环境变量。本机 HTTP 验收时将 `REQUIRE_HTTPS` 显式设为 `false`。

代理审批使用短时、可撤销的数据库授权记录。`权限代理范围` 配置是白名单，当前默认仅开放 `项目审批`，前端从 `GET /Access/获取代理范围` 获取选项，避免自由输入未实现范围。代理人调用纵向或横向项目变更审批接口时，需携带 `X-Proxy-Grantor-Id` 请求头；服务端按固定范围 `项目审批` 检查有效期和撤销状态，审计记录同时保存实际登录人和被代理授权人。撤销后同一请求头立即返回未授权，不能继续代办。纵向/横向项目的申报、立项、中检、变更、结题、经费到账和报销明细读取也统一经过项目级访问判断；项目负责人、参与人、流程发起人、当前步骤指定人/角色或科研管理角色可访问，其余人员返回 403。

Docker 部署文件位于仓库根目录，适合有 Docker 环境的服务器；systemd/Nginx 方案仍适合直接运行发布目录的场景。 使用 `docker-compose.published.yml` 时，应用和 PostgreSQL 会设置 `restart: unless-stopped`，开发环境基础 Compose 不自动改变重启策略。发布覆盖层还会限制 json-file 容器日志，默认每个文件 10 MiB、保留 3 个文件，可通过 `CONTAINER_LOG_MAX_SIZE` 和 `CONTAINER_LOG_MAX_FILES` 调整。两种方式都必须提供 PostgreSQL 连接模板和数据库白名单，连接模板中的 `Database={0}` 由系统替换为允许的数据库名。

预算异常对账由 `GET /Expense/获取预算异常对账` 提供异常汇总，`GET /Expense/获取预算异常处理` 查询处理记录，`POST /Expense/处理预算异常对账` 更新状态、责任人和说明；仅科研管理、财务和系统管理角色可操作。迁移 041 会把历史上误记为报销流程模板 600 的记录按纵向、横向或会议项目归一到 410、450 或 210，避免预算、到账和报销被拆成不同对账行。登录后的“权限代理与预算对账”页面提供代理授权、单条撤销、按人员批量回收和异常处理登记入口。人员禁用会自动撤销其全部有效代理，管理员也可通过 `POST /Access/回收人员代理` 批量回收并留下操作人和原因。系统管理员、科研管理员可通过 `GET /BaseInfo/获取数据完整性异常` 查看迁移后历史重复键，清理前需逐项确认关联数据。

HRP 中间库协同的默认动作是 `push_only`；历史 `push_and_replay`/`replay` 会被服务端拒绝，直到真实财务适配器完成协议验收，防止写入示例付款记录。`GET /Operations/获取经费适配器契约检查` 可查看该阻断原因、OCR/采购订单字段和结构门禁。

上传接口使用统一的扩展名白名单、10 MB 单文件限制、空文件拒绝、随机文件名和目录边界校验；客户端输入错误（例如不支持的扩展名或空文件）统一返回 HTTP 400，避免与服务器故障的 HTTP 500 混淆。

本机统一认证只能通过 `deploy/start-local.sh` 启动。该脚本固定合并 `docker-compose.yml`、`docker-compose.published.yml` 和 `deploy/docker-compose.keycloak.local.yml`，启动前检查合并后的 Keycloak Authority、`/login` 回调和 `Keycloak__Enabled=true`，启动后再检查 `/health/keycloak`、`/health/ready` 和 `/Access/OidcConfig`。直接执行基础 `docker compose up` 不属于本地 SSO 验收路径；基础 Compose 要求环境文件显式设置 `KEYCLOAK_ENABLED`，不会再把缺省值静默解释为密码登录。本机若已有 Keycloak 服务，覆盖层通过 `host-gateway` 访问 `http://keycloak.localhost:8180/realms/scientific-research`。本机启动命令（默认复用已构建镜像；需要重建时加 `BUILD_APP=true`）：

```bash
./deploy/start-local.sh
# 需要重新生成 .NET 10 发布包时：
BUILD_APP=true ./deploy/start-local.sh
```

验收要求用户名对应本地 `人员.工号` 且 `是否启用=true`；禁用人员即使能从 Keycloak 取得 token，也会被 `/Access/Me` 拒绝。
