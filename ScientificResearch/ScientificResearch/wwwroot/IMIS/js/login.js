$(function () {
    $('input, textarea').placeholder();
    var userId, password;

    if (localStorage.ImisLoginInfo) {
        var loginInfo = JSON.parse(localStorage.ImisLoginInfo);
        $('#userId').val(loginInfo.userId);
        // Passwords must never be persisted in localStorage.
        if (loginInfo.password) {
            delete loginInfo.password;
            localStorage.ImisLoginInfo = JSON.stringify(loginInfo);
        }
        $('.remember').attr('checked', true);
    }

    $(window).keydown(function (evt) {
        evt = window.event || evt;
        if (evt.keyCode == 13) {
            var userId = $('#userId').val();
            var password = $('#password').val();
            remember(userId, password);
            login(userId, password);
        }
    });

    $('.btn-login').on("click", function () {
        userId = $('#userId').val();
        password = $('#password').val();
        remember(userId, password);
        login(userId, password);
    });

    $('.remember').on('change', function () {
        if (!this.checked) {
            localStorage.removeItem('ImisLoginInfo');
        }
    });

    function remember(userId, password) {
        if ($('.remember').is(':checked')) {
            var loginInfo = {
                userId: userId
            }
            localStorage.ImisLoginInfo = JSON.stringify(loginInfo);
        }
    }

    function login(userId, password) {
        if (userId == '' || password == '') {
            $(".login-tip").html('帐号/密码不能为空！');
        } else {
            User.userLogin('post', userId, password, function userLoginListener(success, obj, strErro) {
                var tip;
                $('.login-tip').removeClass('login-tip-success');
                if (success) {
                    sessionStorage.userInfo = JSON.stringify(obj);
                    console.info(obj);
                    tip = '登录成功！';
                    $('.login-tip').addClass('login-tip-success');
                    $('.login-tip').html(tip);
                    location.href = '/IMIS/views/index.html';
                } else {
                    tip = '帐号/密码错误！';
                    console.info(strErro);
                }
                $('.login-tip').html(tip);
                setTimeout(function () {
                    $('.login-tip').html('');
                }, 3000);
            });
        }
    }
});

// OIDC Authorization Code + PKCE entry point used when Keycloak is enabled.
(function () {
    'use strict';

    function setTip(message, success) {
        var tip = $('.login-tip');
        tip.toggleClass('login-tip-success', !!success).text(message || '');
    }

    function base64Url(bytes) {
        var binary = '';
        for (var i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i]);
        return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
    }

    function randomVerifier() {
        var bytes = new Uint8Array(32);
        window.crypto.getRandomValues(bytes);
        return base64Url(bytes);
    }

    function sha256(value) {
        return window.crypto.subtle.digest('SHA-256', new TextEncoder().encode(value))
            .then(function (buffer) { return base64Url(new Uint8Array(buffer)); });
    }

    function discovery(config) {
        return fetch(config.authority + '/.well-known/openid-configuration', {
            headers: { 'Accept': 'application/json' },
            credentials: 'omit'
        }).then(function (response) {
            if (!response.ok) throw new Error('无法读取统一身份配置');
            return response.json();
        });
    }

    function finishCallback(config, authorizationCode, verifier) {
        return discovery(config).then(function (metadata) {
            var form = new URLSearchParams();
            form.set('grant_type', 'authorization_code');
            form.set('client_id', config.clientId);
            form.set('redirect_uri', config.redirectUri);
            form.set('code', authorizationCode);
            form.set('code_verifier', verifier);
            return fetch(metadata.token_endpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: form.toString(),
                credentials: 'omit'
            }).then(function (response) {
                return response.json().then(function (body) {
                    if (!response.ok || !body.access_token) throw new Error(body.error_description || '统一身份令牌获取失败');
                    return { accessToken: body.access_token, refreshToken: body.refresh_token, tokenEndpoint: metadata.token_endpoint };
                });
            });
        }).then(function (token) {
            return fetch('/Access/Me', {
                headers: { 'Authorization': 'Bearer ' + token.accessToken, 'Accept': 'application/json' },
                credentials: 'same-origin'
            }).then(function (response) {
                return response.json().then(function (body) {
                    if (!response.ok || !body.data || !body.data.人员) throw new Error(body.error || '统一身份账号未映射到启用人员');
                    sessionStorage.keycloakAccessToken = token.accessToken;
                    if (token.refreshToken) sessionStorage.keycloakRefreshToken = token.refreshToken;
                    sessionStorage.keycloakTokenEndpoint = token.tokenEndpoint;
                    sessionStorage.keycloakClientId = config.clientId;
                    sessionStorage.userInfo = JSON.stringify(body.data);
                    window.location.replace('/IMIS/views/index.html');
                });
            });
        });
    }

    function handleCallback(config) {
        var query = new URLSearchParams(window.location.search);
        var error = query.get('error');
        if (error) {
            setTip(query.get('error_description') || '统一身份登录未完成');
            return;
        }
        var code = query.get('code');
        if (!code) return;

        var state = query.get('state');
        var expectedState = sessionStorage.getItem('keycloakOidcState');
        var verifier = sessionStorage.getItem('keycloakPkceVerifier');
        sessionStorage.removeItem('keycloakOidcState');
        sessionStorage.removeItem('keycloakPkceVerifier');
        if (!state || !expectedState || state !== expectedState || !verifier) {
            setTip('统一身份登录状态校验失败');
            return;
        }

        setTip('正在读取统一身份信息…');
        finishCallback(config, code, verifier).catch(function (reason) {
            sessionStorage.removeItem('keycloakAccessToken');
            sessionStorage.removeItem('keycloakRefreshToken');
            sessionStorage.removeItem('keycloakTokenEndpoint');
            sessionStorage.removeItem('keycloakClientId');
            setTip(reason && reason.message ? reason.message : '统一身份登录失败');
        });
    }

    function beginLogin(config) {
        if (!window.crypto || !window.crypto.subtle) {
            setTip('当前浏览器不支持安全登录，请升级浏览器');
            return;
        }
        var state = randomVerifier();
        var verifier = randomVerifier();
        sessionStorage.setItem('keycloakOidcState', state);
        sessionStorage.setItem('keycloakPkceVerifier', verifier);
        sha256(verifier).then(function (challenge) {
            return discovery(config).then(function (metadata) {
                var params = new URLSearchParams();
                params.set('client_id', config.clientId);
                params.set('response_type', 'code');
                params.set('scope', 'openid profile email');
                params.set('redirect_uri', config.redirectUri);
                params.set('state', state);
                params.set('code_challenge', challenge);
                params.set('code_challenge_method', 'S256');
                window.location.assign(metadata.authorization_endpoint + '?' + params.toString());
            });
        }).catch(function (reason) {
            setTip(reason && reason.message ? reason.message : '无法连接统一身份服务');
        });
    }

    $(function () {
        fetch('/Access/OidcConfig', { headers: { 'Accept': 'application/json' }, credentials: 'same-origin' })
            .then(function (response) { return response.json(); })
            .then(function (envelope) {
                var config = envelope.data || envelope;
                if (!config.enabled) return;
                if (!config.authority || !config.clientId || !config.redirectUri) {
                    setTip('统一身份配置不完整');
                    return;
                }
                $('#userId, #password').closest('.form-group').hide();
                $('.remember').closest('.checkbox').hide();
                $('.btn-login').text('使用统一身份登录').off('click.keycloak').on('click.keycloak', function () {
                    beginLogin(config);
                });
                handleCallback(config);
            })
            .catch(function () { /* Keep the legacy login form available when OIDC is disabled. */ });
    });
}());
