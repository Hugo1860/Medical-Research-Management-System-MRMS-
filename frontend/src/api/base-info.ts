import { get, post, upload } from '@/utils/request'

export interface DictionaryItem {
  编号: number
  分类: string
  值: string
  上级编号: number
  级别: number
  排序: number
  备注?: string
}

export interface SubjectItem {
  编号: number
  上级学科编号: number
  名称: string
  代码: string
}

export interface ProjectCategoryItem {
  编号: number
  上级编号: number
  名称: string
  说明?: string
  排序?: number
  建立时间?: string
}

export interface ExpenseTemplateItem {
  编号: number
  模版名称: string
  备注?: string
  建立时间?: string
}

export interface ExpenseTemplateTypeItem {
  编号: number
  所属经费模版编号: number
  项目支出类型: string
  备注?: string
}

export interface ExpenseTemplateContentItem {
  编号: number
  所属经费模版项目支出类型编号: number
  项目支出内容: string
  备注?: string
}

export interface JournalItem {
  编号: number
  期刊名称: string
  主办单位?: string
  issn?: string
  cn?: string
  刊期?: string
  主管单位?: string
  级别?: string
  是否为核心期刊?: boolean
  影响因子?: number
  备注?: string
}

export interface IndustryCodeItem {
  编号: number
  父级编号: number
  代码: string
  名称: string
  级别?: number
  说明?: string
}

export interface ContractBuyerTypeItem {
  编号: number
  父级编号: number
  名称: string
  说明?: string
}

export interface TechnicalFieldItem {
  编号: number
  父级编号: number
  代码?: string
  名称: string
  说明?: string
}

export interface DisciplineItem {
  编号: number
  上级学科编号: number
  代码: string
  名称: string
  级别?: number
  备注?: string
}

export interface RegionItem {
  编号: number
  父级编号: number
  名称: string
  代码?: string
  级别?: number
  说明?: string
}

// 字典管理
export function getDictionaryByCategory(category: string) {
  return get<DictionaryItem[]>('/BaseInfo/获取某类字典', { 分类: category })
}

export function getDictionaryPaging(
  paging: { index: number; size: number },
  filter: { 分类: string; Like值?: string }
) {
  return get<{ total: number; list: DictionaryItem[] }>('/BaseInfo/分页获取某类字典', {
    ...paging,
    ...filter
  })
}

export function createDictionary(data: Partial<DictionaryItem>) {
  return post('/BaseInfo/新增字典', data)
}

export function updateDictionary(data: Partial<DictionaryItem>) {
  return post('/BaseInfo/修改字典', data)
}

export function deleteDictionary(ids: number[]) {
  return post('/BaseInfo/删除字典', ids)
}

// 项目分类管理
export function getProjectCategories(filter?: { 上级编号?: number; Like名称?: string }) {
  return get<ProjectCategoryItem[]>('/BaseInfo/获取项目分类', filter)
}

export function saveProjectCategory(data: Partial<ProjectCategoryItem>) {
  return post('/BaseInfo/增改项目分类', data)
}

export function deleteProjectCategories(ids: number[]) {
  return post('/BaseInfo/删除项目分类', ids)
}

// 经费模版管理
export function getExpenseTemplates(
  paging: { index: number; size: number },
  filter?: { Like模版名称?: string }
) {
  return get<{ total: number; list: ExpenseTemplateItem[] }>('/BaseInfo/获取经费模版', {
    ...paging,
    ...filter
  })
}

export function saveExpenseTemplate(data: Partial<ExpenseTemplateItem>) {
  return post('/BaseInfo/增改经费模版', data)
}

export function deleteExpenseTemplates(ids: number[]) {
  return post('/BaseInfo/删除经费模版', ids)
}

export function getExpenseTemplateTypes(
  paging: { index: number; size: number },
  filter?: { 所属经费模版编号?: number }
) {
  return get<{ total: number; list: ExpenseTemplateTypeItem[] }>(
    '/BaseInfo/获取经费模版项目支出类型列表',
    { ...paging, ...filter }
  )
}

export function getExpenseTemplateTypeDetail(经费模版项目支出类型编号: number) {
  return get<{
    基本信息: ExpenseTemplateTypeItem
    经费模版项目支出内容列表: ExpenseTemplateContentItem[]
    可选支出内容列表?: string[]
  }>('/BaseInfo/获取经费模版项目支出类型详情', { 经费模版项目支出类型编号 })
}

export function saveExpenseTemplateType(data: any) {
  return post('/BaseInfo/增改经费模版项目支出类型', data)
}

export function deleteExpenseTemplateTypes(ids: number[]) {
  return post('/BaseInfo/删除经费模版项目支出类型', ids)
}

// 核心期刊与学术刊物
export function getPagingJournals(
  paging: { index: number; size: number },
  filter?: { Like期刊名称?: string; LikeIssn?: string; Like主办单位?: string }
) {
  return get<{ total: number; list: JournalItem[] }>('/BaseInfo/分页获取期刊', {
    ...paging,
    ...filter
  })
}

export function saveJournal(data: Partial<JournalItem>) {
  return post('/BaseInfo/增改期刊', data)
}

export function deleteJournals(ids: number[]) {
  return post('/BaseInfo/删除期刊', ids)
}

// 批量导入期刊
export function importJournals(file: File) {
  return upload('/BaseInfo/导入期刊', file, 'files')
}

// 国民行业代码
export function getIndustryCodes(filter?: { 父级编号?: number; Like名称?: string; Like代码?: string }) {
  return get<IndustryCodeItem[]>('/BaseInfo/获取国民行业代码', filter)
}

export function addIndustryCode(data: Partial<IndustryCodeItem>) {
  return post('/BaseInfo/新增国民行业代码', data)
}

export function editIndustryCode(data: Partial<IndustryCodeItem>) {
  return post('/BaseInfo/修改国民行业代码', data)
}

export function deleteIndustryCodes(ids: number[]) {
  return post('/BaseInfo/删除国民行业代码', ids)
}

// 合同买方性质
export function getContractBuyerTypes(filter?: { 父级编号?: number; Like名称?: string }) {
  return get<ContractBuyerTypeItem[]>('/BaseInfo/获取合同买方性质', filter)
}

export function addContractBuyerType(data: Partial<ContractBuyerTypeItem>) {
  return post('/BaseInfo/新增合同买方性质', data)
}

export function editContractBuyerType(data: Partial<ContractBuyerTypeItem>) {
  return post('/BaseInfo/修改合同买方性质', data)
}

export function deleteContractBuyerTypes(ids: number[]) {
  return post('/BaseInfo/删除合同买方性质', ids)
}

// 技术领域
export function getTechnicalFields(filter?: { 父级编号?: number; Like名称?: string }) {
  return get<TechnicalFieldItem[]>('/BaseInfo/获取技术领域', filter)
}

export function addTechnicalField(data: Partial<TechnicalFieldItem>) {
  return post('/BaseInfo/新增技术领域', data)
}

export function editTechnicalField(data: Partial<TechnicalFieldItem>) {
  return post('/BaseInfo/修改技术领域', data)
}

export function deleteTechnicalFields(ids: number[]) {
  return post('/BaseInfo/删除技术领域', ids)
}

// 学科规范
export function getDisciplines(filter?: { 上级学科编号?: number; Like名称?: string; Like代码?: string }) {
  return get<DisciplineItem[]>('/BaseInfo/获取学科', filter)
}

export function addDiscipline(data: Partial<DisciplineItem>) {
  return post('/BaseInfo/新增学科', data)
}

export function editDiscipline(data: Partial<DisciplineItem>) {
  return post('/BaseInfo/修改学科', data)
}

export function deleteDisciplines(ids: number[]) {
  return post('/BaseInfo/删除学科', ids)
}

// 行政区域
export function getRegions(filter?: { 父级编号?: number; Like名称?: string }) {
  return get<RegionItem[]>('/BaseInfo/获取区域', filter)
}

export function addRegion(data: Partial<RegionItem>) {
  return post('/BaseInfo/新增区域', data)
}

export function editRegion(data: Partial<RegionItem>) {
  return post('/BaseInfo/修改区域', data)
}

export function deleteRegions(ids: number[]) {
  return post('/BaseInfo/删除区域', ids)
}
