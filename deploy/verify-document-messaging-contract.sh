#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MIGRATION="$ROOT_DIR/database/postgresql/migrations/098_document_service_messaging.sql"
CONTROLLER="$ROOT_DIR/ScientificResearch/ScientificResearch/Controllers/DocumentMessagingController.cs"
API="$ROOT_DIR/frontend/src/api/document-messaging.ts"
VIEW="$ROOT_DIR/frontend/src/views/office/document-sharing.vue"
ROUTER="$ROOT_DIR/frontend/src/router/index.ts"
LEGACY="$ROOT_DIR/ScientificResearch/ScientificResearch/wwwroot/IMIS/views/科研办公/科研办公/科研文档共享与消息中心.html"

for file in "$MIGRATION" "$CONTROLLER" "$API" "$VIEW" "$ROUTER" "$LEGACY"; do
  [[ -f "$file" ]] || { echo "文档消息契约失败：缺少 $file" >&2; exit 1; }
done
for marker in 'CREATE TABLE IF NOT EXISTS "科研文档"' 'CREATE TABLE IF NOT EXISTS "科研文档版本"' 'CREATE TABLE IF NOT EXISTS "科研文档授权"' 'CREATE TABLE IF NOT EXISTS "科研文档下载日志"' 'CREATE TABLE IF NOT EXISTS "消息通道"' 'CREATE TABLE IF NOT EXISTS "消息模板"' 'CREATE TABLE IF NOT EXISTS "消息发送队列"' 'CREATE TABLE IF NOT EXISTS "消息发送尝试"' '版本状态' '撤销原因'; do
  grep -Fq "$marker" "$MIGRATION" || { echo "文档消息契约失败：迁移缺少 $marker" >&2; exit 1; }
done
for marker in '保存科研文档' '保存科研文档版本' '保存科研文档授权' '下载科研文档' '撤销科研导入导出日志' '保存消息通道' '加入消息发送队列' '登记消息真实回执' '催办科研服务申请'; do
  grep -Fq "$marker" "$CONTROLLER" || { echo "文档消息契约失败：控制器缺少 $marker" >&2; exit 1; }
done
grep -Fq "DocumentSharing" "$ROUTER" || { echo "文档消息契约失败：现代路由未注册" >&2; exit 1; }
if grep -n '已发送.*真实|模拟成功|演示成功' "$MIGRATION" >/dev/null; then
  echo "文档消息契约失败：迁移不应伪造消息发送成功" >&2
  exit 1
fi
echo "科研文档、服务催办和消息通道契约校验通过"
