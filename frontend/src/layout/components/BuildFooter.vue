<template>
  <footer class="build-footer" aria-label="构建信息">
    <span>医院科研管理系统</span>
    <span class="separator">·</span>
    <span>版本 {{ buildVersion }}</span>
    <span class="separator">·</span>
    <span>构建于 {{ buildTime }}</span>
  </footer>
</template>

<script setup lang="ts">
import packageJson from '../../../package.json'

function safeBuildValue(value: unknown, fallback: string): string {
  if (typeof value !== 'string') return fallback
  const normalized = value.trim()
  return normalized && normalized.toLowerCase() !== 'undefined' && normalized.toLowerCase() !== 'null'
    ? normalized
    : fallback
}

const buildVersion = safeBuildValue(import.meta.env.VITE_BUILD_VERSION || packageJson.version, 'unknown')
const buildTime = safeBuildValue(import.meta.env.VITE_BUILD_TIME || __BUILD_TIME__, 'unknown')
</script>

<style scoped lang="scss">
.build-footer {
  flex: 0 0 auto;
  min-height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 6px 16px;
  color: #8b98a8;
  background: #f7f9fc;
  border-top: 1px solid #e8edf3;
  font-size: 12px;
  line-height: 1.4;
}

.separator {
  color: #c5ced8;
}
</style>
