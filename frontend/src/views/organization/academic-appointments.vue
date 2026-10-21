<template>
  <div class="academic-appointments-container">
    <!-- 头部区域 -->
    <div class="header-card">
      <div class="header-info">
        <h2 class="page-title">
          <el-icon class="title-icon"><UserFilled /></el-icon>
          学术任职与专家人才中心
        </h2>
        <p class="page-subtitle">
          全院教职工学术任职全生命周期留痕、院内外专家人才库评定遴选、跨学科科研合作画像与多维推荐治理。
        </p>
      </div>
      <div class="header-actions">
        <el-button type="primary" :icon="Plus" @click="openAppointmentDialog">
          申报学术任职
        </el-button>
        <el-button type="success" :icon="User" @click="openNominateDialog">
          遴选入库专家
        </el-button>
        <el-button :icon="Refresh" @click="refreshActiveTab">刷新</el-button>
      </div>
    </div>

    <!-- 关键指标卡片 -->
    <el-row :gutter="16" class="stats-row">
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap blue"><el-icon><Medal /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">全院学术任职</span>
            <span class="stat-value">{{ appointmentTotal }} <small>项</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap green"><el-icon><Avatar /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">入库评审专家</span>
            <span class="stat-value text-green">{{ expertTotal }} <small>位</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap orange"><el-icon><Reading /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">我的任职申报</span>
            <span class="stat-value text-orange">{{ myAppointmentTotal }} <small>项</small></span>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-icon-wrap purple"><el-icon><Opportunity /></el-icon></div>
          <div class="stat-content">
            <span class="stat-label">开放合作专家</span>
            <span class="stat-value text-purple">{{ partnerList.length }} <small>位</small></span>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 主选项卡 -->
    <el-card shadow="never" class="main-card">
      <el-tabs v-model="activeTab" class="custom-tabs" @tab-change="onTabChange">
        <!-- Tab 1: 全员学术任职 -->
        <el-tab-pane label="全员学术任职台账" name="appointments">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-input
                v-model="appointmentFilters.Like姓名"
                placeholder="搜索任职人员姓名..."
                :prefix-icon="Search"
                clearable
                style="width: 200px"
                @keyup.enter="fetchAppointments"
              />
              <el-input
                v-model="appointmentFilters.Like任职团体名称"
                placeholder="搜索任职学会/协会/团体..."
                :prefix-icon="Search"
                clearable
                style="width: 240px"
                @keyup.enter="fetchAppointments"
              />
              <el-input
                v-model="appointmentFilters.Like所任职务"
                placeholder="搜索所任职务..."
                :prefix-icon="Search"
                clearable
                style="width: 180px"
                @keyup.enter="fetchAppointments"
              />
              <el-button type="primary" :icon="Search" @click="fetchAppointments">查询</el-button>
              <el-button :icon="Refresh" @click="resetAppointmentFilters">重置</el-button>
            </div>
          </div>

          <el-table
            v-loading="loadingAppointments"
            :data="appointmentList"
            stripe
            border
            style="width: 100%"
          >
            <el-table-column prop="编号" label="编号" width="80" align="center" />
            <el-table-column prop="姓名" label="人员姓名" width="120">
              <template #default="{ row }">
                <span class="font-medium">{{ row.姓名 || '-' }}</span>
                <div v-if="row.工号" class="text-muted text-xs">工号: {{ row.工号 }}</div>
              </template>
            </el-table-column>
            <el-table-column prop="部门名称" label="所属科室/部门" width="160" show-overflow-tooltip />
            <el-table-column prop="任职团体名称" label="任职学会 / 团体名称" min-width="200" show-overflow-tooltip />
            <el-table-column prop="所任职务" label="所任职务" width="140">
              <template #default="{ row }">
                <el-tag size="small" effect="plain">{{ row.所任职务 || '-' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="任职区间" width="220" align="center">
              <template #default="{ row }">
                <span>{{ formatDate(row.任职开始时间) }} ~ {{ formatDate(row.任职结束时间) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="聘书证明" width="110" align="center">
              <template #default="{ row }">
                <el-link
                  v-if="row.聘书路径"
                  type="primary"
                  :href="getFileUrl(row.聘书路径)"
                  target="_blank"
                  :underline="false"
                >
                  <el-icon><Document /></el-icon> 查看聘书
                </el-link>
                <span v-else class="text-muted">-</span>
              </template>
            </el-table-column>
            <el-table-column prop="备注" label="备注" min-width="150" show-overflow-tooltip />
            <el-table-column label="操作" width="110" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="viewAppointmentDetail(row.编号)">
                  详情与流转
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-footer">
            <el-pagination
              v-model:current-page="appointmentPaging.Index"
              v-model:page-size="appointmentPaging.Size"
              :page-sizes="[10, 15, 20, 50]"
              :total="appointmentTotal"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="fetchAppointments"
              @current-change="fetchAppointments"
            />
          </div>
        </el-tab-pane>

        <!-- Tab 2: 我的学术任职 -->
        <el-tab-pane label="我的任职申报" name="my_appointments">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-input
                v-model="myAppointmentFilters.Like任职团体名称"
                placeholder="搜索学会或学术团体..."
                :prefix-icon="Search"
                clearable
                style="width: 240px"
                @keyup.enter="fetchMyAppointments"
              />
              <el-input
                v-model="myAppointmentFilters.Like所任职务"
                placeholder="搜索所任职务..."
                :prefix-icon="Search"
                clearable
                style="width: 180px"
                @keyup.enter="fetchMyAppointments"
              />
              <el-button type="primary" :icon="Search" @click="fetchMyAppointments">查询</el-button>
            </div>
            <div class="toolbar-right">
              <el-button type="primary" :icon="Plus" @click="openAppointmentDialog">
                申请新任职
              </el-button>
            </div>
          </div>

          <el-table
            v-loading="loadingMyAppointments"
            :data="myAppointmentList"
            stripe
            border
            style="width: 100%"
          >
            <el-table-column prop="编号" label="编号" width="80" align="center" />
            <el-table-column prop="任职团体名称" label="任职学术团体" min-width="220" show-overflow-tooltip />
            <el-table-column prop="所任职务" label="所任职务" width="150">
              <template #default="{ row }">
                <el-tag type="success" size="small">{{ row.所任职务 || '-' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="任职周期" width="220" align="center">
              <template #default="{ row }">
                <span>{{ formatDate(row.任职开始时间) }} ~ {{ formatDate(row.任职结束时间) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="聘书材料" width="120" align="center">
              <template #default="{ row }">
                <el-link
                  v-if="row.聘书路径"
                  type="primary"
                  :href="getFileUrl(row.聘书路径)"
                  target="_blank"
                  :underline="false"
                >
                  <el-icon><Document /></el-icon> 查看聘书
                </el-link>
                <span v-else class="text-muted">未上传</span>
              </template>
            </el-table-column>
            <el-table-column prop="备注" label="备注说明" min-width="160" show-overflow-tooltip />
            <el-table-column label="操作" width="120" fixed="right" align="center">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="viewAppointmentDetail(row.编号)">
                  查看进度
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-footer">
            <el-pagination
              v-model:current-page="myAppointmentPaging.Index"
              v-model:page-size="myAppointmentPaging.Size"
              :page-sizes="[10, 15, 20, 50]"
              :total="myAppointmentTotal"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="fetchMyAppointments"
              @current-change="fetchMyAppointments"
            />
          </div>
        </el-tab-pane>

        <!-- Tab 3: 专家人才库 -->
        <el-tab-pane label="专家人才库" name="experts">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-input
                v-model="expertFilters.Like姓名"
                placeholder="搜索专家姓名..."
                :prefix-icon="Search"
                clearable
                style="width: 180px"
                @keyup.enter="fetchExperts"
              />
              <el-input
                v-model="expertFilters.Like工号"
                placeholder="搜索专 plague 工号..."
                :prefix-icon="Search"
                clearable
                style="width: 160px"
                @keyup.enter="fetchExperts"
              />
              <el-input
                v-model="expertFilters.Like技术职称"
                placeholder="搜索技术职称..."
                :prefix-icon="Search"
                clearable
                style="width: 160px"
                @keyup.enter="fetchExperts"
              />
              <el-button type="primary" :icon="Search" @click="fetchExperts">查询</el-button>
              <el-button :icon="Refresh" @click="resetExpertFilters">重置</el-button>
            </div>
            <div class="toolbar-right">
              <el-button
                type="danger"
                plain
                :icon="Delete"
                :disabled="!selectedExpertIds.length"
                @click="batchRemoveExperts"
              >
                移出专家库 ({{ selectedExpertIds.length }})
              </el-button>
              <el-button type="primary" :icon="Plus" @click="openNominateDialog">
                遴选新增专家
              </el-button>
            </div>
          </div>

          <el-table
            v-loading="loadingExperts"
            :data="expertList"
            stripe
            border
            style="width: 100%"
            @selection-change="onExpertSelectionChange"
          >
            <el-table-column type="selection" width="50" align="center" />
            <el-table-column prop="编号" label="编号" width="80" align="center" />
            <el-table-column prop="姓名" label="专家姓名" width="130">
              <template #default="{ row }">
                <span class="font-medium">{{ row.姓名 }}</span>
                <el-tag size="small" type="warning" effect="dark" style="margin-left: 6px">专家</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="工号" label="工号" width="120" />
            <el-table-column prop="部门名称" label="所属科室/部门" width="160" show-overflow-tooltip />
            <el-table-column prop="技术职称" label="技术职称" width="130">
              <template #default="{ row }">
                <el-tag v-if="row.技术职称" size="small">{{ row.技术职称 }}</el-tag>
                <span v-else class="text-muted">-</span>
              </template>
            </el-table-column>
            <el-table-column prop="职务" label="行政/业务职务" width="140" show-overflow-tooltip />
            <el-table-column prop="电话" label="联系电话" width="130">
              <template #default="{ row }">{{ row.电话 || '-' }}</template>
            </el-table-column>
            <el-table-column prop="邮箱" label="电子邮箱" min-width="160" show-overflow-tooltip>
              <template #default="{ row }">{{ row.邮箱 || '-' }}</template>
            </el-table-column>
            <el-table-column label="状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="row.是否启用 !== false ? 'success' : 'danger'" size="small">
                  {{ row.是否启用 !== false ? '在库有效' : '已停用' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="120" fixed="right" align="center">
              <template #default="{ row }">
                <el-popconfirm
                  title="确认将该人员移出专家人才库？"
                  confirm-button-text="移出"
                  cancel-button-text="取消"
                  confirm-button-type="danger"
                  @confirm="removeSingleExpert(row.编号)"
                >
                  <template #reference>
                    <el-button link type="danger" size="small">移出专家库</el-button>
                  </template>
                </el-popconfirm>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination-footer">
            <el-pagination
              v-model:current-page="expertPaging.Index"
              v-model:page-size="expertPaging.Size"
              :page-sizes="[10, 15, 20, 50]"
              :total="expertTotal"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="fetchExperts"
              @current-change="fetchExperts"
            />
          </div>
        </el-tab-pane>

        <!-- Tab 4: 专家可用时间与外部邀请治理 -->
        <el-tab-pane label="专家可用时间 / 外部邀请" name="expert-governance">
          <el-alert
            title="邀请只登记本地队列，不会直接发送外部邮件或身份邀请"
            type="warning"
            :closable="false"
            show-icon
            description="未配置真实邮件/身份适配器时状态为“未配置”；只有外部适配器取得真实回执后，管理员才能登记“已发送”。"
            style="margin-bottom: 16px"
          />
          <el-row :gutter="16">
            <el-col :xs="24" :lg="11">
              <el-card shadow="never" class="governance-card">
                <template #header>
                  <div class="card-header-line">
                    <span>专家每周可用时间</span>
                    <el-button size="small" :icon="Refresh" @click="fetchExpertGovernance">刷新</el-button>
                  </div>
                </template>
                <el-form label-width="96px" size="small" @submit.prevent>
                  <el-form-item label="专家">
                    <el-select v-model="governanceExpertId" filterable style="width: 100%" @change="fetchExpertAvailability">
                      <el-option v-for="item in expertList" :key="item.编号" :label="`${item.姓名}（${item.工号}）`" :value="item.编号" />
                    </el-select>
                  </el-form-item>
                  <el-form-item label="星期">
                    <el-select v-model="availabilityForm.星期" style="width: 100%">
                      <el-option v-for="item in weekOptions" :key="item.value" :label="item.label" :value="item.value" />
                    </el-select>
                  </el-form-item>
                  <el-form-item label="可用时段">
                    <el-time-picker
                      v-model="availabilityTimeRange"
                      is-range
                      format="HH:mm"
                      value-format="HH:mm:ss"
                      range-separator="至"
                      start-placeholder="开始时间"
                      end-placeholder="结束时间"
                      style="width: 100%"
                    />
                  </el-form-item>
                  <el-form-item label="时区">
                    <el-input v-model="availabilityForm.时区" maxlength="64" />
                  </el-form-item>
                  <el-form-item label="备注">
                    <el-input v-model="availabilityForm.备注" maxlength="500" />
                  </el-form-item>
                  <el-button type="primary" :loading="savingAvailability" :disabled="!governanceExpertId" @click="saveAvailability">
                    保存可用时间
                  </el-button>
                </el-form>
                <el-table v-loading="loadingAvailability" :data="availabilityList" size="small" stripe border style="margin-top: 16px">
                  <el-table-column prop="星期" label="星期" width="60">
                    <template #default="{ row }">{{ weekLabel(row.星期) }}</template>
                  </el-table-column>
                  <el-table-column label="时间段" min-width="140">
                    <template #default="{ row }">{{ formatTime(row.开始时间) }} - {{ formatTime(row.结束时间) }}</template>
                  </el-table-column>
                  <el-table-column prop="时区" label="时区" width="125" />
                  <el-table-column label="状态" width="80">
                    <template #default="{ row }">
                      <el-tag size="small" :type="row.是否启用 ? 'success' : 'info'">{{ row.是否启用 ? '启用' : '停用' }}</el-tag>
                    </template>
                  </el-table-column>
                  <el-table-column prop="备注" label="备注" show-overflow-tooltip />
                </el-table>
                <el-empty v-if="!availabilityList.length && !loadingAvailability" description="尚未登记可用时间" :image-size="64" />
              </el-card>
            </el-col>
            <el-col :xs="24" :lg="13">
              <el-card shadow="never" class="governance-card">
                <template #header>
                  <div class="card-header-line">
                    <span>外部专家邀请队列</span>
                    <el-button size="small" :icon="Refresh" @click="fetchInvitations">刷新</el-button>
                  </div>
                </template>
                <el-form :model="invitationForm" label-width="92px" size="small" @submit.prevent>
                  <el-row :gutter="8">
                    <el-col :span="12"><el-form-item label="外部姓名"><el-input v-model="invitationForm.外部姓名" maxlength="100" /></el-form-item></el-col>
                    <el-col :span="12"><el-form-item label="外部邮箱"><el-input v-model="invitationForm.外部邮箱" maxlength="320" /></el-form-item></el-col>
                    <el-col :span="12"><el-form-item label="机构"><el-input v-model="invitationForm.外部机构" maxlength="200" /></el-form-item></el-col>
                    <el-col :span="12"><el-form-item label="邀请场景"><el-input v-model="invitationForm.邀请场景" maxlength="100" placeholder="如：项目外部评审" /></el-form-item></el-col>
                    <el-col :span="24"><el-form-item label="幂等键"><el-input v-model="invitationForm.幂等键" maxlength="200" placeholder="每次业务邀请使用稳定且唯一的键" /></el-form-item></el-col>
                  </el-row>
                  <el-button type="primary" :loading="creatingInvitation" @click="createInvitation">登记邀请（不发送）</el-button>
                </el-form>
                <el-table v-loading="loadingInvitations" :data="invitationList" size="small" stripe border style="margin-top: 16px">
                  <el-table-column prop="外部姓名" label="外部专家" width="100" />
                  <el-table-column prop="外部邮箱" label="邮箱" min-width="170" show-overflow-tooltip />
                  <el-table-column prop="邀请场景" label="场景" min-width="120" show-overflow-tooltip />
                  <el-table-column label="状态" width="95">
                    <template #default="{ row }">
                      <el-tag size="small" :type="invitationStatusType(row.状态)">{{ row.状态 || '未知' }}</el-tag>
                    </template>
                  </el-table-column>
                  <el-table-column label="发送说明" min-width="200" show-overflow-tooltip>
                    <template #default="{ row }">{{ row.响应摘要 || row.失败原因 || (row.状态 === '未配置' ? '需配置真实服务并取得回执' : '—') }}</template>
                  </el-table-column>
                </el-table>
                <el-empty v-if="!invitationList.length && !loadingInvitations" description="暂无外部专家邀请记录" :image-size="64" />
              </el-card>
            </el-col>
          </el-row>
        </el-tab-pane>

        <!-- Tab 5: 专家遴选与科研合作画像 -->
        <el-tab-pane label="专家遴选与科研合作画像" name="selection">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-input
                v-model="partnerFilters.关键词"
                placeholder="姓名、工号、研究专长或方向..."
                :prefix-icon="Search"
                clearable
                style="width: 250px"
                @keyup.enter="searchPartners"
              />
              <el-input
                v-model="partnerFilters.技术服务"
                placeholder="搜索技术服务能力..."
                :prefix-icon="Search"
                clearable
                style="width: 200px"
                @keyup.enter="searchPartners"
              />
              <el-select
                v-model="partnerFilters.合作意愿"
                placeholder="合作意愿"
                clearable
                style="width: 140px"
              >
                <el-option label="全部合作意愿" value="" />
                <el-option label="可合作" value="可合作" />
                <el-option label="需邀约" value="需邀约" />
                <el-option label="暂不合作" value="暂不合作" />
              </el-select>
              <el-button type="primary" :icon="Search" @click="searchPartners">匹配画像</el-button>
              <el-button :icon="Refresh" @click="resetPartnerFilters">重置</el-button>
            </div>
          </div>

          <div v-loading="loadingPartners" class="partners-grid-wrapper">
            <el-empty
              v-if="!partnerList.length && !loadingPartners"
              description="暂未找到符合条件的科研人员能力画像"
            />
            <el-row v-else :gutter="16">
              <el-col
                v-for="item in partnerList"
                :key="item.编号"
                :xs="24"
                :sm="12"
                :md="8"
                class="partner-col"
              >
                <el-card shadow="hover" class="partner-card">
                  <div class="card-head">
                    <div class="user-meta">
                      <span class="user-name">{{ item.姓名 }}</span>
                      <span class="dept-title">{{ item.部门名称 || '-' }} / {{ item.技术职称 || '技术骨干' }}</span>
                    </div>
                    <el-tag
                      :type="item.是否专家 ? 'warning' : 'info'"
                      effect="dark"
                      size="small"
                    >
                      {{ item.是否专家 ? '已入库专家' : '候选骨干' }}
                    </el-tag>
                  </div>

                  <div class="card-body-section">
                    <div class="attr-row">
                      <span class="attr-label">研究专长:</span>
                      <span class="attr-val">{{ item.研究专长 || '未登记专长领域' }}</span>
                    </div>
                    <div class="attr-row">
                      <span class="attr-label">方向标签:</span>
                      <div class="tags-box">
                        <template v-if="item.研究方向标签">
                          <el-tag
                            v-for="(tag, idx) in splitTags(item.研究方向标签)"
                            :key="idx"
                            size="small"
                            effect="plain"
                            class="mr-1 mb-1"
                          >
                            {{ tag }}
                          </el-tag>
                        </template>
                        <span v-else class="text-muted text-xs">无标签</span>
                      </div>
                    </div>
                    <div class="attr-row">
                      <span class="attr-label">技术服务:</span>
                      <span class="attr-val">{{ item.技术服务能力 || '未对外开放专有技术' }}</span>
                    </div>
                    <div class="attr-row">
                      <span class="attr-label">合作意愿:</span>
                      <el-tag
                        size="small"
                        :type="item.合作意愿 === '可合作' ? 'success' : item.合作意愿 === '需邀约' ? 'warning' : 'info'"
                      >
                        {{ item.合作意愿 || '待意向沟通' }}
                      </el-tag>
                    </div>
                  </div>

                  <div class="card-footer-actions">
                    <el-button
                      v-if="!item.是否专家"
                      type="primary"
                      size="small"
                      :icon="Plus"
                      @click="nominateDirectly(item.编号, item.姓名)"
                    >
                      纳入专家库
                    </el-button>
                    <span v-else class="text-muted text-xs">已是评审专家</span>
                  </div>
                </el-card>
              </el-col>
            </el-row>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 申报/新增学术任职弹窗 -->
    <el-dialog
      v-model="showAppointmentDialog"
      title="申报/登记学术任职"
      width="600px"
      destroy-on-close
    >
      <el-form
        ref="appointmentFormRef"
        :model="appointmentForm"
        :rules="appointmentRules"
        label-width="120px"
      >
        <el-form-item label="任职学术团体" prop="任职团体名称">
          <el-input
            v-model="appointmentForm.任职团体名称"
            placeholder="例如: 中华医学会放射医学分会、中国药学会..."
          />
        </el-form-item>
        <el-form-item label="所任学术职务" prop="所任职务">
          <el-input
            v-model="appointmentForm.所任职务"
            placeholder="例如: 主任委员、副主任委员、常务委员、理事..."
          />
        </el-form-item>
        <el-form-item label="任职起止周期" required>
          <el-date-picker
            v-model="appointmentDateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            value-format="YYYY-MM-DD"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="聘书证明附件">
          <el-input
            v-model="appointmentForm.聘书路径"
            placeholder="聘书相对路径或文件 URL (如 upload/组织架构/学术任职/...)"
          />
        </el-form-item>
        <el-form-item label="备注说明">
          <el-input
            v-model="appointmentForm.备注"
            type="textarea"
            :rows="3"
            placeholder="输入任期补充信息、学会分级、证书编号或任职职责说明..."
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showAppointmentDialog = false">取消</el-button>
        <el-button type="primary" :loading="submittingAppointment" @click="submitAppointment">
          提交申报
        </el-button>
      </template>
    </el-dialog>

    <!-- 专家候选人员遴选入库弹窗 -->
    <el-dialog
      v-model="showNominateDialog"
      title="从全院科研人员中遴选入库专家"
      width="780px"
      destroy-on-close
    >
      <div class="nominate-toolbar">
        <el-input
          v-model="nonExpertFilters.Like姓名"
          placeholder="按姓名过滤候选人..."
          :prefix-icon="Search"
          clearable
          style="width: 220px"
          @keyup.enter="fetchNonExperts"
        />
        <el-button type="primary" :icon="Search" @click="fetchNonExperts">搜索候选人</el-button>
      </div>

      <el-table
        v-loading="loadingNonExperts"
        :data="nonExpertList"
        stripe
        border
        style="width: 100%; margin-top: 12px"
        max-height="400"
        @selection-change="onNonExpertSelectionChange"
      >
        <el-table-column type="selection" width="50" align="center" />
        <el-table-column prop="编号" label="编号" width="70" align="center" />
        <el-table-column prop="姓名" label="人员姓名" width="110" />
        <el-table-column prop="工号" label="工号" width="110" />
        <el-table-column prop="部门名称" label="所属科室" min-width="140" show-overflow-tooltip />
        <el-table-column prop="技术职称" label="技术职称" width="120" />
        <el-table-column prop="学历" label="学历" width="90" />
      </el-table>

      <div class="pagination-footer" style="padding: 10px 0 0">
        <el-pagination
          v-model:current-page="nonExpertPaging.Index"
          v-model:page-size="nonExpertPaging.Size"
          :page-sizes="[10, 20, 50]"
          :total="nonExpertTotal"
          layout="total, prev, pager, next"
          @size-change="fetchNonExperts"
          @current-change="fetchNonExperts"
        />
      </div>

      <template #footer>
        <span class="selected-hint" style="margin-right: 16px; color: #666">
          已选中 <strong>{{ selectedNominateIds.length }}</strong> 位专家候选人
        </span>
        <el-button @click="showNominateDialog = false">取消</el-button>
        <el-button
          type="primary"
          :disabled="!selectedNominateIds.length"
          :loading="submittingNominate"
          @click="submitNominate"
        >
          确认纳入专家库
        </el-button>
      </template>
    </el-dialog>

    <!-- 任职详情与流转审核抽屉 -->
    <el-drawer
      v-model="showDetailDrawer"
      title="学术任职全景档案与流转记录"
      size="560px"
      destroy-on-close
    >
      <div v-loading="loadingDetail" class="detail-drawer-content">
        <template v-if="detailItem">
          <div class="detail-section">
            <h4 class="section-title">基本任职档案</h4>
            <el-descriptions :column="1" border size="small">
              <el-descriptions-item label="任职团体名称">
                <span class="font-bold">{{ detailItem.任职团体名称 }}</span>
              </el-descriptions-item>
              <el-descriptions-item label="所任职务">
                <el-tag size="small">{{ detailItem.所任职务 }}</el-tag>
              </el-descriptions-item>
              <el-descriptions-item label="任职人员">
                {{ detailItem.姓名 || '-' }} (工号: {{ detailItem.工号 || '-' }})
              </el-descriptions-item>
              <el-descriptions-item label="所属科室">
                {{ detailItem.部门名称 || '-' }}
              </el-descriptions-item>
              <el-descriptions-item label="任职周期">
                {{ formatDate(detailItem.任职开始时间) }} 至 {{ formatDate(detailItem.任职结束时间) }}
              </el-descriptions-item>
              <el-descriptions-item label="聘书材料">
                <el-link
                  v-if="detailItem.聘书路径"
                  type="primary"
                  :href="getFileUrl(detailItem.聘书路径)"
                  target="_blank"
                >
                  <el-icon><Document /></el-icon> 下载聘书原件
                </el-link>
                <span v-else class="text-muted">未上传</span>
              </el-descriptions-item>
              <el-descriptions-item label="备注说明">
                {{ detailItem.备注 || '无特别说明' }}
              </el-descriptions-item>
            </el-descriptions>
          </div>

          <div class="detail-section" style="margin-top: 24px">
            <h4 class="section-title">流程审批与流转留痕</h4>
            <div v-if="!auditSteps.length" class="text-muted text-sm">
              暂无流转历史步骤或已自动归档
            </div>
            <el-timeline v-else style="margin-top: 16px">
              <el-timeline-item
                v-for="(step, idx) in auditSteps"
                :key="idx"
                :timestamp="formatDate(step.处理时间 || step.建立时间)"
                type="primary"
              >
                <div class="step-title font-medium">
                  {{ step.步骤名称 || '节点审批' }} -
                  <el-tag size="small" :type="step.处理结果 === '同意' ? 'success' : 'info'">
                    {{ step.处理结果 || '进行中' }}
                  </el-tag>
                </div>
                <div v-if="step.处理人姓名" class="step-actor text-xs text-muted" style="margin-top: 4px">
                  审核人: {{ step.处理人姓名 }} ({{ step.处理意见 || '审核通过' }})
                </div>
              </el-timeline-item>
            </el-timeline>
          </div>
        </template>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import {
  UserFilled,
  Plus,
  Refresh,
  Search,
  Delete,
  User,
  Document,
  Medal,
  Avatar,
  Reading,
  Opportunity
} from '@element-plus/icons-vue'
import {
  getPagingAcademicAppointments,
  getMyPagingAcademicAppointments,
  getAcademicAppointmentDetail,
  addAcademicAppointment,
  getPagingExperts,
  getPagingNonExperts,
  addExpertsFromPersonnel,
  deleteExperts,
  searchResearchPartners,
  type AcademicAppointmentItem,
  type ExpertItem,
  type ResearchPartner
} from '@/api/academic-appointment'
import {
  listExpertAvailability,
  saveExpertAvailability,
  listExternalExpertInvitations,
  createExternalExpertInvitation
} from '@/api/organization'
import { formatDate } from '@/utils/format'

const route = useRoute()

// 选项卡状态，支持 URL ?tab= 激活
const activeTab = ref('appointments')

// 1. 全员学术任职状态
const loadingAppointments = ref(false)
const appointmentList = ref<AcademicAppointmentItem[]>([])
const appointmentTotal = ref(0)
const appointmentPaging = reactive({ Index: 1, Size: 15, OrderType: false })
const appointmentFilters = reactive({ Like姓名: '', Like任职团体名称: '', Like所任职务: '' })

// 2. 我的学术任职状态
const loadingMyAppointments = ref(false)
const myAppointmentList = ref<AcademicAppointmentItem[]>([])
const myAppointmentTotal = ref(0)
const myAppointmentPaging = reactive({ Index: 1, Size: 15, OrderType: false })
const myAppointmentFilters = reactive({ Like任职团体名称: '', Like所任职务: '' })

// 3. 专家人才库状态
const loadingExperts = ref(false)
const expertList = ref<ExpertItem[]>([])
const expertTotal = ref(0)
const expertPaging = reactive({ Index: 1, Size: 15, OrderType: false })
const expertFilters = reactive({ Like姓名: '', Like工号: '', Like技术职称: '' })
const selectedExpertIds = ref<number[]>([])

// 4. 科研合作画像与专家遴选
const loadingPartners = ref(false)
const partnerList = ref<ResearchPartner[]>([])
const partnerFilters = reactive({ 关键词: '', 技术服务: '', 合作意愿: '' })

// 7. 专家可用时间与外部邀请治理
const governanceExpertId = ref<number | undefined>(undefined)
const loadingAvailability = ref(false)
const savingAvailability = ref(false)
const availabilityList = ref<any[]>([])
const availabilityTimeRange = ref<[string, string]>(['09:00:00', '17:00:00'])
const availabilityForm = reactive({ 星期: 1, 时区: 'Asia/Shanghai', 备注: '' })
const weekOptions = [
  { value: 1, label: '周一' },
  { value: 2, label: '周二' },
  { value: 3, label: '周三' },
  { value: 4, label: '周四' },
  { value: 5, label: '周五' },
  { value: 6, label: '周六' },
  { value: 7, label: '周日' }
]
const loadingInvitations = ref(false)
const creatingInvitation = ref(false)
const invitationList = ref<any[]>([])
const invitationForm = reactive({ 外部姓名: '', 外部邮箱: '', 外部机构: '', 邀请场景: '', 幂等键: '' })

// 5. 申报弹窗
const showAppointmentDialog = ref(false)
const submittingAppointment = ref(false)
const appointmentFormRef = ref<FormInstance>()
const appointmentDateRange = ref<[string, string]>(['', ''])
const appointmentForm = reactive({
  任职团体名称: '',
  所任职务: '',
  聘书路径: '',
  备注: ''
})
const appointmentRules: FormRules = {
  任职团体名称: [{ required: true, message: '请输入任职学会或学术团体名称', trigger: 'blur' }],
  所任职务: [{ required: true, message: '请输入所任职务', trigger: 'blur' }]
}

// 6. 专家入库遴选弹窗
const showNominateDialog = ref(false)
const loadingNonExperts = ref(false)
const submittingNominate = ref(false)
const nonExpertList = ref<ExpertItem[]>([])
const nonExpertTotal = ref(0)
const nonExpertPaging = reactive({ Index: 1, Size: 20 })
const nonExpertFilters = reactive({ Like姓名: '' })
const selectedNominateIds = ref<number[]>([])

// 7. 详情抽屉
const showDetailDrawer = ref(false)
const loadingDetail = ref(false)
const detailItem = ref<AcademicAppointmentItem | null>(null)
const auditSteps = ref<any[]>([])

// 获取全员学术任职
async function fetchAppointments() {
  loadingAppointments.value = true
  try {
    const res = await getPagingAcademicAppointments(appointmentPaging, appointmentFilters)
    const data = (res as any)?.data || res
    appointmentList.value = data?.list || []
    appointmentTotal.value = data?.total || 0
  } catch (err: any) {
    ElMessage.error(err.message || '获取学术任职列表失败')
  } finally {
    loadingAppointments.value = false
  }
}

function resetAppointmentFilters() {
  appointmentFilters.Like姓名 = ''
  appointmentFilters.Like任职团体名称 = ''
  appointmentFilters.Like所任职务 = ''
  appointmentPaging.Index = 1
  fetchAppointments()
}

// 获取我的学术任职
async function fetchMyAppointments() {
  loadingMyAppointments.value = true
  try {
    const res = await getMyPagingAcademicAppointments(myAppointmentPaging, myAppointmentFilters)
    const data = (res as any)?.data || res
    myAppointmentList.value = data?.list || []
    myAppointmentTotal.value = data?.total || 0
  } catch (err: any) {
    ElMessage.error(err.message || '获取我的学术任职列表失败')
  } finally {
    loadingMyAppointments.value = false
  }
}

// 获取专家人才库
async function fetchExperts() {
  loadingExperts.value = true
  try {
    const res = await getPagingExperts(expertPaging, expertFilters)
    const data = (res as any)?.data || res
    expertList.value = data?.list || []
    expertTotal.value = data?.total || 0
    if (!governanceExpertId.value && expertList.value.length) {
      governanceExpertId.value = expertList.value[0].编号
      if (activeTab.value === 'expert-governance') await fetchExpertAvailability()
    }
  } catch (err: any) {
    ElMessage.error(err.message || '获取专家人才库失败')
  } finally {
    loadingExperts.value = false
  }
}

function resetExpertFilters() {
  expertFilters.Like姓名 = ''
  expertFilters.Like工号 = ''
  expertFilters.Like技术职称 = ''
  expertPaging.Index = 1
  fetchExperts()
}

function onExpertSelectionChange(rows: ExpertItem[]) {
  selectedExpertIds.value = rows.map((r) => r.编号)
}

async function removeSingleExpert(id: number) {
  try {
    await deleteExperts([id])
    ElMessage.success('已从专家人才库移出')
    fetchExperts()
  } catch (err: any) {
    ElMessage.error(err.message || '移出专家库失败')
  }
}

async function batchRemoveExperts() {
  if (!selectedExpertIds.value.length) return
  try {
    await deleteExperts(selectedExpertIds.value)
    ElMessage.success(`已成功移出 ${selectedExpertIds.value.length} 位专家`)
    selectedExpertIds.value = []
    fetchExperts()
  } catch (err: any) {
    ElMessage.error(err.message || '批量移出失败')
  }
}

// 科研合作画像与多维检索
async function searchPartners() {
  loadingPartners.value = true
  try {
    const res = await searchResearchPartners({
      关键词: partnerFilters.关键词 || undefined,
      技术服务: partnerFilters.技术服务 || undefined,
      合作意愿: partnerFilters.合作意愿 || undefined,
      数量: 60
    })
    const data = (res as any)?.data || res
    partnerList.value = Array.isArray(data) ? data : []
  } catch (err: any) {
    ElMessage.error(err.message || '检索科研合作人画像失败')
  } finally {
    loadingPartners.value = false
  }
}

function resetPartnerFilters() {
  partnerFilters.关键词 = ''
  partnerFilters.技术服务 = ''
  partnerFilters.合作意愿 = ''
  searchPartners()
}

async function fetchExpertAvailability() {
  if (!governanceExpertId.value) {
    availabilityList.value = []
    return
  }
  loadingAvailability.value = true
  try {
    const res = await listExpertAvailability(governanceExpertId.value)
    const data = (res as any)?.data || res
    availabilityList.value = Array.isArray(data) ? data : []
  } catch (err: any) {
    availabilityList.value = []
    ElMessage.error(err.message || '获取专家可用时间失败')
  } finally {
    loadingAvailability.value = false
  }
}

async function saveAvailability() {
  if (!governanceExpertId.value || availabilityTimeRange.value.length !== 2) {
    ElMessage.warning('请选择专家和完整的可用时间段')
    return
  }
  const [start, end] = availabilityTimeRange.value
  if (!start || !end || start >= end) {
    ElMessage.warning('可用时间段的结束时间必须晚于开始时间')
    return
  }
  savingAvailability.value = true
  try {
    await saveExpertAvailability({
      专家编号: governanceExpertId.value,
      星期: availabilityForm.星期,
      开始时间: start,
      结束时间: end,
      时区: availabilityForm.时区.trim() || 'Asia/Shanghai',
      是否启用: true,
      备注: availabilityForm.备注.trim() || undefined
    })
    ElMessage.success('专家可用时间已保存')
    await fetchExpertAvailability()
  } catch (err: any) {
    ElMessage.error(err.message || '保存专家可用时间失败')
  } finally {
    savingAvailability.value = false
  }
}

async function fetchInvitations() {
  loadingInvitations.value = true
  try {
    const res = await listExternalExpertInvitations()
    const data = (res as any)?.data || res
    invitationList.value = Array.isArray(data?.邀请) ? data.邀请 : []
  } catch (err: any) {
    invitationList.value = []
    ElMessage.error(err.message || '获取外部专家邀请失败')
  } finally {
    loadingInvitations.value = false
  }
}

async function createInvitation() {
  const form = {
    外部姓名: invitationForm.外部姓名.trim(),
    外部邮箱: invitationForm.外部邮箱.trim(),
    外部机构: invitationForm.外部机构.trim() || undefined,
    邀请场景: invitationForm.邀请场景.trim(),
    幂等键: invitationForm.幂等键.trim()
  }
  if (!form.外部姓名 || !form.外部邮箱 || !form.邀请场景 || !form.幂等键) {
    ElMessage.warning('请填写外部姓名、邮箱、邀请场景和幂等键')
    return
  }
  creatingInvitation.value = true
  try {
    const res = await createExternalExpertInvitation(form)
    const data = (res as any)?.data || res
    ElMessage.warning(data?.提示 || '邀请已登记，当前不会发送外部邮件')
    invitationForm.外部姓名 = ''
    invitationForm.外部邮箱 = ''
    invitationForm.外部机构 = ''
    invitationForm.邀请场景 = ''
    invitationForm.幂等键 = ''
    await fetchInvitations()
  } catch (err: any) {
    ElMessage.error(err.message || '登记外部专家邀请失败')
  } finally {
    creatingInvitation.value = false
  }
}

async function fetchExpertGovernance() {
  if (!governanceExpertId.value && expertList.value.length) governanceExpertId.value = expertList.value[0].编号
  await Promise.all([fetchExpertAvailability(), fetchInvitations()])
}

function weekLabel(day: number) {
  return weekOptions.find((item) => item.value === Number(day))?.label || `星期${day || '-'}`
}

function formatTime(value?: string) {
  if (!value) return '--:--'
  return String(value).slice(0, 5)
}

function invitationStatusType(status?: string) {
  if (status === '已发送' || status === '已接受') return 'success'
  if (status === '发送失败' || status === '已拒绝') return 'danger'
  if (status === '未配置') return 'warning'
  return 'info'
}

function splitTags(tags?: string): string[] {
  if (!tags) return []
  return tags.split(/[,;，；\s]+/).filter(Boolean)
}

// 快速纳入专家库
async function nominateDirectly(id: number, name: string) {
  try {
    await addExpertsFromPersonnel([id])
    ElMessage.success(`已成功将【${name}】纳入评审专家人才库`)
    searchPartners()
    fetchExperts()
  } catch (err: any) {
    ElMessage.error(err.message || '纳入专家库失败')
  }
}

// 申报学术任职
function openAppointmentDialog() {
  appointmentForm.任职团体名称 = ''
  appointmentForm.所任职务 = ''
  appointmentForm.聘书路径 = ''
  appointmentForm.备注 = ''
  appointmentDateRange.value = ['', '']
  showAppointmentDialog.value = true
}

async function submitAppointment() {
  if (!appointmentFormRef.value) return
  await appointmentFormRef.value.validate(async (valid) => {
    if (!valid) return
    submittingAppointment.value = true
    try {
      await addAcademicAppointment({
        基本资料: {
          任职团体名称: appointmentForm.任职团体名称.trim(),
          所任职务: appointmentForm.所任职务.trim(),
          任职开始时间: appointmentDateRange.value[0] || undefined,
          任职结束时间: appointmentDateRange.value[1] || undefined,
          聘书路径: appointmentForm.聘书路径.trim() || undefined,
          备注: appointmentForm.备注.trim() || undefined
        }
      })
      ElMessage.success('学术任职登记成功并已发起流程流转')
      showAppointmentDialog.value = false
      fetchAppointments()
      fetchMyAppointments()
    } catch (err: any) {
      ElMessage.error(err.message || '提交学术任职失败')
    } finally {
      submittingAppointment.value = false
    }
  })
}

// 遴选弹窗
function openNominateDialog() {
  selectedNominateIds.value = []
  nonExpertFilters.Like姓名 = ''
  nonExpertPaging.Index = 1
  showNominateDialog.value = true
  fetchNonExperts()
}

async function fetchNonExperts() {
  loadingNonExperts.value = true
  try {
    const res = await getPagingNonExperts(nonExpertPaging, nonExpertFilters)
    const data = (res as any)?.data || res
    nonExpertList.value = data?.list || []
    nonExpertTotal.value = data?.total || 0
  } catch (err: any) {
    ElMessage.error(err.message || '获取候选人员失败')
  } finally {
    loadingNonExperts.value = false
  }
}

function onNonExpertSelectionChange(rows: ExpertItem[]) {
  selectedNominateIds.value = rows.map((r) => r.编号)
}

async function submitNominate() {
  if (!selectedNominateIds.value.length) return
  submittingNominate.value = true
  try {
    await addExpertsFromPersonnel(selectedNominateIds.value)
    ElMessage.success(`成功遴选 ${selectedNominateIds.value.length} 位专家进入专家库`)
    showNominateDialog.value = false
    fetchExperts()
    searchPartners()
  } catch (err: any) {
    ElMessage.error(err.message || '专家入库失败')
  } finally {
    submittingNominate.value = false
  }
}

// 详情查看
async function viewAppointmentDetail(id: number) {
  showDetailDrawer.value = true
  loadingDetail.value = true
  try {
    const res = await getAcademicAppointmentDetail(id)
    const data = (res as any)?.data || res
    detailItem.value = data?.学术任职详情 || null
    auditSteps.value = data?.审核情况 || []
  } catch (err: any) {
    ElMessage.error(err.message || '获取任职详情失败')
  } finally {
    loadingDetail.value = false
  }
}

function getFileUrl(path?: string) {
  if (!path) return '#'
  if (path.startsWith('http')) return path
  return `/${path.replace(/^\/+/, '')}`
}

function onTabChange(tab: string | number) {
  const name = String(tab)
  if (name === 'appointments') fetchAppointments()
  else if (name === 'my_appointments') fetchMyAppointments()
  else if (name === 'experts') fetchExperts()
  else if (name === 'expert-governance') fetchExpertGovernance()
  else if (name === 'selection') searchPartners()
}

function refreshActiveTab() {
  onTabChange(activeTab.value)
}

onMounted(() => {
  // 根据 URL 参数激活对应 tab
  const initialTab = (route.query.tab as string) || 'appointments'
  if (['appointments', 'my_appointments', 'experts', 'expert-governance', 'selection'].includes(initialTab)) {
    activeTab.value = initialTab
  }
  // 初始加载核心数据
  fetchAppointments()
  fetchMyAppointments()
  void fetchExperts().then(() => fetchExpertGovernance())
  searchPartners()
})
</script>

<style scoped>
.academic-appointments-container {
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

.toolbar-left,
.toolbar-right {
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
.mr-1 {
  margin-right: 4px;
}
.mb-1 {
  margin-bottom: 4px;
}

/* 科研合作画像网格 */
.partners-grid-wrapper {
  margin-top: 8px;
}

.partner-col {
  margin-bottom: 16px;
}

.partner-card {
  border-radius: 8px;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.governance-card {
  height: 100%;
  border-radius: 8px;
}

.card-header-line {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-weight: 600;
}

.card-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding-bottom: 10px;
  border-bottom: 1px solid #f0f2f5;
  margin-bottom: 10px;
}

.user-name {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
  display: block;
}

.dept-title {
  font-size: 12px;
  color: #909399;
}

.card-body-section {
  flex: 1;
}

.attr-row {
  margin-bottom: 8px;
  font-size: 13px;
  display: flex;
  align-items: flex-start;
}

.attr-label {
  color: #909399;
  width: 70px;
  flex-shrink: 0;
}

.attr-val {
  color: #606266;
  flex: 1;
}

.tags-box {
  flex: 1;
  display: flex;
  flex-wrap: wrap;
}

.card-footer-actions {
  border-top: 1px solid #f0f2f5;
  padding-top: 10px;
  display: flex;
  justify-content: flex-end;
  align-items: center;
}

.detail-drawer-content {
  padding: 10px 0;
}

.section-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
  margin: 0 0 12px;
}
</style>
