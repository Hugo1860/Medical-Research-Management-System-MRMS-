# 外部系统适配器配置模板

该模板用于把真实 HR、财务、采购、伦理、合同、人才库、生物样本库和论文采集系统接入统一外部同步队列。每个系统先在“外部系统”表登记，再由适配器把统一任务 envelope 转换为供应商协议。未完成字段映射、签名和联调前，保持 `ExternalSync__Enabled=false`。

生产登记必须提供供应商确认过的**真实地址**；通过测试连接、幂等重放和业务字段验收取得的**真实回执**，才可以作为联调证据。示例域名和模拟响应不构成上线配置。

## 系统登记

| 字段 | 示例 | 说明 |
|---|---|---|
| 系统代码 | `HIS-HR` | 全局唯一、稳定不变 |
| 系统类型 | `HR` | `HR`、`财务`、`伦理`、`论文采集` 等 |
| 基础地址 | `https://adapter.example.org/research` | 只填写适配器地址，不把供应商密钥写入数据库 |
| 认证方式 | `Bearer` | 与部署环境密钥引用一致 |
| 认证引用 | `HIS_HR_TOKEN` | 对应 `ExternalSystems__Secrets__HIS_HR_TOKEN` |
| 超时秒数 | `30` | 按供应商 SLA 设置 |
| 最大重试次数 | `3` | 按系统配置继承到任务；网络错误、超时、408、429 和 5xx 重试，其他 4xx 终止 |

### 启用前的必填规则

`是否启用=true` 的登记必须同时满足：基础地址是完整的 `http://` 或 `https://` URL（生产环境使用 HTTPS）、超时为 1–120 秒、最大重试次数为 0–10，并且认证方式不是“无”时认证引用只能使用字母、数字、点、下划线和短横线。认证引用 `HIS_HR_TOKEN` 必须对应部署环境中的非空配置键 `ExternalSystems__Secrets__HIS_HR_TOKEN`；密钥值不写入数据库，也不在日志和预检报告中输出。

八类必需系统（HR、财务、采购、伦理、合同、人才库、生物样本库、论文采集）各保留一个启用的适配器。可以暂时登记停用的候选，但不能在没有完成字段、认证和密钥检查时启用 worker。`http://localhost`、`http://127.0.0.1` 和 `http://[::1]` 只用于本机联调；非本机的生产地址必须使用 HTTPS。

## 统一任务 envelope

worker 发送 POST JSON，并同时设置 `X-Request-ID` 与 `Idempotency-Key`：

```json
{
  "businessType": "人员变更",
  "direction": "推送",
  "businessKey": "person:1001:2026-10-14",
  "idempotencyKey": "hr:HIS-HR:person:1001:2026-10-14",
  "requestId": "generated-request-id",
  "payload": {}
}
```

适配器必须返回 HTTP 2xx 表示已接受；除 408 和 429 外的 4xx 进入最终失败并保留错误；网络错误、请求超时、408、429 和 5xx 由 worker 按退避策略重试。429 如果返回 `Retry-After`，worker 会遵循该等待时间（最多 1 小时），否则使用指数退避。响应摘要只保存业务编号、状态和必要诊断，不保存 token 或完整敏感响应。每个外部系统的超时秒数和最大重试次数从系统登记配置继承，单个任务可以显式覆盖最大重试次数。

P2 回执必须包含供应商业务编号或可追踪的受理状态，并能通过 `X-Request-ID` 与 `Idempotency-Key` 关联本地任务。worker 会在保存响应摘要和错误前脱敏 token、密码、Cookie、`Authorization`、API key、`Bearer`/`Basic` 凭据；适配器不得把这些字段写进业务 payload、错误消息或日志。入站回调还必须校验签名和时间窗，重复回调按幂等键丢弃，不得直接覆盖已认领的本地成果或已确认的付款状态。

## 推荐字段映射

### HR

`externalPersonId` → `人员外部映射.外部人员编号`；`employeeNo` → `人员.工号`；`name` → `人员.姓名`；`departmentCode` → `部门.外部部门编号`；`jobTitle` → `人员.岗位名称`；`active=false` → 禁用人员并回收代理授权。全量批次会将该 HR 系统映射中未出现在完整名单的人员标记离岗并触发禁用；批次存在无效或重复外部人员编号时不会执行缺席人员禁用。

### 财务

`flowNo` → `项目经费台账.流程模板编号/项目编号`；`businessNo` → `业务编号`；`businessType` → `业务类型`；`amount` → `金额`；`postedAt` → `实际付款日期`。业务编号和外部流水号必须组成稳定幂等键。

### 伦理

`approvalId` → `项目伦理同步记录.外部审批编号`；`approvalNo` → `批文编号`；`status` → `状态`；`approvedAt` → `批准时间`；`expiresAt` → `有效期至`；`attachmentUrl` → `批文路径`。

### 采购

`purchaseRequestId` → 采购申请业务主键；`projectNo` → 项目编号；`vendorId` → 供应商外部编号；`contractNo` → 合同编号；`amount` → 采购金额；`status` → 采购状态；`approvedAt` → 审批时间。采购订单、验收和付款事件必须分别使用稳定幂等键，禁止用供应商名称代替供应商编号。

### 合同

`contractId` → 合同外部编号；`contractNo` → 合同编号；`projectNo` → 横向项目编号；`partyName` → 合同对方名称；`signedAt` → 签订时间；`effectiveFrom`/`effectiveTo` → 合同有效期；`status` → 合同状态；`attachmentUrl` → 合同文件路径。合同变更、付款节点和终止事件必须保留原合同编号及版本号。

### 人才库

`talentId` → 人才外部编号；`employeeNo` → 工号；`researchAreas` → 研究专长；`technicalServices` → 可提供技术服务；`mentorId` → 导师外部编号；`availability` → 合作意愿；`updatedAt` → 画像更新时间。人才画像只同步业务必要字段，联系方式和证件信息不得进入同步任务日志。

### 生物样本库

`sampleProjectId` → 样本项目外部编号；`sampleId` → 样本外部编号；`sampleType` → 样本类型；`quantity` → 样本数量；`storageLocation` → 保存位置编码；`status` → 样本状态；`releasedAt` → 领取/释放时间。同步只记录样本编码和数量等最小必要信息，不传输受试者直接身份信息；样本状态变更必须可追溯到项目和伦理审批编号。

### 专利代理通知

`POST /Fruit/通知专利代理机构` 会将申请、授权、变更、年费和撤回事件写入统一同步队列；外部系统登记为“合同”或“其他”类型，幂等键由专利编号、外部系统和事件类型组成。

### 论文采集

`sourceId` → 采集任务业务主键；`title` → 论文标题；`authors` → 作者候选；`journal`、`issn`、`cn` → 期刊识别输入；`doi` → 外部对象编号；`publishedAt` → 出版日期。适配器将结果提交到 `POST /Fruit/接收论文采集结果`，系统按 `外部系统编号+外部论文编号` 幂等落库、自动匹配期刊并进入认领队列，不应直接替换已认领成果。

系统同时提供 `POST /Fruit/从Crossref采集论文` 的 Crossref 公共 API 适配器，可按关键词、作者和出版日期查询，按 DOI 幂等落库，尝试识别本地期刊和第一作者。该接口仍要求已登记“论文采集”类型的外部系统编号，并只允许系统管理员或科研管理员调用。

系统同时提供 `POST /Fruit/从PubMed采集论文` 的 NCBI E-utilities 适配器，可按关键词、作者和出版日期检索 PubMed，按 PMID 幂等落库，保留标题、作者、期刊、ISSN 和出版日期并进入待认领队列。调用同样要求登记“论文采集”类型的外部系统编号，并受系统管理员或科研管理员权限保护。

## 联调验收清单

1. 验证 TLS、认证引用和请求签名。
2. 用同一幂等键重复提交，确认外部系统只处理一次。
3. 分别验证 2xx、4xx、5xx、超时和连接失败。
4. 检查 `请求ID`、尝试次数、下次重试时间和最后错误。
5. 对 HR 离岗、财务冲销、伦理过期、论文重复记录分别做回放。
6. 对采购订单、合同变更、人才画像更新和样本状态变更分别做回放，验证字段白名单和敏感字段脱敏。
7. 使用真实管理员账号执行 `verify-business-contract.sh`，再开启 `ExternalSync__Enabled`。

## 干运行和测试连接报告

### Docker 恢复后的 HR/财务首批配置

仓库提供 `deploy/register-core-adapters.sh` 作为首批配置预检和登记入口。它只接受适配器地址、认证方式和**认证引用名**，从不读取或打印 token；默认是 dry-run，不访问数据库，也不发起供应商请求：

```bash
HR_ADAPTER_ENDPOINT=https://<hr-adapter-host>/research \
FINANCE_ADAPTER_ENDPOINT=https://<finance-adapter-host>/research \
./deploy/register-core-adapters.sh --dry-run
```

确认地址、字段协议、脱敏测试账号和回执格式后，才在受控维护窗口执行登记。`--apply` 必须显式设置 `CONFIRM_EXTERNAL_ADAPTER_WRITE=YES`、`ADAPTER_OPERATOR_ID`，并且默认把两行登记为停用；若通过 `CORE_ADAPTERS_ENABLED=true` 启用其中一行，脚本会拒绝同类型已有多个启用适配器，但仍不会打开 worker：

```bash
HR_ADAPTER_ENDPOINT=https://<hr-adapter-host>/research \
FINANCE_ADAPTER_ENDPOINT=https://<finance-adapter-host>/research \
ADAPTER_OPERATOR_ID=<启用的本地人员编号> \
CONFIRM_EXTERNAL_ADAPTER_WRITE=YES \
./deploy/register-core-adapters.sh --apply
```

生产地址必须使用 HTTPS；HTTP 只可在显式设置 `ALLOW_HTTP_EXTERNAL_ADAPTER=true` 的本机联调中使用。部署环境再以 `HIS_HR_TOKEN`、`FINANCE_API_TOKEN` 注入与登记引用匹配的密钥，密钥不写入 `.env.docker.example`、数据库或任务负载。登记后依次执行 `verify-external-adapter-contract.sh`、`verify-finance-adapter-contract.sh`，由运行管理员对每个编号调用“测试外部系统连接”；只有收到供应商 2xx 受理、幂等重放结果和业务回执，且 HR 离岗/财务付款回写均完成，才可以评审是否开启八类系统的 worker。未完成其余六类系统时，保持 `EXTERNAL_SYNC_ENABLED=false`。

启用 worker 前先运行只读干运行报告：

```bash
DATABASE_CONTAINER=2018-7-24-db-1 \
APP_CONTAINER=2018-7-24-app-1 \
./deploy/verify-external-adapter-contract.sh --dry-run
```

脚本只读取 PostgreSQL 的系统登记和同步队列，以及应用容器中的认证引用名称；不会解析、打印或发送密钥，也不会请求供应商地址。报告分为“逐项登记”和“八类必需系统汇总”，分别指出未登记、停用、字段缺失、认证引用未注入、超时/重试无效和待处理队列问题。worker 关闭时，预检通过只表示本地配置结构可审阅，不表示任何外部系统已联调成功；worker 开启时，八类系统、认证密钥和队列幂等字段任一缺失都会阻断启动。

管理端的“测试连接”操作是显式的网络动作，只对指定编号发起一次带 `X-Request-ID` 的 GET，并返回状态码、耗时和“目标已响应但非 2xx”等诊断，不会改变启用状态。正式联调前应先用上述干运行报告，再由运行管理员对已确认的测试地址逐个执行测试连接；不能把未执行测试连接或预检通过记录为供应商成功。
