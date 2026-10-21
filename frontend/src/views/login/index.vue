<template>
  <div class="login-wrapper">
    <div class="login-box">
      <div class="login-header">
        <div class="logo-circle">
          <el-icon :size="32" color="#409eff"><Management /></el-icon>
        </div>
        <h1 class="system-title">科研信息管理系统</h1>
        <p class="system-sub">Medical Research Management System (MRMS)</p>
      </div>

      <!-- 统一人员管理 / 本地账号登录 双轨选项卡 -->
      <el-tabs v-model="activeTab" class="login-tabs" stretch>
        <!-- Tab 1: 统一人员管理 (Keycloak OIDC SSO) -->
        <el-tab-pane name="oidc">
          <template #label>
            <span class="tab-label">
              <el-icon><Connection /></el-icon>
              <span>统一人员管理</span>
            </span>
          </template>

          <div class="oidc-panel">
            <div class="sso-banner">
              <div class="sso-icon">
                <el-icon :size="28" color="#409eff"><Avatar /></el-icon>
              </div>
              <div class="sso-text">
                <div class="sso-title">院内统一人员与身份认证平台</div>
                <div class="sso-desc">基于 Keycloak / OIDC 权威单点登录</div>
              </div>
            </div>

            <div v-if="oidcConfig?.enabled" class="sso-status-card">
              <div class="status-row">
                <span class="status-badge connected"></span>
                <span class="status-title">认证中心已就绪</span>
              </div>
              <div class="status-endpoint">
                <span>权威节点：</span>
                <code>{{ oidcAuthorityClean }}</code>
              </div>
              <div class="status-meta">
                <span>已同步人事档案，权限与科室角色自动打通</span>
              </div>
            </div>

            <div v-else class="sso-status-card offline">
              <div class="status-row">
                <span class="status-badge standby"></span>
                <span class="status-title">统一认证配置中</span>
              </div>
              <div class="status-meta">
                <span>服务端统一人员配置正在接入，您可使用本地账号先行登录验证</span>
              </div>
            </div>

            <el-button
              type="primary"
              size="large"
              class="submit-btn oidc-btn"
              :loading="oidcLoading"
              :disabled="!oidcConfig?.enabled"
              @click="startOidcLogin"
            >
              <el-icon style="margin-right: 6px;"><Key /></el-icon>
              {{ oidcConfig?.enabled ? '进入统一人员管理认证登录' : '统一认证中心连线中...' }}
            </el-button>

            <!-- 统一认证测试预置人员提示 -->
            <div class="quick-accounts">
              <div class="quick-title">统一人员库已就绪账号：</div>
              <div class="account-chips">
                <el-tag size="small" type="success" effect="plain">111 (李世伟 · 临床专病)</el-tag>
                <el-tag size="small" type="primary" effect="plain">222 (胡英豪 · 科室主任)</el-tag>
                <el-tag size="small" type="info" effect="plain">111aa (李四 · 助理研究员)</el-tag>
              </div>
            </div>

            <div class="tab-switch-hint">
              <span>需使用独立管理员或离线调试？</span>
              <el-button link type="primary" size="small" @click="activeTab = 'local'">
                切换至本地账号登录
              </el-button>
            </div>
          </div>
        </el-tab-pane>

        <!-- Tab 2: 本地账号登录 (工号 + 密码) -->
        <el-tab-pane name="local">
          <template #label>
            <span class="tab-label">
              <el-icon><User /></el-icon>
              <span>本地账号登录</span>
            </span>
          </template>

          <div class="local-panel">
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

              <!-- 快捷预填常用测试人员 -->
              <div class="quick-preset-bar">
                <span class="preset-label">快捷填入：</span>
                <el-button
                  link
                  type="primary"
                  size="small"
                  @click="fillAccount('0', '123')"
                >
                  管理员(0)
                </el-button>
                <el-divider direction="vertical" />
                <el-button
                  link
                  type="primary"
                  size="small"
                  @click="fillAccount('111', '123abc')"
                >
                  科研人(111)
                </el-button>
                <el-divider direction="vertical" />
                <el-button
                  link
                  type="primary"
                  size="small"
                  @click="fillAccount('222', '2222222222')"
                >
                  科主任(222)
                </el-button>
                <el-divider direction="vertical" />
                <el-button
                  link
                  type="primary"
                  size="small"
                  @click="fillAccount('1', '123')"
                >
                  专家(1)
                </el-button>
              </div>

              <el-form-item label="目标数据库标识" prop="DbKey" style="margin-top: 8px;">
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
        </el-tab-pane>
      </el-tabs>

      <div class="login-footer">
        <span>© 2026 科研信息管理平台 · 统一人员接入规范 · .NET 10 + PostgreSQL 18</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { getOidcConfig, type OidcConfigResponse } from '@/api/auth'
import { ElMessage } from 'element-plus'
import {
  User,
  Lock,
  Coin,
  Management,
  Connection,
  Avatar,
  Key
} from '@element-plus/icons-vue'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()

const activeTab = ref<'oidc' | 'local'>('oidc')
const loading = ref(false)
const oidcLoading = ref(false)
const oidcConfig = ref<OidcConfigResponse | null>(null)

const oidcAuthorityClean = computed(() => {
  if (!oidcConfig.value?.authority) return ''
  return oidcConfig.value.authority.replace(/\/+$/, '')
})

const loginForm = reactive({
  工号: '0',
  密码: '123',
  DbKey: 'ScientificResearch_Model'
})

const loginRules = {
  工号: [{ required: true, message: '请输入工号', trigger: 'blur' }],
  密码: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

function fillAccount(工号: string, 密码: string) {
  loginForm.工号 = 工号
  loginForm.密码 = 密码
  ElMessage.info(`已填入工号 ${工号}`)
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

// 发起 Keycloak OIDC PKCE 统一身份认证
async function startOidcLogin() {
  if (!oidcConfig.value?.enabled) {
    ElMessage.warning('统一人员认证中心暂未启用，请使用本地账号登录')
    return
  }
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
    // Explicit logout require fresh login prompt
    if (sessionStorage.getItem('keycloakForceLogin') === '1') {
      params.set('prompt', 'login')
    }

    window.location.assign(`${metadata.authorization_endpoint}?${params.toString()}`)
  } catch (err: any) {
    ElMessage.error(err.message || '连接统一人员认证中心失败')
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
    ElMessage.error('统一人员认证校验状态无效，请重新登录')
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
      throw new Error(tokenBody.error_description || '获取统一人员身份令牌失败')
    }

    userStore.setToken(tokenBody.access_token)
    if (tokenBody.id_token) {
      userStore.setIdToken(tokenBody.id_token)
    }
    sessionStorage.removeItem('keycloakForceLogin')
    await userStore.fetchUserInfo()
    ElMessage.success('统一人员认证登录成功！')
    router.replace('/dashboard')
  } catch (err: any) {
    ElMessage.error(err.message || '统一人员身份校验失败')
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
    if (oidcConfig.value?.enabled) {
      activeTab.value = 'oidc'
    } else {
      activeTab.value = 'local'
    }
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
    max-width: 460px;
    background: #ffffff;
    border-radius: 14px;
    box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.25), 0 10px 10px -5px rgba(0, 0, 0, 0.12);
    padding: 32px 36px 28px;

    .login-header {
      text-align: center;
      margin-bottom: 20px;

      .logo-circle {
        width: 52px;
        height: 52px;
        background: #eff6ff;
        border-radius: 50%;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        margin-bottom: 10px;
      }

      .system-title {
        font-size: 21px;
        font-weight: 700;
        color: #0f172a;
        margin-bottom: 4px;
      }

      .system-sub {
        font-size: 11px;
        color: #64748b;
        letter-spacing: 0.5px;
      }
    }

    .login-tabs {
      margin-bottom: 12px;

      :deep(.el-tabs__item) {
        font-size: 15px;
        font-weight: 500;
        padding: 0 16px;
      }

      .tab-label {
        display: inline-flex;
        align-items: center;
        gap: 6px;
      }
    }

    .oidc-panel {
      padding: 8px 0 4px;

      .sso-banner {
        display: flex;
        align-items: center;
        gap: 12px;
        background: #f0fdf4;
        border: 1px solid #bbf7d0;
        border-radius: 8px;
        padding: 12px 14px;
        margin-bottom: 14px;

        .sso-icon {
          display: flex;
          align-items: center;
          justify-content: center;
        }

        .sso-text {
          .sso-title {
            font-size: 14px;
            font-weight: 600;
            color: #166534;
            line-height: 1.4;
          }

          .sso-desc {
            font-size: 12px;
            color: #15803d;
            margin-top: 2px;
          }
        }
      }

      .sso-status-card {
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 8px;
        padding: 12px 14px;
        margin-bottom: 16px;

        .status-row {
          display: flex;
          align-items: center;
          gap: 8px;
          margin-bottom: 6px;

          .status-badge {
            width: 8px;
            height: 8px;
            border-radius: 50%;

            &.connected {
              background: #22c55e;
              box-shadow: 0 0 0 3px rgba(34, 197, 94, 0.2);
            }

            &.standby {
              background: #eab308;
            }
          }

          .status-title {
            font-size: 13px;
            font-weight: 600;
            color: #1e293b;
          }
        }

        .status-endpoint {
          font-size: 12px;
          color: #64748b;
          margin-bottom: 4px;
          display: flex;
          align-items: center;
          gap: 4px;

          code {
            background: #e2e8f0;
            padding: 1px 6px;
            border-radius: 4px;
            font-family: ui-monospace, SFMono-Regular, monospace;
            color: #0f172a;
          }
        }

        .status-meta {
          font-size: 11px;
          color: #94a3b8;
          line-height: 1.4;
        }

        &.offline {
          background: #fffbeb;
          border-color: #fde68a;
        }
      }

      .oidc-btn {
        background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
        border: none;

        &:hover {
          opacity: 0.95;
        }
      }

      .quick-accounts {
        margin-top: 16px;
        padding: 10px 12px;
        background: #f1f5f9;
        border-radius: 6px;

        .quick-title {
          font-size: 12px;
          color: #475569;
          font-weight: 500;
          margin-bottom: 6px;
        }

        .account-chips {
          display: flex;
          flex-wrap: wrap;
          gap: 6px;
        }
      }

      .tab-switch-hint {
        text-align: center;
        margin-top: 16px;
        font-size: 12px;
        color: #64748b;

        .el-button {
          margin-left: 4px;
        }
      }
    }

    .local-panel {
      padding: 6px 0 0;

      .quick-preset-bar {
        display: flex;
        align-items: center;
        flex-wrap: wrap;
        font-size: 12px;
        color: #64748b;
        margin-bottom: 6px;

        .preset-label {
          margin-right: 4px;
        }
      }
    }

    .submit-btn {
      width: 100%;
      height: 44px;
      font-size: 15px;
      font-weight: 500;
      border-radius: 6px;
      margin-top: 10px;
    }

    .login-footer {
      text-align: center;
      margin-top: 24px;
      font-size: 11px;
      color: #94a3b8;
      line-height: 1.5;
    }
  }
}
</style>
