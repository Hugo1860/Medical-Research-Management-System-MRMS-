# PostgreSQL 例程迁移报告

原始对象来自 `数据库备份文件/科研管理2018-7-24.bak`。表结构、数据和视图已生成迁移脚本；下表按当前迁移边界记录已转换对象和仍保留的遗留过程。

## 对象统计

| SQL Server 对象 | 数量 | 当前状态 |
|---|---:|---|
| SQL_STORED_PROCEDURE | 82 | 74 个过程已转换为 PostgreSQL 函数，8 个 SQL Server 动态过程已安全退休并登记 |
| SQL_SCALAR_FUNCTION | 4 | 4 个已转换为 PostgreSQL，0 个待转换 |
| SQL_INLINE_TABLE_VALUED_FUNCTION | 38 | 38 个已转换，0 个待转换 |
| SQL_TABLE_VALUED_FUNCTION | 2 | 2 个已转换为 PostgreSQL，0 个待转换 |
| TRIGGER | 35 | 35 个重复数据拦截触发器已转换，历史重复键需按清单治理 |

## 必须处理的兼容差异

- 存储过程的表值参数（`tt_*`）需要改为 `jsonb` 参数并使用 `jsonb_to_recordset`，不能把 .NET `DataTable` 直接传给 Npgsql。
- `MERGE`、`OUTPUT INSERTED`、`SCOPE_IDENTITY()`、`TRY/CATCH`、`RAISERROR/THROW` 需要分别改写为 `INSERT ... ON CONFLICT`、`RETURNING`、`RETURNING 编号`、PL/pgSQL 异常块和异常类型。
- `OUTER APPLY` 已在自动转换视图中转换为 `LEFT JOIN LATERAL`；视图脚本已在 PostgreSQL 18 容器执行通过。
- 触发器需要改写为 PostgreSQL trigger function 后再绑定到表。

## 已完成的运行时兼容层

- `migrations/005_crud_routines.sql` 将 54 个单表增改、删除、启禁用过程改为 `jsonb` 参数函数，保留 `DataTable` 数组的调用形态，并对身份列为 0 的新增记录使用 PostgreSQL sequence。
- `migrations/006_workflow_routines.sql` 已实现流程发起、完成步骤、修改密码、纵向申报立项和角色人员同步。
- `migrations/007_flow_query_functions.sql` 已实现流程待办查询及步骤处理权限判断，供分页查询直接使用。
- `migrations/008_simple_query_functions.sql` 已实现角色人员、角色权限、项目详情和下级字典查询函数，供权限和项目详情接口直接使用。
- `migrations/009_workflow_and_notice_query_functions.sql` 已实现步骤、步骤可用操作、通知公告、下载中心和人员权限查询函数，供工作流及办公接口直接使用。
- `migrations/010_expense_arrival_query_function.sql` 已实现经费到账列表及其流程进度查询函数，供经费到账接口直接使用。
- `migrations/011_result_and_score_query_functions.sql` 已实现项目名称、成果作者、成果项目和专家评分查询函数，供成果详情接口直接使用。
- `migrations/012_appointment_and_horizontal_project_functions.sql` 已实现学术任职和横向项目列表查询函数，供人员和横向项目接口直接使用。
- `migrations/013_vertical_project_list_functions.sql` 已实现纵向项目申报和立项列表查询函数，供纵向项目接口直接使用。
- `migrations/014_result_list_functions.sql` 已实现论文、著作、专利和获奖列表查询函数，供成果接口直接使用。
- `migrations/015_midterm_and_reimbursement_functions.sql` 已实现纵向项目中检和报销查询函数。
- `migrations/016_vertical_change_close_functions.sql` 已实现纵向项目变更和结题查询函数。
- `migrations/017_horizontal_change_close_functions.sql` 已实现横向项目变更和结题查询函数。
- `migrations/018_academic_activity_query_functions.sql` 已实现主办讲座、讲座反馈、参加会议、参加会议反馈和历史可用操作查询函数；5 个内联表值函数已全部转换。
- `migrations/019_workbench_summary.sql` 已实现按当前人员过滤的统一工作台摘要，返回待办、未读通知、处理中流程和下一步事项。
- `migrations/020_operation_audit.sql` 已增加追加式操作审计表；.NET 中间件对已登录用户的写请求记录请求编号、路径、状态、耗时和客户端地址，不记录请求体、Cookie 或令牌。
- `migrations/021_project_access.sql` 已增加项目级访问判断，并接入纵向、横向项目详情接口；访问者必须是负责人、参与人、流程发起人或科研管理角色。
- `migrations/022_budget_reconciliation.sql` 已增加预算、到账和报销异常对账函数，并接入财务/科研管理角色限定的查询接口。
- `migrations/023_permission_delegation.sql` 已增加有起止时间、可撤销的权限代理记录和有效性函数。
- `migrations/024_project_change_apply_routines.sql` 已转换纵向、横向项目变更审批后的主项目更新和旧值回写过程，供审批接口的附加动作调用。
- `migrations/025_permission_and_notice_routines.sql` 已转换角色人员增删、角色权限同步/删除和通知公告接收过程，覆盖权限管理和通知详情接口的运行时调用。
- `migrations/026_subject_and_budget_type_routines.sql` 已转换学科删除和经费模板支出类型兼容过程。
- `migrations/027_people_import_routines.sql` 已转换按部门名称导入人员以及新增人员同步到角色过程，并复用人员 JSONB 增改函数。
- `migrations/028_paper_import_routines.sql` 已转换论文和自定义论文导入过程，按刊物名称、作者姓名解析主数据并在同一事务内写入成果作者。
- `migrations/029_remaining_scalar_helpers.sql` 已转换通知条件人数、通知条件判断和字符串拆分兼容函数；4 个标量函数和 2 个表值函数均已具备 PostgreSQL 实现。
- `migrations/030_permission_lifecycle.sql` 已增加代理撤销原因、撤销人、人员禁用后的自动回收和管理角色批量回收函数。
- `migrations/031_budget_reconciliation_workflow.sql` 已增加预算异常对账处理表、状态流转、责任人和处理说明函数。
- `migrations/032_permission_revoke_actor.sql` 已补齐批量回收的操作人记录。
- `migrations/033_permission_revoke_authorization_fix.sql` 已修正重复撤销时仍需经过授权检查的边界。
- `migrations/034_login_and_role_compatibility.sql` 已将登录和角色人员多结果过程改为 JSONB 兼容函数。
- `migrations/035_legacy_uniqueness_triggers.sql` 已将 35 个 SQL Server 重复数据拦截触发器转为 PostgreSQL 行级校验，更新非唯一字段不会被历史重复数据阻断。
- `migrations/036_data_integrity_report.sql` 已增加 `v1_数据完整性异常`，用于在清理前定位历史重复键。
- `migrations/037_schema_version.sql` 已登记当前 schema 版本，并在登记前校验关键表、视图和预算对账对象；应用 readiness 使用同一版本号阻止旧数据库接流量。
- `migrations/038_retire_legacy_dynamic_routines.sql` 已为 8 个 SQL Server 动态过程建立明确的 PostgreSQL 兼容退休标记和机器可读状态函数；它们不再接受任意表名、字段或 where 片段，避免把动态 SQL 安全问题带入生产。
- `migrations/039_governance_validation.sql` 已限制代理双方必须是启用人员、拒绝同一范围重叠授权，并要求预算异常处理记录对应当前异常且责任人/处理人有效。
- `migrations/040_permission_overlap_concurrency.sql` 已增加事务级 advisory lock 和数据库触发器，阻止并发写入造成同一范围的重叠有效代理。
- .NET 项目详情读取已将纵向/横向中检、变更、结题和报销明细接入 `fn_人员可访问项目`；未授权人员返回 403，避免按编号直读造成越权。
- .NET 查询扩展已改为 Npgsql 参数化 SQL，列表和分页不再调用 SQL Server 动态过程；动态来源名称和筛选字段经过白名单式标识符解析。

原始 82 个过程现已全部有明确迁移结论：74 个转换为 PostgreSQL 函数，8 个 SQL Server 动态脚本/旧分页过程以 `fn_迁移遗留例程状态()` 登记为“已退休”。现有 .NET 查询层已使用白名单标识符和参数化 SQL；历史重复键已通过 `v1_数据完整性异常` 暴露，清理前不自动改名或删除科研主数据。

## 原始定义

`source-sqlserver/objects.json` 保存了恢复后的 212 个非系统对象定义，可按名称逐项转换并与 SQL Server 结果对照。
