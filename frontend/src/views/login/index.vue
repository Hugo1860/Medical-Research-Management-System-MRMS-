<template>
  <div class="login-wrapper">
    <div class="login-box">
      <div class="login-header">
        <div class="logo-circle">
          <el-icon :size="32" color="#409eff"><Management /></el-icon>
        </div>
        <h1 class="system-title">科研信息管理系统</h1>
        <p class="system-sub">Scientific Research Information Management System</p>
      </div>

      <!-- Keycloak 统一身份登录模式 -->
      <div v-if="oidcConfig?.enabled" class="oidc-login-section">
        <el-alert
          title="系统已开启 Keycloak 统一身份认证"
          type="info"
          :closable="false"
          show-icon
          style="margin-bottom: 20px;"
        >
          <template #default>
            <span>统一认证服务：{{ oidcConfig.authority }}</span>
          </template>
        </el-alert>

        <el-button
          type="primary"
          size="large"
          class="submit-btn"
          :loading="oidcLoading"
          @click="startOidcLogin"
        >
          使用 Keycloak 统一身份登录
        </el-button>

        <el-divider>
          <span style="font-size: 12px; color: #909399;">或使用应急维护通道</span>
        </el-divider>

        <div style="text-align: center;">
          <el-button link type="info" size="small" @click="showLocalLogin = !showLocalLogin">
            {{ showLocalLogin ? '收起本地密码登录' : '展开本地密码登录 (离线/应急)' }}
          </el-button>
        </div>
      </div>

      <!-- 本地账号密码登录模式 -->
      <div v-if="!oidcConfig?.enabled || showLocalLogin" class="local-login-section">
        <el-form
          ref="loginFormRef"
          :model="loginForm"
          :rules="loginRules"
          label-position="top"
          size="large"
          @submit.prevent="handleLocalLogin"
        >
          <el-form-item label="人员工号" prop="工号">
            <el-input
              v-model="loginForm.工号"
              placeholder="请输入工号"
              :prefix-icon="User"
              clearable
            />
          </el-form-item>

          <el-form-item label="登录密码" prop="密码">
            <el-input
              v-model="loginForm.密码"
              type="password"
              placeholder="请输入密码"
              :prefix-icon="Lock"
              show-password
              @keyup.enter="handleLocalLogin"
            />
          </el-form-item>

          <el-form-item label="目标数据库标识" prop="DbKey">
            <el-input
              v-model="loginForm.DbKey"
              placeholder="ScientificResearch_Model"
              :prefix-icon="Coin"
              disabled
            />
          </el-form-item>

          <el-button
            type="primary"
            size="large"
            class="submit-btn"
            :loading="loading"
            @click="handleLocalLogin"
          >
            登 录
          </el-button>
        </el-form>
      </div>

      <div class="login-footer">
        <span>© 2026 科研信息管理平台 · .NET 10 + PostgreSQL 18</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { getOidcConfig, type OidcConfigResponse } from '@/api/auth'
import { ElMessage } from 'element-plus'
import { User, Lock, Coin, Management } from '@element-plus/icons-vue'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()

const loading = ref(false)
const oidcLoading = ref(false)
const showLocalLogin = ref(false)
const oidcConfig = ref<OidcConfigResponse | null>(null)

const loginForm = reactive({
  工号: '0',
  密码: '123',
  DbKey: 'ScientificResearch_Model'
})

const loginRules = {
  工号: [{ required: true, message: '请输入工号', trigger: 'blur' }],
  密码: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

// Base64URL 辅助工具
function base64Url(bytes: Uint8Array): string {
  let binary = ''
  for (let i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i])
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '')
}

function randomVerifier(): string {
  const bytes = new Uint8Array(32)
  window.crypto.getRandomValues(bytes)
  return base64Url(bytes)
}

async function sha256(value: string): Promise<string> {
  const buffer = await window.crypto.subtle.digest('SHA-256', new TextEncoder().encode(value))
  return base64Url(new Uint8Array(buffer))
}

// 发起 Keycloak OIDC PKCE 认证
async function startOidcLogin() {
  if (!oidcConfig.value) return
  oidcLoading.value = true

  try {
    const config = oidcConfig.value
    const state = randomVerifier()
    const verifier = randomVerifier()
    sessionStorage.setItem('keycloakOidcState', state)
    sessionStorage.setItem('keycloakPkceVerifier', verifier)

    const challenge = await sha256(verifier)
    const discoveryRes = await fetch(`${config.authority}/.well-known/openid-configuration`)
    const metadata = await discoveryRes.json()

    const params = new URLSearchParams()
    params.set('client_id', config.clientId)
    params.set('response_type', 'code')
    params.set('scope', 'openid profile email')
    // 重定向至当前前端登录页完成回调
    params.set('redirect_uri', `${window.location.origin}/login`)
    params.set('state', state)
    params.set('code_challenge', challenge)
    params.set('code_challenge_method', 'S256')
    // After an explicit logout require a fresh Keycloak credential instead of reusing SSO.
    if (sessionStorage.getItem('keycloakForceLogin') === '1') {
      params.set('prompt', 'login')
    }

    window.location.assign(`${metadata.authorization_endpoint}?${params.toString()}`)
  } catch (err: any) {
    ElMessage.error(err.message || '连接 Keycloak 认证中心失败')
    oidcLoading.value = false
  }
}

// 处理 Keycloak 回调 Code 换取 Token
async function handleOidcCallback() {
  const query = route.query
  const code = query.code as string
  const state = query.state as string

  if (!code) return

  const expectedState = sessionStorage.getItem('keycloakOidcState')
  const verifier = sessionStorage.getItem('keycloakPkceVerifier')
  sessionStorage.removeItem('keycloakOidcState')
  sessionStorage.removeItem('keycloakPkceVerifier')

  if (!state || state !== expectedState || !verifier) {
    ElMessage.error('Keycloak 登录校验状态无效')
    return
  }

  oidcLoading.value = true
  try {
    const config = oidcConfig.value || (await getOidcConfig())
    const discoveryRes = await fetch(`${config.authority}/.well-known/openid-configuration`)
    const metadata = await discoveryRes.json()

    const form = new URLSearchParams()
    form.set('grant_type', 'authorization_code')
    form.set('client_id', config.clientId)
    form.set('redirect_uri', `${window.location.origin}/login`)
    form.set('code', code)
    form.set('code_verifier', verifier)

    const tokenRes = await fetch(metadata.token_endpoint, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: form.toString()
    })
    const tokenBody = await tokenRes.json()
    if (!tokenRes.ok || !tokenBody.access_token) {
      throw new Error(tokenBody.error_description || '获取身份令牌失败')
    }

    userStore.setToken(tokenBody.access_token)
    if (tokenBody.id_token) {
      userStore.setIdToken(tokenBody.id_token)
    }
    sessionStorage.removeItem('keycloakForceLogin')
    await userStore.fetchUserInfo()
    ElMessage.success('统一身份登录成功！')
    router.replace('/dashboard')
  } catch (err: any) {
    ElMessage.error(err.message || 'Keycloak 身份校验失败')
  } finally {
    oidcLoading.value = false
  }
}

// 处理账号密码本地登录
async function handleLocalLogin() {
  loading.value = true
  try {
    await userStore.login(loginForm)
    ElMessage.success('登录成功！')
    router.push('/dashboard')
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '登录失败，请检查账号密码')
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  try {
    oidcConfig.value = await getOidcConfig()
    // 检查是否有 OIDC 回调参数
    if (route.query.code) {
      await handleOidcCallback()
    }
  } catch (err) {
    console.debug('Failed to load OidcConfig:', err)
  }
})
</script>

<style scoped lang="scss">
.login-wrapper {
  width: 100%;
  height: 100%;
  min-height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  background: linear-gradient(135deg, #1e3a8a 0%, #0f172a 100%);
  padding: 20px;

  .login-box {
    width: 100%;
    max-width: 440px;
    background: #ffffff;
    border-radius: 12px;
    box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.2), 0 10px 10px -5px rgba(0, 0, 0, 0.1);
    padding: 36px;

    .login-header {
      text-align: center;
      margin-bottom: 28px;

      .logo-circle {
        width: 56px;
        height: 56px;
        background: #eff6ff;
        border-radius: 50%;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        margin-bottom: 12px;
      }

      .system-title {
        font-size: 22px;
        font-weight: 700;
        color: #0f172a;
        margin-bottom: 6px;
      }

      .system-sub {
        font-size: 12px;
        color: #64748b;
        letter-spacing: 0.5px;
      }
    }

    .submit-btn {
      width: 100%;
      height: 44px;
      font-size: 16px;
      font-weight: 500;
      border-radius: 6px;
      margin-top: 10px;
    }

    .local-login-section {
      margin-top: 16px;
    }

    .login-footer {
      text-align: center;
      margin-top: 32px;
      font-size: 12px;
      color: #94a3b8;
    }
  }
}
</style>
