import { useState, useEffect } from 'react'
import type { NoteDto } from '../services/api'
import { documentApi } from '../services/api'

export const useNotes = (documentId: string | undefined) => {
  const [notes, setNotes] = useState<NoteDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchNotes = async (docId: string) => {
    try {
      setLoading(true)
      setError(null)
      const data = await documentApi.getNotes(docId)
      setNotes(data)
    } catch (err) {
      setError('Failed to load notes')
      console.error('Error loading notes:', err)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (documentId) {
      fetchNotes(documentId)
    }
  }, [documentId])

  const addNote = async (text: string) => {
    if (!documentId || !text.trim()) return
    try {
      const note = await documentApi.addNote(documentId, text)
      setNotes([note, ...notes])
      return note
    } catch (err) {
      setError('Failed to add note')
      console.error('Error adding note:', err)
      throw err
    }
  }

  return {
    notes,
    loading,
    error,
    refetch: () => documentId && fetchNotes(documentId),
    addNote,
  }
}
