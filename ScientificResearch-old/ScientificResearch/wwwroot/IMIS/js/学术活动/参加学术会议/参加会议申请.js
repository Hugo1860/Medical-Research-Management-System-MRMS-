$(function () {
    window.addVm = null;
    var xueShuDetails;
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
            subjectOne: [],
            types: [],
            shenHe: [],
            stateVal: '',
            onLoad: function () {
                addVm.getSubjectOne();
                addVm.getMeetingType();
                if (addVm.editType) {
                    addVm.title = '修改会议';
                    xueShuDetails = JSON.parse(sessionStorage.xueShuDetails);
                    addVm.getDetails(xueShuDetails.id);
                } else {
                    addVm.title = '添加会议';
                    addVm.model = {
                        编号: 0,
                        会议名称: '',
                        会议主办单位: '',
                        会议承办单位: '',
                        研讨领域: '',
                        学科类型: '',
                        会议地址: '',
                        会议开始时间: '',
                        会议结束时间: '',
                        往返时间: 0,
                        会议网址: '',
                        会议简介及申请理由: '',
                        申请人编号: addVm.userInfo.人员.编号,
                        参会相关文件路径: ''
                    }
                    addVm.userName = addVm.userInfo.人员.姓名;
                    addVm.department = addVm.userInfo.人员.部门名称;
                    addVm.files = [];
                }
            },
            getDetails: function (id) {
                Meeting.getMeetingDetails('get', id, function getMeetingDetailsListener(success, obj, strErro) {
                    if (success) {
                        if (obj == null || obj.length == 0) {
                            addVm.model = [];
                            return;
                        } else {
                            addVm.shenHe = obj.申请情况;
                            obj = obj.参加会议详情;
                            addVm.model = {
                                编号: obj.编号,
                                会议名称: obj.会议名称,
                                会议主办单位: obj.会议主办单位,
                                会议承办单位: obj.会议承办单位,
                                研讨领域: obj.研讨领域,
                                学科类型: obj.学科类型,
                                会议地址: obj.会议地址,
                                会议开始时间: formatDate(obj.会议开始时间, 'YY-MM-DD'),
                                会议结束时间: formatDate(obj.会议结束时间, 'YY-MM-DD'),
                                往返时间: obj.往返时间,
                                会议网址: obj.会议网址,
                                会议简介及申请理由: obj.会议简介及申请理由,
                                申请人编号: obj.申请人编号,
                                参会相关文件路径: obj.参会相关文件路径
                            }

                            if (obj.参会相关文件路径 != '' && obj.参会相关文件路径 != null) {
                                addVm.files = obj.参会相关文件路径.split(',');
                            }
                            addVm.userName = obj.申请人姓名;
                            if (obj.申请人部门名称 != null) {
                                addVm.department = obj.申请人部门名称;
                            }
                        }

                    } else {
                        console.info('获取讲座详情失败！');
                        console.info(strErro);
                    }
                });
            },
            getHtmlDocName: function (url) {
                var arr = url.split('\\');
                return arr[arr.length - 1];
            },
            getSubjectOne: function () {
                var data = {
                    上级编号: 0
                }
                Subject.getSubjectList('get', data, function getSubjectListListener(success, obj, strErro) {
                    if (success) {
                        addVm.subjectOne = obj;
                    } else {
                        console.info('获取一级学科失败！');
                        console.info(strErro);
                    }
                })
            },
            getMeetingType: function () {
                Dictionary.getDictionaryList('get', '学术活动类型', function getDictionaryListListener(success, obj, strErro) {
                    if (success) {
                        addVm.types = obj;

                    } else {
                        console.info('获取学术活动类型失败：' + strErro);
                    }
                })
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
            clickStop: function () {
                addVm.stateVal = -2;
                addVm.postData();
            },
            clickSubmit: function () {
                addVm.stateVal = 1;
                addVm.postData();
            },
            postData: function () {
                var activityName = addVm.inputVal('.activity-name');
                var address = addVm.inputVal('.meeting-address');
                var starttime = addVm.inputVal('.start-time');
                var endTime = addVm.inputVal('.end-time');

                addVm.model.参会相关文件路径 = addVm.files.join();
                if (activityName && address && starttime && endTime) {
                    if (addVm.editType) {
                        var data = {
                            步骤编号: xueShuDetails.步骤编号,
                            状态值: addVm.stateVal,
                            备注: '',
                            附加数据: addVm.model.$model
                        }
                        addVm.editMeeting(data);

                    } else {
                        addVm.addMeeting(addVm.model.$model);
                    }
                }
            },
            addMeeting: function (data) {
                Meeting.addMeeting('post', data, function addMeetingListener(success, obj, strErro) {
                    postBack(success, strErro, '提交成功！', '提交失败：', '.modal-add', function callBack() {
                        vm.query();
                    });
                });
            },
            editMeeting: function (data) {
                Meeting.editMeeting('post', data, function editMeetingListener(success, obj, strErro) {
                    postBack(success, strErro, '提交成功！', '提交失败：', '.modal-add', function callBack() {
                        vm.query();
                    });
                });
            },
            clickUpload: function () {
                var data = new FormData();
                data.append('file', $("#input-file").get(0).files[0]);
                $('.fileUpload-box .loading').show();
                addVm.fileUpload(data);
            },
            fileUpload: function (data) {
                $.ajax({
                    url: Code.URL_POST_UPLOAD_MEETING_FILE,
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
                        $('.fileUpload-box .loading').hide();

                    }, error: function (e) {
                        e = JSON.parse(e);
                        $('.fileUpload-box .loading').hide();
                        $.oaNotify.error('上传失败：' + e.error);
                    }
                });
            },
            clickBtnReturn: function () {
                $('.modal').modal('hide');
            },
        });
        $('.modal-add .modal-body,.menuContent').mCustomScrollbar({
            theme: 'dark-3',
        });
        $('.form-time').datetimepicker({
            format: 'yyyy-mm-dd',
            minView: "month", //选择日期后，不会再跳转去选择时分秒
            todayBtn: 1,
            autoclose: 1,
            startDate: new Date(), //开始时间
            language: 'zh-CN'
        });
        addVm.onLoad();
        avalon.scan(document.body);
    });
});