$(function () {
    window.noticeVm = null;
    var id = JSON.parse(sessionStorage.noticeId);
    debugger;
    avalon.ready(function () {
        window.noticeVm = avalon.define({
            $id: 'details',
            model: [],
            files: [],
            getDetails: function () {
                Notice.getNoticeDetails('get', id, function getNoticeDetailsListener(success, obj, strErro) {
                    if (success) {
                        if (obj == null || obj.length == 0) {
                            noticeVm.model = [];
                            return;
                        } else {

                            noticeVm.model = obj;
                            noticeVm.files = obj.通知公告.相关文件路径.split(',');
                            $('.detailsPage').contents().find("body").html(noticeVm.model.通知公告.通知内容);
                            $('.detailsPage').contents().find("body").css('font-size', '12px');
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
            clickBtnReturn: function () {
                $('.modal').modal('hide');
            },
        });
        $('.nav-tabs a').on('click', function (e) {
            e.preventDefault();
            $(this).tab('show');
        });
        noticeVm.getDetails();
        avalon.scan(document.body);
    });
});