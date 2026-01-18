import { Download, X } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { ScrollArea } from '@/components/ui/scroll-area'
import { PDFPreview } from './PDFPreview'
import { ImagePreview } from './ImagePreview'
import { UnsupportedPreview } from './UnsupportedPreview'
import { documentApi } from '@/services/api'
import type { DocumentDto } from '@/types/Document'

interface DocumentPreviewProps {
  document: DocumentDto | null
  open: boolean
  onClose: () => void
}

/**
 * Document Preview Dialog Component
 * Follows Open/Closed Principle - extensible for new file types without modification
 * Implements Strategy Pattern - delegates to specific preview components based on file type
 */
export const DocumentPreview = ({ document, open, onClose }: DocumentPreviewProps) => {
  if (!document) return null

  const previewUrl = documentApi.getDocumentPreviewUrl(document.id)
  const downloadUrl = documentApi.getDocumentDownloadUrl(document.id)

  const handleDownload = () => {
    window.open(downloadUrl, '_blank')
  }

  /**
   * Determines which preview component to render based on content type
   * Follows Strategy Pattern - selects appropriate rendering strategy
   */
  const renderPreview = () => {
    const contentType = document.metadata.contentType.toLowerCase()

    // PDF Preview
    if (contentType.includes('pdf')) {
      return <PDFPreview url={previewUrl} fileName={document.fileName} />
    }

    // Image Preview
    if (contentType.includes('image/')) {
      return <ImagePreview url={previewUrl} alt={document.fileName} />
    }

    // Unsupported - Fallback
    return (
      <UnsupportedPreview
        documentId={document.id}
        fileName={document.fileName}
        contentType={document.metadata.contentType}
      />
    )
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent className="max-w-5xl h-[90vh] p-0 flex flex-col">
        {/* Header - Separation of Concerns */}
        <DialogHeader className="p-6 pb-4 border-b">
          <div className="flex items-center justify-between">
            <div className="flex-1 min-w-0">
              <DialogTitle className="text-xl font-semibold truncate">
                {document.fileName}
              </DialogTitle>
              <p className="text-sm text-muted-foreground mt-1">
                {document.metadata.contentType}
              </p>
            </div>

            <div className="flex items-center gap-2 ml-4">
              <Button
                variant="outline"
                size="sm"
                onClick={handleDownload}
                aria-label="Download document"
              >
                <Download className="mr-2 h-4 w-4" />
                Download
              </Button>

              <Button
                variant="ghost"
                size="icon"
                onClick={onClose}
                aria-label="Close preview"
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </DialogHeader>

        {/* Preview Content - Follows Dependency Inversion Principle */}
        <ScrollArea className="flex-1 p-6">
          {renderPreview()}
        </ScrollArea>
      </DialogContent>
    </Dialog>
  )
}
