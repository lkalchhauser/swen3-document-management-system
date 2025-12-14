import React, { useState, useEffect } from "react";
import type { DocumentDto } from "../types/Document";
import { documentApi } from "../services/api";

const DocumentDashboard: React.FC = () => {
  const [documents, setDocuments] = useState<DocumentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showUpload, setShowUpload] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [dragOver, setDragOver] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");

  useEffect(() => {
    loadDocuments();
  }, []);

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setLoading(true);
      if (!searchQuery.trim()) {
        await loadDocuments();
      } else {
        const results = await documentApi.searchDocuments(searchQuery);
        setDocuments(results);
      }
    } catch (err) {
      setError("Search failed");
    } finally {
      setLoading(false);
    }
  };

  const loadDocuments = async () => {
    try {
      setLoading(true);
      const docs = await documentApi.getAllDocuments();
      setDocuments(docs);
      setError(null);
    } catch (err) {
      setError("Failed to load documents");
      console.error("Error loading documents:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteDocument = async (id: string) => {
    if (window.confirm("Are you sure you want to delete this document?")) {
      try {
        await documentApi.deleteDocument(id);
        setDocuments(documents.filter((doc) => doc.id !== id));
      } catch (err) {
        setError("Failed to delete document");
        console.error("Error deleting document:", err);
      }
    }
  };

  const handleFileUpload = async (files: FileList | File[]) => {
    if (files.length === 0) return;

    setUploading(true);
    setError(null);

    try {
      const uploadPromises = Array.from(files).map((file) =>
        documentApi.uploadDocument(file)
      );

      const uploadedDocs = await Promise.all(uploadPromises);

      // Add new documents to the list
      setDocuments((prev) => [...uploadedDocs, ...prev]);
      setShowUpload(false);
    } catch (err) {
      setError("Failed to upload documents");
      console.error("Error uploading documents:", err);
    } finally {
      setUploading(false);
    }
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);

    const files = e.dataTransfer.files;
    if (files.length > 0) {
      handleFileUpload(files);
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (files && files.length > 0) {
      handleFileUpload(files);
    }
  };

  const getFileIcon = (contentType: string) => {
    if (contentType?.includes("pdf")) return "📄";
    if (contentType?.includes("image")) return "🖼️";
    if (contentType?.includes("text")) return "📝";
    if (contentType?.includes("word")) return "📄";
    if (contentType?.includes("excel") || contentType?.includes("sheet"))
      return "📊";
    if (
      contentType?.includes("powerpoint") ||
      contentType?.includes("presentation")
    )
      return "📊";
    return "📄";
  };

  const getFileTypeLabel = (contentType: string, fileName: string) => {
    const ext = fileName.split(".").pop()?.toUpperCase();
    if (contentType?.includes("pdf")) return "PDF";
    if (contentType?.includes("word")) return "DOC";
    if (contentType?.includes("excel") || contentType?.includes("sheet"))
      return "XLS";
    if (
      contentType?.includes("powerpoint") ||
      contentType?.includes("presentation")
    )
      return "PPT";
    return ext || "FILE";
  };

  if (loading) return <div className="loading">Loading documents...</div>;
  if (error) return <div className="error">Error: {error}</div>;

  return (
    <div className="library-container">
      <div className="library-header">
        <div className="library-info">
          <h1 className="library-title">Document Library</h1>
          <span className="library-stats">{documents.length} documents</span>
        </div>
        <button
          className="upload-btn"
          onClick={() => setShowUpload(!showUpload)}
        >
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path
              d="M8 1L8 11M8 1L11 4M8 1L5 4M2 14L14 14"
              stroke="currentColor"
              strokeWidth="1.5"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
          Upload File
        </button>
      </div>

      <div className="search-bar" style={{ marginBottom: "2rem" }}>
        <form onSubmit={handleSearch} style={{ display: "flex", gap: "1rem" }}>
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search documents..."
            style={{
              flex: 1,
              padding: "0.75rem",
              borderRadius: "8px",
              border: "1px solid #e9ecef",
              fontSize: "1rem",
            }}
          />
          <button type="submit" className="btn-primary">
            Search
          </button>
        </form>
      </div>

      <div className="documents-grid">
        {documents.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">📁</div>
            <h3>No documents yet</h3>
            <p>Upload your first document to get started</p>
          </div>
        ) : (
          documents.map((document) => (
            <div
              key={document.id}
              className="document-item"
              onClick={() =>
                (window.location.href = `/document/${document.id}`)
              }
            >
              <div className="document-icon">
                <div className="file-icon">
                  <div className="file-icon-bg">
                    {getFileIcon(document.metadata.contentType)}
                  </div>
                  <span className="file-type">
                    {getFileTypeLabel(
                      document.metadata.contentType,
                      document.fileName
                    )}
                  </span>
                </div>
              </div>
              <div className="document-info">
                <h3 className="document-name">{document.fileName}</h3>
                <p className="document-date">
                  {new Date(document.metadata.createdAt).toLocaleDateString()}
                </p>
              </div>
              <div
                className="document-actions"
                onClick={(e) => e.stopPropagation()}
              >
                <button
                  className="action-btn"
                  onClick={() => handleDeleteDocument(document.id)}
                >
                  <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                    <path
                      d="M6 2H10M2 4H14M12 4V12C12 13.1 11.1 14 10 14H6C4.9 14 4 13.1 4 12V4M6 6V11M10 6V11"
                      stroke="currentColor"
                      strokeWidth="1.5"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    />
                  </svg>
                </button>
              </div>
            </div>
          ))
        )}
      </div>

      {showUpload && (
        <div className="upload-sidebar">
          <div className="upload-header">
            <h2>Upload File</h2>
            <button className="close-btn" onClick={() => setShowUpload(false)}>
              ×
            </button>
          </div>
          <div className="upload-content">
            <div
              className={`upload-zone ${dragOver ? "drag-over" : ""} ${
                uploading ? "uploading" : ""
              }`}
              onDragOver={handleDragOver}
              onDragLeave={handleDragLeave}
              onDrop={handleDrop}
            >
              <div className="upload-icon">
                {uploading ? (
                  <div className="spinner">⟳</div>
                ) : (
                  <svg width="48" height="48" viewBox="0 0 48 48" fill="none">
                    <path
                      d="M24 8L24 32M24 8L30 14M24 8L18 14M8 40L40 40"
                      stroke="currentColor"
                      strokeWidth="2"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    />
                  </svg>
                )}
              </div>
              <h3>{uploading ? "Uploading..." : "Drag your files here"}</h3>
              <p>DOC, PDF, XLS, and PPT formats, up to 50 MB</p>
              <input
                type="file"
                id="file-input"
                multiple
                onChange={handleFileSelect}
                style={{ display: "none" }}
                accept=".pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx"
                disabled={uploading}
              />
              <button
                className="browse-btn"
                onClick={() => document.getElementById("file-input")?.click()}
                disabled={uploading}
              >
                {uploading ? "Uploading..." : "Browse Files"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default DocumentDashboard;
