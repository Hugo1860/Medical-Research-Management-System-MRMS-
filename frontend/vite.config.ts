import { defineConfig } from 'vitest/config'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

const buildTime = process.env.VITE_BUILD_TIME || new Date().toISOString()

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  define: {
    __BUILD_TIME__: JSON.stringify(buildTime)
  },
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src')
    }
  },
  server: {
    port: 3000,
    host: '0.0.0.0',
    proxy: {
      // 代理后端业务 API 路由至运行中的 .NET 10 服务
      '^/(AcademicActivities|AcademicExchange|Access|AchievementIp|Analytics|Attachment|BaseInfo|Biobank|CmeCredit|CommunityHealthcare|ContinuingEducation|Dashboard|DocumentMessaging|EducationStatistics|Ethics|Evaluation|Expense|Fruit|FundExecution|HorizontalContractGovernance|HospitalMode|Incoming|JournalPartition|NewTechnology|Office|Operations|PaperSubmission|People|Permission|PlatformInstrument|PopularScience|ProjectConfiguration|ResearchPlatform|VerticalProjectImport|WorkFlow|XProject|YProject|FurtherEducation|health)(?:/|$)': {
        target: 'http://127.0.0.1:16379',
        changeOrigin: true
      },
      // 代理历史 IMIS 静态资源与子页面，确保双轨模式无缝运行
      '/IMIS': {
        target: 'http://127.0.0.1:16379',
        changeOrigin: true
      },
      // 代理用户上传文件目录
      '/upload': {
        target: 'http://127.0.0.1:16379',
        changeOrigin: true
      }
    }
  },
  build: {
    outDir: 'dist',
    chunkSizeWarningLimit: 1500,
    rollupOptions: {
      output: {
        manualChunks: {
          'vue-vendor': ['vue', 'vue-router', 'pinia'],
          'element-plus': ['element-plus', '@element-plus/icons-vue'],
          'echarts': ['echarts', 'vue-echarts']
        }
      }
    }
  },
  test: {
    globals: true,
    environment: 'jsdom'
  }
})
