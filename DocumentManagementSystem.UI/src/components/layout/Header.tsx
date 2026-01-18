import type { ReactNode } from 'react'

interface HeaderProps {
  children: ReactNode
}

export const Header = ({ children }: HeaderProps) => {
  return (
    <header className="bg-gradient-to-r from-amber-800 to-amber-700 text-white shadow-md mb-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 p-6">
        {children}
      </div>
    </header>
  )
}

interface HeaderSectionProps {
  children: ReactNode
  className?: string
}

export const HeaderSection = ({ children, className = '' }: HeaderSectionProps) => {
  return (
    <div className={`flex items-center gap-4 ${className}`}>
      {children}
    </div>
  )
}
