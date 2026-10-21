$(function () {
    window.vm = null;
    if (sessionStorage.userInfo == null) {
        location.href = '/views/login.html';
    }
    window.mUserInfo = JSON.parse(sessionStorage.userInfo);
    var back = JSON.parse(sessionStorage.mkeyandetails);
    console.info(back);
    avalon.ready(function () {
        window.vm = avalon.define({
            $id: 'root',
            back:back,
            model:{
                旧密码:'',
                新密码:''
            },
            password: function (name) {
                //限制密码格式为数字和字母组合，不能为中文;
                var re = /^[a-zA-Z0-9]{6,20}$/;
                var nubmer = $(name).val();
                if (!re.test(nubmer)) {
                    $(name).next().next().show();
                    $(name).addClass('error-input');
                    return false;
                } else {
                    $(name).next().next().hide();
                    $(name).removeClass('error-input');
                    return true;
                }
            },
            userChangePassword: function (data) {
                User.userChangePassword('post', data, function userChangePasswordListener(success, obj, strErro) {
                    postBack(success, strErro, '提交成功！', '提交失败：', '.modal-add', '');
                });
            },
            clickSubmit: function () {
                var password = vm.password('.password');
                if(password){
                    vm.userChangePassword(vm.model.$model);
                }
            }
        });
        avalon.scan(document.body);
    });
});