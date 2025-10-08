import { Observable } from 'rxjs';
import { McpAdapter, CreateMcpAdapter, UpdateMcpAdapter, AdapterHealth, AdapterList, AdapterType } from '../models/mcp-adapter.model';

export interface IApiService {
  get<T>(endpoint: string): Observable<T>;
  post<T>(endpoint: string, data?: any): Observable<T>;
  put<T>(endpoint: string, data?: any): Observable<T>;
  delete<T>(endpoint: string): Observable<T>;
}

export interface IMcpAdapterService {
  getAllAdapters(): Observable<AdapterList>;
  getEnabledAdapters(): Observable<AdapterList>;
  getAdapterById(id: string): Observable<McpAdapter>;
  getAdapterByName(name: string): Observable<McpAdapter>;
  createAdapter(adapter: CreateMcpAdapter): Observable<McpAdapter>;
  updateAdapter(id: string, adapter: UpdateMcpAdapter): Observable<McpAdapter>;
  deleteAdapter(id: string): Observable<void>;
  checkAdapterHealth(id: string): Observable<AdapterHealth>;
  checkAllAdaptersHealth(): Observable<AdapterHealth[]>;
  searchAdapters(name?: string, enabled?: boolean): Observable<AdapterList>;
  reloadMappings(): Observable<{ message: string; success: boolean }>;testConnection(adapter: McpAdapter): Observable<{ success: boolean; error?: string; tools?: any[] }>;
  getAvailableTools(adapter: McpAdapter): Observable<{ tools: any[]; success: boolean; error?: string }>;
}


export interface IThemeService {
  isDarkMode: boolean;
  toggleTheme(): void;
  setTheme(isDark: boolean): void;
}

export interface ILoadingService {
  isLoading: boolean;
  showLoading(): void;
  hideLoading(): void;
}

export interface INotificationService {
  showSuccess(message: string): void;
  showError(message: string): void;
  showWarning(message: string): void;
  showInfo(message: string): void;
}


