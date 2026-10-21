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
        <el-input v-model="keyword" placeholder="期刊名称或 ISSN 检索" style="width: 240px" clearable />
        <el-button type="primary" @click="loadData">查询分区</el-button>
        <el-button type="warning" @click="handleBatchMatch">批量对齐论文分区</el-button>
        <el-button type="success" @click="handleOpenAddDialog">录入分区条目</el-button>
      </div>

      <el-alert
        title="数据接口提示：CNKI、万方、Web of Science、EI 商业接口需购买机构授权数据包后联调；当前提供标准本地分区规范库智能消歧与自动回写机制。"
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
  batchMatchPaperPartitions
} from '@/api/journal-partition'

const filterSource = ref('')
const keyword = ref('')
const tableData = ref<any[]>([])

const addDialogVisible = ref(false)
const addForm = ref<any>({
  期刊名称: '',
  ISSN: '',
  来源: '中科院分区',
  年度: 2024,
  学科类别: '医学',
  分区: '1区',
  影响因子: 5.234
})

async function loadData() {
  try {
    const res = await getJournalPartitions(filterSource.value, undefined, keyword.value)
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
  try {
    const res = await batchMatchPaperPartitions(2024)
    ElMessage.success(`匹配任务执行成功，对齐产生 ${res?.产生匹配数 || 0} 条推荐记录`)
  } catch (err: any) {
    ElMessage.error(err.message || '批量对齐失败')
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
