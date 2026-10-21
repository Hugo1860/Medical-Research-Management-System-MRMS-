<template>
  <div class="page-container">
    <!-- 顶部概览指标与主动网推中枢 -->
    <el-card shadow="never" class="overview-card">
      <div class="overview-header">
        <div class="overview-title-block">
          <h2 class="overview-title">
            <el-icon class="title-icon"><Cpu /></el-icon>
            论文主动大数据网推与学者消歧中枢
          </h2>
          <p class="overview-subtitle">
            对标清华长庚医院实践：多源大数据接口主动抓取、机构别名与学者拼音智能消歧、中科院分区与影响因子免录入自动打标
          </p>
        </div>
        <div class="overview-actions">
          <el-button
            type="primary"
            :loading="scanning"
            :icon="Refresh"
            @click="handleTriggerActivePush"
          >
            全网主动网推巡检与消歧打标
          </el-button>
          <el-button
            type="default"
            :icon="Setting"
            @click="activeTab = 'disambiguation'"
          >
            机构与学者消歧库
          </el-button>
        </div>
      </div>

      <el-row :gutter="16" class="metric-row">
        <el-col :xs="12" :sm="6">
          <div class="metric-card metric-blue">
            <div class="metric-label">网推学术成果库</div>
            <div class="metric-value">{{ pushList.length }} <span class="unit">篇</span></div>
            <div class="metric-footer">Crossref / PubMed 自动巡检</div>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="metric-card metric-green">
            <div class="metric-label">高置信度待认领</div>
            <div class="metric-value">{{ highConfidenceCount }} <span class="unit">篇</span></div>
            <div class="metric-footer">置信度 ≥ 75% 智能精准匹配</div>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="metric-card metric-purple">
            <div class="metric-label">中科院1区 / Top期刊</div>
            <div class="metric-value">{{ casTopCount }} <span class="unit">篇</span></div>
            <div class="metric-footer">自动关联2.5万+期刊影响因子</div>
          </div>
        </el-col>
        <el-col :xs="12" :sm="6">
          <div class="metric-card metric-amber">
            <div class="metric-label">已确认入库归档</div>
            <div class="metric-value">{{ claimedCount }} <span class="unit">篇</span></div>
            <div class="metric-footer">自动写入成果论文与作者署名</div>
          </div>
        </el-col>
      </el-row>
    </el-card>

    <!-- 标签页主卡片 -->
    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab">
        <!-- 标签页 1：大数据主动网推与消歧推荐 -->
        <el-tab-pane label="大数据主动网推中心" name="activePush">
          <div class="toolbar">
            <el-form :inline="true" :model="pushFilter" class="filter-form">
              <el-form-item label="论文题名/DOI">
                <el-input
                  v-model="pushFilter.keyword"
                  placeholder="检索论文题目、DOI或期刊"
                  clearable
                  style="width: 220px;"
                />
              </el-form-item>
              <el-form-item label="网推状态">
                <el-select v-model="pushFilter.status" placeholder="全部状态" clearable style="width: 140px;">
                  <el-option label="全部状态" value="" />
                  <el-option label="高置信度网推" value="高置信度网推" />
                  <el-option label="普通待认领" value="普通待认领" />
                  <el-option label="已确认认领" value="已确认认领" />
                </el-select>
              </el-form-item>
              <el-form-item label="中科院分区">
                <el-select v-model="pushFilter.casPartition" placeholder="全部分区" clearable style="width: 150px;">
                  <el-option label="全部分区" value="" />
                  <el-option label="中科院1区(Top)" value="1区" />
                  <el-option label="中科院2区" value="2区" />
                  <el-option label="中科院3区" value="3区" />
                  <el-option label="中科院4区" value="4区" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" :icon="Search" @click="fetchActivePushList">查询</el-button>
                <el-button :icon="Refresh" @click="resetPushFilter">重置</el-button>
              </el-form-item>
            </el-form>
          </div>

          <el-table
            v-loading="pushLoading"
            :data="filteredPushList"
            stripe
            border
            style="width: 100%; margin-top: 12px;"
          >
            <el-table-column prop="编号" label="编号" width="70" align="center" />
            <el-table-column label="论文题目 / DOI" min-width="280">
              <template #default="{ row }">
                <div class="paper-title">{{ row.论文标题 }}</div>
                <div v-if="row.外部论文编号" class="paper-doi">
                  <span class="doi-label">DOI / PMID:</span>
                  <a
                    v-if="row.外部论文编号.startsWith('10.')"
                    :href="`https://doi.org/${row.外部论文编号}`"
                    target="_blank"
                    class="doi-link"
                  >
                    {{ row.外部论文编号 }}
                  </a>
                  <span v-else>{{ row.外部论文编号 }}</span>
                </div>
                <div class="paper-author-line">
                  <span class="author-label">检索署名：</span>
                  <span class="author-text">{{ row.作者 || '-' }}</span>
                </div>
              </template>
            </el-table-column>

            <el-table-column label="期刊与影响力指标" min-width="200">
              <template #default="{ row }">
                <div class="journal-title">{{ row.刊物名称 }}</div>
                <div class="journal-tags">
                  <el-tag
                    v-if="row.中科院分区"
                    :type="getPartitionTagType(row.中科院分区)"
                    size="small"
                    effect="dark"
                    class="metric-badge"
                  >
                    {{ row.中科院分区 }}
                  </el-tag>
                  <el-tag
                    v-if="row.是否Top期刊"
                    type="danger"
                    size="small"
                    effect="plain"
                    class="metric-badge"
                  >
                    ★ Top期刊
                  </el-tag>
                  <el-tag
                    v-if="row.影响因子"
                    type="warning"
                    size="small"
                    class="metric-badge"
                  >
                    IF: {{ Number(row.影响因子).toFixed(2) }}
                  </el-tag>
                  <el-tag
                    v-if="row.jcR分区"
                    type="info"
                    size="small"
                    class="metric-badge"
                  >
                    {{ row.jcR分区 }}
                  </el-tag>
                </div>
                <div v-if="row.issN号" class="journal-meta">ISSN: {{ row.issN号 }}</div>
              </template>
            </el-table-column>

            <el-table-column label="智能消歧与推荐认领" min-width="200">
              <template #default="{ row }">
                <div v-if="row.推荐认领人姓名" class="claimant-info">
                  <span class="claimant-name">
                    <el-icon><Check /></el-icon>
                    {{ row.推荐认领人姓名 }}
                  </span>
                  <el-tag
                    :type="getConfidenceTagType(row.消歧置信度)"
                    size="small"
                    class="confidence-tag"
                  >
                    匹配度 {{ row.消歧置信度 || 0 }}%
                  </el-tag>
                </div>
                <div v-else class="claimant-empty">未匹配到确切学者</div>

                <div v-if="row.消歧命中依据" class="match-reason">
                  <el-tooltip :content="row.消歧命中依据" placement="top">
                    <span class="reason-text">依据: {{ row.消歧命中依据 }}</span>
                  </el-tooltip>
                </div>
              </template>
            </el-table-column>

            <el-table-column label="网推状态" width="120" align="center">
              <template #default="{ row }">
                <el-tag
                  :type="getPushStatusTagType(row.网推状态)"
                  effect="light"
                  size="small"
                >
                  {{ row.网推状态 || '待处理' }}
                </el-tag>
              </template>
            </el-table-column>

            <el-table-column label="操作" width="130" fixed="right" align="center">
              <template #default="{ row }">
                <el-button
                  v-if="row.网推状态 !== '已确认认领'"
                  type="primary"
                  size="small"
                  @click="openConfirmClaimDialog(row)"
                >
                  一键确认认领
                </el-button>
                <div v-else class="archived-text">
                  <el-icon color="#10b981"><Check /></el-icon>
                  已入库归档
                </div>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签页 2：外部学术库自动采集 -->
        <el-tab-pane label="外部学术库自动采集 (PubMed / Crossref)" name="harvest">
          <div class="harvest-box">
            <el-alert
              title="支持通过 Crossref 与 PubMed 全球学术元数据接口自动同步本单位新发表成果至主动网推池。"
              type="info"
              :closable="false"
              show-icon
              style="margin-bottom: 20px;"
            />

            <el-form :model="harvestForm" label-width="130px" style="max-width: 620px;">
              <el-form-item label="采集数据源">
                <el-radio-group v-model="harvestForm.source">
                  <el-radio value="Crossref">Crossref 全球DOI元数据</el-radio>
                  <el-radio value="PubMed">PubMed 生物医药文献库</el-radio>
                </el-radio-group>
              </el-form-item>

              <el-form-item label="检索关键词/DOI">
                <el-input
                  v-model="harvestForm.keyword"
                  placeholder="请输入文章DOI、关键词或单位机构英文标识"
                />
              </el-form-item>

              <el-form-item label="最大采集篇数">
                <el-input-number v-model="harvestForm.limit" :min="1" :max="50" />
              </el-form-item>

              <el-form-item>
                <el-button type="primary" :loading="harvesting" @click="handleHarvest">
                  立即启动文献同步与入库
                </el-button>
              </el-form-item>
            </el-form>
          </div>
        </el-tab-pane>

        <!-- 标签页 3：机构别名与学者消歧知识库 -->
        <el-tab-pane label="机构别名与学者消歧知识库" name="disambiguation">
          <div class="knowledge-container">
            <el-row :gutter="20">
              <!-- 左侧：机构别名知识库 -->
              <el-col :xs="24" :md="12">
                <el-card shadow="never" class="sub-card">
                  <template #header>
                    <div class="card-header-flex">
                      <span>
                        <strong>医院科研单位多源别名库</strong>
                        <span class="sub-tip">（国际多中心/中英文署名规范）</span>
                      </span>
                      <el-button type="primary" size="small" :icon="Plus" @click="openAddAliasDialog">
                        新增机构别名
                      </el-button>
                    </div>
                  </template>

                  <el-table :data="disambigConfigs.机构别名" stripe size="small" max-height="400">
                    <el-table-column prop="别名" label="机构英文/缩写别名" min-width="180">
                      <template #default="{ row }">
                        <span style="font-weight: 600; color: #1e293b;">{{ row.别名 }}</span>
                      </template>
                    </el-table-column>
                    <el-table-column prop="来源" label="数据源" width="100">
                      <template #default="{ row }">
                        <el-tag size="small" type="info">{{ row.来源 || '手动' }}</el-tag>
                      </template>
                    </el-table-column>
                    <el-table-column prop="是否启用" label="状态" width="80" align="center">
                      <template #default="{ row }">
                        <el-tag :type="row.是否启用 ? 'success' : 'danger'" size="small">
                          {{ row.是否启用 ? '启用' : '停用' }}
                        </el-tag>
                      </template>
                    </el-table-column>
                  </el-table>
                </el-card>
              </el-col>

              <!-- 右侧：学者署名消歧库 -->
              <el-col :xs="24" :md="12">
                <el-card shadow="never" class="sub-card">
                  <template #header>
                    <div class="card-header-flex">
                      <span>
                        <strong>学者英文署名与ORCID消歧库</strong>
                        <span class="sub-tip">（解决同音、缩写重名冲突）</span>
                      </span>
                      <el-button type="primary" size="small" :icon="Plus" @click="openAddScholarDialog">
                        新增学者署名
                      </el-button>
                    </div>
                  </template>

                  <el-table :data="disambigConfigs.学者消歧" stripe size="small" max-height="400">
                    <el-table-column prop="姓名" label="学者姓名" width="90">
                      <template #default="{ row }">
                        <strong>{{ row.姓名 }}</strong>
                      </template>
                    </el-table-column>
                    <el-table-column prop="英文拼音署名" label="拼音署名变体" min-width="120" />
                    <el-table-column prop="orcid" label="ORCID" width="140" show-overflow-tooltip />
                    <el-table-column prop="科室名称" label="归属科室" width="100" show-overflow-tooltip />
                  </el-table>
                </el-card>
              </el-col>
            </el-row>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 一键认领确认弹窗 -->
    <el-dialog
      v-model="claimDialogVisible"
      title="一键确认认领网推学术成果"
      width="540px"
      destroy-on-close
    >
      <div v-if="currentPushPaper" class="claim-dialog-content">
        <div class="dialog-paper-box">
          <div class="dialog-paper-title">{{ currentPushPaper.论文标题 }}</div>
          <div class="dialog-paper-meta">
            <span>{{ currentPushPaper.刊物名称 }}</span>
            <el-tag
              v-if="currentPushPaper.中科院分区"
              size="small"
              :type="getPartitionTagType(currentPushPaper.中科院分区)"
              style="margin-left: 8px;"
            >
              {{ currentPushPaper.中科院分区 }}
            </el-tag>
            <el-tag
              v-if="currentPushPaper.是否Top期刊"
              size="small"
              type="danger"
              style="margin-left: 6px;"
            >
              Top期刊
            </el-tag>
            <span v-if="currentPushPaper.影响因子" style="margin-left: 8px; color: #d97706; font-weight: 600;">
              IF: {{ Number(currentPushPaper.影响因子).toFixed(2) }}
            </span>
          </div>
        </div>

        <el-form :model="claimForm" label-width="110px" style="margin-top: 16px;">
          <el-form-item label="推荐认领学者">
            <el-input
              :value="currentPushPaper.推荐认领人姓名 ? `${currentPushPaper.推荐认领人姓名} (置信度: ${currentPushPaper.消歧置信度}%)` : '未指定（以当前登录人为主）'"
              disabled
            />
          </el-form-item>

          <el-form-item label="作者位次" required>
            <el-select v-model="claimForm.作者位次" style="width: 100%;">
              <el-option label="第一作者 (First Author)" value="第一作者" />
              <el-option label="共同第一作者 (Co-First Author)" value="共同第一作者" />
              <el-option label="通讯作者 (Corresponding Author)" value="通讯作者" />
              <el-option label="共同通讯作者 (Co-Corresponding)" value="共同通讯作者" />
              <el-option label="主要参与作者 (Co-Author)" value="主要参与作者" />
            </el-select>
          </el-form-item>

          <el-form-item label="成果认领说明">
            <el-input
              v-model="claimForm.认领说明"
              type="textarea"
              :rows="3"
              placeholder="可选填写署名单位、第一署名单位说明或科研基金资助标注信息"
            />
          </el-form-item>
        </el-form>
      </div>

      <template #footer>
        <el-button @click="claimDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleConfirmClaimSubmit">
          确认认领并归档入库
        </el-button>
      </template>
    </el-dialog>

    <!-- 新增机构别名弹窗 -->
    <el-dialog v-model="addAliasVisible" title="新增医院机构别名" width="460px">
      <el-form :model="aliasForm" label-width="100px">
        <el-form-item label="机构别名" required>
          <el-input
            v-model="aliasForm.别名"
            placeholder="如: Beijing Tsinghua Changgung Hospital"
          />
        </el-form-item>
        <el-form-item label="别名来源">
          <el-input v-model="aliasForm.来源" placeholder="如: PubMed / Crossref / WOS" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="addAliasVisible = false">取消</el-button>
        <el-button type="primary" :loading="aliasSubmitting" @click="handleSaveAlias">
          保存别名
        </el-button>
      </template>
    </el-dialog>

    <!-- 新增学者署名消歧弹窗 -->
    <el-dialog v-model="addScholarVisible" title="新增学者署名消歧映射" width="480px">
      <el-form :model="scholarForm" label-width="110px">
        <el-form-item label="人员工号/姓名" required>
          <el-input
            v-model="scholarForm.人员编号Input"
            placeholder="请输入人员编号 (如: 1 或 2)"
          />
        </el-form-item>
        <el-form-item label="英文拼音署名" required>
          <el-input
            v-model="scholarForm.英文拼音署名"
            placeholder="如: Zhang, S. 或 Zhang, San"
          />
        </el-form-item>
        <el-form-item label="ORCID">
          <el-input
            v-model="scholarForm.ORCID"
            placeholder="如: 0000-0002-1825-0097"
          />
        </el-form-item>
        <el-form-item label="所属科室">
          <el-input v-model="scholarForm.科室名称" placeholder="如: 肝胆胰外科 / 肿瘤科" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="addScholarVisible = false">取消</el-button>
        <el-button type="primary" :loading="scholarSubmitting" @click="handleSaveScholar">
          保存学者署名
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import {
  getActivePushPapers,
  triggerActivePaperPush,
  confirmClaimPushPaper,
  getDisambiguationConfigs,
  addInstitutionalAlias,
  addScholarDisambiguation,
  harvestFromCrossref,
  harvestFromPubMed,
  type ActivePushPaperItem,
  type DisambiguationConfigData
} from '@/api/fruit'
import { ElMessage } from 'element-plus'
import {
  Search,
  Refresh,
  Cpu,
  Check,
  Plus,
  Setting
} from '@element-plus/icons-vue'

const activeTab = ref('activePush')
const pushLoading = ref(false)
const scanning = ref(false)
const harvesting = ref(false)
const submitLoading = ref(false)

// 主动网推数据列表
const pushList = ref<ActivePushPaperItem[]>([])
const pushFilter = reactive({
  keyword: '',
  status: '',
  casPartition: ''
})

// 机构与学者消歧知识库
const disambigConfigs = ref<DisambiguationConfigData>({
  机构别名: [],
  学者消歧: []
})

// 采集表单
const harvestForm = reactive({
  source: 'Crossref',
  keyword: '10.1038/s41591-026-03888-x',
  limit: 10
})

// 认领确认弹窗
const claimDialogVisible = ref(false)
const currentPushPaper = ref<ActivePushPaperItem | null>(null)
const claimForm = reactive({
  作者位次: '第一作者',
  认领说明: ''
})

// 机构别名弹窗
const addAliasVisible = ref(false)
const aliasSubmitting = ref(false)
const aliasForm = reactive({
  别名: '',
  来源: '人工维护'
})

// 学者消歧弹窗
const addScholarVisible = ref(false)
const scholarSubmitting = ref(false)
const scholarForm = reactive({
  人员编号Input: '1',
  英文拼音署名: '',
  ORCID: '',
  科室名称: ''
})

// 统计计算
const highConfidenceCount = computed(() => {
  return pushList.value.filter(p => (p.消歧置信度 || 0) >= 75).length
})

const casTopCount = computed(() => {
  return pushList.value.filter(p => (p.中科院分区 && p.中科院分区.includes('1区')) || p.是否Top期刊).length
})

const claimedCount = computed(() => {
  return pushList.value.filter(p => p.网推状态 === '已确认认领').length
})

// 本地筛选结果
const filteredPushList = computed(() => {
  return pushList.value.filter(item => {
    if (pushFilter.status && item.网推状态 !== pushFilter.status) {
      return false
    }
    if (pushFilter.casPartition && (!item.中科院分区 || !item.中科院分区.includes(pushFilter.casPartition))) {
      return false
    }
    if (pushFilter.keyword) {
      const kw = pushFilter.keyword.toLowerCase()
      const titleMatch = item.论文标题?.toLowerCase().includes(kw)
      const doiMatch = item.外部论文编号?.toLowerCase().includes(kw)
      const journalMatch = item.刊物名称?.toLowerCase().includes(kw)
      const authorMatch = item.作者?.toLowerCase().includes(kw)
      if (!titleMatch && !doiMatch && !journalMatch && !authorMatch) {
        return false
      }
    }
    return true
  })
})

function getPartitionTagType(partition?: string): '' | 'success' | 'warning' | 'danger' | 'info' {
  if (!partition) return 'info'
  if (partition.includes('1区')) return 'danger'
  if (partition.includes('2区')) return 'warning'
  if (partition.includes('3区')) return 'primary' as any
  return 'info'
}

function getConfidenceTagType(confidence?: number): '' | 'success' | 'warning' | 'danger' | 'info' {
  const c = confidence || 0
  if (c >= 75) return 'success'
  if (c >= 50) return 'warning'
  return 'info'
}

function getPushStatusTagType(status?: string): '' | 'success' | 'warning' | 'danger' | 'info' {
  if (status === '高置信度网推') return 'success'
  if (status === '已确认认领') return 'info'
  if (status === '普通待认领') return 'warning'
  return 'info'
}

// 获取网推台账列表
async function fetchActivePushList() {
  pushLoading.value = true
  try {
    const res = await getActivePushPapers({
      status: pushFilter.status,
      casPartition: pushFilter.casPartition,
      keyword: pushFilter.keyword
    })
    if (res && Array.isArray(res)) {
      pushList.value = res
    }
  } catch (err) {
    console.warn('获取网推台账失败，使用初始缓存', err)
  } finally {
    pushLoading.value = false
  }
}

function resetPushFilter() {
  pushFilter.keyword = ''
  pushFilter.status = ''
  pushFilter.casPartition = ''
  fetchActivePushList()
}

// 获取机构与学者消歧配置
async function fetchDisambiguationData() {
  try {
    const res = await getDisambiguationConfigs()
    if (res) {
      disambigConfigs.value = {
        机构别名: res.机构别名 || [],
        学者消歧: res.学者消歧 || []
      }
    }
  } catch (err) {
    console.warn('获取消歧配置失败', err)
  }
}

// 触发全网论文主动网推巡检与打标
async function handleTriggerActivePush() {
  scanning.value = true
  try {
    const res = await triggerActivePaperPush()
    ElMessage.success(
      res.message || `巡检完成：扫描 ${res.total_scanned} 篇，高置信度网推 ${res.high_confidence_pushed} 篇，分区匹配 ${res.partition_matched} 篇`
    )
    await fetchActivePushList()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '巡检触发失败')
  } finally {
    scanning.value = false
  }
}

// 打开认领弹窗
function openConfirmClaimDialog(row: ActivePushPaperItem) {
  currentPushPaper.value = row
  claimForm.作者位次 = '第一作者'
  claimForm.认领说明 = ''
  claimDialogVisible.value = true
}

// 提交确认认领
async function handleConfirmClaimSubmit() {
  if (!currentPushPaper.value) return
  submitLoading.value = true
  try {
    const res = await confirmClaimPushPaper({
      采集结果编号: currentPushPaper.value.编号,
      认领人编号: currentPushPaper.value.推荐认领人编号 || undefined,
      作者位次: claimForm.作者位次,
      认领说明: claimForm.认领说明
    })
    ElMessage.success(res.message || '成果认领归档成功！')
    claimDialogVisible.value = false
    await fetchActivePushList()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '认领确认失败')
  } finally {
    submitLoading.value = false
  }
}

// 文献采集
async function handleHarvest() {
  if (!harvestForm.keyword.trim()) {
    ElMessage.warning('请输入检索词或DOI')
    return
  }
  harvesting.value = true
  try {
    if (harvestForm.source === 'Crossref') {
      await harvestFromCrossref({ DOI: harvestForm.keyword, 最大记录数: harvestForm.limit })
    } else {
      await harvestFromPubMed({ 检索词: harvestForm.keyword, 最大记录数: harvestForm.limit })
    }
    ElMessage.success('文献采集与入库成功，已同步至主动网推池')
    activeTab.value = 'activePush'
    await fetchActivePushList()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '采集失败')
  } finally {
    harvesting.value = false
  }
}

// 新增机构别名
function openAddAliasDialog() {
  aliasForm.别名 = ''
  aliasForm.来源 = '人工维护'
  addAliasVisible.value = true
}

async function handleSaveAlias() {
  if (!aliasForm.别名.trim()) {
    ElMessage.warning('请输入机构别名')
    return
  }
  aliasSubmitting.value = true
  try {
    await addInstitutionalAlias({
      别名: aliasForm.别名.trim(),
      来源: aliasForm.来源.trim()
    })
    ElMessage.success('新增机构别名成功')
    addAliasVisible.value = false
    await fetchDisambiguationData()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '保存别名失败')
  } finally {
    aliasSubmitting.value = false
  }
}

// 新增学者消歧
function openAddScholarDialog() {
  scholarForm.人员编号Input = '1'
  scholarForm.英文拼音署名 = ''
  scholarForm.ORCID = ''
  scholarForm.科室名称 = ''
  addScholarVisible.value = true
}

async function handleSaveScholar() {
  const empId = parseInt(scholarForm.人员编号Input, 10)
  if (isNaN(empId) || !scholarForm.英文拼音署名.trim()) {
    ElMessage.warning('请正确填写人员编号与英文拼音署名')
    return
  }
  scholarSubmitting.value = true
  try {
    await addScholarDisambiguation({
      人员编号: empId,
      英文拼音署名: scholarForm.英文拼音署名.trim(),
      ORCID: scholarForm.ORCID.trim() || undefined,
      科室名称: scholarForm.科室名称.trim() || undefined
    })
    ElMessage.success('新增学者消歧署名成功')
    addScholarVisible.value = false
    await fetchDisambiguationData()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '保存学者消歧失败')
  } finally {
    scholarSubmitting.value = false
  }
}

onMounted(async () => {
  await Promise.all([
    fetchActivePushList(),
    fetchDisambiguationData()
  ])
})
</script>

<style scoped lang="scss">
.page-container {
  padding: 16px;

  .overview-card {
    background-color: #ffffff;
    border-radius: 8px;
    margin-bottom: 16px;

    .overview-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
      flex-wrap: wrap;
      gap: 12px;

      .overview-title-block {
        .overview-title {
          font-size: 18px;
          font-weight: 700;
          color: #0f172a;
          margin: 0 0 6px 0;
          display: flex;
          align-items: center;
          gap: 8px;

          .title-icon {
            color: #2563eb;
            font-size: 22px;
          }
        }

        .overview-subtitle {
          font-size: 13px;
          color: #64748b;
          margin: 0;
        }
      }

      .overview-actions {
        display: flex;
        gap: 10px;
      }
    }

    .metric-row {
      .metric-card {
        padding: 14px 16px;
        border-radius: 8px;
        color: #ffffff;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        min-height: 96px;

        .metric-label {
          font-size: 13px;
          font-weight: 500;
          opacity: 0.9;
        }

        .metric-value {
          font-size: 24px;
          font-weight: 700;
          margin: 6px 0;

          .unit {
            font-size: 14px;
            font-weight: 400;
            margin-left: 2px;
          }
        }

        .metric-footer {
          font-size: 11px;
          opacity: 0.8;
        }

        &.metric-blue {
          background: linear-gradient(135deg, #1e40af 0%, #3b82f6 100%);
        }
        &.metric-green {
          background: linear-gradient(135deg, #065f46 0%, #10b981 100%);
        }
        &.metric-purple {
          background: linear-gradient(135deg, #5b21b6 0%, #8b5cf6 100%);
        }
        &.metric-amber {
          background: linear-gradient(135deg, #92400e 0%, #f59e0b 100%);
        }
      }
    }
  }

  .main-card {
    background-color: #ffffff;
    border-radius: 8px;

    .toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
    }

    .paper-title {
      font-weight: 600;
      color: #1e293b;
      line-height: 1.4;
      font-size: 14px;
    }

    .paper-doi {
      margin-top: 4px;
      font-size: 12px;
      color: #64748b;

      .doi-label {
        font-weight: 500;
        margin-right: 4px;
      }

      .doi-link {
        color: #2563eb;
        text-decoration: none;

        &:hover {
          text-decoration: underline;
        }
      }
    }

    .paper-author-line {
      margin-top: 4px;
      font-size: 12px;
      color: #475569;

      .author-label {
        color: #94a3b8;
      }
    }

    .journal-title {
      font-weight: 600;
      color: #0f172a;
      font-size: 13px;
    }

    .journal-tags {
      margin-top: 6px;
      display: flex;
      flex-wrap: wrap;
      gap: 4px;

      .metric-badge {
        font-weight: 600;
      }
    }

    .journal-meta {
      font-size: 11px;
      color: #94a3b8;
      margin-top: 4px;
    }

    .claimant-info {
      display: flex;
      align-items: center;
      gap: 6px;

      .claimant-name {
        font-weight: 600;
        color: #1e293b;
        display: flex;
        align-items: center;
        gap: 3px;
      }

      .confidence-tag {
        font-weight: 600;
      }
    }

    .claimant-empty {
      font-size: 13px;
      color: #94a3b8;
    }

    .match-reason {
      margin-top: 4px;

      .reason-text {
        font-size: 11px;
        color: #64748b;
        background-color: #f1f5f9;
        padding: 2px 6px;
        border-radius: 4px;
        display: inline-block;
        max-width: 100%;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
    }

    .archived-text {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 4px;
      font-size: 13px;
      color: #10b981;
      font-weight: 500;
    }

    .harvest-box {
      padding: 16px 0;
    }

    .knowledge-container {
      padding: 8px 0;

      .sub-card {
        border-radius: 8px;
        margin-bottom: 16px;

        .card-header-flex {
          display: flex;
          justify-content: space-between;
          align-items: center;

          .sub-tip {
            font-size: 12px;
            color: #64748b;
            font-weight: 400;
            margin-left: 4px;
          }
        }
      }
    }
  }

  .claim-dialog-content {
    .dialog-paper-box {
      background-color: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      padding: 12px;

      .dialog-paper-title {
        font-weight: 600;
        font-size: 14px;
        color: #0f172a;
        line-height: 1.4;
      }

      .dialog-paper-meta {
        margin-top: 8px;
        font-size: 12px;
        color: #475569;
        display: flex;
        align-items: center;
        flex-wrap: wrap;
      }
    }
  }
}
</style>
