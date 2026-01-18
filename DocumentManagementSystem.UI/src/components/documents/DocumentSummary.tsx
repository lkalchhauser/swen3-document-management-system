import { Sparkles } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

interface DocumentSummaryProps {
  summary?: string
}

/**
 * Document AI Summary Component
 * Follows Single Responsibility - displays AI-generated summary only
 */
export const DocumentSummary = ({ summary }: DocumentSummaryProps) => {
  if (!summary) return null

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Sparkles className="w-5 h-5 text-primary" />
          AI Generated Summary
        </CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-sm leading-relaxed whitespace-pre-wrap">
          {summary}
        </p>
      </CardContent>
    </Card>
  )
}
