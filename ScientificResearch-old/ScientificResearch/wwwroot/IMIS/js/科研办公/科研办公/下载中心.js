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
                Like文件名称: '',
                Like文件类型: '',
                Like上传人姓名: '',
                是否启用: '',
                OrderType: false
            },
            type: '',
            name: '',
            fileName: '',
            total: '',
            model: [],
            arr: {},
            text: '',
            userInfo:userInfo,
            editType: false,
            nothing: false,
            loaded: false,
            allchecked: false,
            postName: '',
            query: function () {
                vm.loaded = false;
                $.support.cors = true;
                Download.getDownloadList('get', vm.req.$model, function getDownloadListListener(success, obj, strErro) {
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
                        console.info('获取下载列表失败！');
                        console.info(strErro);
                    }
                });

            },
            search: function () {
                vm.req.Index = 1;
                vm.req.Like文件名称 = vm.fileName;
                vm.req.Like文件类型 = vm.type;
                vm.req.Like上传人姓名 = vm.name;
                vm.query();
            },
            submit: function () {
                if (event.keyCode == 13) {
                    vm.search();
                }
            },
            changeType: function () {
                vm.type = $('.screen-box .type').val()
                vm.search();
            },
            changeStatue: function () {
                vm.req.Index = 1;
                vm.req.是否启用 = $('.screen-box .statue').val();
                vm.search();
            },
            clickBtnAdd: function () {
                vm.editType = false;
            },
            clickBtnEdit: function (el) {
                vm.editType = true;
                sessionStorage.downloadDetails = JSON.stringify(el);
            },
            getUrl: function (url) {
                return decodeURI(encodeURI(encodeURI(url)));
            },
            details: function (el) {
                sessionStorage.downloadDetails = JSON.stringify(el);
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
                Download.enableOrDisableDownload('post', vm.arr.$model, function enableOrDisableDownloadListener(success, obj, strErro) {
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
        vm.query();
        avalon.scan(document.body);
    });
});