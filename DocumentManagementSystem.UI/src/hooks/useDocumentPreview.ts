import { useState } from 'react'
import type { DocumentDto } from '../types/Document'

export const useDocumentPreview = () => {
  const [previewDocument, setPreviewDocument] = useState<DocumentDto | null>(null)
  const [isOpen, setIsOpen] = useState(false)

  const openPreview = (document: DocumentDto) => {
    setPreviewDocument(document)
    setIsOpen(true)
  }

  const closePreview = () => {
    setIsOpen(false)
    // Delay clearing the document to allow for close animation
    setTimeout(() => setPreviewDocument(null), 300)
  }

  return {
    previewDocument,
    isOpen,
    openPreview,
    closePreview,
  }
}
