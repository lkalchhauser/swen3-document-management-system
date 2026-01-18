import { useNavigate } from 'react-router-dom'
import { Trash2, Eye } from 'lucide-react'
import { Card, CardContent, CardHeader } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { FileIcon, getFileTypeLabel } from '@/components/shared/FileIcon'
import type { DocumentDto } from '@/types/Document'

interface DocumentCardProps {
  document: DocumentDto
  onDelete: (id: string) => void
  onPreview?: (document: DocumentDto) => void
}

export const DocumentCard = ({ document, onDelete, onPreview }: DocumentCardProps) => {
  const navigate = useNavigate()

  const handleCardClick = () => {
    navigate(`/document/${document.id}`)
  }

  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation()
    if (window.confirm('Are you sure you want to delete this document?')) {
      onDelete(document.id)
    }
  }

  const handlePreview = (e: React.MouseEvent) => {
    e.stopPropagation()
    onPreview?.(document)
  }

  return (
    <Card className="cursor-pointer hover:shadow-lg hover:border-amber-400 transition-all group relative">
      <div className="absolute top-3 right-3 opacity-0 group-hover:opacity-100 transition-opacity flex gap-1 z-10">
        {onPreview && (
          <Button
            variant="secondary"
            size="icon"
            className="h-8 w-8 bg-background/95 backdrop-blur"
            onClick={handlePreview}
            title="Preview document"
          >
            <Eye className="h-4 w-4" />
          </Button>
        )}
        <Button
          variant="destructive"
          size="icon"
          className="h-8 w-8"
          onClick={handleDelete}
          title="Delete document"
        >
          <Trash2 className="h-4 w-4" />
        </Button>
      </div>

      <div onClick={handleCardClick}>
        <CardHeader className="flex flex-col items-center pt-8 pb-4">
          <div className="relative flex flex-col items-center">
            <div className="p-4 bg-gradient-to-br from-amber-700 to-amber-600 rounded-lg shadow-md">
              <FileIcon contentType={document.metadata.contentType} className="w-12 h-12 text-white" />
            </div>
            <Badge className="absolute -bottom-2 px-2 py-0.5 text-xs font-semibold bg-amber-800 text-white border-0">
              {getFileTypeLabel(document.metadata.contentType, document.fileName)}
            </Badge>
          </div>
        </CardHeader>

        <CardContent className="text-center space-y-2">
          <h3 className="font-medium text-sm line-clamp-2 min-h-[2.5rem]">
            {document.fileName}
          </h3>
          <p className="text-xs text-muted-foreground">
            {new Date(document.metadata.createdAt).toLocaleDateString()}
          </p>
        </CardContent>
      </div>
    </Card>
  )
}
