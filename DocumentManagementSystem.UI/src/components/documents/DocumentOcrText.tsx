import { FileText } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { ScrollArea } from '@/components/ui/scroll-area'

interface DocumentOcrTextProps {
  ocrText?: string
}

/**
 * Document OCR Text Component
 * Follows Single Responsibility - displays OCR extracted text only
 */
export const DocumentOcrText = ({ ocrText }: DocumentOcrTextProps) => {
  if (!ocrText) return null

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <FileText className="w-5 h-5" />
          Extracted Text (OCR)
        </CardTitle>
      </CardHeader>
      <CardContent>
        <ScrollArea className="h-[300px] w-full rounded-md border p-4">
          <pre className="text-sm leading-relaxed whitespace-pre-wrap font-mono">
            {ocrText}
          </pre>
        </ScrollArea>
      </CardContent>
    </Card>
  )
}
