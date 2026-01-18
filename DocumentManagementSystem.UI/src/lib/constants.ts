export const FILE_TYPES = {
  PDF: 'application/pdf',
  JPEG: 'image/jpeg',
  PNG: 'image/png',
  DOC: 'application/msword',
  DOCX: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  XLS: 'application/vnd.ms-excel',
  XLSX: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  PPT: 'application/vnd.ms-powerpoint',
  PPTX: 'application/vnd.openxmlformats-officedocument.presentationml.presentation',
} as const

export const ALLOWED_FILE_TYPES = Object.values(FILE_TYPES)

export const MAX_FILE_SIZE = 50 * 1024 * 1024 // 50MB

export const SEARCH_MODES = {
  CONTENT: 'content',
  NOTES: 'notes',
} as const
