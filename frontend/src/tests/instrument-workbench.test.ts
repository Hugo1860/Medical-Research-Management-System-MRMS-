import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { defineComponent } from 'vue'
import ElementPlus from 'element-plus'
import InstrumentWorkbench from '../views/office/components/InstrumentWorkbench.vue'
import * as api from '../api/platform-instrument'

vi.mock('../api/platform-instrument', () => ({
  getInstruments: vi.fn(), saveInstrument: vi.fn(), getInstrumentBookings: vi.fn(),
  applyInstrumentBooking: vi.fn(), auditInstrumentBooking: vi.fn(), registerInstrumentUsage: vi.fn(),
  getPlatformAnnualReports: vi.fn(), generatePlatformAnnualReport: vi.fn()
}))
const instrument: api.Instrument = { 编号: 8, 平台编号: 3, 资产编号: 'LAB-008', 设备名称: '共聚焦显微镜', 平台名称: '中心实验室', 开放状态: '开放', 计费方式: '按小时', 内部单价: 120, 外部单价: 200, 最短预约分钟: 30, 最长预约分钟: 480, 需培训资质: false }
const booking: api.InstrumentBooking = { 编号: 15, 仪器编号: 8, 预约人姓名: '验收人员', 开始时间: '2026-11-01T09:00:00', 结束时间: '2026-11-01T10:00:00', 样品数: 2, 用途说明: '样品检测', 状态: '已批准' }
const datePicker = defineComponent({ props: ['modelValue'], emits: ['update:modelValue'], template: '<input :value="modelValue" @input="$emit(\'update:modelValue\', $event.target.value)" />' })
const numberInput = defineComponent({ props: ['modelValue'], emits: ['update:modelValue'], template: '<input type="number" :value="modelValue" @input="$emit(\'update:modelValue\', Number($event.target.value))" />' })
const select = defineComponent({ props: ['modelValue'], emits: ['update:modelValue', 'change'], template: '<select :value="modelValue" @change="$emit(\'update:modelValue\', Number($event.target.value) || $event.target.value); $emit(\'change\')"><slot /></select>' })
const option = defineComponent({ props: ['value', 'label'], template: '<option :value="value">{{ label }}</option>' })
const dialog = defineComponent({ props: ['modelValue'], template: '<section v-if="modelValue"><slot /><slot name="footer" /></section>' })
async function render(canManage = false) {
  const wrapper = mount(InstrumentWorkbench, {
    props: { platforms: [{ 编号: 3, 平台名称: '中心实验室' }], canManage },
    global: { plugins: [ElementPlus], stubs: { ElDialog: dialog, ElDatePicker: datePicker, ElInputNumber: numberInput, ElSelect: select, ElOption: option } }
  })
  await flushPromises()
  return wrapper
}
beforeEach(() => {
  vi.resetAllMocks()
  vi.mocked(api.getInstruments).mockResolvedValue([instrument])
  vi.mocked(api.getInstrumentBookings).mockResolvedValue([booking])
  vi.mocked(api.getPlatformAnnualReports).mockResolvedValue([])
  vi.mocked(api.applyInstrumentBooking).mockResolvedValue({ 编号: 16, 状态: '待审核' })
  vi.mocked(api.auditInstrumentBooking).mockResolvedValue({ 编号: 15, 状态: '已批准' })
  vi.mocked(api.registerInstrumentUsage).mockResolvedValue({ 编号: 17, 机时小时: 1.25, 计费金额: 150 })
  vi.mocked(api.generatePlatformAnnualReport).mockResolvedValue({ 编号: 18, 统计: {} })
})
describe('仪器工作台实际组件交互', () => {
  it('显示服务端仪器数据，普通用户看不到管理操作', async () => {
    const wrapper = await render()
    expect(api.getInstruments).toHaveBeenCalledWith(undefined)
    expect(wrapper.text()).toContain('共聚焦显微镜')
    expect(wrapper.find('[data-testid="add-instrument"]').exists()).toBe(false)
    await wrapper.get('[data-testid="bookings-8"]').trigger('click')
    await flushPromises()
    expect(api.getInstrumentBookings).toHaveBeenCalledWith(8)
    expect(wrapper.text()).toContain('验收人员')
    expect(wrapper.find('[data-testid="usage-15"]').exists()).toBe(false)
    wrapper.unmount()
  })
  it('实际预约表单检查时段并将填写内容提交到 API', async () => {
    const wrapper = await render()
    await wrapper.get('[data-testid="book-8"]').trigger('click')
    await wrapper.get('[data-testid="submit-booking"]').trigger('click')
    expect(api.applyInstrumentBooking).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('请选择有效的开始和结束时间')
    await wrapper.get('[data-testid="booking-start"]').setValue('2026-11-01T09:00:00')
    await wrapper.get('[data-testid="booking-end"]').setValue('2026-11-01T09:10:00')
    await wrapper.get('[data-testid="submit-booking"]').trigger('click')
    expect(api.applyInstrumentBooking).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('预约时长不在仪器允许范围内')
    await wrapper.get('[data-testid="booking-end"]').setValue('2026-11-01T10:00:00')
    await wrapper.get('[data-testid="booking-purpose"]').setValue('  检测科研样品  ')
    await wrapper.get('[data-testid="submit-booking"]').trigger('click')
    await flushPromises()
    expect(api.applyInstrumentBooking).toHaveBeenCalledWith({ 仪器编号: 8, 开始时间: '2026-11-01T09:00:00', 结束时间: '2026-11-01T10:00:00', 样品数: 1, 用途说明: '检测科研样品' })
    expect(wrapper.find('[data-testid="submit-booking"]').exists()).toBe(false)
    expect(wrapper.text()).toContain('预约已提交，等待审核')
    wrapper.unmount()
  })
  it('服务端拒绝冲突预约时保留表单与错误，不显示成功', async () => {
    vi.mocked(api.applyInstrumentBooking).mockRejectedValue({ response: { data: { error: '该时段已被其他预约占用' } } })
    const wrapper = await render()
    await wrapper.get('[data-testid="book-8"]').trigger('click')
    await wrapper.get('[data-testid="booking-start"]').setValue('2026-11-01T09:00:00')
    await wrapper.get('[data-testid="booking-end"]').setValue('2026-11-01T10:00:00')
    await wrapper.get('[data-testid="booking-purpose"]').setValue('样品检测')
    await wrapper.get('[data-testid="submit-booking"]').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('该时段已被其他预约占用')
    expect(wrapper.find('[data-testid="submit-booking"]').exists()).toBe(true)
    expect(wrapper.text()).not.toContain('预约已提交，等待审核')
    wrapper.unmount()
  })
  it('仅为已批准预约开放使用登记，拒绝负样品并显示服务端核算金额', async () => {
    vi.mocked(api.getInstrumentBookings).mockResolvedValue([booking, { ...booking, 编号: 19, 状态: '已拒绝' }])
    const wrapper = await render(true)
    await wrapper.get('[data-testid="bookings-8"]').trigger('click')
    await flushPromises()
    expect(wrapper.find('[data-testid="usage-19"]').exists()).toBe(false)
    await wrapper.get('[data-testid="usage-15"]').trigger('click')
    await wrapper.get('[data-testid="usage-samples"]').setValue('-1')
    await wrapper.get('[data-testid="submit-usage"]').trigger('click')
    expect(api.registerInstrumentUsage).not.toHaveBeenCalled()
    await wrapper.get('[data-testid="usage-samples"]').setValue('2')
    await wrapper.get('[data-testid="submit-usage"]').trigger('click')
    await flushPromises()
    expect(api.registerInstrumentUsage).toHaveBeenCalledWith(expect.objectContaining({ 预约编号: 15, 实际样品数: 2 }))
    expect(wrapper.text()).toContain('系统核算 1.25 小时，费用 150 元')
    wrapper.unmount()
  })
  it('管理人员通过实际审核弹窗提交待审核预约并刷新记录', async () => {
    vi.mocked(api.getInstrumentBookings).mockResolvedValue([{ ...booking, 状态: '待审核' }])
    const wrapper = await render(true)
    await wrapper.get('[data-testid="bookings-8"]').trigger('click')
    await flushPromises()
    expect(wrapper.find('[data-testid="usage-15"]').exists()).toBe(false)
    await wrapper.get('[data-testid="audit-15"]').trigger('click')
    await wrapper.get('[data-testid="submit-audit"]').trigger('click')
    await flushPromises()
    expect(api.auditInstrumentBooking).toHaveBeenCalledWith({ 编号: 15, 审核结果: '已批准', 审核意见: '' })
    expect(api.getInstrumentBookings).toHaveBeenCalledTimes(2)
    expect(wrapper.text()).toContain('预约审核已保存')
    wrapper.unmount()
  })
  it('选择平台后生成年度草稿并重新读取年报', async () => {
    const wrapper = await render(true)
    await wrapper.get('select[aria-label="筛选科研平台"]').setValue('3')
    await flushPromises()
    vi.mocked(api.getPlatformAnnualReports).mockResolvedValue([{ 编号: 18, 年度: new Date().getFullYear(), 状态: '草稿', 统计快照: { 仪器设备总数: 1, 平台成员总数: 2, 年度预约次数: 3 } }])
    await wrapper.get('[data-testid="generate-report"]').trigger('click')
    await flushPromises()
    expect(api.generatePlatformAnnualReport).toHaveBeenCalledWith({ 平台编号: 3, 年度: new Date().getFullYear(), 年度总结: '' })
    expect(wrapper.text()).toContain('仪器 1 台；成员 2 人；预约 3 次')
    wrapper.unmount()
  })
  it('加载失败显示错误且清空数据，不以空表表示成功', async () => {
    vi.mocked(api.getInstruments).mockRejectedValue(new Error('服务暂不可用'))
    const wrapper = await render()
    expect(wrapper.text()).toContain('服务暂不可用')
    expect(wrapper.text()).not.toContain('共聚焦显微镜')
    wrapper.unmount()
  })
})
