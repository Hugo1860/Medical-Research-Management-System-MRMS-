#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "== 校验 101-105 迁移与新增模块静态契约 =="

for mig in \
  "$root/database/postgresql/migrations/101_project_classification_budget_templates.sql" \
  "$root/database/postgresql/migrations/102_fund_execution_extensions.sql" \
  "$root/database/postgresql/migrations/103_platform_instrument_booking.sql" \
  "$root/database/postgresql/migrations/104_academic_exchange_and_journal_partition.sql" \
  "$root/database/postgresql/migrations/105_vertical_project_pdf_import.sql"; do
  [[ -f "$mig" ]] || { echo "缺失迁移: $mig" >&2; exit 1; }
done

for ctrl in \
  "$root/ScientificResearch/ScientificResearch/Controllers/ProjectConfigurationController.cs" \
  "$root/ScientificResearch/ScientificResearch/Controllers/FundExecutionController.cs" \
  "$root/ScientificResearch/ScientificResearch/Controllers/PlatformInstrumentController.cs" \
  "$root/ScientificResearch/ScientificResearch/Controllers/AcademicExchangeController.cs" \
  "$root/ScientificResearch/ScientificResearch/Controllers/JournalPartitionController.cs" \
  "$root/ScientificResearch/ScientificResearch/Controllers/VerticalProjectImportController.cs"; do
  [[ -f "$ctrl" ]] || { echo "缺失控制器: $ctrl" >&2; exit 1; }
done

for api in \
  "$root/frontend/src/api/project-configuration.ts" \
  "$root/frontend/src/api/fund-execution.ts" \
  "$root/frontend/src/api/platform-instrument.ts" \
  "$root/frontend/src/api/academic-exchange.ts" \
  "$root/frontend/src/api/journal-partition.ts" \
  "$root/frontend/src/api/vertical-import.ts"; do
  [[ -f "$api" ]] || { echo "缺失前端 API: $api" >&2; exit 1; }
done

for view in \
  "$root/frontend/src/views/project/project-configuration.vue" \
  "$root/frontend/src/views/finance/fund-execution.vue" \
  "$root/frontend/src/views/activities/academic-exchange.vue" \
  "$root/frontend/src/views/fruit/journal-partition.vue" \
  "$root/frontend/src/views/project/vertical-import.vue"; do
  [[ -f "$view" ]] || { echo "缺失前端页面: $view" >&2; exit 1; }
done

for html in \
  "$root/ScientificResearch/ScientificResearch/wwwroot/IMIS/views/科研办公/科研办公/项目分类与预算模板.html" \
  "$root/ScientificResearch/ScientificResearch/wwwroot/IMIS/views/科研办公/科研办公/经费执行治理.html" \
  "$root/ScientificResearch/ScientificResearch/wwwroot/IMIS/views/科研办公/科研办公/仪器设备与开放预约.html" \
  "$root/ScientificResearch/ScientificResearch/wwwroot/IMIS/views/科研办公/科研办公/学术交流治理.html" \
  "$root/ScientificResearch/ScientificResearch/wwwroot/IMIS/views/科研办公/科研办公/期刊分区库.html" \
  "$root/ScientificResearch/ScientificResearch/wwwroot/IMIS/views/科研办公/科研办公/纵向项目导入.html"; do
  [[ -f "$html" ]] || { echo "缺失旧版重定向 HTML: $html" >&2; exit 1; }
done

echo "101-105 迁移、控制器、前端组件与重定向契约全部就绪并通过！"
