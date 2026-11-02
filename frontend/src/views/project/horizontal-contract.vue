<template>
  <div class="horizontal-contract-container">
    <el-card shadow="never" class="header-card">
      <div class="page-header">
        <div>
          <h2>横向科研合同与产学研成果转化全周期</h2>
          <p class="subtitle">
            支持横向合同全流程闭环管理：企业委托研发、技术转让、产学研协同、合同变更追踪及结题归档。
          </p>
        </div>
        <div class="header-actions">
          <el-button type="primary" :icon="Plus" @click="handleOpenApplyDialog">
            新增横向合同申请
          </el-button>
        </div>
      </div>

      <el-row :gutter="16" class="statistic-row">
        <el-col :span="6">
          <div class="stat-box">
            <span class="label">进行中合同总数</span>
            <span class="value">{{ ongoingCount }} <small>项</small></span>
          </div>
        </el-col>
        <el-col :span="6">
          <div class="stat-box">
            <span class="label">合同总金额 (登记)</span>
            <span class="value money">¥ {{ formatMoney(totalContractAmount) }}</span>
          </div>
        </el-col>
        <el-col :span="6">
          <div class="stat-box">
            <span class="label">申请技术合同认定</span>
            <span class="value">{{ certifiedCount }} <small>项</small></span>
          </div>
        </el-col>
        <el-col :span="6">
          <div class="stat-box">
            <span class="label">已办结与归档</span>
            <span class="value">{{ closureList.length }} <small>项</small></span>
          </div>
        </el-col>
      </el-row>
    </el-card>

    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab" class="contract-tabs">
        <!-- 标签页 1：横向项目列表 -->
        <el-tab-pane label="横向项目台账" name="projects">
          <!-- 检索表单 -->
          <el-form :inline="true" :model="queryForm" class="filter-form">
            <el-form-item label="合同编号">
              <el-input v-model="queryForm.like合同编号" placeholder="请输入合同编号" clearable />
            </el-form-item>
            <el-form-item label="合同名称">
              <el-input v-model="queryForm.like合同名称" placeholder="请输入合同名称" clearable />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
              <el-button :icon="Refresh" @click="handleReset">重置</el-button>
            </el-form-item>
          </el-form>

          <!-- 列表表格 -->
          <el-table v-loading="loading" :data="projectList" border stripe style="width: 100%">
            <el-table-column prop="合同编号" label="合同编号" width="160" />
            <el-table-column prop="合同名称" label="合同名称" min-width="200" show-overflow-tooltip />
            <el-table-column prop="合作方单位名称" label="合作方单位" min-width="180" show-overflow-tooltip />
            <el-table-column prop="合同金额" label="合同金额 (元)" width="150" align="right">
              <template #default="{ row }">
                <span class="money-text">¥ {{ formatMoney(row.合同金额) }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="签订时间" label="签订日期" width="120">
              <template #default="{ row }">
                {{ formatDate(row.签订时间) }}
              </template>
            </el-table-column>
            <el-table-column prop="是否申请技术合同认定" label="技术合同认定" width="130" align="center">
              <template #default="{ row }">
                <el-tag :type="row.是否申请技术合同认定 ? 'success' : 'info'" size="small">
                  {{ row.是否申请技术合同认定 ? '是' : '否' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="合作方式" label="合作方式" width="130">
              <template #default="{ row }">
                <el-tag type="warning" size="small">{{ row.合作方式 || '未提供' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="180" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="handleViewDetail(row)">
                  详情
                </el-button>
                <el-button link type="warning" size="small" @click="handleApplyChange(row)">
                  申请变更
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <!-- 分页器 -->
          <div class="pagination-container">
            <el-pagination
              v-model:current-page="paging.pageIndex"
              v-model:page-size="paging.pageSize"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              :total="totalCount"
              @size-change="fetchProjects"
              @current-change="fetchProjects"
            />
          </div>
        </el-tab-pane>

        <!-- 标签页 2：横向项目变更追踪 -->
        <el-tab-pane label="合同变更留痕" name="changes">
          <el-table v-loading="changeLoading" :data="changeList" border stripe style="width: 100%">
            <el-table-column prop="合同编号" label="合同编号" width="160" />
            <el-table-column prop="合同名称" label="合同名称" min-width="200" show-overflow-tooltip />
            <el-table-column prop="变更事由" label="变更事由" min-width="240" show-overflow-tooltip />
            <el-table-column prop="变更时间" label="变更发起时间" width="160">
              <template #default="{ row }">
                {{ formatDateTime(row.变更时间) }}
              </template>
            </el-table-column>
            <el-table-column label="变更审核状态" width="120" align="center">
              <template #default="{ row }">
                <el-tag :type="getStatusTag(row.状态)" size="small">
                  {{ getStatusText(row.状态) }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="120" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="handleViewChangeDetail(row)">
                  变更详情
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签页 3：结题与成果验收 -->
        <el-tab-pane label="项目结题归档" name="closures">
          <el-table v-loading="closureLoading" :data="closureList" border stripe style="width: 100%">
            <el-table-column prop="合同编号" label="合同编号" width="160" />
            <el-table-column prop="合同名称" label="合同名称" min-width="200" show-overflow-tooltip />
            <el-table-column prop="结题时间" label="申请结题日期" width="130">
              <template #default="{ row }">
                {{ formatDate(row.结题时间) }}
              </template>
            </el-table-column>
            <el-table-column prop="验收结果" label="验收结论" min-width="180">
              <template #default="{ row }">
                <el-tag v-if="row.验收结果" type="success" size="small">{{ row.验收结果 }}</el-tag>
                <span v-else class="empty-state">未提供验收结论</span>
              </template>
            </el-table-column>
            <el-table-column label="归档留存" width="120" align="center">
              <template #default="{ row }">
                <el-tag v-if="row.归档状态" type="info" size="small">{{ row.归档状态 }}</el-tag>
                <span v-else class="empty-state">未提供档案状态</span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="120" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="handleViewClosureDetail(row)">
                  结题详情
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签页 4：合同治理 -->
        <el-tab-pane label="合同治理" name="governance">
          <el-form :inline="true" class="filter-form">
            <el-form-item label="选择项目">
              <el-select v-model="governanceProjectId" filterable clearable placeholder="选择横向项目" style="width: 360px">
                <el-option v-for="item in projectList" :key="item.编号" :label="`${item.合同编号 || item.编号} · ${item.合同名称 || ''}`" :value="item.编号" />
              </el-select>
            </el-form-item>
            <el-form-item><el-button type="primary" :disabled="!governanceProjectId" @click="fetchGovernance">查询治理记录</el-button></el-form-item>
          </el-form>
          <el-alert title="签审、合作方资质和收入认定状态均来自数据库；未取得正式供应商回执时不会显示为已完成。" type="info" :closable="false" show-icon />
          <el-divider content-position="left">合同版本与签审</el-divider>
          <el-table :data="governance.合同版本" border stripe>
            <el-table-column prop="版本号" label="版本" width="80" />
            <el-table-column prop="版本类型" label="类型" width="110" />
            <el-table-column prop="签审状态" label="签审状态" width="110" />
            <el-table-column prop="文件校验和" label="校验和" min-width="180" show-overflow-tooltip />
            <el-table-column prop="变更摘要" label="变更摘要" min-width="220" show-overflow-tooltip />
          </el-table>
          <el-divider content-position="left">合作方资质</el-divider>
          <el-table :data="governance.合作方资质" border stripe>
            <el-table-column prop="单位名称" label="单位名称" min-width="180" />
            <el-table-column prop="资质类型" label="资质类型" width="140" />
            <el-table-column prop="有效期至" label="有效期至" width="120" />
            <el-table-column prop="核验状态" label="核验状态" width="110" />
          </el-table>
          <el-divider content-position="left">收入/技术合同认定</el-divider>
          <el-table :data="governance.收入认定" border stripe>
            <el-table-column prop="认定类型" label="认定类型" width="140" />
            <el-table-column prop="申请金额" label="申请金额" width="130" />
            <el-table-column prop="认定金额" label="认定金额" width="130" />
            <el-table-column prop="状态" label="状态" width="110" />
            <el-table-column prop="认定编号" label="认定编号" min-width="160" />
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 详情全周期抽屉 -->
    <el-drawer v-model="detailDrawerVisible" title="横向科研项目与合同全周期详情" size="820px" destroy-on-close>
      <div v-loading="detailLoading" class="project-detail">
        <template v-if="currentProject">
          <el-tabs v-model="detailActiveTab">
            <!-- 基本资料 -->
            <el-tab-pane label="基本资料" name="basic">
              <el-descriptions title="合同核心信息" :column="2" border>
                <el-descriptions-item label="合同编号">{{ currentProject.合同编号 }}</el-descriptions-item>
                <el-descriptions-item label="合同名称">{{ currentProject.合同名称 }}</el-descriptions-item>
                <el-descriptions-item label="项目负责人">{{ projectDetailData?.基本资料?.项目负责人姓名 || currentProject.项目负责人姓名 || '—' }}</el-descriptions-item>
                <el-descriptions-item label="负责人工号">{{ projectDetailData?.基本资料?.项目负责人工号 || '—' }}</el-descriptions-item>
                <el-descriptions-item label="归属部门">{{ projectDetailData?.基本资料?.项目归属部门名称 || currentProject.项目归属部门名称 || '—' }}</el-descriptions-item>
                <el-descriptions-item label="合作方单位">{{ currentProject.合作方单位名称 }}</el-descriptions-item>
                <el-descriptions-item label="合同总金额">
                  <span class="money-text">¥ {{ formatMoney(currentProject.合同金额) }}</span>
                </el-descriptions-item>
                <el-descriptions-item label="技术交易额">
                  ¥ {{ formatMoney(currentProject.合同技术交易额 || 0) }}
                </el-descriptions-item>
                <el-descriptions-item label="合作方式">{{ currentProject.合作方式 || '未提供合作方式' }}</el-descriptions-item>
                <el-descriptions-item label="技术领域">{{ currentProject.技术领域 || '未提供技术领域' }}</el-descriptions-item>
                <el-descriptions-item label="合同起止日期" :span="2">
                  {{ formatDate(currentProject.合同开始时间) }} 至 {{ formatDate(currentProject.合同结束时间) }}
                </el-descriptions-item>
                <el-descriptions-item label="知识产权归属" :span="2">
                  {{ currentProject.知识产权 || '未提供知识产权归属' }}
                </el-descriptions-item>
              </el-descriptions>

              <div style="margin-top: 24px">
                <h4>产学研经费执行与合同节点</h4>
                <el-progress
                  v-if="expenseProgress !== null"
                  :percentage="expenseProgress"
                  :stroke-width="18"
                  status="success"
                  :text-inside="true"
                  style="margin: 12px 0 20px"
                />
                <el-empty v-else :image-size="56" description="尚未取得真实经费执行数据" />
                <el-alert
                  title="合规提示：横向项目经费预算总额须与实际合同签署金额严格一致，到账后方可按合同付款节点进行直接经费开支核销。"
                  type="info"
                  :closable="false"
                  show-icon
                />
              </div>
            </el-tab-pane>

            <!-- 参与人 -->
            <el-tab-pane label="项目参与人" name="members">
              <el-table :data="projectDetailData?.参与人列表 || []" border stripe style="width: 100%">
                <el-table-column prop="参与人姓名" label="姓名" width="120">
                  <template #default="{ row }">
                    {{ row.参与人姓名 || row.姓名 || '—' }}
                  </template>
                </el-table-column>
                <el-table-column prop="工号" label="工号" width="130" />
                <el-table-column prop="部门名称" label="归属部门" min-width="160" />
                <el-table-column prop="分工" label="承担分工" min-width="160">
                  <template #default="{ row }">
                    {{ row.分工 || row.承担任务 || '课题参研人员' }}
                  </template>
                </el-table-column>
              </el-table>
              <el-empty v-if="!projectDetailData?.参与人列表?.length" :image-size="56" description="暂无额外登记的参研人员" />
            </el-tab-pane>

            <!-- 经费预算 -->
            <el-tab-pane label="经费预算分项" name="budget">
              <el-table :data="projectDetailData?.经费预算列表 || []" border stripe style="width: 100%">
                <el-table-column prop="项目支出类型" label="支出科目类型" width="150" />
                <el-table-column prop="项目支出内容" label="支出明细项" min-width="180" />
                <el-table-column prop="批准经费" label="批准金额 (元)" width="150" align="right">
                  <template #default="{ row }">
                    ¥ {{ formatMoney(row.批准经费) }}
                  </template>
                </el-table-column>
                <el-table-column prop="配套经费" label="配套/自筹金额" width="140" align="right">
                  <template #default="{ row }">
                    ¥ {{ formatMoney(row.配套经费 || 0) }}
                  </template>
                </el-table-column>
                <el-table-column prop="备注" label="说明" min-width="150" show-overflow-tooltip />
              </el-table>
              <el-empty v-if="!projectDetailData?.经费预算列表?.length" :image-size="56" description="暂无分项预算数据" />
            </el-tab-pane>

            <!-- 审批流程 -->
            <el-tab-pane label="审批流程记录" name="workflow">
              <el-table :data="projectDetailData?.审核记录 || []" border stripe style="width: 100%">
                <el-table-column prop="步骤名称" label="流程节点" width="150" />
                <el-table-column prop="步骤操作人姓名" label="办理人员" width="130">
                  <template #default="{ row }">
                    {{ row.步骤操作人姓名 || row.指定人姓名 || '—' }}
                  </template>
                </el-table-column>
                <el-table-column prop="状态名称" label="节点状态" width="120">
                  <template #default="{ row }">
                    <el-tag :type="getStatusTag(row.状态值 ?? row.状态)" size="small">
                      {{ row.状态名称 || row.步骤状态说明 || '流转中' }}
                    </el-tag>
                  </template>
                </el-table-column>
                <el-table-column prop="建立时间" label="送审时间" width="160">
                  <template #default="{ row }">
                    {{ formatDateTime(row.建立时间) }}
                  </template>
                </el-table-column>
                <el-table-column prop="备注" label="审核意见" min-width="200" show-overflow-tooltip />
              </el-table>
              <el-empty v-if="!projectDetailData?.审核记录?.length" :image-size="56" description="暂无审批流转记录" />
            </el-tab-pane>

            <!-- 全周期档案 -->
            <el-tab-pane label="全周期档案" name="lifecycle">
              <el-divider content-position="left">延期、暂停与终止申请</el-divider>
              <el-table :data="projectLifecycleData?.生命周期申请 || []" border stripe style="width: 100%">
                <el-table-column prop="事件类型" label="变更类型" width="110" />
                <el-table-column prop="申请原因" label="申请原因" min-width="200" show-overflow-tooltip />
                <el-table-column prop="新合同结束日期" label="拟延期至" width="130">
                  <template #default="{ row }">{{ formatDate(row.新合同结束日期) }}</template>
                </el-table-column>
                <el-table-column prop="状态" label="审核状态" width="110" />
                <el-table-column prop="建立时间" label="提交时间" width="160">
                  <template #default="{ row }">{{ formatDateTime(row.建立时间) }}</template>
                </el-table-column>
              </el-table>
              <el-empty v-if="!projectLifecycleData?.生命周期申请?.length" :image-size="48" description="暂无延期或变更记录" />

              <el-divider content-position="left" style="margin-top: 24px">研究进展与年度报告</el-divider>
              <el-table :data="projectLifecycleData?.研究报告 || []" border stripe style="width: 100%">
                <el-table-column prop="报告类型" label="报告类型" width="120" />
                <el-table-column prop="报告年度" label="年度" width="90" />
                <el-table-column prop="报告摘要" label="报告摘要" min-width="220" show-overflow-tooltip />
                <el-table-column prop="状态" label="状态" width="100" />
              </el-table>
              <el-empty v-if="!projectLifecycleData?.研究报告?.length" :image-size="48" description="暂无研究报告" />

              <el-divider content-position="left" style="margin-top: 24px">归档留痕材料</el-divider>
              <el-table :data="projectLifecycleData?.归档材料 || []" border stripe style="width: 100%">
                <el-table-column prop="材料类型" label="材料类型" width="130" />
                <el-table-column prop="材料标题" label="材料标题" min-width="180" />
                <el-table-column prop="文件大小" label="大小" width="100">
                  <template #default="{ row }">{{ row.文件大小 ? `${(row.文件大小 / 1024).toFixed(1)} KB` : '—' }}</template>
                </el-table-column>
                <el-table-column prop="状态" label="归档状态" width="100" />
              </el-table>
              <el-empty v-if="!projectLifecycleData?.归档材料?.length" :image-size="48" description="暂无归档材料" />
            </el-tab-pane>
          </el-tabs>
        </template>
      </div>
    </el-drawer>

    <!-- 新增申请向导弹窗 -->
    <el-dialog v-model="applyDialogVisible" title="新增横向项目立项申请" width="750px">
      <el-form ref="applyFormRef" :model="applyForm" :rules="applyRules" label-width="130px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="合同编号" prop="合同编号">
              <el-input v-model="applyForm.合同编号" placeholder="如: HX-2026-001" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="合同名称" prop="合同名称">
              <el-input v-model="applyForm.合同名称" placeholder="请输入合同或项目全称" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="合作方单位" prop="合作方单位名称">
              <el-input v-model="applyForm.合作方单位名称" placeholder="企业/事业单位名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="合同总金额" prop="合同金额">
              <el-input-number
                v-model="applyForm.合同金额"
                :min="0"
                :precision="2"
                style="width: 100%"
                placeholder="合同总额 (元)"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="合作方式">
              <el-select v-model="applyForm.合作方式" style="width: 100%">
                <el-option label="技术委托开发" value="委托开发" />
                <el-option label="合作研发" value="合作开发" />
                <el-option label="技术转让/许可" value="技术转让" />
                <el-option label="技术咨询服务" value="技术咨询" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="技术合同认定">
              <el-switch
                v-model="applyForm.是否申请技术合同认定"
                active-text="申请技术市场认定登记"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="合同开始时间">
              <el-date-picker
                v-model="applyForm.合同开始时间"
                type="date"
                placeholder="生效开始日期"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="合同结束时间">
              <el-date-picker
                v-model="applyForm.合同结束时间"
                type="date"
                placeholder="预期履约完毕日期"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="知识产权归属">
          <el-input
            v-model="applyForm.知识产权"
            type="textarea"
            :rows="2"
            placeholder="根据合同约定填写知识产权权益归属说明"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="applyDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmitApply">
          提交立项审核
        </el-button>
      </template>
    </el-dialog>

    <!-- 变更申请弹窗 -->
    <el-dialog v-model="changeDialogVisible" title="发起横向合同变更申请" width="650px" destroy-on-close>
      <el-form ref="changeFormRef" :model="changeForm" :rules="changeRules" label-width="110px">
        <el-form-item label="合同编号">
          <el-input :model-value="changeForm.合同编号" disabled />
        </el-form-item>
        <el-form-item label="合同名称">
          <el-input :model-value="changeForm.合同名称" disabled />
        </el-form-item>
        <el-form-item label="变更事由" prop="变更事由">
          <el-input
            v-model="changeForm.变更事由"
            type="textarea"
            :rows="3"
            placeholder="请详细描述本次合同变更原因（如合作范围微调、指标顺延等）"
            maxlength="500"
            show-word-limit
          />
        </el-form-item>
        <el-form-item label="内容简介">
          <el-input
            v-model="changeForm.变更内容简介"
            type="textarea"
            :rows="2"
            placeholder="可选：变更内容简要说明"
            maxlength="200"
          />
        </el-form-item>
        <el-form-item label="变更备注">
          <el-input v-model="changeForm.变更备注" placeholder="可选备注说明" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="changeDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="changeSubmitLoading" @click="handleSubmitChange">
          提交变更申请
        </el-button>
      </template>
    </el-dialog>

    <!-- 变更详情弹窗 -->
    <el-dialog v-model="changeDetailVisible" title="横向项目变更详情" width="620px" destroy-on-close>
      <el-descriptions v-if="currentChangeDetail" :column="2" border>
        <el-descriptions-item label="合同编号">{{ currentChangeDetail.合同编号 }}</el-descriptions-item>
        <el-descriptions-item label="合同名称">{{ currentChangeDetail.合同名称 }}</el-descriptions-item>
        <el-descriptions-item label="变更事由" :span="2">{{ currentChangeDetail.变更事由 }}</el-descriptions-item>
        <el-descriptions-item label="变更内容简介" :span="2">{{ currentChangeDetail.变更内容简介 || '—' }}</el-descriptions-item>
        <el-descriptions-item label="变更发起时间">{{ formatDateTime(currentChangeDetail.变更时间) }}</el-descriptions-item>
        <el-descriptions-item label="审核状态">
          <el-tag :type="getStatusTag(currentChangeDetail.状态)" size="small">
            {{ getStatusText(currentChangeDetail.状态) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="变更备注" :span="2">{{ currentChangeDetail.变更备注 || '—' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="changeDetailVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 结题详情弹窗 -->
    <el-dialog v-model="closureDetailVisible" title="横向项目结题归档详情" width="620px" destroy-on-close>
      <el-descriptions v-if="currentClosureDetail" :column="2" border>
        <el-descriptions-item label="合同编号">{{ currentClosureDetail.合同编号 }}</el-descriptions-item>
        <el-descriptions-item label="合同名称">{{ currentClosureDetail.合同名称 }}</el-descriptions-item>
        <el-descriptions-item label="申请结题日期">{{ formatDate(currentClosureDetail.结题时间) }}</el-descriptions-item>
        <el-descriptions-item label="验收结果">
          <el-tag v-if="currentClosureDetail.验收结果" type="success" size="small">{{ currentClosureDetail.验收结果 }}</el-tag>
          <span v-else>未提供</span>
        </el-descriptions-item>
        <el-descriptions-item label="归档状态" :span="2">
          <el-tag v-if="currentClosureDetail.归档状态" type="info" size="small">{{ currentClosureDetail.归档状态 }}</el-tag>
          <span v-else>未提供档案状态</span>
        </el-descriptions-item>
        <el-descriptions-item label="结题说明" :span="2">{{ currentClosureDetail.备注 || currentClosureDetail.结题说明 || '—' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="closureDetailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { Plus, Search, Refresh } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import {
  getHorizontalProjects,
  getHorizontalProjectDetail,
  getHorizontalProjectLifecycle,
  getHorizontalProjectChanges,
  getHorizontalProjectChangeDetail,
  getHorizontalProjectClosures,
  getHorizontalProjectClosureDetail,
  createHorizontalProject,
  createHorizontalProjectChange,
  type HorizontalProjectItem,
  type HorizontalProjectChangeItem,
  type HorizontalProjectClosureItem
} from '@/api/horizontal-project'
import { getContractGovernance } from '@/api/horizontal-contract-governance'
import dayjs from 'dayjs'

const activeTab = ref('projects')
const loading = ref(false)
const changeLoading = ref(false)
const closureLoading = ref(false)
const submitLoading = ref(false)

const projectList = ref<HorizontalProjectItem[]>([])
const changeList = ref<HorizontalProjectChangeItem[]>([])
const closureList = ref<HorizontalProjectClosureItem[]>([])
const currentProject = ref<HorizontalProjectItem | null>(null)
const detailDrawerVisible = ref(false)
const detailActiveTab = ref('basic')
const detailLoading = ref(false)
const projectDetailData = ref<any>(null)
const projectLifecycleData = ref<any>(null)

// 变更弹窗
const changeDialogVisible = ref(false)
const changeSubmitLoading = ref(false)
const changeFormRef = ref()
const changeForm = reactive({
  横向项目编号: 0,
  合同编号: '',
  合同名称: '',
  变更事由: '',
  变更内容简介: '',
  变更备注: ''
})
const changeRules = {
  变更事由: [{ required: true, message: '请输入变更事由', trigger: 'blur' }]
}

// 变更详情与结题详情弹窗
const changeDetailVisible = ref(false)
const currentChangeDetail = ref<any>(null)
const closureDetailVisible = ref(false)
const currentClosureDetail = ref<any>(null)

const applyDialogVisible = ref(false)
const governanceProjectId = ref<number | null>(null)
const governance = reactive<{ 合同版本: any[]; 合作方资质: any[]; 收入认定: any[] }>({ 合同版本: [], 合作方资质: [], 收入认定: [] })

const paging = reactive({
  pageIndex: 1,
  pageSize: 10
})

const queryForm = reactive({
  like合同编号: '',
  like合同名称: ''
})

const totalCount = ref(0)

const applyForm = reactive({
  合同编号: '',
  合同名称: '',
  合作方单位名称: '',
  合同金额: null as unknown as number,
  合作方式: '委托开发',
  是否申请技术合同认定: true,
  合同开始时间: null as unknown as Date,
  合同结束时间: null as unknown as Date,
  知识产权: ''
})

const applyRules = {
  合同编号: [{ required: true, message: '请输入合同编号', trigger: 'blur' }],
  合同名称: [{ required: true, message: '请输入合同名称', trigger: 'blur' }],
  合作方单位名称: [{ required: true, message: '请输入合作方单位名称', trigger: 'blur' }],
  合同金额: [{ required: true, message: '请输入合同金额', trigger: 'blur' }]
}

// 统计计算
const totalContractAmount = computed(() => {
  return projectList.value.reduce((sum, item) => sum + (Number(item.合同金额) || 0), 0)
})

const certifiedCount = computed(() => {
  return projectList.value.filter(item => item.是否申请技术合同认定).length
})

const ongoingCount = computed(() => {
  return projectList.value.length
})

// 只有服务端返回批准预算/实际支出后才显示进度；缺失数据时显示空状态，避免用固定百分比误导用户。
const expenseProgress = computed<number | null>(() => {
  const project = currentProject.value as (HorizontalProjectItem & {
    批准预算?: number
    已审核支出?: number
    已付款金额?: number
  }) | null
  if (!project) return null
  const total = Number(project.批准预算)
  const spent = Number(project.已审核支出 ?? project.已付款金额)
  if (!Number.isFinite(total) || total <= 0 || !Number.isFinite(spent) || spent < 0) return null
  return Math.min(100, Math.max(0, Math.round((spent / total) * 100)))
})

function formatMoney(val: number | string | undefined) {
  if (val === undefined || val === null) return '0.00'
  return Number(val).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatDate(val: string | undefined) {
  if (!val) return '-'
  return dayjs(val).format('YYYY-MM-DD')
}

function formatDateTime(val: string | undefined) {
  if (!val) return '-'
  return dayjs(val).format('YYYY-MM-DD HH:mm')
}

function getStatusTag(status: any): 'success' | 'warning' | 'info' | 'danger' {
  if (status === 1 || status === '通过') return 'success'
  if (status === 0 || status === '待审') return 'warning'
  return 'info'
}

function getStatusText(status: any) {
  if (status === 1 || status === '通过') return '审核通过'
  if (status === 0 || status === '待审') return '流程流转中'
  return '已留存'
}

async function fetchProjects() {
  loading.value = true
  try {
    const res = await getHorizontalProjects(paging, queryForm)
    if (res && res.data) {
      projectList.value = res.data.list || res.data || []
      totalCount.value = res.data.total || projectList.value.length
    }
  } catch (err: any) {
    ElMessage.error(err.message || '获取横向项目列表失败')
  } finally {
    loading.value = false
  }
}

async function fetchChanges() {
  changeLoading.value = true
  try {
    const res = await getHorizontalProjectChanges({ pageIndex: 1, pageSize: 20 })
    if (res && res.data) {
      changeList.value = res.data.list || res.data || []
    }
  } catch {
    // 降级使用空列表
  } finally {
    changeLoading.value = false
  }
}

async function fetchClosures() {
  closureLoading.value = true
  try {
    const res = await getHorizontalProjectClosures({ pageIndex: 1, pageSize: 20 })
    if (res && res.data) {
      closureList.value = res.data.list || res.data || []
    }
  } catch {
    // 降级使用空列表
  } finally {
    closureLoading.value = false
  }
}

async function fetchGovernance() {
  if (!governanceProjectId.value) return
  try {
    const res = await getContractGovernance(governanceProjectId.value)
    const payload = res?.data || res || {}
    governance.合同版本 = payload.合同版本 || []
    governance.合作方资质 = payload.合作方资质 || []
    governance.收入认定 = payload.收入认定 || []
  } catch (err: any) {
    ElMessage.error(err.message || '获取合同治理记录失败')
  }
}

function handleSearch() {
  paging.pageIndex = 1
  fetchProjects()
}

function handleReset() {
  queryForm.like合同编号 = ''
  queryForm.like合同名称 = ''
  paging.pageIndex = 1
  fetchProjects()
}

async function handleViewDetail(row: HorizontalProjectItem) {
  currentProject.value = row
  detailDrawerVisible.value = true
  detailLoading.value = true
  detailActiveTab.value = 'basic'
  projectDetailData.value = null
  projectLifecycleData.value = null
  try {
    const [detailRes, lifeRes] = await Promise.all([
      getHorizontalProjectDetail(row.编号).catch(() => null),
      getHorizontalProjectLifecycle(row.编号).catch(() => null)
    ])
    projectDetailData.value = detailRes?.data || detailRes || null
    projectLifecycleData.value = lifeRes?.data || lifeRes || null
  } catch {
    // 降级使用现有行数据
  } finally {
    detailLoading.value = false
  }
}

function handleApplyChange(row: HorizontalProjectItem) {
  changeForm.横向项目编号 = row.编号
  changeForm.合同编号 = row.合同编号
  changeForm.合同名称 = row.合同名称
  changeForm.变更事由 = ''
  changeForm.变更内容简介 = ''
  changeForm.变更备注 = ''
  changeDialogVisible.value = true
}

async function handleSubmitChange() {
  if (!changeFormRef.value) return
  await changeFormRef.value.validate(async (valid: boolean) => {
    if (!valid) return
    changeSubmitLoading.value = true
    try {
      await createHorizontalProjectChange({
        基本资料: {
          横向项目编号: changeForm.横向项目编号,
          合同编号: changeForm.合同编号,
          变更事由: changeForm.变更事由,
          变更内容简介: changeForm.变更内容简介,
          变更备注: changeForm.变更备注,
          变更时间: dayjs().toISOString()
        },
        isHold: false
      })
      ElMessage.success('合同变更申请已成功提交审核流程！')
      changeDialogVisible.value = false
      activeTab.value = 'changes'
      fetchChanges()
    } catch (err: any) {
      ElMessage.error(err.message || '提交变更申请失败')
    } finally {
      changeSubmitLoading.value = false
    }
  })
}

async function handleViewChangeDetail(row: HorizontalProjectChangeItem) {
  currentChangeDetail.value = row
  changeDetailVisible.value = true
  try {
    const res = await getHorizontalProjectChangeDetail(row.横向项目编号 || 0, row.编号)
    if (res && res.data) {
      currentChangeDetail.value = { ...row, ...(res.data || {}) }
    }
  } catch {
    // 降级使用列表行数据
  }
}

async function handleViewClosureDetail(row: HorizontalProjectClosureItem) {
  currentClosureDetail.value = row
  closureDetailVisible.value = true
  try {
    const res = await getHorizontalProjectClosureDetail(row.横向项目编号 || 0, row.编号)
    if (res && res.data) {
      currentClosureDetail.value = { ...row, ...(res.data || {}) }
    }
  } catch {
    // 降级使用列表行数据
  }
}

function handleOpenApplyDialog() {
  applyDialogVisible.value = true
}

async function handleSubmitApply() {
  submitLoading.value = true
  try {
    await createHorizontalProject({
      基本资料: {
        合同编号: applyForm.合同编号,
        合同名称: applyForm.合同名称,
        合作方单位名称: applyForm.合作方单位名称,
        合同金额: applyForm.合同金额,
        合作方式: applyForm.合作方式,
        是否申请技术合同认定: applyForm.是否申请技术合同认定,
        合同开始时间: dayjs(applyForm.合同开始时间).toISOString(),
        合同结束时间: dayjs(applyForm.合同结束时间).toISOString(),
        知识产权: applyForm.知识产权
      },
      参与人列表: [],
      经费预算列表: [],
      isHold: false
    })
    ElMessage.success('横向项目立项申请已成功提交审核流程！')
    applyDialogVisible.value = false
    fetchProjects()
  } catch (err: any) {
    ElMessage.error(err.message || '提交失败')
  } finally {
    submitLoading.value = false
  }
}

onMounted(() => {
  fetchProjects()
  fetchChanges()
  fetchClosures()
})
</script>

<style scoped lang="scss">
.horizontal-contract-container {
  .header-card {
    margin-bottom: 16px;
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
      h2 {
        margin: 0 0 6px;
        font-size: 20px;
        color: #303133;
      }
      .subtitle {
        margin: 0;
        font-size: 13px;
        color: #909399;
      }
    }
    .statistic-row {
      margin-top: 12px;
      .stat-box {
        background: #f8f9fa;
        padding: 12px 16px;
        border-radius: 4px;
        display: flex;
        flex-direction: column;
        .label {
          font-size: 12px;
          color: #909399;
          margin-bottom: 4px;
        }
        .value {
          font-size: 18px;
          font-weight: 600;
          color: #303133;
          small {
            font-size: 12px;
            font-weight: normal;
          }
          &.money {
            color: #67c23a;
          }
        }
      }
    }
  }

  .main-card {
    .filter-form {
      margin-bottom: 16px;
    }
    .money-text {
      color: #67c23a;
      font-weight: bold;
    }
    .empty-state {
      color: #c0c4cc;
      font-size: 12px;
    }
    .pagination-container {
      margin-top: 16px;
      display: flex;
      justify-content: flex-end;
    }
  }

  .project-detail {
    padding: 8px 4px;
    .money-text {
      color: #67c23a;
      font-size: 16px;
      font-weight: bold;
    }
  }
}
</style>
