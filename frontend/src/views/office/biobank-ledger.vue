<template>
  <div class="page-container">
    <!-- 顶部核心指标与临床生态中枢 -->
    <el-card shadow="never" class="overview-card">
      <div class="overview-header">
        <div class="overview-title-block">
          <h2 class="overview-title">
            <el-icon class="title-icon"><Box /></el-icon>
            临床生物样本库 (Biobank) 资产与科研借还核销中枢
          </h2>
          <p class="overview-subtitle">
            对标清华长庚医院实践：临床组织标本全生命周期管理、伦理审查前置强校验与硬阻断、课题双向资产核算与实验消耗分摊
          </p>
        </div>
        <div class="overview-actions">
          <el-button
            type="primary"
            :icon="Plus"
            @click="openAddSpecimenDialog"
          >
            样本入库登记
          </el-button>
          <el-button
            type="success"
            :icon="DocumentChecked"
            @click="openApplyLoanDialog(null)"
          >
            申请标本领用
          </el-button>
          <el-button
            :icon="Refresh"
            @click="refreshAll"
          >
            刷新台账
          </el-button>
        </div>
      </div>

      <el-row :gutter="16" class="metric-row">
        <el-col :xs="12" :sm="6">
          <div class="metric-card metric-blue">
            <div class="metric-label">在库标本总资产</div>
            <div class="metric-value">{{ statsData.总样本数 || 0 }} <span class="unit">种</span></div>
            <div class="metric-footer">临床专病队列组织/血液</div>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="metric-card metric-green">
            <div class="metric-label">在库可借用标本</div>
            <div class="metric-value">{{ statsData.在库样本数 || 0 }} <span class="unit">种</span></div>
            <div class="metric-footer">深低温与标准石蜡质控合格</div>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="metric-card metric-amber">
            <div class="metric-label">实验领用流转中</div>
            <div class="metric-value">{{ statsData.正在实验中借出 || 0 }} <span class="unit">单</span></div>
            <div class="metric-footer">已出库 / 正在开展多组学检测</div>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="metric-card metric-purple">
            <div class="metric-label">课题分摊科研成本</div>
            <div class="metric-value">¥{{ Number(statsData.课题已分摊总成本 || 0).toFixed(2) }}</div>
            <div class="metric-footer">实验消耗与制备保管核算</div>
          </div>
        </el-col>
      </el-row>
    </el-card>

    <!-- 主体多标签卡片 -->
    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab">
        <!-- 标签页 1：生物样本资产库存管理 -->
        <el-tab-pane label="生物样本资产库存" name="assets">
          <div class="toolbar">
            <el-form :inline="true" :model="assetFilter" class="filter-form">
              <el-form-item label="关键词检索">
                <el-input
                  v-model="assetFilter.keyword"
                  placeholder="搜索样本编码/疾病/位置/伦理号"
                  clearable
                  style="width: 240px;"
                />
              </el-form-item>
              <el-form-item label="样本类型">
                <el-select v-model="assetFilter.sampleType" placeholder="全部类型" clearable style="width: 140px;">
                  <el-option label="全部类型" value="" />
                  <el-option label="冰冻组织" value="冰冻组织" />
                  <el-option label="石蜡切片" value="石蜡切片" />
                  <el-option label="血清" value="血清" />
                  <el-option label="血浆" value="血浆" />
                  <el-option label="脑脊液" value="脑脊液" />
                  <el-option label="DNA/RNA" value="DNA/RNA" />
                </el-select>
              </el-form-item>
              <el-form-item label="在库状态">
                <el-select v-model="assetFilter.status" placeholder="全部状态" clearable style="width: 130px;">
                  <el-option label="全部状态" value="" />
                  <el-option label="在库" value="在库" />
                  <el-option label="部分领用" value="部分领用" />
                  <el-option label="已借空" value="已借空" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" :icon="Search" @click="fetchSpecimens">查询</el-button>
                <el-button :icon="Refresh" @click="resetAssetFilter">重置</el-button>
              </el-form-item>
            </el-form>
          </div>

          <el-table
            v-loading="loadingSpecimens"
            :data="specimenList"
            stripe
            border
            style="width: 100%; margin-top: 12px;"
          >
            <el-table-column prop="样本编码" label="标本编码 / 条形码" min-width="160">
              <template #default="{ row }">
                <div class="code-text">{{ row.样本编码 }}</div>
                <div class="code-sub">知情同意: {{ row.知情同意书编号 || '已签署' }}</div>
              </template>
            </el-table-column>

            <el-table-column label="标本类型" width="110" align="center">
              <template #default="{ row }">
                <el-tag :type="getSpecimenTypeTag(row.样本类型)" size="small">
                  {{ row.样本类型 }}
                </el-tag>
              </template>
            </el-table-column>

            <el-table-column label="临床诊断 / 解剖部位" min-width="200">
              <template #default="{ row }">
                <div class="disease-title">{{ row.疾病诊断 }}</div>
                <div class="anatomy-text">{{ row.解剖部位 || '临床多中心送检' }}</div>
              </template>
            </el-table-column>

            <el-table-column label="温区与存放坐标" min-width="210">
              <template #default="{ row }">
                <el-tag
                  :type="row.储存温度 === '常温' ? 'success' : 'primary'"
                  size="small"
                  effect="plain"
                  style="margin-bottom: 4px;"
                >
                  {{ row.储存温度 }}
                </el-tag>
                <div class="location-text">
                  <el-icon><Location /></el-icon>
                  {{ row.储存位置 }}
                </div>
              </template>
            </el-table-column>

            <el-table-column label="在库库存 / 初始量" width="150" align="center">
              <template #default="{ row }">
                <div class="stock-num">
                  <span class="stock-current">{{ row.在库余量 }}</span> / {{ row.初始数量 }} {{ row.规格单位 }}
                </div>
                <el-progress
                  :percentage="Math.round((row.在库余量 / (row.初始数量 || 1)) * 100)"
                  :status="row.在库余量 === 0 ? 'exception' : (row.在库余量 < row.初始数量 ? 'warning' : 'success')"
                  :stroke-width="5"
                  style="margin-top: 4px;"
                />
              </template>
            </el-table-column>

            <el-table-column label="资产成本单价" width="120" align="right">
              <template #default="{ row }">
                <div class="price-text">¥{{ Number(row.单价成本).toFixed(2) }}</div>
                <div class="unit-text">每 {{ row.规格单位 }}</div>
              </template>
            </el-table-column>

            <el-table-column label="伦理审查与质控" min-width="170">
              <template #default="{ row }">
                <div class="irb-text">
                  <el-icon><Document /></el-icon>
                  {{ row.伦理批文号 || 'ETH-2026-RENEW-005' }}
                </div>
                <el-tag size="small" type="info" style="margin-top: 4px;">
                  {{ row.质控等级 || '一级合格' }}
                </el-tag>
              </template>
            </el-table-column>

            <el-table-column label="状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="getStatusTag(row.状态)" size="small">
                  {{ row.状态 }}
                </el-tag>
              </template>
            </el-table-column>

            <el-table-column label="操作" width="120" fixed="right" align="center">
              <template #default="{ row }">
                <el-button
                  v-if="row.在库余量 > 0"
                  type="primary"
                  link
                  size="small"
                  @click="openApplyLoanDialog(row)"
                >
                  申请领用
                </el-button>
                <span v-else style="color: #94a3b8; font-size: 13px;">已借空</span>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签页 2：出库领用与消耗核销台账 -->
        <el-tab-pane label="标本出库借还与消耗核销台账" name="loans">
          <div class="toolbar">
            <el-form :inline="true" :model="loanFilter" class="filter-form">
              <el-form-item label="关键词">
                <el-input
                  v-model="loanFilter.keyword"
                  placeholder="搜索单号/编码/课题/申请人"
                  clearable
                  style="width: 220px;"
                />
              </el-form-item>
              <el-form-item label="流转状态">
                <el-select v-model="loanFilter.status" placeholder="全部状态" clearable style="width: 140px;">
                  <el-option label="全部状态" value="" />
                  <el-option label="申请中" value="申请中" />
                  <el-option label="已出库" value="已出库" />
                  <el-option label="已消耗核销" value="已消耗核销" />
                  <el-option label="已全部归还" value="已全部归还" />
                  <el-option label="已部分归还" value="已部分归还" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" :icon="Search" @click="fetchLoanLedger">查询</el-button>
                <el-button :icon="Refresh" @click="resetLoanFilter">重置</el-button>
              </el-form-item>
            </el-form>
          </div>

          <el-table
            v-loading="loadingLoans"
            :data="loanList"
            stripe
            border
            style="width: 100%; margin-top: 12px;"
          >
            <el-table-column prop="编号" label="单号" width="70" align="center" />
            <el-table-column label="标本资产信息" min-width="180">
              <template #default="{ row }">
                <div class="code-text">{{ row.样本编码 }}</div>
                <div class="code-sub">{{ row.疾病诊断 }} ({{ row.样本类型 }})</div>
              </template>
            </el-table-column>

            <el-table-column label="关联科研课题" min-width="220" show-overflow-tooltip>
              <template #default="{ row }">
                <div class="project-title">{{ row.项目名称 || '院级自选科研课题' }}</div>
                <div class="project-sub">课题编号: {{ row.项目编号 ? `Y-PROJ-${row.项目编号}` : '-' }}</div>
              </template>
            </el-table-column>

            <el-table-column label="申请人 / 实验用途" min-width="200">
              <template #default="{ row }">
                <div class="applicant-name">
                  <el-icon><User /></el-icon>
                  {{ row.申请人姓名 }}
                </div>
                <div class="purpose-text">{{ row.申请用途 }}</div>
              </template>
            </el-table-column>

            <el-table-column label="数量流转明细" width="160">
              <template #default="{ row }">
                <div class="qty-line">申请: <strong>{{ row.申请数量 }}</strong> {{ row.规格单位 }}</div>
                <div class="qty-line">已出库: <strong>{{ row.出库数量 }}</strong> {{ row.规格单位 }}</div>
                <div v-if="row.借还状态 !== '申请中' && row.借还状态 !== '已出库'" class="qty-line">
                  归还: {{ row.归还数量 }} | 消耗: {{ row.消耗数量 }}
                </div>
              </template>
            </el-table-column>

            <el-table-column label="伦理审查结果" width="150" align="center">
              <template #default="{ row }">
                <el-tag
                  :type="getEthicsResultTag(row.伦理核验结果)"
                  size="small"
                  effect="dark"
                >
                  {{ row.伦理核验结果 }}
                </el-tag>
                <div v-if="row.伦理批文号" class="irb-sub">{{ row.伦理批文号 }}</div>
              </template>
            </el-table-column>

            <el-table-column label="流转状态" width="110" align="center">
              <template #default="{ row }">
                <el-tag :type="getLoanStatusTag(row.借还状态)" size="small">
                  {{ row.借还状态 }}
                </el-tag>
              </template>
            </el-table-column>

            <el-table-column label="课题分摊成本" width="120" align="right">
              <template #default="{ row }">
                <div class="price-text">¥{{ Number(row.分摊总金额 || 0).toFixed(2) }}</div>
                <div class="unit-text">{{ row.消耗数量 ? `已核算 ${row.消耗数量}${row.规格单位}` : '出库预估' }}</div>
              </template>
            </el-table-column>

            <el-table-column label="操作" width="140" fixed="right" align="center">
              <template #default="{ row }">
                <!-- 申请中 -> 核准出库 -->
                <el-button
                  v-if="row.借还状态 === '申请中'"
                  type="success"
                  size="small"
                  @click="openApproveDialog(row)"
                >
                  核准出库
                </el-button>

                <!-- 已出库 -> 归还与消耗核销 -->
                <el-button
                  v-else-if="row.借还状态 === '已出库'"
                  type="primary"
                  size="small"
                  @click="openReconcileDialog(row)"
                >
                  归还与核销
                </el-button>

                <div v-else class="done-text">
                  <el-icon color="#10b981"><Check /></el-icon>
                  已结清归档
                </div>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签页 3：专病队列与温区看板 -->
        <el-tab-pane label="专病队列与温区资产看板" name="analytics">
          <div class="analytics-container">
            <el-row :gutter="20">
              <el-col :xs="24" :md="12">
                <el-card shadow="never" class="sub-card">
                  <template #header>
                    <strong>标本类型存量分布</strong>
                  </template>
                  <el-table :data="statsData.类型统计" stripe size="small">
                    <el-table-column prop="样本类型" label="样本分类" />
                    <el-table-column prop="样本总数" label="标本批次数" align="center" />
                    <el-table-column prop="在库总余量" label="当前在库总余量" align="right" />
                  </el-table>
                </el-card>
              </el-col>

              <el-col :xs="24" :md="12">
                <el-card shadow="never" class="sub-card">
                  <template #header>
                    <strong>储存温区与冷链设备分布</strong>
                  </template>
                  <el-table :data="statsData.温区统计" stripe size="small">
                    <el-table-column prop="储存温度" label="储存温度" />
                    <el-table-column prop="标本数" label="在架标本数" align="center" />
                  </el-table>
                </el-card>
              </el-col>
            </el-row>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 弹窗 1：申请生物样本领用 -->
    <el-dialog
      v-model="applyDialogVisible"
      title="申请生物标本出库领用 (前置伦理合规审查)"
      width="540px"
      destroy-on-close
    >
      <el-alert
        title="重要合规提醒：领用临床人体生物标本必须挂靠有效受批的纵向科研课题，系统将自动校验伦理批文有效期；超期批文将依法执行硬性阻断！"
        type="warning"
        :closable="false"
        show-icon
        style="margin-bottom: 16px;"
      />

      <el-form :model="applyForm" label-width="110px">
        <el-form-item label="领用标本" required>
          <el-select
            v-model="applyForm.样本编号"
            placeholder="请选择待领用样本"
            style="width: 100%;"
            filterable
          >
            <el-option
              v-for="s in specimenList"
              :key="s.编号"
              :label="`${s.样本编码} - ${s.疾病诊断} (在库: ${s.在库余量}${s.规格单位})`"
              :value="s.编号"
              :disabled="s.在库余量 <= 0"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="归属立项课题" required>
          <el-select
            v-model="applyForm.项目编号"
            placeholder="请选择科研课题以核验伦理批文"
            style="width: 100%;"
          >
            <el-option :label="'课题5: 基于临床生物样本库的原发性肝细胞癌空间多组学标志物筛选及机制研究 (伦理有效)'" :value="5" />
            <el-option :label="'课题6: 临床肿瘤新药早期试验 (伦理已过期-用于合规阻断测试)'" :value="6" />
          </el-select>
        </el-form-item>

        <el-form-item label="申请领用数量" required>
          <el-input-number v-model="applyForm.申请数量" :min="1" :max="50" />
        </el-form-item>

        <el-form-item label="研究实验用途" required>
          <el-input
            v-model="applyForm.申请用途"
            type="textarea"
            :rows="3"
            placeholder="请明确阐述领用该批标本开展的实验方案（如：单细胞测序、石蜡切片HE染色或空间转录组分析）"
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="applyDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="applySubmitting" @click="handleApplySubmit">
          提交合规审查与申请
        </el-button>
      </template>
    </el-dialog>

    <!-- 弹窗 2：样本库核准出库 -->
    <el-dialog
      v-model="approveDialogVisible"
      title="样本库核准标本出库"
      width="460px"
    >
      <div v-if="currentLoan" style="margin-bottom: 16px; color: #334155;">
        <p>正在核准单号：<strong>#{{ currentLoan.编号 }}</strong></p>
        <p>标本编码：<strong>{{ currentLoan.样本编码 }}</strong></p>
        <p>申请人：<strong>{{ currentLoan.申请人姓名 }}</strong></p>
        <p>申请数量：<strong>{{ currentLoan.申请数量 }} {{ currentLoan.规格单位 }}</strong></p>
        <p>伦理合规状态：
          <el-tag size="small" type="success">{{ currentLoan.伦理核验结果 }}</el-tag>
        </p>
      </div>

      <el-form :model="approveForm" label-width="110px">
        <el-form-item label="实际出库数量" required>
          <el-input-number v-model="approveForm.实际出库数量" :min="1" :max="currentLoan?.申请数量 || 50" />
        </el-form-item>
        <el-form-item label="出库经办人">
          <el-input v-model="approveForm.出库经办人" placeholder="样本库库管员" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="approveDialogVisible = false">取消</el-button>
        <el-button type="success" :loading="approveSubmitting" @click="handleApproveSubmit">
          确认核准出库
        </el-button>
      </template>
    </el-dialog>

    <!-- 弹窗 3：登记归还与消耗核销 -->
    <el-dialog
      v-model="reconcileDialogVisible"
      title="登记样本归还与实验消耗核销 (经费核算)"
      width="500px"
    >
      <div v-if="currentLoan" style="margin-bottom: 16px; color: #334155;">
        <p>单号：<strong>#{{ currentLoan.编号 }}</strong> ({{ currentLoan.样本编码 }})</p>
        <p>出库总量：<strong>{{ currentLoan.出库数量 }} {{ currentLoan.规格单位 }}</strong> (成本单价: ¥{{ currentLoan.单价成本 }})</p>
        <el-alert
          title="归还数量 + 实验消耗数量 必须严格等于出库数量。实验消耗部分将自动折算经费并分摊记入课题账目。"
          type="info"
          :closable="false"
          show-icon
        />
      </div>

      <el-form :model="reconcileForm" label-width="110px" style="margin-top: 14px;">
        <el-form-item label="归还在库数量" required>
          <el-input-number v-model="reconcileForm.归还数量" :min="0" :max="currentLoan?.出库数量 || 50" />
        </el-form-item>
        <el-form-item label="实验消耗数量" required>
          <el-input-number v-model="reconcileForm.消耗数量" :min="0" :max="currentLoan?.出库数量 || 50" />
        </el-form-item>
        <el-form-item label="预计分摊金额">
          <span style="font-weight: 700; color: #ea580c; font-size: 16px;">
            ¥{{ (reconcileForm.消耗数量 * (currentLoan?.单价成本 || 0)).toFixed(2) }}
          </span>
        </el-form-item>
        <el-form-item label="归还验收人">
          <el-input v-model="reconcileForm.验收人" placeholder="样本库验收质控员" />
        </el-form-item>
        <el-form-item label="实验消耗说明">
          <el-input
            v-model="reconcileForm.核销说明"
            type="textarea"
            :rows="2"
            placeholder="填写实验完成情况、破坏性提取说明等"
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="reconcileDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="reconcileSubmitting" @click="handleReconcileSubmit">
          办理归还与核销记账
        </el-button>
      </template>
    </el-dialog>

    <!-- 弹窗 4：新增生物标本资产入库 -->
    <el-dialog
      v-model="addSpecimenVisible"
      title="新增临床生物标本资产入库"
      width="540px"
    >
      <el-form :model="specimenForm" label-width="110px">
        <el-form-item label="样本编码" required>
          <el-input v-model="specimenForm.样本编码" placeholder="如: BB-2026-LUNG-005" />
        </el-form-item>
        <el-form-item label="标本类型" required>
          <el-select v-model="specimenForm.样本类型" style="width: 100%;">
            <el-option label="冰冻组织" value="冰冻组织" />
            <el-option label="石蜡切片" value="石蜡切片" />
            <el-option label="血清" value="血清" />
            <el-option label="血浆" value="血浆" />
            <el-option label="脑脊液" value="脑脊液" />
            <el-option label="原代细胞/DNA/RNA" value="DNA/RNA" />
          </el-select>
        </el-form-item>
        <el-form-item label="临床诊断" required>
          <el-input v-model="specimenForm.疾病诊断" placeholder="如: 非小细胞肺癌 (NSCLC)" />
        </el-form-item>
        <el-form-item label="解剖部位">
          <el-input v-model="specimenForm.解剖部位" placeholder="如: 肺右下叶" />
        </el-form-item>
        <el-form-item label="冷链温区" required>
          <el-select v-model="specimenForm.储存温度" style="width: 100%;">
            <el-option label="-80℃ 超低温冰箱" value="-80℃" />
            <el-option label="-196℃ 液氮冻存罐" value="-196℃" />
            <el-option label="4℃ 标本冷藏柜" value="4℃" />
            <el-option label="常温 标本柜" value="常温" />
          </el-select>
        </el-form-item>
        <el-form-item label="储存坐标" required>
          <el-input v-model="specimenForm.储存位置" placeholder="如: Biobank-低温区-80℃冰箱#03-B架-01盒-08格" />
        </el-form-item>
        <el-form-item label="入库数量/单位" required>
          <el-input-number v-model="specimenForm.初始数量" :min="1" :max="500" style="margin-right: 12px;" />
          <el-input v-model="specimenForm.规格单位" placeholder="管 / 片 / 盒" style="width: 100px;" />
        </el-form-item>
        <el-form-item label="资产成本单价">
          <el-input-number v-model="specimenForm.单价成本" :min="0" :step="10" />
        </el-form-item>
        <el-form-item label="伦理审查批号">
          <el-input v-model="specimenForm.伦理批文号" placeholder="如: ETH-2026-RENEW-005" />
        </el-form-item>
        <el-form-item label="知情同意书号">
          <el-input v-model="specimenForm.知情同意书编号" placeholder="如: ICF-2026-088" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="addSpecimenVisible = false">取消</el-button>
        <el-button type="primary" :loading="specimenSubmitting" @click="handleAddSpecimenSubmit">
          确认入库
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import {
  getBiobankSpecimens,
  getBiobankLoanLedger,
  applyBiobankLoan,
  approveBiobankLoan,
  returnAndReconcileBiobankLoan,
  addBiobankSpecimen,
  getBiobankStockStats,
  type BiobankSpecimenItem,
  type BiobankLoanItem,
  type BiobankStockStats
} from '@/api/biobank'
import { ElMessage } from 'element-plus'
import {
  Search,
  Refresh,
  Box,
  Plus,
  DocumentChecked,
  Location,
  Document,
  User,
  Check
} from '@element-plus/icons-vue'

const activeTab = ref('assets')
const loadingSpecimens = ref(false)
const loadingLoans = ref(false)

const specimenList = ref<BiobankSpecimenItem[]>([])
const loanList = ref<BiobankLoanItem[]>([])
const statsData = ref<BiobankStockStats>({
  总样本数: 0,
  在库样本数: 0,
  正在实验中借出: 0,
  课题已分摊总成本: 0,
  类型统计: [],
  温区统计: []
})

// 筛选表单
const assetFilter = reactive({
  keyword: '',
  sampleType: '',
  status: ''
})

const loanFilter = reactive({
  keyword: '',
  status: ''
})

// 申请领用弹窗
const applyDialogVisible = ref(false)
const applySubmitting = ref(false)
const applyForm = reactive({
  样本编号: 0,
  项目编号: 5,
  申请数量: 1,
  申请用途: ''
})

// 核准出库弹窗
const approveDialogVisible = ref(false)
const approveSubmitting = ref(false)
const currentLoan = ref<BiobankLoanItem | null>(null)
const approveForm = reactive({
  实际出库数量: 1,
  出库经办人: '样本库库管员'
})

// 归还核销弹窗
const reconcileDialogVisible = ref(false)
const reconcileSubmitting = ref(false)
const reconcileForm = reactive({
  归还数量: 0,
  消耗数量: 1,
  验收人: '样本库质控员',
  核销说明: ''
})

// 新增样本弹窗
const addSpecimenVisible = ref(false)
const specimenSubmitting = ref(false)
const specimenForm = reactive({
  样本编码: '',
  样本类型: '冰冻组织',
  疾病诊断: '',
  解剖部位: '',
  储存温度: '-80℃',
  储存位置: '',
  规格单位: '管',
  初始数量: 10,
  单价成本: 150,
  伦理批文号: 'ETH-2026-RENEW-005',
  知情同意书编号: 'ICF-2026-0391'
})

function getSpecimenTypeTag(type: string): '' | 'success' | 'warning' | 'danger' | 'info' {
  if (type === '冰冻组织') return 'primary' as any
  if (type === '石蜡切片') return 'warning'
  if (type === '血清' || type === '血浆') return 'danger'
  if (type === '脑脊液') return 'success'
  return 'info'
}

function getStatusTag(status: string): '' | 'success' | 'warning' | 'danger' | 'info' {
  if (status === '在库') return 'success'
  if (status === '部分领用') return 'warning'
  if (status === '已借空') return 'info'
  return 'danger'
}

function getLoanStatusTag(status: string): '' | 'success' | 'warning' | 'danger' | 'info' {
  if (status === '申请中') return 'warning'
  if (status === '已出库') return 'primary' as any
  if (status === '已消耗核销') return 'success'
  if (status === '已全部归还') return 'info'
  return 'danger'
}

function getEthicsResultTag(result: string): '' | 'success' | 'warning' | 'danger' | 'info' {
  if (result === '伦理有效已核准') return 'success'
  if (result.includes('硬阻断')) return 'danger'
  return 'info'
}

// 获取样本列表
async function fetchSpecimens() {
  loadingSpecimens.value = true
  try {
    const res = await getBiobankSpecimens({
      keyword: assetFilter.keyword,
      sampleType: assetFilter.sampleType,
      status: assetFilter.status
    })
    if (res && Array.isArray(res)) {
      specimenList.value = res
    }
  } catch (err) {
    console.warn('获取样本列表失败', err)
  } finally {
    loadingSpecimens.value = false
  }
}

function resetAssetFilter() {
  assetFilter.keyword = ''
  assetFilter.sampleType = ''
  assetFilter.status = ''
  fetchSpecimens()
}

// 获取借还台账
async function fetchLoanLedger() {
  loadingLoans.value = true
  try {
    const res = await getBiobankLoanLedger({
      keyword: loanFilter.keyword,
      status: loanFilter.status
    })
    if (res && Array.isArray(res)) {
      loanList.value = res
    }
  } catch (err) {
    console.warn('获取借还台账失败', err)
  } finally {
    loadingLoans.value = false
  }
}

function resetLoanFilter() {
  loanFilter.keyword = ''
  loanFilter.status = ''
  fetchLoanLedger()
}

// 获取大屏统计
async function fetchStats() {
  try {
    const res = await getBiobankStockStats()
    if (res) {
      statsData.value = res
    }
  } catch (err) {
    console.warn('获取大屏统计失败', err)
  }
}

async function refreshAll() {
  await Promise.all([
    fetchSpecimens(),
    fetchLoanLedger(),
    fetchStats()
  ])
}

// 申请领用
function openApplyLoanDialog(row: BiobankSpecimenItem | null) {
  if (row) {
    applyForm.样本编号 = row.编号
  } else if (specimenList.value.length > 0) {
    applyForm.样本编号 = specimenList.value[0].编号
  }
  applyForm.申请数量 = 1
  applyForm.申请用途 = ''
  applyDialogVisible.value = true
}

async function handleApplySubmit() {
  if (!applyForm.样本编号) {
    ElMessage.warning('请选择样本')
    return
  }
  if (!applyForm.申请用途.trim()) {
    ElMessage.warning('请填写申请实验用途')
    return
  }
  applySubmitting.value = true
  try {
    const res = await applyBiobankLoan({
      样本编号: applyForm.样本编号,
      项目编号: applyForm.项目编号,
      申请用途: applyForm.申请用途.trim(),
      申请数量: applyForm.申请数量
    })
    ElMessage.success(res.message || '样本领用申请成功，伦理合规核查通过')
    applyDialogVisible.value = false
    activeTab.value = 'loans'
    await refreshAll()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '领用申请失败')
  } finally {
    applySubmitting.value = false
  }
}

// 核准出库
function openApproveDialog(row: BiobankLoanItem) {
  currentLoan.value = row
  approveForm.实际出库数量 = row.申请数量
  approveForm.出库经办人 = '样本库库管员'
  approveDialogVisible.value = true
}

async function handleApproveSubmit() {
  if (!currentLoan.value) return
  approveSubmitting.value = true
  try {
    const res = await approveBiobankLoan({
      台账编号: currentLoan.value.编号,
      实际出库数量: approveForm.实际出库数量,
      出库经办人: approveForm.出库经办人
    })
    ElMessage.success(res.message || '样本出库核准成功')
    approveDialogVisible.value = false
    await refreshAll()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '出库核准失败')
  } finally {
    approveSubmitting.value = false
  }
}

// 归还核销
function openReconcileDialog(row: BiobankLoanItem) {
  currentLoan.value = row
  reconcileForm.归还数量 = 0
  reconcileForm.消耗数量 = row.出库数量
  reconcileForm.验收人 = '样本库质控员'
  reconcileForm.核销说明 = '已完成测序实验，样本全部消耗'
  reconcileDialogVisible.value = true
}

async function handleReconcileSubmit() {
  if (!currentLoan.value) return
  if ((reconcileForm.归还数量 + reconcileForm.消耗数量) !== currentLoan.value.出库数量) {
    ElMessage.warning(`归还数量(${reconcileForm.归还数量}) + 消耗数量(${reconcileForm.消耗数量}) 必须等于出库数量(${currentLoan.value.出库数量})`)
    return
  }
  reconcileSubmitting.value = true
  try {
    const res = await returnAndReconcileBiobankLoan({
      台账编号: currentLoan.value.编号,
      归还数量: reconcileForm.归还数量,
      消耗数量: reconcileForm.消耗数量,
      验收人: reconcileForm.验收人,
      核销说明: reconcileForm.核销说明
    })
    ElMessage.success(res.message || '归还与消耗核销办理成功')
    reconcileDialogVisible.value = false
    await refreshAll()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '核销失败')
  } finally {
    reconcileSubmitting.value = false
  }
}

// 新增样本
function openAddSpecimenDialog() {
  specimenForm.样本编码 = `BB-${new Date().getFullYear()}-SPEC-${Math.floor(100 + Math.random() * 900)}`
  specimenForm.疾病诊断 = ''
  specimenForm.解剖部位 = ''
  specimenForm.储存位置 = 'Biobank-低温区-80℃冰箱#03-A架-01盒-01格'
  specimenForm.初始数量 = 10
  specimenForm.单价成本 = 150
  addSpecimenVisible.value = true
}

async function handleAddSpecimenSubmit() {
  if (!specimenForm.样本编码 || !specimenForm.疾病诊断 || !specimenForm.储存位置) {
    ElMessage.warning('请填写必填项')
    return
  }
  specimenSubmitting.value = true
  try {
    const res = await addBiobankSpecimen({
      样本编码: specimenForm.样本编码.trim(),
      样本类型: specimenForm.样本类型,
      疾病诊断: specimenForm.疾病诊断.trim(),
      解剖部位: specimenForm.解剖部位?.trim(),
      储存温度: specimenForm.储存温度,
      储存位置: specimenForm.储存位置.trim(),
      规格单位: specimenForm.规格单位.trim(),
      初始数量: specimenForm.初始数量,
      单价成本: specimenForm.单价成本,
      伦理批文号: specimenForm.伦理批文号?.trim(),
      知情同意书编号: specimenForm.知情同意书编号?.trim()
    })
    ElMessage.success(res.message || '标本入库登记成功')
    addSpecimenVisible.value = false
    await refreshAll()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '入库失败')
  } finally {
    specimenSubmitting.value = false
  }
}

onMounted(async () => {
  await refreshAll()
})
</script>

<style scoped lang="scss">
.page-container {
  padding: 16px;

  .overview-card {
    background-color: #ffffff;
    border-radius: 8px;
    margin-bottom: 16px;

    .overview-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
      flex-wrap: wrap;
      gap: 12px;

      .overview-title-block {
        .overview-title {
          font-size: 18px;
          font-weight: 700;
          color: #0f172a;
          margin: 0 0 6px 0;
          display: flex;
          align-items: center;
          gap: 8px;

          .title-icon {
            color: #0284c7;
            font-size: 22px;
          }
        }

        .overview-subtitle {
          font-size: 13px;
          color: #64748b;
          margin: 0;
        }
      }

      .overview-actions {
        display: flex;
        gap: 10px;
      }
    }

    .metric-row {
      .metric-card {
        padding: 14px 16px;
        border-radius: 8px;
        color: #ffffff;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        min-height: 96px;

        .metric-label {
          font-size: 13px;
          font-weight: 500;
          opacity: 0.9;
        }

        .metric-value {
          font-size: 24px;
          font-weight: 700;
          margin: 6px 0;

          .unit {
            font-size: 14px;
            font-weight: 400;
            margin-left: 2px;
          }
        }

        .metric-footer {
          font-size: 11px;
          opacity: 0.8;
        }

        &.metric-blue {
          background: linear-gradient(135deg, #0369a1 0%, #0ea5e9 100%);
        }
        &.metric-green {
          background: linear-gradient(135deg, #065f46 0%, #10b981 100%);
        }
        &.metric-amber {
          background: linear-gradient(135deg, #b45309 0%, #f59e0b 100%);
        }
        &.metric-purple {
          background: linear-gradient(135deg, #6b21a8 0%, #a855f7 100%);
        }
      }
    }
  }

  .main-card {
    background-color: #ffffff;
    border-radius: 8px;

    .toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
    }

    .code-text {
      font-weight: 700;
      color: #0f172a;
      font-size: 14px;
    }

    .code-sub {
      font-size: 11px;
      color: #64748b;
      margin-top: 2px;
    }

    .disease-title {
      font-weight: 600;
      color: #1e293b;
      font-size: 13px;
    }

    .anatomy-text {
      font-size: 12px;
      color: #64748b;
      margin-top: 2px;
    }

    .location-text {
      font-size: 12px;
      color: #475569;
      display: flex;
      align-items: center;
      gap: 3px;
    }

    .stock-num {
      font-size: 13px;
      color: #475569;

      .stock-current {
        font-size: 15px;
        font-weight: 700;
        color: #0f172a;
      }
    }

    .price-text {
      font-weight: 700;
      color: #ea580c;
      font-size: 14px;
    }

    .unit-text {
      font-size: 11px;
      color: #94a3b8;
    }

    .irb-text {
      font-size: 12px;
      color: #334155;
      font-weight: 500;
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .irb-sub {
      font-size: 11px;
      color: #64748b;
      margin-top: 2px;
    }

    .project-title {
      font-weight: 600;
      color: #1e293b;
      font-size: 13px;
    }

    .project-sub {
      font-size: 11px;
      color: #94a3b8;
      margin-top: 2px;
    }

    .applicant-name {
      font-weight: 600;
      color: #0f172a;
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 13px;
    }

    .purpose-text {
      font-size: 12px;
      color: #64748b;
      margin-top: 2px;
    }

    .qty-line {
      font-size: 12px;
      color: #334155;
      line-height: 1.4;
    }

    .done-text {
      font-size: 13px;
      color: #10b981;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 3px;
    }

    .analytics-container {
      padding: 8px 0;

      .sub-card {
        border-radius: 8px;
        margin-bottom: 16px;
      }
    }
  }
}
</style>
