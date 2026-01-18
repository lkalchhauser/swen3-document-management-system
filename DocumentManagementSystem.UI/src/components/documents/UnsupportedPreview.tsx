import { FileX, Download } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { documentApi } from '@/services/api'

interface UnsupportedPreviewProps {
  documentId: string
  fileName: string
  contentType: string
}

/**
 * Unsupported File Type Preview Component
 * Follows Single Responsibility - handles display for unsupported file types
 * Provides helpful fallback with download option
 */
export const UnsupportedPreview = ({
  documentId,
  fileName,
  contentType
}: UnsupportedPreviewProps) => {
  const handleDownload = () => {
    const downloadUrl = documentApi.getDocumentDownloadUrl(documentId)
    window.open(downloadUrl, '_blank')
  }

  return (
    <div className="flex flex-col items-center justify-center h-[600px] text-center p-8">
      <div className="mb-6 p-6 bg-muted rounded-full">
        <FileX className="h-16 w-16 text-muted-foreground" />
      </div>

      <h3 className="text-xl font-semibold mb-2">Preview Not Available</h3>

      <p className="text-sm text-muted-foreground mb-1">
        Preview is not supported for this file type.
      </p>

      <p className="text-xs text-muted-foreground mb-6">
        File type: <span className="font-mono">{contentType}</span>
      </p>

      <Button onClick={handleDownload} size="lg">
        <Download className="mr-2 h-4 w-4" />
        Download {fileName}
      </Button>

      <p className="text-xs text-muted-foreground mt-4">
        Supported preview formats: PDF, JPG, PNG
      </p>
    </div>
  )
}
