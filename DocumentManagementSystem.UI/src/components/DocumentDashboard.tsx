import { useState } from 'react'
import { LayoutGrid, Table as TableIcon, Upload } from 'lucide-react'
import { AppLayout } from './layout/AppLayout'
import { Header, HeaderSection } from './layout/Header'
import { DocumentSearch } from './documents/DocumentSearch'
import { DocumentGrid } from './documents/DocumentGrid'
import { DocumentTable } from './documents/DocumentTable'
import { DocumentUploadDrawer } from './documents/DocumentUploadDrawer'
import { DocumentPreview } from './documents/DocumentPreview'
import { DocumentGridSkeleton } from '@/components/shared/LoadingState'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { useDocuments } from '@/hooks/useDocuments'
import { useDocumentPreview } from '@/hooks/useDocumentPreview'
import { toast } from 'sonner'

const DocumentDashboard = () => {
  const [viewMode, setViewMode] = useState<'grid' | 'table'>('grid')
  const [showUpload, setShowUpload] = useState(false)
  const { documents, loading, error, searchDocuments, deleteDocument, uploadDocument } = useDocuments()
  const { previewDocument, isOpen, openPreview, closePreview } = useDocumentPreview()

  const handleSearch = async (query: string, mode: 'content' | 'notes') => {
    await searchDocuments(query, mode)
  }

  const handleDelete = async (id: string) => {
    try {
      await deleteDocument(id)
      toast.success('Document deleted successfully')
    } catch (err) {
      toast.error('Failed to delete document')
    }
  }

  const handleUpload = async (files: File[]) => {
    const uploadPromises = files.map(file => uploadDocument(file))
    await Promise.all(uploadPromises)
  }

  if (error) {
    return (
      <AppLayout>
        <div className="flex items-center justify-center min-h-[400px]">
          <div className="text-center space-y-2">
            <p className="text-destructive font-medium">Error loading documents</p>
            <p className="text-sm text-muted-foreground">{error}</p>
          </div>
        </div>
      </AppLayout>
    )
  }

  return (
    <AppLayout>
      <Header>
        <HeaderSection>
          <div>
            <h1 className="text-2xl font-bold text-white">Document Library</h1>
            <Badge className="mt-1 bg-white/20 text-white border-0 backdrop-blur">
              {documents.length} {documents.length === 1 ? 'document' : 'documents'}
            </Badge>
          </div>
        </HeaderSection>

        <HeaderSection className="flex-wrap">
          <Tabs value={viewMode} onValueChange={(value) => setViewMode(value as 'grid' | 'table')}>
            <TabsList className="bg-white/20 backdrop-blur">
              <TabsTrigger value="grid" className="gap-2 data-[state=active]:bg-white data-[state=active]:text-amber-800">
                <LayoutGrid className="h-4 w-4" />
                Grid
              </TabsTrigger>
              <TabsTrigger value="table" className="gap-2 data-[state=active]:bg-white data-[state=active]:text-amber-800">
                <TableIcon className="h-4 w-4" />
                Table
              </TabsTrigger>
            </TabsList>
          </Tabs>

          <Button onClick={() => setShowUpload(true)} className="bg-amber-900 hover:bg-amber-950 text-white border-0 shadow-md">
            <Upload className="mr-2 h-4 w-4" />
            Upload
          </Button>
        </HeaderSection>
      </Header>

      <div className="space-y-6">
        <DocumentSearch onSearch={handleSearch} />

        {loading ? (
          <DocumentGridSkeleton />
        ) : viewMode === 'grid' ? (
          <DocumentGrid
            documents={documents}
            onDelete={handleDelete}
            onPreview={openPreview}
            onUploadClick={() => setShowUpload(true)}
          />
        ) : (
          <DocumentTable
            documents={documents}
            onDelete={handleDelete}
            onPreview={openPreview}
          />
        )}
      </div>

      <DocumentUploadDrawer
        open={showUpload}
        onClose={() => setShowUpload(false)}
        onUpload={handleUpload}
      />

      <DocumentPreview
        document={previewDocument}
        open={isOpen}
        onClose={closePreview}
      />
    </AppLayout>
  )
}

export default DocumentDashboard
