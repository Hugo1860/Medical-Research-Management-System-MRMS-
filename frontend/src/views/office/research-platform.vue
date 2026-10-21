<template>
  <div class="research-resource-page">
    <el-card shadow="never" class="header-card">
      <div class="header"><div><h2>科研平台与服务目录</h2><p>统一查看科研平台资源，按目录发起科研公共服务申请并跟踪办理进度。</p></div><el-button :icon="Refresh" :loading="loading" @click="refresh">刷新</el-button></div>
    </el-card>

    <el-card shadow="never" class="content-card">
      <el-tabs v-model="tab" @tab-change="onTabChange">
        <el-tab-pane label="服务目录" name="services">
          <div class="toolbar"><el-input v-model="serviceKeyword" clearable placeholder="搜索服务名称/编码" style="max-width: 300px" /><el-button type="primary" :icon="Refresh" @click="loadServices">查询</el-button></div>
          <el-table v-loading="loadingServices" :data="filteredServices" border stripe>
            <el-table-column prop="服务编码" label="编码" width="150" />
            <el-table-column prop="服务名称" label="服务名称" min-width="200" />
            <el-table-column prop="服务分类" label="分类" width="140" />
            <el-table-column prop="办理角色" label="办理角色" width="130" />
            <el-table-column label="办理时限" width="110"><template #default="{ row }">{{ row.办理时限天数 || '-' }} 天</template></el-table-column>
            <el-table-column prop="服务说明" label="说明" min-width="260" show-overflow-tooltip />
            <el-table-column label="操作" width="110" fixed="right"><template #default="{ row }"><el-button type="primary" link @click="openApply(row)">发起申请</el-button></template></el-table-column>
          </el-table>
          <el-empty v-if="!loadingServices && !filteredServices.length" description="暂无已启用服务事项，请由科研管理角色配置目录" />
        </el-tab-pane>

        <el-tab-pane label="我的申请" name="applications">
          <el-table v-loading="loadingApplications" :data="applications" border stripe>
            <el-table-column prop="编号" label="编号" width="80" />
            <el-table-column prop="服务名称" label="服务事项" min-width="200" />
            <el-table-column prop="服务分类" label="分类" width="130" />
            <el-table-column prop="状态" label="状态" width="105"><template #default="{ row }"><el-tag :type="statusType(row.状态)">{{ row.状态 }}</el-tag></template></el-table-column>
            <el-table-column prop="当前处理角色" label="当前办理角色" width="145" />
            <el-table-column prop="办理期限" label="办理期限" width="180" />
            <el-table-column prop="处理意见" label="处理意见" min-width="220" show-overflow-tooltip />
            <el-table-column label="操作" width="160" fixed="right"><template #default="{ row }"><el-button v-if="row.状态 === '已完成'" link type="primary" @click="openRate(row)">评价服务</el-button><span v-else class="muted">处理中</span></template></el-table-column>
          </el-table>
          <el-empty v-if="!loadingApplications && !applications.length" description="暂无服务申请" />
        </el-tab-pane>

        <el-tab-pane label="待受理服务" name="pending">
          <div class="notice">仅科研管理员、科技办和系统管理员可查看。处理结果以服务端状态为准。</div>
          <el-table v-loading="loadingPending" :data="pending" border stripe>
            <el-table-column prop="编号" label="编号" width="80" /><el-table-column prop="服务名称" label="服务事项" min-width="200" /><el-table-column prop="申请人姓名" label="申请人" width="110" /><el-table-column prop="状态" label="状态" width="100" /><el-table-column prop="办理期限" label="办理期限" width="180" /><el-table-column prop="申请说明" label="申请说明" min-width="240" show-overflow-tooltip />
            <el-table-column label="处理" width="180" fixed="right"><template #default="{ row }"><el-button size="small" type="primary" @click="process(row, '处理中')">受理</el-button><el-button size="small" type="success" @click="process(row, '已完成')">完成</el-button><el-button size="small" type="danger" link @click="process(row, '退回')">退回</el-button></template></el-table-column>
          </el-table>
          <el-empty v-if="!loadingPending && !pending.length" description="暂无待受理申请或当前账号无权限" />
        </el-tab-pane>

        <el-tab-pane label="科研平台" name="platforms">
          <div class="toolbar"><span class="muted">平台基础信息、负责人和开放状态可由管理角色维护。</span><el-button type="primary" @click="openPlatform()">新增平台</el-button></div>
          <el-table v-loading="loadingPlatforms" :data="platforms" border stripe><el-table-column prop="平台名称" label="平台名称" min-width="200" /><el-table-column prop="平台类型" label="类型" width="130" /><el-table-column prop="依托部门名称" label="依托部门" width="150" /><el-table-column prop="负责人姓名" label="负责人" width="110" /><el-table-column prop="开放状态" label="开放状态" width="100" /><el-table-column prop="成员数" label="成员数" width="90" /><el-table-column prop="联系方式" label="联系方式" min-width="160" /><el-table-column label="操作" width="90" fixed="right"><template #default="{ row }"><el-button link type="primary" @click="openPlatform(row)">编辑</el-button></template></el-table-column></el-table>
          <el-empty v-if="!loadingPlatforms && !platforms.length" description="暂无科研平台资料" />
        </el-tab-pane>

        <el-tab-pane label="导入导出日志" name="logs">
          <el-table v-loading="loadingLogs" :data="logs" border stripe><el-table-column prop="创建时间" label="时间" width="180" /><el-table-column prop="模块" label="模块" width="160" /><el-table-column prop="操作类型" label="类型" width="90" /><el-table-column prop="文件名称" label="文件" min-width="220" /><el-table-column prop="记录数" label="记录数" width="90" /><el-table-column prop="成功数" label="成功" width="80" /><el-table-column prop="失败数" label="失败" width="80" /><el-table-column prop="状态" label="状态" width="100" /><el-table-column prop="操作人姓名" label="操作人" width="110" /></el-table>
          <el-empty v-if="!loadingLogs && !logs.length" description="暂无导入导出操作日志或当前账号无权限" />
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <el-dialog v-model="applyDialog.visible" title="发起科研服务申请" width="620px" destroy-on-close><el-form :model="applyDialog.form" label-width="100px"><el-form-item label="服务事项"><el-input :model-value="applyDialog.service?.服务名称 || ''" disabled /></el-form-item><el-form-item label="关联业务类型"><el-input v-model="applyDialog.form.关联业务类型" placeholder="例如：纵向项目、伦理审查" /></el-form-item><el-form-item label="关联业务编号"><el-input-number v-model="applyDialog.form.关联业务编号" :min="1" :controls="false" /></el-form-item><el-form-item label="申请说明"><el-input v-model="applyDialog.form.申请说明" type="textarea" :rows="4" placeholder="说明服务目的、时间要求和需要准备的材料" /></el-form-item></el-form><template #footer><el-button @click="applyDialog.visible = false">取消</el-button><el-button type="primary" :loading="saving" @click="submitApply">提交申请</el-button></template></el-dialog>
    <el-dialog v-model="rateDialog.visible" title="评价科研服务" width="520px"><el-form :model="rateDialog.form" label-width="90px"><el-form-item label="服务事项"><el-input :model-value="rateDialog.serviceName" disabled /></el-form-item><el-form-item label="满意度"><el-rate v-model="rateDialog.form.评价星级" /></el-form-item><el-form-item label="反馈"><el-input v-model="rateDialog.form.反馈内容" type="textarea" :rows="3" /></el-form-item></el-form><template #footer><el-button @click="rateDialog.visible = false">取消</el-button><el-button type="primary" @click="submitRate">提交评价</el-button></template></el-dialog>
    <el-dialog v-model="platformDialog.visible" :title="platformDialog.form.编号 ? '编辑科研平台' : '新增科研平台'" width="650px"><el-form :model="platformDialog.form" label-width="105px"><el-form-item label="平台名称"><el-input v-model="platformDialog.form.平台名称" /></el-form-item><el-form-item label="平台类型"><el-input v-model="platformDialog.form.平台类型" /></el-form-item><el-form-item label="开放状态"><el-select v-model="platformDialog.form.开放状态"><el-option v-for="item in ['开放','预约','暂停','关闭']" :key="item" :label="item" :value="item" /></el-select></el-form-item><el-form-item label="负责人编号"><el-input-number v-model="platformDialog.form.负责人编号" :min="1" :controls="false" /></el-form-item><el-form-item label="依托部门编号"><el-input-number v-model="platformDialog.form.依托部门编号" :min="1" :controls="false" /></el-form-item><el-form-item label="联系方式"><el-input v-model="platformDialog.form.联系方式" /></el-form-item><el-form-item label="平台简介"><el-input v-model="platformDialog.form.平台简介" type="textarea" :rows="3" /></el-form-item><el-form-item label="启用"><el-switch v-model="platformDialog.form.是否启用" /></el-form-item></el-form><template #footer><el-button @click="platformDialog.visible = false">取消</el-button><el-button type="primary" :loading="saving" @click="savePlatform">保存</el-button></template></el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { Refresh } from '@element-plus/icons-vue'
import { useRoute } from 'vue-router'
import { createResearchServiceApplication, listMyResearchServiceApplications, listPendingResearchServices, listResearchImportExportLogs, listResearchPlatforms, listResearchServices, processResearchServiceApplication, rateResearchService, saveResearchPlatform, type ResearchPlatform, type ResearchService, type ResearchServiceApplication } from '@/api/research-platform'

const route = useRoute()
const tab = ref(String(route.query.tab || 'services'))
const loading = ref(false); const saving = ref(false); const loadingServices = ref(false); const loadingApplications = ref(false); const loadingPending = ref(false); const loadingPlatforms = ref(false); const loadingLogs = ref(false)
const services = ref<ResearchService[]>([]); const applications = ref<ResearchServiceApplication[]>([]); const pending = ref<ResearchServiceApplication[]>([]); const platforms = ref<ResearchPlatform[]>([]); const logs = ref<any[]>([]); const serviceKeyword = ref('')
const applyDialog = reactive<{ visible: boolean; service?: ResearchService; form: any }>({ visible: false, form: {} })
const rateDialog = reactive<{ visible: boolean; serviceName: string; form: any }>({ visible: false, serviceName: '', form: { 服务申请编号: 0, 评价星级: 5, 反馈内容: '', 是否公开: false } })
const platformDialog = reactive<{ visible: boolean; form: ResearchPlatform }>({ visible: false, form: { 平台名称: '', 平台类型: '科研平台', 开放状态: '开放', 是否启用: true } })
function unwrap<T>(value: any): T[] { return Array.isArray(value) ? value : value?.items || value?.list || value?.List || [] }
const filteredServices = computed(() => { const key = serviceKeyword.value.trim().toLowerCase(); return !key ? services.value : services.value.filter(s => `${s.服务编码}${s.服务名称}`.toLowerCase().includes(key)) })
function statusType(value?: string) { return value === '已完成' ? 'success' : value === '退回' || value === '已取消' ? 'danger' : value === '处理中' ? 'warning' : 'info' }
async function loadServices() { loadingServices.value = true; try { services.value = unwrap<ResearchService>(await listResearchServices()) } catch { services.value = [] } finally { loadingServices.value = false } }
async function loadApplications() { loadingApplications.value = true; try { applications.value = unwrap<ResearchServiceApplication>(await listMyResearchServiceApplications()) } catch { applications.value = [] } finally { loadingApplications.value = false } }
async function loadPending() { loadingPending.value = true; try { pending.value = unwrap<ResearchServiceApplication>(await listPendingResearchServices()) } catch { pending.value = [] } finally { loadingPending.value = false } }
async function loadPlatforms() { loadingPlatforms.value = true; try { platforms.value = unwrap<ResearchPlatform>(await listResearchPlatforms({ 仅启用: true })) } catch { platforms.value = [] } finally { loadingPlatforms.value = false } }
async function loadLogs() { loadingLogs.value = true; try { logs.value = unwrap<any>(await listResearchImportExportLogs({ 数量: 100 })) } catch { logs.value = [] } finally { loadingLogs.value = false } }
async function refresh() { loading.value = true; await Promise.allSettled([loadServices(), loadApplications(), loadPending(), loadPlatforms(), loadLogs()]); loading.value = false }
async function onTabChange() { if (tab.value === 'pending') await loadPending(); if (tab.value === 'platforms') await loadPlatforms(); if (tab.value === 'logs') await loadLogs() }
function openApply(service: ResearchService) { applyDialog.service = service; applyDialog.form = { 服务事项编号: service.编号, 关联业务类型: '', 关联业务编号: undefined, 申请说明: '' }; applyDialog.visible = true }
async function submitApply() { if (!applyDialog.form.服务事项编号) return; saving.value = true; try { await createResearchServiceApplication(applyDialog.form); ElMessage.success('服务申请已提交'); applyDialog.visible = false; await loadApplications() } finally { saving.value = false } }
function openRate(row: ResearchServiceApplication) { rateDialog.serviceName = row.服务名称 || ''; rateDialog.form = { 服务申请编号: row.编号, 评价星级: row.评价星级 || 5, 反馈内容: row.反馈内容 || '', 是否公开: false }; rateDialog.visible = true }
async function submitRate() { if (!rateDialog.form.服务申请编号) return; await rateResearchService(rateDialog.form); ElMessage.success('评价已提交'); rateDialog.visible = false; await loadApplications() }
async function process(row: ResearchServiceApplication, 状态: string) { if (!row.编号) return; await processResearchServiceApplication({ 编号: row.编号, 状态, 处理意见: 状态 === '退回' ? '请补充材料后重新提交' : undefined }); ElMessage.success(`申请已${状态 === '处理中' ? '受理' : 状态}`); await Promise.all([loadPending(), loadApplications()]) }
function openPlatform(row?: ResearchPlatform) { platformDialog.form = row ? { ...row } : { 平台名称: '', 平台类型: '科研平台', 开放状态: '开放', 是否启用: true }; platformDialog.visible = true }
async function savePlatform() { if (!platformDialog.form.平台名称?.trim()) { ElMessage.warning('请输入平台名称'); return } saving.value = true; try { await saveResearchPlatform(platformDialog.form); ElMessage.success('科研平台已保存'); platformDialog.visible = false; await loadPlatforms() } finally { saving.value = false } }
onMounted(refresh)
</script>

<style scoped lang="scss">
.research-resource-page { padding: 20px; min-height: 100%; background: #f5f7fa; }.header-card,.content-card { border: 0; border-radius: 10px; margin-bottom: 16px; }.header { display:flex; justify-content:space-between; align-items:center; gap: 16px; }h2 { margin:0 0 8px; color:#1f2937; }.header p { margin:0; color:#7b8794; }.toolbar { display:flex; gap:10px; align-items:center; margin-bottom:14px; flex-wrap:wrap; }.notice { padding:10px 14px; margin-bottom:14px; border-radius:6px; color:#52606d; background:#f0f4f8; }.muted { color:#9aa5b1; font-size:13px; }
</style>
