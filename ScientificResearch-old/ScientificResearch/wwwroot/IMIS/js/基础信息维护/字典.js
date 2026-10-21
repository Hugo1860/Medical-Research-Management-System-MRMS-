$(function () {
    window.vm = null;
    if (sessionStorage.userInfo == null) {
        location.href = '/IMIS/views/login.html';
    }
    window.mCategory = oa.getUrlParam('name');
    window.mCName = oa.getUrlParam('cname');
    window.mType = oa.getUrlParam('type');
    avalon.ready(function () {
        window.vm = avalon.define({
            $id: 'root',
            req: {
                Index: 1,
                Size: 16,
                分类: mCategory,
                Like值: '',
                OrderType: true
            },
            total: '',
            model: [],
            arr: [],
            LikeValue: '',
            allchecked: false,
            loaded: false,
            classType: mType,
            addTyoe: false,
            type: false,
            query: function () {
                vm.loaded = false;
                $.support.cors = true;
                Dictionary.getPagingDictionaryList('get', vm.req.$model, function getPagingDictionaryListListener(success, obj, strErro) {
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
                                vm.allchecked = false;
                                vm.query();
                            }
                        });
                        $('.bs-tooltip').tooltip();
                    } else {
                        console.info('获取' + mCategory + '列表失败！');
                        console.info(strErro);
                    }
                    var href = $('.btn-next a').attr('href');
                    $('.btn-next a').attr('href', encodeURI(href));
                });

            },
            search: function () {
                vm.req.Index = 1;
                vm.req.Like值 = vm.LikeValue;
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
                vm.addTyoe = false;
            },
            clickBtnEdit: function (el) {
                vm.type = true;
                sessionStorage.mDictionary = JSON.stringify(el.$model);
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
                Dictionary.delDictionary('post', arr.$model, function delDictionaryListener(success, obj, strErro) {
                    postBack(success, strErro, '删除成功！', '删除失败：', '.modal-del', function callBack() {
                        vm.query();
                    });
                });
            },
            clickBtnNext: function (el) {
                var data = {
                    name: el.值,
                    number: el.编号,
                    classType: vm.classType,
                    级别: el.级别,
                    mCName: mCName
                }
                sessionStorage.topInfo = JSON.stringify(data);
            },
            clickBtnReturn: function () {
                $('.modal').modal('hide');
            }
        })

        vm.query();

        avalon.scan(document.body);
    });
});