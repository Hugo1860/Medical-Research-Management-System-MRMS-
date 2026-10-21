$(function () {
    var icon;
    var dEditVm = null;
    avalon.ready(function () {
        dEditVm = avalon.define({
            $id: 'add',
            name: '',
            code: '',
            clickSubmit: function () {
                if (dEditVm.name == '' || dEditVm.code == '') {
                    icon = 'icon-shibai1';
                    popover(icon, '提交失败：行业名称或代码值不能为空！');
                    setTimeout("$('.popover').remove()", 2000);
                } else {
                    var data = {
                        编号: 0,
                        上级编号: 0,
                        名称: dEditVm.name,
                        代码: dEditVm.code,
                        级别: 1,
                        排序: 0,
                        备注: ""
                    }
                    dEditVm.addIndustryCode(data);
                }
            },
            addIndustryCode: function (data) {
                IndustryCode.addIndustryCode('post', data, function addIndustryCodeListener(success, obj, strErro) {
                    var icon;
                    if (success) {
                        icon = 'icon-chenggong1';
                        popover(icon, '添加成功!');
                        setTimeout(function () {
                            $('.popover').remove();
                        }, 1000);
                        setTimeout(function () {
                            $('.modal-add').modal('hide');
                            vm.req.上级编号 = 0;
                            vm.query();
                        }, 500);
                    } else {
                        icon = 'icon-shibai1';
                        popover(icon, '添加失败:' + strErro);
                        setTimeout("$('.popover').remove()", 2000);
                    }
                });
            },
            clickBtnReturn: function () {
                $('.modal-add').modal('hide');
            }
        });
        avalon.scan(document.body);
    });
});