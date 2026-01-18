import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import { Toaster } from 'sonner'
import { ErrorBoundary } from './components/shared/ErrorBoundary'
import { MainLayout } from './components/layout/MainLayout'
import DocumentDashboard from './components/DocumentDashboard'
import DocumentDetail from './components/DocumentDetail'
import BatchMonitoring from './components/batch/BatchMonitoring'

function App() {
  return (
    <ErrorBoundary>
      <Router>
        <MainLayout>
          <Routes>
            <Route path="/" element={<DocumentDashboard />} />
            <Route path="/document/:id" element={<DocumentDetail />} />
            <Route path="/batch-monitoring" element={<BatchMonitoring />} />
          </Routes>
        </MainLayout>
        <Toaster position="top-right" richColors />
      </Router>
    </ErrorBoundary>
  )
}

export default App
