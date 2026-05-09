import { Injectable, Inject } from '@angular/core';
import { from, Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { HttpParams } from '@angular/common/http';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';
import { IMcpAdapterService } from '../interfaces/api.interface';
import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { StreamableHTTPClientTransport } from '@modelcontextprotocol/sdk/client/streamableHttp.js';
import { SSEClientTransport } from '@modelcontextprotocol/sdk/client/sse.js';
import {
  McpAdapter,
  CreateMcpAdapter,
  UpdateMcpAdapter,
  AdapterHealth,
  AdapterList,
  AdapterType
} from '../models/mcp-adapter.model';
import { Transport } from '@modelcontextprotocol/sdk/shared/transport.js';
import { APP_CONFIG, AppConfig } from './config.service';


@Injectable({
  providedIn: 'root'
})
export class McpAdapterService implements IMcpAdapterService {
  private readonly endpoint = '/api/adapters';
  private readonly baseUrl: string;

  constructor(
    private apiService: ApiService,
    private authService: AuthService,
    @Inject(APP_CONFIG) config: AppConfig
  ) {
    this.baseUrl = config.apiUrl;
  }

  client = new Client({
    name: 'mcp-client',
    version: ' 2.0.0'
  });

  // ====== CRUD Operations ======
  getAllAdapters(): Observable<AdapterList> {
    return this.apiService.get<AdapterList>(this.endpoint);
  }

  getEnabledAdapters(): Observable<AdapterList> {
    return this.apiService.get<AdapterList>(`${this.endpoint}/enabled`);
  }

  getAdapterById(id: string): Observable<McpAdapter> {
    return this.apiService.get<McpAdapter>(`${this.endpoint}/${id}`);
  }

  getAdapterByName(name: string): Observable<McpAdapter> {
    return this.apiService.get<McpAdapter>(`${this.endpoint}/name/${name}`);
  }

  createAdapter(adapter: CreateMcpAdapter): Observable<McpAdapter> {
    const data = { ...adapter, type: this.getEnumName(adapter.type) };
    return this.apiService.post<McpAdapter>(this.endpoint, data);
  }

  updateAdapter(id: string, adapter: UpdateMcpAdapter): Observable<McpAdapter> {
    const data = { ...adapter, type: adapter.type ? this.getEnumName(adapter.type) : undefined };
    return this.apiService.put<McpAdapter>(`${this.endpoint}/${id}`, data);
  }

  deleteAdapter(id: string): Observable<void> {
    return this.apiService.delete<void>(`${this.endpoint}/${id}`);
  }

  checkAdapterHealth(id: string): Observable<AdapterHealth> {
    return this.apiService.get<AdapterHealth>(`${this.endpoint}/${id}/health`);
  }

  checkAllAdaptersHealth(): Observable<AdapterHealth[]> {
    return this.apiService.post<AdapterHealth[]>(`${this.endpoint}/health-check`, {});
  }

  searchAdapters(name?: string, enabled?: boolean): Observable<AdapterList> {
    let params = new HttpParams();
    if (name) params = params.set('name', name);
    if (enabled !== undefined) params = params.set('enabled', enabled.toString());
    return this.apiService.get<AdapterList>(`${this.endpoint}/search`, params);
  }

  reloadMappings(): Observable<{ message: string; success: boolean }> {
    return this.apiService.post<{ message: string; success: boolean }>(`${this.endpoint}/reload`, {});
  }

  exportAdaptersToExcel(): Observable<Blob> {
    return this.apiService.getBlob(`${this.endpoint}/export`);
  }

  importAdaptersFromExcel(file: File): Observable<{
    message: string;
    successCount: number;
    failedCount: number;
    validationErrors: string[];
    failedAdapters: any[]
  }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.apiService.postFormData<{
      message: string;
      successCount: number;
      failedCount: number;
      validationErrors: string[];
      failedAdapters: any[]
    }>(`${this.endpoint}/import`, formData);
  }

  testConnection(adapter: McpAdapter): Observable<{ success: boolean; error?: string; tools?: any[] }> {
    const type = this.normalizeAdapterType(adapter.type);
    if (type === AdapterType.StreamableHttp) {
      return from(this.testHttpAdapter(adapter)).pipe(
        catchError(err => of({ success: false, error: err.message || 'Connection failed', tools: [] }))
      );
    }
    if (type === AdapterType.Sse) {
      return from(this.testSseAdapter(adapter)).pipe(
        catchError(err => of({ success: false, error: err.message || 'Connection failed', tools: [] }))
      );
    }
    return of({ success: false, error: 'Unsupported adapter type' });
  }

  /** API returns enum as string (JsonStringEnumConverter); UI may also use numeric enum. */
  private normalizeAdapterType(type: McpAdapter['type']): AdapterType | null {
    if (type === AdapterType.StreamableHttp || type === 1) {
      return AdapterType.StreamableHttp;
    }
    if (type === AdapterType.Sse || type === 2) {
      return AdapterType.Sse;
    }
    if (typeof type === 'string') {
      const t = type.replace(/\s+/g, '').toLowerCase();
      if (t === 'streamablehttp' || t === '1') {
        return AdapterType.StreamableHttp;
      }
      if (t === 'sse' || t === '2') {
        return AdapterType.Sse;
      }
    }
    return null;
  }

  private async testHttpAdapter(adapter: McpAdapter): Promise<{ success: boolean; error?: string; tools?: any }> {
    const url = `${this.baseUrl}/adapters/${adapter.name}/mcp`;
    const transport = new StreamableHTTPClientTransport(new URL(url), {
      requestInit: { headers: this.buildAuthHeaders() }
    });
    return this.getTools(transport);
  }

  private async testSseAdapter(adapter: McpAdapter): Promise<{ success: boolean; error?: string; tools?: any }> {
    const url = `${this.baseUrl}/adapters/${adapter.name}/sse`;
    // The MCP SSE transport has two channels:
    //   - GET /sse — held open as the event stream (browsers' native EventSource won't carry
    //     a custom Authorization header; eventSourceInit only forwards `withCredentials`).
    //   - POST /messages — outbound JSON-RPC messages (uses fetch, requestInit.headers works).
    // We attach the gateway JWT to both, but for the GET stream the browser will silently
    // drop it. If your gateway is gating /sse with Authorization-only auth, the request
    // will 401 — accept `?access_token=` query-string fallback on the gateway, or add a
    // server-side cookie for the SSE route.
    const headers = this.buildAuthHeaders();
    const transport = new SSEClientTransport(new URL(url), {
      eventSourceInit: { withCredentials: false } as EventSourceInit,
      requestInit: { headers }
    });
    return this.getTools(transport);
  }

  private async getTools(transport: Transport) {
    await this.client.connect(transport);
    const tools = await this.client.listTools();
    return { success: true, tools: tools.tools };
  }

  /** Returns the gateway-JWT bearer header, or an empty record if the user isn't signed in. */
  private buildAuthHeaders(): Record<string, string> {
    const token = this.authService.getToken();
    return token ? { Authorization: `Bearer ${token}` } : {};
  }

  private getEnumName(enumValue: AdapterType | undefined): string | undefined {
    if (enumValue === undefined || enumValue === null) return undefined;
    return AdapterType[enumValue] as unknown as string;
  }
}
