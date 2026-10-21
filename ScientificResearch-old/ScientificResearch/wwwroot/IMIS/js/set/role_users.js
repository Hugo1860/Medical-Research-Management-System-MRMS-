$(function () {
    var number = JSON.parse(sessionStorage.mRoleNumber).number;
    window.dEditVm = null;
    var mAdds = [];
    var mDeletes = [];
    var departmentTreeA;
    var departmentTreeB;
    var mtagLeft = 0;
    var mtagRight = 0;
    var mLefts = [];
    var mRights = [];
    avalon.ready(function () {
        window.dEditVm = avalon.define({
            $id: 'roleUsers',
            reqNotUsers: {
                角色编号: number,
                Index: 1,
                Size: 20,
                LikeAllName: '',
                部门编号: ''
            },
            reqUsers: {
                角色编号: number,
                Index: 1,
                Size: 20,
                LikeAllName: '',
                部门编号: ''
            },
            users: [],
            notUsers: [],
            allcheckedA: false,
            allcheckedB: false,
            getRoleNotUsers: function () {
                Role.getRoleNotUsers('get', dEditVm.reqNotUsers.$model, function getRoleNotUsersListener(success, obj, strErro) {
                    if (success) {
                        var datas = [];
                        if (obj != null && obj.list != null) {
                            datas = obj.list;
                        }
                        if (mtagLeft != 0) {
                            mLefts = [];
                        }
                        for (var i = 0; i < datas.length; i++) {
                            datas[i].checked = false;
                            mLefts.push(datas[i]);
                        }

                        if (datas.length < dEditVm.reqNotUsers.Size) {
                            $('.left .btn-more').hide();
                        } else {
                            $('.left .btn-more').show();
                        }
                        if (mtagLeft == 0) {
                            dEditVm.notUsers = dEditVm.notUsers.concat(datas);
                        } else {
                            datas = dEditVm.removeExsitLefts(datas);
                            dEditVm.notUsers = datas;
                        }

                    } else {
                        console.info('获取角色中没有的用户失败！');
                        console.info(strErro);
                    }
                });
            },
            getRoleUsers: function () {
                Role.getRoleUsers('get', dEditVm.reqUsers.$model, function getRoleUsersListener(success, obj, strErro) {
                    if (success) {
                        var datas = [];
                        if (obj != null && obj.list != null) {
                            datas = obj.list;
                        }
                        if (mtagRight != 0) {
                            mRights = [];
                        }
                        for (var i = 0; i < datas.length; i++) {
                            datas[i].checked = false;
                            mRights.push(datas[i]);
                        }

                        if (datas.length < dEditVm.reqUsers.Size) {
                            $('.right .btn-more').hide();
                        } else {
                            $('.right .btn-more').show();
                        }
                        if (mtagRight == 0) {
                            dEditVm.users = dEditVm.users.concat(datas);
                        } else {
                            datas = dEditVm.removeExsitRights(datas);
                            dEditVm.users = datas;
                        }

                    } else {
                        console.info('获取角色用户失败！');
                        console.info(strErro);
                    }
                });
            },
            removeExsitLefts: function (datas) {
                for (var i = 0; i < datas.length; i++) {
                    var data = datas[i];
                    var exist = false;
                    for (var a = 0; a < mAdds.length; a++) {
                        console.info(mAdds[a].编号 + '  ' + data.编号);
                        if (mAdds[a].编号 == data.编号) {
                            exist = true;
                            break;
                        }
                    }

                    if (exist) {
                        datas.splice(i, 1);
                        i--;
                    }
                }

                var locals = [];

                for (var i = 0; i < mDeletes.length; i++) {
                    locals.push(mDeletes[i]);
                }

                if (dEditVm.reqNotUsers.部门编号 != '') {
                    for (var i = 0; i < locals.length; i++) {
                        if (dEditVm.reqNotUsers.部门编号 != locals[i].部门编号) {
                            locals.splice(i, 1);
                            i--;
                        }
                    }
                }

                if (dEditVm.reqNotUsers.LikeAllName != '') {
                    for (var i = 0; i < locals.length; i++) {
                        if (locals[i].姓名.indexOf(dEditVm.reqNotUsers.LikeAllName) == -1) {
                            locals.splice(i, 1);
                            i--;
                        }
                    }
                }

                for (var i = 0; i < locals.length; i++) {
                    var data = locals[i];
                    var exist = false;
                    for (var a = 0; a < datas.length; a++) {
                        if (data.编号 == datas[a].编号) {
                            exist = true;
                            break;
                        }
                    }
                    if (!exist) {
                        datas.push(data);
                    }
                }
                return datas;
            },
            removeExsitRights: function (datas) {

                for (var i = 0; i < datas.length; i++) {
                    var data = datas[i];
                    var exist = false;
                    for (var a = 0; a < mDeletes.length; a++) {
                        if (mDeletes[a].编号 == data.编号) {
                            exist = true;
                            break;
                        }
                    }
                    if (exist) {
                        datas.splice(i, 1);
                        i--;
                    }
                }
                var locals = [];

                for (var i = 0; i < mAdds.length; i++) {
                    locals.push(mAdds[i]);
                }

                if (dEditVm.reqUsers.部门编号 != '') {
                    for (var i = 0; i < locals.length; i++) {
                        if (dEditVm.reqUsers.部门编号 != locals[i].部门编号) {
                            locals.splice(i, 1);
                            i--;
                        }
                    }
                }

                if (dEditVm.reqUsers.LikeAllName != '') {
                    for (var i = 0; i < locals.length; i++) {
                        if (locals[i].姓名.indexOf(dEditVm.reqUsers.LikeAllName) == -1) {
                            locals.splice(i, 1);
                            i--;
                        }
                    }
                }

                for (var i = 0; i < locals.length; i++) {
                    var data = locals[i];
                    var exist = false;
                    for (var a = 0; a < datas.length; a++) {
                        if (data.编号 == datas[a].编号) {
                            exist = true;
                            break;
                        }
                    }
                    if (!exist) {
                        datas.push(data);
                    }
                }
                return datas;
            },

            searchNotUsers: function () {
                dEditVm.reqNotUsers.Index = 1;
                mtagLeft = 1;
                dEditVm.getRoleNotUsers();
            },
            searchUsers: function () {
                dEditVm.reqUsers.Index = 1;
                mtagRight = 1;

                dEditVm.getRoleUsers();
            },
            delInputA: function () {
                $('.left .department').val('');
                dEditVm.reqNotUsers.部门编号 = '';
                dEditVm.hideMenu();
            },
            delInputB: function () {
                $('.right .department').val('');
                dEditVm.reqUsers.部门编号 = '';
                dEditVm.hideMenu();
            },
            checkAllA: function (e) {
                var checked = e.target.checked

                dEditVm.notUsers.forEach(function (el) {
                    el.checked = checked;
                });
            },
            checkOneA: function (e) {
                console.info(e);
                var checked = e.target.checked
                if (checked === false) {
                    dEditVm.allcheckedA = false
                } else {
                    dEditVm.allcheckedA = dEditVm.notUsers.every(function (el) {
                        return el.checked
                    })
                }
            },
            checkAllB: function (e) {
                var checked = e.target.checked

                dEditVm.users.forEach(function (el) {
                    el.checked = checked;
                })
            },
            checkOneB: function (e) {
                var checked = e.target.checked
                if (checked === false) {
                    dEditVm.allcheckedB = false
                } else {
                    dEditVm.allcheckedB = dEditVm.users.every(function (el) {
                        return el.checked
                    })
                }
            },
            clickLeft: function () {
                var num = 0;
                dEditVm.allcheckedB = false;
                var current = [];
                for (var i = 0; i < dEditVm.users.length; i++) {
                    if (dEditVm.users[i].checked === true) {
                        dEditVm.users[i].checked = false;
                        dEditVm.doWithDeleteUsers(dEditVm.users[i]);
                        current.push(dEditVm.users[i]);
                        dEditVm.users.removeAt(i);
                        i--;
                        num++;
                    }
                }
                dEditVm.notUsers = dEditVm.notUsers.concat(current);
                if (num == 0) {
                    $.oaNotify.error('请先选中人员！');
                }
            },
            clickRight: function () {
                var num = 0;
                dEditVm.allcheckedA = false;
                var current = [];
                for (var i = 0; i < dEditVm.notUsers.length; i++) {
                    if (dEditVm.notUsers[i].checked == true) {
                        dEditVm.notUsers[i].checked = false;
                        dEditVm.doWithAddUsers(dEditVm.notUsers[i]);
                        current.push(dEditVm.notUsers[i]);
                        dEditVm.notUsers.removeAt(i);
                        i--;
                        num++;
                    }
                }
                dEditVm.users = dEditVm.users.concat(current);

                if (num == 0) {
                    $.oaNotify.error('请先选中人员！');
                }
            },
            doWithAddUsers: function (user) {
                var exsit = false;
                for (var i = 0; i < mRights.length; i++) {
                    if (mRights[i].编号 == user.编号) {
                        exsit = true;
                        break;
                    }
                }
                if (!exsit) {
                    mAdds.push(user);
                }
                for (var i = 0; i < mDeletes.length; i++) {
                    if (user.编号 == mDeletes[i].编号) {
                        mDeletes.splice(i, 1);
                        break;
                    }
                }
            },
            doWithDeleteUsers: function (user) {
                var exsit = false;
                for (var i = 0; i < mLefts.length; i++) {
                    if (mLefts[i].编号 == user.编号) {
                        exsit = true;
                        break;
                    }
                }
                if (!exsit) {
                    mDeletes.push(user);
                }
                for (var i = 0; i < mAdds.length; i++) {
                    if (user.编号 == mAdds[i].编号) {
                        mAdds.splice(i, 1);
                        break;
                    }
                }
            },
            moreNotUsers: function () {
                mtagLeft = 0;
                dEditVm.reqNotUsers.Index++;
                dEditVm.allcheckedA = false;
                dEditVm.getRoleNotUsers();
                console.info(dEditVm.reqNotUsers.Index);
            },
            moreUsers: function () {
                mtagRight = 0;
                dEditVm.reqUsers.Index++;
                dEditVm.allcheckedB = false;
                dEditVm.getRoleUsers();
                console.info(dEditVm.reqUsers.Index);
            },
            hideMenu: function () {
                $('.screen-box .menuContent').fadeOut("fast");
                $("body").unbind("mousedown", dEditVm.onBodyDown());
            },
            onBodyDown: function () {
                if (!$(".menuContent").length > 0) {
                    dEditVm.hideMenu();
                }
            },
            showMenu: function (name) {
                var cityObj = $(name);
                var cityOffset = $(name).offset();
                $(name).next().css({
                    left: cityOffset.left + "px",
                    top: cityOffset.top + cityObj.outerHeight() + "px"
                }).slideDown("fast");

                $("body").bind("mousedown", dEditVm.onBodyDown());
            },
            getDepartmentList: function () {
                var settingA = {
                    data: {
                        view: {
                            dblClickExpand: false
                        },
                        key: {
                            name: '名称'
                        },
                        simpleData: {
                            enable: true,
                            idKey: '编号',
                            pIdKey: '上级部门编号',
                            rootPId: 0
                        }
                    },
                    callback: {
                        onClick: onClickA
                    }
                };
                var settingB = {
                    data: {
                        view: {
                            dblClickExpand: false
                        },
                        key: {
                            name: '名称'
                        },
                        simpleData: {
                            enable: true,
                            idKey: '编号',
                            pIdKey: '上级部门编号',
                            rootPId: 0
                        }
                    },
                    callback: {
                        onClick: onClickB
                    }
                };
                var nodes = [];
                Department.getEnableDepartmentList('get', '', function getEnableDepartmentListListener(success, obj, strErro) {
                    if (success) {
                        for (var i in obj) {
                            obj[i].name = obj[i].名称;
                        }
                        nodes = obj;
                        var newNode = {name: '全部', 上级部门编号: null, 编号: 0, 名称: '全部', open: true};
                        nodes.push(newNode);
                        departmentTreeA = $.fn.zTree.init($("#departmentTreeA"), settingA, nodes);
                        departmentTreeB = $.fn.zTree.init($("#departmentTreeB"), settingB, nodes);
                    } else {
                        console.info(strErro);
                    }
                });

                function onClickA(e, treeId, treeNode) {
                    var nodes = departmentTreeA.getSelectedNodes(),
                        text = "";
                    nodes.sort(function compare(a, b) {
                        return a.id - b.id;
                    });
                    for (var i = 0, l = nodes.length; i < l; i++) {
                        text += nodes[i].name + ",";
                    }
                    if (text.length > 0) text = text.substring(0, text.length - 1);
                    var cityObj = $('.left .department');
                    cityObj.val(text);
                    $('.menuContent').hide();
                    if (treeNode.编号 == 0) {
                        dEditVm.reqNotUsers.部门编号 = '';
                    } else {
                        dEditVm.reqNotUsers.部门编号 = treeNode.编号;
                    }
                    dEditVm.searchNotUsers();
                    $('.left .department-box .btn-del').show();
                }

                function onClickB(e, treeId, treeNode) {
                    var nodes = departmentTreeB.getSelectedNodes(),
                        text = "";
                    nodes.sort(function compare(a, b) {
                        return a.id - b.id;
                    });
                    for (var i = 0, l = nodes.length; i < l; i++) {
                        text += nodes[i].name + ",";
                    }
                    if (text.length > 0) text = text.substring(0, text.length - 1);
                    var cityObj = $('.right .department');
                    cityObj.val(text);
                    $('.menuContent').hide();
                    if (treeNode.编号 == 0) {
                        dEditVm.reqUsers.部门编号 = '';
                    } else {
                        dEditVm.reqUsers.部门编号 = treeNode.编号;
                    }
                    dEditVm.searchUsers();
                    $('.right .department-box .btn-del').show();
                }
            },
            clickSubmit: function () {
                var newAdds = [];
                var newDeletes = [];
                for (var i in mAdds) {
                    newAdds.push(mAdds[i].编号);
                }
                for (var i in mDeletes) {
                    newDeletes.push(mDeletes[i].编号);
                }
                dEditVm.addAndDelRoleUsers(newAdds, newDeletes);
            },
            addAndDelRoleUsers: function (newAdds, newDeletes) {

                var data = {
                    角色编号: number,
                    将增加的人员编号列表: newAdds,
                    将删除的人员编号列表: newDeletes
                }
                Role.addAndDelRoleUsers('post', data, function addAndDelRoleUsersListener(success, obj, strErro) {
                    postBack(success, strErro, '提交成功！', '提交失败：', '.modal-add', function callBack() {
                        sessionStorage.removeItem('mRole');
                        vm.query();
                    });
                });
            },
            clickBtnReturn: function () {
                $('.modal-add').modal('hide');
            }
        });
        $('.select-content .ul,.menuContent').mCustomScrollbar({
            theme: 'dark-3'
        });
        $('body').on('clcik', function () {
            dEditVm.hideMenu();
        });
        dEditVm.getDepartmentList();
        dEditVm.getRoleUsers();
        dEditVm.getRoleNotUsers();
        avalon.scan(document.body);
    });
});