import { Tag } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'

interface DocumentTagsProps {
  tags: string[]
}

/**
 * Document Tags Component
 * Follows Single Responsibility - displays document tags only
 */

// Generate brown colors for tags based on tag name
const getTagColor = (tag: string) => {
  const colors = [
    'bg-amber-700 text-white',
    'bg-amber-800 text-white',
    'bg-orange-700 text-white',
    'bg-orange-800 text-white',
    'bg-yellow-700 text-white',
    'bg-yellow-800 text-white',
    'bg-stone-700 text-white',
    'bg-stone-800 text-white',
    'bg-amber-900 text-white',
    'bg-orange-900 text-white',
  ]

  // Use tag string to generate consistent color per tag
  const hash = tag.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0)
  return colors[hash % colors.length]
}

export const DocumentTags = ({ tags }: DocumentTagsProps) => {
  if (tags.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Tag className="w-5 h-5" />
            Tags
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground italic">No tags</p>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Tag className="w-5 h-5" />
          Tags
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="flex flex-wrap gap-2">
          {tags.map((tag, index) => (
            <Badge key={index} className={`text-sm border-0 ${getTagColor(tag)}`}>
              {tag}
            </Badge>
          ))}
        </div>
      </CardContent>
    </Card>
  )
}
