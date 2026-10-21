#!/usr/bin/env bash
set -euo pipefail

export NO_PROXY="${NO_PROXY:-127.0.0.1,localhost}"
export no_proxy="${no_proxy:-127.0.0.1,localhost}"

: "${APP_BASE_URL:=http://127.0.0.1:16379}"
: "${KEYCLOAK_BASE_URL:=http://127.0.0.1:8180}"
: "${REALM:=scientific-research}"
# 默认只验证未登录边界。启用真实角色验收时，调用者必须显式提供
# 两个由 Keycloak 签发的短期令牌；脚本不会生成、猜测或记录令牌。
: "${AUTH_BOUNDARIES_ROLE_TESTS:=false}"
: "${AUTH_BOUNDARIES_NON_ADMIN_TOKEN:=}"
: "${AUTH_BOUNDARIES_ADMIN_TOKEN:=}"
# 可选的六角色只读 UAT 矩阵。令牌必须由调用者通过环境变量注入；脚本
# 不向 stdout/stderr、文件或请求 URL 输出令牌内容。系统管理员是全局管理
# 角色，默认没有可安全预设为 403 的 GET 端点；可通过
# AUTH_BOUNDARIES_ADMIN_DENY_GET 覆盖为部署方明确约定的只读端点。
: "${AUTH_BOUNDARIES_SIX_ROLE_TESTS:=false}"
: "${AUTH_BOUNDARIES_RESEARCHER_TOKEN:=}"
: "${AUTH_BOUNDARIES_DEPARTMENT_LEAD_TOKEN:=}"
: "${AUTH_BOUNDARIES_SECRETARY_TOKEN:=}"
: "${AUTH_BOUNDARIES_EXPERT_TOKEN:=}"
: "${AUTH_BOUNDARIES_FINANCE_TOKEN:=}"
: "${AUTH_BOUNDARIES_SYSTEM_ADMIN_TOKEN:=}"
: "${AUTH_BOUNDARIES_ADMIN_DENY_GET:=}"
# 真实角色验收可额外要求每个令牌具备可验证的 JWT claims 与 MFA amr。
# 这些开关默认关闭，避免 discovery/未登录门禁伪造或猜测身份；生产 UAT
# 应显式打开，并只注入由 Keycloak 签发的短期令牌。
: "${AUTH_BOUNDARIES_REQUIRE_TOKEN_CLAIMS:=false}"
: "${AUTH_BOUNDARIES_EXPECT_AUDIENCE:=scientificresearch-api}"
: "${AUTH_BOUNDARIES_EXPECT_ISSUER:=}"
: "${AUTH_BOUNDARIES_REQUIRED_CLAIMS:=iss,sub,azp,aud,preferred_username}"
: "${AUTH_BOUNDARIES_REQUIRE_MFA:=false}"
: "${AUTH_BOUNDARIES_MFA_AMR:=otp,mfa}"

python3 - "$APP_BASE_URL" "$KEYCLOAK_BASE_URL" "$REALM" <<'PY'
import json
import os
from urllib.error import HTTPError, URLError
from urllib.parse import quote, parse_qsl, urlencode
from urllib.request import Request, urlopen
import sys

app, keycloak, realm = [x.rstrip('/') for x in sys.argv[1:]]
def get(url, expected, method='GET', token=None, payload=None):
    data = None
    headers = {}
    if method == 'POST':
        data = json.dumps(payload if payload is not None else {}).encode('utf-8')
        headers['Content-Type'] = 'application/json'
    if token:
        headers['Authorization'] = 'Bearer ' + token
    try:
        request = Request(url, data=data, headers=headers, method=method)
        with urlopen(request, timeout=10) as response:
            status=response.status
    except HTTPError as exc:
        status=exc.code
    except (URLError, TimeoutError) as exc:
        raise SystemExit(f'认证边界校验失败: {url}: {exc}')
    if status != expected:
        raise SystemExit(f'认证边界校验失败: {url} 返回 {status}，期望 {expected}')
    print(f'{url} OK ({status})')

def validate_token_claims(token, role_name):
    """检查真实令牌的基本 claims；不验证签名，签名仍由应用 JwtBearer 验证。"""
    required_enabled = os.environ.get('AUTH_BOUNDARIES_REQUIRE_TOKEN_CLAIMS', 'false').lower() in ('1', 'true', 'yes', 'on')
    require_mfa = os.environ.get('AUTH_BOUNDARIES_REQUIRE_MFA', 'false').lower() in ('1', 'true', 'yes', 'on')
    if not required_enabled and not require_mfa:
        return
    parts = token.split('.')
    if len(parts) != 3:
        raise SystemExit(f'角色 {role_name} 的访问令牌不是 JWT')
    try:
        raw = parts[1] + '=' * (-len(parts[1]) % 4)
        claims = json.loads(__import__('base64').urlsafe_b64decode(raw))
    except Exception as exc:
        raise SystemExit(f'角色 {role_name} 的访问令牌 claims 无法解析: {type(exc).__name__}')
    required = {item.strip() for item in os.environ.get('AUTH_BOUNDARIES_REQUIRED_CLAIMS', 'iss,sub,azp,aud,preferred_username').split(',') if item.strip()}
    missing = sorted(item for item in required if not claims.get(item))
    if missing:
        raise SystemExit(f'角色 {role_name} 的访问令牌缺少必需 claims: {", ".join(missing)}')
    expected_aud = os.environ.get('AUTH_BOUNDARIES_EXPECT_AUDIENCE', '').strip()
    aud = claims.get('aud', [])
    if isinstance(aud, str):
        aud = [aud]
    if expected_aud and expected_aud not in aud:
        raise SystemExit(f'角色 {role_name} 的访问令牌缺少 audience: {expected_aud}')
    expected_issuer = os.environ.get('AUTH_BOUNDARIES_EXPECT_ISSUER', '').strip().rstrip('/')
    if expected_issuer and str(claims.get('iss', '')).rstrip('/') != expected_issuer:
        raise SystemExit(f'角色 {role_name} 的访问令牌 issuer 不匹配')
    if claims.get('exp') is None:
        raise SystemExit(f'角色 {role_name} 的访问令牌缺少 exp')
    try:
        if int(claims['exp']) <= int(__import__('time').time()):
            raise SystemExit(f'角色 {role_name} 的访问令牌已过期')
    except (TypeError, ValueError):
        raise SystemExit(f'角色 {role_name} 的访问令牌 exp 无效')
    if require_mfa:
        amr = claims.get('amr', [])
        if isinstance(amr, str):
            amr = [amr]
        accepted = {item.strip() for item in os.environ.get('AUTH_BOUNDARIES_MFA_AMR', 'otp,mfa').split(',') if item.strip()}
        if not accepted.intersection(amr):
            raise SystemExit(f'角色 {role_name} 的访问令牌未通过 MFA amr 验收')
    print(f'角色令牌声明校验通过: {role_name}' + ('（含 MFA）' if require_mfa else ''))

get(app + '/health/live', 200)
get(app + '/health/ready', 200)
get(keycloak + '/realms/' + quote(realm, safe='') + '/.well-known/openid-configuration', 200)
checks = [
    # 代理授权、撤销和人员回收；获取代理范围是公开配置，不列入认证边界。
    ('/Access/Me', 'GET'),
    ('/Access/授权代理', 'POST'),
    ('/Access/撤销代理', 'POST'),
    ('/Access/获取我的代理', 'GET'),
    ('/Access/回收人员代理', 'POST'),
    # 外部系统、评价、统计和迁移治理。
    ('/Operations/获取外部系统', 'GET'),
    ('/Operations/获取外部系统配置检查', 'GET'),
    ('/Operations/获取接口失败告警', 'GET'),
    ('/Operations/获取外部同步失败告警', 'GET'),
    ('/Operations/获取经费适配器契约检查', 'GET'),
    ('/Operations/查询外部同步任务', 'GET'),
    ('/Evaluation/获取科研评价排名', 'GET'),
    ('/Evaluation/导出科研评价结果', 'GET'),
    ('/Evaluation/获取科研评价详情?评价结果编号=1', 'GET'),
    ('/Evaluation/获取科研评价年度批次', 'GET'),
    ('/Evaluation/保存科研评价年度批次', 'POST'),
    ('/Evaluation/开始公示科研评价年度', 'POST'),
    ('/Evaluation/正式发布科研评价年度', 'POST'),
    ('/Evaluation/锁定科研评价年度', 'POST'),
    ('/Evaluation/结算科研评价年度', 'POST'),
    ('/Evaluation/获取年度奖励发放台账', 'GET'),
    ('/Evaluation/复核科研评价', 'POST'),
    ('/Evaluation/提交科研评价申诉', 'POST'),
    ('/Evaluation/处理科研评价申诉', 'POST'),
    ('/Evaluation/登记科研评价奖励', 'POST'),
    ('/Evaluation/确认科研评价奖励发放', 'POST'),
    ('/Analytics/获取科研统计概览', 'GET'),
    ('/Analytics/获取科研统计钻取?指标=科研人员', 'GET'),
    ('/Analytics/获取科研统计指标口径', 'GET'),
    ('/BaseInfo/获取迁移绑定异常', 'GET'),
    # HR、财务、预算异常和伦理同步。
    ('/People/同步HR人员', 'POST'),
    ('/People/预览HR人员同步', 'POST'),
    ('/People/获取人员科研能力画像?人员编号=1', 'GET'),
    ('/People/保存人员科研能力画像', 'POST'),
    ('/People/匹配科研团队成员', 'GET'),
    ('/People/查询科研团队', 'GET'),
    ('/People/获取科研团队详情?团队编号=1', 'GET'),
    ('/People/查询人员项目分工', 'GET'),
    ('/People/查询专家可用时间', 'GET'),
    ('/People/查询外部专家邀请', 'GET'),
    ('/People/保存专家可用时间', 'POST'),
    ('/People/创建外部专家邀请', 'POST'),
    ('/People/回写外部专家邀请回执', 'POST'),
    ('/Expense/同步财务流水', 'POST'),
    ('/Expense/Hrp中间库双向协同', 'POST'),
    ('/Expense/获取Hrp中间库同步状态', 'GET'),
    ('/Expense/获取预算异常对账', 'GET'),
    ('/Expense/获取预算异常处理', 'GET'),
    ('/Expense/获取项目经费分层', 'GET'),
    ('/Expense/按到账维护项目预算', 'POST'),
    ('/Expense/获取按到账项目预算列表', 'GET'),
    ('/Expense/处理预算异常对账', 'POST'),
    ('/Expense/获取科研经费发票查重', 'GET'),
    ('/Expense/登记科研经费发票', 'POST'),
    ('/Expense/复核科研经费发票', 'POST'),
    ('/Expense/获取项目经费预算调整', 'GET'),
    ('/Expense/提交项目经费预算调整', 'POST'),
    ('/Expense/审批项目经费预算调整', 'POST'),
    ('/Expense/获取项目经费年度结算', 'GET'),
    ('/Expense/关闭项目经费年度', 'POST'),
    ('/Expense/重开项目经费年度', 'POST'),
    ('/Expense/回写项目付款状态', 'POST'),
    ('/YProject/同步伦理审批结果', 'POST'),
    ('/YProject/获取纵向项目电子签章服务状态', 'GET'),
    ('/YProject/获取纵向项目正式档案服务状态', 'GET'),
    ('/YProject/提交纵向项目任务书电子签章', 'POST'),
    ('/YProject/提交纵向项目正式电子档案', 'POST'),
    # 独立伦理审查流程：所有查询、提交、受理、委员意见、决定和持续审查动作。
    ('/Ethics/查询伦理申请', 'GET'),
    ('/Ethics/提交伦理申请', 'POST'),
    ('/Ethics/提交伦理审查', 'POST'),
    ('/Ethics/受理伦理申请', 'POST'),
    ('/Ethics/登记伦理委员意见', 'POST'),
    ('/Ethics/登记伦理决定', 'POST'),
    ('/Ethics/提交伦理持续记录', 'POST'),
    ('/Ethics/审核伦理持续记录', 'POST'),
    ('/Ethics/获取伦理申请详情?申请编号=1', 'GET'),
    ('/Ethics/查询伦理到期提醒', 'GET'),
    ('/Ethics/获取伦理持续记录汇总', 'GET'),
    ('/Ethics/获取伦理匿名表决?会议编号=1', 'GET'),
    ('/Ethics/查询伦理会议', 'GET'),
    ('/Ethics/保存伦理会议', 'POST'),
    ('/Ethics/维护伦理会议议程', 'POST'),
    ('/Ethics/获取伦理会议详情?会议编号=1', 'GET'),
    ('/Ethics/保存伦理会议纪要', 'POST'),
    ('/Ethics/确认伦理会议纪要', 'POST'),
    ('/Ethics/查询伦理电子档案', 'GET'),
    ('/Ethics/归档伦理电子材料', 'POST'),
    # 成果治理、转化收益、论文采集和消歧配置。
    ('/Fruit/接收论文采集结果', 'POST'),
    ('/Fruit/从Crossref采集论文', 'POST'),
    ('/Fruit/从PubMed采集论文', 'POST'),
    ('/Fruit/通知专利代理机构', 'POST'),
    ('/Fruit/获取成果治理详情?成果类型=1&成果编号=1', 'GET'),
    ('/Fruit/获取成果治理审计?成果类型=1&成果编号=1', 'GET'),
    ('/Fruit/新增科研成果关联', 'POST'),
    ('/Fruit/核验科研成果作者', 'POST'),
    ('/Fruit/更新科研成果治理状态', 'POST'),
    ('/Fruit/获取科研单位规范', 'GET'),
    ('/Fruit/保存科研单位规范', 'POST'),
    ('/Fruit/保存科研单位别名', 'POST'),
    ('/Fruit/规范科研成果作者单位', 'POST'),
    ('/Fruit/获取成果转化台账', 'GET'),
    ('/Fruit/获取成果转化详情?转化编号=1', 'GET'),
    ('/Fruit/登记成果转化', 'POST'),
    ('/Fruit/审批成果转化', 'POST'),
    ('/Fruit/登记成果转化收益', 'POST'),
    ('/Fruit/保存成果转化收益分配', 'POST'),
    ('/Fruit/回写成果转化财务状态', 'POST'),
    ('/Fruit/获取论文主动网推台账', 'GET'),
    ('/Fruit/触发论文全网主动网推', 'POST'),
    ('/Fruit/一键确认认领网推论文', 'POST'),
    ('/Fruit/获取机构与学者消歧配置', 'GET'),
    ('/Fruit/新增机构别名', 'POST'),
    ('/Fruit/新增学者消歧署名', 'POST'),
    # 字段授权、通知、申报计划和外部队列。
    ('/Operations/测试外部系统连接', 'POST'),
    ('/Permission/获取字段授权规则', 'GET'),
    ('/Permission/保存字段授权规则', 'POST'),
    ('/Permission/获取我的权限变更通知', 'GET'),
    ('/Permission/标记权限变更通知已读', 'POST'),
    ('/YProject/分页获取纵向项目申报计划', 'GET'),
    ('/YProject/保存纵向项目申报计划', 'POST'),
    ('/YProject/发布纵向项目申报计划?编号=1&状态=发布', 'POST'),
    # 电子签章和正式电子档案的供应商门禁；未登录必须先被认证层拒绝。
    ('/YProject/获取纵向项目电子签章服务状态', 'GET'),
    ('/YProject/获取纵向项目正式档案服务状态', 'GET'),
    ('/YProject/提交纵向项目任务书电子签章', 'POST'),
    ('/YProject/提交纵向项目正式电子档案', 'POST'),
    ('/Operations/创建外部同步任务', 'POST'),
    ('/Operations/重试外部同步任务', 'POST'),
    ('/Operations/取消外部同步任务', 'POST'),
]
for path, method in checks:
    prefix, raw_path = path.split('/', 1)
    raw_path, separator, query = raw_path.partition('?')
    encoded_query = urlencode(parse_qsl(query, keep_blank_values=True)) if separator else ''
    get(app + '/' + quote(raw_path, safe='/') + (('?' + encoded_query) if separator else ''), 401, method)

role_tests = os.environ.get('AUTH_BOUNDARIES_ROLE_TESTS', 'false').lower() in ('1', 'true', 'yes', 'on')
if not role_tests:
    print('角色范围边界校验跳过: 未提供真实 Keycloak 令牌（默认只做未登录 401 校验）')
else:
    non_admin = os.environ.get('AUTH_BOUNDARIES_NON_ADMIN_TOKEN', '').strip()
    admin = os.environ.get('AUTH_BOUNDARIES_ADMIN_TOKEN', '').strip()
    if not non_admin or not admin:
        raise SystemExit('角色范围边界校验失败: AUTH_BOUNDARIES_ROLE_TESTS=true 时必须提供 AUTH_BOUNDARIES_NON_ADMIN_TOKEN 和 AUTH_BOUNDARIES_ADMIN_TOKEN')
    validate_token_claims(non_admin, '普通角色')
    validate_token_claims(admin, '系统管理员')
    # 这些 GET 接口在角色检查后才读取数据库，不会写入验收数据。
    protected_gets = [
        '/Permission/获取字段授权规则',
        '/Operations/获取外部系统',
        '/Expense/获取预算异常对账',
    ]
    for path in protected_gets:
        encoded = quote(path.lstrip('/'), safe='/')
        get(app + '/' + encoded, 403, 'GET', non_admin)
        get(app + '/' + encoded, 200, 'GET', admin)
    print('角色范围边界校验通过（普通角色 403，管理角色 200）')

six_role_tests = os.environ.get('AUTH_BOUNDARIES_SIX_ROLE_TESTS', 'false').lower() in ('1', 'true', 'yes', 'on')
if six_role_tests:
    # 这些默认路径均为 GET，且不会创建或修改业务数据。普通科研角色的
    # 工作台摘要按当前登录人过滤；专家待办、财务异常和外部系统配置则
    # 分别覆盖专家、财务和系统管理边界。
    role_matrix = [
        ('科研人员', 'AUTH_BOUNDARIES_RESEARCHER_TOKEN', '/Office/获取科研工作台摘要', '/Operations/获取外部系统'),
        ('科室负责人', 'AUTH_BOUNDARIES_DEPARTMENT_LEAD_TOKEN', '/Office/获取科研工作台摘要', '/Operations/获取外部系统'),
        ('科研秘书', 'AUTH_BOUNDARIES_SECRETARY_TOKEN', '/Office/获取科研工作台摘要', '/Operations/获取外部系统'),
        ('专家', 'AUTH_BOUNDARIES_EXPERT_TOKEN', '/Office/获取专家评审待办?Index=1&Size=10&OrderType=false', '/Operations/获取外部系统'),
        ('财务管理员', 'AUTH_BOUNDARIES_FINANCE_TOKEN', '/Expense/获取预算异常对账', '/Operations/获取外部系统'),
        ('系统管理员', 'AUTH_BOUNDARIES_SYSTEM_ADMIN_TOKEN', '/Operations/获取外部系统', os.environ.get('AUTH_BOUNDARIES_ADMIN_DENY_GET', '').strip()),
    ]
    missing = [env_name for _, env_name, _, _ in role_matrix if not os.environ.get(env_name, '').strip()]
    if missing:
        raise SystemExit('六角色 UAT 校验失败: AUTH_BOUNDARIES_SIX_ROLE_TESTS=true 时必须提供: ' + ', '.join(missing))
    for role_name, env_name, readable, forbidden in role_matrix:
        token = os.environ[env_name].strip()
        validate_token_claims(token, role_name)
        # 先确认令牌能映射到本地人员，再访问角色专属只读接口。两个
        # GET 都不会写入业务数据；readable 可包含查询参数。
        for readable_path in ('/Access/Me', readable):
            read_path, read_separator, read_query = readable_path.lstrip('/').partition('?')
            encoded_read_query = urlencode(parse_qsl(read_query, keep_blank_values=True)) if read_separator else ''
            read_url = app + '/' + quote(read_path, safe='/') + (('?' + encoded_read_query) if read_separator else '')
            get(read_url, 200, 'GET', token)
            print(f'六角色可读校验通过: {role_name} {readable_path} (200)')
        if forbidden:
            deny_path, deny_separator, deny_query = forbidden.lstrip('/').partition('?')
            encoded_deny_query = urlencode(parse_qsl(deny_query, keep_blank_values=True)) if deny_separator else ''
            deny_url = app + '/' + quote(deny_path, safe='/') + (('?' + encoded_deny_query) if deny_separator else '')
            get(deny_url, 403, 'GET', token)
            print(f'六角色应拒绝校验通过: {role_name} {forbidden} (403)')
        else:
            print('六角色应拒绝校验跳过: 系统管理员为全局管理角色，未配置部署方专用 403 GET 端点')
    print('六角色真实 UAT 矩阵校验通过（科研人员、科室负责人、科研秘书、专家、财务管理员、系统管理员）')
print('认证与权限边界校验通过')
PY
