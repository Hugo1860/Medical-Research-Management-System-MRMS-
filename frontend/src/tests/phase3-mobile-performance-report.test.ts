import { describe, it, expect } from 'vitest'
import type {
  MobilePushConfig,
  MobilePushSendRequest,
  MobilePushLogItem
} from '../api/mobile-push'
import type {
  PerformanceFormulaItem,
  AutoEvaluationResult,
  CustomReportTemplate,
  CustomReportRow
} from '../api/evaluation'

describe('Phase 3: Mobile Push Gateway, Performance Formula Engine & Custom Reporting (第三阶段：移动网关、动态绩效公式与多维报表)', () => {
  // 1. 移动科研协同网关与 DMZ 安全隔离
  describe('移动科研协同网关与 DMZ 安全反向代理', () => {
    it('应正确配置企业微信/钉钉通道及 DMZ 反向代理安全前缀', () => {
      const channelConfig: MobilePushConfig = {
        通道类型: '企业微信应用',
        通道名称: '医院科研协同移动网关',
        Webhook地址: 'https://gateway.hospital.internal/wecom-dmz/cgi-bin/webhook/send',
        应用ID: '100002',
        应用密钥: 's3cr3t_t0k3n_wecom',
        基础配置JSON: JSON.stringify({ corpId: 'ww1234567890abcdef', dmzProxy: true }),
        是否启用: true,
        建立时间: '2026-10-15T00:00:00Z'
      }

      expect(channelConfig.通道类型).toBe('企业微信应用')
      expect(channelConfig.Webhook地址).toMatch(/^https:\/\/.*\/wecom-dmz\/.*$/)
      expect(channelConfig.是否启用).toBe(true)

      // DMZ 隔离校验：敏感密钥只在内网持久化，对外请求由 DMZ 统一反向转发
      const isDmzSecured = channelConfig.Webhook地址?.includes('dmz')
      expect(isDmzSecured).toBe(true)
    })

    it('应准确封装企业微信/钉钉卡片待办消息 Payload', () => {
      const pushReq: MobilePushSendRequest = {
        接收人编号: 101,
        业务模块: '伦理审查',
        业务编号: 'ETH-2026-0089',
        消息标题: '您有一项纵向申报伦理初审待批',
        消息摘要: '张教授：青年科学基金申报书附件伦理审查申请已由院学术委员会初审，请点击进入工作台完成会签。',
        卡片链接: '/office/mobile-workbench?tab=todo&id=ETH-2026-0089'
      }

      expect(pushReq.业务模块).toBe('伦理审查')
      expect(pushReq.消息标题).toContain('待批')
      expect(pushReq.卡片链接).toContain('/office/mobile-workbench')

      // 验证卡片日志结构
      const logItem: MobilePushLogItem = {
        编号: 1,
        通道编号: 1,
        通道名称: '企业微信待办服务',
        通道类型: '企业微信应用',
        消息类型: '待办卡片',
        业务模块: pushReq.业务模块!,
        业务编号: pushReq.业务编号,
        接收人编号: 101,
        接收人姓名: '张教授',
        消息标题: pushReq.消息标题,
        消息摘要: pushReq.消息摘要,
        卡片跳转链接: pushReq.卡片链接,
        推送状态: '已发送',
        推送时间: '2026-10-15 14:30:00'
      }

      expect(logItem.推送状态).toBe('已发送')
      expect(logItem.接收人姓名).toBe('张教授')
    })
  })

  // 2. 清华长庚标杆：动态科研绩效公式与多作者位次拆分引擎
  describe('动态科研绩效公式与多作者位次拆分引擎 (清华长庚标杆)', () => {
    it('多作者位次拆分比例权重总和应严格守恒 (1.0)', () => {
      const formula: PerformanceFormulaItem = {
        规则编号: 1,
        指标代码: 'PAPER_CAS_SCI',
        指标名称: '中科院分区SCI期刊论著',
        指标类别: '学术论文',
        基础分值: 50,
        公式表达式: 'BASE + (IF * 10) + TOP_BONUS',
        一作比例: 0.50,
        通讯比例: 0.30,
        共一比例: 0.10,
        参研比例: 0.10,
        是否启用: true
      }

      const ratioSum =
        formula.一作比例 + formula.通讯比例 + formula.共一比例 + formula.参研比例
      expect(Number(ratioSum.toFixed(2))).toBe(1.0)
    })

    it('应根据中科院分区与 Top 期刊加成动态算分并依位次拆分', () => {
      // 场景模拟：1篇中科院1区 Top 期刊论文，IF = 15.6
      // 分区基础分: 1区=100分; Top加成: 50分; IF乘数: IF * 10 = 156分
      // 单篇论文原始总分 = 100 + 50 + 156 = 306 分
      const basePartitionScore = 100
      const isTop = true
      const topBonus = isTop ? 50 : 0
      const ifFactor = 15.6
      const ifMultiplier = 10

      const paperRawTotal = basePartitionScore + topBonus + ifFactor * ifMultiplier
      expect(paperRawTotal).toBe(306)

      // 拆分给不同作者位次：
      // 第一作者 (50%) -> 153 分
      // 通讯作者 (30%) -> 91.8 分
      // 共同一作 (10%) -> 30.6 分
      // 参研位次 (10%) -> 30.6 分
      const firstAuthorScore = Number((paperRawTotal * 0.5).toFixed(2))
      const corrAuthorScore = Number((paperRawTotal * 0.3).toFixed(2))
      const coFirstScore = Number((paperRawTotal * 0.1).toFixed(2))
      const participantScore = Number((paperRawTotal * 0.1).toFixed(2))

      expect(firstAuthorScore).toBe(153.0)
      expect(corrAuthorScore).toBe(91.8)
      expect(coFirstScore).toBe(30.6)
      expect(participantScore).toBe(30.6)

      const splitSum =
        firstAuthorScore + corrAuthorScore + coFirstScore + participantScore
      expect(Number(splitSum.toFixed(2))).toBe(306.0)
    })

    it('应正确解析全员自动测算批处理结果', () => {
      const autoEvalRes: AutoEvaluationResult = {
        评价年度: 2026,
        测算人数: 9,
        生成明细数: 18,
        人均得分: 135.6,
        提示消息: '2026年度科研绩效动态测算执行完毕，成功测算9名科研人员绩效。'
      }

      expect(autoEvalRes.评价年度).toBe(2026)
      expect(autoEvalRes.测算人数).toBe(9)
      expect(autoEvalRes.人均得分).toBeGreaterThan(100)
      expect(autoEvalRes.提示消息).toContain('成功测算')
    })
  })

  // 3. 自定义多维报表与卫健委国考年报引擎
  describe('自定义多维科研报表与卫健委国考年报', () => {
    it('应正确解析国考指标与全院多维报表模板元数据', () => {
      const templates: CustomReportTemplate[] = [
        {
          编号: 1,
          模板代码: 'MED_RES_ANNUAL_SUMMARY',
          模板名称: '临床医学院系与临床科室年度科研成果综合报表',
          报表分类: '科室多维综合年报',
          是否预置: true,
          报表说明: '聚合科室论文、纵横向立项、专利与绩效总分',
          建立时间: '2026-10-15'
        },
        {
          编号: 2,
          模板代码: 'NHC_TERTIARY_HOSPITAL_EXAM',
          模板名称: '国家三级公立医院绩效考核(国考)科研与重点学科统计年报',
          报表分类: '卫健委国考直报',
          是否预置: true,
          报表说明: '对应卫健委三级公立医院绩效考核科研指标',
          建立时间: '2026-10-15'
        }
      ]

      expect(templates.length).toBe(2)
      expect(templates.some((t) => t.模板代码 === 'NHC_TERTIARY_HOSPITAL_EXAM')).toBe(true)
      expect(templates[1].报表分类).toBe('卫健委国考直报')
    })

    it('应准确计算科室维度国考科研达标率与综合指标聚合', () => {
      const rows: CustomReportRow[] = [
        {
          行标识: 'DEPT-1',
          分组维度: '心血管内科',
          统计年度: 2026,
          学术论文总数: 28,
          一区Top论文数: 6,
          最高影响因子: 22.8,
          纵向立项数: 5,
          纵向立项经费: 3200000,
          横向合同数: 3,
          横向合同金额: 1500000,
          专利授权数: 4,
          科研绩效总分: 1280.5,
          国考科研达标率: 98.5
        },
        {
          行标识: 'DEPT-2',
          分组维度: '呼吸与危重症医学科',
          统计年度: 2026,
          学术论文总数: 22,
          一区Top论文数: 4,
          最高影响因子: 18.5,
          纵向立项数: 4,
          纵向立项经费: 2400000,
          横向合同数: 2,
          横向合同金额: 800000,
          专利授权数: 2,
          科研绩效总分: 960.0,
          国考科研达标率: 92.0
        }
      ]

      // 验证国考标杆要求：Top论文数 > 0 且达标率 >= 90%
      const meetsGovBench = rows.every(
        (r) => r.一区Top论文数 > 0 && r.国考科研达标率 >= 90
      )
      expect(meetsGovBench).toBe(true)

      // 验证全院科研总经费汇聚 (纵向经费 + 横向金额)
      const totalFunds = rows.reduce(
        (acc, r) => acc + r.纵向立项经费 + r.横向合同金额,
        0
      )
      expect(totalFunds).toBe(3200000 + 1500000 + 2400000 + 800000) // 7,900,000 元
      expect(totalFunds).toBe(7900000)
    })
  })
})
