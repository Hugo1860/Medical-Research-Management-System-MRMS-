$(function () {
    var icon;
    var dAddVm = null;
    avalon.ready(function () {
        dAddVm = avalon.define({
            $id: 'add',
            type: vm.type,
            template: {},
            title: '',
            loadInfo: function () {
                if (dAddVm.type) {
                    dAddVm.title = '修改';
                    dAddVm.template = JSON.parse(sessionStorage.mTemplate);
                } else {
                    dAddVm.title = '添加';
                    dAddVm.template = {
                        编号: 0,
                        名称: '',
                        备注: ''
                    }
                }
            },

            clickSubmit: function () {
                if (dAddVm.template.名称 == '') {
                    icon = 'icon-shibai1';
                    popover(icon, '提交失败：名称不能为空！');
                    setTimeout("$('.popover').remove()", 2000);
                } else {
                    dAddVm.addOrEditTemplate(dAddVm.template.$model);
                }
            },
            addOrEditTemplate: function (data) {
                ExpenditureTemplate.addOrEditTemplate('post', data, function addOrEditTemplateListener(success, obj, strErro) {
                    postBack(success, strErro, '添加成功！', '添加失败：', '.modal-add', function callBack() {
                        vm.query();
                    });
                });
            },
            clickBtnReturn: function () {
                $('.modal-add').modal('hide');
            }
        });
        dAddVm.loadInfo();
        avalon.scan(document.body);
    });
});