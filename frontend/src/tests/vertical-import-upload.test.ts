import { beforeEach, describe, expect, it, vi } from 'vitest'
import { restoreProposalOriginal, uploadProposalFiles } from '@/api/vertical-import'
import { post } from '@/utils/request'
import { PROPOSAL_RESTORE_LIMIT_HINT, PROPOSAL_UPLOAD_LIMIT_HINT, validateProposalUploadFiles } from '@/utils/proposal-upload'

vi.mock('@/utils/request', () => ({ get: vi.fn(), post: vi.fn() }))

const MiB = 1024 * 1024
const pdf = (size: number, name = 'proposal.pdf') => ({ name, size })

describe('proposal file selection limits', () => {
  it('accepts exactly 20 MiB per file and 100 MiB total', () => {
    expect(validateProposalUploadFiles(Array.from({ length: 5 }, () => pdf(20 * MiB)))).toBeNull()
    expect(validateProposalUploadFiles(Array.from({ length: 10 }, () => pdf(10 * MiB)))).toBeNull()
  })

  it('rejects a single byte over the total even when every file is allowed', () => {
    const files = [...Array.from({ length: 5 }, () => pdf(20 * MiB)), pdf(1)]
    expect(validateProposalUploadFiles(files)).toBe(PROPOSAL_UPLOAD_LIMIT_HINT)
  })

  it('rejects one byte over the individual file limit', () => {
    expect(validateProposalUploadFiles([pdf(20 * MiB + 1)])).toBe(PROPOSAL_UPLOAD_LIMIT_HINT)
  })

  it('requires one to ten nonempty PDFs and allows uppercase extensions', () => {
    expect(validateProposalUploadFiles([])).toBe(PROPOSAL_UPLOAD_LIMIT_HINT)
    expect(validateProposalUploadFiles(Array.from({ length: 11 }, () => pdf(1)))).toBe(PROPOSAL_UPLOAD_LIMIT_HINT)
    expect(validateProposalUploadFiles([pdf(0)])).toBe(PROPOSAL_UPLOAD_LIMIT_HINT)
    expect(validateProposalUploadFiles([pdf(1, 'proposal.pdf.exe')])).toBe(PROPOSAL_UPLOAD_LIMIT_HINT)
    expect(validateProposalUploadFiles([pdf(1, 'proposal.PDF')])).toBeNull()
  })

  it('restricts original restoration to a single PDF', () => {
    expect(validateProposalUploadFiles([pdf(20 * MiB)], 'restore')).toBeNull()
    expect(validateProposalUploadFiles([pdf(1), pdf(1)], 'restore')).toBe(PROPOSAL_RESTORE_LIMIT_HINT)
    expect(validateProposalUploadFiles([pdf(20 * MiB + 1)], 'restore')).toBe(PROPOSAL_RESTORE_LIMIT_HINT)
  })
})

describe('proposal upload request contract', () => {
  beforeEach(() => vi.resetAllMocks())

  it('rejects oversized totals before constructing or sending the request', async () => {
    const files = Array.from({ length: 6 }, () => pdf(18 * MiB)) as File[]
    await expect(uploadProposalFiles(4, files)).rejects.toThrow(PROPOSAL_UPLOAD_LIMIT_HINT)
    expect(post).not.toHaveBeenCalled()
  })

  it('submits each batch file under files with the batch ID and upload timeout', async () => {
    vi.mocked(post).mockResolvedValue({ 新增文件数: 1, 重复文件数: 0 })
    const file = new File(['%PDF-test'], 'proposal.pdf', { type: 'application/pdf' })
    await uploadProposalFiles(4, [file])
    const [url, body, config] = vi.mocked(post).mock.calls[0]
    expect(url).toBe('/VerticalProjectImport/上传计划书?批次编号=4')
    expect((body as FormData).getAll('files')).toEqual([file])
    expect(config).toMatchObject({ timeout: 120000, headers: { 'Content-Type': 'multipart/form-data' } })
  })

  it('restores only the selected detail through the dedicated endpoint', async () => {
    vi.mocked(post).mockResolvedValue({ success: true })
    const file = new File(['%PDF-test'], 'original.pdf', { type: 'application/pdf' })
    await restoreProposalOriginal(19, file)
    const [url, body, config] = vi.mocked(post).mock.calls[0]
    expect(url).toBe('/VerticalProjectImport/补传原件?明细编号=19')
    expect((body as FormData).get('file')).toBe(file)
    expect((body as FormData).has('files')).toBe(false)
    expect(config?.timeout).toBe(120000)
    expect(post).toHaveBeenCalledTimes(1)
  })
})
