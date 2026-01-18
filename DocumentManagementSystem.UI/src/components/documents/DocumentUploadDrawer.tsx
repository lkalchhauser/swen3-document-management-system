import { useState, useRef, type DragEvent, type ChangeEvent } from 'react'
import { Upload, X, Loader2 } from 'lucide-react'
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerFooter,
  DrawerHeader,
  DrawerTitle,
} from '@/components/ui/drawer'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { toast } from 'sonner'

interface DocumentUploadDrawerProps {
  open: boolean
  onClose: () => void
  onUpload: (files: File[]) => Promise<void>
}

export const DocumentUploadDrawer = ({ open, onClose, onUpload }: DocumentUploadDrawerProps) => {
  const [uploading, setUploading] = useState(false)
  const [dragOver, setDragOver] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const handleUpload = async (files: FileList | File[]) => {
    if (files.length === 0) return

    setUploading(true)
    try {
      await onUpload(Array.from(files))
      toast.success(`Successfully uploaded ${files.length} document(s)`)
      onClose()
    } catch (err) {
      toast.error('Failed to upload documents')
    } finally {
      setUploading(false)
    }
  }

  const handleDragOver = (e: DragEvent) => {
    e.preventDefault()
    setDragOver(true)
  }

  const handleDragLeave = (e: DragEvent) => {
    e.preventDefault()
    setDragOver(false)
  }

  const handleDrop = (e: DragEvent) => {
    e.preventDefault()
    setDragOver(false)

    const files = e.dataTransfer.files
    if (files.length > 0) {
      handleUpload(files)
    }
  }

  const handleFileSelect = (e: ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files
    if (files && files.length > 0) {
      handleUpload(files)
    }
  }

  const handleBrowseClick = () => {
    fileInputRef.current?.click()
  }

  return (
    <Drawer open={open} onOpenChange={(isOpen) => !isOpen && onClose()}>
      <DrawerContent>
        <DrawerHeader>
          <DrawerTitle>Upload Documents</DrawerTitle>
          <DrawerDescription>
            Drag and drop files here or click browse to select files
          </DrawerDescription>
        </DrawerHeader>

        <div className="px-4 pb-4">
          <div
            className={cn(
              'border-2 border-dashed rounded-lg p-12 text-center transition-colors',
              dragOver && 'border-primary bg-primary/5',
              !dragOver && 'border-muted-foreground/25',
              uploading && 'opacity-50 pointer-events-none'
            )}
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
          >
            <div className="flex flex-col items-center gap-4">
              {uploading ? (
                <Loader2 className="h-12 w-12 text-primary animate-spin" />
              ) : (
                <div className="p-4 bg-primary/10 rounded-full">
                  <Upload className="h-8 w-8 text-primary" />
                </div>
              )}

              <div className="space-y-2">
                <h3 className="text-lg font-semibold">
                  {uploading ? 'Uploading...' : 'Drag your files here'}
                </h3>
                <p className="text-sm text-muted-foreground">
                  DOC, PDF, XLS, PPT, and image formats, up to 50 MB
                </p>
              </div>

              <input
                ref={fileInputRef}
                type="file"
                multiple
                onChange={handleFileSelect}
                className="hidden"
                accept=".pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.jpg,.jpeg,.png"
                disabled={uploading}
              />

              <Button
                onClick={handleBrowseClick}
                disabled={uploading}
                size="lg"
              >
                {uploading ? 'Uploading...' : 'Browse Files'}
              </Button>
            </div>
          </div>
        </div>

        <DrawerFooter>
          <DrawerClose asChild>
            <Button variant="outline" disabled={uploading}>
              <X className="mr-2 h-4 w-4" />
              Close
            </Button>
          </DrawerClose>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  )
}
