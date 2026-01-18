import { useState, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { ArrowUpDown, Eye, Download, Trash2, MoreVertical } from 'lucide-react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { FileIcon, getFileTypeLabel } from '@/components/shared/FileIcon'
import { formatFileSize, formatDate } from '@/lib/formatters'
import type { DocumentDto } from '@/types/Document'

interface DocumentTableProps {
  documents: DocumentDto[]
  onDelete: (id: string) => void
  onPreview?: (document: DocumentDto) => void
}

type SortBy = 'name' | 'date' | 'size'
type SortOrder = 'asc' | 'desc'

export const DocumentTable = ({ documents, onDelete, onPreview }: DocumentTableProps) => {
  const navigate = useNavigate()
  const [sortBy, setSortBy] = useState<SortBy>('date')
  const [sortOrder, setSortOrder] = useState<SortOrder>('desc')

  const handleSort = (column: SortBy) => {
    if (sortBy === column) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')
    } else {
      setSortBy(column)
      setSortOrder('desc')
    }
  }

  const sortedDocuments = useMemo(() => {
    return [...documents].sort((a, b) => {
      let comparison = 0

      switch (sortBy) {
        case 'name':
          comparison = a.fileName.localeCompare(b.fileName)
          break
        case 'date':
          comparison = new Date(a.metadata.createdAt).getTime() - new Date(b.metadata.createdAt).getTime()
          break
        case 'size':
          comparison = a.metadata.fileSize - b.metadata.fileSize
          break
      }

      return sortOrder === 'asc' ? comparison : -comparison
    })
  }, [documents, sortBy, sortOrder])

  const handleDelete = (e: React.MouseEvent, document: DocumentDto) => {
    e.stopPropagation()
    if (window.confirm(`Are you sure you want to delete "${document.fileName}"?`)) {
      onDelete(document.id)
    }
  }

  const handlePreview = (e: React.MouseEvent, document: DocumentDto) => {
    e.stopPropagation()
    onPreview?.(document)
  }

  const SortButton = ({ column, label }: { column: SortBy, label: string }) => (
    <Button
      variant="ghost"
      onClick={() => handleSort(column)}
      className="h-auto p-0 hover:bg-transparent"
    >
      {label}
      <ArrowUpDown className={`ml-2 h-4 w-4 ${sortBy === column ? 'text-primary' : 'text-muted-foreground'}`} />
    </Button>
  )

  return (
    <div className="rounded-md border">
      <Table>
        <TableHeader>
          <TableRow className="bg-muted/50">
            <TableHead className="w-[50px]">Type</TableHead>
            <TableHead>
              <SortButton column="name" label="Name" />
            </TableHead>
            <TableHead>Tags</TableHead>
            <TableHead className="w-[120px]">
              <SortButton column="size" label="Size" />
            </TableHead>
            <TableHead className="w-[180px]">
              <SortButton column="date" label="Created" />
            </TableHead>
            <TableHead className="w-[80px] text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {sortedDocuments.map((doc) => (
            <TableRow
              key={doc.id}
              className="cursor-pointer hover:bg-muted/50"
              onClick={() => navigate(`/document/${doc.id}`)}
            >
              <TableCell>
                <div className="flex items-center justify-center">
                  <FileIcon contentType={doc.metadata.contentType} className="w-5 h-5 text-muted-foreground" />
                </div>
              </TableCell>
              <TableCell className="font-medium">
                <div className="flex flex-col">
                  <span className="truncate max-w-md">{doc.fileName}</span>
                  <span className="text-xs text-muted-foreground">
                    {getFileTypeLabel(doc.metadata.contentType, doc.fileName)}
                  </span>
                </div>
              </TableCell>
              <TableCell>
                <div className="flex flex-wrap gap-1">
                  {doc.tags.slice(0, 3).map((tag, index) => (
                    <Badge key={index} variant="secondary" className="text-xs">
                      {tag}
                    </Badge>
                  ))}
                  {doc.tags.length > 3 && (
                    <Badge variant="outline" className="text-xs">
                      +{doc.tags.length - 3}
                    </Badge>
                  )}
                </div>
              </TableCell>
              <TableCell className="text-muted-foreground">
                {formatFileSize(doc.metadata.fileSize)}
              </TableCell>
              <TableCell className="text-muted-foreground">
                {formatDate(doc.metadata.createdAt)}
              </TableCell>
              <TableCell className="text-right">
                <DropdownMenu>
                  <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                    <Button variant="ghost" size="icon" className="h-8 w-8">
                      <MoreVertical className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    {onPreview && (
                      <DropdownMenuItem onClick={(e) => handlePreview(e, doc)}>
                        <Eye className="mr-2 h-4 w-4" />
                        Preview
                      </DropdownMenuItem>
                    )}
                    <DropdownMenuItem onClick={(e) => {
                      e.stopPropagation()
                      navigate(`/document/${doc.id}`)
                    }}>
                      <Download className="mr-2 h-4 w-4" />
                      View Details
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem
                      onClick={(e) => handleDelete(e, doc)}
                      className="text-destructive focus:text-destructive"
                    >
                      <Trash2 className="mr-2 h-4 w-4" />
                      Delete
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}
