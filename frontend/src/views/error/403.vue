<template>
  <div class="error-page-container">
    <div class="error-card">
      <div class="error-illustration">
        <div class="code-badge">403</div>
        <el-icon class="icon-lock"><Lock /></el-icon>
      </div>

      <div class="error-content">
        <h1 class="error-title">科研业务访问权限不足</h1>
        <p class="error-desc">
          抱歉，根据医院科研信息管理与网络安全等保三级控制规程，您当前所持角色尚未获批访问此模块或操作此敏感业务数据。
        </p>

        <div class="user-info-box">
          <span class="info-item">当前用户：<strong>{{ userStore.userInfo?.姓名 || '科研用户' }}</strong></span>
          <span class="info-item">所属部门：<strong>{{ userStore.userInfo?.部门名称 || '未分配' }}</strong></span>
          <span class="info-item">工号：<strong>{{ userStore.userInfo?.工号 || '-' }}</strong></span>
        </div>

        <div class="action-buttons">
          <el-button type="primary" :icon="House" size="large" @click="goHome">
            返回科研工作台
          </el-button>
          <el-button type="warning" :icon="Key" size="large" @click="goProxy">
            申请权限代理
          </el-button>
          <el-button :icon="Back" size="large" @click="goBack">
            返回上一页
          </el-button>
        </div>

        <div class="contact-info">
          <span>如需开通对应科研业务权限，请联系本科室科研秘书或科研处管理办公室进行授权审批。</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { House, Back, Lock, Key } from '@element-plus/icons-vue'

const router = useRouter()
const userStore = useUserStore()

function goHome() {
  router.push('/dashboard')
}

function goProxy() {
  router.push('/governance/proxy-reconciliation')
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
    max-width: 620px;
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
        color: #fecdd3;
        line-height: 1;
        letter-spacing: 4px;
      }

      .icon-lock {
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        font-size: 38px;
        color: #e11d48;
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

    .user-info-box {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      padding: 10px 16px;
      border-radius: 6px;
      font-size: 13px;
      margin-bottom: 28px;
      display: flex;
      justify-content: space-around;
      color: #475569;

      strong {
        color: #0f172a;
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
