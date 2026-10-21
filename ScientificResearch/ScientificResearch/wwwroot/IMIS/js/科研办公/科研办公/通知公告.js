$(function () {
    window.vm = null;
    if (sessionStorage.userInfo == null) {
        location.href = '/IMIS/views/login.html';
    }
    var userId = JSON.parse(sessionStorage.userInfo).人员.编号;
    avalon.ready(function () {
        window.vm = avalon.define({
            $id: 'root',
            req: {
                Index: 1,
                Size: 16,
                通知类型: '',
                Like通知名称: '',
                Like通知内容: '',
                Like发送人姓名: '',
                OrderType: false
            },
            type: '',
            name: '',
            tName: '',
            content: '',
            receive: '',
            total: '',
            model: [],
            arr: {},
            text: '',
            deepartmentId:'',
            department:'',
            editType: false,
            nothing: false,
            loaded: false,
            allchecked: false,
            postName:'',
            query: function () {
                vm.loaded = false;
                $.support.cors = true;
                Notice.getNoticeList('get', vm.req.$model, function getNoticeListListener(success, obj, strErro) {
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
                            for (var i = 0; i < obj.length; i++) {
                                obj[i].checked = false;
                            }
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
                        console.info('获取通知公告列表失败！');
                        console.info(strErro);
                    }
                });

            },
            search: function () {
                vm.req.Index = 1;
                vm.req.Like发送人姓名 = vm.name;
                vm.req.Like通知名称 = vm.tName;
                vm.req.Like通知内容 = vm.content;
                vm.query();
            },
            submit: function () {
                if (event.keyCode == 13) {
                    vm.search();
                }
            },
            change: function () {
                vm.req.通知类型 = $('.screen-box .type').val();
                vm.search();
            },
            clickBtnAdd: function () {
                vm.editType = false;
                vm.deepartmentId = '';
                vm.department = '请选择';
                sessionStorage.noticeId = '';
            },
            clickBtnEdit: function (el) {
                vm.editType = true;
                vm.deepartmentId = el.发送人部门编号;
                vm.department = el.发送人部门名称;
                sessionStorage.noticeId = JSON.stringify(el.编号);
            },
            getUrl: function (url) {
                return decodeURI(encodeURI(encodeURI(url)));
            },
            details: function (id) {
                sessionStorage.noticeId = JSON.stringify(id);
            },
            getStateClass: function (state) {
                switch (state) {
                    case 1:
                        return 'not-received';
                }
            },
            getStatusClass: function (status) {
                if (!status)
                    return 'disable';
            },
            clickName: function (index) {
                $('table tbody tr:eq(' + index + ')').removeClass('not-received');
            },
            clickBtnDisable: function (el) {
                vm.arr = {
                    编号: el.编号,
                    发送人编号: el.发送人编号,
                    是否启用: el.是否启用
                }
                if (el.是否启用) {
                    vm.text = '禁用';
                    vm.arr.是否启用 = false;
                } else {
                    vm.text = '启用';
                    vm.arr.是否启用 = true;
                }
            },
            confirmDel: function () {
                var textSuccess;
                var textStrErro;
                if (vm.arr.是否启用) {
                    textSuccess = '启用成功！';
                    textStrErro = '启用失败：';
                } else {
                    textSuccess = '禁用成功！';
                    textStrErro = '禁用失败：';
                }
                Notice.enableOrDisableNotice('post', vm.arr.$model, function enableOrDisableNoticeListener(success, obj, strErro) {
                    postBack(success, strErro, textSuccess, textStrErro, '.modal-disable', function callBack() {
                        vm.query();
                    });
                });
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