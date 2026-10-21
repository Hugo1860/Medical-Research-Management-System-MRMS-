<template>
  <el-dialog
    :model-value="modelValue"
    :title="title || (allowTermination ? '业务流程流转审核' : '业务流程流转审核（无终止）')"
    width="680px"
    class="workflow-approval-dialog"
    destroy-on-close
    append-to-body
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div v-if="task" v-loading="submitting" class="approval-dialog-content">
      <!-- 事项概览横幅 -->
      <div class="task-summary-banner">
        <div class="banner-row">
          <span class="label">事项名称：</span>
          <span class="value font-bold">{{ task.流程名称 }}</span>
        </div>
        <div class="banner-row">
          <span class="label">当前步骤：</span>
          <el-tag type="warning" size="small">{{ task.步骤名称 }}</el-tag>
          <span class="label ml-4">发起人：</span>
          <span class="value">{{ task.匿名评审 ? '【盲审安全脱敏】' : (task.发起人姓名 || '未知') }}</span>
          <span v-if="!task.匿名评审 && task.发起人部门名称" class="dept-text">
            ({{ task.发起人部门名称 }})
          </span>
        </div>
        <div v-if="task.流程相关项目路径" class="banner-row">
          <span class="label">申报材料：</span>
          <el-button link type="primary" size="small" @click="showPreview = !showPreview">
            {{ showPreview ? '收起申报预览' : '查看申报材料详情' }}
          </el-button>
        </div>
      </div>

      <!-- 嵌入式申报材料详情预览 -->
      <el-collapse-transition>
        <div v-if="showPreview && task.流程相关项目路径" class="preview-box">
          <iframe
            :src="formatPreviewUrl(task.流程相关项目路径)"
            class="preview-iframe"
            frameborder="0"
          />
        </div>
      </el-collapse-transition>

      <!-- 专家回避与利益冲突安全预检提示 -->
      <div v-if="conflictCheckLoading" class="conflict-loading">
        <el-icon class="is-loading"><Loading /></el-icon>
        <span>正在执行利益冲突预检查...</span>
      </div>
      <div v-else-if="conflictResult" class="conflict-alert-box">
        <el-alert
          :title="conflictResult.存在利益冲突 ? '⚠️ 检测到潜在利益冲突警告' : '✅ 利益冲突预检查通过：未发现关联申报回避项'"
          :type="conflictResult.存在利益冲突 ? 'error' : 'success'"
          :description="conflictResult.冲突说明 || (conflictResult.存在利益冲突 ? '专家与申报人处于相同课题组或同一科室，依评审规范请进行回避。' : '申报合规，专家可依法依规出具评审结论。')"
          show-icon
          :closable="false"
        />
      </div>

      <!-- 审核表单 -->
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="95px"
        class="approval-form"
      >
        <el-form-item label="审批结论" prop="状态值" required>
          <el-radio-group v-model="form.状态值">
            <el-radio-button :value="1">
              <el-icon><Check /></el-icon> 审核通过
            </el-radio-button>
            <el-radio-button :value="-1">
              <el-icon><Close /></el-icon> 不通过/退回
            </el-radio-button>
            <el-radio-button v-if="allowTermination" :value="-2">
              <el-icon><Delete /></el-icon> 终止流程
            </el-radio-button>
          </el-radio-group>
        </el-form-item>

        <el-form-item label="快捷评语">
          <div class="preset-phrases">
            <el-tag
              v-for="phrase in currentPresets"
              :key="phrase"
              class="phrase-tag"
              effect="plain"
              @click="appendPreset(phrase)"
            >
              {{ phrase }}
            </el-tag>
          </div>
        </el-form-item>

        <el-form-item label="审核说明" prop="备注" :required="form.状态值 !== 1">
          <el-input
            v-model="form.备注"
            type="textarea"
            :rows="4"
            placeholder="请在此输入具体的审核意见、修改指导建议或终止理由..."
            maxlength="1000"
            show-word-limit
          />
        </el-form-item>
      </el-form>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button :disabled="submitting" @click="emit('update:modelValue', false)">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmit">
          提交审核意见
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch, computed } from 'vue'
import {
  auditWorkflowStep,
  terminateWorkflowStep,
  checkExpertConflict,
  type WorkflowTaskItem,
  type ExpertConflictResult
} from '@/api/workflow'
import { ElMessage, type FormInstance } from 'element-plus'
import { Check, Close, Delete, Loading } from '@element-plus/icons-vue'

const props = withDefaults(
  defineProps<{
    modelValue: boolean
    task: WorkflowTaskItem | null
    title?: string
    allowTermination?: boolean
  }>(),
  {
    allowTermination: true
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', visible: boolean): void
  (e: 'success', task: WorkflowTaskItem): void
}>()

const formRef = ref<FormInstance>()
const submitting = ref(false)
const showPreview = ref(false)
const conflictCheckLoading = ref(false)
const conflictResult = ref<ExpertConflictResult | null>(null)

const form = reactive({
  状态值: 1,
  备注: ''
})

const rules = {
  状态值: [{ required: true, message: '请选择审批结论', trigger: 'change' }],
  备注: [
    {
      validator: (_rule: any, value: string, callback: any) => {
        if (form.状态值 !== 1 && !value?.trim()) {
          callback(new Error('不通过或终止时，必须填写具体审核说明'))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
  ]
}

const passPresets = [
  '同意申报，符合医院科研立项指南要求。',
  '材料完整详实，专家评分达标，准予通过。',
  '预算编制合理，科研处复核同意推进下一阶段。'
]

const rejectPresets = [
  '材料不齐全，缺少伦理委员会审批批文，请补充后重新提交。',
  '项目研究周期与计划进度不匹配，请退回课题组修正。',
  '经费预算编制依据不足，设备购置费超标，请调整后重申。'
]

const terminatePresets = [
  '研究内容严重偏离申报指南，予以终止。',
  '存在学术诚信或重复资助情形，流程作终止处理。'
]

const currentPresets = computed(() => {
  if (form.状态值 === 1) return passPresets
  if (form.状态值 === -1) return rejectPresets
  return terminatePresets
})

function appendPreset(phrase: string) {
  if (form.备注) {
    form.备注 += ' ' + phrase
  } else {
    form.备注 = phrase
  }
}

function formatPreviewUrl(path: string): string {
  if (path.startsWith('http')) return path
  if (path.startsWith('/')) return path
  return `/IMIS/views/${path}`
}

async function doConflictCheck(stepId: number) {
  conflictCheckLoading.value = true
  conflictResult.value = null
  try {
    const res = await checkExpertConflict(stepId)
    conflictResult.value = res
  } catch {
    conflictResult.value = null
  } finally {
    conflictCheckLoading.value = false
  }
}

watch(
  () => props.modelValue,
  (val) => {
    if (val && props.task) {
      form.状态值 = 1
      form.备注 = ''
      showPreview.value = false
      if (props.task.步骤编号) {
        doConflictCheck(props.task.步骤编号)
      }
    }
  }
)

async function handleSubmit() {
  const currentTask = props.task
  if (!formRef.value || !currentTask) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitting.value = true
    try {
      const payload = {
        步骤编号: currentTask.步骤编号,
        状态值: form.状态值,
        备注: form.备注.trim()
      }

      if (form.状态值 === -2) {
        await terminateWorkflowStep(payload)
      } else {
        await auditWorkflowStep(payload)
      }

      ElMessage.success('审核结论提交成功')
      emit('success', currentTask)
      emit('update:modelValue', false)
    } catch (err: any) {
      ElMessage.error(err.response?.data?.error || err.message || '审核提交失败')
    } finally {
      submitting.value = false
    }
  })
}
</script>

<style scoped lang="scss">
.approval-dialog-content {
  .task-summary-banner {
    background-color: #f8fafc;
    border: 1px solid #e2e8f0;
    border-radius: 8px;
    padding: 12px 16px;
    margin-bottom: 16px;

    .banner-row {
      display: flex;
      align-items: center;
      margin-bottom: 6px;
      font-size: 13px;

      &:last-child {
        margin-bottom: 0;
      }

      .label {
        color: #64748b;
        width: 75px;
        flex-shrink: 0;
      }

      .value {
        color: #1e293b;

        &.font-bold {
          font-weight: 600;
        }
      }

      .ml-4 {
        margin-left: 16px;
      }

      .dept-text {
        color: #94a3b8;
        margin-left: 6px;
      }
    }
  }

  .preview-box {
    margin-bottom: 16px;
    border: 1px solid #cbd5e1;
    border-radius: 6px;
    overflow: hidden;

    .preview-iframe {
      width: 100%;
      height: 280px;
      border: none;
    }
  }

  .conflict-loading {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 13px;
    color: #64748b;
    margin-bottom: 12px;
  }

  .conflict-alert-box {
    margin-bottom: 16px;
  }

  .approval-form {
    margin-top: 8px;

    .preset-phrases {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;

      .phrase-tag {
        cursor: pointer;
        transition: all 0.2s;

        &:hover {
          color: #409eff;
          border-color: #b3d8ff;
          background-color: #ecf5ff;
        }
      }
    }
  }
}
</style>
