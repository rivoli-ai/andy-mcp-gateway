export interface McpAdapter {
  id: string;
  name: string;
  url: string;
  description?: string;
  timeoutSeconds: number;
  enabled: boolean;
  type: AdapterType | string | number; // Backend may return string, number, or enum
  createdAt: Date;
  updatedAt: Date;
  createdBy?: string;
  updatedBy?: string;
  lastHealthCheck?: Date;
  isHealthy: boolean;
  lastResponseTimeMs?: number;
  lastError?: string;
  status: AdapterStatus | string | number; // Backend may return string, number, or enum
}

export interface CreateMcpAdapter {
  name: string;
  url: string;
  description?: string;
  timeoutSeconds: number;
  enabled: boolean;
  type: AdapterType;
  createdBy?: string;
}

export interface UpdateMcpAdapter {
  name?: string;
  url?: string;
  description?: string;
  timeoutSeconds?: number;
  enabled?: boolean;
  type?: AdapterType;
  updatedBy?: string;
}

export interface AdapterHealth {
  id: string;
  name: string;
  url: string;
  status: AdapterStatus | string | number; // Backend may return string, number, or enum
  lastCheck?: Date;
  responseTimeMs?: number;
  lastError?: string;
}

export interface AdapterList {
  adapters: McpAdapter[];
  total: number;
  healthy: number;
  unhealthy: number;
  disabled: number;
}

export enum AdapterStatus {
  Unknown = 0,
  Healthy = 1,
  Unhealthy = 2,
  Disabled = 3
}

export enum AdapterType {
  StreamableHttp = 1,
  Sse = 2
}

export interface ApiResponse<T> {
  data?: T;
  error?: string;
  success: boolean;
}



