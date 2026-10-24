<template>
  <div class="new-tech-container">
    <!-- 头部横幅与大盘指标卡 -->
    <div class="header-card">
      <div class="header-info">
        <h2 class="page-title">
          <el-icon class="title-icon"><TrendCharts /></el-icon>
          医疗新技术新项目（三新）与适宜技术推广
        </h2>
        <p class="page-subtitle">
          聚焦二级医院等级评审与国考指标：规范院内新技术立项、医务/科教联合准入审查、临床例数随访（满15~30例）与安全性监测、期满转常规评奖，协同推进医共体适宜技术下沉基层。
        </p>
      </div>
      <div class="header-actions">
        <el-button type="primary" :icon="Plus" @click="openDeclareDialog">
          申报新技术新项目
        </el-button>
        <el-button type="success" plain :icon="Promotion" @click="openAppropriateTechDialog">
          登记适宜技术推广
        </el-button>
        <el-button :icon="Refresh" @click="refreshAll">刷新</el-button>
      </div>
    </div>

    <!-- 统计指标大盘 -->
    <el-row :gutter="16" class="metrics-row">
      <el-col :xs="12" :sm="6" :md="4">
        <el-card shadow="hover" class="metric-card">
          <div class="metric-label">新技术申报数</div>
          <div class="metric-value">{{ dashboardStats.新技术申报总数 || 0 }} <span class="unit">项</span></div>
          <div class="metric-sub">临床与微创</div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6" :md="5">
        <el-card shadow="hover" class="metric-card">
          <div class="metric-label">准入审核通过</div>
          <div class="metric-value color-success">{{ dashboardStats.准入审核通过数 || 0 }} <span class="unit">项</span></div>
          <div class="metric-sub">医务/科教联合通过</div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6" :md="5">
        <el-card shadow="hover" class="metric-card">
          <div class="metric-label">临床随访开展例数</div>
          <div class="metric-value color-primary">{{ dashboardStats.临床应用总例数 || 0 }} <span class="unit">例</span></div>
          <div class="metric-sub">不良反应: {{ dashboardStats.不良反应总例数 || 0 }} 例</div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6" :md="5">
        <el-card shadow="hover" class="metric-card">
          <div class="metric-label">期满转常规与获奖</div>
          <div class="metric-value color-warning">{{ dashboardStats.转常规与获奖数 || 0 }} <span class="unit">项</span></div>
          <div class="metric-sub">成效稳定达标</div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6" :md="5">
        <el-card shadow="hover" class="metric-card">
          <div class="metric-label">适宜技术下乡培训</div>
          <div class="metric-value color-cyan">{{ dashboardStats.培训基层医护总人次 || 0 }} <span class="unit">人次</span></div>
          <div class="metric-sub">共 {{ dashboardStats.适宜技术推广总场次 || 0 }} 场</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 主工作区 Tabs -->
    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab" @tab-change="handleTabChange">
        <!-- 标签页 1：新技术新项目准入申报 -->
        <el-tab-pane label="新技术新项目准入台账" name="projects">
          <!-- 筛选工具栏 -->
          <div class="table-toolbar">
            <el-form :inline="true" :model="projectFilter">
              <el-form-item label="申报年度">
                <el-date-picker
                  v-model="projectFilter.year"
                  type="year"
                  placeholder="全部年度"
                  value-format="YYYY"
                  style="width: 120px"
                  @change="loadProjects"
                />
              </el-form-item>
              <el-form-item label="类别">
                <el-select v-model="projectFilter.category" clearable placeholder="全部类别" style="width: 140px" @change="loadProjects">
                  <el-option label="临床新技术" value="临床新技术" />
                  <el-option label="临床新项目" value="临床新项目" />
                  <el-option label="微创新技术" value="微创新技术" />
                  <el-option label="护理新技术" value="护理新技术" />
                </el-select>
              </el-form-item>
              <el-form-item label="难度等级">
                <el-select v-model="projectFilter.difficulty" clearable placeholder="全部" style="width: 110px" @change="loadProjects">
                  <el-option label="一级" value="一级" />
                  <el-option label="二级" value="二级" />
                  <el-option label="三级" value="三级" />
                  <el-option label="四级" value="四级" />
                </el-select>
              </el-form-item>
              <el-form-item label="审批状态">
                <el-select v-model="projectFilter.status" clearable placeholder="全部状态" style="width: 140px" @change="loadProjects">
                  <el-option label="草稿" value="草稿" />
                  <el-option label="待科室审核" value="待科室审核" />
                  <el-option label="待医务处审核" value="待医务处审核" />
                  <el-option label="待科教处审核" value="待科教处审核" />
                  <el-option label="准入已批准" value="准入已批准" />
                  <el-option label="未批准" value="未批准" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-input
                  v-model="projectFilter.keyword"
                  clearable
                  placeholder="项目名称、负责人或科室"
                  style="width: 200px"
                  @keyup.enter="loadProjects"
                />
              </el-form-item>
              <el-form-item>
                <el-button type="primary" :icon="Search" @click="loadProjects">查询</el-button>
                <el-button :icon="Refresh" @click="resetProjectFilter">重置</el-button>
              </el-form-item>
            </el-form>
          </div>

          <!-- 新技术项目表格 -->
          <el-table v-loading="projectLoading" :data="projectList" stripe border class="data-table">
            <el-table-column prop="编号" label="编号" width="70" align="center" />
            <el-table-column prop="项目名称" label="新技术项目名称" min-width="220" show-overflow-tooltip>
              <template #default="{ row }">
                <div class="project-name-cell">
                  <span class="project-name-text">{{ row.项目名称 }}</span>
                  <el-tag size="small" :type="categoryTagType(row.申报类别)">{{ row.申报类别 }}</el-tag>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="技术难度等级" label="难度" width="80" align="center">
              <template #default="{ row }">
                <el-tag size="small" effect="plain">{{ row.技术难度等级 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="风险等级" label="风险" width="90" align="center">
              <template #default="{ row }">
                <el-tag size="small" :type="row.风险等级 === '高风险' ? 'danger' : row.风险等级 === '中风险' ? 'warning' : 'info'">
                  {{ row.风险等级 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="负责人 / 科室" width="160">
              <template #default="{ row }">
                <div><strong>{{ row.负责人姓名 || '未指定' }}</strong></div>
                <div class="muted-text">{{ row.科室名称 || '全院' }}</div>
              </template>
            </el-table-column>
            <el-table-column prop="申报年度" label="年度" width="80" align="center" />
            <el-table-column label="开展例数" width="110" align="center">
              <template #default="{ row }">
                <el-tag size="small" :type="row.临床开展总例数 >= 15 ? 'success' : 'info'">
                  {{ row.临床开展总例数 || 0 }} / {{ row.预期应用例数 || 20 }} 例
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="审批状态" label="审批状态" width="120" align="center">
              <template #default="{ row }">
                <el-tag size="small" :type="statusTagType(row.审批状态)">{{ row.审批状态 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="准入与审查" width="150" align="center">
              <template #default="{ row }">
                <div class="review-status-col">
                  <span>医务: {{ row.医务审核状态 || '待审' }}</span>
                  <span>科教: {{ row.科教审核状态 || '待审' }}</span>
                </div>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="220" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="viewDetail(row)">详情</el-button>
                <el-button link type="success" size="small" @click="openFollowupDialog(row)">例数登记</el-button>
                <el-button
                  v-if="canReview && row.审批状态 !== '准入已批准'"
                  link
                  type="warning"
                  size="small"
                  @click="openReviewDrawer(row)"
                >
                  准入审查
                </el-button>
                <el-button
                  v-if="row.临床开展总例数 >= 15 && row.审批状态 === '准入已批准'"
                  link
                  type="danger"
                  size="small"
                  @click="openTransitionDialog(row)"
                >
                  转常规/评奖
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <!-- 分页 -->
          <div class="pagination-wrapper">
            <el-pagination
              v-model:current-page="projectPagination.page"
              v-model:page-size="projectPagination.pageSize"
              :total="projectPagination.total"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="loadProjects"
              @current-change="loadProjects"
            />
          </div>
        </el-tab-pane>

        <!-- 标签页 2：临床应用例数登记与随访 (等级创建重点) -->
        <el-tab-pane label="临床应用例数与随访台账" name="followups">
          <div class="table-toolbar">
            <el-form :inline="true" :model="followupFilter">
              <el-form-item label="所属新技术">
                <el-select
                  v-model="followupFilter.projectId"
                  clearable
                  filterable
                  placeholder="全部新技术项目"
                  style="width: 260px"
                  @change="loadFollowups"
                >
                  <el-option
                    v-for="item in projectOptions"
                    :key="item.编号"
                    :label="`${item.项目名称} (${item.负责人姓名 || '负责人'})`"
                    :value="item.编号"
                  />
                </el-select>
              </el-form-item>
              <el-form-item label="不良反应">
                <el-select v-model="followupFilter.hasAdverse" clearable placeholder="全部" style="width: 120px" @change="loadFollowups">
                  <el-option label="有不良反应" :value="true" />
                  <el-option label="无不良反应" :value="false" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" :icon="Search" @click="loadFollowups">查询</el-button>
                <el-button type="success" :icon="Plus" @click="openFollowupDialog(null)">登记新例数</el-button>
              </el-form-item>
            </el-form>
          </div>

          <el-table v-loading="followupLoading" :data="followupList" stripe border class="data-table">
            <el-table-column prop="编号" label="例序号" width="80" align="center" />
            <el-table-column prop="新技术名称" label="对应新技术项目" min-width="190" show-overflow-tooltip />
            <el-table-column prop="患者姓名脱敏" label="患者(脱敏)" width="110" align="center" />
            <el-table-column prop="病历号" label="病历号" width="130" align="center" />
            <el-table-column prop="应用日期" label="应用实施日期" width="120" align="center">
              <template #default="{ row }">
                {{ formatDate(row.应用日期) }}
              </template>
            </el-table-column>
            <el-table-column prop="主要术者姓名" label="主要术者" width="110" align="center" />
            <el-table-column prop="临床疗效" label="临床疗效" width="100" align="center">
              <template #default="{ row }">
                <el-tag size="small" :type="row.临床疗效 === '显效' ? 'success' : row.临床疗效 === '有效' ? 'primary' : 'warning'">
                  {{ row.临床疗效 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="不良反应及级别" width="140" align="center">
              <template #default="{ row }">
                <el-tag
                  v-if="row.是否发生不良反应"
                  size="small"
                  :type="row.不良反应分级 === '重度(SAE)' ? 'danger' : 'warning'"
                >
                  {{ row.不良反应分级 || '有不良反应' }}
                </el-tag>
                <span v-else class="text-success">无不良反应</span>
              </template>
            </el-table-column>
            <el-table-column prop="不良反应描述及处置" label="不良反应描述与防范处置" min-width="180" show-overflow-tooltip />
            <el-table-column prop="随访日期" label="随访时间" width="120" align="center">
              <template #default="{ row }">
                {{ formatDate(row.随访日期) }}
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-wrapper">
            <el-pagination
              v-model:current-page="followupPagination.page"
              v-model:page-size="followupPagination.pageSize"
              :total="followupPagination.total"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="loadFollowups"
              @current-change="loadFollowups"
            />
          </div>
        </el-tab-pane>

        <!-- 标签页 3：适宜技术推广（医共体协同） -->
        <el-tab-pane label="医共体适宜技术下乡推广" name="appropriate">
          <div class="table-toolbar">
            <el-form :inline="true" :model="appropriateFilter">
              <el-form-item label="受援基层单位">
                <el-input v-model="appropriateFilter.partner" clearable placeholder="卫生院或社区中心" style="width: 200px" />
              </el-form-item>
              <el-form-item label="推广形式">
                <el-select v-model="appropriateFilter.format" clearable placeholder="全部形式" style="width: 140px">
                  <el-option label="现场培训" value="现场培训" />
                  <el-option label="手把手带教" value="手把手带教" />
                  <el-option label="手术演示" value="手术演示" />
                  <el-option label="线下讲座" value="线下讲座" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" :icon="Search" @click="loadAppropriates">查询</el-button>
                <el-button type="success" :icon="Plus" @click="openAppropriateTechDialog">新增适宜技术推广</el-button>
              </el-form-item>
            </el-form>
          </div>

          <el-table v-loading="appropriateLoading" :data="appropriateList" stripe border class="data-table">
            <el-table-column prop="编号" label="编号" width="70" align="center" />
            <el-table-column prop="技术名称" label="适宜技术名称" min-width="200" show-overflow-tooltip />
            <el-table-column prop="受援基层单位" label="受援基层卫生院/社区中心" min-width="180" show-overflow-tooltip>
              <template #default="{ row }">
                <strong>{{ row.受援基层单位 }}</strong>
              </template>
            </el-table-column>
            <el-table-column prop="推广形式" label="推广形式" width="110" align="center">
              <template #default="{ row }">
                <el-tag size="small" type="info">{{ row.推广形式 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="推广日期" label="推广实施日期" width="120" align="center">
              <template #default="{ row }">
                {{ formatDate(row.推广日期) }}
              </template>
            </el-table-column>
            <el-table-column prop="培训场次" label="培训场次" width="90" align="center">
              <template #default="{ row }">{{ row.培训场次 }} 场</template>
            </el-table-column>
            <el-table-column prop="受益医务人员数" label="受益基层医护" width="120" align="center">
              <template #default="{ row }">
                <el-tag size="small" type="success">{{ row.受益医务人员数 }} 人</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="受益患者人次" label="受益患者人次" width="120" align="center">
              <template #default="{ row }">{{ row.受益患者人次 }} 人次</template>
            </el-table-column>
            <el-table-column prop="推广成效说明" label="成效与反馈" min-width="160" show-overflow-tooltip />
          </el-table>

          <div class="pagination-wrapper">
            <el-pagination
              v-model:current-page="appropriatePagination.page"
              v-model:page-size="appropriatePagination.pageSize"
              :total="appropriatePagination.total"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="loadAppropriates"
              @current-change="loadAppropriates"
            />
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 弹窗 1：申报新技术新项目 -->
    <el-dialog v-model="declareDialogVisible" title="医疗新技术新项目准入申报" width="680px" destroy-on-close>
      <el-form ref="declareFormRef" :model="declareForm" :rules="declareRules" label-width="120px">
        <el-form-item label="项目名称" prop="项目名称">
          <el-input v-model="declareForm.项目名称" placeholder="请输入新技术新项目完整名称" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="申报类别" prop="申报类别">
              <el-select v-model="declareForm.申报类别" style="width: 100%">
                <el-option label="临床新技术" value="临床新技术" />
                <el-option label="临床新项目" value="临床新项目" />
                <el-option label="微创新技术" value="微创新技术" />
                <el-option label="护理新技术" value="护理新技术" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="技术难度等级" prop="技术难度等级">
              <el-select v-model="declareForm.技术难度等级" style="width: 100%">
                <el-option label="一级技术" value="一级" />
                <el-option label="二级技术" value="二级" />
                <el-option label="三级技术" value="三级" />
                <el-option label="四级技术（重大）" value="四级" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="风险等级" prop="风险等级">
              <el-select v-model="declareForm.风险等级" style="width: 100%">
                <el-option label="低风险" value="低风险" />
                <el-option label="中风险" value="中风险" />
                <el-option label="高风险" value="高风险" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="预期开展例数" prop="预期应用例数">
              <el-input-number v-model="declareForm.预期应用例数" :min="15" :max="500" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="计划开始日期" prop="计划开始日期">
              <el-date-picker v-model="declareForm.计划开始日期" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="计划完成日期" prop="计划完成日期">
              <el-date-picker v-model="declareForm.计划完成日期" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="临床适应症" prop="技术适应症">
          <el-input v-model="declareForm.技术适应症" type="textarea" :rows="3" placeholder="明确技术适用的患者群体、疾病阶段及临床指征" />
        </el-form-item>
        <el-form-item label="禁忌与并发防范" prop="禁忌症及防范预案">
          <el-input v-model="declareForm.禁忌症及防范预案" type="textarea" :rows="3" placeholder="详细列出禁忌证、不良反应应急预案及并发症应急防范措施" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="declareDialogVisible = false">取消</el-button>
        <el-button type="info" :loading="declareSubmitting" @click="handleSaveDeclare(false)">保存草稿</el-button>
        <el-button type="primary" :loading="declareSubmitting" @click="handleSaveDeclare(true)">提交准入审核</el-button>
      </template>
    </el-dialog>

    <!-- 弹窗 2：登记例数随访 -->
    <el-dialog v-model="followupDialogVisible" title="登记新技术临床应用例数与不良反应" width="600px" destroy-on-close>
      <el-form ref="followupFormRef" :model="followupForm" :rules="followupRules" label-width="120px">
        <el-form-item label="关联新技术" prop="新技术编号">
          <el-select v-model="followupForm.新技术编号" filterable placeholder="选择新技术项目" style="width: 100%">
            <el-option
              v-for="item in projectOptions"
              :key="item.编号"
              :label="`${item.项目名称} (${item.负责人姓名 || '负责人'})`"
              :value="item.编号"
            />
          </el-select>
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="患者姓名(脱敏)" prop="患者姓名脱敏">
              <el-input v-model="followupForm.患者姓名脱敏" placeholder="如：李*、王*华" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="病历号" prop="病历号">
              <el-input v-model="followupForm.病历号" placeholder="住院号/门诊号" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="实施日期" prop="应用日期">
              <el-date-picker v-model="followupForm.应用日期" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="主要术者姓名">
              <el-input v-model="followupForm.主要术者姓名" placeholder="实施主任/术者" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="临床疗效" prop="临床疗效">
          <el-radio-group v-model="followupForm.临床疗效">
            <el-radio label="显效">显效</el-radio>
            <el-radio label="有效">有效</el-radio>
            <el-radio label="改善">改善</el-radio>
            <el-radio label="无效">无效</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="有无不良反应">
          <el-switch v-model="followupForm.是否发生不良反应" active-text="发生不良反应" inactive-text="平稳无异常" />
        </el-form-item>
        <template v-if="followupForm.是否发生不良反应">
          <el-form-item label="不良反应分级">
            <el-select v-model="followupForm.不良反应分级" style="width: 100%">
              <el-option label="轻度不良反应" value="轻度" />
              <el-option label="中度不良反应" value="中度" />
              <el-option label="重度(SAE严重不良反应)" value="重度(SAE)" />
            </el-select>
          </el-form-item>
          <el-form-item label="不良反应及处置">
            <el-input v-model="followupForm.不良反应描述及处置" type="textarea" :rows="3" placeholder="描述发生的不良反应症状、处置方案及患者预后转归" />
          </el-form-item>
        </template>
        <el-form-item label="随访复查日期">
          <el-date-picker v-model="followupForm.随访日期" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="followupDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="followupSubmitting" @click="handleSaveFollowup">登记入库</el-button>
      </template>
    </el-dialog>

    <!-- 弹窗 3：适宜技术推广登记 -->
    <el-dialog v-model="appropriateDialogVisible" title="医共体适宜技术推广登记" width="620px" destroy-on-close>
      <el-form ref="appropriateFormRef" :model="appropriateForm" :rules="appropriateRules" label-width="130px">
        <el-form-item label="适宜技术名称" prop="技术名称">
          <el-input v-model="appropriateForm.技术名称" placeholder="输入向基层推广的技术名称" />
        </el-form-item>
        <el-form-item label="受援基层单位" prop="受援基层单位">
          <el-input v-model="appropriateForm.受援基层单位" placeholder="如：城关镇中心卫生院、李庄乡卫生院" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="推广形式" prop="推广形式">
              <el-select v-model="appropriateForm.推广形式" style="width: 100%">
                <el-option label="现场培训" value="现场培训" />
                <el-option label="手把手带教" value="手把手带教" />
                <el-option label="手术演示" value="手术演示" />
                <el-option label="线下讲座" value="线下讲座" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="推广日期" prop="推广日期">
              <el-date-picker v-model="appropriateForm.推广日期" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="培训场次">
              <el-input-number v-model="appropriateForm.培训场次" :min="1" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="受益基层医护">
              <el-input-number v-model="appropriateForm.受益医务人员数" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="受益患者人次">
              <el-input-number v-model="appropriateForm.受益患者人次" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="推广成效与总结">
          <el-input v-model="appropriateForm.推广成效说明" type="textarea" :rows="3" placeholder="总结基层技术开展掌握情况及医患满意度" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="appropriateDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="appropriateSubmitting" @click="handleSaveAppropriate">保存记录</el-button>
      </template>
    </el-dialog>

    <!-- 弹窗 4：期满转常规与院内新技术评奖申请 -->
    <el-dialog v-model="transitionDialogVisible" title="期满转常规技术 / 院内新技术评奖申请" width="600px" destroy-on-close>
      <el-alert
        type="success"
        :closable="false"
        title="该新技术累计开展已满 15 例以上，符合转常规技术与申报院内新技术奖准入标准。"
        style="margin-bottom: 14px"
      />
      <el-form ref="transitionFormRef" :model="transitionForm" label-width="120px">
        <el-form-item label="项目名称">
          <strong>{{ currentProject?.项目名称 }}</strong>
        </el-form-item>
        <el-form-item label="申请类型">
          <el-radio-group v-model="transitionForm.申请类型">
            <el-radio label="期满转常规">期满转常规医疗技术</el-radio>
            <el-radio label="院内新技术评奖">申报院内新技术新项目奖</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item v-if="transitionForm.申请类型 === '院内新技术评奖'" label="拟申报奖项">
          <el-select v-model="transitionForm.拟申报奖项" style="width: 100%">
            <el-option label="院级临床新技术一等奖" value="院级临床新技术一等奖" />
            <el-option label="院级临床新技术二等奖" value="院级临床新技术二等奖" />
            <el-option label="院级临床新技术三等奖" value="院级临床新技术三等奖" />
            <el-option label="微创与护理技术创新奖" value="微创与护理技术创新奖" />
          </el-select>
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="累计例数">
              <el-input-number v-model="transitionForm.累计开展例数" :min="15" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="临床显效率(%)">
              <el-input-number v-model="transitionForm.显效率" :min="0" :max="100" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="学术产出及论文">
          <el-input v-model="transitionForm.学术产出说明" type="textarea" :rows="3" placeholder="列出发表的相关论文、科普作品或申报的专利/实用新型" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="transitionDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="transitionSubmitting" @click="handleApplyTransition">提交申请</el-button>
      </template>
    </el-dialog>

    <!-- 抽屉：医务科教联合准入审查 -->
    <el-drawer v-model="reviewDrawerVisible" title="新技术新项目准入联合审查" size="520px">
      <div v-if="currentProject" class="drawer-content">
        <el-descriptions title="技术立项概要" :column="1" border style="margin-bottom: 20px">
          <el-descriptions-item label="项目名称">{{ currentProject.项目名称 }}</el-descriptions-item>
          <el-descriptions-item label="申报类别">{{ currentProject.申报类别 }}</el-descriptions-item>
          <el-descriptions-item label="难度 / 风险">{{ currentProject.技术难度等级 }} / {{ currentProject.风险等级 }}</el-descriptions-item>
          <el-descriptions-item label="负责人 / 科室">{{ currentProject.负责人姓名 }} ({{ currentProject.科室名称 }})</el-descriptions-item>
          <el-descriptions-item label="适应症">{{ currentProject.技术适应症 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="禁忌及防范">{{ currentProject.禁忌症及防范预案 || '-' }}</el-descriptions-item>
        </el-descriptions>

        <el-form ref="reviewFormRef" :model="reviewForm" label-width="110px">
          <el-form-item label="审核部门" required>
            <el-radio-group v-model="reviewForm.审核部门">
              <el-radio label="医务处">医务处（临床资质准入）</el-radio>
              <el-radio label="科教处">科教处（科研管理准入）</el-radio>
            </el-radio-group>
          </el-form-item>
          <el-form-item label="伦理审查编号">
            <el-input v-model="reviewForm.伦理审查编号" placeholder="关联伦理批件编号（如 EC-2026-001）" />
          </el-form-item>
          <el-form-item label="审查结论" required>
            <el-radio-group v-model="reviewForm.通过">
              <el-radio :label="true">同意准入批准</el-radio>
              <el-radio :label="false">驳回不予准入</el-radio>
            </el-radio-group>
          </el-form-item>
          <el-form-item label="审核意见">
            <el-input v-model="reviewForm.意见" type="textarea" :rows="3" placeholder="填写专家论证意见与质控要求" />
          </el-form-item>
        </el-form>
      </div>
      <template #footer>
        <el-button @click="reviewDrawerVisible = false">取消</el-button>
        <el-button type="primary" :loading="reviewSubmitting" @click="handleSubmitReview">确认审批</el-button>
      </template>
    </el-drawer>

    <!-- 弹窗 5：查看详情 -->
    <el-dialog v-model="detailDialogVisible" title="新技术新项目详情全景" width="700px">
      <div v-if="projectDetail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="项目名称" :span="2"><strong>{{ projectDetail.项目?.项目名称 }}</strong></el-descriptions-item>
          <el-descriptions-item label="申报类别">{{ projectDetail.项目?.申报类别 }}</el-descriptions-item>
          <el-descriptions-item label="难度等级">{{ projectDetail.项目?.技术难度等级 }}</el-descriptions-item>
          <el-descriptions-item label="风险等级">{{ projectDetail.项目?.风险等级 }}</el-descriptions-item>
          <el-descriptions-item label="审批状态">
            <el-tag size="small" :type="statusTagType(projectDetail.项目?.审批状态)">{{ projectDetail.项目?.审批状态 }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="负责人">{{ projectDetail.项目?.负责人姓名 }}</el-descriptions-item>
          <el-descriptions-item label="所属科室">{{ projectDetail.项目?.科室名称 }}</el-descriptions-item>
          <el-descriptions-item label="计划周期">
            {{ formatDate(projectDetail.项目?.计划开始日期) }} 至 {{ formatDate(projectDetail.项目?.计划完成日期) }}
          </el-descriptions-item>
          <el-descriptions-item label="临床开展例数">{{ projectDetail.项目?.临床开展总例数 }} 例</el-descriptions-item>
          <el-descriptions-item label="临床适应症" :span="2">{{ projectDetail.项目?.技术适应症 }}</el-descriptions-item>
          <el-descriptions-item label="防范预案" :span="2">{{ projectDetail.项目?.禁忌症及防范预案 }}</el-descriptions-item>
          <el-descriptions-item label="医务审核意见" :span="2">
            {{ projectDetail.项目?.医务审核意见 || '尚未完成医务处审核' }}
          </el-descriptions-item>
          <el-descriptions-item label="科教审核意见" :span="2">
            {{ projectDetail.项目?.科教审核意见 || '尚未完成科教处审核' }}
          </el-descriptions-item>
        </el-descriptions>

        <h4 style="margin: 20px 0 10px; font-weight: bold;">已登记随访例数（最近 {{ projectDetail.随访列表?.length || 0 }} 例）</h4>
        <el-table :data="projectDetail.随访列表 || []" border size="small">
          <el-table-column prop="患者姓名脱敏" label="患者" width="90" align="center" />
          <el-table-column prop="病历号" label="病历号" width="120" align="center" />
          <el-table-column prop="应用日期" label="日期" width="100" align="center">
            <template #default="{ row }">{{ formatDate(row.应用日期) }}</template>
          </el-table-column>
          <el-table-column prop="临床疗效" label="疗效" width="80" align="center" />
          <el-table-column prop="不良反应分级" label="不良反应" width="100" align="center" />
        </el-table>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import {
  TrendCharts, Plus, Promotion, Refresh, Search
} from '@element-plus/icons-vue'
import {
  listNewTechProjects, getNewTechDetail, saveNewTechProject, reviewNewTechProject,
  listNewTechFollowups, saveNewTechFollowup, applyNewTechRegularOrAward,
  listAppropriateTechs, saveAppropriateTech, getMedicalAdvancementDashboard
} from '@/api/medical-advancement'

const activeTab = ref('projects')
const dashboardStats = ref<any>({})
const canReview = ref(true)

// 项目台账
const projectLoading = ref(false)
const projectList = ref<any[]>([])
const projectOptions = ref<any[]>([])
const projectPagination = reactive({ page: 1, pageSize: 10, total: 0 })
const projectFilter = reactive({ year: '', category: '', difficulty: '', status: '', keyword: '' })

// 随访例数台账
const followupLoading = ref(false)
const followupList = ref<any[]>([])
const followupPagination = reactive({ page: 1, pageSize: 10, total: 0 })
const followupFilter = reactive({ projectId: undefined, hasAdverse: undefined })

// 适宜技术推广
const appropriateLoading = ref(false)
const appropriateList = ref<any[]>([])
const appropriatePagination = reactive({ page: 1, pageSize: 10, total: 0 })
const appropriateFilter = reactive({ partner: '', format: '' })

// 弹窗与表单
const declareDialogVisible = ref(false)
const declareSubmitting = ref(false)
const declareForm = reactive<any>({
  项目名称: '', 申报类别: '临床新技术', 技术难度等级: '三级', 风险等级: '中风险',
  预期应用例数: 20, 计划开始日期: '', 计划完成日期: '', 技术适应症: '', 禁忌症及防范预案: ''
})
const declareRules = {
  项目名称: [{ required: true, message: '请输入新技术项目名称', trigger: 'blur' }],
  申报类别: [{ required: true, message: '请选择申报类别', trigger: 'change' }]
}

const followupDialogVisible = ref(false)
const followupSubmitting = ref(false)
const followupForm = reactive<any>({
  新技术编号: undefined, 患者姓名脱敏: '', 病历号: '', 应用日期: '',
  主要术者姓名: '', 临床疗效: '有效', 是否发生不良反应: false, 不良反应分级: '轻度',
  不良反应描述及处置: '', 随访日期: ''
})
const followupRules = {
  新技术编号: [{ required: true, message: '请选择关联新技术', trigger: 'change' }],
  患者姓名脱敏: [{ required: true, message: '请填写脱敏患者姓名', trigger: 'blur' }],
  病历号: [{ required: true, message: '请填写病历号', trigger: 'blur' }],
  应用日期: [{ required: true, message: '请选择实施日期', trigger: 'change' }]
}

const appropriateDialogVisible = ref(false)
const appropriateSubmitting = ref(false)
const appropriateForm = reactive<any>({
  技术名称: '', 受援基层单位: '', 推广形式: '现场培训', 推广日期: '',
  培训场次: 1, 受益医务人员数: 10, 受益患者人次: 0, 推广成效说明: ''
})
const appropriateRules = {
  技术名称: [{ required: true, message: '请输入适宜技术名称', trigger: 'blur' }],
  受援基层单位: [{ required: true, message: '请输入受援基层单位', trigger: 'blur' }]
}

const transitionDialogVisible = ref(false)
const transitionSubmitting = ref(false)
const currentProject = ref<any>(null)
const transitionForm = reactive<any>({
  申请类型: '期满转常规', 拟申报奖项: '院级临床新技术一等奖', 累计开展例数: 20, 显效率: 90, 学术产出说明: ''
})

const reviewDrawerVisible = ref(false)
const reviewSubmitting = ref(false)
const reviewForm = reactive<any>({
  审核部门: '医务处', 通过: true, 意见: '', 伦理审查编号: ''
})

const detailDialogVisible = ref(false)
const projectDetail = ref<any>(null)

// 标签类型辅助
function categoryTagType(cat: string) {
  if (cat === '临床新技术') return 'primary'
  if (cat === '微创新技术') return 'success'
  if (cat === '护理新技术') return 'warning'
  return 'info'
}

function statusTagType(status: string) {
  if (status === '准入已批准') return 'success'
  if (status === '未批准') return 'danger'
  if (status === '待科教处审核' || status === '待医务处审核') return 'warning'
  return 'info'
}

function formatDate(val: any) {
  if (!val) return '-'
  return String(val).substring(0, 10)
}

// 加载逻辑
async function loadDashboard() {
  try {
    const res: any = await getMedicalAdvancementDashboard()
    dashboardStats.value = res?.data || res || {}
  } catch {}
}

async function loadProjects() {
  projectLoading.value = true
  try {
    const res: any = await listNewTechProjects({
      申报年度: projectFilter.year ? parseInt(projectFilter.year) : undefined,
      申报类别: projectFilter.category || undefined,
      难度等级: projectFilter.difficulty || undefined,
      审批状态: projectFilter.status || undefined,
      关键词: projectFilter.keyword || undefined,
      pageIndex: projectPagination.page,
      pageSize: projectPagination.pageSize
    })
    const data = res?.data || res
    projectList.value = data?.list || []
    projectPagination.total = data?.total || 0

    // 更新选择器选项
    if (!projectOptions.value.length) {
      projectOptions.value = projectList.value
    }
  } catch (err: any) {
    ElMessage.error(err.message || '获取新技术列表失败')
  } finally {
    projectLoading.value = false
  }
}

async function loadFollowups() {
  followupLoading.value = true
  try {
    const res: any = await listNewTechFollowups({
      新技术编号: followupFilter.projectId || undefined,
      是否发生不良反应: followupFilter.hasAdverse,
      pageIndex: followupPagination.page,
      pageSize: followupPagination.pageSize
    })
    const data = res?.data || res
    followupList.value = data?.list || []
    followupPagination.total = data?.total || 0
  } catch (err: any) {
    ElMessage.error(err.message || '获取随访列表失败')
  } finally {
    followupLoading.value = false
  }
}

async function loadAppropriates() {
  appropriateLoading.value = true
  try {
    const res: any = await listAppropriateTechs({
      受援基层单位: appropriateFilter.partner || undefined,
      推广形式: appropriateFilter.format || undefined,
      pageIndex: appropriatePagination.page,
      pageSize: appropriatePagination.pageSize
    })
    const data = res?.data || res
    appropriateList.value = data?.list || []
    appropriatePagination.total = data?.total || 0
  } catch (err: any) {
    ElMessage.error(err.message || '获取适宜技术列表失败')
  } finally {
    appropriateLoading.value = false
  }
}

function handleTabChange(tab: string) {
  if (tab === 'projects') loadProjects()
  else if (tab === 'followups') loadFollowups()
  else if (tab === 'appropriate') loadAppropriates()
}

function resetProjectFilter() {
  projectFilter.year = ''
  projectFilter.category = ''
  projectFilter.difficulty = ''
  projectFilter.status = ''
  projectFilter.keyword = ''
  projectPagination.page = 1
  loadProjects()
}

function refreshAll() {
  loadDashboard()
  if (activeTab.value === 'projects') loadProjects()
  else if (activeTab.value === 'followups') loadFollowups()
  else if (activeTab.value === 'appropriate') loadAppropriates()
}

// 申报弹窗
function openDeclareDialog() {
  Object.assign(declareForm, {
    编号: undefined,
    项目名称: '', 申报类别: '临床新技术', 技术难度等级: '三级', 风险等级: '中风险',
    预期应用例数: 20, 计划开始日期: '', 计划完成日期: '', 技术适应症: '', 禁忌症及防范预案: ''
  })
  declareDialogVisible.value = true
}

async function handleSaveDeclare(submit: boolean) {
  if (!declareForm.项目名称) {
    ElMessage.warning('请填写项目名称')
    return
  }
  declareSubmitting.value = true
  try {
    await saveNewTechProject({ ...declareForm, 提交审批: submit })
    ElMessage.success(submit ? '新技术申报已提交，等待医务与科教审核' : '新技术申报草稿已保存')
    declareDialogVisible.value = false
    loadProjects()
    loadDashboard()
  } catch (err: any) {
    ElMessage.error(err.message || '保存失败')
  } finally {
    declareSubmitting.value = false
  }
}

// 随访例数登记
function openFollowupDialog(row: any) {
  Object.assign(followupForm, {
    新技术编号: row?.编号 || undefined,
    患者姓名脱敏: '',
    病历号: '',
    应用日期: new Date().toISOString().substring(0, 10),
    主要术者姓名: row?.负责人姓名 || '',
    临床疗效: '有效',
    是否发生不良反应: false,
    不良反应分级: '无',
    不良反应描述及处置: '',
    随访日期: ''
  })
  followupDialogVisible.value = true
}

async function handleSaveFollowup() {
  if (!followupForm.新技术编号 || !followupForm.患者姓名脱敏 || !followupForm.病历号) {
    ElMessage.warning('请完整填写关键信息')
    return
  }
  followupSubmitting.value = true
  try {
    await saveNewTechFollowup(followupForm)
    ElMessage.success('临床应用例数随访登记成功')
    followupDialogVisible.value = false
    loadFollowups()
    loadProjects()
    loadDashboard()
  } catch (err: any) {
    ElMessage.error(err.message || '登记失败')
  } finally {
    followupSubmitting.value = false
  }
}

// 适宜技术登记
function openAppropriateTechDialog() {
  Object.assign(appropriateForm, {
    技术名称: '', 受援基层单位: '', 推广形式: '现场培训',
    推广日期: new Date().toISOString().substring(0, 10),
    培训场次: 1, 受益医务人员数: 15, 受益患者人次: 0, 推广成效说明: ''
  })
  appropriateDialogVisible.value = true
}

async function handleSaveAppropriate() {
  if (!appropriateForm.技术名称 || !appropriateForm.受援基层单位) {
    ElMessage.warning('请填写技术名称与受援基层单位')
    return
  }
  appropriateSubmitting.value = true
  try {
    await saveAppropriateTech(appropriateForm)
    ElMessage.success('适宜技术推广记录保存成功')
    appropriateDialogVisible.value = false
    loadAppropriates()
    loadDashboard()
  } catch (err: any) {
    ElMessage.error(err.message || '保存失败')
  } finally {
    appropriateSubmitting.value = false
  }
}

// 转常规与评奖
function openTransitionDialog(row: any) {
  currentProject.value = row
  Object.assign(transitionForm, {
    新技术编号: row.编号,
    申请类型: '期满转常规',
    拟申报奖项: '院级临床新技术一等奖',
    累计开展例数: row.临床开展总例数 || 20,
    显效率: 90,
    学术产出说明: ''
  })
  transitionDialogVisible.value = true
}

async function handleApplyTransition() {
  transitionSubmitting.value = true
  try {
    await applyNewTechRegularOrAward({ ...transitionForm, 新技术编号: currentProject.value.编号 })
    ElMessage.success('转常规 / 评奖申请已提交')
    transitionDialogVisible.value = false
    loadProjects()
  } catch (err: any) {
    ElMessage.error(err.message || '申请提交失败')
  } finally {
    transitionSubmitting.value = false
  }
}

// 准入审查抽屉
function openReviewDrawer(row: any) {
  currentProject.value = row
  reviewForm.审核部门 = '医务处'
  reviewForm.通过 = true
  reviewForm.意见 = '经审查，该新技术适应症明确，人员具备资质，同意准入开展临床应用。'
  reviewForm.伦理审查编号 = row.伦理审查编号 || ''
  reviewDrawerVisible.value = true
}

async function handleSubmitReview() {
  reviewSubmitting.value = true
  try {
    await reviewNewTechProject({
      编号: currentProject.value.编号,
      审核部门: reviewForm.审核部门,
      通过: reviewForm.通过,
      意见: reviewForm.意见,
      伦理审查编号: reviewForm.伦理审查编号
    })
    ElMessage.success('准入审查意见已记录')
    reviewDrawerVisible.value = false
    loadProjects()
    loadDashboard()
  } catch (err: any) {
    ElMessage.error(err.message || '审查提交失败')
  } finally {
    reviewSubmitting.value = false
  }
}

// 详情弹窗
async function viewDetail(row: any) {
  try {
    const res: any = await getNewTechDetail(row.编号)
    projectDetail.value = res?.data || res
    detailDialogVisible.value = true
  } catch (err: any) {
    ElMessage.error(err.message || '获取详情失败')
  }
}

onMounted(() => {
  loadDashboard()
  loadProjects()
})
</script>

<style scoped>
.new-tech-container {
  padding: 16px 20px;
  background-color: #f5f7fa;
  min-height: calc(100vh - 84px);
}

.header-card {
  background: linear-gradient(135deg, #1890ff 0%, #096dd9 100%);
  color: #fff;
  padding: 20px 24px;
  border-radius: 8px;
  margin-bottom: 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 4px 12px rgba(24, 144, 255, 0.15);
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  margin: 0 0 6px;
  display: flex;
  align-items: center;
  gap: 8px;
}

.page-subtitle {
  font-size: 13px;
  opacity: 0.9;
  margin: 0;
  max-width: 800px;
  line-height: 1.5;
}

.header-actions {
  display: flex;
  gap: 10px;
}

.metrics-row {
  margin-bottom: 16px;
}

.metric-card {
  border-radius: 8px;
  text-align: center;
  border: 1px solid #ebeef5;
}

.metric-label {
  font-size: 13px;
  color: #909399;
  margin-bottom: 6px;
}

.metric-value {
  font-size: 24px;
  font-weight: 700;
  color: #303133;
}

.metric-value .unit {
  font-size: 12px;
  font-weight: normal;
  color: #909399;
}

.metric-sub {
  font-size: 12px;
  color: #909399;
  margin-top: 4px;
}

.color-primary { color: #1890ff; }
.color-success { color: #52c41a; }
.color-warning { color: #fa8c16; }
.color-cyan { color: #13c2c2; }

.main-card {
  border-radius: 8px;
}

.table-toolbar {
  margin-bottom: 14px;
  display: flex;
  justify-content: space-between;
}

.project-name-cell {
  display: flex;
  align-items: center;
  gap: 8px;
}

.project-name-text {
  font-weight: 600;
  color: #1f2937;
}

.muted-text {
  font-size: 12px;
  color: #8c8c8c;
}

.review-status-col {
  display: flex;
  flex-direction: column;
  font-size: 12px;
  color: #595959;
}

.text-success {
  color: #52c41a;
  font-size: 12px;
}

.pagination-wrapper {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>
