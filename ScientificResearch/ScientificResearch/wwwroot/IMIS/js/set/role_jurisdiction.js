$(function () {
    var icon;
    var mRoleNumber = JSON.parse(sessionStorage.mRoleNumber);
    var dEditVm = null;
    var nodes = [];
    var tree = [];
    avalon.ready(function () {
        dEditVm = avalon.define({
            $id: 'roleUsers',
            Jurisdictio: {},
            mRoleNumber:mRoleNumber,
            clickSubmit: function () {
                var node = tree.getCheckedNodes(true);
                var arr = [];
                for (var i in node) {
                    arr.push(node[i].编号);
                }
                var data = {
                    角色编号: dEditVm.mRoleNumber.number,
                    权限编号列表: arr
                }
                console.info(data);
                dEditVm.editRoleJurisdiction(data);

            },
            getRoleJurisdiction: function (number) {
                var setting = {
                    key: {
                        name: 'name',
                        checked: 'checked'
                    },

                    check: {
                        enable: true
                    },
                    data: {
                        simpleData: {
                            enable: true,
                            idKey: '编号',
                            pIdKey: '上级编号',
                            rootPId: 0
                        }
                    },
                };

                Role.getRoleJurisdiction('get', number, function getRoleJurisdictionListener(success, obj, strErro) {
                    if (success) {
                        dEditVm.Jurisdictio = obj;

                        nodes = obj;
                        console.info(nodes);
                        if (nodes != null) {
                            for (var i in nodes) {
                                nodes[i].name = nodes[i].名称;
                                nodes[i].checked = nodes[i].拥有;
                            }
                            tree = $.fn.zTree.init($(".treeDept"), setting, nodes);
                        }

                        console.info(obj);
                    } else {
                        console.info('获取角色权限失败！');
                        console.info(strErro);
                    }
                });


            },
            editRoleJurisdiction: function (data) {
                Role.editRoleJurisdiction('post', data, function editRoleJurisdictionListener(success, obj, strErro) {
                    if (success) {
                        icon = 'icon-chenggong1';
                        popover(icon, '提交成功！');
                        setTimeout(function () {
                            $('.popover').remove();
                        }, 1000);
                        setTimeout(function () {
                            $('.modal-add').modal('hide');
                            sessionStorage.removeItem('mRole');
                            vm.query();
                        }, 500);
                    } else {
                        icon = 'icon-shibai1';
                        popover(icon, '提交失败：' + strErro);
                        setTimeout("$('.popover').remove()", 1000);
                    }
                });
            },
            clickBtnReturn: function () {
                $('.modal-add').modal('hide');
            }
        });
        $('.ztree-box').mCustomScrollbar({
            theme: 'dark-3'
        });
        dEditVm.getRoleJurisdiction(dEditVm.mRoleNumber.number);
        avalon.scan(document.body);
    });
});