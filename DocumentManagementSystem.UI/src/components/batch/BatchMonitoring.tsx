import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { Badge } from '../ui/badge';
import { RefreshCw, FileText, CheckCircle2, XCircle, Clock } from 'lucide-react';
import { Button } from '../ui/button';
import api from '../../services/api';

interface BatchFileInfo {
  fileName: string;
  fileSizeBytes: number;
  lastModified: string;
  status: string;
}

interface BatchProcessingStatus {
  pendingFilesCount: number;
  archivedFilesCount: number;
  errorFilesCount: number;
  pendingFiles: BatchFileInfo[];
  archivedFiles: BatchFileInfo[];
  errorFiles: BatchFileInfo[];
  lastChecked: string;
}

interface DailyAccess {
  date: string;
  accessCount: number;
}

interface DocumentAccessStatistics {
  documentId: string;
  documentName: string;
  totalAccessCount: number;
  lastAccessDate: string;
  dailyAccess: DailyAccess[];
}

export default function BatchMonitoring() {
  const [status, setStatus] = useState<BatchProcessingStatus | null>(null);
  const [statistics, setStatistics] = useState<DocumentAccessStatistics[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const fetchData = async () => {
    try {
      setRefreshing(true);
      const [statusResponse, statsResponse] = await Promise.all([
        api.get('/batchprocessing/status'),
        api.get('/batchprocessing/statistics?top=10')
      ]);
      setStatus(statusResponse.data);
      setStatistics(statsResponse.data);
    } catch (error) {
      console.error('Error fetching batch monitoring data:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, 30000);
    return () => clearInterval(interval);
  }, []);

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(2)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <RefreshCw className="w-8 h-8 animate-spin" />
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Batch Processing Monitor</h1>
          <p className="text-muted-foreground mt-1">
            Monitor XML file processing and access statistics
          </p>
        </div>
        <Button onClick={fetchData} disabled={refreshing} variant="outline">
          <RefreshCw className={`w-4 h-4 mr-2 ${refreshing ? 'animate-spin' : ''}`} />
          Refresh
        </Button>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Pending Files</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{status?.pendingFilesCount || 0}</div>
            <p className="text-xs text-muted-foreground">Waiting for processing</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Archived Files</CardTitle>
            <CheckCircle2 className="h-4 w-4 text-green-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{status?.archivedFilesCount || 0}</div>
            <p className="text-xs text-muted-foreground">Successfully processed</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Error Files</CardTitle>
            <XCircle className="h-4 w-4 text-red-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{status?.errorFilesCount || 0}</div>
            <p className="text-xs text-muted-foreground">Failed to process</p>
          </CardContent>
        </Card>
      </div>

      <Tabs defaultValue="statistics" className="w-full">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="statistics">Access Statistics</TabsTrigger>
          <TabsTrigger value="pending">Pending ({status?.pendingFilesCount || 0})</TabsTrigger>
          <TabsTrigger value="archived">Archived ({status?.archivedFilesCount || 0})</TabsTrigger>
          <TabsTrigger value="errors">Errors ({status?.errorFilesCount || 0})</TabsTrigger>
        </TabsList>

        <TabsContent value="statistics" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Top Accessed Documents</CardTitle>
              <CardDescription>Documents with the most access logs</CardDescription>
            </CardHeader>
            <CardContent>
              {statistics.length === 0 ? (
                <p className="text-muted-foreground text-center py-8">No access statistics available yet</p>
              ) : (
                <div className="space-y-4">
                  {statistics.map((stat) => (
                    <div key={stat.documentId} className="border rounded-lg p-4">
                      <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center gap-2">
                          <FileText className="w-4 h-4" />
                          <span className="font-medium">{stat.documentName}</span>
                        </div>
                        <Badge variant="secondary">{stat.totalAccessCount} accesses</Badge>
                      </div>
                      <div className="text-sm text-muted-foreground">
                        Last accessed: {formatDate(stat.lastAccessDate)}
                      </div>
                      {stat.dailyAccess.length > 0 && (
                        <div className="mt-3 space-y-1">
                          <div className="text-xs font-medium text-muted-foreground">Recent Activity:</div>
                          <div className="flex gap-2 flex-wrap">
                            {stat.dailyAccess.slice(0, 7).map((daily) => (
                              <Badge key={daily.date} variant="outline" className="text-xs">
                                {new Date(daily.date).toLocaleDateString()}: {daily.accessCount}
                              </Badge>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="pending">
          <Card>
            <CardHeader>
              <CardTitle>Pending Files</CardTitle>
              <CardDescription>Files waiting to be processed</CardDescription>
            </CardHeader>
            <CardContent>
              {status?.pendingFiles.length === 0 ? (
                <p className="text-muted-foreground text-center py-8">No pending files</p>
              ) : (
                <FileList files={status?.pendingFiles || []} formatFileSize={formatFileSize} formatDate={formatDate} />
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="archived">
          <Card>
            <CardHeader>
              <CardTitle>Archived Files</CardTitle>
              <CardDescription>Successfully processed files</CardDescription>
            </CardHeader>
            <CardContent>
              {status?.archivedFiles.length === 0 ? (
                <p className="text-muted-foreground text-center py-8">No archived files</p>
              ) : (
                <FileList files={status?.archivedFiles || []} formatFileSize={formatFileSize} formatDate={formatDate} />
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="errors">
          <Card>
            <CardHeader>
              <CardTitle>Error Files</CardTitle>
              <CardDescription>Files that failed to process</CardDescription>
            </CardHeader>
            <CardContent>
              {status?.errorFiles.length === 0 ? (
                <p className="text-muted-foreground text-center py-8">No error files</p>
              ) : (
                <FileList files={status?.errorFiles || []} formatFileSize={formatFileSize} formatDate={formatDate} />
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {status && (
        <p className="text-xs text-muted-foreground text-center">
          Last updated: {formatDate(status.lastChecked)}
        </p>
      )}
    </div>
  );
}

function FileList({ files, formatFileSize, formatDate }: {
  files: BatchFileInfo[];
  formatFileSize: (bytes: number) => string;
  formatDate: (date: string) => string;
}) {
  return (
    <div className="space-y-2">
      {files.map((file) => (
        <div key={file.fileName} className="flex items-center justify-between p-3 border rounded-lg">
          <div className="flex items-center gap-2">
            <FileText className="w-4 h-4" />
            <span className="font-medium">{file.fileName}</span>
          </div>
          <div className="flex items-center gap-4 text-sm text-muted-foreground">
            <span>{formatFileSize(file.fileSizeBytes)}</span>
            <span>{formatDate(file.lastModified)}</span>
          </div>
        </div>
      ))}
    </div>
  );
}
