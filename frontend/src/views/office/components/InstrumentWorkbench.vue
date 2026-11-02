<template>
  <section class="instrument-workbench">
    <div class="toolbar">
      <el-select v-model="platformId" clearable placeholder="全部科研平台" aria-label="筛选科研平台" style="width: 250px" @change="refresh">
        <el-option v-for="p in platforms" :key="p.编号" :label="p.平台名称" :value="p.编号!" />
      </el-select>
      <el-button :loading="loading" @click="refresh">刷新仪器与年报</el-button>
      <el-button v-if="canManage" type="primary" data-testid="add-instrument" @click="openInstrument()">新增仪器</el-button>
    </div>
    <el-alert v-if="pageError" :title="pageError" type="error" :closable="false" show-icon />
    <el-alert v-if="notice" :title="notice" type="success" closable @close="notice = ''" />
    <el-table v-loading="loading" :data="instruments" border stripe empty-text="暂无仪器设备">
      <el-table-column prop="资产编号" label="资产编号" width="130" />
      <el-table-column prop="设备名称" label="设备名称" min-width="180" />
      <el-table-column prop="平台名称" label="所属平台" min-width="140" />
      <el-table-column prop="开放状态" label="开放状态" width="100" />
      <el-table-column label="内部收费" width="140"><template #default="{ row }">{{ row.计费方式 }} · {{ row.内部单价 }} 元</template></el-table-column>
      <el-table-column label="培训资质" width="95"><template #default="{ row }">{{ row.需培训资质 ? '需要' : '不要求' }}</template></el-table-column>
      <el-table-column label="操作" width="240" fixed="right"><template #default="{ row }">
        <el-button link type="primary" :data-testid="`book-${row.编号}`" :disabled="!['开放','仅内部'].includes(row.开放状态)" @click="openBooking(row)">预约</el-button>
        <el-button link type="primary" :data-testid="`bookings-${row.编号}`" @click="selectInstrument(row)">预约记录</el-button>
        <el-button v-if="canManage" link type="primary" @click="openInstrument(row)">编辑</el-button>
      </template></el-table-column>
    </el-table>

    <div v-if="selectedInstrument" class="section">
      <h3>{{ selectedInstrument.设备名称 }} · 预约记录</h3>
      <el-alert v-if="bookingError" :title="bookingError" type="error" :closable="false" />
      <el-table v-loading="loadingBookings" :data="bookings" border empty-text="暂无预约记录">
        <el-table-column prop="预约人姓名" label="预约人" width="110" />
        <el-table-column prop="开始时间" label="开始时间" min-width="175" />
        <el-table-column prop="结束时间" label="结束时间" min-width="175" />
        <el-table-column prop="状态" label="状态" width="100" />
        <el-table-column prop="用途说明" label="用途" min-width="150" show-overflow-tooltip />
        <el-table-column prop="审核意见" label="审核意见" min-width="150" show-overflow-tooltip />
        <el-table-column v-if="canManage" label="操作" width="140" fixed="right"><template #default="{ row }">
          <el-button v-if="row.状态 === '待审核'" link type="primary" :data-testid="`audit-${row.编号}`" @click="openAudit(row)">审核</el-button>
          <el-button v-if="['已批准','使用中'].includes(row.状态)" link type="primary" :data-testid="`usage-${row.编号}`" @click="openUsage(row)">登记使用与计费</el-button>
        </template></el-table-column>
      </el-table>
    </div>

    <div class="section">
      <h3>平台年报</h3>
      <p class="hint">选择科研平台后可查看年度快照。管理人员可按实际仪器与预约记录生成或更新年度草稿。</p>
      <div v-if="platformId && canManage" class="toolbar">
        <el-input-number v-model="reportYear" :min="2000" :max="2100" aria-label="统计年度" />
        <el-input v-model="reportSummary" placeholder="年度总结（可选）" maxlength="5000" style="max-width: 430px" />
        <el-button type="primary" :loading="saving" data-testid="generate-report" @click="generateReport">生成年度草稿</el-button>
      </div>
      <el-table v-if="platformId" :data="reports" border empty-text="暂无平台年报">
        <el-table-column prop="年度" label="年度" width="90" />
        <el-table-column prop="状态" label="状态" width="100" />
        <el-table-column label="统计快照" min-width="240"><template #default="{ row }">{{ describeSnapshot(row.统计快照) }}</template></el-table-column>
        <el-table-column prop="年度总结" label="年度总结" min-width="200" show-overflow-tooltip />
        <el-table-column prop="提交人姓名" label="提交人" width="110" />
      </el-table>
    </div>

    <el-dialog v-model="bookingVisible" :title="`预约 · ${bookingInstrument?.设备名称 || ''}`" width="min(620px, 94vw)" destroy-on-close>
      <el-alert v-if="dialogError" :title="dialogError" type="error" :closable="false" />
      <p class="hint">预约时长 {{ bookingInstrument?.最短预约分钟 }}—{{ bookingInstrument?.最长预约分钟 }} 分钟；时段冲突与培训资质由系统提交时核验。</p>
      <el-form label-width="100px">
        <el-form-item label="开始时间" required><el-date-picker v-model="bookingForm.开始时间" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" data-testid="booking-start" /></el-form-item>
        <el-form-item label="结束时间" required><el-date-picker v-model="bookingForm.结束时间" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" data-testid="booking-end" /></el-form-item>
        <el-form-item label="样品数" required><el-input-number v-model="bookingForm.样品数" :min="1" :precision="0" /></el-form-item>
        <el-form-item label="用途说明" required><el-input v-model="bookingForm.用途说明" type="textarea" maxlength="1000" data-testid="booking-purpose" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="bookingVisible = false">取消</el-button><el-button type="primary" :loading="saving" data-testid="submit-booking" @click="submitBooking">提交预约</el-button></template>
    </el-dialog>

    <el-dialog v-model="auditVisible" title="审核预约" width="min(560px, 94vw)" destroy-on-close>
      <el-alert v-if="dialogError" :title="dialogError" type="error" :closable="false" />
      <el-form label-width="90px">
        <el-form-item label="审核结果"><el-select v-model="auditForm.审核结果"><el-option label="批准" value="已批准" /><el-option label="拒绝" value="已拒绝" /></el-select></el-form-item>
        <el-form-item label="审核意见"><el-input v-model="auditForm.审核意见" type="textarea" maxlength="1000" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="auditVisible = false">取消</el-button><el-button type="primary" :loading="saving" data-testid="submit-audit" @click="submitAudit">确认审核</el-button></template>
    </el-dialog>

    <el-dialog v-model="usageVisible" title="登记使用与计费" width="min(620px, 94vw)" destroy-on-close>
      <el-alert v-if="dialogError" :title="dialogError" type="error" :closable="false" />
      <p class="hint">确认后按仪器内部单价核算并完成本次预约，金额以系统返回为准；这不会向财务系统发起支付。</p>
      <el-form label-width="110px">
        <el-form-item label="实际开始" required><el-date-picker v-model="usageForm.实际开始时间" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" data-testid="usage-start" /></el-form-item>
        <el-form-item label="实际结束" required><el-date-picker v-model="usageForm.实际结束时间" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" data-testid="usage-end" /></el-form-item>
        <el-form-item label="实际样品数" required><el-input-number v-model="usageForm.实际样品数" :min="1" :precision="0" data-testid="usage-samples" /></el-form-item>
        <el-form-item label="结算凭证号"><el-input v-model="usageForm.结算凭证号" maxlength="100" placeholder="已有真实凭证时填写" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="usageForm.备注" type="textarea" maxlength="1000" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="usageVisible = false">取消</el-button><el-button type="primary" :loading="saving" data-testid="submit-usage" @click="submitUsage">确认使用并核算</el-button></template>
    </el-dialog>

    <el-dialog v-model="instrumentVisible" :title="instrumentForm.编号 ? '编辑仪器' : '新增仪器'" width="min(660px, 94vw)" destroy-on-close>
      <el-alert v-if="dialogError" :title="dialogError" type="error" :closable="false" />
      <el-form label-width="120px">
        <el-form-item label="所属平台" required><el-select v-model="instrumentForm.平台编号"><el-option v-for="p in platforms" :key="p.编号" :label="p.平台名称" :value="p.编号!" /></el-select></el-form-item>
        <el-form-item label="资产编号" required><el-input v-model="instrumentForm.资产编号" maxlength="100" /></el-form-item>
        <el-form-item label="设备名称" required><el-input v-model="instrumentForm.设备名称" maxlength="200" /></el-form-item>
        <el-form-item label="型号规格"><el-input v-model="instrumentForm.型号规格" maxlength="200" /></el-form-item>
        <el-form-item label="存放地点"><el-input v-model="instrumentForm.存放地点" maxlength="200" /></el-form-item>
        <el-form-item label="开放状态"><el-select v-model="instrumentForm.开放状态"><el-option v-for="s in ['开放','仅内部','维修','停用']" :key="s" :label="s" :value="s" /></el-select></el-form-item>
        <el-form-item label="计费方式"><el-select v-model="instrumentForm.计费方式"><el-option v-for="s in ['按小时','按样品','按次','免费']" :key="s" :label="s" :value="s" /></el-select></el-form-item>
        <el-form-item label="内部单价（元）"><el-input-number v-model="instrumentForm.内部单价" :min="0" :precision="2" /></el-form-item>
        <el-form-item label="外部单价（元）"><el-input-number v-model="instrumentForm.外部单价" :min="0" :precision="2" /></el-form-item>
        <el-form-item label="最短预约（分）"><el-input-number v-model="instrumentForm.最短预约分钟" :min="1" :precision="0" /></el-form-item>
        <el-form-item label="最长预约（分）"><el-input-number v-model="instrumentForm.最长预约分钟" :min="instrumentForm.最短预约分钟" :precision="0" /></el-form-item>
        <el-form-item label="需培训资质"><el-switch v-model="instrumentForm.需培训资质" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="instrumentForm.备注" type="textarea" maxlength="1000" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="instrumentVisible = false">取消</el-button><el-button type="primary" :loading="saving" @click="submitInstrument">保存仪器</el-button></template>
    </el-dialog>
  </section>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import type { ResearchPlatform } from '@/api/research-platform'
import { getInstruments, saveInstrument, getInstrumentBookings, applyInstrumentBooking, auditInstrumentBooking, registerInstrumentUsage, getPlatformAnnualReports, generatePlatformAnnualReport, type Instrument, type InstrumentSave, type InstrumentBooking, type BookingApply, type UsageRegister, type AnnualReport } from '@/api/platform-instrument'

const props = defineProps<{ platforms: ResearchPlatform[]; canManage: boolean }>()
const platformId = ref<number>()
const instruments = ref<Instrument[]>([])
const bookings = ref<InstrumentBooking[]>([])
const reports = ref<AnnualReport[]>([])
const selectedInstrument = ref<Instrument>()
const bookingInstrument = ref<Instrument>()
const loading = ref(false), loadingBookings = ref(false), saving = ref(false)
const pageError = ref(''), bookingError = ref(''), dialogError = ref(''), notice = ref('')
const bookingVisible = ref(false), auditVisible = ref(false), usageVisible = ref(false), instrumentVisible = ref(false)
const bookingForm = reactive<BookingApply>({ 仪器编号: 0, 开始时间: '', 结束时间: '', 样品数: 1, 用途说明: '' })
const auditForm = reactive<{ 编号: number; 审核结果: '已批准' | '已拒绝'; 审核意见: string }>({ 编号: 0, 审核结果: '已批准', 审核意见: '' })
const usageForm = reactive<UsageRegister>({ 预约编号: 0, 实际开始时间: '', 实际结束时间: '', 实际样品数: 1, 结算凭证号: '', 备注: '' })
const instrumentForm = ref<InstrumentSave>(emptyInstrument())
const reportYear = ref(new Date().getFullYear()), reportSummary = ref('')
let refreshSequence = 0, bookingSequence = 0

function emptyInstrument(): InstrumentSave { return { 平台编号: platformId.value || 0, 资产编号: '', 设备名称: '', 开放状态: '开放', 计费方式: '按小时', 内部单价: 0, 外部单价: 0, 最短预约分钟: 30, 最长预约分钟: 480, 需培训资质: false } }
function errorText(error: any, fallback: string) { return String(error?.response?.data?.error || error?.message || fallback) }
function durationMinutes(start: string, end: string) { return (new Date(end).getTime() - new Date(start).getTime()) / 60000 }
function validSamples(count: number) { return Number.isInteger(count) && count >= 1 }
function describeSnapshot(value: AnnualReport['统计快照']) {
  try {
    const snapshot = typeof value === 'string' ? JSON.parse(value) : value
    return `仪器 ${snapshot?.仪器设备总数 ?? '—'} 台；成员 ${snapshot?.平台成员总数 ?? '—'} 人；预约 ${snapshot?.年度预约次数 ?? '—'} 次`
  } catch { return '统计快照暂不可读取' }
}
async function refresh() {
  const sequence = ++refreshSequence
  loading.value = true; pageError.value = ''
  selectedInstrument.value = undefined; bookings.value = []; ++bookingSequence
  const id = platformId.value || undefined
  try {
    const [items, annual] = await Promise.all([getInstruments(id), id ? getPlatformAnnualReports(id) : Promise.resolve([])])
    if (sequence !== refreshSequence) return
    instruments.value = items; reports.value = annual
  } catch (error) {
    if (sequence !== refreshSequence) return
    instruments.value = []; reports.value = []; pageError.value = errorText(error, '仪器数据读取失败，请重试')
  } finally { if (sequence === refreshSequence) loading.value = false }
}
async function selectInstrument(instrument: Instrument) {
  selectedInstrument.value = instrument; bookings.value = []; bookingError.value = ''; loadingBookings.value = true
  const sequence = ++bookingSequence
  try { const rows = await getInstrumentBookings(instrument.编号); if (sequence === bookingSequence) bookings.value = rows }
  catch (error) { if (sequence === bookingSequence) bookingError.value = errorText(error, '预约记录读取失败') }
  finally { if (sequence === bookingSequence) loadingBookings.value = false }
}
function openBooking(instrument: Instrument) {
  bookingInstrument.value = instrument; dialogError.value = ''
  Object.assign(bookingForm, { 仪器编号: instrument.编号, 开始时间: '', 结束时间: '', 样品数: 1, 用途说明: '' })
  bookingVisible.value = true
}
async function submitBooking() {
  if (saving.value) return
  const minutes = durationMinutes(bookingForm.开始时间, bookingForm.结束时间)
  const instrument = bookingInstrument.value
  if (!instrument || !Number.isFinite(minutes) || minutes <= 0) { dialogError.value = '请选择有效的开始和结束时间'; return }
  if (minutes < instrument.最短预约分钟 || minutes > instrument.最长预约分钟) { dialogError.value = '预约时长不在仪器允许范围内'; return }
  if (!validSamples(bookingForm.样品数) || !bookingForm.用途说明.trim()) { dialogError.value = '请填写正整数样品数和用途说明'; return }
  saving.value = true; dialogError.value = ''
  try {
    await applyInstrumentBooking({ ...bookingForm, 用途说明: bookingForm.用途说明.trim() })
    bookingVisible.value = false; notice.value = '预约已提交，等待审核'
    await selectInstrument(instrument)
  } catch (error) { dialogError.value = errorText(error, '预约提交失败') }
  finally { saving.value = false }
}
function openAudit(row: InstrumentBooking) { if (!props.canManage) return; Object.assign(auditForm, { 编号: row.编号, 审核结果: '已批准', 审核意见: '' }); dialogError.value = ''; auditVisible.value = true }
async function submitAudit() {
  if (!props.canManage || saving.value) return
  if (auditForm.审核结果 === '已拒绝' && !auditForm.审核意见.trim()) { dialogError.value = '请填写拒绝原因'; return }
  saving.value = true; dialogError.value = ''
  try { await auditInstrumentBooking({ ...auditForm }); auditVisible.value = false; notice.value = '预约审核已保存'; if (selectedInstrument.value) await selectInstrument(selectedInstrument.value) }
  catch (error) { dialogError.value = errorText(error, '审核失败') }
  finally { saving.value = false }
}
function openUsage(row: InstrumentBooking) {
  if (!props.canManage || !['已批准','使用中'].includes(row.状态)) return
  Object.assign(usageForm, { 预约编号: row.编号, 实际开始时间: row.开始时间, 实际结束时间: row.结束时间, 实际样品数: row.样品数, 结算凭证号: '', 备注: '' })
  dialogError.value = ''; usageVisible.value = true
}
async function submitUsage() {
  if (!props.canManage || saving.value) return
  const minutes = durationMinutes(usageForm.实际开始时间, usageForm.实际结束时间)
  if (!Number.isFinite(minutes) || minutes <= 0 || !validSamples(usageForm.实际样品数)) { dialogError.value = '请填写有效使用时段和正整数样品数'; return }
  saving.value = true; dialogError.value = ''
  try {
    const result = await registerInstrumentUsage({ ...usageForm })
    usageVisible.value = false; notice.value = `使用已登记，系统核算 ${result.机时小时} 小时，费用 ${result.计费金额} 元`
    if (selectedInstrument.value) await selectInstrument(selectedInstrument.value)
  } catch (error) { dialogError.value = errorText(error, '使用登记失败') }
  finally { saving.value = false }
}
function openInstrument(row?: Instrument) { if (!props.canManage) return; instrumentForm.value = row ? { ...row } : emptyInstrument(); dialogError.value = ''; instrumentVisible.value = true }
async function submitInstrument() {
  if (!props.canManage || saving.value) return
  const form = instrumentForm.value
  if (!form.平台编号 || !form.资产编号.trim() || !form.设备名称.trim()) { dialogError.value = '请填写平台、资产编号和设备名称'; return }
  if (!validSamples(form.最短预约分钟) || !validSamples(form.最长预约分钟) || form.最长预约分钟 < form.最短预约分钟 || form.内部单价 < 0 || form.外部单价 < 0) { dialogError.value = '请检查预约时长范围和单价'; return }
  saving.value = true; dialogError.value = ''
  try { await saveInstrument({ ...form }); instrumentVisible.value = false; notice.value = '仪器已保存'; await refresh() }
  catch (error) { dialogError.value = errorText(error, '仪器保存失败') }
  finally { saving.value = false }
}
async function generateReport() {
  if (!props.canManage || !platformId.value || saving.value) return
  if (!Number.isInteger(reportYear.value) || reportYear.value < 2000 || reportYear.value > 2100) { pageError.value = '请选择有效统计年度'; return }
  saving.value = true; pageError.value = ''
  try { await generatePlatformAnnualReport({ 平台编号: platformId.value, 年度: reportYear.value, 年度总结: reportSummary.value.trim() }); reports.value = await getPlatformAnnualReports(platformId.value); notice.value = '年度草稿已更新' }
  catch (error) { pageError.value = errorText(error, '年报生成失败') }
  finally { saving.value = false }
}
onMounted(refresh)
</script>

<style scoped>
.toolbar { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; margin-bottom: 16px; }
.section { margin-top: 28px; }
h3 { margin-bottom: 12px; font-size: 17px; }
.hint { color: #606266; line-height: 1.6; font-size: 13px; }
.el-alert { margin-bottom: 14px; }
</style>
