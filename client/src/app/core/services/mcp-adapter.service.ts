import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { HttpHeaders, HttpParams } from '@angular/common/http';
import { ApiService } from './api.service';
import { MsalService } from '@azure/msal-angular';
import { IMcpAdapterService } from '../interfaces/api.interface';
import {
  McpAdapter,
  CreateMcpAdapter,
  UpdateMcpAdapter,
  AdapterHealth,
  AdapterList,
  AdapterType
} from '../models/mcp-adapter.model';
import { environment } from '../../../environments/environment';

interface McpResponse<T = any> {
  success: boolean;
  error?: string;
  data?: T;
  tools?: any[];
}

@Injectable({
  providedIn: 'root'
})
export class McpAdapterService implements IMcpAdapterService {
  private readonly endpoint = '/api/adapters';
  private readonly DEFAULT_HEADERS = {
    'Content-Type': 'application/json',
    'Accept': 'application/json, text/event-stream, text/plain',
    'User-Agent': 'mcpfront/3.32.0',
     'X-Accel-Buffering': 'no'
  };

  private msalService = inject(MsalService);

  constructor(private apiService: ApiService) {}

  // Helper method to get authentication token
  private async getAuthToken(): Promise<string> {
    try {
      const account = this.msalService.instance.getActiveAccount();
      if (account) {
        console.log('Acquiring token for account:', account.username);
        console.log('Requesting scopes:', [environment.azureAd.scopes.apiAccess]);
        
        const tokenResponse = await this.msalService.acquireTokenSilent({
          scopes: [environment.azureAd.scopes.apiAccess],
          account: account
        }).toPromise();
        
        if (tokenResponse) {
          console.log('Token acquired successfully');
          console.log('Token scopes:', tokenResponse.scopes);
          console.log('Token audience:', tokenResponse.account?.idTokenClaims?.aud);
          
          // Decode and inspect the token
          if (tokenResponse.accessToken) {
            const decodedToken = this.decodeToken(tokenResponse.accessToken);
            if (decodedToken) {
              console.log('Decoded token scopes:', decodedToken.scp);
              console.log('Decoded token audience:', decodedToken.aud);
              console.log('Decoded token issuer:', decodedToken.iss);
              console.log('Decoded token subject:', decodedToken.sub);
              console.log('Expected audience should be:', environment.azureAd.apiAudience);
              
              // Check if audience matches
              if (decodedToken.aud === environment.azureAd.apiAudience) {
                console.log('✅ Token audience matches expected audience');
              } else {
                console.warn('❌ Token audience does NOT match expected audience');
                console.warn('Token audience:', decodedToken.aud);
                console.warn('Expected audience:', environment.azureAd.apiAudience);
              }
            }
          }
          
          return tokenResponse.accessToken || '';
        }
      } else {
        console.warn('No active account found');
      }
    } catch (error) {
      console.error('Failed to acquire token:', error);
    }
    return '';
  }

  // Helper method to get headers with authentication
  private async getAuthHeaders(): Promise<Record<string, string>> {
    const headers: Record<string, string> = { ...this.DEFAULT_HEADERS };
    const token = await this.getAuthToken();
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
      console.log('Token added to headers');
    } else {
      console.warn('No token available for request');
    }
    return headers;
  }

  // Helper method to check if user is authenticated
  private isAuthenticated(): boolean {
    const account = this.msalService.instance.getActiveAccount();
    return account !== null;
  }

  // Helper method to decode JWT token and inspect its contents
  private decodeToken(token: string): any {
    try {
      const base64Url = token.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
      }).join(''));
      return JSON.parse(jsonPayload);
    } catch (error) {
      console.error('Failed to decode token:', error);
      return null;
    }
  }

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

  // ====== Health ======
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

  // ====== Connection Test ======
  testConnection(adapter: McpAdapter): Observable<{ success: boolean; error?: string; tools?: any[] }> {
    if (adapter.type === AdapterType.StreamableHttp) {
      return this.testHttpAdapter(adapter);
    } else if (adapter.type === AdapterType.Sse) {
      return this.getAvailableTools(adapter).pipe(
        map(res => ({ success: true, tools: res.tools })),
        catchError(err => of({ success: false, error: err.message || 'Connection failed' }))
      );
    }
    return of({ success: false, error: 'Unsupported adapter type' });
  }

  // ====== Streamable HTTP Helpers ======
 // ====== Streamable HTTP Helpers ======
private testHttpAdapter(adapter: McpAdapter): Observable<{ success: boolean; error?: string; tools?: any[] }> {
  return new Observable(observer => {
    this.initializeSession(adapter)
      .then(sessionId => this.fetchTools(adapter, sessionId))
      .then(tools => {
        observer.next({ success: true, tools });
        observer.complete();
      })
      .catch(err => observer.error({ success: false, error: err.message, tools: [] }));
  });
}

private async initializeSession(adapter: McpAdapter): Promise<string> {
  const initRequest = {
    jsonrpc: '2.0',
    method: 'initialize',
    id: 0,
    params: {
      protocolVersion: '2025-03-26',
      capabilities: { experimental: { streaming: true } },
      clientInfo: { name: 'mcpfront', version: '3.32.0' }
    }
  };

  const json = await this.streamHttpRequest(adapter.name, initRequest);
  return json.result?.sessionId || `mcp-session-${Date.now()}`;
}

private async fetchTools(adapter: McpAdapter, sessionId: string): Promise<any[]> {
  const toolsRequest = {
    jsonrpc: '2.0',
    method: 'tools/list',
    id: 1,
    params: { sessionId }
  };

  const json = await this.streamHttpRequest(adapter.name, toolsRequest);
  return json.result?.tools || [];
}

private async streamHttpRequest(adapterName: string, payload: any): Promise<any> {
  const url = `${environment.apiUrl}/adapters/${adapterName}/mcp`;

  // Get headers with authentication token
  const headers = await this.getAuthHeaders();

  const response = await fetch(url, {
    method: 'POST',
    headers,
    body: JSON.stringify(payload)
  });

  if (!response.ok) throw new Error(`HTTP ${response.status}`);
  if (!response.body) throw new Error('No response body');

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { value, done } = await reader.read();
    if (done) break;
    if (value) {
      buffer += decoder.decode(value, { stream: true });

      const lines = buffer.split('\n');
      for (let i = 0; i < lines.length - 1; i++) {
        const line = lines[i].trim();
        if (!line) continue;
        try {
          return JSON.parse(line.startsWith('data:') ? line.slice(5).trim() : line);
        } catch {
          // incomplete JSON, skip
        }
      }
      buffer = lines[lines.length - 1];
    }
  }

  if (buffer.trim()) return JSON.parse(buffer.startsWith('data:') ? buffer.slice(5).trim() : buffer);
  throw new Error('Failed to parse HTTP stream');
}


  // ====== SSE Helpers ======
  getAvailableTools(adapter: McpAdapter): Observable<{ tools: any[]; success: boolean; error?: string }> {
    return new Observable(observer => {
      this.authenticatedSseConnection(adapter, observer);
    });
  }

  private async authenticatedSseConnection(adapter: McpAdapter, observer: any) {
    try {
      // Get authentication headers
      const headers = await this.getAuthHeaders();
      
      // Create authenticated SSE connection using fetch with streaming
      const response = await fetch(`${environment.apiUrl}/adapters/${adapter.name}/sse`, {
        method: 'GET',
        headers: {
          ...headers,
          'Accept': 'text/event-stream',
          'Cache-Control': 'no-cache'
        }
      });

      if (!response.ok) {
        throw new Error(`SSE connection failed: ${response.status}`);
      }

      if (!response.body) {
        throw new Error('No response body for SSE');
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { value, done } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          if (line.trim() === '') continue;
          
          try {
            // Parse SSE data
            let data: any;
            if (line.startsWith('data: ')) {
              data = JSON.parse(line.slice(6));
            } else if (line.startsWith('data:')) {
              data = JSON.parse(line.slice(5));
            } else {
              data = JSON.parse(line);
            }

            console.log('SSE message received:', data);

            if (data.sessionId) {
              try {
                const headers = await this.getAuthHeaders();
                await fetch(`${environment.apiUrl}/adapters/${adapter.name}/messages?sessionId=${data.sessionId}`, {
                  method: 'POST',
                  headers,
                  body: JSON.stringify({ jsonrpc: '2.0', method: 'tools/list', id: 1, params: {} })
                });
              } catch (err: any) {
                console.error('Failed to send authenticated message:', err);
                observer.error({ success: false, tools: [], error: err.message });
                return;
              }
            } else if (data.result?.tools) {
              observer.next({ success: true, tools: data.result.tools });
              observer.complete();
              return;
            } else if (data.done) {
              observer.complete();
              return;
            }
          } catch (parseError) {
            console.error('Failed to parse SSE data:', parseError);
          }
        }
      }

      // Connection ended without completion
      observer.error({ success: false, tools: [], error: 'SSE connection ended unexpectedly' });
      
    } catch (error: any) {
      console.error('SSE connection error:', error);
      observer.error({ success: false, tools: [], error: error.message || 'SSE connection failed' });
    }
  }

  // ====== Helpers ======
  private getEnumName(enumValue: AdapterType | undefined): string | undefined {
    if (enumValue === undefined || enumValue === null) return undefined;
    return AdapterType[enumValue] as unknown as string;
  }
}
