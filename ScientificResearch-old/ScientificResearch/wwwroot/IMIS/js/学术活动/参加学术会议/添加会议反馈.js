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
                    addVm.getLectureFeedbackDetails(xueShuDetails.id);
                } else {
                    addVm.title = '添加反馈';
                    addVm.getDetails(xueShuDetails.id);
                    addVm.model = {
                        编号: 0,
                        讲座编号: xueShuDetails.id,
                        专家姓名: '',
                        专家身份证号码: '',
                        专家护照号: '',
                        专家联系方式: '',
                        专家讲课费金额: 0,
                        结算方式: '',
                        活动持续分钟: 0,
                        参会人数: 0,
                        讲座附件路径: '',
                        讲座效果: ''
                    }
                    addVm.files = [];
                }
            },
            getDetails: function (id) {
                Lecture.getHostLectureDetails('get', id, function getHostLectureDetailsListener(success, obj, strErro) {
                    if (success) {
                        if (obj == null || obj.length == 0) {
                            addVm.details = [];
                            return;
                        } else {
                            addVm.shenHe = obj.审核情况;
                            addVm.details = obj.学术任职详情;
                            obj = obj.学术任职详情;

                            if (obj.主讲人简历路径 != '' && obj.主讲人简历路径 != null) {
                                addVm.LectureFiles = obj.主讲人简历路径.split(',');
                            }
                        }

                    } else {
                        console.info('获取讲座详情失败！');
                        console.info(strErro);
                    }
                });
            },
            getLectureFeedbackDetails: function (id) {
                Lecture.getLectureFeedbackDetails('get', id, function getLectureFeedbackDetailsListener(success, obj, strErro) {
                    if (success) {
                        if (obj == null || obj.length == 0) {
                            addVm.model = [];
                            return;
                        } else {
                            addVm.details = obj;
                            addVm.shenHe = obj.审核情况;
                            obj = obj.讲座反馈详情;
                            addVm.model = {
                                编号: obj.编号,
                                讲座编号: obj.讲座编号,
                                专家姓名: obj.专家姓名,
                                专家身份证号码: obj.专家身份证号码,
                                专家护照号: obj.专家护照号,
                                专家联系方式: obj.专家联系方式,
                                专家讲课费金额: obj.专家讲课费金额,
                                结算方式: obj.结算方式,
                                活动持续分钟: obj.活动持续分钟,
                                参会人数: obj.参会人数,
                                讲座附件路径: obj.讲座附件路径,
                                讲座效果: obj.讲座效果
                            }
                            if (obj.主讲人简历路径 != '' && obj.主讲人简历路径 != null) {
                                addVm.LectureFiles = obj.主讲人简历路径.split(',');
                            }
                            if (obj.讲座附件路径 != '' && obj.讲座附件路径 != null) {
                                addVm.files = obj.讲座附件路径.split(',');
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
                var department = addVm.inputVal('.department');
                var starttime = addVm.inputVal('.start-time');
                var endTime = addVm.inputVal('.end-time');

                addVm.model.讲座附件路径 = addVm.files.join();
                if (activityName && department && starttime && endTime) {
                    if (addVm.feedback) {
                        var data = {
                            步骤编号: xueShuDetails.步骤编号,
                            状态值: addVm.stateVal,
                            备注: '',
                            附加数据: addVm.model.$model
                        }
                        addVm.editLectureFeedback(data);

                    } else {
                        addVm.addLectureFeedback(addVm.model.$model);
                    }
                }
            },
            addLectureFeedback: function (data) {
                Lecture.addLectureFeedback('post', data, function addLectureFeedbackListener(success, obj, strErro) {
                    postBack(success, strErro, '提交成功！', '提交失败：', '.modal-add', function callBack() {
                        vm.query();
                    });
                });
            },
            editLectureFeedback: function (data) {
                Lecture.editLectureFeedback('post', data, function editLectureFeedbackListener(success, obj, strErro) {
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
                    url: Code.URL_POST_UPLOAD_LECTURE_ENCLOSURE,
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