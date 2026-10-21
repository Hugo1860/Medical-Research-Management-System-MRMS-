(function () {
    'use strict';
    var state = { page: 1, size: 15, pages: 1, total: 0, rows: [] };
    function esc(value) { return $('<div>').text(value == null ? '' : value).html(); }
    function api(method, path, data, done) { Connect_Http.httpDatas(method, path, data || {}, function (ok, obj, error) { done(ok, obj && obj.data !== undefined ? obj.data : obj, error); }); }
    function message(text, ok) { $('#message').text(text || '').removeClass('alert-success alert-danger').addClass(ok ? 'alert-success' : 'alert-danger').toggle(!!text); }
    function load() {
        var filter = { Index: state.page, Size: state.size, OrderType: false, Like论文标题: $.trim($('#paper-keyword').val()) };
        api('get', '/Fruit/分页获取可领取论文', filter, function (ok, data, error) {
            if (!ok) { $('#paper-table tbody').html('<tr><td colspan="6">' + esc(error) + '</td></tr>'); return; }
            state.total = Number(data && data.total || 0); state.pages = Math.max(1, Math.ceil(state.total / state.size)); state.rows = data && data.list || [];
            $('#paper-total').text('共 ' + state.total + ' 条'); $('#paper-page').text(state.page + ' / ' + state.pages); $('#paper-prev').prop('disabled', state.page <= 1); $('#paper-next').prop('disabled', state.page >= state.pages);
            $('#paper-table tbody').html(state.rows.length ? state.rows.map(function (row) { return '<tr><td>' + esc(row.论文标题) + '</td><td>' + esc(row.刊物名称) + '</td><td>' + esc(row.论文Online日期) + '</td><td>' + esc(row.论文正式出版日期) + '</td><td>' + esc(row.第一作者) + '</td><td><button class="btn btn-success btn-xs claim" data-id="' + esc(row.编号) + '">认领</button></td></tr>'; }).join('') : '<tr><td colspan="6">暂无可认领论文</td></tr>');
        });
    }
    $('#paper-search').on('click', function () { state.page = 1; load(); });
    $('#paper-prev').on('click', function () { if (state.page > 1) { state.page--; load(); } });
    $('#paper-next').on('click', function () { if (state.page < state.pages) { state.page++; load(); } });
    $('#paper-table').on('click', '.claim', function () { var id = Number($(this).data('id')); if (!id || !window.confirm('确认认领这篇论文并提交审核？')) return; api('post', '/Fruit/认领论文', { 论文编号: id, IsHold: false }, function (ok, data, error) { message(ok ? '论文已提交认领审核。' : error, ok); if (ok) load(); }); });
    $('#journal-identify').on('click', function () { api('get', '/Fruit/识别论文期刊信息', { 刊物名称: $('#journal-name').val(), ISSN号: $('#journal-issn').val(), CN号: $('#journal-cn').val() }, function (ok, rows, error) { if (!ok) { $('#journal-table tbody').html('<tr><td colspan="7">' + esc(error) + '</td></tr>'); return; } $('#journal-table tbody').html(rows && rows.length ? rows.map(function (x) { return '<tr><td>' + esc(x.名称 || x.英文刊名) + '</td><td>' + esc(x.ISSN号) + '</td><td>' + esc(x.CN号) + '</td><td>' + esc(x.收录情况) + '</td><td>' + esc(x.JCR分区) + '</td><td>' + esc(x.影响因子) + '</td><td>' + esc(x.匹配分数) + '</td></tr>'; }).join('') : '<tr><td colspan="7">未匹配到期刊记录</td></tr>'); }); });
    if (!sessionStorage.userInfo && !sessionStorage.keycloakAccessToken && !sessionStorage.access_token) location.href = '/IMIS/views/login.html'; else load();
}());
