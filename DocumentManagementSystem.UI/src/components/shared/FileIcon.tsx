import { FileText, FileImage, FileSpreadsheet, Presentation, File } from 'lucide-react'

interface FileIconProps {
  contentType: string
  className?: string
}

export const FileIcon = ({ contentType, className = "w-6 h-6" }: FileIconProps) => {
  const getIcon = () => {
    if (contentType?.includes('pdf') || contentType?.includes('word')) {
      return <FileText className={className} />
    }
    if (contentType?.includes('image')) {
      return <FileImage className={className} />
    }
    if (contentType?.includes('excel') || contentType?.includes('sheet')) {
      return <FileSpreadsheet className={className} />
    }
    if (contentType?.includes('powerpoint') || contentType?.includes('presentation')) {
      return <Presentation className={className} />
    }
    return <File className={className} />
  }

  return getIcon()
}

export const getFileTypeLabel = (contentType: string, fileName: string): string => {
  const ext = fileName.split('.').pop()?.toUpperCase()
  if (contentType?.includes('pdf')) return 'PDF'
  if (contentType?.includes('word')) return 'DOC'
  if (contentType?.includes('excel') || contentType?.includes('sheet')) return 'XLS'
  if (contentType?.includes('powerpoint') || contentType?.includes('presentation')) return 'PPT'
  if (contentType?.includes('image/jpeg')) return 'JPG'
  if (contentType?.includes('image/png')) return 'PNG'
  return ext || 'FILE'
}
