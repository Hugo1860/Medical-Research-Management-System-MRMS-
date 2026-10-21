$(function () {
    var dAddVm = null;
    var userInfo = JSON.parse(sessionStorage.userInfo).人员;
    avalon.ready(function () {
        dAddVm = avalon.define({
            $id: 'Add',
            model: {},
            userInfo: userInfo,
            files: [],
            loadInfo: function () {
                dAddVm.model = {
                    基本资料: {
                        编号: 0,
                        任职开始时间: '',
                        任职截止时间: '',
                        任职人员编号: userInfo.编号,
                        任职团体名称: '',
                        所任职务: '',
                        相关文件路径: '',
                        建立时间: new Date().format('yyyy-MM-dd'),
                        备注: ''
                    },
                    isHold: false
                };
            },
            inputVal: function (name) {
                if ($(name).val() != '') {
                    $(name).next().hide();
                    $(name).removeClass('error-input');
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
                dAddVm.model.基本资料.相关文件路径 = files.slice(1);
                if (tName && startTime && endTime) {
                    dAddVm.addLearningUser(dAddVm.model.$model);
                }
            },
            clickSubmit: function () {
                dAddVm.model.isHold = false;
                dAddVm.postData();
            },
            temporary: function () {
                dAddVm.model.isHold = true;
                dAddVm.postData();
            },
            addLearningUser: function (data) {
                Learning.addLearningUser('post', data, function addPeriodicalListener(success, obj, strErro) {
                    if (success) {
                        $.oaNotify.ok(' 提交成功！');
                        dAddVm.clickBtnReturn();
                        vm.query();
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
