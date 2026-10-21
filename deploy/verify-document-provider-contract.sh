#!/usr/bin/env bash
set -euo pipefail

# 电子签章/正式电子档案是外部供应商能力。该脚本只校验契约门禁，
# 不会调用供应商、不上传文件，也不会把内部确认结果当成法律签章。
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
controller="$repo_root/ScientificResearch/ScientificResearch/Controllers/YProjectController.cs"
models="$repo_root/ScientificResearch/ScientificResearch/Models/Custom/YProject.cs"
settings="$repo_root/ScientificResearch/ScientificResearch/appsettings.json"

[[ -f "$controller" ]] || { echo "电子文档契约校验失败: 找不到 YProjectController.cs" >&2; exit 1; }
[[ -f "$models" ]] || { echo "电子文档契约校验失败: 找不到 YProject.cs" >&2; exit 1; }
[[ -f "$settings" ]] || { echo "电子文档契约校验失败: 找不到 appsettings.json" >&2; exit 1; }

required_controller_markers=(
  '获取纵向项目电子签章服务状态'
  '获取纵向项目正式档案服务状态'
  '提交纵向项目任务书电子签章'
  '提交纵向项目正式电子档案'
  'DOCUMENT_PROVIDER_NOT_READY'
  '不构成法律电子签章'
)
for marker in "${required_controller_markers[@]}"; do
  if ! grep -Fq "$marker" "$controller"; then
    echo "电子文档契约校验失败: 控制器缺少标记 $marker" >&2
    exit 1
  fi
done
for marker in '纵向项目任务书电子签章请求' '纵向项目正式电子档案提交请求'; do
  if ! grep -Fq "$marker" "$models"; then
    echo "电子文档契约校验失败: 模型缺少标记 $marker" >&2
    exit 1
  fi
done

python3 - "$settings" <<'PY'
import json, sys
path = sys.argv[1]
with open(path, encoding='utf-8-sig') as handle:
    data = json.load(handle)
for section in ('ElectronicSignature', 'ElectronicArchive'):
    cfg = data.get(section)
    if not isinstance(cfg, dict):
        raise SystemExit(f'电子文档契约校验失败: 缺少 {section} 配置段')
    for key in ('Enabled', 'Provider', 'Endpoint', 'ProtocolVersion', 'AdapterReady', 'RequireHttps'):
        if key not in cfg:
            raise SystemExit(f'电子文档契约校验失败: {section}.{key} 未定义')
    # 默认发布配置必须 fail-closed；只有部署方显式配置并验收后才能开放。
    if cfg['Enabled'] is not False or cfg['AdapterReady'] is not False:
        raise SystemExit(f'电子文档契约校验失败: 默认 {section} 必须保持关闭且未验收')
print('电子文档契约静态校验通过: 默认关闭、未验收、内部确认不具法律电子签章效力')
PY

: "${CHECK_HTTP:=false}"
: "${APP_BASE_URL:=http://127.0.0.1:16379}"
: "${DOCUMENT_PROVIDER_TOKEN:=}"
: "${DOCUMENT_PROVIDER_EXPECTED_STATUS:=503}"

check_http_normalized="$(printf '%s' "$CHECK_HTTP" | tr '[:upper:]' '[:lower:]')"
if [[ "$check_http_normalized" != "true" && "$check_http_normalized" != "1" && "$check_http_normalized" != "yes" ]]; then
  echo "电子文档契约 HTTP 校验跳过: 设置 CHECK_HTTP=true；不会发起任何供应商请求。"
  exit 0
fi

tmp_file="$(mktemp)"
trap 'rm -f "$tmp_file"' EXIT
for path in '/YProject/获取纵向项目电子签章服务状态' '/YProject/获取纵向项目正式档案服务状态'; do
  if [[ -n "$DOCUMENT_PROVIDER_TOKEN" ]]; then
    status="$(curl -sS -o "$tmp_file" -w '%{http_code}' --max-time "${HTTP_TIMEOUT_SECONDS:-10}" \
      -H "Authorization: Bearer $DOCUMENT_PROVIDER_TOKEN" \
      "$APP_BASE_URL$path")"
    expected="$DOCUMENT_PROVIDER_EXPECTED_STATUS"
  else
    status="$(curl -sS -o "$tmp_file" -w '%{http_code}' --max-time "${HTTP_TIMEOUT_SECONDS:-10}" "$APP_BASE_URL$path")"
    expected=401
  fi
  if [[ "$status" != "$expected" ]]; then
    echo "电子文档契约 HTTP 校验失败: $path 返回 $status，期望 $expected" >&2
    exit 1
  fi
  echo "电子文档契约 HTTP 校验通过: $path ($status)"
done
if [[ -n "$DOCUMENT_PROVIDER_TOKEN" ]]; then
  echo "电子文档契约门禁通过: 真实角色令牌下未接入供应商明确返回 $DOCUMENT_PROVIDER_EXPECTED_STATUS。"
else
  echo "电子文档契约认证边界通过: 未登录请求均返回 401。"
fi
