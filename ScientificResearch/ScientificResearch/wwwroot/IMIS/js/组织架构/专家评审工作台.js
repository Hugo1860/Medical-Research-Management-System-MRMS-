(function () {
    'use strict';
    function esc(value) { return $('<div>').text(value == null ? '' : value).html(); }
    function message(text, ok) { $('#message').text(text || '').removeClass('alert-success alert-danger alert-warning').addClass('alert-' + (ok ? 'success' : 'danger')).toggle(!!text); }
    function api(method, path, data, done) { Connect_Http.httpDatas(method, path, data || {}, function (ok, obj, error) { done(ok, obj && obj.data !== undefined ? obj.data : obj, error); }); }
    function reviewLink(row) {
        var path = row.步骤链接路径 || '';
        if (!path) return '<span class="text-muted">暂无评审入口</span>';
        var href = path.charAt(0) === '/' ? path : '/IMIS/views/' + path;
        return '<a class="btn btn-primary btn-xs" target="_self" href="' + esc(href) + '">进入评审</a>';
    }
    function checkConflict(stepId, button, output) {
        $(button).prop('disabled', true); $(output).text('检查中…').removeClass('text-success text-danger');
        api('get', '/WorkFlow/预检查专家利益冲突', { 步骤编号: stepId }, function (ok, data, error) {
            $(button).prop('disabled', false);
            var passed = ok && data && data.通过 === true;
            $(output).text(passed ? '通过：未发现利益冲突' : '阻断：' + ((data && data.说明) || error || '无法提交评审')).toggleClass('text-success', passed).toggleClass('text-danger', !passed);
        });
    }
    function scoreEndpoint(templateId) {
        if (templateId === 400) return '/YProject/专家评审纵向项目申报';
        if (templateId === 420) return '/YProject/专家评审纵向项目中检';
        return '';
    }
    function scoreForm(row, index) {
        var templateId = Number(row.流程模板编号 || 0), projectId = Number(row.项目编号 || 0), stepId = Number(row.步骤编号 || 0);
        if (!scoreEndpoint(templateId)) return reviewLink(row);
        return '<div class="review-actions" data-template="' + esc(templateId) + '" data-project="' + esc(projectId) + '" data-step="' + esc(stepId) + '">' +
            '<button type="button" class="btn btn-info btn-xs conflict" data-step="' + esc(stepId) + '" data-output="#conflict-' + index + '">检查利益冲突</button> ' +
            '<span id="conflict-' + index + '" class="conflict-result"></span>' +
            '<div class="score-editor" style="margin-top:6px">' +
            '<input class="form-control input-sm score-item" maxlength="100" style="display:inline-block;width:125px" placeholder="评分项" aria-label="评分项"> ' +
            '<input class="form-control input-sm score-value" maxlength="30" style="display:inline-block;width:75px" placeholder="分数" aria-label="分数"> ' +
            '<select class="form-control input-sm score-status" style="display:inline-block;width:105px" aria-label="评审结论"><option value="1">通过</option><option value="0">退回修改</option><option value="-2">终止</option></select> ' +
            '<input class="form-control input-sm score-note" maxlength="1000" style="display:inline-block;width:150px" placeholder="评分说明" aria-label="评分说明"> ' +
            '<button type="button" class="btn btn-warning btn-xs submit-score">提交评分</button>' +
            '</div></div>';
    }
    function render(data) {
        var rows = data && (data.List || data.list) || [];
        if (!rows.length) { $('#review-task-table tbody').html('<tr><td colspan="5" class="text-center text-muted">暂无可处理的评审待办</td></tr>'); return; }
        $('#review-task-table tbody').html(rows.map(function (row, index) {
            var step = Number(row.步骤编号 || 0);
            return '<tr><td>' + esc(row.流程名称 || row.流程模板编号) + '</td><td>' + esc(row.步骤名称) + '<br><small class="text-muted">步骤 #' + esc(step) + '</small></td><td>匿名项目 #' + esc(row.项目编号) + '</td><td>' + esc(row.发起人姓名 || '匿名申请人') + '</td><td>' + scoreForm(row, index) + '</td></tr>';
        }).join(''));
        $('#review-task-table').off('click', '.conflict').on('click', '.conflict', function () { checkConflict(Number($(this).data('step')), this, $(this).data('output')); });
        $('#review-task-table').off('click', '.submit-score').on('click', '.submit-score', function () {
            var editor = $(this).closest('.review-actions'), templateId = Number(editor.data('template')), projectId = Number(editor.data('project')), stepId = Number(editor.data('step'));
            var item = editor.find('.score-item').val().trim(), score = editor.find('.score-value').val().trim(), note = editor.find('.score-note').val().trim(), status = Number(editor.find('.score-status').val());
            if (!item || !score) { message('请填写评分项和分数。', false); return; }
            $(this).prop('disabled', true);
            api('get', '/WorkFlow/预检查专家利益冲突', { 步骤编号: stepId }, function (safe, conflict, error) {
                if (!safe || !conflict || conflict.通过 !== true) { message((conflict && conflict.说明) || error || '利益冲突检查未通过，不能提交评分。', false); $('.submit-score').prop('disabled', false); return; }
                var endpoint = scoreEndpoint(templateId);
                api('post', endpoint, { 步骤编号: stepId, 状态值: status, 备注: note, 附加数据: [{ 流程模板编号: templateId, 项目编号: projectId, 评分项: item, 评分: score, 备注: note }] }, function (ok, value, submitError) {
                    $('.submit-score').prop('disabled', false); message(ok ? '专家评分已提交。' : (submitError || '评分提交失败'), ok); if (ok) load();
                });
            });
        });
    }
    function load() { api('get', '/Office/获取专家评审待办', { Index: 1, Size: 50, OrderType: false }, function (ok, data, error) { if (!ok) { message(error || '当前账号没有专家评审权限', false); $('#review-task-table tbody').html('<tr><td colspan="5" class="text-center text-muted">无法加载评审待办</td></tr>'); return; } render(data); }); }
    $(function () {
        if (!sessionStorage.userInfo && !sessionStorage.keycloakAccessToken && !sessionStorage.access_token) { location.href = '/IMIS/views/login.html'; return; }
        load();
    });
}());
