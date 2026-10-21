(function () {
    'use strict';
    var message = document.getElementById('message');
    var proxyList = document.getElementById('proxy-list');
    var budgetList = document.getElementById('budget-list');
    var budgetForm = document.getElementById('budget-form');

    function showMessage(text, ok) {
        message.textContent = text || '';
        message.className = 'alert ' + (ok ? 'alert-success' : 'alert-danger');
        message.style.display = text ? 'block' : 'none';
    }
    function api(method, path, body, done) {
        Connect_Http.httpDatas(method, path, body || {}, function (success, data, error) {
            if (!success) { showMessage(error || '请求失败', false); }
            done(success, data);
        });
    }
    function escapeHtml(value) {
        return String(value == null ? '' : value).replace(/[&<>"']/g, function (c) {
            return {'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c];
        });
    }
    function formatDate(value) { return value ? String(value).replace('T', ' ').slice(0, 19) : ''; }

    function loadProxyScopes() {
        api('get', '/Access/获取代理范围', {}, function (ok, scopes) {
            var select = document.getElementById('proxy-scope');
            if (!select) return;
            select.innerHTML = ok && scopes && scopes.length ? scopes.map(function (scope) {
                return '<option value="' + escapeHtml(scope) + '">' + escapeHtml(scope) + '</option>';
            }).join('') : '<option value="">暂无可用代理范围</option>';
            select.disabled = !(ok && scopes && scopes.length);
        });
    }

    function loadProxies() {
        api('get', '/Access/获取我的代理', {}, function (ok, rows) {
            if (!ok) { proxyList.innerHTML = '<tr><td colspan="8">无权查看或加载失败</td></tr>'; return; }
            rows = rows || [];
            proxyList.innerHTML = rows.length ? rows.map(function (row) {
                var revoked = row.撤销时间 != null;
                return '<tr><td>' + escapeHtml(row.编号) + '</td><td>' + escapeHtml(row.授权人姓名 || row.授权人编号) +
                    '</td><td>' + escapeHtml(row.被代理人姓名 || row.被代理人编号) + '</td><td>' + escapeHtml(row.代理范围) +
                    '</td><td>' + escapeHtml(formatDate(row.开始时间)) + '</td><td>' + escapeHtml(formatDate(row.结束时间)) +
                    '</td><td>' + (revoked ? ('已撤销' + (row.撤销原因 ? '：' + escapeHtml(row.撤销原因) : '')) : '有效') + '</td><td>' + (revoked ? '' :
                    '<button class="btn btn-xs btn-warning revoke-proxy" data-id="' + escapeHtml(row.编号) + '">撤销</button>') + '</td></tr>';
            }).join('') : '<tr><td colspan="8">暂无代理记录</td></tr>';
        });
    }
    function loadBudget() {
        api('get', '/Expense/获取预算异常对账', {}, function (ok, rows) {
            if (!ok) { budgetList.innerHTML = '<tr><td colspan="11">无权查看或加载失败</td></tr>'; return; }
            rows = rows || [];
            api('get', '/Expense/获取预算异常处理', {}, function (historyOk, history) {
                var dispositions = {};
                (historyOk ? (history || []) : []).forEach(function (item) {
                    dispositions[String(item.流程模板编号) + ':' + String(item.项目编号) + ':' + String(item.异常类型)] = item;
                });
                budgetList.innerHTML = rows.length ? rows.map(function (row, index) {
                    var disposition = dispositions[String(row.流程模板编号) + ':' + String(row.项目编号) + ':' + String(row.异常类型)] || {};
                    return '<tr><td>' + escapeHtml(row.流程模板编号) + '</td><td>' + escapeHtml(row.项目编号) +
                        '</td><td>' + escapeHtml(row.异常类型) + '</td><td>' + escapeHtml(row.批准预算) +
                        '</td><td>' + escapeHtml(row.到账金额) + '</td><td>' + escapeHtml(row.已审核报销) +
                        '</td><td>' + escapeHtml(row.待审核报销) + '</td><td>' + escapeHtml(disposition.状态 || '未登记') +
                        '</td><td>' + escapeHtml(disposition.责任人姓名 || disposition.责任人编号 || '') + '</td><td>' +
                        escapeHtml(disposition.处理说明 || '') + '</td><td><button class="btn btn-xs btn-primary handle-budget" data-index="' + index + '">登记处理</button></td></tr>';
                }).join('') : '<tr><td colspan="11">暂无异常</td></tr>';
                budgetList._rows = rows;
            });
        });
    }

    document.getElementById('grant-form').addEventListener('submit', function (event) {
        event.preventDefault();
        var f = event.target;
        api('post', '/Access/授权代理', {
            被代理人编号: Number(f.delegateId.value), 代理范围: f.scope.value,
            开始时间: f.start.value, 结束时间: f.end.value, 备注: f.note.value
        }, function (ok) { if (ok) { showMessage('代理授权已创建', true); f.reset(); loadProxies(); } });
    });
    proxyList.addEventListener('click', function (event) {
        var button = event.target.closest('.revoke-proxy');
        if (!button) return;
        var reason = window.prompt('请输入撤销原因', '管理员手工撤销代理权限');
        if (reason === null) return;
        api('post', '/Access/撤销代理', { 编号: Number(button.getAttribute('data-id')), 原因: reason }, function (ok) {
            if (ok) { showMessage('代理已撤销', true); loadProxies(); }
        });
    });
    document.getElementById('revoke-person').addEventListener('click', function () {
        var personId = Number(document.getElementById('revoke-person-id').value);
        if (!personId) { showMessage('请输入离岗人员编号', false); return; }
        api('post', '/Access/回收人员代理', { 人员编号: personId, 原因: document.getElementById('revoke-person-reason').value }, function (ok, data) {
            if (ok) { showMessage('已回收 ' + (data.回收数量 || 0) + ' 条代理', true); loadProxies(); }
        });
    });
    document.getElementById('refresh-budget').addEventListener('click', loadBudget);
    budgetList.addEventListener('click', function (event) {
        var button = event.target.closest('.handle-budget');
        if (!button || !budgetList._rows) return;
        var row = budgetList._rows[Number(button.getAttribute('data-index'))];
        var form = document.getElementById('budget-disposition-form');
        form.flowId.value = row.流程模板编号; form.projectId.value = row.项目编号; form.anomaly.value = row.异常类型;
        form.note.value = ''; budgetForm.style.display = 'block';
    });
    document.getElementById('cancel-budget').addEventListener('click', function () { budgetForm.style.display = 'none'; });
    document.getElementById('budget-disposition-form').addEventListener('submit', function (event) {
        event.preventDefault();
        var f = event.target;
        api('post', '/Expense/处理预算异常对账', {
            流程模板编号: Number(f.flowId.value), 项目编号: Number(f.projectId.value), 异常类型: f.anomaly.value,
            状态: f.status.value, 责任人编号: f.ownerId.value ? Number(f.ownerId.value) : null, 处理说明: f.note.value
        }, function (ok) { if (ok) { showMessage('预算异常处理已登记', true); budgetForm.style.display = 'none'; loadBudget(); } });
    });
    loadProxyScopes();
    loadProxies();
    loadBudget();
}());
