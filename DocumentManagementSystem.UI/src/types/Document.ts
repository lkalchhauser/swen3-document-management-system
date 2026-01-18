export interface DocumentDto {
  id: string;
  fileName: string;
  metadata: DocumentMetadataDto;
  tags: string[];
  accessCount: number;
}

export interface DocumentMetadataDto {
  id: string;
  documentId: string;
  updatedAt?: string;
  fileSize: number;
  contentType: string;
  storagePath?: string;
  ocrText?: string;
  summary?: string;
  createdAt: string;
}

export interface DocumentCreateDto {
  fileName: string;
  contentType?: string;
  tags: string[];
}