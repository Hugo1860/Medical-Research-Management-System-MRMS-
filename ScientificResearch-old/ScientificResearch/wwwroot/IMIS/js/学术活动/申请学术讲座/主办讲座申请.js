$(function () {
    window.addVm = null;
    var departmentTree = null;
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
            types: [],
            shenHe: [],
            stateVal: '',
            onLoad: function () {
                addVm.getMeetingType();
                if (addVm.editType) {
                    addVm.title = '修改';
                    xueShuDetails = JSON.parse(sessionStorage.xueShuDetails);
                    addVm.getDetails(xueShuDetails.id);
                } else {
                    addVm.title = '添加';
                    addVm.model = {
                        编号: 0,
                        活动名称: '',
                        所属部门编号: 0,
                        活动经费: 0,
                        会议类型: '',
                        活动对象: '',
                        开始时间: '',
                        结束时间: '',
                        主办人编号: addVm.userInfo.人员.编号,
                        主讲人姓名: '',
                        主讲人国别: '',
                        主讲人单位: '',
                        主讲人学位: '',
                        主讲人职称: '',
                        主讲人简历路径: '',
                        讲座地点: '',
                        内容概述: ''
                    }
                    addVm.userName = addVm.userInfo.人员.姓名;
                    addVm.files = [];
                }
            },
            getDetails: function (id) {
                Lecture.getHostLectureDetails('get', id, function getHostLectureDetailsListener(success, obj, strErro) {
                    if (success) {
                        if (obj == null || obj.length == 0) {
                            addVm.model = [];
                            return;
                        } else {
                            addVm.shenHe = obj.审核情况;
                            obj = obj.学术任职详情;
                            addVm.model = {
                                编号: obj.编号,
                                活动名称: obj.活动名称,
                                所属部门编号: obj.所属部门编号,
                                活动经费: obj.活动经费,
                                会议类型: obj.会议类型,
                                活动对象: obj.活动对象,
                                开始时间: formatDate(obj.开始时间,'YY-MM-DD'),
                                结束时间: formatDate(obj.结束时间,'YY-MM-DD'),
                                主办人编号: obj.主办人编号,
                                主讲人姓名: obj.主讲人姓名,
                                主讲人国别: obj.主讲人国别,
                                主讲人单位: obj.主讲人单位,
                                主讲人学位: obj.主讲人学位,
                                主讲人职称: obj.主讲人职称,
                                主讲人简历路径: obj.主讲人简历路径,
                                讲座地点: obj.讲座地点,
                                内容概述: obj.内容概述
                            }

                            if (obj.主讲人简历路径 != '' && obj.主讲人简历路径 != null) {
                                addVm.files = obj.主讲人简历路径.split(',');
                            }
                            addVm.userName = obj.主办人姓名;
                            if(obj.所属部门名称!=null){
                                addVm.department = obj.所属部门名称;
                            }

                            $('.meeting-type').val(obj.会议类型);
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
            getMeetingType: function () {
                Dictionary.getDictionaryList('get', '会议类型', function getDictionaryListListener(success, obj, strErro) {
                    if (success) {
                        addVm.types = obj;
                        console.info('types');
                        console.info(obj);

                    } else {
                        console.info('获取会议类型失败：' + strErro);
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
                var department = addVm.inputVal('.department');
                var starttime = addVm.inputVal('.start-time');
                var endTime = addVm.inputVal('.end-time');

                addVm.model.主讲人简历路径 = addVm.files.join();
                if (activityName && department && starttime && endTime) {
                    if (addVm.files.length > 0) {
                        if (addVm.editType) {
                            var data = {
                                步骤编号: xueShuDetails.步骤编号,
                                状态值: addVm.stateVal,
                                备注: '',
                                附加数据: addVm.model.$model
                            }
                            addVm.editHostLecture(data);

                        } else {
                            addVm.addHostLecture(addVm.model.$model);
                        }
                    } else {
                        $.oaNotify.error(' 请上传简历！');
                    }

                }
            },
            addHostLecture: function (data) {
                Lecture.addHostLecture('post', data, function addHostLectureListener(success, obj, strErro) {
                    postBack(success, strErro, '提交成功！', '提交失败：', '.modal-add', function callBack() {
                        vm.query();
                    });
                });
            },
            editHostLecture: function (data) {
                Lecture.editHostLecture('post', data, function editHostLectureListener(success, obj, strErro) {
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
                    url: Code.URL_POST_UPLOAD_SPONSOR_RESUME,
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
            getEnableDepartment: function () {
                var setting = {
                    data: {
                        key: {
                            name: 'name'
                        },
                        simpleData: {
                            enable: true,
                            idKey: 'id',
                            pIdKey: 'pId',
                            rootPId: 0
                        },
                    },
                    callback: {
                        onClick: onClick
                    }
                };

                var nodes = [];
                Department.getEnableDepartmentList('get', '', function getEnableDepartmentListListener(success, obj, strErro) {
                    if (success) {
                        nodes = obj.reverse();
                        if (nodes != null) {

                            for (var i = 0, l = nodes.length; i < l; i++) {

                                nodes[i].id = nodes[i].编号;
                                nodes[i].name = nodes[i].名称.replace(/\.n/g, '.');
                                nodes[i].pId = nodes[i].上级部门编号;
                            }

                            departmentTree = $.fn.zTree.init($('.departmentTree'), setting, nodes);
                        }
                    } else {
                        alert('获取部门列表数据失败');
                        console.info(strErro);
                    }
                });

                function onClick(e, treeId, treeNode) {
                    var nodes = departmentTree.getSelectedNodes(),
                        text = '';
                    nodes.sort(function compare(a, b) {
                        return a.id - b.id;
                    });
                    for (var i = 0, l = nodes.length; i < l; i++) {
                        text += nodes[i].name + ",";
                    }
                    if (text.length > 0) text = text.substring(0, text.length - 1);
                    var cityObj = $('.department-box .department');
                    cityObj.val(text);
                    addVm.model.所属部门编号 = treeNode.编号;
                    addVm.department = treeNode.名称;
                    addVm.hideMenu();
                }
            },
            hideMenu: function () {
                $('.department-box #menuContent').fadeOut('fast');
                $('body').unbind('mousedown', addVm.onBodyDown());
            },
            onBodyDown: function () {
                if (!('.department-box #menuContent').length > 0) {
                    addVm.hideMenu();
                }
            },
            showMenu: function () {
                var obj = $('.department-box .department');
                var offset = $('.department-box .department').offset();
                $('.department-box #menuContent').css({
                    left: offset.left + 'px',
                    top: offset.top + obj.outerHeight() + 'px'
                }).slideDown('fast');

                $('body').bind('mousedown', addVm.onBodyDown());
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
        addVm.getEnableDepartment();
        avalon.scan(document.body);
    });
});