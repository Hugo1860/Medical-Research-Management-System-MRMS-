$(function () {
    window.addVm = null;
    avalon.ready(function () {
        window.addVm = avalon.define({
            $id: 'Add',
            model: {},
            flieName: '',
            files: [],
            userInfo: vm.userInfo,
            userName: '',
            department: '',
            title: '',
            editType: vm.editType,
            onLoad: function () {
                if (addVm.editType) {
                    addVm.title = '修改下载';
                    var details = JSON.parse(sessionStorage.downloadDetails);
                    addVm.model = {
                        编号: details.编号,
                        文件名称: details.文件名称,
                        文件类型: details.文件类型,
                        文件路径: details.文件路径,
                        是否启用: details.是否启用,
                        上传人编号: details.上传人编号,
                        建立时间: details.建立时间,
                        备注: details.备注
                    }
                    addVm.userName = details.上传人姓名;
                    addVm.department = details.上传人部门名称;
                    addVm.files = details.文件路径.split(',');

                } else {
                    addVm.title = '添加下载';
                    addVm.model = {
                        编号: 0,
                        文件名称: '',
                        文件类型: '',
                        文件路径: '',
                        是否启用: true,
                        上传人编号: addVm.userInfo.人员.编号,
                        建立时间: new Date().format('yyyy-MM-dd hh:mm:ss'),
                        备注: ''
                    }
                    addVm.userName = addVm.userInfo.人员.姓名;
                    addVm.department = addVm.userInfo.人员.部门名称;

                    addVm.files = [];
                }
            },
            getHtmlDocName: function (url) {
                var arr = url.split('\\');
                return arr[arr.length - 1];
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
            clickSubmit: function () {
                var tName = addVm.inputVal('.tName');
                var type = addVm.inputVal('.file-type');

                addVm.model.文件路径 = addVm.files.join();
                if (tName && type) {
                    if (addVm.files.length > 0) {
                        addVm.addOrEditDownload(addVm.model.$model);
                    } else {
                        $.oaNotify.error(' 请上传文件！');
                    }

                }
            },
            addOrEditDownload: function (data) {
                Download.addOrEditDownload('post', data, function addOrEditDownloadListener(success, obj, strErro) {
                    postBack(success, strErro, '提交成功！', '提交失败：', '.modal-add', function callBack() {
                        vm.query();
                    });
                });
            },
            clickUpload: function () {
                var data = new FormData();
                data.append('file', $("#input-file").get(0).files[0]);
                $('.loading').show();
                addVm.fileUpload(data);
            },
            fileUpload: function (data) {
                $.ajax({
                    url: Code.URL_POST_UPLOAD_DOWNLOAD_FILE,
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
                            addVm.type = true;
                            for (var i in e.data) {
                                addVm.files.push(e.data[i]);
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
            clickBtnReturn: function () {
                $('.modal').modal('hide');
            },
        });
        $('.modal-add .modal-body').mCustomScrollbar({
            theme: 'dark-3',
        });
        addVm.onLoad();
        avalon.scan(document.body);
    });


});