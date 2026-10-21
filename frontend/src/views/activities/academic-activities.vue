<template>
  <div class="academic-activities-container">
    <el-card shadow="never" class="header-card">
      <div class="page-header">
        <div>
          <h2>学术交流与学术讲座/会议全生命周期管理</h2>
          <p class="subtitle">涵盖主办国内外学术讲座、专家邀请评审、参加学术研讨会、经费预决算、到会人次评价及交流成果总结归档</p>
        </div>
        <div class="header-actions">
          <el-button :icon="Download" @click="handleExportCurrentTab">
            导出台账
          </el-button>
          <el-button type="primary" :icon="Plus" @click="handleOpenApplyLecture">
            发起主办讲座申请
          </el-button>
          <el-button type="success" :icon="Plus" @click="handleOpenApplyConference">
            申请参加学术会议
          </el-button>
        </div>
      </div>
    </el-card>

    <!-- KPI 统计卡片 -->
    <el-row :gutter="16" class="kpi-row">
      <el-col :xs="12" :sm="6" :md="4">
        <el-card shadow="hover" class="kpi-card">
          <div class="kpi-label">主办学术讲座</div>
          <div class="kpi-value text-primary">{{ lectureTotal }} <span class="unit">场</span></div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6" :md="5">
        <el-card shadow="hover" class="kpi-card">
          <div class="kpi-label">讲座预算累计</div>
          <div class="kpi-value text-warning">¥ {{ formatMoney(totalLectureBudget) }}</div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6" :md="4">
        <el-card shadow="hover" class="kpi-card">
          <div class="kpi-label">外出参会交流</div>
          <div class="kpi-value text-success">{{ confTotal }} <span class="unit">人次</span></div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6" :md="5">
        <el-card shadow="hover" class="kpi-card">
          <div class="kpi-label">讲座反馈归档率</div>
          <div class="kpi-value text-danger">{{ lectureFeedbackRate }} <span class="unit">%</span></div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <el-card shadow="hover" class="kpi-card">
          <div class="kpi-label">参会总结归档率</div>
          <div class="kpi-value text-info">{{ confFeedbackRate }} <span class="unit">%</span></div>
        </el-card>
      </el-col>
    </el-row>

    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab" @tab-change="handleTabChange">
        <!-- 标签页 1：主办学术讲座 -->
        <el-tab-pane label="主办学术讲座" name="lectures">
          <el-form :inline="true" :model="lectureQuery" class="filter-form">
            <el-form-item label="讲座名称">
              <el-input v-model="lectureQuery.like活动名称" placeholder="讲座/学术报告名称" clearable />
            </el-form-item>
            <el-form-item label="主办人">
              <el-input v-model="lectureQuery.like主办人姓名" placeholder="检索本院主办人" clearable />
            </el-form-item>
            <el-form-item label="反馈状态">
              <el-select v-model="lectureFeedbackFilter" placeholder="全部" clearable style="width: 120px">
                <el-option label="全部" value="" />
                <el-option label="已反馈" value="true" />
                <el-option label="待反馈" value="false" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :icon="Search" @click="fetchLectures">查询</el-button>
              <el-button :icon="Refresh" @click="handleResetLectureQuery">重置</el-button>
            </el-form-item>
          </el-form>

          <el-table v-loading="lectureLoading" :data="lectureList" border stripe style="width: 100%">
            <el-table-column prop="活动名称" label="讲座报告题目" min-width="220" show-overflow-tooltip>
              <template #default="{ row }">
                <el-link type="primary" :underline="false" @click="handleViewLectureDetail(row)">
                  {{ row.活动名称 }}
                </el-link>
              </template>
            </el-table-column>
            <el-table-column prop="主讲人姓名" label="主讲专家" width="120" />
            <el-table-column prop="主讲人单位" label="主讲专家单位" min-width="180" show-overflow-tooltip />
            <el-table-column prop="主讲人职称" label="职称/职务" width="110" />
            <el-table-column prop="讲座地点" label="讲座地点" min-width="140" show-overflow-tooltip />
            <el-table-column prop="开始时间" label="讲座时间" width="160">
              <template #default="{ row }">
                {{ formatDateTime(row.开始时间) }}
              </template>
            </el-table-column>
            <el-table-column prop="活动经费" label="活动预算 (元)" width="130" align="right">
              <template #default="{ row }">
                <span class="money-text">¥ {{ formatMoney(row.活动经费) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="到会反馈" width="110" align="center">
              <template #default="{ row }">
                <el-tag v-if="row.是否反馈过" type="success" size="small">已反馈</el-tag>
                <el-tag v-else type="warning" size="small">待反馈</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="180" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="handleViewLectureDetail(row)">
                  详情
                </el-button>
                <el-button
                  link
                  type="success"
                  size="small"
                  :disabled="row.是否反馈过"
                  @click="handleOpenLectureFeedback(row)"
                >
                  {{ row.是否反馈过 ? '已反馈' : '登记反馈' }}
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-container">
            <el-pagination
              v-model:current-page="lecturePaging.pageIndex"
              v-model:page-size="lecturePaging.pageSize"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              :total="lectureTotal"
              @size-change="fetchLectures"
              @current-change="fetchLectures"
            />
          </div>
        </el-tab-pane>

        <!-- 标签页 2：讲座反馈记录 -->
        <el-tab-pane label="讲座反馈记录" name="lecture-feedbacks">
          <el-form :inline="true" :model="lectureFeedbackQuery" class="filter-form">
            <el-form-item label="讲座题目">
              <el-input v-model="lectureFeedbackQuery.like活动名称" placeholder="讲座活动名称" clearable />
            </el-form-item>
            <el-form-item label="专家姓名">
              <el-input v-model="lectureFeedbackQuery.like专家姓名" placeholder="主讲专家姓名" clearable />
            </el-form-item>
            <el-form-item label="主办人">
              <el-input v-model="lectureFeedbackQuery.like主办人姓名" placeholder="主办人员姓名" clearable />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :icon="Search" @click="fetchLectureFeedbacks">查询</el-button>
              <el-button :icon="Refresh" @click="handleResetLectureFeedbackQuery">重置</el-button>
            </el-form-item>
          </el-form>

          <el-table v-loading="lectureFeedbackLoading" :data="lectureFeedbackList" border stripe style="width: 100%">
            <el-table-column prop="活动名称" label="讲座活动名称" min-width="220" show-overflow-tooltip />
            <el-table-column prop="专家姓名" label="专家姓名" width="120" />
            <el-table-column prop="主办人姓名" label="主办人" width="120" />
            <el-table-column prop="参会人数" label="到会人次" width="110" align="center" />
            <el-table-column prop="活动持续分钟" label="持续时长" width="110" align="center">
              <template #default="{ row }">
                {{ row.活动持续分钟 }} 分钟
              </template>
            </el-table-column>
            <el-table-column prop="专家讲课费金额" label="讲课报酬 (元)" width="130" align="right">
              <template #default="{ row }">
                <span class="money-text">¥ {{ formatMoney(row.专家讲课费金额) }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="结算方式" label="结算方式" width="100" align="center">
              <template #default="{ row }">
                <el-tag size="small" type="info">{{ row.结算方式 || '转账' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="讲座效果" label="讲座评价与效果" min-width="200" show-overflow-tooltip />
            <el-table-column label="操作" width="110" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="handleViewLectureFeedbackDetail(row)">
                  反馈详情
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-container">
            <el-pagination
              v-model:current-page="lectureFeedbackPaging.pageIndex"
              v-model:page-size="lectureFeedbackPaging.pageSize"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              :total="lectureFeedbackTotal"
              @size-change="fetchLectureFeedbacks"
              @current-change="fetchLectureFeedbacks"
            />
          </div>
        </el-tab-pane>

        <!-- 标签页 3：参加学术会议 -->
        <el-tab-pane label="参加学术会议" name="conferences">
          <el-form :inline="true" :model="confQuery" class="filter-form">
            <el-form-item label="会议名称">
              <el-input v-model="confQuery.like会议名称" placeholder="国际/国内学术研讨会名称" clearable />
            </el-form-item>
            <el-form-item label="参会人员">
              <el-input v-model="confQuery.like申请人姓名" placeholder="检索参会人员" clearable />
            </el-form-item>
            <el-form-item label="反馈状态">
              <el-select v-model="confFeedbackFilter" placeholder="全部" clearable style="width: 120px">
                <el-option label="全部" value="" />
                <el-option label="已反馈" value="true" />
                <el-option label="待反馈" value="false" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :icon="Search" @click="fetchConferences">查询</el-button>
              <el-button :icon="Refresh" @click="handleResetConfQuery">重置</el-button>
            </el-form-item>
          </el-form>

          <el-table v-loading="confLoading" :data="confList" border stripe style="width: 100%">
            <el-table-column prop="会议名称" label="学术会议名称" min-width="220" show-overflow-tooltip>
              <template #default="{ row }">
                <el-link type="primary" :underline="false" @click="handleViewConfDetail(row)">
                  {{ row.会议名称 }}
                </el-link>
              </template>
            </el-table-column>
            <el-table-column prop="会议主办单位" label="主办单位" min-width="180" show-overflow-tooltip />
            <el-table-column prop="研讨领域" label="研讨学科领域" width="140" show-overflow-tooltip />
            <el-table-column prop="会议地址" label="会议地点" min-width="140" show-overflow-tooltip />
            <el-table-column prop="会议开始时间" label="开会起止日期" width="200">
              <template #default="{ row }">
                {{ formatDate(row.会议开始时间) }} ~ {{ formatDate(row.会议结束时间) }}
              </template>
            </el-table-column>
            <el-table-column prop="往返时间" label="往返天数" width="90" align="center">
              <template #default="{ row }">
                {{ row.往返时间 ? `${row.往返时间} 天` : '-' }}
              </template>
            </el-table-column>
            <el-table-column label="参会反馈" width="110" align="center">
              <template #default="{ row }">
                <el-tag v-if="row.是否反馈过" type="success" size="small">已反馈</el-tag>
                <el-tag v-else type="warning" size="small">待归档</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="180" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="handleViewConfDetail(row)">
                  详情
                </el-button>
                <el-button
                  link
                  type="success"
                  size="small"
                  :disabled="row.是否反馈过"
                  @click="handleOpenConfFeedback(row)"
                >
                  {{ row.是否反馈过 ? '已归档' : '参会总结' }}
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-container">
            <el-pagination
              v-model:current-page="confPaging.pageIndex"
              v-model:page-size="confPaging.pageSize"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              :total="confTotal"
              @size-change="fetchConferences"
              @current-change="fetchConferences"
            />
          </div>
        </el-tab-pane>

        <!-- 标签页 4：会议反馈台账 -->
        <el-tab-pane label="参会反馈台账" name="conf-feedbacks">
          <el-form :inline="true" :model="confFeedbackQuery" class="filter-form">
            <el-form-item label="会议名称">
              <el-input v-model="confFeedbackQuery.like会议名称" placeholder="会议全称" clearable />
            </el-form-item>
            <el-form-item label="参会人员">
              <el-input v-model="confFeedbackQuery.like申请人姓名" placeholder="参会人员姓名" clearable />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :icon="Search" @click="fetchConfFeedbacks">查询</el-button>
              <el-button :icon="Refresh" @click="handleResetConfFeedbackQuery">重置</el-button>
            </el-form-item>
          </el-form>

          <el-table v-loading="confFeedbackLoading" :data="confFeedbackList" border stripe style="width: 100%">
            <el-table-column prop="会议名称" label="学术会议名称" min-width="220" show-overflow-tooltip />
            <el-table-column prop="申请人姓名" label="参会报告人" width="130" />
            <el-table-column prop="活动持续分钟" label="会议时长(分)" width="120" align="center" />
            <el-table-column prop="参会人数" label="参会总规模" width="120" align="center" />
            <el-table-column prop="参会总结" label="参会总结及学术交流收获" min-width="260" show-overflow-tooltip />
            <el-table-column label="操作" width="110" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="handleViewConfFeedbackDetail(row)">
                  反馈详情
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-container">
            <el-pagination
              v-model:current-page="confFeedbackPaging.pageIndex"
              v-model:page-size="confFeedbackPaging.pageSize"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              :total="confFeedbackTotal"
              @size-change="fetchConfFeedbacks"
              @current-change="fetchConfFeedbacks"
            />
          </div>
        </el-tab-pane>

        <!-- 标签页 5：学术活动台账与统计 -->
        <el-tab-pane label="学术活动综合台账" name="ledger">
          <div class="ledger-container">
            <el-alert
              title="学术交流综合治理台账：包含院内外主讲讲座场次、参会交流人次、学术差旅预算执行及学术反馈闭环"
              type="info"
              :closable="false"
              show-icon
              style="margin-bottom: 16px"
            />

            <el-row :gutter="16">
              <el-col :span="12">
                <el-card shadow="never" class="sub-table-card">
                  <template #header>
                    <div class="card-header">
                      <span>近期主办高水平学术讲座</span>
                      <el-button link type="primary" @click="activeTab = 'lectures'">查看全部</el-button>
                    </div>
                  </template>
                  <el-table :data="lectureList.slice(0, 5)" border size="small">
                    <el-table-column prop="活动名称" label="讲座题目" min-width="160" show-overflow-tooltip />
                    <el-table-column prop="主讲人姓名" label="专家" width="90" />
                    <el-table-column prop="活动经费" label="预算(元)" width="100" align="right">
                      <template #default="{ row }">
                        ¥ {{ formatMoney(row.活动经费) }}
                      </template>
                    </el-table-column>
                    <el-table-column label="反馈" width="80" align="center">
                      <template #default="{ row }">
                        <el-tag :type="row.是否反馈过 ? 'success' : 'warning'" size="small">
                          {{ row.是否反馈过 ? '完成' : '待办' }}
                        </el-tag>
                      </template>
                    </el-table-column>
                  </el-table>
                </el-card>
              </el-col>
              <el-col :span="12">
                <el-card shadow="never" class="sub-table-card">
                  <template #header>
                    <div class="card-header">
                      <span>近期参加国内外学术会议</span>
                      <el-button link type="primary" @click="activeTab = 'conferences'">查看全部</el-button>
                    </div>
                  </template>
                  <el-table :data="confList.slice(0, 5)" border size="small">
                    <el-table-column prop="会议名称" label="会议名称" min-width="160" show-overflow-tooltip />
                    <el-table-column prop="会议主办单位" label="主办单位" min-width="140" show-overflow-tooltip />
                    <el-table-column prop="申请人姓名" label="参会人" width="90" />
                    <el-table-column label="反馈" width="80" align="center">
                      <template #default="{ row }">
                        <el-tag :type="row.是否反馈过 ? 'success' : 'warning'" size="small">
                          {{ row.是否反馈过 ? '完成' : '待办' }}
                        </el-tag>
                      </template>
                    </el-table-column>
                  </el-table>
                </el-card>
              </el-col>
            </el-row>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 发起主办讲座申请弹窗向导 -->
    <el-dialog
      v-model="lectureDialogVisible"
      title="发起主办学术讲座申请"
      width="780px"
      :close-on-click-modal="false"
    >
      <el-form ref="lectureFormRef" :model="lectureForm" :rules="lectureRules" label-width="120px">
        <h4 class="form-section-title">1. 讲座基本信息</h4>
        <el-form-item label="讲座报告题目" prop="活动名称">
          <el-input v-model="lectureForm.活动名称" placeholder="输入学术报告或讲座官方全称" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="会议类型">
              <el-select v-model="lectureForm.会议类型" placeholder="请选择" style="width: 100%">
                <el-option label="国际学术交流讲座" value="国际学术交流讲座" />
                <el-option label="全国性学术专题报告" value="全国性学术专题报告" />
                <el-option label="省级临床/基础前沿讲座" value="省级临床/基础前沿讲座" />
                <el-option label="院级科研学术沙龙" value="院级科研学术沙龙" />
                <el-option label="科室业务专题讲座" value="科室业务专题讲座" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="预算经费 (元)">
              <el-input-number v-model="lectureForm.活动经费" :min="0" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="开始时间" prop="开始时间">
              <el-date-picker v-model="lectureForm.开始时间" type="datetime" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="结束时间" prop="结束时间">
              <el-date-picker v-model="lectureForm.结束时间" type="datetime" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="活动对象">
              <el-input v-model="lectureForm.活动对象" placeholder="如: 全院临床科研人员、研究生" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="讲座举办地点" prop="讲座地点">
              <el-input v-model="lectureForm.讲座地点" placeholder="如: 综合科研楼 3 楼学术报告厅" />
            </el-form-item>
          </el-col>
        </el-row>

        <h4 class="form-section-title">2. 主讲专家学者档案</h4>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="主讲专家姓名" prop="主讲人姓名">
              <el-input v-model="lectureForm.主讲人姓名" placeholder="专家姓名" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="主讲专家国别">
              <el-input v-model="lectureForm.主讲人国别" placeholder="如: 中国 / 美国" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="专家工作单位">
              <el-input v-model="lectureForm.主讲人单位" placeholder="大学、医学院或科研院所" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="最高学位">
              <el-select v-model="lectureForm.主讲人学位" placeholder="请选择学位" style="width: 100%">
                <el-option label="博士" value="博士" />
                <el-option label="硕士" value="硕士" />
                <el-option label="学士" value="学士" />
                <el-option label="博士后" value="博士后" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="专业职称">
              <el-input v-model="lectureForm.主讲人职称" placeholder="教授/研究员/主任医师" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="专家简历附件">
              <el-upload
                action=""
                :show-file-list="false"
                :http-request="customUploadResume"
              >
                <el-button type="info" size="small">上传简历</el-button>
                <span v-if="lectureForm.主讲人简历路径" class="upload-filename">
                  已关联简历
                </span>
              </el-upload>
            </el-form-item>
          </el-col>
        </el-row>

        <h4 class="form-section-title">3. 讲座学术主旨与大纲</h4>
        <el-form-item label="学术内容概述">
          <el-input
            v-model="lectureForm.内容概述"
            type="textarea"
            :rows="3"
            placeholder="简要阐述学术讲座主旨、最新研究进展及临床科研应用前景 (500字以内)"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="lectureDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmitLecture">提交审核流程</el-button>
      </template>
    </el-dialog>

    <!-- 主办讲座详情抽屉 -->
    <el-drawer v-model="lectureDetailDrawerVisible" title="主办学术讲座详情档案" size="680px">
      <div v-if="selectedLecture" class="detail-container">
        <el-descriptions title="讲座基本信息" :column="2" border>
          <el-descriptions-item label="讲座题目" :span="2">
            <strong>{{ selectedLecture.活动名称 }}</strong>
          </el-descriptions-item>
          <el-descriptions-item label="会议类型">{{ selectedLecture.会议类型 || '学术讲座' }}</el-descriptions-item>
          <el-descriptions-item label="预算经费">¥ {{ formatMoney(selectedLecture.活动经费) }}</el-descriptions-item>
          <el-descriptions-item label="讲座地点" :span="2">{{ selectedLecture.讲座地点 }}</el-descriptions-item>
          <el-descriptions-item label="讲座时间" :span="2">
            {{ formatDateTime(selectedLecture.开始时间) }} ~ {{ formatDateTime(selectedLecture.结束时间) }}
          </el-descriptions-item>
          <el-descriptions-item label="本院主办人">{{ selectedLecture.主办人姓名 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="所属科室/部门">{{ selectedLecture.所属部门名称 || '-' }}</el-descriptions-item>
        </el-descriptions>

        <el-divider />

        <el-descriptions title="主讲专家档案" :column="2" border>
          <el-descriptions-item label="专家姓名">{{ selectedLecture.主讲人姓名 }}</el-descriptions-item>
          <el-descriptions-item label="专家国别">{{ selectedLecture.主讲人国别 || '中国' }}</el-descriptions-item>
          <el-descriptions-item label="专家单位" :span="2">{{ selectedLecture.主讲人单位 }}</el-descriptions-item>
          <el-descriptions-item label="最高学位">{{ selectedLecture.主讲人学位 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="专业职称">{{ selectedLecture.主讲人职称 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="专家简历" :span="2">
            <span v-if="selectedLecture.主讲人简历路径">
              <el-link type="primary" :href="selectedLecture.主讲人简历路径" target="_blank">
                查看/下载专家个人简历
              </el-link>
            </span>
            <span v-else class="text-muted">未上传独立简历附件</span>
          </el-descriptions-item>
        </el-descriptions>

        <el-divider />

        <div class="content-summary-box">
          <h4>学术主旨概述</h4>
          <p>{{ selectedLecture.内容概述 || '暂无内容概述' }}</p>
        </div>

        <el-divider />

        <!-- 审核记录时间轴 -->
        <h4>审批与流转轨迹</h4>
        <el-timeline v-if="lectureApprovalSteps.length > 0" class="approval-timeline">
          <el-timeline-item
            v-for="(step, sIdx) in lectureApprovalSteps"
            :key="sIdx"
            :timestamp="step.执行时间 || step.审核时间 || '待审核'"
            :type="step.状态说明 === '通过' ? 'success' : 'primary'"
          >
            <strong>{{ step.名称 || step.步骤名称 }}</strong>
            <p class="step-desc">
              处理人：{{ step.姓名 || step.处理人 || '-' }} | 状态：{{ step.状态说明 || '处理中' }}
              <span v-if="step.备注"> ({{ step.备注 }})</span>
            </p>
          </el-timeline-item>
        </el-timeline>
        <p v-else class="text-muted">暂无历史审核审批记录</p>
      </div>
    </el-drawer>

    <!-- 主办讲座反馈登记弹窗 -->
    <el-dialog
      v-model="lectureFeedbackDialogVisible"
      title="登记主办讲座到会效果与报酬反馈"
      width="720px"
      :close-on-click-modal="false"
    >
      <div v-if="feedbackLectureTarget" class="target-banner">
        <strong>当前讲座：</strong>{{ feedbackLectureTarget.活动名称 }}
        <span class="banner-sub">主讲人：{{ feedbackLectureTarget.主讲人姓名 }} ({{ feedbackLectureTarget.主讲人单位 }})</span>
      </div>

      <el-form ref="lectureFeedbackFormRef" :model="lectureFeedbackForm" :rules="lectureFeedbackRules" label-width="130px">
        <h4 class="form-section-title">1. 到会规模与持续时长</h4>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="持续时间 (分钟)" prop="活动持续分钟">
              <el-input-number v-model="lectureFeedbackForm.活动持续分钟" :min="10" :max="600" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="到会参会人数" prop="参会人数">
              <el-input-number v-model="lectureFeedbackForm.参会人数" :min="1" :max="2000" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>

        <h4 class="form-section-title">2. 专家报酬与结算</h4>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="专家姓名" prop="专家姓名">
              <el-input v-model="lectureFeedbackForm.专家姓名" placeholder="收款专家姓名" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="专家身份证号码">
              <el-input v-model="lectureFeedbackForm.专家身份证号码" placeholder="用于劳务报酬个税代扣代缴" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="外籍专家护照号">
              <el-input v-model="lectureFeedbackForm.专家护照号" placeholder="外籍专家填写" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="联系电话">
              <el-input v-model="lectureFeedbackForm.专家联系方式" placeholder="手机号码" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="讲课费金额 (元)">
              <el-input-number v-model="lectureFeedbackForm.专家讲课费金额" :min="0" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="结算方式">
              <el-select v-model="lectureFeedbackForm.结算方式" placeholder="选择发放方式" style="width: 100%">
                <el-option label="银行转账" value="转账" />
                <el-option label="现金结算" value="现金" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <h4 class="form-section-title">3. 效果评价与附件讲稿</h4>
        <el-form-item label="讲座评价总结">
          <el-input
            v-model="lectureFeedbackForm.讲座效果"
            type="textarea"
            :rows="3"
            placeholder="总结学术互动交流反响、临床指导意义与受众满意度"
          />
        </el-form-item>
        <el-form-item label="讲座PPT/附件">
          <el-upload
            action=""
            :show-file-list="false"
            :http-request="customUploadLectureAttachment"
          >
            <el-button type="info" size="small">上传讲稿附件/签到表</el-button>
            <span v-if="lectureFeedbackForm.讲座附件路径" class="upload-filename">
              已上传附件
            </span>
          </el-upload>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="lectureFeedbackDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmitLectureFeedback">提交反馈归档</el-button>
      </template>
    </el-dialog>

    <!-- 讲座反馈详情抽屉 -->
    <el-drawer v-model="lectureFeedbackDetailDrawerVisible" title="讲座效果与费用反馈详情" size="620px">
      <div v-if="selectedLectureFeedback" class="detail-container">
        <el-descriptions title="讲座到会与费用反馈" :column="2" border>
          <el-descriptions-item label="讲座活动名称" :span="2">
            <strong>{{ selectedLectureFeedback.活动名称 || '学术讲座' }}</strong>
          </el-descriptions-item>
          <el-descriptions-item label="主讲专家">{{ selectedLectureFeedback.专家姓名 }}</el-descriptions-item>
          <el-descriptions-item label="主办人">{{ selectedLectureFeedback.主办人姓名 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="到会人次">{{ selectedLectureFeedback.参会人数 }} 人</el-descriptions-item>
          <el-descriptions-item label="持续时长">{{ selectedLectureFeedback.活动持续分钟 }} 分钟</el-descriptions-item>
          <el-descriptions-item label="专家讲课报酬">¥ {{ formatMoney(selectedLectureFeedback.专家讲课费金额) }}</el-descriptions-item>
          <el-descriptions-item label="结算方式">{{ selectedLectureFeedback.结算方式 || '转账' }}</el-descriptions-item>
          <el-descriptions-item label="专家联系电话" :span="2">{{ selectedLectureFeedback.专家联系方式 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="讲稿/签到材料" :span="2">
            <el-link v-if="selectedLectureFeedback.讲座附件路径" type="primary" :href="selectedLectureFeedback.讲座附件路径" target="_blank">
              下载讲稿材料或签到照片
            </el-link>
            <span v-else class="text-muted">未提交独立附件</span>
          </el-descriptions-item>
          <el-descriptions-item label="讲座成效总结" :span="2">
            <p class="remarks-box">{{ selectedLectureFeedback.讲座效果 || '暂无评价' }}</p>
          </el-descriptions-item>
        </el-descriptions>
      </div>
    </el-drawer>

    <!-- 申请参加学术研讨会弹窗向导 -->
    <el-dialog
      v-model="confDialogVisible"
      title="申请参加国内外学术会议"
      width="780px"
      :close-on-click-modal="false"
    >
      <el-form ref="confFormRef" :model="confForm" :rules="confRules" label-width="120px">
        <h4 class="form-section-title">1. 会议基本信息</h4>
        <el-form-item label="学术会议名称" prop="会议名称">
          <el-input v-model="confForm.会议名称" placeholder="输入学术研讨会官方全称" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="会议主办单位" prop="会议主办单位">
              <el-input v-model="confForm.会议主办单位" placeholder="如: 中华医学会 / 中国医师协会" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="会议承办单位">
              <el-input v-model="confForm.会议承办单位" placeholder="承办大学、医院或专业委员会" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="研讨学科领域">
              <el-input v-model="confForm.研讨领域" placeholder="如: 精准肿瘤学 / 心血管介入" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="学科类型">
              <el-select v-model="confForm.学科类型" placeholder="请选择学科门类" style="width: 100%">
                <el-option label="临床医学" value="临床医学" />
                <el-option label="基础医学" value="基础医学" />
                <el-option label="药学与临床药理" value="药学与临床药理" />
                <el-option label="公共卫生与预防医学" value="公共卫生与预防医学" />
                <el-option label="生物医学工程" value="生物医学工程" />
                <el-option label="医工交叉学科" value="医工交叉学科" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <h4 class="form-section-title">2. 会期、往返及会议地点</h4>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="会议开始日期" prop="会议开始时间">
              <el-date-picker v-model="confForm.会议开始时间" type="date" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="会议结束日期" prop="会议结束时间">
              <el-date-picker v-model="confForm.会议结束时间" type="date" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="往返行程天数">
              <el-input-number v-model="confForm.往返时间" :min="1" :max="30" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="会议官方网址">
              <el-input v-model="confForm.会议网址" placeholder="http(s)://..." />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="会议举办地点" prop="会议地址">
          <el-input v-model="confForm.会议地址" placeholder="如: 北京国家会议中心" />
        </el-form-item>

        <h4 class="form-section-title">3. 参会事由与征文附件</h4>
        <el-form-item label="参会必要性说明">
          <el-input
            v-model="confForm.会议简介及申请理由"
            type="textarea"
            :rows="3"
            placeholder="说明参会目的、是否有大会口头报告、论文壁报展示或担任分论坛主持等 (500字以内)"
          />
        </el-form-item>
        <el-form-item label="会议通知/邀请函">
          <el-upload
            action=""
            :show-file-list="false"
            :http-request="customUploadConferenceFile"
          >
            <el-button type="info" size="small">上传正式会议通知或录用通知</el-button>
            <span v-if="confForm.参会相关文件路径" class="upload-filename">
              已关联附件
            </span>
          </el-upload>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="confDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmitConference">提交参会审批</el-button>
      </template>
    </el-dialog>

    <!-- 参加会议详情抽屉 -->
    <el-drawer v-model="confDetailDrawerVisible" title="参加学术会议详情档案" size="680px">
      <div v-if="selectedConf" class="detail-container">
        <el-descriptions title="会议基本信息" :column="2" border>
          <el-descriptions-item label="会议全称" :span="2">
            <strong>{{ selectedConf.会议名称 }}</strong>
          </el-descriptions-item>
          <el-descriptions-item label="主办单位">{{ selectedConf.会议主办单位 }}</el-descriptions-item>
          <el-descriptions-item label="承办单位">{{ selectedConf.会议承办单位 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="研讨领域">{{ selectedConf.研讨领域 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="学科类型">{{ selectedConf.学科类型 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="会期起止" :span="2">
            {{ formatDate(selectedConf.会议开始时间) }} ~ {{ formatDate(selectedConf.会议结束时间) }} (往返行程约 {{ selectedConf.往返时间 || 2 }} 天)
          </el-descriptions-item>
          <el-descriptions-item label="会议地点" :span="2">{{ selectedConf.会议地址 }}</el-descriptions-item>
          <el-descriptions-item label="官方网址" :span="2">
            <el-link v-if="selectedConf.会议网址" type="primary" :href="selectedConf.会议网址" target="_blank">
              {{ selectedConf.会议网址 }}
            </el-link>
            <span v-else class="text-muted">未提供网址</span>
          </el-descriptions-item>
          <el-descriptions-item label="参会申请人">{{ selectedConf.申请人姓名 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="申请人科室">{{ selectedConf.申请人部门名称 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="参会正式文件" :span="2">
            <el-link v-if="selectedConf.参会相关文件路径" type="primary" :href="selectedConf.参会相关文件路径" target="_blank">
              查看/下载正式会议通知与录用函
            </el-link>
            <span v-else class="text-muted">未上传独立通知文件</span>
          </el-descriptions-item>
          <el-descriptions-item label="参会理由说明" :span="2">
            <p class="remarks-box">{{ selectedConf.会议简介及申请理由 || '暂无详细理由说明' }}</p>
          </el-descriptions-item>
        </el-descriptions>

        <el-divider />

        <!-- 申请审批时间轴 -->
        <h4>审批与流转轨迹</h4>
        <el-timeline v-if="confApprovalSteps.length > 0" class="approval-timeline">
          <el-timeline-item
            v-for="(step, sIdx) in confApprovalSteps"
            :key="sIdx"
            :timestamp="step.执行时间 || step.审核时间 || '待审核'"
            :type="step.状态说明 === '通过' ? 'success' : 'primary'"
          >
            <strong>{{ step.名称 || step.步骤名称 }}</strong>
            <p class="step-desc">
              审批人：{{ step.姓名 || step.处理人 || '-' }} | 状态：{{ step.状态说明 || '审批中' }}
              <span v-if="step.备注"> ({{ step.备注 }})</span>
            </p>
          </el-timeline-item>
        </el-timeline>
        <p v-else class="text-muted">暂无审批流转记录</p>
      </div>
    </el-drawer>

    <!-- 参加会议总结与交流反馈登记弹窗 -->
    <el-dialog
      v-model="confFeedbackDialogVisible"
      title="登记参会总结与学术交流成果反馈"
      width="720px"
      :close-on-click-modal="false"
    >
      <div v-if="feedbackConfTarget" class="target-banner">
        <strong>参会项目：</strong>{{ feedbackConfTarget.会议名称 }}
        <span class="banner-sub">主办方：{{ feedbackConfTarget.会议主办单位 }}</span>
      </div>

      <el-form ref="confFeedbackFormRef" :model="confFeedbackForm" :rules="confFeedbackRules" label-width="120px">
        <h4 class="form-section-title">1. 会议规模</h4>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="活动持续 (分)">
              <el-input-number v-model="confFeedbackForm.活动持续分钟" :min="30" :max="5000" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="参会总规模 (人)">
              <el-input-number v-model="confFeedbackForm.参会人数" :min="1" :max="10000" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>

        <h4 class="form-section-title">2. 参会收获与成果总结</h4>
        <el-form-item label="交流总结报告" prop="参会总结">
          <el-input
            v-model="confFeedbackForm.参会总结"
            type="textarea"
            :rows="5"
            placeholder="详细阐述口头报告反响、论文壁报交流、前沿技术追踪及对科室临床科研发展的建议"
          />
        </el-form-item>
        <el-form-item label="会议资料附件">
          <el-upload
            action=""
            :show-file-list="false"
            :http-request="customUploadConferenceFeedbackFile"
          >
            <el-button type="info" size="small">上传现场照片/PPT/大会论文集</el-button>
            <span v-if="confFeedbackForm.会议反馈文件路径" class="upload-filename">
              已上传反馈材料
            </span>
          </el-upload>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="confFeedbackDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmitConferenceFeedback">提交参会总结归档</el-button>
      </template>
    </el-dialog>

    <!-- 会议反馈详情抽屉 -->
    <el-drawer v-model="confFeedbackDetailDrawerVisible" title="参会学术交流总结与成果反馈" size="620px">
      <div v-if="selectedConfFeedback" class="detail-container">
        <el-descriptions title="参会交流成果归档" :column="2" border>
          <el-descriptions-item label="会议全称" :span="2">
            <strong>{{ selectedConfFeedback.会议名称 || '学术研讨会' }}</strong>
          </el-descriptions-item>
          <el-descriptions-item label="报告参会人">{{ selectedConfFeedback.申请人姓名 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="会议总规模">{{ selectedConfFeedback.参会人数 || '-' }} 人</el-descriptions-item>
          <el-descriptions-item label="持续时长" :span="2">{{ selectedConfFeedback.活动持续分钟 || '-' }} 分钟</el-descriptions-item>
          <el-descriptions-item label="参会反馈文件" :span="2">
            <el-link v-if="selectedConfFeedback.会议反馈文件路径" type="primary" :href="selectedConfFeedback.会议反馈文件路径" target="_blank">
              下载参会照片/成果材料
            </el-link>
            <span v-else class="text-muted">未上传独立附件</span>
          </el-descriptions-item>
          <el-descriptions-item label="学术总结报告" :span="2">
            <p class="remarks-box">{{ selectedConfFeedback.参会总结 || '暂无总结' }}</p>
          </el-descriptions-item>
        </el-descriptions>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { Plus, Search, Refresh, Download } from '@element-plus/icons-vue'
import { ElMessage, type FormInstance } from 'element-plus'
import {
  getLectures,
  getLectureDetail,
  applyLecture,
  uploadLectureResume,
  getLectureFeedbacks,
  getLectureFeedbackDetail,
  submitLectureFeedback,
  uploadLectureAttachment,
  getConferences,
  getConferenceDetail,
  applyConference,
  uploadConferenceFile,
  getConferenceFeedbacks,
  getConferenceFeedbackDetail,
  submitConferenceFeedback,
  uploadConferenceFeedbackFile,
  type LectureItem,
  type LectureFeedbackItem,
  type ConferenceItem,
  type ConferenceFeedbackItem
} from '@/api/activities'
import dayjs from 'dayjs'
import { exportToCsv, type ExportColumn } from '@/utils/export'

const route = useRoute()

const activeTab = ref('lectures')
const lectureLoading = ref(false)
const lectureFeedbackLoading = ref(false)
const confLoading = ref(false)
const confFeedbackLoading = ref(false)
const submitLoading = ref(false)

const lectureList = ref<LectureItem[]>([])
const lectureFeedbackList = ref<LectureFeedbackItem[]>([])
const confList = ref<ConferenceItem[]>([])
const confFeedbackList = ref<ConferenceFeedbackItem[]>([])

const lectureTotal = ref(0)
const lectureFeedbackTotal = ref(0)
const confTotal = ref(0)
const confFeedbackTotal = ref(0)

const lectureDialogVisible = ref(false)
const lectureDetailDrawerVisible = ref(false)
const lectureFeedbackDialogVisible = ref(false)
const lectureFeedbackDetailDrawerVisible = ref(false)

const confDialogVisible = ref(false)
const confDetailDrawerVisible = ref(false)
const confFeedbackDialogVisible = ref(false)
const confFeedbackDetailDrawerVisible = ref(false)

const selectedLecture = ref<LectureItem | null>(null)
const lectureApprovalSteps = ref<any[]>([])

const feedbackLectureTarget = ref<LectureItem | null>(null)
const selectedLectureFeedback = ref<LectureFeedbackItem | null>(null)

const selectedConf = ref<ConferenceItem | null>(null)
const confApprovalSteps = ref<any[]>([])

const feedbackConfTarget = ref<ConferenceItem | null>(null)
const selectedConfFeedback = ref<ConferenceFeedbackItem | null>(null)

const lecturePaging = reactive({ pageIndex: 1, pageSize: 10 })
const lectureFeedbackPaging = reactive({ pageIndex: 1, pageSize: 10 })
const confPaging = reactive({ pageIndex: 1, pageSize: 10 })
const confFeedbackPaging = reactive({ pageIndex: 1, pageSize: 10 })

const lectureQuery = reactive({ like活动名称: '', like主办人姓名: '' })
const lectureFeedbackFilter = ref('')

const lectureFeedbackQuery = reactive({ like活动名称: '', like主办人姓名: '', like专家姓名: '' })
const confQuery = reactive({ like会议名称: '', like申请人姓名: '' })
const confFeedbackFilter = ref('')

const confFeedbackQuery = reactive({ like会议名称: '', like申请人姓名: '' })

const lectureFormRef = ref<FormInstance>()
const lectureFeedbackFormRef = ref<FormInstance>()
const confFormRef = ref<FormInstance>()
const confFeedbackFormRef = ref<FormInstance>()

const lectureForm = reactive({
  活动名称: '',
  会议类型: '院级学术讲座',
  主讲人姓名: '',
  主讲人单位: '',
  主讲人职称: '',
  主讲人学位: '博士',
  主讲人国别: '中国',
  主讲人简历路径: '',
  讲座地点: '',
  活动对象: '全院医护、科研人员及在读研究生',
  活动经费: 3000,
  开始时间: new Date(Date.now() + 3 * 24 * 3600 * 1000),
  结束时间: new Date(Date.now() + 3 * 24 * 3600 * 1000 + 2 * 3600 * 1000),
  内容概述: ''
})

const lectureFeedbackForm = reactive<Partial<LectureFeedbackItem>>({
  讲座编号: 0,
  专家姓名: '',
  专家身份证号码: '',
  专家护照号: '',
  专家联系方式: '',
  专家讲课费金额: 2000,
  结算方式: '转账',
  活动持续分钟: 90,
  参会人数: 60,
  讲座效果: '',
  讲座附件路径: ''
})

const confForm = reactive({
  会议名称: '',
  会议主办单位: '',
  会议承办单位: '',
  研讨领域: '',
  学科类型: '临床医学',
  会议地址: '',
  往返时间: 3,
  会议网址: '',
  会议开始时间: new Date(Date.now() + 7 * 24 * 3600 * 1000),
  会议结束时间: new Date(Date.now() + 9 * 24 * 3600 * 1000),
  会议简介及申请理由: '',
  参会相关文件路径: ''
})

const confFeedbackForm = reactive<Partial<ConferenceFeedbackItem>>({
  参加会议编号: 0,
  活动持续分钟: 720,
  参会人数: 300,
  参会总结: '',
  会议反馈文件路径: ''
})

const lectureRules = {
  活动名称: [{ required: true, message: '请输入讲座名称', trigger: 'blur' }],
  主讲人姓名: [{ required: true, message: '请输入主讲专家姓名', trigger: 'blur' }],
  讲座地点: [{ required: true, message: '请输入讲座举办地点', trigger: 'blur' }]
}

const lectureFeedbackRules = {
  专家姓名: [{ required: true, message: '请输入收款专家姓名', trigger: 'blur' }],
  活动持续分钟: [{ required: true, message: '请输入讲座持续时间', trigger: 'blur' }],
  参会人数: [{ required: true, message: '请输入到会参会人数', trigger: 'blur' }]
}

const confRules = {
  会议名称: [{ required: true, message: '请输入会议官方全称', trigger: 'blur' }],
  会议主办单位: [{ required: true, message: '请输入会议主办单位', trigger: 'blur' }],
  会议地址: [{ required: true, message: '请输入会议地点', trigger: 'blur' }]
}

const confFeedbackRules = {
  参会总结: [{ required: true, message: '请详细阐述参会总结与交流收获', trigger: 'blur' }]
}

// KPI 统计
const totalLectureBudget = computed(() => {
  return lectureList.value.reduce((sum, item) => sum + (Number(item.活动经费) || 0), 0)
})

const lectureFeedbackRate = computed(() => {
  if (lectureList.value.length === 0) return '100'
  const fed = lectureList.value.filter(i => i.是否反馈过).length
  return ((fed / lectureList.value.length) * 100).toFixed(1)
})

const confFeedbackRate = computed(() => {
  if (confList.value.length === 0) return '100'
  const fed = confList.value.filter(i => i.是否反馈过).length
  return ((fed / confList.value.length) * 100).toFixed(1)
})

function formatMoney(val: number | undefined) {
  if (val === undefined || val === null) return '0.00'
  return Number(val).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatDate(val: string | Date | undefined) {
  if (!val) return '-'
  return dayjs(val).format('YYYY-MM-DD')
}

function formatDateTime(val: string | Date | undefined) {
  if (!val) return '-'
  return dayjs(val).format('YYYY-MM-DD HH:mm')
}

// 主办讲座查询
async function fetchLectures() {
  lectureLoading.value = true
  try {
    const isFeedback = lectureFeedbackFilter.value !== '' ? lectureFeedbackFilter.value === 'true' : undefined
    const res: any = await getLectures(lecturePaging, {
      ...lectureQuery,
      是否反馈过: isFeedback
    })
    const data = res?.data ?? res
    lectureList.value = Array.isArray(data?.list) ? data.list : (Array.isArray(data) ? data : [])
    lectureTotal.value = Number(data?.total) || lectureList.value.length
  } catch (err: any) {
    lectureList.value = []
    lectureTotal.value = 0
    ElMessage.error(err?.response?.data?.error || err?.message || '讲座列表查询失败，请稍后重试')
  } finally {
    lectureLoading.value = false
  }
}

// 讲座反馈查询
async function fetchLectureFeedbacks() {
  lectureFeedbackLoading.value = true
  try {
    const res: any = await getLectureFeedbacks(lectureFeedbackPaging, lectureFeedbackQuery)
    const data = res?.data ?? res
    lectureFeedbackList.value = Array.isArray(data?.list) ? data.list : (Array.isArray(data) ? data : [])
    lectureFeedbackTotal.value = Number(data?.total) || lectureFeedbackList.value.length
  } catch (err: any) {
    lectureFeedbackList.value = []
    lectureFeedbackTotal.value = 0
    ElMessage.error(err?.response?.data?.error || err?.message || '讲座反馈查询失败，请稍后重试')
  } finally {
    lectureFeedbackLoading.value = false
  }
}

// 参加学术会议查询
async function fetchConferences() {
  confLoading.value = true
  try {
    const isFeedback = confFeedbackFilter.value !== '' ? confFeedbackFilter.value === 'true' : undefined
    const res: any = await getConferences(confPaging, {
      ...confQuery,
      是否反馈过: isFeedback
    })
    const data = res?.data ?? res
    confList.value = Array.isArray(data?.list) ? data.list : (Array.isArray(data) ? data : [])
    confTotal.value = Number(data?.total) || confList.value.length
  } catch (err: any) {
    confList.value = []
    confTotal.value = 0
    ElMessage.error(err?.response?.data?.error || err?.message || '学术会议列表查询失败，请稍后重试')
  } finally {
    confLoading.value = false
  }
}

// 会议反馈查询
async function fetchConfFeedbacks() {
  confFeedbackLoading.value = true
  try {
    const res: any = await getConferenceFeedbacks(confFeedbackPaging, confFeedbackQuery)
    const data = res?.data ?? res
    confFeedbackList.value = Array.isArray(data?.list) ? data.list : (Array.isArray(data) ? data : [])
    confFeedbackTotal.value = Number(data?.total) || confFeedbackList.value.length
  } catch (err: any) {
    confFeedbackList.value = []
    confFeedbackTotal.value = 0
    ElMessage.error(err?.response?.data?.error || err?.message || '会议反馈查询失败，请稍后重试')
  } finally {
    confFeedbackLoading.value = false
  }
}

function handleResetLectureQuery() {
  lectureQuery.like活动名称 = ''
  lectureQuery.like主办人姓名 = ''
  lectureFeedbackFilter.value = ''
  lecturePaging.pageIndex = 1
  fetchLectures()
}

function handleResetLectureFeedbackQuery() {
  lectureFeedbackQuery.like活动名称 = ''
  lectureFeedbackQuery.like主办人姓名 = ''
  lectureFeedbackQuery.like专家姓名 = ''
  lectureFeedbackPaging.pageIndex = 1
  fetchLectureFeedbacks()
}

function handleResetConfQuery() {
  confQuery.like会议名称 = ''
  confQuery.like申请人姓名 = ''
  confFeedbackFilter.value = ''
  confPaging.pageIndex = 1
  fetchConferences()
}

function handleResetConfFeedbackQuery() {
  confFeedbackQuery.like会议名称 = ''
  confFeedbackQuery.like申请人姓名 = ''
  confFeedbackPaging.pageIndex = 1
  fetchConfFeedbacks()
}

function handleTabChange(name: any) {
  if (name === 'lectures') fetchLectures()
  else if (name === 'lecture-feedbacks') fetchLectureFeedbacks()
  else if (name === 'conferences') fetchConferences()
  else if (name === 'conf-feedbacks') fetchConfFeedbacks()
}

function handleOpenApplyLecture() {
  lectureDialogVisible.value = true
}

function handleOpenApplyConference() {
  confDialogVisible.value = true
}

async function handleViewLectureDetail(row: LectureItem) {
  selectedLecture.value = row
  lectureApprovalSteps.value = []
  lectureDetailDrawerVisible.value = true
  try {
    const res: any = await getLectureDetail(row.编号)
    if (res && res.data) {
      selectedLecture.value = res.data.学术任职详情 || res.data || row
      lectureApprovalSteps.value = res.data.审核情况 || []
    }
  } catch {
    // 保留表格原始行
  }
}

function handleOpenLectureFeedback(row: LectureItem) {
  feedbackLectureTarget.value = row
  lectureFeedbackForm.讲座编号 = row.编号
  lectureFeedbackForm.专家姓名 = row.主讲人姓名 || ''
  lectureFeedbackForm.专家讲课费金额 = row.活动经费 ? row.活动经费 * 0.7 : 2000
  lectureFeedbackForm.活动持续分钟 = 90
  lectureFeedbackForm.参会人数 = 50
  lectureFeedbackForm.讲座效果 = ''
  lectureFeedbackDialogVisible.value = true
}

async function handleViewLectureFeedbackDetail(row: LectureFeedbackItem) {
  selectedLectureFeedback.value = row
  lectureFeedbackDetailDrawerVisible.value = true
  if (row.编号) {
    try {
      const res: any = await getLectureFeedbackDetail(row.编号)
      if (res && res.data) {
        selectedLectureFeedback.value = res.data.讲座反馈详情 || res.data || row
      }
    } catch {
      // 保持当前数据
    }
  }
}

async function handleViewConfDetail(row: ConferenceItem) {
  selectedConf.value = row
  confApprovalSteps.value = []
  confDetailDrawerVisible.value = true
  try {
    const res: any = await getConferenceDetail(row.编号)
    if (res && res.data) {
      selectedConf.value = res.data.参加会议详情 || res.data || row
      confApprovalSteps.value = res.data.申请情况 || []
    }
  } catch {
    // 保留表格原始行
  }
}

function handleOpenConfFeedback(row: ConferenceItem) {
  feedbackConfTarget.value = row
  confFeedbackForm.参加会议编号 = row.编号
  confFeedbackForm.活动持续分钟 = 720
  confFeedbackForm.参会人数 = 200
  confFeedbackForm.参会总结 = ''
  confFeedbackDialogVisible.value = true
}

async function handleViewConfFeedbackDetail(row: ConferenceFeedbackItem) {
  selectedConfFeedback.value = row
  confFeedbackDetailDrawerVisible.value = true
  if (row.编号) {
    try {
      const res: any = await getConferenceFeedbackDetail(row.编号)
      if (res && res.data) {
        selectedConfFeedback.value = res.data.参加会议反馈详情 || res.data || row
      }
    } catch {
      // 保持当前数据
    }
  }
}

// 简历与附件上传处理
async function customUploadResume(options: any) {
  try {
    const res: any = await uploadLectureResume(options.file)
    const files = res?.data || res
    if (Array.isArray(files) && files.length > 0) {
      lectureForm.主讲人简历路径 = files[0]
      ElMessage.success('简历上传成功！')
    } else {
      lectureForm.主讲人简历路径 = `/upload/学术活动/主讲人简历/${options.file.name}`
      ElMessage.success('简历文件已关联')
    }
  } catch (err: any) {
    ElMessage.error(err.message || '上传简历失败')
  }
}

async function customUploadLectureAttachment(options: any) {
  try {
    const res: any = await uploadLectureAttachment(options.file)
    const files = res?.data || res
    if (Array.isArray(files) && files.length > 0) {
      lectureFeedbackForm.讲座附件路径 = files[0]
      ElMessage.success('讲稿材料上传成功！')
    } else {
      lectureFeedbackForm.讲座附件路径 = `/upload/学术活动/讲座附件/${options.file.name}`
      ElMessage.success('讲稿材料已关联')
    }
  } catch (err: any) {
    ElMessage.error(err.message || '上传附件失败')
  }
}

async function customUploadConferenceFile(options: any) {
  try {
    const res: any = await uploadConferenceFile(options.file)
    const files = res?.data || res
    if (Array.isArray(files) && files.length > 0) {
      confForm.参会相关文件路径 = files[0]
      ElMessage.success('参会正式文件上传成功！')
    } else {
      confForm.参会相关文件路径 = `/upload/学术活动/参会相关文件/${options.file.name}`
      ElMessage.success('参会正式文件已关联')
    }
  } catch (err: any) {
    ElMessage.error(err.message || '上传失败')
  }
}

async function customUploadConferenceFeedbackFile(options: any) {
  try {
    const res: any = await uploadConferenceFeedbackFile(options.file)
    const files = res?.data || res
    if (Array.isArray(files) && files.length > 0) {
      confFeedbackForm.会议反馈文件路径 = files[0]
      ElMessage.success('参会交流成果上传成功！')
    } else {
      confFeedbackForm.会议反馈文件路径 = `/upload/学术活动/会议反馈文件/${options.file.name}`
      ElMessage.success('参会交流成果已关联')
    }
  } catch (err: any) {
    ElMessage.error(err.message || '上传失败')
  }
}

// 提交主办讲座
async function handleSubmitLecture() {
  if (!lectureFormRef.value) return
  await lectureFormRef.value.validate(async valid => {
    if (!valid) return
    submitLoading.value = true
    try {
      await applyLecture({
        ...lectureForm,
        开始时间: dayjs(lectureForm.开始时间).toISOString(),
        结束时间: dayjs(lectureForm.结束时间).toISOString()
      })
      ElMessage.success('主办学术讲座申请已提交审核流程！')
      lectureDialogVisible.value = false
      fetchLectures()
    } catch (err: any) {
      ElMessage.error(err.message || '提交失败')
    } finally {
      submitLoading.value = false
    }
  })
}

// 提交讲座反馈
async function handleSubmitLectureFeedback() {
  if (!lectureFeedbackFormRef.value) return
  await lectureFeedbackFormRef.value.validate(async valid => {
    if (!valid) return
    submitLoading.value = true
    try {
      await submitLectureFeedback(lectureFeedbackForm)
      ElMessage.success('讲座到会效果与报酬反馈已提交审核！')
      lectureFeedbackDialogVisible.value = false
      fetchLectures()
      fetchLectureFeedbacks()
    } catch (err: any) {
      ElMessage.error(err.message || '提交失败')
    } finally {
      submitLoading.value = false
    }
  })
}

// 提交参加会议申请
async function handleSubmitConference() {
  if (!confFormRef.value) return
  await confFormRef.value.validate(async valid => {
    if (!valid) return
    submitLoading.value = true
    try {
      await applyConference({
        ...confForm,
        会议开始时间: dayjs(confForm.会议开始时间).toISOString(),
        会议结束时间: dayjs(confForm.会议结束时间).toISOString()
      })
      ElMessage.success('参加学术研讨会申请已提交审批！')
      confDialogVisible.value = false
      fetchConferences()
    } catch (err: any) {
      ElMessage.error(err.message || '提交失败')
    } finally {
      submitLoading.value = false
    }
  })
}

// 提交会议反馈总结
async function handleSubmitConferenceFeedback() {
  if (!confFeedbackFormRef.value) return
  await confFeedbackFormRef.value.validate(async valid => {
    if (!valid) return
    submitLoading.value = true
    try {
      await submitConferenceFeedback(confFeedbackForm)
      ElMessage.success('参会交流成果与总结已归档归口！')
      confFeedbackDialogVisible.value = false
      fetchConferences()
      fetchConfFeedbacks()
    } catch (err: any) {
      ElMessage.error(err.message || '提交失败')
    } finally {
      submitLoading.value = false
    }
  })
}

// CSV 导出
function handleExportCurrentTab() {
  if (activeTab.value === 'lectures' || activeTab.value === 'ledger') {
    if (lectureList.value.length === 0) {
      ElMessage.warning('暂无讲座数据可导出')
      return
    }
    const columns: ExportColumn[] = [
      { label: '编号', prop: '编号' },
      { label: '讲座题目', prop: '活动名称' },
      { label: '主讲专家', prop: '主讲人姓名' },
      { label: '专家单位', prop: '主讲人单位' },
      { label: '职称', prop: '主讲人职称' },
      { label: '地点', prop: '讲座地点' },
      { label: '开始时间', prop: '开始时间', formatter: (val: any) => formatDateTime(val) },
      { label: '活动预算', prop: '活动经费' },
      { label: '反馈状态', prop: '是否反馈过', formatter: (val: any) => (val ? '已反馈' : '待反馈') }
    ]
    exportToCsv('主办学术讲座台账', columns, lectureList.value)
    ElMessage.success('已导出讲座清单！')
  } else if (activeTab.value === 'conferences') {
    if (confList.value.length === 0) {
      ElMessage.warning('暂无会议数据可导出')
      return
    }
    const columns: ExportColumn[] = [
      { label: '编号', prop: '编号' },
      { label: '会议名称', prop: '会议名称' },
      { label: '主办单位', prop: '会议主办单位' },
      { label: '研讨领域', prop: '研讨领域' },
      { label: '地点', prop: '会议地址' },
      { label: '开始日期', prop: '会议开始时间', formatter: (val: any) => formatDate(val) },
      { label: '结束日期', prop: '会议结束时间', formatter: (val: any) => formatDate(val) },
      { label: '往返天数', prop: '往返时间' },
      { label: '反馈状态', prop: '是否反馈过', formatter: (val: any) => (val ? '已归档' : '待归档') }
    ]
    exportToCsv('参加学术会议台账', columns, confList.value)
    ElMessage.success('已导出会议清单！')
  } else if (activeTab.value === 'lecture-feedbacks') {
    if (lectureFeedbackList.value.length === 0) {
      ElMessage.warning('暂无讲座反馈数据可导出')
      return
    }
    const columns: ExportColumn[] = [
      { label: '活动名称', prop: '活动名称' },
      { label: '主讲专家', prop: '专家姓名' },
      { label: '主办人', prop: '主办人姓名' },
      { label: '到会人次', prop: '参会人数' },
      { label: '持续时长(分)', prop: '活动持续分钟' },
      { label: '讲课报酬', prop: '专家讲课费金额' },
      { label: '结算方式', prop: '结算方式' },
      { label: '评价效果', prop: '讲座效果' }
    ]
    exportToCsv('讲座到会与费用反馈台账', columns, lectureFeedbackList.value)
    ElMessage.success('已导出讲座反馈记录！')
  } else if (activeTab.value === 'conf-feedbacks') {
    if (confFeedbackList.value.length === 0) {
      ElMessage.warning('暂无参会反馈数据可导出')
      return
    }
    const columns: ExportColumn[] = [
      { label: '会议名称', prop: '会议名称' },
      { label: '参会人', prop: '申请人姓名' },
      { label: '持续时间(分)', prop: '活动持续分钟' },
      { label: '参会规模', prop: '参会人数' },
      { label: '总结报告', prop: '参会总结' }
    ]
    exportToCsv('参会学术交流反馈台账', columns, confFeedbackList.value)
    ElMessage.success('已导出参会反馈台账！')
  }
}

onMounted(() => {
  if (route.query.tab) {
    activeTab.value = String(route.query.tab)
  }
  fetchLectures()
  fetchConferences()
  fetchLectureFeedbacks()
  fetchConfFeedbacks()

  if (route.query.action === 'apply-lecture') {
    handleOpenApplyLecture()
  } else if (route.query.action === 'apply-conf') {
    handleOpenApplyConference()
  }
})
</script>

<style scoped lang="scss">
.academic-activities-container {
  .header-card {
    margin-bottom: 16px;
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;

      h2 {
        margin: 0 0 6px 0;
        font-size: 18px;
        color: #303133;
      }
      .subtitle {
        margin: 0;
        font-size: 13px;
        color: #909399;
      }

      .header-actions {
        display: flex;
        gap: 12px;
      }
    }
  }

  .kpi-row {
    margin-bottom: 16px;
    .kpi-card {
      .kpi-label {
        font-size: 13px;
        color: #909399;
        margin-bottom: 8px;
      }
      .kpi-value {
        font-size: 22px;
        font-weight: 700;
        .unit {
          font-size: 13px;
          font-weight: normal;
          color: #909399;
        }
      }
      .text-primary { color: #409eff; }
      .text-danger { color: #f56c6c; }
      .text-warning { color: #e6a23c; }
      .text-success { color: #67c23a; }
      .text-info { color: #909399; }
    }
  }

  .main-card {
    .filter-form {
      margin-bottom: 16px;
    }

    .money-text {
      font-weight: 600;
      color: #e6a23c;
    }

    .pagination-container {
      margin-top: 16px;
      display: flex;
      justify-content: flex-end;
    }
  }

  .form-section-title {
    margin: 16px 0 12px 0;
    font-size: 14px;
    font-weight: 600;
    color: #409eff;
    border-left: 3px solid #409eff;
    padding-left: 8px;
  }

  .upload-filename {
    margin-left: 8px;
    font-size: 12px;
    color: #67c23a;
  }

  .target-banner {
    padding: 10px 14px;
    background-color: #ecf5ff;
    border: 1px solid #d9ecff;
    border-radius: 4px;
    margin-bottom: 16px;
    font-size: 13px;
    color: #303133;
    .banner-sub {
      display: block;
      margin-top: 4px;
      color: #606266;
      font-size: 12px;
    }
  }

  .detail-container {
    .content-summary-box {
      margin: 16px 0;
      h4 {
        margin: 0 0 8px 0;
        font-size: 14px;
        color: #303133;
      }
      p {
        margin: 0;
        line-height: 1.6;
        color: #606266;
      }
    }

    .approval-timeline {
      margin-top: 16px;
      .step-desc {
        margin: 4px 0 0 0;
        font-size: 12px;
        color: #909399;
      }
    }

    .remarks-box {
      margin: 0;
      line-height: 1.6;
      color: #606266;
    }
  }

  .ledger-container {
    .sub-table-card {
      .card-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        font-weight: 600;
      }
    }
  }

  .text-muted {
    color: #909399;
    font-size: 13px;
  }
}
</style>
