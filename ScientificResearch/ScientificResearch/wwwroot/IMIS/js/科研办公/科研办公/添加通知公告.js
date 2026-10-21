$(function () {
    window.addVm = null;
    var id = JSON.parse(sessionStorage.noticeId);
    var departmentTree = null;
    var roleTree = null;
    var userName = JSON.parse(sessionStorage.userInfo).人员.姓名;
    var um;
    var roleArr = [], peopleArr = [], departmentArr = [];
    var day = new Date();
    var Tomorrow = new Date(day.setTime(day.getTime() + 24 * 60 * 60 * 1000)).format('yyyy-MM-dd 00:00:00');
    var arrIds = [];
    avalon.ready(function () {
        window.addVm = avalon.define({
            $id: 'Add',
            model: {},
            files: [],
            title: '',
            type: '普通通知',
            peopleType: 0,
            oldPeopleType: '',
            editType: vm.editType,
            deepartmentId: vm.deepartmentId,
            arr: [],
            name: '',
            department: '',
            role: '',
            people: '',
            loadInfo: function () {
                //实例化编辑器
                UM.delEditor('UMContent');
                um = UM.getEditor('UMContent');
                if (addVm.editType) {
                    addVm.title = '修改通知公告';
                    addVm.getDetails();
                } else {
                    addVm.title = '添加通知公告';
                    addVm.model = {
                        基本资料: {
                            编号: 0,
                            通知类型: '',
                            通知标识: '',
                            发送人编号: 0,
                            通知名称: '',
                            关闭时间: Tomorrow,
                            相关文件路径: '',
                            通知内容: '',
                            接收人数: 0,
                            已接收人数: 0,
                            是否启用: true,
                            建立时间: new Date().format('yyyy-MM-dd hh:mm:ss'),
                            备注: ''
                        },
                        通知公告接收条件列表: [
                            {
                                编号: 0,
                                通知公告编号: 0,
                                接收者类型: 0,
                                接收者编号: 0
                            }
                        ]
                    }
                    addVm.name = userName;
                    addVm.peopleType = 0;
                }

            },
            getDetails: function () {
                Notice.getNoticeDetails('get', id, function getNoticeDetailsListener(success, obj, strErro) {
                    if (success) {
                        addVm.name = obj.通知公告.发送人姓名;
                        obj.通知公告.关闭时间 = formatDate(obj.通知公告.关闭时间, 'YY-MM-DD');
                        addVm.model = {
                            基本资料: obj.通知公告,
                            通知公告接收条件列表: obj.接收条件
                        }
                        if (obj.通知公告.相关文件路径 != null) {
                            addVm.files = obj.通知公告.相关文件路径.split(',');
                        }

                        um.ready(function () {
                            um.setContent(obj.通知公告.通知内容);
                        });
                        addVm.type = obj.通知公告.通知类型;
                        var arr = [];

                        if (obj.接收条件 != []) {
                            addVm.peopleType = obj.接收条件[0].接收者类型;
                            addVm.oldPeopleType = obj.接收条件[0].接收者类型;

                            for (var i in obj.接收条件) {
                                arr.push(obj.接收条件[i].接收者名称);
                                arrIds.push(obj.接收条件[i].接收者编号);
                            }
                            arr = arr.join();
                        } else {
                            addVm.oldPeopleType = 0;
                        }

                        switch (addVm.peopleType) {
                            case 0:
                                $('.configure-box').hide();
                                addVm.arrIds = '';
                                break;
                            case 10:
                                $('.configure-box').hide();
                                $('.configure-box:eq(2)').show();
                                addVm.people = arr;
                                peopleArr = arrIds;
                                break;
                            case 20:
                                $('.configure-box').hide();
                                $('.configure-box:eq(0)').show();
                                addVm.department = arr;
                                departmentArr = arrIds;
                                break;
                            case 30:
                                $('.configure-box').hide();
                                $('.configure-box:eq(1)').show();
                                addVm.role = arr;
                                roleArr = arrIds;
                                break;
                        }

                    } else {
                        console.info('获取通知详情失败！');
                        console.info(strErro);
                    }
                });
            },
            getUrl: function (url) {
                return decodeURI(encodeURI(encodeURI(url)));
            },
            getHtmlDocName: function (url) {
                var arr = url.split('\\');
                return arr[arr.length - 1];
            },
            changePeopleType: function (val) {
                switch (val) {
                    case 0:
                        $('.configure-box').hide();
                        arrIds = [0];
                        break;
                    case 10:
                        $('.configure-box').hide();
                        $('.configure-box:eq(2)').show();
                        arrIds = peopleArr;
                        break;
                    case 20:
                        $('.configure-box').hide();
                        $('.configure-box:eq(0)').show();
                        arrIds = departmentArr;
                        break;
                    case 30:
                        $('.configure-box').hide();
                        $('.configure-box:eq(1)').show();
                        arrIds = roleArr;
                        break;
                }
            },
            showUser: function () {
                $('.modal-user').modal('show');
                var url = addVm.getUrl('选择用户.html');
                if (addVm.editType) {
                    if (addVm.oldPeopleType == addVm.peopleType) {
                        sessionStorage.selectedUsers = JSON.stringify(addVm.model.通知公告接收条件列表);
                    } else {
                        sessionStorage.selectedUsers = JSON.stringify(null);
                    }
                } else {
                    sessionStorage.selectedUsers = JSON.stringify(null);
                }
                console.info('sessionStorage.selectedUsers');
                console.info(sessionStorage.selectedUsers);
                $.ajax({
                    url: url,
                    type: 'get',
                    success: function (data) {
                        $('.modal-user .modal-dialog').html("");
                        $('.modal-user .modal-dialog').append(data);

                    }
                });
            },
            getEnableDepartment: function () {
                var setting = {
                    check: {
                        enable: true,
                    },
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
                    view: {
                        dblClickExpand: false
                    },
                    callback: {
                        beforeClick: beforeClick,
                        onCheck: onCheck
                    }
                };
                var nodes = [];
                Department.getEnableDepartmentList('get', '', function getEnableDepartmentListListener(success, obj, strErro) {
                    if (success) {
                        nodes = obj.reverse();
                        if (nodes != null) {

                            for (var i = 0; i < nodes.length; i++) {

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

                function beforeClick(treeId, treeNode) {
                    var zTree = $.fn.zTree.getZTreeObj("departmentTree");
                    zTree.checkNode(treeNode, !treeNode.checked, null, true);
                    return false;
                }

                function onCheck(e, treeId, treeNode) {
                    var zTree = $.fn.zTree.getZTreeObj("departmentTree");
                    var nodes = zTree.getCheckedNodes(true);
                    var text = [];
                    departmentArr = [];
                    for (var i = 0; i < nodes.length; i++) {
                        text.push(nodes[i].name);
                        departmentArr.push(nodes[i].id);
                    }
                    addVm.department = text.join();
                    arrIds = departmentArr;
                    console.info(departmentArr);
                }
            },
            getRole: function () {
                var setting = {
                    check: {
                        enable: true,
                    },
                    data: {
                        key: {
                            name: 'name'
                        },
                        simpleData: {
                            enable: true,
                            idKey: 'id'
                        },
                    },
                    view: {
                        dblClickExpand: false
                    },
                    callback: {
                        beforeClick: beforeClick,
                        onCheck: onCheck
                    }
                };
                var nodes = [];
                Role.getRoleList('get', function getRoleListListener(success, obj, strErro) {
                    if (success) {
                        nodes = obj.reverse();
                        if (nodes != null) {
                            for (var i = 0; i < nodes.length; i++) {
                                nodes[i].id = nodes[i].编号;
                                nodes[i].name = nodes[i].名称;
                            }
                            roleTree = $.fn.zTree.init($('.roleTree'), setting, nodes);
                        }
                    } else {
                        alert('获取职位列表数据失败');
                        console.info(strErro);
                    }
                });

                function beforeClick(treeId, treeNode) {
                    var zTree = $.fn.zTree.getZTreeObj("roleTree");
                    zTree.checkNode(treeNode, !treeNode.checked, null, true);
                    return false;
                }

                function onCheck(e, treeId, treeNode) {
                    var zTree = $.fn.zTree.getZTreeObj("roleTree");
                    var nodes = zTree.getCheckedNodes(true);
                    var text = [];
                    roleArr = [];
                    for (var i = 0; i < nodes.length; i++) {
                        text.push(nodes[i].name);
                        roleArr.push(nodes[i].id);
                    }
                    addVm.role = text.join();
                    arrIds = roleArr;
                    console.info(roleArr);
                }
            },
            hideMenu: function () {
                $('.menuContent').fadeOut('fast');
                $('body').unbind('mousedown', addVm.onBodyDown());
            },
            onBodyDown: function () {
                if (!('.menuContent').length > 0) {
                    addVm.hideMenu();
                }
            },
            showMenu: function (name) {
                var cityObj = $(name);
                var cityOffset = $(name).offset();
                $(name).next().css({
                    left: cityOffset.left + "px",
                    top: cityOffset.top + cityObj.outerHeight() + "px"
                }).slideDown("fast");

                $("body").bind("mousedown", addVm.onBodyDown());
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
                var newArr = [];
                for (var i = 0; i < arrIds.length; i++) {
                    var obj = {
                        编号: 0,
                        通知公告编号: addVm.model.基本资料.编号,
                        接收者类型: parseInt(addVm.model.通知公告接收条件列表[0].接收者类型),
                        接收者编号: arrIds[i]
                    }
                    newArr.push(obj);
                }
                addVm.model.通知公告接收条件列表 = newArr;
                var noticeTitle = addVm.inputVal('.notice-title');
                addVm.model.基本资料.相关文件路径 = addVm.files.join();
                addVm.model.基本资料.通知内容 = um.getContent();
                addVm.model.基本资料.通知类型 = addVm.type;


                console.info(addVm.model);
                if (noticeTitle && addVm.model.通知公告接收条件列表 != []) {
                    addVm.addNotice(addVm.model.$model);
                }
            },
            clickUpload: function () {
                var data = new FormData();
                data.append('file', $("#input-file").get(0).files[0]);
                $('.loading').show();
                addVm.fileUpload(data);
            },
            fileUpload: function (data) {
                $.ajax({
                    url: Code.URL_POST_UPLOAD_NOTICE_FILE,
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
            addNotice: function (data) {
                Notice.addNotice('post', data, function addNoticeListener(success, obj, strErro) {
                    postBack(success, strErro, '提交成功！', '提交失败：', '.modal-add', function callBack() {
                        vm.query();
                    });
                });
            },
            clickBtnReturn: function () {
                $('.modal').modal('hide');
            },
        });
        $('.nav-tabs a').on('click', function (e) {
            e.preventDefault();
            $(this).tab('show');
        });

        $('.form-time').datetimepicker({
            format: 'yyyy-mm-dd',
            minView: "month", //选择日期后，不会再跳转去选择时分秒
            todayBtn: 1,
            autoclose: 1,
            startDate: Tomorrow, //开始时间
            language: 'zh-CN'
        });

        $('.menuContent').mCustomScrollbar({
            theme: 'dark-3'
        });
        addVm.loadInfo();
        addVm.getEnableDepartment();
        addVm.getRole();
        avalon.scan(document.body);
    });
});