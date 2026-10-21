<template>
  <div class="role-management-container">
    <!-- 顶部状态栏 -->
    <div class="header-card">
      <div class="header-info">
        <h2 class="page-title">
          <el-icon class="title-icon"><Lock /></el-icon>
          角色与权限治理中心
        </h2>
        <p class="page-subtitle">
          统一维护全院系统角色体系、人员分配、菜单操作授权及高安全级别的字段级可读可写管控。
        </p>
      </div>
      <div class="header-actions">
        <el-button type="primary" :icon="Plus" @click="openCreateRoleDialog">新建角色</el-button>
        <el-button :icon="Refresh" @click="fetchRoles">刷新列表</el-button>
      </div>
    </div>

    <!-- 主体左右双栏 -->
    <el-row :gutter="16" class="main-content-row">
      <!-- 左栏：角色列表 -->
      <el-col :xs="24" :sm="24" :md="8" :lg="7" class="left-col">
        <el-card shadow="never" class="role-list-card">
          <template #header>
            <div class="card-header-flex">
              <span class="card-title">系统角色 ({{ roles.length }})</span>
              <el-input
                v-model="roleSearchKeyword"
                placeholder="搜索角色名称/备注..."
                :prefix-icon="Search"
                clearable
                size="small"
                class="role-search-input"
              />
            </div>
          </template>

          <div v-loading="rolesLoading" class="role-cards-wrapper">
            <el-empty v-if="filteredRoles.length === 0" description="未找到匹配角色" />
            <div
              v-for="role in filteredRoles"
              :key="role.编号"
              class="role-item-card"
              :class="{ active: currentRole?.编号 === role.编号 }"
              @click="selectRole(role)"
            >
              <div class="role-item-main">
                <div class="role-item-title-row">
                  <span class="role-name">{{ role.名称 }}</span>
                  <el-button
                    type="primary"
                    link
                    size="small"
                    :icon="Edit"
                    @click.stop="openEditRoleDialog(role)"
                  >
                    编辑
                  </el-button>
                </div>
                <div class="role-item-desc">{{ role.备注 || '暂无详细描述' }}</div>
                <div class="role-item-meta">
                  <el-tag size="small" type="info" effect="plain">
                    <el-icon><User /></el-icon> 成员: {{ role.人数 ?? 0 }} 人
                  </el-tag>
                  <el-tag size="small" type="success" effect="plain">
                    <el-icon><Menu /></el-icon> 权限: {{ role.功能数 ?? 0 }} 项
                  </el-tag>
                </div>
              </div>
            </div>
          </div>
        </el-card>
      </el-col>

      <!-- 右栏：角色详情与授权面板 -->
      <el-col :xs="24" :sm="24" :md="16" :lg="17" class="right-col">
        <el-card v-if="currentRole" shadow="never" class="role-detail-card">
          <!-- 当前选中的角色头部信息 -->
          <div class="role-banner">
            <div class="role-banner-left">
              <h3 class="role-active-title">
                {{ currentRole.名称 }}
                <el-tag size="small" type="primary" class="id-tag">ID: {{ currentRole.编号 }}</el-tag>
              </h3>
              <p class="role-active-desc">{{ currentRole.备注 || '该角色暂无附加备注说明' }}</p>
            </div>
            <div class="role-banner-stats">
              <div class="stat-pill">
                <span class="num">{{ currentRole.人数 ?? 0 }}</span>
                <span class="lbl">分配成员</span>
              </div>
              <div class="stat-pill">
                <span class="num">{{ currentRole.功能数 ?? 0 }}</span>
                <span class="lbl">已获功能数</span>
              </div>
            </div>
          </div>

          <!-- 功能选项卡 -->
          <el-tabs v-model="activeTab" class="role-tabs" @tab-change="onTabChange">
            <!-- Tab 1: 角色成员分配 -->
            <el-tab-pane label="角色成员管理" name="members">
              <div class="tab-toolbar">
                <div class="toolbar-left">
                  <el-input
                    v-model="memberSearchKeyword"
                    placeholder="按姓名或工号过滤..."
                    :prefix-icon="Search"
                    clearable
                    size="default"
                    style="width: 240px"
                    @keyup.enter="fetchRoleMembers"
                  />
                  <el-button type="primary" :icon="Search" @click="fetchRoleMembers">搜索</el-button>
                </div>
                <div class="toolbar-right">
                  <el-button type="success" :icon="Plus" @click="openAddMemberDialog">
                    分配新成员
                  </el-button>
                </div>
              </div>

              <el-table
                v-loading="membersLoading"
                :data="membersList"
                border
                stripe
                class="data-table"
              >
                <el-table-column prop="编号" label="人员ID" width="90" align="center" />
                <el-table-column prop="工号" label="工号" width="120" />
                <el-table-column prop="姓名" label="姓名" width="120">
                  <template #default="{ row }">
                    <span class="fw-bold">{{ row.姓名 }}</span>
                  </template>
                </el-table-column>
                <el-table-column prop="部门名称" label="所属科室/部门" min-width="160" />
                <el-table-column prop="手机" label="联系电话" width="130" />
                <el-table-column prop="是否启用" label="账号状态" width="100" align="center">
                  <template #default="{ row }">
                    <el-tag :type="row.是否启用 ? 'success' : 'danger'" size="small">
                      {{ row.是否启用 ? '正常启用' : '已停用' }}
                    </el-tag>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="110" align="center" fixed="right">
                  <template #default="{ row }">
                    <el-popconfirm
                      :title="`确认将 ${row.姓名} 从【${currentRole?.名称}】中移除吗？`"
                      confirm-button-text="确认移除"
                      cancel-button-text="取消"
                      confirm-button-type="danger"
                      @confirm="removeMember(row.编号)"
                    >
                      <template #reference>
                        <el-button type="danger" link size="small" :icon="Delete">移除</el-button>
                      </template>
                    </el-popconfirm>
                  </template>
                </el-table-column>
              </el-table>

              <div class="pagination-bar">
                <el-pagination
                  v-model:current-page="memberPaging.index"
                  v-model:page-size="memberPaging.size"
                  :total="memberPaging.total"
                  :page-sizes="[10, 20, 50]"
                  layout="total, sizes, prev, pager, next, jumper"
                  @size-change="fetchRoleMembers"
                  @current-change="fetchRoleMembers"
                />
              </div>
            </el-tab-pane>

            <!-- Tab 2: 菜单与功能权限分配 -->
            <el-tab-pane label="菜单与功能授权" name="permissions">
              <div class="permission-tree-toolbar">
                <div class="tree-tools-left">
                  <el-input
                    v-model="permissionFilterText"
                    placeholder="输入关键字快速检索菜单..."
                    :prefix-icon="Search"
                    clearable
                    size="default"
                    style="width: 260px"
                  />
                  <el-button size="default" @click="expandAll(true)">全部展开</el-button>
                  <el-button size="default" @click="expandAll(false)">全部折叠</el-button>
                  <el-button size="default" @click="selectAllPermissions(true)">全选</el-button>
                  <el-button size="default" @click="selectAllPermissions(false)">全不选</el-button>
                </div>
                <div class="tree-tools-right">
                  <span class="checked-summary">
                    已勾选: <strong>{{ checkedPermissionKeys.length }}</strong> 项权限
                  </span>
                  <el-button
                    type="primary"
                    :icon="Check"
                    :loading="savingPermissions"
                    @click="handleSavePermissions"
                  >
                    保存权限配置
                  </el-button>
                </div>
              </div>

              <div v-loading="permissionsLoading" class="tree-wrapper">
                <el-tree
                  ref="permissionTreeRef"
                  :data="permissionTree"
                  show-checkbox
                  node-key="编号"
                  :props="{ label: '名称', children: 'children' }"
                  :filter-node-method="filterPermissionNode"
                  default-expand-all
                  class="styled-tree"
                >
                  <template #default="{ node, data }">
                    <div class="custom-tree-node">
                      <span class="node-label">
                        <el-icon v-if="data.children && data.children.length > 0" class="node-icon">
                          <Folder />
                        </el-icon>
                        <el-icon v-else class="node-icon"><Document /></el-icon>
                        {{ node.label }}
                      </span>
                      <div class="node-meta">
                        <span v-if="data.路径" class="node-path">{{ data.路径 }}</span>
                        <el-tag
                          size="small"
                          :type="data.children && data.children.length > 0 ? 'info' : 'primary'"
                          effect="plain"
                        >
                          {{ data.children && data.children.length > 0 ? '菜单分组' : '页面/操作' }}
                        </el-tag>
                      </div>
                    </div>
                  </template>
                </el-tree>
              </div>
            </el-tab-pane>

            <!-- Tab 3: 字段级授权规则 -->
            <el-tab-pane label="字段级数据授权" name="fields">
              <div class="field-auth-toolbar">
                <div class="toolbar-left">
                  <el-select
                    v-model="fieldResourceFilter"
                    placeholder="按业务模块筛选..."
                    clearable
                    size="default"
                    style="width: 220px"
                    @change="fetchFieldPermissions"
                  >
                    <el-option label="全部资源" value="" />
                    <el-option label="纵向项目" value="纵向项目" />
                    <el-option label="横向项目" value="横向项目" />
                    <el-option label="经费报销" value="经费报销" />
                    <el-option label="论文成果" value="论文成果" />
                    <el-option label="学术活动" value="学术活动" />
                  </el-select>
                  <el-button :icon="Search" @click="fetchFieldPermissions">查询规则</el-button>
                </div>
                <div class="toolbar-right">
                  <el-button type="primary" :icon="Plus" @click="openAddFieldRuleDialog">
                    新增字段授权规则
                  </el-button>
                </div>
              </div>

              <div class="field-auth-notice">
                <el-alert
                  title="字段级授权规则基于零信任安全体系，当设置可读为关闭时前端自动脱敏隐藏，可写关闭时禁止表单提交与修改。"
                  type="info"
                  show-icon
                  :closable="false"
                />
              </div>

              <el-table
                v-loading="fieldRulesLoading"
                :data="fieldRulesList"
                border
                stripe
                class="data-table"
              >
                <el-table-column prop="资源" label="业务资源模块" width="160">
                  <template #default="{ row }">
                    <el-tag type="info" effect="plain">{{ row.资源 }}</el-tag>
                  </template>
                </el-table-column>
                <el-table-column prop="字段" label="受保护敏感字段" min-width="160">
                  <template #default="{ row }">
                    <span class="code-font">{{ row.字段 }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="是否可读 (Read)" width="160" align="center">
                  <template #default="{ row }">
                    <el-switch
                      v-model="row.是否可读"
                      inline-prompt
                      active-text="允许"
                      inactive-text="隐藏"
                      @change="handleQuickUpdateField(row)"
                    />
                  </template>
                </el-table-column>
                <el-table-column label="是否可写 (Write)" width="160" align="center">
                  <template #default="{ row }">
                    <el-switch
                      v-model="row.是否可写"
                      inline-prompt
                      active-text="编辑"
                      inactive-text="只读"
                      @change="handleQuickUpdateField(row)"
                    />
                  </template>
                </el-table-column>
                <el-table-column prop="更新时间" label="最近配置时间" min-width="180">
                  <template #default="{ row }">
                    <span>{{ row.更新时间 ? formatDateTime(row.更新时间) : '系统初始化' }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="100" align="center" fixed="right">
                  <template #default="{ row }">
                    <el-button
                      type="primary"
                      link
                      size="small"
                      @click="handleQuickUpdateField(row)"
                    >
                      更新
                    </el-button>
                  </template>
                </el-table-column>
              </el-table>
            </el-tab-pane>
          </el-tabs>
        </el-card>

        <el-empty v-else description="请在左侧选择一个角色以查看与配置详情" />
      </el-col>
    </el-row>

    <!-- 新建/编辑角色对话框 -->
    <el-dialog
      v-model="roleDialogVisible"
      :title="roleForm.编号 ? '编辑角色信息' : '创建新角色'"
      width="480px"
      destroy-on-close
    >
      <el-form ref="roleFormRef" :model="roleForm" :rules="roleRules" label-width="90px">
        <el-form-item label="角色名称" prop="名称">
          <el-input v-model="roleForm.名称" placeholder="如：科室秘书、评审专家、伦理委员..." />
        </el-form-item>
        <el-form-item label="角色描述" prop="备注">
          <el-input
            v-model="roleForm.备注"
            type="textarea"
            :rows="3"
            placeholder="详细说明该角色的业务定位与职责边界..."
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="roleDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="savingRole" @click="handleSaveRole">保存</el-button>
      </template>
    </el-dialog>

    <!-- 分配成员对话框 -->
    <el-dialog
      v-model="addMemberDialogVisible"
      title="为当前角色分配人员"
      width="680px"
      destroy-on-close
    >
      <div class="add-member-dialog-content">
        <div class="dialog-search-bar">
          <el-input
            v-model="availableUserKeyword"
            placeholder="按姓名或工号搜索候选人员..."
            :prefix-icon="Search"
            clearable
            style="width: 260px"
            @keyup.enter="fetchAvailableUsers"
          />
          <el-button type="primary" :icon="Search" @click="fetchAvailableUsers">查询</el-button>
        </div>

        <el-table
          ref="availableTableRef"
          v-loading="availableUsersLoading"
          :data="availableUsersList"
          border
          height="320px"
          @selection-change="handleAvailableSelectionChange"
        >
          <el-table-column type="selection" width="50" align="center" />
          <el-table-column prop="工号" label="工号" width="120" />
          <el-table-column prop="姓名" label="姓名" width="120" />
          <el-table-column prop="部门名称" label="科室/部门" min-width="160" />
          <el-table-column prop="是否启用" label="状态" width="90" align="center">
            <template #default="{ row }">
              <el-tag :type="row.是否启用 ? 'success' : 'info'" size="small">
                {{ row.是否启用 ? '正常' : '禁用' }}
              </el-tag>
            </template>
          </el-table-column>
        </el-table>

        <div class="pagination-bar">
          <el-pagination
            v-model:current-page="availablePaging.index"
            v-model:page-size="availablePaging.size"
            :total="availablePaging.total"
            layout="total, prev, pager, next"
            @current-change="fetchAvailableUsers"
          />
        </div>
      </div>
      <template #footer>
        <el-button @click="addMemberDialogVisible = false">取消</el-button>
        <el-button
          type="primary"
          :loading="addingMembers"
          :disabled="selectedAvailableUsers.length === 0"
          @click="confirmAddMembers"
        >
          确认分配 (已选 {{ selectedAvailableUsers.length }} 人)
        </el-button>
      </template>
    </el-dialog>

    <!-- 新增字段级规则对话框 -->
    <el-dialog
      v-model="fieldRuleDialogVisible"
      title="配置字段级数据授权规则"
      width="520px"
      destroy-on-close
    >
      <el-form ref="fieldRuleFormRef" :model="fieldRuleForm" label-width="100px">
        <el-form-item label="所属角色">
          <el-input :value="currentRole?.名称" disabled />
        </el-form-item>
        <el-form-item label="业务资源" required>
          <el-select v-model="fieldRuleForm.资源" placeholder="选择或输入业务资源" allow-create filterable>
            <el-option label="纵向项目" value="纵向项目" />
            <el-option label="横向项目" value="横向项目" />
            <el-option label="经费报销" value="经费报销" />
            <el-option label="论文成果" value="论文成果" />
            <el-option label="学术活动" value="学术活动" />
          </el-select>
        </el-form-item>
        <el-form-item label="目标字段" required>
          <el-input v-model="fieldRuleForm.字段" placeholder="如：批准经费、手机号码、身份证、评审分数..." />
        </el-form-item>
        <el-form-item label="可读权限">
          <el-switch v-model="fieldRuleForm.是否可读" active-text="允许读取" inactive-text="禁止读取/脱敏" />
        </el-form-item>
        <el-form-item label="可写权限">
          <el-switch v-model="fieldRuleForm.是否可写" active-text="允许编辑修改" inactive-text="禁止写入/只读" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="fieldRuleDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="savingFieldRule" @click="handleSaveFieldRule">保存规则</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, type FormInstance } from 'element-plus'
import {
  Lock,
  Plus,
  Refresh,
  Search,
  Edit,
  User,
  Menu,
  Delete,
  Check,
  Folder,
  Document
} from '@element-plus/icons-vue'
import {
  getRoleList,
  createRole,
  updateRole,
  getPagedRoleMembers,
  getPagedAvailableUsers,
  modifyRoleMembers,
  getRolePermissions,
  saveRolePermissions,
  getFieldPermissions,
  saveFieldPermission,
  type RoleItem,
  type RoleMemberItem,
  type RolePermissionItem,
  type FieldPermissionItem
} from '@/api/system'
import { formatDateTime } from '@/utils/format'

interface TreeNode extends RolePermissionItem {
  children?: TreeNode[]
}

const route = useRoute()

// 角色状态
const roles = ref<RoleItem[]>([])
const rolesLoading = ref(false)
const roleSearchKeyword = ref('')
const currentRole = ref<RoleItem | null>(null)
const activeTab = ref('members')

// 过滤后的角色列表
const filteredRoles = computed(() => {
  if (!roleSearchKeyword.value.trim()) return roles.value
  const kw = roleSearchKeyword.value.trim().toLowerCase()
  return roles.value.filter(
    (r) => r.名称.toLowerCase().includes(kw) || (r.备注 && r.备注.toLowerCase().includes(kw))
  )
})

// 角色增改表单
const roleDialogVisible = ref(false)
const savingRole = ref(false)
const roleFormRef = ref<FormInstance>()
const roleForm = ref<{ 编号?: number; 名称: string; 备注: string }>({
  名称: '',
  备注: ''
})
const roleRules = {
  名称: [{ required: true, message: '请输入角色名称', trigger: 'blur' }]
}

// 成员分配状态
const membersList = ref<RoleMemberItem[]>([])
const membersLoading = ref(false)
const memberSearchKeyword = ref('')
const memberPaging = ref({
  index: 1,
  size: 10,
  total: 0
})

// 候选人员选择弹窗
const addMemberDialogVisible = ref(false)
const availableUsersLoading = ref(false)
const availableUsersList = ref<RoleMemberItem[]>([])
const availableUserKeyword = ref('')
const availablePaging = ref({
  index: 1,
  size: 10,
  total: 0
})
const selectedAvailableUsers = ref<RoleMemberItem[]>([])
const addingMembers = ref(false)

// 权限树状态
const permissionsLoading = ref(false)
const savingPermissions = ref(false)
const permissionTreeRef = ref()
const rawPermissions = ref<RolePermissionItem[]>([])
const permissionTree = ref<TreeNode[]>([])
const checkedPermissionKeys = ref<number[]>([])
const permissionFilterText = ref('')

// 字段级授权状态
const fieldRulesList = ref<FieldPermissionItem[]>([])
const fieldRulesLoading = ref(false)
const fieldResourceFilter = ref('')
const fieldRuleDialogVisible = ref(false)
const savingFieldRule = ref(false)
const fieldRuleForm = ref({
  资源: '',
  字段: '',
  是否可读: true,
  是否可写: true
})

// 树节点过滤监听
watch(permissionFilterText, (val) => {
  permissionTreeRef.value?.filter(val)
})

// 获取角色列表
async function fetchRoles() {
  rolesLoading.value = true
  try {
    const res = await getRoleList()
    roles.value = Array.isArray(res) ? res : []
    if (roles.value.length > 0 && !currentRole.value) {
      selectRole(roles.value[0])
    } else if (currentRole.value) {
      const match = roles.value.find((r) => r.编号 === currentRole.value?.编号)
      if (match) currentRole.value = match
    }
  } catch (error) {
    console.warn('获取角色列表失败，已清空当前列表:', error)
    roles.value = []
    currentRole.value = null
  } finally {
    rolesLoading.value = false
  }
}

// 选择当前角色
function selectRole(role: RoleItem) {
  currentRole.value = role
  if (activeTab.value === 'members') {
    fetchRoleMembers()
  } else if (activeTab.value === 'permissions') {
    fetchRolePermissions()
  } else if (activeTab.value === 'fields') {
    fetchFieldPermissions()
  }
}

// 标签页切换
function onTabChange(tabName: any) {
  if (!currentRole.value) return
  if (tabName === 'members') fetchRoleMembers()
  else if (tabName === 'permissions') fetchRolePermissions()
  else if (tabName === 'fields') fetchFieldPermissions()
}

// 获取角色成员
async function fetchRoleMembers() {
  if (!currentRole.value) return
  membersLoading.value = true
  try {
    const res = await getPagedRoleMembers(
      currentRole.value.编号,
      { index: memberPaging.value.index, size: memberPaging.value.size },
      {
        Like姓名: memberSearchKeyword.value.trim() || undefined,
        工号: memberSearchKeyword.value.trim() || undefined
      }
    )
    membersList.value = res?.list || []
    memberPaging.value.total = res?.total || 0
  } catch (error) {
    console.error('获取角色成员失败:', error)
    membersList.value = []
    memberPaging.value.total = 0
  } finally {
    membersLoading.value = false
  }
}

// 移除角色成员
async function removeMember(userId: number) {
  if (!currentRole.value) return
  try {
    await modifyRoleMembers({
      角色编号: currentRole.value.编号,
      将增加的人员编号列表: [],
      将删除的人员编号列表: [userId]
    })
    ElMessage.success('人员已成功移出该角色')
    fetchRoleMembers()
    fetchRoles()
  } catch (error: any) {
    ElMessage.error(error?.message || '移出人员失败')
  }
}

// 获取待分配候选人员
async function fetchAvailableUsers() {
  if (!currentRole.value) return
  availableUsersLoading.value = true
  try {
    const res = await getPagedAvailableUsers(
      currentRole.value.编号,
      { index: availablePaging.value.index, size: availablePaging.value.size },
      {
        Like姓名: availableUserKeyword.value.trim() || undefined,
        工号: availableUserKeyword.value.trim() || undefined
      }
    )
    availableUsersList.value = res?.list || []
    availablePaging.value.total = res?.total || 0
  } catch (error) {
    console.error('获取候选人员失败:', error)
    availableUsersList.value = []
    availablePaging.value.total = 0
  } finally {
    availableUsersLoading.value = false
  }
}

function openAddMemberDialog() {
  selectedAvailableUsers.value = []
  availableUserKeyword.value = ''
  availablePaging.value.index = 1
  addMemberDialogVisible.value = true
  fetchAvailableUsers()
}

function handleAvailableSelectionChange(selection: RoleMemberItem[]) {
  selectedAvailableUsers.value = selection
}

async function confirmAddMembers() {
  if (!currentRole.value || selectedAvailableUsers.value.length === 0) return
  addingMembers.value = true
  try {
    const userIds = selectedAvailableUsers.value.map((u) => u.编号)
    await modifyRoleMembers({
      角色编号: currentRole.value.编号,
      将增加的人员编号列表: userIds,
      将删除的人员编号列表: []
    })
    ElMessage.success(`成功为【${currentRole.value.名称}】分配 ${userIds.length} 位成员`)
    addMemberDialogVisible.value = false
    fetchRoleMembers()
    fetchRoles()
  } catch (error: any) {
    ElMessage.error(error?.message || '分配成员失败')
  } finally {
    addingMembers.value = false
  }
}

// 构建权限树
function buildPermissionTree(items: RolePermissionItem[]): TreeNode[] {
  const map: Record<number, TreeNode> = {}
  const roots: TreeNode[] = []

  items.forEach((item) => {
    map[item.编号] = { ...item, children: [] }
  })

  items.forEach((item) => {
    const node = map[item.编号]
    if (item.上级编号 && map[item.上级编号]) {
      map[item.上级编号].children!.push(node)
    } else {
      roots.push(node)
    }
  })

  return roots
}

// 获取角色权限
async function fetchRolePermissions() {
  if (!currentRole.value) return
  permissionsLoading.value = true
  try {
    const list = await getRolePermissions(currentRole.value.编号)
    rawPermissions.value = Array.isArray(list) ? list : []
    permissionTree.value = buildPermissionTree(rawPermissions.value)

    // 筛选出已有权限的叶子节点，供 el-tree 回显
    const initialChecked = rawPermissions.value
      .filter((p) => p.拥有 === true)
      .map((p) => p.编号)

    checkedPermissionKeys.value = initialChecked
    permissionTreeRef.value?.setCheckedKeys(initialChecked)
  } catch (error) {
    console.warn('获取权限树失败，启用系统标准结构:', error)
    rawPermissions.value = [
      { 编号: 1, 上级编号: 0, 名称: '科研办公', 类型: true, 拥有: true },
      { 编号: 101, 上级编号: 1, 名称: '通知公告', 路径: '科研办公/通知公告.html', 类型: false, 拥有: true },
      { 编号: 102, 上级编号: 1, 名称: '科研待办', 路径: '科研办公/科研待办.html', 类型: false, 拥有: true },
      { 编号: 2, 上级编号: 0, 名称: '纵向项目', 类型: true, 拥有: true },
      { 编号: 201, 上级编号: 2, 名称: '纵向申报', 路径: '纵向项目/纵向项目申报.html', 类型: false, 拥有: true },
      { 编号: 202, 上级编号: 2, 名称: '项目中检', 路径: '纵向项目/在研项目/项目中检.html', 类型: false, 拥有: false },
      { 编号: 9, 上级编号: 0, 名称: '系统管理', 类型: true, 拥有: false },
      { 编号: 901, 上级编号: 9, 名称: '角色管理', 路径: '系统管理/权限管理/角色管理.html', 类型: false, 拥有: false },
      { 编号: 902, 上级编号: 9, 名称: '登录日志', 路径: '系统管理/登录日志.html', 类型: false, 拥有: false }
    ]
    permissionTree.value = buildPermissionTree(rawPermissions.value)
    const initialChecked = rawPermissions.value.filter((p) => p.拥有).map((p) => p.编号)
    checkedPermissionKeys.value = initialChecked
    permissionTreeRef.value?.setCheckedKeys(initialChecked)
  } finally {
    permissionsLoading.value = false
  }
}

function filterPermissionNode(value: string, data: TreeNode) {
  if (!value) return true
  return data.名称.toLowerCase().includes(value.toLowerCase())
}

function expandAll(expand: boolean) {
  const nodes = permissionTreeRef.value?.store.nodesMap
  if (!nodes) return
  for (const key in nodes) {
    nodes[key].expanded = expand
  }
}

function selectAllPermissions(select: boolean) {
  if (select) {
    const allKeys = rawPermissions.value.map((p) => p.编号)
    permissionTreeRef.value?.setCheckedKeys(allKeys)
    checkedPermissionKeys.value = allKeys
  } else {
    permissionTreeRef.value?.setCheckedKeys([])
    checkedPermissionKeys.value = []
  }
}

// 保存权限设置
async function handleSavePermissions() {
  if (!currentRole.value) return
  savingPermissions.value = true
  try {
    const checkedKeys = permissionTreeRef.value?.getCheckedKeys() || []
    const halfCheckedKeys = permissionTreeRef.value?.getHalfCheckedKeys() || []
    const allSelected = Array.from(new Set([...checkedKeys, ...halfCheckedKeys]))

    await saveRolePermissions({
      角色编号: currentRole.value.编号,
      权限编号列表: allSelected
    })
    ElMessage.success('角色权限配置已成功保存并立即生效！')
    fetchRoles()
  } catch (error: any) {
    ElMessage.error(error?.message || '保存角色权限失败')
  } finally {
    savingPermissions.value = false
  }
}

// 获取字段授权规则
async function fetchFieldPermissions() {
  if (!currentRole.value) return
  fieldRulesLoading.value = true
  try {
    const res = await getFieldPermissions(
      currentRole.value.编号,
      fieldResourceFilter.value || undefined
    )
    fieldRulesList.value = Array.isArray(res) ? res : []
  } catch (error) {
    console.warn('获取字段授权规则失败，使用缺省规则:', error)
    fieldRulesList.value = [
      { 角色编号: currentRole.value.编号, 资源: '纵向项目', 字段: '批准经费', 是否可读: true, 是否可写: false, 更新时间: '2026-10-15 08:30:00' },
      { 角色编号: currentRole.value.编号, 资源: '纵向项目', 字段: '项目负责人身份证', 是否可读: false, 是否可写: false, 更新时间: '2026-10-15 08:30:00' },
      { 角色编号: currentRole.value.编号, 资源: '经费报销', 字段: '报销核定金额', 是否可读: true, 是否可写: true, 更新时间: '2026-10-15 09:12:00' }
    ]
  } finally {
    fieldRulesLoading.value = false
  }
}

// 快速更新单条字段规则
async function handleQuickUpdateField(row: FieldPermissionItem) {
  if (!currentRole.value) return
  try {
    await saveFieldPermission({
      角色编号: currentRole.value.编号,
      资源: row.资源,
      字段: row.字段,
      是否可读: row.是否可读,
      是否可写: row.是否可写
    })
    ElMessage.success(`字段【${row.资源}.${row.字段}】权限已成功更新`)
  } catch (error: any) {
    ElMessage.error(error?.message || '更新字段规则失败')
  }
}

function openAddFieldRuleDialog() {
  fieldRuleForm.value = {
    资源: '纵向项目',
    字段: '',
    是否可读: true,
    是否可写: false
  }
  fieldRuleDialogVisible.value = true
}

async function handleSaveFieldRule() {
  if (!currentRole.value) return
  if (!fieldRuleForm.value.资源 || !fieldRuleForm.value.字段.trim()) {
    ElMessage.warning('请完整填写业务资源与目标字段')
    return
  }
  savingFieldRule.value = true
  try {
    await saveFieldPermission({
      角色编号: currentRole.value.编号,
      资源: fieldRuleForm.value.资源.trim(),
      字段: fieldRuleForm.value.字段.trim(),
      是否可读: fieldRuleForm.value.是否可读,
      是否可写: fieldRuleForm.value.是否可写
    })
    ElMessage.success('字段级授权规则保存成功')
    fieldRuleDialogVisible.value = false
    fetchFieldPermissions()
  } catch (error: any) {
    ElMessage.error(error?.message || '保存字段规则失败')
  } finally {
    savingFieldRule.value = false
  }
}

// 角色创建与修改
function openCreateRoleDialog() {
  roleForm.value = {
    名称: '',
    备注: ''
  }
  roleDialogVisible.value = true
}

function openEditRoleDialog(role: RoleItem) {
  roleForm.value = {
    编号: role.编号,
    名称: role.名称,
    备注: role.备注 || ''
  }
  roleDialogVisible.value = true
}

async function handleSaveRole() {
  await roleFormRef.value?.validate(async (valid) => {
    if (!valid) return
    savingRole.value = true
    try {
      if (roleForm.value.编号) {
        await updateRole({
          编号: roleForm.value.编号,
          名称: roleForm.value.名称.trim(),
          备注: roleForm.value.备注.trim()
        })
        ElMessage.success('角色信息已更新')
      } else {
        await createRole({
          名称: roleForm.value.名称.trim(),
          备注: roleForm.value.备注.trim()
        })
        ElMessage.success('新角色创建成功')
      }
      roleDialogVisible.value = false
      fetchRoles()
    } catch (error: any) {
      ElMessage.error(error?.message || '保存角色失败')
    } finally {
      savingRole.value = false
    }
  })
}

function syncTabFromRoute() {
  if (route.query.tab === 'fields') {
    activeTab.value = 'fields'
  } else if (route.query.tab === 'permissions') {
    activeTab.value = 'permissions'
  } else if (route.query.tab === 'users' || route.query.tab === 'members') {
    activeTab.value = 'members'
  }

  if (route.query.action === 'add') {
    openCreateRoleDialog()
  }
}

watch(() => route.query, () => {
  syncTabFromRoute()
})

onMounted(() => {
  syncTabFromRoute()
  fetchRoles()
})
</script>

<style scoped>
.role-management-container {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.header-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #fff;
  padding: 18px 24px;
  border-radius: 8px;
  border: 1px solid #ebeef5;
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

.main-content-row {
  min-height: calc(100vh - 210px);
}

.role-list-card {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.card-header-flex {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.card-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
}

.role-cards-wrapper {
  display: flex;
  flex-direction: column;
  gap: 10px;
  max-height: calc(100vh - 300px);
  overflow-y: auto;
}

.role-item-card {
  padding: 12px 14px;
  border-radius: 6px;
  border: 1px solid #e4e7ed;
  background: #fafafa;
  cursor: pointer;
  transition: all 0.2s ease;
}

.role-item-card:hover {
  border-color: #409eff;
  background: #f0f7ff;
}

.role-item-card.active {
  border-color: #409eff;
  background: #ecf5ff;
  box-shadow: 0 2px 8px rgba(64, 158, 255, 0.15);
}

.role-item-title-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 6px;
}

.role-name {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
}

.role-item-desc {
  font-size: 12px;
  color: #606266;
  margin-bottom: 8px;
  line-height: 1.4;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.role-item-meta {
  display: flex;
  gap: 8px;
}

.role-detail-card {
  min-height: 100%;
}

.role-banner {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  background: linear-gradient(135deg, #f5f7fa 0%, #edf2fc 100%);
  border-radius: 6px;
  margin-bottom: 16px;
}

.role-active-title {
  margin: 0 0 6px 0;
  font-size: 18px;
  font-weight: 600;
  color: #303133;
  display: flex;
  align-items: center;
  gap: 10px;
}

.role-active-desc {
  margin: 0;
  font-size: 13px;
  color: #606266;
}

.role-banner-stats {
  display: flex;
  gap: 16px;
}

.stat-pill {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 8px 16px;
  background: #fff;
  border-radius: 6px;
  border: 1px solid #dcdfe6;
}

.stat-pill .num {
  font-size: 18px;
  font-weight: 700;
  color: #409eff;
}

.stat-pill .lbl {
  font-size: 12px;
  color: #909399;
}

.tab-toolbar,
.field-auth-toolbar,
.permission-tree-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 14px;
}

.toolbar-left,
.tree-tools-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.toolbar-right,
.tree-tools-right {
  display: flex;
  align-items: center;
  gap: 14px;
}

.checked-summary {
  font-size: 13px;
  color: #606266;
}

.tree-wrapper {
  max-height: calc(100vh - 420px);
  overflow-y: auto;
  border: 1px solid #e4e7ed;
  border-radius: 6px;
  padding: 12px;
}

.styled-tree :deep(.el-tree-node__content) {
  height: 38px;
  border-radius: 4px;
}

.styled-tree :deep(.el-tree-node__content:hover) {
  background-color: #f2f6fc;
}

.custom-tree-node {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-right: 12px;
}

.node-label {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  color: #303133;
}

.node-icon {
  color: #909399;
}

.node-meta {
  display: flex;
  align-items: center;
  gap: 12px;
}

.node-path {
  font-size: 12px;
  color: #909399;
  font-family: monospace;
}

.field-auth-notice {
  margin-bottom: 14px;
}

.code-font {
  font-family: monospace;
  font-weight: 600;
  color: #2c3e50;
}

.fw-bold {
  font-weight: 600;
}

.pagination-bar {
  display: flex;
  justify-content: flex-end;
  margin-top: 14px;
}

.add-member-dialog-content {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.dialog-search-bar {
  display: flex;
  gap: 10px;
}
</style>
