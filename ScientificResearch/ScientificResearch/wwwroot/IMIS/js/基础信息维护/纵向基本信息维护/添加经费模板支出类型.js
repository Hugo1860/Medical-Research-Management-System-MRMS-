$(function () {
    var icon;
    var dAddVm = null;
    avalon.ready(function () {
        dAddVm = avalon.define({
            $id: 'add',
            type: vm.type,
            TemplateId:vm.req.所属经费模版编号,
            expenditureTypes: {},
            title: '',
            loadInfo: function () {
                if (dAddVm.type) {
                    dAddVm.title = '修改';
                    dAddVm.expenditureTypes = JSON.parse(sessionStorage.mExpenditureTypes);
                } else {
                    dAddVm.title = '添加';
                    dAddVm.expenditureTypes = {
                        编号: 0,
                        所属经费模版编号: dAddVm.TemplateId,
                        项目支出类型:'',
                        经费占比:0,
                        备注: ''
                    }
                }
            },

            clickSubmit: function () {
                if (dAddVm.expenditureTypes.名称 == ''||dAddVm.expenditureTypes.经费占比<=0) {
                    icon = 'icon-shibai1';
                    popover(icon, '提交失败：名称不能为空！');
                    setTimeout("$('.popover').remove()", 2000);
                } else {
                    dAddVm.addOrEditTemplate(dAddVm.expenditureTypes.$model);
                }
            },
            addOrEditTemplate: function (data) {
                ExpenditureTemplate.addOrEditTemplate('post', data, function addOrEditTemplateListener(success, obj, strErro) {
                    var icon;
                    if (success) {
                        icon = 'icon-chenggong1';
                        popover(icon, '添加成功！');
                        setTimeout(function () {
                            $('.popover').remove();
                        }, 1000);
                        setTimeout(function () {
                            $('.modal-add').modal('hide');
                            vm.query();
                        }, 500);
                    } else {
                        icon = 'icon-shibai1';
                        popover(icon, '添加失败：' + strErro);
                        setTimeout("$('.popover').remove()", 2000);
                    }
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