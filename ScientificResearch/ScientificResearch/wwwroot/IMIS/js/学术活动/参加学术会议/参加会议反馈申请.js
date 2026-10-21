$(function () {
    window.addVm = null;
    var xueShuDetails = JSON.parse(sessionStorage.xueShuDetails);
    avalon.ready(function () {
        window.addVm = avalon.define({
            $id: 'Add',
            model: {},
            details: {},
            flieName: '',
            files: [],
            LectureFiles: [],
            userInfo: vm.userInfo,
            userName: '',
            department: '',
            title: '',
            feedback: vm.feedback,
            types: [],
            shenHe: [],
            stateVal: '',
            onLoad: function () {
                if (addVm.feedback) {
                    addVm.title = '修改反馈';
                    addVm.getMeetingFeedbackDetails(xueShuDetails.id);
                } else {
                    addVm.title = '添加反馈';
                    addVm.getMeetingDetails(xueShuDetails.id);
                    addVm.model = {
                        编号: 0,
                        参加会议编号: xueShuDetails.id,
                        活动持续分钟: 0,
                        参会人数: 0,
                        参会总结: '',
                        会议反馈文件路径:''
                    }
                    addVm.files = [];
                }
            },
            getMeetingDetails: function (id) {
                Meeting.getMeetingDetails('get', id, function getMeetingDetailsListener(success, obj, strErro) {
                    if (success) {
                        if (obj == null || obj.length == 0) {
                            addVm.details = [];
                            return;
                        } else {
                            addVm.shenHe = obj.审核情况;
                            addVm.details = obj.参加会议详情;
                            obj = obj.参加会议详情;

                            if (obj.参会相关文件路径 != '' && obj.参会相关文件路径 != null) {
                                addVm.LectureFiles = obj.参会相关文件路径.split(',');
                            }
                        }

                    } else {
                        console.info('获取会议详情失败！');
                        console.info(strErro);
                    }
                });
            },
            getMeetingFeedbackDetails: function (id) {
                Meeting.getMeetingFeedbackDetails('get', id, function getMeetingFeedbackDetailsListener(success, obj, strErro) {
                    if (success) {
                        if (obj == null || obj.length == 0) {
                            addVm.model = [];
                            return;
                        } else {
                            addVm.details = obj.参加会议反馈详情;
                            addVm.shenHe = obj.审核情况;
                            obj = obj.参加会议反馈详情;
                            addVm.model = {
                                编号: obj.编号,
                                参加会议编号: obj.参加会议编号,
                                活动持续分钟: obj.活动持续分钟,
                                参会人数: obj.参会人数,
                                参会总结: obj.参会总结,
                                会议反馈文件路径:obj.会议反馈文件路径
                            }
                            if (obj.参会相关文件路径 != '' && obj.参会相关文件路径 != null) {
                                addVm.LectureFiles = obj.参会相关文件路径.split(',');
                            }
                            if (obj.会议反馈文件路径 != '' && obj.会议反馈文件路径 != null) {
                                addVm.files = obj.会议反馈文件路径.split(',');
                            }

                        }

                    } else {
                        console.info('获取会议反馈详情失败！');
                        console.info(strErro);
                    }
                });
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
            clickStop: function () {
                addVm.stateVal = -2;
                addVm.postData();
            },
            clickSubmit: function () {
                addVm.stateVal = 1;
                addVm.postData();
            },
            postData: function () {
                var meetingTime = addVm.inputVal('.meeting-time');
                var meetingPeople = addVm.inputVal('.meeting-people');

                addVm.model.会议反馈文件路径 = addVm.files.join();
                if (meetingTime && meetingPeople) {
                    if (addVm.feedback) {
                        var data = {
                            步骤编号: xueShuDetails.步骤编号,
                            状态值: addVm.stateVal,
                            备注: '',
                            附加数据: addVm.model.$model
                        }
                        addVm.editMeetingFeedback(data);

                    } else {
                        addVm.addMeetingFeedback(addVm.model.$model);
                    }
                }
            },
            addMeetingFeedback: function (data) {
                Meeting.addMeetingFeedback('post', data, function addMeetingFeedbackListener(success, obj, strErro) {
                    postBack(success, strErro, '提交成功！', '提交失败：', '.modal-add', function callBack() {
                        vm.query();
                    });
                });
            },
            editMeetingFeedback: function (data) {
                Meeting.editMeetingFeedback('post', data, function editMeetingFeedbackListener(success, obj, strErro) {
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
                    url: Code.URL_POST_UPLOAD_MEETING_FEEDBACK_FILE,
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
        addVm.onLoad();
        avalon.scan(document.body);
    });
});