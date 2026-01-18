import { useState, type FormEvent } from 'react'
import { Send } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { toast } from 'sonner'

interface AddNoteFormProps {
  onAdd: (text: string) => Promise<void | unknown>
}

/**
 * Add Note Form Component
 * Follows Single Responsibility - handles note creation form only
 * Implements proper validation and error handling
 */
export const AddNoteForm = ({ onAdd }: AddNoteFormProps) => {
  const [text, setText] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    // Validation
    if (!text.trim()) {
      toast.error('Note cannot be empty')
      return
    }

    if (text.length > 500) {
      toast.error('Note must be less than 500 characters')
      return
    }

    setSubmitting(true)
    try {
      await onAdd(text.trim())
      setText('') // Clear form on success
      toast.success('Note added successfully')
    } catch (err) {
      toast.error('Failed to add note')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-3">
      <Textarea
        value={text}
        onChange={(e) => setText(e.target.value)}
        placeholder="Add a note..."
        className="min-h-[100px] resize-none"
        disabled={submitting}
        maxLength={500}
        aria-label="Note text"
      />

      <div className="flex items-center justify-between">
        <span className="text-xs text-muted-foreground">
          {text.length}/500 characters
        </span>

        <Button
          type="submit"
          disabled={submitting || !text.trim()}
          size="sm"
          className="gap-2"
        >
          <Send className="h-4 w-4" />
          {submitting ? 'Adding...' : 'Add Note'}
        </Button>
      </div>
    </form>
  )
}
