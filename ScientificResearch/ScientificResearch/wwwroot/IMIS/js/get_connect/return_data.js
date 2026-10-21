//用户
var User = {
    userLogin: function (type, userId, password, userLoginListener) {
        var url = Code.URL_POST_LOGIN;
        var postData = {
            工号: userId,
            密码: password,
            dbKey: 'ScientificResearch_Model'
        }
        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                userLoginListener(true, obj.data, '');

            } else {
                userLoginListener(false, '', strErro);

            }
        });
    },
    userChangePassword: function (type, options, userChangePasswordListener) {
        var url = Code.URL_POST_CHANGE_PASSWORD;
        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                userChangePasswordListener(true, obj.data, '');

            } else {
                userChangePasswordListener(false, '', strErro);

            }
        });
    },
    getUsersList: function (type, options, getUsersListListener) {
        var url = Code.URL_GET_USERS_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getUsersListListener(true, obj.data, '')
            } else {
                getUsersListListener(false, '', strErro)
            }
        });
    },
    getPagingUsersList: function (type, options, getPagingUsersListListener) {
        var url = Code.URL_GET_PAGING_USERS_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getPagingUsersListListener(true, obj.data, '')
            } else {
                getPagingUsersListListener(false, '', strErro)
            }
        });
    },
    getEnableUsersList: function (type, options, getEnableUsersListListener) {
        var url = Code.URL_GET_PAGING_ENABLE_USERS_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getEnableUsersListListener(true, obj.data, '')
            } else {
                getEnableUsersListListener(false, '', strErro)
            }
        });
    },
    addOrEditUsers: function (type, options, addOrEditUsersListener) {
        var url = Code.URL_POST_ADD_OR_EDIT_USERS;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addOrEditUsersListener(true, obj, '')
            } else {
                addOrEditUsersListener(false, '', strErro)
            }
        });
    },
    enableUsers: function (type, id, enableDepartmentListener) {
        var url = Code.URL_POST_ENABLE_USERS;

        Connect_Http.httpDatas(type, url, id, function getBackListener(success, obj, strErro) {
            if (success) {
                enableDepartmentListener(true, obj, '')
            } else {
                enableDepartmentListener(false, '', strErro)
            }
        });
    },
    DisableUsers: function (type, id, DisableDepartmentListener) {
        var url = Code.URL_POST_DISABLE_USERS;

        Connect_Http.httpDatas(type, url, id, function getBackListener(success, obj, strErro) {
            if (success) {
                DisableDepartmentListener(true, obj, '')
            } else {
                DisableDepartmentListener(false, '', strErro)
            }
        });
    },
    getUsersDetails: function (type, id, getUsersDetailsListener) {
        var url = Code.URL_GET_USER_DETAILS;
        var postDat = {
            人员编号: id
        }

        Connect_Http.httpDatas(type, url, postDat, function getBackListener(success, obj, strErro) {
            if (success) {
                getUsersDetailsListener(true, obj.data, '')
            } else {
                getUsersDetailsListener(false, '', strErro)
            }
        });
    },
    getUsersDetailsAuxiliaryData: function (type, getUsersDetailsAuxiliaryDataListener) {
        var url = Code.URL_GET_USER_DETAILS_AUXILIARY_DATA;

        Connect_Http.httpDatas(type, url, '', function getBackListener(success, obj, strErro) {
            if (success) {
                getUsersDetailsAuxiliaryDataListener(true, obj.data, '')
            } else {
                getUsersDetailsAuxiliaryDataListener(false, '', strErro)
            }
        });
    },

}

//人才与团队能力画像
var Talent = {
    getPeopleCapabilityProfile: function (type, peopleId, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_PEOPLE_CAPABILITY_PROFILE, { 人员编号: peopleId }, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    savePeopleCapabilityProfile: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_SAVE_PEOPLE_CAPABILITY_PROFILE, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    saveMentorRelation: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_SAVE_MENTOR_RELATION, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    findResearchCollaborators: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_RESEARCH_COLLABORATORS, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    saveResearchTeam: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_SAVE_RESEARCH_TEAM, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    saveResearchTeamMember: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_SAVE_RESEARCH_TEAM_MEMBER, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    savePeopleProjectAssignment: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_SAVE_PEOPLE_PROJECT_ASSIGNMENT, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    }
};

//科研评价与奖励
var Evaluation = {
    getRules: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_EVALUATION_RULES, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getRuleDetail: function (type, ruleId, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_EVALUATION_RULE_DETAIL, { 规则编号: ruleId }, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    saveRule: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_SAVE_EVALUATION_RULE, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    saveRuleItem: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_SAVE_EVALUATION_RULE_ITEM, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    generate: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_GENERATE_EVALUATION, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getResults: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_EVALUATION_RESULTS, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getDetail: function (type, resultId, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_EVALUATION_DETAIL, { 评价结果编号: resultId }, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    review: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_REVIEW_EVALUATION, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    appeal: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_APPEAL_EVALUATION, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    handleAppeal: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_HANDLE_EVALUATION_APPEAL, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    registerReward: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_REGISTER_EVALUATION_REWARD, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getRanking: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_EVALUATION_RANKING, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    }
};

//科研瓶颈与决策分析
var Analytics = {
    getBottleneckEvents: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_BOTTLENECK_EVENTS, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    saveBottleneckEvent: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_SAVE_BOTTLENECK_EVENT, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getBottleneckSummary: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_BOTTLENECK_SUMMARY, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getResourceUsageSummary: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_RESOURCE_USAGE_SUMMARY, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    saveResourceUsage: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_SAVE_RESOURCE_USAGE, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getDecisionSummary: function (type, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_DECISION_SUMMARY, {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    }
};

//运行治理、流程版本、失败告警和数据导出
var Operations = {
    getInterfaceFailureAlerts: function (type, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_INTERFACE_FAILURE_ALERTS, {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getFlowVersions: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_FLOW_VERSIONS, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    saveFlowVersion: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_SAVE_FLOW_VERSION, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    publishFlowVersion: function (type, versionId, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_PUBLISH_FLOW_VERSION, { 流程版本编号: versionId }, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    createDataExport: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_CREATE_DATA_EXPORT, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getDataExportTasks: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_DATA_EXPORT_TASKS, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    updateDataExport: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_UPDATE_DATA_EXPORT, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getExternalSystems: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_EXTERNAL_SYSTEMS, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    saveExternalSystem: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_SAVE_EXTERNAL_SYSTEM, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getExternalSyncTasks: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_EXTERNAL_SYNC_TASKS, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    createExternalSyncTask: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_CREATE_EXTERNAL_SYNC_TASK, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    retryExternalSyncTask: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_RETRY_EXTERNAL_SYNC_TASK, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    cancelExternalSyncTask: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_CANCEL_EXTERNAL_SYNC_TASK, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    }
};

//科研单位名称规范
var Institution = {
    getNormalization: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_RESEARCH_INSTITUTION_NORMALIZATION, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    saveInstitution: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_SAVE_RESEARCH_INSTITUTION, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    saveAlias: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_SAVE_RESEARCH_INSTITUTION_ALIAS, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    normalizeResultAuthorInstitution: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_NORMALIZE_RESULT_AUTHOR_INSTITUTION, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    }
};

//统一工作台
var Workbench = {
    getSummary: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_WORKBENCH_SUMMARY, options || {}, function (success, obj, strErro) {
            if (success) {
                listener(true, obj.data, '');
            } else {
                listener(false, '', strErro);
            }
        });
    }
};

//纵向项目申报
var YProject = {
    preflight: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_VERTICAL_PROJECT_PREFLIGHT, options, function (success, obj, strErro) {
            if (success) {
                listener(true, obj.data, '');
            } else {
                listener(false, '', strErro);
            }
        });
    },
    lifecyclePreflight: function (type, stage, projectId, listener) {
        var urls = {
            midterm: Code.URL_GET_VERTICAL_MIDTERM_PREFLIGHT,
            change: Code.URL_GET_VERTICAL_CHANGE_PREFLIGHT,
            closeout: Code.URL_GET_VERTICAL_CLOSEOUT_PREFLIGHT
        };
        var url = urls[stage];
        if (!url) {
            listener(false, '', '不支持的项目生命周期阶段');
            return;
        }
        Connect_Http.httpDatas(type, url, { 纵向项目立项编号: projectId }, function (success, obj, strErro) {
            if (success) {
                listener(true, obj.data, '');
            } else {
                listener(false, '', strErro);
            }
        });
    },
    reviewerConflictPreflight: function (type, stepId, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_REVIEWER_CONFLICT_PREFLIGHT, { 步骤编号: stepId }, function (success, obj, strErro) {
            if (success) {
                listener(true, obj.data, '');
            } else {
                listener(false, '', strErro);
            }
        });
    },
    getApplicationReviewSummary: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_VERTICAL_APPLICATION_REVIEW_SUMMARY, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    compareApplicationVersions: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_VERTICAL_APPLICATION_VERSION_COMPARE, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    addReviewComment: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_VERTICAL_REVIEW_COMMENT, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    addUnfundedReview: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_VERTICAL_UNFUNDED_REVIEW, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getProjectLifecycleArchive: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_VERTICAL_LIFECYCLE_ARCHIVE, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    requestProjectLifecycle: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_VERTICAL_LIFECYCLE_REQUEST, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    reviewProjectLifecycle: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_VERTICAL_LIFECYCLE_REVIEW, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    submitResearchReport: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_VERTICAL_RESEARCH_REPORT, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    reviewResearchReport: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_VERTICAL_RESEARCH_REPORT_REVIEW, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    archiveProjectMaterial: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_VERTICAL_ARCHIVE_MATERIAL, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getProjectBudgetLayered: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_PROJECT_BUDGET_LAYERED, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getProjectPaymentMilestones: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_PROJECT_PAYMENT_MILESTONES, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    addProjectLedger: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_PROJECT_LEDGER, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    saveProjectPaymentMilestone: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_PROJECT_PAYMENT_MILESTONE, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    syncFinanceTransactions: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_SYNC_FINANCE_TRANSACTIONS, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    getResultGovernance: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_RESULT_GOVERNANCE, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    addResultLink: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_RESULT_LINK, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    verifyResultAuthor: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_RESULT_AUTHOR_VERIFY, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    updateResultGovernanceStatus: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_RESULT_GOVERNANCE_STATUS, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    }
};

//角色
var Role = {
    getRoleList: function (type, getRoleListListener) {
        var url = Code.URL_GET_ROLE_LIST;

        Connect_Http.httpDatas(type, url, '', function getBackListener(success, obj, strErro) {
            if (success) {
                getRoleListListener(true, obj.data, '')
            } else {
                getRoleListListener(false, '', strErro)
            }
        });
    },
    addRole: function (type, options, addRoleListener) {
        var url = Code.URL_POST_ADD_ROLE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addRoleListener(true, obj, '')
            } else {
                addRoleListener(false, '', strErro)
            }
        });
    },
    editRole: function (type, options, editRoleListener) {
        var url = Code.URL_POST_ADD_ROLE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editRoleListener(true, obj, '')
            } else {
                editRoleListener(false, '', strErro)
            }
        });
    },
    getRoleUsers: function (type, options, getRoleUsersListener) {
        var url = Code.URL_GET_ROLE_USERS;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getRoleUsersListener(true, obj.data, '')
            } else {
                getRoleUsersListener(false, '', strErro)
            }
        });
    },
    getRoleNotUsers: function (type, options, getRoleNotUsersListener) {
        var url = Code.URL_GET_ROLE_NOT_USERS;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getRoleNotUsersListener(true, obj.data, '')
            } else {
                getRoleNotUsersListener(false, '', strErro)
            }
        });
    },
    editRoleUsers: function (type, options, editRoleUsersListener) {
        var url = Code.URL_POST_EDIT_ROLE_USERS;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editRoleUsersListener(true, obj, '')
            } else {
                editRoleUsersListener(false, '', strErro)
            }
        });
    },
    addAndDelRoleUsers: function (type, options, addAndDelRoleUsersListener) {
        var url = Code.URL_POST_ADD_AND_DEL_ROLE_USERS;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addAndDelRoleUsersListener(true, obj, '')
            } else {
                addAndDelRoleUsersListener(false, '', strErro)
            }
        });
    },
    getRoleJurisdiction: function (type, number, getRoleJurisdictionListener) {
        var url = Code.URL_GET_ROLE_JURISDICTION;
        var postData = {
            角色编号: number
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getRoleJurisdictionListener(true, obj.data, '')
            } else {
                getRoleJurisdictionListener(false, '', strErro)
            }
        });
    },
    editRoleJurisdiction: function (type, options, editRoleJurisdictionListener) {
        var url = Code.URL_POST_EDIT_ROLE_JURISDICTION;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editRoleJurisdictionListener(true, obj, '')
            } else {
                editRoleJurisdictionListener(false, '', strErro)
            }
        });
    },
}

//日志
var Log = {
    getLoginLog: function (type, options, getLoginLogListener) {
        var url = Code.URL_GET_LOGIN_LOG;
        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getLoginLogListener(true, obj.data, '');

            } else {
                getLoginLogListener(false, '', strErro);

            }
        });
    },
}

//部门
var Department = {
    getDepartmentList: function (type, name, getDepartmentListListener) {
        var url = Code.URL_GET_DEPARTMENT_LIST;
        var postData = {
            Like名称: name
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getDepartmentListListener(true, obj.data, '')
            } else {
                getDepartmentListListener(false, '', strErro)
            }
        });
    },
    getEnableDepartmentList: function (type, name, getEnableDepartmentListListener) {
        var url = Code.URL_GET_ENABLE_DEPARTMENT_LIST;
        var postData = {
            Like名称: name
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getEnableDepartmentListListener(true, obj.data, '')
            } else {
                getEnableDepartmentListListener(false, '', strErro)
            }
        });
    },
    addOrEditDepartment: function (type, options, addOrEditDepartmentListener) {
        var url = Code.URL_POST_ADD_OR_EDIT_DEPARTMENT;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addOrEditDepartmentListener(true, obj, '')
            } else {
                addOrEditDepartmentListener(false, '', strErro)
            }
        });
    },
    enableDepartment: function (type, id, enableDepartmentListener) {
        var url = Code.URL_POST_ENABLE_DEPARTMENT;

        Connect_Http.httpDatas(type, url, id, function getBackListener(success, obj, strErro) {
            if (success) {
                enableDepartmentListener(true, obj, '')
            } else {
                enableDepartmentListener(false, '', strErro)
            }
        });
    },
    DisableDepartment: function (type, id, DisableDepartmentListener) {
        var url = Code.URL_POST_DISABLE_DEPARTMENT;

        Connect_Http.httpDatas(type, url, id, function getBackListener(success, obj, strErro) {
            if (success) {
                DisableDepartmentListener(true, obj, '')
            } else {
                DisableDepartmentListener(false, '', strErro)
            }
        });
    },

}

//字典
var Dictionary = {
    getPagingDictionaryList: function (type, options, getPagingDictionaryListListener) {
        var url = Code.URL_GET_PAGING_CATEGORY_DICTIONARY;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getPagingDictionaryListListener(true, obj.data, '')
            } else {
                getPagingDictionaryListListener(false, '', strErro)
            }
        });
    },
    getDictionaryList: function (type, name, getDictionaryListListener) {
        var url = Code.URL_GET_CATEGORY_DICTIONARY;
        var postData = {
            分类: name
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getDictionaryListListener(true, obj.data, '')
            } else {
                getDictionaryListListener(false, '', strErro)
            }
        });
    },
    addDictionary: function (type, options, addDictionaryListener) {
        var url = Code.URL_POST_ADD_DICTIONARY;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addDictionaryListener(true, obj, '')
            } else {
                addDictionaryListener(false, '', strErro)
            }
        });
    },
    editDictionary: function (type, options, editDictionaryListener) {
        var url = Code.URL_POST_EDIT_DICTIONARY;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editDictionaryListener(true, obj, '')
            } else {
                editDictionaryListener(false, '', strErro)
            }
        });
    },
    delDictionary: function (type, options, delDictionaryListener) {
        var url = Code.URL_POST_DEL_DICTIONARY;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                delDictionaryListener(true, obj, '')
            } else {
                delDictionaryListener(false, '', strErro)
            }
        });
    },
    getPagingDictionaryChild: function (type, options, getPagingDictionaryChildListener) {
        var url = Code.URL_GET_PAGING_DICTIONARY_CHILD;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getPagingDictionaryChildListener(true, obj.data, '')
            } else {
                getPagingDictionaryChildListener(false, '', strErro)
            }
        });
    },
    getDictionaryChild: function (type, id, getDictionaryChildListener) {
        var url = Code.URL_GET_DICTIONARY_CHILD;

        var postData = {
            编号: id
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getDictionaryChildListener(true, obj.data, '')
            } else {
                getDictionaryChildListener(false, '', strErro)
            }
        });
    }
}

//学科
var Subject = {
    getSubjectList: function (type, options, getSubjectListListener) {
        var url = Code.URL_GET_SUBJECT_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getSubjectListListener(true, obj.data, '')
            } else {
                getSubjectListListener(false, '', strErro)
            }
        });
    },
    addSubject: function (type, options, addSubjectListener) {
        var url = Code.URL_POST_ADD_SUBJECT;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addSubjectListener(true, obj, '')
            } else {
                addSubjectListener(false, '', strErro)
            }
        });
    },
    editSubject: function (type, options, editSubjectListener) {
        var url = Code.URL_POST_EDIT_SUBJECT;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editSubjectListener(true, obj, '')
            } else {
                editSubjectListener(false, '', strErro)
            }
        });
    },
    delSubject: function (type, options, delSubjectListener) {
        var url = Code.URL_POST_DEL_SUBJECT;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                delSubjectListener(true, obj, '')
            } else {
                delSubjectListener(false, '', strErro)
            }
        });
    }
}

//国民行业代码
var IndustryCode = {
    getIndustryCodeList: function (type, options, getIndustryCodeListListener) {
        var url = Code.URL_GET_INDUSTRY_CODE_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getIndustryCodeListListener(true, obj.data, '')
            } else {
                getIndustryCodeListListener(false, '', strErro)
            }
        });
    },
    addIndustryCode: function (type, options, addIndustryCodeListener) {
        var url = Code.URL_POST_ADD_INDUSTRY_CODE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addIndustryCodeListener(true, obj, '')
            } else {
                addIndustryCodeListener(false, '', strErro)
            }
        });
    },
    editIndustryCode: function (type, options, editIndustryCodeListener) {
        var url = Code.URL_POST_EDIT_INDUSTRY_CODE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editIndustryCodeListener(true, obj, '')
            } else {
                editIndustryCodeListener(false, '', strErro)
            }
        });
    },
    delIndustryCode: function (type, options, delIndustryCodeListener) {
        var url = Code.URL_POST_DEL_INDUSTRY_CODE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                delIndustryCodeListener(true, obj, '')
            } else {
                delIndustryCodeListener(false, '', strErro)
            }
        });
    }
}

//合同买方性质
var ContractBuyer = {
    getContractBuyerList: function (type, options, getContractBuyerListListener) {
        var url = Code.URL_GET_CONTRACT_BUYER_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getContractBuyerListListener(true, obj.data, '')
            } else {
                getContractBuyerListListener(false, '', strErro)
            }
        });
    },
    addContractBuyer: function (type, options, addContractBuyerListener) {
        var url = Code.URL_POST_ADD_CONTRACT_BUYER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addContractBuyerListener(true, obj, '')
            } else {
                addContractBuyerListener(false, '', strErro)
            }
        });
    },
    editContractBuyer: function (type, options, editContractBuyerListener) {
        var url = Code.URL_POST_EDIT_CONTRACT_BUYER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editContractBuyerListener(true, obj, '')
            } else {
                editContractBuyerListener(false, '', strErro)
            }
        });
    },
    delContractBuyer: function (type, options, delContractBuyerListener) {
        var url = Code.URL_POST_DEL_CONTRACT_BUYER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                delContractBuyerListener(true, obj, '')
            } else {
                delContractBuyerListener(false, '', strErro)
            }
        });
    }
}

//技术领域
var TechnicalField = {
    getTechnicalFieldList: function (type, options, getTechnicalFieldListListener) {
        var url = Code.URL_GET_TECHNICAL_FIELD_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getTechnicalFieldListListener(true, obj.data, '')
            } else {
                getTechnicalFieldListListener(false, '', strErro)
            }
        });
    },
    addTechnicalField: function (type, options, addTechnicalFieldListener) {
        var url = Code.URL_POST_ADD_TECHNICAL_FIELD;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addTechnicalFieldListener(true, obj, '')
            } else {
                addTechnicalFieldListener(false, '', strErro)
            }
        });
    },
    editTechnicalField: function (type, options, editTechnicalFieldListener) {
        var url = Code.URL_POST_EDIT_TECHNICAL_FIELD;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editTechnicalFieldListener(true, obj, '')
            } else {
                editTechnicalFieldListener(false, '', strErro)
            }
        });
    },
    delTechnicalField: function (type, options, delTechnicalFieldListener) {
        var url = Code.URL_POST_DEL_TECHNICAL_FIELD;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                delTechnicalFieldListener(true, obj, '')
            } else {
                delTechnicalFieldListener(false, '', strErro)
            }
        });
    }
}

//区域
var Area = {
    getAreaList: function (type, options, getAreaListListener) {
        var url = Code.URL_GET_AREA_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getAreaListListener(true, obj.data, '')
            } else {
                getAreaListListener(false, '', strErro)
            }
        });
    },
    addArea: function (type, options, addAreaListener) {
        var url = Code.URL_POST_ADD_AREA;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addAreaListener(true, obj, '')
            } else {
                addAreaListener(false, '', strErro)
            }
        });
    },
    editArea: function (type, options, editAreaListener) {
        var url = Code.URL_POST_EDIT_AREA;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editAreaListener(true, obj, '')
            } else {
                editAreaListener(false, '', strErro)
            }
        });
    },
    delArea: function (type, options, delAreaListener) {
        var url = Code.URL_POST_DEL_AREA;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                delAreaListener(true, obj, '')
            } else {
                delAreaListener(false, '', strErro)
            }
        });
    }
}

//经费模版
var ExpenditureTemplate = {
    getTemplateList: function (type, options, getTemplateListListener) {
        var url = Code.URL_GET_EXPENDITURE_TEMPLATE_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getTemplateListListener(true, obj.data, '')
            } else {
                getTemplateListListener(false, '', strErro)
            }
        });
    },
    addOrEditTemplate: function (type, options, addOrEditTemplateListener) {
        var url = Code.URL_POST_ADD_OR_EDIT_EXPENDITURE_TEMPLATE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addOrEditTemplateListener(true, obj, '')
            } else {
                addOrEditTemplateListener(false, '', strErro)
            }
        });
    },
    delTemplate: function (type, options, delTemplateListener) {
        var url = Code.URL_POST_DEL_EXPENDITURE_TEMPLATE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                delTemplateListener(true, obj, '')
            } else {
                delTemplateListener(false, '', strErro)
            }
        });
    },
    getExpenditureType: function (type, options, getExpenditureTypeListener) {
        var url = Code.URL_GET_EXPENDITURE_TYPE_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getExpenditureTypeListener(true, obj.data, '')
            } else {
                getExpenditureTypeListener(false, '', strErro)
            }
        });
    },
    addOrEditExpenditureType: function (type, options, addOrEditExpenditureTypeListener) {
        var url = Code.URL_POST_ADD_OR_EDIT_EXPENDITURE_TYPE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addOrEditExpenditureTypeListener(true, obj, '')
            } else {
                addOrEditExpenditureTypeListener(false, '', strErro)
            }
        });
    },
    delExpenditureType: function (type, options, delExpenditureTypeListener) {
        var url = Code.URL_POST_DEL_EXPENDITURE_TYPE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                delExpenditureTypeListener(true, obj, '')
            } else {
                delExpenditureTypeListener(false, '', strErro)
            }
        });
    },
    getExpenditureTypeDetail: function (type, id, getExpenditureTypeDetailListener) {
        var url = Code.URL_GET_EXPENDITURE_TYPE_DETAIL;
        var postData = {
            经费模版项目支出类型编号: id
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getExpenditureTypeDetailListener(true, obj.data, '')
            } else {
                getExpenditureTypeDetailListener(false, '', strErro)
            }
        });
    },
}

//项目分类
var ProjectType = {
    getProjectTypeList: function (type, options, getProjectTypeListListener) {
        var url = Code.URL_GET_PROJECT_TYPE_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getProjectTypeListListener(true, obj.data, '')
            } else {
                getProjectTypeListListener(false, '', strErro)
            }
        });
    },
    addOrEditProjectType: function (type, options, addOrEditProjectTypeListener) {
        var url = Code.URL_POST_ADD_OR_EDIT_PROJECT_TYPE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addOrEditProjectTypeListener(true, obj, '')
            } else {
                addOrEditProjectTypeListener(false, '', strErro)
            }
        });
    },
    delProjectType: function (type, options, delProjectTypeListener) {
        var url = Code.URL_POST_DEL_PROJECT_TYPE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                delProjectTypeListener(true, obj, '')
            } else {
                delProjectTypeListener(false, '', strErro)
            }
        });
    },
}

//期刊
var Periodical = {
    getPeriodicalList: function (type, options, getPeriodicalListListener) {
        var url = Code.URL_GET_PERIODICAL_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getPeriodicalListListener(true, obj.data, '')
            } else {
                getPeriodicalListListener(false, '', strErro)
            }
        });
    },
    addOrEditPeriodical: function (type, options, addOrEditPeriodicalListener) {
        var url = Code.URL_POST_ADD_OR_EDIT_PERIODICAL;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addOrEditPeriodicalListener(true, obj, '')
            } else {
                addOrEditPeriodicalListener(false, '', strErro)
            }
        });
    },
    delPeriodical: function (type, options, delPeriodicalListener) {
        var url = Code.URL_POST_DEL_PERIODICAL;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                delPeriodicalListener(true, obj, '')
            } else {
                delPeriodicalListener(false, '', strErro)
            }
        });
    },
    getPeriodicalSeries: function (type, classify, getPeriodicalSeriesListener) {
        var url = Code.URL_GET_CATEGORY_DICTIONARY;
        var postData = {
            分类: classify
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getPeriodicalSeriesListener(true, obj.data, '')
            } else {
                getPeriodicalSeriesListener(false, '', strErro)
            }
        });
    }
}

//学术任职
var Learning = {
    getPagingLearning: function (type, options, getPagingLearningListener) {
        var url = Code.URL_GET_PAGING_LEARNING;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getPagingLearningListener(true, obj.data, '')
            } else {
                getPagingLearningListener(false, '', strErro)
            }
        });
    },
    getLearningDetails: function (type, id, getLearningDetailsListener) {
        var url = Code.URL_GET_LEARNING_DETAILS;
        var postData = {
            任职编号: id
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getLearningDetailsListener(true, obj.data, '')
            } else {
                getLearningDetailsListener(false, '', strErro)
            }
        });
    },
    addLearningUser: function (type, options, addLearningUserUserListener) {
        var url = Code.URL_POST_ADD_LEARNING_USER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addLearningUserUserListener(true, obj, '')
            } else {
                addLearningUserUserListener(false, '', strErro)
            }
        });
    },
    editLearningUser: function (type, options, editLearningUserUserListener) {
        var url = Code.URL_POST_EDIT_LEARNING_USER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editLearningUserUserListener(true, obj, '')
            } else {
                editLearningUserUserListener(false, '', strErro)
            }
        });
    },
    uploadLearningFile: function (type, options, uploadLearningFileListener) {
        var url = Code.URL_POST_EDIT_LEARNING_USER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                uploadLearningFileListener(true, obj, '')
            } else {
                uploadLearningFileListener(false, '', strErro)
            }
        });
    }
}

//专家人员
var ExpertTalents = {
    getExpertTalentshUsers: function (type, options, getExpertTalentshUsersListener) {
        var url = Code.URL_GET_PAGING_EXPERTALENTS_USER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getExpertTalentshUsersListener(true, obj.data, '')
            } else {
                getExpertTalentshUsersListener(false, '', strErro)
            }
        });
    },
    addExpertTalentsUsers: function (type, options, addExpertTalentsUsersListener) {
        var url = Code.URL_POST_ADD_EXPERTALENTS_USER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addExpertTalentsUsersListener(true, obj, '')
            } else {
                addExpertTalentsUsersListener(false, '', strErro)
            }
        });
    },
    delExpertTalentsUsers: function (type, options, delExpertTalentsUsersListener) {
        var url = Code.URL_POST_DEL_EXPERTALENTS_USER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                delExpertTalentsUsersListener(true, obj, '')
            } else {
                delExpertTalentsUsersListener(false, '', strErro)
            }

        });
    },
}

//科研
var ScientificResearch = {
    getScientificResearchDetails: function (type, id, getScientificResearchDetailsListener) {
        var url = Code.URL_GET_SCIENTIFICRESARCH_DETAILS;
        var postData = {
            当前登录人编号: id
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getScientificResearchDetailsListener(true, obj.data, '')
            } else {
                getScientificResearchDetailsListener(false, '', strErro)
            }
        });
    },
    getScientificResearchUsers: function (type, options, getScientificResearchUsersListener) {
        var url = Code.URL_GET_PAGING_SCIENTIFICRESARCH_USER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getScientificResearchUsersListener(true, obj.data, '')
            } else {
                getScientificResearchUsersListener(false, '', strErro)
            }
        });
    },
    addScientificResearchUsers: function (type, options, addScientificResearchUsersListener) {
        var url = Code.URL_POST_ADD_SCIENTIFICRESARCH_USER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addScientificResearchUsersListener(true, obj, '')
            } else {
                addScientificResearchUsersListener(false, '', strErro)
            }
        });
    },
    delScientificResearchUsers: function (type, options, delScientificResearchUsersListener) {
        var url = Code.URL_POST_DEL_SCIENTIFICRESARCH_USER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                delScientificResearchUsersListener(true, obj, '')
            } else {
                delScientificResearchUsersListener(false, '', strErro)
            }
        });
    },
    getScientificResearchUntreated: function (type, options, getScientificResearchUntreatedListener) {
        var url = Code.URL_GET_SCIENTIFICRESARCH_UNTREATED;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getScientificResearchUntreatedListener(true, obj.data, '')
            } else {
                getScientificResearchUntreatedListener(false, '', strErro)
            }
        });
    },
}

//工作流程
var WorkFlow = {
    WorkFlow: function (type, options, WorkFlowListener) {
        var url = Code.URL_POST_WORKFLOW;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                WorkFlowListener(true, obj, '')
            } else {
                WorkFlowListener(false, '', strErro)
            }

        });
    },
}

//通知公告
var Notice = {
    getUserNoticeList: function (type, options, getUserNoticeListListener) {
        var url = Code.URL_GET_PAGING_USER_NOTICE_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getUserNoticeListListener(true, obj.data, '')
            } else {
                getUserNoticeListListener(false, '', strErro)
            }
        });
    },
    getNoticeList: function (type, options, getNoticeListListener) {
        var url = Code.URL_GET_PAGING_NOTICE_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getNoticeListListener(true, obj.data, '')
            } else {
                getNoticeListListener(false, '', strErro)
            }
        });
    },
    getNoticeDetails: function (type, id, getNoticeDetailsListener) {
        var url = Code.URL_GET_NOTICE_DETAILS;
        var postData = {
            编号: id
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getNoticeDetailsListener(true, obj.data, '')
            } else {
                getNoticeDetailsListener(false, '', strErro)
            }
        });
    },
    enableOrDisableNotice: function (type, options, enableOrDisableNoticeListener) {
        var url = Code.URL_POST_ENABLE_OR_DISABLE_NOTICE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                enableOrDisableNoticeListener(true, obj, '')
            } else {
                enableOrDisableNoticeListener(false, '', strErro)
            }

        });
    },
    addNotice: function (type, options, addNoticeListener) {
        var url = Code.URL_POST_ADD_NOTICE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addNoticeListener(true, obj, '')
            } else {
                addNoticeListener(false, '', strErro)
            }

        });
    },
}

//下载中心
var Download = {
    getDownloadList: function (type, options, getDownloadListListener) {
        var url = Code.URL_GET_DOWNLOAD_CENTER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getDownloadListListener(true, obj.data, '')
            } else {
                getDownloadListListener(false, '', strErro)
            }
        });
    },
    getEnableDownloadList: function (type, options, getEnableDownloadListListener) {
        var url = Code.URL_GET_ENABLE_DOWNLOAD_CENTER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getEnableDownloadListListener(true, obj.data, '')
            } else {
                getEnableDownloadListListener(false, '', strErro)
            }
        });
    },
    addOrEditDownload: function (type, options, addOrEditDownloadListener) {
        var url = Code.URL_POST_ADD_OR_DOWNLOAD;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addOrEditDownloadListener(true, obj, '')
            } else {
                addOrEditDownloadListener(false, '', strErro)
            }

        });
    },
    enableOrDisableDownload: function (type, options, enableOrDisableDownloadListener) {
        var url = Code.URL_POST_ENABLE_OR_DISABLE_DOWNLOAD;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                enableOrDisableDownloadListener(true, obj, '')
            } else {
                enableOrDisableDownloadListener(false, '', strErro)
            }

        });
    },
}

//讲座
var Lecture = {
    getHostLectureList: function (type, options, getHostLectureListListener) {
        var url = Code.URL_GET_HOST_LECTURE_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getHostLectureListListener(true, obj.data, '')
            } else {
                getHostLectureListListener(false, '', strErro)
            }
        });
    },
    getHostLectureDetails: function (type, id, getHostLectureDetailsListener) {
        var url = Code.URL_GET_HOST_LECTURE_DETAILS;
        var postData = {
            主办讲座编号: id
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getHostLectureDetailsListener(true, obj.data, '')
            } else {
                getHostLectureDetailsListener(false, '', strErro)
            }
        });

    },
    addHostLecture: function (type, options, addHostLectureListener) {
        var url = Code.URL_POST_ADD_HOST_LECTURE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addHostLectureListener(true, obj, '')
            } else {
                addHostLectureListener(false, '', strErro)
            }

        });
    },
    editHostLecture: function (type, options, editHostLectureListener) {
        var url = Code.URL_POST_EDIT_HOST_LECTURE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editHostLectureListener(true, obj, '')
            } else {
                editHostLectureListener(false, '', strErro)
            }

        });
    },
    getFeedbackLecture: function (type, options, getFeedbackLectureListener) {
        var url = Code.URL_GET_FEEDBACK_LECTURE;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getFeedbackLectureListener(true, obj.data, '')
            } else {
                getFeedbackLectureListener(false, '', strErro)
            }
        });
    },
    getLectureFeedbackDetails: function (type, id, getLectureFeedbackDetailsListener) {
        var url = Code.URL_GET_LECTURE_FEEDBACK_DETAILS;
        var postData = {
            讲座反馈编号: id
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getLectureFeedbackDetailsListener(true, obj.data, '')
            } else {
                getLectureFeedbackDetailsListener(false, '', strErro)
            }
        });

    },
    addLectureFeedback: function (type, options, addLectureFeedbackListener) {
        var url = Code.URL_POST_ADD_LECTURE_FEEDBACK;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addLectureFeedbackListener(true, obj, '')
            } else {
                addLectureFeedbackListener(false, '', strErro)
            }

        });
    },
    editLectureFeedback: function (type, options, editLectureFeedbackListener) {
        var url = Code.URL_POST_EDIT_LECTURE_FEEDBACK;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editLectureFeedbackListener(true, obj, '')
            } else {
                editLectureFeedbackListener(false, '', strErro)
            }

        });
    },
}

//会议
var Meeting={
    getMeetingList: function (type, options, getMeetingListListener) {
        var url = Code.URL_GET_MEETING_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getMeetingListListener(true, obj.data, '')
            } else {
                getMeetingListListener(false, '', strErro)
            }
        });
    },
    getMeetingDetails: function (type, id, getMeetingDetailsListener) {
        var url = Code.URL_GET_MEETING_DETAILS;
        var postData = {
            参加会议编号: id
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getMeetingDetailsListener(true, obj.data, '')
            } else {
                getMeetingDetailsListener(false, '', strErro)
            }
        });

    },
    addMeeting: function (type, options, addMeetingListener) {
        var url = Code.URL_POST_ADD_MEETING;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addMeetingListener(true, obj, '')
            } else {
                addMeetingListener(false, '', strErro)
            }

        });
    },
    editMeeting: function (type, options, editMeetingListener) {
        var url = Code.URL_POST_EDIT_MEETING;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editMeetingListener(true, obj, '')
            } else {
                editMeetingListener(false, '', strErro)
            }

        });
    },
    getMeetingFeedbackList: function (type, options, getMeetingFeedbackListListener) {
        var url = Code.URL_GET_MEETING_FEEDBACK_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getMeetingFeedbackListListener(true, obj.data, '')
            } else {
                getMeetingFeedbackListListener(false, '', strErro)
            }
        });
    },
    getMeetingFeedbackDetails: function (type, id, getMeetingFeedbackDetailsListener) {
        var url = Code.URL_GET_MEETING_FEEDBACK_DETAILS;
        var postData = {
            参加会议反馈编号: id
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getMeetingFeedbackDetailsListener(true, obj.data, '')
            } else {
                getMeetingFeedbackDetailsListener(false, '', strErro)
            }
        });

    },
    addMeetingFeedback: function (type, options, addMeetingFeedbackListener) {
        var url = Code.URL_POST_ADD_MEETING_FEEDBACK;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addMeetingFeedbackListener(true, obj, '')
            } else {
                addMeetingFeedbackListener(false, '', strErro)
            }

        });
    },
    editMeetingFeedback: function (type, options, editMeetingFeedbackListener) {
        var url = Code.URL_POST_EDIT_MEETING_FEEDBACK;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editMeetingFeedbackListener(true, obj, '')
            } else {
                editMeetingFeedbackListener(false, '', strErro)
            }

        });
    }
}

//论文
var Paper={
    getPaperList: function (type, options, getPaperListListener) {
        var url = Code.URL_GET_PAPER_LIST;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                getPaperListListener(true, obj.data, '')
            } else {
                getPaperListListener(false, '', strErro)
            }
        });
    },
    getPaperDetails: function (type, id, getPaperDetailsListener) {
        var url = Code.URL_GET_PAPER_DETAILS;
        var postData = {
            论文编号: id
        }

        Connect_Http.httpDatas(type, url, postData, function getBackListener(success, obj, strErro) {
            if (success) {
                getPaperDetailsListener(true, obj.data, '')
            } else {
                getPaperDetailsListener(false, '', strErro)
            }
        });

    },
    getClaimablePaperList: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_CLAIMABLE_PAPER_LIST, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    claimPaper: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_POST_CLAIM_PAPER, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    identifyPaperJournal: function (type, options, listener) {
        Connect_Http.httpDatas(type, Code.URL_GET_PAPER_JOURNAL_METADATA, options || {}, function (success, obj, strErro) {
            listener(success, success ? obj.data : '', success ? '' : strErro);
        });
    },
    addPaper: function (type, options, addPaperListener) {
        var url = Code.URL_POST_ADD_PAPER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                addPaperListener(true, obj, '')
            } else {
                addPaperListener(false, '', strErro)
            }

        });
    },
    editPaper: function (type, options, editPaperListener) {
        var url = Code.URL_POST_EDIT_PAPER;

        Connect_Http.httpDatas(type, url, options, function getBackListener(success, obj, strErro) {
            if (success) {
                editPaperListener(true, obj, '')
            } else {
                editPaperListener(false, '', strErro)
            }

        });
    },




}

