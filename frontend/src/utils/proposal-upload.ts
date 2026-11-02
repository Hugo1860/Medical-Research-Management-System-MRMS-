export const PROPOSAL_MAX_FILES = 10
export const PROPOSAL_MAX_FILE_BYTES = 20 * 1024 * 1024
export const PROPOSAL_MAX_TOTAL_BYTES = 100 * 1024 * 1024
export const PROPOSAL_UPLOAD_LIMIT_HINT = '每次请选择 1–10 份 PDF，每份不能为空且不能超过 20 MB，文件总计不能超过 100 MB'
export const PROPOSAL_RESTORE_LIMIT_HINT = '补传原件请选择 1 份 PDF，文件不能为空且不能超过 20 MB'

/** Checks file metadata before allocating a multipart request. Content/hash verification stays on the server. */
export function validateProposalUploadFiles(
  files: ReadonlyArray<Pick<File, 'name' | 'size'>>,
  mode: 'batch' | 'restore' = 'batch'
): string | null {
  const maxFiles = mode === 'restore' ? 1 : PROPOSAL_MAX_FILES
  const invalid = files.length < 1 || files.length > maxFiles ||
    files.some(file => !file.name.toLowerCase().endsWith('.pdf') || !Number.isSafeInteger(file.size) || file.size <= 0 || file.size > PROPOSAL_MAX_FILE_BYTES) ||
    files.reduce((total, file) => total + file.size, 0) > PROPOSAL_MAX_TOTAL_BYTES
  return invalid ? (mode === 'restore' ? PROPOSAL_RESTORE_LIMIT_HINT : PROPOSAL_UPLOAD_LIMIT_HINT) : null
}
