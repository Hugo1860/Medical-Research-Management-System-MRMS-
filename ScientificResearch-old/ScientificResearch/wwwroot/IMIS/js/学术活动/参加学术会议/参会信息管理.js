$(function () {
    window.vm = null;
    if (sessionStorage.userInfo == null) {
        location.href = '/IMIS/views/login.html';
    }
    var userInfo = JSON.parse(sessionStorage.userInfo);
    avalon.ready(function () {
        window.vm = avalon.define({
            $id: 'root',
            req: {
                Index: 1,
                Size: 16,
                Like申请人姓名: '',
                Like会议名称	: '',
                是否反馈过: '',
                OrderType: false
            },
            userInfo:userInfo,
            title: '',
            name:'',
            total: '',
            model: [],
            nothing: false,
            loaded: false,
            editType:false,
            detailsUrl: '',
            query: function () {
                vm.loaded = false;
                $.support.cors = true;
                Meeting.getMeetingList('get', vm.req.$model, function getMeetingListListener(success, obj, strErro) {
                    if (success) {
                        vm.loaded = true;
                        vm.total = obj.total;
                        if (obj == null || obj.list.length == 0) {
                            $('.pager').hide();
                            vm.model = [];
                            vm.nothing = true;
                            return;
                        } else {
                            obj = obj.list;
                            vm.model = obj;
                            $('.pager').show();
                            vm.nothing = false;
                            vm.allchecked = false;
                        }
                        $('.pager').mamPager({
                            pageSize: vm.req.Size,                       //页大小
                            pageIndex: vm.req.Index,                     //当前页
                            recordTotal: vm.total,                       //数据总数
                            type: 1,
                            prevText: "&laquo;",                         //上一页显示内容
                            nextText: "&raquo;",
                            noData: "暂无数据",
                            pageChange: function (index) {
                                vm.req.Index = index;
                                vm.loaded = false;
                                vm.nothing = false;
                                vm.query();
                            }
                        });
                        $('.bs-tooltip').tooltip();
                    } else {
                        console.info('获取讲座列表失败！');
                        console.info(strErro);
                    }
                });

            },
            search: function () {
                vm.req.Index = 1;
                vm.req.Like申请人姓名 = vm.name;
                vm.req.Like会议名称 = vm.title;
                vm.query();
            },
            submit: function () {
                if (event.keyCode == 13) {
                    vm.search();
                }
            },
            change:function () {
                vm.req.是否反馈过 = $('.screen-box .type').val();
                vm.search();
            },
            clickBtnAdd: function () {
                vm.editType = false;
            },
            clickBtnEdit: function (el) {
                vm.editType = true;
                vm.feedback = false;
                sessionStorage.editInfo = JSON.stringify(el.$model);
                var details = {
                    id: el.编号,
                    name: el.姓名,
                    shenHeUrl: el.步骤链接路径,
                    步骤编号: el.步骤编号
                }
                sessionStorage.xueShuDetails = JSON.stringify(details);
            },
            getUrl: function (url) {
                return decodeURI(encodeURI(encodeURI(url)));
            },
            details: function (el) {
                var details = {
                    id: el.编号,
                    name: el.姓名,
                    shenHeUrl: el.步骤链接路径,
                    步骤编号: el.步骤编号
                }
                var detailsUrl = el.流程相关项目路径;
                sessionStorage.xueShuDetails = JSON.stringify(details);
                $('.modal-details .detailsPage').attr('src',detailsUrl);

            },
            getStatusClass: function (name) {
                if (name == '参加会议申请') {
                    return 'btn-edit';
                } else {
                    return 'btn-examine';
                }
            },
            getClass: function (statue) {
                switch (statue) {
                    case '审核通过':
                        return 'state-mark';
                    case '审核不通过':
                        return 'state-overdue';
                    case '审核已终止':
                        return 'state-overdue';
                    case '待审核':
                        return 'state-accomplish';
                    default:
                        return '';
                }
            },
            getFeedbackClass: function (statue) {
                if(statue){
                    return 'btn-feedback';
                }
            },
            clickBtnReturn: function () {
                $('.modal').modal('hide');
            },
        });
        $('.modal-add .modal-body').mCustomScrollbar({
            theme: 'dark-3',
        });
        vm.search();
        avalon.scan(document.body);
    });
});