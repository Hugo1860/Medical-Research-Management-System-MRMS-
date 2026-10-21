$(function () {
    $.fn.extend({
        animateCss: function (animationName, callback) {
            var animationEnd = "webkitAnimationEnd mozAnimationEnd MSAnimationEnd oanimationend animationend";
            $(this).addClass("animated " + animationName).one(animationEnd, function () {
                $(this).removeClass("animated " + animationName);
                if (callback != null)
                    callback();
            });
        }
    });
});

//提示组件
(function () {

    function seniorTimer(callback, delay) {
        var timerId, start, remaining = delay;

        this.pause = function () {
            window.clearTimeout(timerId);
            remaining -= new Date() - start;
        };

        this.resume = function () {
            start = new Date();
            window.clearTimeout(timerId);
            timerId = window.setTimeout(callback, remaining);
        };

        this.resume();
    }

    function notify(options) {
        var self = this;
        var timer;
        this.opts = $.extend({}, $.oaNotify.defaultOptions, options);

        function initContainer() {
            if (self.opts.element instanceof $) {
                self.container = self.opts.element.parent().css("position", "relative");
            } else {
                self.container = $("." + self.opts.containerClassName + "");
                if (self.container.length == 0) {
                    self.container = $("<div class='" + self.opts.containerClassName + "'></div>");
                    $("body").append(self.container);
                }
            }
        }

        function initNotify() {
            var className = self.opts.className;
            if (self.opts.state != '' && self.opts.state != null)
                className = className + ' ' + className + '-' + self.opts.state;
            self.notify = $("<div class='" + className + "'>" + self.opts.content + "</div>");
            self.container.append(self.notify);
            if (self.opts.element instanceof $)
                self.setPosition();
        }

        function initEvent() {
            if (self.opts.clickToClose)
                self.notify.on("click", self.close.bind(self));
            if (self.opts.closeDelay != 0) {
                timer = new seniorTimer(self.close.bind(self), self.opts.closeDelay);
                self.notify.on('mouseenter', function () {
                    timer.pause();
                }).on('mouseleave', function () {
                    timer.resume();
                });
            }

        }

        initContainer();
        initNotify();
        initEvent();

        this.open();
    }

    notify.prototype.container = {};
    notify.prototype.notify = {};
    notify.prototype.open = function () {
        if (this.opts.openClassName == "")
            this.notify.fadeIn();
        else
            this.notify.animateCss(this.opts.openClassName);
    }
    notify.prototype.close = function () {
        if (this.opts.closeClassName == "")
            this.notify.fadeOut(this.destroy.bind(this));
        else
            this.notify.animateCss(this.opts.closeClassName, this.destroy.bind(this));
    }
    notify.prototype.destroy = function () {
        this.notify.remove();
        //是否删除父容器
        if (this.container.children().length == 0) {
            this.container.remove();
        }
        this.opts.closeCallback();
    }
    notify.prototype.setPosition = function () {
        var pos = this.opts.element.position();

        var w1 = this.opts.element.outerWidth();
        var m = this.opts.element.css('marginLeft');
        if (m != null)
            w1 += parseInt(m);
        m = this.opts.element.css('marginRigth');
        if (m != null)
            w1 += parseInt(m);
        var h1 = this.opts.element.outerHeight();
        var w2 = this.notify.outerWidth();
        var h2 = this.notify.outerHeight();
        var top = 0;
        var left = 0;
        switch (this.opts.position) {
            case "top":
                top = pos.top - h2;
                if (w2 > w1)
                    left = -((w2 - w1) / 2);
                else
                    left = (w2 - w1) / 2;
                break;
            case "bottom":
                top = pos.top + h1;
                left = pos.left - (w2 - w1) / 2;
                break;
            case "left":
                top = pos.top - (h2 - h1) / 2;
                left = pos.left - w2;
                break;
            case "rigth":
                top = pos.top - (h2 - h1) / 2;
                left = pos.left + w1;
                break;
            default:
        }
        this.notify.css({position: "absolute", top: top, left: left});
    }

    function createNotify(content, options, state) {
        return new notify($.extend({}, {content: content, state: state}, options));
    }

    $.oaNotify = {
        info: function (content, options) {
            return createNotify(content, options, '');
        },
        ok: function (content, options) {
            return createNotify(content, options, 'ok');
        },
        error: function (content, options) {
            return createNotify(content, options, 'error');
        }
    }

    $.fn.oaNotify = function (content, options) {
        $(this).each(function () {
            new notify($.extend({}, {element: $(this), content: content}, options));
        });
        return this;
    }

    $.oaNotify.defaultOptions = {
        containerClassName: "oa-notify-container",
        className: "oa-notify",
        state: '',

        openClassName: "",

        closeDelay: 2000,
        clickToClose: true,
        closeClassName: "",
        closeCallback: function () {
        },

        content: "",

        element: null,
        position: "top"
    }
    $.oaNotify.defaults = function (options) {
        $.oaNotify.defaultOptions = $.extend({}, $.oaNotify.defaultOptions, options);
    }
})();


(function () {
    window.oa = {};
    //获取到 url地址中的参数
    oa.getUrlParam = function (key) {
        key = key.replace(/[\[\]]/g, "\\$&");
        var regex = new RegExp("[?&]" + key + "(=([^&#]*)|&|#|$)"),
            results = regex.exec(window.location.href);
        if (!results) return null;
        if (!results[2]) return '';
        return decodeURIComponent(results[2].replace(/\+/g, " "));
    }
    //获取url中的文件名
    oa.getHtmlDocName = function (url) {
        var str = url;
        str = str.substring(str.lastIndexOf("/") + 1);
        str = str.substring(0, str.lastIndexOf("."));
        return str;
    }
    oa.headerHeight = function () {
        var height = $('.page-header').height();
        $('.conmmon').css('margin-top', height + 'px');
    }

})();

//时间组件设置
$(function () {
    (function ($) {
        $.fn.datetimepicker.dates['zh-CN'] = {
            days: ["星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六", "星期日"],
            daysShort: ["周日", "周一", "周二", "周三", "周四", "周五", "周六", "周日"],
            daysMin: ["日", "一", "二", "三", "四", "五", "六", "日"],
            months: ["一月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "十一月", "十二月"],
            monthsShort: ["一月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "十一月", "十二月"],
            today: "今天",
            suffix: [],
            meridiem: ["上午", "下午"]
        };
    }(jQuery));

    //获取当前时间
    function getnowtime() {
        var mydate = new Date();
        var year = mydate.getFullYear();
        var month = mydate.getMonth() + 1;
        var time = year + '-' + month;

        $("#datetimepicker").val(time);
    }

    getnowtime();

    // //时间选择
    $('.form-time').datetimepicker({
        format: 'yyyy-mm-dd',
        minView: "month", //选择日期后，不会再跳转去选择时分秒
        todayBtn: 1,
        autoclose: 1,
        language: 'zh-CN'
    });
    // $('#datetimepicker').datetimepicker({
    //     format: 'yyyy-mm',
    //     weekStart: 1,
    //     autoclose: true,
    //     startView: 3,
    //     minView: 3,
    //     forceParse: false,
    //     language: 'zh-CN',
    //     linkField: "mirror_field"
    // });
    // $('.form-datetime').datetimepicker({
    //     format: 'yyyy-mm-dd hh:ii:00',
    //     showMeridian: true,
    //     autoclose: true,
    //     todayBtn: true,
    //     language: 'zh-CN'
    // });
    // $('.form-time-all').datetimepicker({
    //     format: 'yyyy-mm-dd 00:00:00',
    //     minView: "month", //选择日期后，不会再跳转去选择时分秒
    //     todayBtn: 1,
    //     autoclose: 1,
    //     language: 'zh-CN'
    // });
    // $('.year').datetimepicker({
    //     format: 'yyyy',
    //     todayBtn: 1,
    //     autoclose: 1,
    //     startView: 4,
    //     minView: 4,
    //     language: 'zh-CN',
    // });
    // $('.form-his').datetimepicker({
    //     format: 'hh:ii:00',
    //     weekStart: 1,
    //     todayBtn: 1,
    //     todayHighlight: 1,
    //     startView: 1,
    //     minView: 0,
    //     maxView: 1,
    //     forceParse: 0,
    //     showMeridian: true,
    //     autoclose: true,
    //     language: 'zh-CN'
    // });
});


//成功、失败提示框
function popover(status, msg) {
    var popover = '<div class="popover fade right in" role="tooltip" id="popover984305">' +
        '<div class="arrow"></div><div class="popover-content">' +
        '<i class="icon iconfont ' + status + '"></i><span>' + msg + '</span></div></div>';

    $('.tip-box').append(popover);
};

function changeUrl(a) {
    var url = $(a).attr('href');
    $.ajax({
        url: url,
        type: 'get',
        success: function (data) {
            $('.modal-add .modal-dialog').html('');
            $('.modal-add .modal-dialog').append(data);
        }
    });
}


//时间戳转日期，例如：2015-05-01 10:33:21
Date.prototype.format = function (format) {
    var date = {
        "M+": this.getMonth() + 1,
        "d+": this.getDate(),
        "h+": this.getHours(),
        "m+": this.getMinutes(),
        "s+": this.getSeconds(),
        "q+": Math.floor((this.getMonth() + 3) / 3),
        "S+": this.getMilliseconds()
    };
    if (/(y+)/i.test(format)) {
        format = format.replace(RegExp.$1, (this.getFullYear() + '').substr(4 - RegExp.$1.length));
    }
    for (var k in date) {
        if (new RegExp("(" + k + ")").test(format)) {
            format = format.replace(RegExp.$1, RegExp.$1.length == 1
                ? date[k] : ("00" + date[k]).substr(("" + date[k]).length));
        }
    }
    return format;
}
//任意设置时间模式
function formatDate(time, format) {
    var date = new Date(time);

    var year = date.getFullYear(),
        month = date.getMonth() + 1,//月份是从0开始的
        day = date.getDate(),
        hour = date.getHours(),
        min = date.getMinutes(),
        sec = date.getSeconds();
    var preArr = Array.apply(null, Array(10)).map(function (elem, index) {
        return '0' + index;
    });////开个长度为10的数组 格式为 00 01 02 03

    var newTime = format.replace(/YY/g, year)
        .replace(/MM/g, preArr[month] || month)
        .replace(/DD/g, preArr[day] || day)
        .replace(/hh/g, preArr[hour] || hour)
        .replace(/mm/g, preArr[min] || min)
        .replace(/ss/g, preArr[sec] || sec);

    return newTime;
};
//如：var date = new Date();
//formatDate(date.getTime());//2017-05-12 10:05:44
//formatDate(date.getTime(),'YY年MM月DD日');//2017年05月12日
//formatDate(date.getTime(),'今天是YY/MM/DD hh:mm:ss');//今天是2017/05/12 10:07:45

function getStrTime(timestamp) {
    var date = new Date();
    date.setTime(timestamp);
    var strTime = date.format('yyyy-MM-dd hh:mm:ss')
    return strTime;
}

function changeUrl(a) {
    var url = $(a).attr('href');
    $.ajax({
        url: url,
        type: 'get',
        success: function (data) {
            $('.modal-add .modal-dialog').html("");
            $('.modal-add .modal-dialog').append(data);
            var icon = 'icon-chenggong1';
        }
    });
}

function getTime(val) {
    var morningStamp = Date.parse(new Date(new Date().setHours(0, 0, 0, 0)));
    var time = {
        beginTime: '',
        endTime: ''
    }
    switch (val) {
        case 0:
            time.beginTime = '2018-01-01 00:00:00';
            time.endTime = getStrTime(Date.parse(new Date()));
            return time;
            break;
        case 1:
            time.beginTime = getStrTime(morningStamp);
            time.endTime = getStrTime(Date.parse(new Date()));
            return time;
            break;
        case 2:
            time.beginTime = getWeekStartAndEnd(0).beginTime;
            time.endTime = getStrTime(Date.parse(new Date()));

            return time;
            break;
        case 3:
            time.beginTime = getWeekStartAndEnd(-1).beginTime;
            time.endTime = getWeekStartAndEnd(-1).endTime;
            return time;
            break;

        case 4:
            time.beginTime = getMonthStartAndEnd(0).beginTime;
            time.endTime = getStrTime(Date.parse(new Date()));
            return time;
            break;

        case 5:
            time.beginTime = getMonthStartAndEnd(-1).beginTime;
            time.endTime = getMonthStartAndEnd(-1).endTime;
            return time;
            break;

        case 6:
            time.beginTime = getMonthStartAndEnd(-3).beginTime;
            time.endTime = getStrTime(Date.parse(new Date()));
            return time;
            break;
        case 7:
            time.beginTime = getMonthStartAndEnd(-6).beginTime;
            time.endTime = getStrTime(Date.parse(new Date()));
            return time;
            break;
        case 8:
            time.beginTime = getMonthStartAndEnd(-11).beginTime;
            time.endTime = getStrTime(Date.parse(new Date()));
            return time;
            break;
    }
}

function getWeekStartAndEnd(AddWeekCount) {
    var millisecond = 24 * 60 * 60 * 1000;
    var currentDate = new Date();
    var currentDate = new Date(currentDate.getTime() + (millisecond * 7 * AddWeekCount));
    var thisWeek = currentDate.getDay();
    var minusDay = thisWeek != 0 ? thisWeek - 1 : 6;
    var currentWeekFirstDay = new Date(new Date(currentDate.getTime() - (millisecond * minusDay)).setHours(0, 0, 0, 0));
    var currentWeekLastDay = new Date(new Date(currentWeekFirstDay.getTime() + (millisecond * 6)).setHours(23, 59, 59, 59));

    beginTime = getStrTime(Date.parse(currentWeekFirstDay));
    endTime = getStrTime(Date.parse(currentWeekLastDay));
    var time = {
        beginTime: beginTime,
        endTime: endTime
    }
    return time;
}

function getMonthStartAndEnd(AddMonthCount) {
    var currentDate = new Date();
    var month = currentDate.getMonth() + AddMonthCount;
    if (month < 0) {
        var n = parseInt((-month) / 12);
        month += n * 12;
        currentDate.setFullYear(currentDate.getFullYear() - n);
    }
    currentDate = new Date(currentDate.setMonth(month));
    var currentMonth = currentDate.getMonth();
    var currentYear = currentDate.getFullYear();
    var currentMonthFirstDay = new Date(currentYear, currentMonth, 1);
    var currentMonthLastDay = new Date(currentYear, currentMonth + 1, 0);
    currentMonthFirstDay = new Date(new Date(currentMonthFirstDay).setHours(0, 0, 0, 0));
    currentMonthLastDay = new Date(new Date(currentMonthLastDay).setHours(23, 59, 59, 59));
    beginTime = getStrTime(Date.parse(currentMonthFirstDay));
    endTime = getStrTime(Date.parse(currentMonthLastDay));
    var time = {
        beginTime: beginTime,
        endTime: endTime
    }
    return time;
}

function IFrameResize() {
    //弹出当前页面的高度
    var obj = parent.document.getElementById('subpage'); //取得父页面IFrame对象
    obj.height = this.document.body.scrollHeight; //调整父页面中IFrame的高度为此页面的高度
}

//iframe子页面刷新
function refresh() {
    window.location.reload();
}

$(function () {
    $(".btn-refresh").click(function () {
        refresh();
    });
});

//模态框请求返回
function postBack(success, strErro, tipSuccess, tipstrErro, modelName, callBack) {
    var icon;
    if (success) {
        icon = 'icon-chenggong1';
        popover(icon, tipSuccess);
        setTimeout(function () {
            $('.popover').remove();
        }, 1000);
        setTimeout(function () {
            $(modelName).modal('hide');
        }, 500);
        callBack();
    } else {
        icon = 'icon-shibai1';
        popover(icon, tipstrErro + strErro);
        setTimeout("$('.popover').remove()", 2000);
    }
}

//防抖动
function debounce(fn, delay) {

    // 定时器，用来 setTimeout
    var timer

    // 返回一个函数，这个函数会在一个时间区间结束后的 delay 毫秒时执行 fn 函数
    return function () {

        // 保存函数调用时的上下文和参数，传递给 fn
        var context = this
        var args = arguments

        // 每次这个返回的函数被调用，就清除定时器，以保证不执行 fn
        clearTimeout(timer)

        // 当返回的函数被最后一次调用后（也就是用户停止了某个连续的操作），
        // 再过 delay 毫秒就执行 fn
        timer = setTimeout(function () {
            fn.apply(context, args)
        }, delay)
    }
}

//中文url处理
function getUrl(url) {
    return decodeURI(encodeURI(encodeURI(url)));
}

//文件上传前处理
function fileChange(oneself, inputUpload) {
    var format;
    var fileName;
    var fileInfo;
    $(inputUpload).on('change', function () {
        var $this = $(this);
        var userAgent = navigator.userAgent; //取得浏览器的userAgent字符串
        var isIE = userAgent.indexOf("compatible") > -1 && userAgent.indexOf("MSIE") > -1; //判断是否IE<11浏览器

        if (isIE) {
            var reIE = new RegExp("MSIE (\\d+\\.\\d+);");
            reIE.test(userAgent);
            var fIEVersion = parseFloat(RegExp["$1"]);

            if (fIEVersion == 8 || fIEVersion == 9) {
                fileInfo = $(this).val().split("\\");
                fileName = fileInfo[2];
            } else {
                getfileInfo($this);
            }
        } else {
            getfileInfo($this);
        }
        $('.file-box').find('.file-name').html(fileName);
    });

    function getfileInfo($this) {
        fileInfo = $this.get(0).files[0];
        format = getFileExtName(fileInfo.name);
        fileName = fileInfo.name;
    }

    //获取文件后缀。
    function getFileExtName(b) {
        var ExtName = b.lastIndexOf(".");
        return b.substring(ExtName + 1);
    }
}
avalon.config({debug: false});

