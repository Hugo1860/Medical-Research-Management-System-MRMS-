<template>
  <div class="vertical-import-container">
    <el-card shadow="never">
      <template #header><div class="card-header"><span>计划书 PDF 导入</span><el-tag type="info">上传 → 核对原件 → 确认立项</el-tag></div></template>
      <el-alert title="系统提取 PDF 文字层。扫描件需人工填写，不执行 OCR；所有文件都需核对负责人、批准信息、金额单位和项目日期后才能导入。" type="info" :closable="false" show-icon />
      <div class="toolbar">
        <el-button type="primary" @click="createVisible = true">创建导入批次</el-button>
        <el-button @click="loadBatches">刷新列表</el-button>
      </div>
      <el-table :data="batches" border>
        <el-table-column prop="批次名称" label="批次名称" min-width="200" />
        <el-table-column prop="来源类型" label="来源" width="160" />
        <el-table-column prop="状态" label="状态" width="100" />
        <el-table-column prop="总文件数" label="文件数" width="80" />
        <el-table-column prop="解析成功数" label="文字提取" width="90" />
        <el-table-column prop="导入成功数" label="已导入" width="80" />
        <el-table-column prop="导入人姓名" label="导入人" width="100" />
        <el-table-column label="操作" width="240"><template #default="{ row }">
          <el-button link type="primary" @click="handleViewDetails(row)">上传 / 核对</el-button>
          <el-button v-if="row.状态 === '已校验'" link type="success" :disabled="confirming" @click="handleConfirmImport(row)">确认批量立项</el-button>
        </template></el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="createVisible" title="创建计划书导入批次" width="500px">
      <el-form label-width="90px">
        <el-form-item label="批次名称" required><el-input v-model="batchName" maxlength="200" /></el-form-item>
        <el-form-item label="来源" required><el-select v-model="batchSource"><el-option v-for="source in ['国家自然科学基金','省部级','其他']" :key="source" :label="source" :value="source" /></el-select></el-form-item>
      </el-form>
      <template #footer><el-button @click="createVisible = false">取消</el-button><el-button type="primary" :loading="creating" @click="handleCreateBatch">创建</el-button></template>
    </el-dialog>

    <el-drawer v-model="drawerVisible" :title="currentBatch?.批次名称 || '导入明细'" size="85%">
      <div class="toolbar">
        <input ref="fileInput" type="file" accept=".pdf,application/pdf" multiple hidden @change="handleUpload" />
        <input ref="restoreInput" type="file" accept=".pdf,application/pdf" hidden @change="handleRestoreOriginal" />
        <el-button type="primary" :disabled="!batchEditable || restoring" :loading="uploading" @click="fileInput?.click()">上传 PDF</el-button>
        <span class="hint">{{ PROPOSAL_UPLOAD_LIMIT_HINT }}；同一文件重复上传可修复原件路径，并保留核对结果。</span>
      </div>
      <el-alert title="历史原件无法下载时，可选择“补传原件”。须提供与首次上传内容完全一致的 PDF；补传仅修复原件，不改变核对结果或重新导入项目。已导入、已关闭批次也可补传。" type="info" :closable="false" class="restore-hint" />
      <el-table :data="details" border>
        <el-table-column prop="源文件名" label="文件名" min-width="170" />
        <el-table-column prop="解析状态" label="文字提取" width="100" />
        <el-table-column prop="校验状态" label="核对状态" width="100"><template #default="{ row }"><el-tag :type="row.校验状态 === '通过' ? 'success' : 'warning'">{{ row.校验状态 }}</el-tag></template></el-table-column>
        <el-table-column prop="处理状态" label="处理状态" width="100" />
        <el-table-column label="提取 / 核对信息" min-width="230"><template #default="{ row }">
          <div>名称：{{ row.解析字段?.项目名称 || '待填写' }}</div><div>批准号：{{ row.解析字段?.批准号 || '待填写' }}</div>
          <div>经费：{{ row.解析字段?.资助经费万元 ?? '待填写' }} 万元</div>
          <div v-for="(message, key) in row.校验结果" :key="key" class="hint">{{ key }}：{{ message }}</div>
        </template></el-table-column>
        <el-table-column label="操作" width="200"><template #default="{ row }">
          <el-button link type="primary" @click="handleDownload(row)">下载原件</el-button>
          <el-button link type="primary" :disabled="restoring || uploading" :loading="restoring && restoreTarget?.编号 === row.编号" @click="chooseOriginal(row)">补传原件</el-button>
          <el-button v-if="row.处理状态 === '待处理' && row.解析状态 !== '解析失败' && batchEditable" link type="primary" @click="openReview(row)">核对字段</el-button>
          <el-button v-if="row.处理状态 === '待处理' && batchEditable" link type="warning" @click="handleIgnore(row)">忽略</el-button>
        </template></el-table-column>
      </el-table>
    </el-drawer>

    <el-dialog v-model="reviewVisible" title="对照原计划书核对字段" width="780px">
      <el-alert title="提取结果仅供核对。空缺内容请按原件补齐；请按工号确认真实负责人，金额单位为万元。" type="info" :closable="false" />
      <el-form :model="reviewFields" label-width="115px" class="review-form">
        <el-form-item label="项目名称" required><el-input v-model="reviewFields.项目名称" maxlength="100" /></el-form-item>
        <el-form-item label="批准号" required><el-input v-model="reviewFields.批准号" maxlength="50" /></el-form-item>
        <el-form-item label="真实负责人" required>
          <el-select v-model="reviewFields.负责人编号" filterable remote :remote-method="searchOwners" :loading="ownersLoading" placeholder="输入姓名或工号" style="width:100%" @change="selectOwner">
            <el-option v-for="person in owners" :key="person.编号" :label="`${person.姓名}（${person.工号}）`" :value="person.编号" />
          </el-select>
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12"><el-form-item label="立项年度" required><el-input-number v-model="reviewFields.年度" :min="1900" :max="2200" :precision="0" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="经费（万元）" required><el-input-number v-model="reviewFields.资助经费万元" :min="0" :precision="6" /></el-form-item></el-col>
        </el-row>
        <el-form-item label="批准日期" required><el-date-picker v-model="reviewFields.批准日期" type="date" value-format="YYYY-MM-DD" /></el-form-item>
        <el-form-item label="开始日期" required><el-date-picker v-model="reviewFields.开始日期" type="date" value-format="YYYY-MM-DD" /></el-form-item>
        <el-form-item label="结束日期" required><el-date-picker v-model="reviewFields.结束日期" type="date" value-format="YYYY-MM-DD" /></el-form-item>
        <el-form-item label="分类" required><el-select v-model="reviewFields.分类"><el-option label="科研项目" :value="1" /><el-option label="重点学科" :value="2" /><el-option label="人才培养" :value="3" /></el-select></el-form-item>
        <el-row :gutter="16"><el-col v-for="field in classificationFields" :key="field" :span="12"><el-form-item :label="field" required><el-input v-model="reviewFields[field]" maxlength="50" /></el-form-item></el-col></el-row>
        <el-checkbox v-model="reviewChecked">我已对照原件核实以上字段，确认金额单位、负责人和日期正确。</el-checkbox>
      </el-form>
      <template #footer><el-button @click="reviewVisible = false">取消</el-button><el-button type="primary" :loading="reviewSaving" :disabled="!reviewChecked" @click="saveReview">保存核对结果</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getImportBatches, getImportDetails, createImportBatch, confirmBatchImport, uploadProposalFiles, restoreProposalOriginal, reviewProposalFields, searchProposalOwners, downloadProposal, ignoreProposal, type ProposalFields } from '@/api/vertical-import'
import { PROPOSAL_UPLOAD_LIMIT_HINT, validateProposalUploadFiles } from '@/utils/proposal-upload'

const batches = ref<any[]>([])
const details = ref<any[]>([])
const currentBatch = ref<any>(null)
const drawerVisible = ref(false)
const fileInput = ref<HTMLInputElement>()
const restoreInput = ref<HTMLInputElement>()
const restoreTarget = ref<{ 编号: number; 源文件名: string } | null>(null)
const restoring = ref(false)
const uploading = ref(false)
const confirming = ref(false)
const createVisible = ref(false)
const creating = ref(false)
const batchName = ref('')
const batchSource = ref('国家自然科学基金')
const batchEditable = computed(() => currentBatch.value && !['已导入','已关闭'].includes(currentBatch.value.状态))
const reviewVisible = ref(false)
const reviewSaving = ref(false)
const reviewChecked = ref(false)
const reviewId = ref(0)
const reviewFields = ref<ProposalFields>({})
const owners = ref<Array<{ 编号: number; 姓名: string; 工号: string; 部门编号: number }>>([])
const ownersLoading = ref(false)
const classificationFields = ['学科门类','学科类型','研究类型','项目类型','项目级别','项目来源单位'] as const

function objectValue(value: any) { if (typeof value !== 'string') return value || {}; try { return JSON.parse(value) } catch { return {} } }
async function loadBatches() {
  try { batches.value = await getImportBatches() || []; if (currentBatch.value) currentBatch.value = batches.value.find(b => b.编号 === currentBatch.value.编号) || currentBatch.value }
  catch (err: any) { ElMessage.error(err.message || '加载批次失败') }
}
async function refreshDetails() {
  if (!currentBatch.value) return
  details.value = (await getImportDetails(currentBatch.value.编号) || []).map(row => ({ ...row, 解析字段: objectValue(row.解析字段), 校验结果: objectValue(row.校验结果) }))
}
async function handleCreateBatch() {
  if (!batchName.value.trim() || creating.value) return
  creating.value = true
  try { const result = await createImportBatch({ 批次名称: batchName.value.trim(), 来源类型: batchSource.value }); createVisible.value = false; await loadBatches(); await handleViewDetails(batches.value.find(b => b.编号 === result.编号)); ElMessage.success('批次已创建，请上传 PDF') }
  catch (err: any) { ElMessage.error(err.message || '创建失败') } finally { creating.value = false }
}
async function handleViewDetails(row: any) {
  if (!row) return
  currentBatch.value = row
  try { await refreshDetails(); drawerVisible.value = true } catch (err: any) { ElMessage.error(err.message || '加载明细失败') }
}
async function handleUpload(event: Event) {
  const input = event.target as HTMLInputElement
  const files = Array.from(input.files || [])
  input.value = ''
  if (!files.length || uploading.value) return
  const error = validateProposalUploadFiles(files)
  if (error) { ElMessage.warning(error); return }
  uploading.value = true
  try { const result = await uploadProposalFiles(currentBatch.value.编号, files); await refreshDetails(); await loadBatches(); ElMessage.success(`新增 ${result.新增文件数} 份，${result.重复文件数} 份重复文件已保留原核对结果并更新原件路径`) }
  catch (err: any) { ElMessage.error(err.message || '上传失败') } finally { uploading.value = false }
}
function chooseOriginal(row: { 编号: number; 源文件名: string }) {
  if (restoring.value || uploading.value) return
  restoreTarget.value = { 编号: row.编号, 源文件名: row.源文件名 }
  restoreInput.value?.click()
}
async function handleRestoreOriginal(event: Event) {
  const input = event.target as HTMLInputElement
  const files = Array.from(input.files || [])
  input.value = ''
  const target = restoreTarget.value
  if (!files.length || !target || restoring.value) return
  const error = validateProposalUploadFiles(files, 'restore')
  if (error) { ElMessage.warning(error); return }
  restoring.value = true
  try {
    await restoreProposalOriginal(target.编号, files[0])
    await refreshDetails()
    ElMessage.success(`“${target.源文件名}”原件已补传，核对结果和导入状态保持不变`)
  } catch (err: any) { ElMessage.error(err.response?.data?.error || err.message || '补传失败，请选择首次上传的原 PDF') }
  finally { restoring.value = false; restoreTarget.value = null }
}
async function searchOwners(keyword: string) {
  if (!keyword.trim()) { owners.value = []; return }
  ownersLoading.value = true
  try { owners.value = await searchProposalOwners(keyword) } catch { ElMessage.error('负责人查询失败') } finally { ownersLoading.value = false }
}
function selectOwner(id: number) { const person = owners.value.find(p => p.编号 === id); if (person) reviewFields.value.负责人 = person.姓名 }
async function openReview(row: any) {
  reviewId.value = row.编号
  reviewFields.value = { ...row.解析字段, 年度: row.解析字段.年度 || undefined }
  for (const field of ['批准日期','开始日期','结束日期'] as const) if (reviewFields.value[field]) reviewFields.value[field] = reviewFields.value[field]!.slice(0, 10)
  reviewChecked.value = false
  reviewVisible.value = true
  await searchOwners(reviewFields.value.负责人 || '')
}
async function saveReview() {
  if (!reviewChecked.value || reviewSaving.value) return
  reviewSaving.value = true
  try {
    const result = await reviewProposalFields(currentBatch.value.编号, reviewId.value, reviewFields.value, reviewChecked.value)
    await refreshDetails(); await loadBatches()
    if (result.通过) { reviewVisible.value = false; ElMessage.success('人工核对通过') }
    else ElMessage.error(Object.entries(result.校验结果).filter(([key]) => !['人工复核','复核人编号','复核时间'].includes(key)).map(([, value]) => value).join('；'))
  } catch (err: any) { ElMessage.error(err.message || '保存失败') } finally { reviewSaving.value = false }
}
async function handleDownload(row: any) {
  try { const blob = await downloadProposal(row.编号); const url = URL.createObjectURL(blob); const anchor = document.createElement('a'); anchor.href = url; anchor.download = row.源文件名; anchor.click(); setTimeout(() => URL.revokeObjectURL(url), 1000) }
  catch { ElMessage.error('无法下载原件，请确认文件仍存在') }
}
async function handleIgnore(row: any) {
  try { await ElMessageBox.confirm(`忽略“${row.源文件名}”后，此文件不会生成项目。`, '确认忽略', { type: 'warning' }); await ignoreProposal(currentBatch.value.编号, row.编号); await refreshDetails(); await loadBatches() }
  catch (err: any) { if (err !== 'cancel' && err !== 'close') ElMessage.error(err.message || '操作失败') }
}
async function handleConfirmImport(row: any) {
  if (confirming.value) return
  try { await ElMessageBox.confirm('将按人工核对后的原件信息登记立项。任何待处理文件未通过核对时，整批不会导入。', '确认批量立项', { type: 'warning' }); confirming.value = true; const result = await confirmBatchImport(row.编号); await loadBatches(); await refreshDetails(); ElMessage.success(`本次导入 ${result.成功导入项目数} 项`) }
  catch (err: any) { if (err !== 'cancel' && err !== 'close') ElMessage.error(err.message || '确认失败') } finally { confirming.value = false }
}
onMounted(loadBatches)
</script>

<style scoped>
.vertical-import-container { padding: 16px; }
.card-header, .toolbar { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
.card-header { justify-content: space-between; font-weight: 600; }
.toolbar { margin: 16px 0; }
.hint { color: #606266; font-size: 12px; overflow-wrap: anywhere; }
.review-form { margin-top: 16px; }
.restore-hint { margin-bottom: 16px; }
</style>
