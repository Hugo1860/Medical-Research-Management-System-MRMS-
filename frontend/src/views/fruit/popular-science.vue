<template>
  <div class="popular-science-container">
    <!-- 头部横幅 -->
    <div class="header-card">
      <div class="header-info">
        <h2 class="page-title">
          <el-icon class="title-icon"><Reading /></el-icon>
          科普成果、临床典型案例与护理发明库
        </h2>
        <p class="page-subtitle">
          聚焦基层及二级公立医院医务人员职称晋升、绩效考核特色成果：收录医学科普作品（图文/短视频/讲座）、临床疑难典型案例与护理改良器具/小发明转化。
        </p>
      </div>
      <div class="header-actions">
        <el-button type="primary" :icon="Plus" @click="openAddDialog">
          登记科普与案例成果
        </el-button>
        <el-button :icon="Refresh" @click="refreshAll">刷新</el-button>
      </div>
    </div>

    <!-- 统计指标大盘 -->
    <el-row :gutter="16" class="metrics-row">
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="metric-card">
          <div class="metric-label">医学科普作品</div>
          <div class="metric-value color-primary">{{ stats.科普作品总数 || 0 }} <span class="unit">篇/部</span></div>
          <div class="metric-sub">文章、视频与线下讲座</div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="metric-card">
          <div class="metric-label">临床典型案例库</div>
          <div class="metric-value color-success">{{ stats.临床案例总数 || 0 }} <span class="unit">例</span></div>
          <div class="metric-sub">疑难救治与质控病案</div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="metric-card">
          <div class="metric-label">护理小发明/实用新型</div>
          <div class="metric-value color-warning">{{ stats.护理小发明总数 || 0 }} <span class="unit">项</span></div>
          <div class="metric-sub">临床改良与器具创新</div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="hover" class="metric-card">
          <div class="metric-label">累计受众与传播成效</div>
          <div class="metric-value color-purple">{{ stats.累计受众与成效 || 0 }} <span class="unit">人次</span></div>
          <div class="metric-sub">医患受益与科普影响</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 成果列表 -->
    <el-card shadow="never" class="main-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="filter">
          <el-form-item label="成果分类">
            <el-select v-model="filter.category" clearable placeholder="全部分类" style="width: 170px" @change="loadList">
              <el-option label="医学科普作品" value="医学科普作品" />
              <el-option label="临床典型案例" value="临床典型案例" />
              <el-option label="护理小发明" value="护理小发明" />
            </el-select>
          </el-form-item>
          <el-form-item label="形式类型">
            <el-select v-model="filter.type" clearable placeholder="全部形式" style="width: 140px" @change="loadList">
              <el-option label="科普文章" value="科普文章" />
              <el-option label="科普短视频" value="科普短视频" />
              <el-option label="科普讲座" value="科普讲座" />
              <el-option label="疑难病案" value="疑难病案" />
              <el-option label="质控案例" value="质控案例" />
              <el-option label="护理改良器具" value="护理改良器具" />
              <el-option label="实用新型转化" value="实用新型转化" />
            </el-select>
          </el-form-item>
          <el-form-item label="审核状态">
            <el-select v-model="filter.status" clearable placeholder="全部状态" style="width: 120px" @change="loadList">
              <el-option label="待审核" value="待审核" />
              <el-option label="已入库" value="已入库" />
              <el-option label="已驳回" value="已驳回" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-input v-model="filter.keyword" clearable placeholder="成果名称、第一作者" style="width: 200px" @keyup.enter="loadList" />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" :icon="Search" @click="loadList">查询</el-button>
            <el-button :icon="Refresh" @click="resetFilter">重置</el-button>
          </el-form-item>
        </el-form>
      </div>

      <el-table v-loading="loading" :data="list" stripe border class="data-table">
        <el-table-column prop="编号" label="编号" width="70" align="center" />
        <el-table-column prop="成果名称" label="成果/作品名称" min-width="220" show-overflow-tooltip>
          <template #default="{ row }">
            <div class="name-cell">
              <strong>{{ row.成果名称 }}</strong>
              <el-tag size="small" :type="categoryTag(row.分类)">{{ row.分类 }}</el-tag>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="形式类型" label="形式" width="110" align="center">
          <template #default="{ row }">
            <el-tag size="small" effect="plain">{{ row.形式类型 }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="第一作者 / 科室" width="160">
          <template #default="{ row }">
            <div><strong>{{ row.第一作者姓名 }}</strong></div>
            <div class="muted-text">{{ row.科室名称 || '临床科室' }}</div>
          </template>
        </el-table-column>
        <el-table-column prop="发布载体与平台" label="发布载体 / 平台" min-width="150" show-overflow-tooltip />
        <el-table-column prop="完成日期" label="完成/发布日期" width="120" align="center">
          <template #default="{ row }">{{ formatDate(row.完成日期) }}</template>
        </el-table-column>
        <el-table-column prop="受众传播量与成效" label="受众传播量" width="110" align="center">
          <template #default="{ row }">
            <span class="views-count">{{ row.受众传播量与成效 || 0 }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="审核状态" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag size="small" :type="row.审核状态 === '已入库' ? 'success' : row.审核状态 === '已驳回' ? 'danger' : 'warning'">
              {{ row.审核状态 }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="viewDetail(row)">详情</el-button>
            <el-button
              v-if="row.审核状态 === '待审核'"
              link
              type="success"
              size="small"
              @click="openAudit(row)"
            >
              入库审核
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="pagination.page"
          v-model:page-size="pagination.pageSize"
          :total="pagination.total"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="loadList"
          @current-change="loadList"
        />
      </div>
    </el-card>

    <!-- 登记弹窗 -->
    <el-dialog v-model="addDialogVisible" title="登记科普成果 / 典型案例 / 护理小发明" width="650px" destroy-on-close>
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="120px">
        <el-form-item label="成果分类" prop="分类">
          <el-radio-group v-model="form.分类" @change="handleCategoryChange">
            <el-radio label="医学科普作品">医学科普作品</el-radio>
            <el-radio label="临床典型案例">临床典型案例</el-radio>
            <el-radio label="护理小发明">护理小发明</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="形式类型" prop="形式类型">
              <el-select v-model="form.形式类型" style="width: 100%">
                <el-option v-for="t in formatOptions" :key="t" :label="t" :value="t" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="完成/发布日期" prop="完成日期">
              <el-date-picker v-model="form.完成日期" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="成果名称" prop="成果名称">
          <el-input v-model="form.成果名称" placeholder="输入科普作品名、案例标题或改良发明名称" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="发布载体/平台">
              <el-input v-model="form.发布载体与平台" placeholder="如：医院官方微信公众号、抖音、临床质控汇编" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="受众传播成效">
              <el-input-number v-model="form.受众传播量与成效" :min="0" placeholder="阅读量/播放量/受益人次" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="成果摘要与说明">
          <el-input v-model="form.成果摘要" type="textarea" :rows="3" placeholder="简述核心科普内容、案例启示或护理发明技术要点与临床转化成效" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="addDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSave">确认提交</el-button>
      </template>
    </el-dialog>

    <!-- 审核弹窗 -->
    <el-dialog v-model="auditDialogVisible" title="成果入库审核" width="500px">
      <el-form :model="auditForm" label-width="90px">
        <el-form-item label="审核结果">
          <el-radio-group v-model="auditForm.通过">
            <el-radio :label="true">审核通过并入库</el-radio>
            <el-radio :label="false">驳回修改</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="审核意见">
          <el-input v-model="auditForm.审核意见" type="textarea" :rows="3" placeholder="输入审核意见与赋分建议" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="auditDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="auditSubmitting" @click="handleAudit">确认审核</el-button>
      </template>
    </el-dialog>

    <!-- 详情弹窗 -->
    <el-dialog v-model="detailDialogVisible" title="成果详情" width="600px">
      <div v-if="currentDetail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="成果名称" :span="2"><strong>{{ currentDetail.成果名称 }}</strong></el-descriptions-item>
          <el-descriptions-item label="分类">{{ currentDetail.分类 }}</el-descriptions-item>
          <el-descriptions-item label="形式类型">{{ currentDetail.形式类型 }}</el-descriptions-item>
          <el-descriptions-item label="第一作者">{{ currentDetail.第一作者姓名 }}</el-descriptions-item>
          <el-descriptions-item label="所属科室">{{ currentDetail.科室名称 || '临床科室' }}</el-descriptions-item>
          <el-descriptions-item label="完成日期">{{ formatDate(currentDetail.完成日期) }}</el-descriptions-item>
          <el-descriptions-item label="传播受众量">{{ currentDetail.受众传播量与成效 }} 人次</el-descriptions-item>
          <el-descriptions-item label="发布平台" :span="2">{{ currentDetail.发布载体与平台 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="审核状态">
            <el-tag size="small">{{ currentDetail.审核状态 }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="审核人">{{ currentDetail.审核人姓名 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="成果摘要" :span="2">{{ currentDetail.成果摘要 || '-' }}</el-descriptions-item>
          <el-descriptions-item label="审核意见" :span="2">{{ currentDetail.审核意见 || '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Reading, Plus, Refresh, Search } from '@element-plus/icons-vue'
import {
  listPopularScience, savePopularScience, auditPopularScience, getPopularScienceStats
} from '@/api/medical-advancement'

const stats = ref<any>({})
const loading = ref(false)
const list = ref<any[]>([])
const pagination = reactive({ page: 1, pageSize: 10, total: 0 })
const filter = reactive({ category: '', type: '', status: '', keyword: '' })

const addDialogVisible = ref(false)
const submitting = ref(false)
const form = reactive<any>({
  分类: '医学科普作品', 形式类型: '科普文章', 成果名称: '', 完成日期: '',
  发布载体与平台: '', 受众传播量与成效: 100, 成果摘要: ''
})
const formRules = {
  成果名称: [{ required: true, message: '请输入成果名称', trigger: 'blur' }],
  完成日期: [{ required: true, message: '请选择完成日期', trigger: 'change' }]
}

const auditDialogVisible = ref(false)
const auditSubmitting = ref(false)
const auditForm = reactive({ 编号: 0, 通过: true, 审核意见: '成果内容真实合规，符合医院科教业绩入库标准。' })

const detailDialogVisible = ref(false)
const currentDetail = ref<any>(null)

const formatOptions = computed(() => {
  if (form.分类 === '医学科普作品') return ['科普文章', '科普短视频', '科普讲座']
  if (form.分类 === '临床典型案例') return ['疑难病案', '质控案例']
  return ['护理改良器具', '实用新型转化']
})

function handleCategoryChange(cat: string) {
  if (cat === '医学科普作品') form.形式类型 = '科普文章'
  else if (cat === '临床典型案例') form.形式类型 = '疑难病案'
  else form.形式类型 = '护理改良器具'
}

function categoryTag(cat: string) {
  if (cat === '医学科普作品') return 'primary'
  if (cat === '临床典型案例') return 'success'
  return 'warning'
}

function formatDate(val: any) {
  if (!val) return '-'
  return String(val).substring(0, 10)
}

async function loadStats() {
  try {
    const res: any = await getPopularScienceStats()
    stats.value = res?.data || res || {}
  } catch {}
}

async function loadList() {
  loading.value = true
  try {
    const res: any = await listPopularScience({
      分类: filter.category || undefined,
      形式类型: filter.type || undefined,
      审核状态: filter.status || undefined,
      关键词: filter.keyword || undefined,
      pageIndex: pagination.page,
      pageSize: pagination.pageSize
    })
    const data = res?.data || res
    list.value = data?.list || []
    pagination.total = data?.total || 0
  } catch (err: any) {
    ElMessage.error(err.message || '获取成果列表失败')
  } finally {
    loading.value = false
  }
}

function resetFilter() {
  filter.category = ''
  filter.type = ''
  filter.status = ''
  filter.keyword = ''
  pagination.page = 1
  loadList()
}

function refreshAll() {
  loadStats()
  loadList()
}

function openAddDialog() {
  Object.assign(form, {
    分类: '医学科普作品', 形式类型: '科普文章', 成果名称: '',
    完成日期: new Date().toISOString().substring(0, 10),
    发布载体与平台: '', 受众传播量与成效: 100, 成果摘要: ''
  })
  addDialogVisible.value = true
}

async function handleSave() {
  if (!form.成果名称) {
    ElMessage.warning('请输入成果名称')
    return
  }
  submitting.value = true
  try {
    await savePopularScience(form)
    ElMessage.success('成果登记成功，已进入科教科审核队列')
    addDialogVisible.value = false
    loadList()
    loadStats()
  } catch (err: any) {
    ElMessage.error(err.message || '登记失败')
  } finally {
    submitting.value = false
  }
}

function openAudit(row: any) {
  auditForm.编号 = row.编号
  auditForm.通过 = true
  auditForm.审核意见 = '成果内容真实合规，符合医院科教业绩入库标准。'
  auditDialogVisible.value = true
}

async function handleAudit() {
  auditSubmitting.value = true
  try {
    await auditPopularScience(auditForm)
    ElMessage.success('审核操作成功')
    auditDialogVisible.value = false
    loadList()
    loadStats()
  } catch (err: any) {
    ElMessage.error(err.message || '审核失败')
  } finally {
    auditSubmitting.value = false
  }
}

function viewDetail(row: any) {
  currentDetail.value = row
  detailDialogVisible.value = true
}

onMounted(() => {
  loadStats()
  loadList()
})
</script>

<style scoped>
.popular-science-container {
  padding: 16px 20px;
  background-color: #f5f7fa;
  min-height: calc(100vh - 84px);
}

.header-card {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: #fff;
  padding: 20px 24px;
  border-radius: 8px;
  margin-bottom: 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.15);
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  margin: 0 0 6px;
  display: flex;
  align-items: center;
  gap: 8px;
}

.page-subtitle {
  font-size: 13px;
  opacity: 0.9;
  margin: 0;
  max-width: 800px;
  line-height: 1.5;
}

.header-actions {
  display: flex;
  gap: 10px;
}

.metrics-row {
  margin-bottom: 16px;
}

.metric-card {
  border-radius: 8px;
  text-align: center;
  border: 1px solid #ebeef5;
}

.metric-label {
  font-size: 13px;
  color: #909399;
  margin-bottom: 6px;
}

.metric-value {
  font-size: 24px;
  font-weight: 700;
  color: #303133;
}

.metric-value .unit {
  font-size: 12px;
  font-weight: normal;
  color: #909399;
}

.metric-sub {
  font-size: 12px;
  color: #909399;
  margin-top: 4px;
}

.color-primary { color: #10b981; }
.color-success { color: #3b82f6; }
.color-warning { color: #f59e0b; }
.color-purple { color: #8b5cf6; }

.main-card {
  border-radius: 8px;
}

.table-toolbar {
  margin-bottom: 14px;
  display: flex;
  justify-content: space-between;
}

.name-cell {
  display: flex;
  align-items: center;
  gap: 8px;
}

.muted-text {
  font-size: 12px;
  color: #8c8c8c;
}

.views-count {
  font-weight: 600;
  color: #059669;
}

.pagination-wrapper {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>
