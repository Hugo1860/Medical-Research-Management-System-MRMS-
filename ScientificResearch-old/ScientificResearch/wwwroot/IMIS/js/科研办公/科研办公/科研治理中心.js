(function () {
    'use strict';
    var message = document.getElementById('message');
    function escapeHtml(value) {
        return String(value == null ? '' : value).replace(/[&<>"']/g, function (c) {
            return {'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c];
        });
    }
    function formatDate(value) { return value ? String(value).replace('T', ' ').slice(0, 19) : ''; }
    function showMessage(text, ok) {
        message.textContent = text || '';
        message.className = 'alert ' + (ok ? 'alert-success' : 'alert-warning');
        message.style.display = text ? 'block' : 'none';
    }
    function api(method, path, body, done) {
        Connect_Http.httpDatas(method, path, body || {}, function (success, data, error) {
            done(success, data, error || '请求失败');
        });
    }
    function setRows(id, rows, columns, emptyText) {
        var body = document.querySelector('#' + id + ' tbody');
        if (!body) return;
        if (!rows || !rows.length) {
            body.innerHTML = '<tr><td colspan="' + columns + '" class="empty">' + escapeHtml(emptyText || '暂无数据') + '</td></tr>';
            return;
        }
        body.innerHTML = rows;
    }
    function loadDecision() {
        api('get', '/Analytics/获取科研决策摘要', {}, function (ok, data, error) {
            var summary = document.getElementById('decision-summary');
            if (!ok) { summary.innerHTML = '<span class="status-muted">' + escapeHtml(error) + '</span>'; document.getElementById('open-bottlenecks').textContent = '—'; document.getElementById('waiting-hours').textContent = '—'; return; }
            data = data || {};
            document.getElementById('open-bottlenecks').textContent = data.开放瓶颈数 == null ? 0 : data.开放瓶颈数;
            document.getElementById('waiting-hours').textContent = data.待处理资源等待小时 == null ? 0 : data.待处理资源等待小时;
            var bottlenecks = data.主要瓶颈 || [];
            var resources = data.资源等待 || [];
            summary.innerHTML = '<p><strong>主要瓶颈：</strong>' + (bottlenecks.length ? bottlenecks.slice(0, 3).map(function (x) { return escapeHtml(x.事件类型) + '（' + escapeHtml(x.事件数) + '）'; }).join('、') : '暂无') + '</p>' +
                '<p><strong>资源等待：</strong>' + (resources.length ? resources.slice(0, 3).map(function (x) { return escapeHtml(x.资源名称) + '（' + escapeHtml(x.等待小时) + ' 小时）'; }).join('、') : '暂无') + '</p>';
        });
    }
    function loadBottlenecks() {
        api('get', '/Analytics/获取科研瓶颈汇总', {}, function (ok, rows, error) {
            if (!ok) { setRows('bottleneck-table', [], 7, error); return; }
            setRows('bottleneck-table', (rows || []).map(function (x) { return '<tr><td>' + escapeHtml(x.事件类型) + '</td><td>' + escapeHtml(x.原因代码) + '</td><td>' + escapeHtml(x.严重程度) + '</td><td>' + escapeHtml(x.状态) + '</td><td>' + escapeHtml(x.事件数) + '</td><td>' + escapeHtml(x.平均等待天数) + '</td><td>' + escapeHtml(x.最大等待天数) + '</td></tr>'; }), 7, '暂无瓶颈事件');
        });
    }
    function loadMigrationBindings() {
        api('get', '/BaseInfo/获取迁移绑定异常', {}, function (ok, rows, error) {
            if (!ok) { setRows('migration-table', [], 6, error); return; }
            setRows('migration-table', (rows || []).map(function (x) { return '<tr><td>' + escapeHtml(x.异常类型) + '</td><td>' + escapeHtml(x.记录编号) + '</td><td>' + escapeHtml(x.流程模板编号) + '</td><td>' + escapeHtml(x.项目编号) + '</td><td>' + escapeHtml(x.关联编号) + '</td><td>' + escapeHtml(x.说明) + '</td></tr>'; }), 6, '暂无迁移绑定异常');
        });
    }
    function loadAlerts() {
        api('get', '/Operations/获取接口失败告警', {}, function (ok, rows, error) {
            if (!ok) { document.getElementById('failure-count').textContent = '—'; setRows('alert-table', [], 5, error); return; }
            rows = rows || []; document.getElementById('failure-count').textContent = rows.length;
            setRows('alert-table', rows.map(function (x) { return '<tr><td>' + escapeHtml(x.请求路径) + '</td><td>' + escapeHtml(x.请求方法) + '</td><td>' + escapeHtml(x.响应状态) + '</td><td>' + escapeHtml(x.失败次数) + '</td><td>' + escapeHtml(formatDate(x.最近失败时间)) + '</td></tr>'; }), 5, '暂无接口失败告警');
        });
    }
    function loadExports() {
        api('get', '/Operations/查询数据导出任务', {}, function (ok, rows, error) {
            if (!ok) { document.getElementById('export-count').textContent = '—'; setRows('export-table', [], 4, error); return; }
            rows = rows || []; var active = rows.filter(function (x) { return ['申请中', '生成中'].indexOf(x.状态) >= 0; }).length; document.getElementById('export-count').textContent = active;
            setRows('export-table', rows.slice(0, 10).map(function (x) { return '<tr><td>' + escapeHtml(x.导出范围) + '</td><td>' + escapeHtml(x.状态) + '</td><td>' + escapeHtml(formatDate(x.申请时间)) + '</td><td>' + escapeHtml(x.文件校验) + '</td></tr>'; }), 4, '暂无导出任务');
        });
    }
    function loadEvaluationRules() {
        api('get', '/Evaluation/获取科研评价规则', {}, function (ok, rows, error) {
            if (!ok) { setRows('evaluation-table', [], 5, error); return; }
            setRows('evaluation-table', (rows || []).slice(0, 20).map(function (x) { return '<tr><td>' + escapeHtml(x.规则名称) + '</td><td>' + escapeHtml(x.版本号) + '</td><td>' + escapeHtml(x.适用岗位) + '</td><td>' + escapeHtml(x.评价周期) + '</td><td>' + escapeHtml(x.状态) + '</td></tr>'; }), 5, '暂无评价规则');
        });
    }
    function loadInstitutions() {
        api('get', '/Fruit/获取科研单位规范', { 仅启用: true }, function (ok, rows, error) {
            var select = document.getElementById('institution-select');
            if (!ok) { setRows('institution-table', [], 5, error); select.innerHTML = '<option value="">无权或加载失败</option>'; return; }
            rows = rows || [];
            select.innerHTML = rows.length ? '<option value="">选择规范单位</option>' + rows.map(function (x) { return '<option value="' + escapeHtml(x.编号) + '">' + escapeHtml(x.规范名称) + '</option>'; }).join('') : '<option value="">暂无规范单位</option>';
            setRows('institution-table', rows.map(function (x) {
                var aliases = x.别名 || [];
                return '<tr><td>' + escapeHtml(x.规范名称) + '</td><td>' + escapeHtml(x.统一社会信用代码) + '</td><td>' + escapeHtml(x.状态) + '</td><td>' + escapeHtml(aliases.map(function (a) { return a.别名; }).join('、')) + '</td><td>' + escapeHtml(x.备注) + '</td></tr>';
            }), 5, '暂无规范单位');
        });
    }
    function loadExternalSystems() {
        api('get', '/Operations/获取外部系统', {}, function (ok, rows, error) {
            if (!ok) { setRows('external-system-table', [], 5, error); return; }
            setRows('external-system-table', (rows || []).map(function (x) {
                return '<tr><td>' + escapeHtml(x.系统名称) + '<br><small>' + escapeHtml(x.系统代码) + '</small></td><td>' + escapeHtml(x.系统类型) + '</td><td>' + escapeHtml(x.基础地址 || '未配置') + '</td><td>' + escapeHtml(x.认证方式) + (x.认证引用 ? '<br><small>' + escapeHtml(x.认证引用) + '</small>' : '') + '</td><td>' + escapeHtml(x.是否启用 ? '启用' : '停用') + ' <button class="btn btn-xs btn-info external-test" data-id="' + escapeHtml(x.编号) + '">测试连接</button></td></tr>';
            }), 5, '暂无外部系统配置');
        });
    }
    function loadExternalConfigCheck() {
        api('get', '/Operations/获取外部系统配置检查', {}, function (ok, data, error) {
            if (!ok) { setRows('external-config-table', [], 6, error); return; }
            data = data || {};
            var rows = data.必需系统 || [];
            setRows('external-config-table', rows.map(function (x) {
                return '<tr><td>' + escapeHtml(x.系统类型) + '</td><td>' + (x.已登记 ? '是' : '否') + '</td><td>' + (x.已配置地址 ? '是' : '否') + '</td><td>' + (x.已配置认证 ? '是' : '否') + '</td><td class="' + (x.通过 ? 'text-success' : 'text-danger') + '">' + (x.通过 ? '通过' : '未通过') + '</td><td>' + escapeHtml(x.提示) + '</td></tr>';
            }), 6, '暂无检查结果');
            var enabled = data.worker已启用 ? 'worker 当前已启用' : 'worker 当前未启用';
            showMessage(enabled + '；' + (data.可启用worker ? '配置检查通过。' : '配置尚不完整，请按检查结果补齐。'), !!data.可启用worker);
        });
    }
    document.getElementById('external-config-check').addEventListener('click', loadExternalConfigCheck);
    document.getElementById('external-system-table').addEventListener('click', function (event) {
        var button = event.target.closest('.external-test'); if (!button) return;
        button.disabled = true;
        api('post', '/Operations/测试外部系统连接', { 编号: Number(button.getAttribute('data-id')) }, function (ok, data, error) {
            button.disabled = false;
            if (!ok) { showMessage(error || '连接测试失败', false); return; }
            showMessage('连接测试：' + (data.测试结果 || '失败') + (data.状态码 ? '（HTTP ' + data.状态码 + '，' + data.耗时毫秒 + 'ms）' : '：' + (data.说明 || '')), data.测试结果 === '通过');
        });
    });
    function loadExternalSyncTasks() {
        api('get', '/Operations/查询外部同步任务', { 数量: 20 }, function (ok, rows, error) {
            if (!ok) { setRows('external-sync-table', [], 7, error); return; }
            setRows('external-sync-table', (rows || []).map(function (x) {
                var action = '';
                if (x.状态 === '失败' || x.状态 === '已取消') action += '<button class="btn btn-xs btn-info sync-retry" data-id="' + escapeHtml(x.编号) + '">重试</button> ';
                if (x.状态 === '待处理' || x.状态 === '失败') action += '<button class="btn btn-xs btn-warning sync-cancel" data-id="' + escapeHtml(x.编号) + '">取消</button>';
                return '<tr><td>' + escapeHtml(x.系统名称 || x.系统代码) + '</td><td>' + escapeHtml(x.业务类型) + '</td><td>' + escapeHtml(x.方向) + '</td><td>' + escapeHtml(x.状态) + '</td><td>' + escapeHtml(x.尝试次数) + '/' + escapeHtml(x.最大重试次数) + '</td><td>' + escapeHtml(x.最后错误 || '') + '</td><td>' + action + '</td></tr>';
            }), 7, '暂无同步任务');
        });
    }
    document.getElementById('external-sync-table').addEventListener('click', function (event) {
        var button = event.target.closest('.sync-retry,.sync-cancel'); if (!button) return;
        var retry = button.classList.contains('sync-retry'); button.disabled = true;
        api('post', retry ? '/Operations/重试外部同步任务' : '/Operations/取消外部同步任务', { 编号: Number(button.getAttribute('data-id')) }, function (ok, data, error) {
            button.disabled = false; showMessage(ok ? (retry ? '任务已重新排队。' : '任务已取消。') : error, ok); if (ok) loadExternalSyncTasks();
        });
    });
    document.getElementById('external-sync-form').addEventListener('submit', function (event) {
        event.preventDefault(); var form = event.target; var payload = form.elements.请求负载.value.trim() || '{}';
        try { JSON.parse(payload); } catch (e) { showMessage('请求负载必须是有效 JSON。', false); return; }
        api('post', '/Operations/创建外部同步任务', { 外部系统编号: Number(form.elements.外部系统编号.value), 业务类型: form.elements.业务类型.value, 方向: form.elements.方向.value, 业务主键: form.elements.业务主键.value, 幂等键: form.elements.幂等键.value, 请求负载: payload }, function (ok, data, error) { showMessage(ok ? '同步任务已加入队列。' : error, ok); if (ok) { form.reset(); loadExternalSyncTasks(); } });
    });
    document.getElementById('external-system-form').addEventListener('submit', function (event) {
        event.preventDefault(); var form = event.target;
        api('post', '/Operations/保存外部系统', {
            系统代码: form.elements.系统代码.value,
            系统名称: form.elements.系统名称.value,
            系统类型: form.elements.系统类型.value,
            基础地址: form.elements.基础地址.value,
            认证方式: form.elements.认证方式.value,
            认证引用: form.elements.认证引用.value,
            是否启用: false,
            超时秒数: 15,
            最大重试次数: 3
        }, function (ok, data, error) {
            showMessage(ok ? '外部系统已保存，默认停用。配置联调通过后再启用。' : error, ok);
            if (ok) { form.reset(); loadExternalSystems(); }
        });
    });
    document.getElementById('institution-form').addEventListener('submit', function (event) {
        event.preventDefault(); var form = event.target;
        api('post', '/Fruit/保存科研单位规范', { 规范名称: form.elements.规范名称.value, 统一社会信用代码: form.elements.统一社会信用代码.value, 备注: form.elements.备注.value }, function (ok, data, error) {
            showMessage(ok ? '规范单位已保存' : error, ok); if (ok) { form.reset(); loadInstitutions(); }
        });
    });
    document.getElementById('alias-form').addEventListener('submit', function (event) {
        event.preventDefault(); var form = event.target;
        api('post', '/Fruit/保存科研单位别名', { 规范单位编号: Number(form.elements.规范单位编号.value), 别名: form.elements.别名.value, 来源: form.elements.来源.value, 是否启用: true }, function (ok, data, error) {
            showMessage(ok ? '单位别名已保存' : error, ok); if (ok) { form.reset(); loadInstitutions(); }
        });
    });
    document.getElementById('hr-sync-form').addEventListener('submit', function (event) {
        event.preventDefault(); var form = event.target, rows;
        try { rows = JSON.parse(form.elements.人员列表.value); } catch (e) { showMessage('人员列表必须是合法 JSON 数组。', false); return; }
        if (!Array.isArray(rows) || !rows.length) { showMessage('人员列表至少需要一条记录。', false); return; }
        api('post', '/People/同步HR人员', { 外部系统编号: Number(form.elements.外部系统编号.value), 同步批次号: form.elements.同步批次号.value.trim(), 是否全量: !!form.elements.是否全量.checked, 人员列表: rows }, function (ok, data, error) { showMessage(ok ? ('HR 同步完成：新增 ' + (data.新增 || 0) + '，更新 ' + (data.更新 || 0) + '，禁用 ' + (data.禁用 || 0) + '。') : error, ok); });
    });
    document.getElementById('hr-preview').addEventListener('click', function () {
        var form = document.getElementById('hr-sync-form'), rows;
        try { rows = JSON.parse(form.elements.人员列表.value); } catch (e) { showMessage('人员列表必须是合法 JSON 数组。', false); return; }
        api('post', '/People/预览HR人员同步', { 外部系统编号: Number(form.elements.外部系统编号.value), 同步批次号: form.elements.同步批次号.value.trim(), 是否全量: !!form.elements.是否全量.checked, 人员列表: rows }, function (ok, data, error) {
            if (!ok) { showMessage(error, false); return; }
            var duplicates = (data.重复外部人员编号 || []).length;
            showMessage('预览：有效 ' + data.有效记录数 + '，无效 ' + data.无效记录数 + '，重复 ' + duplicates + '，预计禁用缺席人员 ' + data.预计禁用缺席人员数 + '。' + (data.可安全执行 ? '可以执行同步。' : '请修正数据后再执行。'), data.可安全执行);
        });
    });
    function bindBatchForm(id, endpoint, field, successText) {
        document.getElementById(id).addEventListener('submit', function (event) {
            event.preventDefault(); var form = event.target, rows;
            try { rows = JSON.parse(form.elements[field].value); } catch (e) { showMessage(field + '必须是合法 JSON 数组。', false); return; }
            if (!Array.isArray(rows) || !rows.length) { showMessage(field + '至少需要一条记录。', false); return; }
            var body = { 外部系统编号: Number(form.elements.外部系统编号.value), 同步批次号: form.elements.同步批次号.value.trim() }; body[field] = rows;
            api('post', endpoint, body, function (ok, data, error) { showMessage(ok ? successText : error, ok); });
        });
    }
    bindBatchForm('finance-sync-form', '/Expense/同步财务流水', '流水列表', '财务同步完成。');
    bindBatchForm('ethics-sync-form', '/YProject/同步伦理审批结果', '审批列表', '伦理审批同步完成。');
    function refreshAll() { loadDecision(); loadBottlenecks(); loadMigrationBindings(); loadAlerts(); loadExports(); loadEvaluationRules(); loadInstitutions(); loadExternalSystems(); loadExternalSyncTasks(); }
    document.getElementById('refresh-all').addEventListener('click', refreshAll);
    refreshAll();
}());
