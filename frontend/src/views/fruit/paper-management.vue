<template>
  <div class="paper-management-container">
    <!-- 头部区域 -->
    <div class="header-card">
      <div class="header-info">
        <h2 class="page-title">
          <el-icon class="title-icon"><Document /></el-icon>
          学术论文全生命周期台账与文献采集中心
        </h2>
        <p class="page-subtitle">
          全院学术论文登记、DOI/PubMed 自动采集、期刊智能比对（ISSN/CN/分区）、作者团队位次核验与项目基金资助关联。
        </p>
      </div>
      <div class="header-actions">
        <el-button type="primary" :icon="Plus" @click="openAddDialog">登记学术论文</el-button>
        <el-button type="success" :icon="Download" @click="showHarvestDialog = true">
          外部文献采集
        </el-button>
        <el-dropdown trigger="click" @command="handleImportCommand">
          <el-button :icon="Upload">
            批量导入论文<el-icon class="el-icon--right"><ArrowDown /></el-icon>
          </el-button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="cnki">导入知网 (CNKI) 导出文件</el-dropdown-item>
              <el-dropdown-item command="custom">导入自定义 Excel 论文</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
        <el-button :icon="DocumentChecked" @click="goToClaim">进入论文认领</el-button>
        <el-button :icon="Refresh" @click="fetchPapers">刷新台账</el-button>
      </div>
    </div>

    <!-- 关键指标卡片 -->
    <el-row :gutter="16" class="stats-row">
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap blue"><el-icon><Reading /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">全院学术论文</span>
            <span class="stat-value">{{ paperTotal }} <small>篇</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap green"><el-icon><Collection /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">当年正式出版</span>
            <span class="stat-value text-green">{{ currentYearCount }} <small>篇</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap orange"><el-icon><Avatar /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">第一/通讯作者署名</span>
            <span class="stat-value text-orange">{{ primaryAuthorCount }} <small>篇</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap purple"><el-icon><Share /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">课题基金支撑产出</span>
            <span class="stat-value text-purple">{{ fundedPaperCount }} <small>篇</small></span>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 主表格卡片 -->
    <el-card shadow="never" class="main-card">
      <div class="tab-toolbar">
        <div class="toolbar-left">
          <el-input
            v-model="filters.Like论文标题"
            placeholder="搜索论文标题/关键词..."
            :prefix-icon="Search"
            clearable
            style="width: 240px"
            @keyup.enter="fetchPapers"
          />
          <el-input
            v-model="filters.Like第一作者"
            placeholder="搜索第一作者/通信作者..."
            :prefix-icon="Search"
            clearable
            style="width: 180px"
            @keyup.enter="fetchPapers"
          />
          <el-input
            v-model="filters.Like刊物名称"
            placeholder="搜索发表刊物/期刊名称..."
            :prefix-icon="Search"
            clearable
            style="width: 200px"
            @keyup.enter="fetchPapers"
          />
          <el-input
            v-model="filtersYearStr"
            placeholder="发表年度 (如 2026)..."
            clearable
            style="width: 140px"
            @keyup.enter="fetchPapers"
          />
          <el-button type="primary" :icon="Search" @click="fetchPapers">查询</el-button>
          <el-button :icon="Refresh" @click="resetFilters">重置</el-button>
        </div>
      </div>

      <el-table
        v-loading="loading"
        :data="paperList"
        stripe
        border
        style="width: 100%"
      >
        <el-table-column prop="编号" label="编号" width="75" align="center" />
        <el-table-column prop="论文标题" label="论文标题" min-width="260" show-overflow-tooltip>
          <template #default="{ row }">
            <span class="font-medium">{{ row.论文标题 }}</span>
            <div class="article-meta-tags">
              <el-tag v-if="row.DOI" size="small" type="info" class="meta-tag">
                DOI: {{ row.DOI }}
              </el-tag>
              <el-tag v-if="row.PMID" size="small" type="success" class="meta-tag">
                PMID: {{ row.PMID }}
              </el-tag>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="刊物名称" label="发表刊物 / 期刊" min-width="180" show-overflow-tooltip>
          <template #default="{ row }">
            <span>{{ row.刊物名称 || '-' }}</span>
            <div v-if="row.ISSN号" class="text-xs text-muted">ISSN: {{ row.ISSN号 }}</div>
          </template>
        </el-table-column>
        <el-table-column prop="第一作者" label="第一作者" width="110">
          <template #default="{ row }">
            <el-tag size="small" effect="plain">{{ row.第一作者 || '-' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="通讯作者" label="通讯作者" width="110">
          <template #default="{ row }">
            <span class="text-sm">{{ row.通讯作者 || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="出版日期 / 年度" width="140" align="center">
          <template #default="{ row }">
            <div>{{ formatDate(row.论文正式出版日期 || row.论文Online日期) }}</div>
            <div v-if="row.年度" class="text-xs text-muted">{{ row.年度 }} 年度</div>
          </template>
        </el-table-column>
        <el-table-column label="卷期页码" width="130" show-overflow-tooltip>
          <template #default="{ row }">
            <span v-if="row.卷号 || row.期号 || row.页码范围">
              {{ row.卷号 ? `Vol.${row.卷号}` : '' }}
              {{ row.期号 ? `No.${row.期号}` : '' }}
              {{ row.页码范围 ? `pp.${row.页码范围}` : '' }}
            </span>
            <span v-else class="text-muted">-</span>
          </template>
        </el-table-column>
        <el-table-column label="附件与正文" width="110" align="center">
          <template #default="{ row }">
            <el-link
              v-if="row.论文正文路径"
              type="primary"
              :href="getFileUrl(row.论文正文路径)"
              target="_blank"
              :underline="false"
            >
              <el-icon><Document /></el-icon> 正文
            </el-link>
            <span v-else class="text-muted">-</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="130" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="viewDetail(row.编号)">
              详情档案
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-footer">
        <el-pagination
          v-model:current-page="paging.Index"
          v-model:page-size="paging.Size"
          :page-sizes="[10, 15, 20, 50]"
          :total="paperTotal"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="fetchPapers"
          @current-change="fetchPapers"
        />
      </div>
    </el-card>

    <!-- 登记/申报学术论文弹窗 -->
    <el-dialog
      v-model="showAddDialog"
      title="登记 / 申报学术论文成果"
      width="680px"
      destroy-on-close
    >
      <el-form
        ref="formRef"
        :model="form"
        :rules="formRules"
        label-width="120px"
      >
        <el-form-item label="论文标题" prop="论文标题">
          <el-input
            v-model="form.论文标题"
            placeholder="输入完整中英文学术论文标题..."
          />
        </el-form-item>
        <el-form-item label="发表刊物名称" prop="刊物名称">
          <div style="display: flex; gap: 8px; width: 100%">
            <el-input
              v-model="form.刊物名称"
              placeholder="例如: Nature Communications, 中华医学杂志..."
            />
            <el-button type="info" plain :loading="matchingJournal" @click="matchJournalInfo">
              期刊比对
            </el-button>
          </div>
          <!-- 智能匹配期刊候选提示 -->
          <div v-if="matchedJournals.length" class="matched-journal-box">
            <span class="text-xs text-muted">期刊库匹配结果:</span>
            <div
              v-for="j in matchedJournals"
              :key="j.编号"
              class="matched-journal-pill"
              @click="selectMatchedJournal(j)"
            >
              <el-tag size="small" type="success">{{ j.名称 }}</el-tag>
              <span class="text-xs">ISSN: {{ j.ISSN号 || '-' }}</span>
              <span class="text-xs">匹配度: {{ j.匹配分数 }}%</span>
            </div>
          </div>
        </el-form-item>
        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="ISSN号">
              <el-input v-model="form.ISSN号" placeholder="国际标准期刊号" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="CN号">
              <el-input v-model="form.CN号" placeholder="国内统一刊号" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="DOI编码">
              <el-input v-model="form.DOI" placeholder="10.xxxx/..." />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="PMID编号">
              <el-input v-model="form.PMID" placeholder="PubMed唯一索引号" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="Online发表日期">
              <el-date-picker
                v-model="form.论文Online日期"
                type="date"
                placeholder="选择Online日期"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="正式出版日期">
              <el-date-picker
                v-model="form.论文正式出版日期"
                type="date"
                placeholder="选择正式出版日期"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="12">
          <el-col :span="8">
            <el-form-item label="卷号">
              <el-input v-model="form.卷号" placeholder="卷" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="期号">
              <el-input v-model="form.期号" placeholder="期" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="页码范围">
              <el-input v-model="form.页码范围" placeholder="例如: 102-115" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="论文正文路径">
          <el-input
            v-model="form.论文正文路径"
            placeholder="正文相对路径 (如 upload/成果/论文/论文正文/...)"
          />
        </el-form-item>
        <el-form-item label="关键字">
          <el-input v-model="form.关键字" placeholder="多个关键词用分号隔开" />
        </el-form-item>
        <el-form-item label="论文摘要">
          <el-input
            v-model="form.论文摘要"
            type="textarea"
            :rows="3"
            placeholder="论文结构化摘要 (背景、方法、结果、结论)..."
          />
        </el-form-item>
        <el-form-item label="备注说明">
          <el-input v-model="form.备注" placeholder="基金资助号、第一完成单位说明等" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showAddDialog = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="submitAdd">
          提交登记
        </el-button>
      </template>
    </el-dialog>

    <!-- 外部学术文献自动采集弹窗 (Crossref / PubMed) -->
    <el-dialog
      v-model="showHarvestDialog"
      title="从外部权威学术文献库批量采集论文"
      width="560px"
      destroy-on-close
    >
      <el-form label-width="110px" :model="harvestForm">
        <el-form-item label="数据源">
          <el-radio-group v-model="harvestSource">
            <el-radio value="crossref">Crossref (DOI公共库)</el-radio>
            <el-radio value="pubmed">PubMed (生物医药库)</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="检索关键词">
          <el-input
            v-model="harvestForm.关键词"
            placeholder="例如: cardiology, coronary artery disease..."
          />
        </el-form-item>
        <el-form-item label="作者姓名">
          <el-input v-model="harvestForm.作者" placeholder="例如: Zhang San / Li Si" />
        </el-form-item>
        <el-form-item v-if="harvestSource === 'crossref'" label="指定DOI">
          <el-input v-model="harvestForm.DOI" placeholder="10.1016/j.athoracsur.2024..." />
        </el-form-item>
        <el-form-item label="采集上限">
          <el-input-number v-model="harvestForm.行数" :min="1" :max="50" style="width: 140px" />
          <span class="text-xs text-muted" style="margin-left: 10px">条 (单批次建议 10~30 条)</span>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showHarvestDialog = false">取消</el-button>
        <el-button type="primary" :loading="harvesting" @click="submitHarvest">
          开始同步采集
        </el-button>
      </template>
    </el-dialog>

    <!-- 论文详情与流转审核抽屉 -->
    <el-drawer
      v-model="showDetailDrawer"
      title="学术论文全生命周期档案"
      size="580px"
      destroy-on-close
    >
      <div v-loading="loadingDetail" class="detail-drawer-content">
        <template v-if="detailItem">
          <h4 class="paper-drawer-title">{{ detailItem.论文标题 }}</h4>

          <el-descriptions :column="1" border size="small" style="margin-top: 12px">
            <el-descriptions-item label="发表刊物">
              <span class="font-bold">{{ detailItem.刊物名称 || '-' }}</span>
            </el-descriptions-item>
            <el-descriptions-item label="第一作者">
              {{ detailItem.第一作者 || '-' }}
            </el-descriptions-item>
            <el-descriptions-item label="通讯作者">
              {{ detailItem.通讯作者 || '-' }}
            </el-descriptions-item>
            <el-descriptions-item label="出版周期">
              {{ formatDate(detailItem.论文正式出版日期 || detailItem.论文Online日期) }}
              <span v-if="detailItem.年度"> ({{ detailItem.年度 }} 年度)</span>
            </el-descriptions-item>
            <el-descriptions-item label="卷号 / 期号 / 页码">
              {{ detailItem.卷号 || '-' }} / {{ detailItem.期号 || '-' }} / {{ detailItem.页码范围 || '-' }}
            </el-descriptions-item>
            <el-descriptions-item label="DOI / PMID">
              <div>DOI: {{ detailItem.DOI || '-' }}</div>
              <div>PMID: {{ detailItem.PMID || '-' }}</div>
            </el-descriptions-item>
            <el-descriptions-item label="正文附件">
              <el-link
                v-if="detailItem.论文正文路径"
                type="primary"
                :href="getFileUrl(detailItem.论文正文路径)"
                target="_blank"
              >
                <el-icon><Document /></el-icon> 下载论文正文
              </el-link>
              <span v-else class="text-muted">未上传</span>
            </el-descriptions-item>
            <el-descriptions-item label="论文摘要">
              {{ detailItem.论文摘要 || '无摘要内容' }}
            </el-descriptions-item>
          </el-descriptions>

          <!-- 课题项目支撑 -->
          <div style="margin-top: 20px">
            <h5 class="section-title">依托支撑科研课题 ({{ detailProjects.length }})</h5>
            <el-table :data="detailProjects" stripe border size="small" style="margin-top: 8px">
              <el-table-column prop="项目编号" label="项目编号" width="90" align="center" />
              <el-table-column prop="项目中文名称" label="课题名称" min-width="160" show-overflow-tooltip />
              <el-table-column prop="负责人姓名" label="课题负责人" width="100" />
            </el-table>
          </div>

          <!-- 审批流转留痕 -->
          <div style="margin-top: 20px">
            <h5 class="section-title">审核流转历史留痕</h5>
            <div v-if="!auditSteps.length" class="text-muted text-xs" style="margin-top: 8px">
              暂无历史审核步骤或已自动归档
            </div>
            <el-timeline v-else style="margin-top: 12px">
              <el-timeline-item
                v-for="(step, idx) in auditSteps"
                :key="idx"
                :timestamp="formatDate(step.处理时间 || step.建立时间)"
                type="primary"
              >
                <div class="font-medium text-sm">
                  {{ step.步骤名称 || '节点审核' }} -
                  <el-tag size="small" :type="step.处理结果 === '同意' ? 'success' : 'info'">
                    {{ step.处理结果 || '进行中' }}
                  </el-tag>
                </div>
                <div v-if="step.处理人姓名" class="text-xs text-muted" style="margin-top: 4px">
                  审核人: {{ step.处理人姓名 }} ({{ step.处理意见 || '无' }})
                </div>
              </el-timeline-item>
            </el-timeline>
          </div>
        </template>
      </div>
    </el-drawer>

    <!-- 论文批量导入弹窗 -->
    <el-alert
      v-if="showImportDialog && importMode === 'cnki'"
      title="当前仅支持上传 CNKI 导出的 Excel 文件，不代表已接入 CNKI 实时接口；万方接口尚未接入。"
      type="warning"
      :closable="false"
      show-icon
      style="margin: 12px 0"
    />
    <DataImportDialog
      v-model="showImportDialog"
      :title="importMode === 'cnki' ? '导入中国知网 (CNKI) 导出文件' : '批量导入自定义格式学术论文'"
      :template-name="importMode === 'cnki' ? '知网导出文件导入模板.xlsx' : '自定义学术论文导入模板.xlsx'"
      :upload-handler="handleImportPaper"
      @success="fetchPapers"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import DataImportDialog from '@/components/DataImportDialog.vue'
import {
  Document,
  Plus,
  Refresh,
  Search,
  Download,
  DocumentChecked,
  Reading,
  Collection,
  Avatar,
  Share,
  Upload,
  ArrowDown
} from '@element-plus/icons-vue'
import {
  getPagingPapers,
  getPaperDetail,
  addPaper,
  identifyJournal,
  harvestFromCrossref,
  harvestFromPubMed,
  importCNKIPapers,
  importCustomPapers,
  type PaperItem,
  type PaperFilter,
  type MatchedJournal
} from '@/api/fruit'
import { formatDate } from '@/utils/format'

const router = useRouter()

// 列表与分页
const loading = ref(false)
const paperList = ref<PaperItem[]>([])
const paperTotal = ref(0)
const paging = reactive({ Index: 1, Size: 15, OrderType: false })
const filters = reactive<PaperFilter>({
  Like论文标题: '',
  Like第一作者: '',
  Like刊物名称: '',
  年度: undefined
})
const filtersYearStr = ref('')

// 统计指标计算
const currentYear = new Date().getFullYear()
const currentYearCount = computed(() => {
  return paperList.value.filter((p) => p.年度 === currentYear).length
})
const primaryAuthorCount = computed(() => {
  return paperList.value.filter((p) => Boolean(p.第一作者 || p.通讯作者)).length
})
const fundedPaperCount = computed(() => {
  return paperList.value.filter((p) => Boolean(p.备注?.includes('基金') || p.备注?.includes('项目'))).length
})

// 批量导入论文
const showImportDialog = ref(false)
const importMode = ref<'cnki' | 'custom'>('cnki')

function handleImportCommand(command: string) {
  importMode.value = command as 'cnki' | 'custom'
  showImportDialog.value = true
}

async function handleImportPaper(file: File) {
  if (importMode.value === 'cnki') {
    return await importCNKIPapers(file)
  } else {
    return await importCustomPapers(file)
  }
}

// 登记新增论文弹窗
const showAddDialog = ref(false)
const submitting = ref(false)
const formRef = ref<FormInstance>()
const form = reactive({
  论文标题: '',
  刊物名称: '',
  ISSN号: '',
  CN号: '',
  DOI: '',
  PMID: '',
  论文Online日期: '',
  论文正式出版日期: '',
  卷号: '',
  期号: '',
  页码范围: '',
  关键字: '',
  论文摘要: '',
  论文正文路径: '',
  备注: ''
})
const formRules: FormRules = {
  论文标题: [{ required: true, message: '请输入论文标题', trigger: 'blur' }],
  刊物名称: [{ required: true, message: '请输入发表刊物名称', trigger: 'blur' }]
}

// 期刊匹配候选
const matchingJournal = ref(false)
const matchedJournals = ref<MatchedJournal[]>([])

// 外部文献采集弹窗
const showHarvestDialog = ref(false)
const harvesting = ref(false)
const harvestSource = ref<'crossref' | 'pubmed'>('crossref')
const harvestForm = reactive({
  关键词: '',
  作者: '',
  DOI: '',
  行数: 20
})

// 详情抽屉
const showDetailDrawer = ref(false)
const loadingDetail = ref(false)
const detailItem = ref<PaperItem | null>(null)
const detailProjects = ref<any[]>([])
const auditSteps = ref<any[]>([])

// 查询论文列表
async function fetchPapers() {
  loading.value = true
  const yearNum = filtersYearStr.value ? Number(filtersYearStr.value) : undefined
  filters.年度 = isNaN(yearNum as number) ? undefined : yearNum
  try {
    const res = await getPagingPapers(paging, filters)
    const data = (res as any)?.data || res
    paperList.value = data?.list || []
    paperTotal.value = data?.total || 0
  } catch (err: any) {
    ElMessage.error(err.message || '获取论文列表失败')
  } finally {
    loading.value = false
  }
}

function resetFilters() {
  filters.Like论文标题 = ''
  filters.Like第一作者 = ''
  filters.Like刊物名称 = ''
  filters.年度 = undefined
  filtersYearStr.value = ''
  paging.Index = 1
  fetchPapers()
}

// 打开新增弹窗
function openAddDialog() {
  form.论文标题 = ''
  form.刊物名称 = ''
  form.ISSN号 = ''
  form.CN号 = ''
  form.DOI = ''
  form.PMID = ''
  form.论文Online日期 = ''
  form.论文正式出版日期 = ''
  form.卷号 = ''
  form.期号 = ''
  form.页码范围 = ''
  form.关键字 = ''
  form.论文摘要 = ''
  form.论文正文路径 = ''
  form.备注 = ''
  matchedJournals.value = []
  showAddDialog.value = true
}

// 智能比对期刊库
async function matchJournalInfo() {
  if (!form.刊物名称 && !form.ISSN号 && !form.CN号) {
    ElMessage.warning('请先输入刊物名称或 ISSN/CN 号')
    return
  }
  matchingJournal.value = true
  try {
    const res = await identifyJournal({
      刊物名称: form.刊物名称.trim() || undefined,
      ISSN号: form.ISSN号.trim() || undefined,
      CN号: form.CN号.trim() || undefined
    })
    const data = (res as any)?.data || res
    matchedJournals.value = Array.isArray(data) ? data : []
    if (!matchedJournals.value.length) {
      ElMessage.info('未在期刊库检索到高匹配度刊物，可手动录入')
    } else {
      ElMessage.success(`成功在期刊库匹配到 ${matchedJournals.value.length} 个相关刊物`)
    }
  } catch (err: any) {
    ElMessage.error(err.message || '期刊比对失败')
  } finally {
    matchingJournal.value = false
  }
}

function selectMatchedJournal(j: MatchedJournal) {
  form.刊物名称 = j.名称
  if (j.ISSN号) form.ISSN号 = j.ISSN号
  if (j.CN号) form.CN号 = j.CN号
  ElMessage.success(`已应用期刊规范: ${j.名称}`)
}

// 提交新增论文
async function submitAdd() {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitting.value = true
    try {
      await addPaper({
        基本资料: {
          论文标题: form.论文标题.trim(),
          刊物名称: form.刊物名称.trim(),
          DOI: form.DOI.trim() || undefined,
          PMID: form.PMID.trim() || undefined,
          论文Online日期: form.论文Online日期 || undefined,
          论文正式出版日期: form.论文正式出版日期 || undefined,
          卷号: form.卷号.trim() || undefined,
          期号: form.期号.trim() || undefined,
          页码范围: form.页码范围.trim() || undefined,
          关键字: form.关键字.trim() || undefined,
          论文摘要: form.论文摘要.trim() || undefined,
          论文正文路径: form.论文正文路径.trim() || undefined,
          备注: form.备注.trim() || undefined
        }
      })
      ElMessage.success('学术论文登记成功并已发起流程流转')
      showAddDialog.value = false
      fetchPapers()
    } catch (err: any) {
      ElMessage.error(err.message || '登记论文失败')
    } finally {
      submitting.value = false
    }
  })
}

// 外部文献采集
async function submitHarvest() {
  if (!harvestForm.关键词 && !harvestForm.作者 && !harvestForm.DOI) {
    ElMessage.warning('关键词、作者或 DOI 至少填写一项')
    return
  }
  harvesting.value = true
  try {
    if (harvestSource.value === 'crossref') {
      await harvestFromCrossref({
        外部系统编号: 8, // 论文库外部系统
        关键词: harvestForm.关键词 || undefined,
        作者: harvestForm.作者 || undefined,
        DOI: harvestForm.DOI || undefined,
        行数: harvestForm.行数
      })
      ElMessage.success('Crossref 论文同步采集成功，新采集成果已加入认领队列')
    } else {
      await harvestFromPubMed({
        外部系统编号: 8,
        关键词: harvestForm.关键词 || undefined,
        作者: harvestForm.作者 || undefined,
        行数: harvestForm.行数
      })
      ElMessage.success('PubMed 论文同步采集成功，已进入待认领队列')
    }
    showHarvestDialog.value = false
    fetchPapers()
  } catch (err: any) {
    ElMessage.error(err.message || '文献同步采集失败')
  } finally {
    harvesting.value = false
  }
}

// 查看论文详情档案
async function viewDetail(id: number) {
  showDetailDrawer.value = true
  loadingDetail.value = true
  try {
    const res = await getPaperDetail(id)
    const data = (res as any)?.data || res
    detailItem.value = data?.论文详情 || null
    detailProjects.value = data?.成果项目 || []
    auditSteps.value = data?.审核情况 || []
  } catch (err: any) {
    ElMessage.error(err.message || '获取论文详情失败')
  } finally {
    loadingDetail.value = false
  }
}

function goToClaim() {
  router.push('/fruit/paper-claim')
}

function getFileUrl(path?: string) {
  if (!path) return '#'
  if (path.startsWith('http')) return path
  return `/${path.replace(/^\/+/, '')}`
}

onMounted(() => {
  fetchPapers()
})
</script>

<style scoped>
.paper-management-container {
  padding: 16px 20px 30px;
  background-color: #f6f8fa;
  min-height: calc(100vh - 84px);
}

.header-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #ffffff;
  padding: 18px 24px;
  border-radius: 8px;
  border: 1px solid #ebeef5;
  margin-bottom: 16px;
}

.page-title {
  margin: 0 0 6px 0;
  font-size: 20px;
  font-weight: 600;
  color: #303133;
  display: flex;
  align-items: center;
  gap: 8px;
}

.title-icon {
  color: #409eff;
}

.page-subtitle {
  margin: 0;
  font-size: 13px;
  color: #909399;
}

.stats-row {
  margin-bottom: 16px;
}

.stat-card {
  border-radius: 8px;
  display: flex;
  align-items: center;
  padding: 8px 12px;
}

:deep(.stat-card .el-card__body) {
  display: flex;
  align-items: center;
  width: 100%;
  padding: 10px 14px;
}

.stat-icon-wrap {
  width: 46px;
  height: 46px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
  margin-right: 14px;
}

.stat-icon-wrap.blue {
  background: #ecf5ff;
  color: #409eff;
}
.stat-icon-wrap.green {
  background: #f0f9eb;
  color: #67c23a;
}
.stat-icon-wrap.orange {
  background: #fdf6ec;
  color: #e6a23c;
}
.stat-icon-wrap.purple {
  background: #f4f0ff;
  color: #8e44ad;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 12px;
  color: #909399;
  margin-bottom: 4px;
}

.stat-value {
  font-size: 22px;
  font-weight: 700;
  color: #303133;
}

.stat-value small {
  font-size: 12px;
  font-weight: normal;
}

.text-green {
  color: #67c23a;
}
.text-orange {
  color: #e6a23c;
}
.text-purple {
  color: #8e44ad;
}

.main-card {
  border-radius: 8px;
  border: 1px solid #ebeef5;
}

.tab-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  margin-bottom: 16px;
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.pagination-footer {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}

.font-medium {
  font-weight: 500;
}
.font-bold {
  font-weight: 700;
}
.text-muted {
  color: #909399;
}
.text-xs {
  font-size: 12px;
}
.text-sm {
  font-size: 13px;
}

.article-meta-tags {
  margin-top: 4px;
  display: flex;
  gap: 6px;
}

.meta-tag {
  font-size: 11px;
}

.matched-journal-box {
  margin-top: 8px;
  background: #f8fbfd;
  padding: 8px 12px;
  border-radius: 6px;
  border: 1px dashed #d9ecff;
  width: 100%;
}

.matched-journal-pill {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 4px;
  cursor: pointer;
  padding: 4px 6px;
  border-radius: 4px;
  transition: background 0.2s;
}

.matched-journal-pill:hover {
  background: #ecf5ff;
}

/* 抽屉详情 */
.detail-drawer-content {
  padding: 8px 0;
}

.paper-drawer-title {
  margin: 0 0 10px;
  font-size: 17px;
  font-weight: 600;
  color: #303133;
  line-height: 1.4;
}

.section-title {
  margin: 0 0 8px;
  font-size: 14px;
  font-weight: 600;
  color: #303133;
}
</style>
