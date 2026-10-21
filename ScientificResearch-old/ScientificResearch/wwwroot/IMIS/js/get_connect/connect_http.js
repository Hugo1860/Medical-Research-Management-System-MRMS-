var Connect_Http = {
    // Keycloak 登录由统一身份入口完成；短期令牌和刷新令牌均只保存在当前标签页。
    // 未配置令牌时仍走原有 Session 模式。
    refreshInFlight: null,
    getAccessToken: function () {
        return sessionStorage.getItem('keycloakAccessToken') || sessionStorage.getItem('access_token');
    },
    refreshAccessToken: function (done) {
        var refreshToken = sessionStorage.getItem('keycloakRefreshToken');
        var tokenEndpoint = sessionStorage.getItem('keycloakTokenEndpoint');
        if (!refreshToken || !tokenEndpoint) { done(false); return; }
        if (this.refreshInFlight) { this.refreshInFlight.then(done); return; }
        var form = new URLSearchParams();
        form.set('grant_type', 'refresh_token');
        form.set('client_id', sessionStorage.getItem('keycloakClientId') || '');
        form.set('refresh_token', refreshToken);
        this.refreshInFlight = fetch(tokenEndpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: form.toString(),
            credentials: 'omit'
        }).then(function (response) {
            return response.json().then(function (body) {
                if (!response.ok || !body.access_token) return false;
                sessionStorage.keycloakAccessToken = body.access_token;
                if (body.refresh_token) sessionStorage.keycloakRefreshToken = body.refresh_token;
                return true;
            });
        }).catch(function () { return false; }).then(function (result) {
            Connect_Http.refreshInFlight = null;
            return result;
        });
        this.refreshInFlight.then(done);
    },
    httpDatas: function (type, url, data, callBackListener, retried) {
        var originalUrl = url;

        if (type == 'get') {
            if (data != null) {
                var nowTime = new Date().getTime();
                url += '?time=' + nowTime+'&';
                for (var filed in data) {
                    url += filed + '=' + data[filed] + '&';
                }
                url = url.substring(0, url.length - 1);
            }
        }
        var xhr;

        /*var userAgent = navigator.userAgent; //取得浏览器的userAgent字符串
        var isIE = userAgent.indexOf("compatible") > -1 && userAgent.indexOf("MSIE") > -1; //判断是否IE<11浏览器
        var isIE11 = userAgent.indexOf('Trident') > -1 && userAgent.indexOf("rv:11.0") > -1;
        var reIE = new RegExp("MSIE (\\d+\\.\\d+);");
        reIE.test(userAgent);
        var fIEVersion = parseFloat(RegExp["$1"]);
        if (fIEVersion == 8 || fIEVersion == 9) {
            xhr = new XDomainRequest();

            xhr.onerror = function () {
                callBackListener(false, null, '错啦');
            };
            xhr.onload = function () {
                Connect_Http.callBack(xhr.responseText, callBackListener);
            }

            var url = encodeURI(url);
            console.info(decodeURI(url));
            xhr.open(type, url);
            var sendStr = null;
            if (data != null && data.length != 0) {
                sendStr = JSON.stringify(data);
            }
            if (type == 'get') {
                xhr.send(null);
            } else {
                xhr.send(sendStr);
            }
        } else {*/
        if (window.XMLHttpRequest) {
            xhr = new XMLHttpRequest();
        } else {
            xhr = new ActiveXObject('Microsoft.XMLHTTP');
        }

        xhr.onreadystatechange = function () {
            if (xhr.readyState == 4) {
                var status = xhr.status;
                if (status >= 200 && status < 300) {
                    Connect_Http.callBack(xhr.responseText, callBackListener);
                } else if (status == 401 && !retried && Connect_Http.getAccessToken()) {
                    Connect_Http.refreshAccessToken(function (refreshed) {
                        if (refreshed) {
                            Connect_Http.httpDatas(type, originalUrl, data, callBackListener, true);
                        } else {
                            callBackListener(false, '', '登录已过期，请重新登录');
                        }
                    });
                } else {
                    callBackListener(false, '', '错误代码: ' + xhr.status);
                }
            }
        }

        var url = encodeURI(encodeURI(url));
        xhr.open(type, decodeURI(url), true);
        xhr.setRequestHeader('Content-Type', 'application/json;charset=utf-8');
        var accessToken = Connect_Http.getAccessToken();
        if (accessToken) {
            xhr.setRequestHeader('Authorization', 'Bearer ' + accessToken);
        }
        data = JSON.stringify(data);

        if (type == 'get') {
            xhr.send(null);
        } else {
            xhr.send(data);
        }
        // }
    },
    getAjaxFormData: function (formData) {
        var obj = new Object();
        for (var i = 0; i < formData.length; i++) {
            var a = (formData[i].name);
            obj[a] = formData[i].value;
        }
        return obj;
    },
    ajaxData: function (url, type, data, callBackListener, retried) {
        $.ajax({
            url: url,
            type: type,
            data: data,
            dataType: 'json',
            contentType: 'application/json',
            beforeSend: function (xhr) {
                var accessToken = Connect_Http.getAccessToken();
                if (accessToken) {
                    xhr.setRequestHeader('Authorization', 'Bearer ' + accessToken);
                }
            },
            success: function (e) {
                e = JSON.stringify(e);
                Connect_Http.callBack(e, callBackListener);
            },
            error: function (xhr) {
                if (xhr.status == 401 && !retried && Connect_Http.getAccessToken()) {
                    Connect_Http.refreshAccessToken(function (refreshed) {
                        if (refreshed) {
                            Connect_Http.ajaxData(url, type, data, callBackListener, true);
                        } else {
                            callBackListener(false, '', '登录已过期，请重新登录');
                        }
                    });
                } else {
                    callBackListener(false, '', '请求失败');
                }
            }

        })
    },
    callBack: function (strDatas, callBackListener) {
        var code = 0;
        var strErro = null;
        if (strDatas != null && strDatas.charAt(0) == '{' && strDatas.charAt(strDatas.length - 1) == '}') {
            strDatas = JSON.parse(strDatas);
            strErro = strDatas.error;
            if (strErro != null && strErro.length > 0) {
                code = 1;
            }
        }
        if (code == 0) {
            var oDatas = strDatas;
            callBackListener(true, oDatas, '');

        } else {
            callBackListener(false, strDatas, strErro);
        }
    },

    judgeSuccess: function (strDatas) {
        var code = 0;
        var strErro = null;
        if (strDatas != null && strDatas.charAt(0) == '{' && strDatas.charAt(strDatas.length - 1) == '}') {

            strErro = strDatas.error;
            if (strErro != null && strErro.length > 0) {
                code = 1;
            }
        }
        if (code == 0) {
            return true;

        } else {
            return false;
        }
    }
}
