<template>
  <div class="online-research-services">
    <el-card shadow="never" class="header-card">
      <div class="page-header">
        <div>
          <div class="title-line"><el-icon class="title-icon"><Service /></el-icon><h2>网上科研服务</h2><el-tag type="success" effect="plain">线上办理</el-tag></div>
          <p>从服务目录发起申请，在线查看办理人员、材料模板、咨询电话和办理时限，随时跟踪待办进度。</p>
        </div>
        <el-button :icon="Refresh" :loading="loading" @click="refresh">刷新数据</el-button>
      </div>
    </el-card>

    <el-card shadow="never" class="content-card">
      <el-tabs v-model="tab" @tab-change="onTabChange">
        <el-tab-pane label="服务工作台" name="workbench">
          <div class="metric-grid">
            <div class="metric-card blue"><div class="metric-icon"><el-icon><List /></el-icon></div><div><span>可申请服务</span><strong>{{ serviceMetrics.available }}</strong><small>项已启用</small></div></div>
            <div class="metric-card orange"><div class="metric-icon"><el-icon><Clock /></el-icon></div><div><span>我的待办</span><strong>{{ serviceMetrics.todo }}</strong><small>项需要跟进</small></div></div>
            <div class="metric-card green"><div class="metric-icon"><el-icon><Service /></el-icon></div><div><span>办理中申请</span><strong>{{ serviceMetrics.processing }}</strong><small>项正在处理</small></div></div>
            <div class="metric-card purple"><div class="metric-icon"><el-icon><Star /></el-icon></div><div><span>待评价服务</span><strong>{{ serviceMetrics.toRate }}</strong><small>完成后可评价</small></div></div>
          </div>

          <div class="workbench-grid">
            <el-card shadow="never" class="inner-card">
              <template #header><div class="card-header"><span>最近服务申请</span><el-button link type="primary" @click="tab = 'applications'">查看全部 <el-icon><ArrowRight /></el-icon></el-button></div></template>
              <el-table :data="recentApplications" stripe size="small" empty-text="暂无服务申请">
                <el-table-column prop="服务名称" label="服务事项" min-width="190" show-overflow-tooltip />
                <el-table-column prop="状态" label="状态" width="90"><template #default="{ row }"><el-tag size="small" :type="statusType(row.状态)">{{ row.状态 || '待受理' }}</el-tag></template></el-table-column>
                <el-table-column prop="办理期限" label="办理期限" width="160" show-overflow-tooltip />
                <el-table-column label="操作" width="100" align="right"><template #default="{ row }"><el-button v-if="row.状态 === '已完成'" link type="primary" size="small" @click="openRate(row)">评价</el-button><el-button v-else link type="warning" size="small" @click="openReminder(row)">催办</el-button></template></el-table-column>
              </el-table>
            </el-card>
            <el-card shadow="never" class="inner-card shortcut-card">
              <template #header><div class="card-header"><span>快捷办理</span><el-button link type="primary" @click="tab = 'services'">服务目录</el-button></div></template>
              <div class="shortcut-list">
                <button v-for="service in services.filter(item => item.是否启用 !== false).slice(0, 5)" :key="service.编号 || service.服务编码" type="button" class="shortcut-item" @click="openApply(service)"><span class="shortcut-name">{{ service.服务名称 }}</span><span class="shortcut-meta">{{ service.办理时限天数 || '-' }} 个工作日 <el-icon><ArrowRight /></el-icon></span></button>
                <el-empty v-if="!services.length" :image-size="56" description="暂无可申请服务" />
              </div>
            </el-card>
          </div>
          <div class="workbench-hint"><el-icon><Bell /></el-icon><span>办理期限临近或材料需要补充时，可在“我的申请”中直接催办；服务完成后可提交 1—5 星满意度评价。</span><el-button link type="primary" @click="tab = 'todo'">进入我的待办</el-button></div>
        </el-tab-pane>

        <el-tab-pane label="服务事项目录" name="services">
          <div class="toolbar"><el-input v-model="serviceKeyword" clearable :prefix-icon="Search" placeholder="搜索服务名称、编码或分类" style="max-width: 330px" /><el-button type="primary" :icon="Refresh" @click="loadServices">查询</el-button><el-button v-if="canManage" type="success" :icon="Plus" @click="openServiceEditor()">新增服务事项</el-button><span class="toolbar-tip">共 {{ filteredServices.length }} 项可申请服务</span></div>
          <el-table v-loading="loadingServices" :data="filteredServices" border stripe>
            <el-table-column prop="服务编码" label="编码" width="125" /><el-table-column prop="服务名称" label="服务事项" min-width="190" /><el-table-column prop="服务分类" label="分类" width="125" />
            <el-table-column label="办理人员" min-width="140"><template #default="{ row }">{{ row.办理人员姓名 || row.办理人员 || row.办理角色 || '待配置' }}</template></el-table-column>
            <el-table-column label="咨询电话" width="145"><template #default="{ row }"><a v-if="row.咨询电话" class="phone-link" :href="'tel:' + row.咨询电话"><el-icon><Phone /></el-icon>{{ row.咨询电话 }}</a><span v-else class="muted">未提供</span></template></el-table-column>
            <el-table-column v-if="canManage" label="状态" width="85" align="center"><template #default="{ row }"><el-tag size="small" :type="row.是否启用 === false ? 'info' : 'success'">{{ row.是否启用 === false ? '已停用' : '启用' }}</el-tag></template></el-table-column>
            <el-table-column label="办理时限" width="105" align="center"><template #default="{ row }"><el-tag size="small" type="warning">{{ row.办理时限天数 || '-' }} 天</el-tag></template></el-table-column>
            <el-table-column label="材料模板" width="115" align="center"><template #default="{ row }"><el-button v-if="row.所需材料模板路径" link type="primary" @click="openMaterial(row.所需材料模板路径)"><el-icon><Document /></el-icon>查看模板</el-button><span v-else class="muted">未提供</span></template></el-table-column>
            <el-table-column label="操作" width="210" fixed="right" align="center"><template #default="{ row }"><el-button link type="info" @click="openServiceDetail(row)">查看详情</el-button><el-button v-if="canManage" link type="warning" @click="openServiceEditor(row)">编辑</el-button><el-button link type="primary" :disabled="row.是否启用 === false" @click="openApply(row)">{{ row.是否启用 === false ? '已停用' : '发起申请' }}</el-button></template></el-table-column>
          </el-table>
          <el-empty v-if="!loadingServices && !filteredServices.length" description="暂无已启用服务事项，请联系科研管理人员配置目录" />
        </el-tab-pane>

        <el-tab-pane label="我的申请" name="applications">
          <div class="notice">申请提交后，系统会按服务事项的办理时限生成截止时间；办理人员和处理意见以服务端记录为准。</div>
          <el-table v-loading="loadingApplications" :data="applications" border stripe>
            <el-table-column prop="编号" label="编号" width="75" /><el-table-column prop="服务名称" label="服务事项" min-width="190" show-overflow-tooltip /><el-table-column prop="服务分类" label="分类" width="120" />
            <el-table-column prop="状态" label="状态" width="95"><template #default="{ row }"><el-tag :type="statusType(row.状态)">{{ row.状态 || '待受理' }}</el-tag></template></el-table-column>
            <el-table-column label="办理人员" min-width="145" show-overflow-tooltip><template #default="{ row }">{{ row.当前处理人姓名 || row.当前处理角色 || row.办理人员姓名 || row.办理角色 || '待分配' }}</template></el-table-column>
            <el-table-column prop="办理期限" label="办理期限" width="165" show-overflow-tooltip><template #default="{ row }"><span :class="{ overdue: isOverdue(row) }">{{ row.办理期限 || '-' }}</span></template></el-table-column>
            <el-table-column prop="处理意见" label="处理意见" min-width="190" show-overflow-tooltip />
            <el-table-column label="操作" width="185" fixed="right" align="center"><template #default="{ row }"><el-button v-if="!isTerminal(row)" link type="warning" @click="openReminder(row)">催办</el-button><el-button v-if="row.状态 === '已完成'" link type="primary" @click="openRate(row)">{{ row.评价星级 ? '修改评价' : '满意度评价' }}</el-button><span v-if="isTerminal(row) && row.状态 !== '已完成'" class="muted">已结束</span></template></el-table-column>
          </el-table>
          <el-empty v-if="!loadingApplications && !applications.length" description="暂无服务申请，可前往服务事项目录发起申请" />
        </el-tab-pane>

        <el-tab-pane label="我的待办" name="todo">
          <div class="todo-header"><div class="notice"><el-icon><Clock /></el-icon>{{ canManage ? '以下为当前账号可处理的科研服务申请。' : '以下为尚未完成的服务申请，请关注办理期限和处理意见。' }}</div><el-button link type="primary" :icon="Refresh" @click="loadTodo">刷新待办</el-button></div>
          <el-table v-loading="loadingPending" :data="todoRows" border stripe>
            <el-table-column prop="编号" label="编号" width="75" /><el-table-column prop="服务名称" label="服务事项" min-width="190" show-overflow-tooltip /><el-table-column v-if="canManage" prop="申请人姓名" label="申请人" width="105" />
            <el-table-column prop="状态" label="状态" width="95"><template #default="{ row }"><el-tag :type="statusType(row.状态)">{{ row.状态 || '待受理' }}</el-tag></template></el-table-column>
            <el-table-column label="办理人员" min-width="140" show-overflow-tooltip><template #default="{ row }">{{ row.当前处理人姓名 || row.当前处理角色 || row.办理人员姓名 || row.办理角色 || '待分配' }}</template></el-table-column>
            <el-table-column prop="办理期限" label="办理期限" width="165" show-overflow-tooltip><template #default="{ row }"><span :class="{ overdue: isOverdue(row) }">{{ row.办理期限 || '-' }}</span></template></el-table-column>
            <el-table-column prop="申请说明" label="申请说明" min-width="210" show-overflow-tooltip />
            <el-table-column label="操作" width="220" fixed="right" align="center"><template #default="{ row }"><template v-if="canManage"><el-button size="small" type="primary" @click="process(row, '处理中')">受理</el-button><el-button size="small" type="success" @click="process(row, '已完成')">完成</el-button><el-button size="small" type="danger" link @click="process(row, '退回')">退回</el-button></template><el-button v-else link type="warning" @click="openReminder(row)">催办</el-button></template></el-table-column>
          </el-table>
          <el-empty v-if="!loadingPending && !todoRows.length" description="暂无待办事项" />
        </el-tab-pane>

        <el-tab-pane label="科研平台" name="platforms">
          <div class="toolbar"><span class="muted">平台基础信息、负责人和开放状态可由管理角色维护。</span><el-button type="primary" @click="openPlatform()">新增平台</el-button></div>
          <el-table v-loading="loadingPlatforms" :data="platforms" border stripe><el-table-column prop="平台名称" label="平台名称" min-width="200" /><el-table-column prop="平台类型" label="类型" width="130" /><el-table-column prop="依托部门名称" label="依托部门" width="150" /><el-table-column prop="负责人姓名" label="负责人" width="110" /><el-table-column prop="开放状态" label="开放状态" width="100" /><el-table-column prop="成员数" label="成员数" width="90" /><el-table-column prop="联系方式" label="联系方式" min-width="160" /><el-table-column label="操作" width="90" fixed="right"><template #default="{ row }"><el-button link type="primary" @click="openPlatform(row)">编辑</el-button></template></el-table-column></el-table>
          <el-empty v-if="!loadingPlatforms && !platforms.length" description="暂无科研平台资料" />
        </el-tab-pane>
        <el-tab-pane label="导入导出日志" name="logs"><el-table v-loading="loadingLogs" :data="logs" border stripe><el-table-column prop="创建时间" label="时间" width="180" /><el-table-column prop="模块" label="模块" width="160" /><el-table-column prop="操作类型" label="类型" width="90" /><el-table-column prop="文件名称" label="文件" min-width="220" /><el-table-column prop="记录数" label="记录数" width="90" /><el-table-column prop="成功数" label="成功" width="80" /><el-table-column prop="失败数" label="失败" width="80" /><el-table-column prop="状态" label="状态" width="100" /><el-table-column prop="操作人姓名" label="操作人" width="110" /></el-table><el-empty v-if="!loadingLogs && !logs.length" description="暂无导入导出操作日志或当前账号无权限" /></el-tab-pane>
      </el-tabs>
    </el-card>

    <el-dialog v-model="serviceEditor.visible" :title="serviceEditor.form.编号 ? '编辑服务事项' : '新增服务事项'" width="700px" destroy-on-close>
      <el-form ref="serviceFormRef" :model="serviceEditor.form" :rules="serviceRules" label-width="115px">
        <el-row :gutter="14">
          <el-col :span="12"><el-form-item label="服务编码" prop="服务编码"><el-input v-model="serviceEditor.form.服务编码" maxlength="80" placeholder="例如：RS-ETHICS-001" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="服务名称" prop="服务名称"><el-input v-model="serviceEditor.form.服务名称" maxlength="200" placeholder="例如：科研伦理审查咨询" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="服务分类"><el-input v-model="serviceEditor.form.服务分类" maxlength="80" placeholder="科研公共服务" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="办理时限（天）" prop="办理时限天数"><el-input-number v-model="serviceEditor.form.办理时限天数" :min="1" :max="365" controls-position="right" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="办理人员"><div class="handler-picker"><el-tag v-if="serviceEditor.form.办理人员编号" closable @close="clearHandler">{{ serviceEditor.form.办理人员姓名 || '已选择人员 #' + serviceEditor.form.办理人员编号 }}</el-tag><el-button type="primary" plain @click="handlerPickerVisible = true">从人员库选择</el-button></div><div class="field-tip">办理人员来自系统人员库，申请提交后自动进入该人员的待办。</div></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="办理角色"><el-input v-model="serviceEditor.form.办理角色" maxlength="100" placeholder="可选：科室或岗位角色" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="咨询电话" prop="咨询电话"><el-input v-model="serviceEditor.form.咨询电话" maxlength="100" placeholder="座机或手机号码，可留空" /></el-form-item></el-col>
          <el-col :span="24"><el-form-item label="材料模板"><div class="template-uploader"><el-upload :show-file-list="false" :auto-upload="false" accept=".pdf,.doc,.docx,.xls,.xlsx" :on-change="handleServiceTemplateSelected" :disabled="templateUploading"><el-button type="primary" plain :loading="templateUploading"><el-icon><Upload /></el-icon>{{ templateUploading ? '正在上传…' : '选择并上传文件' }}</el-button></el-upload><div v-if="serviceEditor.form.所需材料模板路径" class="template-file"><el-icon><Document /></el-icon><span :title="serviceEditor.form.所需材料模板路径">{{ serviceEditor.form.材料模板文件名 || templateName(serviceEditor.form.所需材料模板路径) }}</span><el-button link type="danger" @click="clearServiceTemplate">移除</el-button></div><div class="field-tip">支持 PDF、Word、Excel 文件，上传后自动生成模板地址；请保存服务事项完成绑定。</div></div></el-form-item></el-col>
          <el-col :span="24"><el-form-item label="服务说明"><el-input v-model="serviceEditor.form.服务说明" type="textarea" :rows="4" maxlength="3000" show-word-limit placeholder="说明办理范围、申请条件和材料要求" /></el-form-item></el-col>
          <el-col :span="24"><el-form-item label="启用服务"><el-switch v-model="serviceEditor.form.是否启用" active-text="对外开放" inactive-text="暂停申请" /></el-form-item></el-col>
        </el-row>
      </el-form>
      <div class="editor-tip">保存后，服务事项会立即出现在申请人的服务目录；停用只会阻止新申请，不影响已提交申请的跟踪。</div>
      <template #footer><el-button @click="serviceEditor.visible = false">取消</el-button><el-button type="primary" :loading="saving" :disabled="templateUploading" @click="saveService">保存服务事项</el-button></template>
    </el-dialog>

    <UserSelectDialog v-model="handlerPickerVisible" title="选择服务办理人员" :multiple="false" :initial-user-ids="serviceEditor.form.办理人员编号 ? [serviceEditor.form.办理人员编号] : []" @confirm="onHandlerSelected" />

    <el-dialog v-model="serviceDetail.visible" :title="serviceDetail.form.服务名称 || '服务事项详情'" width="680px" destroy-on-close>
      <el-descriptions :column="2" border><el-descriptions-item label="服务编码">{{ serviceDetail.form.服务编码 || '-' }}</el-descriptions-item><el-descriptions-item label="服务分类">{{ serviceDetail.form.服务分类 || '-' }}</el-descriptions-item><el-descriptions-item label="办理人员">{{ serviceDetail.form.办理人员姓名 || serviceDetail.form.办理人员 || serviceDetail.form.办理角色 || '待配置' }}</el-descriptions-item><el-descriptions-item label="咨询电话"><a v-if="serviceDetail.form.咨询电话" class="phone-link" :href="'tel:' + serviceDetail.form.咨询电话">{{ serviceDetail.form.咨询电话 }}</a><span v-else>-</span></el-descriptions-item><el-descriptions-item label="办理时限">{{ serviceDetail.form.办理时限天数 || '-' }} 天</el-descriptions-item><el-descriptions-item label="材料模板"><el-button v-if="serviceDetail.form.所需材料模板路径" link type="primary" @click="openMaterial(serviceDetail.form.所需材料模板路径)">查看/下载材料模板</el-button><span v-else>暂未提供</span></el-descriptions-item><el-descriptions-item label="服务说明" :span="2">{{ serviceDetail.form.服务说明 || '暂无说明' }}</el-descriptions-item></el-descriptions>
      <template #footer><el-button @click="serviceDetail.visible = false">关闭</el-button><el-button type="primary" @click="openApply(serviceDetail.form); serviceDetail.visible = false">发起申请</el-button></template>
    </el-dialog>

    <el-dialog v-model="applyDialog.visible" title="发起科研服务申请" width="620px" destroy-on-close>
      <el-form :model="applyDialog.form" label-width="105px"><el-form-item label="服务事项"><el-input :model-value="applyDialog.service?.服务名称 || ''" disabled /></el-form-item><el-form-item label="办理信息"><span class="apply-meta">{{ applyDialog.service?.办理人员姓名 || applyDialog.service?.办理人员 || applyDialog.service?.办理角色 || '待配置' }} · {{ applyDialog.service?.办理时限天数 || '-' }} 天</span></el-form-item><el-form-item label="材料模板"><el-button v-if="applyDialog.service?.所需材料模板路径" link type="primary" @click="openMaterial(applyDialog.service?.所需材料模板路径)">查看材料模板</el-button><span v-else class="muted">暂无模板，请按服务说明准备材料</span></el-form-item><el-form-item label="关联业务类型"><el-input v-model="applyDialog.form.关联业务类型" placeholder="例如：纵向项目、伦理审查" /></el-form-item><el-form-item label="关联业务编号"><el-input-number v-model="applyDialog.form.关联业务编号" :min="1" :controls="false" /></el-form-item><el-form-item label="附件路径"><el-input v-model="applyDialog.form.附件路径" placeholder="已有附件地址可填写，多个地址请按系统约定提交" /></el-form-item><el-form-item label="申请说明"><el-input v-model="applyDialog.form.申请说明" type="textarea" :rows="4" placeholder="说明服务目的、时间要求和需要准备的材料" /></el-form-item></el-form>
      <template #footer><el-button @click="applyDialog.visible = false">取消</el-button><el-button type="primary" :loading="saving" @click="submitApply">提交申请</el-button></template>
    </el-dialog>

    <el-dialog v-model="reminderDialog.visible" title="催办科研服务申请" width="520px" destroy-on-close><el-alert title="催办会记录在服务申请的催办日志，并按已配置的消息通道发送提醒。" type="info" :closable="false" show-icon /><el-form :model="reminderDialog.form" label-width="90px" class="dialog-form"><el-form-item label="服务事项"><el-input :model-value="reminderDialog.serviceName" disabled /></el-form-item><el-form-item label="催办原因"><el-input v-model="reminderDialog.form.催办原因" type="textarea" :rows="4" maxlength="500" show-word-limit placeholder="可填写截止日期、临床进度或材料补充情况" /></el-form-item></el-form><template #footer><el-button @click="reminderDialog.visible = false">取消</el-button><el-button type="warning" :loading="saving" @click="submitReminder">发送催办</el-button></template></el-dialog>

    <el-dialog v-model="rateDialog.visible" title="评价科研服务" width="520px" destroy-on-close><el-form :model="rateDialog.form" label-width="90px"><el-form-item label="服务事项"><el-input :model-value="rateDialog.serviceName" disabled /></el-form-item><el-form-item label="满意度"><el-rate v-model="rateDialog.form.评价星级" show-text /></el-form-item><el-form-item label="反馈"><el-input v-model="rateDialog.form.反馈内容" type="textarea" :rows="3" maxlength="1000" show-word-limit placeholder="请留下对办理效率、材料指引或服务态度的建议" /></el-form-item></el-form><template #footer><el-button @click="rateDialog.visible = false">取消</el-button><el-button type="primary" @click="submitRate">提交评价</el-button></template></el-dialog>

    <el-dialog v-model="platformDialog.visible" :title="platformDialog.form.编号 ? '编辑科研平台' : '新增科研平台'" width="650px"><el-form :model="platformDialog.form" label-width="105px"><el-form-item label="平台名称"><el-input v-model="platformDialog.form.平台名称" /></el-form-item><el-form-item label="平台类型"><el-input v-model="platformDialog.form.平台类型" /></el-form-item><el-form-item label="开放状态"><el-select v-model="platformDialog.form.开放状态"><el-option v-for="item in ['开放','预约','暂停','关闭']" :key="item" :label="item" :value="item" /></el-select></el-form-item><el-form-item label="负责人编号"><el-input-number v-model="platformDialog.form.负责人编号" :min="1" :controls="false" /></el-form-item><el-form-item label="依托部门编号"><el-input-number v-model="platformDialog.form.依托部门编号" :min="1" :controls="false" /></el-form-item><el-form-item label="联系方式"><el-input v-model="platformDialog.form.联系方式" /></el-form-item><el-form-item label="平台简介"><el-input v-model="platformDialog.form.平台简介" type="textarea" :rows="3" /></el-form-item><el-form-item label="启用"><el-switch v-model="platformDialog.form.是否启用" /></el-form-item></el-form><template #footer><el-button @click="platformDialog.visible = false">取消</el-button><el-button type="primary" :loading="saving" @click="savePlatform">保存</el-button></template></el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, type FormInstance, type UploadFile } from 'element-plus'
import { useRoute } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { Refresh, Service, List, Clock, Star, ArrowRight, Bell, Search, Phone, Document, Plus, Upload } from '@element-plus/icons-vue'
import { createResearchServiceApplication, listMyResearchServiceApplications, listPendingResearchServices, listResearchImportExportLogs, listResearchPlatforms, listResearchServices, processResearchServiceApplication, rateResearchService, saveResearchPlatform, saveResearchService, uploadResearchServiceTemplate, type ResearchPlatform, type ResearchService, type ResearchServiceApplication } from '@/api/research-platform'
import { remindResearchService } from '@/api/document-messaging'
import UserSelectDialog from '@/components/UserSelectDialog.vue'
import type { PersonItem } from '@/api/organization'

const route = useRoute()
const userStore = useUserStore()
const initialTab = String(route.query.tab || 'workbench')
const tab = ref(['workbench', 'services', 'applications', 'todo', 'platforms', 'logs'].includes(initialTab) ? initialTab : initialTab === 'instruments' ? 'platforms' : 'workbench')
const loading = ref(false)
const saving = ref(false)
const loadingServices = ref(false)
const loadingApplications = ref(false)
const loadingPending = ref(false)
const loadingPlatforms = ref(false)
const loadingLogs = ref(false)
const services = ref<ResearchService[]>([])
const applications = ref<ResearchServiceApplication[]>([])
const pending = ref<ResearchServiceApplication[]>([])
const platforms = ref<ResearchPlatform[]>([])
const logs = ref<any[]>([])
const serviceKeyword = ref('')
const applyDialog = reactive<{ visible: boolean; service?: ResearchService; form: any }>({ visible: false, form: {} })
const serviceFormRef = ref<FormInstance>()
const handlerPickerVisible = ref(false)
const templateUploading = ref(false)
const serviceEditor = reactive<{ visible: boolean; form: ResearchService }>({ visible: false, form: { 服务编码: '', 服务名称: '', 服务分类: '科研公共服务', 服务说明: '', 所需材料模板路径: '', 材料模板文件名: '', 办理人员编号: undefined, 办理人员姓名: '', 办理角色: '', 办理时限天数: 5, 咨询电话: '', 是否启用: true } })
const serviceDetail = reactive<{ visible: boolean; form: ResearchService }>({ visible: false, form: { 服务编码: '', 服务名称: '' } })
const reminderDialog = reactive<{ visible: boolean; serviceName: string; form: { 服务申请编号: number; 催办原因: string } }>({ visible: false, serviceName: '', form: { 服务申请编号: 0, 催办原因: '' } })
const rateDialog = reactive<{ visible: boolean; serviceName: string; form: any }>({ visible: false, serviceName: '', form: { 服务申请编号: 0, 评价星级: 5, 反馈内容: '', 是否公开: false } })
const platformDialog = reactive<{ visible: boolean; form: ResearchPlatform }>({ visible: false, form: { 平台名称: '', 平台类型: '科研平台', 开放状态: '开放', 是否启用: true } })

function unwrap<T>(value: any): T[] { return Array.isArray(value) ? value : value?.items || value?.list || value?.List || [] }
const roleText = computed(() => String(userStore.userInfo?.角色名称 || ''))
const canManage = computed(() => ['系统管理员', '科研管理员', '科技办'].some(role => roleText.value.includes(role)))
const filteredServices = computed(() => {
  const key = serviceKeyword.value.trim().toLowerCase()
  if (!key) return services.value
  return services.value.filter(service => [service.服务编码, service.服务名称, service.服务分类, service.办理人员姓名, service.办理角色].filter(Boolean).join(' ').toLowerCase().includes(key))
})
const todoRows = computed(() => canManage.value ? pending.value : applications.value.filter(row => !isTerminal(row)))
const recentApplications = computed(() => applications.value.slice(0, 5))
const serviceMetrics = computed(() => ({ available: services.value.filter(service => service.是否启用 !== false).length, todo: todoRows.value.length, processing: applications.value.filter(row => row.状态 === '处理中' || row.状态 === '待受理').length, toRate: applications.value.filter(row => row.状态 === '已完成' && !row.评价星级).length }))
const serviceRules = { 服务编码: [{ required: true, message: '请输入服务编码', trigger: 'blur' }], 服务名称: [{ required: true, message: '请输入服务名称', trigger: 'blur' }], 办理时限天数: [{ required: true, type: 'number', min: 1, max: 365, message: '办理时限应为 1—365 天', trigger: 'change' }], 咨询电话: [{ pattern: /^[0-9+()\-\s]{6,30}$/, message: '请输入有效的咨询电话', trigger: 'blur' }] }

function statusType(value?: string) { return value === '已完成' ? 'success' : value === '退回' || value === '已取消' ? 'danger' : value === '处理中' ? 'warning' : 'info' }
function isTerminal(row: ResearchServiceApplication) { return row.状态 === '已完成' || row.状态 === '已取消' }
function isOverdue(row: ResearchServiceApplication) { return Boolean(row.办理期限 && !isTerminal(row) && new Date(row.办理期限).getTime() < Date.now()) }
function openMaterial(path?: string) { if (!path) return; const value = path.trim(); if (!/^https?:\/\//i.test(value) && !value.startsWith('/')) { ElMessage.warning('材料模板地址格式不受支持'); return }; window.open(value, '_blank', 'noopener,noreferrer') }
function openServiceDetail(service: ResearchService) { serviceDetail.form = { ...service }; serviceDetail.visible = true }
function openServiceEditor(service?: ResearchService) { serviceEditor.form = service ? { ...service } : { 服务编码: '', 服务名称: '', 服务分类: '科研公共服务', 服务说明: '', 所需材料模板路径: '', 材料模板文件名: '', 办理人员编号: undefined, 办理人员姓名: '', 办理角色: '', 办理时限天数: 5, 咨询电话: '', 是否启用: true }; serviceEditor.visible = true; void serviceFormRef.value?.clearValidate() }
function onHandlerSelected(users: PersonItem[]) { const person = users[0]; serviceEditor.form.办理人员编号 = person?.编号; serviceEditor.form.办理人员姓名 = person?.姓名 || ''; handlerPickerVisible.value = false }
function clearHandler() { serviceEditor.form.办理人员编号 = undefined; serviceEditor.form.办理人员姓名 = '' }
function templateName(path?: string) { if (!path) return ''; const value = path.split('?')[0].split('/').pop() || ''; return value || '已上传材料模板' }
function clearServiceTemplate() { serviceEditor.form.所需材料模板路径 = ''; serviceEditor.form.材料模板文件名 = '' }
async function handleServiceTemplateSelected(file: UploadFile) { const rawFile = file.raw; if (!rawFile) return; if (!/\.(pdf|docx?|xlsx?)$/i.test(rawFile.name)) { ElMessage.warning('仅支持 PDF、Word、Excel 文件'); return }; templateUploading.value = true; try { const result = await uploadResearchServiceTemplate(rawFile); const path = Array.isArray(result) ? result[0] : ''; if (!path) throw new Error('上传接口未返回文件地址'); serviceEditor.form.所需材料模板路径 = path; serviceEditor.form.材料模板文件名 = rawFile.name; ElMessage.success('材料模板上传成功，请保存服务事项') } catch (error) { console.error(error); ElMessage.error('材料模板上传失败，请重试') } finally { templateUploading.value = false } }
function openApply(service: ResearchService) { applyDialog.service = service; applyDialog.form = { 服务事项编号: service.编号, 关联业务类型: '', 关联业务编号: undefined, 附件路径: '', 申请说明: '' }; applyDialog.visible = true }
function openReminder(row: ResearchServiceApplication) { reminderDialog.serviceName = row.服务名称 || ''; reminderDialog.form = { 服务申请编号: row.编号 || 0, 催办原因: '' }; reminderDialog.visible = true }
function openRate(row: ResearchServiceApplication) { rateDialog.serviceName = row.服务名称 || ''; rateDialog.form = { 服务申请编号: row.编号, 评价星级: row.评价星级 || 5, 反馈内容: row.反馈内容 || '', 是否公开: false }; rateDialog.visible = true }

async function loadServices() { loadingServices.value = true; try { services.value = unwrap<ResearchService>(await listResearchServices({ 仅启用: canManage.value ? undefined : true })) } catch { services.value = [] } finally { loadingServices.value = false } }
async function loadApplications() { loadingApplications.value = true; try { applications.value = unwrap<ResearchServiceApplication>(await listMyResearchServiceApplications()) } catch { applications.value = [] } finally { loadingApplications.value = false } }
async function loadTodo() { if (!canManage.value) return; loadingPending.value = true; try { pending.value = unwrap<ResearchServiceApplication>(await listPendingResearchServices()) } catch { pending.value = [] } finally { loadingPending.value = false } }
async function loadPlatforms() { loadingPlatforms.value = true; try { platforms.value = unwrap<ResearchPlatform>(await listResearchPlatforms({ 仅启用: true })) } catch { platforms.value = [] } finally { loadingPlatforms.value = false } }
async function loadLogs() { loadingLogs.value = true; try { logs.value = unwrap<any>(await listResearchImportExportLogs({ 数量: 100 })) } catch { logs.value = [] } finally { loadingLogs.value = false } }
async function refresh() { loading.value = true; await Promise.allSettled([loadServices(), loadApplications(), loadTodo(), loadPlatforms(), loadLogs()]); loading.value = false }
async function onTabChange(name: string | number) { const next = String(name); if (next === 'todo') await loadTodo(); if (next === 'platforms') await loadPlatforms(); if (next === 'logs') await loadLogs() }

async function submitApply() { if (!applyDialog.form.服务事项编号) return; saving.value = true; try { await createResearchServiceApplication(applyDialog.form); ElMessage.success('服务申请已提交，请在“我的申请”跟踪进度'); applyDialog.visible = false; await loadApplications() } finally { saving.value = false } }
async function saveService() { if (!canManage.value || templateUploading.value) return; const valid = await serviceFormRef.value?.validate().catch(() => false); if (!valid) return; saving.value = true; try { const payload = { ...serviceEditor.form } as ResearchService & Record<string, unknown>; delete payload.材料模板文件名; delete payload.办理人员姓名; await saveResearchService(payload); ElMessage.success(serviceEditor.form.编号 ? '服务事项已更新' : '服务事项已新增'); serviceEditor.visible = false; await loadServices() } finally { saving.value = false } }
async function submitReminder() { if (!reminderDialog.form.服务申请编号) return; saving.value = true; try { await remindResearchService(reminderDialog.form); ElMessage.success('催办已记录并进入消息队列'); reminderDialog.visible = false } finally { saving.value = false } }
async function submitRate() { if (!rateDialog.form.服务申请编号) return; await rateResearchService(rateDialog.form); ElMessage.success('满意度评价已提交'); rateDialog.visible = false; await loadApplications() }
async function process(row: ResearchServiceApplication, 状态: string) { if (!row.编号) return; await processResearchServiceApplication({ 编号: row.编号, 状态, 处理意见: 状态 === '退回' ? '请补充材料后重新提交' : undefined }); ElMessage.success('申请已' + (状态 === '处理中' ? '受理' : 状态)); await Promise.all([loadTodo(), loadApplications()]) }
function openPlatform(row?: ResearchPlatform) { platformDialog.form = row ? { ...row } : { 平台名称: '', 平台类型: '科研平台', 开放状态: '开放', 是否启用: true }; platformDialog.visible = true }
async function savePlatform() { if (!platformDialog.form.平台名称?.trim()) { ElMessage.warning('请输入平台名称'); return }; saving.value = true; try { await saveResearchPlatform(platformDialog.form); ElMessage.success('科研平台已保存'); platformDialog.visible = false; await loadPlatforms() } finally { saving.value = false } }

onMounted(refresh)
</script>

<style scoped lang="scss">
.online-research-services { padding: 20px; min-height: 100%; background: #f5f7fa; }
.header-card, .content-card { border: 0; border-radius: 10px; margin-bottom: 16px; }
.page-header { display: flex; justify-content: space-between; align-items: center; gap: 16px; }
.title-line { display: flex; align-items: center; gap: 9px; }.title-icon { color: #409eff; font-size: 25px; }h2 { margin: 0; color: #1f2937; }.page-header p { margin: 9px 0 0; color: #7b8794; }
.metric-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; margin-bottom: 18px; }.metric-card { display: flex; align-items: center; gap: 14px; padding: 18px; border-radius: 9px; color: #27364b; background: #f8fbff; border: 1px solid #e6eef8; }.metric-card.blue { background: #eef6ff; }.metric-card.orange { background: #fff7ed; }.metric-card.green { background: #effaf4; }.metric-card.purple { background: #f7f1ff; }.metric-icon { display: grid; place-items: center; width: 40px; height: 40px; border-radius: 10px; color: #409eff; background: #dceeff; font-size: 21px; }.orange .metric-icon { color: #e6a23c; background: #ffe9c6; }.green .metric-icon { color: #67c23a; background: #dcf6d2; }.purple .metric-icon { color: #8e62d9; background: #eadfff; }.metric-card span, .metric-card small { display: block; color: #718096; font-size: 13px; }.metric-card strong { display: inline-block; margin: 3px 7px 1px 0; font-size: 25px; color: #1f2937; }
.workbench-grid { display: grid; grid-template-columns: minmax(0, 1.35fr) minmax(280px, .85fr); gap: 16px; }.inner-card { border: 1px solid #edf0f5; }.card-header { display: flex; justify-content: space-between; align-items: center; gap: 8px; font-weight: 600; color: #303133; }.shortcut-list { display: flex; flex-direction: column; gap: 2px; }.shortcut-item { display: flex; justify-content: space-between; align-items: center; gap: 10px; padding: 11px 4px; border: 0; border-bottom: 1px solid #f1f3f5; background: transparent; color: #303133; text-align: left; cursor: pointer; }.shortcut-item:hover { color: #409eff; background: #f8fbff; }.shortcut-name { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }.shortcut-meta { display: inline-flex; align-items: center; flex-shrink: 0; color: #8b98a8; font-size: 12px; }.workbench-hint { display: flex; align-items: center; gap: 8px; margin-top: 16px; padding: 11px 14px; border-radius: 6px; color: #52606d; background: #f0f4f8; font-size: 13px; }.workbench-hint .el-button { margin-left: auto; }
.toolbar { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; margin-bottom: 14px; }.toolbar-tip { color: #8b98a8; font-size: 13px; }.notice { display: flex; align-items: center; gap: 7px; padding: 10px 14px; margin-bottom: 14px; border-radius: 6px; color: #52606d; background: #f0f4f8; font-size: 13px; }.todo-header { display: flex; align-items: flex-start; gap: 12px; }.todo-header .notice { flex: 1; }.todo-header > .el-button { margin-top: 7px; }.muted { color: #9aa5b1; font-size: 13px; }.phone-link { display: inline-flex; align-items: center; gap: 3px; color: #409eff; text-decoration: none; }.phone-link:hover { text-decoration: underline; }.overdue { color: #f56c6c; font-weight: 600; }.apply-meta { color: #606266; }.dialog-form { margin-top: 16px; }
.editor-tip { margin: -4px 0 4px 115px; color: #8b98a8; font-size: 12px; line-height: 1.5; }
.handler-picker { display: flex; align-items: center; gap: 8px; min-height: 32px; flex-wrap: wrap; }.template-uploader { display: flex; flex-direction: column; gap: 8px; }.template-file { display: inline-flex; align-items: center; gap: 6px; max-width: 100%; color: #409eff; }.template-file span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }.field-tip { margin-top: 4px; color: #8b98a8; font-size: 12px; line-height: 1.4; }
@media (max-width: 1100px) { .metric-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }.workbench-grid { grid-template-columns: 1fr; } }@media (max-width: 640px) { .online-research-services { padding: 12px; }.page-header { align-items: flex-start; flex-direction: column; }.metric-grid { grid-template-columns: 1fr; }.workbench-hint { align-items: flex-start; flex-wrap: wrap; }.workbench-hint .el-button { margin-left: 0; } }
</style>
