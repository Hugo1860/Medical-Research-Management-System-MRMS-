$(function () {
    window.vm = null;
    if (sessionStorage.userInfo == null) {
        location.href = '/IMIS/views/login.html';
    }
    window.mUserInfo = JSON.parse(sessionStorage.userInfo);
    window.mUserId = mUserInfo.人员.编号;
    avalon.ready(function () {
        window.vm = avalon.define({
            $id: 'root',
            total: '',
            notice: [],
            reqNotice:{
                Index: 1,
                Size: 10,
                人员编号: mUserId,
                OrderType: false
            },
            downFile: [],
            reqdownFile: {
                Index: 1,
                Size: 10,
                Like文件名称: '',
                Like文件类型: '',
                Like上传人姓名: '',
                是否启用: true,
                OrderType: false
            },
            noticeTotal:'',
            downFileTotal:'',
            workbench: {
                待办数量: 0,
                未读通知数量: 0,
                已发起流程数量: 0,
                最近待办事项: '',
                角色名称: '',
                下一步办理人: '',
                下一步办理角色: '',
                待办提示: '当前没有待办事项',
                下一步流程名称: '',
                下一步状态说明: '',
                缺件提示: '',
                缺件数: 0
            },
            nothing:false,
            nothing2:false,
            getUserNoticeList:function () {
                Notice.getUserNoticeList('get', vm.reqNotice.$model, function getUserNoticeListListener(success, obj, strErro) {
                    if (success) {
                        vm.noticeTotal = obj.total;
                        if (obj == null || obj.list.length == 0) {
                            $('.notice .pager').hide();
                            vm.notice = [];
                            vm.nothing = true;
                            return;
                        } else {
                            obj = obj.list;
                            vm.notice = obj;
                            $('.notice .pager').show();
                            vm.nothing = false;
                        }
                        $('.notice .pager').mamPager({
                            pageSize: vm.reqNotice.Size,                       //页大小
                            pageIndex: vm.reqNotice.Index,                     //当前页
                            recordTotal: vm.noticeTotal,                       //数据总数
                            type: 1,
                            prevText: "&laquo;",                         //上一页显示内容
                            nextText: "&raquo;",
                            noData: "暂无数据",
                            pageChange: function (index) {
                                vm.reqNotice.Index = index;
                                vm.nothing = false;
                                vm.getUserNoticeList();
                            }
                        });
                        $('.bs-tooltip').tooltip();
                    } else {
                        console.info('获取通知公告列表失败！');
                        console.info(strErro);
                    }
                });
            },
            getEnableDownloadList:function () {
                Download.getEnableDownloadList('get', vm.reqdownFile.$model, function getEnableDownloadListListener(success, obj, strErro) {
                    if (success) {
                        vm.downFileTotal = obj.total;
                        if (obj == null || obj.list.length == 0) {
                            $('.doc-download .pager').hide();
                            vm.downFile = [];
                            vm.nothing2 = true;
                            return;
                        } else {
                            obj = obj.list;
                            vm.downFile = obj;
                            $('.doc-download .pager').show();
                            vm.nothing2 = false;
                        }
                        $('.doc-download .pager').mamPager({
                            pageSize: vm.reqdownFile.Size,                       //页大小
                            pageIndex: vm.reqdownFile.Index,                     //当前页
                            recordTotal: vm.downFileTotal,                       //数据总数
                            type: 1,
                            prevText: "&laquo;",                         //上一页显示内容
                            nextText: "&raquo;",
                            noData: "暂无数据",
                            pageChange: function (index) {
                                vm.reqdownFile.Index = index;
                                vm.nothing2 = false;
                                vm.getEnableDownloadList();
                            }
                        });
                        $('.bs-tooltip').tooltip();
                    } else {
                        console.info('获取下载列表失败！');
                        console.info(strErro);
                    }
                });
            },
            getStateClass: function (state) {
                switch (state) {
                    case 1:
                        return 'not-received';
                }
            },
            noticeDetails: function (id) {
                sessionStorage.noticeId = JSON.stringify(id);
            },
            downloadDetails: function (el) {
                sessionStorage.downloadDetails = JSON.stringify(el);
            },
            getWorkbenchSummary: function () {
                Workbench.getSummary('get', {}, function (success, obj, strErro) {
                    if (success && obj) {
                        vm.workbench = obj;
                    } else {
                        console.info('获取工作台摘要失败！');
                        console.info(strErro);
                    }
                });
            },
            clickBtnReturn: function () {
                $('.modal').modal('hide');
            },
        });
        vm.getUserNoticeList();
        vm.getEnableDownloadList();
        vm.getWorkbenchSummary();

        avalon.scan(document.body);
    });
});
