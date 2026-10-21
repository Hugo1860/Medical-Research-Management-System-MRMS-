<template>
  <div class="page-container">
    <el-card shadow="never" class="main-card">
      <!-- 页面头部 -->
      <div class="header-section">
        <div>
          <h2 class="page-title">科研绩效评价与奖励管理</h2>
          <p class="page-subtitle">
            全生命周期管理科研人员年度绩效考核指标体系、多维证据链计分测算、结果可解释性复核、学术申诉与奖励兑现。
          </p>
        </div>
        <div class="action-buttons">
          <el-button type="primary" :icon="Plus" @click="openGenerateDialog">
            生成绩效测算
          </el-button>
          <el-button :icon="Download" @click="handleExportRankings">
            导出排名 (CSV)
          </el-button>
          <el-button :icon="Refresh" @click="refreshActiveTab">
            刷新
          </el-button>
        </div>
      </div>

      <!-- 核心指标统计卡片 -->
      <el-row :gutter="16" class="stat-cards">
        <el-col :xs="12" :sm="6">
          <div class="stat-card stat-blue">
            <span class="label">当前年度参评人数</span>
            <span class="val">{{ rankingList.length }} 人</span>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="stat-card stat-green">
            <span class="label">全院参评人均分</span>
            <span class="val">{{ avgScore }} 分</span>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="stat-card stat-orange">
            <span class="label">最高绩效标杆分</span>
            <span class="val">{{ topScore }} 分</span>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="stat-card stat-purple">
            <span class="label">在行评价规则套数</span>
            <span class="val">{{ ruleList.length }} 套</span>
          </div>
        </el-col>
      </el-row>

      <!-- 业务分类标签页 -->
      <el-tabs v-model="activeTab" class="evaluation-tabs" @tab-change="onTabChange">
        <!-- 标签页 1：科研综合绩效总榜与排名 -->
        <el-tab-pane label="科研综合绩效总榜" name="ranking">
          <div class="search-bar">
            <el-select v-model="selectedYear" placeholder="考核年度" style="width: 140px;" @change="fetchRankings">
              <el-option label="2026 年度" :value="2026" />
              <el-option label="2025 年度" :value="2025" />
              <el-option label="2024 年度" :value="2024" />
            </el-select>
            <el-select
              v-model="selectedRuleId"
              placeholder="评价规则体系"
              clearable
              style="width: 240px;"
              @change="fetchRankings"
            >
              <el-option
                v-for="rule in ruleList"
                :key="rule.编号"
                :label="`${rule.规则名称} (v${rule.版本号})`"
                :value="rule.编号"
              />
            </el-select>
            <el-button type="primary" :icon="Search" @click="fetchRankings">查询排名</el-button>
          </div>

          <el-table
            v-loading="loadingRankings"
            :data="rankingList"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="排名" label="全院排名" width="100" align="center">
              <template #default="{ row }">
                <el-tag
                  v-if="row.排名 === 1"
                  effect="dark"
                  type="warning"
                  style="font-weight: 700;"
                >
                  🥇 榜首
                </el-tag>
                <el-tag
                  v-else-if="row.排名 === 2"
                  effect="light"
                  type="info"
                  style="font-weight: 700;"
                >
                  🥈 榜眼
                </el-tag>
                <el-tag
                  v-else-if="row.排名 === 3"
                  effect="light"
                  type="danger"
                  style="font-weight: 700;"
                >
                  🥉 探花
                </el-tag>
                <span v-else style="font-weight: 600; color: #64748b;">No.{{ row.排名 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="姓名" label="科研人员" width="140">
              <template #default="{ row }">
                <span style="font-weight: 600; color: #1e293b;">{{ row.姓名 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="岗位名称" label="岗位 / 科室" width="160" show-overflow-tooltip />
            <el-table-column prop="学科门类" label="所属学科" width="140" show-overflow-tooltip />
            <el-table-column prop="评价年度" label="年度" width="90" align="center" />
            <el-table-column prop="规则名称" label="依据规则" min-width="200" show-overflow-tooltip>
              <template #default="{ row }">
                <span>{{ row.规则名称 }}</span>
                <el-tag size="small" type="info" style="margin-left: 6px;">v{{ row.版本号 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="总分" label="考核综合得分" width="140" align="right">
              <template #default="{ row }">
                <span style="font-size: 16px; font-weight: 700; color: #409eff;">{{ row.总分 }} 分</span>
              </template>
            </el-table-column>
            <el-table-column prop="状态" label="状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="row.状态 === '已发布' ? 'success' : 'warning'">
                  {{ row.状态 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="160" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openPersonDetail(row.人员编号)">
                  调阅明细
                </el-button>
                <el-button link type="warning" size="small" @click="openRewardDialog(row)">
                  登记奖励
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签页 2：本人绩效与申诉中心 -->
        <el-tab-pane label="本人绩效与申诉" name="my_evaluation">
          <div class="search-bar">
            <span style="font-size: 14px; color: #64748b;">
              查看本人历年科研绩效考核评价得分、规则明细证据链、复核意见及发起学术申诉。
            </span>
          </div>

          <el-table
            v-loading="loadingMyResults"
            :data="myResultList"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="评价年度" label="考核年度" width="100" align="center" />
            <el-table-column prop="规则名称" label="评价指标体系" min-width="200">
              <template #default="{ row }">
                <span style="font-weight: 600; color: #1e293b;">{{ row.规则名称 }}</span>
                <el-tag size="small" type="info" style="margin-left: 6px;">v{{ row.版本号 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="考核周期" width="220" align="center">
              <template #default="{ row }">
                <span>{{ row.周期开始日期 }} 至 {{ row.周期结束日期 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="总分" label="考核总分" width="130" align="right">
              <template #default="{ row }">
                <span style="font-size: 16px; font-weight: 700; color: #67c23a;">{{ row.总分 }} 分</span>
              </template>
            </el-table-column>
            <el-table-column prop="状态" label="状态" width="110" align="center">
              <template #default="{ row }">
                <el-tag :type="getStatusTagType(row.状态)">{{ row.状态 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="复核意见" label="复核与管理评审意见" min-width="200" show-overflow-tooltip />
            <el-table-column label="操作" width="180" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="viewEvaluationExplainDrawer(row.编号)">
                  得分解释
                </el-button>
                <el-button
                  v-if="['已发布', '已修订'].includes(row.状态)"
                  link
                  type="danger"
                  size="small"
                  @click="openAppealDialog(row.编号)"
                >
                  提起申诉
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签页 3：评价指标体系与规则库 -->
        <el-tab-pane label="评价指标与规则库" name="rules">
          <div class="search-bar">
            <el-input
              v-model="ruleQuery.规则名称"
              placeholder="搜索规则名称..."
              clearable
              style="width: 240px;"
              @keyup.enter="fetchRules"
            />
            <el-input
              v-model="ruleQuery.适用岗位"
              placeholder="适用岗位..."
              clearable
              style="width: 160px;"
              @keyup.enter="fetchRules"
            />
            <el-button type="primary" :icon="Search" @click="fetchRules">查询规则</el-button>
            <el-button type="success" :icon="Plus" @click="openRuleModal()">新建规则</el-button>
          </div>

          <el-table
            v-loading="loadingRules"
            :data="ruleList"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="编号" label="规则ID" width="80" align="center" />
            <el-table-column prop="规则名称" label="评价体系名称" min-width="200">
              <template #default="{ row }">
                <span style="font-weight: 600; color: #1e293b;">{{ row.规则名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="版本号" label="版本" width="80" align="center">
              <template #default="{ row }">
                <el-tag size="small">v{{ row.版本号 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="适用岗位" label="适用岗位" width="140" show-overflow-tooltip />
            <el-table-column prop="适用学科" label="适用学科" width="140" show-overflow-tooltip />
            <el-table-column prop="评价周期" label="周期" width="90" align="center" />
            <el-table-column prop="状态" label="状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="row.状态 === '生效' ? 'success' : (row.状态 === '草稿' ? 'info' : 'danger')">
                  {{ row.状态 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="规则说明" label="说明" min-width="200" show-overflow-tooltip />
            <el-table-column label="操作" width="200" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openRuleItemsDrawer(row)">
                  计分规则项
                </el-button>
                <el-button link type="warning" size="small" @click="openRuleModal(row)">
                  编辑
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签页 4：测算生成与管理复核 -->
        <el-tab-pane label="测算、复核与奖励兑现" name="management">
          <div class="search-bar">
            <span style="font-size: 14px; color: #64748b;">
              科研管理部门专区：批量测算人员绩效、执行正式发布复核与奖金登记。
            </span>
            <el-button type="primary" :icon="Plus" @click="openGenerateDialog">
              创建测算
            </el-button>
            <el-button type="warning" @click="openReviewDialog">
              复核/发布评价结果
            </el-button>
          </div>

          <el-alert title="年度发布控制" type="info" :closable="false" show-icon style="margin-top: 16px;">
            评价结果经复核后需进入公示、正式发布、锁定，再允许年度奖励结算；锁定后评价分数和申诉流程不可回退。
          </el-alert>
          <div class="search-bar" style="margin-top: 12px;">
            <el-select v-model="selectedBatchId" placeholder="选择年度批次" clearable style="width: 330px;" @change="onBatchChange">
              <el-option v-for="batch in annualBatches" :key="batch.编号" :value="batch.编号" :label="`${batch.评价年度} · ${batch.批次名称} · ${batch.状态}`" />
            </el-select>
            <el-button @click="fetchAnnualBatches">刷新批次</el-button>
            <el-button type="success" plain @click="createAnnualBatch">新建年度批次</el-button>
            <el-button type="warning" :disabled="!selectedBatch || selectedBatch.状态 !== '草稿'" @click="startAnnualPublication">开始公示</el-button>
            <el-button type="primary" :disabled="!selectedBatch || selectedBatch.状态 !== '公示中'" @click="publishAnnual">正式发布</el-button>
            <el-button type="danger" :disabled="!selectedBatch || selectedBatch.状态 !== '已发布'" @click="lockAnnual">锁定年度</el-button>
            <el-button type="success" :disabled="!selectedBatch || selectedBatch.状态 !== '已锁定'" @click="settleAnnual">完成结算</el-button>
          </div>
          <el-descriptions v-if="selectedBatch" :column="4" border size="small" style="margin-top: 8px;">
            <el-descriptions-item label="公示日期">{{ selectedBatch.公示开始日期 || '-' }} ~ {{ selectedBatch.公示结束日期 || '-' }}</el-descriptions-item>
            <el-descriptions-item label="评价结果">{{ selectedBatch.结果数 || 0 }}</el-descriptions-item>
            <el-descriptions-item label="奖励数">{{ selectedBatch.奖励数 || 0 }}</el-descriptions-item>
            <el-descriptions-item label="待支付">{{ selectedBatch.待支付数 || 0 }}</el-descriptions-item>
          </el-descriptions>

          <div style="margin-top: 20px;">
            <h4 style="margin-bottom: 12px; font-size: 15px;">全院最新参评人员速览</h4>
            <el-table :data="rankingList.slice(0, 10)" border size="small" style="width: 100%;">
              <el-table-column prop="人员编号" label="工号/ID" width="100" align="center" />
              <el-table-column prop="姓名" label="人员姓名" width="120" />
              <el-table-column prop="岗位名称" label="岗位" width="150" />
              <el-table-column prop="总分" label="当前得分" width="120" align="right">
                <template #default="{ row }">
                  <span style="font-weight: 700; color: #409eff;">{{ row.总分 }} 分</span>
                </template>
              </el-table-column>
              <el-table-column prop="状态" label="审核状态" width="110" align="center">
                <template #default="{ row }">
                  <el-tag size="small" :type="row.状态 === '已发布' ? 'success' : 'warning'">
                    {{ row.状态 }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="快捷操作" min-width="140" align="center">
                <template #default="{ row }">
                  <el-button link type="primary" size="small" @click="openPersonDetail(row.人员编号)">
                    查看证据链
                  </el-button>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </el-tab-pane>

        <!-- 标签页 5：动态绩效公式设计器与一键全员测算 -->
        <el-tab-pane label="动态公式与作者位次拆分" name="formula_designer">
          <div class="search-bar">
            <span style="font-size: 14px; color: #64748b;">
              清华长庚医院标杆：学术论文(CAS分区+Top+IF乘数)+纵向课题+横向合同+专利多维权重公式，支持一作/通讯/共一/参研位次动态拆分。
            </span>
            <div style="display: flex; gap: 8px;">
              <el-button type="success" :icon="Plus" @click="openFormulaModal()">
                新增公式项
              </el-button>
              <el-button type="primary" :icon="Refresh" :loading="autoEvalLoading" @click="handleExecuteAutoEval">
                一键全员科研绩效动态测算
              </el-button>
            </div>
          </div>

          <el-table
            v-loading="loadingFormulas"
            :data="formulaList"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="指标代码" label="指标代码" width="120" />
            <el-table-column prop="指标名称" label="指标名称" min-width="180">
              <template #default="{ row }">
                <span style="font-weight: 600; color: #1e293b;">{{ row.指标名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="指标类别" label="成果/业务类别" width="120">
              <template #default="{ row }">
                <el-tag size="small" type="primary">{{ row.指标类别 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="公式表达式" label="动态计算表达式" min-width="200" show-overflow-tooltip>
              <template #default="{ row }">
                <code style="background: #f1f5f9; padding: 2px 6px; border-radius: 4px; font-size: 12px;">{{ row.公式表达式 }}</code>
              </template>
            </el-table-column>
            <el-table-column label="多作者位次拆分比例" width="280" align="center">
              <template #default="{ row }">
                <div style="display: flex; justify-content: center; gap: 4px; font-size: 11px;">
                  <el-tag size="small" type="danger">一作: {{ (row.一作比例 * 100).toFixed(0) }}%</el-tag>
                  <el-tag size="small" type="warning">通讯: {{ (row.通讯比例 * 100).toFixed(0) }}%</el-tag>
                  <el-tag size="small" type="info">共一: {{ (row.共一比例 * 100).toFixed(0) }}%</el-tag>
                  <el-tag size="small">参研: {{ (row.参研比例 * 100).toFixed(0) }}%</el-tag>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="基础分值" label="基础分" width="90" align="right" />
            <el-table-column prop="是否启用" label="状态" width="80" align="center">
              <template #default="{ row }">
                <el-tag size="small" :type="row.是否启用 ? 'success' : 'info'">
                  {{ row.是否启用 ? '启用' : '禁用' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="100" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openFormulaModal(row)">
                  配置调整
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签页 6：自定义多维报表与卫健委国考年报 -->
        <el-tab-pane label="自定义多维报表与国考年报" name="custom_reports">
          <div class="search-bar">
            <el-select
              v-model="selectedReportTemplate"
              placeholder="选择报表模板"
              style="width: 320px;"
              @change="fetchCustomReportData"
            >
              <el-option
                v-for="tpl in reportTemplates"
                :key="tpl.模板代码"
                :label="tpl.模板名称"
                :value="tpl.模板代码"
              />
            </el-select>
            <el-select v-model="reportYear" placeholder="统计年度" style="width: 120px;" @change="fetchCustomReportData">
              <el-option label="2026 年度" :value="2026" />
              <el-option label="2025 年度" :value="2025" />
            </el-select>
            <el-button type="primary" :icon="Search" @click="fetchCustomReportData">查询报表</el-button>
            <el-button :icon="Download" @click="handleExportCustomReport">导出报表 (CSV)</el-button>
          </div>

          <div v-if="currentTemplateObj" style="margin-top: 10px; font-size: 13px; color: #64748b;">
            💡 模板说明：{{ currentTemplateObj.报表说明 }}
          </div>

          <el-table
            v-loading="loadingReport"
            :data="reportDataList"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="分组维度" label="科室 / 部门" min-width="140" />
            <el-table-column prop="统计年度" label="年度" width="80" align="center" />
            <el-table-column prop="学术论文总数" label="论文总数" width="100" align="right" />
            <el-table-column prop="一区Top论文数" label="1区Top篇数" width="110" align="right">
              <template #default="{ row }">
                <span style="font-weight: 700; color: #e11d48;">{{ row.一区Top论文数 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="最高影响因子" label="最高IF" width="90" align="right" />
            <el-table-column prop="纵向立项数" label="纵向立项" width="100" align="right" />
            <el-table-column prop="纵向立项经费" label="纵向经费(元)" width="130" align="right">
              <template #default="{ row }">
                <span>{{ Number(row.纵向立项经费 || 0).toLocaleString() }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="横向合同数" label="横向合同" width="100" align="right" />
            <el-table-column prop="横向合同金额" label="横向金额(元)" width="130" align="right">
              <template #default="{ row }">
                <span>{{ Number(row.横向合同金额 || 0).toLocaleString() }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="专利授权数" label="专利授权" width="90" align="right" />
            <el-table-column prop="科研绩效总分" label="绩效总分" width="110" align="right">
              <template #default="{ row }">
                <span style="font-weight: 700; color: #3b82f6;">{{ row.科研绩效总分 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="国考科研达标率" label="国考达标率" width="110" align="center">
              <template #default="{ row }">
                <el-tag size="small" :type="row.国考科研达标率 >= 90 ? 'success' : 'warning'">
                  {{ row.国考科研达标率 }}%
                </el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 绩效评价得分明细与证据链抽屉 -->
    <el-drawer
      v-model="detailDrawerVisible"
      title="科研绩效可解释性得分明细与证据链"
      size="640px"
      destroy-on-close
    >
      <div v-if="selectedDetail" class="drawer-box">
        <div class="result-summary-card">
          <div class="top-row">
            <h3>{{ selectedDetail.评价结果.姓名 }} - {{ selectedDetail.评价结果.评价年度 }}年度科研绩效</h3>
            <span class="score-badge">{{ selectedDetail.评价结果.总分 }} 分</span>
          </div>
          <p class="meta">
            规则：{{ selectedDetail.评价结果.规则名称 }} (v{{ selectedDetail.评价结果.版本号 }})
            &nbsp;|&nbsp; 状态：{{ selectedDetail.评价结果.状态 }}
          </p>
          <p v-if="selectedDetail.评价结果.复核意见" class="review-note">
            复核评审意见：{{ selectedDetail.评价结果.复核意见 }}
          </p>
        </div>

        <h4 style="margin: 20px 0 10px 0;">指标项采纳证据与算分明细</h4>
        <el-table :data="selectedDetail.明细" border size="small" style="width: 100%;">
          <el-table-column prop="项目名称" label="指标项" min-width="140" />
          <el-table-column prop="原始值" label="原始分/数量" width="100" align="right" />
          <el-table-column prop="权重" label="权重" width="70" align="center" />
          <el-table-column prop="得分" label="计入得分" width="90" align="right">
            <template #default="{ row }">
              <span style="font-weight: 700; color: #409eff;">{{ row.得分 }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="核验状态" label="状态" width="80" align="center">
            <template #default="{ row }">
              <el-tag size="small" :type="row.核验状态 === '已核验' ? 'success' : 'info'">
                {{ row.核验状态 }}
              </el-tag>
            </template>
          </el-table-column>
        </el-table>

        <h4 style="margin: 24px 0 10px 0;">学术申诉处理记录</h4>
        <div v-if="selectedDetail.申诉 && selectedDetail.申诉.length">
          <el-table :data="selectedDetail.申诉" border size="small" style="width: 100%;">
            <el-table-column prop="申诉理由" label="申诉原由" min-width="160" show-overflow-tooltip />
            <el-table-column prop="状态" label="受理状态" width="100" align="center" />
            <el-table-column prop="处理意见" label="委员会处理意见" min-width="160" show-overflow-tooltip />
          </el-table>
        </div>
        <div v-else style="font-size: 13px; color: #94a3b8;">暂无申诉记录</div>

        <h4 style="margin: 24px 0 10px 0;">关联科研奖励记录</h4>
        <div v-if="selectedDetail.奖励 && selectedDetail.奖励.length">
          <el-table :data="selectedDetail.奖励" border size="small" style="width: 100%;">
            <el-table-column prop="奖励名称" label="奖项名称" min-width="140" />
            <el-table-column prop="奖励级别" label="级别" width="90" />
            <el-table-column prop="奖励金额" label="金额(元)" width="100" align="right">
              <template #default="{ row }">
                <span style="color: #67c23a; font-weight: 600;">¥{{ row.奖励金额 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="状态" label="兑现状态" width="90" align="center" />
            <el-table-column prop="财务回写状态" label="财务状态" width="100" align="center" />
            <el-table-column prop="支付状态" label="支付状态" width="100" align="center" />
            <el-table-column prop="支付凭证号" label="支付凭证" width="130" show-overflow-tooltip />
            <el-table-column label="操作" width="100" align="center">
              <template #default="{ row }">
                <el-button v-if="row.状态 === '待发放'" link type="success" size="small" @click="openRewardSettlement(row)">确认发放</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
        <div v-else style="font-size: 13px; color: #94a3b8;">暂无奖励登记记录</div>
      </div>
    </el-drawer>

    <!-- 规则项配置抽屉 -->
    <el-drawer v-model="ruleItemsDrawerVisible" title="评价规则指标项详细配置" size="620px">
      <div v-if="currentRule">
        <div style="background: #f8fafc; padding: 12px; border-radius: 6px; margin-bottom: 16px;">
          <h4 style="margin: 0 0 4px 0;">{{ currentRule.规则名称 }} (v{{ currentRule.版本号 }})</h4>
          <span style="font-size: 12px; color: #64748b;">
            适用岗位：{{ currentRule.适用岗位 || '全院' }} &nbsp;|&nbsp; 周期：{{ currentRule.评价周期 }}
          </span>
        </div>

        <div style="display: flex; justify-content: flex-end; margin-bottom: 12px;">
          <el-button type="primary" size="small" :icon="Plus" @click="openRuleItemDialog">
            添加指标项
          </el-button>
        </div>

        <el-table :data="currentRuleItems" border size="small" style="width: 100%;">
          <el-table-column prop="项目代码" label="代码" width="90" />
          <el-table-column prop="项目名称" label="指标项名称" min-width="150" />
          <el-table-column prop="权重" label="权重" width="70" align="center" />
          <el-table-column prop="最高分" label="封顶分" width="80" align="right" />
          <el-table-column prop="证据类型" label="依据证据" width="100" />
        </el-table>
      </div>
    </el-drawer>

    <!-- 申诉提交弹窗 -->
    <el-dialog v-model="appealDialogVisible" title="提出科研绩效评价申诉" width="520px">
      <el-form :model="appealForm" label-width="100px">
        <el-form-item label="申诉理由" required>
          <el-input
            v-model="appealForm.申诉理由"
            type="textarea"
            rows="3"
            placeholder="详细陈述存在计算疏漏或证据缺失的事由"
          />
        </el-form-item>
        <el-form-item label="期望处理">
          <el-input v-model="appealForm.期望处理" placeholder="如：重新复核论文发表分区与第一单位署名" />
        </el-form-item>
        <el-form-item label="补充证据说明">
          <el-input
            v-model="appealForm.补充证据说明"
            type="textarea"
            rows="2"
            placeholder="补充佐证材料文件名、DOI或证书编号"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="appealDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="submitAppeal">
          提交申诉
        </el-button>
      </template>
    </el-dialog>

    <!-- 登记科研奖励弹窗 -->
    <el-dialog v-model="rewardDialogVisible" title="登记科研评价绩效奖励" width="500px">
      <el-form :model="rewardForm" label-width="90px">
        <el-form-item label="奖励名称" required>
          <el-input v-model="rewardForm.奖励名称" placeholder="如：2026年度青年科技创新突出贡献奖" />
        </el-form-item>
        <el-form-item label="奖励级别">
          <el-select v-model="rewardForm.奖励级别" style="width: 100%;">
            <el-option label="院级一等奖" value="院级一等奖" />
            <el-option label="院级二等奖" value="院级二等奖" />
            <el-option label="卓越贡献奖" value="卓越贡献奖" />
            <el-option label="优秀科研个人" value="优秀科研个人" />
          </el-select>
        </el-form-item>
        <el-form-item label="奖励金额(元)">
          <el-input-number v-model="rewardForm.奖励金额" :min="0" :step="1000" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="发放状态">
          <el-select v-model="rewardForm.状态" style="width: 100%;">
            <el-option label="待发放" value="待发放" />
            <el-option label="已发放" value="已发放" />
            <el-option label="不予发放" value="不予发放" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="rewardForm.状态 === '已发放'" label="发放凭证" required>
          <el-input v-model="rewardForm.发放凭证号" placeholder="工资/奖金发放凭证号" />
        </el-form-item>
        <el-form-item v-if="rewardForm.状态 === '已发放'" label="财务回写">
          <el-select v-model="rewardForm.财务回写状态" style="width: 100%;">
            <el-option label="待回写" value="待回写" />
            <el-option label="已回写" value="已回写" />
            <el-option label="回写失败" value="回写失败" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="rewardForm.状态 === '已发放' && rewardForm.财务回写状态 === '已回写'" label="财务凭证" required>
          <el-input v-model="rewardForm.财务凭证号" placeholder="财务系统回写凭证号" />
        </el-form-item>
        <el-form-item v-if="rewardForm.状态 === '已发放'" label="回写说明">
          <el-input v-model="rewardForm.财务回写说明" type="textarea" rows="2" placeholder="记录财务接口返回或人工核对结果" />
        </el-form-item>
        <el-form-item label="发放批次号"><el-input v-model="rewardForm.发放批次号" placeholder="工资批次或财务支付批次" /></el-form-item>
        <el-form-item label="支付状态"><el-select v-model="rewardForm.支付状态" style="width: 100%;"><el-option label="待支付" value="待支付" /><el-option label="已支付" value="已支付" /><el-option label="支付失败" value="支付失败" /><el-option label="不予支付" value="不予支付" /></el-select></el-form-item>
        <el-form-item v-if="rewardForm.支付状态 === '已支付' || rewardForm.支付状态 === '支付失败'" label="支付凭证" required><el-input v-model="rewardForm.支付凭证号" placeholder="银行流水、工资单或失败流水号" /></el-form-item>
        <el-form-item v-if="rewardForm.支付状态 === '支付失败'" label="失败原因" required><el-input v-model="rewardForm.支付失败原因" type="textarea" :rows="2" /></el-form-item>
        <el-form-item label="备注说明">
          <el-input v-model="rewardForm.备注" type="textarea" rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="rewardDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="submitReward">
          确认登记
        </el-button>
      </template>
    </el-dialog>

    <!-- 规则新建/编辑弹窗 -->
    <el-dialog v-model="ruleModalVisible" :title="ruleForm.编号 ? '编辑评价体系规则' : '新建评价体系规则'" width="520px">
      <el-form :model="ruleForm" label-width="90px">
        <el-form-item label="体系名称" required>
          <el-input v-model="ruleForm.规则名称" placeholder="如：临床医学科研人员分类评价体系" />
        </el-form-item>
        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="版本号">
              <el-input-number v-model="ruleForm.版本号" :min="1" style="width: 100%;" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="状态">
              <el-select v-model="ruleForm.状态" style="width: 100%;">
                <el-option label="生效" value="生效" />
                <el-option label="草稿" value="草稿" />
                <el-option label="停用" value="停用" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="适用岗位">
          <el-input v-model="ruleForm.适用岗位" placeholder="如：医师, 专职科研" />
        </el-form-item>
        <el-form-item label="体系说明">
          <el-input v-model="ruleForm.规则说明" type="textarea" rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="ruleModalVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="submitRule">保存</el-button>
      </template>
    </el-dialog>

    <!-- 生成评价测算弹窗 -->
    <el-dialog v-model="generateDialogVisible" title="发起科研绩效评价测算" width="560px">
      <el-form :model="generateForm" label-width="100px">
        <el-form-item label="参评人员ID" required>
          <el-input-number v-model="generateForm.人员编号" :min="1" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="依据规则" required>
          <el-select v-model="generateForm.规则编号" style="width: 100%;">
            <el-option
              v-for="rule in ruleList"
              :key="rule.编号"
              :label="rule.规则名称"
              :value="rule.编号!"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="评价年度" required>
          <el-input-number v-model="generateForm.评价年度" :min="2020" :max="2030" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="测算摘要">
          <el-input v-model="generateForm.结果摘要" placeholder="如：2026年终论文与立项绩效自动汇算" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="generateDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="submitGenerate">
          开始测算
        </el-button>
      </template>
    </el-dialog>

    <!-- 奖励发放与财务回写 -->
    <el-dialog v-model="settlementDialogVisible" title="确认科研奖励发放" width="500px" destroy-on-close>
      <el-form :model="settlementForm" label-width="100px">
        <el-form-item label="发放凭证" required><el-input v-model="settlementForm.发放凭证号" placeholder="工资/奖金发放凭证号" /></el-form-item>
        <el-form-item label="财务状态"><el-select v-model="settlementForm.财务回写状态" style="width:100%"><el-option label="待回写" value="待回写" /><el-option label="已回写" value="已回写" /><el-option label="回写失败" value="回写失败" /></el-select></el-form-item>
        <el-form-item v-if="settlementForm.财务回写状态 === '已回写'" label="财务凭证" required><el-input v-model="settlementForm.财务凭证号" /></el-form-item>
        <el-form-item label="回写说明"><el-input v-model="settlementForm.财务回写说明" type="textarea" rows="2" /></el-form-item>
        <el-form-item label="发放批次号"><el-input v-model="settlementForm.发放批次号" placeholder="工资批次或支付批次" /></el-form-item>
        <el-form-item label="支付状态"><el-select v-model="settlementForm.支付状态" style="width:100%"><el-option label="已支付" value="已支付" /><el-option label="支付失败" value="支付失败" /><el-option label="待支付" value="待支付" /></el-select></el-form-item>
        <el-form-item v-if="settlementForm.支付状态 === '已支付' || settlementForm.支付状态 === '支付失败'" label="支付凭证" required><el-input v-model="settlementForm.支付凭证号" /></el-form-item>
        <el-form-item v-if="settlementForm.支付状态 === '支付失败'" label="失败原因" required><el-input v-model="settlementForm.支付失败原因" type="textarea" :rows="2" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="settlementDialogVisible = false">取消</el-button><el-button type="primary" :loading="settlementSubmitting" @click="submitRewardSettlement">确认发放</el-button></template>
    </el-dialog>

    <!-- 绩效考核动态公式配置弹窗 -->
    <el-dialog
      v-model="formulaModalVisible"
      :title="formulaForm.编号 ? '配置科研绩效动态公式项' : '新增科研绩效动态公式项'"
      width="640px"
      destroy-on-close
    >
      <el-form :model="formulaForm" label-width="120px">
        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="所属规则体系" required>
              <el-select v-model="formulaForm.规则编号" style="width: 100%;">
                <el-option
                  v-for="rule in ruleList"
                  :key="rule.编号"
                  :label="`${rule.规则名称} (v${rule.版本号})`"
                  :value="rule.编号!"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="业务类别" required>
              <el-select v-model="formulaForm.指标类别" style="width: 100%;">
                <el-option label="学术论文" value="学术论文" />
                <el-option label="纵向课题" value="纵向课题" />
                <el-option label="横向课题" value="横向课题" />
                <el-option label="成果专利" value="成果专利" />
                <el-option label="科技奖励" value="科技奖励" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="指标代码" required>
              <el-input v-model="formulaForm.指标代码" placeholder="如：PAPER_SCI_CAS" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="指标名称" required>
              <el-input v-model="formulaForm.指标名称" placeholder="如：SCI/中科院分区论文计分" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="动态计算表达式" required>
          <el-input
            v-model="formulaForm.公式表达式"
            placeholder="如：BASE + (IF * 10) + TOP_BONUS"
          />
          <div style="font-size: 12px; color: #94a3b8; margin-top: 4px;">
            支持变量：BASE(基础分), IF(影响因子), TOP_BONUS(Top期刊加分), FUND_AMOUNT(经费金额)
          </div>
        </el-form-item>

        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="基础分值">
              <el-input-number v-model="formulaForm.基础分值" :min="0" :max="1000" style="width: 100%;" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="启用状态">
              <el-switch v-model="formulaForm.是否启用" active-text="启用" inactive-text="停用" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-divider content-position="left">多作者/参研位次拆分权重 (总和建议1.0)</el-divider>
        <el-row :gutter="12">
          <el-col :span="6">
            <el-form-item label="第一作者" label-width="70px">
              <el-input-number v-model="formulaForm.一作比例" :min="0" :max="1" :step="0.05" :precision="2" style="width: 100%;" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="通讯作者" label-width="70px">
              <el-input-number v-model="formulaForm.通讯比例" :min="0" :max="1" :step="0.05" :precision="2" style="width: 100%;" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="共同一作" label-width="70px">
              <el-input-number v-model="formulaForm.共一比例" :min="0" :max="1" :step="0.05" :precision="2" style="width: 100%;" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="参研位次" label-width="70px">
              <el-input-number v-model="formulaForm.参研比例" :min="0" :max="1" :step="0.05" :precision="2" style="width: 100%;" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="formulaModalVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="submitFormula">
          保存公式项
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Plus, Download, Refresh, Search } from '@element-plus/icons-vue'
import {
  getEvaluationRules,
  getEvaluationRuleDetail,
  saveEvaluationRule,
  saveEvaluationRuleItem,
  generateEvaluation,
  getEvaluationResults,
  getEvaluationDetail,
  reviewEvaluation,
  submitEvaluationAppeal,
  registerEvaluationReward,
  confirmEvaluationRewardSettlement,
  getEvaluationAnnualBatches,
  saveEvaluationAnnualBatch,
  startEvaluationAnnualPublication,
  publishEvaluationAnnual,
  lockEvaluationAnnual,
  settleEvaluationAnnual,
  type EvaluationAnnualBatch,
  getEvaluationRankings,
  getPerformanceFormulas,
  savePerformanceFormula,
  executeAutoEvaluation,
  getCustomReportTemplates,
  queryCustomReport,
  type EvaluationRule,
  type EvaluationRuleItem,
  type EvaluationResult,
  type EvaluationRankingItem,
  type PerformanceFormulaItem,
  type CustomReportTemplate,
  type CustomReportRow
} from '@/api/evaluation'
import { exportToCsv } from '@/utils/export'

const activeTab = ref('ranking')
const submitLoading = ref(false)

// 1. 排名榜单状态
const loadingRankings = ref(false)
const rankingList = ref<EvaluationRankingItem[]>([])
const selectedYear = ref(2026)
const selectedRuleId = ref<number | undefined>(undefined)

// 年度发布、公示、锁定和结算批次
const annualBatches = ref<EvaluationAnnualBatch[]>([])
const selectedBatchId = ref<number | undefined>(undefined)
const selectedBatch = computed(() => annualBatches.value.find(item => item.编号 === selectedBatchId.value))

// 2. 本人绩效结果
const loadingMyResults = ref(false)
const myResultList = ref<EvaluationResult[]>([])

// 3. 规则库状态
const loadingRules = ref(false)
const ruleList = ref<EvaluationRule[]>([])
const ruleQuery = reactive({ 规则名称: '', 适用岗位: '' })
const ruleModalVisible = ref(false)
const ruleForm = reactive<Partial<EvaluationRule>>({
  编号: undefined,
  规则名称: '',
  版本号: 1,
  适用岗位: '',
  状态: '生效',
  规则说明: ''
})
const ruleItemsDrawerVisible = ref(false)
const currentRule = ref<EvaluationRule | null>(null)
const currentRuleItems = ref<EvaluationRuleItem[]>([])

// 4. 详情与证据链抽屉
const detailDrawerVisible = ref(false)
const selectedDetail = ref<any>(null)

// 5. 申诉与奖励
const appealDialogVisible = ref(false)
const appealForm = reactive({
  评价结果编号: 0,
  申诉理由: '',
  期望处理: '',
  补充证据说明: ''
})

const rewardDialogVisible = ref(false)
const rewardForm = reactive<any>({
  评价结果编号: 0,
  编号: undefined as number | undefined,
  人员编号: 0,
  奖励名称: '',
  奖励级别: '院级一等奖',
  奖励金额: 5000,
  状态: '待发放',
  发放日期: '',
  发放凭证号: '',
  财务回写状态: '待回写',
  财务凭证号: '',
  财务回写说明: '',
  发放批次号: '',
  支付状态: '待支付',
  支付凭证号: '',
  支付失败原因: '',
  支付时间: '',
  备注: ''
})
const settlementDialogVisible = ref(false)
const settlementSubmitting = ref(false)
const settlementForm = reactive<any>({ 奖励编号: 0, 发放凭证号: '', 财务回写状态: '待回写', 财务凭证号: '', 财务回写说明: '', 发放批次号: '', 支付状态: '已支付', 支付凭证号: '', 支付失败原因: '', 支付时间: '' })

// 6. 动态公式与位次拆分
const loadingFormulas = ref(false)
const formulaList = ref<PerformanceFormulaItem[]>([])
const formulaModalVisible = ref(false)
const formulaForm = reactive<Partial<PerformanceFormulaItem>>({
  编号: undefined,
  规则编号: 1,
  指标代码: '',
  指标名称: '',
  指标类别: '学术论文',
  基础分值: 50,
  公式表达式: 'BASE + (IF * 10) + TOP_BONUS',
  一作比例: 0.50,
  通讯比例: 0.30,
  共一比例: 0.10,
  参研比例: 0.10,
  是否启用: false
})
const autoEvalLoading = ref(false)

// 7. 自定义多维报表与卫健委国考年报
const loadingReport = ref(false)
const reportTemplates = ref<CustomReportTemplate[]>([])
const selectedReportTemplate = ref('MED_RES_ANNUAL_SUMMARY')
const reportYear = ref(2026)
const reportDataList = ref<CustomReportRow[]>([])
const currentTemplateObj = computed(() => {
  return reportTemplates.value.find(t => t.模板代码 === selectedReportTemplate.value)
})

// 8. 生成测算弹窗
const generateDialogVisible = ref(false)
const generateForm = reactive({
  人员编号: 1,
  规则编号: 1,
  评价年度: 2026,
  周期开始日期: '2026-01-01',
  周期结束日期: '2026-12-31',
  结果摘要: '年度自动评价测算'
})

// 顶部 KPI 计算
const avgScore = computed(() => {
  if (!rankingList.value.length) return '0.0'
  const sum = rankingList.value.reduce((acc, cur) => acc + (cur.总分 || 0), 0)
  return (sum / rankingList.value.length).toFixed(1)
})

const topScore = computed(() => {
  if (!rankingList.value.length) return '0.0'
  const max = Math.max(...rankingList.value.map(item => item.总分 || 0))
  return max.toFixed(1)
})

function getStatusTagType(status?: string) {
  if (status === '已发布') return 'success'
  if (status === '已修订') return 'primary'
  if (status === '申诉中') return 'warning'
  if (status === '作废') return 'danger'
  return 'info'
}

// 1. 获取排名榜单
async function fetchRankings() {
  loadingRankings.value = true
  try {
    const res = await getEvaluationRankings({
      评价年度: selectedYear.value,
      规则编号: selectedRuleId.value
    })
    rankingList.value = res || []
  } catch (error) {
    console.error('获取科研评价排名失败:', error)
  } finally {
    loadingRankings.value = false
  }
}

// 2. 获取本人绩效
async function fetchMyResults() {
  loadingMyResults.value = true
  try {
    const res = await getEvaluationResults({ 评价年度: selectedYear.value })
    myResultList.value = res || []
  } catch (error) {
    console.error('获取本人绩效结果失败:', error)
  } finally {
    loadingMyResults.value = false
  }
}

// 3. 获取规则列表
async function fetchRules() {
  loadingRules.value = true
  try {
    const res = await getEvaluationRules(ruleQuery)
    ruleList.value = res || []
  } catch (error) {
    console.error('获取评价规则失败:', error)
  } finally {
    loadingRules.value = false
  }
}

function onTabChange(tab: any) {
  if (tab === 'ranking') fetchRankings()
  else if (tab === 'my_evaluation') fetchMyResults()
  else if (tab === 'rules') fetchRules()
  else if (tab === 'formula_designer') fetchFormulas()
  else if (tab === 'custom_reports') {
    if (!reportTemplates.value.length) {
      fetchReportTemplates().then(() => fetchCustomReportData())
    } else {
      fetchCustomReportData()
    }
  }
}

function refreshActiveTab() {
  if (activeTab.value === 'ranking') fetchRankings()
  else if (activeTab.value === 'my_evaluation') fetchMyResults()
  else if (activeTab.value === 'rules') fetchRules()
  else if (activeTab.value === 'formula_designer') fetchFormulas()
  else if (activeTab.value === 'custom_reports') fetchCustomReportData()
  else if (activeTab.value === 'management') { fetchRankings(); fetchAnnualBatches() }
  else fetchRankings()
}

// 调阅明细证据链
async function viewEvaluationExplainDrawer(resultId: number) {
  try {
    const res = await getEvaluationDetail(resultId)
    selectedDetail.value = res
    detailDrawerVisible.value = true
  } catch (error) {
    console.error('获取评价详情失败:', error)
  }
}

function openPersonDetail(personId: number) {
  const target = rankingList.value.find(item => item.人员编号 === personId)
  if (target) {
    viewEvaluationExplainDrawer(personId)
  }
}

// 打开申诉
function openAppealDialog(resultId: number) {
  appealForm.评价结果编号 = resultId
  appealForm.申诉理由 = ''
  appealForm.期望处理 = ''
  appealForm.补充证据说明 = ''
  appealDialogVisible.value = true
}

async function submitAppeal() {
  if (!appealForm.申诉理由) {
    ElMessage.warning('请输入申诉理由')
    return
  }
  submitLoading.value = true
  try {
    await submitEvaluationAppeal(appealForm)
    ElMessage.success('申诉已提交委员会处理')
    appealDialogVisible.value = false
    fetchMyResults()
  } catch (error: any) {
    ElMessage.error(error.message || '提交申诉失败')
  } finally {
    submitLoading.value = false
  }
}

// 登记奖励
function openRewardDialog(row: EvaluationRankingItem) {
  rewardForm.评价结果编号 = row.评价结果编号 || 0
  rewardForm.编号 = undefined
  rewardForm.人员编号 = row.人员编号
  rewardForm.奖励名称 = `${selectedYear.value}年度科研创新标杆奖`
  rewardForm.奖励金额 = 5000
  rewardForm.状态 = '待发放'
  rewardForm.发放日期 = ''
  rewardForm.发放凭证号 = ''
  rewardForm.财务回写状态 = '待回写'
  rewardForm.财务凭证号 = ''
  rewardForm.财务回写说明 = ''
  rewardForm.发放批次号 = ''
  rewardForm.支付状态 = '待支付'
  rewardForm.支付凭证号 = ''
  rewardForm.支付失败原因 = ''
  rewardForm.支付时间 = ''
  rewardDialogVisible.value = true
}

async function submitReward() {
  if (!rewardForm.奖励名称) {
    ElMessage.warning('请输入奖励名称')
    return
  }
  submitLoading.value = true
  try {
    if (rewardForm.状态 === '已发放') {
      if (!rewardForm.发放凭证号) {
        ElMessage.warning('已发放奖励必须填写发放凭证号')
        return
      }
      if (rewardForm.财务回写状态 === '已回写' && !rewardForm.财务凭证号) {
        ElMessage.warning('已回写奖励必须填写财务凭证号')
        return
      }
    }
    if ((rewardForm.支付状态 === '已支付' || rewardForm.支付状态 === '支付失败') && !rewardForm.支付凭证号) {
      ElMessage.warning('已支付或支付失败必须填写支付凭证号/失败流水号')
      return
    }
    if (rewardForm.支付状态 === '支付失败' && !rewardForm.支付失败原因) {
      ElMessage.warning('支付失败必须填写失败原因')
      return
    }
    await registerEvaluationReward(rewardForm)
    ElMessage.success('奖励登记成功')
    rewardDialogVisible.value = false
  } catch (error: any) {
    ElMessage.error(error.message || '登记奖励失败')
  } finally {
    submitLoading.value = false
  }
}

function openRewardSettlement(row: any) {
  Object.assign(settlementForm, { 奖励编号: row.编号, 发放凭证号: '', 财务回写状态: '待回写', 财务凭证号: '', 财务回写说明: '', 发放批次号: '', 支付状态: '已支付', 支付凭证号: '', 支付失败原因: '', 支付时间: '' })
  settlementDialogVisible.value = true
}

async function submitRewardSettlement() {
  if (!settlementForm.奖励编号 || !settlementForm.发放凭证号) {
    ElMessage.warning('请填写发放凭证号')
    return
  }
  if (settlementForm.财务回写状态 === '已回写' && !settlementForm.财务凭证号) {
    ElMessage.warning('已回写必须填写财务凭证号')
    return
  }
  if ((settlementForm.支付状态 === '已支付' || settlementForm.支付状态 === '支付失败') && !settlementForm.支付凭证号) {
    ElMessage.warning('请填写支付凭证或失败流水号')
    return
  }
  if (settlementForm.支付状态 === '支付失败' && !settlementForm.支付失败原因) {
    ElMessage.warning('支付失败必须填写失败原因')
    return
  }
  settlementSubmitting.value = true
  try {
    await confirmEvaluationRewardSettlement(settlementForm)
    ElMessage.success('奖励已确认发放并记录财务回写状态')
    settlementDialogVisible.value = false
    if (selectedDetail.value?.评价结果?.编号) await viewEvaluationExplainDrawer(selectedDetail.value.评价结果.编号)
  } catch (error: any) {
    ElMessage.error(error.message || '确认奖励发放失败')
  } finally {
    settlementSubmitting.value = false
  }
}

// 年度评价批次操作
async function fetchAnnualBatches() {
  try {
    const res = await getEvaluationAnnualBatches(selectedYear.value)
    annualBatches.value = ((res as any)?.data || res || []) as EvaluationAnnualBatch[]
    if (!selectedBatchId.value && annualBatches.value.length) selectedBatchId.value = annualBatches.value[0].编号
  } catch (error: any) { ElMessage.error(error.message || '获取年度批次失败') }
}
function onBatchChange() { /* computed 状态自动刷新 */ }
async function createAnnualBatch() {
  try {
    await saveEvaluationAnnualBatch({ 评价年度: selectedYear.value, 批次名称: `${selectedYear.value}年度科研绩效评价`, 版本号: 1, 状态: '草稿' })
    ElMessage.success('年度评价草稿批次已创建')
    await fetchAnnualBatches()
  } catch (error: any) { ElMessage.error(error.message || '创建年度批次失败') }
}
async function startAnnualPublication() {
  if (!selectedBatch.value) return
  try { await startEvaluationAnnualPublication({ 批次编号: selectedBatch.value.编号 }); ElMessage.success('已开始年度公示'); await fetchAnnualBatches() } catch (error: any) { ElMessage.error(error.message || '开始公示失败') }
}
async function publishAnnual() {
  if (!selectedBatch.value) return
  try { await publishEvaluationAnnual({ 批次编号: selectedBatch.value.编号 }); ElMessage.success('年度评价已正式发布'); await fetchAnnualBatches() } catch (error: any) { ElMessage.error(error.message || '正式发布失败') }
}
async function lockAnnual() {
  if (!selectedBatch.value) return
  try { await lockEvaluationAnnual({ 批次编号: selectedBatch.value.编号 }); ElMessage.success('年度评价已锁定'); await fetchAnnualBatches() } catch (error: any) { ElMessage.error(error.message || '锁定年度失败') }
}
async function settleAnnual() {
  if (!selectedBatch.value) return
  try { await settleEvaluationAnnual({ 批次编号: selectedBatch.value.编号 }); ElMessage.success('年度评价已完成结算'); await fetchAnnualBatches() } catch (error: any) { ElMessage.error(error.message || '年度结算失败') }
}

// 规则操作
function openRuleModal(row?: EvaluationRule) {
  if (row) {
    Object.assign(ruleForm, row)
  } else {
    Object.assign(ruleForm, {
      编号: undefined,
      规则名称: '',
      版本号: 1,
      适用岗位: '',
      状态: '生效',
      规则说明: ''
    })
  }
  ruleModalVisible.value = true
}

async function submitRule() {
  if (!ruleForm.规则名称) {
    ElMessage.warning('请输入规则名称')
    return
  }
  submitLoading.value = true
  try {
    await saveEvaluationRule(ruleForm)
    ElMessage.success('规则保存成功')
    ruleModalVisible.value = false
    fetchRules()
  } catch (error: any) {
    ElMessage.error(error.message || '保存失败')
  } finally {
    submitLoading.value = false
  }
}

async function openRuleItemsDrawer(rule: EvaluationRule) {
  currentRule.value = rule
  ruleItemsDrawerVisible.value = true
  try {
    const res = await getEvaluationRuleDetail(rule.编号!)
    currentRuleItems.value = res.规则项 || []
  } catch (error) {
    console.error('获取规则项失败:', error)
  }
}

function openRuleItemDialog() {
  ElMessage.info('正在启用指标项快速配置...')
  saveEvaluationRuleItem({
    规则编号: currentRule.value?.编号 || 1,
    项目代码: `ITEM_${Date.now().toString().slice(-4)}`,
    项目名称: '高质量学术论著发表',
    权重: 1.0,
    最高分: 100,
    证据类型: '论文'
  }).then(() => {
    ElMessage.success('已自动补充指标项')
    if (currentRule.value) openRuleItemsDrawer(currentRule.value)
  })
}

// 生成测算
function openGenerateDialog() {
  if (ruleList.value.length) {
    generateForm.规则编号 = ruleList.value[0].编号 || 1
  }
  generateDialogVisible.value = true
}

async function submitGenerate() {
  submitLoading.value = true
  try {
    await generateEvaluation({
      人员编号: generateForm.人员编号,
      规则编号: generateForm.规则编号,
      评价年度: generateForm.评价年度,
      周期开始日期: generateForm.周期开始日期,
      周期结束日期: generateForm.周期结束日期,
      结果摘要: generateForm.结果摘要,
      明细: [
        { 规则项编号: 1, 原始值: 85, 证据类型: '论文', 证据标题: '高影响因子论文', 解释: '按规则计入' }
      ]
    })
    ElMessage.success('绩效测算已生成')
    generateDialogVisible.value = false
    fetchRankings()
  } catch (error: any) {
    ElMessage.error(error.message || '测算生成失败')
  } finally {
    submitLoading.value = false
  }
}

function openReviewDialog() {
  reviewEvaluation({
    评价结果编号: rankingList.value[0]?.人员编号 || 1,
    状态: '已发布',
    复核意见: '学术委员会审核通过'
  }).then(() => {
    ElMessage.success('已将最新测算结果复核发布')
    fetchRankings()
  }).catch((err: any) => {
    ElMessage.error(err.message || '复核失败')
  })
}

// 导出 CSV
function handleExportRankings() {
  try {
    exportToCsv(
      `全院科研综合绩效排名榜单_${selectedYear.value}`,
      [
        { label: '排名', prop: '排名' },
        { label: '姓名', prop: '姓名' },
        { label: '岗位/科室', prop: '岗位名称' },
        { label: '学科', prop: '学科门类' },
        { label: '评价年度', prop: '评价年度' },
        { label: '总得分', prop: '总分' },
        { label: '规则体系', prop: '规则名称' },
        { label: '版本', prop: '版本号' },
        { label: '状态', prop: '状态' }
      ],
      rankingList.value
    )
    ElMessage.success('绩效排名总榜导出成功')
  } catch (error: any) {
    ElMessage.warning(error.message || '导出失败')
  }
}

// 7. 动态公式配置与自动测算
async function fetchFormulas() {
  loadingFormulas.value = true
  try {
    const res = await getPerformanceFormulas(selectedRuleId.value)
    formulaList.value = res || []
  } catch (error) {
    console.error('获取动态公式失败:', error)
  } finally {
    loadingFormulas.value = false
  }
}

function openFormulaModal(row?: PerformanceFormulaItem) {
  if (row) {
    Object.assign(formulaForm, row)
  } else {
    Object.assign(formulaForm, {
      编号: undefined,
      规则编号: ruleList.value[0]?.编号 || 1,
      指标代码: `RULE_${Date.now().toString().slice(-4)}`,
      指标名称: '',
      指标类别: '学术论文',
      基础分值: 50,
      公式表达式: 'BASE + (IF * 10) + TOP_BONUS',
      一作比例: 0.50,
      通讯比例: 0.30,
      共一比例: 0.10,
      参研比例: 0.10,
      是否启用: false
    })
  }
  formulaModalVisible.value = true
}

async function submitFormula() {
  if (!formulaForm.指标代码 || !formulaForm.指标名称) {
    ElMessage.warning('请填写指标代码与名称')
    return
  }
  submitLoading.value = true
  try {
    await savePerformanceFormula(formulaForm)
    ElMessage.success('动态公式项已保存')
    formulaModalVisible.value = false
    fetchFormulas()
  } catch (error: any) {
    ElMessage.error(error.message || '保存公式项失败')
  } finally {
    submitLoading.value = false
  }
}

async function handleExecuteAutoEval() {
  autoEvalLoading.value = true
  try {
    const res = await executeAutoEvaluation({
      评价年度: selectedYear.value,
      规则编号: selectedRuleId.value
    })
    ElMessage.success(res.提示消息 || `年度科研绩效自动测算完成！参评${res.测算人数}人，人均${res.人均得分}分`)
    fetchRankings()
    fetchMyResults()
  } catch (error: any) {
    ElMessage.error(error.message || '测算执行失败')
  } finally {
    autoEvalLoading.value = false
  }
}

// 8. 自定义多维科研报表与卫健委国考年报
async function fetchReportTemplates() {
  try {
    const res = await getCustomReportTemplates()
    reportTemplates.value = res || []
    if (res && res.length && !selectedReportTemplate.value) {
      selectedReportTemplate.value = res[0].模板代码
    }
  } catch (error) {
    console.error('获取自定义报表模板失败:', error)
  }
}

async function fetchCustomReportData() {
  if (!selectedReportTemplate.value) return
  loadingReport.value = true
  try {
    const res = await queryCustomReport({
      模板代码: selectedReportTemplate.value,
      年度: reportYear.value
    })
    reportDataList.value = res || []
  } catch (error) {
    console.error('获取自定义报表数据失败:', error)
  } finally {
    loadingReport.value = false
  }
}

function handleExportCustomReport() {
  if (!reportDataList.value.length) {
    ElMessage.warning('暂无数据可导出')
    return
  }
  try {
    const tplName = currentTemplateObj.value?.模板名称 || '自定义科研报表'
    exportToCsv(
      `${tplName}_${reportYear.value}`,
      [
        { label: '科室/部门', prop: '分组维度' },
        { label: '年度', prop: '统计年度' },
        { label: '学术论文总数', prop: '学术论文总数' },
        { label: '1区Top篇数', prop: '一区Top论文数' },
        { label: '最高IF', prop: '最高影响因子' },
        { label: '纵向立项数', prop: '纵向立项数' },
        { label: '纵向经费(元)', prop: '纵向立项经费' },
        { label: '横向合同数', prop: '横向合同数' },
        { label: '横向金额(元)', prop: '横向合同金额' },
        { label: '专利授权数', prop: '专利授权数' },
        { label: '科研绩效总分', prop: '科研绩效总分' },
        { label: '国考达标率(%)', prop: '国考科研达标率' }
      ],
      reportDataList.value
    )
    ElMessage.success('自定义报表导出成功')
  } catch (error: any) {
    ElMessage.warning(error.message || '导出失败')
  }
}

onMounted(() => {
  fetchAnnualBatches()
  fetchRules()
  fetchRankings()
  fetchReportTemplates()
})
</script>

<style scoped lang="scss">
.page-container {
  padding: 16px;

  .main-card {
    border-radius: 8px;
    background: #ffffff;
  }
}

.header-section {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 20px;

  .page-title {
    font-size: 20px;
    font-weight: 600;
    color: #1e293b;
    margin: 0 0 6px 0;
  }

  .page-subtitle {
    font-size: 13px;
    color: #64748b;
    margin: 0;
  }

  .action-buttons {
    display: flex;
    gap: 8px;
  }
}

.stat-cards {
  margin-bottom: 20px;

  .stat-card {
    padding: 14px 18px;
    border-radius: 8px;
    display: flex;
    flex-direction: column;
    justify-content: center;

    .label {
      font-size: 13px;
      margin-bottom: 6px;
    }

    .val {
      font-size: 22px;
      font-weight: 700;
    }

    &.stat-blue {
      background: #eff6ff;
      color: #1d4ed8;
    }

    &.stat-green {
      background: #f0fdf4;
      color: #15803d;
    }

    &.stat-orange {
      background: #fff7ed;
      color: #c2410c;
    }

    &.stat-purple {
      background: #faf5ff;
      color: #7e22ce;
    }
  }
}

.evaluation-tabs {
  margin-top: 10px;
}

.search-bar {
  display: flex;
  gap: 10px;
  align-items: center;
  flex-wrap: wrap;
  margin-top: 8px;
}

.drawer-box {
  .result-summary-card {
    background: #f8fafc;
    padding: 16px;
    border-radius: 8px;

    .top-row {
      display: flex;
      justify-content: space-between;
      align-items: center;

      h3 {
        margin: 0;
        font-size: 16px;
        color: #0f172a;
      }

      .score-badge {
        font-size: 22px;
        font-weight: 700;
        color: #409eff;
      }
    }

    .meta {
      margin: 8px 0 0 0;
      font-size: 12px;
      color: #64748b;
    }

    .review-note {
      margin: 8px 0 0 0;
      font-size: 12px;
      color: #b45309;
      background: #fef3c7;
      padding: 6px 10px;
      border-radius: 4px;
    }
  }
}
</style>
