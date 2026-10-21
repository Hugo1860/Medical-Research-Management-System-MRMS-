#!/usr/bin/env bash
set -euo pipefail

# Static-only gate for source evidence behind retraction/correction states.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
controller="$root/ScientificResearch/ScientificResearch/Controllers/FruitController.cs"
model="$root/ScientificResearch/ScientificResearch/Models/Custom/Fruit.cs"
migration="$root/database/postgresql/migrations/086_achievement_status_evidence.sql"
for path in "$controller" "$model" "$migration"; do [[ -f "$path" ]] || { echo "成果状态凭证契约失败: 缺少 $path" >&2; exit 1; }; done
for marker in '科研成果状态凭证' '科研成果状态凭证核验' '撤稿、更正和恢复有效必须登记来源类型' '核验科研成果状态凭证' '凭证核验状态'; do
  grep -Fq "$marker" "$controller" || { echo "成果状态凭证契约失败: 控制器缺少 $marker" >&2; exit 1; }
done
for marker in '来源类型' '来源凭证编号' '来源文件SHA256' '科研成果状态凭证核验'; do
  grep -Fq "$marker" "$model" || { echo "成果状态凭证契约失败: 模型缺少 $marker" >&2; exit 1; }
done
for marker in '不可变审计' 'tr_科研成果状态凭证_只读' 'ck_科研成果状态凭证_SHA256' '状态凭证编号'; do
  grep -Fq "$marker" "$migration" || { echo "成果状态凭证契约失败: 086 缺少 $marker" >&2; exit 1; }
done
echo '成果状态凭证静态契约通过: 撤稿/更正来源、SHA-256、待核验状态和不可变审计已登记（未连接运行库）'
