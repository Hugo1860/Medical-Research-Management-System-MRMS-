<template>
  <div class="ethics-page">
    <el-card shadow="never" class="header-card">
      <div class="page-header">
        <div>
          <h2><el-icon><DocumentChecked /></el-icon> 科研伦理审查工作台</h2>
          <p>覆盖申请、秘书受理、委员意见、正式决定，以及年度跟踪、修正案和不良事件记录。页面只显示服务端真实数据。</p>
        </div>
        <div class="header-actions">
          <el-button :icon="Refresh" @click="refresh">刷新</el-button>
          <el-button v-if="canCreateApplication" type="primary" :icon="Plus" @click="openCreate">新建伦理申请</el-button>
          <el-tag v-else type="info" effect="plain">当前角色只读</el-tag>
        </div>
      </div>
      <el-alert
        title="伦理合规提示"
        description="伦理申请必须关联真实项目；只有委员会正式登记批准决定后，项目伦理状态和批件有效期才会更新。"
        type="info"
        :closable="false"
        show-icon
      />
      <div class="workflow-legend" aria-label="伦理审查流程阶段">
        <span class="legend-label">流程阶段</span>
        <el-tag size="small" type="info">初审 / 修正</el-tag>
        <el-tag size="small" type="warning">委员会审查</el-tag>
        <el-tag size="small" type="success">决定 / 批件</el-tag>
        <el-tag size="small" type="primary">持续审查</el-tag>
        <span class="muted">提交后申请不可直接编辑，需补正时才可修改并重新提交。</span>
      </div>
      <el-alert
        v-if="loadError"
        class="load-error"
        :title="loadError"
        type="warning"
        :closable="false"
        show-icon
      />
    </el-card>

    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab" @tab-change="handleTabChange">
        <el-tab-pane label="伦理申请台账" name="applications">
          <div class="toolbar">
            <el-form :inline="true" :model="filter">
              <el-form-item label="状态">
                <el-select v-model="filter.状态" clearable placeholder="全部状态" style="width: 130px">
                  <el-option v-for="item in statusOptions" :key="item" :label="item" :value="item" />
                </el-select>
              </el-form-item>
              <el-form-item label="类型">
                <el-select v-model="filter.申请类型" clearable placeholder="全部类型" style="width: 150px">
                  <el-option v-for="item in typeOptions" :key="item" :label="item" :value="item" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-input v-model="filter.关键词" clearable placeholder="标题、批件编号或申请人" style="width: 230px" @keyup.enter="loadApplications" />
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="loadApplications">查询</el-button>
                <el-button @click="resetFilter">重置</el-button>
              </el-form-item>
            </el-form>
          </div>
          <el-table v-loading="loading" :data="applications" stripe border>
            <el-table-column prop="编号" label="编号" width="80" align="center" />
            <el-table-column prop="申请标题" label="申请标题" min-width="230" show-overflow-tooltip />
            <el-table-column label="项目 / 申请人" min-width="190">
              <template #default="{ row }">
                <div>{{ row.项目名称 || `项目 #${row.项目编号}` }}</div>
                <small class="muted">{{ row.申请人姓名 || `人员 #${row.申请人编号}` }}</small>
              </template>
            </el-table-column>
            <el-table-column prop="申请类型" label="申请类型" width="130" />
            <el-table-column prop="受试者风险等级" label="风险" width="100" align="center">
              <template #default="{ row }"><el-tag :type="riskType(row.受试者风险等级)" size="small">{{ row.受试者风险等级 }}</el-tag></template>
            </el-table-column>
            <el-table-column prop="状态" label="状态" width="110" align="center">
              <template #default="{ row }"><el-tag :type="statusType(row.状态)" size="small">{{ row.状态 }}</el-tag></template>
            </el-table-column>
            <el-table-column prop="有效期至" label="批件有效期" width="130" align="center">
              <template #default="{ row }">{{ formatDate(row.有效期至) }}</template>
            </el-table-column>
            <el-table-column label="操作" width="360" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="viewDetail(row)">详情</el-button>
                <el-button v-if="canEditApplication(row)" link type="primary" size="small" @click="openEdit(row)">编辑</el-button>
                <el-button v-if="canSubmitApplication(row)" link type="success" size="small" @click="submitApplication(row)">提交审查</el-button>
                <el-button v-if="canManageEthics && row.状态 === '待受理'" link type="warning" size="small" @click="acceptApplication(row)">受理</el-button>
                <el-button v-if="canManageEthics && row.状态 === '待受理'" link type="danger" size="small" @click="returnApplication(row)">退回补正</el-button>
                <el-button v-if="canSubmitOpinion && row.状态 === '审查中'" link type="primary" size="small" @click="openOpinion(row)">委员意见</el-button>
                <el-button v-if="canManageEthics && row.状态 === '审查中'" link type="danger" size="small" @click="openDecision(row)">登记决定</el-button>
                <el-button v-if="canSubmitFollowup && row.状态 === '已批准'" link type="info" size="small" @click="openFollowup(row)">持续记录</el-button>
              </template>
            </el-table-column>
          </el-table>
          <el-empty v-if="!loading && applications.length === 0" description="暂无伦理申请记录" />
        </el-tab-pane>

        <el-tab-pane v-if="canManageEthics" label="委员会排期与纪要" name="meetings">
          <div class="toolbar">
            <span class="muted">先受理申请，再排入委员会会议议程；纪要确认后才可归档。</span>
            <el-button v-if="canManageEthics" type="primary" :icon="Plus" @click="openMeeting">新建会议排期</el-button>
          </div>
          <el-table v-loading="meetingsLoading" :data="meetings" stripe border>
            <el-table-column prop="会议序号" label="会议序号" width="140" />
            <el-table-column prop="会议时间" label="会议时间" width="170"><template #default="{ row }">{{ formatDateTime(row.会议时间) }}</template></el-table-column>
            <el-table-column prop="会议地点" label="地点" min-width="150" show-overflow-tooltip />
            <el-table-column prop="议程数量" label="议程" width="80" align="center" />
            <el-table-column prop="状态" label="会议状态" width="100" align="center" />
            <el-table-column prop="纪要状态" label="纪要状态" width="110" align="center" />
            <el-table-column label="操作" width="300" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="viewMeeting(row)">详情</el-button>
                <el-button v-if="canManageEthics && row.状态 !== '已完成' && row.状态 !== '已取消'" link type="warning" size="small" @click="editMeeting(row)">调整排期</el-button>
                <el-button v-if="canManageEthics && row.状态 !== '已完成' && row.状态 !== '已取消'" link type="primary" size="small" @click="openAgenda(row)">排入申请</el-button>
                <el-button v-if="canManageEthics && row.纪要状态 === '待确认'" link type="success" size="small" @click="confirmMinutes(row)">确认纪要</el-button>
                <el-button v-if="canManageEthics && row.纪要状态 !== '已归档'" link type="info" size="small" @click="openMinutes(row)">录入纪要</el-button>
              </template>
            </el-table-column>
          </el-table>
          <el-empty v-if="!meetingsLoading && meetings.length === 0" description="暂无委员会会议排期" />
        </el-tab-pane>

        <el-tab-pane label="到期与持续审查提醒" name="reminders">
          <div class="reminder-toolbar">
            <span>查看未来</span>
            <el-input-number v-model="reminderDays" :min="0" :max="365" controls-position="right" />
            <span>天内的提醒</span>
            <el-button type="primary" :icon="Refresh" @click="loadReminders">查询提醒</el-button>
          </div>
          <el-divider content-position="left">批件有效期提醒</el-divider>
          <el-table :data="reminders.批件提醒" stripe border>
            <el-table-column prop="申请编号" label="申请编号" width="100" />
            <el-table-column prop="申请标题" label="申请标题" min-width="230" />
            <el-table-column prop="批件编号" label="批件编号" width="150" />
            <el-table-column prop="有效期至" label="有效期至" width="130" />
            <el-table-column prop="剩余天数" label="剩余天数" width="100" align="center">
              <template #default="{ row }"><el-tag :type="row.剩余天数 < 0 ? 'danger' : 'warning'">{{ row.剩余天数 < 0 ? `已过期 ${Math.abs(row.剩余天数)} 天` : `${row.剩余天数} 天` }}</el-tag></template>
            </el-table-column>
          </el-table>
          <el-divider content-position="left">持续审查提醒</el-divider>
          <el-table :data="reminders.持续审查提醒" stripe border>
            <el-table-column prop="申请编号" label="申请编号" width="100" />
            <el-table-column prop="申请标题" label="申请标题" min-width="230" />
            <el-table-column prop="记录类型" label="记录类型" width="150" />
            <el-table-column prop="下次审查日期" label="下次审查日期" width="140" />
            <el-table-column prop="状态" label="状态" width="110" />
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <el-dialog v-model="formVisible" :title="form.编号 ? '编辑伦理申请' : '新建伦理审查申请'" width="680px" destroy-on-close>
      <el-alert
        v-if="form.编号 && !isEditableStatus(form.状态)"
        title="当前申请已提交，暂不可编辑"
        description="提交、受理或审查中的申请会保留原始版本；如需调整，请通过修正案重新发起伦理审查。"
        type="warning"
        :closable="false"
        show-icon
        class="dialog-alert"
      />
      <el-alert
        v-if="materialCheck(form).missing.length"
        title="材料完整性预检"
        type="warning"
        :closable="false"
        show-icon
        class="dialog-alert"
      >
        <template #default>
          提交审查前仍缺少：{{ materialCheck(form).missing.join('、') }}。当前可以先保存草稿，补齐材料后再提交。
        </template>
      </el-alert>
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="120px" :disabled="!!form.编号 && !isEditableStatus(form.状态)">
        <el-row :gutter="12">
          <el-col :span="12"><el-form-item label="流程模板编号" prop="流程模板编号"><el-input-number v-model="form.流程模板编号" :min="1" style="width: 100%" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="项目编号" prop="项目编号"><el-input-number v-model="form.项目编号" :min="1" style="width: 100%" /></el-form-item></el-col>
        </el-row>
        <el-form-item label="申请类型" prop="申请类型"><el-select v-model="form.申请类型" style="width: 100%"><el-option v-for="item in typeOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
        <el-form-item label="申请标题" prop="申请标题"><el-input v-model="form.申请标题" maxlength="200" show-word-limit /></el-form-item>
        <el-form-item label="风险等级"><el-radio-group v-model="form.受试者风险等级"><el-radio value="低风险">低风险</el-radio><el-radio value="一般风险">一般风险</el-radio><el-radio value="高风险">高风险</el-radio></el-radio-group></el-form-item>
        <el-form-item label="研究方案摘要" prop="研究方案摘要"><el-input v-model="form.研究方案摘要" type="textarea" :rows="5" maxlength="20000" show-word-limit /></el-form-item>
        <el-form-item label="知情同意说明"><el-input v-model="form.知情同意说明" type="textarea" :rows="2" /></el-form-item>
        <el-form-item label="附件路径"><el-input v-model="form.附件路径" placeholder="填写已上传附件的服务端路径" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="formVisible = false">关闭</el-button><el-button v-if="!form.编号 || isEditableStatus(form.状态)" type="primary" :loading="saving" @click="saveApplication">保存草稿</el-button></template>
    </el-dialog>

    <el-dialog v-model="detailVisible" title="伦理申请详情" width="850px" destroy-on-close>
      <template v-if="detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="申请编号">{{ detail.申请.编号 }}</el-descriptions-item>
          <el-descriptions-item label="项目">{{ detail.申请.项目名称 || `项目 #${detail.申请.项目编号}` }}</el-descriptions-item>
          <el-descriptions-item label="申请类型">{{ detail.申请.申请类型 }}</el-descriptions-item>
          <el-descriptions-item label="状态"><el-tag :type="statusType(detail.申请.状态)">{{ detail.申请.状态 }}</el-tag></el-descriptions-item>
          <el-descriptions-item label="研究方案摘要" :span="2">{{ detail.申请.研究方案摘要 }}</el-descriptions-item>
          <el-descriptions-item label="批件编号">{{ detail.申请.批件编号 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="有效期至">{{ formatDate(detail.申请.有效期至) }}</el-descriptions-item>
          <el-descriptions-item label="当前可见范围">{{ detail.可见范围 || '申请人/项目范围' }}</el-descriptions-item>
        </el-descriptions>
        <el-alert
          v-if="!isEditableStatus(detail.申请.状态)"
          class="dialog-alert"
          title="当前状态不可编辑"
          description="提交后的申请只能通过补正或修正案形成新版本；原申请、委员意见和决定记录保持只读。"
          type="info"
          :closable="false"
          show-icon
        />
        <el-alert
          v-if="isEditableStatus(detail.申请.状态) && materialCheck(detail.申请).missing.length"
          class="dialog-alert"
          title="材料完整性预检未通过"
          type="warning"
          :closable="false"
          show-icon
        >
          <template #default>缺少：{{ materialCheck(detail.申请).missing.join('、') }}。未补齐前不能提交委员会审查。</template>
        </el-alert>
        <el-divider content-position="left">委员会意见（{{ detail.委员意见.length }}）</el-divider>
        <el-table :data="detail.委员意见" size="small" border><el-table-column prop="委员姓名" label="委员" width="100" /><el-table-column prop="意见结论" label="结论" width="110" /><el-table-column prop="是否利益冲突" label="利益冲突" width="100"><template #default="{ row }">{{ row.是否利益冲突 ? '已回避' : '否' }}</template></el-table-column><el-table-column prop="意见内容" label="意见" min-width="260" /></el-table>
        <el-divider content-position="left">委员会决定（{{ (detail.决定 || []).length }}）</el-divider>
        <el-table :data="detail.决定 || []" size="small" border>
          <el-table-column prop="决定" label="决定" width="120"><template #default="{ row }"><el-tag :type="decisionType(row.决定)" size="small">{{ row.决定 }}</el-tag></template></el-table-column>
          <el-table-column prop="决定说明" label="决定说明" min-width="260" show-overflow-tooltip />
          <el-table-column prop="批件编号" label="批件编号" width="150" />
          <el-table-column prop="有效期至" label="有效期至" width="130"><template #default="{ row }">{{ formatDate(row.有效期至) }}</template></el-table-column>
          <el-table-column prop="批准时间" label="登记时间" width="150"><template #default="{ row }">{{ formatDateTime(row.批准时间 || row.决定时间) }}</template></el-table-column>
        </el-table>
        <el-divider content-position="left">持续审查与事件（{{ detail.持续记录.length }}）</el-divider>
        <el-table :data="detail.持续记录" size="small" border>
          <el-table-column prop="记录类型" label="类型" width="140" />
          <el-table-column prop="严重程度" label="严重程度" width="110" />
          <el-table-column prop="事件说明" label="说明" min-width="260" show-overflow-tooltip />
          <el-table-column prop="状态" label="状态" width="110" />
          <el-table-column label="操作" width="130" fixed="right" align="center">
            <template #default="{ row }">
              <el-button v-if="canManageEthics && (row.状态 === '待审查' || row.状态 === '审查中')" link type="warning" size="small" @click="openFollowupReview(row)">委员会审核</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
      <template #footer>
        <el-button v-if="detail && canSubmitOpinion && detail.申请 && detail.申请.状态 === '审查中'" type="primary" @click="openOpinion(detail.申请)">登记委员意见</el-button>
        <el-button v-if="detail && canManageEthics && latestDecision(detail)" type="warning" @click="openDecisionArchive(detail)">归档决定书</el-button>
        <el-button @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="opinionVisible" title="登记伦理委员意见" width="620px" destroy-on-close>
      <el-alert title="委员意见必须关联已排期会议；如存在利益冲突，只能提交回避意见。" type="info" :closable="false" show-icon />
      <el-form :model="opinion" label-width="110px" class="review-form">
        <el-form-item label="申请编号"><el-input-number v-model="opinion.申请编号" :min="1" disabled style="width: 100%" /></el-form-item>
        <el-form-item label="会议编号" required><el-input-number v-model="opinion.会议编号" :min="1" style="width: 100%" /></el-form-item>
        <el-form-item label="意见结论" required><el-select v-model="opinion.意见结论" style="width: 100%"><el-option v-for="item in opinionOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
        <el-form-item label="利益冲突"><el-switch v-model="opinion.是否利益冲突" active-text="需要回避" inactive-text="无冲突" /></el-form-item>
        <el-form-item label="评分"><el-input-number v-model="opinion.评分" :min="0" :max="100" :precision="2" controls-position="right" /></el-form-item>
        <el-form-item label="意见内容"><el-input v-model="opinion.意见内容" type="textarea" :rows="5" maxlength="4000" show-word-limit /></el-form-item>
      </el-form>
      <template #footer><el-button @click="opinionVisible = false">取消</el-button><el-button type="primary" :loading="saving" @click="saveOpinion">提交意见</el-button></template>
    </el-dialog>

    <el-dialog v-model="decisionVisible" title="登记伦理委员会决定" width="600px" destroy-on-close>
      <el-form :model="decision" label-width="110px">
        <el-form-item label="决定"><el-select v-model="decision.决定" style="width: 100%"><el-option v-for="item in decisionOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
        <el-form-item v-if="decision.决定 === '批准' || decision.决定 === '修改后批准'" label="批件编号"><el-input v-model="decision.批件编号" /></el-form-item>
        <el-form-item v-if="decision.决定 === '批准' || decision.决定 === '修改后批准'" label="有效期至"><el-date-picker v-model="decision.有效期至" type="date" value-format="YYYY-MM-DD" style="width: 100%" /></el-form-item>
        <el-form-item label="决定说明"><el-input v-model="decision.决定说明" type="textarea" :rows="4" /></el-form-item>
        <el-form-item label="决定书路径"><el-input v-model="decision.决定书路径" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="decisionVisible = false">取消</el-button><el-button type="primary" :loading="saving" @click="saveDecision">提交决定</el-button></template>
    </el-dialog>

    <el-dialog v-model="followupVisible" title="提交持续伦理记录" width="620px" destroy-on-close>
      <el-form :model="followup" label-width="110px">
        <el-form-item label="记录类型"><el-select v-model="followup.记录类型" style="width: 100%"><el-option v-for="item in followupOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
        <el-form-item label="发生日期"><el-date-picker v-model="followup.发生日期" type="date" value-format="YYYY-MM-DD" style="width: 100%" /></el-form-item>
        <el-form-item label="下次审查日期"><el-date-picker v-model="followup.下次审查日期" type="date" value-format="YYYY-MM-DD" style="width: 100%" /></el-form-item>
        <el-form-item v-if="followup.记录类型 === '不良事件'" label="严重程度"><el-select v-model="followup.严重程度" style="width: 100%"><el-option label="一般" value="一般" /><el-option label="严重" value="严重" /><el-option label="危及生命" value="危及生命" /></el-select></el-form-item>
        <el-form-item label="事件说明"><el-input v-model="followup.事件说明" type="textarea" :rows="5" /></el-form-item>
        <el-form-item label="附件路径"><el-input v-model="followup.附件路径" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="followupVisible = false">取消</el-button><el-button type="primary" :loading="saving" @click="saveFollowup">提交记录</el-button></template>
    </el-dialog>

    <el-dialog v-model="followupReviewVisible" title="审核持续伦理记录" width="620px" destroy-on-close>
      <el-alert title="审核结果会写入伦理审查台账并保留审核人、审核时间；不良事件和修正案的处理结论必须明确填写。" type="info" :closable="false" show-icon />
      <el-form :model="followupReview" label-width="110px" class="review-form">
        <el-form-item label="审核结果">
          <el-select v-model="followupReview.结果" style="width: 100%">
            <el-option label="审查中" value="审查中" />
            <el-option label="已批准" value="已批准" />
            <el-option label="需补正" value="需补正" />
            <el-option label="已关闭" value="已关闭" />
          </el-select>
        </el-form-item>
        <el-form-item label="处理结论" required>
          <el-input v-model="followupReview.处理结论" type="textarea" :rows="5" maxlength="2000" show-word-limit placeholder="记录委员会对跟踪审查、修正案、不良事件或方案偏离的处理决定" />
        </el-form-item>
      </el-form>
      <template #footer><el-button @click="followupReviewVisible = false">取消</el-button><el-button type="primary" :loading="saving" @click="saveFollowupReview">提交审核</el-button></template>
    </el-dialog>

    <el-dialog v-model="meetingVisible" :title="meeting.编号 ? '调整委员会会议' : '新建委员会会议'" width="620px" destroy-on-close>
      <el-form :model="meeting" label-width="110px">
        <el-form-item label="会议序号"><el-input v-model="meeting.会议序号" maxlength="50" /></el-form-item>
        <el-form-item label="会议时间"><el-date-picker v-model="meeting.会议时间" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" style="width: 100%" /></el-form-item>
        <el-form-item label="会议地点"><el-input v-model="meeting.会议地点" /></el-form-item>
        <el-form-item label="会议状态"><el-select v-model="meeting.状态" style="width: 100%"><el-option v-for="item in meetingStatusOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
        <el-form-item label="议程说明"><el-input v-model="meeting.议程说明" type="textarea" :rows="2" /></el-form-item>
        <el-form-item label="议程附件路径"><el-input v-model="meeting.议程路径" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="meetingVisible = false">取消</el-button><el-button type="primary" :loading="meetingSaving" @click="saveMeetingForm">保存排期</el-button></template>
    </el-dialog>

    <el-dialog v-model="meetingDetailVisible" title="委员会会议详情" width="920px" destroy-on-close>
      <template v-if="meetingDetail">
        <el-descriptions :column="3" border>
          <el-descriptions-item label="会议序号">{{ meetingDetail.会议.会议序号 }}</el-descriptions-item>
          <el-descriptions-item label="会议时间">{{ formatDateTime(meetingDetail.会议.会议时间) }}</el-descriptions-item>
          <el-descriptions-item label="纪要状态">{{ meetingDetail.会议.纪要状态 }}</el-descriptions-item>
          <el-descriptions-item label="当前可见范围">{{ meetingDetail.可见范围 || '伦理管理' }}</el-descriptions-item>
          <el-descriptions-item label="表决方式">{{ meetingDetail.会议.是否匿名表决 === false ? '实名受控审计' : '匿名汇总' }}</el-descriptions-item>
        </el-descriptions>
        <el-divider content-position="left">会议议程</el-divider>
        <el-table :data="meetingDetail.议程" size="small" border>
          <el-table-column prop="排序号" label="序号" width="65" />
          <el-table-column prop="申请编号" label="申请编号" width="90" />
          <el-table-column prop="申请标题" label="申请标题" min-width="230" />
          <el-table-column prop="申请人姓名" label="申请人" width="100" />
          <el-table-column prop="状态" label="议程状态" width="100" />
        </el-table>
        <el-divider content-position="left">匿名表决汇总</el-divider>
        <el-table :data="meetingVotes" size="small" border>
          <el-table-column prop="申请编号" label="申请编号" width="90" />
          <el-table-column prop="申请标题" label="申请标题" min-width="220" />
          <el-table-column prop="同意票" label="同意" width="70" />
          <el-table-column prop="修改后同意票" label="修改后同意" width="90" />
          <el-table-column prop="不同意票" label="不同意" width="80" />
          <el-table-column prop="回避票" label="回避" width="70" />
          <el-table-column prop="有效意见数" label="有效意见" width="90" />
        </el-table>
        <el-divider content-position="left">会议纪要</el-divider>
        <el-alert v-if="!meetingDetail.纪要" title="尚未录入会议纪要" type="info" :closable="false" />
        <template v-else>
          <el-descriptions :column="2" border>
            <el-descriptions-item label="状态">{{ meetingDetail.纪要.状态 }}</el-descriptions-item>
            <el-descriptions-item label="记录时间">{{ formatDateTime(meetingDetail.纪要.更新时间 || meetingDetail.纪要.建立时间) }}</el-descriptions-item>
            <el-descriptions-item label="纪要正文" :span="2">{{ meetingDetail.纪要.纪要正文 }}</el-descriptions-item>
          </el-descriptions>
        </template>
        <el-divider content-position="left">电子档案（{{ meetingDetail.电子档案.length }}）</el-divider>
        <el-table :data="meetingDetail.电子档案" size="small" border><el-table-column prop="档案类型" label="类型" width="110" /><el-table-column prop="文件名称" label="文件名" min-width="180" /><el-table-column prop="状态" label="状态" width="100" /><el-table-column prop="实际校验状态" label="实际校验" width="100" /><el-table-column prop="文件大小" label="大小(B)" width="100" /><el-table-column prop="内容类型" label="类型" width="150" show-overflow-tooltip /><el-table-column prop="文件路径" label="路径" min-width="250" show-overflow-tooltip /></el-table>
        <el-alert v-if="archiveVerification" class="dialog-alert" type="success" :closable="false" show-icon title="对象存储实际文件校验已完成"><template #default>SHA-256 已核对；大小 {{ archiveVerification.文件大小 || 0 }} B，内容类型 {{ archiveVerification.内容类型 || '未知' }}。</template></el-alert>
      </template>
      <template #footer>
        <el-button v-if="canManageEthics && meetingDetail && meetingDetail.纪要 && meetingDetail.纪要.状态 === '待确认'" type="success" @click="confirmMinutes({ 编号: meetingDetail.会议.编号, 纪要状态: '待确认' } as any)">确认纪要</el-button>
        <el-button v-if="canManageEthics && meetingDetail" type="primary" @click="openMinutes(meetingDetail.会议)">录入/修改纪要</el-button>
        <el-button v-if="canManageEthics && meetingDetail" type="warning" @click="openArchive(meetingDetail.会议)">归档会议材料</el-button>
        <el-button @click="meetingDetailVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="minutesVisible" title="录入委员会会议纪要" width="680px" destroy-on-close>
      <el-form :model="minutes" label-width="120px">
        <el-form-item label="纪要正文"><el-input v-model="minutes.纪要正文" type="textarea" :rows="7" maxlength="20000" show-word-limit /></el-form-item>
        <el-form-item label="出席委员摘要"><el-input v-model="minutes.出席委员摘要" type="textarea" :rows="2" /></el-form-item>
        <el-form-item label="决议摘要"><el-input v-model="minutes.决议摘要" type="textarea" :rows="2" /></el-form-item>
        <el-form-item label="纪要附件路径"><el-input v-model="minutes.附件路径" /></el-form-item>
        <el-form-item label="提交状态"><el-radio-group v-model="minutes.状态"><el-radio value="草稿">保存草稿</el-radio><el-radio value="待确认">提交确认</el-radio></el-radio-group></el-form-item>
      </el-form>
      <template #footer><el-button @click="minutesVisible = false">取消</el-button><el-button type="primary" :loading="meetingSaving" @click="saveMinutesForm">保存纪要</el-button></template>
    </el-dialog>

    <el-dialog v-model="archiveVisible" :title="archive.档案类型 === '决定书' ? '归档伦理决定书' : '归档伦理会议材料'" width="620px" destroy-on-close>
      <el-form :model="archive" label-width="110px">
        <el-form-item label="档案类型"><el-select v-model="archive.档案类型" style="width: 100%"><el-option v-for="item in archiveTypeOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
        <el-form-item label="文件名称"><el-input v-model="archive.文件名称" /></el-form-item>
        <el-form-item label="文件路径"><el-input v-model="archive.文件路径" placeholder="受控文件服务路径或对象存储键" /></el-form-item>
        <el-form-item label="SHA-256校验值" required><el-input v-model="archive.文件校验值" maxlength="64" placeholder="正式会议材料必填64位十六进制SHA-256摘要" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="archive.备注" type="textarea" :rows="2" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="archiveVisible = false">取消</el-button><el-button type="primary" :loading="meetingSaving" @click="saveArchiveForm">提交归档</el-button></template>
    </el-dialog>

    <el-dialog v-model="agendaVisible" title="将伦理申请排入委员会会议" width="560px" destroy-on-close>
      <el-form :model="agenda" label-width="110px">
        <el-form-item label="会议编号"><el-input-number v-model="agenda.会议编号" :min="1" disabled style="width: 100%" /></el-form-item>
        <el-form-item label="申请编号"><el-input-number v-model="agenda.申请编号" :min="1" style="width: 100%" /></el-form-item>
        <el-form-item label="议程排序"><el-input-number v-model="agenda.排序号" :min="1" style="width: 100%" /></el-form-item>
        <el-form-item label="议题说明"><el-input v-model="agenda.议题说明" type="textarea" :rows="3" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="agendaVisible = false">取消</el-button><el-button type="primary" :loading="meetingSaving" @click="saveAgendaForm">保存议程</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { DocumentChecked, Plus, Refresh } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'
import {
  acceptEthicsApplication,
  getEthicsApplicationDetail,
  getEthicsApplications,
  getEthicsExpiryReminders,
  getEthicsMeetings,
  getEthicsMeetingDetail,
  saveEthicsApplication,
  saveEthicsOpinion,
  saveEthicsDecision,
  saveEthicsFollowup,
  reviewEthicsFollowup,
  saveEthicsMeeting,
  saveEthicsAgenda,
  saveEthicsMinutes,
  confirmEthicsMinutes,
  getEthicsAnonymousVote,
  archiveEthicsMaterial,
  submitEthicsApplication
} from '@/api/ethics'
import type { EthicsApplication, EthicsMeeting } from '@/api/ethics'

const userStore = useUserStore()
const roleNames = computed(() => {
  const value = userStore.userInfo?.角色名称 || ''
  return value.split(/[、,，;；|/\s]+/).map(item => item.trim()).filter(Boolean)
})
function hasAnyRole(...roles: string[]) {
  return roles.some(role => roleNames.value.some(current => current === role || current.includes(role) || role.includes(current)))
}
// 这些判断只用于收敛前端操作入口，最终权限仍由服务端接口强制校验。
// 委员/专家只负责提交意见，不显示受理、决定、会议排期或归档等管理动作。
const canManageEthics = computed(() => hasAnyRole('系统管理员', '科研管理员', '科技办', '科研科负责人', '科研处处长', '伦理秘书', '伦理委员会秘书'))
const canSubmitOpinion = computed(() => hasAnyRole('系统管理员', '科研管理员', '科技办', '科研科负责人', '科研处处长', '伦理秘书', '伦理委员会秘书', '伦理委员', '伦理委员会委员', '专家'))
const canCreateApplication = computed(() => roleNames.value.length === 0 || hasAnyRole('系统管理员', '科研人员', '科室负责人', '科研秘书', '科研管理员', '科技办', '科研科负责人', '科研处处长'))
const canSubmitFollowup = computed(() => roleNames.value.length === 0 || hasAnyRole('系统管理员', '科研人员', '科室负责人', '科研秘书', '科研管理员', '科技办', '科研科负责人', '科研处处长'))
const editableStatuses = new Set(['草稿', '需补正'])
function isEditableStatus(status?: string) { return !!status && editableStatuses.has(status) }
function canEditApplication(row: EthicsApplication) { return canCreateApplication.value && isEditableStatus(row.状态) }
function canSubmitApplication(row: EthicsApplication) { return canCreateApplication.value && isEditableStatus(row.状态) }

const activeTab = ref('applications')
const loading = ref(false)
const loadError = ref('')
const saving = ref(false)
const applications = ref<EthicsApplication[]>([])
const filter = reactive({ 状态: '', 申请类型: '', 关键词: '' })
const statusOptions = ['草稿', '待受理', '审查中', '需补正', '已批准', '已拒绝', '已撤回']
const typeOptions = ['初始审查', '修正案', '年度跟踪审查', '延期续展']
const decisionOptions = ['批准', '修改后批准', '补正后再审', '拒绝', '终止']
const opinionOptions = ['同意', '修改后同意', '不同意', '回避']
const followupOptions = ['年度跟踪审查', '方案修正案', '知情同意修订', '不良事件', '偏离方案']
const reminderDays = ref(60)
const reminders = reactive<{ 批件提醒: any[]; 持续审查提醒: any[] }>({ 批件提醒: [], 持续审查提醒: [] })

const formVisible = ref(false)
const formRef = ref<FormInstance>()
const form = reactive<any>({ 编号: undefined, 状态: '草稿', 流程模板编号: 0, 项目编号: 0, 申请类型: '初始审查', 申请标题: '', 研究方案摘要: '', 受试者风险等级: '一般风险', 知情同意说明: '', 附件路径: '' })
const formRules: FormRules = { 流程模板编号: [{ required: true, message: '请输入流程模板编号', trigger: 'blur' }], 项目编号: [{ required: true, message: '请输入项目编号', trigger: 'blur' }], 申请类型: [{ required: true, message: '请选择申请类型', trigger: 'change' }], 申请标题: [{ required: true, message: '请输入申请标题', trigger: 'blur' }], 研究方案摘要: [{ required: true, message: '请填写研究方案摘要', trigger: 'blur' }] }

const detailVisible = ref(false)
const detail = ref<any>(null)
const opinionVisible = ref(false)
const opinion = reactive<any>({ 申请编号: 0, 会议编号: 0, 是否利益冲突: false, 意见结论: '同意', 意见内容: '', 评分: undefined })
const decisionVisible = ref(false)
const decision = reactive<any>({ 申请编号: 0, 决定: '批准', 决定说明: '', 批件编号: '', 有效期至: '', 决定书路径: '' })
const followupVisible = ref(false)
const followup = reactive<any>({ 申请编号: 0, 记录类型: '年度跟踪审查', 发生日期: '', 下次审查日期: '', 严重程度: '一般', 事件说明: '', 附件路径: '' })
const followupReviewVisible = ref(false)
const followupReview = reactive<any>({ 记录编号: 0, 结果: '已批准', 处理结论: '' })

const meetingsLoading = ref(false)
const meetingSaving = ref(false)
const meetings = ref<EthicsMeeting[]>([])
const meetingStatusOptions = ['计划中', '进行中', '已完成', '已取消']
const archiveTypeOptions = ['会议议程', '会议纪要', '决定书', '申请材料', '持续审查', '不良事件', '修正案', '其他']
const meetingVisible = ref(false)
const meeting = reactive<any>({ 会议序号: '', 会议时间: '', 会议地点: '', 状态: '计划中', 议程说明: '', 议程路径: '', 备注: '' })
const meetingDetailVisible = ref(false)
const meetingDetail = ref<any>(null)
const meetingVotes = ref<any[]>([])
const minutesVisible = ref(false)
const minutes = reactive<any>({ 会议编号: 0, 纪要正文: '', 出席委员摘要: '', 决议摘要: '', 附件路径: '', 状态: '草稿' })
const archiveVisible = ref(false)
const archive = reactive<any>({ 申请编号: undefined, 会议编号: undefined, 决定编号: undefined, 档案类型: '会议纪要', 文件名称: '', 文件路径: '', 文件校验值: '', 备注: '' })
const archiveVerification = ref<any>(null)
const agendaVisible = ref(false)
const agenda = reactive<any>({ 会议编号: 0, 申请编号: 0, 排序号: 1, 议题说明: '', 状态: '已排期' })

function materialCheck(value: Partial<EthicsApplication> | null | undefined) {
  const missing: string[] = []
  if (!value?.流程模板编号 || Number(value.流程模板编号) <= 0) missing.push('流程模板')
  if (!value?.项目编号 || Number(value.项目编号) <= 0) missing.push('关联项目')
  if (!String(value?.申请标题 || '').trim()) missing.push('申请标题')
  if (!String(value?.研究方案摘要 || '').trim()) missing.push('研究方案摘要')
  if (value?.申请类型 === '初始审查' || value?.受试者风险等级 === '高风险') {
    if (!String(value?.知情同意说明 || '').trim()) missing.push('知情同意说明')
  }
  if (!String(value?.附件路径 || '').trim()) missing.push('申请附件')
  if (value?.材料完整 === false && Array.isArray(value.缺失材料)) {
    for (const item of value.缺失材料) if (item && !missing.includes(item)) missing.push(item)
  }
  return { complete: missing.length === 0, missing }
}

async function loadApplications() { loading.value = true; loadError.value = ''; try { applications.value = await getEthicsApplications({ ...filter }) } catch { applications.value = []; loadError.value = '伦理申请台账加载失败，请检查登录状态和服务端连接后重试。' } finally { loading.value = false } }
async function loadMeetings() { meetingsLoading.value = true; try { meetings.value = await getEthicsMeetings() } catch { meetings.value = []; loadError.value = '伦理委员会会议排期加载失败，请检查服务端连接后重试。' } finally { meetingsLoading.value = false } }
async function loadReminders() { try { const result = await getEthicsExpiryReminders(reminderDays.value); reminders.批件提醒 = result.批件提醒 || []; reminders.持续审查提醒 = result.持续审查提醒 || [] } catch { reminders.批件提醒 = []; reminders.持续审查提醒 = []; loadError.value = '伦理到期提醒加载失败，请检查服务端连接后重试。' } }
async function refresh() { await loadApplications(); await loadReminders(); await loadMeetings() }
async function handleTabChange(name: string | number) { if (name === 'reminders') await loadReminders(); if (name === 'meetings') await loadMeetings() }
function resetFilter() { filter.状态 = ''; filter.申请类型 = ''; filter.关键词 = ''; loadApplications() }
function openCreate() { Object.assign(form, { 编号: undefined, 状态: '草稿', 流程模板编号: 0, 项目编号: 0, 申请类型: '初始审查', 申请标题: '', 研究方案摘要: '', 受试者风险等级: '一般风险', 知情同意说明: '', 附件路径: '' }); formVisible.value = true }
async function openEdit(row: EthicsApplication) {
  if (!canEditApplication(row)) { ElMessage.info('当前申请状态不可编辑，请通过修正案重新发起'); return }
  try {
    const result = await getEthicsApplicationDetail(row.编号)
    Object.assign(form, result.申请, { 状态: result.申请.状态 || row.状态 })
    formVisible.value = true
  } catch { /* request interceptor has shown the error */ }
}
async function saveApplication() {
  if (form.编号 && !isEditableStatus(form.状态)) { ElMessage.info('当前申请状态不可编辑，请通过修正案重新发起'); return }
  if (!formRef.value) return
  await formRef.value.validate(async valid => {
    if (!valid) return
    saving.value = true
    try { await saveEthicsApplication(form); ElMessage.success('伦理申请草稿已保存'); formVisible.value = false; await loadApplications() }
    catch { /* request interceptor has shown the error */ }
    finally { saving.value = false }
  })
}
async function submitApplication(row: EthicsApplication) {
  try {
    const result = await getEthicsApplicationDetail(row.编号)
    const check = materialCheck(result.申请)
    if (!check.complete) { ElMessage.warning(`材料不完整，暂不能提交：${check.missing.join('、')}`); return }
    await ElMessageBox.confirm('提交后申请将进入伦理委员会受理队列，申请人不能直接修改，是否继续？', '提交伦理审查', { type: 'warning' })
    await submitEthicsApplication(row.编号)
    ElMessage.success('已提交伦理审查')
    await loadApplications()
  } catch { /* cancel or request error */ }
}
async function acceptApplication(row: EthicsApplication) { try { const result = await ElMessageBox.prompt('可填写补正原因；直接确认表示受理。', '受理伦理申请', { inputPlaceholder: '补正原因（退回时必填）', confirmButtonText: '受理', cancelButtonText: '取消' }); await acceptEthicsApplication({ 申请编号: row.编号, 结果: '受理', 退回原因: result.value }); ElMessage.success('申请已受理'); await loadApplications() } catch { /* cancel or request error */ } }
async function returnApplication(row: EthicsApplication) { try { const result = await ElMessageBox.prompt('请明确需要申请人补充的材料或说明。', '退回补正', { inputPlaceholder: '补正原因（必填）', confirmButtonText: '退回补正', cancelButtonText: '取消', inputValidator: value => value.trim().length > 0 ? true : '请填写补正原因' }); await acceptEthicsApplication({ 申请编号: row.编号, 结果: '退回补正', 退回原因: result.value }); ElMessage.success('申请已退回补正'); await loadApplications() } catch { /* cancel or request error */ } }
async function viewDetail(row: EthicsApplication) { try { detail.value = await getEthicsApplicationDetail(row.编号); detailVisible.value = true } catch { /* request interceptor has shown the error */ } }
function openOpinion(row: EthicsApplication) { Object.assign(opinion, { 申请编号: row.编号, 会议编号: 0, 是否利益冲突: false, 意见结论: '同意', 意见内容: '', 评分: undefined }); opinionVisible.value = true }
async function saveOpinion() {
  if (!opinion.会议编号 || opinion.会议编号 <= 0) { ElMessage.warning('请填写已排期的伦理委员会会议编号'); return }
  if (opinion.是否利益冲突 && opinion.意见结论 !== '回避') { ElMessage.warning('存在利益冲突时只能提交回避意见'); return }
  saving.value = true
  try { await saveEthicsOpinion(opinion); ElMessage.success('伦理委员意见已提交'); opinionVisible.value = false; if (detail.value?.申请?.编号) detail.value = await getEthicsApplicationDetail(detail.value.申请.编号) } catch { /* request interceptor has shown the error */ } finally { saving.value = false }
}
function latestDecision(value: any) { return Array.isArray(value?.决定) && value.决定.length > 0 ? value.决定[0] : null }
function openDecisionArchive(value: any) {
  const current = latestDecision(value)
  if (!current) return
  Object.assign(archive, { 申请编号: value.申请.编号, 会议编号: undefined, 决定编号: current.编号, 档案类型: '决定书', 文件名称: '', 文件路径: value.申请.批件路径 || current.决定书路径 || '', 文件校验值: '', 备注: '' })
  archiveVisible.value = true
}
function openDecision(row: EthicsApplication) { Object.assign(decision, { 申请编号: row.编号, 决定: '批准', 决定说明: '', 批件编号: '', 有效期至: '', 决定书路径: '' }); decisionVisible.value = true }
async function saveDecision() { if (!decision.决定说明.trim()) { ElMessage.warning('请填写决定说明'); return } if ((decision.决定 === '批准' || decision.决定 === '修改后批准') && (!decision.批件编号.trim() || !decision.有效期至)) { ElMessage.warning('批准决定必须填写批件编号和有效期'); return } saving.value = true; try { await saveEthicsDecision(decision); ElMessage.success('伦理决定已登记'); decisionVisible.value = false; await loadApplications() } catch { /* request interceptor has shown the error */ } finally { saving.value = false } }
function openFollowup(row: EthicsApplication) { Object.assign(followup, { 申请编号: row.编号, 记录类型: '年度跟踪审查', 发生日期: '', 下次审查日期: '', 严重程度: '一般', 事件说明: '', 附件路径: '' }); followupVisible.value = true }
async function saveFollowup() {
  if (!followup.事件说明.trim()) { ElMessage.warning('请填写记录说明'); return }
  if (followup.记录类型 === '不良事件' && !followup.严重程度) { ElMessage.warning('不良事件必须选择严重程度'); return }
  if (followup.记录类型 === '不良事件' && !followup.发生日期) { ElMessage.warning('不良事件必须填写发生日期'); return }
  saving.value = true
  try { await saveEthicsFollowup(followup); ElMessage.success('持续伦理记录已提交'); followupVisible.value = false; await loadReminders() } catch { /* request interceptor has shown the error */ } finally { saving.value = false }
}
function openFollowupReview(row: any) { Object.assign(followupReview, { 记录编号: row.编号, 结果: row.状态 === '审查中' ? '已批准' : '审查中', 处理结论: row.处理结论 || '' }); followupReviewVisible.value = true }
async function saveFollowupReview() {
  if (!followupReview.处理结论.trim()) { ElMessage.warning('请填写处理结论'); return }
  saving.value = true
  try {
    await reviewEthicsFollowup(followupReview)
    ElMessage.success('持续伦理记录审核已保存')
    followupReviewVisible.value = false
    if (detail.value?.申请?.编号) detail.value = await getEthicsApplicationDetail(detail.value.申请.编号)
    await loadReminders()
  } catch { /* request interceptor has shown the error */ } finally { saving.value = false }
}
function openMeeting() { Object.assign(meeting, { 编号: undefined, 会议序号: '', 会议时间: '', 会议地点: '', 状态: '计划中', 议程说明: '', 议程路径: '', 备注: '' }); meetingVisible.value = true }
function editMeeting(row: EthicsMeeting) { Object.assign(meeting, row); meetingVisible.value = true }
async function saveMeetingForm() { if (!meeting.会议序号.trim() || !meeting.会议时间) { ElMessage.warning('会议序号和时间不能为空'); return } meetingSaving.value = true; try { await saveEthicsMeeting(meeting); ElMessage.success('会议排期已保存'); meetingVisible.value = false; await loadMeetings() } catch { /* request interceptor has shown the error */ } finally { meetingSaving.value = false } }
async function viewMeeting(row: EthicsMeeting) { try { meetingDetail.value = await getEthicsMeetingDetail(row.编号); const votes = await getEthicsAnonymousVote(row.编号); meetingVotes.value = votes.表决 || []; meetingDetailVisible.value = true } catch { meetingVotes.value = []; /* request interceptor has shown the error */ } }
function openAgenda(row: EthicsMeeting) { Object.assign(agenda, { 会议编号: row.编号, 申请编号: 0, 排序号: 1, 议题说明: '', 状态: '已排期' }); agendaVisible.value = true }
async function saveAgendaForm() { if (!agenda.会议编号 || !agenda.申请编号) { ElMessage.warning('会议编号和申请编号不能为空'); return } meetingSaving.value = true; try { await saveEthicsAgenda(agenda); ElMessage.success('申请已排入会议议程'); agendaVisible.value = false; await loadMeetings(); if (meetingDetailVisible.value) meetingDetail.value = await getEthicsMeetingDetail(agenda.会议编号) } catch { /* request interceptor has shown the error */ } finally { meetingSaving.value = false } }
function openMinutes(row: EthicsMeeting) { Object.assign(minutes, { 会议编号: row.编号, 纪要正文: '', 出席委员摘要: '', 决议摘要: '', 附件路径: '', 状态: '草稿' }); minutesVisible.value = true }
async function saveMinutesForm() { if (!minutes.纪要正文.trim()) { ElMessage.warning('请填写纪要正文'); return } meetingSaving.value = true; try { await saveEthicsMinutes(minutes); ElMessage.success(minutes.状态 === '待确认' ? '纪要已提交确认' : '纪要草稿已保存'); minutesVisible.value = false; await loadMeetings(); if (meetingDetailVisible.value) meetingDetail.value = await getEthicsMeetingDetail(minutes.会议编号) } catch { /* request interceptor has shown the error */ } finally { meetingSaving.value = false } }
async function confirmMinutes(row: EthicsMeeting) { try { await ElMessageBox.confirm('确认后纪要不能覆盖，是否继续？', '确认会议纪要', { type: 'warning' }); await confirmEthicsMinutes(row.编号); ElMessage.success('会议纪要已确认'); await loadMeetings(); if (meetingDetailVisible.value) meetingDetail.value = await getEthicsMeetingDetail(row.编号) } catch { /* cancel or request error */ } }
function openArchive(row: EthicsMeeting) { Object.assign(archive, { 申请编号: undefined, 会议编号: row.编号, 决定编号: undefined, 档案类型: row.纪要状态 === '已确认' ? '会议纪要' : '会议议程', 文件名称: '', 文件路径: '', 文件校验值: '', 备注: '' }); archiveVisible.value = true }
async function saveArchiveForm() {
  if (!archive.文件路径.trim()) { ElMessage.warning('请填写文件路径'); return }
  if (!/^[0-9a-fA-F]{64}$/.test(String(archive.文件校验值 || '').trim())) { ElMessage.warning('正式伦理档案必须填写64位十六进制SHA-256校验值'); return }
  if (/(^|[/\\])(demo|sample|模拟|样例)([/\\]|$)/i.test(archive.文件路径.trim())) { ElMessage.warning('不能归档示例或演示文件路径'); return }
  meetingSaving.value = true
  try { const result = await archiveEthicsMaterial(archive); archiveVerification.value = result; ElMessage.success(`材料已归档，实际校验状态：${result.实际校验状态 || '已核验'}`); archiveVisible.value = false; if (meetingDetailVisible.value) meetingDetail.value = await getEthicsMeetingDetail(archive.会议编号) } catch { /* request interceptor has shown the error */ } finally { meetingSaving.value = false }
}
function formatDate(value?: string) { return value ? String(value).slice(0, 10) : '-' }
function formatDateTime(value?: string) { return value ? String(value).replace('T', ' ').slice(0, 16) : '-' }
function riskType(value?: string) { return value === '高风险' ? 'danger' : value === '低风险' ? 'success' : 'warning' }
function statusType(value?: string) { return value === '已批准' ? 'success' : value === '已拒绝' || value === '已撤回' ? 'danger' : value === '需补正' ? 'warning' : 'info' }
function decisionType(value?: string) { return value === '批准' || value === '修改后批准' ? 'success' : value === '拒绝' || value === '终止' ? 'danger' : 'warning' }
onMounted(refresh)
</script>

<style scoped lang="scss">
.ethics-page { padding: 18px; background: #f5f7fa; min-height: 100%; }
.header-card, .main-card { margin-bottom: 16px; border: 1px solid #e5e7eb; }
.page-header { display: flex; justify-content: space-between; gap: 16px; align-items: flex-start; margin-bottom: 16px; }
h2 { margin: 0 0 8px; color: #1f2937; display: flex; align-items: center; gap: 8px; font-size: 21px; }
.page-header p { margin: 0; color: #64748b; font-size: 13px; }
.header-actions { white-space: nowrap; }
.workflow-legend { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; margin-top: 12px; padding-top: 12px; border-top: 1px solid #eef2f7; font-size: 12px; }
.legend-label { color: #475569; font-weight: 600; }
.dialog-alert { margin: 0 0 14px; }
.toolbar { display: flex; justify-content: space-between; margin-bottom: 10px; }
.muted { color: #94a3b8; }
.reminder-toolbar { display: flex; align-items: center; gap: 10px; color: #64748b; }
@media (max-width: 800px) { .page-header { flex-direction: column; } .header-actions { width: 100%; } .toolbar :deep(.el-form) { display: flex; flex-wrap: wrap; } }
</style>
