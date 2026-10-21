(function () {
    'use strict';

    var configs = {
        'y-application': { title: '纵向项目申报', description: '查看当前账号可访问的纵向项目申报记录。', endpoint: '/YProject/分页获取科研项目申报', filter: 'Like项目名称', columns: ['项目名称', '分类名称', '姓名', '部门名称', '项目级别', '申请经费', '申报时间'] },
        'y-approval': { title: '纵向项目立项', description: '查看当前账号可访问的纵向项目立项记录。', endpoint: '/YProject/分页获取科研项目立项', filter: 'Like项目中文名称', columns: ['项目中文名称', '分类名称', '项目级别', '项目来源单位', '负责人姓名', '批准经费', '配套经费'] },
        'y-midterm': { title: '纵向项目中检', description: '查看纵向项目中检记录和当前流程状态。', endpoint: '/YProject/分页获取纵向项目中检', filter: 'Like项目中文名称', columns: ['项目中文名称', '分类名称', '负责人姓名', '中检日期', '状态名称', '建立时间'] },
        'y-change': { title: '纵向项目变更', description: '查看纵向项目变更申请和审核进度。', endpoint: '/YProject/分页获取纵向项目变更', filter: 'Like项目中文名称', columns: ['项目中文名称', '分类名称', '变更事由', '负责人姓名', '状态名称', '建立时间'] },
        'y-close': { title: '纵向项目结题', description: '查看纵向项目结题记录和审核进度。', endpoint: '/YProject/分页获取纵向项目结题', filter: 'Like项目中文名称', columns: ['项目中文名称', '分类名称', '负责人姓名', '结题日期', '状态名称', '建立时间'] },
        'y-finished': { title: '已结纵向项目', description: '已结项目统一从立项列表查询，后续可按结题状态继续筛选。', endpoint: '/YProject/分页获取科研项目立项', filter: 'Like项目中文名称', columns: ['项目中文名称', '分类名称', '项目级别', '负责人姓名', '批准经费', '计划完成日期'] },
        'x-list': { title: '横向项目列表', description: '查看当前账号可访问的横向项目和合同信息。', endpoint: '/XProject/分页获取横向项目', filter: 'Like合同名称', columns: ['合同名称', '合同编号', '合同金额', '签订时间', '项目负责人姓名', '项目负责人部门名称'] },
        'x-change': { title: '横向项目变更', description: '查看横向项目变更申请和审核进度。', endpoint: '/XProject/分页获取横向项目变更', filter: 'Like合同名称', columns: ['合同名称', '合同编号', '变更事由', '状态名称', '建立时间'] },
        'x-close': { title: '横向项目结题', description: '查看横向项目结题记录和审核进度。', endpoint: '/XProject/分页获取横向项目结题', filter: 'Like合同名称', columns: ['合同名称', '合同编号', '状态名称', '结题日期', '建立时间'] },
        'y-income': { title: '纵向经费到账', description: '查看纵向项目批准经费、到账金额和待审核到账。', endpoint: '/Incoming/分页获取纵向项目经费合计到账列表', filter: 'Like项目中文名称', columns: ['项目中文名称', '负责人姓名', '批准经费', '经费', '未到经费', '待审核到账', '已审核到账'] },
        'x-income': { title: '横向经费到账', description: '查看横向项目合同金额、到账金额和待审核到账。', endpoint: '/Incoming/分页获取横向项目经费合计到账列表', filter: 'Like合同名称', columns: ['合同名称', '合同编号', '合同金额', '经费', '未到经费', '待审核到账', '已审核到账'] },
        'y-expense': { title: '纵向经费报销', description: '查看纵向项目经费执行和报销汇总。', endpoint: '/Expense/分页获取纵向项目经费合计报销列表', filter: 'Like项目中文名称', columns: ['项目中文名称', '负责人姓名', '批准经费', '经费', '剩余经费', '待审核报销', '已审核报销'] },
        'x-expense': { title: '横向经费报销', description: '查看横向项目经费执行和报销汇总。', endpoint: '/Expense/分页获取横向项目经费合计报销列表', filter: 'Like合同名称', columns: ['合同名称', '合同编号', '合同金额', '经费', '剩余经费', '待审核报销', '已审核报销'] },
        'bill-search': { title: '经费单据查找', description: '按项目名称、合同名称或单据关键字查询经费汇总。', endpoint: '/Expense/分页获取纵向项目经费合计报销列表', filter: 'Like项目中文名称', columns: ['项目中文名称', '负责人姓名', '批准经费', '经费', '剩余经费', '已审核报销'] },
        'lifecycle': { title: '项目生命周期管理', description: '进入项目生命周期页面提交延期、暂停、终止申请、研究报告和档案材料。', endpoint: '/YProject/分页获取科研项目立项', filter: 'Like项目中文名称', columns: ['项目中文名称', '项目级别', '负责人姓名', '计划完成日期', '审核进度'] }
    };

    var links = [
        ['纵向项目申报', 'y-application'], ['纵向项目立项', 'y-approval'], ['纵向项目中检', 'y-midterm'],
        ['纵向项目变更', 'y-change'], ['纵向项目结题', 'y-close'], ['横向项目列表', 'x-list'],
        ['横向项目变更', 'x-change'], ['横向项目结题', 'x-close'], ['纵向经费到账', 'y-income'],
        ['横向经费到账', 'x-income'], ['纵向经费报销', 'y-expense'], ['横向经费报销', 'x-expense'],
        ['预算异常对账', null], ['项目生命周期', 'lifecycle']
    ];

    var state = { module: '', config: null, page: 1, size: 15, total: 0, pages: 1 };

    function query(name) {
        var match = new RegExp('[?&]' + name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '=([^&]*)').exec(location.search);
        return match ? decodeURIComponent(match[1]) : '';
    }

    function escapeHtml(value) {
        return $('<div>').text(value == null ? '' : value).html();
    }

    function formatValue(value) {
        if (value == null || value === '') return '';
        if (typeof value === 'string' && value.length > 18 && value.indexOf('T') > 8) return escapeHtml(value.substring(0, 10));
        if (typeof value === 'number') return escapeHtml(value.toLocaleString('zh-CN', { maximumFractionDigits: 2 }));
        return escapeHtml(value);
    }

    function payloadOf(obj) {
        var payload = obj && obj.data !== undefined ? obj.data : obj;
        if (payload && payload.list) return payload;
        if (Array.isArray(payload)) return { list: payload, total: payload.length };
        return { list: [], total: 0 };
    }

    function renderLinks() {
        var html = links.map(function (item) {
            var href = item[1] === 'lifecycle' ? '项目生命周期.html' : (item[1] === 'x-list' ? '横向项目流程.html' : (item[1] ? '科研业务台账.html?module=' + item[1] : '权限代理与预算对账.html'));
            var active = item[1] === state.module ? ' active' : '';
            return '<a class="btn btn-default btn-sm' + active + '" href="' + href + '">' + escapeHtml(item[0]) + '</a>';
        }).join(' ');
        $('#quick-links').html(html);
    }

    function renderTable(rows) {
        var columns = state.config.columns;
        var head = '<tr>' + columns.map(function (column) { return '<th>' + escapeHtml(column) + '</th>'; }).join('') + '</tr>';
        var body = rows.length ? rows.map(function (row) {
            return '<tr>' + columns.map(function (column) { return '<td>' + formatValue(row[column]) + '</td>'; }).join('') + '</tr>';
        }).join('') : '<tr><td colspan="' + columns.length + '" class="text-center text-muted">暂无数据</td></tr>';
        $('#ledger-table thead').html(head);
        $('#ledger-table tbody').html(body);
    }

    function load() {
        var params = { Index: state.page, Size: state.size, OrderType: false };
        var keyword = $.trim($('#keyword').val());
        if (keyword && state.config.filter) params[state.config.filter] = keyword;
        $('#ledger-table tbody').html('<tr><td colspan="20" class="text-center text-muted">正在加载…</td></tr>');
        Connect_Http.httpDatas('get', state.config.endpoint, params, function (success, obj, error) {
            if (!success) {
                renderTable([]);
                $('#total').text(error || '加载失败');
                return;
            }
            var result = payloadOf(obj);
            state.total = Number(result.total || result.list.length || 0);
            state.pages = Math.max(1, Math.ceil(state.total / state.size));
            renderTable(result.list || []);
            $('#total').text('共 ' + state.total + ' 条');
            $('#page').text(state.page + ' / ' + state.pages);
            $('#prev').prop('disabled', state.page <= 1);
            $('#next').prop('disabled', state.page >= state.pages);
        });
    }

    $(function () {
        if (!sessionStorage.userInfo && !sessionStorage.keycloakAccessToken && !sessionStorage.access_token) {
            location.href = '/IMIS/views/login.html';
            return;
        }
        state.module = query('module') || 'y-application';
        state.config = configs[state.module] || configs['y-application'];
        $('#page-title').text(state.config.title);
        $('#page-description').text(state.config.description);
        renderLinks();
        $('#search, #refresh').on('click', function () { state.page = 1; load(); });
        $('#keyword').on('keydown', function (event) { if (event.keyCode === 13) { state.page = 1; load(); } });
        $('#prev').on('click', function () { if (state.page > 1) { state.page--; load(); } });
        $('#next').on('click', function () { if (state.page < state.pages) { state.page++; load(); } });
        load();
    });
}());
