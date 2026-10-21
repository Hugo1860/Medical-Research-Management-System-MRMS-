$(function () {
    window.vmDetails = null;
    var downloadDetails = JSON.parse(sessionStorage.downloadDetails);
    avalon.ready(function () {
        window.vmDetails = avalon.define({
            $id: 'details',
            model: downloadDetails,
            flieName: '',
            files: [],
            onLoad: function () {
                vmDetails.files = vmDetails.model.文件路径.split(',');
            },
            getHtmlDocName: function (url) {
                var arr = url.split('\\');
                return arr[arr.length - 1];
            },
            clickBtnReturn: function () {
                $('.modal').modal('hide');
            },
        });
        vmDetails.onLoad();
        avalon.scan(document.body);
    });


});