#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
controller="$root/ScientificResearch/ScientificResearch/Controllers/PeopleController.cs"
models="$root/ScientificResearch/ScientificResearch/Models/Custom/People.cs"
settings="$root/ScientificResearch/ScientificResearch/appsettings.json"
compose="$root/docker-compose.yml"
env_example="$root/.env.docker.example"
for marker in '查询专家可用时间' '保存专家可用时间' '查询外部专家邀请' '创建外部专家邀请' '回写外部专家邀请回执' '未配置' '真实回执' '外部发送、失败或接受/拒绝状态必须来自真实适配器回执' '尚未纳入评审专家角色' '流程模板编号.HasValue != model.项目编号.HasValue'; do
  grep -Fq "$marker" "$controller" || { echo "专家邀请契约失败: Controller 缺少 $marker" >&2; exit 1; }
done
for marker in '专家可用时间保存' '外部专家邀请创建' '外部专家邀请回执'; do
  grep -Fq "$marker" "$models" || { echo "专家邀请契约失败: Model 缺少 $marker" >&2; exit 1; }
done
for marker in \
  'ExternalExpertInvitation__Enabled: ${EXTERNAL_EXPERT_INVITATION_ENABLED:-false}' \
  'ExternalExpertInvitation__AdapterReady: ${EXTERNAL_EXPERT_INVITATION_ADAPTER_READY:-false}' \
  'ExternalExpertInvitation__Provider: ${EXTERNAL_EXPERT_INVITATION_PROVIDER:-}' \
  'ExternalExpertInvitation__Endpoint: ${EXTERNAL_EXPERT_INVITATION_ENDPOINT:-}' \
  'ExternalExpertInvitation__RequireHttps: ${EXTERNAL_EXPERT_INVITATION_REQUIRE_HTTPS:-true}'; do
  grep -Fq "$marker" "$compose" || { echo "专家邀请契约失败: Compose 缺少 $marker" >&2; exit 1; }
done
grep -Fq 'POSTGRES_EXPECTED_SCHEMA_VERSION=105' "$env_example" || {
  echo '专家邀请契约失败: .env.docker.example 未对齐 Schema 105' >&2
  exit 1
}
python3 - "$settings" <<'PY'
import json, sys
cfg=json.load(open(sys.argv[1], encoding='utf-8'))
section=cfg.get('ExternalExpertInvitation')
if not isinstance(section, dict) or section.get('Enabled') is not False or section.get('AdapterReady') is not False:
    raise SystemExit('专家邀请契约失败: ExternalExpertInvitation 必须默认关闭且未验收')
print('专家邀请契约静态校验通过: 可用时间、幂等邀请和真实回执门禁存在，默认不发送')
PY
