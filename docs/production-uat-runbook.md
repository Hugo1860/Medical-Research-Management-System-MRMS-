# 科研管理系统生产前验收运行手册

这份手册用于在真实 Keycloak 测试账号、供应商地址、认证引用和字段协议准备后执行上线前验收。没有这些输入时只执行本地只读门禁，保持 `ExternalSync__Enabled=false`。

## 1. 本地发布基线

```bash
./deploy/check-runtime.sh
# 如需把最近一次备份纳入运行时只读门禁：
# CHECK_BACKUP_ARTIFACTS=true BACKUP_FILE=/secure/backup/scientificresearch-<timestamp>.dump ./deploy/check-runtime.sh
APP_BASE_URL=http://127.0.0.1:16379 CHECK_HTTP=true ./deploy/verify-menu-paths.sh
./deploy/verify-external-adapter-contract.sh
./deploy/verify-external-worker-contract.sh
./deploy/verify-postgres-backup-contract.sh
DATABASE_CONTAINER=2018-7-24-db-1 ./database/postgresql/verify_migration.sh
```

预期结果：应用和 Keycloak 就绪、153 个菜单路径 HTTP 200、Schema 100、外部 worker 关闭或配置检查通过；移动通道未取得真实网关回执时不得显示“已发送”。任务书内部确认和项目本地档案核验也必须保持其边界：未完成供应商合同、协议版本、HTTPS 端点和真实适配器多角色验收时，电子签章/正式电子档案门禁返回 `503 DOCUMENT_PROVIDER_NOT_READY`，不产生“已签章”或“已托管”记录。

## 2. Keycloak 与权限验收

为系统管理员、科研秘书、科室负责人、普通科研人员、专家和财务管理员准备独立测试账号。生产环境使用 Authorization Code + PKCE（S256），并按需要启用 MFA：

在尚未取得真实管理员令牌或测试用户令牌时，只运行 Realm discovery 只读检查。该模式只证明 Realm 的 issuer、authorization endpoint、token endpoint 和 JWKS 可访问，不代表客户端、角色、MFA 或业务权限已经验收：

```bash
KEYCLOAK_DISCOVERY_ONLY=true \
KEYCLOAK_AUTHORITY=http://keycloak.localhost:8180/realms/scientific-research \
./deploy/verify-keycloak.sh
```

完整验收需要管理员令牌检查实际客户端配置；需要用户令牌检查 claims、Audience、`azp` 和本地人员映射。业务角色仍以科研系统 PostgreSQL 的“角色/人员角色”为准，Keycloak Realm/client 角色只有在部署方明确配置 `KEYCLOAK_REQUIRED_ROLES` 时才作为额外门禁：

```bash
KEYCLOAK_AUTHORITY=https://sso.example.org/realms/scientific-research \
APP_BASE_URL=https://research.example.org \
KEYCLOAK_CLIENT_ID=scientificresearch-api \
KEYCLOAK_ADMIN_TOKEN="$KEYCLOAK_ADMIN_TOKEN" \
KEYCLOAK_EXPECT_REDIRECT_URI=https://research.example.org/IMIS/views/login.html \
KEYCLOAK_EXPECT_WEB_ORIGIN=https://research.example.org \
KEYCLOAK_EXPECT_PKCE_METHOD=S256 \
KEYCLOAK_REQUIRED_DISCOVERY_SCOPES=openid,profile \
KEYCLOAK_REQUIRED_CLIENT_SCOPES=openid,profile \
KEYCLOAK_REQUIRED_CLAIMS=iss,sub,azp,aud,preferred_username \
KEYCLOAK_REQUIRED_ROLES="科研管理员" \
KEYCLOAK_ACCESS_TOKEN="$KEYCLOAK_ACCESS_TOKEN" \
./deploy/verify-keycloak.sh
```

`KEYCLOAK_ADMIN_TOKEN` 与 `KEYCLOAK_ACCESS_TOKEN` 只从短期 Secret/环境变量注入，脚本不会打印或持久化令牌。普通模式没有用户令牌时以退出码 2 结束，表示“只完成 discovery、真实验收未完成”；不要把该结果记录为登录验收通过。

逐账号确认：登录人员与本地工号一致；菜单、项目范围和字段授权符合角色；撤销角色、禁用人员和撤销代理后，旧令牌不能继续访问业务接口；专家账号不能看到被回避项目的评分对象。

离岗与代理回收必须按以下顺序使用专用验收账号和可回滚数据回放：

1. 登录人员 A，保存登录后的访问令牌和请求 ID；由管理员在 Keycloak 禁用 A 或移除相应角色，并在 HR 全量同步中将 A 标记 `active=false`。
2. 调用应用 `/Access/Me` 和一个受保护业务接口，确认 A 被禁用后新请求返回 401/403；将失效前保存的令牌通过只读探针复核，预期同样返回 401/403：

   ```bash
   KEYCLOAK_REVOKED_ACCESS_TOKEN="$OLD_TOKEN" \
   KEYCLOAK_REVOKED_ENDPOINT=/Access/Me \
   KEYCLOAK_EXPECT_REVOKED_TOKEN_STATUS=401,403 \
   KEYCLOAK_AUTHORITY=https://sso.example.org/realms/scientific-research \
   APP_BASE_URL=https://research.example.org \
   ./deploy/verify-keycloak.sh
   ```

   若要把同一旧令牌的复核扩展到多个受保护接口，可使用逗号分隔的
   `KEYCLOAK_REVOKED_ENDPOINTS=/Access/Me,/Expense/获取预算异常对账`。代理撤销
   需要在专用验收数据上先执行 `/Access/撤销代理` 或 `/Access/回收人员代理`，再
   提供被代理人的旧令牌、授权人编号和原业务请求体进行只读结果复核；脚本不会
   自行创建或撤销代理，也不会打印令牌：

   ```bash
   KEYCLOAK_REVOKED_PROXY_ACCESS_TOKEN="$OLD_DELEGATE_TOKEN" \
   KEYCLOAK_REVOKED_PROXY_GRANTOR_ID="$GRANTOR_PERSON_ID" \
   KEYCLOAK_REVOKED_PROXY_ENDPOINT='/YProject/审核并应用纵向项目变更' \
   KEYCLOAK_REVOKED_PROXY_PAYLOAD='{"附加数据":123,"步骤编号":1,"状态值":1}' \
   KEYCLOAK_EXPECT_REVOKED_PROXY_STATUS=401,403 \
   KEYCLOAK_AUTHORITY=https://sso.example.org/realms/scientific-research \
   APP_BASE_URL=https://research.example.org \
   ./deploy/verify-keycloak.sh
   ```

3. 由 A 授权 B 一个“项目审批”代理，记录代理编号；执行 `/Access/撤销代理`，再用 `X-Proxy-Grantor-Id` 回放同一业务请求，确认返回 401/403，并检查审计中的撤销人、原因、时间和请求 ID。
4. 通过 HR 离岗同步或 `/Access/回收人员代理` 批量回收 A 的代理，再检查 A 作为授权人和被代理人的有效代理数为 0。该步骤会写入业务数据，必须使用专用验收人员，不能用在研项目和生产账号。

令牌中的 `iss` 必须等于配置的 Realm Authority，`aud` 必须包含 `scientificresearch-api`，`azp` 必须等于 `scientificresearch-api`，并且至少包含 `sub`、`preferred_username`；启用 `KEYCLOAK_REQUIRE_MFA=true` 时还必须有 `amr=otp` 或 `amr=mfa`。没有真实令牌时不得声称这些声明已通过。

未登录边界和无写入角色范围检查可单独执行。普通角色令牌对字段授权、外部系统和预算异常管理接口应返回 403，系统管理员令牌应返回 200：

```bash
AUTH_BOUNDARIES_ROLE_TESTS=true \
AUTH_BOUNDARIES_NON_ADMIN_TOKEN="$NON_ADMIN_TOKEN" \
AUTH_BOUNDARIES_ADMIN_TOKEN="$ADMIN_TOKEN" \
APP_BASE_URL=https://research.example.org \
KEYCLOAK_BASE_URL=https://sso.example.org \
./deploy/verify-auth-boundaries.sh
```

脚本不会生成或持久化令牌；未设置角色验收开关时只检查未登录请求的 401。代理授权/撤销、人员离岗批量回收、字段规则变更通知和旧令牌失效要在真实账号上完成回放并保留请求 ID。

如需执行六类真实账号的只读 GET 矩阵，另行注入六个短期访问令牌并显式打开开关。脚本按角色验证一个可读接口和一个应返回 403 的管理接口；令牌只从环境变量读取，不会出现在日志、文件或请求 URL 中：

```bash
AUTH_BOUNDARIES_SIX_ROLE_TESTS=true \
AUTH_BOUNDARIES_RESEARCHER_TOKEN="$RESEARCHER_TOKEN" \
AUTH_BOUNDARIES_DEPARTMENT_LEAD_TOKEN="$DEPARTMENT_LEAD_TOKEN" \
AUTH_BOUNDARIES_SECRETARY_TOKEN="$SECRETARY_TOKEN" \
AUTH_BOUNDARIES_EXPERT_TOKEN="$EXPERT_TOKEN" \
AUTH_BOUNDARIES_FINANCE_TOKEN="$FINANCE_TOKEN" \
AUTH_BOUNDARIES_SYSTEM_ADMIN_TOKEN="$SYSTEM_ADMIN_TOKEN" \
AUTH_BOUNDARIES_REQUIRE_TOKEN_CLAIMS=true \
AUTH_BOUNDARIES_EXPECT_ISSUER=https://sso.example.org/realms/scientific-research \
AUTH_BOUNDARIES_REQUIRE_MFA=true \
AUTH_BOUNDARIES_MFA_AMR=otp,mfa \
APP_BASE_URL=https://research.example.org \
./deploy/verify-auth-boundaries.sh
```

矩阵默认覆盖：六个角色各自的 `/Access/Me` 本地人员映射，以及科研人员、科室负责人和科研秘书的工作台摘要；专家评审待办；财务管理员预算异常；系统管理员外部系统配置。系统管理员是全局管理角色，默认没有可安全预设为 403 的 GET 端点，因此只验证其管理接口返回 200；部署方如有明确的系统管理员拒绝接口，可额外设置 `AUTH_BOUNDARIES_ADMIN_DENY_GET=/path` 纳入 403 检查。设置 `AUTH_BOUNDARIES_REQUIRE_TOKEN_CLAIMS=true` 会检查每个令牌的 `iss`、`sub`、`azp`、`aud`、`preferred_username`、有效期和目标 audience；再设置 `AUTH_BOUNDARIES_REQUIRE_MFA=true` 才会逐令牌检查 `amr` 包含 `otp` 或 `mfa`。脚本只做 JWT claims 结构预检，签名和撤销仍由应用 JwtBearer/Keycloak 实际验证。若六个令牌任一缺失或 claims/MFA 不符，脚本会明确失败，不会降级为模拟令牌或跳过该角色。

本地六角色验收也可以使用封装命令：先运行 `./deploy/prepare-scientificresearch-uat-tokens-watch.sh`，完成六个现代登录页账号的复制后，再运行 `./deploy/run-scientificresearch-uat.sh`。该命令从权限为 600 的 `/private/tmp/scientificresearch-uat-tokens.env` 读取短期令牌，依次执行六角色权限边界和伦理 P2 只读探针；不会打印令牌或写入业务数据。令牌过期时应先重新登录收集，401 结果不能记为权限验收通过。

## 3. 项目、经费和绩效业务回归

使用脱敏验收数据依次完成：纵向申报预检查、申报版本比较、专家回避检查、评分提交、立项、中检、变更、延期/暂停/终止、研究报告、档案归集；再验证预算、到账、报销、异常对账、合同付款节点和绩效生成/复核/申诉/奖励/导出。

绩效奖励支付按“创建批次→登记财务受理→逐笔成功/失败回写→批次对账”回放；提交、回写和对账接口只接受真实外部批次/流水/凭证和幂等键，不调用或伪造供应商支付。至少验证一次重复幂等键、失败原因、重试后的成功回写以及数量或金额不一致的对账结果。

只读业务回归命令：

```bash
APP_BASE_URL=https://research.example.org \
KEYCLOAK_AUTHORITY=https://sso.example.org/realms/scientific-research \
KEYCLOAK_CLIENT_ID=scientificresearch-api \
KEYCLOAK_USERNAME="$KEYCLOAK_USERNAME" \
KEYCLOAK_PASSWORD="$KEYCLOAK_PASSWORD" \
BUSINESS_Y_PROJECT_ID="$BUSINESS_Y_PROJECT_ID" \
BUSINESS_X_PROJECT_ID="$BUSINESS_X_PROJECT_ID" \
BUSINESS_Y_FLOW_TEMPLATE_ID="$BUSINESS_Y_FLOW_TEMPLATE_ID" \
./deploy/verify-business-contract.sh
```

写入型动作必须使用专用验收项目和可回滚数据，验收后清理或归档，不得使用真实在研项目。

## 4. 八类外部系统联调

先在治理中心登记系统代码、基础地址、认证方式和认证引用，再执行：

```bash
DATABASE_CONTAINER=2018-7-24-db-1 ./deploy/verify-external-adapter-contract.sh
./deploy/verify-external-worker-contract.sh
DATABASE_CONTAINER=2018-7-24-db-1 ./deploy/verify-finance-adapter-contract.sh --strict
```

经费专项预检只读核对 HR/财务/采购适配器、HRP 中间表、发票 OCR 状态、预算调整/年度结算/付款回写和采购订单关联字段，并检查历史 HRP 过程是否仍包含示例付款逻辑。迁移 085 应用后，数据库过程只把纵向项目和预算推送到待同步队列，不读取或写入示例付款流水；在迁移尚未应用或预检发现结构缺口时，不得把本地中间库记录当作真实财务联调证据；`POST /Expense/Hrp中间库双向协同` 的 `push_and_replay`/`replay` 动作会被阻断，只允许 `push_only`。

每类系统至少回放一条成功、4xx、5xx、超时、重复幂等键和业务冲销/撤回事件：

- HR：人员新增、部门/岗位变更、离岗禁用和代理回收。
- 财务：经费到账、实际付款、冲销和对账差异。
- 采购：采购申请、订单、验收和付款事件。
- 伦理：批准、过期、撤回和附件路径。
- 合同：签订、变更、付款节点和终止。
- 人才库：研究专长、技术服务、导师关系和合作意愿更新。
- 生物样本库：样本入库、领用、归还和状态变更，确认不传输直接身份信息。
- 论文采集：DOI/PMID 重复、作者候选、期刊识别和认领队列；分别回放 Crossref 与 PubMed。CNKI 当前只解析用户上传的导出文件，不视为 CNKI 实时适配器；万方尚未登记，不能把文件导入结果表述为供应商同步成功。

确认队列中的请求 ID、幂等键、尝试次数、退避时间、最终状态和错误摘要。仅当核心系统配置完整、供应商返回协议通过且密钥由 Secret/环境变量注入后，才设置 `ExternalSync__Enabled=true`。

## 5. 迁移和恢复验收

保存逐表、逐项目和预算核对报告；当前报告中的孤立参与人记录需由业务负责人确认后才能清理。上线前完成一次隔离备份恢复：

已有数据卷执行 068–090 前，先完成备份，再执行 `DRY_RUN=true DATABASE_CONTAINER=2018-7-24-db-1 ./deploy/apply-postgres-migrations.sh` 复核待执行序列；确认目标版本和备份可恢复后，再去掉 `DRY_RUN=true` 应用迁移。脚本逐个核对登记版本，任一步失败即停止，不会跳过中间版本。

运行库恢复并完成基础健康检查后，可在验收环境执行只读并发烟测：`LOAD_REQUESTS=200 LOAD_CONCURRENCY=20 LOAD_MAX_P95_MS=1000 LOAD_MAX_ERROR_RATE=0 APP_BASE_URL=https://research.example.org ./deploy/verify-performance-load.sh`。该结果只作为有界压力信号，正式容量测试仍需按医院并发模型、数据规模和监控方案单独执行。

迁移审计还会生成稳定的 TSV 和 JSON 摘要。`difference_class` 会把治理扩展、运行时追加、目标库追加与源数据缺失、项目/预算绑定孤立、金额不一致分开；`STRICT_MIGRATION_DIFF=true` 只阻断后几类高风险差异，`STRICT_MIGRATION_DIFF_ALL=true` 才把所有数量差异作为门禁。082 检查移动通道示例凭据和“模拟成功”状态，083 检查成果治理历史和重复合并审计，084 检查专家可用时间与邀请审计，085 检查 HRP 只推送安全过程，086 检查成果状态来源凭证和核验审计，087 检查奖励支付批次、支付流水和对账约束，088 检查横向项目生命周期、研究报告和档案材料结构。审计脚本只读，不自动删除或修复历史业务记录：

```bash
DATABASE_CONTAINER=2018-7-24-db-1 ./deploy/verify-migration-data-diff.sh
STRICT_MIGRATION_DIFF=true DATABASE_CONTAINER=2018-7-24-db-1 ./deploy/verify-migration-data-diff.sh
```

```bash
backup_file=/secure/backup/scientificresearch-$(date +%Y%m%d-%H%M%S).dump
EXPORT_FILE="$backup_file" ./deploy/export-postgres-data.sh
EXPECTED_SCHEMA_VERSION=106 BACKUP_FILE="$backup_file" ./deploy/verify-postgres-backup.sh
```

导出脚本默认不覆盖已有文件，并以原子方式提交 dump、`.sha256` 和 `.json` 元数据；验收脚本只读取已有 dump，先核对校验和/元数据，再在唯一命名的 PostgreSQL 18 隔离容器中恢复。它不会操作正式数据库卷；如果 `RESTORE_CONTAINER` 或 `RESTORE_VOLUME` 已存在会直接失败，避免误删。成功后会生成 `${backup_file}.restore-report.json`，作为本次恢复演练报告，记录 schema 版本、备份 SHA-256、临时恢复资源和对象检查结果。验收记录应包含账号角色、数据集版本、接口请求 ID、供应商响应摘要、报告文件校验值和最终放行人。报告与 dump 应分别归档，恢复临时资源由脚本在退出时清理。
