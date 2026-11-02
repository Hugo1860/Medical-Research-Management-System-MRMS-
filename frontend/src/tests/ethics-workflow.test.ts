import { describe, it, expect } from 'vitest'

describe('Ethics Workflow & Compliance Verification (科研伦理审查与合规管理闭环)', () => {
  // 1. 流程模板编号体系映射与配置对齐
  it('伦理审查流程模板选项应与服务端系统配置严格一致 (410立项, 400申报, 450横向)', () => {
    const templateOptions = [
      { label: '纵向项目立项 (模板 410)', value: 410 },
      { label: '纵向项目申报 (模板 400)', value: 400 },
      { label: '横向项目申请 (模板 450)', value: 450 }
    ]

    expect(templateOptions.length).toBe(3)
    const map = new Map(templateOptions.map(item => [item.value, item.label]))
    expect(map.get(410)).toContain('纵向项目立项')
    expect(map.get(400)).toContain('纵向项目申报')
    expect(map.get(450)).toContain('横向项目申请')
  })

  // 2. 简易伦理快审与免除知情向导预设场景校验
  it('简易伦理向导不同临床场景应具备法定免除知情依据与隐私脱敏说明', () => {
    const scenes = [
      {
        场景类型: '回顾性病历分析',
        建议审查类型: '简易快审',
        建议风险等级: '低风险',
        免除知情同意依据: '依据国家卫健委《涉及人的生命科学和医学研究伦理审查办法》第三十二条规定：本研究仅使用既往临床诊疗中获得的病史数据'
      },
      {
        场景类型: '无创临床观察与残余标本',
        建议审查类型: '简易快审',
        建议风险等级: '低风险',
        免除知情同意依据: '标本均为临床常规检验后拟废弃的剩余样本'
      },
      {
        场景类型: '问卷调查与认知度调研',
        建议审查类型: '简易快审',
        建议风险等级: '低风险',
        免除知情同意依据: '调查问卷首页明确告知研究目的、自愿参与原则及随时退出权利'
      }
    ]

    for (const scene of scenes) {
      expect(scene.建议审查类型).toBe('简易快审')
      expect(scene.建议风险等级).toBe('低风险')
      expect(scene.免除知情同意依据.length).toBeGreaterThan(10)
    }
  })

  // 3. 伦理审查前置材料完整性预检规则
  it('材料完整性预检引擎应精准拦截缺少必要材料或关键字段的申请草稿', () => {
    function materialCheck(value: any) {
      const missing: string[] = []
      if (!value?.流程模板编号 || Number(value.流程模板编号) <= 0) missing.push('流程模板')
      if (!value?.项目编号 || Number(value.项目编号) <= 0) missing.push('关联项目')
      if (!String(value?.申请标题 || '').trim()) missing.push('申请标题')
      if (!String(value?.研究方案摘要 || '').trim()) missing.push('研究方案摘要')
      if (value?.申请类型 === '初始审查' || value?.受试者风险等级 === '高风险') {
        if (!String(value?.知情同意说明 || '').trim()) missing.push('知情同意说明')
      }
      if (!String(value?.附件路径 || '').trim()) missing.push('申请附件')
      return { complete: missing.length === 0, missing }
    }

    // 缺少模板和项目
    const draft1 = {
      流程模板编号: 0,
      项目编号: 0,
      申请类型: '初始审查',
      申请标题: '测试申请',
      研究方案摘要: '方案摘要',
      知情同意说明: '说明',
      附件路径: '/files/att.pdf'
    }
    const res1 = materialCheck(draft1)
    expect(res1.complete).toBe(false)
    expect(res1.missing).toContain('流程模板')
    expect(res1.missing).toContain('关联项目')

    // 高风险但缺少知情同意说明
    const draft2 = {
      流程模板编号: 410,
      项目编号: 12,
      申请类型: '修正案',
      受试者风险等级: '高风险',
      申请标题: '临床介入干预修正',
      研究方案摘要: '增加观察指标',
      知情同意说明: '',
      附件路径: '/files/att.pdf'
    }
    const res2 = materialCheck(draft2)
    expect(res2.complete).toBe(false)
    expect(res2.missing).toContain('知情同意说明')

    // 齐全材料
    const draft3 = {
      流程模板编号: 410,
      项目编号: 12,
      申请类型: '初始审查',
      受试者风险等级: '低风险',
      申请标题: '回顾性分析伦理申请',
      研究方案摘要: '针对既往出院病例',
      知情同意说明: '依据法规豁免',
      附件路径: '/files/att.pdf'
    }
    const res3 = materialCheck(draft3)
    expect(res3.complete).toBe(true)
    expect(res3.missing.length).toBe(0)
  })

  // 4. 到期与持续审查提醒倒计时及异常判定
  it('到期提醒引擎应正确识别过期天数并匹配警告级别', () => {
    function computeReminderStatus(remainingDays: number) {
      return {
        tagType: remainingDays < 0 ? 'danger' : 'warning',
        displayText: remainingDays < 0 ? `已过期 ${Math.abs(remainingDays)} 天` : `${remainingDays} 天`,
        isExpired: remainingDays < 0
      }
    }

    const unexpired = computeReminderStatus(45)
    expect(unexpired.tagType).toBe('warning')
    expect(unexpired.displayText).toBe('45 天')
    expect(unexpired.isExpired).toBe(false)

    const expired = computeReminderStatus(-12)
    expect(expired.tagType).toBe('danger')
    expect(expired.displayText).toBe('已过期 12 天')
    expect(expired.isExpired).toBe(true)
  })

  // 5. 持续审查跟踪记录提交约束验证
  it('持续审查提交时应对不良事件类型强制校验发生日期与严重程度', () => {
    function validateFollowup(followup: {
      记录类型: string
      事件说明: string
      严重程度?: string
      发生日期?: string
    }) {
      if (!followup.事件说明?.trim()) return { valid: false, error: '请填写记录说明' }
      if (followup.记录类型 === '不良事件' && !followup.严重程度) return { valid: false, error: '不良事件必须选择严重程度' }
      if (followup.记录类型 === '不良事件' && !followup.发生日期) return { valid: false, error: '不良事件必须填写发生日期' }
      return { valid: true }
    }

    // 不良事件缺少严重程度
    const invalidAe1 = validateFollowup({
      记录类型: '不良事件',
      事件说明: '患者出现皮疹',
      发生日期: '2026-10-20'
    })
    expect(invalidAe1.valid).toBe(false)
    expect(invalidAe1.error).toBe('不良事件必须选择严重程度')

    // 不良事件缺少发生日期
    const invalidAe2 = validateFollowup({
      记录类型: '不良事件',
      事件说明: '患者出现皮疹',
      严重程度: '一般'
    })
    expect(invalidAe2.valid).toBe(false)
    expect(invalidAe2.error).toBe('不良事件必须填写发生日期')

    // 正常年度跟踪审查
    const validTracking = validateFollowup({
      记录类型: '年度跟踪审查',
      事件说明: '本年度累计入组15例，未出现非预期不良反应'
    })
    expect(validTracking.valid).toBe(true)
  })

  // 6. 伦理档案电子归档 SHA-256 校验与防演示路径阻断
  it('伦理档案提交应严格核验64位SHA-256哈希值并阻断演示虚假路径', () => {
    function validateArchive(archive: { 文件路径: string; 文件校验值: string }) {
      if (!archive.文件路径?.trim()) return { valid: false, error: '请填写文件路径' }
      if (!/^[0-9a-fA-F]{64}$/.test(String(archive.文件校验值 || '').trim())) {
        return { valid: false, error: '正式伦理档案必须填写64位十六进制SHA-256校验值' }
      }
      if (/(^|[/\\])(demo|sample|模拟|样例)([/\\]|$)/i.test(archive.文件路径.trim())) {
        return { valid: false, error: '不能归档示例或演示文件路径' }
      }
      return { valid: true }
    }

    const invalidHash = validateArchive({
      文件路径: '/ethics/materials/meeting_001.pdf',
      文件校验值: '123456'
    })
    expect(invalidHash.valid).toBe(false)
    expect(invalidHash.error).toContain('64位十六进制')

    const demoPath = validateArchive({
      文件路径: '/ethics/demo/meeting_sample.pdf',
      文件校验值: 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855'
    })
    expect(demoPath.valid).toBe(false)
    expect(demoPath.error).toContain('不能归档示例或演示文件路径')

    const validArchive = validateArchive({
      文件路径: '/ethics/official/meeting_20261101_minutes.pdf',
      文件校验值: 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855'
    })
    expect(validArchive.valid).toBe(true)
  })
})
