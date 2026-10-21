$(function () {
    var icon;
    var dAddVm = null;
    avalon.ready(function () {
        dAddVm = avalon.define({
            $id: 'Add',
            type: vm.type,
            PeriodicalInfo: {},
            series: [],
            partition: [],
            collection: [],
            catalogue: [],
            catalogueType: [],
            title: '',
            issN: '',
            cN: '',
            jcR: '',
            loadInfo: function () {
                if (dAddVm.type) {
                    dAddVm.title = '修改期刊';
                    dAddVm.PeriodicalInfo = JSON.parse(sessionStorage.mPeriodicalInfo);
                } else {
                    dAddVm.title = '添加期刊';
                    dAddVm.PeriodicalInfo = {
                        编号: 0,
                        名称: '',
                        英文刊名: '',
                        主办单位: '',
                        期刊系列: '',
                        语种: '中文',
                        jcR分区: '',
                        收录情况: '',
                        编目名称: '',
                        编目类别: '',
                        创刊日期: '',
                        被引用次数: 0,
                        影响因子: 0,
                        特征因子分值: 0,
                        issN号: '',
                        年度: '',
                        cN号: '',
                        出版周期: '',
                        出版地: '',
                        期刊开本: ''
                    };
                }
            },

            clickSubmit: function () {
                dAddVm.PeriodicalInfo.issN号 = dAddVm.issN;
                dAddVm.PeriodicalInfo.cN号 = dAddVm.cN;
                dAddVm.PeriodicalInfo.jcR分区 = dAddVm.jcR;
                dAddVm.PeriodicalInfo.创刊时间 = $('.modal-add .form-time').val();

                if (dAddVm.PeriodicalInfo.英文刊名 == '') {
                    dAddVm.PeriodicalInfo.英文刊名 = null;
                }

                var year = $('.modal-add .year').val();

                if (year != '') {
                    dAddVm.PeriodicalInfo.年度 = parseInt(year);
                } else {
                    dAddVm.PeriodicalInfo.年度 = null;
                }

                var list = '';
                $('.modal-add .collection option:selected').each(function () {
                    list += ',' + $(this).val();
                });
                dAddVm.PeriodicalInfo.收录情况 = list.slice(1);
                if (dAddVm.PeriodicalInfo.名称 == '') {
                    erroTip('期刊名称不能为空!');
                } else {
                    if (dAddVm.PeriodicalInfo.收录情况 == '') {
                        erroTip('收录情况不能为空!');
                    } else {
                        if (dAddVm.PeriodicalInfo.年度 == null) {
                            erroTip('年度不能为空!');
                        } else {
                            dAddVm.addOrEditPeriodical(dAddVm.PeriodicalInfo.$model);
                        }
                    }
                }

                function erroTip(text) {
                    icon = 'icon-shibai1';
                    popover(icon, '提交失败：' + text);
                    setTimeout("$('.popover').remove()", 2000);
                }
            },
            addOrEditPeriodical: function (data) {
                Periodical.addOrEditPeriodical('post', data, function addPeriodicalListener(success, obj, strErro) {
                    postBack(success, strErro, '提交成功！', '提交失败：', '.modal-add', function callBack() {
                        sessionStorage.removeItem('mPeriodicalInfo');
                        vm.query();
                    });
                });
            },
            getPeriodicalSeries: function () {
                Periodical.getPeriodicalSeries('get', '期刊系列', function getPeriodicalSeriesListener(success, obj, strErro) {
                    if (success) {
                        dAddVm.series = obj;
                        $('.series').val(dAddVm.PeriodicalInfo.期刊系列);
                    } else {
                        console.info('获取期刊系列失败！');
                        console.info(strErro);
                    }
                })
            },
            getPeriodicalPartition: function () {
                Periodical.getPeriodicalSeries('get', '期刊JCR分区', function getPeriodicalSeriesListener(success, obj, strErro) {
                    if (success) {
                        dAddVm.partition = obj;
                        $('.partition').val(dAddVm.PeriodicalInfo.jcR分区);
                    } else {
                        console.info('获取期刊JCR分区失败！');
                        console.info(strErro);
                    }
                })
            },
            getPeriodicalCollection: function () {
                Periodical.getPeriodicalSeries('get', '期刊收录数据库', function getPeriodicalSeriesListener(success, obj, strErro) {
                    if (success) {
                        dAddVm.collection = obj;
                        dAddVm.initMultiselect('.modal-add .collection');
                        var arr = dAddVm.PeriodicalInfo.收录情况.split(',');
                        $('.modal-add .collection').multiselect('select', arr);
                    } else {
                        console.info('获取期刊收录数据库失败！');
                        console.info(strErro);
                    }
                })
            },
            initMultiselect: function (obj) {
                $(obj).multiselect({
                    buttonWidth: '100%',
                    maxHeight: 280,
                    enableFiltering: true,
                    includeSelectAllOption: true,
                    checkAllText: "全选",
                    nonSelectedText: '请选择',
                    nSelectedText: '个被选中',
                    allSelectedText: '全选',
                    selectAllText: '全选',
                    filterPlaceholder: '输入关键字...',
                });
            },
            getPeriodicalCatalogue: function () {
                Periodical.getPeriodicalSeries('get', '期刊编目名称', function getPeriodicalSeriesListener(success, obj, strErro) {
                    if (success) {
                        dAddVm.catalogue = obj;
                        $('.catalogue').val(dAddVm.PeriodicalInfo.编目名称);
                    } else {
                        console.info('获取期刊编目名称失败！');
                        console.info(strErro);
                    }
                })
            },
            getPeriodicalCatalogueType: function () {
                Periodical.getPeriodicalSeries('get', '期刊编目类别', function getPeriodicalSeriesListener(success, obj, strErro) {
                    if (success) {
                        dAddVm.catalogueType = obj;
                        $('.modal-add .catalogueType').multiselect({
                            buttonWidth: '100%',
                            maxHeight: 240,
                            nonSelectedText: '请选择',
                            enableFiltering: true,//是否显示搜索
                            filterPlaceholder: '输入关键字...',
                            onChange: function (option, checked, select) {
                                dAddVm.PeriodicalInfo.编目类别 = $(option).val();
                            }
                        });
                        console.info(dAddVm.PeriodicalInfo.编目类别);
                        $('.modal-add .catalogueType').multiselect('select', [dAddVm.PeriodicalInfo.编目类别]);
                    } else {
                        console.info('获取期刊编目类别失败！');
                        console.info(strErro);
                    }
                })
            },
            clickBtnReturn: function () {
                $('.modal-add').modal('hide');
            }
        });
        $('.modal-add .modal-body,.multiselect-container').mCustomScrollbar({
            theme: 'dark-3'
        });
        $('.modal-add .year').datetimepicker({
            format: 'yyyy',
            autoclose: 1,
            startView: 4,
            minView: 4,
            language: 'zh-CN',
        });
        $('.modal-add .form-time').datetimepicker({
            format: 'yyyy-mm-dd',
            minView: "month",
            todayBtn: 1,
            autoclose: 1,
            language: 'zh-CN'
        });
        dAddVm.loadInfo();
        dAddVm.getPeriodicalSeries();
        dAddVm.getPeriodicalPartition();
        dAddVm.getPeriodicalCollection();
        dAddVm.getPeriodicalCatalogue();
        dAddVm.getPeriodicalCatalogueType();
        avalon.scan(document.body);
    });
});