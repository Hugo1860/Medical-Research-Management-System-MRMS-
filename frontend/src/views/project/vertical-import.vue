<template>
  <div class="vertical-import-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span class="title">国家自然科学基金 / 计划书 PDF 批量导入</span>
          <el-tag type="danger">PDF 计划书智能解析 / 查重校验 / 批量立项</el-tag>
        </div>
      </template>

      <div class="action-bar" style="margin-bottom: 15px; display: flex; gap: 12px">
        <el-button type="primary" @click="handleCreateBatch">创建导入批次</el-button>
        <el-button @click="loadBatches">刷新列表</el-button>
      </div>

      <el-table :data="batches" border style="width: 100%">
        <el-table-column prop="批次名称" label="批次名称" min-width="200" />
        <el-table-column prop="来源类型" label="计划书类型" width="160" />
        <el-table-column prop="状态" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.状态 === '已导入' ? 'success' : 'warning'">{{ row.状态 }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="总文件数" label="文件数" width="90" align="center" />
        <el-table-column prop="解析成功数" label="解析成功" width="100" align="center" />
        <el-table-column prop="导入成功数" label="已立项" width="90" align="center" />
        <el-table-column prop="导入人姓名" label="导入人" width="110" />
        <el-table-column label="操作" width="220" align="center">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleViewDetails(row)">查看明细</el-button>
            <el-button v-if="row.状态 !== '已导入'" link type="success" @click="handleConfirmImport(row)">
              确认批量立项
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 明细列表抽屉 -->
      <el-drawer v-model="drawerVisible" title="导入明细与查重校验" size="70%">
        <el-table :data="details" border style="width: 100%">
          <el-table-column prop="源文件名" label="文件名" min-width="180" />
          <el-table-column prop="解析状态" label="解析" width="90" align="center" />
          <el-table-column prop="校验状态" label="校验" width="90" align="center">
            <template #default="{ row }">
              <el-tag :type="row.校验状态 === '通过' ? 'success' : row.校验状态 === '警告' ? 'warning' : 'danger'">
                {{ row.校验状态 }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="处理状态" label="立项状态" width="100" align="center" />
          <el-table-column label="解析关键信息" min-width="220">
            <template #default="{ row }">
              <div>名称：{{ row.解析字段?.项目名称 }}</div>
              <div>批准号：{{ row.解析字段?.批准号 }}</div>
              <div>经费：{{ row.解析字段?.资助经费万元 }} 万元</div>
            </template>
          </el-table-column>
        </el-table>
      </el-drawer>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import {
  getImportBatches,
  getImportDetails,
  createImportBatch,
  confirmBatchImport
} from '@/api/vertical-import'

const batches = ref<any[]>([])
const drawerVisible = ref(false)
const details = ref<any[]>([])
const currentBatch = ref<any>(null)

async function loadBatches() {
  try {
    const res = await getImportBatches()
    batches.value = res || []
  } catch (err: any) {
    ElMessage.error(err.message || '加载导入批次失败')
  }
}

async function handleCreateBatch() {
  try {
    const name = `国家自然科学基金立项计划书_${new Date().toLocaleDateString()}`
    await createImportBatch({ 批次名称: name, 来源类型: '国家自然科学基金' })
    ElMessage.success('批次创建成功')
    loadBatches()
  } catch (err: any) {
    ElMessage.error(err.message || '创建批次失败')
  }
}

async function handleViewDetails(row: any) {
  currentBatch.value = row
  try {
    const res = await getImportDetails(row.编号)
    details.value = res || []
    drawerVisible.value = true
  } catch (err: any) {
    ElMessage.error(err.message || '加载明细失败')
  }
}

async function handleConfirmImport(row: any) {
  try {
    const res = await confirmBatchImport(row.编号)
    ElMessage.success(`导入完成，成功入库立项 ${res?.成功导入项目数 || 0} 项`)
    loadBatches()
  } catch (err: any) {
    ElMessage.error(err.message || '确认立项失败')
  }
}

onMounted(() => {
  loadBatches()
})
</script>

<style scoped>
.vertical-import-container {
  padding: 16px;
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.title {
  font-size: 16px;
  font-weight: 600;
}
</style>
