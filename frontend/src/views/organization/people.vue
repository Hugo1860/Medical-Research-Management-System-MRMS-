<template>
  <div class="people-page-container">
    <el-row :gutter="16" class="full-height-row">
      <!-- 左侧：部门树筛选与组织架构维护 -->
      <el-col :xs="24" :sm="8" :md="6" class="tree-col">
        <el-card shadow="never" class="tree-card">
          <template #header>
            <div class="tree-header">
              <span class="header-title">组织架构与部门</span>
              <div class="header-actions">
                <el-button link type="primary" size="small" @click="openDeptDialog(false)">新增部门</el-button>
                <el-button link type="info" size="small" @click="clearDeptFilter">全部</el-button>
              </div>
            </div>
          </template>
          <el-input
            v-model="deptSearch"
            placeholder="搜索部门名称..."
            prefix-icon="Search"
            clearable
            style="margin-bottom: 12px;"
          />
          <el-scrollbar style="height: calc(100vh - 240px);">
            <el-tree
              ref="deptTreeRef"
              :data="departmentTree"
              :props="{ label: '名称', children: 'children' }"
              :filter-node-method="filterDeptNode"
              node-key="编号"
              highlight-current
              default-expand-all
              @node-click="handleDeptNodeClick"
            >
              <template #default="{ data }">
                <div class="dept-tree-node">
                  <span class="node-label" :class="{ disabled: !data.是否启用 }">
                    {{ data.名称 }}
                  </span>
                  <div class="node-actions" @click.stop>
                    <el-button
                      link
                      type="primary"
                      :icon="Edit"
                      size="small"
                      title="编辑部门"
                      @click="openDeptDialog(true, data)"
                    />
                    <el-button
                      link
                      type="success"
                      :icon="Plus"
                      size="small"
                      title="添加子部门"
                      @click="openDeptDialog(false, undefined, data.编号)"
                    />
                  </div>
                </div>
              </template>
            </el-tree>
          </el-scrollbar>
        </el-card>
      </el-col>

      <!-- 右侧：人员列表表格 -->
      <el-col :xs="24" :sm="16" :md="18" class="table-col">
        <el-card shadow="never" class="table-card">
          <!-- 搜索与工具栏 -->
          <div class="filter-toolbar">
            <el-form :inline="true" :model="queryFilter" class="filter-form">
              <el-form-item label="人员工号">
                <el-input v-model="queryFilter.工号" placeholder="按工号搜索" clearable style="width: 130px;" />
              </el-form-item>
              <el-form-item label="人员姓名">
                <el-input v-model="queryFilter.Like姓名" placeholder="姓名模糊搜索" clearable style="width: 130px;" />
              </el-form-item>
              <el-form-item label="启用状态">
                <el-select v-model="queryFilter.是否启用" placeholder="全部" clearable style="width: 100px;">
                  <el-option label="已启用" :value="true" />
                  <el-option label="已禁用" :value="false" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
                <el-button :icon="Refresh" @click="resetSearch">重置</el-button>
                <el-button type="success" :icon="Plus" @click="openCreateDialog">新增人员</el-button>
                <el-button :icon="Upload" @click="importDialogVisible = true">批量导入</el-button>
                <el-button type="warning" :icon="User" @click="userPickerVisible = true">快速选人</el-button>
                <el-button type="warning" plain :icon="Search" @click="partnerDialogVisible = true">找合作人</el-button>
              </el-form-item>
            </el-form>
          </div>

          <!-- 表格主体 -->
          <el-table
            v-loading="loading"
            :data="peopleList"
            stripe
            style="width: 100%;"
            height="calc(100vh - 300px)"
          >
            <el-table-column prop="工号" label="工号" width="110" />
            <el-table-column prop="姓名" label="姓名" width="110">
              <template #default="{ row }">
                <span class="user-name-highlight">{{ row.姓名 }}</span>
              </template>
            </el-table-column>
            <el-table-column label="性别" width="70" align="center">
              <template #default="{ row }">
                <span>{{ row.性别 ? '男' : '女' }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="部门名称" label="所属部门" min-width="140" show-overflow-tooltip />
            <el-table-column prop="电话" label="联系电话" width="130">
              <template #default="{ row }">
                {{ row.电话 || row.手机 || '-' }}
              </template>
            </el-table-column>
            <el-table-column prop="邮箱" label="电子邮箱" min-width="150" show-overflow-tooltip />
            <el-table-column prop="技术职称" label="技术职称" width="120" show-overflow-tooltip />
            <el-table-column label="账号状态" width="90" align="center">
              <template #default="{ row }">
                <el-tag :type="row.是否启用 ? 'success' : 'danger'" size="small">
                  {{ row.是否启用 ? '正常' : '禁用' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="180" fixed="right" align="center">
              <template #default="{ row }">
                <el-button type="primary" link size="small" @click="openEditDialog(row)">编辑</el-button>
                <el-button
                  :type="row.是否启用 ? 'danger' : 'success'"
                  link
                  size="small"
                  @click="togglePersonStatus(row)"
                >
                  {{ row.是否启用 ? '禁用' : '启用' }}
                </el-button>
                <el-button type="info" link size="small" @click="viewCapability(row)">画像</el-button>
              </template>
            </el-table-column>
          </el-table>

          <!-- 分页栏 -->
          <div class="pagination-wrapper">
            <el-pagination
              v-model:current-page="paging.index"
              v-model:page-size="paging.size"
              :total="totalCount"
              :page-sizes="[10, 20, 50, 100]"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="fetchPeople"
              @current-change="fetchPeople"
            />
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 新增/编辑部门弹窗 (对应老旧 组织架构/添加部门.html) -->
    <el-dialog
      v-model="deptDialogVisible"
      :title="isDeptEdit ? '编辑部门信息' : '新增组织部门'"
      width="500px"
      destroy-on-close
    >
      <el-form ref="deptFormRef" :model="deptForm" :rules="deptRules" label-width="95px">
        <el-form-item label="部门名称" prop="名称">
          <el-input v-model="deptForm.名称" placeholder="请输入部门名称" />
        </el-form-item>
        <el-form-item label="上级部门">
          <el-select v-model="deptForm.上级部门编号" placeholder="请选择上级部门" style="width: 100%;">
            <el-option label="无（顶级部门）" :value="0" />
            <el-option
              v-for="d in rawDepartments"
              :key="d.编号"
              :label="d.名称"
              :value="d.编号"
              :disabled="isDeptEdit && d.编号 === deptForm.编号"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="部门负责人">
          <el-input
            v-model="deptForm.负责人姓名"
            placeholder="点击选择负责人"
            readonly
            @click="deptLeaderPickerVisible = true"
          >
            <template #append>
              <el-button :icon="Search" @click="deptLeaderPickerVisible = true">选择</el-button>
            </template>
          </el-input>
        </el-form-item>
        <el-form-item label="排序权重">
          <el-input-number v-model="deptForm.排序值" :min="0" :max="999" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="启用状态">
          <el-switch v-model="deptForm.是否启用" active-text="启用" inactive-text="停用" />
        </el-form-item>
        <el-form-item label="备注说明">
          <el-input v-model="deptForm.备注" type="textarea" :rows="3" placeholder="可选备注信息" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="deptDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="deptSubmitLoading" @click="handleDeptSubmit">
          保存部门
        </el-button>
      </template>
    </el-dialog>

    <!-- 新增/编辑人员全量信息抽屉 (对应老旧 组织架构/添加用户.html) -->
    <el-drawer
      v-model="editDialogVisible"
      :title="isEdit ? `编辑人员资料（${personForm.姓名}）` : '新增科研人员档案'"
      size="860px"
      destroy-on-close
    >
      <el-form
        ref="personFormRef"
        :model="personForm"
        :rules="personRules"
        label-width="110px"
        class="person-full-form"
      >
        <el-tabs v-model="activePersonTab">
          <!-- 基础信息 -->
          <el-tab-pane label="基本档案" name="basic">
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item label="工号" prop="工号">
                  <el-input v-model="personForm.工号" placeholder="请输入工号" :disabled="isEdit" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="姓名" prop="姓名">
                  <el-input v-model="personForm.姓名" placeholder="请输入姓名" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="身份证" prop="身份证">
                  <el-input v-model="personForm.身份证" placeholder="请输入有效身份证件号" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="初始密码" prop="密码">
                  <el-input
                    v-model="personForm.密码"
                    type="password"
                    show-password
                    placeholder="留空则保持原密码"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="所属部门" prop="部门编号">
                  <el-select v-model="personForm.部门编号" placeholder="请选择部门" style="width: 100%;">
                    <el-option
                      v-for="dept in rawDepartments"
                      :key="dept.编号"
                      :label="dept.名称"
                      :value="dept.编号"
                    />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="性别">
                  <el-radio-group v-model="personForm.性别">
                    <el-radio :value="'男'">男</el-radio>
                    <el-radio :value="'女'">女</el-radio>
                  </el-radio-group>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="手机号码">
                  <el-input v-model="personForm.手机" placeholder="常用手机号码" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="办公邮箱">
                  <el-input v-model="personForm.邮箱" placeholder="工作电子邮箱" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="民族">
                  <el-select v-model="personForm.民族" placeholder="请选择民族" clearable style="width: 100%;">
                    <el-option v-for="m in auxData.民族" :key="m" :label="m" :value="m" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="政治面貌">
                  <el-select v-model="personForm.政治面貌" placeholder="请选择政治面貌" clearable style="width: 100%;">
                    <el-option v-for="p in auxData.政治面貌" :key="p" :label="p" :value="p" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="出生日期">
                  <el-date-picker
                    v-model="personForm.出生日期"
                    type="date"
                    value-format="YYYY-MM-DD"
                    placeholder="选择出生日期"
                    style="width: 100%;"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="婚姻情况">
                  <el-select v-model="personForm.婚姻情况" placeholder="请选择" clearable style="width: 100%;">
                    <el-option label="未婚" value="未婚" />
                    <el-option label="已婚" value="已婚" />
                    <el-option label="初婚" value="初婚" />
                    <el-option label="再婚" value="再婚" />
                    <el-option label="离异" value="离异" />
                    <el-option label="丧偶" value="丧偶" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="籍贯">
                  <el-input v-model="personForm.籍贯" placeholder="省/市籍贯" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="QQ号码">
                  <el-input v-model="personForm.QQ" placeholder="常用QQ号码" />
                </el-form-item>
              </el-col>
              <el-col :span="24">
                <el-form-item label="住宅地址">
                  <el-input v-model="personForm.住宅地址" placeholder="常住联系地址" />
                </el-form-item>
              </el-col>
            </el-row>
          </el-tab-pane>

          <!-- 人事信息 -->
          <el-tab-pane label="人事与岗位" name="hr">
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item label="人员类别">
                  <el-select v-model="personForm.人员类别" placeholder="请选择人员类别" clearable style="width: 100%;">
                    <el-option v-for="c in auxData.人员类别" :key="c" :label="c" :value="c" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="岗位性质">
                  <el-select v-model="personForm.岗位性质" placeholder="请选择岗位性质" clearable style="width: 100%;">
                    <el-option v-for="c in auxData.岗位性质" :key="c" :label="c" :value="c" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="岗位类型">
                  <el-select v-model="personForm.岗位类型" placeholder="请选择岗位类型" clearable style="width: 100%;">
                    <el-option v-for="c in auxData.岗位类型" :key="c" :label="c" :value="c" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="岗位名称">
                  <el-select v-model="personForm.岗位名称" placeholder="请选择岗位名称" clearable style="width: 100%;">
                    <el-option v-for="c in auxData.岗位名称" :key="c" :label="c" :value="c" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="入职日期">
                  <el-date-picker
                    v-model="personForm.入职日期"
                    type="date"
                    value-format="YYYY-MM-DD"
                    placeholder="选择入职日期"
                    style="width: 100%;"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="参加工作时间">
                  <el-date-picker
                    v-model="personForm.参加工作日期"
                    type="date"
                    value-format="YYYY-MM-DD"
                    placeholder="选择参加工作时间"
                    style="width: 100%;"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="合同开始日期">
                  <el-date-picker
                    v-model="personForm.合同开始日期"
                    type="date"
                    value-format="YYYY-MM-DD"
                    placeholder="开始时间"
                    style="width: 100%;"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="合同结束日期">
                  <el-date-picker
                    v-model="personForm.合同结束日期"
                    type="date"
                    value-format="YYYY-MM-DD"
                    placeholder="结束时间"
                    style="width: 100%;"
                  />
                </el-form-item>
              </el-col>
            </el-row>
          </el-tab-pane>

          <!-- 教育与学科 -->
          <el-tab-pane label="学历与学科" name="education">
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item label="最高学历">
                  <el-select v-model="personForm.学历" placeholder="最高学历" clearable style="width: 100%;">
                    <el-option v-for="e in auxData.学历" :key="e" :label="e" :value="e" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="最高学位">
                  <el-select v-model="personForm.学位" placeholder="最高学位" clearable style="width: 100%;">
                    <el-option v-for="d in auxData.学位" :key="d" :label="d" :value="d" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="学科门类">
                  <el-select v-model="personForm.学科门类" placeholder="学科门类" clearable style="width: 100%;">
                    <el-option v-for="s in auxData.学科门类" :key="s" :label="s" :value="s" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="一级学科">
                  <el-input v-model="personForm.一级学科" placeholder="如：临床医学、基础医学" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="二级学科">
                  <el-input v-model="personForm.二级学科" placeholder="如：内科学、外科学" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="三级学科">
                  <el-input v-model="personForm.三级学科" placeholder="如：心血管病学" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="毕业学校">
                  <el-input v-model="personForm.获得最高学历的院校" placeholder="最高学历院校名称" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="最高学位院校">
                  <el-input v-model="personForm.获得最高学位的院校" placeholder="最高学位授予单位" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="学习形式">
                  <el-select v-model="personForm.学习形式" placeholder="全日制 / 非全日制" clearable style="width: 100%;">
                    <el-option v-for="s in auxData.学习形式" :key="s" :label="s" :value="s" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="毕业时间">
                  <el-date-picker
                    v-model="personForm.毕业日期"
                    type="date"
                    value-format="YYYY-MM-DD"
                    placeholder="选择毕业时间"
                    style="width: 100%;"
                  />
                </el-form-item>
              </el-col>
            </el-row>
          </el-tab-pane>

          <!-- 职称与科研特长 -->
          <el-tab-pane label="职称与特长" name="academic">
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item label="技术职称分类">
                  <el-select v-model="personForm.技术职称类别" placeholder="技术职称分类" clearable style="width: 100%;">
                    <el-option v-for="t in auxData.技术职称分类" :key="t" :label="t" :value="t" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="技术职称">
                  <el-input v-model="personForm.技术职称" placeholder="如：主任医师、研究员" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="专业技术级别">
                  <el-select v-model="personForm.专业技术级别" placeholder="专业技术级别" clearable style="width: 100%;">
                    <el-option v-for="l in auxData.专业技术级别" :key="l" :label="l" :value="l" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="职务名称">
                  <el-select v-model="personForm.职务名称" placeholder="选择职务" clearable style="width: 100%;">
                    <el-option v-for="p in auxData.职位" :key="p" :label="p" :value="p" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="24">
                <el-form-item label="学术特长">
                  <el-input
                    v-model="personForm.学术特长"
                    type="textarea"
                    :rows="3"
                    placeholder="请输入学术技术专长领域..."
                  />
                </el-form-item>
              </el-col>
              <el-col :span="24">
                <el-form-item label="研究方向">
                  <el-input
                    v-model="personForm.研究方向"
                    type="textarea"
                    :rows="3"
                    placeholder="重点攻关方向及科研选题领域..."
                  />
                </el-form-item>
              </el-col>
              <el-col :span="24">
                <el-form-item label="荣誉与奖项">
                  <el-input
                    v-model="personForm.个人获得的荣誉和奖项"
                    type="textarea"
                    :rows="3"
                    placeholder="国家级/省部级/市级科学技术奖及人才称号..."
                  />
                </el-form-item>
              </el-col>
            </el-row>
          </el-tab-pane>
        </el-tabs>
      </el-form>
      <template #footer>
        <div class="drawer-footer">
          <el-button @click="editDialogVisible = false">取消</el-button>
          <el-button type="primary" :loading="submitLoading" @click="handlePersonSubmit">
            保存人员档案
          </el-button>
        </div>
      </template>
    </el-drawer>

    <!-- 负责人/人员选择弹窗 -->
    <UserSelectDialog
      v-model="deptLeaderPickerVisible"
      title="选择部门负责人"
      :multiple="false"
      @confirm="onDeptLeaderSelected"
    />

    <!-- 通用选人弹窗测试与交互 -->
    <UserSelectDialog
      v-model="userPickerVisible"
      title="组织架构快速选人"
      :multiple="true"
      @confirm="onUsersQuickSelected"
    />

    <!-- 人员批量导入弹窗 -->
    <DataImportDialog
      v-model="importDialogVisible"
      title="批量导入科研人员档案"
      template-name="医院科研人员导入模板.xlsx"
      :upload-handler="handleImportPeople"
      @success="handleSearch"
    />

    <!-- 科研能力画像抽屉 -->
    <el-drawer v-model="capabilityDrawerVisible" title="科研人员能力与分工画像" size="620px">
      <div v-if="capabilityProfile" class="capability-box" v-loading="capabilityLoading">
        <div class="profile-header">
          <el-avatar :size="56" style="background-color: #409eff; font-size: 20px;">
            {{ capabilityProfile.人员.姓名?.substring(0, 1) }}
          </el-avatar>
          <div style="margin-left: 12px;">
            <h3>{{ capabilityProfile.人员.姓名 }}</h3>
            <p style="color: #64748b; font-size: 13px;">工号: {{ capabilityProfile.人员.工号 }} · {{ capabilityProfile.人员.部门名称 }}</p>
          </div>
        </div>

        <el-divider />
        <el-form :model="capabilityForm" label-width="110px" class="capability-form">
          <el-form-item label="研究专长">
            <el-input v-model="capabilityForm.研究专长" type="textarea" :rows="2" placeholder="可用逗号分隔多个专长" />
          </el-form-item>
          <el-form-item label="研究方向标签">
            <el-input v-model="capabilityForm.研究方向标签" placeholder="如：肿瘤、临床试验、循证医学" />
          </el-form-item>
          <el-form-item label="技术服务能力">
            <el-input v-model="capabilityForm.技术服务能力" type="textarea" :rows="2" placeholder="可提供的方法学、统计、样本等服务" />
          </el-form-item>
          <el-row :gutter="12">
            <el-col :span="12">
              <el-form-item label="合作意愿">
                <el-select v-model="capabilityForm.合作意愿" style="width: 100%;">
                  <el-option label="可合作" value="可合作" />
                  <el-option label="需邀约" value="需邀约" />
                  <el-option label="暂不合作" value="暂不合作" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="每周可用小时">
                <el-input-number v-model="capabilityForm.每周可用小时" :min="0" :max="168" :precision="1" style="width: 100%;" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="12">
            <el-col :span="12">
              <el-form-item label="可承担项目数">
                <el-input-number v-model="capabilityForm.可承担项目数" :min="0" :max="100" style="width: 100%;" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="当前可接项目"><el-switch v-model="capabilityForm.当前可接项目" /></el-form-item>
            </el-col>
          </el-row>
          <el-form-item label="可用时间说明">
            <el-input v-model="capabilityForm.可用时间" placeholder="如：周二/周四 18:00 后，寒暑假可集中参与" />
          </el-form-item>
          <el-form-item label="合作说明">
            <el-input v-model="capabilityForm.合作说明" type="textarea" :rows="2" />
          </el-form-item>
          <el-form-item label="对外展示"><el-switch v-model="capabilityForm.可公开展示" /></el-form-item>
        </el-form>
        <div class="capability-actions"><el-button type="primary" :loading="capabilitySaving" @click="saveCapabilityForm">保存能力画像</el-button></div>

        <el-divider />
        <h4>能力标签</h4>
        <div v-if="capabilityProfile.能力标签?.length" class="tag-list">
          <el-tag v-for="tag in capabilityProfile.能力标签" :key="tag.编号" style="margin: 0 6px 6px 0" effect="plain">{{ tag.标签类型 }} · {{ tag.标签 }}</el-tag>
        </div>
        <el-empty v-else description="尚未维护能力标签" :image-size="50" />
        <h4>导师关系</h4>
        <el-table :data="capabilityProfile.导师关系" size="small" border max-height="180">
          <el-table-column prop="导师姓名" label="导师" />
          <el-table-column prop="学员姓名" label="学员" />
          <el-table-column prop="关系类型" label="关系" />
          <el-table-column prop="状态" label="状态" />
        </el-table>
        <h4>团队与项目分工</h4>
        <el-table :data="capabilityProfile.团队" size="small" border max-height="180">
          <el-table-column prop="团队名称" label="团队" show-overflow-tooltip />
          <el-table-column prop="成员角色" label="角色" width="90" />
          <el-table-column prop="成员状态" label="状态" width="80" />
        </el-table>
        <el-table :data="capabilityProfile.项目分工" size="small" border max-height="180" style="margin-top: 10px">
          <el-table-column prop="项目编号" label="项目" width="80" />
          <el-table-column prop="分工类型" label="分工" />
          <el-table-column prop="工作量占比" label="工作量%" width="90" />
          <el-table-column prop="状态" label="状态" width="80" />
        </el-table>
      </div>
    </el-drawer>

    <!-- 合作人匹配 -->
    <el-dialog v-model="partnerDialogVisible" title="科研合作人匹配" width="900px" destroy-on-close>
      <el-form :inline="true" :model="partnerQuery" @submit.prevent="loadPartners">
        <el-form-item label="关键词"><el-input v-model="partnerQuery.关键词" clearable placeholder="研究专长/方向/姓名" /></el-form-item>
        <el-form-item label="技术服务"><el-input v-model="partnerQuery.技术服务" clearable placeholder="统计、样本、伦理等" /></el-form-item>
        <el-form-item label="最低每周小时"><el-input-number v-model="partnerQuery.最低每周可用小时" :min="0" :max="168" /></el-form-item>
        <el-form-item><el-button type="primary" :loading="partnerLoading" @click="loadPartners">匹配</el-button></el-form-item>
      </el-form>
      <el-table :data="partnerList" v-loading="partnerLoading" border stripe>
        <el-table-column prop="姓名" label="人员" width="100" />
        <el-table-column prop="部门名称" label="部门" width="140" />
        <el-table-column prop="研究专长" label="研究专长" min-width="180" show-overflow-tooltip />
        <el-table-column prop="技术服务能力" label="技术服务" min-width="180" show-overflow-tooltip />
        <el-table-column prop="每周可用小时" label="可用小时" width="90" />
        <el-table-column prop="剩余项目容量" label="剩余容量" width="90" />
        <el-table-column prop="匹配分数" label="匹配分" width="80" />
        <el-table-column label="合作意愿" width="95"><template #default="{ row }"><el-tag size="small">{{ row.合作意愿 }}</el-tag></template></el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import {
  getDepartmentList,
  getPeopleList,
  savePerson,
  enablePerson,
  disablePerson,
  saveDepartment,
  getPersonDetail,
  getPeopleDetailAuxData,
  importPeople,
  getPersonCapability,
  savePersonCapability,
  findResearchPartners,
  type PersonCapability,
  type PersonCapabilityProfile,
  type ResearchPartner,
  type Department,
  type PersonItem,
  type PersonDetail
} from '@/api/organization'
import UserSelectDialog from '@/components/UserSelectDialog.vue'
import DataImportDialog from '@/components/DataImportDialog.vue'
import { ElMessage, ElMessageBox, type ElTree, type FormInstance } from 'element-plus'
import { Search, Plus, Refresh, Edit, User, Upload } from '@element-plus/icons-vue'

const route = useRoute()

const loading = ref(false)
const submitLoading = ref(false)
const importDialogVisible = ref(false)
const deptSearch = ref('')
const deptTreeRef = ref<InstanceType<typeof ElTree>>()

const rawDepartments = ref<Department[]>([])
const departmentTree = ref<any[]>([])
const peopleList = ref<PersonItem[]>([])
const totalCount = ref(0)

const paging = reactive({
  index: 1,
  size: 20
})

const queryFilter = reactive({
  工号: '',
  Like姓名: '',
  部门编号: undefined as number | undefined,
  是否启用: undefined as boolean | undefined
})

// 辅助字典数据
const auxData = reactive({
  民族: [] as string[],
  政治面貌: [] as string[],
  人员类别: [] as string[],
  岗位名称: [] as string[],
  岗位类型: [] as string[],
  岗位性质: [] as string[],
  学历: [] as string[],
  学位: [] as string[],
  学科门类: [] as string[],
  学制: [] as string[],
  所学专业: [] as string[],
  学习形式: [] as string[],
  学历性质: [] as string[],
  个人身份: [] as string[],
  职位: [] as string[],
  职位级别: [] as string[],
  专业技术级别: [] as string[],
  技术职称分类: [] as string[]
})

// 部门弹窗相关
const deptDialogVisible = ref(false)
const isDeptEdit = ref(false)
const deptSubmitLoading = ref(false)
const deptLeaderPickerVisible = ref(false)
const userPickerVisible = ref(false)
const deptFormRef = ref<FormInstance>()

const deptForm = reactive({
  编号: 0,
  名称: '',
  上级部门编号: 0,
  负责人编号: null as number | null,
  负责人姓名: '',
  排序值: 10,
  备注: '',
  是否启用: true
})

const deptRules = {
  名称: [{ required: true, message: '部门名称不能为空', trigger: 'blur' }]
}

// 人员抽屉相关
const editDialogVisible = ref(false)
const isEdit = ref(false)
const activePersonTab = ref('basic')
const personFormRef = ref<FormInstance>()

const personForm = reactive<PersonDetail>({
  编号: 0,
  工号: '',
  姓名: '',
  密码: '',
  部门编号: 0,
  部门名称: '',
  性别: '男',
  身份证: '',
  出生日期: '',
  入职日期: '',
  手机: '',
  电话: '',
  QQ: '',
  邮箱: '',
  籍贯: '',
  毕业日期: '',
  合同开始日期: '',
  合同结束日期: '',
  参加工作日期: '',
  专业技术获得日期: '',
  职务任职日期: '',
  学位: '',
  学历: '',
  政治面貌: '',
  技术职称类别: '',
  技术职称: '',
  第二技术职称类别: '',
  第二技术职称: '',
  婚姻情况: '',
  学制: '',
  学历性质: '',
  学习形式: '',
  人员类别: '',
  个人身份: '',
  民族: '',
  专业技术级别: '',
  所学专业名称: '',
  岗位性质: '',
  职务名称: '',
  职务级别: '',
  岗位名称: '',
  岗位类型: '',
  学科门类: '',
  一级学科: '',
  二级学科: '',
  三级学科: '',
  人员类型: '',
  获得最高学历的院校: '',
  获得最高学位的院校: '',
  住宅地址: '',
  学术特长: '',
  研究方向: '',
  个人获得的荣誉和奖项: '',
  备注: '',
  是否启用: true
})

const personRules = {
  工号: [{ required: true, message: '请输入工号', trigger: 'blur' }],
  姓名: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  部门编号: [{ required: true, message: '请选择所属部门', trigger: 'change' }]
}

const capabilityDrawerVisible = ref(false)
const currentPersonProfile = ref<PersonItem | null>(null)
const capabilityProfile = ref<PersonCapabilityProfile | null>(null)
const capabilityLoading = ref(false)
const capabilitySaving = ref(false)
const capabilityForm = reactive<PersonCapability>({
  人员编号: 0,
  研究专长: '',
  研究方向标签: '',
  技术服务能力: '',
  合作意愿: '可合作',
  合作说明: '',
  可用时间: '',
  每周可用小时: 0,
  可承担项目数: 3,
  当前可接项目: true,
  可公开展示: true
})
const partnerDialogVisible = ref(false)
const partnerLoading = ref(false)
const partnerList = ref<ResearchPartner[]>([])
const partnerQuery = reactive({
  关键词: '',
  技术服务: '',
  最低每周可用小时: undefined as number | undefined
})

watch(deptSearch, (val) => {
  deptTreeRef.value?.filter(val)
})

function filterDeptNode(value: string, data: any) {
  if (!value) return true
  return data.名称?.includes(value)
}

function handleDeptNodeClick(data: Department) {
  queryFilter.部门编号 = data.编号
  paging.index = 1
  fetchPeople()
}

function clearDeptFilter() {
  queryFilter.部门编号 = undefined
  deptTreeRef.value?.setCurrentKey(undefined)
  paging.index = 1
  fetchPeople()
}

function handleSearch() {
  paging.index = 1
  fetchPeople()
}

async function handleImportPeople(file: File) {
  return await importPeople(file)
}

function resetSearch() {
  queryFilter.工号 = ''
  queryFilter.Like姓名 = ''
  queryFilter.部门编号 = undefined
  queryFilter.是否启用 = undefined
  paging.index = 1
  fetchPeople()
}

async function fetchDepartments() {
  try {
    const list = await getDepartmentList()
    rawDepartments.value = Array.isArray(list) ? list : []
    departmentTree.value = buildDeptTree(rawDepartments.value, 0)
  } catch (err: any) {
    rawDepartments.value = []
    departmentTree.value = []
    ElMessage.error(err?.response?.data?.error || err?.message || '组织架构读取失败，请稍后重试')
  }
}

function buildDeptTree(list: Department[], parentId: number): any[] {
  return list
    .filter((d) => (d.上级部门编号 || 0) === parentId)
    .map((d) => ({
      ...d,
      children: buildDeptTree(list, d.编号)
    }))
}

async function fetchAuxData() {
  try {
    const data = await getPeopleDetailAuxData()
    if (data) {
      Object.assign(auxData, data)
    }
  } catch (err: any) {
    Object.keys(auxData).forEach(key => { (auxData as any)[key] = [] })
    ElMessage.error(err?.response?.data?.error || err?.message || '人员字典读取失败，请稍后重试')
  }
}

async function fetchPeople() {
  loading.value = true
  try {
    const res = await getPeopleList(paging, queryFilter)
    peopleList.value = Array.isArray(res?.list) ? res.list : []
    totalCount.value = Number(res?.total) || peopleList.value.length
  } catch (err: any) {
    peopleList.value = []
    totalCount.value = 0
    ElMessage.error(err?.response?.data?.error || err?.message || '人员列表读取失败，请稍后重试')
  } finally {
    loading.value = false
  }
}

// 部门增改弹窗逻辑
function openDeptDialog(isEditMode: boolean, deptData?: Department, parentId?: number) {
  isDeptEdit.value = isEditMode
  if (isEditMode && deptData) {
    deptForm.编号 = deptData.编号
    deptForm.名称 = deptData.名称
    deptForm.上级部门编号 = deptData.上级部门编号 || 0
    deptForm.负责人编号 = deptData.负责人编号 || null
    deptForm.负责人姓名 = deptData.负责人姓名 || ''
    deptForm.排序值 = deptData.排序值 ?? 10
    deptForm.备注 = deptData.备注 || ''
    deptForm.是否启用 = deptData.是否启用 !== false
  } else {
    deptForm.编号 = 0
    deptForm.名称 = ''
    deptForm.上级部门编号 = parentId !== undefined ? parentId : 0
    deptForm.负责人编号 = null
    deptForm.负责人姓名 = ''
    deptForm.排序值 = 10
    deptForm.备注 = ''
    deptForm.是否启用 = true
  }
  deptDialogVisible.value = true
}

function onDeptLeaderSelected(users: PersonItem[]) {
  if (users.length > 0) {
    deptForm.负责人编号 = users[0].编号
    deptForm.负责人姓名 = users[0].姓名
  }
}

function onUsersQuickSelected(users: PersonItem[]) {
  ElMessage.success(`已选定 ${users.length} 名科研人员：${users.map(u => u.姓名).join('、')}`)
}

async function handleDeptSubmit() {
  if (!deptFormRef.value) return
  await deptFormRef.value.validate(async (valid) => {
    if (!valid) return
    deptSubmitLoading.value = true
    try {
      await saveDepartment({
        编号: deptForm.编号,
        名称: deptForm.名称,
        上级部门编号: deptForm.上级部门编号,
        负责人编号: deptForm.负责人编号,
        排序值: deptForm.排序值,
        备注: deptForm.备注,
        是否启用: deptForm.是否启用
      })
      ElMessage.success(isDeptEdit.value ? '修改部门成功' : '新增部门成功')
      deptDialogVisible.value = false
      await fetchDepartments()
    } catch (err: any) {
      ElMessage.error(err.response?.data?.error || err.message || '保存部门失败')
    } finally {
      deptSubmitLoading.value = false
    }
  })
}

// 人员详情与编辑逻辑
function resetPersonForm() {
  personForm.编号 = 0
  personForm.工号 = ''
  personForm.姓名 = ''
  personForm.密码 = ''
  personForm.部门编号 = queryFilter.部门编号 || rawDepartments.value[0]?.编号 || 0
  personForm.部门名称 = ''
  personForm.性别 = '男'
  personForm.身份证 = ''
  personForm.出生日期 = ''
  personForm.入职日期 = ''
  personForm.手机 = ''
  personForm.电话 = ''
  personForm.QQ = ''
  personForm.邮箱 = ''
  personForm.籍贯 = ''
  personForm.毕业日期 = ''
  personForm.合同开始日期 = ''
  personForm.合同结束日期 = ''
  personForm.参加工作日期 = ''
  personForm.专业技术获得日期 = ''
  personForm.职务任职日期 = ''
  personForm.学位 = ''
  personForm.学历 = ''
  personForm.政治面貌 = ''
  personForm.技术职称类别 = ''
  personForm.技术职称 = ''
  personForm.第二技术职称类别 = ''
  personForm.第二技术职称 = ''
  personForm.婚姻情况 = ''
  personForm.学制 = ''
  personForm.学历性质 = ''
  personForm.学习形式 = ''
  personForm.人员类别 = ''
  personForm.个人身份 = ''
  personForm.民族 = '汉族'
  personForm.专业技术级别 = ''
  personForm.所学专业名称 = ''
  personForm.岗位性质 = ''
  personForm.职务名称 = ''
  personForm.职务级别 = ''
  personForm.岗位名称 = ''
  personForm.岗位类型 = ''
  personForm.学科门类 = ''
  personForm.一级学科 = ''
  personForm.二级学科 = ''
  personForm.三级学科 = ''
  personForm.人员类型 = ''
  personForm.获得最高学历的院校 = ''
  personForm.获得最高学位的院校 = ''
  personForm.住宅地址 = ''
  personForm.学术特长 = ''
  personForm.研究方向 = ''
  personForm.个人获得的荣誉和奖项 = ''
  personForm.备注 = ''
  personForm.是否启用 = true
}

function openCreateDialog() {
  isEdit.value = false
  activePersonTab.value = 'basic'
  resetPersonForm()
  editDialogVisible.value = true
}

async function openEditDialog(item: PersonItem) {
  isEdit.value = true
  activePersonTab.value = 'basic'
  resetPersonForm()

  try {
    const detail = await getPersonDetail(item.编号)
    if (detail) {
      Object.assign(personForm, detail)
      if (typeof detail.性别 === 'boolean') {
        personForm.性别 = detail.性别 ? '男' : '女'
      }
    } else {
      Object.assign(personForm, item)
    }
  } catch {
    Object.assign(personForm, item)
  }
  editDialogVisible.value = true
}

async function handlePersonSubmit() {
  if (!personFormRef.value) return
  await personFormRef.value.validate(async (valid) => {
    if (!valid) return
    submitLoading.value = true
    try {
      await savePerson(personForm)
      ElMessage.success(isEdit.value ? '修改人员档案成功' : '新增人员成功')
      editDialogVisible.value = false
      await fetchPeople()
    } catch (err: any) {
      ElMessage.error(err.response?.data?.error || err.message || '保存失败')
    } finally {
      submitLoading.value = false
    }
  })
}

async function togglePersonStatus(item: PersonItem) {
  const action = item.是否启用 ? '禁用' : '启用'
  try {
    await ElMessageBox.confirm(`确定要${action}人员【${item.姓名}】(工号: ${item.工号}) 吗？`, '提示', {
      type: item.是否启用 ? 'warning' : 'info'
    })
    if (item.是否启用) {
      await disablePerson(item.编号)
    } else {
      await enablePerson(item.编号)
    }
    ElMessage.success(`人员已${action}`)
    await fetchPeople()
  } catch {
    // canceled
  }
}

async function viewCapability(item: PersonItem) {
  currentPersonProfile.value = item
  capabilityProfile.value = null
  capabilityLoading.value = true
  capabilityDrawerVisible.value = true
  try {
    const profile = await getPersonCapability(item.编号)
    capabilityProfile.value = profile
    Object.assign(capabilityForm, {
      人员编号: item.编号,
      研究专长: profile?.科研能力?.研究专长 || '',
      研究方向标签: profile?.科研能力?.研究方向标签 || '',
      技术服务能力: profile?.科研能力?.技术服务能力 || '',
      合作意愿: profile?.科研能力?.合作意愿 || '可合作',
      合作说明: profile?.科研能力?.合作说明 || '',
      可用时间: profile?.科研能力?.可用时间 || '',
      每周可用小时: profile?.科研能力?.每周可用小时 || 0,
      可承担项目数: profile?.科研能力?.可承担项目数 ?? 3,
      当前可接项目: profile?.科研能力?.当前可接项目 !== false,
      可公开展示: profile?.科研能力?.可公开展示 !== false
    })
  } catch (err: any) {
    ElMessage.error(err?.response?.data?.error || '读取科研能力画像失败')
  } finally {
    capabilityLoading.value = false
  }
}

async function saveCapabilityForm() {
  if (!capabilityForm.人员编号) return
  capabilitySaving.value = true
  try {
    await savePersonCapability({ ...capabilityForm })
    ElMessage.success('科研能力画像已保存')
    const selected = currentPersonProfile.value || peopleList.value.find((p) => p.编号 === capabilityForm.人员编号)
    if (selected) await viewCapability(selected)
  } catch (err: any) {
    ElMessage.error(err?.response?.data?.error || '保存科研能力画像失败')
  } finally {
    capabilitySaving.value = false
  }
}

async function loadPartners() {
  partnerLoading.value = true
  try {
    const rows = await findResearchPartners({
      关键词: partnerQuery.关键词 || undefined,
      技术服务: partnerQuery.技术服务 || undefined,
      最低每周可用小时: partnerQuery.最低每周可用小时,
      数量: 100
    })
    partnerList.value = Array.isArray(rows) ? rows : []
  } catch {
    partnerList.value = []
  } finally {
    partnerLoading.value = false
  }
}

onMounted(async () => {
  await Promise.all([fetchDepartments(), fetchAuxData()])
  await fetchPeople()

  // 响应快捷路由指令参数
  if (route.query.action === 'add-user') {
    openCreateDialog()
  } else if (route.query.action === 'add-dept') {
    openDeptDialog(false)
  }
})
</script>

<style scoped lang="scss">
.people-page-container {
  padding: 16px;
  height: 100%;

  .full-height-row {
    height: 100%;
  }

  .tree-card {
    height: 100%;
    background-color: #ffffff;

    .tree-header {
      display: flex;
      justify-content: space-between;
      align-items: center;

      .header-title {
        font-weight: 600;
        font-size: 14px;
      }
    }
  }

  .dept-tree-node {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    padding-right: 8px;

    .node-label {
      font-size: 13px;
      &.disabled {
        color: #94a3b8;
        text-decoration: line-through;
      }
    }

    .node-actions {
      display: none;
    }

    &:hover .node-actions {
      display: inline-flex;
      gap: 4px;
    }
  }

  .table-card {
    height: 100%;
    background-color: #ffffff;

    .filter-toolbar {
      margin-bottom: 12px;
    }

    .user-name-highlight {
      font-weight: 500;
      color: #1e293b;
    }

    .pagination-wrapper {
      margin-top: 16px;
      display: flex;
      justify-content: flex-end;
    }
  }

  .person-full-form {
    padding: 8px 12px;
  }

  .drawer-footer {
    display: flex;
    justify-content: flex-end;
    gap: 12px;
  }

  .capability-box {
    .profile-header {
      display: flex;
      align-items: center;
    }

    .capability-metrics {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 12px;
      margin-top: 12px;

      .metric-item {
        background-color: #f8fafc;
        padding: 12px;
        border-radius: 6px;
        display: flex;
        flex-direction: column;

        .m-label {
          font-size: 12px;
          color: #64748b;
        }

        .m-val {
          font-size: 16px;
          font-weight: 600;
          color: #0f172a;
          margin-top: 4px;
        }
      }
    }

    .capability-form {
      padding: 0 4px;
    }

    .capability-actions {
      display: flex;
      justify-content: flex-end;
      margin-top: 4px;
    }

    .tag-list {
      min-height: 28px;
      margin-bottom: 12px;
    }
  }
}
</style>
