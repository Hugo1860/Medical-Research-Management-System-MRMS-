<template>
  <div class="page-container">
    <el-card shadow="never" class="main-card">
      <!-- 页面头部 -->
      <div class="header-section">
        <div>
          <h2 class="page-title">科研基础信息与标准规范中心</h2>
          <p class="page-subtitle">
            统一维护项目多级分类、科研经费预算模版与支出科目、核心学术期刊、国家学科门类、国民行业代码及行政区划标准。
          </p>
        </div>
        <div class="action-buttons">
          <el-button type="primary" :icon="Plus" @click="handleOpenCreate">
            新建规范条目
          </el-button>
          <el-button :icon="Download" @click="handleExportCurrentTab">
            导出配置 (CSV)
          </el-button>
          <el-button :icon="Refresh" @click="refreshCurrentTab">
            刷新
          </el-button>
        </div>
      </div>

      <!-- 标签页导航 -->
      <el-tabs v-model="activeTab" class="config-tabs" @tab-change="onTabChange">
        <!-- 标签 1：项目多级分类规范 -->
        <el-tab-pane label="项目分类规范" name="categories">
          <div class="tab-toolbar">
            <el-input
              v-model="categoryQuery.Like名称"
              placeholder="搜索项目分类名称..."
              clearable
              style="width: 260px;"
              @keyup.enter="fetchCategories"
            />
            <el-button type="primary" :icon="Search" @click="fetchCategories">查询</el-button>
            <el-button :icon="Plus" type="success" @click="openCategoryDialog(0)">
              添加根分类
            </el-button>
          </div>

          <el-table
            v-loading="loadingCategories"
            :data="categoryList"
            row-key="编号"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="编号" label="编号" width="90" align="center" />
            <el-table-column prop="名称" label="分类名称" min-width="220">
              <template #default="{ row }">
                <span style="font-weight: 600; color: #1e293b;">{{ row.名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="排序" label="展示顺序" width="100" align="center" />
            <el-table-column prop="说明" label="分类说明与适用范围" min-width="240" show-overflow-tooltip />
            <el-table-column label="操作" width="180" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openCategoryDialog(row.上级编号, row)">
                  修改
                </el-button>
                <el-button link type="success" size="small" @click="openCategoryDialog(row.编号)">
                  加子类
                </el-button>
                <el-popconfirm title="确定删除该分类吗？" @confirm="handleDeleteCategory(row.编号)">
                  <template #reference>
                    <el-button link type="danger" size="small">删除</el-button>
                  </template>
                </el-popconfirm>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签 2：经费模版与支出科目配置 -->
        <el-tab-pane label="经费模版与支出科目" name="templates">
          <div class="tab-toolbar">
            <el-input
              v-model="templateQuery.Like模版名称"
              placeholder="搜索模版名称..."
              clearable
              style="width: 260px;"
              @keyup.enter="fetchTemplates"
            />
            <el-button type="primary" :icon="Search" @click="fetchTemplates">查询</el-button>
            <el-button :icon="Plus" type="success" @click="openTemplateDialog()">
              新建模版
            </el-button>
          </div>

          <el-table
            v-loading="loadingTemplates"
            :data="templateList"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="编号" label="模版编号" width="100" align="center" />
            <el-table-column prop="模版名称" label="经费模版名称" min-width="220">
              <template #default="{ row }">
                <span style="font-weight: 600; color: #1e293b;">{{ row.模版名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="备注" label="模版说明 / 对应资助体系" min-width="260" show-overflow-tooltip />
            <el-table-column prop="建立时间" label="建立时间" width="160" />
            <el-table-column label="操作" width="200" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="viewTemplateTypes(row)">
                  支出科目清单
                </el-button>
                <el-button link type="warning" size="small" @click="openTemplateDialog(row)">
                  编辑
                </el-button>
                <el-popconfirm title="确定删除该经费模版吗？" @confirm="handleDeleteTemplate(row.编号)">
                  <template #reference>
                    <el-button link type="danger" size="small">删除</el-button>
                  </template>
                </el-popconfirm>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签 3：核心期刊与学术刊物 -->
        <el-tab-pane label="学术刊物与期刊库" name="journals">
          <div class="tab-toolbar">
            <el-input
              v-model="journalQuery.Like期刊名称"
              placeholder="期刊名称..."
              clearable
              style="width: 220px;"
              @keyup.enter="fetchJournals"
            />
            <el-input
              v-model="journalQuery.LikeIssn"
              placeholder="ISSN号..."
              clearable
              style="width: 160px;"
              @keyup.enter="fetchJournals"
            />
            <el-input
              v-model="journalQuery.Like主办单位"
              placeholder="主办单位..."
              clearable
              style="width: 180px;"
              @keyup.enter="fetchJournals"
            />
            <el-button type="primary" :icon="Search" @click="fetchJournals">查询</el-button>
            <el-button :icon="Plus" type="success" @click="openJournalDialog()">
              录入期刊
            </el-button>
            <el-button :icon="Upload" @click="journalImportVisible = true">
              批量导入期刊
            </el-button>
          </div>

          <el-table
            v-loading="loadingJournals"
            :data="journalList"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="期刊名称" label="期刊/论文集名称" min-width="220">
              <template #default="{ row }">
                <span style="font-weight: 600; color: #1e293b;">{{ row.期刊名称 }}</span>
                <el-tag
                  v-if="row.是否为核心期刊"
                  size="small"
                  type="danger"
                  style="margin-left: 6px;"
                >
                  核心期刊
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="issn" label="ISSN" width="130" />
            <el-table-column prop="cn" label="CN号" width="130" />
            <el-table-column prop="级别" label="学术级别" width="110" align="center" />
            <el-table-column prop="主办单位" label="主办/出版单位" min-width="180" show-overflow-tooltip />
            <el-table-column label="影响因子" width="100" align="right">
              <template #default="{ row }">
                <span style="color: #409eff; font-weight: 600;">{{ row.影响因子 || '-' }}</span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="140" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openJournalDialog(row)">
                  编辑
                </el-button>
                <el-popconfirm title="确定删除该期刊条目？" @confirm="handleDeleteJournal(row.编号)">
                  <template #reference>
                    <el-button link type="danger" size="small">删除</el-button>
                  </template>
                </el-popconfirm>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-wrapper">
            <el-pagination
              v-model:current-page="journalPaging.index"
              v-model:page-size="journalPaging.size"
              :total="journalTotal"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="fetchJournals"
              @current-change="fetchJournals"
            />
          </div>
        </el-tab-pane>

        <!-- 标签 4：国民经济行业代码 -->
        <el-tab-pane label="国民行业代码" name="industry">
          <div class="tab-toolbar">
            <el-input
              v-model="industryQuery.Like名称"
              placeholder="搜索行业名称..."
              clearable
              style="width: 220px;"
              @keyup.enter="fetchIndustryCodes"
            />
            <el-input
              v-model="industryQuery.Like代码"
              placeholder="行业代码..."
              clearable
              style="width: 160px;"
              @keyup.enter="fetchIndustryCodes"
            />
            <el-button type="primary" :icon="Search" @click="fetchIndustryCodes">查询</el-button>
            <el-button :icon="Plus" type="success" @click="openIndustryDialog()">
              新增行业分类
            </el-button>
          </div>

          <el-table
            v-loading="loadingIndustry"
            :data="industryList"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="代码" label="国民行业代码" width="140" align="center" />
            <el-table-column prop="名称" label="行业分类名称" min-width="220">
              <template #default="{ row }">
                <span style="font-weight: 600; color: #1e293b;">{{ row.名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="级别" label="层级" width="90" align="center" />
            <el-table-column prop="说明" label="说明与门类划分" min-width="260" show-overflow-tooltip />
            <el-table-column label="操作" width="140" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openIndustryDialog(row)">
                  编辑
                </el-button>
                <el-popconfirm title="确定删除该行业代码？" @confirm="handleDeleteIndustry(row.编号)">
                  <template #reference>
                    <el-button link type="danger" size="small">删除</el-button>
                  </template>
                </el-popconfirm>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签 5：学科规范与技术领域 -->
        <el-tab-pane label="学科门类与技术领域" name="disciplines">
          <div class="tab-toolbar">
            <el-input
              v-model="disciplineQuery.Like名称"
              placeholder="搜索学科名称..."
              clearable
              style="width: 220px;"
              @keyup.enter="fetchDisciplines"
            />
            <el-input
              v-model="disciplineQuery.Like代码"
              placeholder="学科代码..."
              clearable
              style="width: 160px;"
              @keyup.enter="fetchDisciplines"
            />
            <el-button type="primary" :icon="Search" @click="fetchDisciplines">查询学科</el-button>
            <el-button :icon="Plus" type="success" @click="openDisciplineDialog()">
              新增学科
            </el-button>
            <el-button type="warning" @click="fetchTechnicalFields">
              查看技术领域 ({{ technicalFieldList.length }})
            </el-button>
          </div>

          <el-table
            v-loading="loadingDisciplines"
            :data="disciplineList"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="代码" label="学科代码" width="130" align="center" />
            <el-table-column prop="名称" label="国家标准学科名称" min-width="220">
              <template #default="{ row }">
                <span style="font-weight: 600; color: #1e293b;">{{ row.名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="级别" label="学科等级" width="100" align="center" />
            <el-table-column prop="备注" label="备注说明" min-width="200" show-overflow-tooltip />
            <el-table-column label="操作" width="140" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openDisciplineDialog(row)">
                  编辑
                </el-button>
                <el-popconfirm title="确定删除该学科条目？" @confirm="handleDeleteDiscipline(row.编号)">
                  <template #reference>
                    <el-button link type="danger" size="small">删除</el-button>
                  </template>
                </el-popconfirm>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 标签 6：行政区域与买方性质 -->
        <el-tab-pane label="行政区域与买方性质" name="regions">
          <div class="tab-toolbar">
            <el-input
              v-model="regionQuery.Like名称"
              placeholder="搜索省/市/区县行政区划..."
              clearable
              style="width: 240px;"
              @keyup.enter="fetchRegions"
            />
            <el-button type="primary" :icon="Search" @click="fetchRegions">查询区划</el-button>
            <el-button :icon="Plus" type="success" @click="openRegionDialog()">
              新增区划
            </el-button>
            <el-button type="info" @click="fetchContractBuyers">
              查看买方性质 ({{ contractBuyerList.length }})
            </el-button>
          </div>

          <el-table
            v-loading="loadingRegions"
            :data="regionList"
            stripe
            border
            style="width: 100%; margin-top: 16px;"
          >
            <el-table-column prop="编号" label="区划编号" width="100" align="center" />
            <el-table-column prop="名称" label="行政区划名称" min-width="200">
              <template #default="{ row }">
                <span style="font-weight: 600; color: #1e293b;">{{ row.名称 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="代码" label="行政代码" width="130" align="center" />
            <el-table-column prop="级别" label="层级" width="90" align="center" />
            <el-table-column prop="说明" label="说明" min-width="200" show-overflow-tooltip />
            <el-table-column label="操作" width="140" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openRegionDialog(row)">
                  编辑
                </el-button>
                <el-popconfirm title="确定删除该区划？" @confirm="handleDeleteRegion(row.编号)">
                  <template #reference>
                    <el-button link type="danger" size="small">删除</el-button>
                  </template>
                </el-popconfirm>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 分类新增/编辑弹窗 -->
    <el-dialog v-model="categoryDialogVisible" :title="categoryModalTitle" width="500px">
      <el-form :model="categoryForm" label-width="90px">
        <el-form-item label="分类名称" required>
          <el-input v-model="categoryForm.名称" placeholder="输入项目分类名称" />
        </el-form-item>
        <el-form-item label="显示排序">
          <el-input-number v-model="categoryForm.排序" :min="1" :max="999" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="业务说明">
          <el-input
            v-model="categoryForm.说明"
            type="textarea"
            rows="3"
            placeholder="说明该分类对应的纵向/横向基金或项目范围"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="categoryDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="dialogLoading" @click="submitCategory">保存</el-button>
      </template>
    </el-dialog>

    <!-- 经费模版新增/编辑弹窗 -->
    <el-dialog v-model="templateDialogVisible" :title="templateForm.编号 ? '编辑经费模版' : '新建经费模版'" width="500px">
      <el-form :model="templateForm" label-width="90px">
        <el-form-item label="模版名称" required>
          <el-input v-model="templateForm.模版名称" placeholder="如：国家自然科学基金经费预算模版" />
        </el-form-item>
        <el-form-item label="备注说明">
          <el-input
            v-model="templateForm.备注"
            type="textarea"
            rows="3"
            placeholder="说明模版适用的课题级别与资助部门"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="templateDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="dialogLoading" @click="submitTemplate">保存</el-button>
      </template>
    </el-dialog>

    <!-- 支出科目清单抽屉 -->
    <el-drawer v-model="templateTypesDrawerVisible" title="经费模版预算支出科目配置" size="580px">
      <div v-if="currentTemplate">
        <div style="background: #f8fafc; padding: 12px; border-radius: 6px; margin-bottom: 16px;">
          <h4 style="margin: 0 0 4px 0;">{{ currentTemplate.模版名称 }}</h4>
          <span style="font-size: 12px; color: #64748b;">模版编号：{{ currentTemplate.编号 }}</span>
        </div>

        <el-table :data="templateTypesList" border size="small" style="width: 100%;">
          <el-table-column prop="编号" label="科目ID" width="80" align="center" />
          <el-table-column prop="项目支出类型" label="支出科目名称" min-width="180">
            <template #default="{ row }">
              <span style="font-weight: 600;">{{ row.项目支出类型 }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="备注" label="备注" min-width="120" show-overflow-tooltip />
        </el-table>
      </div>
    </el-drawer>

    <!-- 期刊新增/编辑弹窗 -->
    <el-dialog v-model="journalDialogVisible" :title="journalForm.编号 ? '编辑学术期刊' : '录入学术期刊'" width="580px">
      <el-form :model="journalForm" label-width="100px">
        <el-form-item label="期刊名称" required>
          <el-input v-model="journalForm.期刊名称" placeholder="输入期刊或论文集全称" />
        </el-form-item>
        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="ISSN">
              <el-input v-model="journalForm.issn" placeholder="0028-0836" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="CN号">
              <el-input v-model="journalForm.cn" placeholder="11-1234/R" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="主办单位">
              <el-input v-model="journalForm.主办单位" placeholder="如：中华医学会" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="学术级别">
              <el-select v-model="journalForm.级别" style="width: 100%;">
                <el-option label="SCI 一区 (Top)" value="SCI-1" />
                <el-option label="SCI 二区" value="SCI-2" />
                <el-option label="SCI 三/四区" value="SCI-3/4" />
                <el-option label="北大核心/CSCD" value="北大核心" />
                <el-option label="科技核心/统计源" value="科技核心" />
                <el-option label="国家级期刊" value="国家级" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="是否核心">
          <el-switch v-model="journalForm.是否为核心期刊" active-text="核心学术期刊" inactive-text="普通期刊" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="journalDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="dialogLoading" @click="submitJournal">保存</el-button>
      </template>
    </el-dialog>

    <!-- 国民行业代码弹窗 -->
    <el-dialog v-model="industryDialogVisible" :title="industryForm.编号 ? '编辑行业代码' : '新增国民行业代码'" width="500px">
      <el-form :model="industryForm" label-width="90px">
        <el-form-item label="行业代码" required>
          <el-input v-model="industryForm.代码" placeholder="如：Q8411 (综合医院)" />
        </el-form-item>
        <el-form-item label="行业名称" required>
          <el-input v-model="industryForm.名称" placeholder="行业分类具体名称" />
        </el-form-item>
        <el-form-item label="行业说明">
          <el-input v-model="industryForm.说明" type="textarea" rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="industryDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="dialogLoading" @click="submitIndustry">保存</el-button>
      </template>
    </el-dialog>

    <!-- 学科门类弹窗 -->
    <el-dialog v-model="disciplineDialogVisible" :title="disciplineForm.编号 ? '编辑学科条目' : '新增标准学科'" width="500px">
      <el-form :model="disciplineForm" label-width="90px">
        <el-form-item label="学科代码" required>
          <el-input v-model="disciplineForm.代码" placeholder="如：1002 (临床医学)" />
        </el-form-item>
        <el-form-item label="学科名称" required>
          <el-input v-model="disciplineForm.名称" placeholder="学科名称" />
        </el-form-item>
        <el-form-item label="学科备注">
          <el-input v-model="disciplineForm.备注" type="textarea" rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="disciplineDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="dialogLoading" @click="submitDiscipline">保存</el-button>
      </template>
    </el-dialog>

    <!-- 行政区域弹窗 -->
    <el-dialog v-model="regionDialogVisible" :title="regionForm.编号 ? '编辑行政区划' : '新增行政区划'" width="500px">
      <el-form :model="regionForm" label-width="90px">
        <el-form-item label="区划名称" required>
          <el-input v-model="regionForm.名称" placeholder="如：广东省 / 广州市 / 天河区" />
        </el-form-item>
        <el-form-item label="行政代码">
          <el-input v-model="regionForm.代码" placeholder="如：440106" />
        </el-form-item>
        <el-form-item label="说明">
          <el-input v-model="regionForm.说明" type="textarea" rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="regionDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="dialogLoading" @click="submitRegion">保存</el-button>
      </template>
    </el-dialog>

    <!-- 期刊批量导入弹窗 -->
    <DataImportDialog
      v-model="journalImportVisible"
      title="批量导入核心学术期刊目录"
      template-name="标准学术期刊目录导入模板.xlsx"
      :upload-handler="handleImportJournals"
      @success="fetchJournals"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import DataImportDialog from '@/components/DataImportDialog.vue'
import { Plus, Download, Refresh, Search, Upload } from '@element-plus/icons-vue'
import {
  getProjectCategories,
  saveProjectCategory,
  deleteProjectCategories,
  getExpenseTemplates,
  saveExpenseTemplate,
  deleteExpenseTemplates,
  getExpenseTemplateTypes,
  getPagingJournals,
  saveJournal,
  deleteJournals,
  importJournals,
  getIndustryCodes,
  addIndustryCode,
  editIndustryCode,
  deleteIndustryCodes,
  getDisciplines,
  addDiscipline,
  editDiscipline,
  deleteDisciplines,
  getTechnicalFields,
  getRegions,
  addRegion,
  editRegion,
  deleteRegions,
  getContractBuyerTypes,
  type ProjectCategoryItem,
  type ExpenseTemplateItem,
  type ExpenseTemplateTypeItem,
  type JournalItem,
  type IndustryCodeItem,
  type DisciplineItem,
  type TechnicalFieldItem,
  type RegionItem,
  type ContractBuyerTypeItem
} from '@/api/base-info'
import { exportToCsv } from '@/utils/export'

const route = useRoute()

// 标签页状态
const activeTab = ref('categories')
const dialogLoading = ref(false)

// 1. 项目分类
const loadingCategories = ref(false)
const categoryList = ref<ProjectCategoryItem[]>([])
const categoryQuery = reactive({ Like名称: '' })
const categoryDialogVisible = ref(false)
const categoryModalTitle = ref('新增项目分类')
const categoryForm = reactive<Partial<ProjectCategoryItem>>({
  编号: 0,
  上级编号: 0,
  名称: '',
  排序: 1,
  说明: ''
})

// 2. 经费模版
const loadingTemplates = ref(false)
const templateList = ref<ExpenseTemplateItem[]>([])
const templateQuery = reactive({ Like模版名称: '' })
const templateDialogVisible = ref(false)
const templateForm = reactive<Partial<ExpenseTemplateItem>>({
  编号: 0,
  模版名称: '',
  备注: ''
})
const templateTypesDrawerVisible = ref(false)
const currentTemplate = ref<ExpenseTemplateItem | null>(null)
const templateTypesList = ref<ExpenseTemplateTypeItem[]>([])

// 3. 期刊
const loadingJournals = ref(false)
const journalList = ref<JournalItem[]>([])
const journalTotal = ref(0)
const journalPaging = reactive({ index: 1, size: 10 })
const journalQuery = reactive({ Like期刊名称: '', LikeIssn: '', Like主办单位: '' })
const journalDialogVisible = ref(false)
const journalImportVisible = ref(false)

async function handleImportJournals(file: File) {
  return await importJournals(file)
}
const journalForm = reactive<Partial<JournalItem>>({
  编号: 0,
  期刊名称: '',
  issn: '',
  cn: '',
  主办单位: '',
  级别: 'SCI-2',
  是否为核心期刊: true
})

// 4. 国民行业代码
const loadingIndustry = ref(false)
const industryList = ref<IndustryCodeItem[]>([])
const industryQuery = reactive({ Like名称: '', Like代码: '' })
const industryDialogVisible = ref(false)
const industryForm = reactive<Partial<IndustryCodeItem>>({
  编号: 0,
  代码: '',
  名称: '',
  说明: ''
})

// 5. 学科与技术领域
const loadingDisciplines = ref(false)
const disciplineList = ref<DisciplineItem[]>([])
const technicalFieldList = ref<TechnicalFieldItem[]>([])
const disciplineQuery = reactive({ Like名称: '', Like代码: '' })
const disciplineDialogVisible = ref(false)
const disciplineForm = reactive<Partial<DisciplineItem>>({
  编号: 0,
  代码: '',
  名称: '',
  备注: ''
})

// 6. 行政区域与买方性质
const loadingRegions = ref(false)
const regionList = ref<RegionItem[]>([])
const contractBuyerList = ref<ContractBuyerTypeItem[]>([])
const regionQuery = reactive({ Like名称: '' })
const regionDialogVisible = ref(false)
const regionForm = reactive<Partial<RegionItem>>({
  编号: 0,
  名称: '',
  代码: '',
  说明: ''
})

// 1. 分类加载与操作
async function fetchCategories() {
  loadingCategories.value = true
  try {
    const res = await getProjectCategories(categoryQuery)
    categoryList.value = res || []
  } catch (error) {
    console.error('获取项目分类失败:', error)
  } finally {
    loadingCategories.value = false
  }
}

function openCategoryDialog(parentId = 0, row?: ProjectCategoryItem) {
  if (row) {
    categoryModalTitle.value = '编辑项目分类'
    Object.assign(categoryForm, {
      编号: row.编号,
      上级编号: row.上级编号,
      名称: row.名称,
      排序: row.排序 || 1,
      说明: row.说明 || ''
    })
  } else {
    categoryModalTitle.value = parentId === 0 ? '添加根分类' : '添加子分类'
    Object.assign(categoryForm, {
      编号: 0,
      上级编号: parentId,
      名称: '',
      排序: 1,
      说明: ''
    })
  }
  categoryDialogVisible.value = true
}

async function submitCategory() {
  if (!categoryForm.名称) {
    ElMessage.warning('请输入分类名称')
    return
  }
  dialogLoading.value = true
  try {
    await saveProjectCategory(categoryForm)
    ElMessage.success('项目分类已成功保存')
    categoryDialogVisible.value = false
    fetchCategories()
  } catch (error: any) {
    ElMessage.error(error.message || '保存失败')
  } finally {
    dialogLoading.value = false
  }
}

async function handleDeleteCategory(id: number) {
  try {
    await deleteProjectCategories([id])
    ElMessage.success('项目分类已删除')
    fetchCategories()
  } catch (error: any) {
    ElMessage.error(error.message || '删除失败')
  }
}

// 2. 经费模版加载与操作
async function fetchTemplates() {
  loadingTemplates.value = true
  try {
    const res = await getExpenseTemplates({ index: 1, size: 50 }, templateQuery)
    templateList.value = res.list || []
  } catch (error) {
    console.error('获取经费模版失败:', error)
  } finally {
    loadingTemplates.value = false
  }
}

function openTemplateDialog(row?: ExpenseTemplateItem) {
  if (row) {
    Object.assign(templateForm, { 编号: row.编号, 模版名称: row.模版名称, 备注: row.备注 || '' })
  } else {
    Object.assign(templateForm, { 编号: 0, 模版名称: '', 备注: '' })
  }
  templateDialogVisible.value = true
}

async function submitTemplate() {
  if (!templateForm.模版名称) {
    ElMessage.warning('请输入模版名称')
    return
  }
  dialogLoading.value = true
  try {
    await saveExpenseTemplate(templateForm)
    ElMessage.success('经费模版已成功保存')
    templateDialogVisible.value = false
    fetchTemplates()
  } catch (error: any) {
    ElMessage.error(error.message || '保存失败')
  } finally {
    dialogLoading.value = false
  }
}

async function handleDeleteTemplate(id: number) {
  try {
    await deleteExpenseTemplates([id])
    ElMessage.success('经费模版已删除')
    fetchTemplates()
  } catch (error: any) {
    ElMessage.error(error.message || '删除失败')
  }
}

async function viewTemplateTypes(template: ExpenseTemplateItem) {
  currentTemplate.value = template
  templateTypesDrawerVisible.value = true
  try {
    const res = await getExpenseTemplateTypes({ index: 1, size: 100 }, { 所属经费模版编号: template.编号 })
    templateTypesList.value = res.list || []
  } catch (error) {
    console.error('获取模版支出类型失败:', error)
  }
}

// 3. 期刊操作
async function fetchJournals() {
  loadingJournals.value = true
  try {
    const res = await getPagingJournals(journalPaging, journalQuery)
    journalList.value = res.list || []
    journalTotal.value = res.total || 0
  } catch (error) {
    console.error('获取期刊失败:', error)
  } finally {
    loadingJournals.value = false
  }
}

function openJournalDialog(row?: JournalItem) {
  if (row) {
    Object.assign(journalForm, row)
  } else {
    Object.assign(journalForm, {
      编号: 0,
      期刊名称: '',
      issn: '',
      cn: '',
      主办单位: '',
      级别: 'SCI-2',
      是否为核心期刊: true
    })
  }
  journalDialogVisible.value = true
}

async function submitJournal() {
  if (!journalForm.期刊名称) {
    ElMessage.warning('请输入期刊名称')
    return
  }
  dialogLoading.value = true
  try {
    await saveJournal(journalForm)
    ElMessage.success('期刊条目已保存')
    journalDialogVisible.value = false
    fetchJournals()
  } catch (error: any) {
    ElMessage.error(error.message || '保存失败')
  } finally {
    dialogLoading.value = false
  }
}

async function handleDeleteJournal(id: number) {
  try {
    await deleteJournals([id])
    ElMessage.success('期刊条目已删除')
    fetchJournals()
  } catch (error: any) {
    ElMessage.error(error.message || '删除失败')
  }
}

// 4. 国民行业代码操作
async function fetchIndustryCodes() {
  loadingIndustry.value = true
  try {
    const res = await getIndustryCodes(industryQuery)
    industryList.value = res || []
  } catch (error) {
    console.error('获取行业代码失败:', error)
  } finally {
    loadingIndustry.value = false
  }
}

function openIndustryDialog(row?: IndustryCodeItem) {
  if (row) {
    Object.assign(industryForm, row)
  } else {
    Object.assign(industryForm, { 编号: 0, 代码: '', 名称: '', 说明: '' })
  }
  industryDialogVisible.value = true
}

async function submitIndustry() {
  if (!industryForm.名称 || !industryForm.代码) {
    ElMessage.warning('请填写行业代码与名称')
    return
  }
  dialogLoading.value = true
  try {
    if (industryForm.编号) {
      await editIndustryCode(industryForm)
    } else {
      await addIndustryCode(industryForm)
    }
    ElMessage.success('行业代码已保存')
    industryDialogVisible.value = false
    fetchIndustryCodes()
  } catch (error: any) {
    ElMessage.error(error.message || '保存失败')
  } finally {
    dialogLoading.value = false
  }
}

async function handleDeleteIndustry(id: number) {
  try {
    await deleteIndustryCodes([id])
    ElMessage.success('行业代码已删除')
    fetchIndustryCodes()
  } catch (error: any) {
    ElMessage.error(error.message || '删除失败')
  }
}

// 5. 学科与技术领域操作
async function fetchDisciplines() {
  loadingDisciplines.value = true
  try {
    const res = await getDisciplines(disciplineQuery)
    disciplineList.value = res || []
  } catch (error) {
    console.error('获取学科失败:', error)
  } finally {
    loadingDisciplines.value = false
  }
}

async function fetchTechnicalFields() {
  try {
    const res = await getTechnicalFields()
    technicalFieldList.value = res || []
    ElMessage.info(`已同步 ${technicalFieldList.value.length} 个技术领域分类`)
  } catch (error) {
    console.error('获取技术领域失败:', error)
  }
}

function openDisciplineDialog(row?: DisciplineItem) {
  if (row) {
    Object.assign(disciplineForm, row)
  } else {
    Object.assign(disciplineForm, { 编号: 0, 代码: '', 名称: '', 备注: '' })
  }
  disciplineDialogVisible.value = true
}

async function submitDiscipline() {
  if (!disciplineForm.名称 || !disciplineForm.代码) {
    ElMessage.warning('请填写学科代码与名称')
    return
  }
  dialogLoading.value = true
  try {
    if (disciplineForm.编号) {
      await editDiscipline(disciplineForm)
    } else {
      await addDiscipline(disciplineForm)
    }
    ElMessage.success('学科条目已保存')
    disciplineDialogVisible.value = false
    fetchDisciplines()
  } catch (error: any) {
    ElMessage.error(error.message || '保存失败')
  } finally {
    dialogLoading.value = false
  }
}

async function handleDeleteDiscipline(id: number) {
  try {
    await deleteDisciplines([id])
    ElMessage.success('学科条目已删除')
    fetchDisciplines()
  } catch (error: any) {
    ElMessage.error(error.message || '删除失败')
  }
}

// 6. 行政区域操作
async function fetchRegions() {
  loadingRegions.value = true
  try {
    const res = await getRegions(regionQuery)
    regionList.value = res || []
  } catch (error) {
    console.error('获取行政区划失败:', error)
  } finally {
    loadingRegions.value = false
  }
}

async function fetchContractBuyers() {
  try {
    const res = await getContractBuyerTypes()
    contractBuyerList.value = res || []
    ElMessage.info(`已同步 ${contractBuyerList.value.length} 个合同买方性质分类`)
  } catch (error) {
    console.error('获取合同买方性质失败:', error)
  }
}

function openRegionDialog(row?: RegionItem) {
  if (row) {
    Object.assign(regionForm, row)
  } else {
    Object.assign(regionForm, { 编号: 0, 名称: '', 代码: '', 说明: '' })
  }
  regionDialogVisible.value = true
}

async function submitRegion() {
  if (!regionForm.名称) {
    ElMessage.warning('请填写行政区划名称')
    return
  }
  dialogLoading.value = true
  try {
    if (regionForm.编号) {
      await editRegion(regionForm)
    } else {
      await addRegion(regionForm)
    }
    ElMessage.success('行政区划已保存')
    regionDialogVisible.value = false
    fetchRegions()
  } catch (error: any) {
    ElMessage.error(error.message || '保存失败')
  } finally {
    dialogLoading.value = false
  }
}

async function handleDeleteRegion(id: number) {
  try {
    await deleteRegions([id])
    ElMessage.success('行政区划已删除')
    fetchRegions()
  } catch (error: any) {
    ElMessage.error(error.message || '删除失败')
  }
}

// 头部全局新建
function handleOpenCreate() {
  if (activeTab.value === 'categories') openCategoryDialog(0)
  else if (activeTab.value === 'templates') openTemplateDialog()
  else if (activeTab.value === 'journals') openJournalDialog()
  else if (activeTab.value === 'industry') openIndustryDialog()
  else if (activeTab.value === 'disciplines') openDisciplineDialog()
  else if (activeTab.value === 'regions') openRegionDialog()
}

function refreshCurrentTab() {
  if (activeTab.value === 'categories') fetchCategories()
  else if (activeTab.value === 'templates') fetchTemplates()
  else if (activeTab.value === 'journals') fetchJournals()
  else if (activeTab.value === 'industry') fetchIndustryCodes()
  else if (activeTab.value === 'disciplines') fetchDisciplines()
  else if (activeTab.value === 'regions') fetchRegions()
}

function onTabChange(tab: any) {
  if (tab === 'categories' && !categoryList.value.length) fetchCategories()
  else if (tab === 'templates' && !templateList.value.length) fetchTemplates()
  else if (tab === 'journals' && !journalList.value.length) fetchJournals()
  else if (tab === 'industry' && !industryList.value.length) fetchIndustryCodes()
  else if (tab === 'disciplines' && !disciplineList.value.length) fetchDisciplines()
  else if (tab === 'regions' && !regionList.value.length) fetchRegions()
}

// 导出 CSV
function handleExportCurrentTab() {
  try {
    if (activeTab.value === 'categories') {
      exportToCsv(
        '项目分类规范',
        [
          { label: '编号', prop: '编号' },
          { label: '上级编号', prop: '上级编号' },
          { label: '分类名称', prop: '名称' },
          { label: '排序', prop: '排序' },
          { label: '说明', prop: '说明' }
        ],
        categoryList.value
      )
    } else if (activeTab.value === 'templates') {
      exportToCsv(
        '经费预算模版配置',
        [
          { label: '模版编号', prop: '编号' },
          { label: '模版名称', prop: '模版名称' },
          { label: '备注说明', prop: '备注' },
          { label: '建立时间', prop: '建立时间' }
        ],
        templateList.value
      )
    } else if (activeTab.value === 'journals') {
      exportToCsv(
        '核心学术期刊库',
        [
          { label: '期刊名称', prop: '期刊名称' },
          { label: 'ISSN', prop: 'issn' },
          { label: 'CN号', prop: 'cn' },
          { label: '学术级别', prop: '级别' },
          { label: '主办单位', prop: '主办单位' },
          { label: '是否核心', prop: '是否为核心期刊' }
        ],
        journalList.value
      )
    } else if (activeTab.value === 'industry') {
      exportToCsv(
        '国民经济行业代码',
        [
          { label: '行业代码', prop: '代码' },
          { label: '行业分类名称', prop: '名称' },
          { label: '级别', prop: '级别' },
          { label: '说明', prop: '说明' }
        ],
        industryList.value
      )
    } else if (activeTab.value === 'disciplines') {
      exportToCsv(
        '学科分类规范',
        [
          { label: '学科代码', prop: '代码' },
          { label: '学科名称', prop: '名称' },
          { label: '等级', prop: '级别' },
          { label: '备注', prop: '备注' }
        ],
        disciplineList.value
      )
    } else {
      exportToCsv(
        '行政区划代码',
        [
          { label: '区划编号', prop: '编号' },
          { label: '行政区划名称', prop: '名称' },
          { label: '行政代码', prop: '代码' },
          { label: '层级', prop: '级别' }
        ],
        regionList.value
      )
    }
    ElMessage.success('配置数据已成功导出')
  } catch (error: any) {
    ElMessage.warning(error.message || '导出失败')
  }
}

function syncTabAndAction() {
  const queryTab = route.query.tab as string
  if (queryTab && ['categories', 'templates', 'journals', 'industry', 'disciplines', 'regions'].includes(queryTab)) {
    activeTab.value = queryTab
  }
  if (route.query.action === 'add') {
    handleOpenCreate()
  }
}

watch(() => route.query, () => {
  syncTabAndAction()
  refreshCurrentTab()
})

onMounted(() => {
  syncTabAndAction()
  fetchCategories()
  fetchTemplates()
  if (activeTab.value === 'journals') fetchJournals()
  else if (activeTab.value === 'industry') fetchIndustryCodes()
  else if (activeTab.value === 'disciplines') fetchDisciplines()
  else if (activeTab.value === 'regions') fetchRegions()
})
</script>

<style scoped lang="scss">
.page-container {
  padding: 16px;

  .main-card {
    border-radius: 8px;
    background: #ffffff;
  }
}

.header-section {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 20px;

  .page-title {
    font-size: 20px;
    font-weight: 600;
    color: #1e293b;
    margin: 0 0 6px 0;
  }

  .page-subtitle {
    font-size: 13px;
    color: #64748b;
    margin: 0;
  }

  .action-buttons {
    display: flex;
    gap: 8px;
  }
}

.config-tabs {
  margin-top: 10px;
}

.tab-toolbar {
  display: flex;
  gap: 10px;
  align-items: center;
  flex-wrap: wrap;
  margin-top: 8px;
}

.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}
</style>
