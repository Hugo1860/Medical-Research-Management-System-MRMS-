#!/usr/bin/env bash
set -euo pipefail

# Static-only contract for the bounded read-only load smoke. It verifies that
# the script remains explicit about scope and thresholds; it never generates
# traffic itself.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
load="$root/deploy/verify-performance-load.sh"
baseline="$root/deploy/verify-performance-baseline.sh"
readme="$root/deploy/README.md"
runbook="$root/docs/production-uat-runbook.md"
release="$root/deploy/verify-local-release.sh"
failures=0
fail() { echo "性能压力契约失败: $*" >&2; failures=$((failures + 1)); }

[[ -x "$load" ]] || fail 'verify-performance-load.sh 必须可执行'
for marker in \
  'LOAD_REQUESTS' \
  'LOAD_CONCURRENCY' \
  'LOAD_MAX_P95_MS' \
  'LOAD_MAX_ERROR_RATE' \
  'health/live' \
  'health/ready' \
  'Accept' \
  'ThreadPoolExecutor' \
  '不代表正式容量/稳定性测试'; do
  grep -Fq -- "$marker" "$load" || fail "压力脚本缺少边界: $marker"
done

for forbidden in \
  '/Access/' \
  'POST /' \
  'Authorization:' \
  'ExternalSync' \
  'DELETE /'; do
  if grep -Fq -- "$forbidden" "$load"; then fail "压力脚本包含非只读或需认证调用: $forbidden"; fi
done

grep -Fq '性能基线' "$baseline" || fail '基线脚本缺少基线说明'
grep -Fq '不代表正式压力测试通过' "$baseline" || fail '基线脚本未声明范围边界'
grep -Fq 'verify-performance-load.sh' "$readme" || fail '部署文档未登记压力烟测命令'
grep -Fq 'verify-performance-load-contract.sh' "$release" || fail '统一发布门禁未纳入压力契约'
grep -Fq 'LOAD_REQUESTS' "$runbook" || fail '运行手册未登记可配置压力烟测参数'

if (( failures > 0 )); then
  echo "性能压力契约未通过: ${failures} 项" >&2
  exit 1
fi
echo '性能压力契约静态校验通过：并发、p95、错误率和只读路径边界已固化（未发起请求）'
