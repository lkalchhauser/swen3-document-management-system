import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import type { DocumentDto } from '../types/Document';
import type { NoteDto } from '../services/api';
import { documentApi } from '../services/api';

const DocumentDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [document, setDocument] = useState<DocumentDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // Notes state
  const [notes, setNotes] = useState<NoteDto[]>([]);
  const [newNote, setNewNote] = useState('');

  useEffect(() => {
    if (id) {
      loadDocument(id);
      loadNotes(id);
    }
  }, [id]);

  const loadDocument = async (docId: string) => {
    try {
      setLoading(true);
      const doc = await documentApi.getDocumentById(docId);
      setDocument(doc);
      setError(null);
    } catch (err) {
      setError('Failed to load document');
      console.error('Error loading document:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadNotes = async (docId: string) => {
    try {
      const data = await documentApi.getNotes(docId);
      setNotes(data);
    } catch (err) {
      console.error('Failed to load notes', err);
    }
  };

  const handleDelete = async () => {
    if (!document || !window.confirm('Are you sure you want to delete this document?')) {
      return;
    }

    try {
      await documentApi.deleteDocument(document.id);
      navigate('/');
    } catch (err) {
      setError('Failed to delete document');
      console.error('Error deleting document:', err);
    }
  };

  const handleAddNote = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!document || !newNote.trim()) return;
    try {
      const note = await documentApi.addNote(document.id, newNote);
      setNotes([note, ...notes]);
      setNewNote('');
    } catch (err) {
      alert('Failed to add note');
    }
  };

  if (loading) return <div className="loading">Loading document...</div>;
  if (error) return <div className="error">Error: {error}</div>;
  if (!document) return <div className="error">Document not found</div>;

  return (
    <div className="document-detail">
      <header className="detail-header">
        <button className="btn-secondary" onClick={() => navigate('/')}>
          ← Back to Dashboard
        </button>
        <div className="actions">
          <button className="btn-primary" onClick={() => navigate(`/document/${document.id}/edit`)}>
            Edit
          </button>
          <button className="btn-danger" onClick={handleDelete}>
            Delete
          </button>
        </div>
      </header>

      <div className="document-content">
        <h1>{document.fileName}</h1>

        <div className="document-info">
          {/* File Info Section */}
          <div className="info-section">
            <h2>File Information</h2>
            <div className="info-grid">
              <div>
                <strong>File Size:</strong> {(document.metadata.fileSize / 1024).toFixed(1)} KB
              </div>
              <div>
                <strong>Content Type:</strong> {document.metadata.contentType}
              </div>
              <div>
                <strong>Created:</strong> {new Date(document.metadata.createdAt).toLocaleDateString()}
              </div>
              {document.metadata.updatedAt && (
                <div>
                  <strong>Updated:</strong> {new Date(document.metadata.updatedAt).toLocaleDateString()}
                </div>
              )}
              {document.metadata.storagePath && (
                <div>
                  <strong>Storage Path:</strong> {document.metadata.storagePath}
                </div>
              )}
            </div>
          </div>

          {/* Tags Section */}
          {document.tags.length > 0 && (
            <div className="info-section">
              <h2>Tags</h2>
              <div className="document-tags">
                {document.tags.map((tag, index) => (
                  <span key={index} className="tag">{tag}</span>
                ))}
              </div>
            </div>
          )}

          {/* AI Summary Section */}
          {document.metadata.summary && (
            <div className="info-section">
              <h2>AI Generated Summary</h2>
              <div className="summary-content">
                <p>{document.metadata.summary}</p>
              </div>
            </div>
          )}

          {/* Notes Section (New Use Case) */}
          <div className="info-section">
            <h2>Notes</h2>
            <form onSubmit={handleAddNote} style={{ marginBottom: '1rem', display: 'flex', gap: '0.5rem' }}>
              <input
                type="text"
                value={newNote}
                onChange={(e) => setNewNote(e.target.value)}
                placeholder="Add a note..."
                style={{ flex: 1, padding: '0.5rem', borderRadius: '4px', border: '1px solid #ddd' }}
              />
              <button type="submit" className="btn-primary" style={{ padding: '0.5rem 1rem' }}>Add</button>
            </form>
            <div className="notes-list">
              {notes.map(note => (
                <div key={note.id} style={{ 
                  padding: '1rem', 
                  background: '#f8f9fa', 
                  color: '#212529', /* FIXED: Explicit dark text color */
                  borderRadius: '4px', 
                  marginBottom: '0.5rem',
                  borderLeft: '4px solid #4f7cff'
                }}>
                  <p style={{ margin: '0 0 0.5rem 0', fontWeight: 500 }}>{note.text}</p>
                  <small style={{ color: '#6c757d' }}>{new Date(note.createdAt).toLocaleString()}</small>
                </div>
              ))}
              {notes.length === 0 && <p style={{ color: '#6c757d', fontStyle: 'italic' }}>No notes yet.</p>}
            </div>
          </div>

          {/* OCR Content Section */}
          {document.metadata.ocrText && (
            <div className="info-section">
              <h2>Extracted Text (OCR)</h2>
              <div className="ocr-content">
                <pre>{document.metadata.ocrText}</pre>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default DocumentDetail;