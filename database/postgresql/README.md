# PostgreSQL 迁移包

迁移源是 `数据库备份文件/科研管理2018-7-24.bak`。`source-sqlserver/` 保存从隔离 SQL Server 容器恢复出的元数据、对象定义和数据 JSON；`migrations/` 是可重复执行的 PostgreSQL 脚本。

执行顺序：

迁移文件按三位版本号排序执行。全新数据库使用以下命令，已有数据卷必须使用仓库根目录的增量执行器，避免再次导入基础数据：

```bash
# 全新空库（仅对明确创建的空数据库执行）
for migration in $(find database/postgresql/migrations -maxdepth 1 -type f -name '[0-9][0-9][0-9]_*.sql' | sort -V); do
  psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f "$migration"
done

# 已有 Docker 数据卷（只执行当前版本之后的脚本）
DATABASE_CONTAINER=2018-7-24-db-1 ./deploy/apply-postgres-migrations.sh
```

`apply-postgres-migrations.sh` 会读取 `系统迁移版本`，检查仓库中的连续版本号，逐个执行待迁移脚本并在每个脚本后核对登记版本；静态发布前还应运行 `deploy/verify-migration-window-contract.sh`，它会阻止 068–073 的示例金额、默认身份、论文/样本种子、模拟移动回执和 HRP 付款回放；它拒绝降级、跳过版本和超过仓库最新版本的目标。先设置 `DRY_RUN=true` 可只查看待执行的 068–095 脚本，不会写入数据库。升级前应先用 `deploy/export-postgres-data.sh` 生成可恢复备份；该过程不会删除或重建 PostgreSQL 数据卷。

原始迁移已验证 58 张业务表、37,241 条数据、51 个视图可以在 PostgreSQL 18 中创建和查询；当前运行库已完成 Schema 106 验收（222 张表、56 个视图），应用后仍需按发布批次复核对象数量。068–092 增量迁移分别覆盖纵向申报、HRP/经费、伦理、成果治理、移动推送、评价奖励、专家可用时间、外部邀请、横向项目生命周期、伦理 P0/P1 治理和 P2 匿名表决汇总；其中 083 增加成果治理不可变历史与重复成果合并审计，084 增加专家可用时间与外部专家邀请队列，085 将 HRP 中间库双向协同收敛为只推送待同步记录的安全过程，086 增加成果状态来源凭证，087 增加奖励支付批次和财务回写流水，088 增加横向项目延期/暂停/终止、研究报告和合同档案留痕，089 增加伦理委员正式指派/回避/出席、法定人数、主席确认、决定门禁和治理审计，090 增加被指派委员最小披露范围、受控文件实际校验元数据和已归档必须核验约束，091 增加匿名表决标记与管理端汇总查询，均拒绝伪造凭证。

变更审批接口支持通过 `X-Proxy-Grantor-Id` 使用有效的 `项目审批` 代理授权，授权人、代理人和撤销状态会写入操作审计。人员禁用会自动回收其作为授权人或被代理人的有效代理；预算异常可通过处理接口记录状态、责任人和说明；`/BaseInfo/获取数据完整性异常` 提供历史重复键清单。项目详情访问除负责人、参与人、流程发起人和科研管理角色外，也会识别当前未完成步骤的指定人及步骤角色，避免专家评审人员因项目级过滤被错误拒绝。流程完成写入前会在同一事务内再次调用 `fn_某人是否可以操作某步骤`，防止直接调用接口绕过待办授权。`024_project_change_apply_routines.sql` 已补齐纵向、横向项目变更审批后的主项目更新和旧值回写逻辑，`025_permission_and_notice_routines.sql` 已补齐角色人员、角色权限同步/删除和通知公告接收过程，`026_subject_and_budget_type_routines.sql` 已补齐学科删除和经费模板支出类型兼容过程，`027_people_import_routines.sql` 已补齐按部门名称导入人员以及新增人员同步到角色，`028_paper_import_routines.sql` 已补齐论文和自定义论文导入，`029_remaining_scalar_helpers.sql` 已补齐通知条件人数、通知条件判断和字符串拆分兼容函数。

原始备份包含 58 张业务表、37,241 条数据和 51 个视图；当前运行库验收为 Schema 106。完成 083–091 后应重新执行对象数量、完整性和业务差异校验；增量迁移只新增或更新业务对象，不删除原有业务表和数据。

迁移 041 修正报销流程模板（600）与项目预算模板（410/450）范围不同导致的对账拆行问题。

迁移 042 为科研管理员补齐科研办公、项目、经费、成果、学术活动和科研统计菜单权限，不授予组织架构或系统管理根菜单；迁移 043 为统一工作台增加角色、下一步责任人和办理提示。

重新从备份导出时，先恢复 `.bak` 到 SQL Server，再运行：

```bash
python3 generate_schema.py
python3 generate_data.py
PYTHONPATH=/tmp/sqlglot python3 generate_views.py
```

生成脚本会保留中文表名、列名和原始身份值。生产导入前应在独立数据库执行行数、主键、视图结果和业务接口对照，不要直接覆盖原 SQL Server 数据库。

## Docker 部署

仓库根目录的 `docker-compose.yml` 会启动 PostgreSQL 18，并在全新数据卷初始化时按 `001` 到 `106` 的文件名顺序执行全部迁移；应用容器通过 `connectionString__ScientificResearch_Release` 连接到 `db` 服务；`Postgres__ExpectedSchemaVersion` 默认值为 `106`，readiness 会拒绝低于配置版本的数据库。已有数据卷不会再次执行初始化脚本，应使用 `deploy/apply-postgres-migrations.sh` 补执行 096–106。复制 `.env.docker.example` 为 `.env`，设置强密码后先执行 `./deploy/verify-docker-env.sh`（启用 Redis 覆盖层时设置 `CHECK_REDIS=true`），再执行：

```bash
docker compose up -d --build
curl -f http://127.0.0.1:16379/swagger/v1/swagger.json
```

Dockerfile 已为 NuGet 配置 BuildKit 缓存、120 秒请求超时和最多 4 次重试，恢复和发布阶段共用缓存，避免 `NETSDK1064` 找不到 `StackExchange.Redis`。若构建服务器仍不能访问 NuGet，可先在有 .NET 10 SDK 的环境执行 `dotnet publish`，再使用 `Dockerfile.published` 和 `docker-compose.published.yml` 构建运行镜像。`APP_PLATFORM` 默认是 `linux/amd64`，ARM 服务器应先发布 `linux-arm64` 并调整该参数。

## 迁移验证

验证脚本还会检查仓库迁移文件从 001 到 106 连续、当前版本脚本名与 106 文件一致；缺号、跳号或版本登记异常会立即失败。`database/postgresql/verify_migration.sh` 默认只读检查当前数据库的迁移版本、148 张表、56 个视图、关键数据和治理函数；宿主机没有 `psql` 时可设置 `DATABASE_CONTAINER=2018-7-24-db-1 DATABASE_NAME=ScientificResearch_Model DATABASE_USER=postgres`，脚本会改用数据库容器内的 `psql`。只有显式设置 `FRESH_DATABASE_URL` 才会在明确的空数据库中按顺序重放迁移，避免误改运行库。应用 `/health/ready` 同时校验 `Postgres__ExpectedSchemaVersion`，当前版本要求为 106。

备份恢复验收使用 `deploy/export-postgres-data.sh` 和 `deploy/verify-postgres-backup.sh`：前者以临时文件原子生成 custom-format dump、SHA-256 sidecar 和 schema 元数据，后者只读取已有 dump，在临时 PostgreSQL 18 容器恢复并检查版本、对象、关键治理函数、代理触发器及 077–091 新增特征表，成功后写出恢复演练 JSON 报告。两个脚本都不会向源库或正式数据卷写入；恢复脚本对临时资源使用唯一名称，遇到已存在的容器或卷会失败而不删除。
治理回归可使用 `deploy/verify-governance.sh`，它在数据库事务中验证代理授权、单条撤销、按人员批量回收、有效性、并发触发器和预算异常处理，完成后回滚测试数据。

数据差异审计使用 `deploy/verify-migration-data-diff.sh`：除 001–058 源表行数、逐项目主键、预算批准金额和预算/到账/报销/参与人流程绑定外，还检查 077 的任务书/电子档案与项目绑定、078 的预算调整/年度结算绑定、082 的移动通道示例凭据和模拟成功状态、083 的成果治理历史与合并审计结构，以及 084 的专家可用时间与邀请审计结构、085 的 HRP 只推送安全过程、086 的成果状态来源凭证与核验审计、087 的奖励支付批次/支付流水和对账约束，以及 088 的横向项目生命周期/研究报告/档案材料结构和 091 的伦理匿名表决标记。报告生成 `reports/migration-diff-current.tsv` 与 `reports/migration-diff-current.summary.json`（摘要含运行库 schema 版本）；分类为阻断性差异时只告警或按 `STRICT_MIGRATION_DIFF` 退出，不自动删除或修复历史数据。

已有 Docker 数据卷不会重复执行 `/docker-entrypoint-initdb.d`。升级已有数据卷时，先完成备份，再执行 `./deploy/apply-postgres-migrations.sh`；脚本按已登记版本选择后续迁移，逐个检查版本登记，拒绝降级、跳过版本和迁移文件缺失。可先用 `DRY_RUN=true ./deploy/apply-postgres-migrations.sh` 查看待执行脚本，确认后再运行正式命令，最后重启应用并执行发布验收。
