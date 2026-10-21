$(function () {
    window.vm = null;
    if (sessionStorage.userInfo == null) {
        location.href = '/IMIS/views/login.html';
    }
    avalon.ready(function () {
        window.vm = avalon.define({
            $id: 'root',
            req: {
                Index: 1,
                Size: 16,
                Like名称: '',
                OrderType: true
            },
            total: '',
            model: [],
            arr: [],
            LikeValue: '',
            allchecked: false,
            loaded: false,
            type: false,
            query: function () {
                vm.loaded = false;
                $.support.cors = true;
                ExpenditureTemplate.getTemplateList('get', vm.req.$model, function getTemplateListListener(success, obj, strErro) {
                    if (success) {
                        vm.loaded = true;
                        vm.total = obj.total;
                        if (obj == null || obj.list.length == 0) {
                            $('.pager').hide();
                            vm.model = [];
                            return;
                        } else {
                            obj = obj.list;
                            var number = (vm.req.Index - 1) * vm.req.Size + 1;
                            for (var i = 0; i < obj.length; i++) {
                                obj[i].number = number;
                                obj[i].checked = false;
                                number++;
                            }
                            vm.model = obj;
                            $('.pager').show();
                        }
                        $('.pager').mamPager({
                            pageSize: vm.req.Size,                           //页大小
                            pageIndex: vm.req.Index,                     //当前页
                            recordTotal: vm.total,                      //数据总数
                            type: 1,
                            prevText: "&laquo;",                       //上一页显示内容
                            nextText: "&raquo;",
                            noData: "暂无数据",
                            pageChange: function (index) {
                                vm.req.Index = index;
                                vm.loaded = false;
                                vm.allchecked = false,
                                    vm.query();
                            }
                        });
                        $('.bs-tooltip').tooltip();
                    } else {
                        console.info('获取' + mCategory + '列表失败！');
                        console.info(strErro);
                    }
                });

            },
            search: function () {
                vm.req.Index = 1;
                vm.req.Like名称= vm.LikeValue;
                vm.query();
            },
            submit: function () {
                if (event.keyCode == 13) {
                    vm.search();
                }
            },
            checkAll: function (e) {
                var checked = e.target.checked

                vm.model.forEach(function (el) {
                    el.checked = checked
                })
            },
            checkOne: function (e) {
                var checked = e.target.checked
                if (checked === false) {
                    vm.allchecked = false
                } else {
                    vm.allchecked = vm.model.every(function (el) {
                        return el.checked
                    })
                }
            },
            clickBtnAdd: function () {
                vm.type = false;
            },
            clickBtnEdit: function (el) {
                vm.type = true;
                sessionStorage.mTemplate = JSON.stringify(el.$model);
            },
            clickBtnDel: function (el) {
                vm.arr = [];
                $('.modal-del .modal-title').text('您确定要执行删除操作吗？');
                vm.arr.push(el.编号);
            },
            batchDel: function () {
                $('.modal-del .modal-title').text('您确定要删除已选项吗？');
                vm.arr = [];
                for (var i = 0; i < vm.model.length; i++) {
                    if (vm.model[i].checked) {
                        vm.arr.push(vm.model[i].编号);
                    }
                }
                if (vm.arr.length > 0) {
                    $('.modal-del').modal('show');
                } else {
                    $.oaNotify.error('请先勾选！');
                }
            },
            confirmDel: function (arr) {
                console.info(arr);
                ExpenditureTemplate.delTemplate('post', arr.$model, function delTemplateListener(success, obj, strErro) {
                    postBack(success, strErro, '删除成功！', '删除失败：', '.modal-del',function callBack() {
                        vm.query();
                    });
                });
            },
            clickBtnReturn: function () {
                $('.modal').modal('hide');
            },
            getUrl: function (url) {
                return decodeURI(encodeURI(encodeURI(url)));
            }
        })

        vm.query();

        avalon.scan(document.body);
    });
});