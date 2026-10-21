$(function () {
    window.vm = null;
    if (sessionStorage.userInfo == null) {
        location.href = '/IMIS/views/login.html';
    }
    var zTreeObj;
    var treenode;
    avalon.ready(function () {
        window.vm = avalon.define({
            $id: 'root',
            req: {
                上级编号: 0,
                name: ''
            },
            arr: [],
            delType: false,
            query: function () {
                var setting = {
                    async: {
                        enable: true,
                        url: getUrl,
                        contentType: 'application/json',
                        autoParam: [],
                        dataType: 'json',
                        type: 'get',
                        dataFilter: filter
                    },
                    view: {
                        expandSpeed: "",
                        addHoverDom: addHoverDom,
                        removeHoverDom: removeHoverDom,
                        selectedMulti: true,
                    },
                    edit: {
                        enable: true,
                        showRemoveBtn: true,
                        showRenameBtn: true,
                        removeTitle: "删除",
                        renameTitle: "修改"
                    },
                    data: {
                        simpleData: {
                            enable: true,
                            idKey: "id",
                            pIdKey: "pId",
                        },
                        key: {
                            name: "name"
                        }
                    },
                    check: {
                        enable: true,
                    },

                    callback: {
                        beforeDrag: zTreeBeforeDrag,   //全部禁止拖拽
                        onClick: nodeOnClick,
                        beforeExpand: zTreeBeforeExpand,
                        beforeRemove: beforeRemove,
                        onRemove: onRemove,
                        beforeRename: beforeRename
                    }
                };

                function getUrl() {
                    var param = '?上级编号=' + vm.req.上级编号;
                    var url = encodeURI(encodeURI(Code.URL_GET_INDUSTRY_CODE_LIST + param));
                    return decodeURI(url);
                }

                function zTreeBeforeExpand(treeId, treeNode) {
                    vm.req.上级编号 = treeNode.编号;
                    return vm.req.上级编号;
                };

                function zTreeBeforeDrag(treeId, treeNodes) {
                    return false;
                };

                function filter(treeId, parentNode, childNodes) {
                    if (!childNodes) return null;
                    childNodes = childNodes.data;
                    for (var i = 0, l = childNodes.length; i < l; i++) {

                        childNodes[i].id = childNodes[i].编号;
                        childNodes[i].name = childNodes[i].名称.replace(/\.n/g, '.') + ' 【代码：' + childNodes[i].代码 + '】';
                        childNodes[i].pId = childNodes[i].上级编号;

                        if (childNodes[i].是否存在下级 == 1) {
                            childNodes[i].isParent = true;
                        } else {
                            childNodes[i].isParent = false;
                        }
                    }
                    return childNodes;
                }

                function beforeRename(treeId, treeNode, newName) {
                    if (newName.length == 0) {
                        setTimeout(function () {
                            var zTree = $.fn.zTree.getZTreeObj("treeDemo");
                            zTree.cancelEditName();
                            alert("节点名称不能为空.");
                        }, 0);
                        return false;
                    } else {
                        var arr = newName.split('【');
                        var name = arr[0].substring(0, arr[0].length).replace(/\s/g,"");
                        var code = arr[1].substring(3, arr[1].length - 1);

                        var data = {
                            编号: treeNode.编号,
                            上级编号: treeNode.上级编号,
                            名称: name,
                            代码: code,
                            级别: treeNode.级别,
                            排序: treeNode.排序,
                            备注: treeNode.备注
                        }
                       vm.editIndustryCode(data);
                    }
                }

                function nodeOnClick(e, treeId, treeNode) {
                    vm.req.上级编号 = treeNode.编号;
                    zTreeObj.checkNode(treeNode, !treeNode.checked, true);
                    treenode = treeNode;
                }

                var newCount = 1;

                function addHoverDom(treeId, treeNode) {
                    var sObj = $("#" + treeNode.tId + "_span");
                    if (treeNode.editNameFlag || $("#addBtn_" + treeNode.tId).length > 0) return;
                    var addStr = "<span class='button add' id='addBtn_" + treeNode.tId
                        + "' title='添加节点' onfocus='this.blur();'></span>";
                    sObj.after(addStr);

                    var btn = $("#addBtn_" + treeNode.tId);
                    if (btn) btn.bind("click", function () {
                        var zTree = $.fn.zTree.getZTreeObj("treeDemo");
                        var name = '新增节点' + (newCount++);
                        var data = {
                            编号: 0,
                            上级编号: treeNode.编号,
                            名称: name,
                            代码: '',
                            级别: treeNode.级别 + 1,
                            排序: 0,
                            备注: ""
                        }
                        console.info(name);
                        console.info(data);
                        vm.addIndustryCode(treeNode, data, name, zTree);

                    });
                };

                function removeHoverDom(treeId, treeNode) {
                    $("#addBtn_" + treeNode.tId).unbind().remove();
                    console.info(treeNode);
                };

                function beforeRemove(treeId, treeNode) {
                    if (vm.delType) {
                        return
                    } else {
                        if (treeNode.isParent) {
                            $.oaNotify.error(' 请先删除子节点！');
                            return false;
                        } else {
                            return confirm("确认删除 节点 -- 【" + treeNode.name + "】 吗？");
                        }
                    }
                }

                function onRemove(event, treeId, treeNode) {
                    vm.delType = false;
                    var arr = [];
                    arr.push(treeNode.编号);
                    vm.delIndustryCode(arr);
                }

                $(document).ready(function () {
                    zTreeObj = $.fn.zTree.init($("#treeDemo"), setting);
                    zTreeObj.expandAll(true);
                });

            },
            search: function () {
                var treeObj = $.fn.zTree.getZTreeObj("treeDemo");
                treeObj.checkAllNodes(false);
                var nodes = treeObj.getNodesByParamFuzzy("name", vm.req.name, null);
                treeObj.selectNode(nodes[0]);
                for (var i = 0; i < nodes.length; i++) {
                    zTreeObj.checkNode(nodes[i], !nodes[i].checked, true);
                }
            },
            submit: function () {
                if (event.keyCode == 13) {
                    vm.search();
                }
            },
            addIndustryCode: function (treeNode, data, name, zTree) {
                IndustryCode.addIndustryCode('post', data, function addIndustryCodeListener(success, obj, strErro) {
                    if (success) {
                        var newNode = {
                            id: 0,
                            编号: 0,
                            tId: treeNode.tId,
                            pId: treeNode.编号,
                            上级编号: treeNode.编号,
                            name: name,
                            名称: data.名称,
                            代码: '',
                            级别: treeNode.级别 + 1,
                        }
                        if ((!treeNode && event.target.tagName.toLowerCase() != "button" && $(event.target).parents("a").length == 0) || treeNode.zAsync) {
                            treeNode = zTreeObj.addNodes(treeNode, newNode);
                            var nodes = zTreeObj.getSelectedNodes();
                            zTree.reAsyncChildNodes(nodes[0], 'refresh', false);
                        } else {
                            zTree.reAsyncChildNodes(treeNode, "refresh");
                        }
                        $.oaNotify.ok(' 添加成功！');

                    } else {
                        $.oaNotify.error(strErro);
                        console.info('添加失败:错误原因' + strErro);
                    }
                });

            },
            editIndustryCode: function (data) {
                IndustryCode.editIndustryCode('post', data, function editIndustryCodeListener(success, obj, strErro) {
                    if (success) {
                        $.oaNotify.ok(' 修改成功！');

                    } else {
                        $.oaNotify.error(strErro);
                        console.info('修改失败:' + strErro);
                    }
                });
            },
            batchDel: function () {
                vm.delType = true;
                var data = zTreeObj.getCheckedNodes(true);
                if (data.length > 0) {
                    vm.arr = [];
                    for (var i = 0; i < data.length; i++) {
                        var halfCheck = data[i].getCheckStatus();
                        if (!halfCheck.half) {
                            vm.arr.push(data[i].编号);
                        }
                    }
                    $('.modal-del').modal('show');
                    $('.modal-del .modal-title').text('您确定要删除已选项吗？');
                } else {
                    $.oaNotify.error('请先勾选！');
                }
            },
            confirmDel: function (arr) {
                console.info(arr);
                IndustryCode.delIndustryCode('post', arr.$model, function delIndustryCodeListener(success, obj, strErro) {
                    var icon;
                    if (success) {
                        icon = 'icon-chenggong1';
                        popover(icon, '删除成功！');
                        setTimeout(function () {
                            $('.popover').remove();
                        }, 1000);
                        setTimeout(function () {
                            $('.modal-del').modal('hide');
                            vm.req.上级编号 = 0;
                            vm.query();
                        }, 500);
                    } else {
                        icon = 'icon-shibai1';
                        popover(icon, '删除失败' + strErro);
                        setTimeout("$('.popover').remove()", 2000);
                    }
                });
            },
            delIndustryCode: function (arr) {
                IndustryCode.delIndustryCode('post', arr, function delIndustryCodeListener(success, obj, strErro) {
                    if (success) {
                        $.oaNotify.ok('删除成功！');
                    } else {
                        $.oaNotify.error('删除失败:' + strErro)
                    }
                });
            },
            clickBtnReturn: function () {
                $('.modal').modal('hide');
            }
        })

        vm.query();
        avalon.scan(document.body);
    });
});