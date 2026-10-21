<template>
  <div class="application-page">
    <!-- 顶部数据驾驶舱 KPI 汇总指标卡片 -->
    <div class="stats-overview">
      <el-row :gutter="16">
        <el-col :xs="12" :sm="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-content">
              <div class="stat-icon bg-blue">
                <el-icon><Collection /></el-icon>
              </div>
              <div class="stat-meta">
                <div class="stat-label">申报项目总数</div>
                <div class="stat-value text-blue">{{ statsData.totalCount }} <span class="stat-unit">项</span></div>
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :xs="12" :sm="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-content">
              <div class="stat-icon bg-green">
                <el-icon><Money /></el-icon>
              </div>
              <div class="stat-meta">
                <div class="stat-label">申报经费总额</div>
                <div class="stat-value text-green">¥{{ formatWan(statsData.totalBudget) }} <span class="stat-unit">万</span></div>
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :xs="12" :sm="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-content">
              <div class="stat-icon bg-purple">
                <el-icon><TrendCharts /></el-icon>
              </div>
              <div class="stat-meta">
                <div class="stat-label">重点/重大项目</div>
                <div class="stat-value text-purple">{{ statsData.keyProjectCount }} <span class="stat-unit">项</span></div>
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :xs="12" :sm="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-content">
              <div class="stat-icon bg-amber">
                <el-icon><Check /></el-icon>
              </div>
              <div class="stat-meta">
                <div class="stat-label">审查通过 / 推荐</div>
                <div class="stat-value text-amber">{{ statsData.recommendedCount }} <span class="stat-unit">项</span></div>
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </div>

    <!-- 院级项目批次配置：通过 /project/vertical-application?tab=plans 打开，普通人员只看到已发布批次 -->
    <el-card v-if="route.query.tab === 'plans'" shadow="never" class="batch-config-card">
      <div class="batch-toolbar">
        <div>
          <div class="section-title">院级项目申报批次</div>
          <div class="batch-help">配置申报窗口、个人/总量限额、科室资格和评审计划；提交时服务端会再次核验。</div>
        </div>
        <el-button v-if="isPlanManager" type="primary" :icon="Plus" @click="openBatchDialog">新建批次</el-button>
      </div>
      <el-table v-loading="batchLoading" :data="batchRows" size="small" border stripe style="width: 100%; margin-top: 12px;">
        <el-table-column prop="计划年度" label="年度" width="75" align="center" />
        <el-table-column prop="批次名称" label="批次名称" min-width="180" />
        <el-table-column label="申报窗口" width="210" align="center"><template #default="{ row }">{{ formatDate(row.发布开始日期) }} ~ {{ formatDate(row.截止日期) }}</template></el-table-column>
        <el-table-column label="限额" width="130" align="center"><template #default="{ row }">个人 {{ row.个人申报限额 }}<span v-if="row.批次总限额"> / 总 {{ row.批次总限额 }}</span></template></el-table-column>
        <el-table-column label="评审计划" width="180" align="center"><template #default="{ row }">{{ row.评审开始日期 || row.评审截止日期 ? `${formatDate(row.评审开始日期)} ~ ${formatDate(row.评审截止日期)}` : '未配置' }}</template></el-table-column>
        <el-table-column prop="状态" label="状态" width="80" align="center" />
        <el-table-column v-if="isPlanManager" label="操作" width="150" align="center"><template #default="{ row }"><el-button type="primary" link size="small" @click="editBatch(row)">编辑</el-button><el-button v-if="row.状态 === '草稿'" type="success" link size="small" @click="publishBatch(row)">发布</el-button><el-button v-else-if="row.状态 === '发布'" type="warning" link size="small" @click="closeBatch(row)">关闭</el-button></template></el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="batchDialogVisible" :title="batchForm.编号 ? '编辑院级项目批次' : '新建院级项目批次'" width="680px">
      <el-form :model="batchForm" label-width="120px">
        <el-row :gutter="12"><el-col :span="12"><el-form-item label="计划年度"><el-input-number v-model="batchForm.计划年度" :min="2000" :max="2200" style="width: 100%;" /></el-form-item></el-col><el-col :span="12"><el-form-item label="批次名称"><el-input v-model="batchForm.批次名称" maxlength="200" /></el-form-item></el-col></el-row>
        <el-row :gutter="12"><el-col :span="12"><el-form-item label="申报开始"><el-date-picker v-model="batchForm.发布开始日期" value-format="YYYY-MM-DD" type="date" style="width: 100%;" /></el-form-item></el-col><el-col :span="12"><el-form-item label="申报截止"><el-date-picker v-model="batchForm.截止日期" value-format="YYYY-MM-DD" type="date" style="width: 100%;" /></el-form-item></el-col></el-row>
        <el-row :gutter="12"><el-col :span="12"><el-form-item label="个人限额"><el-input-number v-model="batchForm.个人申报限额" :min="1" :max="1000" /></el-form-item></el-col><el-col :span="12"><el-form-item label="批次总限额"><el-input-number v-model="batchForm.批次总限额" :min="1" :max="100000" clearable /></el-form-item></el-col></el-row>
        <el-form-item label="允许科室编号"><el-input v-model="batchForm.允许部门编号文本" placeholder="可选，逗号分隔；留空表示全院" /></el-form-item>
        <el-row :gutter="12"><el-col :span="12"><el-form-item label="评审开始"><el-date-picker v-model="batchForm.评审开始日期" value-format="YYYY-MM-DD" type="date" style="width: 100%;" /></el-form-item></el-col><el-col :span="12"><el-form-item label="评审截止"><el-date-picker v-model="batchForm.评审截止日期" value-format="YYYY-MM-DD" type="date" style="width: 100%;" /></el-form-item></el-col></el-row>
        <el-form-item label="评审方案"><el-input v-model="batchForm.评审方案文本" type="textarea" :rows="3" placeholder='JSON，例如：{"方式":"匿名评审","专家数":3,"评分项":["创新性","可行性"]}' /></el-form-item>
        <el-form-item label="批次说明"><el-input v-model="batchForm.批次说明" type="textarea" :rows="2" maxlength="2000" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="batchDialogVisible = false">取消</el-button><el-button type="primary" :loading="batchSaving" @click="saveBatch">保存草稿</el-button></template>
    </el-dialog>

    <!-- 主卡片：检索过滤与申报列表 -->
    <el-card shadow="never" class="main-table-card">
      <div class="toolbar">
        <el-form :inline="true" :model="queryFilter" class="filter-form">
          <el-form-item label="项目名称">
            <el-input
              v-model="queryFilter.Like名称"
              placeholder="搜索项目名称或关键字"
              clearable
              style="width: 220px;"
              @keyup.enter="handleSearch"
            />
          </el-form-item>
          <el-form-item label="申报负责人">
            <el-input
              v-model="queryFilter.Like申报人姓名"
              placeholder="负责人姓名/工号"
              clearable
              style="width: 150px;"
              @keyup.enter="handleSearch"
            />
          </el-form-item>
          <el-form-item label="项目级别">
            <el-select v-model="selectedLevel" placeholder="全部级别" clearable style="width: 140px;">
              <el-option label="全部级别" value="" />
              <el-option label="国家级重点" value="国家级重点" />
              <el-option label="省部级重点" value="省部级重点" />
              <el-option label="校级培育" value="校级重点" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
            <el-button :icon="Refresh" @click="resetSearch">重置</el-button>
            <el-button type="success" :icon="Plus" @click="openApplicationDrawer">发起新申报</el-button>
          </el-form-item>
        </el-form>
      </div>

      <!-- 申报项目列表表格 -->
      <el-table
        v-loading="loading"
        :data="filteredList"
        stripe
        border
        style="width: 100%; margin-top: 14px;"
        :header-cell-style="{ background: '#f8fafc', color: '#334155', fontWeight: '600' }"
      >
        <el-table-column prop="编号" label="申报编号" width="90" align="center" />
        <el-table-column label="项目基本信息" min-width="260">
          <template #default="{ row }">
            <div class="project-title-cell">
              <span class="project-name">{{ row.项目名称 || row.名称 }}</span>
              <div class="project-sub-meta">
                <el-tag size="small" type="info" effect="plain" class="meta-tag">
                  {{ row.学科门类 || row.学科名称 || '医学' }}
                </el-tag>
                <el-tag size="small" :type="getLevelTag(row.项目级别)" class="meta-tag">
                  {{ row.项目级别 || '国家级重点' }}
                </el-tag>
                <el-tag size="small" type="success" effect="plain" class="meta-tag">
                  v{{ row.版本号 || 1 }}
                </el-tag>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="申报人 / 部门" width="160">
          <template #default="{ row }">
            <div class="applicant-cell">
              <div class="applicant-name">
                <el-icon style="margin-right: 4px; vertical-align: middle;"><User /></el-icon>
                <span>{{ row.姓名 || row.申报人姓名 || (row.工号 ? `工号 ${row.工号}` : '未指定') }}</span>
              </div>
              <div class="applicant-dept">{{ row.部门名称 || '临床科研中心' }}</div>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="申请经费" width="130" align="right">
          <template #default="{ row }">
            <span class="budget-amount">¥{{ formatMoney(row.申请经费) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="执行周期" width="160" align="center">
          <template #default="{ row }">
            <div class="date-range-text">
              {{ formatDate(row.计划开始日期 || row.申报时间 || row.申报日期) }}
              <span class="date-separator">~</span>
              {{ formatDate(row.计划完成日期 || '2028-12-31') }}
            </div>
          </template>
        </el-table-column>
        <el-table-column label="审核进度与状态" width="150" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusTag(row.状态名称 || row.项目状态 || row.审核进度)" effect="light">
              {{ row.步骤名称 || row.状态名称 || row.项目状态 || '初审中' }}
            </el-tag>
            <div v-if="row.审核进度" class="step-desc-text">{{ row.审核进度 }}</div>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="230" fixed="right" align="center">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="viewReviews(row)">评审汇总</el-button>
            <el-button type="info" link size="small" @click="viewVersionDiff(row)">版本差异</el-button>
            <el-button type="warning" link size="small" @click="openPreCheck(row)">智能预检</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页栏 -->
      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="paging.index"
          v-model:page-size="paging.size"
          :total="totalCount"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="fetchApplications"
          @current-change="fetchApplications"
        />
      </div>
    </el-card>

    <!-- 新建申报抽屉（4步智能分步向导） -->
    <el-drawer
      v-model="drawerVisible"
      title="发起纵向科研项目申报"
      size="720px"
      :close-on-click-modal="false"
      destroy-on-close
    >
      <div class="drawer-inner">
        <el-steps :active="currentStep" finish-status="success" simple class="step-header">
          <el-step title="基本资料" />
          <el-step title="团队与预算" />
          <el-step title="论证与伦理" />
          <el-step title="智能预检" />
        </el-steps>

        <!-- Step 0: 基本信息 -->
        <div v-show="currentStep === 0" class="step-body">
          <el-alert
            title="填写纵向科研项目基本申报信息，带 * 项为必填项。"
            type="info"
            :closable="false"
            show-icon
            style="margin-bottom: 16px;"
          />
          <el-form :model="appForm" label-width="120px" class="compact-form">
            <el-form-item label="项目名称" required>
              <el-input
                v-model="appForm.名称"
                placeholder="请输入申报项目全称（如：脑微环境免疫机制研究）"
                maxlength="100"
                show-word-limit
              />
            </el-form-item>
            <el-row :gutter="12">
              <el-col :span="12">
                <el-form-item label="学科门类" required>
                  <el-select v-model="appForm.学科门类" style="width: 100%;">
                    <el-option label="医学与生命科学" value="医学" />
                    <el-option label="生物医药工程" value="工学" />
                    <el-option label="公共卫生与预防医学" value="理学" />
                    <el-option label="中西医结合与中药学" value="中医学" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="申报级别" required>
                  <el-select v-model="appForm.项目级别" style="width: 100%;">
                    <el-option label="国家级重大/重点" value="国家级重点" />
                    <el-option label="国家自然科学基金面上" value="国家级面上" />
                    <el-option label="国家自然科学基金青年" value="国家级青年" />
                    <el-option label="省部级重点专项" value="省部级重点" />
                    <el-option label="校级培育基金" value="校级重点" />
                  </el-select>
                </el-form-item>
              </el-col>
            </el-row>
            <el-form-item label="院级项目批次">
              <el-select v-model="appForm.院级批次编号" clearable placeholder="可选，选择已发布院级批次" style="width: 100%;">
                <el-option v-for="batch in batchRows.filter(item => item.状态 === '发布' && item.是否可申报 !== false)" :key="batch.编号" :label="`${batch.计划年度}｜${batch.批次名称}（个人限额${batch.个人申报限额}）`" :value="batch.编号" />
              </el-select>
            </el-form-item>
            <el-row :gutter="12">
              <el-col :span="12">
                <el-form-item label="申请经费(元)" required>
                  <el-input-number
                    v-model="appForm.申请经费"
                    :min="10000"
                    :max="50000000"
                    :step="50000"
                    style="width: 100%;"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="申报年度" required>
                  <el-input-number v-model="appForm.申报年度" :min="2020" :max="2030" style="width: 100%;" />
                </el-form-item>
              </el-col>
            </el-row>
            <el-form-item label="执行周期" required>
              <el-date-picker
                v-model="appForm.dateRange"
                type="daterange"
                range-separator="至"
                start-placeholder="拟开始日期"
                end-placeholder="拟结题日期"
                value-format="YYYY-MM-DD"
                style="width: 100%;"
              />
            </el-form-item>
          </el-form>
        </div>

        <!-- Step 1: 团队与预算分解 -->
        <div v-show="currentStep === 1" class="step-body">
          <div class="section-title">
            <span>预算明细分解（单位：万元）</span>
            <el-tag size="small" type="success">总预算：{{ (appForm.申请经费 / 10000).toFixed(2) }} 万元</el-tag>
          </div>
          <el-row :gutter="12" style="margin-bottom: 16px;">
            <el-col :span="8">
              <el-form-item label="直接科研费">
                <el-input-number v-model="budgetBreakdown.直接费" :min="0" :step="5" style="width: 100%;" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="仪器设备购置">
                <el-input-number v-model="budgetBreakdown.设备费" :min="0" :step="2" style="width: 100%;" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="间接/绩效费用">
                <el-input-number v-model="budgetBreakdown.间接费" :min="0" :step="2" style="width: 100%;" />
              </el-form-item>
            </el-col>
          </el-row>

          <div class="section-title" style="margin-top: 18px;">
            <span>项目核心成员列表</span>
            <el-button type="primary" link size="small" :icon="Plus" @click="addParticipant">添加成员</el-button>
          </div>
          <el-table :data="participants" size="small" border stripe style="width: 100%;">
            <el-table-column prop="参与人编号" label="人员编号" width="90">
              <template #default="{ row }">
                <el-input-number v-model="row.参与人编号" :min="1" size="small" controls-position="right" style="width: 100%;" />
              </template>
            </el-table-column>
            <el-table-column label="工作量占比 (%)" width="130">
              <template #default="{ row }">
                <el-input-number v-model="row.工作量占比" :min="1" :max="100" size="small" controls-position="right" style="width: 100%;" />
              </template>
            </el-table-column>
            <el-table-column prop="备注" label="项目分工与职责">
              <template #default="{ row }">
                <el-input v-model="row.备注" size="small" placeholder="如：主持课题、动物实验、单细胞测序" />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="70" align="center">
              <template #default="{ $index }">
                <el-button
                  type="danger"
                  link
                  size="small"
                  :disabled="participants.length <= 1"
                  @click="participants.splice($index, 1)"
                >
                  删除
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>

        <!-- Step 2: 论证与伦理 -->
        <div v-show="currentStep === 2" class="step-body">
          <el-form :model="appForm" label-width="120px">
            <el-form-item label="研究摘要" required>
              <el-input
                v-model="appForm.摘要"
                type="textarea"
                rows="4"
                placeholder="简述科学假说、立项依据、关键技术路线、临床意义与考核指标（正式提交时不少于50字）"
              />
            </el-form-item>
            <el-form-item label="外委实验">
              <el-radio-group v-model="appForm.是否委托外单位进行实验或实验分析">
                <el-radio :value="false">无外委实验</el-radio>
                <el-radio :value="true">涉及第三方外委检测/质谱分析</el-radio>
              </el-radio-group>
            </el-form-item>
            <el-form-item
              v-if="appForm.是否委托外单位进行实验或实验分析"
              label="外委检测说明"
              required
            >
              <el-input
                v-model="appForm.实验委托检测内容"
                placeholder="请详细列明第三方质谱、流式或测序外包服务单位与内容"
              />
            </el-form-item>
            <el-form-item label="伦理审查要求">
              <el-radio-group v-model="appForm.伦理要求">
                <el-radio :value="false">不涉及人体/动物伦理审查</el-radio>
                <el-radio :value="true">涉及人体标本或动物实验（需前置伦理批文）</el-radio>
              </el-radio-group>
            </el-form-item>
            <template v-if="appForm.伦理要求">
              <el-row :gutter="12">
                <el-col :span="12">
                  <el-form-item label="伦理批文编号">
                    <el-input v-model="appForm.伦理批文编号" placeholder="如：KY2026-ETH-089" />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="批文电子件">
                    <el-input v-model="appForm.伦理批文路径" placeholder="/upload/纵向项目/伦理批文/approval.pdf" />
                  </el-form-item>
                </el-col>
              </el-row>
            </template>
            <el-form-item label="申报书正文路径">
              <el-input v-model="appForm.项目申报书路径" placeholder="/upload/纵向项目/项目申报书/application.pdf" />
            </el-form-item>
          </el-form>
        </div>

        <!-- Step 3: 智能预检与提交 -->
        <div v-show="currentStep === 3" class="step-body">
          <div class="precheck-action-bar">
            <div>
              <div style="font-weight: 600; font-size: 15px; color: #1e293b;">
                服务端规则库完整性与冲突智能排查
              </div>
              <div style="font-size: 13px; color: #64748b; margin-top: 4px;">
                涵盖：重名排查、申报限项、预算合理性、伦理一致性、成员状态与资质阻断项。
              </div>
            </div>
            <el-button type="primary" :icon="Search" :loading="precheckLoading" @click="runPreCheck">
              立即执行智能预检
            </el-button>
          </div>

          <div v-if="preCheckData" class="precheck-results-box">
            <el-alert
              :title="preCheckData.通过 ? '预检查全部通过：各项指标均符合申报规范！' : '预检查提示：存在阻断项或合规警告，请核正后再正式提交'"
              :type="preCheckData.通过 ? 'success' : 'error'"
              :closable="false"
              show-icon
              style="margin-bottom: 14px;"
            />
            <el-table :data="normalizedCheckItems" size="small" border stripe style="width: 100%;">
              <el-table-column prop="name" label="预检规则项" width="160" />
              <el-table-column label="检查结果" width="100" align="center">
                <template #default="{ row }">
                  <el-tag size="small" :type="row.passed ? 'success' : row.blocking ? 'danger' : 'warning'">
                    {{ row.passed ? '通过' : row.blocking ? '阻断' : '警告' }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="message" label="规则判定与诊断意见" />
            </el-table>
          </div>
        </div>
      </div>

      <template #footer>
        <div class="drawer-footer-actions">
          <el-button v-if="currentStep > 0" @click="currentStep--">上一步</el-button>
          <el-button v-if="currentStep < 3" type="primary" @click="currentStep++">下一步</el-button>
          <el-button
            v-if="currentStep === 3"
            type="info"
            plain
            :loading="saveDraftLoading"
            @click="handleSaveDraft"
          >
            保存草稿(暂存)
          </el-button>
          <el-button
            v-if="currentStep === 3"
            type="success"
            :loading="submitLoading"
            @click="handleSubmit"
          >
            正式提交申报
          </el-button>
        </div>
      </template>
    </el-drawer>

    <!-- 评审汇总与专家意见抽屉 -->
    <el-drawer v-model="reviewDrawerVisible" title="纵向项目评审汇总与版本追踪" size="580px">
      <div v-if="currentProject" class="review-panel">
        <div class="project-header-badge">
          <h3>{{ currentProject.项目名称 || currentProject.名称 }}</h3>
          <div class="meta-row">
            <span>申报人：{{ currentProject.姓名 || currentProject.申报人姓名 || '管理员' }}</span>
            <span class="dot-sep">·</span>
            <span>申请经费：¥{{ formatMoney(currentProject.申请经费) }}</span>
            <span class="dot-sep">·</span>
            <span>流程状态：{{ currentProject.状态名称 || '审查中' }}</span>
          </div>
        </div>

        <!-- 评审意见时间线 -->
        <div class="section-title" style="margin-top: 18px;">
          <span>同行评审与形审意见记录</span>
          <el-button type="primary" link size="small" :icon="Plus" @click="openAddOpinionDialog">
            录入审查意见
          </el-button>
        </div>
        <div v-if="reviewOpinions.length > 0" class="timeline-container">
          <el-timeline>
            <el-timeline-item
              v-for="op in reviewOpinions"
              :key="op.编号"
              :timestamp="op.建立时间 || '2026-10-15'"
              placement="top"
            >
              <el-card shadow="hover" size="small" class="opinion-card">
                <div class="op-header">
                  <el-tag size="small" type="primary">{{ op.意见类型 }}</el-tag>
                  <span class="reviewer-tag">评审人：{{ op.评审人姓名 || '学术专家/科技处' }}</span>
                </div>
                <div class="op-content">{{ op.意见内容 }}</div>
              </el-card>
            </el-timeline-item>
          </el-timeline>
        </div>
        <el-empty v-else description="暂无评审意见记录，点击上方按钮即可录入审查结论" :image-size="60" />

        <!-- 版本快照记录 -->
        <div class="section-title" style="margin-top: 24px;">
          <span>历史申报版本快照</span>
        </div>
        <el-table :data="versionSnapshots" size="small" border stripe style="width: 100%;">
          <el-table-column prop="版本号" label="版本" width="70" align="center">
            <template #default="{ row }">
              <el-tag size="small" type="success">v{{ row.版本号 }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="变更类型" label="变更类型" width="90" align="center" />
          <el-table-column prop="建立时间" label="建立时间" width="160" />
          <el-table-column label="操作" align="center">
            <template #default="{ row }">
              <el-button type="primary" link size="small" @click="compareWithCurrent(row.版本号)">
                差异对比
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-drawer>

    <!-- 版本差异对比对话框 -->
    <el-dialog v-model="diffDialogVisible" title="纵向项目申报快照版本差异对比" width="750px">
      <div v-if="diffData" class="diff-dialog-content">
        <div class="diff-header-bar">
          <div>项目编号：#{{ diffData.项目编号 }}</div>
          <div>对比基线：<el-tag size="small">v{{ diffData.左版本号 }}</el-tag> 对比目标：<el-tag size="small" type="success">v{{ diffData.右版本号 }}</el-tag></div>
          <div>发现差异：<span style="color: #ef4444; font-weight: 700;">{{ diffData.变化数量 }} 项变更</span></div>
        </div>

        <el-table :data="diffData.变化项" size="small" border stripe style="width: 100%; margin-top: 14px;">
          <el-table-column prop="字段" label="修改字段" width="140" />
          <el-table-column label="原版本值 (v{{ diffData.左版本号 }})" min-width="220">
            <template #default="{ row }">
              <span class="diff-old-value">{{ row.左值 || '(空)' }}</span>
            </template>
          </el-table-column>
          <el-table-column label="新版本值 (v{{ diffData.右版本号 }})" min-width="220">
            <template #default="{ row }">
              <span class="diff-new-value">{{ row.右值 || '(空)' }}</span>
            </template>
          </el-table-column>
        </el-table>
      </div>
      <div v-else class="diff-loading">正在计算版本快照差异...</div>
    </el-dialog>

    <!-- 录入审查意见对话框 -->
    <el-dialog v-model="opinionDialogVisible" title="录入专家/科技处审查意见" width="500px">
      <el-form :model="opinionForm" label-width="90px">
        <el-form-item label="意见类型" required>
          <el-select v-model="opinionForm.意见类型" style="width: 100%;">
            <el-option label="科技处形审意见" value="科技处审查意见" />
            <el-option label="同行专家评议" value="同行专家评审" />
            <el-option label="学术委员会推荐" value="学术委员会审定" />
            <el-option label="伦理专项核查" value="伦理审查意见" />
          </el-select>
        </el-form-item>
        <el-form-item label="审查结论" required>
          <el-input
            v-model="opinionForm.意见内容"
            type="textarea"
            rows="4"
            placeholder="填写评审打分建议、技术可行性论证及审查结论"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="opinionDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="opinionSubmitting" @click="submitOpinion">保存意见</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import {
  getVerticalApplications,
  preCheckApplication,
  submitVerticalApplication,
  getApplicationReviewSummary,
  compareApplicationVersions,
  addReviewOpinion,
  getInternalProjectBatches,
  saveInternalProjectBatch,
  publishInternalProjectBatch,
  type VerticalApplicationItem,
  type InternalProjectBatch,
  type PreCheckResult,
  type VersionDiffResult,
  type ReviewOpinionItem
} from '@/api/project'
import { ElMessage } from 'element-plus'
import {
  Search,
  Plus,
  Refresh,
  Check,
  Collection,
  Money,
  TrendCharts,
  User
} from '@element-plus/icons-vue'

const loading = ref(false)
const submitLoading = ref(false)
const saveDraftLoading = ref(false)
const precheckLoading = ref(false)
const opinionSubmitting = ref(false)
const batchLoading = ref(false)
const batchSaving = ref(false)

const applicationList = ref<VerticalApplicationItem[]>([])
const totalCount = ref(0)
const selectedLevel = ref('')
const route = useRoute()
const batchRows = ref<InternalProjectBatch[]>([])
const batchDialogVisible = ref(false)
const batchForm = reactive<any>({
  编号: undefined,
  计划年度: new Date().getFullYear(),
  批次名称: '',
  项目类别: '院级科研项目',
  发布开始日期: `${new Date().getFullYear()}-01-01`,
  截止日期: `${new Date().getFullYear()}-12-31`,
  个人申报限额: 3,
  批次总限额: undefined,
  允许部门编号文本: '',
  评审开始日期: '',
  评审截止日期: '',
  评审方案文本: '{"方式":"匿名评审","专家数":3,"评分项":["创新性","可行性"]}',
  批次说明: ''
})

const paging = reactive({
  index: 1,
  size: 20
})

const queryFilter = reactive({
  Like名称: '',
  Like申报人姓名: ''
})

const drawerVisible = ref(false)
const reviewDrawerVisible = ref(false)
const diffDialogVisible = ref(false)
const opinionDialogVisible = ref(false)

const currentStep = ref(0)
const currentProject = ref<VerticalApplicationItem | null>(null)
const preCheckData = ref<PreCheckResult | null>(null)
const diffData = ref<VersionDiffResult | null>(null)

const reviewOpinions = ref<ReviewOpinionItem[]>([])
const versionSnapshots = ref<any[]>([])

const appForm = reactive({
  名称: '肺腺癌脑转移靶向耐药微环境机制与联合治疗新策略',
  学科门类: '医学',
  项目级别: '国家级重点',
  申请经费: 650000,
  申报年度: 2026,
  dateRange: ['2026-01-01', '2028-12-31'],
  摘要: '针对晚期肺腺癌脑转移微环境中免疫抑制与靶向耐药难题，本项目拟采用单细胞转录组与空间多组学联用技术，解析耐药克隆演变规律，为临床联合用药提供精准分子分型和靶向干预方案。',
  是否委托外单位进行实验或实验分析: false,
  实验委托检测内容: '',
  伦理要求: false,
  是否需要伦理批文: false,
  伦理批文编号: '',
  伦理批文路径: '',
  项目申报书路径: '/upload/纵向项目/项目申报书/application.pdf',
  院级批次编号: undefined as number | undefined
})

const isPlanManager = computed(() => {
  const userInfo = sessionStorage.getItem('userInfo') || ''
  return userInfo.includes('系统管理员') || userInfo.includes('科研管理员') || userInfo.includes('科技办')
})

const budgetBreakdown = reactive({
  直接费: 45,
  设备费: 10,
  间接费: 10
})

const participants = ref([
  { 参与人编号: 6, 工作量占比: 60, 备注: '课题主持与全面统筹' },
  { 参与人编号: 2, 工作量占比: 40, 备注: '负责单细胞生信分析与队列样本管理' }
])

const opinionForm = reactive({
  意见类型: '科技处审查意见',
  意见内容: ''
})

// 归一化预检查结果
const normalizedCheckItems = computed(() => {
  if (!preCheckData.value) return []
  if (Array.isArray(preCheckData.value.检查项)) {
    return preCheckData.value.检查项.map(item => ({
      name: item.名称 || item.代码 || '规则检查',
      passed: Boolean(item.通过),
      blocking: Boolean(item.阻断提交),
      message: item.提示 || (item.通过 ? '合规通过' : '未达到提交要求')
    }))
  }
  if (Array.isArray(preCheckData.value.检查项明细)) {
    return preCheckData.value.检查项明细.map(item => ({
      name: item.检查项 || '规则检查',
      passed: item.状态 === '通过',
      blocking: item.状态 === '阻断',
      message: item.提示信息 || '检查完成'
    }))
  }
  return []
})

// 过滤列表
const filteredList = computed(() => {
  if (!selectedLevel.value) return applicationList.value
  return applicationList.value.filter(item => (item.项目级别 || '').includes(selectedLevel.value))
})

// KPI 统计
const statsData = computed(() => {
  const total = applicationList.value.length
  const totalBudget = applicationList.value.reduce((acc, curr) => acc + (Number(curr.申请经费) || 0), 0)
  const keyProjectCount = applicationList.value.filter(item => (item.项目级别 || '').includes('重点') || (item.项目级别 || '').includes('重大')).length
  const recommendedCount = applicationList.value.filter(item =>
    (item.状态名称 || item.项目状态 || '').includes('推荐') ||
    (item.状态名称 || item.项目状态 || '').includes('通过') ||
    item.是否立项 === true
  ).length

  return {
    totalCount: total,
    totalBudget,
    keyProjectCount,
    recommendedCount
  }
})

function formatMoney(num?: number) {
  if (num == null) return '0.00'
  return Number(num).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatWan(num: number) {
  return (num / 10000).toFixed(1)
}

function formatDate(dateStr?: string) {
  if (!dateStr) return '-'
  return dateStr.substring(0, 10)
}

function getLevelTag(level?: string): '' | 'success' | 'warning' | 'info' | 'danger' {
  if (level?.includes('国家')) return 'danger'
  if (level?.includes('省部')) return 'warning'
  return 'info'
}

function getStatusTag(status?: string): '' | 'success' | 'warning' | 'info' | 'danger' {
  if (!status) return 'info'
  if (status.includes('立项') || status.includes('通过') || status.includes('推荐')) return 'success'
  if (status.includes('未完成') || status.includes('审核') || status.includes('评审')) return 'warning'
  if (status.includes('拒绝') || status.includes('未获')) return 'danger'
  return 'info'
}

function addParticipant() {
  participants.value.push({
    参与人编号: 3,
    工作量占比: 10,
    备注: '实验协助与数据整理'
  })
}

function handleSearch() {
  paging.index = 1
  fetchApplications()
}

function resetSearch() {
  queryFilter.Like名称 = ''
  queryFilter.Like申报人姓名 = ''
  selectedLevel.value = ''
  paging.index = 1
  fetchApplications()
}

async function fetchApplications() {
  loading.value = true
  try {
    const res = await getVerticalApplications(paging, queryFilter)
    applicationList.value = Array.isArray(res?.list) ? res.list : []
    totalCount.value = Number(res?.total) || applicationList.value.length
  } catch (err: any) {
    applicationList.value = []
    totalCount.value = 0
    ElMessage.error(err.response?.data?.error || err.message || '申报项目查询失败，请稍后重试')
  } finally {
    loading.value = false
  }
}

async function fetchBatches() {
  batchLoading.value = true
  try {
    const res = await getInternalProjectBatches({ Index: 1, Size: 100 })
    batchRows.value = Array.isArray(res?.list) ? res.list : []
  } catch {
    batchRows.value = []
  } finally {
    batchLoading.value = false
  }
}

function resetBatchForm() {
  const year = new Date().getFullYear()
  Object.assign(batchForm, {
    编号: undefined, 计划年度: year, 批次名称: '', 项目类别: '院级科研项目',
    发布开始日期: `${year}-01-01`, 截止日期: `${year}-12-31`, 个人申报限额: 3,
    批次总限额: undefined, 允许部门编号文本: '', 评审开始日期: '', 评审截止日期: '',
    评审方案文本: '{"方式":"匿名评审","专家数":3,"评分项":["创新性","可行性"]}', 批次说明: ''
  })
}
function openBatchDialog() { resetBatchForm(); batchDialogVisible.value = true }
function editBatch(row: InternalProjectBatch) {
  const qualification = typeof row.资格规则 === 'string' ? {} : (row.资格规则 || {})
  Object.assign(batchForm, row, {
    允许部门编号文本: Array.isArray((qualification as any).允许部门编号) ? (qualification as any).允许部门编号.join(',') : '',
    评审方案文本: typeof row.评审方案 === 'string' ? row.评审方案 : JSON.stringify(row.评审方案 || {}, null, 2)
  })
  batchDialogVisible.value = true
}
async function saveBatch() {
  if (!batchForm.批次名称?.trim()) { ElMessage.warning('请填写批次名称'); return }
  let review: any
  try { review = JSON.parse(batchForm.评审方案文本 || '{}') } catch { ElMessage.error('评审方案必须是合法 JSON'); return }
  const departments = String(batchForm.允许部门编号文本 || '').split(',').map((x: string) => Number(x.trim())).filter((x: number) => x > 0)
  batchSaving.value = true
  try {
    await saveInternalProjectBatch({ ...batchForm, 资格规则: departments.length ? { 允许部门编号: departments } : {}, 评审方案: review })
    ElMessage.success('院级项目批次已保存')
    batchDialogVisible.value = false
    await fetchBatches()
  } catch (err: any) { ElMessage.error(err.response?.data?.error || err.message || '批次保存失败') } finally { batchSaving.value = false }
}
async function publishBatch(row: InternalProjectBatch) { try { await publishInternalProjectBatch(row.编号, '发布'); ElMessage.success('批次已发布'); await fetchBatches() } catch (err: any) { ElMessage.error(err.response?.data?.error || '批次发布失败') } }
async function closeBatch(row: InternalProjectBatch) { try { await publishInternalProjectBatch(row.编号, '关闭'); ElMessage.success('批次已关闭'); await fetchBatches() } catch (err: any) { ElMessage.error(err.response?.data?.error || '批次关闭失败') } }

function openApplicationDrawer() {
  currentStep.value = 0
  preCheckData.value = null
  drawerVisible.value = true
}

async function runPreCheck() {
  precheckLoading.value = true
  try {
    const payload = {
      ...appForm,
      参与人列表: participants.value
    }
    const res = await preCheckApplication(payload)
    if (res) {
      preCheckData.value = res
    }
  } catch (err: any) {
    preCheckData.value = null
    ElMessage.error(err.response?.data?.error || err.message || '申报预检查失败，无法确认是否满足提交条件')
  } finally {
    precheckLoading.value = false
  }
}

async function handleSaveDraft() {
  saveDraftLoading.value = true
  try {
    const payload = {
      ...appForm,
      参与人列表: participants.value
    }
    await submitVerticalApplication(payload, true)
    ElMessage.success('申报草稿已成功暂存！')
    drawerVisible.value = false
    await fetchApplications()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '暂存失败')
  } finally {
    saveDraftLoading.value = false
  }
}

async function handleSubmit() {
  submitLoading.value = true
  try {
    const payload = {
      ...appForm,
      参与人列表: participants.value
    }
    await submitVerticalApplication(payload, false)
    ElMessage.success('项目申报正式提交成功，已自动流转至科研管理员审核！')
    drawerVisible.value = false
    await fetchApplications()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '提交失败')
  } finally {
    submitLoading.value = false
  }
}

async function viewReviews(row: VerticalApplicationItem) {
  currentProject.value = row
  reviewOpinions.value = []
  versionSnapshots.value = []
  reviewDrawerVisible.value = true

  try {
    const res = await getApplicationReviewSummary(row.编号)
    if (res) {
      reviewOpinions.value = res.评审意见 || []
      versionSnapshots.value = res.版本 || []
    }
  } catch (err: any) {
    reviewOpinions.value = []
    versionSnapshots.value = []
    ElMessage.error(err.response?.data?.error || err.message || '评审记录查询失败')
  }
}

async function viewVersionDiff(row: VerticalApplicationItem) {
  currentProject.value = row
  diffData.value = null
  diffDialogVisible.value = true

  try {
    const res = await compareApplicationVersions(row.编号, 1, row.版本号 || 2)
    if (res && res.变化项) {
      diffData.value = res
    } else {
      diffData.value = null
      ElMessage.info('该项目暂无可比较的版本差异')
    }
  } catch (err: any) {
    diffData.value = null
    ElMessage.error(err.response?.data?.error || err.message || '版本差异查询失败')
  }
}

async function compareWithCurrent(versionNo: number) {
  if (!currentProject.value) return
  if (versionNo <= 1) {
    ElMessage.info('当前已是最初申报版本')
    return
  }
  diffData.value = null
  diffDialogVisible.value = true
  try {
    const res = await compareApplicationVersions(currentProject.value.编号, versionNo - 1, versionNo)
    diffData.value = res
  } catch (err: any) {
    diffData.value = null
    ElMessage.error(err.response?.data?.error || err.message || '版本差异查询失败')
  }
}

function openPreCheck(row: VerticalApplicationItem) {
  currentProject.value = row
  openApplicationDrawer()
  currentStep.value = 3
  appForm.名称 = row.项目名称 || row.名称 || ''
  appForm.申请经费 = row.申请经费 || 0
  runPreCheck()
}

function openAddOpinionDialog() {
  opinionForm.意见内容 = ''
  opinionDialogVisible.value = true
}

async function submitOpinion() {
  if (!currentProject.value) return
  if (!opinionForm.意见内容.trim()) {
    ElMessage.warning('请填写审查结论内容')
    return
  }
  opinionSubmitting.value = true
  try {
    await addReviewOpinion({
      流程模板编号: 400,
      项目编号: currentProject.value.编号,
      意见类型: opinionForm.意见类型,
      意见内容: opinionForm.意见内容.trim()
    })
    ElMessage.success('审查意见录入成功！')
    opinionDialogVisible.value = false
    await viewReviews(currentProject.value)
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '录入失败')
  } finally {
    opinionSubmitting.value = false
  }
}

onMounted(async () => {
  await Promise.all([fetchApplications(), fetchBatches()])
})
</script>

<style scoped lang="scss">
.application-page {
  padding: 16px;
  background-color: #f1f5f9;
  min-height: calc(100vh - 60px);

  .stats-overview {
    margin-bottom: 16px;

    .stat-card {
      border-radius: 8px;
      border: 1px solid #e2e8f0;
      transition: all 0.2s ease;

      &:hover {
        transform: translateY(-2px);
      }

      .stat-content {
        display: flex;
        align-items: center;

        .stat-icon {
          width: 48px;
          height: 48px;
          border-radius: 10px;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 24px;
          color: #ffffff;
          margin-right: 14px;
          flex-shrink: 0;

          &.bg-blue { background: linear-gradient(135deg, #3b82f6, #1d4ed8); }
          &.bg-green { background: linear-gradient(135deg, #10b981, #047857); }
          &.bg-purple { background: linear-gradient(135deg, #8b5cf6, #6d28d9); }
          &.bg-amber { background: linear-gradient(135deg, #f59e0b, #d97706); }
        }

        .stat-meta {
          .stat-label {
            font-size: 13px;
            color: #64748b;
          }
          .stat-value {
            font-size: 22px;
            font-weight: 700;
            line-height: 1.2;
            margin-top: 4px;

            &.text-blue { color: #2563eb; }
            &.text-green { color: #059669; }
            &.text-purple { color: #7c3aed; }
            &.text-amber { color: #d97706; }

            .stat-unit {
              font-size: 13px;
              font-weight: 500;
              color: #94a3b8;
              margin-left: 2px;
            }
          }
        }
      }
    }
  }

  .main-table-card {
    border-radius: 8px;
    border: 1px solid #e2e8f0;
    background-color: #ffffff;

    .toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;

      .filter-form {
        margin-bottom: 0;
      }
    }

    .project-title-cell {
      .project-name {
        font-weight: 600;
        color: #1e293b;
        font-size: 14px;
        line-height: 1.4;
      }
      .project-sub-meta {
        margin-top: 6px;
        display: flex;
        gap: 6px;
        flex-wrap: wrap;
      }
    }

    .applicant-cell {
      .applicant-name {
        font-size: 13px;
        font-weight: 500;
        color: #334155;
      }
      .applicant-dept {
        font-size: 12px;
        color: #94a3b8;
        margin-top: 2px;
      }
    }

    .budget-amount {
      color: #059669;
      font-weight: 700;
      font-size: 14px;
    }

    .date-range-text {
      font-size: 12px;
      color: #64748b;
      .date-separator {
        color: #cbd5e1;
        margin: 0 4px;
      }
    }

    .step-desc-text {
      font-size: 11px;
      color: #94a3b8;
      margin-top: 3px;
    }

    .pagination-wrapper {
      margin-top: 16px;
      display: flex;
      justify-content: flex-end;
    }
  }

  .drawer-inner {
    .step-header {
      margin-bottom: 20px;
    }

    .step-body {
      padding: 4px 0;

      .section-title {
        font-size: 14px;
        font-weight: 600;
        color: #1e293b;
        margin-bottom: 12px;
        display: flex;
        justify-content: space-between;
        align-items: center;
      }
    }

    .precheck-action-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      background-color: #f8fafc;
      padding: 16px;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
      margin-bottom: 16px;
    }

    .precheck-results-box {
      margin-top: 12px;
    }
  }

  .drawer-footer-actions {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
  }

  .review-panel {
    .project-header-badge {
      padding: 14px 16px;
      background: linear-gradient(135deg, #f8fafc, #edf2f7);
      border-radius: 8px;
      border: 1px solid #e2e8f0;
      margin-bottom: 16px;

      h3 {
        margin: 0 0 6px 0;
        font-size: 16px;
        color: #0f172a;
      }
      .meta-row {
        font-size: 13px;
        color: #64748b;
        .dot-sep {
          margin: 0 6px;
          color: #cbd5e1;
        }
      }
    }

    .section-title {
      font-size: 14px;
      font-weight: 600;
      color: #1e293b;
      margin-bottom: 12px;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .opinion-card {
      background-color: #f8fafc;
      border-radius: 6px;
      .op-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 6px;
        .reviewer-tag {
          font-size: 12px;
          color: #64748b;
        }
      }
      .op-content {
        font-size: 13px;
        color: #334155;
        line-height: 1.5;
      }
    }
  }

  .diff-dialog-content {
    .diff-header-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      background-color: #f8fafc;
      padding: 10px 14px;
      border-radius: 6px;
      border: 1px solid #e2e8f0;
      font-size: 13px;
      color: #475569;
    }

    .diff-old-value {
      color: #991b1b;
      background-color: #fee2e2;
      padding: 2px 6px;
      border-radius: 4px;
      font-family: monospace;
      word-break: break-all;
    }
    .diff-new-value {
      color: #065f46;
      background-color: #d1fae5;
      padding: 2px 6px;
      border-radius: 4px;
      font-family: monospace;
      word-break: break-all;
    }
  }
}
</style>
