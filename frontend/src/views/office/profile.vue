<template>
  <div class="user-profile-container">
    <!-- 头部卡片 -->
    <div class="header-card">
      <div class="header-info">
        <h2 class="page-title">
          <el-icon class="title-icon"><UserFilled /></el-icon>
          个人科研中心与账户安全
        </h2>
        <p class="page-subtitle">
          维护全院科研人员个人基本档案、学术专业方向、代表性成果荣誉及基于统一身份认证的安全密码设置。
        </p>
      </div>
      <div class="header-actions">
        <el-button :icon="Refresh" @click="fetchProfile">刷新资料</el-button>
      </div>
    </div>

    <el-row :gutter="16" class="profile-content-row">
      <!-- 左栏：个人名片与能力画像 -->
      <el-col :xs="24" :sm="24" :md="8" :lg="7" class="left-col">
        <!-- 基础名片卡片 -->
        <el-card shadow="never" class="profile-card">
          <div class="avatar-section">
            <div class="user-avatar-circle">
              {{ (profile.姓名 || '研').slice(0, 1) }}
            </div>
            <h3 class="user-name">{{ profile.姓名 || '科研人员' }}</h3>
            <p class="user-emp-id">工号: {{ profile.工号 || '---' }}</p>
            <div class="user-badge-row">
              <el-tag size="small" type="success" effect="light">正常在岗</el-tag>
              <el-tag size="small" type="primary" effect="plain">Keycloak 统一认证</el-tag>
            </div>
          </div>

          <el-divider />

          <div class="profile-meta-list">
            <div class="meta-item">
              <el-icon class="meta-icon"><OfficeBuilding /></el-icon>
              <span class="meta-label">所属部门:</span>
              <span class="meta-value">{{ profile.部门名称 || '未分配科室' }}</span>
            </div>
            <div class="meta-item">
              <el-icon class="meta-icon"><Phone /></el-icon>
              <span class="meta-label">联系电话:</span>
              <span class="meta-value">{{ profile.手机 || '未填报' }}</span>
            </div>
            <div class="meta-item">
              <el-icon class="meta-icon"><Message /></el-icon>
              <span class="meta-label">电子邮箱:</span>
              <span class="meta-value">{{ profile.邮箱 || '未填报' }}</span>
            </div>
            <div class="meta-item">
              <el-icon class="meta-icon"><School /></el-icon>
              <span class="meta-label">最高学历院校:</span>
              <span class="meta-value">{{ profile.获得最高学历的院校 || '未填报' }}</span>
            </div>
          </div>
        </el-card>

        <!-- 综合科研指数卡片 -->
        <el-card shadow="never" class="capacity-card mt-4">
          <template #header>
            <div class="card-header-title">
              <el-icon><DataAnalysis /></el-icon>
              <span>科研能力量化指标</span>
            </div>
          </template>
          <div class="radar-score-list">
            <div class="radar-metric">
              <div class="metric-top">
                <span>论文发表能力</span>
                <span class="fw-bold">{{ capacity.论文能力得分 ?? '—' }}<template v-if="capacity.论文能力得分 != null"> 分</template></span>
              </div>
              <el-progress :percentage="capacity.论文能力得分 ?? 0" :show-text="false" status="success" />
            </div>
            <div class="radar-metric">
              <div class="metric-top">
                <span>课题主持能力</span>
                <span class="fw-bold">{{ capacity.课题主持得分 ?? '—' }}<template v-if="capacity.课题主持得分 != null"> 分</template></span>
              </div>
              <el-progress :percentage="capacity.课题主持得分 ?? 0" :show-text="false" />
            </div>
            <div class="radar-metric">
              <div class="metric-top">
                <span>经费规模体量</span>
                <span class="fw-bold">{{ capacity.经费规模得分 ?? '—' }}<template v-if="capacity.经费规模得分 != null"> 分</template></span>
              </div>
              <el-progress :percentage="capacity.经费规模得分 ?? 0" :show-text="false" status="warning" />
            </div>
            <div class="radar-metric">
              <div class="metric-top">
                <span>产学研成果转化</span>
                <span class="fw-bold">{{ capacity.成果转化得分 ?? '—' }}<template v-if="capacity.成果转化得分 != null"> 分</template></span>
              </div>
              <el-progress :percentage="capacity.成果转化得分 ?? 0" :show-text="false" />
            </div>
          </div>
        </el-card>
      </el-col>

      <!-- 右栏：资料维护与安全设置 Tabs -->
      <el-col :xs="24" :sm="24" :md="16" :lg="17" class="right-col">
        <el-card shadow="never" class="main-profile-tabs-card">
          <el-tabs v-model="activeTab" class="profile-tabs">
            <!-- Tab 1: 基本资料与联系方式 -->
            <el-tab-pane label="基本资料与联系方式" name="basic">
              <el-form
                ref="basicFormRef"
                :model="profileForm"
                label-width="110px"
                class="profile-edit-form"
              >
                <el-row :gutter="16">
                  <el-col :span="12">
                    <el-form-item label="人员工号">
                      <el-input :value="profile.工号" disabled />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="人员姓名">
                      <el-input :value="profile.姓名" disabled />
                    </el-form-item>
                  </el-col>
                </el-row>

                <el-row :gutter="16">
                  <el-col :span="12">
                    <el-form-item label="所属部门/科室">
                      <el-input :value="profile.部门名称" disabled />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="联系手机">
                      <el-input v-model="profileForm.手机" placeholder="如：13800000000" />
                    </el-form-item>
                  </el-col>
                </el-row>

                <el-row :gutter="16">
                  <el-col :span="12">
                    <el-form-item label="电子邮箱">
                      <el-input v-model="profileForm.邮箱" placeholder="如：researcher@hospital.org" />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="办公 QQ">
                      <el-input v-model="profileForm.QQ" placeholder="填写联系 QQ 号码" />
                    </el-form-item>
                  </el-col>
                </el-row>

                <el-row :gutter="16">
                  <el-col :span="12">
                    <el-form-item label="籍贯归属">
                      <el-input v-model="profileForm.籍贯" placeholder="如：北京市海淀区" />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="现居住地址">
                      <el-input v-model="profileForm.住宅地址" placeholder="常住地址..." />
                    </el-form-item>
                  </el-col>
                </el-row>

                <el-form-item class="form-actions mt-4">
                  <el-button type="primary" :loading="savingProfile" @click="handleSaveBasicProfile">
                    保存基本资料
                  </el-button>
                </el-form-item>
              </el-form>
            </el-tab-pane>

            <!-- Tab 2: 学术特长与科研画像 -->
            <el-tab-pane label="学术方向与科研特长" name="academic">
              <el-form
                ref="academicFormRef"
                :model="profileForm"
                label-width="110px"
                class="profile-edit-form"
              >
                <el-row :gutter="16">
                  <el-col :span="12">
                    <el-form-item label="最高学历院校">
                      <el-input v-model="profileForm.获得最高学历的院校" placeholder="如：协和医学院、北大医学部..." />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="最高学位院校">
                      <el-input v-model="profileForm.获得最高学位的院校" placeholder="如：清华大学医学院..." />
                    </el-form-item>
                  </el-col>
                </el-row>

                <el-form-item label="学术专长领域">
                  <el-input
                    v-model="profileForm.学术特长"
                    type="textarea"
                    :rows="3"
                    placeholder="如：单细胞转录组测序、恶性肿瘤耐药机制、心肌缺血再灌注损伤保护..."
                  />
                </el-form-item>

                <el-form-item label="核心研究方向">
                  <el-input
                    v-model="profileForm.研究方向"
                    type="textarea"
                    :rows="3"
                    placeholder="如：纳米靶向递药系统、临床队列生物样本库建设与多模态影像 AI 诊断..."
                  />
                </el-form-item>

                <el-form-item label="代表性荣誉奖项">
                  <el-input
                    v-model="profileForm.个人获得的荣誉和奖项"
                    type="textarea"
                    :rows="3"
                    placeholder="如：省科技进步一等奖（排名第二）、青年科技拔尖人才称号..."
                  />
                </el-form-item>

                <el-form-item class="form-actions mt-4">
                  <el-button type="primary" :loading="savingProfile" @click="handleSaveAcademicProfile">
                    保存学术特长
                  </el-button>
                </el-form-item>
              </el-form>
            </el-tab-pane>

            <!-- Tab 3: 账号安全与密码修改 -->
            <el-tab-pane label="账号安全与密码设置" name="security">
              <div class="security-intro-alert">
                <el-alert
                  title="安全策略提示：当前科研管理系统已深度集成医院 Keycloak 统一身份认证平台。如需修改登录口令，请按医院安全基线规范设置强密码。"
                  type="info"
                  show-icon
                  :closable="false"
                />
              </div>

              <el-form
                ref="passwordFormRef"
                :model="passwordForm"
                :rules="passwordRules"
                label-width="120px"
                class="security-password-form"
              >
                <el-form-item label="当前原密码" prop="旧密码">
                  <el-input
                    v-model="passwordForm.旧密码"
                    type="password"
                    show-password
                    placeholder="输入当前使用的登录密码"
                    style="max-width: 320px"
                  />
                </el-form-item>
                <el-form-item label="设置新密码" prop="新密码">
                  <el-input
                    v-model="passwordForm.新密码"
                    type="password"
                    show-password
                    placeholder="须包含大小写字母与数字，至少 8 位"
                    style="max-width: 320px"
                  />
                </el-form-item>
                <el-form-item label="确认新密码" prop="确认密码">
                  <el-input
                    v-model="passwordForm.确认密码"
                    type="password"
                    show-password
                    placeholder="再次输入新密码以确认"
                    style="max-width: 320px"
                  />
                </el-form-item>

                <el-form-item class="form-actions mt-4">
                  <el-button type="warning" :loading="changingPassword" @click="handleChangePassword">
                    确认修改登录密码
                  </el-button>
                </el-form-item>
              </el-form>
            </el-tab-pane>
          </el-tabs>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, type FormInstance } from 'element-plus'
import {
  UserFilled,
  Refresh,
  OfficeBuilding,
  Phone,
  Message,
  School,
  DataAnalysis
} from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'
import {
  getCurrentUser,
  getPersonDetail,
  getPersonCapacityRadar,
  updatePersonProfile,
  changeUserPassword,
  type UserProfileDetail,
  type CapacityRadarData
} from '@/api/profile'

const route = useRoute()
const userStore = useUserStore()

const activeTab = ref('basic')
const savingProfile = ref(false)
const changingPassword = ref(false)

const profile = ref<UserProfileDetail>({
  编号: 0,
  工号: '',
  姓名: '',
  部门名称: ''
})

const capacity = ref<CapacityRadarData>({ 人员编号: 0 })

const profileForm = reactive<Partial<UserProfileDetail>>({
  手机: '',
  邮箱: '',
  QQ: '',
  籍贯: '',
  住宅地址: '',
  获得最高学历的院校: '',
  获得最高学位的院校: '',
  学术特长: '',
  研究方向: '',
  个人获得的荣誉和奖项: ''
})

// 密码表单
const passwordFormRef = ref<FormInstance>()
const passwordForm = reactive({
  旧密码: '',
  新密码: '',
  确认密码: ''
})

const passwordRules = {
  旧密码: [{ required: true, message: '请输入当前密码', trigger: 'blur' }],
  新密码: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码长度至少为 6 位', trigger: 'blur' }
  ],
  确认密码: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    {
      validator: (_rule: any, value: string, callback: any) => {
        if (value !== passwordForm.新密码) {
          callback(new Error('两次输入的密码不一致'))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
  ]
}

async function fetchProfile() {
  try {
    const user = await getCurrentUser()
    if (user?.编号) {
      profile.value.编号 = user.编号
      profile.value.工号 = user.工号
      profile.value.姓名 = user.姓名
      profile.value.部门名称 = user.部门名称

      // 获取详情
      const detail = await getPersonDetail(user.编号)
      if (detail) {
        profile.value = { ...profile.value, ...detail }
        Object.assign(profileForm, {
          手机: detail.手机 || '',
          邮箱: detail.邮箱 || '',
          QQ: detail.QQ || '',
          籍贯: detail.籍贯 || '',
          住宅地址: detail.住宅地址 || '',
          获得最高学历的院校: detail.获得最高学历的院校 || '',
          获得最高学位的院校: detail.获得最高学位的院校 || '',
          学术特长: detail.学术特长 || '',
          研究方向: detail.研究方向 || '',
          个人获得的荣誉和奖项: detail.个人获得的荣誉和奖项 || ''
        })
      }

      // 获取科研能力画像
      const cap = await getPersonCapacityRadar(user.编号)
      if (cap) {
        capacity.value = cap
      }
    }
  } catch (error) {
    console.warn('获取个人资料失败，使用当前会话缓存数据:', error)
    profile.value = {
      编号: userStore.userInfo?.编号 || 0,
      工号: userStore.userInfo?.工号 || '',
      姓名: userStore.userInfo?.姓名 || '',
      部门名称: userStore.userInfo?.部门名称 || ''
    }
    Object.assign(profileForm, profile.value)
    capacity.value = { 人员编号: profile.value.编号 || 0 }
  }
}

async function handleSaveBasicProfile() {
  savingProfile.value = true
  try {
    await updatePersonProfile({
      编号: profile.value.编号,
      手机: profileForm.手机,
      邮箱: profileForm.邮箱,
      QQ: profileForm.QQ,
      籍贯: profileForm.籍贯,
      住宅地址: profileForm.住宅地址
    })
    ElMessage.success('基本联系资料已成功更新！')
    fetchProfile()
  } catch (error: any) {
    ElMessage.error(error?.message || '保存基本资料失败')
  } finally {
    savingProfile.value = false
  }
}

async function handleSaveAcademicProfile() {
  savingProfile.value = true
  try {
    await updatePersonProfile({
      编号: profile.value.编号,
      获得最高学历的院校: profileForm.获得最高学历的院校,
      获得最高学位的院校: profileForm.获得最高学位的院校,
      学术特长: profileForm.学术特长,
      研究方向: profileForm.研究方向,
      个人获得的荣誉和奖项: profileForm.个人获得的荣誉和奖项
    })
    ElMessage.success('学术特长与科研画像已成功保存！')
    fetchProfile()
  } catch (error: any) {
    ElMessage.error(error?.message || '保存学术画像失败')
  } finally {
    savingProfile.value = false
  }
}

async function handleChangePassword() {
  await passwordFormRef.value?.validate(async (valid) => {
    if (!valid) return
    changingPassword.value = true
    try {
      await changeUserPassword({
        旧密码: passwordForm.旧密码,
        新密码: passwordForm.新密码
      })
      ElMessage.success('登录密码已成功更新！')
      passwordForm.旧密码 = ''
      passwordForm.新密码 = ''
      passwordForm.确认密码 = ''
    } catch (error: any) {
      ElMessage.error(error?.message || '修改密码失败，请核对原密码')
    } finally {
      changingPassword.value = false
    }
  })
}

watch(
  () => route.query.tab,
  (newTab) => {
    if (newTab === 'security') {
      activeTab.value = 'security'
    } else if (newTab === 'academic') {
      activeTab.value = 'academic'
    } else {
      activeTab.value = 'basic'
    }
  }
)

onMounted(() => {
  if (route.query.tab === 'security') {
    activeTab.value = 'security'
  } else if (route.query.tab === 'academic') {
    activeTab.value = 'academic'
  }
  fetchProfile()
})
</script>

<style scoped>
.user-profile-container {
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

.profile-card {
  background: #fff;
}

.avatar-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 12px 0 6px;
}

.user-avatar-circle {
  width: 72px;
  height: 72px;
  border-radius: 50%;
  background: linear-gradient(135deg, #409eff 0%, #67c23a 100%);
  color: #fff;
  font-size: 32px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 12px rgba(64, 158, 255, 0.3);
  margin-bottom: 12px;
}

.user-name {
  margin: 0 0 4px 0;
  font-size: 18px;
  font-weight: 600;
  color: #303133;
}

.user-emp-id {
  margin: 0 0 10px 0;
  font-size: 13px;
  color: #909399;
}

.user-badge-row {
  display: flex;
  gap: 8px;
}

.profile-meta-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.meta-item {
  display: flex;
  align-items: center;
  font-size: 13px;
  gap: 8px;
}

.meta-icon {
  color: #909399;
  font-size: 15px;
}

.meta-label {
  color: #909399;
  width: 95px;
}

.meta-value {
  color: #303133;
  font-weight: 500;
}

.capacity-card {
  background: #fff;
}

.card-header-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 14px;
  color: #303133;
}

.radar-score-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.radar-metric {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.metric-top {
  display: flex;
  justify-content: space-between;
  font-size: 13px;
  color: #606266;
}

.fw-bold {
  font-weight: 600;
  color: #303133;
}

.main-profile-tabs-card {
  background: #fff;
  min-height: 100%;
}

.profile-edit-form,
.security-password-form {
  padding: 12px 16px;
}

.security-intro-alert {
  margin-bottom: 18px;
}

.form-actions {
  display: flex;
  justify-content: flex-start;
}

.mt-4 {
  margin-top: 16px;
}
</style>
