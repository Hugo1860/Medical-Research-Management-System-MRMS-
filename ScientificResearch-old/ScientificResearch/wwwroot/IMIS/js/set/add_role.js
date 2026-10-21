$(function () {
    var icon;
    var dEditVm = null;
    avalon.ready(function () {
        dEditVm = avalon.define({
            $id: 'roleAdd',
            Type: vm.RoleType,
            role: {},
            title: '',
            loadInfo: function () {
                if (dEditVm.Type) {
                    dEditVm.title = '修改角色';
                    dEditVm.role = JSON.parse(sessionStorage.mRole);
                } else {
                    dEditVm.title = '添加角色';
                    dEditVm.role = {
                        名称: '',
                        备注: ''
                    };
                }
            },

            clickSubmit: function () {
                if (dEditVm.role.名称 == '') {
                    icon = 'icon-shibai1';
                    popover(icon, '提交失败：请选择填写角色名称！');
                    setTimeout("$('.popover').remove()", 2000);
                } else {
                    if (dEditVm.Type) {
                        dEditVm.editRole(dEditVm.role.$model);
                    } else {
                        dEditVm.addRole(dEditVm.role.$model);
                    }
                }
            },
            addRole: function (data) {
                debugger;
                Role.addRole('post', data, function addRoleListener(success, obj, strErro) {
                    if (success) {
                        icon = 'icon-chenggong1';
                        popover(icon, '提交成功！');
                        setTimeout(function () {
                            $('.popover').remove();
                        }, 1000);
                        setTimeout(function () {
                            $('.modal-add').modal('hide');
                            vm.query();
                        }, 500);
                    } else {
                        icon = 'icon-shibai1';
                        popover(icon, '提交失败：' + strErro);
                        setTimeout("$('.popover').remove()", 1000);
                    }
                });
            },
            editRole: function (data) {
                Role.editRole('post', data, function editRoleListener(success, obj, strErro) {
                    var icon;
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
            clickBtnReturn:function () {
                $('.modal-add').modal('hide');
            }
        });
        dEditVm.loadInfo();
        avalon.scan(document.body);
    });
});