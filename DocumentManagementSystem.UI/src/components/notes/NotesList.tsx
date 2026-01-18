import { StickyNote } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { NoteItem } from './NoteItem'
import { AddNoteForm } from './AddNoteForm'
import { useNotes } from '@/hooks/useNotes'

interface NotesListProps {
  documentId: string
}

/**
 * Notes List Component
 * Follows Single Responsibility - manages notes display and creation
 * Uses custom hook for data management (Dependency Inversion)
 */
export const NotesList = ({ documentId }: NotesListProps) => {
  const { notes, loading, addNote } = useNotes(documentId)

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <StickyNote className="w-5 h-5" />
          Notes
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <AddNoteForm onAdd={addNote} />

        <Separator />

        {loading ? (
          <p className="text-sm text-muted-foreground italic">Loading notes...</p>
        ) : notes.length === 0 ? (
          <p className="text-sm text-muted-foreground italic">No notes yet.</p>
        ) : (
          <div className="space-y-3">
            {notes.map(note => (
              <NoteItem key={note.id} note={note} />
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
