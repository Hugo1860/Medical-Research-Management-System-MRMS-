<template>
  <div class="dictionary-container">
    <el-row :gutter="16" class="full-height-row">
      <!-- 左侧：字典大类树/列表 -->
      <el-col :xs="24" :sm="8" :md="6" class="category-col">
        <el-card shadow="never" class="category-card">
          <template #header>
            <div class="category-header">
              <span class="header-title">科研字典标准规范</span>
              <el-tag size="small" type="info">{{ filteredCategories.length }} 类</el-tag>
            </div>
          </template>
          <el-input
            v-model="categorySearch"
            placeholder="搜索字典分类..."
            prefix-icon="Search"
            clearable
            style="margin-bottom: 12px;"
          />
          <el-scrollbar style="height: calc(100vh - 240px);">
            <div
              v-for="cat in filteredCategories"
              :key="cat"
              class="category-item"
              :class="{ active: currentCategory === cat }"
              @click="selectCategory(cat)"
            >
              <el-icon class="cat-icon"><Folder /></el-icon>
              <span class="cat-name">{{ cat }}</span>
            </div>
          </el-scrollbar>
        </el-card>
      </el-col>

      <!-- 右侧：当前分类下字典项表格 -->
      <el-col :xs="24" :sm="16" :md="18" class="detail-col">
        <el-card shadow="never" class="detail-card">
          <!-- 顶部操作条 -->
          <div class="toolbar">
            <div class="left">
              <h3 class="current-title">【{{ currentCategory }}】字典列表</h3>
            </div>
            <div class="right">
              <el-input
                v-model="keywordSearch"
                placeholder="搜索字典值..."
                prefix-icon="Search"
                clearable
                style="width: 180px; margin-right: 12px;"
              />
              <el-button type="primary" :icon="Plus" @click="openAddDialog">添加项</el-button>
              <el-button :icon="Refresh" @click="fetchDictionaryList">刷新</el-button>
            </div>
          </div>

          <!-- 表格主体 -->
          <el-table
            v-loading="loading"
            :data="filteredItems"
            stripe
            style="width: 100%; margin-top: 12px;"
            height="calc(100vh - 280px)"
          >
            <el-table-column prop="编号" label="编码" width="90" align="center" />
            <el-table-column prop="值" label="字典条目值" min-width="180">
              <template #default="{ row }">
                <span style="font-weight: 500; color: #1e293b;">{{ row.值 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="排序" label="排序权重" width="100" align="center" />
            <el-table-column prop="备注" label="备注说明" min-width="160" show-overflow-tooltip />
            <el-table-column label="操作" width="140" fixed="right" align="center">
              <template #default="{ row }">
                <el-button type="primary" link size="small" @click="openEditDialog(row)">编辑</el-button>
                <el-button type="danger" link size="small" @click="handleDelete(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
    </el-row>

    <!-- 增改字典弹窗 -->
    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑字典条目' : '新增字典条目'" width="450px">
      <el-form ref="formRef" :model="formData" :rules="formRules" label-width="90px">
        <el-form-item label="所属大类">
          <el-input v-model="formData.分类" disabled />
        </el-form-item>
        <el-form-item label="字典项值" prop="值">
          <el-input v-model="formData.值" placeholder="请输入条目名称" />
        </el-form-item>
        <el-form-item label="排序权重">
          <el-input-number v-model="formData.排序" :min="0" :max="999" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="备注说明">
          <el-input v-model="formData.备注" type="textarea" rows="3" placeholder="可选备注信息" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import {
  getDictionaryByCategory,
  createDictionary,
  updateDictionary,
  deleteDictionary,
  type DictionaryItem
} from '@/api/base-info'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Refresh, Folder } from '@element-plus/icons-vue'

const route = useRoute()

const loading = ref(false)
const submitLoading = ref(false)
const categorySearch = ref('')
const keywordSearch = ref('')
const currentCategory = ref('项目级别')

// 标准内置分类清单
const categories = ref<string[]>([
  '项目级别',
  '计划来源',
  '计划来源分类',
  '项目支出类型',
  '论文类型',
  '论文发表范围',
  '论文版面',
  '论文作者级别',
  '期刊收录数据库',
  '期刊JCR分区',
  '专利类型',
  '专利作者级别',
  '获奖级别',
  '获奖类别',
  '获奖作者级别',
  '著作类型',
  '著作作者级别',
  '学科门类',
  '技术职称',
  '技术职称分类',
  '专业技术级别',
  '岗位名称',
  '岗位类型',
  '岗位级别',
  '人员类别',
  '个人身份',
  '学历',
  '学位',
  '学习形式',
  '学历性质',
  '民族',
  '政治面貌',
  '学术任职名称',
  '学术活动类型',
  '知识产权',
  '社会经济目标分类',
  '票据类型',
  '合同付款方式',
  '合同类型大类',
  '评分方案'
])

const dictionaryItems = ref<DictionaryItem[]>([])
const dialogVisible = ref(false)
const isEdit = ref(false)

const formData = reactive({
  编号: 0,
  分类: '',
  值: '',
  上级编号: 0,
  级别: 1,
  排序: 10,
  备注: ''
})

const formRules = {
  值: [{ required: true, message: '字典值不能为空', trigger: 'blur' }]
}

const filteredCategories = computed(() => {
  if (!categorySearch.value) return categories.value
  return categories.value.filter((cat) => cat.includes(categorySearch.value))
})

const filteredItems = computed(() => {
  if (!keywordSearch.value) return dictionaryItems.value
  return dictionaryItems.value.filter((item) => item.值?.includes(keywordSearch.value))
})

async function selectCategory(cat: string) {
  currentCategory.value = cat
  await fetchDictionaryList()
}

async function fetchDictionaryList() {
  loading.value = true
  try {
    const list = await getDictionaryByCategory(currentCategory.value)
    dictionaryItems.value = Array.isArray(list) ? list : []
  } catch (err: any) {
    dictionaryItems.value = []
    ElMessage.error(err.response?.data?.error || err.message || '字典数据查询失败，请稍后重试')
  } finally {
    loading.value = false
  }
}

function openAddDialog() {
  isEdit.value = false
  formData.编号 = 0
  formData.分类 = currentCategory.value
  formData.值 = ''
  formData.排序 = 10
  formData.备注 = ''
  dialogVisible.value = true
}

function openEditDialog(item: DictionaryItem) {
  isEdit.value = true
  formData.编号 = item.编号
  formData.分类 = item.分类
  formData.值 = item.值
  formData.排序 = item.排序
  formData.备注 = item.备注 || ''
  dialogVisible.value = true
}

async function handleSubmit() {
  if (!formData.值.trim()) {
    ElMessage.warning('请输入字典项值')
    return
  }
  submitLoading.value = true
  try {
    if (isEdit.value) {
      await updateDictionary(formData)
      ElMessage.success('修改字典项成功')
    } else {
      await createDictionary(formData)
      ElMessage.success('新增字典项成功')
    }
    dialogVisible.value = false
    await fetchDictionaryList()
  } catch (err: any) {
    ElMessage.error(err.response?.data?.error || err.message || '操作失败')
  } finally {
    submitLoading.value = false
  }
}

async function handleDelete(item: DictionaryItem) {
  try {
    await ElMessageBox.confirm(`确定要删除字典项【${item.值}】吗？`, '提示', {
      type: 'warning'
    })
    await deleteDictionary([item.编号])
    ElMessage.success('删除成功')
    await fetchDictionaryList()
  } catch {
    // canceled
  }
}

function syncCategoryFromRoute() {
  const queryName = route.query.name
  if (queryName && typeof queryName === 'string') {
    const trimmed = queryName.trim()
    if (trimmed) {
      if (!categories.value.includes(trimmed)) {
        categories.value.unshift(trimmed)
      }
      currentCategory.value = trimmed
    }
  }
  if (route.query.action === 'add' || route.query.action === 'child') {
    openAddDialog()
  }
}

watch(
  () => route.query.name,
  async () => {
    syncCategoryFromRoute()
    await fetchDictionaryList()
  }
)

onMounted(async () => {
  syncCategoryFromRoute()
  await fetchDictionaryList()
})
</script>

<style scoped lang="scss">
.dictionary-container {
  padding: 16px;
  height: 100%;

  .full-height-row {
    height: 100%;
  }

  .category-card {
    height: 100%;
    background-color: #ffffff;

    .category-header {
      display: flex;
      justify-content: space-between;
      align-items: center;

      .header-title {
        font-weight: 600;
        font-size: 14px;
      }
    }

    .category-item {
      display: flex;
      align-items: center;
      padding: 10px 12px;
      margin-bottom: 4px;
      border-radius: 6px;
      cursor: pointer;
      color: #334155;
      font-size: 13px;
      transition: all 0.2s;

      .cat-icon {
        margin-right: 8px;
        color: #94a3b8;
      }

      &:hover {
        background-color: #f1f5f9;
        color: #3b82f6;

        .cat-icon {
          color: #3b82f6;
        }
      }

      &.active {
        background-color: #eff6ff;
        color: #2563eb;
        font-weight: 600;

        .cat-icon {
          color: #2563eb;
        }
      }
    }
  }

  .detail-card {
    height: 100%;
    background-color: #ffffff;

    .toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;

      .current-title {
        font-size: 16px;
        font-weight: 600;
        color: #0f172a;
      }

      .right {
        display: flex;
        align-items: center;
      }
    }
  }
}
</style>
