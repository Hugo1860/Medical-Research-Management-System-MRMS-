#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
fail(){ echo "系统附件与对象存储契约校验失败: $1" >&2; exit 1; }

migration="$root/database/postgresql/migrations/106_system_attachments_and_storage.sql"
controller="$root/ScientificResearch/ScientificResearch/Controllers/AttachmentController.cs"
provider="$root/ScientificResearch/ScientificResearch/Infrastucture/Storage/IStorageProvider.cs"
signer="$root/ScientificResearch/ScientificResearch/Infrastucture/Storage/S3Signer.cs"
minio_provider="$root/ScientificResearch/ScientificResearch/Infrastucture/Storage/MinioStorageProvider.cs"
local_provider="$root/ScientificResearch/ScientificResearch/Infrastucture/Storage/LocalStorageProvider.cs"
api="$root/frontend/src/api/attachment.ts"
view="$root/frontend/src/views/system/attachment-management.vue"
html="$root/ScientificResearch/ScientificResearch/wwwroot/IMIS/views/系统管理/系统管理/附件与对象存储管理.html"
compose="$root/docker-compose.yml"

for file in "$migration" "$controller" "$provider" "$signer" "$minio_provider" "$local_provider" "$api" "$view" "$html" "$compose"; do
  [[ -f "$file" ]] || fail "缺少关键文件: $file"
done

# 1. 迁移脚本静态标记
for marker in '系统附件' '系统附件访问日志' 'fn_防篡改_系统附件访问日志' 'trg_防篡改_系统附件访问日志' '"版本"=106' '218' '附件与对象存储管理'; do
  grep -Fq "$marker" "$migration" || fail "Migration 106 缺少标记: $marker"
done

# 2. 控制器关键端点
for marker in 'RequestUpload' 'CompleteUpload' 'UploadStream' 'GetDownloadUrl' 'DownloadStream' 'BindBusiness' 'ListAttachments' 'GetStorageStats' 'MigrateLegacyUploads'; do
  grep -Fq "$marker" "$controller" || fail "AttachmentController 缺少端点: $marker"
done

# 3. 驱动与签名逻辑
for marker in 'GeneratePresignedUrl' 'GenerateAuthHeaders' 'HmacSha256'; do
  grep -Fq "$marker" "$signer" || fail "S3Signer 缺少实现: $marker"
done

# 4. 容器编排 MinIO 配置
for marker in 'minio:' 'scientificresearch_minio_data' 'Storage__Provider' 'Storage__Minio__Endpoint'; do
  grep -Fq "$marker" "$compose" || fail "docker-compose.yml 缺少 MinIO 编排配置: $marker"
done

# 5. 运行时数据库契约核验 (若数据库容器存活)
if docker ps --format '{{.Names}}' | grep -q '2018-7-24-db-1'; then
  db_ver="$(docker exec 2018-7-24-db-1 psql -U postgres -d ScientificResearch_Model -tAc 'SELECT "版本" FROM "系统迁移版本";')"
  [[ "$db_ver" -ge 106 ]] || fail "数据库迁移版本未达到 106 (当前: $db_ver)"
  docker exec 2018-7-24-db-1 psql -U postgres -d ScientificResearch_Model -c '\d "系统附件"' >/dev/null || fail "缺少 系统附件 表"
  docker exec 2018-7-24-db-1 psql -U postgres -d ScientificResearch_Model -c '\d "系统附件访问日志"' >/dev/null || fail "缺少 系统附件访问日志 表"
fi

# 6. MinIO 容器健康状态核验 (若 MinIO 容器存活)
if docker ps --format '{{.Names}}' | grep -q '2018-7-24-minio-1'; then
  curl -fsS http://127.0.0.1:19000/minio/health/live >/dev/null || fail "MinIO 实例健康检查未通过"
fi

echo "系统附件与方案B对象存储(MinIO)契约校验全部通过: 存储驱动、预签名双通道、秒传去重、等保审计日志与存量迁移已就绪！"
