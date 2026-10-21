#!/usr/bin/env bash
set -euo pipefail

# Read-only local release gate. It does not rebuild or restart containers.
app_base_url="${APP_BASE_URL:-http://127.0.0.1:16379}"
database_container="${DATABASE_CONTAINER:-2018-7-24-db-1}"

echo '== 运行时预检 =='
APP_BASE_URL="$app_base_url" \
KEYCLOAK_LOCAL_OVERLAY=true \
REQUIRE_KEYCLOAK=true \
CHECK_KEYCLOAK_DISCOVERY=true \
EXPECTED_KEYCLOAK_AUTHORITY="${EXPECTED_KEYCLOAK_AUTHORITY:-http://keycloak.localhost:8180/realms/scientific-research}" \
./deploy/check-runtime.sh

echo '== 启动边界契约（静态） =='
./deploy/verify-startup-contract.sh

echo '== 发布包与迁移序列 =='
./deploy/verify-publish-parity.sh

echo '== 迁移特征契约（静态） =='
./deploy/verify-migration-feature-contract.sh

echo '== 068-073 迁移窗口安全契约（静态） =='
./deploy/verify-migration-window-contract.sh

echo '== 示例数据回退契约 =='
./deploy/verify-no-demo-fallback.sh

echo '== 工作台/移动端角色任务契约（静态） =='
./deploy/verify-workbench-contract.sh

echo '== 绩效奖励发放契约 =='
./deploy/verify-evaluation-reward-contract.sh

echo '== 六角色/MFA/权限回收 UAT 契约（静态） =='
./deploy/verify-auth-uat-contract.sh

echo '== 科研伦理流程契约 =='
./deploy/verify-ethics-workflow-contract.sh

echo '== 科研伦理 P2 验收矩阵（静态/可选只读运行时） =='
./deploy/verify-ethics-p2-acceptance.sh

echo '== 人员进修 P0 契约 =='
./deploy/verify-further-education-p0-contract.sh

echo '== 科研平台与服务目录契约 =='
./deploy/verify-research-platform-service-contract.sh

echo '== 首页组件与业务催办契约 =='
./deploy/verify-dashboard-component-contract.sh

echo '== 横向合同治理契约 =='
./deploy/verify-horizontal-contract-governance-contract.sh

echo '== 教育部科研统计契约 =='
./deploy/verify-education-statistics-contract.sh

echo '== 成果/IP/获奖契约 =='
./deploy/verify-achievement-ip-award-contract.sh

echo '== 文档/服务/消息契约 =='
./deploy/verify-document-messaging-contract.sh

echo '== 经费到账认领契约 =='
./deploy/verify-finance-incoming-contract.sh

echo '== 成果状态凭证契约 =='
./deploy/verify-achievement-status-evidence-contract.sh

echo '== 奖励财务闭环契约 =='
./deploy/verify-reward-finance-closure-contract.sh

echo '== 101-105 欠缺功能闭环契约 =='
./deploy/verify-gap-closure-contract.sh

echo '== PostgreSQL 备份恢复静态契约 =='
./deploy/verify-postgres-backup-contract.sh

echo '== PostgreSQL 迁移应用契约（静态） =='
./deploy/verify-migration-apply-contract.sh

echo '== 菜单源码与 HTTP =='
APP_BASE_URL="$app_base_url" CHECK_HTTP=true ./deploy/verify-menu-paths.sh

echo '== 发布 HTTP 契约 =='
APP_BASE_URL="$app_base_url" \
KEYCLOAK_ENABLED=true \
KEYCLOAK_DISCOVERY_ONLY=true \
KEYCLOAK_AUTHORITY="${KEYCLOAK_AUTHORITY:-http://keycloak.localhost:8180/realms/scientific-research}" \
KEYCLOAK_CLIENT_ID="${KEYCLOAK_CLIENT_ID:-scientificresearch-api}" \
EXPECT_SWAGGER_DISABLED=true ./deploy/verify-release.sh

echo '== 只读性能基线（非压力测试） =='
APP_BASE_URL="$app_base_url" ./deploy/verify-performance-baseline.sh

echo '== 性能压力烟测契约（静态） =='
./deploy/verify-performance-load-contract.sh

echo '== 未登录认证边界 =='
APP_BASE_URL="$app_base_url" ./deploy/verify-auth-boundaries.sh

echo '== PostgreSQL 迁移与治理 =='
DATABASE_CONTAINER="$database_container" ./database/postgresql/verify_migration.sh
DATABASE_CONTAINER="$database_container" MIN_SCHEMA_VERSION=68 MAX_SCHEMA_VERSION=73 ./deploy/verify-migration-window.sh
./deploy/verify-governance.sh

echo '== 迁移逐项目差异报告 =='
DATABASE_CONTAINER="$database_container" ./deploy/verify-migration-data-diff.sh

echo '== 外部适配器契约 =='
DATABASE_CONTAINER="$database_container" ./deploy/verify-external-adapter-contract.sh

echo '== 外部 worker 静态契约 =='
./deploy/verify-external-worker-contract.sh

echo '== 外部集成 P2 静态契约 =='
./deploy/verify-external-p2-contract.sh

echo '== HR/财务核心适配器配置契约（静态） =='
./deploy/verify-core-adapter-config-contract.sh
echo '== 外部适配器就绪边界契约（静态） =='
./deploy/verify-adapter-readiness-contract.sh

echo '== 经费/HRP 专项契约 =='
DATABASE_CONTAINER="$database_container" ./deploy/verify-finance-adapter-contract.sh

echo '== 经费控制契约（静态） =='
./deploy/verify-finance-controls-contract.sh

echo '== 专家可用时间与邀请契约 =='
./deploy/verify-expert-invitation-contract.sh

echo '== 人员科研能力/导师团队契约 =='
./deploy/verify-people-expertise-contract.sh

echo '== 项目全生命周期契约 =='
./deploy/verify-project-lifecycle-contract.sh

echo '== 横向项目全生命周期契约 =='
./deploy/verify-horizontal-lifecycle-contract.sh

echo '== 电子签章与正式电子档案契约 =='
./deploy/verify-document-provider-contract.sh

echo '== 系统附件与方案B对象存储契约 =='
./deploy/verify-attachment-storage-contract.sh

echo '本地发布全量验收通过'
