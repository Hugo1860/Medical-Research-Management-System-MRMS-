<template>
  <div class="education-statistics-container">
    <!-- 头部横幅 -->
    <div class="header-card">
      <div class="header-info">
        <h2 class="page-title">
          <el-icon class="title-icon"><Histogram /></el-icon>
          卫生健康科技统计年报与等级评审佐证包
        </h2>
        <p class="page-subtitle">
          全面适配国家卫健委《全国卫生健康科技统计年报》标准，一键汇编二级公立医院绩效考核（国考指标48/49/50/51）与二甲/二优等级医院评审科教专项佐证材料包。
        </p>
      </div>
      <div class="header-actions">
        <el-button
          v-if="activeTab === 'dossier'"
          type="success"
          :icon="Printer"
          @click="printEvidenceDossier"
        >
          打印/导出等级评审佐证包
        </el-button>
        <el-button
          v-if="activeTab === 'nhc'"
          type="primary"
          :icon="Download"
          @click="printNhcReport"
        >
          导出卫健委年报大盘
        </el-button>
        <el-button :icon="Refresh" @click="refreshCurrent">刷新</el-button>
      </div>
    </div>

    <!-- 顶层主标签切换 -->
    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab" @tab-change="handleTabChange">
        <!-- 标签页 1：全国卫生健康科技统计年报（卫健委标准） -->
        <el-tab-pane label="卫健委科技统计年报" name="nhc">
          <div class="filter-bar">
            <span class="label">统计年度：</span>
            <el-select v-model="selectedYear" style="width: 130px" @change="loadNhcData">
              <el-option :value="2026" label="2026 年度" />
              <el-option :value="2025" label="2025 年度" />
              <el-option :value="2024" label="2024 年度" />
              <el-option :value="2023" label="2023 年度" />
            </el-select>
            <el-button type="primary" :icon="Search" style="margin-left: 10px;" @click="loadNhcData">
              统计汇算
            </el-button>
          </div>

          <div v-loading="nhcLoading" class="nhc-dashboard">
            <!-- 概览指标卡 -->
            <el-row :gutter="16" class="metrics-row">
              <el-col :xs="12" :sm="6" :md="4">
                <el-card shadow="hover" class="metric-card">
                  <div class="metric-label">卫技总人数</div>
                  <div class="metric-value">{{ nhcData?.人力资源?.总人数 || 0 }} <span class="unit">人</span></div>
                  <div class="metric-sub">正高 {{ nhcData?.人力资源?.正高人数 || 0 }} / 副高 {{ nhcData?.人力资源?.副高人数 || 0 }}</div>
                </el-card>
              </el-col>
              <el-col :xs="12" :sm="6" :md="5">
                <el-card shadow="hover" class="metric-card">
                  <div class="metric-label">纵向科研立项</div>
                  <div class="metric-value color-primary">{{ totalProjectCount }} <span class="unit">项</span></div>
                  <div class="metric-sub">资助总额: {{ totalFundingSum.toFixed(2) }} 万元</div>
                </el-card>
              </el-col>
              <el-col :xs="12" :sm="6" :md="5">
                <el-card shadow="hover" class="metric-card">
                  <div class="metric-label">医疗新技术应用</div>
                  <div class="metric-value color-success">{{ nhcData?.医疗新技术?.临床应用例数 || 0 }} <span class="unit">例</span></div>
                  <div class="metric-sub">立项 {{ nhcData?.医疗新技术?.新技术立项数 || 0 }} / 转常规 {{ nhcData?.医疗新技术?.转常规与获奖数 || 0 }}</div>
                </el-card>
              </el-col>
              <el-col :xs="12" :sm="6" :md="5">
                <el-card shadow="hover" class="metric-card">
                  <div class="metric-label">适宜技术下乡受训</div>
                  <div class="metric-value color-cyan">{{ nhcData?.适宜技术推广?.培训基层医护数 || 0 }} <span class="unit">人次</span></div>
                  <div class="metric-sub">共 {{ nhcData?.适宜技术推广?.培训总场次 || 0 }} 场培训</div>
                </el-card>
              </el-col>
              <el-col :xs="12" :sm="6" :md="5">
                <el-card shadow="hover" class="metric-card">
                  <div class="metric-label">论文与科普成果</div>
                  <div class="metric-value color-warning">{{ (nhcData?.论文成果?.论文总数 || 0) + (nhcData?.科普与案例?.科普作品数 || 0) }} <span class="unit">篇/部</span></div>
                  <div class="metric-sub">科普传播 {{ nhcData?.科普与案例?.累计受益与传播量 || 0 }} 人次</div>
                </el-card>
              </el-col>
            </el-row>

            <!-- 统计大盘明细网格 -->
            <el-row :gutter="16">
              <!-- 纵向课题级别分布 -->
              <el-col :span="12">
                <el-card shadow="never" class="sub-table-card">
                  <div class="card-title">纵向科研项目级别与经费投入结构</div>
                  <el-table :data="nhcData?.纵向课题 || []" border size="small" empty-text="本年度暂无纵向立项">
                    <el-table-column prop="项目级别" label="项目级别" width="130" />
                    <el-table-column prop="项目数" label="立项数" width="100" align="center" />
                    <el-table-column prop="经费总额" label="资助经费(万元)" min-width="120" align="right">
                      <template #default="{ row }">
                        {{ Number(row.经费总额 || 0).toFixed(2) }}
                      </template>
                    </el-table-column>
                  </el-table>
                </el-card>
              </el-col>

              <!-- 人力资源职称结构 -->
              <el-col :span="12">
                <el-card shadow="never" class="sub-table-card">
                  <div class="card-title">全院专业技术人员职称金字塔结构</div>
                  <el-table :data="personnelRows" border size="small">
                    <el-table-column prop="职称级别" label="专业技术资格级别" width="150" />
                    <el-table-column prop="人数" label="在岗人数" width="110" align="center" />
                    <el-table-column prop="占比" label="占比" min-width="110" align="center" />
                  </el-table>
                </el-card>
              </el-col>
            </el-row>

            <el-row :gutter="16" style="margin-top: 16px;">
              <!-- 适宜技术与成果综合表 -->
              <el-col :span="24">
                <el-card shadow="never" class="sub-table-card">
                  <div class="card-title">卫健委重点关注创新指标一览</div>
                  <el-descriptions :column="3" border>
                    <el-descriptions-item label="医疗新技术（三新）新申报">{{ nhcData?.医疗新技术?.新技术立项数 || 0 }} 项</el-descriptions-item>
                    <el-descriptions-item label="临床应用随访病例总数">{{ nhcData?.医疗新技术?.临床应用例数 || 0 }} 例</el-descriptions-item>
                    <el-descriptions-item label="期满转常规与获奖数">{{ nhcData?.医疗新技术?.转常规与获奖数 || 0 }} 项</el-descriptions-item>
                    <el-descriptions-item label="适宜技术下乡场次">{{ nhcData?.适宜技术推广?.培训总场次 || 0 }} 场</el-descriptions-item>
                    <el-descriptions-item label="培训基层医护人员数">{{ nhcData?.适宜技术推广?.培训基层医护数 || 0 }} 人</el-descriptions-item>
                    <el-descriptions-item label="基层受益患者人次">{{ nhcData?.适宜技术推广?.受益患者人次 || 0 }} 人次</el-descriptions-item>
                    <el-descriptions-item label="医学科普作品篇数">{{ nhcData?.科普与案例?.科普作品数 || 0 }} 篇</el-descriptions-item>
                    <el-descriptions-item label="临床典型救治案例库">{{ nhcData?.科普与案例?.典型案例数 || 0 }} 例</el-descriptions-item>
                    <el-descriptions-item label="护理小发明/转化数">{{ nhcData?.科普与案例?.护理小发明数 || 0 }} 项</el-descriptions-item>
                    <el-descriptions-item label="接收基层卫生院进修">{{ nhcData?.医共体协同?.接收基层进修人数 || 0 }} 人次</el-descriptions-item>
                    <el-descriptions-item label="总院专家对口下乡帮扶">{{ nhcData?.医共体协同?.下乡帮扶人次 || 0 }} 人次</el-descriptions-item>
                    <el-descriptions-item label="科研伦理审查审批数">{{ nhcData?.伦理合规?.审查批准数 || 0 }} 件</el-descriptions-item>
                  </el-descriptions>
                </el-card>
              </el-col>
            </el-row>
          </div>
        </el-tab-pane>

        <!-- 标签页 2：二级公立医院绩效考核（国考）/ 等级评审佐证包 -->
        <el-tab-pane label="国考 / 等级评审佐证材料包" name="dossier">
          <div class="filter-bar">
            <span class="label">考核评审年度：</span>
            <el-select v-model="selectedYear" style="width: 130px" @change="loadDossierData">
              <el-option :value="2026" label="2026 年度" />
              <el-option :value="2025" label="2025 年度" />
              <el-option :value="2024" label="2024 年度" />
            </el-select>
            <el-button type="primary" :icon="Search" style="margin-left: 10px;" @click="loadDossierData">
              抽取佐证数据
            </el-button>
            <el-button type="success" :icon="Printer" style="margin-left: 10px;" @click="printEvidenceDossier">
              打印 / 导出装订佐证档案
            </el-button>
          </div>

          <div v-loading="dossierLoading" class="dossier-box">
            <el-alert
              type="info"
              :closable="false"
              title="以下清单已自动按二级公立医院绩效考核（国考指标48/49/50/51）与二甲等级复审规范完成关联归集，点击上方“打印/导出装订佐证档案”可直接生成带签章栏的正式纸质卷宗。"
              style="margin-bottom: 16px;"
            />

            <!-- 1. 新技术新项目准入佐证 -->
            <div class="evidence-block">
              <div class="evidence-title">
                <span>【国考指标 48 / 等级评审 4.8】临床医疗新技术新项目准入与应用佐证</span>
                <el-tag size="small" type="success">共 {{ dossierData?.新技术清单?.length || 0 }} 项</el-tag>
              </div>
              <el-table :data="dossierData?.新技术清单 || []" border size="small" empty-text="本年度暂无新技术新项目记录">
                <el-table-column prop="编号" label="序号" width="70" align="center" />
                <el-table-column prop="项目名称" label="技术新项目名称" min-width="200" show-overflow-tooltip />
                <el-table-column prop="申报类别" label="类别" width="110" align="center" />
                <el-table-column prop="技术难度等级" label="难度" width="80" align="center" />
                <el-table-column prop="审批状态" label="准入状态" width="100" align="center" />
                <el-table-column prop="临床开展总例数" label="开展例数" width="100" align="center">
                  <template #default="{ row }">{{ row.临床开展总例数 || 0 }} 例</template>
                </el-table-column>
              </el-table>
            </div>

            <!-- 2. 适宜技术推广佐证 -->
            <div class="evidence-block">
              <div class="evidence-title">
                <span>【国考指标 49 / 医共体协同】面向基层乡镇卫生院适宜技术推广佐证</span>
                <el-tag size="small" type="primary">共 {{ dossierData?.适宜技术清单?.length || 0 }} 项</el-tag>
              </div>
              <el-table :data="dossierData?.适宜技术清单 || []" border size="small" empty-text="本年度暂无适宜技术推广记录">
                <el-table-column prop="编号" label="序号" width="70" align="center" />
                <el-table-column prop="技术名称" label="适宜技术名称" min-width="180" show-overflow-tooltip />
                <el-table-column prop="受援基层单位" label="受援基层卫生院/社区中心" min-width="180" show-overflow-tooltip />
                <el-table-column prop="推广形式" label="形式" width="110" align="center" />
                <el-table-column prop="推广日期" label="日期" width="110" align="center">
                  <template #default="{ row }">{{ formatDate(row.推广日期) }}</template>
                </el-table-column>
                <el-table-column prop="培训场次" label="场次" width="80" align="center">
                  <template #default="{ row }">{{ row.培训场次 }} 场</template>
                </el-table-column>
                <el-table-column prop="受益医务人员数" label="受益基层医护" width="120" align="center">
                  <template #default="{ row }">{{ row.受益医务人员数 }} 人</template>
                </el-table-column>
              </el-table>
            </div>

            <!-- 3. 特色科普与案例佐证 -->
            <div class="evidence-block">
              <div class="evidence-title">
                <span>【特色加分项】医学科普作品、临床典型案例库与护理发明转化佐证</span>
                <el-tag size="small" type="warning">共 {{ dossierData?.科普案例清单?.length || 0 }} 项</el-tag>
              </div>
              <el-table :data="dossierData?.科普案例清单 || []" border size="small" empty-text="本年度暂无科普与案例记录">
                <el-table-column prop="编号" label="序号" width="70" align="center" />
                <el-table-column prop="分类" label="分类" width="120" align="center" />
                <el-table-column prop="成果名称" label="成果名称" min-width="180" show-overflow-tooltip />
                <el-table-column prop="第一作者姓名" label="第一作者" width="110" align="center" />
                <el-table-column prop="发布载体与平台" label="发布载体/平台" min-width="150" show-overflow-tooltip />
                <el-table-column prop="受众传播量与成效" label="传播受益量" width="100" align="center" />
                <el-table-column prop="审核状态" label="审核状态" width="90" align="center" />
              </el-table>
            </div>

            <!-- 4. 进修与帮扶佐证 -->
            <div class="evidence-block">
              <div class="evidence-title">
                <span>【人才培养与对口支援】县域医共体人员来院进修与总院下乡帮扶佐证</span>
                <el-tag size="small" type="success">共 {{ dossierData?.进修帮扶清单?.length || 0 }} 人次</el-tag>
              </div>
              <el-table :data="dossierData?.进修帮扶清单 || []" border size="small" empty-text="本年度暂无进修帮扶记录">
                <el-table-column prop="编号" label="序号" width="70" align="center" />
                <el-table-column prop="业务类型" label="业务类型" width="130" align="center" />
                <el-table-column prop="合作单位" label="合作医疗单位" min-width="170" show-overflow-tooltip />
                <el-table-column prop="人员姓名" label="进修/帮扶人员" width="110" align="center" />
                <el-table-column prop="人员职称" label="职称/专业" width="130" align="center" />
                <el-table-column label="起止时间" width="180" align="center">
                  <template #default="{ row }">
                    {{ formatDate(row.开始日期) }} ~ {{ formatDate(row.结束日期) }}
                  </template>
                </el-table-column>
                <el-table-column prop="考核鉴定结果" label="考核结果" width="90" align="center" />
              </el-table>
            </div>
          </div>
        </el-tab-pane>

        <!-- 标签页 3：教育部科研统计（传统模式保留） -->
        <el-tab-pane label="教育部科研统计（高校/附院模式）" name="tasks">
          <div class="header-row" style="margin-bottom: 12px; display: flex; justify-content: space-between;">
            <div class="muted">高校与大学附属医院传统教育部科研统计任务与校验通道</div>
            <div>
              <el-button type="primary" size="small" @click="openTask()">新建统计任务</el-button>
              <el-button size="small" @click="loadAll">刷新</el-button>
            </div>
          </div>
          <el-table v-loading="loading" :data="tasks" stripe>
            <el-table-column prop="统计年度" label="年度" width="80" />
            <el-table-column prop="任务代码" label="任务代码" width="150" />
            <el-table-column prop="任务名称" label="任务名称" min-width="220" />
            <el-table-column prop="当前版本号" label="当前版本" width="100" />
            <el-table-column prop="状态" label="状态" width="100"><template #default="{ row }"><el-tag>{{ row.状态 }}</el-tag></template></el-table-column>
            <el-table-column prop="数据包数量" label="数据包" width="90" />
            <el-table-column label="操作" width="230" fixed="right">
              <template #default="{ row }"><el-button link type="primary" @click="showVersions(row)">版本</el-button><el-button link @click="openPackage(row)">登记数据包</el-button><el-button link @click="openTask(row)">编辑</el-button></template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 弹窗：编辑任务 -->
    <el-dialog v-model="taskDialog" :title="taskForm.编号 ? '编辑统计任务' : '新建统计任务'" width="560px"><el-form :model="taskForm" label-width="110px"><el-form-item label="统计年度" required><el-input-number v-model="taskForm.统计年度" :min="2000" :max="2200" /></el-form-item><el-form-item label="任务代码" required><el-input v-model="taskForm.任务代码" placeholder="如：KY2026" /></el-form-item><el-form-item label="任务名称" required><el-input v-model="taskForm.任务名称" /></el-form-item><el-form-item label="截止日期"><el-date-picker v-model="taskForm.截止日期" type="date" value-format="YYYY-MM-DD" /></el-form-item><el-form-item label="数据截止日期"><el-date-picker v-model="taskForm.数据截止日期" type="date" value-format="YYYY-MM-DD" /></el-form-item><el-form-item label="说明"><el-input v-model="taskForm.说明" type="textarea" /></el-form-item></el-form><template #footer><el-button @click="taskDialog=false">取消</el-button><el-button type="primary" @click="saveTask">保存</el-button></template></el-dialog>
    <el-dialog v-model="versionDialog" title="统计版本" width="680px"><div class="version-header"><span>{{ selectedTask?.任务名称 }}</span><el-button type="primary" size="small" @click="newVersion">新增版本</el-button></div><el-table :data="versions" stripe><el-table-column prop="版本号" label="版本" width="80" /><el-table-column prop="模板代码" label="模板代码" width="150" /><el-table-column prop="模板名称" label="模板名称" min-width="180" /><el-table-column prop="状态" label="状态" width="90" /><el-table-column label="操作" width="90"><template #default="{ row }"><el-button link type="primary" @click="publishVersion(row)" :disabled="row.状态 === '生效'">发布</el-button></template></el-table-column></el-table></el-dialog>
    <el-dialog v-model="versionEditDialog" title="新建统计版本" width="700px"><el-form :model="versionForm" label-width="110px"><el-form-item label="版本号"><el-input-number v-model="versionForm.版本号" :min="1" /></el-form-item><el-form-item label="模板代码"><el-input v-model="versionForm.模板代码" /></el-form-item><el-form-item label="模板名称"><el-input v-model="versionForm.模板名称" /></el-form-item><el-form-item label="字段定义 JSON"><el-input v-model="versionForm.字段定义" type="textarea" :rows="9" placeholder='[{"代码":"项目数","类型":"integer","必填":true}]' /></el-form-item><el-form-item label="变更说明"><el-input v-model="versionForm.变更说明" type="textarea" /></el-form-item></el-form><template #footer><el-button @click="versionEditDialog=false">取消</el-button><el-button type="primary" @click="saveVersion">保存</el-button></template></el-dialog>
    <el-dialog v-model="packageDialog" title="登记统计数据包" width="700px"><el-alert type="warning" :closable="false" title="仅在已实际取得并解析数据后填写数据内容；只登记路径不会生成校验通过结果。" style="margin-bottom: 12px" /><el-form :model="packageForm" label-width="110px"><el-form-item label="统计任务"><el-select v-model="packageForm.任务编号" style="width:100%" @change="loadActiveVersion"><el-option v-for="item in tasks" :key="item.编号" :label="`${item.统计年度} · ${item.任务名称}`" :value="item.编号" /></el-select></el-form-item><el-form-item label="生效版本"><el-select v-model="packageForm.版本编号" style="width:100%"><el-option v-for="item in activeVersions" :key="item.编号" :label="`v${item.版本号} · ${item.模板名称}`" :value="item.编号" /></el-select></el-form-item><el-form-item label="包名称"><el-input v-model="packageForm.包名称" /></el-form-item><el-form-item label="文件路径"><el-input v-model="packageForm.文件路径" placeholder="服务器受控存储路径（可选）" /></el-form-item><el-form-item label="文件校验和"><el-input v-model="packageForm.文件校验和" placeholder="SHA-256（可选）" /></el-form-item><el-form-item label="数据内容 JSON"><el-input v-model="packageForm.数据内容" type="textarea" :rows="8" placeholder="实际解析后的 JSON 数组；留空则仅登记元数据" /></el-form-item></el-form><template #footer><el-button @click="packageDialog=false">取消</el-button><el-button type="primary" @click="registerPackage">登记</el-button></template></el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { ElMessage } from 'element-plus'
import { Histogram, Printer, Download, Refresh, Search } from '@element-plus/icons-vue'
import {
  listEducationStatisticsTasks, saveEducationStatisticsTask, listEducationStatisticsVersions,
  saveEducationStatisticsVersion, publishEducationStatisticsVersion,
  registerEducationStatisticsPackage,
  getHealthCommissionReport, getSecondaryHospitalEvidenceDossier
} from '@/api/education-statistics'

const activeTab = ref('nhc')
const selectedYear = ref(new Date().getFullYear())

// 卫健委科技年报
const nhcLoading = ref(false)
const nhcData = ref<any>(null)

// 国考佐证材料包
const dossierLoading = ref(false)
const dossierData = ref<any>(null)

// 教育部统计
const loading = ref(false)
const tasks = ref<any[]>([])
const selectedTask = ref<any>()
const taskDialog = ref(false)
const versionDialog = ref(false)
const versionEditDialog = ref(false)
const packageDialog = ref(false)
const versions = ref<any[]>([])
const activeVersions = ref<any[]>([])
const taskForm = ref<any>({ 统计年度: new Date().getFullYear(), 任务代码: '', 任务名称: '', 状态: '草稿' })
const versionForm = ref<any>({ 任务编号: 0, 版本号: 1, 模板代码: '', 模板名称: '', 字段定义: '[]', 状态: '草稿' })
const packageForm = ref<any>({ 任务编号: 0, 版本编号: 0, 包名称: '', 数据内容: '' })

const unwrap = <T,>(value: any): T => (value?.data ?? value) as T

const totalProjectCount = computed(() => {
  if (!nhcData.value?.纵向课题) return 0
  return nhcData.value.纵向课题.reduce((sum: number, item: any) => sum + Number(item.项目数 || 0), 0)
})

const totalFundingSum = computed(() => {
  if (!nhcData.value?.纵向课题) return 0
  return nhcData.value.纵向课题.reduce((sum: number, item: any) => sum + Number(item.经费总额 || 0), 0)
})

const personnelRows = computed(() => {
  const p = nhcData.value?.人力资源
  if (!p) return []
  const total = p.总人数 || 1
  return [
    { 职称级别: '正高级专业技术人员 (主任医师/护师)', 人数: p.正高人数 || 0, 占比: `${(((p.正高人数 || 0) / total) * 100).toFixed(1)}%` },
    { 职称级别: '副高级专业技术人员 (副主任医师/护师)', 人数: p.副高人数 || 0, 占比: `${(((p.副高人数 || 0) / total) * 100).toFixed(1)}%` },
    { 职称级别: '中级专业技术人员 (主治医师/主管护师)', 人数: p.中级人数 || 0, 占比: `${(((p.中级人数 || 0) / total) * 100).toFixed(1)}%` },
    { 职称级别: '初级及其他人员 (住院医师/护士)', 人数: p.初级人数 || 0, 占比: `${(((p.初级人数 || 0) / total) * 100).toFixed(1)}%` }
  ]
})

function formatDate(val: any) {
  if (!val) return '-'
  return String(val).substring(0, 10)
}

async function loadNhcData() {
  nhcLoading.value = true
  try {
    const res: any = await getHealthCommissionReport({ 年度: selectedYear.value })
    nhcData.value = unwrap(res)
  } catch (err: any) {
    ElMessage.error(err.message || '获取卫健委年报失败')
  } finally {
    nhcLoading.value = false
  }
}

async function loadDossierData() {
  dossierLoading.value = true
  try {
    const res: any = await getSecondaryHospitalEvidenceDossier({ 年度: selectedYear.value })
    dossierData.value = unwrap(res)
  } catch (err: any) {
    ElMessage.error(err.message || '获取佐证材料包失败')
  } finally {
    dossierLoading.value = false
  }
}

async function loadTasks() {
  loading.value = true
  try {
    tasks.value = unwrap<any[]>(await listEducationStatisticsTasks())
  } finally {
    loading.value = false
  }
}

async function loadAll() {
  await loadTasks()
}

function handleTabChange(tab: string) {
  if (tab === 'nhc') loadNhcData()
  else if (tab === 'dossier') loadDossierData()
  else if (tab === 'tasks') loadTasks()
}

function refreshCurrent() {
  if (activeTab.value === 'nhc') loadNhcData()
  else if (activeTab.value === 'dossier') loadDossierData()
  else if (activeTab.value === 'tasks') loadTasks()
}

function printEvidenceDossier() {
  if (!dossierData.value?.佐证包Html) {
    ElMessage.warning('暂无佐证包内容可供打印')
    return
  }
  const printWin = window.open('', '_blank')
  if (printWin) {
    printWin.document.write(dossierData.value.佐证包Html)
    printWin.document.close()
    printWin.focus()
    setTimeout(() => {
      printWin.print()
    }, 500)
  }
}

function printNhcReport() {
  window.print()
}

// 任务操作
function openTask(row?: any) {
  taskForm.value = row ? { ...row } : { 统计年度: new Date().getFullYear(), 任务代码: '', 任务名称: '', 状态: '草稿' }
  taskDialog.value = true
}

async function saveTask() {
  try {
    await saveEducationStatisticsTask(taskForm.value)
    taskDialog.value = false
    ElMessage.success('统计任务已保存')
    await loadTasks()
  } catch {}
}

async function showVersions(row: any) {
  selectedTask.value = row
  versions.value = unwrap<any[]>(await listEducationStatisticsVersions(row.编号))
  versionDialog.value = true
}

function newVersion() {
  versionForm.value = { 任务编号: selectedTask.value.编号, 版本号: (versions.value[0]?.版本号 || 0) + 1, 模板代码: '', 模板名称: '', 字段定义: '[]', 状态: '草稿' }
  versionEditDialog.value = true
}

async function saveVersion() {
  try {
    await saveEducationStatisticsVersion(versionForm.value)
    versionEditDialog.value = false
    versions.value = unwrap<any[]>(await listEducationStatisticsVersions(selectedTask.value.编号))
    ElMessage.success('统计版本已保存')
  } catch {}
}

async function publishVersion(row: any) {
  try {
    await publishEducationStatisticsVersion(row.编号!)
    versions.value = unwrap<any[]>(await listEducationStatisticsVersions(selectedTask.value.编号))
    await loadTasks()
    ElMessage.success('版本已发布')
  } catch {}
}

async function loadActiveVersion() {
  activeVersions.value = packageForm.value.任务编号 ? unwrap<any[]>(await listEducationStatisticsVersions(packageForm.value.任务编号)).filter((x: any) => x.状态 === '生效') : []
  packageForm.value.版本编号 = activeVersions.value[0]?.编号 || 0
}

async function openPackage(row: any) {
  packageForm.value = { 任务编号: row.编号, 版本编号: 0, 包名称: '', 数据内容: '' }
  await loadActiveVersion()
  packageDialog.value = true
}

async function registerPackage() {
  try {
    await registerEducationStatisticsPackage(packageForm.value)
    packageDialog.value = false
    ElMessage.success('数据包已登记')
  } catch {}
}

onMounted(() => {
  loadNhcData()
})
</script>

<style scoped>
.education-statistics-container {
  padding: 16px 20px;
  background-color: #f5f7fa;
  min-height: calc(100vh - 84px);
}

.header-card {
  background: linear-gradient(135deg, #4f46e5 0%, #3730a3 100%);
  color: #fff;
  padding: 20px 24px;
  border-radius: 8px;
  margin-bottom: 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 4px 12px rgba(79, 70, 229, 0.15);
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

.main-card {
  border-radius: 8px;
}

.filter-bar {
  margin-bottom: 16px;
  display: flex;
  align-items: center;
}

.filter-bar .label {
  font-size: 14px;
  font-weight: 600;
  color: #374151;
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

.color-primary { color: #4f46e5; }
.color-success { color: #10b981; }
.color-warning { color: #f59e0b; }
.color-cyan { color: #06b6d4; }

.sub-table-card {
  border-radius: 6px;
  margin-bottom: 10px;
}

.card-title {
  font-size: 14px;
  font-weight: 600;
  margin-bottom: 10px;
  color: #1f2937;
  border-left: 3px solid #4f46e5;
  padding-left: 8px;
}

.evidence-block {
  margin-bottom: 24px;
}

.evidence-title {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
  font-size: 14px;
  font-weight: 600;
  color: #111827;
}

.muted {
  font-size: 12px;
  color: #6b7280;
}
</style>
