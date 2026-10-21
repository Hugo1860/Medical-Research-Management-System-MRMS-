$(function () {
    var icon;
    // var number = JSON.parse(sessionStorage.mRoleNumber).number;
    addVm.people
    var dEditVm = null;
    var departmentTreeA;
    var mEnableChooseCount = 0;
    var mReadyChooseCount = 0;
    var mtagLeft = 0;
    var mtagRight = 0;
    avalon.ready(function () {
        dEditVm = avalon.define({
            $id: 'roleUsers',
            reqAllUsers: {
                角色编号: number,
                Index: 1,
                Size: 12,
                LikeAllName: '',
                部门编号: ''
            },
            nameA: '',
            nameB: '',
            selectedUsers: [],
            allUsers: [],
            newSelectedUsers: [],
            newAllUsers: [],
            total: '',
            total2: '',
            newTotal: -1,
            delData: [],
            addData: [],
            allcheckedA: false,
            allcheckedB: false,
            clickSubmit: function () {
                dEditVm.editRoleUsers();
            },
            getEnableUsersList: function () {
                User.getEnableUsersList('get', dEditVm.reqAllUsers.$model, function getEnableUsersListListener(success, obj, strErro) {
                    if (success) {
                        dEditVm.total = obj.total;
                        if (dEditVm.newTotal == -1) {
                            dEditVm.newTotal = dEditVm.total;
                        }
                        if (obj == null || obj.list.length == 0) {
                            $('.left .btn-more').hide();
                            return;
                        } else {
                            obj = obj.list;
                            for (var i = 0; i < obj.length; i++) {
                                obj[i].checked = false;
                            }

                            dEditVm.newAllUsers = dEditVm.newAllUsers.concat(obj);

                            if (mtagRight == 0) {
                                dEditVm.allUsers = dEditVm.allUsers.concat(obj);
                            } else {
                                dEditVm.allUsers = obj;
                            }
                            mEnableChooseCount += obj.length;
                            if (mEnableChooseCount == dEditVm.total) {
                                $('.left .btn-more').hide();
                            }
                        }
                    } else {
                        console.info('获取用户失败！');
                        console.info(strErro);
                    }
                });
            },
            getSelectedUsers: function () {




                var selectedUsers = JSON.parse(sessionStorage.selectedUsers);
                if (selectedUsers == null) {
                    dEditVm.total2 = 0;
                    dEditVm.selectedUsers = [];
                    return;
                } else {
                    selectedUsers = selectedUsers.splice(',');
                    dEditVm.total2 = selectedUsers.length;
                    for (var i = 0; i < selectedUsers.length; i++) {
                        selectedUsers[i].checked = false;
                        selectedUsers[i].编号 = selectedUsers[i].接收者编号;
                        selectedUsers[i].姓名 = selectedUsers[i].接收者名称;
                    }

                    dEditVm.newSelectedUsers = dEditVm.newSelectedUsers.concat(selectedUsers);

                    dEditVm.selectedUsers = selectedUsers;

                    mReadyChooseCount += selectedUsers.length;
                }
            },
            editRoleUsers: function () {
                /*var selectedUsers = [];
                for (var i in dEditVm.selectedUsers) {
                    selectedUsers.push(dEditVm.selectedUsers[i].编号);
                }*/
                var data = {
                    角色编号: number,
                    将增加的人员编号列表: dEditVm.addData,
                    将删除的人员编号列表: dEditVm.delData
                }
                console.info(data);
                Role.editRoleUsers('post', data, function editRoleUsersListener(success, obj, strErro) {
                    if (success) {
                        icon = 'icon-chenggong1';
                        popover(icon, '提交成功！');
                        setTimeout(function () {
                            $('.popover').remove();
                        }, 2000);
                        setTimeout(function () {
                            $('.modal-add').modal('hide');
                            sessionStorage.removeItem('mRole');
                            vm.query();
                        }, 1000);
                    } else {
                        icon = 'icon-shibai1';
                        popover(icon, '提交失败：' + strErro);
                        setTimeout("$('.popover').remove()", 1000);
                    }
                });
            },
            searchAllUsers: function () {
                dEditVm.reqAllUsers.Index = 1;
                dEditVm.reqAllUsers.Like姓名 = dEditVm.nameA;
                /*for (var i in dEditVm.reqAllUsers) {
                    if (dEditVm.reqAllUsers[i] == null || dEditVm.reqAllUsers[i] == '') {
                        delete dEditVm.reqAllUsers[i];
                        // dEditVm.reqAllUsers.removeAt(i)
                    }
                }
                dEditVm.getEnableUsersList();*/

                mtagLeft = 1;
                if ($.trim(dEditVm.nameA) != '') {
                    var data = [];
                    var isExist = false;
                    for (var i in dEditVm.allUsers) {
                        if (dEditVm.allUsers[i].姓名.indexOf(dEditVm.nameA) != -1) {
                            isExist = true;
                            data.push(dEditVm.allUsers[i]);
                            break;
                        }
                    }
                    if (isExist) {
                        dEditVm.allUsers = data;
                    } else {
                        for (var j in dEditVm.reqAllUsers) {
                            if (dEditVm.reqAllUsers[j] == null || dEditVm.reqAllUsers[j] == '') {
                                delete dEditVm.reqSelectedUsers[j];
                            }
                        }
                        dEditVm.getEnableUsersList();
                    }

                } else {
                    dEditVm.allUsers = dEditVm.newAllUsers;

                }
            },
            checkAllA: function (e) {
                var checked = e.target.checked

                dEditVm.allUsers.forEach(function (el) {
                    el.checked = checked;
                });
            },
            checkOneA: function (e) {
                console.info(e);
                var checked = e.target.checked
                if (checked === false) {
                    dEditVm.allcheckedA = false
                } else {
                    dEditVm.allcheckedA = dEditVm.allUsers.every(function (el) {
                        return el.checked
                    })
                }
            },
            checkAllB: function (e) {
                var checked = e.target.checked

                dEditVm.selectedUsers.forEach(function (el) {
                    el.checked = checked;
                })
            },
            checkOneB: function (e) {
                var checked = e.target.checked
                if (checked === false) {
                    dEditVm.allcheckedB = false
                } else {
                    dEditVm.allcheckedB = dEditVm.selectedUsers.every(function (el) {
                        return el.checked
                    })
                }
            },
            clickLeft: function () {
                var num = 0;
                var data = [];
                dEditVm.allcheckedB = false;
                for (var i = 0; i < dEditVm.selectedUsers.length; i++) {
                    if (dEditVm.selectedUsers[i].checked === true) {
                        dEditVm.selectedUsers[i].checked = false;
                        dEditVm.allUsers.push(dEditVm.selectedUsers[i]);
                        data.push(dEditVm.allUsers[i]);

                        if (dEditVm.addData.length > 0) {
                            var isExist = false;
                            for (var j in dEditVm.addData) {
                                if (dEditVm.addData[j].编号 == dEditVm.selectedUsers[i].编号) {
                                    dEditVm.addData.removeAt(j);
                                    isExist = true;
                                    break;
                                }
                            }
                            if (!isExist) {
                                dEditVm.delData.push(dEditVm.selectedUsers[i]);
                            }
                        } else {
                            dEditVm.delData.push(dEditVm.selectedUsers[i]);
                        }

                        dEditVm.selectedUsers.removeAt(i);
                        num++;
                        i--;
                    }
                }


                console.info('delData--111---');
                console.info(dEditVm.delData);
                console.info('addData--222---');
                console.info(dEditVm.addData);
                dEditVm.newTotal = dEditVm.newTotal + data.length;
                dEditVm.total2 = dEditVm.total2 - data.length;
                if (num == 0) {
                    $.oaNotify.error('请先选中人员！');
                }
            },
            clickRight: function () {
                var num = 0;
                dEditVm.allcheckedA = false;
                var data = [];
                for (var i = 0; i < dEditVm.allUsers.length; i++) {
                    if (dEditVm.allUsers[i].checked === true) {
                        dEditVm.allUsers[i].checked = false;
                        debugger;
                        dEditVm.selectedUsers.push(dEditVm.allUsers[i]);
                        data.push(dEditVm.allUsers[i]);
                        debugger;
                        if (dEditVm.addData.length > 0) {
                            var isExist = false;
                            for (var j in dEditVm.addData) {
                                if (dEditVm.addData[j].编号 == dEditVm.allUsers[i].编号) {
                                    dEditVm.addData.removeAt(j);
                                    debugger;
                                    isExist = true;
                                    break;
                                }
                            }
                            if (!isExist) {
                                dEditVm.addData.push(dEditVm.allUsers[i]);
                            }
                        } else {
                            dEditVm.addData.push(dEditVm.allUsers[i]);
                        }

                        dEditVm.allUsers.removeAt(i);
                        num++;
                        i--;
                    }
                }
                console.info('delData--333---');
                console.info(dEditVm.delData);
                console.info('addData--444---');
                console.info(dEditVm.addData);
                dEditVm.newTotal = dEditVm.newTotal - data.length;
                dEditVm.total2 = dEditVm.total2 + data.length;
                if (num == 0) {
                    $.oaNotify.error('请先选中人员！');
                }


                // debugger;
                // var num = 0;
                // dEditVm.allcheckedA = false;
                // var data = [];
                // for (var i = 0; i < dEditVm.allUsers.length; i++) {
                //     if (dEditVm.allUsers[i].checked === true) {
                //         dEditVm.allUsers[i].checked = false;
                //         dEditVm.selectedUsers.push(dEditVm.allUsers[i]);
                //         data.push(dEditVm.allUsers[i]);
                //         dEditVm.allUsers.removeAt(i);
                //         num++;
                //         i--;
                //     }
                // }
                // debugger;
                // dEditVm.newTotal = dEditVm.newTotal - data.length;
                // dEditVm.total2 = dEditVm.total2 + data.length;
                // if (num == 0) {
                //     $.oaNotify.error('请先选中人员！');
                // }
            },
            moreAllUsers: function () {
                mtagLeft = 0;
                dEditVm.reqAllUsers.Index++;
                dEditVm.allcheckedA = false;
                dEditVm.getEnableUsersList();
                console.info(dEditVm.reqAllUsers.Index);
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
                        dEditVm.reqAllUsers.部门编号 = '';
                    } else {
                        dEditVm.reqAllUsers.部门编号 = treeNode.编号;
                    }
                    dEditVm.searchAllUsers();
                }
            },
            clickBtnReturn: function () {
                $('.modal-user').modal('hide');
            }
        });
        $('.select-content .ul,.menuContent').mCustomScrollbar({
            theme: 'dark-3'
        });
        $('body').on('clcik', function () {
            dEditVm.hideMenu();
        });
        dEditVm.getDepartmentList();
        dEditVm.getSelectedUsers();
        dEditVm.getEnableUsersList();
        avalon.scan(document.body);
    });
});