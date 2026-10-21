<template>
  <div class="document-sharing-page">
    <el-card shadow="never" class="header-card">
      <div class="page-header">
        <div>
          <h2>科研文档共享与消息中心</h2>
          <p class="subtitle">文档分类、版本、授权下载、服务催办和短信/邮件/微信消息队列统一管理</p>
        </div>
        <el-tag type="warning">真实外部通道默认关闭</el-tag>
      </div>
    </el-card>

    <el-card shadow="never">
      <el-tabs v-model="activeTab" @tab-change="handleTabChange">
        <el-tab-pane label="文档共享" name="documents">
          <el-form :inline="true" :model="documentQuery">
            <el-form-item label="关键词"><el-input v-model="documentQuery.关键词" clearable placeholder="名称或编码" /></el-form-item>
            <el-form-item label="分类"><el-input v-model="documentQuery.文档分类" clearable placeholder="政策、模板、制度" /></el-form-item>
            <el-button type="primary" @click="loadDocuments">查询</el-button>
            <el-button @click="openDocumentDialog()">新增文档</el-button>
          </el-form>
          <el-table v-loading="loading" :data="documents" border stripe>
            <el-table-column prop="文档编码" label="编码" width="150" />
            <el-table-column prop="文档名称" label="文档名称" min-width="240" show-overflow-tooltip />
            <el-table-column prop="文档分类" label="分类" width="120" />
            <el-table-column prop="版本号" label="当前版本" width="100" />
            <el-table-column prop="授权模式" label="授权模式" width="100" />
            <el-table-column prop="下载次数" label="下载次数" width="100" />
            <el-table-column label="操作" width="270" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" @click="openVersionDialog(row)">版本</el-button>
                <el-button link type="primary" @click="openAuthDialog(row)">授权</el-button>
                <el-button link type="success" :disabled="!row.当前版本编号" @click="download(row)">下载</el-button>
                <el-button link @click="openDocumentDialog(row)">编辑</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="消息通道" name="channels">
          <div class="toolbar"><el-button type="primary" @click="openChannelDialog()">登记通道</el-button><span class="tip">仅配置“已验证”且启用的真实通道会进入发送队列</span></div>
          <el-table v-loading="loading" :data="channels" border stripe>
            <el-table-column prop="通道类型" label="类型" width="110" />
            <el-table-column prop="通道名称" label="名称" min-width="220" />
            <el-table-column prop="服务地址" label="服务地址" min-width="220" show-overflow-tooltip />
            <el-table-column prop="通道状态" label="状态" width="100" />
            <el-table-column prop="是否启用" label="启用" width="80"><template #default="{ row }"><el-tag :type="row.是否启用 ? 'success' : 'info'">{{ row.是否启用 ? '是' : '否' }}</el-tag></template></el-table-column>
            <el-table-column label="操作" width="100"><template #default="{ row }"><el-button link @click="openChannelDialog(row)">编辑</el-button></template></el-table-column>
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="消息模板" name="templates">
          <div class="toolbar"><el-button type="primary" @click="openTemplateDialog()">新增模板</el-button></div>
          <el-table v-loading="loading" :data="templates" border stripe>
            <el-table-column prop="模板编码" label="编码" width="160" /><el-table-column prop="模板名称" label="名称" min-width="180" />
            <el-table-column prop="消息类型" label="消息类型" width="120" /><el-table-column prop="标题模板" label="标题模板" min-width="220" show-overflow-tooltip />
            <el-table-column prop="是否启用" label="启用" width="80"><template #default="{ row }"><el-tag :type="row.是否启用 ? 'success' : 'info'">{{ row.是否启用 ? '是' : '否' }}</el-tag></template></el-table-column>
            <el-table-column label="操作" width="100"><template #default="{ row }"><el-button link @click="openTemplateDialog(row)">编辑</el-button></template></el-table-column>
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="发送队列" name="queue">
          <div class="toolbar"><el-button @click="loadQueue">刷新</el-button><span class="tip">“已发送”必须由真实适配器回执登记</span></div>
          <el-table v-loading="loading" :data="queue" border stripe>
            <el-table-column prop="消息标题" label="消息" min-width="240" show-overflow-tooltip /><el-table-column prop="通道名称" label="通道" width="160" />
            <el-table-column prop="接收人姓名" label="接收人" width="100" /><el-table-column prop="队列状态" label="状态" width="100" />
            <el-table-column prop="重试次数" label="重试" width="80" /><el-table-column prop="创建时间" label="创建时间" width="170" />
            <el-table-column label="操作" width="100"><template #default="{ row }"><el-button link type="warning" :disabled="!['发送失败','未配置','待发送'].includes(row.队列状态)" @click="retry(row)">重试</el-button></template></el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <el-dialog v-model="documentDialogVisible" :title="documentForm.编号 ? '编辑文档' : '新增文档'" width="520px">
      <el-form :model="documentForm" label-width="90px"><el-form-item label="文档编码"><el-input v-model="documentForm.文档编码" /></el-form-item><el-form-item label="文档名称"><el-input v-model="documentForm.文档名称" /></el-form-item><el-form-item label="文档分类"><el-input v-model="documentForm.文档分类" /></el-form-item><el-form-item label="授权模式"><el-select v-model="documentForm.授权模式"><el-option label="按授权" value="按授权" /><el-option label="公开" value="公开" /></el-select></el-form-item><el-form-item label="说明"><el-input v-model="documentForm.文档说明" type="textarea" /></el-form-item><el-form-item label="启用"><el-switch v-model="documentForm.是否启用" /></el-form-item></el-form>
      <template #footer><el-button @click="documentDialogVisible = false">取消</el-button><el-button type="primary" @click="saveDocument">保存</el-button></template>
    </el-dialog>
    <el-dialog v-model="versionDialogVisible" title="登记文档版本" width="520px"><el-form :model="versionForm" label-width="90px"><el-form-item label="版本号"><el-input v-model="versionForm.版本号" /></el-form-item><el-form-item label="文件名称"><el-input v-model="versionForm.文件名称" /></el-form-item><el-form-item label="文件路径"><el-input v-model="versionForm.文件路径" placeholder="相对于 wwwroot 的路径" /></el-form-item><el-form-item label="SHA256"><el-input v-model="versionForm.SHA256" /></el-form-item><el-form-item label="状态"><el-select v-model="versionForm.版本状态"><el-option label="草稿" value="草稿" /><el-option label="生效" value="生效" /><el-option label="撤销" value="撤销" /></el-select></el-form-item></el-form><template #footer><el-button @click="versionDialogVisible = false">取消</el-button><el-button type="primary" @click="saveVersion">保存</el-button></template></el-dialog>
    <el-dialog v-model="authDialogVisible" title="文档授权" width="520px"><el-form :model="authForm" label-width="90px"><el-form-item label="授权类型"><el-select v-model="authForm.授权类型"><el-option label="所有人" value="所有人" /><el-option label="人员" value="人员" /><el-option label="部门" value="部门" /><el-option label="角色" value="角色" /></el-select></el-form-item><el-form-item label="对象编号"><el-input-number v-model="authForm.授权对象编号" :min="0" /></el-form-item><el-form-item label="允许下载"><el-switch v-model="authForm.可下载" /></el-form-item></el-form><template #footer><el-button @click="authDialogVisible = false">取消</el-button><el-button type="primary" @click="saveAuth">保存</el-button></template></el-dialog>
    <el-dialog v-model="channelDialogVisible" title="消息通道" width="520px"><el-form :model="channelForm" label-width="90px"><el-form-item label="类型"><el-select v-model="channelForm.通道类型"><el-option v-for="item in ['短信','邮件','微信','企业微信']" :key="item" :label="item" :value="item" /></el-select></el-form-item><el-form-item label="名称"><el-input v-model="channelForm.通道名称" /></el-form-item><el-form-item label="服务地址"><el-input v-model="channelForm.服务地址" /></el-form-item><el-form-item label="凭据引用"><el-input v-model="channelForm.凭据引用" placeholder="只填密钥管理系统引用，不填明文密钥" /></el-form-item><el-form-item label="状态"><el-select v-model="channelForm.通道状态"><el-option label="待配置" value="待配置" /><el-option label="待验证" value="待验证" /><el-option label="已验证" value="已验证" /><el-option label="验证失败" value="验证失败" /><el-option label="已停用" value="已停用" /></el-select></el-form-item><el-form-item label="启用"><el-switch v-model="channelForm.是否启用" /></el-form-item></el-form><template #footer><el-button @click="channelDialogVisible = false">取消</el-button><el-button type="primary" @click="saveChannel">保存</el-button></template></el-dialog>
    <el-dialog v-model="templateDialogVisible" title="消息模板" width="560px"><el-form :model="templateForm" label-width="90px"><el-form-item label="模板编码"><el-input v-model="templateForm.模板编码" /></el-form-item><el-form-item label="模板名称"><el-input v-model="templateForm.模板名称" /></el-form-item><el-form-item label="消息类型"><el-input v-model="templateForm.消息类型" /></el-form-item><el-form-item label="标题模板"><el-input v-model="templateForm.标题模板" /></el-form-item><el-form-item label="正文模板"><el-input v-model="templateForm.正文模板" type="textarea" /></el-form-item><el-form-item label="变量定义"><el-input v-model="templateForm.变量定义" placeholder="JSON，例如 {&quot;申请人&quot;:&quot;string&quot;}" /></el-form-item><el-form-item label="启用"><el-switch v-model="templateForm.是否启用" /></el-form-item></el-form><template #footer><el-button @click="templateDialogVisible = false">取消</el-button><el-button type="primary" @click="saveTemplate">保存</el-button></template></el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import type { ResearchDocument, MessageChannel, MessageTemplate } from '@/api/document-messaging'
import { listResearchDocuments, saveResearchDocument, saveDocumentVersion, saveDocumentAuthorization, getDocumentDownloadUrl, listMessageChannels, saveMessageChannel, listMessageTemplates, saveMessageTemplate, listMessageQueue, retryMessage } from '@/api/document-messaging'

const activeTab = ref('documents'); const loading = ref(false)
const documents = ref<ResearchDocument[]>([]); const channels = ref<MessageChannel[]>([]); const templates = ref<MessageTemplate[]>([]); const queue = ref<any[]>([])
const documentQuery = reactive({ 关键词: '', 文档分类: '' })
const documentDialogVisible = ref(false); const versionDialogVisible = ref(false); const authDialogVisible = ref(false); const channelDialogVisible = ref(false); const templateDialogVisible = ref(false)
const documentForm = reactive<any>({ 文档编码: '', 文档名称: '', 文档分类: '未分类', 授权模式: '按授权', 文档说明: '', 是否启用: true })
const versionForm = reactive<any>({ 文档编号: 0, 版本号: '', 文件名称: '', 文件路径: '', 版本状态: '草稿', SHA256: '' }); const authForm = reactive<any>({ 文档编号: 0, 授权类型: '人员', 授权对象编号: 0, 可下载: true })
const channelForm = reactive<any>({ 通道类型: '短信', 通道名称: '', 服务地址: '', 凭据引用: '', 通道状态: '待配置', 是否启用: false }); const templateForm = reactive<any>({ 模板编码: '', 模板名称: '', 消息类型: '待办提醒', 标题模板: '', 正文模板: '', 变量定义: '{}', 是否启用: true })

async function loadDocuments() { loading.value = true; try { documents.value = await listResearchDocuments({ 关键词: documentQuery.关键词 || undefined, 文档分类: documentQuery.文档分类 || undefined }) as any } finally { loading.value = false } }
async function loadChannels() { loading.value = true; try { channels.value = await listMessageChannels() as any } finally { loading.value = false } }
async function loadTemplates() { loading.value = true; try { templates.value = await listMessageTemplates() as any } finally { loading.value = false } }
async function loadQueue() { loading.value = true; try { queue.value = await listMessageQueue() as any } finally { loading.value = false } }
function handleTabChange(name: string) { if (name === 'documents') loadDocuments(); else if (name === 'channels') loadChannels(); else if (name === 'templates') loadTemplates(); else if (name === 'queue') loadQueue() }
function openDocumentDialog(row?: ResearchDocument) { Object.assign(documentForm, row || { 编号: undefined, 文档编码: '', 文档名称: '', 文档分类: '未分类', 授权模式: '按授权', 文档说明: '', 是否启用: true }); documentDialogVisible.value = true }
async function saveDocument() { await saveResearchDocument(documentForm); ElMessage.success('文档已保存'); documentDialogVisible.value = false; await loadDocuments() }
function openVersionDialog(row: ResearchDocument) { Object.assign(versionForm, { 文档编号: row.编号, 版本号: '', 文件名称: '', 文件路径: '', 版本状态: '草稿', SHA256: '' }); versionDialogVisible.value = true }
async function saveVersion() { await saveDocumentVersion(versionForm); ElMessage.success('版本已保存'); versionDialogVisible.value = false; await loadDocuments() }
function openAuthDialog(row: ResearchDocument) { Object.assign(authForm, { 文档编号: row.编号, 授权类型: '人员', 授权对象编号: 0, 可下载: true }); authDialogVisible.value = true }
async function saveAuth() { await saveDocumentAuthorization(authForm); ElMessage.success('授权已保存'); authDialogVisible.value = false }
function download(row: ResearchDocument) { if (row.当前版本编号) window.open(getDocumentDownloadUrl(row.当前版本编号), '_blank', 'noopener') }
function openChannelDialog(row?: MessageChannel) { Object.assign(channelForm, row || { 编号: undefined, 通道类型: '短信', 通道名称: '', 服务地址: '', 凭据引用: '', 通道状态: '待配置', 是否启用: false }); channelDialogVisible.value = true }
async function saveChannel() { await saveMessageChannel(channelForm); ElMessage.success('通道配置已保存，未执行外部发送'); channelDialogVisible.value = false; await loadChannels() }
function openTemplateDialog(row?: MessageTemplate) { Object.assign(templateForm, row || { 编号: undefined, 模板编码: '', 模板名称: '', 消息类型: '待办提醒', 标题模板: '', 正文模板: '', 变量定义: '{}', 是否启用: true }); templateDialogVisible.value = true }
async function saveTemplate() { await saveMessageTemplate(templateForm); ElMessage.success('消息模板已保存'); templateDialogVisible.value = false; await loadTemplates() }
async function retry(row: any) { await retryMessage(row.编号); ElMessage.success('已登记重试，等待真实适配器'); await loadQueue() }
onMounted(loadDocuments)
</script>

<style scoped>
.document-sharing-page { padding: 16px; }
.page-header { display: flex; justify-content: space-between; align-items: center; }
.subtitle, .tip { color: var(--el-text-color-secondary); font-size: 13px; }
.toolbar { display: flex; align-items: center; gap: 14px; margin-bottom: 14px; }
</style>
