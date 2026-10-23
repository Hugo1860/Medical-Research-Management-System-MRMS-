<template>
  <div class="welcome-page">
    <!-- 顶部品牌导航 -->
    <header class="welcome-brand">
      <span class="welcome-mark" aria-hidden="true">
        <svg viewBox="0 0 24 24" width="26" height="26" stroke="currentColor" stroke-width="2.2" fill="none" stroke-linecap="round" stroke-linejoin="round">
          <path d="M12 2v20M2 12h20" stroke-width="2.6" />
          <circle cx="12" cy="12" r="9" stroke-width="1.8" />
        </svg>
      </span>
      <div class="brand-titles">
        <span class="brand-main">医学科研管理系统</span>
        <small class="brand-sub" lang="en">Medical Research Management System (MRMS)</small>
      </div>
    </header>

    <!-- 登录主布局：左右分栏 -->
    <main class="welcome-layout">
      <!-- 左侧叙述区：平台使命与核心模块 -->
      <section class="welcome-story" aria-labelledby="welcome-title">
        <span class="welcome-eyebrow">临床与转化医学科研平台</span>
        <h1 id="welcome-title">
          规范科研全生命周期，<br />
          让每一次探索成为<span>临床创新。</span>
        </h1>
        <p class="welcome-description">
          涵盖项目申报、纵向横向合同、科研经费卡、伦理审查、学术交流与成果转化，<br class="welcome-desktop-break" />
          打造医院标准化、全流程、高效率的智慧科研管理与协同空间。
        </p>

        <div class="welcome-features">
          <div class="feature-item">
            <span class="welcome-feature-icon">
              <el-icon :size="20"><Management /></el-icon>
            </span>
            <div class="feature-text">
              <strong>全流程科研治理</strong>
              <small>立项申报 · 过程跟踪 · 结题验收</small>
            </div>
          </div>

          <div class="feature-item">
            <span class="welcome-feature-icon">
              <el-icon :size="20"><Coin /></el-icon>
            </span>
            <div class="feature-text">
              <strong>经费精细化执行</strong>
              <small>到款认领 · 预算执行 · 外拨合规</small>
            </div>
          </div>

          <div class="feature-item">
            <span class="welcome-feature-icon">
              <el-icon :size="20"><TrendCharts /></el-icon>
            </span>
            <div class="feature-text">
              <strong>学术成果与人才</strong>
              <small>论文专著 · 成果转化 · 人员画像</small>
            </div>
          </div>
        </div>

        <p class="welcome-motto" lang="en">Rigorous Science · Driven Innovation · Improved Care</p>
      </section>

      <!-- 右侧卡片：登录与身份认证 -->
      <section class="welcome-card" aria-labelledby="welcome-card-title">
        <div class="welcome-card-header">
          <span class="welcome-card-icon" aria-hidden="true">
            <el-icon :size="24"><Lock /></el-icon>
          </span>
          <div class="welcome-card-titles">
            <span class="welcome-card-kicker">科研人员与管理统一入口</span>
            <h2 id="welcome-card-title">登录科研管理系统</h2>
          </div>
        </div>

        <p class="welcome-card-description">
          使用您的科研人员工号与密码，或通过医院统一身份认证授权访问。
        </p>

        <!-- 登录方式分段选项卡：默认优先统一人员身份认证 -->
        <div class="auth-tabs-nav" role="tablist">
          <button
            type="button"
            role="tab"
            :aria-selected="activeTab === 'oidc'"
            :class="['tab-btn', { active: activeTab === 'oidc' }]"
            @click="activeTab = 'oidc'"
          >
            <el-icon><Connection /></el-icon>
            <span>统一身份认证 (推荐)</span>
            <span v-if="oidcConfig?.enabled" class="online-dot" title="统一认证中心已就绪"></span>
          </button>
          <button
            type="button"
            role="tab"
            :aria-selected="activeTab === 'local'"
            :class="['tab-btn', { active: activeTab === 'local' }]"
            @click="activeTab = 'local'"
          >
            <el-icon><User /></el-icon>
            <span>工号密码登录</span>
          </button>
        </div>

        <!-- 选项卡 1：统一人员身份认证 (Keycloak OIDC SSO - 默认优先) -->
        <div v-show="activeTab === 'oidc'" class="tab-panel oidc-tab">
          <div class="welcome-account-note">
            <el-icon :size="18" style="color: #27765f;"><Avatar /></el-icon>
            <span>院内统一人员与身份认证平台 · Keycloak / OIDC 权威单点登录</span>
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
              已同步人事档案，权限与科室角色自动打通
            </div>
          </div>

          <div v-else class="sso-status-card offline">
            <div class="status-row">
              <span class="status-badge standby"></span>
              <span class="status-title">统一认证配置中</span>
            </div>
            <div class="status-meta">
              服务端统一人员配置正在接入，您可切换至工号密码登录先行验证
            </div>
          </div>

          <button
            type="button"
            class="welcome-submit oidc-action-btn"
            :disabled="!oidcConfig?.enabled || oidcLoading"
            @click="startOidcLogin"
          >
            <el-icon v-if="oidcLoading" class="welcome-spinner"><Loading /></el-icon>
            <span>{{ oidcLoading ? '正在连接统一认证中心…' : oidcConfig?.enabled ? '使用统一身份认证登录' : '统一认证中心连线中…' }}</span>
            <el-icon v-if="oidcConfig?.enabled && !oidcLoading"><Right /></el-icon>
          </button>

          <!-- 统一人员账号就绪提示 -->
          <div class="quick-accounts">
            <div class="quick-title">统一人员库已就绪账号：</div>
            <div class="account-chips">
              <span class="acc-chip tag-green">111 (李世伟 · 临床专病)</span>
              <span class="acc-chip tag-blue">222 (胡英豪 · 科室主任)</span>
              <span class="acc-chip tag-gray">111aa (李四 · 助理研究员)</span>
            </div>
          </div>

          <div class="tab-switch-hint">
            <span>需要使用应急系统管理员或本地调试账号？</span>
            <button type="button" class="switch-link" @click="activeTab = 'local'">
              切换至工号密码登录
            </button>
          </div>
        </div>

        <!-- 选项卡 2：本地工号密码登录 -->
        <div v-show="activeTab === 'local'" class="tab-panel local-tab">
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
                placeholder="请输入工号 (如 0 或 111)"
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
              <button type="button" class="preset-chip" @click="fillAccount('0', '123')">管理员(0)</button>
              <button type="button" class="preset-chip" @click="fillAccount('111', '123abc')">科研人(111)</button>
              <button type="button" class="preset-chip" @click="fillAccount('222', '2222222222')">科主任(222)</button>
              <button type="button" class="preset-chip" @click="fillAccount('1', '123')">评审专家(1)</button>
            </div>

            <!-- 数据域标识折叠信息 -->
            <div class="db-target-note">
              <span>数据域：</span>
              <code>{{ loginForm.DbKey }}</code>
            </div>

            <button
              type="submit"
              class="welcome-submit"
              :disabled="loading"
              @click.prevent="handleLocalLogin"
            >
              <el-icon v-if="loading" class="welcome-spinner"><Loading /></el-icon>
              <span>{{ loading ? '正在验证登录…' : '立即登录' }}</span>
              <el-icon v-if="!loading"><Right /></el-icon>
            </button>

            <div class="tab-switch-hint">
              <span>优先使用医院统一单点登录？</span>
              <button type="button" class="switch-link" @click="activeTab = 'oidc'">
                返回统一身份认证登录
              </button>
            </div>
          </el-form>
        </div>

        <!-- 底部求助与授权提示 -->
        <div class="welcome-help">
          <strong>还没有账号或无法登录？</strong>
          <p>请联系医院科研处（科技处）管理员或科室科研秘书，确认人员工号与系统访问授权。</p>
        </div>
      </section>
    </main>

    <!-- 页脚版权与口号 -->
    <footer class="welcome-footer">
      <span>医学科研管理系统 · Medical Research Management System</span>
      <span>共同推进规范科研与高水平临床转化创新。</span>
    </footer>
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
  Right,
  Loading,
  TrendCharts
} from '@element-plus/icons-vue'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()

const activeTab = ref<'local' | 'oidc'>('oidc')
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
    ElMessage.warning('统一人员认证中心暂未启用，请使用工号密码登录')
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
    params.set('redirect_uri', `${window.location.origin}/login`)
    params.set('state', state)
    params.set('code_challenge', challenge)
    params.set('code_challenge_method', 'S256')
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
    // 检查是否有 OIDC 回调参数
    if (route.query.code) {
      activeTab.value = 'oidc'
      await handleOidcCallback()
    } else if (oidcConfig.value && !oidcConfig.value.enabled) {
      // 若服务端显式未开启统一认证，则优雅降级至工号密码登录
      activeTab.value = 'local'
    } else {
      // 默认优先统一人员身份认证
      activeTab.value = 'oidc'
    }
  } catch (err) {
    console.debug('Failed to load OidcConfig:', err)
  }
})
</script>

<style scoped lang="scss">
.welcome-page {
  min-height: 100vh;
  min-height: 100svh;
  display: flex;
  flex-direction: column;
  background: radial-gradient(ellipse at 3% 40%, #e4f2ed 0, transparent 55%), #f7f9f8;
  color: #223e37;
  padding: 32px clamp(24px, 5.5vw, 96px) 24px;
  font-family: -apple-system, BlinkMacSystemFont, 'PingFang SC', 'Noto Sans SC', 'Segoe UI', Roboto, 'Microsoft YaHei', sans-serif;
  box-sizing: border-box;
}

/* 顶部品牌区 */
.welcome-brand {
  display: flex;
  gap: 14px;
  align-items: center;
  max-width: 1180px;
  width: 100%;
  margin: 0 auto;
}

.welcome-mark {
  width: 48px;
  height: 48px;
  background: #1e6352;
  color: #ffffff;
  display: grid;
  place-items: center;
  border-radius: 14px;
  box-shadow: 0 6px 16px rgba(30, 99, 82, 0.16);
  flex-shrink: 0;
}

.brand-titles {
  display: flex;
  flex-direction: column;

  .brand-main {
    font-size: 19px;
    font-weight: 650;
    color: #1b3832;
    line-height: 1.3;
  }

  .brand-sub {
    font-size: 12px;
    color: #59796e;
    font-weight: 500;
    letter-spacing: 0.4px;
    margin-top: 3px;
  }
}

/* 主栅格布局 */
.welcome-layout {
  display: grid;
  grid-template-columns: minmax(0, 1.25fr) minmax(360px, 0.95fr);
  align-items: center;
  gap: clamp(32px, 6vw, 84px);
  max-width: 1180px;
  width: 100%;
  flex: 1;
  margin: 0 auto;
  padding: 56px 0 48px;
}

/* 左侧叙述区 */
.welcome-story {
  min-width: 0;

  .welcome-eyebrow {
    font-size: 13px;
    color: #427764;
    letter-spacing: 2px;
    display: inline-flex;
    align-items: center;
    gap: 10px;
    font-weight: 600;

    &::before {
      content: '';
      width: 22px;
      height: 2px;
      background: #5a9980;
      border-radius: 2px;
    }
  }

  h1 {
    font-size: clamp(32px, 3.5vw, 46px);
    font-weight: 650;
    line-height: 1.45;
    letter-spacing: -0.8px;
    color: #1e3832;
    margin: 20px 0;

    span {
      color: #27765f;
    }
  }

  .welcome-description {
    font-size: 15px;
    color: #5c756b;
    line-height: 1.95;
    max-width: 620px;
  }

  .welcome-features {
    display: flex;
    flex-direction: column;
    gap: 18px;
    margin-top: 36px;

    .feature-item {
      display: flex;
      align-items: center;
      gap: 14px;

      .welcome-feature-icon {
        display: grid;
        place-items: center;
        width: 42px;
        height: 42px;
        border: 1px solid #d5e5dc;
        background: rgba(255, 255, 255, 0.85);
        border-radius: 12px;
        color: #38735b;
        flex-shrink: 0;
        box-shadow: 0 2px 6px rgba(36, 76, 58, 0.04);
      }

      .feature-text {
        strong {
          display: block;
          font-size: 14.5px;
          font-weight: 600;
          color: #233e37;
        }

        small {
          display: block;
          margin-top: 3px;
          font-size: 12px;
          color: #647b70;
        }
      }
    }
  }

  .welcome-motto {
    font-size: 12px;
    color: #6c8378;
    letter-spacing: 0.8px;
    margin-top: 44px;
    font-weight: 500;
  }
}

/* 右侧卡片区 */
.welcome-card {
  min-width: 0;
  background: #ffffff;
  border: 1px solid #e1e9e4;
  border-radius: 24px;
  padding: 38px 34px 28px;
  box-shadow: 0 16px 48px rgba(36, 76, 58, 0.06), 0 2px 5px rgba(36, 76, 58, 0.02);
  text-align: left;

  .welcome-card-header {
    display: flex;
    align-items: center;
    gap: 14px;
    margin-bottom: 8px;

    .welcome-card-icon {
      display: grid;
      place-items: center;
      width: 48px;
      height: 48px;
      border-radius: 14px;
      background: #edf6f1;
      color: #27765f;
      flex-shrink: 0;
    }

    .welcome-card-titles {
      .welcome-card-kicker {
        display: block;
        font-size: 12px;
        color: #71867b;
        margin-bottom: 3px;
      }

      h2 {
        font-size: 23px;
        letter-spacing: -0.4px;
        line-height: 1.35;
        color: #1e3832;
        margin: 0;
        font-weight: 650;
      }
    }
  }

  .welcome-card-description {
    font-size: 13.5px;
    color: #65796e;
    margin: 12px 0 20px;
    line-height: 1.7;
  }

  /* 分段切换按钮 */
  .auth-tabs-nav {
    display: flex;
    background: #f1f6f3;
    padding: 4px;
    border-radius: 12px;
    gap: 4px;
    margin-bottom: 22px;

    .tab-btn {
      flex: 1;
      border: none;
      background: transparent;
      padding: 9px 12px;
      border-radius: 9px;
      font-size: 13.5px;
      font-weight: 500;
      color: #557268;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 6px;
      position: relative;
      transition: all 0.16s ease;

      &:hover {
        color: #1e6352;
      }

      &.active {
        background: #ffffff;
        color: #1e6352;
        font-weight: 600;
        box-shadow: 0 2px 8px rgba(20, 65, 55, 0.08);
      }

      .online-dot {
        width: 7px;
        height: 7px;
        border-radius: 50%;
        background: #22c55e;
        box-shadow: 0 0 0 2px rgba(34, 197, 94, 0.25);
      }
    }
  }

  /* 本地表单定制 */
  .local-tab {
    :deep(.el-form-item) {
      margin-bottom: 16px;

      .el-form-item__label {
        font-size: 13px;
        color: #314d45;
        font-weight: 550;
        padding-bottom: 4px;
      }

      .el-input__wrapper {
        box-shadow: 0 0 0 1px #d1dcda inset;
        border-radius: 9px;
        padding: 4px 12px;
        transition: box-shadow 0.15s ease;

        &:hover {
          box-shadow: 0 0 0 1px #9cbdb6 inset;
        }

        &.is-focus {
          box-shadow: 0 0 0 2px #27765f inset !important;
        }
      }
    }

    .quick-preset-bar {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: 6px;
      font-size: 12px;
      color: #63776e;
      margin-bottom: 14px;
      padding: 4px 0;

      .preset-label {
        color: #798c83;
      }

      .preset-chip {
        border: 1px solid #d5e5dc;
        background: #f7faf8;
        color: #27765f;
        border-radius: 6px;
        padding: 3px 8px;
        font-size: 11.5px;
        cursor: pointer;
        transition: all 0.15s ease;

        &:hover {
          background: #edf6f1;
          border-color: #aed0c1;
          color: #19573f;
        }
      }
    }

    .db-target-note {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 11.5px;
      color: #768a80;
      margin-bottom: 18px;

      code {
        background: #f1f5f3;
        border: 1px solid #e1eae5;
        color: #294c40;
        padding: 2px 7px;
        border-radius: 5px;
        font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
      }
    }
  }

  /* 统一认证面板 */
  .oidc-tab {
    .welcome-account-note {
      display: flex;
      gap: 10px;
      align-items: center;
      font-size: 12px;
      color: #3b6b5a;
      background: #f3f8f5;
      border: 1px solid #e1eee7;
      border-radius: 10px;
      padding: 12px 14px;
      margin-bottom: 16px;
      line-height: 1.5;
    }

    .sso-status-card {
      background: #f9fbf9;
      border: 1px solid #e5ede8;
      border-radius: 11px;
      padding: 14px 16px;
      margin-bottom: 20px;

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
            box-shadow: 0 0 0 3px rgba(234, 179, 8, 0.2);
          }
        }

        .status-title {
          font-size: 13.5px;
          font-weight: 600;
          color: #1e3832;
        }
      }

      .status-endpoint {
        font-size: 12px;
        color: #62776e;
        margin-bottom: 6px;
        display: flex;
        align-items: center;
        gap: 6px;

        code {
          background: #e9f0ec;
          padding: 1px 6px;
          border-radius: 4px;
          font-family: ui-monospace, SFMono-Regular, monospace;
          color: #1d3e35;
        }
      }

      .status-meta {
        font-size: 11.5px;
        color: #85988f;
        line-height: 1.45;
      }

      &.offline {
        background: #fffdf7;
        border-color: #f7e8c3;
      }
    }

    .quick-accounts {
      margin-top: 18px;
      padding: 12px 14px;
      background: #f7faf8;
      border: 1px solid #e6efe9;
      border-radius: 10px;

      .quick-title {
        font-size: 12px;
        color: #557267;
        font-weight: 550;
        margin-bottom: 8px;
      }

      .account-chips {
        display: flex;
        flex-direction: column;
        gap: 5px;

        .acc-chip {
          display: inline-block;
          font-size: 11.5px;
          padding: 3px 8px;
          border-radius: 5px;
          background: #edf5f0;
          color: #276953;
          border: 1px solid #daeae0;
        }
      }
    }
  }

  /* 登录主提交按钮 */
  .welcome-submit {
    width: 100%;
    min-height: 48px;
    padding: 12px 18px;
    border-radius: 11px;
    background: #236e57;
    color: #ffffff;
    font-size: 14.5px;
    font-weight: 600;
    border: none;
    box-shadow: 0 4px 12px rgba(35, 110, 87, 0.16);
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    transition: background 0.15s ease, box-shadow 0.15s ease, transform 0.1s ease;

    &:hover:not(:disabled) {
      background: #19573f;
      box-shadow: 0 6px 16px rgba(35, 110, 87, 0.26);
    }

    &:active:not(:disabled) {
      transform: translateY(1px);
    }

    &:disabled {
      opacity: 0.65;
      cursor: not-allowed;
    }

    &:focus-visible {
      outline: 3px solid #82bba3;
      outline-offset: 3px;
    }

    span {
      flex: 1;
      text-align: center;
    }

    .welcome-spinner {
      animation: welcome-spin 1s linear infinite;
    }
  }

  /* 登录方式切换提示 */
  .tab-switch-hint {
    text-align: center;
    margin-top: 16px;
    font-size: 12px;
    color: #657b71;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 6px;

    .switch-link {
      border: none;
      background: transparent;
      color: #236e57;
      font-size: 12px;
      font-weight: 550;
      cursor: pointer;
      text-decoration: underline;
      text-underline-offset: 3px;
      padding: 0;
      transition: color 0.15s ease;

      &:hover {
        color: #154d3d;
      }
    }
  }

  /* 帮助求助区域 */
  .welcome-help {
    border-top: 1px solid #edf2ef;
    padding-top: 20px;
    margin-top: 24px;

    strong {
      font-size: 12px;
      font-weight: 600;
      color: #3b5a50;
      display: block;
    }

    p {
      font-size: 12px;
      color: #72857b;
      margin: 5px 0 0;
      line-height: 1.75;
    }
  }
}

/* 底部声明 */
.welcome-footer {
  max-width: 1180px;
  width: 100%;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  gap: 12px;
  font-size: 12px;
  color: #748477;
  padding-top: 20px;
  border-top: 1px solid #dfe8e1;
}

@keyframes welcome-spin {
  to {
    transform: rotate(360deg);
  }
}

/* 响应式断点适配 */
@media (max-width: 960px) {
  .welcome-layout {
    gap: 32px;
    grid-template-columns: minmax(0, 1fr) minmax(320px, 1fr);
  }

  .welcome-story h1 {
    font-size: 32px;
  }

  .welcome-card {
    padding: 30px 24px 22px;
  }

  .welcome-desktop-break {
    display: none;
  }
}

@media (max-width: 740px) {
  .welcome-page {
    padding: 24px 18px 20px;
  }

  .welcome-layout {
    display: flex;
    flex-direction: column;
    gap: 32px;
    padding: 28px 0;
    align-items: stretch;
    max-width: 460px;
  }

  .welcome-brand {
    max-width: 460px;
  }

  .welcome-story h1 {
    font-size: 28px;
    margin: 14px 0 10px;
  }

  .welcome-features,
  .welcome-motto {
    display: none;
  }

  .welcome-card {
    padding: 26px 20px 22px;
    border-radius: 20px;
  }

  .welcome-footer {
    max-width: 460px;
    flex-direction: column;
    gap: 6px;
    font-size: 11.5px;
  }
}
</style>
