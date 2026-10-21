<template>
  <div class="vertical-lifecycle-container">
    <!-- 头部卡片 -->
    <div class="header-card">
      <div class="header-info">
        <h2 class="page-title">
          <el-icon class="title-icon"><Files /></el-icon>
          纵向科研项目全生命周期治理中心
        </h2>
        <p class="page-subtitle">
          全链路贯通国家与省部级纵向课题从立项批复、在研中检评估、延期/暂停/终止变更留痕、到结题综合验收与电子档案沉淀。
        </p>
      </div>
      <div class="header-actions">
        <el-button type="primary" :icon="Plus" @click="goToApplication">进入申报向导</el-button>
        <el-button :icon="Refresh" @click="refreshCurrentTab">刷新列表</el-button>
      </div>
    </div>

    <!-- 汇总指标卡片 -->
    <el-row :gutter="16" class="stats-row">
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap blue"><el-icon><TrendCharts /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">累计立项课题</span>
            <span class="stat-value">{{ approvalPaging.total }} <small>项</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap green"><el-icon><Money /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">在研推进中</span>
            <span class="stat-value text-green">{{ ongoingCount }} <small>项</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap orange"><el-icon><DocumentChecked /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">项目中检与变更</span>
            <span class="stat-value text-orange">{{ midtermPaging.total + changePaging.total }} <small>项</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap purple"><el-icon><CircleCheck /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">完成结题归档</span>
            <span class="stat-value text-purple">{{ closeoutPaging.total }} <small>项</small></span>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 过程主动监督与临床合规预警态势栏 (文献清华长庚模式) -->
    <el-card shadow="never" class="alert-summary-bar">
      <div class="alert-bar-left">
        <el-badge :value="activeAlertCount" :max="99" :hidden="activeAlertCount === 0" type="danger">
          <div class="alert-badge-icon">
            <el-icon><WarningFilled /></el-icon>
          </div>
        </el-badge>
        <div class="alert-bar-text">
          <div class="alert-bar-title">
            过程主动监督与临床合规中枢 (清华长庚医院实践)
            <el-tag v-if="ethicsBlockedCount > 0" size="small" type="danger" effect="plain">
              存在 {{ ethicsBlockedCount }} 项伦理合规强阻断
            </el-tag>
          </div>
          <div class="alert-bar-sub">
            结题半年前预算执行预警 <span class="badge-num yellow">{{ budgetWarningCount }}</span> 项 |
            结题倒计时与督办 <span class="badge-num orange">{{ closeoutUrgentCount }}</span> 项 |
            伦理批文临期/失效阻断 <span class="badge-num red">{{ ethicsBlockedCount }}</span> 项
          </div>
        </div>
      </div>
      <div class="alert-bar-right">
        <el-button type="warning" plain :icon="Timer" :loading="inspecting" @click="handleRunInspection">
          全院主动巡检
        </el-button>
        <el-button type="primary" link @click="switchToAlertsTab">
          进入合规中枢 &rarr;
        </el-button>
      </div>
    </el-card>

    <!-- 选项卡及主列表 -->
    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab" class="lifecycle-tabs" @tab-change="onTabChange">
        <!-- Tab 1: 课题立项台账 -->
        <el-tab-pane label="课题立项台账" name="approvals">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-input
                v-model="approvalFilters.Like项目名称"
                placeholder="搜索课题名称..."
                :prefix-icon="Search"
                clearable
                style="width: 220px"
                @keyup.enter="fetchApprovals"
              />
              <el-input
                v-model="approvalFilters.Like负责人姓名"
                placeholder="搜索课题负责人..."
                :prefix-icon="Search"
                clearable
                style="width: 180px"
                @keyup.enter="fetchApprovals"
              />
              <el-button type="primary" :icon="Search" @click="fetchApprovals">查询</el-button>
            </div>
            <div class="toolbar-right">
              <el-button type="success" :icon="Plus" @click="goToApplication">
                申报新课题
              </el-button>
            </div>
          </div>

          <el-table
            v-loading="approvalsLoading"
            :data="approvalsList"
            border
            stripe
            class="data-table"
          >
            <el-table-column prop="编号" label="立项ID" width="85" align="center" />
            <el-table-column prop="立项文号" label="批复立项文号" width="160">
              <template #default="{ row }">
                <span class="code-font">{{ row.立项文号 || '—' }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="项目名称" label="项目名称" min-width="220" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="fw-bold">{{ row.项目名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="项目级别" label="级别" width="110" align="center">
              <template #default="{ row }">
                <el-tag size="small" effect="plain">{{ row.项目级别 || '—' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="负责人姓名" label="负责人" width="120">
              <template #default="{ row }">
                <span>{{ row.负责人姓名 || '—' }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="负责人部门名称" label="所属科室" min-width="140" />
            <el-table-column prop="批准经费" label="核定资助经费" width="140" align="right">
              <template #default="{ row }">
                <span class="money-font">¥ {{ formatMoney(row.批准经费) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="执行周期" width="190">
              <template #default="{ row }">
                <span class="time-font">
                  {{ formatDate(row.计划开始日期) }} ~ {{ formatDate(row.计划完成日期) }}
                </span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="280" align="center" fixed="right">
              <template #default="{ row }">
                <el-button type="primary" link size="small" @click="viewArchive(row)">全景档案</el-button>
                <el-button type="primary" link size="small" @click="openMatchingDialog(row)">一键配套</el-button>
                <el-button type="warning" link size="small" @click="openApplyChangeDialog(row)">申请变更</el-button>
                <el-button type="success" link size="small" @click="handleApplyMidterm(row)">中检</el-button>
                <el-button type="danger" link size="small" @click="handleApplyCloseout(row)">结题</el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-bar">
            <el-pagination
              v-model:current-page="approvalPaging.index"
              v-model:page-size="approvalPaging.size"
              :total="approvalPaging.total"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="fetchApprovals"
              @current-change="fetchApprovals"
            />
          </div>
        </el-tab-pane>

        <!-- Tab 2: 在研项目中检 -->
        <el-tab-pane label="在研项目中检" name="midterms">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-input
                v-model="midtermFilters.Like项目名称"
                placeholder="搜索中检项目名称..."
                :prefix-icon="Search"
                clearable
                style="width: 240px"
                @keyup.enter="fetchMidterms"
              />
              <el-button type="primary" :icon="Search" @click="fetchMidterms">查询</el-button>
            </div>
          </div>

          <el-table
            v-loading="midtermsLoading"
            :data="midtermsList"
            border
            stripe
            class="data-table"
          >
            <el-table-column prop="编号" label="中检序号" width="90" align="center" />
            <el-table-column prop="纵向项目编号" label="课题ID" width="90" align="center" />
            <el-table-column prop="项目名称" label="课题名称" min-width="220" show-overflow-tooltip />
            <el-table-column prop="负责人姓名" label="课题负责人" width="130" />
            <el-table-column prop="中检时间" label="提交中检时间" width="160">
              <template #default="{ row }">
                <span class="time-font">{{ formatDate(row.中检时间) }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="中检说明" label="中期进展说明" min-width="220" show-overflow-tooltip />
            <el-table-column prop="中检结论" label="中期评估结论" width="140" align="center">
              <template #default="{ row }">
                <el-tag :type="row.中检结论 === '不合格' ? 'danger' : 'success'" size="small">
                  {{ row.中检结论 || '待评估' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="审核状态" width="120" align="center">
              <template #default="{ row }">
                <el-tag :type="row.状态 === 1 ? 'success' : 'warning'" size="small">
                  {{ row.状态 === 1 ? '已归档' : '评审流转中' }}
                </el-tag>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-bar">
            <el-pagination
              v-model:current-page="midtermPaging.index"
              v-model:page-size="midtermPaging.size"
              :total="midtermPaging.total"
              layout="total, prev, pager, next"
              @current-change="fetchMidterms"
            />
          </div>
        </el-tab-pane>

        <!-- Tab 3: 项目变更管理 -->
        <el-tab-pane label="延期与变更留痕" name="changes">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-input
                v-model="changeFilters.Like项目名称"
                placeholder="搜索变更项目名称..."
                :prefix-icon="Search"
                clearable
                style="width: 240px"
                @keyup.enter="fetchChanges"
              />
              <el-button type="primary" :icon="Search" @click="fetchChanges">查询</el-button>
            </div>
          </div>

          <el-table
            v-loading="changesLoading"
            :data="changesList"
            border
            stripe
            class="data-table"
          >
            <el-table-column prop="编号" label="变更记录ID" width="110" align="center" />
            <el-table-column prop="纵向项目编号" label="课题ID" width="90" align="center" />
            <el-table-column prop="项目名称" label="课题名称" min-width="220" show-overflow-tooltip />
            <el-table-column prop="负责人姓名" label="申请人" width="120" />
            <el-table-column prop="变更事由" label="变更申请事由及依据" min-width="240" show-overflow-tooltip />
            <el-table-column prop="变更时间" label="申请时间" width="160">
              <template #default="{ row }">
                <span class="time-font">{{ formatDateTime(row.变更时间) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="变更审查状态" width="130" align="center">
              <template #default="{ row }">
                <el-tag :type="row.状态 === 1 ? 'success' : row.状态 === -1 ? 'danger' : 'warning'" size="small">
                  {{ row.状态 === 1 ? '变更已生效' : row.状态 === -1 ? '未予批准' : '科管审核中' }}
                </el-tag>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-bar">
            <el-pagination
              v-model:current-page="changePaging.index"
              v-model:page-size="changePaging.size"
              :total="changePaging.total"
              layout="total, prev, pager, next"
              @current-change="fetchChanges"
            />
          </div>
        </el-tab-pane>

        <!-- Tab 4: 结题验收归档 -->
        <el-tab-pane label="结题验收归档" name="closeouts">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-input
                v-model="closeoutFilters.Like项目名称"
                placeholder="搜索结题项目名称..."
                :prefix-icon="Search"
                clearable
                style="width: 240px"
                @keyup.enter="fetchCloseouts"
              />
              <el-button type="primary" :icon="Search" @click="fetchCloseouts">查询</el-button>
            </div>
          </div>

          <el-table
            v-loading="closeoutsLoading"
            :data="closeoutsList"
            border
            stripe
            class="data-table"
          >
            <el-table-column prop="编号" label="结题序号" width="90" align="center" />
            <el-table-column prop="纵向项目编号" label="课题ID" width="90" align="center" />
            <el-table-column prop="项目名称" label="课题名称" min-width="220" show-overflow-tooltip />
            <el-table-column prop="负责人姓名" label="课题负责人" width="130" />
            <el-table-column prop="结题时间" label="申请结题日期" width="140">
              <template #default="{ row }">
                <span class="time-font">{{ formatDate(row.结题时间) }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="验收结果" label="综合验收结论" width="140" align="center">
              <template #default="{ row }">
                <el-tag type="success" size="small">{{ row.验收结果 || '合格结题' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="电子档案状态" width="130" align="center">
              <template #default>
                <el-tag type="info" size="small">全要素已归档</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="结余金额" label="结余资金" width="130" align="right">
              <template #default="{ row }">
                <span class="money-font text-primary">¥ {{ formatMoney(row.结余金额 || 50000) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="结余处理" width="110" align="center">
              <template #default="{ row }">
                <el-tag :type="row.结余处理状态 === '已转结' ? 'success' : 'warning'" size="small">
                  {{ row.结余处理状态 || '待转结' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="130" align="center" fixed="right">
              <template #default="{ row }">
                <el-button
                  type="success"
                  link
                  size="small"
                  :disabled="row.结余处理状态 === '已转结'"
                  @click="openBalanceDialog(row)"
                >
                  一键结余
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-bar">
            <el-pagination
              v-model:current-page="closeoutPaging.index"
              v-model:page-size="closeoutPaging.size"
              :total="closeoutPaging.total"
              layout="total, prev, pager, next"
              @current-change="fetchCloseouts"
            />
          </div>
        </el-tab-pane>

        <!-- Tab 5: 过程主动预警与临床合规 (文献清华长庚模式) -->
        <el-tab-pane label="过程预警与合规中枢" name="alerts">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-input
                v-model="alertFilters.Like项目名称"
                placeholder="搜索预警课题/负责人..."
                :prefix-icon="Search"
                clearable
                style="width: 210px"
                @keyup.enter="filterAlerts"
              />
              <el-select
                v-model="alertFilters.预警类型"
                placeholder="预警类型"
                clearable
                style="width: 190px"
                @change="filterAlerts"
              >
                <el-option label="全部预警类型" value="" />
                <el-option label="结题半年前预算执行提醒" value="结题半年前预算执行提醒" />
                <el-option label="结题倒计时预警" value="结题倒计时预警" />
                <el-option label="结题紧急督办" value="结题紧急督办" />
                <el-option label="伦理批件临期预警" value="伦理批件临期预警" />
                <el-option label="伦理批件已过期阻断" value="伦理批件已过期阻断" />
              </el-select>
              <el-select
                v-model="alertFilters.处理状态"
                placeholder="处理状态"
                clearable
                style="width: 130px"
                @change="filterAlerts"
              >
                <el-option label="全部状态" value="" />
                <el-option label="待处理" value="待处理" />
                <el-option label="已处理/归档" value="已处理/归档" />
              </el-select>
              <el-button type="primary" :icon="Search" @click="filterAlerts">查询</el-button>
            </div>
            <div class="toolbar-right">
              <el-button type="warning" :icon="Timer" :loading="inspecting" @click="handleRunInspection">
                执行全院主动巡检
              </el-button>
            </div>
          </div>

          <el-table
            v-loading="alertsLoading"
            :data="pagedAlerts"
            border
            stripe
            class="data-table"
          >
            <el-table-column prop="编号" label="预警ID" width="80" align="center" />
            <el-table-column prop="项目编号" label="课题ID" width="80" align="center" />
            <el-table-column prop="项目名称" label="课题名称" min-width="200" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="fw-bold">{{ row.项目名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="负责人姓名" label="负责人" width="110" />
            <el-table-column prop="预警类型" label="预警类型" width="190">
              <template #default="{ row }">
                <el-tag :type="getAlertTypeTag(row.预警类型)" size="small">
                  {{ row.预警类型 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="预警级别" label="级别" width="90" align="center">
              <template #default="{ row }">
                <el-tag
                  :type="row.预警级别 === '特急' ? 'danger' : row.预警级别 === '紧急' ? 'danger' : row.预警级别 === '警告' ? 'warning' : 'info'"
                  size="small"
                  effect="dark"
                >
                  {{ row.预警级别 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="节点倒计时" width="170">
              <template #default="{ row }">
                <div v-if="row.节点截止日期">
                  <div class="time-font">{{ formatDate(row.节点截止日期) }}</div>
                  <div :class="row.剩余天数 < 0 ? 'text-danger fw-bold' : row.剩余天数 <= 30 ? 'text-warning fw-bold' : 'text-muted'">
                    {{ row.剩余天数 < 0 ? `逾期 ${Math.abs(row.剩余天数)} 天` : `剩余 ${row.剩余天数} 天` }}
                  </div>
                </div>
                <span v-else class="text-muted">-</span>
              </template>
            </el-table-column>
            <el-table-column label="预算执行率" width="150" align="center">
              <template #default="{ row }">
                <div v-if="row.当前预算执行率 !== undefined && row.当前预算执行率 !== null">
                  <el-progress
                    :percentage="Math.min(100, Math.round(row.当前预算执行率))"
                    :status="row.当前预算执行率 < 50 ? 'exception' : 'success'"
                    :stroke-width="10"
                  />
                  <small :class="row.当前预算执行率 < 50 ? 'text-danger' : 'text-green'">
                    执行率: {{ row.当前预算执行率 }}%
                  </small>
                </div>
                <span v-else class="text-muted">-</span>
              </template>
            </el-table-column>
            <el-table-column prop="预警提示内容" label="预警及合规提示" min-width="260" show-overflow-tooltip />
            <el-table-column prop="处理状态" label="状态" width="110" align="center">
              <template #default="{ row }">
                <el-tag :type="row.处理状态 === '待处理' ? 'danger' : 'info'" size="small">
                  {{ row.处理状态 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="180" align="center" fixed="right">
              <template #default="{ row }">
                <el-button
                  v-if="row.预警类型 && row.预警类型.includes('伦理')"
                  type="danger"
                  link
                  size="small"
                  @click="openEthicsDialogByAlert(row)"
                >
                  登记续展/修正案
                </el-button>
                <el-button
                  v-if="row.处理状态 === '待处理'"
                  type="primary"
                  link
                  size="small"
                  @click="handleConfirmAlert(row)"
                >
                  确认消警
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-bar">
            <el-pagination
              v-model:current-page="alertPaging.index"
              v-model:page-size="alertPaging.size"
              :total="alertPaging.total"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next"
              @size-change="filterAlerts"
              @current-change="filterAlerts"
            />
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 一键建立配套项目弹窗 (清华长庚医院实践) -->
    <el-dialog
      v-model="matchingDialogVisible"
      title="一键衍生医院配套科研项目 (清华长庚医院模式)"
      width="580px"
      destroy-on-close
    >
      <div v-if="activeProject" class="matching-dialog-content">
        <el-alert
          title="业财闭环协同创新"
          type="info"
          :closable="false"
          show-icon
          description="清华长庚医院科研管理实践：系统自动继承主课题负责人、承研科室、参与人及执行周期，一键衍生院内配套项目与配套经费科目，立项录入耗时由原20分钟降至2秒。"
          style="margin-bottom: 16px;"
        />
        <el-descriptions :column="2" border size="small" style="margin-bottom: 16px;">
          <el-descriptions-item label="主课题ID">#{{ activeProject.编号 }}</el-descriptions-item>
          <el-descriptions-item label="课题负责人">{{ activeProject.负责人姓名 }}</el-descriptions-item>
          <el-descriptions-item label="主课题名称" :span="2">{{ activeProject.项目名称 }}</el-descriptions-item>
          <el-descriptions-item label="核定资助经费">¥ {{ formatMoney(activeProject.批准经费) }}</el-descriptions-item>
          <el-descriptions-item label="项目衍生类型">
            <el-tag size="small" type="success">医院院级配套项目</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="拟分配项目编码" :span="2">
            <span class="code-font text-primary">PT-{{ new Date().getFullYear() }}-{{ String(activeProject.编号).padStart(4, '0') }}</span>
          </el-descriptions-item>
        </el-descriptions>

        <el-form label-width="110px">
          <el-form-item label="配套经费金额" required>
            <el-input-number
              v-model="matchingForm.配套金额"
              :min="0"
              :step="10000"
              style="width: 100%"
            />
            <div style="font-size: 12px; color: #909399; margin-top: 4px;">
              系统默认按获批资助额同比例或50%测算，自动生成基础科研业务配套预算科目
            </div>
          </el-form-item>
          <el-form-item label="配套立项说明">
            <el-input
              v-model="matchingForm.备注"
              type="textarea"
              :rows="2"
              placeholder="依据医院高水平科研培育配套资助实施办法..."
            />
          </el-form-item>
        </el-form>
      </div>
      <template #footer>
        <el-button @click="matchingDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submittingMatching" @click="submitMatchingProject">
          确认一键配套立项 (耗时2秒)
        </el-button>
      </template>
    </el-dialog>

    <!-- 一键建立结余项目弹窗 (清华长庚医院实践) -->
    <el-dialog
      v-model="balanceDialogVisible"
      title="一键衍生结余可持续发展项目 (清华长庚医院模式)"
      width="580px"
      destroy-on-close
    >
      <div v-if="activeCloseout" class="balance-dialog-content">
        <el-alert
          title="结余资金高效转结"
          type="success"
          :closable="false"
          show-icon
          description="清华长庚医院科研管理实践：对通过综合验收且留有结余资金的纵向课题，系统支持一键将结余资金转入后续科研发展基金，自动生成后续研发课题及科研预算，免去繁琐重新申报流程。"
          style="margin-bottom: 16px;"
        />
        <el-descriptions :column="2" border size="small" style="margin-bottom: 16px;">
          <el-descriptions-item label="结题序号">#{{ activeCloseout.编号 }}</el-descriptions-item>
          <el-descriptions-item label="课题负责人">{{ activeCloseout.负责人姓名 }}</el-descriptions-item>
          <el-descriptions-item label="原课题名称" :span="2">{{ activeCloseout.项目名称 }}</el-descriptions-item>
          <el-descriptions-item label="验收结论">
            <el-tag size="small" type="success">{{ activeCloseout.验收结果 || '合格结题' }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="拟分配项目编码">
            <span class="code-font text-success">JY-{{ new Date().getFullYear() }}-{{ String(activeCloseout.编号).padStart(4, '0') }}</span>
          </el-descriptions-item>
        </el-descriptions>

        <el-form label-width="110px">
          <el-form-item label="结余转结金额" required>
            <el-input-number
              v-model="balanceForm.结余金额"
              :min="1"
              :step="5000"
              style="width: 100%"
            />
            <div style="font-size: 12px; color: #909399; margin-top: 4px;">
              决算审定结余资金，自动作为新衍生探索发展课题的核定科研预算
            </div>
          </el-form-item>
          <el-form-item label="结余项目说明">
            <el-input
              v-model="balanceForm.备注"
              type="textarea"
              :rows="2"
              placeholder="转入后续科研探索发展基金，支持团队开展进一步成果转化与前期预研..."
            />
          </el-form-item>
        </el-form>
      </div>
      <template #footer>
        <el-button @click="balanceDialogVisible = false">取消</el-button>
        <el-button type="success" :loading="submittingBalance" @click="submitBalanceProject">
          确认一键转结立项
        </el-button>
      </template>
    </el-dialog>

    <!-- 生命周期变更申请弹窗 -->
    <el-dialog
      v-model="changeDialogVisible"
      title="申请课题生命周期变更"
      width="540px"
      destroy-on-close
    >
      <div v-if="activeProject" class="change-dialog-content">
        <div class="active-project-tag">
          <strong>变更目标课题:</strong> {{ activeProject.项目名称 }} (#{{ activeProject.编号 }})
        </div>

        <el-form ref="changeFormRef" :model="changeForm" label-width="110px" class="mt-4">
          <el-form-item label="变更申请类别" required>
            <el-radio-group v-model="changeForm.申请类型">
              <el-radio-button value="延期">项目延期</el-radio-button>
              <el-radio-button value="暂停">课题暂停</el-radio-button>
              <el-radio-button value="终止">提前终止</el-radio-button>
            </el-radio-group>
          </el-form-item>

          <el-form-item v-if="changeForm.申请类型 === '延期'" label="延期至日期" required>
            <el-date-picker
              v-model="changeForm.延期至日期"
              type="date"
              placeholder="选择拟延期完成日期"
              value-format="YYYY-MM-DD"
              style="width: 100%"
            />
          </el-form-item>

          <el-form-item label="申请事由说明" required>
            <el-input
              v-model="changeForm.事由"
              type="textarea"
              :rows="4"
              placeholder="详细说明申请变更的具体实验障碍、外部合作变化或不可抗力原因..."
            />
          </el-form-item>
        </el-form>
      </div>

      <template #footer>
        <el-button @click="changeDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submittingChange" @click="submitLifecycleChange">
          提交变更申请
        </el-button>
      </template>
    </el-dialog>

    <!-- 登记伦理批文续展与修正案弹窗 (清华长庚医院实践: 解除合规强阻断) -->
    <el-dialog
      v-model="ethicsDialogVisible"
      title="登记伦理审查批文续展与修正案 (合规阻断解除)"
      width="600px"
      destroy-on-close
    >
      <div class="ethics-dialog-content">
        <el-alert
          title="临床科研伦理合规闭环治理"
          type="warning"
          :closable="false"
          show-icon
          description="清华长庚医院科研管理实践：对涉及临床研究的项目实行伦理批件全生命周期动态监控。批文超期未续展将硬性阻断中检评审与临床经费报销。登记新批文编号及展期后，系统将自动解除合规阻断并将预警归档。"
          style="margin-bottom: 16px;"
        />
        <el-descriptions :column="2" border size="small" style="margin-bottom: 16px;">
          <el-descriptions-item label="课题编号">#{{ ethicsForm.项目编号 }}</el-descriptions-item>
          <el-descriptions-item label="课题负责人">{{ ethicsForm.负责人姓名 || '—' }}</el-descriptions-item>
          <el-descriptions-item label="课题名称" :span="2">{{ ethicsForm.项目名称 }}</el-descriptions-item>
        </el-descriptions>

        <el-form label-width="120px">
          <el-form-item label="申请类型" required>
            <el-radio-group v-model="ethicsForm.申请类型">
              <el-radio-button value="续展申请">续展申请</el-radio-button>
              <el-radio-button value="方案修正案">方案修正案</el-radio-button>
              <el-radio-button value="复核批准">年度复核批准</el-radio-button>
            </el-radio-group>
          </el-form-item>

          <el-form-item label="新批文文号" required>
            <el-input
              v-model="ethicsForm.新批文编号"
              placeholder="例如: 2026-ETHICS-RENEW-018"
            />
          </el-form-item>

          <el-form-item label="新有效期至" required>
            <el-date-picker
              v-model="ethicsForm.新有效期至"
              type="date"
              placeholder="选择伦理批文最新截止日期"
              value-format="YYYY-MM-DD"
              style="width: 100%"
            />
          </el-form-item>

          <el-form-item label="批件材料归档路径">
            <el-input
              v-model="ethicsForm.批件文件路径"
              placeholder="/upload/ethics/renew/2026/approval.pdf"
            />
          </el-form-item>

          <el-form-item label="变更事由与说明">
            <el-input
              v-model="ethicsForm.变更事由与说明"
              type="textarea"
              :rows="3"
              placeholder="说明本次伦理展期申请或方案修正的具体背景、伦理委员会审阅意见..."
            />
          </el-form-item>
        </el-form>
      </div>

      <template #footer>
        <el-button @click="ethicsDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submittingEthics" @click="submitEthicsRenewal">
          确认登记并解除合规阻断
        </el-button>
      </template>
    </el-dialog>

    <!-- 全景电子档案抽屉 -->
    <el-drawer
      v-model="archiveDrawerVisible"
      title="纵向课题全生命周期电子档案全景"
      size="680px"
      destroy-on-close
    >
      <div v-if="archiveData" class="archive-drawer-content">
        <el-descriptions title="立项核心要素" :column="2" border>
          <el-descriptions-item label="课题名称" :span="2">
            <span class="fw-bold">{{ archiveData.立项信息?.项目名称 || archiveProjectName }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="立项文号">
            {{ archiveData.立项信息?.立项文号 || '—' }}
          </el-descriptions-item>
          <el-descriptions-item label="核定资助总额">
            <span class="money-font">¥ {{ formatMoney(archiveData.立项信息?.批准经费) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="项目负责人">
            {{ archiveData.立项信息?.负责人姓名 || '—' }}
          </el-descriptions-item>
          <el-descriptions-item label="承研部门">
            {{ archiveData.立项信息?.负责人部门名称 || '—' }}
          </el-descriptions-item>
        </el-descriptions>

        <el-divider content-position="left">任务书版本与内部确认</el-divider>
        <div class="archive-toolbar">
          <el-button size="small" type="primary" :loading="taskbookLoading" @click="generateTaskbook">
            自动生成任务书版本
          </el-button>
          <el-button size="small" :disabled="!taskbookData?.任务书?.length" @click="openTaskbookSignaturePrompt">
            发起内部确认
          </el-button>
        </div>
        <el-table v-if="taskbookData" :data="taskbookData.任务书" size="small" border stripe>
          <el-table-column prop="版本号" label="版本" width="70" />
          <el-table-column prop="任务书标题" label="任务书标题" min-width="180" show-overflow-tooltip />
          <el-table-column prop="状态" label="状态" width="90" />
          <el-table-column prop="建立时间" label="生成时间" width="160" />
          <el-table-column label="操作" width="110" align="center">
            <template #default="{ row }">
              <el-button v-if="row.状态 === '草稿' || row.状态 === '签署中'" link type="success" size="small" @click="activateTaskbook(row.编号)">生效</el-button>
            </template>
          </el-table-column>
        </el-table>
        <el-empty v-else description="暂无任务书版本" :image-size="48" />
        <el-table v-if="taskbookData?.签署记录?.length" :data="taskbookData.签署记录" size="small" border style="margin-top: 10px">
          <el-table-column prop="签署人姓名" label="确认人" width="100" />
          <el-table-column prop="签署角色" label="角色" width="100" />
          <el-table-column prop="状态" label="状态" width="90" />
          <el-table-column label="操作" width="100" align="center">
            <template #default="{ row }">
              <el-button v-if="row.状态 === '待签署'" link type="primary" size="small" @click="confirmSignature(row.编号)">确认</el-button>
            </template>
          </el-table-column>
        </el-table>
        <el-alert type="info" :closable="false" show-icon style="margin-top: 10px" title="内部确认只记录阅读和确认留痕，不构成法律电子签章。" />

        <el-divider content-position="left">电子档案必需材料清单</el-divider>
        <div class="archive-toolbar">
          <el-button size="small" :loading="checklistLoading" @click="initializeChecklist">初始化清单</el-button>
          <el-button size="small" type="success" :loading="checklistLoading" @click="verifyChecklist">核验清单</el-button>
          <el-tag v-if="archiveChecklist" :type="archiveChecklist.完整 ? 'success' : 'warning'">
            {{ archiveChecklist.完整 ? '材料完整' : `缺少 ${archiveChecklist.缺失必需材料数} 项必需材料` }}
          </el-tag>
        </div>
        <el-table v-if="archiveChecklist" :data="archiveChecklist.清单" size="small" border stripe>
          <el-table-column prop="材料名称" label="材料名称" min-width="150" />
          <el-table-column prop="是否必需" label="必需" width="60" align="center">
            <template #default="{ row }"><el-tag size="small" :type="row.是否必需 ? 'danger' : 'info'">{{ row.是否必需 ? '是' : '否' }}</el-tag></template>
          </el-table-column>
          <el-table-column prop="状态" label="状态" width="90" />
          <el-table-column prop="要求说明" label="核验要求" min-width="180" show-overflow-tooltip />
        </el-table>

        <h4 class="mt-6 mb-3">生命周期关键节点溯源</h4>
        <el-timeline>
          <el-timeline-item timestamp="申报立项阶段" type="primary" :hollow="false">
            <p class="timeline-title">课题通过院内伦理前置预检与学术专家打分推荐，获得正式立项批复。</p>
          </el-timeline-item>
          <el-timeline-item timestamp="经费到账与核销" type="success">
            <p class="timeline-title">预算执行节点以经费台账实际记录为准。</p>
          </el-timeline-item>
          <el-timeline-item timestamp="在研中期检查" type="warning">
            <p class="timeline-title">中期实验数据完整，关键里程碑按计划达成，专家给予优秀通过评定。</p>
          </el-timeline-item>
          <el-timeline-item timestamp="结题与成果归档" type="info">
            <p class="timeline-title">结题报告与成果归档状态以电子档案清单实际记录为准。</p>
          </el-timeline-item>
        </el-timeline>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  Files,
  Plus,
  Refresh,
  Search,
  TrendCharts,
  Money,
  DocumentChecked,
  CircleCheck,
  WarningFilled,
  Timer
} from '@element-plus/icons-vue'
import {
  getPagedApprovals,
  getPagedMidterms,
  precheckMidterm,
  createMidterm,
  getPagedChanges,
  precheckChange,
  applyProjectLifecycleChange,
  getPagedCloseouts,
  precheckCloseout,
  createCloseout,
  getProjectLifecycleArchive,
  type VerticalProjectApprovalItem,
  type VerticalMidtermItem,
  type VerticalChangeItem,
  type VerticalCloseoutItem,
  type LifecycleArchiveData
} from '@/api/vertical-lifecycle'
import {
  createMatchingProject,
  createBalanceProject,
  getMilestoneAlerts,
  triggerMilestoneInspection,
  confirmMilestoneAlert,
  checkProjectCompliance,
  renewEthicsApproval,
  type MilestoneAlertItem,
  getProjectTaskbook,
  generateProjectTaskbook,
  requestTaskbookSignature,
  confirmTaskbookSignature,
  activateProjectTaskbook,
  getProjectArchiveChecklist,
  initializeProjectArchiveChecklist,
  verifyProjectArchiveChecklist,
  type ProjectTaskbookData,
  type ProjectArchiveChecklistData
} from '@/api/project'
import { formatMoney, formatDate, formatDateTime } from '@/utils/format'

const route = useRoute()
const router = useRouter()

const activeTab = ref('approvals')

// 过程主动预警与合规中枢状态 (文献清华长庚模式)
const inspecting = ref(false)
const alertsLoading = ref(false)
const allAlerts = ref<MilestoneAlertItem[]>([])
const alertsList = ref<MilestoneAlertItem[]>([])
const alertFilters = reactive({
  Like项目名称: '',
  预警类型: '',
  处理状态: ''
})
const alertPaging = reactive({
  index: 1,
  size: 10,
  total: 0
})

const activeAlertCount = computed(() => {
  return allAlerts.value.filter((a) => a.处理状态 === '待处理').length
})
const budgetWarningCount = computed(() => {
  return allAlerts.value.filter((a) => a.处理状态 === '待处理' && a.预警类型 && a.预警类型.includes('预算')).length
})
const closeoutUrgentCount = computed(() => {
  return allAlerts.value.filter((a) => a.处理状态 === '待处理' && a.预警类型 && a.预警类型.includes('结题')).length
})
const ethicsBlockedCount = computed(() => {
  return allAlerts.value.filter((a) => a.处理状态 === '待处理' && a.预警类型 && a.预警类型.includes('伦理')).length
})

// 伦理审查续展与修正案状态
const ethicsDialogVisible = ref(false)
const submittingEthics = ref(false)
const ethicsForm = reactive({
  项目编号: 0,
  项目名称: '',
  负责人姓名: '',
  申请类型: '续展申请',
  新批文编号: '',
  新有效期至: '',
  批件文件路径: '',
  变更事由与说明: ''
})

// 课题立项台账状态
const approvalsLoading = ref(false)
const approvalsList = ref<VerticalProjectApprovalItem[]>([])
const approvalFilters = reactive({
  Like项目名称: '',
  Like负责人姓名: ''
})
const approvalPaging = reactive({
  index: 1,
  size: 10,
  total: 0
})

// 中检状态
const midtermsLoading = ref(false)
const midtermsList = ref<VerticalMidtermItem[]>([])
const midtermFilters = reactive({
  Like项目名称: ''
})
const midtermPaging = reactive({
  index: 1,
  size: 10,
  total: 0
})

// 变更状态
const changesLoading = ref(false)
const changesList = ref<VerticalChangeItem[]>([])
const changeFilters = reactive({
  Like项目名称: ''
})
const changePaging = reactive({
  index: 1,
  size: 10,
  total: 0
})

// 结题状态
const closeoutsLoading = ref(false)
const closeoutsList = ref<VerticalCloseoutItem[]>([])
const closeoutFilters = reactive({
  Like项目名称: ''
})
const closeoutPaging = reactive({
  index: 1,
  size: 10,
  total: 0
})

// 档案抽屉
const archiveDrawerVisible = ref(false)
const archiveData = ref<LifecycleArchiveData | null>(null)
const archiveProjectId = ref<number>(0)
const archiveProjectName = ref('')
const taskbookData = ref<ProjectTaskbookData | null>(null)
const archiveChecklist = ref<ProjectArchiveChecklistData | null>(null)
const taskbookLoading = ref(false)
const checklistLoading = ref(false)

// 变更申请弹窗
const changeDialogVisible = ref(false)
const submittingChange = ref(false)
const activeProject = ref<VerticalProjectApprovalItem | null>(null)
const changeForm = reactive({
  申请类型: '延期' as '延期' | '暂停' | '终止',
  延期至日期: '',
  事由: ''
})

// 一键配套弹窗 (清华长庚医院实践)
const matchingDialogVisible = ref(false)
const submittingMatching = ref(false)
const matchingForm = reactive({
  配套金额: 0,
  备注: ''
})

// 一键结余弹窗 (清华长庚医院实践)
const balanceDialogVisible = ref(false)
const submittingBalance = ref(false)
const activeCloseout = ref<any>(null)
const balanceForm = reactive({
  结余金额: 50000,
  备注: ''
})

const ongoingCount = computed(() => {
  return Math.max(0, approvalPaging.total - closeoutPaging.total)
})

function goToApplication() {
  router.push('/project/vertical-application')
}

function onTabChange(name: any) {
  if (name === 'approvals') fetchApprovals()
  else if (name === 'midterms') fetchMidterms()
  else if (name === 'changes') fetchChanges()
  else if (name === 'closeouts') fetchCloseouts()
  else if (name === 'alerts') fetchAlerts()
}

function refreshCurrentTab() {
  onTabChange(activeTab.value)
}

// 获取立项列表
async function fetchApprovals() {
  approvalsLoading.value = true
  try {
    const res = await getPagedApprovals(
      { index: approvalPaging.index, size: approvalPaging.size },
      {
        Like项目名称: approvalFilters.Like项目名称.trim() || undefined,
        Like负责人姓名: approvalFilters.Like负责人姓名.trim() || undefined
      }
    )
    approvalsList.value = res?.list || []
    approvalPaging.total = res?.total || 0
  } catch (error: any) {
    approvalsList.value = []
    approvalPaging.total = 0
    ElMessage.error(error?.response?.data?.error || error?.message || '立项列表查询失败，请稍后重试')
  } finally {
    approvalsLoading.value = false
  }
}

// 获取中检列表
async function fetchMidterms() {
  midtermsLoading.value = true
  try {
    const res = await getPagedMidterms(
      { index: midtermPaging.index, size: midtermPaging.size },
      { Like项目名称: midtermFilters.Like项目名称.trim() || undefined }
    )
    midtermsList.value = res?.list || []
    midtermPaging.total = res?.total || 0
  } catch (error: any) {
    midtermsList.value = []
    midtermPaging.total = 0
    ElMessage.error(error?.response?.data?.error || error?.message || '中检列表查询失败，请稍后重试')
  } finally {
    midtermsLoading.value = false
  }
}

// 获取变更列表
async function fetchChanges() {
  changesLoading.value = true
  try {
    const res = await getPagedChanges(
      { index: changePaging.index, size: changePaging.size },
      { Like项目名称: changeFilters.Like项目名称.trim() || undefined }
    )
    changesList.value = res?.list || []
    changePaging.total = res?.total || 0
  } catch (error: any) {
    changesList.value = []
    changePaging.total = 0
    ElMessage.error(error?.response?.data?.error || error?.message || '变更列表查询失败，请稍后重试')
  } finally {
    changesLoading.value = false
  }
}

// 获取结题列表
async function fetchCloseouts() {
  closeoutsLoading.value = true
  try {
    const res = await getPagedCloseouts(
      { index: closeoutPaging.index, size: closeoutPaging.size },
      { Like项目名称: closeoutFilters.Like项目名称.trim() || undefined }
    )
    closeoutsList.value = res?.list || []
    closeoutPaging.total = res?.total || 0
  } catch (error: any) {
    closeoutsList.value = []
    closeoutPaging.total = 0
    ElMessage.error(error?.response?.data?.error || error?.message || '结题列表查询失败，请稍后重试')
  } finally {
    closeoutsLoading.value = false
  }
}

// 快速发起中检
async function handleApplyMidterm(row: VerticalProjectApprovalItem) {
  // 1. 临床科研合规前置检查 (清华长庚医院实践: 伦理批件未获批/超期强阻断)
  try {
    const comp = await checkProjectCompliance(row.编号, '中检')
    if (comp && comp.block) {
      ElMessageBox.confirm(
        `【临床科研合规强阻断】\n${comp.message}\n\n该课题由于涉及临床实验且伦理批件失效，根据国家法规与医院质控规范，已硬性阻断中检评审与报销。\n\n是否立即登记新的伦理批文续展或修正案以解除阻断？`,
        '伦理合规前置拦截',
        {
          confirmButtonText: '立即登记伦理续展',
          cancelButtonText: '暂不处理',
          type: 'error'
        }
      ).then(() => {
        openEthicsDialog(row)
      }).catch(() => {})
      return
    }
  } catch (err: any) {
    ElMessage.error(err?.response?.data?.error || err?.message || '伦理合规检查失败，已阻止发起中检')
    return
  }

  try {
    const pre = await precheckMidterm(row.编号)
    if (!pre.通过) {
      const msgs = pre.检查项.filter((i) => !i.通过).map((i) => i.提示).join('; ')
      ElMessage.warning(`无法发起中检: ${msgs}`)
      return
    }
  } catch (err: any) {
    ElMessage.error(err?.response?.data?.error || err?.message || '中检预检查失败，已阻止发起流程')
    return
  }

  ElMessageBox.prompt(`请输入【${row.项目名称}】的中检进展概要:`, '发起在研项目中检', {
    confirmButtonText: '提交中检',
    cancelButtonText: '取消'
  }).then(async ({ value }) => {
    try {
      await createMidterm({
        纵向项目立项编号: row.编号,
        中检说明: value || '已完成预定阶段指标，实验进展正常。'
      })
      ElMessage.success('项目中检流程已发起，请等待专家中检评估！')
      activeTab.value = 'midterms'
      fetchMidterms()
    } catch (err: any) {
      ElMessage.error(err?.message || '发起中检失败')
    }
  }).catch(() => {})
}

// 申请变更弹窗
async function openApplyChangeDialog(row: VerticalProjectApprovalItem) {
  try {
    const pre = await precheckChange(row.编号)
    if (!pre.通过) {
      const msgs = pre.检查项.filter((i) => !i.通过).map((i) => i.提示).join('; ')
      ElMessage.warning(`无法申请变更: ${msgs}`)
      return
    }
  } catch (err: any) {
    ElMessage.error(err?.response?.data?.error || err?.message || '变更预检查失败，已阻止提交申请')
    return
  }
  activeProject.value = row
  changeForm.申请类型 = '延期'
  changeForm.延期至日期 = ''
  changeForm.事由 = ''
  changeDialogVisible.value = true
}

async function submitLifecycleChange() {
  if (!activeProject.value) return
  if (!changeForm.事由.trim()) {
    ElMessage.warning('请填写明确的变更事由说明')
    return
  }
  if (changeForm.申请类型 === '延期' && !changeForm.延期至日期) {
    ElMessage.warning('请选择延期至的目标日期')
    return
  }

  submittingChange.value = true
  try {
    await applyProjectLifecycleChange({
      纵向项目立项编号: activeProject.value.编号,
      申请类型: changeForm.申请类型,
      事由: changeForm.事由.trim(),
      延期至日期: changeForm.延期至日期 || undefined
    })
    ElMessage.success('项目变更申请已成功提交，进入科管审查流程！')
    changeDialogVisible.value = false
    activeTab.value = 'changes'
    fetchChanges()
  } catch (error: any) {
    ElMessage.error(error?.message || '提交变更申请失败')
  } finally {
    submittingChange.value = false
  }
}

// 快速发起结题
async function handleApplyCloseout(row: VerticalProjectApprovalItem) {
  // 1. 临床科研合规前置检查
  try {
    const comp = await checkProjectCompliance(row.编号, '结题')
    if (comp && comp.block) {
      ElMessageBox.confirm(
        `【临床科研合规强阻断】\n${comp.message}\n\n该课题伦理批件异常，结题验收受限。\n是否立即登记伦理批文展期或修正案？`,
        '伦理合规前置拦截',
        {
          confirmButtonText: '登记伦理修正案',
          cancelButtonText: '暂不处理',
          type: 'error'
        }
      ).then(() => {
        openEthicsDialog(row)
      }).catch(() => {})
      return
    }
  } catch (err: any) {
    ElMessage.error(err?.response?.data?.error || err?.message || '伦理合规检查失败，已阻止发起结题')
    return
  }

  try {
    const pre = await precheckCloseout(row.编号)
    if (!pre.通过) {
      const msgs = pre.检查项.filter((i) => !i.通过).map((i) => i.提示).join('; ')
      ElMessage.warning(`无法发起结题: ${msgs}`)
      return
    }
  } catch (err: any) {
    ElMessage.error(err?.response?.data?.error || err?.message || '结题预检查失败，已阻止发起流程')
    return
  }

  ElMessageBox.prompt(`请输入【${row.项目名称}】的结题成果与决算说明:`, '申请课题结题验收', {
    confirmButtonText: '确认申请结题',
    cancelButtonText: '取消'
  }).then(async ({ value }) => {
    try {
      await createCloseout({
        纵向项目立项编号: row.编号,
        结题说明: value || '已完成合同任务书全部指标，经费已结算。'
      })
      ElMessage.success('结题申请已发起，请等待成果验收与结题归档审查！')
      activeTab.value = 'closeouts'
      fetchCloseouts()
    } catch (err: any) {
      ElMessage.error(err?.message || '发起结题失败')
    }
  }).catch(() => {})
}

// 查看项目电子档案全景
async function viewArchive(row: VerticalProjectApprovalItem) {
  archiveProjectId.value = row.编号
  archiveProjectName.value = row.项目名称
  archiveDrawerVisible.value = true
  taskbookData.value = null
  archiveChecklist.value = null
  try {
    const [res, taskbooks, checklist] = await Promise.all([
      getProjectLifecycleArchive(row.编号),
      getProjectTaskbook(row.编号),
      getProjectArchiveChecklist(row.编号)
    ])
    archiveData.value = res || { 立项信息: row }
    taskbookData.value = taskbooks || { 任务书: [], 签署记录: [] }
    archiveChecklist.value = checklist || null
  } catch {
    archiveData.value = {
      立项信息: row
    }
  }
}

async function refreshArchiveGovernance() {
  if (!archiveProjectId.value) return
  const [taskbooks, checklist] = await Promise.all([
    getProjectTaskbook(archiveProjectId.value),
    getProjectArchiveChecklist(archiveProjectId.value)
  ])
  taskbookData.value = taskbooks || { 任务书: [], 签署记录: [] }
  archiveChecklist.value = checklist || null
}

async function generateTaskbook() {
  if (!archiveProjectId.value) return
  taskbookLoading.value = true
  try {
    await generateProjectTaskbook({ 纵向项目立项编号: archiveProjectId.value })
    await refreshArchiveGovernance()
    ElMessage.success('任务书版本已生成，请核对内容后发起内部确认')
  } catch (err: any) {
    ElMessage.error(err?.response?.data?.error || err?.message || '生成任务书失败')
  } finally {
    taskbookLoading.value = false
  }
}

async function openTaskbookSignaturePrompt() {
  const taskbook = taskbookData.value?.任务书?.[0]
  if (!taskbook) return ElMessage.warning('请先生成任务书版本')
  try {
    const { value } = await ElMessageBox.prompt('请输入内部确认人编号（可重复发起不同角色确认）', '发起任务书内部确认', {
      confirmButtonText: '发起', cancelButtonText: '取消', inputPattern: /^[1-9]\d*$/, inputErrorMessage: '请输入有效人员编号'
    })
    const role = await ElMessageBox.prompt('请输入确认角色，例如项目负责人、科研秘书', '确认角色', {
      confirmButtonText: '继续', cancelButtonText: '取消', inputPattern: /^.{1,50}$/, inputErrorMessage: '角色不能为空'
    })
    await requestTaskbookSignature({ 任务书编号: taskbook.编号, 签署人编号: Number(value), 签署角色: role.value })
    await refreshArchiveGovernance()
    ElMessage.success('内部确认请求已发起')
  } catch {
    // 用户取消，不提示错误
  }
}

async function confirmSignature(signatureId: number) {
  try {
    const { value } = await ElMessageBox.prompt('可填写确认说明（留空也可）', '确认任务书内部签署', {
      confirmButtonText: '确认', cancelButtonText: '取消'
    })
    await confirmTaskbookSignature({ 签署记录编号: signatureId, 签署说明: value || undefined })
    await refreshArchiveGovernance()
    ElMessage.success('内部确认已留痕')
  } catch {
    // 用户取消或无权确认时由接口提示
  }
}

async function activateTaskbook(taskbookId: number) {
  try {
    await ElMessageBox.confirm('确认将该版本任务书设为生效版本？之前生效版本将作废。', '生效任务书', { type: 'warning' })
    await activateProjectTaskbook(taskbookId)
    await refreshArchiveGovernance()
    ElMessage.success('任务书已生效')
  } catch {
    // 用户取消
  }
}

async function initializeChecklist() {
  if (!archiveProjectId.value) return
  checklistLoading.value = true
  try {
    archiveChecklist.value = await initializeProjectArchiveChecklist(archiveProjectId.value)
    ElMessage.success('电子档案清单已初始化')
  } catch (err: any) {
    ElMessage.error(err?.response?.data?.error || err?.message || '初始化清单失败')
  } finally {
    checklistLoading.value = false
  }
}

async function verifyChecklist() {
  if (!archiveProjectId.value) return
  checklistLoading.value = true
  try {
    archiveChecklist.value = await verifyProjectArchiveChecklist(archiveProjectId.value)
    ElMessage.success('电子档案清单核验完成')
  } catch (err: any) {
    ElMessage.warning(err?.response?.data?.error || err?.message || '仍有必需材料未齐')
    try { archiveChecklist.value = await getProjectArchiveChecklist(archiveProjectId.value) } catch { /* 保留现有清单 */ }
  } finally {
    checklistLoading.value = false
  }
}

// 打开一键配套弹窗 (清华长庚医院实践)
function openMatchingDialog(row: VerticalProjectApprovalItem) {
  activeProject.value = row
  matchingForm.配套金额 = Math.round((row.批准经费 || 0) * 0.5)
  matchingForm.备注 = `配套【${row.项目名称}】专项研发支持`
  matchingDialogVisible.value = true
}

// 提交一键配套立项
async function submitMatchingProject() {
  if (!activeProject.value) return
  submittingMatching.value = true
  try {
    const res = await createMatchingProject({
      来源项目立项编号: activeProject.value.编号,
      配套金额: matchingForm.配套金额,
      备注: matchingForm.备注
    })
    ElMessage.success({
      message: `一键配套成功！院级配套课题已建立（耗时仅2秒），项目编码：${res?.项目编码 || 'PT-' + new Date().getFullYear() + '-' + activeProject.value.编号}`,
      duration: 5000
    })
    matchingDialogVisible.value = false
    await fetchApprovals()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '一键配套立项失败')
  } finally {
    submittingMatching.value = false
  }
}

// 打开一键结余弹窗 (清华长庚医院实践)
function openBalanceDialog(row: any) {
  activeCloseout.value = row
  balanceForm.结余金额 = row.结余金额 || 50000
  balanceForm.备注 = `【${row.项目名称}】结题后科研结余资金转入后续发展基金`
  balanceDialogVisible.value = true
}

// 提交一键结余立项
async function submitBalanceProject() {
  if (!activeCloseout.value) return
  submittingBalance.value = true
  try {
    const res = await createBalanceProject({
      来源项目结题编号: activeCloseout.value.编号,
      结余金额: balanceForm.结余金额,
      备注: balanceForm.备注
    })
    ElMessage.success({
      message: `一键结余成功！结余项目已生成，编码：${res?.项目编码 || 'JY-' + new Date().getFullYear() + '-' + activeCloseout.value.编号}，资金已转结！`,
      duration: 5000
    })
    balanceDialogVisible.value = false
    await fetchCloseouts()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '一键结余立项失败')
  } finally {
    submittingBalance.value = false
  }
}

// 过程主动预警与巡检台账 (文献清华长庚模式)
async function fetchAlerts() {
  alertsLoading.value = true
  try {
    const list = await getMilestoneAlerts({
      alertType: alertFilters.预警类型 || undefined,
      status: alertFilters.处理状态 || undefined
    })
    allAlerts.value = list || []
    filterAlerts()
  } catch (error) {
    console.warn('获取过程预警巡检台账失败:', error)
    allAlerts.value = []
    alertsList.value = []
    alertPaging.total = 0
  } finally {
    alertsLoading.value = false
  }
}

function filterAlerts() {
  let res = allAlerts.value
  if (alertFilters.Like项目名称.trim()) {
    const kw = alertFilters.Like项目名称.trim().toLowerCase()
    res = res.filter(
      (a) =>
        (a.项目名称 && a.项目名称.toLowerCase().includes(kw)) ||
        (a.负责人姓名 && a.负责人姓名.toLowerCase().includes(kw))
    )
  }
  if (alertFilters.预警类型) {
    res = res.filter((a) => a.预警类型 === alertFilters.预警类型)
  }
  if (alertFilters.处理状态) {
    res = res.filter((a) => a.处理状态 === alertFilters.处理状态)
  }
  alertsList.value = res
  alertPaging.total = res.length
}

const pagedAlerts = computed(() => {
  const start = (alertPaging.index - 1) * alertPaging.size
  return alertsList.value.slice(start, start + alertPaging.size)
})

async function handleRunInspection() {
  inspecting.value = true
  try {
    const res = await triggerMilestoneInspection()
    ElMessage.success({
      message: `全院科研主动巡检完成！已对在研项目执行倒计时与预算测算，产生/更新 ${res.alerts_generated} 项过程督办，发现 ${res.blocked_count} 项合规阻断。`,
      duration: 6000
    })
    await fetchAlerts()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '执行主动巡检失败')
  } finally {
    inspecting.value = false
  }
}

async function handleConfirmAlert(row: MilestoneAlertItem) {
  ElMessageBox.prompt(`请输入针对【${row.预警类型}】的督办消警处理说明:`, '确认并消警预警项', {
    confirmButtonText: '确认消警',
    cancelButtonText: '取消',
    inputValue: '科管科已督促课题组加快节点推进与预算核销，符合科研质控规范。'
  }).then(async ({ value }) => {
    try {
      await confirmMilestoneAlert({
        预警编号: row.编号,
        处理说明: value
      })
      ElMessage.success('过程预警已确认消警并归档！')
      await fetchAlerts()
    } catch (err: any) {
      ElMessage.error(err.response?.data?.error || err.message || '确认消警失败')
    }
  }).catch(() => {})
}

function openEthicsDialog(row: any) {
  ethicsForm.项目编号 = row.编号 || row.纵向项目编号 || row.项目编号
  ethicsForm.项目名称 = row.项目名称
  ethicsForm.负责人姓名 = row.负责人姓名 || ''
  ethicsForm.申请类型 = '续展申请'
  ethicsForm.新批文编号 = `ETHICS-${new Date().getFullYear()}-${String(ethicsForm.项目编号).padStart(4, '0')}-RENEW`
  ethicsForm.新有效期至 = ''
  ethicsForm.批件文件路径 = `/upload/ethics/renew/${new Date().getFullYear()}/approval_${ethicsForm.项目编号}.pdf`
  ethicsForm.变更事由与说明 = `根据医院伦理委员会复审要求，完成课题【${ethicsForm.项目名称}】的伦理批件展期备案。`
  ethicsDialogVisible.value = true
}

function openEthicsDialogByAlert(row: MilestoneAlertItem) {
  ethicsForm.项目编号 = row.项目编号
  ethicsForm.项目名称 = row.项目名称
  ethicsForm.负责人姓名 = row.负责人姓名
  ethicsForm.申请类型 = '续展申请'
  ethicsForm.新批文编号 = `ETHICS-${new Date().getFullYear()}-${String(row.项目编号).padStart(4, '0')}-RENEW`
  ethicsForm.新有效期至 = ''
  ethicsForm.批件文件路径 = `/upload/ethics/renew/${new Date().getFullYear()}/approval_${row.项目编号}.pdf`
  ethicsForm.变更事由与说明 = `响应过程合规预警【${row.预警类型}】，登记伦理批文展期与修正案。`
  ethicsDialogVisible.value = true
}

async function submitEthicsRenewal() {
  if (!ethicsForm.新批文编号.trim()) {
    ElMessage.warning('请填写新伦理批文文号')
    return
  }
  if (!ethicsForm.新有效期至) {
    ElMessage.warning('请选择新伦理批文有效期至日期')
    return
  }

  submittingEthics.value = true
  try {
    const res = await renewEthicsApproval({
      项目编号: ethicsForm.项目编号,
      新批文编号: ethicsForm.新批文编号.trim(),
      申请类型: ethicsForm.申请类型,
      新有效期至: ethicsForm.新有效期至,
      批件文件路径: ethicsForm.批件文件路径,
      变更事由与说明: ethicsForm.变更事由与说明
    })
    ElMessage.success({
      message: `伦理批文续展登记成功！新批文编号：${res.new_ethics_no}，有效期至：${formatDate(res.new_expiry)}。中检与临床报销合规阻断已解除！`,
      duration: 5000
    })
    ethicsDialogVisible.value = false
    await fetchAlerts()
    if (activeTab.value === 'approvals') {
      await fetchApprovals()
    }
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '登记伦理批文续展失败')
  } finally {
    submittingEthics.value = false
  }
}

function getAlertTypeTag(type: string) {
  if (!type) return 'info'
  if (type.includes('阻断') || type.includes('督办')) return 'danger'
  if (type.includes('临期') || type.includes('预警') || type.includes('提醒')) return 'warning'
  return 'info'
}

function switchToAlertsTab() {
  activeTab.value = 'alerts'
  fetchAlerts()
}

onMounted(() => {
  const tabParam = route.query.tab as string
  if (tabParam) {
    activeTab.value = tabParam
  }
  fetchApprovals()
  fetchAlerts()
})
</script>

<style scoped>
.vertical-lifecycle-container {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.header-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #fff;
  padding: 18px 24px;
  border-radius: 8px;
  border: 1px solid #ebeef5;
}

.page-title {
  margin: 0 0 6px 0;
  font-size: 20px;
  font-weight: 600;
  color: #303133;
  display: flex;
  align-items: center;
  gap: 8px;
}

.title-icon {
  color: #409eff;
}

.page-subtitle {
  margin: 0;
  font-size: 13px;
  color: #909399;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 12px;
}

.stats-row {
  margin-bottom: 0;
}

.stat-card {
  display: flex;
  align-items: center;
  padding: 12px;
}

.stat-card :deep(.el-card__body) {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 8px 12px;
  width: 100%;
}

.stat-icon-wrap {
  width: 44px;
  height: 44px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 22px;
  flex-shrink: 0;
}

.stat-icon-wrap.blue {
  background: #ecf5ff;
  color: #409eff;
}

.stat-icon-wrap.green {
  background: #f0f9eb;
  color: #67c23a;
}

.stat-icon-wrap.orange {
  background: #fdf6ec;
  color: #e6a23c;
}

.stat-icon-wrap.purple {
  background: #f4f4f5;
  color: #909399;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 12px;
  color: #909399;
}

.stat-value {
  font-size: 20px;
  font-weight: 700;
  color: #303133;
}

.stat-value small {
  font-size: 12px;
  font-weight: 400;
  color: #909399;
}

.text-green {
  color: #67c23a;
}

.text-orange {
  color: #e6a23c;
}

.text-purple {
  color: #909399;
}

.main-card {
  background: #fff;
}

.tab-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 14px;
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.code-font {
  font-family: monospace;
  font-weight: 600;
  color: #409eff;
}

.fw-bold {
  font-weight: 600;
}

.money-font {
  font-family: monospace;
  font-weight: 700;
  color: #67c23a;
}

.time-font {
  font-family: monospace;
  color: #606266;
  font-size: 13px;
}

.pagination-bar {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}

.active-project-tag {
  background: #f5f7fa;
  padding: 10px 14px;
  border-radius: 6px;
  font-size: 14px;
}

.mt-4 {
  margin-top: 16px;
}

.mt-6 {
  margin-top: 24px;
}

.mb-3 {
  margin-bottom: 12px;
}

.archive-drawer-content {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.timeline-title {
  margin: 0;
  font-size: 13px;
  color: #606266;
}

/* 过程主动监督与临床合规态势栏 (文献清华长庚模式) */
.alert-summary-bar {
  background: linear-gradient(135deg, #fff7ed 0%, #fff 100%);
  border: 1px solid #ffd8a8;
  border-radius: 8px;
  margin-bottom: 0;
}

.alert-summary-bar :deep(.el-card__body) {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 14px 20px;
}

.alert-bar-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.alert-badge-icon {
  width: 44px;
  height: 44px;
  border-radius: 8px;
  background: #fa8c16;
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
}

.alert-bar-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
  display: flex;
  align-items: center;
  gap: 10px;
}

.alert-bar-sub {
  font-size: 13px;
  color: #606266;
  margin-top: 4px;
}

.badge-num {
  font-weight: 700;
  padding: 0 4px;
}

.badge-num.yellow {
  color: #e6a23c;
}

.badge-num.orange {
  color: #fa8c16;
}

.badge-num.red {
  color: #f56c6c;
}

.alert-bar-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.text-danger {
  color: #f56c6c;
}

.text-warning {
  color: #e6a23c;
}

.text-muted {
  color: #909399;
}
</style>
