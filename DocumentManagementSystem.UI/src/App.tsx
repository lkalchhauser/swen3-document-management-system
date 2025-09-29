import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import DocumentDashboard from './components/DocumentDashboard'
import DocumentDetail from './components/DocumentDetail'
import './App.css'

function App() {
  return (
    <Router>
      <div className="App">
        <Routes>
          <Route path="/" element={<DocumentDashboard />} />
          <Route path="/document/:id" element={<DocumentDetail />} />
        </Routes>
      </div>
    </Router>
  )
}

export default App
