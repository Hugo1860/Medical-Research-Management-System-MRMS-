(function () {
    'use strict';
    function esc(value) { return $('<div>').text(value == null ? '' : value).html(); }
    function message(text, ok) { var el = $('#message'); el.text(text || '').removeClass('alert-success alert-danger alert-warning').addClass('alert-' + (ok ? 'success' : 'danger')).toggle(!!text); }
    function api(method, path, data, done) { Connect_Http.httpDatas(method, path, data || {}, function (ok, obj, error) { done(ok, obj && obj.data !== undefined ? obj.data : obj, error); }); }
    function payload() {
        var form = document.getElementById('application-form');
        function val(name) { return form.elements[name].value; }
        function checked(name) { return !!form.elements[name].checked; }
        return {
            基本资料: {
                分类: Number(val('分类')), 项目名称: val('项目名称').trim(), 申报年度: Number(val('申报年度')), 申请经费: Number(val('申请经费')),
                计划开始日期: val('计划开始日期'), 计划完成日期: val('计划完成日期'), 项目类型: val('项目类型').trim(), 项目级别: val('项目级别').trim(), 项目来源单位: val('项目来源单位').trim(),
                项目摘要: val('项目摘要').trim(), 项目申报书路径: val('项目申报书路径').trim(), 是否需要伦理评审: checked('是否需要伦理评审'), 是否需要伦理批文: checked('是否需要伦理批文'),
                伦理批文编号: val('伦理批文编号').trim(), 伦理批文路径: val('伦理批文路径').trim(), 是否委托外单位进行实验或实验分析: checked('是否委托外单位进行实验或实验分析'), 实验委托检测内容: val('实验委托检测内容').trim()
            }, 参与人列表: [], IsHold: false
        };
    }
    function renderChecks(data) {
        var rows = (data && data.检查项) || [];
        $('#check-table tbody').html(rows.length ? rows.map(function (x) { return '<tr><td>' + esc(x.名称) + '</td><td class="' + (x.通过 ? 'check-pass' : 'check-fail') + '">' + (x.通过 ? '通过' : '未通过') + (x.阻断提交 ? '（阻断）' : '') + '</td><td>' + esc(x.提示) + '</td></tr>'; }).join('') : '<tr><td colspan="3">暂无检查结果</td></tr>');
    }
    function listRows(list, empty, builder) { return list && list.length ? list.map(builder).join('') : empty; }
    function renderReview(data) {
        var basic = data && data.基本信息;
        if (!basic) { $('#review-basic').text('未找到申报记录。'); return; }
        $('#review-basic').html('<strong>' + esc(basic.项目名称) + '</strong>　申报年度：' + esc(basic.申报年度) + '　流程状态：' + esc(basic.流程状态 || '未开始') + '　是否立项：' + (basic.是否立项 ? '是' : '否'));
        $('#review-versions tbody').html(listRows(data.版本, '<tr><td colspan="4">暂无版本记录</td></tr>', function (x) { return '<tr><td>' + esc(x.版本号) + '</td><td>' + esc(x.变更类型) + '</td><td>' + esc(x.变更人编号) + '</td><td>' + esc(x.建立时间) + '</td></tr>'; }));
        $('#review-opinions tbody').html(listRows(data.评审意见, '<tr><td colspan="5">暂无公开评审意见</td></tr>', function (x) { return '<tr><td>' + esc(x.评审人姓名 || x.评审人编号) + '</td><td>' + esc(x.意见类型) + '</td><td style="white-space:pre-wrap">' + esc(x.意见内容) + '</td><td>' + (x.是否公开给申请人 ? '是' : '否') + '</td><td>' + esc(x.建立时间) + '</td></tr>'; }));
        $('#review-scores tbody').html(listRows(data.专家评分, '<tr><td colspan="4">暂无专家评分</td></tr>', function (x) { return '<tr><td>' + esc(x.评分项) + '</td><td>' + esc(x.评分) + '</td><td>' + esc(x.备注) + '</td><td>' + esc(x.建立时间) + '</td></tr>'; }));
        $('#review-recaps tbody').html(listRows(data.未获资助复盘, '<tr><td colspan="5">暂无复盘记录</td></tr>', function (x) { return '<tr><td>' + esc(x.原因分类) + '</td><td>' + esc(x.复盘结论) + '</td><td>' + esc(x.改进措施) + '</td><td>' + esc(x.负责人姓名 || x.负责人编号) + '</td><td>' + esc(x.建立时间) + '</td></tr>'; }));
        $('#version-compare-output').hide().text('');
    }
    $('#preflight').on('click', function () { api('post', '/YProject/预检查纵向项目申报', payload(), function (ok, data, error) { if (!ok) { message(error, false); return; } renderChecks(data); message(data.通过 ? '预检查通过，可以提交申报。' : '预检查未通过，请按提示补齐材料。', data.通过); }); });
    $('#application-form').on('submit', function (event) {
        event.preventDefault();
        var data = payload(); var endpoint = { '1': '/YProject/新增科研项目申报', '2': '/YProject/新增重点学科申报', '3': '/YProject/新增人才培养申报' }[String(data.基本资料.分类)];
        api('post', endpoint, data, function (ok, result, error) { message(ok ? '申报已提交，已进入审核流程。' : error, ok); if (ok) $('#preflight').trigger('click'); });
    });
    $('#review-query').on('click', function () {
        var id = Number($('#review-id').val()); if (!id) { message('请输入纵向项目申报编号。', false); return; }
        api('get', '/YProject/获取纵向项目申报评审汇总', { 纵向项目申报编号: id }, function (ok, data, error) { if (!ok) { $('#review-basic').text(error); return; } renderReview(data); });
    });
    $('#version-compare').on('click', function () {
        var id = Number($('#review-id').val()), left = Number($('#left-version').val()), right = Number($('#right-version').val());
        if (!id || !left || !right || left === right) { message('请输入申报编号及两个不同的版本号。', false); return; }
        api('get', '/YProject/比较纵向项目申报版本', { 纵向项目申报编号: id, 左版本号: left, 右版本号: right }, function (ok, data, error) { var out = $('#version-compare-output'); out.text(ok ? JSON.stringify(data || {}, null, 2) : error).show(); });
    });
    $('#conflict-check').on('click', function () {
        var stepId = Number($('#review-step-id').val());
        if (!stepId) { $('#conflict-output').text('请输入评审步骤编号。').removeClass('check-pass').addClass('check-fail'); return; }
        api('get', '/WorkFlow/预检查专家利益冲突', { 步骤编号: stepId }, function (ok, data, error) {
            if (!ok) { $('#conflict-output').text(error).removeClass('check-pass').addClass('check-fail'); return; }
            var passed = data && data.通过 === true;
            $('#conflict-output').text(passed ? '通过：未发现利益冲突。' : '阻断：' + (data.说明 || '存在利益冲突。')).toggleClass('check-pass', passed).toggleClass('check-fail', !passed);
        });
    });
    $('#review-comment-form').on('submit', function (event) {
        event.preventDefault(); var form = event.target;
        api('post', '/YProject/新增纵向项目评审意见', {
            项目编号: Number(form.elements.项目编号.value), 流程模板编号: Number(form.elements.流程模板编号.value),
            步骤编号: form.elements.步骤编号.value ? Number(form.elements.步骤编号.value) : null,
            意见类型: form.elements.意见类型.value, 意见内容: form.elements.意见内容.value,
            是否公开给申请人: !!form.elements.是否公开给申请人.checked
        }, function (ok, data, error) { message(ok ? '评审意见已登记。' : error, ok); if (ok) form.reset(); });
    });
    $('#review-score-form').on('submit', function (event) {
        event.preventDefault(); var form = event.target; var stepId = Number($('#review-step-id').val());
        if (!stepId) { message('请先填写评审步骤编号。', false); return; }
        api('get', '/WorkFlow/预检查专家利益冲突', { 步骤编号: stepId }, function (safe, conflict) {
            if (!safe || !conflict || conflict.通过 !== true) { message((conflict && conflict.说明) || '利益冲突检查未通过，不能提交评分。', false); return; }
            var projectId = Number($('#review-comment-form [name="项目编号"]').val() || $('#review-id').val());
            var templateId = Number($('#review-comment-form [name="流程模板编号"]').val());
            if (!projectId || !templateId) { message('请填写申报编号和流程模板编号。', false); return; }
            api('post', '/YProject/专家评审纵向项目申报', { 步骤编号: stepId, 状态值: Number(form.elements.状态值.value), 备注: form.elements.评审备注.value, 附加数据: [{ 流程模板编号: templateId, 项目编号: projectId, 评分项: form.elements.评分项.value, 评分: form.elements.评分.value, 备注: form.elements.评审备注.value }] }, function (ok, data, error) { message(ok ? '专家评分已提交。' : error, ok); if (ok) form.reset(); });
        });
    });
    var category = new URLSearchParams(location.search).get('category');
    if (category && ['1', '2', '3'].indexOf(category) >= 0) document.querySelector('[name="分类"]').value = category;
    if (!sessionStorage.userInfo && !sessionStorage.keycloakAccessToken && !sessionStorage.access_token) location.href = '/IMIS/views/login.html';
}());
