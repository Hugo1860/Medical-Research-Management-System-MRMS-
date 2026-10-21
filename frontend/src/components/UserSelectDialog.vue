<template>
  <el-dialog
    :model-value="modelValue"
    :title="title"
    width="820px"
    class="user-select-dialog"
    destroy-on-close
    append-to-body
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div v-loading="loading" class="user-select-body">
      <!-- 左栏：组织架构与候选人员 -->
      <div class="user-col candidate-col">
        <div class="col-header">
          <span class="col-title">可选人员</span>
          <el-tag size="small" type="info">共 {{ filteredCandidates.length }} 人</el-tag>
        </div>

        <div class="filter-box">
          <el-input
            v-model="keywordSearch"
            placeholder="搜索姓名 / 工号..."
            prefix-icon="Search"
            clearable
            size="small"
            style="margin-bottom: 8px;"
          />
          <el-select
            v-model="selectedDeptId"
            placeholder="按所属部门筛选"
            clearable
            size="small"
            style="width: 100%;"
            @change="handleDeptFilterChange"
          >
            <el-option label="全部部门" :value="0" />
            <el-option
              v-for="dept in departments"
              :key="dept.编号"
              :label="dept.名称"
              :value="dept.编号"
            />
          </el-select>
        </div>

        <div class="candidate-list-wrapper">
          <div class="batch-select-bar">
            <el-checkbox
              v-model="isAllCandidateChecked"
              :indeterminate="isCandidateIndeterminate"
              @change="handleCandidateCheckAll"
            >
              全选当前列表 ({{ candidateCheckList.length }}/{{ filteredCandidates.length }})
            </el-checkbox>
          </div>
          <el-scrollbar max-height="320px">
            <div v-if="filteredCandidates.length === 0" class="empty-tip">
              暂无符合条件的候选人员
            </div>
            <el-checkbox-group v-else v-model="candidateCheckList">
              <div
                v-for="u in filteredCandidates"
                :key="u.编号"
                class="user-check-item"
              >
                <el-checkbox :value="u.编号">
                  <span class="user-name">{{ u.姓名 }}</span>
                  <span class="user-sub">{{ u.工号 }} · {{ u.部门名称 || '未分配' }}</span>
                </el-checkbox>
              </div>
            </el-checkbox-group>
          </el-scrollbar>
        </div>
      </div>

      <!-- 中间操作按钮组 -->
      <div class="transfer-actions">
        <el-button
          type="primary"
          :icon="ArrowRight"
          :disabled="candidateCheckList.length === 0"
          circle
          @click="transferToSelected"
        />
        <el-button
          type="danger"
          :icon="ArrowLeft"
          :disabled="selectedCheckList.length === 0"
          circle
          @click="transferFromSelected"
        />
      </div>

      <!-- 右栏：已选人员清单 -->
      <div class="user-col selected-col">
        <div class="col-header">
          <span class="col-title">已选人员</span>
          <div class="header-right">
            <el-tag size="small" type="success">已选 {{ selectedUsers.length }} 人</el-tag>
            <el-button
              v-if="selectedUsers.length > 0"
              link
              type="danger"
              size="small"
              style="margin-left: 8px;"
              @click="clearAllSelected"
            >
              清空
            </el-button>
          </div>
        </div>

        <div class="selected-list-wrapper">
          <div v-if="selectedUsers.length > 0" class="batch-select-bar">
            <el-checkbox
              v-model="isAllSelectedChecked"
              :indeterminate="isSelectedIndeterminate"
              @change="handleSelectedCheckAll"
            >
              全选已选 ({{ selectedCheckList.length }}/{{ selectedUsers.length }})
            </el-checkbox>
          </div>
          <el-scrollbar max-height="380px">
            <div v-if="selectedUsers.length === 0" class="empty-tip">
              尚未选择任何人员，请从左侧勾选添加
            </div>
            <el-checkbox-group v-else v-model="selectedCheckList">
              <div
                v-for="u in selectedUsers"
                :key="u.编号"
                class="user-check-item selected-item"
              >
                <el-checkbox :value="u.编号">
                  <span class="user-name">{{ u.姓名 }}</span>
                  <span class="user-sub">{{ u.工号 }} · {{ u.部门名称 || '未分配' }}</span>
                </el-checkbox>
                <el-button
                  link
                  type="danger"
                  :icon="Close"
                  size="small"
                  @click.stop="removeSingleSelected(u.编号)"
                />
              </div>
            </el-checkbox-group>
          </el-scrollbar>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="handleCancel">取消</el-button>
        <el-button type="primary" :disabled="selectedUsers.length === 0" @click="handleConfirm">
          确定选择 ({{ selectedUsers.length }})
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import {
  getDepartmentList,
  getPeopleList,
  type Department,
  type PersonItem
} from '@/api/organization'
import { ArrowRight, ArrowLeft, Close } from '@element-plus/icons-vue'

const props = withDefaults(
  defineProps<{
    modelValue: boolean
    title?: string
    multiple?: boolean
    initialUsers?: PersonItem[]
    initialUserIds?: number[]
  }>(),
  {
    title: '选择科研人员',
    multiple: true,
    initialUsers: () => [],
    initialUserIds: () => []
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', visible: boolean): void
  (e: 'confirm', users: PersonItem[]): void
}>()

const loading = ref(false)
const keywordSearch = ref('')
const selectedDeptId = ref<number>(0)
const departments = ref<Department[]>([])
const allUsers = ref<PersonItem[]>([])

const candidateCheckList = ref<number[]>([])
const selectedUsers = ref<PersonItem[]>([])
const selectedCheckList = ref<number[]>([])

// 过滤候选人员（排除已选择的人员，且匹配部门与关键字）
const filteredCandidates = computed(() => {
  const selectedIds = new Set(selectedUsers.value.map(u => u.编号))
  return allUsers.value.filter(u => {
    if (selectedIds.has(u.编号)) return false
    if (selectedDeptId.value && selectedDeptId.value > 0 && u.部门编号 !== selectedDeptId.value) {
      return false
    }
    if (keywordSearch.value.trim()) {
      const kw = keywordSearch.value.trim().toLowerCase()
      const matchName = u.姓名?.toLowerCase().includes(kw)
      const matchGonghao = u.工号?.toLowerCase().includes(kw)
      const matchDept = u.部门名称?.toLowerCase().includes(kw)
      return matchName || matchGonghao || matchDept
    }
    return true
  })
})

// 候选人全选状态
const isAllCandidateChecked = computed({
  get: () => {
    if (filteredCandidates.value.length === 0) return false
    return candidateCheckList.value.length === filteredCandidates.value.length
  },
  set: (val: boolean) => {
    if (val) {
      candidateCheckList.value = filteredCandidates.value.map(u => u.编号)
    } else {
      candidateCheckList.value = []
    }
  }
})

const isCandidateIndeterminate = computed(() => {
  const total = filteredCandidates.value.length
  const checked = candidateCheckList.value.length
  return checked > 0 && checked < total
})

function handleCandidateCheckAll(val: boolean | string | number) {
  isAllCandidateChecked.value = Boolean(val)
}

// 已选人员全选状态
const isAllSelectedChecked = computed({
  get: () => {
    if (selectedUsers.value.length === 0) return false
    return selectedCheckList.value.length === selectedUsers.value.length
  },
  set: (val: boolean) => {
    if (val) {
      selectedCheckList.value = selectedUsers.value.map(u => u.编号)
    } else {
      selectedCheckList.value = []
    }
  }
})

const isSelectedIndeterminate = computed(() => {
  const total = selectedUsers.value.length
  const checked = selectedCheckList.value.length
  return checked > 0 && checked < total
})

function handleSelectedCheckAll(val: boolean | string | number) {
  isAllSelectedChecked.value = Boolean(val)
}

// 穿梭向右：加入已选
function transferToSelected() {
  if (candidateCheckList.value.length === 0) return
  const checkSet = new Set(candidateCheckList.value)
  const toAdd = allUsers.value.filter(u => checkSet.has(u.编号))

  if (!props.multiple) {
    selectedUsers.value = toAdd.slice(0, 1)
  } else {
    selectedUsers.value = [...selectedUsers.value, ...toAdd]
  }

  candidateCheckList.value = []
}

// 穿梭向左：移除已选
function transferFromSelected() {
  if (selectedCheckList.value.length === 0) return
  const checkSet = new Set(selectedCheckList.value)
  selectedUsers.value = selectedUsers.value.filter(u => !checkSet.has(u.编号))
  selectedCheckList.value = []
}

function removeSingleSelected(id: number) {
  selectedUsers.value = selectedUsers.value.filter(u => u.编号 !== id)
  selectedCheckList.value = selectedCheckList.value.filter(itemId => itemId !== id)
}

function clearAllSelected() {
  selectedUsers.value = []
  selectedCheckList.value = []
}

function handleDeptFilterChange() {
  candidateCheckList.value = []
}

async function loadData() {
  loading.value = true
  try {
    const [deptList, peopleRes] = await Promise.all([
      getDepartmentList(),
      getPeopleList({ index: 1, size: 500 }, { 是否启用: true })
    ])
    departments.value = deptList || []
    allUsers.value = (peopleRes?.list || []).map(p => ({
      ...p,
      性别: typeof p.性别 === 'boolean' ? (p.性别 ? '男' : '女') : p.性别
    }))

    // 初始化已选
    if (props.initialUsers && props.initialUsers.length > 0) {
      selectedUsers.value = [...props.initialUsers]
    } else if (props.initialUserIds && props.initialUserIds.length > 0) {
      const idSet = new Set(props.initialUserIds)
      selectedUsers.value = allUsers.value.filter(u => idSet.has(u.编号))
    }
  } catch (err) {
    console.error('加载人员选择数据失败', err)
  } finally {
    loading.value = false
  }
}

watch(
  () => props.modelValue,
  (val) => {
    if (val) {
      candidateCheckList.value = []
      selectedCheckList.value = []
      loadData()
    }
  }
)

function handleCancel() {
  emit('update:modelValue', false)
}

function handleConfirm() {
  emit('confirm', [...selectedUsers.value])
  emit('update:modelValue', false)
}

onMounted(() => {
  if (props.modelValue) {
    loadData()
  }
})
</script>

<style scoped lang="scss">
.user-select-body {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  min-height: 440px;

  .user-col {
    flex: 1;
    border: 1px solid #e2e8f0;
    border-radius: 8px;
    padding: 12px;
    background-color: #f8fafc;
    display: flex;
    flex-direction: column;
    min-height: 440px;

    .col-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-bottom: 8px;
      border-bottom: 1px solid #e2e8f0;
      margin-bottom: 8px;

      .col-title {
        font-weight: 600;
        font-size: 14px;
        color: #1e293b;
      }
    }

    .filter-box {
      margin-bottom: 8px;
    }

    .batch-select-bar {
      padding: 6px 8px;
      background: #edf2f7;
      border-radius: 4px;
      margin-bottom: 6px;
      font-size: 12px;
    }

    .candidate-list-wrapper,
    .selected-list-wrapper {
      flex: 1;
      display: flex;
      flex-direction: column;
    }

    .user-check-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 6px 8px;
      border-radius: 4px;
      margin-bottom: 4px;
      background: #ffffff;
      border: 1px solid #edf2f7;
      transition: all 0.2s;

      &:hover {
        background: #f1f5f9;
        border-color: #cbd5e1;
      }

      .user-name {
        font-weight: 500;
        color: #1e293b;
        margin-right: 8px;
      }

      .user-sub {
        font-size: 12px;
        color: #64748b;
      }

      &.selected-item {
        background: #f0fdf4;
        border-color: #bbf7d0;
      }
    }

    .empty-tip {
      text-align: center;
      color: #94a3b8;
      font-size: 13px;
      padding: 40px 0;
    }
  }

  .transfer-actions {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }
}
</style>
