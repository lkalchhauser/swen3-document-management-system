import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import type { DocumentDto } from '../types/Document';
import { documentApi } from '../services/api';

const DocumentDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [document, setDocument] = useState<DocumentDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (id) {
      loadDocument(id);
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

          {document.metadata.summary && (
            <div className="info-section">
              <h2>AI Generated Summary</h2>
              <div className="summary-content">
                <p>{document.metadata.summary}</p>
              </div>
            </div>
          )}

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