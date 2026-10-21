#!/usr/bin/env bash
set -euo pipefail

# Static-only P2 gate for external integration.  It does not connect to
# PostgreSQL, start Docker, read secrets, or call any supplier endpoint.  The
# real eight-system acceptance still requires an operator, a test account, a
# supplier 2xx receipt and an idempotent replay result.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
worker="$root/ScientificResearch/ScientificResearch/Infrastucture/ExternalSyncWorker.cs"
operations="$root/ScientificResearch/ScientificResearch/Controllers/OperationsController.cs"
people="$root/ScientificResearch/ScientificResearch/Controllers/PeopleController.cs"
revoke_migration="$root/database/postgresql/migrations/030_permission_lifecycle.sql"
contract="$root/docs/external-integration-contract.md"
template="$root/docs/external-adapter-config-template.md"
readme="$root/deploy/README.md"
failures=0

fail() { echo "外部集成 P2 契约失败: $*" >&2; failures=$((failures + 1)); }

[[ -f "$worker" ]] || fail 'ExternalSyncWorker.cs 不存在'
[[ -f "$operations" ]] || fail 'OperationsController.cs 不存在'
[[ -f "$people" ]] || fail 'PeopleController.cs 不存在'
[[ -f "$revoke_migration" ]] || fail '权限回收迁移不存在'
[[ -f "$contract" ]] || fail 'external-integration-contract.md 不存在'
[[ -f "$template" ]] || fail 'external-adapter-config-template.md 不存在'

for marker in \
  'RequiredSystemTypes = { "HR", "财务", "采购", "伦理", "合同", "人才库", "生物样本库", "论文采集" }' \
  'Idempotency-Key' \
  'X-Request-ID' \
  'MarkSuccessAsync' \
  'MarkFailureAsync' \
  'statusCode >= 500 || statusCode is 408 or 429' \
  'RetryAfter' \
  'RequeueStaleAsync' \
  'SanitizeResponseSummary' \
  '[REDACTED]'; do
  grep -Fq -- "$marker" "$worker" || fail "worker 缺少 P2 边界: $marker"
done

for marker in \
  '预览HR人员同步' \
  '同步HR人员' \
  '人员外部映射' \
  '是否全量' \
  '是否在职'; do
  grep -Fq -- "$marker" "$people" || fail "HR 入站同步缺少 P2 边界: $marker"
done
for marker in \
  'trg_人员_禁用回收代理' \
  '人员已禁用，自动回收代理权限'; do
  grep -Fq -- "$marker" "$revoke_migration" || fail "HR 离岗权限回收缺少数据库门禁: $marker"
done

for marker in \
  '获取外部系统配置检查' \
  '测试外部系统连接' \
  '创建外部同步任务' \
  '重试外部同步任务' \
  '取消外部同步任务' \
  '获取外部同步失败告警' \
  'response.IsSuccessStatusCode' \
  '认证密钥已注入'; do
  grep -Fq -- "$marker" "$operations" || fail "运行治理接口缺少 P2 边界: $marker"
done

for marker in \
  'HR、财务、采购、伦理、合同、人才库、生物样本库和论文采集' \
  '真实回执' \
  '幂等重放' \
  'X-Request-ID' \
  'Idempotency-Key' \
  'Retry-After' \
  '字段白名单' \
  '回调签名' \
  '敏感响应' \
  'ExternalSync__Enabled=false'; do
  grep -Fq -- "$marker" "$contract" || fail "集成契约文档缺少 P2 验收边界: $marker"
done

for marker in \
  '### HR' \
  '### 财务' \
  '### 伦理' \
  '### 采购' \
  '### 合同' \
  '### 人才库' \
  '### 生物样本库' \
  '### 论文采集' \
  'P2 回执' \
  '入站回调' \
  '真实管理员账号' \
  '供应商 2xx'; do
  grep -Fq -- "$marker" "$template" || fail "适配器模板缺少 P2 内容: $marker"
done

if (( failures > 0 )); then
  echo "外部集成 P2 契约未通过: ${failures} 项" >&2
  exit 1
fi
echo '外部集成 P2 契约通过：八类适配器、幂等与追踪、回执/重试、租约回收、响应脱敏和真实联调边界均已静态固化。'
