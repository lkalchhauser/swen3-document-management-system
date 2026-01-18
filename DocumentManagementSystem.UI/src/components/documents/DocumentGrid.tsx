import { FolderOpen, Upload } from 'lucide-react'
import { DocumentCard } from './DocumentCard'
import { EmptyState } from '@/components/shared/EmptyState'
import { Button } from '@/components/ui/button'
import type { DocumentDto } from '@/types/Document'

interface DocumentGridProps {
  documents: DocumentDto[]
  onDelete: (id: string) => void
  onPreview?: (document: DocumentDto) => void
  onUploadClick?: () => void
}

export const DocumentGrid = ({ documents, onDelete, onPreview, onUploadClick }: DocumentGridProps) => {
  if (documents.length === 0) {
    return (
      <EmptyState
        icon={FolderOpen}
        title="No documents yet"
        description="Upload your first document to get started"
        action={
          onUploadClick && (
            <Button onClick={onUploadClick}>
              <Upload className="mr-2 h-4 w-4" />
              Upload Document
            </Button>
          )
        }
      />
    )
  }

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
      {documents.map((document) => (
        <DocumentCard
          key={document.id}
          document={document}
          onDelete={onDelete}
          onPreview={onPreview}
        />
      ))}
    </div>
  )
}
