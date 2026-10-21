$(function () {
    var icon;
    var dAddVm = null;
    avalon.ready(function () {
        dAddVm = avalon.define({
            $id: 'roleAdd',
            type: vm.type,
            classType: vm.classType,
            addTyoe: vm.addTyoe,
            dictionary: {},
            title: '',
            mtopName: vm.topName,
            loadInfo: function () {
                if (dAddVm.type) {
                    dAddVm.title = '修改';
                    dAddVm.dictionary = JSON.parse(sessionStorage.mDictionary);
                } else {
                    dAddVm.title = '添加';
                    dAddVm.dictionary = {
                        编号: 0,
                        分类: vm.req.分类,
                        值: '',
                        上级编号: 0,
                        级别: 0,
                        排序: 0,
                        备注: ''
                    };
                    if (dAddVm.addTyoe) {
                        dAddVm.dictionary.上级编号 = topInfo.number;
                        dAddVm.dictionary.级别 = topInfo.级别 + 1;
                        dAddVm.dictionary.分类 = topInfo.mCName;
                    }

                }
            },

            clickSubmit: function () {
                if (dAddVm.dictionary.值 == '') {
                    icon = 'icon-shibai1';
                    popover(icon, '提交失败：名称不能为空！');
                    setTimeout("$('.popover').remove()", 2000);
                } else {
                    if (dAddVm.type) {
                        dAddVm.editDictionary(dAddVm.dictionary.$model);
                    } else {
                        dAddVm.addDictionary(dAddVm.dictionary.$model);
                    }
                }
            },
            addDictionary: function (data) {
                Dictionary.addDictionary('post', data, function addDictionaryListener(success, obj, strErro) {
                    var icon;
                    if (success) {
                        icon = 'icon-chenggong1';
                        popover(icon, '添加成功！');
                        setTimeout(function () {
                            $('.popover').remove();
                        }, 1000);
                        setTimeout(function () {
                            $('.modal-add').modal('hide');
                            sessionStorage.removeItem('topInfo');
                            vm.query();
                        }, 500);
                    } else {
                        icon = 'icon-shibai1';
                        popover(icon, '添加失败：' + strErro);
                        setTimeout("$('.popover').remove()", 2000);
                    }
                });
            },
            editDictionary: function (data) {
                Dictionary.editDictionary('post', data, function editDictionaryListener(success, obj, strErro) {
                    var icon;
                    if (success) {
                        icon = 'icon-chenggong1';
                        popover(icon, '修改成功！');
                        setTimeout(function () {
                            $('.popover').remove();
                        }, 1000);
                        setTimeout(function () {
                            $('.modal-add').modal('hide');
                            sessionStorage.removeItem('mDictionary');
                            vm.query();
                        }, 500);
                    } else {
                        icon = 'icon-shibai1';
                        popover(icon, '修改失败：' + strErro);
                        setTimeout("$('.popover').remove()", 2000);
                    }
                });
            },
            clickBtnReturn: function () {
                $('.modal-add').modal('hide');
            }
        });
        console.info('dAddVm.mtopName----');
        console.info(dAddVm.mtopName);
        dAddVm.loadInfo();
        avalon.scan(document.body);
    });
});