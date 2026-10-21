<template>
  <div class="error-page-container">
    <div class="error-card">
      <div class="error-illustration">
        <div class="code-badge">404</div>
        <el-icon class="icon-search"><Search /></el-icon>
      </div>

      <div class="error-content">
        <h1 class="error-title">科研资源或页面未找到</h1>
        <p class="error-desc">
          抱歉，您访问的科研业务模块路径或电子档案资源不存在，可能已在系统升级中重构迁移或归档。
        </p>

        <div v-if="requestedPath" class="path-box">
          <span class="path-lbl">尝试请求的路径：</span>
          <code class="path-code">{{ requestedPath }}</code>
        </div>

        <div class="action-buttons">
          <el-button type="primary" :icon="House" size="large" @click="goHome">
            返回科研工作台
          </el-button>
          <el-button :icon="Back" size="large" @click="goBack">
            返回上一页
          </el-button>
        </div>

        <div class="contact-info">
          <span>如遇业务疑问，请联系科研处管理办公室 (分机: 8888-6666 · Email: research@hospital.org)</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { House, Back, Search } from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()

const requestedPath = computed(() => {
  return route.fullPath !== '/error/404' ? route.fullPath : ''
})

function goHome() {
  router.push('/dashboard')
}

function goBack() {
  if (window.history.length > 1) {
    router.back()
  } else {
    router.push('/dashboard')
  }
}
</script>

<style scoped lang="scss">
.error-page-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: calc(100vh - 120px);
  padding: 24px;
  background-color: #f8fafc;

  .error-card {
    max-width: 600px;
    width: 100%;
    background: #ffffff;
    border-radius: 12px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.05);
    border: 1px solid #e2e8f0;
    padding: 48px 36px;
    text-align: center;

    .error-illustration {
      position: relative;
      display: inline-block;
      margin-bottom: 24px;

      .code-badge {
        font-size: 76px;
        font-weight: 800;
        color: #e2e8f0;
        line-height: 1;
        letter-spacing: 4px;
      }

      .icon-search {
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        font-size: 38px;
        color: #409eff;
      }
    }

    .error-title {
      font-size: 22px;
      font-weight: 700;
      color: #1e293b;
      margin-bottom: 12px;
    }

    .error-desc {
      font-size: 14px;
      color: #64748b;
      line-height: 1.6;
      margin-bottom: 20px;
    }

    .path-box {
      background: #f1f5f9;
      padding: 8px 12px;
      border-radius: 6px;
      font-size: 12px;
      margin-bottom: 28px;
      display: inline-block;

      .path-lbl {
        color: #64748b;
      }

      .path-code {
        color: #0284c7;
        font-weight: 600;
      }
    }

    .action-buttons {
      display: flex;
      justify-content: center;
      gap: 16px;
      margin-bottom: 32px;
    }

    .contact-info {
      font-size: 12px;
      color: #94a3b8;
      border-top: 1px solid #f1f5f9;
      padding-top: 16px;
    }
  }
}
</style>
