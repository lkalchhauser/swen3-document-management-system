import { useState, useEffect } from 'react'
import type { DocumentDto } from '../types/Document'
import { documentApi } from '../services/api'

export const useDocument = (id: string | undefined) => {
  const [document, setDocument] = useState<DocumentDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchDocument = async (docId: string) => {
    try {
      setLoading(true)
      setError(null)
      const doc = await documentApi.getDocumentById(docId)
      setDocument(doc)
    } catch (err) {
      setError('Failed to load document')
      console.error('Error loading document:', err)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (id) {
      fetchDocument(id)
    }
  }, [id])

  const deleteDocument = async () => {
    if (!document) return
    try {
      await documentApi.deleteDocument(document.id)
    } catch (err) {
      setError('Failed to delete document')
      console.error('Error deleting document:', err)
      throw err
    }
  }

  return {
    document,
    loading,
    error,
    refetch: () => id && fetchDocument(id),
    deleteDocument,
  }
}
