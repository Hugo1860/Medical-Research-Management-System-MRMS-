<template>
  <div class="page-container">
    <el-card shadow="never" class="main-card">
      <!-- 顶部标题与 KPI 摘要 -->
      <div class="header-section">
        <div>
          <h2 class="page-title">科研经费报销与票据核销台账</h2>
          <p class="page-subtitle">
            全周期追踪纵向课题、横向合同及学术会议经费报销进度，支持多科目分录核算、公务卡核销与财务凭证对账。
          </p>
        </div>
        <div class="action-buttons">
          <el-button type="primary" :icon="Plus" @click="openExpenseDialog">
            新建经费报销
          </el-button>
          <el-button :icon="Download" @click="handleExport">
            导出台账 (CSV)
          </el-button>
          <el-button :icon="Refresh" @click="loadActiveData">
            刷新
          </el-button>
        </div>
      </div>

      <!-- 核心指标统计卡片 -->
      <el-row :gutter="16" class="stat-cards">
        <el-col :xs="12" :sm="6">
          <div class="stat-card stat-blue">
            <span class="label">累计已审报销总额</span>
            <span class="val">¥{{ formatMoney(totalApprovedExpense) }}</span>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="stat-card stat-orange">
            <span class="label">在途待审报销金额</span>
            <span class="val">¥{{ formatMoney(totalPendingExpense) }}</span>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="stat-card stat-green">
            <span class="label">可用预算净结余</span>
            <span class="val">¥{{ formatMoney(totalRemainingBudget) }}</span>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="stat-card stat-purple">
            <span class="label">纳管报销项目数</span>
            <span class="val">{{ totalProjectsCount }} 个</span>
          </div>
        </el-col>
      </el-row>

      <!-- 业务分类标签页 -->
      <el-tabs v-model="activeTab" class="finance-tabs" @tab-change="onTabChange">
        <!-- 标签页 1：纵向项目经费报销 -->
        <el-tab-pane label="纵向项目经费报销" name="y-expense">
          <!-- 筛选栏 -->
          <div class="search-bar">
            <el-input
              v-model="verticalQuery.Like项目中文名称"
              placeholder="搜索项目名称、分类或级别..."
              prefix-icon="Search"
              clearable
              style="width: 280px;"
              @keyup.enter="fetchVerticalExpenses"
            />
            <el-input
              v-model="verticalQuery.Like负责人姓名"
              placeholder="负责人姓名..."
              clearable
              style="width: 160px;"
              @keyup.enter="fetchVerticalExpenses"
            />
            <el-button type="primary" :icon="Search" @click="fetchVerticalExpenses">查询</el-button>
            <el-button @click="resetVerticalQuery">重置</el-button>
          </div>

          <!-- 纵向报销汇总表格 -->
          <el-table
            v-loading="loadingVertical"
            :data="verticalList"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="项目中文名称" label="课题名称" min-width="220" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="prj-title">{{ row.项目中文名称 }}</span>
                <div class="prj-sub">
                  <el-tag size="small" type="info">{{ row.分类名称 || '纵向项目' }}</el-tag>
                  <el-tag v-if="row.项目级别" size="small" type="success" style="margin-left: 4px;">
                    {{ row.项目级别 }}
                  </el-tag>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="负责人姓名" label="项目负责人" width="130">
              <template #default="{ row }">
                <div>{{ row.负责人姓名 }}</div>
                <div style="font-size: 11px; color: #94a3b8;">{{ row.部门名称 || '-' }}</div>
              </template>
            </el-table-column>
            <el-table-column label="批准经费" width="130" align="right">
              <template #default="{ row }">
                <span>¥{{ formatMoney(row.批准经费) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="到账经费" width="130" align="right">
              <template #default="{ row }">
                <span style="color: #409eff; font-weight: 600;">¥{{ formatMoney(row.经费) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="已审报销" width="130" align="right">
              <template #default="{ row }">
                <span style="color: #67c23a; font-weight: 600;">¥{{ formatMoney(row.已审核报销) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="在途待审" width="120" align="right">
              <template #default="{ row }">
                <span style="color: #e6a23c;">¥{{ formatMoney(row.待审核报销) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="剩余经费" width="130" align="right">
              <template #default="{ row }">
                <span :style="{ color: row.剩余经费 < 0 ? '#f56c6c' : '#303133', fontWeight: 600 }">
                  ¥{{ formatMoney(row.剩余经费) }}
                </span>
              </template>
            </el-table-column>
            <el-table-column label="执行进度" min-width="140">
              <template #default="{ row }">
                <el-progress
                  :percentage="calcRate(row.已审核报销, row.批准经费 || row.经费)"
                  :status="calcRateStatus(row.已审核报销, row.批准经费 || row.经费)"
                />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="180" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openProjectDetailDrawer(row, 410)">
                  明细及凭证
                </el-button>
                <el-button link type="success" size="small" @click="handleFastClaim(row, '纵向')">
                  去报销
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <!-- 分页 -->
          <div class="pagination-wrapper">
            <el-pagination
              v-model:current-page="verticalPaging.index"
              v-model:page-size="verticalPaging.size"
              :total="verticalTotal"
              :page-sizes="[10, 20, 50, 100]"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="fetchVerticalExpenses"
              @current-change="fetchVerticalExpenses"
            />
          </div>
        </el-tab-pane>

        <!-- 标签页 2：横向项目经费报销 -->
        <el-tab-pane label="横向项目经费报销" name="x-expense">
          <div class="search-bar">
            <el-input
              v-model="horizontalQuery.Like合同名称"
              placeholder="搜索合同名称..."
              clearable
              style="width: 260px;"
              @keyup.enter="fetchHorizontalExpenses"
            />
            <el-input
              v-model="horizontalQuery.Like合同编号"
              placeholder="合同编号..."
              clearable
              style="width: 180px;"
              @keyup.enter="fetchHorizontalExpenses"
            />
            <el-input
              v-model="horizontalQuery.Like项目负责人姓名"
              placeholder="负责人姓名..."
              clearable
              style="width: 160px;"
              @keyup.enter="fetchHorizontalExpenses"
            />
            <el-button type="primary" :icon="Search" @click="fetchHorizontalExpenses">查询</el-button>
            <el-button @click="resetHorizontalQuery">重置</el-button>
          </div>

          <el-table
            v-loading="loadingHorizontal"
            :data="horizontalList"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="合同名称" label="合同/横向项目名称" min-width="220" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="prj-title">{{ row.合同名称 }}</span>
                <div class="prj-sub">
                  <el-tag size="small" type="info">{{ row.合同编号 }}</el-tag>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="项目负责人姓名" label="项目负责人" width="130">
              <template #default="{ row }">
                <div>{{ row.项目负责人姓名 }}</div>
                <div style="font-size: 11px; color: #94a3b8;">{{ row.项目负责人部门名称 || '-' }}</div>
              </template>
            </el-table-column>
            <el-table-column label="合同总金额" width="140" align="right">
              <template #default="{ row }">
                <span>¥{{ formatMoney(row.合同金额) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="累计到账" width="130" align="right">
              <template #default="{ row }">
                <span style="color: #409eff; font-weight: 600;">¥{{ formatMoney(row.经费) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="已审报销" width="130" align="right">
              <template #default="{ row }">
                <span style="color: #67c23a; font-weight: 600;">¥{{ formatMoney(row.已审核报销) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="在途待审" width="120" align="right">
              <template #default="{ row }">
                <span style="color: #e6a23c;">¥{{ formatMoney(row.待审核报销) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="剩余经费" width="130" align="right">
              <template #default="{ row }">
                <span :style="{ color: row.剩余经费 < 0 ? '#f56c6c' : '#303133', fontWeight: 600 }">
                  ¥{{ formatMoney(row.剩余经费) }}
                </span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="180" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openProjectDetailDrawer(row, 450)">
                  明细及凭证
                </el-button>
                <el-button link type="success" size="small" @click="handleFastClaim(row, '横向')">
                  去报销
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-wrapper">
            <el-pagination
              v-model:current-page="horizontalPaging.index"
              v-model:page-size="horizontalPaging.size"
              :total="horizontalTotal"
              :page-sizes="[10, 20, 50, 100]"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="fetchHorizontalExpenses"
              @current-change="fetchHorizontalExpenses"
            />
          </div>
        </el-tab-pane>

        <!-- 标签页 3：学术会议报销 -->
        <el-tab-pane label="学术会议费用报销" name="conference">
          <div class="search-bar">
            <el-input
              v-model="conferenceQuery.Like会议名称"
              placeholder="搜索学术会议名称..."
              clearable
              style="width: 280px;"
              @keyup.enter="fetchConferenceExpenses"
            />
            <el-input
              v-model="conferenceQuery.Like申请人姓名"
              placeholder="申请人姓名..."
              clearable
              style="width: 160px;"
              @keyup.enter="fetchConferenceExpenses"
            />
            <el-button type="primary" :icon="Search" @click="fetchConferenceExpenses">查询</el-button>
          </div>

          <el-table
            v-loading="loadingConference"
            :data="conferenceList"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="会议名称" label="学术会议名称" min-width="240" />
            <el-table-column prop="申请人姓名" label="申请人" width="140">
              <template #default="{ row }">
                <div>{{ row.申请人姓名 }}</div>
                <div style="font-size: 11px; color: #94a3b8;">{{ row.部门名称 || '-' }}</div>
              </template>
            </el-table-column>
            <el-table-column label="报销总金额" width="160" align="right">
              <template #default="{ row }">
                <span style="color: #e6a23c; font-weight: 600;">¥{{ formatMoney(row.报销总金额) }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="报销时间" label="申报时间" width="160" />
            <el-table-column label="审批状态" width="130" align="center">
              <template #default="{ row }">
                <el-tag :type="getStatusTagType(row.状态名称)">
                  {{ row.状态名称 || '已办结' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="140" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openProjectDetailDrawer(row, 210)">
                  查看凭证
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-wrapper">
            <el-pagination
              v-model:current-page="conferencePaging.index"
              v-model:page-size="conferencePaging.size"
              :total="conferenceTotal"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="fetchConferenceExpenses"
              @current-change="fetchConferenceExpenses"
            />
          </div>
        </el-tab-pane>

        <!-- 标签页 4：经费单据查找与财务核销 -->
        <el-tab-pane label="单据查找与发票核销" name="bills">
          <div class="search-bar">
            <el-input
              v-model="billSearchKeyword"
              placeholder="输入单据号、项目名称或发票备注..."
              clearable
              style="width: 320px;"
              @keyup.enter="handleBillSearch"
            />
            <el-button type="primary" :icon="Search" @click="handleBillSearch">检索单据</el-button>
            <el-button type="warning" :icon="Upload" @click="openSyncDialog">
              录入/同步外部财务凭证
            </el-button>
          </div>

          <el-table
            v-loading="loadingVertical"
            :data="filteredBillList"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="项目中文名称" label="关联课题/合同" min-width="220" />
            <el-table-column prop="负责人姓名" label="经办人/负责人" width="140" />
            <el-table-column label="批准经费" width="130" align="right">
              <template #default="{ row }">
                <span>¥{{ formatMoney(row.批准经费) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="累计报销支出" width="140" align="right">
              <template #default="{ row }">
                <span style="color: #67c23a; font-weight: 600;">¥{{ formatMoney(row.已审核报销) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="待核销金额" width="130" align="right">
              <template #default="{ row }">
                <span style="color: #e6a23c;">¥{{ formatMoney(row.待审核报销) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="结余净额" width="130" align="right">
              <template #default="{ row }">
                <span style="font-weight: 600;">¥{{ formatMoney(row.剩余经费) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="140" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openProjectDetailDrawer(row, 410)">
                  调阅单据档案
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 申报经费报销弹窗 -->
    <el-dialog
      v-model="expenseDialogVisible"
      :title="`新建${claimForm.projectType}经费报销申请`"
      width="780px"
      destroy-on-close
    >
      <el-form :model="claimForm" label-width="110px" style="padding: 0 10px;">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="关联项目" required>
              <el-select
                v-model="claimForm.项目编号"
                placeholder="请选择报销依托项目"
                filterable
                style="width: 100%;"
                @change="onProjectSelect"
              >
                <template v-if="claimForm.projectType === '纵向'">
                  <el-option
                    v-for="item in verticalList"
                    :key="item.编号"
                    :label="item.项目中文名称"
                    :value="item.编号"
                  />
                </template>
                <template v-else>
                  <el-option
                    v-for="item in horizontalList"
                    :key="item.编号"
                    :label="item.合同名称"
                    :value="item.编号"
                  />
                </template>
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="报销方式" required>
              <el-select v-model="claimForm.报销方式" placeholder="选择支付核销方式" style="width: 100%;">
                <el-option label="公务卡结算" value="公务卡" />
                <el-option label="银行对公转账" value="对公转账" />
                <el-option label="个人垫付现金报销" value="现金" />
                <el-option label="支票汇票支付" value="支票" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row v-if="claimForm.报销方式 === '公务卡'" :gutter="16">
          <el-col :span="12">
            <el-form-item label="公务卡卡号" required>
              <el-input v-model="claimForm.公务卡号" placeholder="622848..." />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="持卡人姓名" required>
              <el-input v-model="claimForm.公务卡所属人姓名" placeholder="姓名" />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 支出科目分录动态表格 -->
        <div class="expense-items-header">
          <span style="font-weight: 600; font-size: 14px;">报销支出科目分录细项</span>
          <el-button type="primary" link :icon="Plus" size="small" @click="addExpenseItem">
            添加一行支出科目
          </el-button>
        </div>

        <el-table :data="claimForm.details" border size="small" style="width: 100%; margin-bottom: 16px;">
          <el-table-column label="支出大类" width="140">
            <template #default="{ row }">
              <el-select v-model="row.项目支出类型" placeholder="类别">
                <el-option label="直接经费" value="直接经费" />
                <el-option label="间接经费" value="间接经费" />
                <el-option label="外协经费" value="外协经费" />
              </el-select>
            </template>
          </el-table-column>
          <el-table-column label="支出科目内容" min-width="160">
            <template #default="{ row }">
              <el-select v-model="row.项目支出内容" placeholder="科目" filterable allow-create>
                <el-option label="材料与耗材试剂费" value="材料与耗材试剂费" />
                <el-option label="测试化验与加工检验费" value="测试化验与加工检验费" />
                <el-option label="差旅与学术会议费" value="差旅与学术会议费" />
                <el-option label="出版/文献/信息传播/知识产权事务费" value="出版/文献/信息传播费" />
                <el-option label="劳务费与学生助研津贴" value="劳务费与助研津贴" />
                <el-option label="专家咨询费与评审费" value="专家咨询费" />
                <el-option label="仪器设备购置与改装费" value="仪器设备购置费" />
                <el-option label="管理费与绩效支出" value="绩效支出" />
              </el-select>
            </template>
          </el-table-column>
          <el-table-column label="报销金额 (元)" width="150">
            <template #default="{ row }">
              <el-input-number
                v-model="row.报销金额"
                :min="1"
                :step="100"
                controls-position="right"
                style="width: 100%;"
              />
            </template>
          </el-table-column>
          <el-table-column label="发票凭证与用途说明" min-width="150">
            <template #default="{ row }">
              <el-input v-model="row.备注" placeholder="发票编号/采购用途" />
            </template>
          </el-table-column>
          <el-table-column label="操作" width="60" align="center">
            <template #default="{ $index }">
              <el-button
                link
                type="danger"
                :icon="Delete"
                :disabled="claimForm.details.length <= 1"
                @click="removeExpenseItem($index)"
              />
            </template>
          </el-table-column>
        </el-table>

        <div class="expense-sum-row">
          <span>本次申报报销总额：</span>
          <span class="sum-val">¥{{ formatMoney(calcClaimSum) }}</span>
        </div>

        <el-form-item label="发票附件说明">
          <el-input
            v-model="claimForm.相关文件路径"
            placeholder="填写电子发票凭证路径或打包上传文件名"
          />
        </el-form-item>
        <el-form-item label="报销综合备注">
          <el-input
            v-model="claimForm.备注"
            type="textarea"
            rows="2"
            placeholder="补充报销事由、同行人员、单据张数等信息"
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="expenseDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="submitExpenseClaim">
          提交审核
        </el-button>
      </template>
    </el-dialog>

    <!-- 项目报销历史与凭证档案抽屉 -->
    <el-drawer
      v-model="drawerVisible"
      title="项目经费报销明细与凭证档案"
      size="650px"
      destroy-on-close
    >
      <div v-if="selectedProject" class="drawer-content">
        <div class="project-brief">
          <h3>{{ selectedProject.项目中文名称 || selectedProject.合同名称 || selectedProject.会议名称 }}</h3>
          <p class="meta">
            负责人：{{ selectedProject.负责人姓名 || selectedProject.项目负责人姓名 || selectedProject.申请人姓名 }}
            &nbsp;|&nbsp; 批准/合同经费：¥{{ formatMoney(selectedProject.批准经费 || selectedProject.合同金额 || 0) }}
            &nbsp;|&nbsp; 剩余额度：¥{{ formatMoney(selectedProject.剩余经费 || 0) }}
          </p>
        </div>

        <!-- 科目预算执行对比 -->
        <h4 style="margin: 20px 0 10px 0; font-size: 14px;">预算科目执行情况</h4>
        <el-table :data="budgetSpentList" border size="small" style="width: 100%;">
          <el-table-column prop="项目支出类型" label="支出大类" width="110" />
          <el-table-column prop="项目支出内容" label="科目内容" min-width="140" />
          <el-table-column label="已报销支出" width="120" align="right">
            <template #default="{ row }">
              <span style="color: #67c23a; font-weight: 600;">¥{{ formatMoney(row.已报销金额) }}</span>
            </template>
          </el-table-column>
        </el-table>

        <!-- 历次报销记录 -->
        <h4 style="margin: 24px 0 10px 0; font-size: 14px;">历次申报报销单据流</h4>
        <el-table :data="recordList" stripe border size="small" style="width: 100%;">
          <el-table-column prop="报销编号" label="单据号" width="130" />
          <el-table-column prop="报销方式" label="支付方式" width="90" />
          <el-table-column label="报销金额" width="110" align="right">
            <template #default="{ row }">
              <span style="color: #e6a23c; font-weight: 600;">¥{{ formatMoney(row.报销金额) }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="报销时间" label="申报时间" width="110" />
          <el-table-column label="操作" width="80" align="center">
            <template #default="{ row }">
              <el-button link type="primary" size="small" @click="viewSingleExpenseDetail(row.编号)">
                分录
              </el-button>
            </template>
          </el-table-column>
        </el-table>

        <!-- 单笔报销支出细项清单 -->
        <div v-if="singleExpenseDetails.length" style="margin-top: 20px;">
          <h4 style="font-size: 14px; color: #409eff;">选中单据支出分录</h4>
          <el-table :data="singleExpenseDetails" border size="small" style="width: 100%;">
            <el-table-column prop="项目支出类型" label="类别" width="100" />
            <el-table-column prop="项目支出内容" label="支出科目" min-width="140" />
            <el-table-column label="金额" width="110" align="right">
              <template #default="{ row }">
                <span>¥{{ formatMoney(row.报销金额) }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="备注" label="用途/发票" min-width="130" show-overflow-tooltip />
          </el-table>
        </div>
      </div>
    </el-drawer>

    <!-- 财务凭证同步弹窗 -->
    <el-dialog v-model="syncDialogVisible" title="录入 / 同步外部财务流水凭证" width="560px">
      <el-form :model="syncForm" label-width="110px">
        <el-form-item label="外部财务系统" required>
          <el-select v-model="syncForm.外部系统编号" style="width: 100%;">
            <el-option label="医院综合财务核算系统 (NC-ERP)" :value="1" />
            <el-option label="科研资金银医直联系统 (BOC)" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item label="同步批次号" required>
          <el-input v-model="syncForm.同步批次号" placeholder="BATCH-20261015-01" />
        </el-form-item>
        <el-form-item label="财务单据号" required>
          <el-input v-model="syncSingleBill.外部单据号" placeholder="NC-BILL-88902" />
        </el-form-item>
        <el-form-item label="业务类型" required>
          <el-select v-model="syncSingleBill.业务类型" style="width: 100%;">
            <el-option label="实际付款" value="实际付款" />
            <el-option label="付款申请" value="付款申请" />
            <el-option label="冲销" value="冲销" />
          </el-select>
        </el-form-item>
        <el-form-item label="金额(元)" required>
          <el-input-number v-model="syncSingleBill.金额" :min="1" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="收款方">
          <el-input v-model="syncSingleBill.收款方" placeholder="供应商或报销人" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="syncDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="syncLoading" @click="handleSyncSubmit">
          确认同步
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Plus, Download, Refresh, Search, Delete, Upload } from '@element-plus/icons-vue'
import {
  getVerticalExpenses,
  getHorizontalExpenses,
  getConferenceExpenses,
  getProjectVerticalExpenseList,
  getProjectHorizontalExpenseList,
  getProjectConferenceExpenseList,
  getExpenseDetail,
  getProjectVerticalBudgetSpent,
  getProjectHorizontalBudgetSpent,
  addVerticalExpense,
  addHorizontalExpense,
  syncFinancialFlow,
  type VerticalExpenseSummaryItem,
  type HorizontalExpenseSummaryItem,
  type ConferenceExpenseSummaryItem,
  type ExpenseRecordItem,
  type ExpenseDetailItem,
  type BudgetSpentDetailItem
} from '@/api/finance'
import { exportToCsv } from '@/utils/export'

const route = useRoute()

// 标签页状态
const activeTab = ref('y-expense')

// 纵向项目状态
const loadingVertical = ref(false)
const verticalList = ref<VerticalExpenseSummaryItem[]>([])
const verticalTotal = ref(0)
const verticalPaging = reactive({ index: 1, size: 10 })
const verticalQuery = reactive({ Like项目中文名称: '', Like负责人姓名: '' })

// 横向项目状态
const loadingHorizontal = ref(false)
const horizontalList = ref<HorizontalExpenseSummaryItem[]>([])
const horizontalTotal = ref(0)
const horizontalPaging = reactive({ index: 1, size: 10 })
const horizontalQuery = reactive({ Like合同名称: '', Like合同编号: '', Like项目负责人姓名: '' })

// 会议报销状态
const loadingConference = ref(false)
const conferenceList = ref<ConferenceExpenseSummaryItem[]>([])
const conferenceTotal = ref(0)
const conferencePaging = reactive({ index: 1, size: 10 })
const conferenceQuery = reactive({ Like会议名称: '', Like申请人姓名: '' })

// 单据查找关键字
const billSearchKeyword = ref('')

// 抽屉详情状态
const drawerVisible = ref(false)
const selectedProject = ref<any>(null)
const budgetSpentList = ref<BudgetSpentDetailItem[]>([])
const recordList = ref<ExpenseRecordItem[]>([])
const singleExpenseDetails = ref<ExpenseDetailItem[]>([])

// 申报弹窗状态
const expenseDialogVisible = ref(false)
const submitLoading = ref(false)
const claimForm = reactive({
  projectType: '纵向',
  项目编号: 0,
  报销方式: '公务卡',
  公务卡号: '',
  公务卡所属人姓名: '',
  相关文件路径: '',
  备注: '',
  details: [
    { 项目支出类型: '直接经费', 项目支出内容: '材料与耗材试剂费', 报销金额: 1500, 备注: '' }
  ]
})

// 财务凭证同步状态
const syncDialogVisible = ref(false)
const syncLoading = ref(false)
const syncForm = reactive({
  外部系统编号: 1,
  同步批次号: `BATCH-${new Date().toISOString().slice(0, 10).replace(/-/g, '')}-01`
})
const syncSingleBill = reactive({
  外部单据号: '',
  业务类型: '实际付款',
  金额: 5000,
  收款方: ''
})

// 顶部 KPI 统计计算
const totalApprovedExpense = computed(() => {
  return verticalList.value.reduce((acc, cur) => acc + (cur.已审核报销 || 0), 0) +
         horizontalList.value.reduce((acc, cur) => acc + (cur.已审核报销 || 0), 0)
})

const totalPendingExpense = computed(() => {
  return verticalList.value.reduce((acc, cur) => acc + (cur.待审核报销 || 0), 0) +
         horizontalList.value.reduce((acc, cur) => acc + (cur.待审核报销 || 0), 0)
})

const totalRemainingBudget = computed(() => {
  return verticalList.value.reduce((acc, cur) => acc + (cur.剩余经费 || 0), 0) +
         horizontalList.value.reduce((acc, cur) => acc + (cur.剩余经费 || 0), 0)
})

const totalProjectsCount = computed(() => {
  return verticalTotal.value + horizontalTotal.value
})

const calcClaimSum = computed(() => {
  return claimForm.details.reduce((acc, cur) => acc + (cur.报销金额 || 0), 0)
})

// 单据过滤
const filteredBillList = computed(() => {
  if (!billSearchKeyword.value.trim()) {
    return verticalList.value
  }
  const kw = billSearchKeyword.value.trim().toLowerCase()
  return verticalList.value.filter(item =>
    (item.项目中文名称 && item.项目中文名称.toLowerCase().includes(kw)) ||
    (item.负责人姓名 && item.负责人姓名.toLowerCase().includes(kw))
  )
})

function formatMoney(val?: number) {
  if (val === null || val === undefined) return '0.00'
  return val.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function calcRate(spent?: number, total?: number) {
  if (!total || total <= 0) return 0
  const rate = Math.round(((spent || 0) / total) * 100)
  return Math.min(rate, 100)
}

function calcRateStatus(spent?: number, total?: number) {
  const rate = calcRate(spent, total)
  if (rate >= 95) return 'exception'
  if (rate >= 75) return 'warning'
  return 'success'
}

function getStatusTagType(status?: string) {
  if (status === '已办结' || status === '已审核') return 'success'
  if (status === '待审核' || status === '审核中') return 'warning'
  if (status === '已驳回') return 'danger'
  return 'info'
}

// 获取纵向报销
async function fetchVerticalExpenses() {
  loadingVertical.value = true
  try {
    const res = await getVerticalExpenses(verticalPaging, verticalQuery)
    verticalList.value = res.list || []
    verticalTotal.value = res.total || 0
  } catch (error) {
    console.error('获取纵向经费报销失败:', error)
  } finally {
    loadingVertical.value = false
  }
}

function resetVerticalQuery() {
  verticalQuery.Like项目中文名称 = ''
  verticalQuery.Like负责人姓名 = ''
  verticalPaging.index = 1
  fetchVerticalExpenses()
}

// 获取横向报销
async function fetchHorizontalExpenses() {
  loadingHorizontal.value = true
  try {
    const res = await getHorizontalExpenses(horizontalPaging, horizontalQuery)
    horizontalList.value = res.list || []
    horizontalTotal.value = res.total || 0
  } catch (error) {
    console.error('获取横向经费报销失败:', error)
  } finally {
    loadingHorizontal.value = false
  }
}

function resetHorizontalQuery() {
  horizontalQuery.Like合同名称 = ''
  horizontalQuery.Like合同编号 = ''
  horizontalQuery.Like项目负责人姓名 = ''
  horizontalPaging.index = 1
  fetchHorizontalExpenses()
}

// 获取会议报销
async function fetchConferenceExpenses() {
  loadingConference.value = true
  try {
    const res = await getConferenceExpenses(conferencePaging, conferenceQuery)
    conferenceList.value = res.list || []
    conferenceTotal.value = res.total || 0
  } catch (error) {
    console.error('获取会议报销失败:', error)
  } finally {
    loadingConference.value = false
  }
}

function onTabChange(tabName: any) {
  if (tabName === 'y-expense' && !verticalList.value.length) {
    fetchVerticalExpenses()
  } else if (tabName === 'x-expense' && !horizontalList.value.length) {
    fetchHorizontalExpenses()
  } else if (tabName === 'conference' && !conferenceList.value.length) {
    fetchConferenceExpenses()
  } else if (tabName === 'bills' && !verticalList.value.length) {
    fetchVerticalExpenses()
  }
}

function loadActiveData() {
  if (activeTab.value === 'y-expense' || activeTab.value === 'bills') {
    fetchVerticalExpenses()
  } else if (activeTab.value === 'x-expense') {
    fetchHorizontalExpenses()
  } else if (activeTab.value === 'conference') {
    fetchConferenceExpenses()
  }
}

function handleBillSearch() {
  if (billSearchKeyword.value.trim()) {
    ElMessage.info(`正在按关键词“${billSearchKeyword.value}”查找单据...`)
  }
}

// 抽屉详情
async function openProjectDetailDrawer(row: any, flowId: number) {
  selectedProject.value = row
  drawerVisible.value = true
  singleExpenseDetails.value = []

  try {
    if (flowId === 410) {
      const [spentRes, recordsRes] = await Promise.all([
        getProjectVerticalBudgetSpent(row.编号),
        getProjectVerticalExpenseList(row.编号)
      ])
      budgetSpentList.value = spentRes || []
      recordList.value = recordsRes || []
    } else if (flowId === 450) {
      const [spentRes, recordsRes] = await Promise.all([
        getProjectHorizontalBudgetSpent(row.编号),
        getProjectHorizontalExpenseList(row.编号)
      ])
      budgetSpentList.value = spentRes || []
      recordList.value = recordsRes || []
    } else {
      const recordsRes = await getProjectConferenceExpenseList(row.编号)
      recordList.value = recordsRes || []
      budgetSpentList.value = []
    }
  } catch (error) {
    console.error('获取项目报销详情档案失败:', error)
  }
}

async function viewSingleExpenseDetail(expenseId: number) {
  try {
    const res = await getExpenseDetail(expenseId)
    singleExpenseDetails.value = res || []
  } catch (error) {
    console.error('获取单据支出细项失败:', error)
  }
}

// 快速去报销
function handleFastClaim(row: any, type: string) {
  claimForm.projectType = type
  claimForm.项目编号 = row.编号
  claimForm.报销方式 = '公务卡'
  claimForm.公务卡号 = ''
  claimForm.公务卡所属人姓名 = row.负责人姓名 || row.项目负责人姓名 || ''
  claimForm.details = [
    { 项目支出类型: '直接经费', 项目支出内容: '材料与耗材试剂费', 报销金额: 1000, 备注: '' }
  ]
  expenseDialogVisible.value = true
}

function openExpenseDialog() {
  claimForm.projectType = activeTab.value === 'x-expense' ? '横向' : '纵向'
  claimForm.项目编号 = 0
  claimForm.details = [
    { 项目支出类型: '直接经费', 项目支出内容: '材料与耗材试剂费', 报销金额: 1000, 备注: '' }
  ]
  expenseDialogVisible.value = true
}

function onProjectSelect(id: number) {
  if (claimForm.projectType === '纵向') {
    const p = verticalList.value.find(item => item.编号 === id)
    if (p) claimForm.公务卡所属人姓名 = p.负责人姓名
  } else {
    const p = horizontalList.value.find(item => item.编号 === id)
    if (p) claimForm.公务卡所属人姓名 = p.项目负责人姓名
  }
}

function addExpenseItem() {
  claimForm.details.push({
    项目支出类型: '直接经费',
    项目支出内容: '差旅与学术会议费',
    报销金额: 500,
    备注: ''
  })
}

function removeExpenseItem(index: number) {
  claimForm.details.splice(index, 1)
}

async function submitExpenseClaim() {
  if (!claimForm.项目编号) {
    ElMessage.warning('请选择关联项目')
    return
  }
  if (!calcClaimSum.value || calcClaimSum.value <= 0) {
    ElMessage.warning('报销总金额必须大于0')
    return
  }

  submitLoading.value = true
  try {
    const payload = {
      基本资料: {
        项目编号: claimForm.项目编号,
        报销金额: calcClaimSum.value,
        报销方式: claimForm.报销方式,
        公务卡号: claimForm.报销方式 === '公务卡' ? claimForm.公务卡号 : undefined,
        公务卡所属人姓名: claimForm.报销方式 === '公务卡' ? claimForm.公务卡所属人姓名 : undefined,
        相关文件路径: claimForm.相关文件路径,
        备注: claimForm.备注
      },
      报销详情列表: claimForm.details
    }

    if (claimForm.projectType === '纵向') {
      await addVerticalExpense(payload)
    } else {
      await addHorizontalExpense(payload)
    }

    ElMessage.success('经费报销申请已成功提交审核')
    expenseDialogVisible.value = false
    loadActiveData()
  } catch (error: any) {
    ElMessage.error(error.message || '提交报销申请失败')
  } finally {
    submitLoading.value = false
  }
}

// 财务凭证同步
function openSyncDialog() {
  syncDialogVisible.value = true
}

async function handleSyncSubmit() {
  if (!syncSingleBill.外部单据号) {
    ElMessage.warning('请输入外部单据号')
    return
  }
  syncLoading.value = true
  try {
    await syncFinancialFlow({
      外部系统编号: syncForm.外部系统编号,
      同步批次号: syncForm.同步批次号,
      财务单据列表: [
        {
          外部单据号: syncSingleBill.外部单据号,
          流程模板编号: 410,
          项目编号: verticalList.value[0]?.编号 || 1,
          业务类型: syncSingleBill.业务类型,
          金额: syncSingleBill.金额,
          收款方: syncSingleBill.收款方
        }
      ]
    })
    ElMessage.success('财务流水同步成功')
    syncDialogVisible.value = false
    loadActiveData()
  } catch (error: any) {
    ElMessage.error(error.message || '凭证同步失败')
  } finally {
    syncLoading.value = false
  }
}

// 导出 CSV
function handleExport() {
  try {
    if (activeTab.value === 'y-expense' || activeTab.value === 'bills') {
      exportToCsv(
        '纵向科研项目经费报销台账',
        [
          { label: '课题名称', prop: '项目中文名称' },
          { label: '项目分类', prop: '分类名称' },
          { label: '项目级别', prop: '项目级别' },
          { label: '负责人', prop: '负责人姓名' },
          { label: '归属科室', prop: '部门名称' },
          { label: '批准总经费', prop: '批准经费' },
          { label: '到账经费', prop: '经费' },
          { label: '已审核报销', prop: '已审核报销' },
          { label: '在途待审报销', prop: '待审核报销' },
          { label: '剩余结余净额', prop: '剩余经费' }
        ],
        verticalList.value
      )
    } else if (activeTab.value === 'x-expense') {
      exportToCsv(
        '横向科研项目经费报销台账',
        [
          { label: '合同名称', prop: '合同名称' },
          { label: '合同编号', prop: '合同编号' },
          { label: '负责人', prop: '项目负责人姓名' },
          { label: '部门', prop: '项目负责人部门名称' },
          { label: '合同金额', prop: '合同金额' },
          { label: '累计到账', prop: '经费' },
          { label: '已审核报销', prop: '已审核报销' },
          { label: '待审核报销', prop: '待审核报销' },
          { label: '剩余经费', prop: '剩余经费' }
        ],
        horizontalList.value
      )
    } else {
      exportToCsv(
        '学术会议费用报销台账',
        [
          { label: '会议名称', prop: '会议名称' },
          { label: '申请人', prop: '申请人姓名' },
          { label: '部门', prop: '部门名称' },
          { label: '报销总金额', prop: '报销总金额' },
          { label: '申报时间', prop: '报销时间' },
          { label: '审批状态', prop: '状态名称' }
        ],
        conferenceList.value
      )
    }
    ElMessage.success('台账报表导出成功')
  } catch (error: any) {
    ElMessage.warning(error.message || '导出失败')
  }
}

onMounted(() => {
  // 检查路由 query 中的 tab 参数
  const queryTab = route.query.tab as string
  if (queryTab && ['y-expense', 'x-expense', 'conference', 'bills'].includes(queryTab)) {
    activeTab.value = queryTab
  }
  fetchVerticalExpenses()
  fetchHorizontalExpenses()
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

    &.stat-orange {
      background: #fff7ed;
      color: #c2410c;
    }

    &.stat-green {
      background: #f0fdf4;
      color: #15803d;
    }

    &.stat-purple {
      background: #faf5ff;
      color: #7e22ce;
    }
  }
}

.finance-tabs {
  margin-top: 10px;
}

.search-bar {
  display: flex;
  gap: 10px;
  align-items: center;
  flex-wrap: wrap;
  margin-top: 8px;
}

.prj-title {
  font-weight: 600;
  color: #1e293b;
  display: block;
}

.prj-sub {
  margin-top: 4px;
}

.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}

.expense-items-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin: 16px 0 8px 0;
}

.expense-sum-row {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  font-size: 14px;
  color: #64748b;
  margin-bottom: 16px;

  .sum-val {
    font-size: 20px;
    font-weight: 700;
    color: #e6a23c;
    margin-left: 8px;
  }
}

.drawer-content {
  .project-brief {
    background: #f8fafc;
    padding: 14px 16px;
    border-radius: 6px;

    h3 {
      margin: 0 0 6px 0;
      font-size: 16px;
      color: #0f172a;
    }

    .meta {
      margin: 0;
      font-size: 12px;
      color: #64748b;
    }
  }
}
</style>
