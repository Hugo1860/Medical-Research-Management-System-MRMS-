<template>
  <div class="page-container">
    <el-card shadow="never">
      <template #header>
        <div class="header-row">
          <div><strong>教育部科研统计</strong><span class="muted">任务版本、数据包校验、调整留痕与导出状态</span></div>
          <div class="actions"><el-button type="primary" @click="openTask()">新建统计任务</el-button><el-button @click="loadAll">刷新</el-button></div>
        </div>
      </template>
      <el-alert type="info" :closable="false" show-icon style="margin-bottom: 14px;" title="系统只记录实际导入的数据包、校验结果和导出状态；不会伪造教育部统计数据或外部回执。" />
      <el-tabs v-model="activeTab">
        <el-tab-pane label="统计任务与版本" name="tasks">
          <el-table v-loading="loading" :data="tasks" stripe>
            <el-table-column prop="统计年度" label="年度" width="80" />
            <el-table-column prop="任务代码" label="任务代码" width="150" />
            <el-table-column prop="任务名称" label="任务名称" min-width="220" />
            <el-table-column prop="当前版本号" label="当前版本" width="100" />
            <el-table-column prop="状态" label="状态" width="100"><template #default="{ row }"><el-tag>{{ row.状态 }}</el-tag></template></el-table-column>
            <el-table-column prop="数据包数量" label="数据包" width="90" />
            <el-table-column label="操作" width="230" fixed="right">
              <template #default="{ row }"><el-button link type="primary" @click="showVersions(row)">版本</el-button><el-button link @click="openPackage(row)">登记数据包</el-button><el-button link @click="openTask(row)">编辑</el-button></template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
        <el-tab-pane label="数据包与校验" name="packages">
          <div class="toolbar"><el-select v-model="packageTaskId" clearable placeholder="按任务筛选" @change="loadPackages" style="width: 260px"><el-option v-for="item in tasks" :key="item.编号" :label="`${item.统计年度} · ${item.任务名称}`" :value="item.编号" /></el-select></div>
          <el-table v-loading="packageLoading" :data="packages" stripe>
            <el-table-column prop="包名称" label="数据包" min-width="220" /><el-table-column prop="任务代码" label="任务代码" width="140" /><el-table-column prop="版本号" label="版本" width="80" />
            <el-table-column prop="记录数" label="记录数" width="90" /><el-table-column prop="校验错误数" label="错误" width="80" /><el-table-column prop="校验警告数" label="警告" width="80" />
            <el-table-column prop="状态" label="状态" width="100"><template #default="{ row }"><el-tag :type="row.状态 === '校验通过' ? 'success' : row.状态 === '校验失败' ? 'danger' : 'info'">{{ row.状态 }}</el-tag></template></el-table-column>
            <el-table-column label="操作" width="250" fixed="right"><template #default="{ row }"><el-button link type="primary" @click="validatePackage(row)" :disabled="row.状态 === '待导入' || row.状态 === '已作废'">校验</el-button><el-button link @click="showValidation(row)">校验明细</el-button><el-button link type="success" :disabled="row.状态 !== '校验通过'" @click="createExport(row)">申请导出</el-button></template></el-table-column>
          </el-table>
        </el-tab-pane>
        <el-tab-pane label="导出任务" name="exports"><el-table v-loading="exportLoading" :data="exports" stripe><el-table-column prop="任务代码" label="任务代码" width="160" /><el-table-column prop="版本号" label="版本" width="80" /><el-table-column prop="导出格式" label="格式" width="80" /><el-table-column prop="状态" label="状态" width="100"><template #default="{ row }"><el-tag>{{ row.状态 }}</el-tag></template></el-table-column><el-table-column prop="文件路径" label="文件路径" min-width="280" show-overflow-tooltip /><el-table-column prop="错误信息" label="错误信息" min-width="180" show-overflow-tooltip /><el-table-column prop="申请时间" label="申请时间" width="170" /></el-table></el-tab-pane>
        <el-tab-pane label="操作审计" name="audit"><el-table v-loading="auditLoading" :data="auditRows" stripe><el-table-column prop="操作类型" label="操作" width="170" /><el-table-column prop="说明" label="说明" min-width="420" show-overflow-tooltip /><el-table-column prop="操作人姓名" label="操作人" width="120" /><el-table-column prop="创建时间" label="时间" width="180" /></el-table></el-tab-pane>
      </el-tabs>
    </el-card>

    <el-dialog v-model="taskDialog" :title="taskForm.编号 ? '编辑统计任务' : '新建统计任务'" width="560px"><el-form :model="taskForm" label-width="110px"><el-form-item label="统计年度" required><el-input-number v-model="taskForm.统计年度" :min="2000" :max="2200" /></el-form-item><el-form-item label="任务代码" required><el-input v-model="taskForm.任务代码" placeholder="如：KY2026" /></el-form-item><el-form-item label="任务名称" required><el-input v-model="taskForm.任务名称" /></el-form-item><el-form-item label="截止日期"><el-date-picker v-model="taskForm.截止日期" type="date" value-format="YYYY-MM-DD" /></el-form-item><el-form-item label="数据截止日期"><el-date-picker v-model="taskForm.数据截止日期" type="date" value-format="YYYY-MM-DD" /></el-form-item><el-form-item label="说明"><el-input v-model="taskForm.说明" type="textarea" /></el-form-item></el-form><template #footer><el-button @click="taskDialog=false">取消</el-button><el-button type="primary" @click="saveTask">保存</el-button></template></el-dialog>

    <el-dialog v-model="versionDialog" title="统计版本" width="680px"><div class="version-header"><span>{{ selectedTask?.任务名称 }}</span><el-button type="primary" size="small" @click="newVersion">新增版本</el-button></div><el-table :data="versions" stripe><el-table-column prop="版本号" label="版本" width="80" /><el-table-column prop="模板代码" label="模板代码" width="150" /><el-table-column prop="模板名称" label="模板名称" min-width="180" /><el-table-column prop="状态" label="状态" width="90" /><el-table-column label="操作" width="90"><template #default="{ row }"><el-button link type="primary" @click="publishVersion(row)" :disabled="row.状态 === '生效'">发布</el-button></template></el-table-column></el-table></el-dialog>
    <el-dialog v-model="versionEditDialog" title="新建统计版本" width="700px"><el-form :model="versionForm" label-width="110px"><el-form-item label="版本号"><el-input-number v-model="versionForm.版本号" :min="1" /></el-form-item><el-form-item label="模板代码"><el-input v-model="versionForm.模板代码" /></el-form-item><el-form-item label="模板名称"><el-input v-model="versionForm.模板名称" /></el-form-item><el-form-item label="字段定义 JSON"><el-input v-model="versionForm.字段定义" type="textarea" :rows="9" placeholder='[{"代码":"项目数","类型":"integer","必填":true}]' /></el-form-item><el-form-item label="变更说明"><el-input v-model="versionForm.变更说明" type="textarea" /></el-form-item></el-form><template #footer><el-button @click="versionEditDialog=false">取消</el-button><el-button type="primary" @click="saveVersion">保存</el-button></template></el-dialog>
    <el-dialog v-model="packageDialog" title="登记统计数据包" width="700px"><el-alert type="warning" :closable="false" title="仅在已实际取得并解析数据后填写数据内容；只登记路径不会生成校验通过结果。" style="margin-bottom: 12px" /><el-form :model="packageForm" label-width="110px"><el-form-item label="统计任务"><el-select v-model="packageForm.任务编号" style="width:100%" @change="loadActiveVersion"><el-option v-for="item in tasks" :key="item.编号" :label="`${item.统计年度} · ${item.任务名称}`" :value="item.编号" /></el-select></el-form-item><el-form-item label="生效版本"><el-select v-model="packageForm.版本编号" style="width:100%"><el-option v-for="item in activeVersions" :key="item.编号" :label="`v${item.版本号} · ${item.模板名称}`" :value="item.编号" /></el-select></el-form-item><el-form-item label="包名称"><el-input v-model="packageForm.包名称" /></el-form-item><el-form-item label="文件路径"><el-input v-model="packageForm.文件路径" placeholder="服务器受控存储路径（可选）" /></el-form-item><el-form-item label="文件校验和"><el-input v-model="packageForm.文件校验和" placeholder="SHA-256（可选）" /></el-form-item><el-form-item label="数据内容 JSON"><el-input v-model="packageForm.数据内容" type="textarea" :rows="8" placeholder="实际解析后的 JSON 数组；留空则仅登记元数据" /></el-form-item></el-form><template #footer><el-button @click="packageDialog=false">取消</el-button><el-button type="primary" @click="registerPackage">登记</el-button></template></el-dialog>
    <el-dialog v-model="validationDialog" title="字段校验明细" width="850px"><el-table v-loading="validationLoading" :data="validationRows" stripe max-height="520"><el-table-column prop="记录序号" label="行" width="70" /><el-table-column prop="字段代码" label="字段" width="150" /><el-table-column prop="严重程度" label="级别" width="80" /><el-table-column prop="消息" label="消息" min-width="300" /><el-table-column prop="实际值" label="实际值" min-width="180" show-overflow-tooltip /><el-table-column prop="状态" label="处理状态" width="90" /></el-table></el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import {
  listEducationStatisticsTasks, saveEducationStatisticsTask, listEducationStatisticsVersions, saveEducationStatisticsVersion, publishEducationStatisticsVersion,
  listEducationStatisticsPackages, registerEducationStatisticsPackage, validateEducationStatisticsPackage, listEducationStatisticsValidations,
  createEducationStatisticsExport, listEducationStatisticsExports, listEducationStatisticsAudit
} from '@/api/education-statistics'

const activeTab = ref('tasks'); const loading = ref(false); const packageLoading = ref(false); const exportLoading = ref(false); const auditLoading = ref(false)
const tasks = ref<any[]>([]); const packages = ref<any[]>([]); const exports = ref<any[]>([]); const auditRows = ref<any[]>([]); const versions = ref<any[]>([]); const activeVersions = ref<any[]>([])
const packageTaskId = ref<number | undefined>(); const selectedTask = ref<any>(); const taskDialog = ref(false); const versionDialog = ref(false); const versionEditDialog = ref(false); const packageDialog = ref(false); const validationDialog = ref(false); const validationLoading = ref(false); const validationRows = ref<any[]>([])
const taskForm = ref<any>({ 统计年度: new Date().getFullYear(), 任务代码: '', 任务名称: '', 状态: '草稿' }); const versionForm = ref<any>({ 任务编号: 0, 版本号: 1, 模板代码: '', 模板名称: '', 字段定义: '[]', 状态: '草稿' }); const packageForm = ref<any>({ 任务编号: 0, 版本编号: 0, 包名称: '', 数据内容: '' })

const unwrap = <T,>(value: any): T => (value?.data ?? value) as T
async function loadTasks() { loading.value = true; try { tasks.value = unwrap<any[]>(await listEducationStatisticsTasks()) } finally { loading.value = false } }
async function loadPackages() { packageLoading.value = true; try { packages.value = unwrap<any[]>(await listEducationStatisticsPackages(packageTaskId.value ? { 任务编号: packageTaskId.value } : {})) } finally { packageLoading.value = false } }
async function loadExports() { exportLoading.value = true; try { exports.value = unwrap<any[]>(await listEducationStatisticsExports()) } finally { exportLoading.value = false } }
async function loadAudit() { auditLoading.value = true; try { auditRows.value = unwrap<any[]>(await listEducationStatisticsAudit({ 数量: 200 })) } finally { auditLoading.value = false } }
async function loadAll() { await Promise.all([loadTasks(), loadPackages(), loadExports(), loadAudit()]) }
function openTask(row?: any) { taskForm.value = row ? { ...row } : { 统计年度: new Date().getFullYear(), 任务代码: '', 任务名称: '', 状态: '草稿' }; taskDialog.value = true }
async function saveTask() { try { await saveEducationStatisticsTask(taskForm.value); taskDialog.value = false; ElMessage.success('统计任务已保存'); await loadTasks() } catch { /* request interceptor displays server error */ } }
async function showVersions(row: any) { selectedTask.value = row; versions.value = unwrap<any[]>(await listEducationStatisticsVersions(row.编号)); versionDialog.value = true }
function newVersion() { versionForm.value = { 任务编号: selectedTask.value.编号, 版本号: (versions.value[0]?.版本号 || 0) + 1, 模板代码: '', 模板名称: '', 字段定义: '[]', 状态: '草稿' }; versionEditDialog.value = true }
async function saveVersion() { try { await saveEducationStatisticsVersion(versionForm.value); versionEditDialog.value = false; versions.value = unwrap<any[]>(await listEducationStatisticsVersions(selectedTask.value.编号)); ElMessage.success('统计版本已保存') } catch {} }
async function publishVersion(row: any) { try { await publishEducationStatisticsVersion(row.编号!); versions.value = unwrap<any[]>(await listEducationStatisticsVersions(selectedTask.value.编号)); await loadTasks(); ElMessage.success('版本已发布') } catch {} }
async function loadActiveVersion() { activeVersions.value = packageForm.value.任务编号 ? unwrap<any[]>(await listEducationStatisticsVersions(packageForm.value.任务编号)).filter((x: any) => x.状态 === '生效') : []; packageForm.value.版本编号 = activeVersions.value[0]?.编号 || 0 }
async function openPackage(row: any) { packageForm.value = { 任务编号: row.编号, 版本编号: 0, 包名称: '', 数据内容: '' }; await loadActiveVersion(); packageDialog.value = true }
async function registerPackage() { try { await registerEducationStatisticsPackage(packageForm.value); packageDialog.value = false; await loadPackages(); ElMessage.success('数据包已登记，需执行字段校验') } catch {} }
async function validatePackage(row: any) { try { await validateEducationStatisticsPackage(row.编号); await loadPackages(); ElMessage.success('数据包字段校验已完成') } catch {} }
async function showValidation(row: any) { validationDialog.value = true; validationLoading.value = true; try { validationRows.value = unwrap<any[]>(await listEducationStatisticsValidations(row.编号)) } finally { validationLoading.value = false } }
async function createExport(row: any) { try { await createEducationStatisticsExport({ 任务编号: row.任务编号, 版本编号: row.版本编号, 数据包编号: row.编号, 导出格式: 'xlsx' }); await loadExports(); ElMessage.success('导出任务已创建，等待受控 worker 生成文件') } catch {} }
onMounted(loadAll)
</script>

<style scoped>
.header-row,.version-header,.toolbar,.actions{display:flex;align-items:center;justify-content:space-between;gap:12px}.muted{margin-left:12px;color:#909399;font-size:13px}.page-container{padding:18px}.toolbar{justify-content:flex-start;margin-bottom:12px}
</style>
