<template>
  <div class="journal-partition-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span class="title">期刊分区库与智能匹配</span>
          <el-tag type="info">中科院分区 / JCR / CSSCI / 北大核心 / 论文自动对齐</el-tag>
        </div>
      </template>

      <div class="filter-bar" style="margin-bottom: 15px; display: flex; gap: 12px; align-items: center">
        <el-select v-model="filterSource" placeholder="选择分区来源" style="width: 150px" clearable>
          <el-option label="中科院分区" value="中科院分区" />
          <el-option label="JCR" value="JCR" />
          <el-option label="CSSCI" value="CSSCI" />
          <el-option label="北大核心" value="北大核心" />
        </el-select>
        <el-input-number v-model="filterYear" :min="1900" :max="9999" :precision="0" placeholder="论文年度" :disabled="batchMatching" style="width: 150px" />
        <el-input v-model="keyword" placeholder="期刊名称或 ISSN 检索" style="width: 240px" clearable />
        <el-button type="primary" @click="loadData">查询分区</el-button>
        <el-button type="warning" :loading="batchMatching" @click="handleBatchMatch">批量对齐论文分区</el-button>
        <el-button type="success" @click="handleOpenAddDialog">录入分区条目</el-button>
      </div>
      <p v-if="batchMatching" role="status">已处理 {{ batchProgress.processed }} 篇论文，新增 {{ batchProgress.matched }} 条待确认推荐。</p>

      <el-alert
        title="匹配使用已录入的期刊分区库，结果保存为待确认推荐，不覆盖已有审核结果。外部数据库接入仍需机构授权及实际联调。年度留空时处理全部年度。"
        type="info"
        show-icon
        :closable="false"
        style="margin-bottom: 15px"
      />

      <el-table :data="tableData" border style="width: 100%">
        <el-table-column prop="期刊名称" label="期刊名称" min-width="200" />
        <el-table-column prop="ISSN" label="ISSN" width="130" />
        <el-table-column prop="来源" label="数据源" width="120" />
        <el-table-column prop="年度" label="年度" width="90" align="center" />
        <el-table-column prop="学科类别" label="学科门类" width="120" />
        <el-table-column prop="分区" label="分区等级" width="100" align="center">
          <template #default="{ row }">
            <el-tag type="danger">{{ row.分区 }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="影响因子" label="影响因子(IF)" width="120" align="right" />
      </el-table>
    </el-card>

    <!-- 条目新增弹窗 -->
    <el-dialog v-model="addDialogVisible" title="录入期刊分区条目" width="500px">
      <el-form :model="addForm" label-width="100px">
        <el-form-item label="期刊名称" required>
          <el-input v-model="addForm.期刊名称" />
        </el-form-item>
        <el-form-item label="ISSN">
          <el-input v-model="addForm.ISSN" />
        </el-form-item>
        <el-form-item label="数据来源">
          <el-select v-model="addForm.来源" style="width: 100%">
            <el-option label="中科院分区" value="中科院分区" />
            <el-option label="JCR" value="JCR" />
            <el-option label="CSSCI" value="CSSCI" />
            <el-option label="北大核心" value="北大核心" />
          </el-select>
        </el-form-item>
        <el-form-item label="分区年度" required>
          <el-input-number v-model="addForm.年度" :min="1900" :max="9999" :precision="0" />
        </el-form-item>
        <el-form-item label="分区等级" required>
          <el-input v-model="addForm.分区" placeholder="如：1区、Q1" />
        </el-form-item>
        <el-form-item label="影响因子">
          <el-input-number v-model="addForm.影响因子" :precision="3" :step="0.1" style="width: 100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="addDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitAdd">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import {
  getJournalPartitions,
  saveJournalPartition,
  matchAllPaperPartitions
} from '@/api/journal-partition'

const filterSource = ref('')
const filterYear = ref<number | undefined>(new Date().getFullYear())
const keyword = ref('')
const tableData = ref<any[]>([])
const batchMatching = ref(false)
const batchProgress = ref({ processed: 0, matched: 0 })

const addDialogVisible = ref(false)
const addForm = ref<any>({
  期刊名称: '',
  ISSN: '',
  来源: '中科院分区',
  年度: new Date().getFullYear(),
  学科类别: '医学',
  分区: '1区',
  影响因子: undefined
})

async function loadData() {
  try {
    const res = await getJournalPartitions(filterSource.value || undefined, filterYear.value, keyword.value || undefined)
    tableData.value = res || []
  } catch (err: any) {
    ElMessage.error(err.message || '加载期刊分区库失败')
  }
}

function handleOpenAddDialog() {
  addDialogVisible.value = true
}

async function submitAdd() {
  if (!addForm.value.期刊名称 || !addForm.value.分区) {
    ElMessage.warning('期刊名称和分区等级不能为空')
    return
  }
  try {
    await saveJournalPartition(addForm.value)
    ElMessage.success('保存成功')
    addDialogVisible.value = false
    loadData()
  } catch (err: any) {
    ElMessage.error(err.message || '保存失败')
  }
}

async function handleBatchMatch() {
  if (batchMatching.value) return
  batchMatching.value = true
  batchProgress.value = { processed: 0, matched: 0 }
  try {
    const result = await matchAllPaperPartitions(filterYear.value, progress => { batchProgress.value = progress })
    ElMessage.success(`已处理 ${result.processed} 篇论文，新增 ${result.matched} 条待确认推荐`)
  } catch (err: any) {
    ElMessage.error(`匹配中断，已处理 ${batchProgress.value.processed} 篇论文。${err.message || '请稍后重试'}；重试不会重复生成已有推荐。`)
  } finally {
    batchMatching.value = false
  }
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.journal-partition-container {
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
