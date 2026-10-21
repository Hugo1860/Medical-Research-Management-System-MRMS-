$(function () {
    window.vm = null;
    if (sessionStorage.userInfo == null) {
        location.href = '/IMIS/views/login.html';
    }
    var bTime = getMonthStartAndEnd(0).beginTime.slice(0, 10)+' 00:00:00';
    var eTime = new Date().format('yyyy-MM-dd hh:mm:ss');
    var beginTime,endTime,timeVal;
    avalon.ready(function () {
        window.vm = avalon.define({
            $id: 'root',
            req: {
                Index: 1,
                Size: 18,
                工号: '',
                Like姓名: '',
                Begin登录时间: bTime,
                End登录时间: eTime
            },
            name: '',
            number:'',
            total: '',
            model: [],
            loaded: false,
            nothing:false,
            query: function () {
                vm.loaded = false;
                $.support.cors = true;
                Log.getLoginLog('get', vm.req.$model, function getLoginLogListener(success, obj, strErro) {
                    if (success) {
                        vm.loaded = true;
                        vm.total = obj.total;
                        if (obj == null || obj.list.length == 0) {
                            $('.pager').hide();
                            vm.model = [];
                            return;
                        } else {
                            obj = obj.list;
                            vm.model = obj;
                            vm.nothing = false;
                            $('.pager').show();
                        }
                        $('.pager').mamPager({
                            pageSize: vm.req.Size,                           //页大小
                            pageIndex: vm.req.Index,                     //当前页
                            recordTotal: vm.total,                      //数据总数
                            type: 1,
                            prevText: "&laquo;",                       //上一页显示内容
                            nextText: "&raquo;",
                            noData: "暂无数据",
                            pageChange: function (index) {
                                vm.req.Index = index;
                                vm.loaded = false;
                                vm.query();
                            }
                        });
                        $('.bs-tooltip').tooltip();
                    } else {
                        console.info('获取日志列表失败！');
                        console.info(strErro);
                    }
                });

            },
            search: function () {
                vm.req.Index = 1;
                vm.req.Like姓名 = vm.name;
                vm.req.工号 = vm.number;
                vm.query();
            },
            submit: function () {
                if (event.keyCode == 13) {
                    vm.search();
                }
            },
        });
        $('.time-form-list .beginTime').on('change', function () {
            beginTime = $(this).val();
        });
        $('.time-form-list .endTime').on('change', function () {
            endTime = $(this).val();
        });
        // var timeVal = $('.time-form .time-slot').children('option:selected').val();
        // var beginTime = getTime(parseInt(timeVal)).beginTime;
        // var endTime = getTime(parseInt(timeVal)).endTime;
        // vm.req.Begin登录时间 = beginTime;
        // vm.req.End登录时间 = endTime;

        $('.time-form .time-slot').on('change', function () {
            timeVal = $(this).children('option:selected').val();
            if (timeVal < 9) {
                var beginTime = getTime(parseInt(timeVal)).beginTime;
                var endTime = getTime(parseInt(timeVal)).endTime;
                vm.req.Begin登录时间 = beginTime;
                vm.req.End登录时间 = endTime;
                vm.req.Index = 1;
                vm.search();
            } else if (timeVal == 9) {
                $('.time-form-list').show();
            }
        })
        $('.time-form-list .btn-ok').on('click', function () {
            vm.req.Begin登录时间 = beginTime + ' 00:00:00';
            vm.req.End登录时间 = endTime + ' 23:59:59';
            $(this).parents('.form-box').hide();
            vm.req.Index = 1;
            vm.search();
        });
        $('.form-box .btn-close').on('click', function () {
            $(this).parents('.form-box').hide();
        });
        vm.query();
        avalon.scan(document.body);
    });
});