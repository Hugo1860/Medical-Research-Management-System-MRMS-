#!/usr/bin/env bash
set -euo pipefail

# Static guardrail for the startup path. It catches accidental removal of the
# local Keycloak overlay or the runtime OIDC check before a container is run.
repo_dir="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
start_script="$repo_dir/deploy/start-local.sh"
runtime_script="$repo_dir/deploy/check-runtime.sh"
overlay="$repo_dir/deploy/docker-compose.keycloak.local.yml"
failures=0
fail() { echo "启动边界契约失败: $*" >&2; failures=$((failures + 1)); }

[[ -x "$start_script" ]] || fail 'deploy/start-local.sh 不存在或不可执行'
[[ -x "$runtime_script" ]] || fail 'deploy/check-runtime.sh 不存在或不可执行'
[[ -f "$overlay" ]] || fail 'Keycloak 本地覆盖层不存在'

if [[ -f "$start_script" ]]; then
  grep -Fq 'deploy/docker-compose.keycloak.local.yml' "$start_script" || fail '本地启动脚本未包含 Keycloak 覆盖层'
  grep -Fq 'REQUIRE_KEYCLOAK=true' "$start_script" || fail '本地启动脚本未启用统一认证门禁'
  grep -Fq 'check-runtime.sh' "$start_script" || fail '本地启动脚本未执行运行时预检'
fi
if [[ -f "$runtime_script" ]]; then
  grep -Fq '/Access/OidcConfig' "$runtime_script" || fail '运行时预检未核对 OIDC 公共配置'
  grep -Fq 'OIDC 公共配置 enabled=false' "$runtime_script" || fail '运行时预检未阻断 OIDC 静默关闭'
fi
if [[ -f "$overlay" ]]; then
  grep -Fq 'Keycloak__Enabled: "true"' "$overlay" || fail '覆盖层未强制开启 Keycloak'
  grep -Fq 'Keycloak__Authority:' "$overlay" || fail '覆盖层缺少 Keycloak Authority'
  grep -Fq 'Keycloak__RedirectUri:' "$overlay" || fail '覆盖层缺少 Keycloak 回调地址'
fi

if (( failures > 0 )); then
  echo "启动边界契约未通过: ${failures} 项" >&2
  exit 1
fi
echo '启动边界契约通过: 本地启动必须使用 Keycloak 覆盖层并核对 OIDC 公共配置'
