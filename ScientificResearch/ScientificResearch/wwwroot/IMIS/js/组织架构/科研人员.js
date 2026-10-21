$(function () {
    window.vm = null;
    if (sessionStorage.userInfo == null) {
        location.href = '/IMIS/views/login.html';
    }
    var departmentTree=null;
    avalon.ready(function () {
        window.vm = avalon.define({
            $id: 'root',
            req: {
                Index: 1,
                Size: 16,
                LikeAllName: '',
                部门编号: '',
                是否启用: '',
                OrderType: false
            },
            name: '',
            departmentId: '',
            department:'',
            userId:'',
            status: false,
            disableType:false,
            total: '',
            model: [],
            arr:[],
            nothing: false,
            loaded: false,
            allchecked: false,
            query: function () {
                vm.loaded = false;
                $.support.cors = true;
                ScientificResearch.getScientificResearchUsers('get', vm.req.$model, function getScientificResearchUsersListener(success, obj, strErro) {
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
                        $('.user-statue').on('change', function () {
                            vm.req.是否启用 = $(this).children('option:selected').val();
                            vm.search();
                        });
                        $('.bs-tooltip').tooltip();
                    } else {
                        console.info('获取科研人员列表失败！');
                        console.info(strErro);
                    }
                });

            },
            search: function () {
                vm.req.Index = 1;
                vm.req.LikeAllName = vm.name;
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
                sessionStorage.addUerType = JSON.stringify(2);
                var userType = {
                    type: false,
                    infoBack: true,
                    btnBack:false
                }
                sessionStorage.userType = JSON.stringify(userType);
            },
            clickBtnEdit: function (el) {
                sessionStorage.mUsers = JSON.stringify(el.$model);
                sessionStorage.addUerType = JSON.stringify(2);
                var userType = {
                    type: true,
                    infoBack: true,
                    btnBack:false
                }
                sessionStorage.userType = JSON.stringify(userType);
            },
            getUrl: function (url) {
                return decodeURI(encodeURI(encodeURI(url)));
            },
            details:function () {
                sessionStorage.mkeyandetails = true;
            },
            getStatusClass: function (status) {
                if (!status)
                    return 'disable';
            },
            getTheDeparments: function () {
                var setting = {
                    data: {
                        key: {
                            name: 'name'
                        },
                        simpleData: {
                            enable: true,
                            idKey: 'id',
                            pIdKey: 'pId',
                            rootPId: 0
                        },
                    },
                    callback: {
                        onClick: onClick
                    }
                };

                var nodes = [];
                Department.getEnableDepartmentList('get','' , function getEnableDepartmentListListener(success, obj, strErro) {
                    if (success) {
                        obj = obj.reverse();
                        if (obj != null) {
                            for (var i = 0; i < obj.length; i++) {
                                obj[i].id = obj[i].编号;
                                obj[i].pId = obj[i].上级部门编号;
                                obj[i].name = obj[i].名称;
                            }

                            nodes = obj;
                            departmentTree = $.fn.zTree.init($('.departmentTree'), setting, nodes);
                        }
                    } else {
                        alert('获取部门列表数据失败');
                        console.info(strErro);
                    }
                });

                function onClick(e, treeId, treeNode) {
                    var nodes = departmentTree.getSelectedNodes(),
                        text = '';
                    nodes.sort(function compare(a, b) {
                        return a.id - b.id;
                    });
                    for (var i = 0, l = nodes.length; i < l; i++) {
                        text += nodes[i].name + ",";
                    }
                    if (text.length > 0) text = text.substring(0, text.length - 1);
                    var cityObj = $('.screen-box .department');
                    cityObj.val(text);
                    vm.req.部门编号	 = treeNode.编号;
                    vm.department = treeNode.名称;
                    vm.hideMenu();
                    vm.query();
                }
            },
            hideMenu: function () {
                $('.screen-box #menuContent').fadeOut('fast');
                $('body').unbind('mousedown', vm.onBodyDown());
            },
            onBodyDown: function () {
                if (!('.screen-box #menuContent').length > 0) {
                    vm.hideMenu();
                }
            },
            showMenu: function () {
                var obj = $('.screen-box .department');
                var offset = $('.screen-box .department').offset();
                $('.screen-box #menuContent').css({
                    left: offset.left + 'px',
                    top: offset.top + obj.outerHeight() + 'px'
                }).slideDown('fast');

                $('body').bind('mousedown', vm.onBodyDown());
                $('.screen-box .btn-del').show();
            },
            delInput: function () {
                vm.department = '';
                vm.req.部门编号 = '';
                vm.query();
                vm.hideMenu();
            },
            batchDel: function () {
                vm.arr = [];
                for (var i = 0; i < vm.model.length; i++) {
                    if (vm.model[i].checked) {
                        vm.arr.push(vm.model[i].编号);
                    }
                }
                if (vm.arr.length == 0) {
                    $.oaNotify.error('没有已选项！');
                } else {
                    $('.modal-del').modal('show');
                    $('.modal-del .modal-body p').text('您确定要删除已选用户吗？');
                }

            },
            clickBtnDel: function (el) {
                vm.arr = [];
                vm.arr.push(el.编号);
                var name = el.姓名;
                $('.modal-del .modal-body p').text('您确定要删除【' + name + '】吗？');
            },
            userDel: function (arr) {
                vm.delUser(arr);
            },
            delUser: function (arr) {
                ScientificResearch.delScientificResearchUsers('post', arr, function delScientificResearchUsersListener(success, obj, strErro) {
                    postBack(success, strErro, '删除成功！', '删除失败', '.modal-del', function callBack() {
                        vm.query();
                    })
                })
            },
            clickBtnReturn: function () {
                $('.modal').modal('hide');
            }
        });
        vm.search();
        vm.getTheDeparments();
        avalon.scan(document.body);
    });
});