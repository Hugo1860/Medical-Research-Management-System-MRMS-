$(function () {
    window.vm = null;
    if (sessionStorage.userInfo == null) {
        location.href = '/IMIS/views/login.html';
    }
    window.mUserInfo = JSON.parse(sessionStorage.userInfo);
    window.mUserId = mUserInfo.人员.编号;

    avalon.ready(function () {
        window.vm = avalon.define({
            $id: 'root',
            total: '',
            model: [],
            allchecked: false,
            loaded: false,
            RoleType: false,
            query: function () {
                vm.loaded = false;
                $.support.cors = true;

                Role.getRoleList('get', function getRoleListListener(success, obj, strErro) {
                    if (success) {
                        vm.loaded = true;
                        console.info(obj);
                        vm.total = obj.length;
                        var number = 1;
                        for (var i = 0; i < obj.length; i++) {
                            obj[i].number = number;
                            obj[i].checked = false;
                            number++;
                        }
                        vm.model = obj;

                        $('.bs-tooltip').tooltip();
                    } else {
                        console.info('获取角色列表失败！');
                        console.info(strErro);
                    }
                });

            },
            clickBtnAdd: function () {
                vm.RoleType = false;
                $('.modal-add .modal-dialog').removeClass('modal-lg');
            },
            clickBtnEdit: function (el) {
                var data = {
                    编号: el.编号,
                    名称: el.名称,
                    备注: el.备注
                }
                vm.RoleType = true;
                sessionStorage.mRole = JSON.stringify(data);
                $('.modal-add .modal-dialog').removeClass('modal-lg');
            },
            roleUsers: function (el) {
                var data = {
                    number: el.编号,
                    name: el.名称
                }

                $('.modal-add .modal-dialog').addClass('modal-lg');
                sessionStorage.mRoleNumber = JSON.stringify(data);
            },
            roleJurisdiction: function (el) {
                var data = {
                    number: el.编号,
                    name: el.名称
                }
                $('.modal-add .modal-dialog').removeClass('modal-lg');
                sessionStorage.mRoleNumber = JSON.stringify(data);
            }
        })
        vm.query();
        avalon.scan(document.body);
    });
});