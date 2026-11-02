import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getPagedApprovals,
  getPagedMidterms,
  getPagedChanges,
  getPagedCloseouts
} from '../api/vertical-lifecycle'
import { get } from '../utils/request'

vi.mock('../utils/request', () => ({ get: vi.fn(), post: vi.fn() }))

beforeEach(() => { vi.mocked(get).mockReset() })

describe('纵向课题全周期搜索请求参数映射', () => {
  it('把立项台账项目名称和负责人映射到后端接受的字段', async () => {
    vi.mocked(get).mockResolvedValue({ list: [], total: 0 })
    await getPagedApprovals({ index: 2, size: 10 }, { Like项目名称: '课题', Like负责人姓名: '负责人' })
    expect(get).toHaveBeenCalledWith('/YProject/分页获取科研项目立项', {
      Index: 2, Size: 10, Like项目中文名称: '课题', Like负责人姓名: '负责人'
    })
  })

  it('把中检项目名称和负责人映射到后端接受的字段', async () => {
    vi.mocked(get).mockResolvedValue({ list: [], total: 0 })
    await getPagedMidterms({ index: 1, size: 10 }, { Like项目名称: '中检课题', Like负责人姓名: '李四' })
    expect(get).toHaveBeenCalledWith('/YProject/分页获取纵向项目中检', {
      Index: 1, Size: 10, Like项目中文名称: '中检课题', Like负责人姓名: '李四'
    })
  })

  it('把变更项目名称和负责人映射到后端接受的字段', async () => {
    vi.mocked(get).mockResolvedValue({ list: [], total: 0 })
    await getPagedChanges({ index: 1, size: 10 }, { Like项目名称: '变更课题', Like负责人姓名: '王五' })
    expect(get).toHaveBeenCalledWith('/YProject/分页获取纵向项目变更', {
      Index: 1, Size: 10, Like项目中文名称: '变更课题', Like负责人姓名: '王五'
    })
  })

  it('把结题项目名称和负责人映射到后端接受的字段', async () => {
    vi.mocked(get).mockResolvedValue({ list: [], total: 0 })
    await getPagedCloseouts({ index: 1, size: 10 }, { Like项目名称: '结题课题', Like负责人姓名: '赵六' })
    expect(get).toHaveBeenCalledWith('/YProject/分页获取纵向项目结题', {
      Index: 1, Size: 10, Like项目中文名称: '结题课题', Like负责人姓名: '赵六'
    })
  })
})
