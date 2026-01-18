import { useState } from 'react'
import { ZoomIn, ZoomOut, RotateCw } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface ImagePreviewProps {
  url: string
  alt: string
}

/**
 * Image Preview Component
 * Follows Single Responsibility Principle - handles image display with zoom/rotate
 * Implements accessibility best practices with proper alt text
 */
export const ImagePreview = ({ url, alt }: ImagePreviewProps) => {
  const [scale, setScale] = useState<number>(1.0)
  const [rotation, setRotation] = useState<number>(0)
  const [error, setError] = useState<boolean>(false)

  const zoomIn = () => {
    setScale(prev => Math.min(3.0, prev + 0.25))
  }

  const zoomOut = () => {
    setScale(prev => Math.max(0.5, prev - 0.25))
  }

  const rotate = () => {
    setRotation(prev => (prev + 90) % 360)
  }

  const resetTransform = () => {
    setScale(1.0)
    setRotation(0)
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-[600px]">
        <p className="text-destructive">Failed to load image</p>
      </div>
    )
  }

  return (
    <div className="flex flex-col h-full">
      {/* Controls - Separation of Concerns */}
      <div className="flex items-center justify-between mb-4 p-4 bg-muted/50 rounded-lg">
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="icon"
            onClick={zoomOut}
            disabled={scale <= 0.5}
            aria-label="Zoom out"
          >
            <ZoomOut className="h-4 w-4" />
          </Button>

          <span className="text-sm font-medium min-w-[60px] text-center">
            {Math.round(scale * 100)}%
          </span>

          <Button
            variant="outline"
            size="icon"
            onClick={zoomIn}
            disabled={scale >= 3.0}
            aria-label="Zoom in"
          >
            <ZoomIn className="h-4 w-4" />
          </Button>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="icon"
            onClick={rotate}
            aria-label="Rotate image"
          >
            <RotateCw className="h-4 w-4" />
          </Button>

          <Button
            variant="outline"
            size="sm"
            onClick={resetTransform}
            aria-label="Reset view"
          >
            Reset
          </Button>
        </div>
      </div>

      {/* Image Viewer - Following Liskov Substitution (can be swapped with other image viewers) */}
      <div className="flex-1 overflow-auto flex items-center justify-center bg-muted/20 rounded-lg p-4">
        <img
          src={url}
          alt={alt}
          onError={() => setError(true)}
          className="max-w-full max-h-full object-contain transition-transform duration-200"
          style={{
            transform: `scale(${scale}) rotate(${rotation}deg)`,
            transformOrigin: 'center center'
          }}
          loading="lazy"
        />
      </div>
    </div>
  )
}
