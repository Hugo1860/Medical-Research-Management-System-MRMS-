$(function () {
    var dAddVm = null;
    avalon.ready(function () {
        dAddVm = avalon.define({
            $id: 'Add',
            model: {},
            editInfo: {},
            flieName: '',
            files: [],
            loadInfo: function () {
                var editInfo = JSON.parse(sessionStorage.editInfo);
                dAddVm.editInfo = editInfo;
                dAddVm.files = editInfo.相关文件路径.split(',');
                dAddVm.flieName = oa.getHtmlDocName(editInfo.相关文件路径)
                dAddVm.model = {
                    步骤编号: editInfo.步骤编号,
                    状态值: 1,
                    备注: editInfo.备注,
                    附加数据: {
                        基本资料: {
                            编号: editInfo.编号,
                            任职开始时间: formatDate(editInfo.任职开始时间,'YY-MM-DD'),
                            任职截止时间: formatDate(editInfo.任职截止时间,'YY-MM-DD'),
                            任职人员编号: editInfo.任职人员编号,
                            任职团体名称: editInfo.任职团体名称,
                            所任职务: editInfo.所任职务,
                            相关文件路径: editInfo.相关文件路径,
                            建立时间: editInfo.建立时间,
                            备注: editInfo.备注
                        },
                        isHold: false
                    }
                }
                console.info(dAddVm.model);
            },
            inputVal: function (name) {
                if ($(name).val() != '') {
                    $(name).next().hide();
                    $(name).removeClass('error-input');
                    console.info($(name).val());
                    return true;
                } else {
                    $(name).next().show();
                    $(name).addClass('error-input');
                    return false;
                }
            },
            postData: function () {
                var tName = dAddVm.inputVal('.tName');
                var startTime = dAddVm.inputVal('.start-time');
                var endTime = dAddVm.inputVal('.end-time');
                var files = '';
                for (var j in dAddVm.files) {
                    files += ',' + dAddVm.files[j];
                }
                console.info(files);
                dAddVm.model.附加数据.基本资料.相关文件路径 = files.slice(1);
                if (tName && startTime && endTime) {
                    dAddVm.editLearningUser(dAddVm.model.$model);
                }
            },
            clickSubmit: function () {
                dAddVm.model.状态值 = 1;
                dAddVm.model.附加数据.isHold = false;
                dAddVm.postData();
            },
            temporary: function () {
                dAddVm.model.状态值 = 1;
                dAddVm.model.附加数据.isHold = true;
                dAddVm.postData();
            },
            clickStop: function () {
                dAddVm.model.状态值 = -2;
                dAddVm.model.附加数据.isHold = false;
                dAddVm.postData();
            },
            editLearningUser: function (data) {
                Learning.editLearningUser('post', data, function addPeriodicalListener(success, obj, strErro) {
                    if (success) {
                        $.oaNotify.ok(' 提交成功！');
                        sessionStorage.removeItem('addXueShu');
                        dAddVm.clickBtnReturn();
                        vm.search();
                    } else {
                        $.oaNotify.error(' 提交失败：' + strErro);
                    }
                });
            },
            clickUpload: function () {
                var data = new FormData();
                data.append('file', $("#input-file").get(0).files[0]);
                $('.loading').show();
                dAddVm.fileUpload(data);
            },
            fileUpload: function (data) {
                $.ajax({
                    url: Code.URL_POST_UPLOAD_LEARNING_FILE,
                    type: "POST",
                    processData: false,
                    contentType: false,
                    data: data,
                    dataType: 'text',
                    success: function (e) {
                        e = JSON.parse(e);
                        if (e.error) {
                            $.oaNotify.error(' 上传失败：' + e.error);
                        } else {
                            $.oaNotify.ok(' 上传成功!');
                            dAddVm.type = true;
                            for (var i in e.data) {
                                dAddVm.files.push(e.data[i]);
                            }
                        }

                        $('.loading').hide();

                    }, error: function (e) {
                        e = JSON.parse(e);
                        $('.loading').hide();
                        $.oaNotify.error('上传失败：' + e.error);
                    }
                });
            },
            getHtmlDocName: function (url) {
                var arr = url.split('\\');
                return arr[arr.length - 1];
            },
            getUrl: function (url) {
                return decodeURI(url);
            },
            clickBtnReturn: function () {
                $('.modal-add').modal('hide');
            }
        });
        $('.modal-add .modal-body').mCustomScrollbar({
            theme: 'dark-3',
        });
        $('.modal-add .form-time').datetimepicker({
            format: 'yyyy-mm-dd',
            minView: "month",
            todayBtn: 1,
            autoclose: 1,
            language: 'zh-CN'
        });

        dAddVm.loadInfo();
        avalon.scan(document.body);
    });
});
