import { useParams, useNavigate } from 'react-router-dom'
import { AppLayout } from './layout/AppLayout'
import { DocumentDetailHeader } from './documents/DocumentDetailHeader'
import { DocumentInfo } from './documents/DocumentInfo'
import { DocumentSummary } from './documents/DocumentSummary'
import { DocumentOcrText } from './documents/DocumentOcrText'
import { DocumentTags } from './documents/DocumentTags'
import { NotesList } from './notes/NotesList'
import { DocumentPreview } from './documents/DocumentPreview'
import { DocumentDetailSkeleton } from '@/components/shared/LoadingState'
import { useDocument } from '@/hooks/useDocument'
import { useDocumentPreview } from '@/hooks/useDocumentPreview'
import { toast } from 'sonner'

/**
 * Document Detail Page Component
 * Follows Single Responsibility - orchestrates detail page components only
 * Implements clean component composition (Open/Closed Principle)
 */
const DocumentDetail = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { document, loading, error, deleteDocument } = useDocument(id)
  const { previewDocument, isOpen, openPreview, closePreview } = useDocumentPreview()

  const handleDelete = async () => {
    try {
      await deleteDocument()
      toast.success('Document deleted successfully')
      navigate('/')
    } catch (err) {
      toast.error('Failed to delete document')
    }
  }

  const handlePreview = () => {
    if (document) {
      openPreview(document)
    }
  }

  // Loading State
  if (loading) {
    return (
      <AppLayout>
        <DocumentDetailSkeleton />
      </AppLayout>
    )
  }

  // Error State
  if (error) {
    return (
      <AppLayout>
        <div className="flex items-center justify-center min-h-[400px]">
          <div className="text-center space-y-2">
            <p className="text-destructive font-medium">Error loading document</p>
            <p className="text-sm text-muted-foreground">{error}</p>
          </div>
        </div>
      </AppLayout>
    )
  }

  // Not Found State
  if (!document) {
    return (
      <AppLayout>
        <div className="flex items-center justify-center min-h-[400px]">
          <div className="text-center space-y-2">
            <p className="text-lg font-medium">Document not found</p>
            <p className="text-sm text-muted-foreground">
              The document you're looking for doesn't exist.
            </p>
          </div>
        </div>
      </AppLayout>
    )
  }

  // Success State - Component Composition following SOLID principles
  return (
    <AppLayout>
      <DocumentDetailHeader
        document={document}
        onDelete={handleDelete}
        onPreview={handlePreview}
      />

      {/* 2-Column Layout - Enterprise Professional Design */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Content (Left Column - 2/3 width on large screens) */}
        <div className="lg:col-span-2 space-y-6">
          <DocumentInfo document={document} />
          <DocumentSummary summary={document.metadata.summary} />
          <DocumentOcrText ocrText={document.metadata.ocrText} />
        </div>

        {/* Sidebar (Right Column - 1/3 width on large screens) */}
        <div className="space-y-6">
          <DocumentTags tags={document.tags} />
          <NotesList documentId={document.id} />
        </div>
      </div>

      {/* Document Preview Modal */}
      <DocumentPreview
        document={previewDocument}
        open={isOpen}
        onClose={closePreview}
      />
    </AppLayout>
  )
}

export default DocumentDetail
