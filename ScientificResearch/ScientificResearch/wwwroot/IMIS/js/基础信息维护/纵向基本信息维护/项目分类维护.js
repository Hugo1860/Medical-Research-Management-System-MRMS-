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
                    var url = encodeURI(encodeURI(Code.URL_GET_PROJECT_TYPE_LIST + param));
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
                        childNodes[i].name = childNodes[i].名称.replace(/\.n/g, '.');
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
                        var data = {
                            编号: treeNode.编号,
                            上级编号: treeNode.上级编号,
                            名称: newName,
                            级别: treeNode.级别,
                            排序: treeNode.排序,
                            备注: treeNode.备注
                        }
                        vm.editProjectType(data);
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
                            级别: treeNode.级别 + 1,
                            排序: 0,
                            备注: ""
                        }
                        vm.addProjectType(treeNode, data, name, zTree);
                    });
                };

                function removeHoverDom(treeId, treeNode) {
                    $("#addBtn_" + treeNode.tId).unbind().remove();
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
                    vm.delProjectType(arr);
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
            addProjectType: function (treeNode, data, name, zTree) {
                ProjectType.addOrEditProjectType('post', data, function addOrEditProjectTypeListener(success, obj, strErro) {
                    if (success) {
                        var newNode = {
                            id: 0,
                            编号: 0,
                            tId: treeNode.tId,
                            pId: treeNode.编号,
                            上级编号: treeNode.编号,
                            name: name,
                            名称: name,
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
            editProjectType: function (data) {
                ProjectType.addOrEditProjectType('post', data, function addOrEditProjectTypeListener(success, obj, strErro) {
                    if (success) {
                        $.oaNotify.ok(' 修改成功！');

                    } else {
                        $.oaNotify.error(strErro);
                        console.info('修改失败:' + strErro);
                    }
                });
            },
            delProjectType: function (arr) {
                ProjectType.delProjectType('post', arr, function delProjectTypeListener(success, obj, strErro) {
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