import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { formatFileSize, formatDate } from '@/lib/formatters'
import { FileIcon } from '@/components/shared/FileIcon'
import { Copy, Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useState } from 'react'
import { toast } from 'sonner'
import type { DocumentDto } from '@/types/Document'

interface DocumentInfoProps {
  document: DocumentDto
}

/**
 * Document Information Card Component
 * Follows Single Responsibility - displays document metadata only
 */
export const DocumentInfo = ({ document }: DocumentInfoProps) => {
  const { metadata } = document
  const [copied, setCopied] = useState(false)

  const copyDocumentId = () => {
    navigator.clipboard.writeText(document.id)
    setCopied(true)
    toast.success('Document ID copied to clipboard')
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <FileIcon contentType={metadata.contentType} className="w-5 h-5" />
          File Information
        </CardTitle>
      </CardHeader>
      <CardContent>
        <dl className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="md:col-span-2">
            <dt className="text-sm font-medium text-muted-foreground">Document ID</dt>
            <dd className="mt-1 flex items-center gap-2">
              <code className="text-xs bg-muted px-2 py-1 rounded font-mono">{document.id}</code>
              <Button
                variant="ghost"
                size="sm"
                onClick={copyDocumentId}
                className="h-7 w-7 p-0"
              >
                {copied ? <Check className="w-3 h-3" /> : <Copy className="w-3 h-3" />}
              </Button>
            </dd>
          </div>

          <div>
            <dt className="text-sm font-medium text-muted-foreground">File Size</dt>
            <dd className="mt-1 text-sm font-semibold">{formatFileSize(metadata.fileSize)}</dd>
          </div>

          <div>
            <dt className="text-sm font-medium text-muted-foreground">Content Type</dt>
            <dd className="mt-1 text-sm font-mono text-xs">{metadata.contentType}</dd>
          </div>

          <div>
            <dt className="text-sm font-medium text-muted-foreground">Created</dt>
            <dd className="mt-1 text-sm">{formatDate(metadata.createdAt)}</dd>
          </div>

          {metadata.updatedAt && (
            <div>
              <dt className="text-sm font-medium text-muted-foreground">Last Updated</dt>
              <dd className="mt-1 text-sm">{formatDate(metadata.updatedAt)}</dd>
            </div>
          )}

          {metadata.storagePath && (
            <div className="md:col-span-2">
              <dt className="text-sm font-medium text-muted-foreground">Storage Path</dt>
              <dd className="mt-1 text-sm font-mono text-xs truncate">{metadata.storagePath}</dd>
            </div>
          )}
        </dl>
      </CardContent>
    </Card>
  )
}
