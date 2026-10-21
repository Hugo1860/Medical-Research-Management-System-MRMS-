<template>
  <div class="achievement-ledger-container">
    <!-- 头部区域 -->
    <div class="header-card">
      <div class="header-info">
        <h2 class="page-title">
          <el-icon class="title-icon"><Reading /></el-icon>
          科研成果综合业务台账与治理中心
        </h2>
        <p class="page-subtitle">
          全链路归集学术论文、专著、发明专利与科技奖励成果，支持多源采集比对、专利代理机构事件协同推送与学术归属治理留痕。
        </p>
      </div>
      <div class="header-actions">
        <el-button type="primary" :icon="DocumentChecked" @click="goToClaim">
          论文成果认领
        </el-button>
        <el-button :icon="Refresh" @click="refreshActiveTab">刷新台账</el-button>
      </div>
    </div>

    <!-- 指标汇总卡片 -->
    <el-row :gutter="16" class="stats-row">
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap blue"><el-icon><Files /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">成果全景总数</span>
            <span class="stat-value">{{ overviewTotal }} <small>项</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap green"><el-icon><Notebook /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">出版学术著作</span>
            <span class="stat-value text-green">{{ bookTotal }} <small>部</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap orange"><el-icon><Key /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">知识产权与专利</span>
            <span class="stat-value text-orange">{{ patentTotal }} <small>项</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap purple"><el-icon><Trophy /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">科技荣誉与表彰</span>
            <span class="stat-value text-purple">{{ awardTotal }} <small>项</small></span>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 选项卡及主数据表 -->
    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab" class="custom-tabs" @tab-change="onTabChange">
        <!-- Tab 1: 成果综合总览 -->
        <el-tab-pane label="成果全景总览" name="overview">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-input
                v-model="overviewFilters.Like成果名称"
                placeholder="搜索成果名称/标题..."
                :prefix-icon="Search"
                clearable
                style="width: 260px"
                @keyup.enter="fetchOverview"
              />
              <el-select
                v-model="overviewFilters.成果类型"
                placeholder="全部成果类型"
                clearable
                style="width: 150px"
                @change="fetchOverview"
              >
                <el-option label="全部成果类型" value="" />
                <el-option label="论文" value="论文" />
                <el-option label="著作" value="著作" />
                <el-option label="专利" value="专利" />
                <el-option label="获奖" value="获奖" />
              </el-select>
              <el-button type="primary" :icon="Search" @click="fetchOverview">查询</el-button>
              <el-button :icon="Refresh" @click="resetOverviewFilters">重置</el-button>
            </div>
          </div>

          <el-table
            v-loading="loadingOverview"
            :data="overviewList"
            stripe
            border
            style="width: 100%"
          >
            <el-table-column prop="编号" label="编号" width="80" align="center" />
            <el-table-column prop="成果名称" label="成果名称 / 专著标题" min-width="260" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="font-medium">{{ row.成果名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="成果类型" label="成果类型" width="120" align="center">
              <template #default="{ row }">
                <el-tag :type="getTypeTagType(row.成果类型)" size="small">
                  {{ row.成果类型 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="发起人姓名" label="发起人 / 负责人" width="140">
              <template #default="{ row }">
                <span>{{ row.发起人姓名 || '-' }}</span>
                <span v-if="row.工号" class="text-muted text-xs"> ({{ row.工号 }})</span>
              </template>
            </el-table-column>
            <el-table-column prop="发起人部门名称" label="所属科室 / 部门" min-width="160" show-overflow-tooltip>
              <template #default="{ row }">
                {{ row.发起人部门名称 || '-' }}
              </template>
            </el-table-column>
            <el-table-column label="成果治理与操作" width="160" fixed="right" align="center">
              <template #default="{ row }">
                <el-button
                  link
                  type="primary"
                  size="small"
                  :icon="Connection"
                  @click="openGovernanceDrawer(getTypeCode(row.成果类型), row.编号, row.成果名称)"
                >
                  治理追溯
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-footer">
            <el-pagination
              v-model:current-page="overviewPaging.Index"
              v-model:page-size="overviewPaging.Size"
              :page-sizes="[10, 15, 20, 50]"
              :total="overviewTotal"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="fetchOverview"
              @current-change="fetchOverview"
            />
          </div>
        </el-tab-pane>

        <!-- Tab 2: 著作管理 -->
        <el-tab-pane label="著作管理" name="work">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-input
                v-model="bookFilters.Like著作名称"
                placeholder="搜索著作名称..."
                :prefix-icon="Search"
                clearable
                style="width: 240px"
                @keyup.enter="fetchBooks"
              />
              <el-button type="primary" :icon="Search" @click="fetchBooks">查询</el-button>
              <el-button :icon="Refresh" @click="resetBookFilters">重置</el-button>
            </div>
          </div>

          <el-table
            v-loading="loadingBooks"
            :data="bookList"
            stripe
            border
            style="width: 100%"
          >
            <el-table-column prop="编号" label="编号" width="80" align="center" />
            <el-table-column prop="著作名称" label="著作名称" min-width="240" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="font-medium">{{ row.著作名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="出版社名称" label="出版社名称" min-width="160" show-overflow-tooltip />
            <el-table-column prop="著作类型" label="著作类型" width="130">
              <template #default="{ row }">
                <el-tag size="small" effect="plain">{{ row.著作类型 || '-' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="ISBN号" label="ISBN号" width="150">
              <template #default="{ row }">{{ row.ISBN号 || '-' }}</template>
            </el-table-column>
            <el-table-column label="出版日期" width="120" align="center">
              <template #default="{ row }">{{ formatDate(row.著作出版日期) }}</template>
            </el-table-column>
            <el-table-column prop="主编" label="主编 / 作者" width="120">
              <template #default="{ row }">{{ row.主编 || '-' }}</template>
            </el-table-column>
            <el-table-column prop="著作字数" label="字数 (千字)" width="110" align="right">
              <template #default="{ row }">{{ row.著作字数 != null ? row.著作字数 : '-' }}</template>
            </el-table-column>
            <el-table-column label="操作" width="140" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="viewBookDetail(row.编号)">
                  详情
                </el-button>
                <el-button
                  link
                  type="success"
                  size="small"
                  @click="openGovernanceDrawer(2, row.编号, row.著作名称)"
                >
                  关联治理
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-footer">
            <el-pagination
              v-model:current-page="bookPaging.Index"
              v-model:page-size="bookPaging.Size"
              :page-sizes="[10, 15, 20, 50]"
              :total="bookTotal"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="fetchBooks"
              @current-change="fetchBooks"
            />
          </div>
        </el-tab-pane>

        <!-- Tab 3: 专利管理 -->
        <el-tab-pane label="专利与知识产权" name="patent">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-input
                v-model="patentFilters.Like专利名称"
                placeholder="搜索专利名称..."
                :prefix-icon="Search"
                clearable
                style="width: 240px"
                @keyup.enter="fetchPatents"
              />
              <el-button type="primary" :icon="Search" @click="fetchPatents">查询</el-button>
              <el-button :icon="Refresh" @click="resetPatentFilters">重置</el-button>
            </div>
          </div>

          <el-table
            v-loading="loadingPatents"
            :data="patentList"
            stripe
            border
            style="width: 100%"
          >
            <el-table-column prop="编号" label="编号" width="80" align="center" />
            <el-table-column prop="专利名称" label="专利名称" min-width="240" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="font-medium">{{ row.专利名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="专利权人名称" label="专利权人" min-width="160" show-overflow-tooltip />
            <el-table-column prop="专利类型" label="专利类型" width="130">
              <template #default="{ row }">
                <el-tag size="small">{{ row.专利类型 || '-' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="授权状态" width="110" align="center">
              <template #default="{ row }">
                <el-tag :type="row.是否授权 ? 'success' : 'warning'" size="small">
                  {{ row.是否授权 ? '已授权' : '审查中' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="申请号" label="申请号 / 专利号" width="150" show-overflow-tooltip>
              <template #default="{ row }">
                {{ row.专利号 || row.申请号 || '-' }}
              </template>
            </el-table-column>
            <el-table-column label="授权公告日" width="120" align="center">
              <template #default="{ row }">{{ formatDate(row.授权公告日) }}</template>
            </el-table-column>
            <el-table-column prop="代理机构" label="代理机构" min-width="140" show-overflow-tooltip />
            <el-table-column label="协同操作" width="180" fixed="right" align="center">
              <template #default="{ row }">
                <el-button
                  link
                  type="primary"
                  size="small"
                  :icon="Bell"
                  @click="openNotifyAgentDialog(row)"
                >
                  通知代理机构
                </el-button>
                <el-button
                  link
                  type="success"
                  size="small"
                  @click="openGovernanceDrawer(3, row.编号, row.专利名称)"
                >
                  治理
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-footer">
            <el-pagination
              v-model:current-page="patentPaging.Index"
              v-model:page-size="patentPaging.Size"
              :page-sizes="[10, 15, 20, 50]"
              :total="patentTotal"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="fetchPatents"
              @current-change="fetchPatents"
            />
          </div>
        </el-tab-pane>

        <!-- Tab 4: 获奖管理 -->
        <el-tab-pane label="科技获奖与表彰" name="award">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-input
                v-model="awardFilters.Like获奖名称"
                placeholder="搜索获奖成果名称..."
                :prefix-icon="Search"
                clearable
                style="width: 240px"
                @keyup.enter="fetchAwards"
              />
              <el-button type="primary" :icon="Search" @click="fetchAwards">查询</el-button>
              <el-button :icon="Refresh" @click="resetAwardFilters">重置</el-button>
            </div>
          </div>

          <el-table
            v-loading="loadingAwards"
            :data="awardList"
            stripe
            border
            style="width: 100%"
          >
            <el-table-column prop="编号" label="编号" width="80" align="center" />
            <el-table-column prop="获奖名称" label="获奖成果名称" min-width="240" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="font-medium">{{ row.获奖名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="获奖级别" label="获奖级别" width="120" align="center">
              <template #default="{ row }">
                <el-tag size="small" type="danger" effect="plain">{{ row.获奖级别 || '-' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="获奖类别" label="获奖类别" width="130" show-overflow-tooltip />
            <el-table-column prop="颁奖单位" label="颁奖单位 / 机构" min-width="160" show-overflow-tooltip />
            <el-table-column label="获奖日期" width="120" align="center">
              <template #default="{ row }">{{ formatDate(row.获奖日期) }}</template>
            </el-table-column>
            <el-table-column prop="年度" label="年度" width="90" align="center" />
            <el-table-column prop="完成人数" label="完成人数" width="90" align="center" />
            <el-table-column label="操作" width="140" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="viewAwardDetail(row.编号)">
                  详情
                </el-button>
                <el-button
                  link
                  type="success"
                  size="small"
                  @click="openGovernanceDrawer(4, row.编号, row.获奖名称)"
                >
                  成果治理
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-footer">
            <el-pagination
              v-model:current-page="awardPaging.Index"
              v-model:page-size="awardPaging.Size"
              :page-sizes="[10, 15, 20, 50]"
              :total="awardTotal"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="fetchAwards"
              @current-change="fetchAwards"
            />
          </div>
        </el-tab-pane>

        <!-- Tab 5: 成果转化与合同收益闭环 -->
        <el-tab-pane label="成果转化与收益" name="conversion">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-select v-model="conversionStatusFilter" placeholder="全部状态" clearable style="width: 150px" @change="fetchConversions">
                <el-option label="待审批" value="待审批" />
                <el-option label="已批准" value="已批准" />
                <el-option label="执行中" value="执行中" />
                <el-option label="已完成" value="已完成" />
                <el-option label="已驳回" value="已驳回" />
              </el-select>
              <el-button type="primary" :icon="Search" @click="fetchConversions">查询</el-button>
              <el-button type="success" :icon="Plus" @click="openConversionDialog()">登记成果转化</el-button>
            </div>
          </div>
          <el-table v-loading="loadingConversions" :data="conversionList" stripe border style="width: 100%">
            <el-table-column prop="编号" label="编号" width="80" align="center" />
            <el-table-column prop="成果名称" label="关联成果" min-width="220" show-overflow-tooltip />
            <el-table-column prop="转化名称" label="转化事项" min-width="220" show-overflow-tooltip />
            <el-table-column prop="转化方式" label="方式" width="110" />
            <el-table-column prop="合同编号" label="合同编号" width="150" show-overflow-tooltip />
            <el-table-column prop="合同金额" label="合同金额(元)" width="125" align="right" />
            <el-table-column prop="已收款金额" label="已收款(元)" width="120" align="right" />
            <el-table-column prop="状态" label="状态" width="100" align="center">
              <template #default="{ row }"><el-tag size="small" :type="row.状态 === '已批准' || row.状态 === '执行中' ? 'success' : row.状态 === '已驳回' ? 'danger' : 'warning'">{{ row.状态 }}</el-tag></template>
            </el-table-column>
            <el-table-column label="操作" width="250" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openConversionDetail(row)">收益明细</el-button>
                <el-button v-if="row.状态 === '待审批'" link type="success" size="small" @click="approveConversion(row, '已批准')">审批通过</el-button>
                <el-button v-if="row.状态 === '已批准' || row.状态 === '执行中'" link type="warning" size="small" @click="openRevenueDialog(row)">登记收款</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 通知专利代理机构弹窗 -->
    <el-dialog
      v-model="showNotifyDialog"
      title="通知专利代理机构"
      width="520px"
      destroy-on-close
    >
      <el-form label-width="130px" :model="notifyForm">
        <el-form-item label="专利名称">
          <span class="font-medium">{{ currentPatent?.专利名称 }}</span>
        </el-form-item>
        <el-form-item label="代理机构外部系统" required>
          <el-input-number
            v-model="notifyForm.外部系统编号"
            :min="1"
            placeholder="已登记的外部系统编号"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="协同事件类型" required>
          <el-select v-model="notifyForm.事件类型" style="width: 100%">
            <el-option label="申请 (Application)" value="申请" />
            <el-option label="授权 (Grant)" value="授权" />
            <el-option label="变更 (Amendment)" value="变更" />
            <el-option label="年费 (Maintenance Fee)" value="年费" />
            <el-option label="撤回 (Withdrawal)" value="撤回" />
          </el-select>
        </el-form-item>
        <el-form-item label="协同补充说明">
          <el-input
            v-model="notifyForm.说明"
            type="textarea"
            :rows="3"
            placeholder="填写递交细节、费用处理指令或代理合同特别约定..."
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showNotifyDialog = false">取消</el-button>
        <el-button type="primary" :loading="submittingNotify" @click="submitNotifyAgent">
          加入推送队列
        </el-button>
      </template>
    </el-dialog>

    <!-- 成果转化登记 -->
    <el-dialog v-model="conversionDialogVisible" title="登记成果转化" width="580px" destroy-on-close>
      <el-form :model="conversionForm" label-width="110px">
        <el-form-item label="成果类型" required><el-select v-model="conversionForm.成果类型" style="width: 100%"><el-option label="论文" :value="1" /><el-option label="著作" :value="2" /><el-option label="专利" :value="3" /><el-option label="获奖" :value="4" /></el-select></el-form-item>
        <el-form-item label="成果编号" required><el-input-number v-model="conversionForm.成果编号" :min="1" style="width: 100%" /></el-form-item>
        <el-form-item label="转化事项" required><el-input v-model="conversionForm.转化名称" placeholder="例如：专利技术许可给合作单位" /></el-form-item>
        <el-form-item label="转化方式"><el-select v-model="conversionForm.转化方式" style="width: 100%"><el-option label="技术转让" value="技术转让" /><el-option label="许可使用" value="许可使用" /><el-option label="作价入股" value="作价入股" /><el-option label="合作开发" value="合作开发" /><el-option label="其他" value="其他" /></el-select></el-form-item>
        <el-form-item label="合作单位"><el-input v-model="conversionForm.合作单位" /></el-form-item>
        <el-row :gutter="12"><el-col :span="12"><el-form-item label="合同编号"><el-input v-model="conversionForm.合同编号" /></el-form-item></el-col><el-col :span="12"><el-form-item label="合同金额"><el-input-number v-model="conversionForm.合同金额" :min="0" :precision="2" style="width: 100%" /></el-form-item></el-col></el-row>
        <el-form-item label="申请说明"><el-input v-model="conversionForm.申请说明" type="textarea" rows="3" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="conversionDialogVisible = false">取消</el-button><el-button type="primary" :loading="conversionSubmitting" @click="submitConversion">提交审批</el-button></template>
    </el-dialog>

    <!-- 合同收款登记 -->
    <el-dialog v-model="revenueDialogVisible" title="登记成果转化收款" width="500px" destroy-on-close>
      <el-form :model="revenueForm" label-width="110px">
        <el-form-item label="转化事项"><span>{{ selectedConversion?.转化名称 }}</span></el-form-item>
        <el-form-item label="收益类型"><el-select v-model="revenueForm.收益类型" style="width: 100%"><el-option label="合同款" value="合同款" /><el-option label="许可费" value="许可费" /><el-option label="分成" value="分成" /><el-option label="其他" value="其他" /></el-select></el-form-item>
        <el-form-item label="计划金额"><el-input-number v-model="revenueForm.计划金额" :min="0" :precision="2" style="width: 100%" /></el-form-item>
        <el-form-item label="实际收款"><el-input-number v-model="revenueForm.实际金额" :min="0" :precision="2" style="width: 100%" /></el-form-item>
        <el-form-item label="收款日期" required><el-date-picker v-model="revenueForm.收款日期" type="date" value-format="YYYY-MM-DD" style="width: 100%" /></el-form-item>
        <el-form-item label="收款凭证号" required><el-input v-model="revenueForm.收款凭证号" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="revenueForm.备注" type="textarea" rows="2" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="revenueDialogVisible = false">取消</el-button><el-button type="primary" :loading="revenueSubmitting" @click="submitRevenue">保存收款</el-button></template>
    </el-dialog>

    <el-drawer v-model="conversionDetailVisible" title="成果转化合同与收益分配" size="680px" destroy-on-close>
      <div v-loading="conversionDetailLoading">
        <el-descriptions v-if="conversionDetail" :column="2" border size="small">
          <el-descriptions-item label="转化事项">{{ conversionDetail.转化.转化名称 }}</el-descriptions-item><el-descriptions-item label="状态">{{ conversionDetail.转化.状态 }}</el-descriptions-item>
          <el-descriptions-item label="合同编号">{{ conversionDetail.转化.合同编号 || '-' }}</el-descriptions-item><el-descriptions-item label="合同金额">{{ conversionDetail.转化.合同金额 }}</el-descriptions-item>
        </el-descriptions>
        <h4 style="margin: 18px 0 8px">收款记录</h4>
        <el-table :data="conversionDetail?.收益 || []" border size="small"><el-table-column prop="收益类型" label="类型" width="90" /><el-table-column prop="计划金额" label="计划金额" width="110" /><el-table-column prop="实际金额" label="实际金额" width="110" /><el-table-column prop="收款凭证号" label="凭证号" min-width="130" /><el-table-column prop="分配状态" label="分配状态" width="100" /><el-table-column label="操作" width="100" fixed="right"><template #default="{ row }"><el-button link type="primary" size="small" :disabled="!row.实际金额" @click="openDistributionDialog(row)">分配收益</el-button></template></el-table-column></el-table>
        <h4 style="margin: 18px 0 8px">收益分配及财务回写</h4>
        <el-table :data="conversionDetail?.分配 || []" border size="small"><el-table-column prop="姓名" label="人员" width="100" /><el-table-column prop="分配金额" label="分配金额" width="110" /><el-table-column prop="财务回写状态" label="财务状态" width="110" /><el-table-column prop="财务凭证号" label="财务凭证" min-width="130" /><el-table-column label="操作" width="100" fixed="right"><template #default="{ row }"><el-button v-if="row.财务回写状态 !== '已回写'" link type="primary" size="small" @click="openDistributionFinance(row)">回写财务</el-button></template></el-table-column></el-table>
      </div>
    </el-drawer>

    <!-- 成果转化收益分配 -->
    <el-dialog v-model="distributionDialogVisible" title="登记成果转化收益分配" width="500px" destroy-on-close>
      <el-form :model="distributionForm" label-width="110px">
        <el-form-item label="收益编号">{{ distributionForm.收益编号 }}</el-form-item>
        <el-form-item label="人员编号" required><el-input-number v-model="distributionForm.人员编号" :min="1" style="width: 100%" /></el-form-item>
        <el-form-item label="分配比例"><el-input-number v-model="distributionForm.分配比例" :min="0" :max="1" :step="0.05" :precision="5" style="width: 100%" /></el-form-item>
        <el-form-item label="分配金额" required><el-input-number v-model="distributionForm.分配金额" :min="0" :precision="2" style="width: 100%" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="distributionForm.备注" type="textarea" rows="2" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="distributionDialogVisible = false">取消</el-button><el-button type="primary" :loading="distributionSubmitting" @click="submitDistribution">保存分配</el-button></template>
    </el-dialog>

    <!-- 成果转化收益财务回写 -->
    <el-dialog v-model="distributionFinanceDialogVisible" title="收益分配财务回写" width="500px" destroy-on-close>
      <el-form :model="distributionFinanceForm" label-width="110px">
        <el-form-item label="分配编号">{{ distributionFinanceForm.分配编号 }}</el-form-item>
        <el-form-item label="回写状态"><el-select v-model="distributionFinanceForm.财务回写状态" style="width: 100%"><el-option label="已回写" value="已回写" /><el-option label="回写失败" value="回写失败" /></el-select></el-form-item>
        <el-form-item label="财务凭证" required><el-input v-model="distributionFinanceForm.财务凭证号" placeholder="财务系统凭证或失败流水号" /></el-form-item>
        <el-form-item label="回写说明"><el-input v-model="distributionFinanceForm.财务回写说明" type="textarea" rows="2" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="distributionFinanceDialogVisible = false">取消</el-button><el-button type="primary" :loading="distributionFinanceSubmitting" @click="submitDistributionFinance">保存回写</el-button></template>
    </el-dialog>

    <!-- 成果全景治理与学术归属抽屉 -->
    <el-drawer
      v-model="showGovernanceDrawer"
      title="科研成果学术归属治理与关联全景"
      size="620px"
      destroy-on-close
    >
      <div v-loading="loadingGovernance" class="governance-drawer-content">
        <div class="gov-header-info">
          <h4 class="gov-achievement-title">{{ currentGovTitle }}</h4>
          <div class="gov-badges">
            <el-tag :type="getTypeTagType(currentGovTypeStr)" size="small">
              {{ currentGovTypeStr }}
            </el-tag>
            <el-tag
              size="small"
              :type="govDetail?.成果治理?.状态 === '有效' ? 'success' : govDetail?.成果治理?.状态 === '已撤稿' ? 'danger' : 'warning'"
              style="margin-left: 8px"
            >
              治理状态: {{ govDetail?.成果治理?.状态 || '有效' }}
            </el-tag>
          </div>
        </div>

        <el-divider />

        <!-- 关联项目与外部资产 -->
        <div class="gov-section">
          <div class="section-header-flex">
            <h5 class="section-heading">课题资助与外部关联 ({{ govDetail?.成果关联?.length || 0 }})</h5>
            <el-button type="primary" link size="small" :icon="Plus" @click="showAddRelationDialog = true">
              添加关联
            </el-button>
          </div>
          <el-table
            :data="govDetail?.成果关联 || []"
            stripe
            border
            size="small"
            style="margin-top: 8px"
          >
            <el-table-column prop="关联类型" label="关联类型" width="90" align="center">
              <template #default="{ row }">
                <el-tag size="small" effect="plain">{{ row.关联类型 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="关联对象" min-width="160">
              <template #default="{ row }">
                <div v-if="row.项目编号">关联课题: #{{ row.项目编号 }}</div>
                <div v-else-if="row.外部对象类型">
                  外部资产: {{ row.外部对象类型 }} ({{ row.外部对象编号 }})
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="关联说明" label="说明" min-width="140" show-overflow-tooltip />
            <el-table-column label="建立时间" width="110" align="center">
              <template #default="{ row }">{{ formatDate(row.建立时间) }}</template>
            </el-table-column>
          </el-table>
        </div>

        <el-divider />

        <!-- 作者身份与单位规范 -->
        <div class="gov-section">
          <div class="section-header-flex">
            <h5 class="section-heading">作者身份核验与单位规范 ({{ govDetail?.作者身份核验?.length || 0 }})</h5>
          </div>
          <el-table
            :data="govDetail?.作者身份核验 || []"
            stripe
            border
            size="small"
            style="margin-top: 8px"
          >
            <el-table-column prop="作者顺序" label="位次" width="60" align="center" />
            <el-table-column prop="作者显示姓名" label="署名姓名" width="100" />
            <el-table-column prop="单位名称" label="第一单位 / 署名单位" min-width="140" show-overflow-tooltip />
            <el-table-column prop="身份状态" label="身份核验" width="90" align="center">
              <template #default="{ row }">
                <el-tag
                  size="small"
                  :type="row.身份状态 === '已核验' ? 'success' : row.身份状态 === '需更正' ? 'danger' : 'info'"
                >
                  {{ row.身份状态 || '待核验' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="ORCID" label="ORCID" width="130" show-overflow-tooltip />
          </el-table>
        </div>

        <el-divider />

        <!-- 成果治理状态更新 -->
        <div class="gov-section">
          <h5 class="section-heading">更新成果治理状态</h5>
          <el-form label-width="90px" style="margin-top: 10px">
            <el-form-item label="治理状态">
              <el-radio-group v-model="governanceForm.状态">
                <el-radio value="有效">有效</el-radio>
                <el-radio value="待更正">待更正</el-radio>
                <el-radio value="已更正">已更正</el-radio>
                <el-radio value="已撤稿">已撤稿</el-radio>
                <el-radio value="重复">重复合并</el-radio>
              </el-radio-group>
            </el-form-item>
            <template v-if="governanceForm.状态 === '重复'">
              <el-form-item label="规范成果类型" required>
                <el-select v-model="governanceForm.规范成果类型" style="width: 100%">
                  <el-option label="论文" :value="1" /><el-option label="著作" :value="2" />
                  <el-option label="专利" :value="3" /><el-option label="获奖" :value="4" />
                </el-select>
              </el-form-item>
              <el-form-item label="规范成果编号" required>
                <el-input-number v-model="governanceForm.规范成果编号" :min="1" style="width: 100%" />
              </el-form-item>
            </template>
            <template v-if="['待更正', '已更正', '已撤稿'].includes(governanceForm.状态)">
              <el-form-item label="来源类型" required>
                <el-select v-model="governanceForm.来源类型" style="width: 100%">
                  <el-option label="期刊/出版社通知" value="期刊/出版社通知" /><el-option label="数据库状态" value="数据库状态" />
                  <el-option label="作者/机构申请" value="作者/机构申请" /><el-option label="伦理/监管决定" value="伦理/监管决定" />
                  <el-option label="内部核查" value="内部核查" /><el-option label="其他" value="其他" />
                </el-select>
              </el-form-item>
              <el-form-item label="来源凭证编号" required><el-input v-model="governanceForm.来源凭证编号" maxlength="200" /></el-form-item>
              <el-form-item label="来源凭证摘要" required><el-input v-model="governanceForm.来源凭证摘要" type="textarea" :rows="2" maxlength="1000" /></el-form-item>
              <el-form-item label="来源文件路径"><el-input v-model="governanceForm.来源文件路径" /></el-form-item>
              <el-form-item label="文件 SHA-256"><el-input v-model="governanceForm.来源文件SHA256" maxlength="64" /></el-form-item>
              <el-form-item label="来源系统"><el-input v-model="governanceForm.来源系统" maxlength="100" /></el-form-item>
              <el-alert title="新登记的撤稿/更正来源默认处于待核验状态，系统不会自动声明已核验。" type="warning" :closable="false" show-icon />
            </template>
            <el-form-item label="治理原因">
              <el-input
                v-model="governanceForm.治理原因"
                type="textarea"
                :rows="2"
                placeholder="录入治理判断原因、学术委员会决议或合并映射说明..."
              />
            </el-form-item>
            <el-form-item>
              <el-button
                type="primary"
                size="small"
                :loading="submittingGovStatus"
                @click="submitGovStatus"
              >
                保存治理判定
              </el-button>
            </el-form-item>
          </el-form>
        </div>

        <el-divider />

        <div class="gov-section">
          <h5 class="section-heading">不可变治理历史 ({{ govDetail?.治理历史?.length || 0 }})</h5>
          <el-table :data="govDetail?.治理历史 || []" stripe border size="small" style="margin-top: 8px">
            <el-table-column prop="变更动作" label="动作" width="75" />
            <el-table-column prop="原状态" label="原状态" width="85"><template #default="{ row }">{{ row.原状态 || '—' }}</template></el-table-column>
            <el-table-column prop="新状态" label="新状态" width="85" />
            <el-table-column prop="治理原因" label="原因" min-width="160" show-overflow-tooltip />
            <el-table-column prop="操作时间" label="时间" width="145"><template #default="{ row }">{{ formatDate(row.操作时间) }}</template></el-table-column>
          </el-table>
          <h5 class="section-heading" style="margin-top: 16px">重复成果合并映射 ({{ govDetail?.合并审计?.length || 0 }})</h5>
          <el-table :data="govDetail?.合并审计 || []" stripe border size="small" style="margin-top: 8px">
            <el-table-column label="来源成果" width="105"><template #default="{ row }">{{ row.来源成果类型 }} / #{{ row.来源成果编号 }}</template></el-table-column>
            <el-table-column label="规范成果" width="105"><template #default="{ row }">{{ row.规范成果类型 }} / #{{ row.规范成果编号 }}</template></el-table-column>
            <el-table-column prop="合并原因" label="合并原因" min-width="160" show-overflow-tooltip />
          </el-table>
        </div>
      </div>
    </el-drawer>

    <!-- 新增成果关联弹窗 -->
    <el-dialog
      v-model="showAddRelationDialog"
      title="新增科研成果关联"
      width="500px"
      destroy-on-close
    >
      <el-form label-width="110px" :model="relationForm">
        <el-form-item label="关联类型" required>
          <el-select v-model="relationForm.关联类型" style="width: 100%">
            <el-option label="研究 (Research)" value="研究" />
            <el-option label="资助 (Funding)" value="资助" />
            <el-option label="伦理 (Ethics)" value="伦理" />
            <el-option label="数据 (Data)" value="数据" />
            <el-option label="其他 (Other)" value="其他" />
          </el-select>
        </el-form-item>
        <el-form-item label="关联模式">
          <el-radio-group v-model="relationMode">
            <el-radio value="project">院内科研项目</el-radio>
            <el-radio value="external">外部系统资产</el-radio>
          </el-radio-group>
        </el-form-item>
        <template v-if="relationMode === 'project'">
          <el-form-item label="流程模板编号" required>
            <el-input-number v-model="relationForm.流程模板编号" :min="1" style="width: 100%" />
          </el-form-item>
          <el-form-item label="项目编号" required>
            <el-input-number v-model="relationForm.项目编号" :min="1" style="width: 100%" />
          </el-form-item>
        </template>
        <template v-else>
          <el-form-item label="外部对象类型" required>
            <el-input v-model="relationForm.外部对象类型" placeholder="例如: 伦理批件、共享临床数据集、GCP项目..." />
          </el-form-item>
          <el-form-item label="外部对象编号" required>
            <el-input v-model="relationForm.外部对象编号" placeholder="例如: IRB-2026-0812、DS-0091..." />
          </el-form-item>
        </template>
        <el-form-item label="关联说明">
          <el-input v-model="relationForm.关联说明" placeholder="简述该成果与目标对象的支撑产出关系..." />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showAddRelationDialog = false">取消</el-button>
        <el-button type="primary" :loading="submittingRelation" @click="submitAddRelation">
          确认关联
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import {
  Reading,
  Files,
  Notebook,
  Key,
  Trophy,
  Search,
  Refresh,
  Bell,
  Connection,
  Plus,
  DocumentChecked
} from '@element-plus/icons-vue'
import {
  getPagingAchievements,
  getAchievementGovernanceDetail,
  addAchievementRelation,
  updateAchievementGovernance,
  getPagingBooks,
  getPagingPatents,
  notifyPatentAgent,
  getPagingAwards,
  getAchievementConversions,
  getAchievementConversionDetail,
  saveAchievementConversion,
  approveAchievementConversion,
  saveAchievementConversionRevenue,
  saveAchievementConversionDistribution,
  updateAchievementConversionFinance,
  type AchievementOverviewItem,
  type BookItem,
  type PatentItem,
  type AwardItem,
  type AchievementConversion
} from '@/api/fruit'
import { formatDate } from '@/utils/format'

const route = useRoute()
const router = useRouter()

// 当前激活 Tab
const activeTab = ref('overview')

// 1. 成果全景总览
const loadingOverview = ref(false)
const overviewList = ref<AchievementOverviewItem[]>([])
const overviewTotal = ref(0)
const overviewPaging = reactive({ Index: 1, Size: 15, OrderType: false })
const overviewFilters = reactive({ Like成果名称: '', 成果类型: '' })

// 2. 著作管理
const loadingBooks = ref(false)
const bookList = ref<BookItem[]>([])
const bookTotal = ref(0)
const bookPaging = reactive({ Index: 1, Size: 15, OrderType: false })
const bookFilters = reactive({ Like著作名称: '' })

// 3. 专利管理
const loadingPatents = ref(false)
const patentList = ref<PatentItem[]>([])
const patentTotal = ref(0)
const patentPaging = reactive({ Index: 1, Size: 15, OrderType: false })
const patentFilters = reactive({ Like专利名称: '' })

// 4. 获奖管理
const loadingAwards = ref(false)
const awardList = ref<AwardItem[]>([])
const awardTotal = ref(0)
const awardPaging = reactive({ Index: 1, Size: 15, OrderType: false })
const awardFilters = reactive({ Like获奖名称: '' })

// 5. 专利代理机构通知弹窗
const showNotifyDialog = ref(false)
const submittingNotify = ref(false)
const currentPatent = ref<PatentItem | null>(null)
const notifyForm = reactive<{
  外部系统编号: number
  事件类型: '申请' | '授权' | '变更' | '年费' | '撤回'
  说明: string
}>({
  外部系统编号: 1,
  事件类型: '申请',
  说明: ''
})

// 6. 治理抽屉
const showGovernanceDrawer = ref(false)
const loadingGovernance = ref(false)
const currentGovType = ref(1)
const currentGovId = ref(0)
const currentGovTitle = ref('')
const govDetail = ref<any>(null)
const governanceForm = reactive<{
  状态: '有效' | '待更正' | '已更正' | '已撤稿' | '重复'
  规范成果类型?: number
  规范成果编号?: number
  治理原因: string
  来源类型?: string
  来源凭证编号?: string
  来源凭证摘要?: string
  来源文件路径?: string
  来源文件SHA256?: string
  来源系统?: string
}>({
  状态: '有效',
  规范成果类型: undefined,
  规范成果编号: undefined,
  治理原因: '', 来源类型: '', 来源凭证编号: '', 来源凭证摘要: '', 来源文件路径: '', 来源文件SHA256: '', 来源系统: ''
})
const submittingGovStatus = ref(false)

// 7. 新增成果关联弹窗
const showAddRelationDialog = ref(false)
const submittingRelation = ref(false)
const relationMode = ref<'project' | 'external'>('project')
const relationForm = reactive<{
  关联类型: '研究' | '资助' | '伦理' | '数据' | '其他'
  流程模板编号?: number
  项目编号?: number
  外部对象类型?: string
  外部对象编号?: string
  关联说明?: string
}>({
  关联类型: '研究',
  流程模板编号: 1,
  项目编号: 1,
  外部对象类型: '',
  外部对象编号: '',
  关联说明: ''
})

const currentGovTypeStr = computed(() => {
  const map: Record<number, string> = { 1: '论文', 2: '著作', 3: '专利', 4: '获奖' }
  return map[currentGovType.value] || '成果'
})

function getTypeTagType(type?: string) {
  if (type === '论文') return 'primary'
  if (type === '著作') return 'success'
  if (type === '专利') return 'warning'
  if (type === '获奖') return 'danger'
  return 'info'
}

function getTypeCode(type?: string): number {
  if (type === '论文') return 1
  if (type === '著作') return 2
  if (type === '专利') return 3
  if (type === '获奖') return 4
  return 1
}

// 5. 成果转化与合同收益
const loadingConversions = ref(false)
const conversionList = ref<AchievementConversion[]>([])
const conversionStatusFilter = ref('')
const conversionDialogVisible = ref(false)
const conversionSubmitting = ref(false)
const conversionForm = reactive<any>({ 成果类型: 3, 成果编号: 1, 转化名称: '', 转化方式: '技术转让', 合作单位: '', 合同编号: '', 合同金额: 0, 申请说明: '' })
const revenueDialogVisible = ref(false)
const revenueSubmitting = ref(false)
const selectedConversion = ref<AchievementConversion | null>(null)
const revenueForm = reactive<any>({ 转化编号: 0, 收益类型: '合同款', 计划金额: 0, 实际金额: 0, 收款日期: '', 收款凭证号: '', 备注: '' })
const distributionDialogVisible = ref(false)
const distributionSubmitting = ref(false)
const distributionForm = reactive<any>({ 收益编号: 0, 人员编号: 0, 分配比例: 0, 分配金额: 0, 备注: '' })
const conversionDetailVisible = ref(false)
const conversionDetailLoading = ref(false)
const conversionDetail = ref<any>(null)
const distributionFinanceDialogVisible = ref(false)
const distributionFinanceSubmitting = ref(false)
const distributionFinanceForm = reactive<any>({ 分配编号: 0, 财务回写状态: '已回写', 财务凭证号: '', 财务回写说明: '' })

// 获取成果总览
async function fetchOverview() {
  loadingOverview.value = true
  try {
    const res = await getPagingAchievements(overviewPaging, {
      Like成果名称: overviewFilters.Like成果名称 || undefined,
      成果类型: overviewFilters.成果类型 || undefined
    })
    const data = (res as any)?.data || res
    overviewList.value = data?.list || []
    overviewTotal.value = data?.total || 0
  } catch (err: any) {
    ElMessage.error(err.message || '获取成果全景列表失败')
  } finally {
    loadingOverview.value = false
  }
}

function resetOverviewFilters() {
  overviewFilters.Like成果名称 = ''
  overviewFilters.成果类型 = ''
  overviewPaging.Index = 1
  fetchOverview()
}

// 获取著作列表
async function fetchBooks() {
  loadingBooks.value = true
  try {
    const res = await getPagingBooks(bookPaging, {
      Like著作名称: bookFilters.Like著作名称 || undefined
    })
    const data = (res as any)?.data || res
    bookList.value = data?.list || []
    bookTotal.value = data?.total || 0
  } catch (err: any) {
    ElMessage.error(err.message || '获取学术著作列表失败')
  } finally {
    loadingBooks.value = false
  }
}

function resetBookFilters() {
  bookFilters.Like著作名称 = ''
  bookPaging.Index = 1
  fetchBooks()
}

// 获取专利列表
async function fetchPatents() {
  loadingPatents.value = true
  try {
    const res = await getPagingPatents(patentPaging, {
      Like专利名称: patentFilters.Like专利名称 || undefined
    })
    const data = (res as any)?.data || res
    patentList.value = data?.list || []
    patentTotal.value = data?.total || 0
  } catch (err: any) {
    ElMessage.error(err.message || '获取专利列表失败')
  } finally {
    loadingPatents.value = false
  }
}

function resetPatentFilters() {
  patentFilters.Like专利名称 = ''
  patentPaging.Index = 1
  fetchPatents()
}

// 获取获奖列表
async function fetchAwards() {
  loadingAwards.value = true
  try {
    const res = await getPagingAwards(awardPaging, {
      Like获奖名称: awardFilters.Like获奖名称 || undefined
    })
    const data = (res as any)?.data || res
    awardList.value = data?.list || []
    awardTotal.value = data?.total || 0
  } catch (err: any) {
    ElMessage.error(err.message || '获取科技获奖列表失败')
  } finally {
    loadingAwards.value = false
  }
}

function resetAwardFilters() {
  awardFilters.Like获奖名称 = ''
  awardPaging.Index = 1
  fetchAwards()
}

// 通知代理机构弹窗
function openNotifyAgentDialog(row: PatentItem) {
  currentPatent.value = row
  notifyForm.外部系统编号 = 1
  notifyForm.事件类型 = '申请'
  notifyForm.说明 = ''
  showNotifyDialog.value = true
}

async function submitNotifyAgent() {
  if (!currentPatent.value) return
  submittingNotify.value = true
  try {
    await notifyPatentAgent({
      专利编号: currentPatent.value.编号,
      外部系统编号: notifyForm.外部系统编号,
      事件类型: notifyForm.事件类型,
      说明: notifyForm.说明 || undefined
    })
    ElMessage.success('专利协同通知已成功加入外部推送队列 (幂等键已留痕)')
    showNotifyDialog.value = false
  } catch (err: any) {
    ElMessage.error(err.message || '通知专利代理机构失败')
  } finally {
    submittingNotify.value = false
  }
}

// 治理抽屉
async function openGovernanceDrawer(type: number, id: number, title: string) {
  currentGovType.value = type
  currentGovId.value = id
  currentGovTitle.value = title
  showGovernanceDrawer.value = true
  loadingGovernance.value = true
  try {
    const res = await getAchievementGovernanceDetail(type, id)
    const data = (res as any)?.data || res
    govDetail.value = data
    governanceForm.状态 = data?.成果治理?.状态 || '有效'
    governanceForm.规范成果类型 = data?.成果治理?.规范成果类型 || undefined
    governanceForm.规范成果编号 = data?.成果治理?.规范成果编号 || undefined
    governanceForm.治理原因 = data?.成果治理?.治理原因 || ''
    governanceForm.来源类型 = data?.成果治理?.来源类型 || ''
    governanceForm.来源凭证编号 = data?.成果治理?.来源凭证编号 || ''
    governanceForm.来源凭证摘要 = data?.成果治理?.来源凭证摘要 || ''
    governanceForm.来源文件路径 = data?.成果治理?.来源文件路径 || ''
    governanceForm.来源文件SHA256 = data?.成果治理?.来源文件SHA256 || ''
    governanceForm.来源系统 = data?.成果治理?.来源系统 || ''
  } catch (err: any) {
    ElMessage.error(err.message || '获取成果治理档案失败')
  } finally {
    loadingGovernance.value = false
  }
}

async function submitGovStatus() {
  submittingGovStatus.value = true
  try {
    const needsEvidence = ['待更正', '已更正', '已撤稿'].includes(governanceForm.状态)
    await updateAchievementGovernance({
      成果类型: currentGovType.value,
      成果编号: currentGovId.value,
      状态: governanceForm.状态,
      规范成果类型: governanceForm.状态 === '重复' ? governanceForm.规范成果类型 : undefined,
      规范成果编号: governanceForm.状态 === '重复' ? governanceForm.规范成果编号 : undefined,
      治理原因: governanceForm.治理原因 || undefined,
      来源类型: needsEvidence ? governanceForm.来源类型 || undefined : undefined,
      来源凭证编号: needsEvidence ? governanceForm.来源凭证编号 || undefined : undefined,
      来源凭证摘要: needsEvidence ? governanceForm.来源凭证摘要 || undefined : undefined,
      来源文件路径: needsEvidence ? governanceForm.来源文件路径 || undefined : undefined,
      来源文件SHA256: needsEvidence ? governanceForm.来源文件SHA256 || undefined : undefined,
      来源系统: needsEvidence ? governanceForm.来源系统 || undefined : undefined
    })
    ElMessage.success('成果治理状态判定已成功更新')
    openGovernanceDrawer(currentGovType.value, currentGovId.value, currentGovTitle.value)
  } catch (err: any) {
    ElMessage.error(err.message || '更新治理状态失败')
  } finally {
    submittingGovStatus.value = false
  }
}

async function submitAddRelation() {
  submittingRelation.value = true
  try {
    await addAchievementRelation({
      成果类型: currentGovType.value,
      成果编号: currentGovId.value,
      关联类型: relationForm.关联类型,
      流程模板编号: relationMode.value === 'project' ? relationForm.流程模板编号 : undefined,
      项目编号: relationMode.value === 'project' ? relationForm.项目编号 : undefined,
      外部对象类型: relationMode.value === 'external' ? relationForm.外部对象类型 : undefined,
      外部对象编号: relationMode.value === 'external' ? relationForm.外部对象编号 : undefined,
      关联说明: relationForm.关联说明 || undefined
    })
    ElMessage.success('成果关联已成功建立')
    showAddRelationDialog.value = false
    openGovernanceDrawer(currentGovType.value, currentGovId.value, currentGovTitle.value)
  } catch (err: any) {
    ElMessage.error(err.message || '添加关联失败')
  } finally {
    submittingRelation.value = false
  }
}

function viewBookDetail(id: number) {
  openGovernanceDrawer(2, id, `学术专著 #${id}`)
}

function viewAwardDetail(id: number) {
  openGovernanceDrawer(4, id, `科技奖励 #${id}`)
}

async function fetchConversions() {
  loadingConversions.value = true
  try {
    const res = await getAchievementConversions({ 状态: conversionStatusFilter.value || undefined })
    conversionList.value = ((res as any)?.data || res || []) as AchievementConversion[]
  } catch (err: any) {
    ElMessage.error(err.message || '获取成果转化台账失败')
  } finally {
    loadingConversions.value = false
  }
}

function openConversionDialog() {
  Object.assign(conversionForm, { 成果类型: 3, 成果编号: 1, 转化名称: '', 转化方式: '技术转让', 合作单位: '', 合同编号: '', 合同金额: 0, 申请说明: '' })
  conversionDialogVisible.value = true
}

async function submitConversion() {
  if (!conversionForm.转化名称 || !conversionForm.成果编号) {
    ElMessage.warning('请填写关联成果编号和转化事项')
    return
  }
  conversionSubmitting.value = true
  try {
    await saveAchievementConversion(conversionForm)
    ElMessage.success('成果转化已登记并提交审批')
    conversionDialogVisible.value = false
    await fetchConversions()
  } catch (err: any) {
    ElMessage.error(err.message || '登记成果转化失败')
  } finally {
    conversionSubmitting.value = false
  }
}

async function approveConversion(row: AchievementConversion, status: string) {
  try {
    await approveAchievementConversion({ 转化编号: row.编号, 状态: status, 审批意见: '系统管理员在线审批' })
    ElMessage.success('成果转化审批状态已更新')
    await fetchConversions()
  } catch (err: any) {
    ElMessage.error(err.message || '审批成果转化失败')
  }
}

function openRevenueDialog(row: AchievementConversion) {
  selectedConversion.value = row
  Object.assign(revenueForm, { 转化编号: row.编号, 收益类型: '合同款', 计划金额: row.合同金额 || 0, 实际金额: 0, 收款日期: new Date().toISOString().slice(0, 10), 收款凭证号: '', 备注: '' })
  revenueDialogVisible.value = true
}

async function submitRevenue() {
  if (!revenueForm.实际金额 || revenueForm.实际金额 < 0) {
    ElMessage.warning('请填写实际收款金额')
    return
  }
  if (!revenueForm.收款日期 || !revenueForm.收款凭证号) {
    ElMessage.warning('实际收款必须填写日期和收款凭证号')
    return
  }
  revenueSubmitting.value = true
  try {
    await saveAchievementConversionRevenue(revenueForm)
    ElMessage.success('收款记录已保存，已进入收益分配台账')
    revenueDialogVisible.value = false
    await fetchConversions()
  } catch (err: any) {
    ElMessage.error(err.message || '登记收款失败')
  } finally {
    revenueSubmitting.value = false
  }
}

function openDistributionDialog(row: any) {
  Object.assign(distributionForm, { 收益编号: row.编号, 人员编号: 0, 分配比例: 0, 分配金额: row.实际金额 || 0, 备注: '' })
  distributionDialogVisible.value = true
}

async function submitDistribution() {
  if (!distributionForm.收益编号 || !distributionForm.人员编号 || distributionForm.分配金额 < 0) { ElMessage.warning('请填写收益编号、人员编号和分配金额'); return }
  distributionSubmitting.value = true
  try {
    await saveAchievementConversionDistribution(distributionForm)
    ElMessage.success('收益分配已保存')
    distributionDialogVisible.value = false
    if (selectedConversion.value) await openConversionDetail(selectedConversion.value)
  } catch (error: any) { ElMessage.error(error.message || '保存收益分配失败') } finally { distributionSubmitting.value = false }
}

function openDistributionFinance(row: any) {
  Object.assign(distributionFinanceForm, { 分配编号: row.编号, 财务回写状态: '已回写', 财务凭证号: '', 财务回写说明: '' })
  distributionFinanceDialogVisible.value = true
}

async function submitDistributionFinance() {
  if (!distributionFinanceForm.分配编号 || !distributionFinanceForm.财务凭证号) { ElMessage.warning('请填写财务凭证或失败流水号'); return }
  distributionFinanceSubmitting.value = true
  try {
    await updateAchievementConversionFinance(distributionFinanceForm)
    ElMessage.success('收益分配财务状态已回写')
    distributionFinanceDialogVisible.value = false
    if (selectedConversion.value) await openConversionDetail(selectedConversion.value)
  } catch (error: any) { ElMessage.error(error.message || '财务回写失败') } finally { distributionFinanceSubmitting.value = false }
}

async function openConversionDetail(row: AchievementConversion) {
  conversionDetailVisible.value = true
  conversionDetailLoading.value = true
  try {
    const res = await getAchievementConversionDetail(row.编号)
    conversionDetail.value = (res as any)?.data || res
  } catch (err: any) {
    ElMessage.error(err.message || '获取成果转化详情失败')
  } finally {
    conversionDetailLoading.value = false
  }
}

function goToClaim() {
  router.push('/fruit/paper-claim')
}

function onTabChange(tab: string | number) {
  const name = String(tab)
  if (name === 'overview') fetchOverview()
  else if (name === 'work') fetchBooks()
  else if (name === 'patent') fetchPatents()
  else if (name === 'award') fetchAwards()
  else if (name === 'conversion') fetchConversions()
}

function refreshActiveTab() {
  onTabChange(activeTab.value)
}

onMounted(() => {
  const initialTab = (route.query.tab as string) || (route.query.module as string) || 'overview'
  if (['overview', 'work', 'patent', 'award', 'conversion'].includes(initialTab)) {
    activeTab.value = initialTab
  }
  fetchOverview()
  fetchBooks()
  fetchPatents()
  fetchAwards()
  fetchConversions()
})
</script>

<style scoped>
.achievement-ledger-container {
  padding: 16px 20px 30px;
  background-color: #f6f8fa;
  min-height: calc(100vh - 84px);
}

.header-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #ffffff;
  padding: 18px 24px;
  border-radius: 8px;
  border: 1px solid #ebeef5;
  margin-bottom: 16px;
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

.stats-row {
  margin-bottom: 16px;
}

.stat-card {
  border-radius: 8px;
  display: flex;
  align-items: center;
  padding: 8px 12px;
}

:deep(.stat-card .el-card__body) {
  display: flex;
  align-items: center;
  width: 100%;
  padding: 10px 14px;
}

.stat-icon-wrap {
  width: 46px;
  height: 46px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
  margin-right: 14px;
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
  background: #f4f0ff;
  color: #8e44ad;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 12px;
  color: #909399;
  margin-bottom: 4px;
}

.stat-value {
  font-size: 22px;
  font-weight: 700;
  color: #303133;
}

.stat-value small {
  font-size: 12px;
  font-weight: normal;
}

.text-green {
  color: #67c23a;
}
.text-orange {
  color: #e6a23c;
}
.text-purple {
  color: #8e44ad;
}

.main-card {
  border-radius: 8px;
  border: 1px solid #ebeef5;
}

.tab-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  margin-bottom: 16px;
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.pagination-footer {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}

.font-medium {
  font-weight: 500;
}
.text-muted {
  color: #909399;
}
.text-xs {
  font-size: 12px;
}

/* 治理抽屉 */
.governance-drawer-content {
  padding: 10px 0;
}

.gov-achievement-title {
  margin: 0 0 8px;
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.gov-badges {
  display: flex;
  align-items: center;
}

.section-header-flex {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.section-heading {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: #303133;
}

.gov-section {
  margin-bottom: 12px;
}
</style>
