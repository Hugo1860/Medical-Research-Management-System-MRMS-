<template>
  <el-dialog
    :model-value="modelValue"
    title="自定义左侧菜单显示顺序"
    width="680px"
    class="menu-sort-dialog"
    destroy-on-close
    :append-to-body="true"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div class="sort-tips-bar">
      <el-alert
        type="info"
        :closable="false"
        show-icon
      >
        <template #title>
          <span style="font-weight: 600;">个人专属偏好设置</span>
        </template>
        <template #default>
          <span>通过鼠标拖拽或使用【⬆上移】/【⬇下移】/【🔝置顶】按钮调整排序，顺序将自动保存在当前浏览器本地，随开即用。</span>
        </template>
      </el-alert>
    </div>

    <el-tabs v-model="activeTab" class="sort-tabs">
      <!-- Tab 1: 一级业务模块排序 -->
      <el-tab-pane label="一级业务模块排序" name="root">
        <div class="sort-pane-header">
          <span class="pane-title">当前一级模块（共 {{ rootItems.length }} 个）</span>
          <span class="pane-hint">拖拽把手或点击箭头微调位置</span>
        </div>

        <div class="sort-list-container">
          <div
            v-for="(item, index) in rootItems"
            :key="item.name"
            class="sort-list-item"
            :class="{ 'is-dragging': dragIndex === index, 'is-drag-over': dragOverIndex === index }"
            draggable="true"
            @dragstart="onDragStart(index)"
            @dragover.prevent="onDragOver(index)"
            @dragleave="onDragLeave"
            @drop="onDrop(index)"
            @dragend="onDragEnd"
          >
            <div class="item-left">
              <span class="drag-handle" title="按住拖拽排序">
                <el-icon><Rank /></el-icon>
              </span>
              <span class="item-index">{{ index + 1 }}</span>
              <el-icon class="item-icon"><component :is="getIcon(item.name)" /></el-icon>
              <span class="item-name">{{ item.name }}</span>
              <el-tag v-if="item.subCount" size="small" type="info" effect="plain" class="count-tag">
                {{ item.subCount }} 个子功能
              </el-tag>
            </div>

            <div class="item-actions">
              <el-button
                link
                type="primary"
                size="small"
                :disabled="index === 0"
                title="置顶"
                @click="moveToTop(index)"
              >
                <el-icon><Top /></el-icon>
                <span>置顶</span>
              </el-button>
              <el-button
                link
                type="primary"
                size="small"
                :disabled="index === 0"
                title="上移"
                @click="moveUp(index)"
              >
                <el-icon><ArrowUp /></el-icon>
                <span>上移</span>
              </el-button>
              <el-button
                link
                type="primary"
                size="small"
                :disabled="index === rootItems.length - 1"
                title="下移"
                @click="moveDown(index)"
              >
                <el-icon><ArrowDown /></el-icon>
                <span>下移</span>
              </el-button>
              <el-button
                link
                type="primary"
                size="small"
                :disabled="index === rootItems.length - 1"
                title="置底"
                @click="moveToBottom(index)"
              >
                <el-icon><Bottom /></el-icon>
                <span>置底</span>
              </el-button>
            </div>
          </div>
        </div>
      </el-tab-pane>

      <!-- Tab 2: 二级功能菜单排序 -->
      <el-tab-pane label="二级功能菜单排序" name="sub">
        <div class="sub-filter-row">
          <span class="filter-label">选择父级模块：</span>
          <el-select v-model="selectedParentName" placeholder="请选择模块" style="width: 240px;">
            <el-option
              v-for="item in rootItemsWithChildren"
              :key="item.name"
              :label="item.name + ` (${item.subCount}项)`"
              :value="item.name"
            />
          </el-select>
        </div>

        <div v-if="currentSubItems.length > 0" class="sort-list-container" style="margin-top: 14px;">
          <div
            v-for="(sub, subIdx) in currentSubItems"
            :key="sub.name"
            class="sort-list-item"
            :class="{ 'is-dragging': subDragIndex === subIdx, 'is-drag-over': subDragOverIndex === subIdx }"
            draggable="true"
            @dragstart="onSubDragStart(subIdx)"
            @dragover.prevent="onSubDragOver(subIdx)"
            @dragleave="onSubDragLeave"
            @drop="onSubDrop(subIdx)"
            @dragend="onSubDragEnd"
          >
            <div class="item-left">
              <span class="drag-handle" title="按住拖拽排序">
                <el-icon><Rank /></el-icon>
              </span>
              <span class="item-index">{{ subIdx + 1 }}</span>
              <el-icon class="item-icon" style="color: #64748b;"><Document /></el-icon>
              <span class="item-name">{{ sub.name }}</span>
            </div>

            <div class="item-actions">
              <el-button
                link
                type="primary"
                size="small"
                :disabled="subIdx === 0"
                title="置顶"
                @click="moveSubToTop(subIdx)"
              >
                <el-icon><Top /></el-icon>
                <span>置顶</span>
              </el-button>
              <el-button
                link
                type="primary"
                size="small"
                :disabled="subIdx === 0"
                title="上移"
                @click="moveSubUp(subIdx)"
              >
                <el-icon><ArrowUp /></el-icon>
                <span>上移</span>
              </el-button>
              <el-button
                link
                type="primary"
                size="small"
                :disabled="subIdx === currentSubItems.length - 1"
                title="下移"
                @click="moveSubDown(subIdx)"
              >
                <el-icon><ArrowDown /></el-icon>
                <span>下移</span>
              </el-button>
              <el-button
                link
                type="primary"
                size="small"
                :disabled="subIdx === currentSubItems.length - 1"
                title="置底"
                @click="moveSubToBottom(subIdx)"
              >
                <el-icon><Bottom /></el-icon>
                <span>置底</span>
              </el-button>
            </div>
          </div>
        </div>

        <el-empty
          v-else
          description="该模块下暂无可排序的二级菜单"
          :image-size="80"
          style="padding: 24px 0;"
        />
      </el-tab-pane>
    </el-tabs>

    <template #footer>
      <div class="dialog-footer">
        <el-button type="danger" plain @click="handleReset">
          <el-icon style="margin-right: 4px;"><Refresh /></el-icon>
          恢复系统默认排序
        </el-button>
        <div class="footer-right">
          <el-button @click="emit('update:modelValue', false)">取 消</el-button>
          <el-button type="primary" @click="handleSave">
            <el-icon style="margin-right: 4px;"><Check /></el-icon>
            保存并生效
          </el-button>
        </div>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useMenuOrderStore, type MenuOrderConfig } from '@/stores/menuOrder'
import type { PermissionNode } from '@/api/auth'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  Rank,
  Top,
  Bottom,
  ArrowUp,
  ArrowDown,
  Refresh,
  Check,
  Document,
  TrendCharts,
  Collection,
  Histogram,
  Money,
  Reading,
  Files,
  User,
  Setting,
  House
} from '@element-plus/icons-vue'

const props = defineProps<{
  modelValue: boolean
  rawPermissions: PermissionNode[]
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', val: boolean): void
  (e: 'order-changed'): void
}>()

const menuOrderStore = useMenuOrderStore()

const activeTab = ref<'root' | 'sub'>('root')
const selectedParentName = ref('')

interface SortItem {
  name: string
  subCount: number
  children?: SortItem[]
}

const rootItems = ref<SortItem[]>([])
const subItemsMap = ref<Record<string, SortItem[]>>({})

// 拖拽辅助状态
const dragIndex = ref<number | null>(null)
const dragOverIndex = ref<number | null>(null)

const subDragIndex = ref<number | null>(null)
const subDragOverIndex = ref<number | null>(null)

function getIcon(name: string) {
  if (name.includes('办公') || name.includes('工作台')) return House
  if (name.includes('纵向')) return TrendCharts
  if (name.includes('横向')) return Collection
  if (name.includes('经费')) return Money
  if (name.includes('成果') || name.includes('论文')) return Reading
  if (name.includes('学术') || name.includes('会议')) return Reading
  if (name.includes('统计') || name.includes('大屏')) return Histogram
  if (name.includes('组织') || name.includes('人员')) return User
  if (name.includes('系统') || name.includes('配置')) return Setting
  return Files
}

const rootItemsWithChildren = computed(() => {
  return rootItems.value.filter((item) => (subItemsMap.value[item.name]?.length || 0) > 0)
})

const currentSubItems = computed(() => {
  if (!selectedParentName.value) return []
  return subItemsMap.value[selectedParentName.value] || []
})

// 初始化待排序列表
function initSortData() {
  const currentOrders = menuOrderStore.customOrderMap
  const rootOrder = currentOrders['_root'] || []

  // 1. 提取所有一级模块与二级菜单
  const roots: SortItem[] = []
  const subMap: Record<string, SortItem[]> = {}

  for (const node of props.rawPermissions) {
    const parentName = node.菜单?.名称
    if (!parentName) continue

    const subNodes = node.子级菜单 || []
    const subList: SortItem[] = subNodes.map((s) => ({
      name: s.菜单?.名称 || '',
      subCount: (s.子级菜单 || []).length
    })).filter((s) => Boolean(s.name))

    // 如果该父级有自定义二级排序
    const customSubOrder = currentOrders[parentName]
    if (customSubOrder && customSubOrder.length > 0) {
      const rankMap = new Map<string, number>()
      customSubOrder.forEach((name, idx) => rankMap.set(name, idx))
      subList.sort((a, b) => {
        const rankA = rankMap.has(a.name) ? rankMap.get(a.name)! : 9999
        const rankB = rankMap.has(b.name) ? rankMap.get(b.name)! : 9999
        return rankA - rankB
      })
    }

    subMap[parentName] = subList

    roots.push({
      name: parentName,
      subCount: subList.length
    })
  }

  // 2. 对一级模块应用已存的自定义顺序
  if (rootOrder && rootOrder.length > 0) {
    const rankMap = new Map<string, number>()
    rootOrder.forEach((name, idx) => rankMap.set(name, idx))
    roots.sort((a, b) => {
      const rankA = rankMap.has(a.name) ? rankMap.get(a.name)! : 9999
      const rankB = rankMap.has(b.name) ? rankMap.get(b.name)! : 9999
      return rankA - rankB
    })
  }

  rootItems.value = roots
  subItemsMap.value = subMap

  if (rootItemsWithChildren.value.length > 0 && !selectedParentName.value) {
    selectedParentName.value = rootItemsWithChildren.value[0].name
  }
}

watch(
  () => props.modelValue,
  (val) => {
    if (val) {
      initSortData()
    }
  }
)

// === 一级拖拽交互 ===
function onDragStart(idx: number) {
  dragIndex.value = idx
}

function onDragOver(idx: number) {
  dragOverIndex.value = idx
}

function onDragLeave() {
  dragOverIndex.value = null
}

function onDrop(dropIdx: number) {
  if (dragIndex.value === null || dragIndex.value === dropIdx) {
    dragOverIndex.value = null
    return
  }
  const item = rootItems.value.splice(dragIndex.value, 1)[0]
  rootItems.value.splice(dropIdx, 0, item)
  dragIndex.value = null
  dragOverIndex.value = null
}

function onDragEnd() {
  dragIndex.value = null
  dragOverIndex.value = null
}

// === 一级按钮移动 ===
function moveUp(idx: number) {
  if (idx <= 0) return
  const item = rootItems.value.splice(idx, 1)[0]
  rootItems.value.splice(idx - 1, 0, item)
}

function moveDown(idx: number) {
  if (idx >= rootItems.value.length - 1) return
  const item = rootItems.value.splice(idx, 1)[0]
  rootItems.value.splice(idx + 1, 0, item)
}

function moveToTop(idx: number) {
  if (idx <= 0) return
  const item = rootItems.value.splice(idx, 1)[0]
  rootItems.value.unshift(item)
}

function moveToBottom(idx: number) {
  if (idx >= rootItems.value.length - 1) return
  const item = rootItems.value.splice(idx, 1)[0]
  rootItems.value.push(item)
}

// === 二级拖拽交互 ===
function onSubDragStart(idx: number) {
  subDragIndex.value = idx
}

function onSubDragOver(idx: number) {
  subDragOverIndex.value = idx
}

function onSubDragLeave() {
  subDragOverIndex.value = null
}

function onSubDrop(dropIdx: number) {
  if (subDragIndex.value === null || subDragIndex.value === dropIdx || !selectedParentName.value) {
    subDragOverIndex.value = null
    return
  }
  const list = subItemsMap.value[selectedParentName.value]
  const item = list.splice(subDragIndex.value, 1)[0]
  list.splice(dropIdx, 0, item)
  subDragIndex.value = null
  subDragOverIndex.value = null
}

function onSubDragEnd() {
  subDragIndex.value = null
  subDragOverIndex.value = null
}

// === 二级按钮移动 ===
function moveSubUp(idx: number) {
  if (idx <= 0 || !selectedParentName.value) return
  const list = subItemsMap.value[selectedParentName.value]
  const item = list.splice(idx, 1)[0]
  list.splice(idx - 1, 0, item)
}

function moveSubDown(idx: number) {
  if (!selectedParentName.value) return
  const list = subItemsMap.value[selectedParentName.value]
  if (idx >= list.length - 1) return
  const item = list.splice(idx, 1)[0]
  list.splice(idx + 1, 0, item)
}

function moveSubToTop(idx: number) {
  if (idx <= 0 || !selectedParentName.value) return
  const list = subItemsMap.value[selectedParentName.value]
  const item = list.splice(idx, 1)[0]
  list.unshift(item)
}

function moveSubToBottom(idx: number) {
  if (!selectedParentName.value) return
  const list = subItemsMap.value[selectedParentName.value]
  if (idx >= list.length - 1) return
  const item = list.splice(idx, 1)[0]
  list.push(item)
}

// === 保存与恢复 ===
function handleSave() {
  const newConfig: MenuOrderConfig = {
    _root: rootItems.value.map((i) => i.name)
  }

  for (const parent of Object.keys(subItemsMap.value)) {
    const list = subItemsMap.value[parent]
    if (list && list.length > 0) {
      newConfig[parent] = list.map((i) => i.name)
    }
  }

  menuOrderStore.saveAllOrders(newConfig)
  emit('order-changed')
  ElMessage.success('左侧菜单显示顺序已保存生效！')
  emit('update:modelValue', false)
}

function handleReset() {
  ElMessageBox.confirm('确定要恢复系统默认的菜单排列顺序吗？', '提示', {
    confirmButtonText: '确定恢复',
    cancelButtonText: '取消',
    type: 'warning'
  })
    .then(() => {
      menuOrderStore.resetAllOrders()
      initSortData()
      emit('order-changed')
      ElMessage.info('已恢复系统默认菜单顺序！')
      emit('update:modelValue', false)
    })
    .catch(() => {})
}
</script>

<style scoped lang="scss">
.menu-sort-dialog {
  :deep(.el-dialog__body) {
    padding: 16px 24px;
  }

  .sort-tips-bar {
    margin-bottom: 16px;
  }

  .sort-tabs {
    .sort-pane-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;

      .pane-title {
        font-size: 14px;
        font-weight: 600;
        color: #1e293b;
      }

      .pane-hint {
        font-size: 12px;
        color: #64748b;
      }
    }

    .sub-filter-row {
      display: flex;
      align-items: center;
      gap: 10px;
      margin-bottom: 10px;

      .filter-label {
        font-size: 13px;
        font-weight: 500;
        color: #334155;
      }
    }

    .sort-list-container {
      max-height: 400px;
      overflow-y: auto;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      background: #f8fafc;
      padding: 6px;

      .sort-list-item {
        display: flex;
        justify-content: space-between;
        align-items: center;
        background: #ffffff;
        border: 1px solid #e2e8f0;
        border-radius: 6px;
        padding: 9px 14px;
        margin-bottom: 6px;
        transition: all 0.2s ease;
        user-select: none;

        &:last-child {
          margin-bottom: 0;
        }

        &:hover {
          border-color: #93c5fd;
          box-shadow: 0 2px 6px rgba(0, 0, 0, 0.05);
        }

        &.is-dragging {
          opacity: 0.4;
          background: #eff6ff;
          border-style: dashed;
        }

        &.is-drag-over {
          border-color: #2563eb;
          background: #eff6ff;
          transform: translateY(2px);
        }

        .item-left {
          display: flex;
          align-items: center;
          gap: 10px;

          .drag-handle {
            cursor: grab;
            color: #94a3b8;
            display: inline-flex;
            align-items: center;
            padding: 4px;

            &:hover {
              color: #2563eb;
            }

            &:active {
              cursor: grabbing;
            }
          }

          .item-index {
            font-size: 12px;
            font-weight: 600;
            color: #64748b;
            background: #f1f5f9;
            width: 22px;
            height: 22px;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            border-radius: 50%;
          }

          .item-icon {
            font-size: 17px;
            color: #3b82f6;
          }

          .item-name {
            font-size: 14px;
            font-weight: 500;
            color: #0f172a;
          }

          .count-tag {
            font-size: 11px;
            margin-left: 4px;
          }
        }

        .item-actions {
          display: flex;
          align-items: center;
          gap: 4px;

          .el-button {
            padding: 4px 6px;
            font-size: 12px;

            .el-icon {
              margin-right: 2px;
            }
          }
        }
      }
    }
  }

  .dialog-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    width: 100%;

    .footer-right {
      display: flex;
      gap: 10px;
    }
  }
}
</style>
