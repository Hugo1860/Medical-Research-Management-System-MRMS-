<template>
  <div class="project-configuration-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span class="title">项目分类与预算模板配置</span>
          <el-tag type="info">树形分类 / 科目模板 / 建卡模式 / 编号生成</el-tag>
        </div>
      </template>

      <el-tabs v-model="activeTab">
        <!-- 分类树 -->
        <el-tab-pane label="项目分类树" name="tree">
          <div class="action-bar">
            <el-button type="primary" @click="handleOpenCategoryDialog()">新增根分类</el-button>
            <el-button @click="loadCategories">刷新</el-button>
          </div>
          <el-table :data="categories" row-key="编号" border default-expand-all style="width: 100%; margin-top: 15px">
            <el-table-column prop="名称" label="分类名称" min-width="200" />
            <el-table-column prop="分类编码" label="分类编码" width="140" />
            <el-table-column prop="项目性质" label="项目性质" width="100">
              <template #default="{ row }">
                <el-tag :type="row.项目性质 === '纵向' ? 'primary' : row.项目性质 === '横向' ? 'success' : 'warning'">
                  {{ row.项目性质 }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="建卡模式" label="建卡模式" width="160" />
            <el-table-column prop="排序" label="排序" width="80" align="center" />
            <el-table-column prop="是否启用" label="状态" width="90" align="center">
              <template #default="{ row }">
                <el-tag :type="row.是否启用 ? 'success' : 'info'">{{ row.是否启用 ? '启用' : '禁用' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="150" align="center">
              <template #default="{ row }">
                <el-button link type="primary" @click="handleOpenCategoryDialog(row)">编辑</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 预算科目模板 -->
        <el-tab-pane label="预算科目模板" name="templates">
          <div class="action-bar">
            <el-button type="primary" @click="handleOpenTemplateDialog()">新建科目模板</el-button>
            <el-button @click="loadTemplates">刷新</el-button>
          </div>
          <el-table :data="templates" border style="width: 100%; margin-top: 15px">
            <el-table-column prop="模板编码" label="模板编码" width="140" />
            <el-table-column prop="模板名称" label="模板名称" min-width="180" />
            <el-table-column prop="适用项目性质" label="适用性质" width="100" />
            <el-table-column prop="间接费比例上限" label="间接费上限(%)" width="140" align="right" />
            <el-table-column prop="状态" label="状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="row.状态 === '启用' ? 'success' : 'info'">{{ row.状态 }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="150" align="center">
              <template #default="{ row }">
                <el-button link type="primary" @click="handleOpenTemplateDialog(row)">编辑</el-button>
                <el-button link type="success" @click="handleTestSplit(row)">测算拆分</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- 编号规则 -->
        <el-tab-pane label="项目编号规则" name="rules">
          <div class="action-bar">
            <el-button type="primary" @click="handleOpenRuleDialog()">新建编号规则</el-button>
            <el-button @click="loadRules">刷新</el-button>
          </div>
          <el-table :data="rules" border style="width: 100%; margin-top: 15px">
            <el-table-column prop="规则编码" label="规则编码" width="140" />
            <el-table-column prop="规则名称" label="规则名称" min-width="180" />
            <el-table-column prop="前缀" label="前缀" width="100" />
            <el-table-column prop="日期格式" label="日期格式" width="100" />
            <el-table-column prop="流水位数" label="流水位数" width="90" align="center" />
            <el-table-column prop="重置周期" label="重置周期" width="90" align="center" />
            <el-table-column label="操作" width="160" align="center">
              <template #default="{ row }">
                <el-button link type="primary" @click="handleGenerateTest(row)">测试生成</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 分类编辑弹窗 -->
    <el-dialog v-model="categoryDialogVisible" title="项目分类信息" width="550px">
      <el-form :model="categoryForm" label-width="120px">
        <el-form-item label="分类编码">
          <el-input v-model="categoryForm.分类编码" placeholder="如：V-NSFC" />
        </el-form-item>
        <el-form-item label="分类名称" required>
          <el-input v-model="categoryForm.名称" placeholder="输入分类名称" />
        </el-form-item>
        <el-form-item label="项目性质">
          <el-select v-model="categoryForm.项目性质" style="width: 100%">
            <el-option label="纵向" value="纵向" />
            <el-option label="横向" value="横向" />
            <el-option label="院级" value="院级" />
          </el-select>
        </el-form-item>
        <el-form-item label="建卡模式">
          <el-select v-model="categoryForm.建卡模式" style="width: 100%">
            <el-option label="单卡" value="单卡" />
            <el-option label="按经费来源拆卡" value="按经费来源拆卡" />
            <el-option label="按年度拆卡" value="按年度拆卡" />
            <el-option label="按承担单位拆卡" value="按承担单位拆卡" />
          </el-select>
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="categoryForm.排序" :min="0" style="width: 100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="categoryDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitCategory">保存</el-button>
      </template>
    </el-dialog>

    <!-- 预算拆分测算弹窗 -->
    <el-dialog v-model="splitDialogVisible" title="预算科目拆分测算" width="650px">
      <div style="margin-bottom: 15px">
        <span>当前模板：{{ activeTemplate?.模板名称 }}</span>
        <div style="display: flex; gap: 10px; margin-top: 10px">
          <el-input-number v-model="splitTotalAmount" :min="1000" :step="10000" style="width: 250px" />
          <el-button type="primary" @click="doCalculateSplit">测算平账</el-button>
        </div>
      </div>
      <el-table :data="splitResults" border>
        <el-table-column prop="科目名称" label="科目名称" />
        <el-table-column prop="比例" label="比例(%)" width="100" align="right" />
        <el-table-column prop="拆分金额" label="测算金额(元)" width="160" align="right">
          <template #default="{ row }">
            {{ Number(row.拆分金额).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import {
  getProjectClassificationTree,
  saveProjectClassification,
  getBudgetTemplates,
  getNumberRules,
  generateProjectNumber,
  calculateBudgetSplit
} from '@/api/project-configuration'

const activeTab = ref('tree')
const categories = ref<any[]>([])
const templates = ref<any[]>([])
const rules = ref<any[]>([])

const categoryDialogVisible = ref(false)
const categoryForm = ref<any>({
  编号: 0,
  名称: '',
  分类编码: '',
  项目性质: '纵向',
  建卡模式: '单卡',
  排序: 0,
  是否启用: true
})

const splitDialogVisible = ref(false)
const activeTemplate = ref<any>(null)
const splitTotalAmount = ref(500000)
const splitResults = ref<any[]>([])

async function loadCategories() {
  try {
    const res = await getProjectClassificationTree()
    categories.value = res || []
  } catch (err: any) {
    ElMessage.error(err.message || '加载项目分类失败')
  }
}

async function loadTemplates() {
  try {
    const res = await getBudgetTemplates()
    templates.value = res || []
  } catch (err: any) {
    ElMessage.error(err.message || '加载预算模板失败')
  }
}

async function loadRules() {
  try {
    const res = await getNumberRules()
    rules.value = res || []
  } catch (err: any) {
    ElMessage.error(err.message || '加载编号规则失败')
  }
}

function handleOpenCategoryDialog(row?: any) {
  if (row) {
    categoryForm.value = { ...row }
  } else {
    categoryForm.value = {
      编号: 0,
      名称: '',
      分类编码: '',
      项目性质: '纵向',
      建卡模式: '单卡',
      排序: 0,
      是否启用: true
    }
  }
  categoryDialogVisible.value = true
}

async function submitCategory() {
  if (!categoryForm.value.名称) {
    ElMessage.warning('分类名称不能为空')
    return
  }
  try {
    await saveProjectClassification(categoryForm.value)
    ElMessage.success('保存项目分类成功')
    categoryDialogVisible.value = false
    loadCategories()
  } catch (err: any) {
    ElMessage.error(err.message || '保存失败')
  }
}

function handleOpenTemplateDialog(_row?: any) {
  ElMessage.info('科目明细可在已有方案列表实时维护或测算')
}

function handleOpenRuleDialog() {
  ElMessage.info('编号规则遵循前缀+周期+流水原子锁')
}

async function handleGenerateTest(row: any) {
  try {
    const res = await generateProjectNumber(row.规则编码)
    ElMessage.success(`生成编号成功: ${res.项目编号}`)
  } catch (err: any) {
    ElMessage.error(err.message || '编号生成失败')
  }
}

function handleTestSplit(row: any) {
  activeTemplate.value = row
  splitDialogVisible.value = true
  doCalculateSplit()
}

async function doCalculateSplit() {
  if (!activeTemplate.value) return
  try {
    const res = await calculateBudgetSplit({
      模板编号: activeTemplate.value.编号,
      总金额: splitTotalAmount.value
    })
    splitResults.value = res?.科目拆分 || []
  } catch (err: any) {
    ElMessage.error(err.message || '测算失败')
  }
}

onMounted(() => {
  loadCategories()
  loadTemplates()
  loadRules()
})
</script>

<style scoped>
.project-configuration-container {
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
.action-bar {
  display: flex;
  gap: 10px;
}
</style>
