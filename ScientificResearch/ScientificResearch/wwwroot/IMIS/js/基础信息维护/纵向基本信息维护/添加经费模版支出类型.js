$(function () {
    var icon;
    var dAddVm = null;
    if (sessionStorage.mTypeId) {
        typeId = sessionStorage.mTypeId;
    }
    avalon.ready(function () {
        dAddVm = avalon.define({
            $id: 'addContent',
            type: vm.type,
            TemplateId: vm.req.所属经费模版编号,
            types: [],
            finance: [],
            typesInfo: {},
            typesChild: [],
            optional: [],
            title: '',
            name:'',
            allchecked: false,
            checkAll: function (e) {
                var checked = e.target.checked

                dAddVm.typesChild.forEach(function (el) {
                    el.checked = checked
                })
            },
            checkOne: function (e) {
                var checked = e.target.checked
                if (checked === false) {
                    dAddVm.allchecked = false
                } else {
                    dAddVm.allchecked = dAddVm.typesChild.every(function (el) {
                        return el.checked
                    });
                }
            },
            initMultiselect: function (obj) {
                $(obj).multiselect({
                    multiple: true,
                    buttonWidth: '70%',
                    maxHeight: 300,
                    noneSelectedText: '请选择',
                    enableFiltering: true,//是否显示搜索
                    filterPlaceholder: '输入关键字...',
                    onChange: function (option, checked, select) {
                        dAddVm.typesInfo.基本信息.编号 = $(option).val();
                        var text = $(option).text();
                        debugger;
                        if (text == dAddVm.name) {
                            dAddVm.typesChild = dAddVm.forData(dAddVm.typesInfo.经费模版项目支出内容列表, dAddVm.optional);

                        } else {
                            if (dAddVm.typesInfo.基本信息.编号 != 0) {
                                dAddVm.getTypesChild(dAddVm.typesInfo.基本信息.编号);
                                dAddVm.typesInfo.基本信息.项目支出类型 = text;
                            } else{
                                dAddVm.typesChild=[];
                            }
                        }
                    },
                });
                if (dAddVm.typesInfo.基本信息.编号 > 0) {
                    $('.multiselect-selected-text').text(dAddVm.typesInfo.基本信息.项目支出类型);
                }
            },
            loadInfo: function () {
                dAddVm.typesInfo = {
                    基本信息: {
                        编号: 0,
                        所属经费模版编号: dAddVm.TemplateId,
                        项目支出类型: '',
                        经费占比: 0,
                        备注: ''
                    },
                    经费模版项目支出内容列表: []
                }
                if (dAddVm.type) {
                    dAddVm.title = '修改';
                    dAddVm.getTypesInfo();
                } else {
                    dAddVm.title = '添加';
                }

                dAddVm.initMultiselect('#types');
                $('.multiselect-native-select').on('click', function () {

                    if (dAddVm.types.length < 2 || dAddVm.typesInfo.基本信息.编号 == 0) {
                        dAddVm.getexpenditureTypes();
                    }
                });
            },
            forData: function (selected, optional) {
                var arr = [];
                for (var i in optional) {
                    var oData = {
                        checked: false,
                        项目支出内容: optional[i],
                        财务科目: '',
                    }

                    for (var j in selected) {
                        if (selected[j].项目支出内容 == optional[i]) {
                            oData.checked = true;
                            oData.财务科目 = selected[j].财务科目;
                            break;
                        }
                    }
                    arr.push(oData);
                }
                return arr;
            },
            getTypesInfo: function () {
                ExpenditureTemplate.getExpenditureTypeDetail('get', typeId, function getExpenditureTypeDetailListener(success, obj, strErro) {
                    if (success) {
                        dAddVm.typesInfo.基本信息 = obj.基本信息;
                        dAddVm.name=dAddVm.typesInfo.基本信息.项目支出类型;
                        dAddVm.typesInfo.经费模版项目支出内容列表 = obj.经费模版项目支出内容列表;
                        var selected = obj.经费模版项目支出内容列表;
                        dAddVm.optional = obj.可选支出内容列表;
                        dAddVm.typesChild = dAddVm.forData(selected, dAddVm.optional);
                        dAddVm.types = [{
                            label: dAddVm.typesInfo.基本信息.项目支出类型,
                            title: dAddVm.typesInfo.基本信息.项目支出类型,
                            value: dAddVm.typesInfo.基本信息.编号
                        }]
                        dAddVm.initMultiselect('#types');
                    } else {
                        console.info('获取项目支出类型详情失败！');
                        console.info(strErro);
                    }
                })
            },
            getexpenditureTypes: function () {
                Dictionary.getDictionaryList('get', '项目支出类型', function getDictionaryListListener(success, obj, strErro) {
                    if (success) {
                        dAddVm.types = obj;

                        var options = [{
                            label: '请选择',
                            title: '请选择',
                            value: 0
                        }];
                        for (var i in obj) {
                            var option = {
                                label: obj[i].值,
                                title: obj[i].值,
                                value: obj[i].编号
                            }
                            options.push(option);
                        }

                        $('#types').multiselect('dataprovider', options);
                        dAddVm.initMultiselect('#types');
                    } else {
                        console.info('获取' + mCategory + '列表失败！');
                        console.info(strErro);
                    }
                });
            },
            getTypesChild: function (id) {
                Dictionary.getDictionaryChild('get', id, function getDictionaryChildListener(success, obj, strErro) {
                    if (success) {
                        for (var i in obj) {
                            obj[i].checked = false;
                            obj[i].项目支出内容 = obj[i].值;
                            obj[i].财务科目 = '';
                        }
                        dAddVm.typesChild = obj;

                    } else {
                        console.info('获取项目支出类型下级失败！');
                        console.info(strErro);
                    }
                });
            },
            getFinance: function () {
                Dictionary.getDictionaryList('get', '财务接口科目', function getDictionaryListListener(success, obj, strErro) {
                    if (success) {
                        dAddVm.finance = obj;
                    } else {
                        console.info('获取' + mCategory + '列表失败！');
                        console.info(strErro);
                    }
                });
            },
            clickSubmit: function () {
                console.info(dAddVm.typesInfo.经费占比);
                if (dAddVm.typesInfo.基本信息.名称 == '请选择') {
                    icon = 'icon-shibai1';
                    popover(icon, '提交失败：名称不能为空！');
                    setTimeout("$('.popover').remove()", 2000);
                } else {
                    if (dAddVm.typesInfo.基本信息.经费占比 <= 0) {
                        icon = 'icon-shibai1';
                        popover(icon, '提交失败：经费占比不能为0！');
                        setTimeout("$('.popover').remove()", 2000);
                    } else {
                        var arr = [];
                        for (var i = 0; i < dAddVm.typesChild.length; i++) {
                            if (dAddVm.typesChild[i].checked) {
                                arr.push(dAddVm.typesChild[i]);
                            }
                        }
                        dAddVm.typesInfo.经费模版项目支出内容列表 = arr;
                        dAddVm.addOrEditExpenditureType(dAddVm.typesInfo.$model);
                        console.info(dAddVm.typesInfo.$model);
                    }
                }
            },
            addOrEditExpenditureType: function (data) {
                ExpenditureTemplate.addOrEditExpenditureType('post', data, function addOrEditExpenditureTypeListener(success, obj, strErro) {
                    postBack(success, strErro, '添加成功！', '添加失败：', '.modal-add', function callBack() {
                        vm.query();
                    });
                });
            },
            clickBtnReturn: function () {
                $('.modal-add').modal('hide');
            }
        });

        dAddVm.getFinance();
        dAddVm.loadInfo();
        avalon.scan(document.body);
    });
});