import { z } from 'zod'
import { MAX_FILE_SIZE } from './constants'

export const uploadSchema = z.object({
  file: z.instanceof(File)
    .refine(file => file.size <= MAX_FILE_SIZE, 'File must be less than 50MB')
    .refine(file => {
      const allowedTypes = [
        'application/pdf',
        'image/jpeg',
        'image/png',
        'application/msword',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
        'application/vnd.ms-excel',
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        'application/vnd.ms-powerpoint',
        'application/vnd.openxmlformats-officedocument.presentationml.presentation',
      ]
      return allowedTypes.includes(file.type)
    }, 'File type not supported'),
  tags: z.array(z.string()).optional()
})

export const noteSchema = z.object({
  text: z.string()
    .min(1, 'Note cannot be empty')
    .max(500, 'Note must be less than 500 characters')
})
