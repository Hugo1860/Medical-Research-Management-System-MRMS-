$(function () {
    window.vm = null;
    if (sessionStorage.userInfo == null) {
        location.href = '/IMIS/views/login.html';
    }
    var zTreeObj;
    var treenode;
    var newtree;
    avalon.ready(function () {
        window.vm = avalon.define({
            $id: 'root',
            req: {
                上级部门编号: 0,
                department: ''
            },
            reqUser: {
                Index: 1,
                Size: 16,
                部门编号: '',
                Like姓名: '',
                是否启用: ''
            },
            departmentName: '',
            name: '',
            delType: false,
            model: [],
            total: '',
            total2: '',
            LikeValue: '',
            nodes: [],
            enableOrDisable: false,
            addOrEdit: false,
            departmentId: '',
            addType: false,
            title: '',
            departmentInfo: {
                编号: 0,
                上级部门编号: 0,
                名称: '',
                负责人编号: 0,
                负责人姓名: '',
                是否启用: true
            },
            userId: '',
            status: false,
            searchUser: {
                Index: 1,
                Size: 8,
                部门编号: '',
                Like姓名: '',
                是否启用: true
            },
            users: [],
            disableType: false,
            allchecked: false,
            getDepartment: function () {
                var setting = {
                    async: {
                        enable: true,
                        url: getUrl,
                        contentType: 'application/json',
                        autoParam: [],
                        dataType: 'json',
                        type: 'get',
                        dataFilter: filter
                    },
                    view: {
                        expandSpeed: "",
                        addHoverDom: addHoverDom,
                        removeHoverDom: removeHoverDom,
                        selectedMulti: true,
                    },
                    edit: {
                        enable: true,
                        showRemoveBtn: true,
                        showRenameBtn: true,
                        removeTitle: "禁用或启用",
                        renameTitle: "修改"
                    },
                    data: {
                        simpleData: {
                            enable: true,
                            idKey: "id",
                            pIdKey: "pId",
                        },
                        key: {
                            name: "name"
                        }
                    },
                    callback: {
                        beforeDrag: zTreeBeforeDrag,     //全部禁止拖拽
                        onClick: nodeOnClick,            //节点被点击的事件回调函数
                        beforeExpand: zTreeBeforeExpand, //父节点展开之前的事件回调函数
                        beforeRemove: beforeRemove,      //节点被删除之前的事件回调函数
                        beforeEditName: beforeEditName,  //节点编辑按钮的 click 事件
                        onAsyncSuccess: onAsyncSuccess   //异步加载正常结束的事件回调函数
                    }
                };

                function getUrl() {
                    var param = '?上级部门编号=' + vm.req.上级部门编号;
                    var url = encodeURI(encodeURI(Code.URL_GET_DEPARTMENT_LIST + param));
                    return decodeURI(url);
                }

                function zTreeBeforeExpand(treeId, treeNode) {
                    vm.req.上级部门编号 = treeNode.编号;
                    return vm.req.上级部门编号;
                };

                function zTreeBeforeDrag(treeId, treeNodes) {
                    return false;
                };

                function filter(treeId, parentNode, childNodes) {
                    if (!childNodes) return null;
                    childNodes = childNodes.data;
                    for (var i = 0, l = childNodes.length; i < l; i++) {

                        childNodes[i].id = childNodes[i].编号;
                        childNodes[i].name = childNodes[i].名称.replace(/\.n/g, '.');
                        childNodes[i].pId = childNodes[i].上级部门编号;

                        if (childNodes[i].是否存在下级 == 1) {
                            childNodes[i].isParent = true;
                        } else {
                            childNodes[i].isParent = false;
                        }
                    }
                    return childNodes;
                }

                function beforeEditName(treeId, treeNode, newName) {
                    newtree = $.fn.zTree.getZTreeObj("treeDemo");
                    newtree.selectNode(treeNode);
                    treenode = treeNode;
                    vm.req.上级部门编号 = treeNode.编号;
                    vm.addOrEdit = false;
                    vm.title = '修改部门';
                    vm.departmentInfo = {
                        编号: treeNode.编号,
                        上级部门编号: treeNode.上级部门编号,
                        名称: treeNode.名称,
                        负责人编号: treeNode.负责人编号,
                        负责人姓名: treeNode.负责人姓名,
                        是否启用: treeNode.是否启用,
                    };
                    $('.modal-addDP').modal('show');
                    vm.getDepartmentUsers();
                    return false;
                }

                function nodeOnClick(e, treeId, treeNode) {
                    vm.req.上级部门编号 = treeNode.编号;
                    zTreeObj.checkNode(treeNode, !treeNode.checked, true);
                    treenode = treeNode;
                    vm.reqUser.部门编号 = treeNode.编号;
                    vm.departmentName = treeNode.名称;
                    vm.name = '';
                    vm.reqUser.Like姓名 = '';
                    vm.getusers();
                }

                function addHoverDom(treeId, treeNode) {
                    var sObj = $("#" + treeNode.tId + "_span");
                    if (treeNode.editNameFlag || $("#addBtn_" + treeNode.tId).length > 0) return;
                    var addStr = "<span class='button add' id='addBtn_" + treeNode.tId
                        + "' title='添加节点' onfocus='this.blur();'></span>";
                    sObj.after(addStr);

                    if (treeNode.是否启用) {
                        sObj.parent().removeClass('disable').find('.remove').attr('title', '禁用');

                    } else {
                        sObj.parent().addClass('disable').find('.remove').attr('title', '启用');
                    }
                    var addBtn = $("#addBtn_" + treeNode.tId);
                    if (addBtn) addBtn.bind("click", function () {
                        newtree = $.fn.zTree.getZTreeObj("treeDemo");

                        vm.addOrEdit = true;
                        vm.addType = true;
                        vm.title = '添加部门';
                        vm.departmentInfo = {
                            编号: 0,
                            上级部门编号: 0,
                            名称: '',
                            负责人编号: 0,
                            负责人姓名: '',
                            是否启用: true
                        };
                        if (vm.addType) {
                            vm.departmentInfo.上级部门编号 = treeNode.编号;
                        }
                        $('.modal-addDP').modal('show');
                        vm.getDepartmentUsers();
                    });
                };

                function removeHoverDom(treeId, treeNode) {
                    $("#addBtn_" + treeNode.tId).unbind().remove();
                };

                function beforeRemove(treeId, treeNode) {
                    if (!treeNode.checked) {
                        zTreeObj.checkNode(treeNode, !treeNode.checked, true);
                        vm.getusers();
                    }
                    vm.enableOrDisable = treeNode.是否启用;
                    vm.departmentId = treeNode.编号;

                    treenode = treeNode;
                    newtree = $.fn.zTree.getZTreeObj("treeDemo");
                    newtree.selectNode(treeNode);
                    var text;
                    if (treeNode.是否启用) {
                        text = '禁用';
                    } else {
                        text = '启用'
                    }
                    $('.modal-disable').modal('show');
                    $('.modal-disable .modal-body p').text('确定' + text + '部门【 ' + treeNode.name + '】吗？');
                    vm.disableType = false;
                    return false;
                }

                function onAsyncSuccess(event, treeId, treeNode, msg) {
                    newtree = $.fn.zTree.getZTreeObj("treeDemo");
                    var nodes = newtree.getNodesByParam("name", vm.departmentInfo.名称, null);
                    newtree.selectNode(nodes[0]);
                }

                $(document).ready(function () {
                    zTreeObj = $.fn.zTree.init($("#treeDemo"), setting);
                    zTreeObj.expandAll(true);
                });
            },
            searchDepartment: function () {
                var treeObj = $.fn.zTree.getZTreeObj("treeDemo");
                var nodes = treeObj.getNodesByParamFuzzy("name", vm.req.department, null);
                treeObj.selectNode(nodes[0]);
            },
            submitDepartment: function () {
                if (event.keyCode == 13) {
                    vm.searchDepartment();
                }
            },
            reAsyncChildNodes: function (treeNode, zTree, newNode) {
                if ((!treeNode && event.target.tagName.toLowerCase() != "button" && $(event.target).parents("a").length == 0) || treeNode.zAsync) {
                    treeNode = zTreeObj.addNodes(treeNode, newNode);
                    var nodes = zTreeObj.getSelectedNodes();
                    nodes[0].isParent = true;
                    zTree.reAsyncChildNodes(nodes[0], 'refresh', false);
                } else {
                    zTree.reAsyncChildNodes(treeNode, "refresh");
                }
            },
            updateNode: function (treeNode, zTree, newNode) {
                treeNode.是否启用 = vm.departmentInfo.是否启用;
                var sObj = $("#" + treeNode.tId + "_span");
                if (treeNode.是否启用) {
                    sObj.parent().removeClass('disable').find('.remove').attr('title', '禁用');

                } else {
                    sObj.parent().addClass('disable').find('.remove').attr('title', '启用');
                }
                zTree.updateNode(treeNode);
            },

            clickSubmit: function () {
                var icon;
                if (vm.departmentInfo.名称 == '') {
                    icon = 'icon-shibai1';
                    popover(icon, '提交失败：部门名称不能为空！');
                    setTimeout("$('.popover').remove()", 2000);
                } else {
                    if (vm.addOrEdit) {
                        vm.addDepartment(vm.departmentInfo);
                    } else {
                        vm.editDepartment(vm.departmentInfo);
                    }
                }
            },
            addDepartment: function (data) {
                var newNode = {
                    id: 0,
                    编号: 0,
                    tId: treenode.tId,
                    pId: treenode.编号,
                    上级部门编号: treenode.编号,
                    name: vm.departmentInfo.名称,
                    名称: vm.departmentInfo.名称,
                    负责人编号: 0,
                    负责人姓名: '',
                    是否启用: true,
                }
                if (vm.addType) {
                    newNode.上级部门编号 = treenode.上级部门编号;
                    newNode.pId = treenode.pId;
                } else {
                    newNode.上级部门编号 = 0;
                    newNode.pId = 0;
                }

                Department.addOrEditDepartment('post', data, function addOrEditDepartmentListener(success, obj, strErro) {
                    postBack(success, strErro, '添加成功！', '添加失败：', '.modal-addDP', function callBack() {
                        vm.reAsyncChildNodes(treenode, newtree, newNode);
                    });
                });
            },
            editDepartment: function (data) {
                Department.addOrEditDepartment('post', data, function addOrEditDepartmentListener(success, obj, strErro) {
                    postBack(success, strErro, '修改成功！', '修改失败：', '.modal-addDP', function callBack() {
                        treenode.name = vm.departmentInfo.名称;
                        treenode.名称 = vm.departmentInfo.名称;
                        treenode.负责人编号 = vm.departmentInfo.负责人编号;
                        treenode.负责人姓名 = vm.departmentInfo.负责人姓名;
                        newtree.updateNode(treenode);
                    });
                });
            },
            enableDepartment: function (id) {
                Department.enableDepartment('post', id, function enableDepartmentListener(success, obj, strErro) {
                    postBack(success, strErro, '启用成功！', '启用失败：', '.modal-disable', function callBack() {
                        vm.updateNode(treenode, newtree, '')
                    });
                });
            },
            DisableDepartment: function (id) {
                Department.DisableDepartment('post', id, function DisableDepartmentListener(success, obj, strErro) {
                    postBack(success, strErro, '禁用成功！', '禁用失败：', '.modal-disable', function callBack() {
                        vm.updateNode(treenode, newtree, '')
                    });
                });
            },
            departmentDisable: function (id) {
                if (vm.enableOrDisable) {
                    vm.DisableDepartment(id)
                    vm.departmentInfo.是否启用 = false;
                } else {
                    vm.enableDepartment(id);
                    vm.departmentInfo.是否启用 = true;
                }
            },
            getusers: function () {
                User.getPagingUsersList('get', vm.reqUser.$model, function (success, obj, strErro) {
                    if (success) {
                        vm.total = obj.total;
                        if (obj == null || obj.list.length == 0) {
                            $('.pager').hide();
                            vm.model = [];
                            return;
                        } else {
                            obj = obj.list;
                            var number = (vm.reqUser.Index - 1) * vm.reqUser.Size + 1;
                            for (var i = 0; i < obj.length; i++) {
                                obj[i].number = number;
                                obj[i].checked = false;
                                number++;
                            }
                            vm.model = obj;
                            console.info(vm.model);
                            $('.pager').show();
                        }
                        $('.pager').mamPager({
                            pageSize: vm.reqUser.Size,                           //页大小
                            pageIndex: vm.reqUser.Index,                     //当前页
                            recordTotal: vm.total,                      //数据总数
                            type: 1,
                            prevText: "&laquo;",                       //上一页显示内容
                            nextText: "&raquo;",
                            noData: "暂无数据",
                            pageChange: function (index) {
                                vm.reqUser.Index = index;
                                vm.allchecked = false,
                                    vm.getusers();
                            }
                        });
                        $('.user-statue').on('change', function () {
                            vm.reqUser.是否启用 = $(this).children('option:selected').val();
                            vm.req.Index = 1;
                            vm.getusers();
                        });
                        $('.bs-tooltip').tooltip();
                    } else {
                        console.info('获取用户列表失败！');
                        console.info(strErro);
                    }
                })
            },
            addBtnDP: function () {
                vm.addType = false;
                vm.addOrEdit = true;
                newtree = $.fn.zTree.getZTreeObj("treeDemo");

                vm.title = '添加部门';
                vm.departmentInfo = {
                    编号: 0,
                    上级部门编号: 0,
                    名称: '',
                    负责人编号: 0,
                    负责人姓名: '',
                    是否启用: true,
                    是否存在下级: 0
                }

                vm.getDepartmentUsers();
            },

            search: function () {
                vm.reqUser.Index = 1;
                vm.reqUser.Like姓名 = vm.name;
                vm.getusers();
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
                vm.addTyoe = false;
                sessionStorage.addUerType = JSON.stringify(0);
                var userType = {
                    type: false,
                    infoBack: true,
                    btnBack: false
                }
                sessionStorage.userType = JSON.stringify(userType);
            },
            clickBtnEdit: function (el) {
                sessionStorage.mUsers = JSON.stringify(el.$model);
                sessionStorage.addUerType = JSON.stringify(0);
                var userType = {
                    type: true,
                    infoBack: true,
                    btnBack: false
                }
                sessionStorage.userType = JSON.stringify(userType);
            },
            getUrl: function (url) {
                return decodeURI(encodeURI(encodeURI(url)));
            },
            getStatusClass: function (status) {
                if (!status)
                    return 'disable';
            },
            clickBtnDisable: function (el) {
                vm.userId = el.编号;
                vm.status = el.是否启用;
                var name = el.姓名;
                vm.disableType = true;
                if (el.是否启用) {
                    $('.modal-disable .modal-body p').text('您确定要禁用【' + name + '】吗？');
                } else {
                    $('.modal-disable .modal-body p').text('您确定要启用【' + name + '】吗？');
                }
            },
            userDisable: function (id) {
                if (vm.status) {
                    vm.DisableUsers(id);
                } else {
                    vm.enableUsers(id);
                }
            },
            enableUsers: function (id) {
                User.enableUsers('post', id, function enableDepartmentListener(success, obj, strErro) {
                    postBack(success, strErro, '启用成功！', '启用失败', '.modal-disable', function callBack() {
                        vm.getusers();
                    })
                })
            },
            DisableUsers: function (id) {
                User.DisableUsers('post', id, function enableDepartmentListener(success, obj, strErro) {
                    postBack(success, strErro, '禁用成功！', '禁用失败', '.modal-disable', function callBack() {
                        vm.getusers();
                    })
                })
            },
            initMultiselect: function (obj) {
                $(obj).multiselect({
                    multiple: true,
                    buttonWidth: '100%',
                    maxHeight: 300,
                    noneSelectedText: '请选择',
                    enableFiltering: true,//是否显示搜索
                    filterPlaceholder: '输入关键字...',
                    onChange: function (option, checked, select) {
                        vm.departmentInfo.负责人编号 = $(option).val();
                        vm.departmentInfo.负责人姓名 = $(option).text();
                        $('.multiselect-container.dropdown-menu').hide();
                    }
                });
            },
            getDepartmentUsers: function () {
                User.getPagingUsersList('get', vm.searchUser.$model, function (success, obj, strErro) {
                    if (success) {
                        vm.total2 = obj.total;
                        obj = obj.list;
                        if (obj.length == 0) {
                            obj = [{姓名: '无数据', 编号: 0}];
                        }
                        vm.initMultiselect('#users');

                        var options = [];
                        for (var i in obj) {
                            var option = {
                                label: obj[i].姓名,
                                title: obj[i].姓名,
                                value: obj[i].编号
                            }
                            options.push(option);
                        }

                        $('#users').multiselect('dataprovider', options);
                        if (vm.departmentInfo.负责人编号 > 0) {
                            $('#users').multiselect('select', vm.departmentInfo.负责人编号);
                        }
                        $('.multiselect-search').val(vm.searchUser.Like姓名);
                        $('.multiselect-search').focus();

                        var pager = $('<li><div class="page paging text-center">' +
                            '<div class="pager paging"></div></div></li>');


                        $('.add-department .page').remove();
                        $('.multiselect-container.dropdown-menu').append(pager);
                        $('.multiselect-search').on('keyup', debounce(function () {
                            vm.searchUser.Like姓名 = $(this).val();
                            vm.searchUser.Index = 1;
                            vm.getDepartmentUsers();
                        }, 500));


                        $('.modal-addDP .pager').mamPager({
                            pageSize: vm.searchUser.Size,                           //页大小
                            pageIndex: vm.searchUser.Index,                     //当前页
                            recordTotal: vm.total2,                      //数据总数
                            type: 1,
                            prevText: "&laquo;",                       //上一页显示内容
                            nextText: "&raquo;",
                            noData: "暂无数据",
                            pageChange: function (index) {
                                vm.searchUser.Index = index;
                                vm.getDepartmentUsers();
                            }
                        });

                        $('.bs-tooltip').tooltip();
                    } else {
                        console.info('获取用户列表失败！');
                        console.info(strErro);
                    }
                })
            },
            clickUserName: function () {
                $('.multiselect-container.dropdown-menu').show();
                vm.searchUser.Like姓名 = '';
                $('body').on('click', function () {
                    $('.multiselect-container.dropdown-menu').hide();
                });
            },
            clickBtnReturn: function () {
                $('.modal').modal('hide');
                $('.multiselect-container.dropdown-menu').hide();
            },
            details: function () {
                sessionStorage.mkeyandetails = true;
            }
        });
        $('.tree-box').mCustomScrollbar({
            theme: 'dark-3'
        });
        vm.getDepartment();

        avalon.scan(document.body);
    });
});