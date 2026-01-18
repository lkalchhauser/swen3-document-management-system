import { useState, useEffect } from 'react'
import type { DocumentDto } from '../types/Document'
import { documentApi } from '../services/api'

export const useDocuments = () => {
  const [documents, setDocuments] = useState<DocumentDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchDocuments = async () => {
    try {
      setLoading(true)
      setError(null)
      const docs = await documentApi.getAllDocuments()
      setDocuments(docs)
    } catch (err) {
      setError('Failed to load documents')
      console.error('Error loading documents:', err)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchDocuments()
  }, [])

  const searchDocuments = async (query: string, mode: 'content' | 'notes' = 'content') => {
    try {
      setLoading(true)
      setError(null)
      if (!query.trim()) {
        await fetchDocuments()
      } else {
        const results = await documentApi.searchDocuments(query, mode)
        setDocuments(results)
      }
    } catch (err) {
      setError('Search failed')
      console.error('Error searching documents:', err)
    } finally {
      setLoading(false)
    }
  }

  const deleteDocument = async (id: string) => {
    try {
      await documentApi.deleteDocument(id)
      setDocuments(documents.filter(doc => doc.id !== id))
    } catch (err) {
      setError('Failed to delete document')
      console.error('Error deleting document:', err)
      throw err
    }
  }

  const uploadDocument = async (file: File) => {
    try {
      const uploadedDoc = await documentApi.uploadDocument(file)
      setDocuments([uploadedDoc, ...documents])
      return uploadedDoc
    } catch (err) {
      setError('Failed to upload document')
      console.error('Error uploading document:', err)
      throw err
    }
  }

  return {
    documents,
    loading,
    error,
    refetch: fetchDocuments,
    searchDocuments,
    deleteDocument,
    uploadDocument,
  }
}
