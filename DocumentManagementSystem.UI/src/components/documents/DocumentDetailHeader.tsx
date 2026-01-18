import { useNavigate } from 'react-router-dom'
import { ArrowLeft, Eye, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import type { DocumentDto } from '@/types/Document'

interface DocumentDetailHeaderProps {
  document: DocumentDto
  onDelete: () => void
  onPreview: () => void
}

/**
 * Document Detail Header Component
 * Follows Single Responsibility Principle - handles header navigation and actions only
 */
export const DocumentDetailHeader = ({ document, onDelete, onPreview }: DocumentDetailHeaderProps) => {
  const navigate = useNavigate()

  const handleDelete = () => {
    if (window.confirm(`Are you sure you want to delete "${document.fileName}"?`)) {
      onDelete()
    }
  }

  return (
    <div className="bg-card border-b border-border mb-6">
      <div className="p-6">
        <div className="flex items-center justify-between mb-4">
          <Button
            variant="ghost"
            onClick={() => navigate('/')}
            className="gap-2"
          >
            <ArrowLeft className="h-4 w-4" />
            Back to Library
          </Button>

          <div className="flex gap-2">
            <Button
              variant="outline"
              onClick={onPreview}
              className="gap-2"
            >
              <Eye className="h-4 w-4" />
              Preview
            </Button>

            <Button
              variant="destructive"
              onClick={handleDelete}
              className="gap-2"
            >
              <Trash2 className="h-4 w-4" />
              Delete
            </Button>
          </div>
        </div>

        <h1 className="text-3xl font-bold text-foreground break-words">
          {document.fileName}
        </h1>
      </div>
    </div>
  )
}
