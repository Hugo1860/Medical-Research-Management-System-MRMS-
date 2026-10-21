$(function () {
    if (sessionStorage.userInfo == null) {
        location.href = '/IMIS/views/login.html';
    }
    window.mUserInfo = JSON.parse(sessionStorage.userInfo);
    console.info(mUserInfo);
    var ParentIndex;
    var TwoIndex;
    var mUsers;
    avalon.config({debug:false});
    window.vm = null;
    avalon.ready(function () {
        window.vm = avalon.define({
            $id: 'root',
            userInfo: mUserInfo.人员,
            jurisdiction: mUserInfo.权限,
            ClickLiParent: function (index, el) {
                ParentIndex = index + 1;
                var li = $('.nav-sidebar .parent-li:eq(' + ParentIndex + ')');
                var siblings = li.siblings().removeClass('active');
                siblings.find('.treeview-menu').slideUp().find('li.active').removeClass('active');
                siblings.find('.icon-shouqi').addClass('icon-xiala');
                li.addClass('active').find('.treeview-menu').slideToggle(500);
                $('.nav-sidebar .parent-li:eq(' + ParentIndex + ')>a').find('.icon-shouqi').toggleClass('icon-xiala');
                var url = el.菜单.路径;
                if (url != null && url != '') {
                    $('.subpage #subpage').attr('src', url);
                    sessionStorage.mkeyandetails = false;
                    return false;
                }
            },
            ClickLiTwo: function (index, aa) {
                TwoIndex = index;
                var li = $('.parent-li:eq(' + ParentIndex + ') .treeview-menu >li:eq(' + TwoIndex + ')');
                var siblings = li.siblings().removeClass('active');
                siblings.children('ul').slideUp().children('li.active').removeClass('active');
                siblings.children('.icon-shouqi').addClass('icon-xiala');
                li.addClass('active').children('ul').slideToggle(500);
                $('.parent-li:eq(' + ParentIndex + ') .treeview-menu >li:eq(' + TwoIndex + ')>a').children('.icon-shouqi').toggleClass('icon-xiala');
                var url = aa.菜单.路径;
                if (url != null && url != '') {
                    $('.subpage #subpage').attr('src', url);
                    sessionStorage.mkeyandetails = false;
                    return false;
                }
            },
            ClickLiThree: function (index, bb) {
                var li = $('.parent-li:eq(' + ParentIndex + ') .treeview-menu >li:eq(' + TwoIndex + ') .treeview-menu-sub >li:eq(' + (index) + ')');
                li.addClass('active').siblings().removeClass('active');
                var url = bb.菜单.路径;
                if (url != null && url != '') {
                    $('.subpage #subpage').attr('src', url);
                    sessionStorage.mkeyandetails = false;
                    var arr = url.split('/');
                    var newUrl = decodeURI(arr[arr.length - 1]);
                    if (newUrl == '个人资料.html') {
                        vm.clickBtnEdit(false);
                    }
                    return false;
                }
            },
            hoverLi: function (index, el) {
                if ($('body').hasClass('mini-sidebar')) {
                    ParentIndex = index + 1;
                    var li = $('.nav-sidebar .parent-li:eq(' + ParentIndex + ')');
                    var siblings = li.siblings().removeClass('active');
                    siblings.find('.icon-shouqi').addClass('icon-xiala');
                    li.addClass('active').find('.treeview-menu').slideDown(500);
                    $('.nav-sidebar .parent-li:eq(' + ParentIndex + ')>a').find('.icon-shouqi').toggleClass('icon-xiala');
                    var url = el.菜单.路径;
                    if (url != null && url != '') {
                        $('.subpage #subpage').attr('src', url);
                        return false;
                    }
                }
            },
            clickBtnEdit: function (val) {
                sessionStorage.addUerType = JSON.stringify(0);
                var userType = {
                    type: true,
                    infoBack: false,
                    btnBack: val
                }
                sessionStorage.userType = JSON.stringify(userType);
                vm.getUserDetail(mUserInfo.人员.编号);
                // $('.subpage #subpage').attr('src', '/IMIS/views/科研办公/科研办公/个人资料.html');
            },
            clickPassWord: function () {
                sessionStorage.mkeyandetails = true;
                $('.subpage #subpage').attr('src', '/IMIS/views/科研办公/科研办公/修改密码.html');
            },
            getUserDetail: function (userId) {
                User.getUsersDetails('get', userId, function getUsersDetailsListener(success, obj, strErro) {
                    if (success) {
                        sessionStorage.mUsers = JSON.stringify(obj);
                        $('.subpage #subpage').attr('src', '/IMIS/views/科研办公/科研办公/个人资料.html');
                    } else {
                        alert('获取个人信息失败！');
                        console.info(strErro)
                    }
                });
            },
        });
        //左边导航
        $('.sidebar .parent-li a:first').on('click', function () {
            var li = $(this).parent();
            var siblings = li.addClass('active').siblings().removeClass('active');
            siblings.find('.treeview-menu').slideUp().find('li.active').removeClass('active');
            var url = $(this).attr('href');
            if (url != 'javascript:;') {
                $('.subpage #subpage').attr('src', url);
                return false;
            }
        });
        avalon.scan(document.body);
    });

    //顶部菜单
    $('.sidebar-toggle').on('click', function () {
        $('body').toggleClass('mini-sidebar');
        $('.parent-li ul').hide();

    });

    //可视窗口的高度
    function layout() {
        var height = $(window).height();
        $('#subpage').css('height', height - 71);
        $('.nav-sidebar-box').css('height', height - 56);
    }

    layout();
    $(window).resize(function () {
        var width = $(window).width();
        layout();
        if (width <= 1280) {
            $('body').addClass('mini-sidebar');
            $('.parent-li ul').hide();
        } else {
            $('body').removeClass('mini-sidebar');
        }
    });

    $('.bs-tooltip').tooltip();
    //调用占位符插件
    $('input, textarea').placeholder();


    $('.nav-sidebar-box').mCustomScrollbar({
        theme: 'dark-3'
    });

    //头部导航
    $('.user-info .dropdown-menu a').on('click', function () {
        $('.dropdown-menu li').removeClass('active');
        $(this).parents('li').addClass('active').siblings().removeClass('active');

        var url = $(this).attr('href');
        if (url != 'javascript:;') {
            $('.subpage #subpage').attr('src', url);
            return false;
        }
    });

    //主题
    // sessionStorage.mystyle = 'default';
    if (sessionStorage.mystyle) {
        var cookieStyle = sessionStorage.mystyle;
        var linkList = $('link');
        themeInit(cookieStyle);
    }

    $('.theme li').on('click', function () {
        var style = $(this).attr('id');
        sessionStorage.mystyle = style;
        if (style == 'default') {
            $('link[ty="theme"]').attr('disabled', 'true');
        } else {
            themeSwitch(style);
        }
    });

    function themeInit(cookieStyle) {
        if (cookieStyle == null) {
            $('link[title="default"]').removeAttr('disabled');
            $('.theme li#default').addClass('active');

        } else {
            $('link[title="' + cookieStyle + '"]').removeAttr('disabled');
            $('.theme li[id="' + cookieStyle + '"]').addClass('active');

            for (var i = 0; i < linkList.length; i++) {
                if (linkList[i].attributes.ty != null && linkList[i].title != cookieStyle) {
                    linkList[i].disabled = 'true';
                }
            }
        }
    }

    function themeSwitch(style) {
        $('link[title="' + style + '"]').removeAttr('disabled');
        for (var i = 0; i < linkList.length; i++) {
            if (linkList[i].attributes.ty != null && linkList[i].title != style) {
                linkList[i].disabled = 'true';
            }
        }
        $(this).addClass('active').siblings().removeClass('active');
    }


    $('.dropdown-toggle').dropdown();

    $('.btn-out').on('click', function () {
        sessionStorage.removeItem('userInfo');
        sessionStorage.removeItem('keycloakAccessToken');
        sessionStorage.removeItem('keycloakRefreshToken');
        sessionStorage.removeItem('keycloakTokenEndpoint');
        sessionStorage.removeItem('keycloakClientId');
        sessionStorage.removeItem('access_token');
    });
    $('body').on('click', function () {
        $('.dropdown-toggle').dropdown();
    });

    $('.bs-tooltip').tooltip();

});
