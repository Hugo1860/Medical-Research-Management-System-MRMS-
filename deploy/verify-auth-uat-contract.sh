#!/usr/bin/env bash
set -euo pipefail

# Static contract gate for the real six-role/MFA/revocation UAT runner. It does
# not mint tokens, call Keycloak, or claim that a live UAT has passed.
script="deploy/verify-auth-boundaries.sh"
keycloak_script="deploy/verify-keycloak.sh"
keycloak_options="ScientificResearch/ScientificResearch/Infrastucture/KeycloakIntegration.cs"
startup="ScientificResearch/ScientificResearch/Startup.cs"
[[ -f "$script" && -f "$keycloak_script" && -f "$keycloak_options" && -f "$startup" ]] || {
  echo '认证 UAT 契约失败: 缺少认证验收脚本' >&2
  exit 1
}

python3 - "$script" "$keycloak_script" "$keycloak_options" "$startup" <<'PY'
import re, sys

auth = open(sys.argv[1], encoding='utf-8').read()
keycloak = open(sys.argv[2], encoding='utf-8').read()
options = open(sys.argv[3], encoding='utf-8').read()
startup = open(sys.argv[4], encoding='utf-8').read()
required_auth = (
    'AUTH_BOUNDARIES_SIX_ROLE_TESTS',
    'AUTH_BOUNDARIES_REQUIRE_TOKEN_CLAIMS',
    'AUTH_BOUNDARIES_REQUIRE_MFA',
    'AUTH_BOUNDARIES_EXPECT_ISSUER',
    'AUTH_BOUNDARIES_MFA_AMR',
    "('/Access/Me', readable)",
    '六角色真实 UAT 矩阵校验通过',
)
missing = [marker for marker in required_auth if marker not in auth]
if missing:
    raise SystemExit('认证 UAT 契约失败: verify-auth-boundaries.sh 缺少 ' + ', '.join(missing))
roles = re.findall(r"\('([^']+)', 'AUTH_BOUNDARIES_[A-Z_]+_TOKEN'", auth)
expected = {'科研人员', '科室负责人', '科研秘书', '专家', '财务管理员', '系统管理员'}
if set(roles) != expected:
    raise SystemExit('认证 UAT 契约失败: 六角色矩阵不完整或包含未登记角色')
required_keycloak = (
    'KEYCLOAK_REVOKED_ENDPOINTS',
    'KEYCLOAK_REVOKED_PROXY_ACCESS_TOKEN',
    'KEYCLOAK_REVOKED_PROXY_GRANTOR_ID',
    'KEYCLOAK_REVOKED_PROXY_PAYLOAD',
    'Revoked/disabled token rejected OK',
    'Revoked proxy grant rejected OK',
)
missing = [marker for marker in required_keycloak if marker not in keycloak]
if missing:
    raise SystemExit('认证 UAT 契约失败: verify-keycloak.sh 缺少 ' + ', '.join(missing))
print('认证 UAT 静态契约通过: 六角色、本地人员映射、claims/MFA 开关、旧令牌与代理撤销探针已登记')
required_runtime_mfa = ('RequireMfa', 'MfaAmr')
missing_runtime = [marker for marker in required_runtime_mfa if marker not in options]
if 'keycloak.RequireMfa' not in startup or 'Keycloak token MFA requirement not satisfied' not in startup:
    missing_runtime.append('Startup runtime MFA rejection')
if missing_runtime:
    raise SystemExit('认证 UAT 契约失败: 应用未固化运行时 MFA 门禁: ' + ', '.join(missing_runtime))
print('运行时 MFA 门禁契约通过: RequireMfa/MfaAmr 配置与 amr 拒绝路径已固化')
PY

bash -n "$script" "$keycloak_script"
