# 纵向项目申报详情与全周期兼容搜索修复验收报告 (2026-11-02)

## 1. 概述与缺陷排查

针对纵向科研项目生命周期模块的运行异常，本次执行了根因排查、双端兼容修复、数据库迁移补齐、隔离回归验证以及离线发布包封签对齐。

### 缺陷 1：纵向项目申报详情 500 异常
- **异常现象**：调用 `获取纵向项目申报详情` 与 `通过申报详情获取可用的纵向项目立项详情` 接口时，PostgreSQL 抛出 `42P01: relation "v2_纵向项目申报_详情" does not exist`。
- **根因**：后端 `YProjectController.cs` 中使用 Dapper ORM 执行 `Db.GetModelByIdSpAsync<v2_纵向项目申报_详情>`，依赖同名视图；数据库迁移历史中仅有 `v2_纵向项目申报` 基础视图，缺少 `v2_纵向项目申报_详情`。
- **修复**：
  1. 新增数据库迁移 `database/postgresql/migrations/121_vertical_application_detail_view_and_compat.sql`，定义视图 `v2_纵向项目申报_详情`，关联 `纵向项目申报` 与 `v1_人员_带部门名`，补充申请人姓名、部门等关联属性。
  2. 补齐 `v2_报销` 兼容视图（对应 `v2_项目经费报销`），消除别名映射隐患。
  3. 同步补齐基线初始化 `database/postgresql/migrations/004_views.sql`。
  4. 将系统架构版本提升至 `121`，同步对齐所有配置文件与部署契约。

### 缺陷 2：纵向全周期（中检、变更、结题）列表搜索参数名不匹配
- **异常现象**：前端在生命周期列表界面输入项目名称进行搜索时，无法过滤出目标记录。
- **根因**：前端 `vertical-lifecycle.ts` 透传搜索字段 `Like项目名称`，而立项、中检、变更、结题底层数据源为 `v2_纵向项目立项_列表`，字段为 `项目中文名称`；后端 Filter 模型的绑定属性名为 `Like项目中文名称`。
- **修复**：
  1. 前端 `frontend/src/api/vertical-lifecycle.ts` 在 `getPagedMidterms`、`getPagedChanges`、`getPagedCloseouts` 中建立参数规范化映射：`filter?.Like项目中文名称 ?? filter?.Like项目名称`。
  2. 后端 `Models/Custom/YProject.cs` 在 `纵向项目中检Filter`、`纵向项目变更Filter`、`纵向项目结题Filter` 中新增 `Like项目名称` 兼容属性。
  3. 控制器 `YProjectController.cs` 的 `分页获取授权纵向列表` 增加阶段判定，动态切换申报阶段（`项目名称`）与后续阶段（`项目中文名称`）。

---

## 2. 隔离回归验证与测试证据

在独立容器环境下执行全流程回归（`deploy/run-isolated-regressions.sh`）：

- **测试环境**：临时 PostgreSQL 18 容器（无宿主端口暴露、tmpfs 存储、无生产数据交互）；
- **迁移重放**：001 至 121 迁移全量依次回放并通过结构检查；
- **后端行为回归测试套件**：
  - `ExternalSyncRegression`: 31 个场景通过；
  - `InstrumentRegression`: 22 项检查通过；
  - `ProposalImportRegression`: PDF 解析、上限制约、暂存与提交全流程通过；
  - `VerticalScopeRegression`: 立项授权边界通过；
  - `VerticalLifecycleScopeRegression`: 申报、中检、变更、结题全阶段授权、跨人员隔离、`Like项目名称` 兼容检索、`v2_纵向项目申报_详情` 视图加载全部通过（71+ 项检查）；
  - `VerticalWriteScopeRegression`: 57 项写入阶段授权与流程绑定检查通过；
  - `MenuMoveRegression`: 生产迁移与菜单权限放置检查通过；
- **前端单元测试**：37 个测试文件、165 项单测全部通过；
- **源码清单校验**：1420 个源码与配置哈希无未受控漂移。

证据日志位置：`reports/local-regression-20261102142733-36645/summary.json`（`status: passed`）。

---

## 3. 本地离线发布包与静态门禁封签

运行 `deploy/build-local-release.sh` 完成离线交付物编译与封签：
- **发布目录**：`publish/linux-x64`
- **内容清单**：
  - SPA 静态资产（128 个文件）与 IMIS 经典界面融合至 `wwwroot`（共 860 个静态文件）；
  - 后端 .NET 10 Release 编译产物；
  - `appsettings.json`（已声明 `ExpectedSchemaVersion = 121`）；
  - `release-source-manifest.json`（已执行 `seal` 锁定完整哈希）。
- **门禁验证**：`deploy/verify-publish-parity.sh` 与所有只读安全/灾备契约校验全部通过。
