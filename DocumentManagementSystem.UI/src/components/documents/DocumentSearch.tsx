import { useState, type FormEvent } from 'react'
import { Search } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

interface DocumentSearchProps {
  onSearch: (query: string, mode: 'content' | 'notes') => void
}

export const DocumentSearch = ({ onSearch }: DocumentSearchProps) => {
  const [searchQuery, setSearchQuery] = useState('')
  const [searchMode, setSearchMode] = useState<'content' | 'notes'>('content')

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    onSearch(searchQuery, searchMode)
  }

  return (
    <form onSubmit={handleSubmit} className="flex gap-3 w-full max-w-3xl">
      <Select value={searchMode} onValueChange={(value: 'content' | 'notes') => setSearchMode(value)}>
        <SelectTrigger className="w-[180px]">
          <SelectValue placeholder="Search mode" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="content">Search Content</SelectItem>
          <SelectItem value="notes">Search Notes</SelectItem>
        </SelectContent>
      </Select>

      <Input
        type="text"
        value={searchQuery}
        onChange={(e) => setSearchQuery(e.target.value)}
        placeholder={
          searchMode === 'notes'
            ? 'Search inside notes...'
            : 'Search filename, tags, content...'
        }
        className="flex-1"
      />

      <Button type="submit" size="default">
        <Search className="mr-2 h-4 w-4" />
        Search
      </Button>
    </form>
  )
}
