#!/usr/bin/env bash
set -euo pipefail

# Static-only contract for the desktop and mobile workbench. It verifies that
# role-aware next steps, missing-material counts, and responsibility fields are
# sourced from the authenticated APIs. It does not call the application or
# manufacture a six-role acceptance result.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
api="$root/frontend/src/api/workbench.ts"
dashboard="$root/frontend/src/views/dashboard/index.vue"
mobile="$root/frontend/src/views/office/mobile-workbench.vue"
auth="$root/deploy/verify-auth-boundaries.sh"
release="$root/deploy/verify-local-release.sh"
evaluation_models="$root/ScientificResearch/ScientificResearch/Models/Custom/Evaluation.cs"
failures=0
fail() { echo "工作台契约失败: $*" >&2; failures=$((failures + 1)); }

for path in "$api" "$dashboard" "$mobile" "$auth" "$release" "$evaluation_models"; do
  [[ -f "$path" ]] || fail "缺少 $path"
done

python3 - "$evaluation_models" <<'PY'
import pathlib, re, sys

text = pathlib.Path(sys.argv[1]).read_text(encoding='utf-8-sig')
blocks = {}
for name in ('科研绩效动态公式保存', '移动协同消息配置保存'):
    pattern = r'class\s+' + re.escape(name) + r'\b.*?(?=\n\s*public class|\Z)'
    match = re.search(pattern, text, re.S)
    if not match:
        raise SystemExit(f'工作台契约失败: 缺少模型 {name}')
    blocks[name] = match.group(0)
    if not re.search(r'public bool 是否启用\s*\{ get; set; \}\s*=\s*false\s*;', match.group(0)):
        raise SystemExit(f'工作台契约失败: {name} 的是否启用默认值必须为 false')
PY

for marker in \
  "get<WorkbenchSummary>('/Office/获取科研工作台摘要')" \
  '角色名称?: string' \
  '缺件数?: number' \
  '缺件提示?: string' \
  '下一步办理人?: string' \
  '下一步办理角色?: string' \
  '下一步流程名称?: string'; do
  grep -Fq -- "$marker" "$api" || fail "API 未声明真实工作台字段: $marker"
done

for marker in \
  'getWorkbenchSummary()' \
  'row.步骤操作人姓名' \
  'row.步骤操作人部门名称' \
  '服务端未返回责任人' \
  '暂无真实统计数据'; do
  grep -Fq -- "$marker" "$dashboard" || fail "桌面工作台未绑定服务端责任信息: $marker"
done

for marker in \
  'getWorkbenchSummary()' \
  'workbenchData?.缺件数' \
  'workbenchData.缺件提示' \
  'workbenchData.下一步办理人' \
  'workbenchData.下一步办理角色' \
  'workbenchData.下一步流程名称' \
  'item.流程编号' \
  'item.步骤操作人姓名' \
  '当前账号可办理' \
  '待办缺少真实流程编号或名称'; do
  grep -Fq -- "$marker" "$mobile" || fail "移动工作台未绑定真实待办/责任信息: $marker"
done

# These strings previously made a failed/partial response look like a real
# workflow item. A neutral empty state is acceptable; fabricated ownership or
# task identity is not.
for forbidden in \
  '发起人：${item.发起人姓名 || '\''项目组'\''}' \
  "item.发起人姓名 || '项目组'" \
  "'科研事项审核'" \
  "'待我审核'" \
  '业务编号: item.编号 ?'; do
  if grep -Fq -- "$forbidden" "$dashboard" "$mobile"; then
    fail "发现工作台示例/伪造责任回退: $forbidden"
  fi
done

python3 - "$auth" <<'PY'
import re, sys

text = open(sys.argv[1], encoding='utf-8').read()
expected = {'科研人员', '科室负责人', '科研秘书', '专家', '财务管理员', '系统管理员'}
roles = set(re.findall(r"\('([^']+)', 'AUTH_BOUNDARIES_[A-Z_]+_TOKEN'", text))
if roles != expected:
    raise SystemExit('工作台契约失败: 六角色 UAT 矩阵不完整: ' + ', '.join(sorted(roles)))
role_paths = {
    '科研人员': '/Office/获取科研工作台摘要',
    '科室负责人': '/Office/获取科研工作台摘要',
    '科研秘书': '/Office/获取科研工作台摘要',
    '专家': '/Office/获取专家评审待办',
    '财务管理员': '/Expense/获取预算异常对账',
    '系统管理员': '/Operations/获取外部系统',
}
for role, path in role_paths.items():
    if path not in text:
        raise SystemExit(f'工作台契约失败: {role} 未登记真实角色数据接口 {path}')
PY

grep -Fq './deploy/verify-workbench-contract.sh' "$release" || \
  fail 'verify-local-release.sh 未纳入工作台契约'

if (( failures > 0 )); then
  echo "工作台契约未通过: ${failures} 项" >&2
  exit 1
fi

echo '工作台契约静态校验通过: 六角色接口矩阵、下一步/缺件/责任人字段和移动端真实待办绑定已登记；未发现示例责任回退。'
