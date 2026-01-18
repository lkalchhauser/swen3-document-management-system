import { Calendar } from 'lucide-react'
import { formatDate } from '@/lib/formatters'
import type { NoteDto } from '@/services/api'

interface NoteItemProps {
  note: NoteDto
}

/**
 * Note Item Component
 * Follows Single Responsibility - displays single note only
 */
export const NoteItem = ({ note }: NoteItemProps) => {
  return (
    <div className="p-4 bg-amber-50 rounded-lg border-l-4 border-amber-600 shadow-sm">
      <p className="text-sm leading-relaxed mb-2 whitespace-pre-wrap text-amber-900">
        {note.text}
      </p>

      <div className="flex items-center gap-1 text-xs text-amber-700">
        <Calendar className="h-3 w-3" />
        {formatDate(note.createdAt)}
      </div>
    </div>
  )
}
