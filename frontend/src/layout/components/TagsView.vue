<template>
  <div class="tags-view-container">
    <!-- 左滑按钮 -->
    <button
      type="button"
      class="scroll-nav-btn left"
      title="向左滚动标签栏"
      @click="scrollByOffset(-240)"
    >
      <el-icon><ArrowLeft /></el-icon>
    </button>

    <!-- 横向不换行标签滚动区 -->
    <el-scrollbar
      ref="scrollbarRef"
      class="tags-scrollbar"
      @wheel.prevent="onWheelScroll"
    >
      <div ref="tagsWrapperRef" class="tags-wrapper">
        <router-link
          v-for="tab in tabsStore.tabs"
          :key="tab.path"
          :to="tab.path"
          :data-path="tab.path"
          :title="tab.title"
          class="tag-item"
          :class="{ active: isActive(tab.path) }"
        >
          <span v-if="isActive(tab.path)" class="active-dot" />
          <span class="tag-title">{{ tab.title }}</span>
          <el-icon
            v-if="tab.path !== '/dashboard'"
            class="close-icon"
            title="关闭此标签"
            @click.prevent.stop="onCloseTab(tab.path)"
          >
            <Close />
          </el-icon>
        </router-link>
      </div>
    </el-scrollbar>

    <!-- 右滑按钮 -->
    <button
      type="button"
      class="scroll-nav-btn right"
      title="向右滚动标签栏"
      @click="scrollByOffset(240)"
    >
      <el-icon><ArrowRight /></el-icon>
    </button>

    <!-- 标签页快捷管理与溢出列表 -->
    <div class="tags-actions">
      <el-dropdown trigger="click" max-height="360px" @command="handleAction">
        <button type="button" class="action-trigger-btn" title="标签页管理与快速切换">
          <span class="tab-count-badge">{{ tabsStore.tabs.length }}</span>
          <el-icon><ArrowDown /></el-icon>
        </button>
        <template #dropdown>
          <el-dropdown-menu class="tags-dropdown-menu">
            <el-dropdown-item
              command="closeCurrent"
              :disabled="route.fullPath === '/dashboard'"
            >
              关闭当前标签
            </el-dropdown-item>
            <el-dropdown-item command="closeOthers">
              关闭其他标签
            </el-dropdown-item>
            <el-dropdown-item command="closeAll">
              全部关闭 ({{ tabsStore.tabs.length }})
            </el-dropdown-item>

            <el-dropdown-item
              v-for="tab in tabsStore.tabs"
              :key="'jump-' + tab.path"
              :divided="tab.path === '/dashboard'"
              :command="'jump:' + tab.path"
            >
              <div class="dropdown-tab-row">
                <span
                  class="dropdown-dot"
                  :class="{ active: isActive(tab.path) }"
                />
                <span
                  class="dropdown-tab-title"
                  :class="{ active: isActive(tab.path) }"
                >
                  {{ tab.title }}
                </span>
              </div>
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, nextTick, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useTabsStore } from '@/stores/tabs'
import { Close, ArrowDown, ArrowLeft, ArrowRight } from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const tabsStore = useTabsStore()

const scrollbarRef = ref<any>(null)
const tagsWrapperRef = ref<HTMLElement | null>(null)

function isActive(path: string) {
  return route.fullPath === path
}

function getScrollWrap(): HTMLElement | null {
  return scrollbarRef.value?.wrapRef || null
}

function scrollByOffset(offset: number) {
  const wrap = getScrollWrap()
  if (!wrap) return
  wrap.scrollTo({
    left: wrap.scrollLeft + offset,
    behavior: 'smooth'
  })
}

function onWheelScroll(e: WheelEvent) {
  const wrap = getScrollWrap()
  if (!wrap) return
  const delta = Math.abs(e.deltaX) > Math.abs(e.deltaY) ? e.deltaX : e.deltaY
  wrap.scrollLeft += delta
}

function scrollToActiveTag() {
  nextTick(() => {
    const wrap = getScrollWrap()
    const wrapper = tagsWrapperRef.value
    if (!wrap || !wrapper) return
    const activeEl = wrapper.querySelector('.tag-item.active') as HTMLElement | null
    if (!activeEl) return

    const activeLeft = activeEl.offsetLeft
    const activeRight = activeLeft + activeEl.offsetWidth
    const viewLeft = wrap.scrollLeft
    const viewRight = viewLeft + wrap.clientWidth

    if (activeLeft < viewLeft + 12) {
      wrap.scrollTo({ left: Math.max(0, activeLeft - 16), behavior: 'smooth' })
    } else if (activeRight > viewRight - 12) {
      wrap.scrollTo({
        left: activeRight - wrap.clientWidth + 24,
        behavior: 'smooth'
      })
    }
  })
}

function onCloseTab(targetPath: string) {
  const next = tabsStore.closeTab(targetPath)
  if (next && next !== route.fullPath) {
    router.push(next)
  }
}

function handleAction(command: string) {
  if (command === 'closeCurrent') {
    if (route.fullPath !== '/dashboard') {
      onCloseTab(route.fullPath)
    }
  } else if (command === 'closeOthers') {
    tabsStore.closeOthers(route.fullPath)
    scrollToActiveTag()
  } else if (command === 'closeAll') {
    tabsStore.closeAll()
    router.push('/dashboard')
    scrollToActiveTag()
  } else if (command.startsWith('jump:')) {
    const targetPath = command.slice(5)
    if (targetPath && targetPath !== route.fullPath) {
      router.push(targetPath)
    } else {
      scrollToActiveTag()
    }
  }
}

watch(
  () => [route.fullPath, tabsStore.tabs.length],
  () => {
    scrollToActiveTag()
  }
)

onMounted(() => {
  scrollToActiveTag()
})
</script>

<style scoped lang="scss">
.tags-view-container {
  height: 36px;
  width: 100%;
  background: #ffffff;
  border-bottom: 1px solid #d8dce5;
  box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.05);
  display: flex;
  align-items: center;
  flex-wrap: nowrap;
  overflow: hidden;
  user-select: none;

  .scroll-nav-btn {
    width: 24px;
    height: 100%;
    flex-shrink: 0;
    border: none;
    background: #f8fafc;
    color: #64748b;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    transition: all 0.15s;

    &.left {
      border-right: 1px solid #e2e8f0;
    }

    &.right {
      border-left: 1px solid #e2e8f0;
    }

    &:hover {
      background: #eff6ff;
      color: #2563eb;
    }
  }

  .tags-scrollbar {
    flex: 1;
    min-width: 0;
    overflow: hidden;
    white-space: nowrap;

    :deep(.el-scrollbar__wrap) {
      overflow-y: hidden;
      display: flex;
      align-items: center;
    }

    :deep(.el-scrollbar__view) {
      display: inline-flex;
      align-items: center;
      min-width: max-content;
    }

    .tags-wrapper {
      display: inline-flex;
      flex-wrap: nowrap;
      align-items: center;
      padding: 0 8px;
      gap: 6px;
      min-width: max-content;
      height: 34px;
    }

    .tag-item {
      display: inline-flex;
      align-items: center;
      flex-shrink: 0;
      white-space: nowrap;
      height: 26px;
      line-height: 26px;
      max-width: 220px;
      border: 1px solid #d8dce5;
      color: #495060;
      background: #ffffff;
      padding: 0 9px;
      font-size: 12px;
      border-radius: 4px;
      cursor: pointer;
      text-decoration: none;
      transition: all 0.2s;

      &:hover {
        background-color: #f1f5f9;
        border-color: #cbd5e1;
      }

      &.active {
        background-color: #2563eb;
        color: #ffffff;
        border-color: #2563eb;
        font-weight: 500;

        .close-icon:hover {
          background-color: rgba(255, 255, 255, 0.28);
          color: #ffffff;
        }
      }

      .active-dot {
        width: 6px;
        height: 6px;
        border-radius: 50%;
        background: #ffffff;
        margin-right: 5px;
        flex-shrink: 0;
      }

      .tag-title {
        display: inline-block;
        max-width: 172px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        margin-right: 4px;
        vertical-align: middle;
      }

      .close-icon {
        flex-shrink: 0;
        border-radius: 50%;
        font-size: 11px;
        padding: 2px;
        transition: all 0.2s;

        &:hover {
          background-color: #cbd5e1;
          color: #1e293b;
        }
      }
    }
  }

  .tags-actions {
    padding: 0 8px;
    border-left: 1px solid #e2e8f0;
    flex-shrink: 0;
    height: 100%;
    display: flex;
    align-items: center;

    .action-trigger-btn {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      height: 26px;
      padding: 0 8px;
      border: 1px solid #e2e8f0;
      border-radius: 4px;
      background: #f8fafc;
      color: #475569;
      font-size: 12px;
      cursor: pointer;
      white-space: nowrap;
      transition: all 0.15s;

      &:hover {
        background: #eff6ff;
        border-color: #bfdbfe;
        color: #2563eb;
      }

      .tab-count-badge {
        background: #e2e8f0;
        color: #334155;
        font-size: 11px;
        font-weight: 600;
        padding: 0 5px;
        border-radius: 10px;
        line-height: 16px;
      }
    }
  }
}

.dropdown-tab-row {
  display: flex;
  align-items: center;
  gap: 6px;
  max-width: 240px;
  white-space: nowrap;

  .dropdown-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: #cbd5e1;
    flex-shrink: 0;

    &.active {
      background: #2563eb;
    }
  }

  .dropdown-tab-title {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    font-size: 12px;

    &.active {
      color: #2563eb;
      font-weight: 600;
    }
  }
}
</style>
