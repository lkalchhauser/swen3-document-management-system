import axios from "axios";
import type { DocumentDto, DocumentCreateDto } from "../types/Document";

const API_BASE_URL = "/api";

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

export interface NoteDto {
  id: string;
  text: string;
  createdAt: string;
  documentId: string;
}

export const documentApi = {
  // Get all documents
  getAllDocuments: async (): Promise<DocumentDto[]> => {
    const response = await apiClient.get<DocumentDto[]>("/document");
    return response.data;
  },

  // Get document by ID
  getDocumentById: async (id: string): Promise<DocumentDto> => {
    const response = await apiClient.get<DocumentDto>(`/document/${id}`);
    return response.data;
  },

  // Create new document
  createDocument: async (document: DocumentCreateDto): Promise<DocumentDto> => {
    const response = await apiClient.post<DocumentDto>("/document", document);
    return response.data;
  },

  // Update document
  updateDocument: async (
    id: string,
    document: DocumentCreateDto
  ): Promise<DocumentDto> => {
    const response = await apiClient.put<DocumentDto>(
      `/document/${id}`,
      document
    );
    return response.data;
  },

  // Upload document file
  uploadDocument: async (file: File, tags?: string[]): Promise<DocumentDto> => {
    const formData = new FormData();
    formData.append("file", file);

    if (tags && tags.length > 0) {
      formData.append("tags", tags.join(","));
    }

    const response = await apiClient.post<DocumentDto>(
      "/document/upload",
      formData,
      {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      }
    );
    return response.data;
  },

  // Delete document
  deleteDocument: async (id: string): Promise<void> => {
    await apiClient.delete(`/document/${id}`);
  },

  searchDocuments: async (
    query: string,
    mode: "content" | "notes" = "content"
  ): Promise<DocumentDto[]> => {
    const response = await apiClient.get<DocumentDto[]>("/document/search", {
      params: {
        query,
        mode,
      },
    });
    return response.data;
  },

  getNotes: async (documentId: string): Promise<NoteDto[]> => {
    const response = await apiClient.get<NoteDto[]>(
      `/document/${documentId}/notes`
    );
    return response.data;
  },

  addNote: async (documentId: string, text: string): Promise<NoteDto> => {
    const response = await apiClient.post<NoteDto>(
      `/document/${documentId}/notes`,
      { text }
    );
    return response.data;
  },

  /**
   * Get the preview URL for a document.
   * Returns a URL that can be used directly in <img> or <iframe> elements.
   * Follows Single Responsibility Principle - generates URL only.
   * @param id - Document unique identifier
   * @returns Full URL to preview endpoint
   */
  getDocumentPreviewUrl: (id: string): string => {
    return `${API_BASE_URL}/document/${id}/preview`;
  },

  /**
   * Get the download URL for a document.
   * Returns a URL that triggers browser download with proper filename.
   * @param id - Document unique identifier
   * @returns Full URL to download endpoint
   */
  getDocumentDownloadUrl: (id: string): string => {
    return `${API_BASE_URL}/document/${id}/download`;
  },

  /**
   * Download document file as a blob.
   * Useful for programmatic downloads or processing file content.
   * Follows Open/Closed Principle - can be extended without modification.
   * @param id - Document unique identifier
   * @returns Promise resolving to file blob
   */
  downloadDocument: async (id: string): Promise<Blob> => {
    const response = await apiClient.get(`/document/${id}/download`, {
      responseType: 'blob'
    });
    return response.data;
  },
};

export default apiClient;
