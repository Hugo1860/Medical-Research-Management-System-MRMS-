<template>
  <div v-loading="loading" class="iframe-container">
    <iframe
      v-if="targetUrl"
      ref="iframeRef"
      :src="targetUrl"
      class="legacy-iframe"
      frameborder="0"
      scrolling="auto"
      @load="onIframeLoad"
    ></iframe>
    <div v-else class="empty-tip">
      <el-empty description="未指定有效的业务页面路径" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { resolveModernRoute } from '@/utils/route-resolver'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const loading = ref(true)
const iframeRef = ref<HTMLIFrameElement | null>(null)

const targetUrl = computed(() => {
  const url = route.query.url as string
  if (!url) return ''
  return decodeURIComponent(url)
})

function checkModernTakeover(url: string): boolean {
  if (!url) return false
  const modern = resolveModernRoute(url)
  if (modern) {
    router.replace(modern)
    return true
  }
  return false
}

onMounted(() => {
  if (checkModernTakeover(targetUrl.value)) return
})

watch(
  () => targetUrl.value,
  (newVal) => {
    if (checkModernTakeover(newVal)) return
    loading.value = true
  }
)

function onIframeLoad() {
  loading.value = false

  try {
    const iframeWindow = iframeRef.value?.contentWindow as any
    if (iframeWindow) {
      // 避免旧页面通过 window.location.replace 导致现代 SPA 在 iframe 中嵌套渲染
      try {
        const loc = iframeWindow.location
        if (loc && loc.pathname && !loc.pathname.includes('/IMIS/')) {
          const dest = loc.pathname + (loc.search || '') + (loc.hash || '')
          router.replace(dest)
          return
        }
      } catch {
        // cross-origin or navigation error
      }
      // 向嵌入的旧版页面同步最新的 Keycloak 访问令牌与用户信息
      const token = userStore.token
      if (token) {
        iframeWindow.sessionStorage?.setItem('keycloakAccessToken', token)
      }
      if (userStore.userInfo) {
        iframeWindow.sessionStorage?.setItem(
          'userInfo',
          JSON.stringify({ 人员: userStore.userInfo, 权限: userStore.permissions })
        )
      }
    }
  } catch (err) {
    // 跨域或安全限制不阻断渲染
    console.debug('Iframe context sync notice:', err)
  }
}
</script>

<style scoped lang="scss">
.iframe-container {
  width: 100%;
  height: 100%;
  position: relative;
  overflow: hidden;
  background-color: #ffffff;

  .legacy-iframe {
    width: 100%;
    height: 100%;
    display: block;
    border: none;
  }

  .empty-tip {
    padding-top: 100px;
  }
}
</style>
