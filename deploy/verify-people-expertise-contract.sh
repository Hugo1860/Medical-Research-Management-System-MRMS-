#!/usr/bin/env bash
set -euo pipefail

# Static-only contract for people capability profiles, mentor/team matching and
# expert availability. It does not invent people or call the runtime database.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
controller="$root/ScientificResearch/ScientificResearch/Controllers/PeopleController.cs"
migration="$root/database/postgresql/migrations/079_people_team_expertise.sql"
migration_dir="$root/database/postgresql/migrations"
people_view="$root/frontend/src/views/organization/people.vue"
appointments_view="$root/frontend/src/views/organization/academic-appointments.vue"
api="$root/frontend/src/api/organization.ts"
release="$root/deploy/verify-local-release.sh"
failures=0
fail() { echo "人员与专家画像契约失败: $*" >&2; failures=$((failures + 1)); }

for path in "$controller" "$migration" "$people_view" "$appointments_view" "$api" "$release"; do
  [[ -f "$path" ]] || fail "缺少 $path"
done

for marker in \
  '获取人员科研能力画像' \
  '保存人员科研能力画像' \
  '保存人员导师关系' \
  '匹配科研团队成员' \
  '保存科研团队' \
  '保存科研团队成员' \
  '查询专家可用时间' \
  '保存专家可用时间' \
  '查询外部专家邀请' \
  '创建外部专家邀请' \
  '回写外部专家邀请回执' \
  '人员不存在或已停用' \
  '人员均有效' \
  'CanAccessProjectAsync' \
  '可公开展示' \
  '当前项目数' \
  '剩余项目容量' \
  '未配置' \
  '真实适配器回执'; do
  grep -Fq -- "$marker" "$controller" || fail "服务端缺少边界: $marker"
done

for marker in \
  '人员科研能力' \
  '科研团队' \
  '科研团队成员' \
  '人员项目分工' \
  'v1_人员团队项目分工'; do
  grep -Fq -- "$marker" "$migration" || fail "079 迁移缺少结构: $marker"
done

for marker in '人员导师关系' '专家可用时间' '外部专家邀请'; do
  grep -R -Fq -- "$marker" "$migration_dir" || fail "迁移序列缺少人员/专家结构: $marker"
done

for marker in \
  '获取人员科研能力画像' \
  '保存人员科研能力画像' \
  '匹配科研团队成员' \
  '查询专家可用时间' \
  '创建外部专家邀请' \
  '暂无外部专家邀请记录'; do
  grep -Fq -- "$marker" "$people_view" "$appointments_view" "$api" || fail "前端未绑定真实画像/专家接口: $marker"
done

for forbidden in \
  'CurrentUser.编号 > 0 ? CurrentUser.编号 : 2' \
  '默认管理员' \
  '示范数据' \
  '科研专家1' \
  '专家张三'; do
  if grep -Fq -- "$forbidden" "$controller" "$people_view" "$appointments_view"; then
    fail "发现人员/专家示例回退: $forbidden"
  fi
done

grep -Fq './deploy/verify-people-expertise-contract.sh' "$release" || fail '统一发布门禁未纳入人员与专家画像契约'

if (( failures > 0 )); then
  echo "人员与专家画像契约未通过: ${failures} 项" >&2
  exit 1
fi
echo '人员与专家画像契约静态校验通过: 能力画像、导师/团队匹配、项目访问范围、专家可用时间和外部邀请回执边界已登记（未连接运行库）'
