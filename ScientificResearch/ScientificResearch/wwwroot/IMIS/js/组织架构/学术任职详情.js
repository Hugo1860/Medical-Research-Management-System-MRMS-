$(function () {
    window.vm = null;
    var xueShuDetails = JSON.parse(sessionStorage.xueShuDetails);
    debugger;
    avalon.ready(function () {
        window.vm = avalon.define({
            $id: 'root',
            model: [],
            details: xueShuDetails,
            flieName: '',
            files: [],
            getDetails: function (id) {
                Learning.getLearningDetails('get', id, function getLearningDetailsListener(success, obj, strErro) {
                    if (success) {
                        if (obj == null || obj.length == 0) {
                            vm.model = [];
                            return;
                        } else {
                            vm.model = obj;

                            if (obj.学术任职详情.相关文件路径 != '' && obj.学术任职详情.相关文件路径 != null) {
                                vm.files = obj.学术任职详情.相关文件路径.split(',');
                            }
                        }

                    } else {
                        console.info('获取学术任职详情失败！');
                        console.info(strErro);
                    }
                });
            },
            getClass: function (statue) {
                switch (statue) {
                    case '审核通过':
                        return 'state-mark';
                    case '审核不通过':
                        return 'state-overdue';
                    case '审核已终止':
                        return 'state-overdue';
                    case '待审核':
                        return 'state-accomplish';
                    default:
                        return '';
                }
            },
            getHtmlDocName: function (url) {
                var arr = url.split('\\');
                return arr[arr.length - 1];
            },

        });
        vm.getDetails(xueShuDetails.id);
        avalon.scan(document.body);
    });


});