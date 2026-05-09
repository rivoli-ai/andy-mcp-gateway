import { Component, OnInit, NgZone, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MarkdownModule } from 'ngx-markdown';
import { McpAdapterService } from '../../../core/services/mcp-adapter.service';
import { McpAdapter, AdapterHealth, AdapterType } from '../../../core/models/mcp-adapter.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { IntegrationClientTabIconComponent } from '../../../shared/components/integration-client-tab-icon/integration-client-tab-icon.component';
import { APP_CONFIG, AppConfig } from '../../../core/services/config.service';
import {
  INTEGRATION_CLIENT_OPTIONS,
  IntegrationClientId,
  IntegrationSerde,
  IntegrationSnippetInput,
  buildIntegrationSnippet,
} from '../../../core/utils/mcp-integration-snippets';

@Component({
  selector: 'app-adapter-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MarkdownModule,
    LoadingSpinnerComponent,
    StatusBadgeComponent,
    IntegrationClientTabIconComponent,
  ],
  templateUrl: './adapter-details.component.html',
  styleUrl: './adapter-details.component.scss'
})
export class AdapterDetailsComponent implements OnInit {
  adapter: McpAdapter | null = null;
  isLoading = false;
  isCheckingHealth = false;
  isListingTools = false;
  toolsResult: { success: boolean; error?: string; tools?: any[] } | null = null;

  readonly integrationClientOptions = INTEGRATION_CLIENT_OPTIONS;
  integrationClient: IntegrationClientId = 'opencode';
  integrationSerde: IntegrationSerde = 'json';

  constructor(
    private adapterService: McpAdapterService,
    private route: ActivatedRoute,
    private zone: NgZone,
    @Inject(APP_CONFIG) private readonly appConfig: AppConfig
  ) {}

  ngOnInit(): void {
    const adapterId = this.route.snapshot.paramMap.get('id');
    if (adapterId) {
      this.loadAdapter(adapterId);
    }
  }

  loadAdapter(id: string): void {
    this.isLoading = true;
    this.adapterService.getAdapterById(id).subscribe({
      next: (adapter) => {
        this.adapter = adapter;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading adapter:', error);
        this.isLoading = false;
      }
    });
  }

  listTools(): void {
    if (!this.adapter) {
      return;
    }

    this.isListingTools = true;
    this.toolsResult = null;

    this.adapterService.testConnection(this.adapter).subscribe({
      next: (result) => {
        this.zone.run(() => {
          this.toolsResult = result;
          this.isListingTools = false;
        });
      },
      error: (error) => {
        this.zone.run(() => {
          this.toolsResult = { success: false, error: error.message || 'Failed to load tools' };
          this.isListingTools = false;
        });
      }
    });
  }

  getSchemaProperties(schema: any): Array<{ name: string; type: string; required: boolean }> {
    if (!schema || !schema.properties) {
      return [];
    }

    const properties = schema.properties;
    const required = schema.required || [];

    return Object.keys(properties).map(propName => ({
      name: propName,
      type: properties[propName].type || 'any',
      required: required.includes(propName)
    }));
  }

  checkHealth(): void {
    if (!this.adapter) return;
    
    this.isCheckingHealth = true;
    this.adapterService.checkAdapterHealth(this.adapter.id).subscribe({
      next: (health) => {
        if (this.adapter) {
          this.adapter.lastHealthCheck = health.lastCheck;
          this.adapter.lastResponseTimeMs = health.responseTimeMs;
          this.adapter.isHealthy = health.status === 1; // AdapterStatus.Healthy
          this.adapter.status = health.status;
          this.adapter.lastError = health.lastError;
        }
        this.isCheckingHealth = false;
      },
      error: (error) => {
        console.error('Error checking health:', error);
        this.isCheckingHealth = false;
      }
    });
  }

  deleteAdapter(): void {
    if (!this.adapter) return;
    
    if (confirm(`Are you sure you want to delete the adapter "${this.adapter.name}"?`)) {
      this.adapterService.deleteAdapter(this.adapter.id).subscribe({
        next: () => {
          // Navigate back to adapters list
          window.history.back();
        },
        error: (error) => {
          console.error('Error deleting adapter:', error);
        }
      });
    }
  }

  setIntegrationClient(client: IntegrationClientId): void {
    this.integrationClient = client;
  }

  setIntegrationSerde(serde: IntegrationSerde): void {
    this.integrationSerde = serde;
  }

  getIntegrationCode(): string {
    if (!this.adapter) {
      return '';
    }

    const adapterType = this.getAdapterTypeString(this.adapter.type);
    const label: IntegrationSnippetInput['adapterTypeLabel'] =
      adapterType === 'sse' ? 'sse' : 'streamable-http';

    const input: IntegrationSnippetInput = {
      adapterName: this.adapter.name,
      gatewayBaseUrl: this.appConfig.apiUrl,
      adapterTypeLabel: label,
      timeoutSeconds: this.adapter.timeoutSeconds || 30,
      enabled: !!this.adapter.enabled,
      headers:
        this.adapter.headers && Object.keys(this.adapter.headers).length > 0
          ? { ...this.adapter.headers }
          : undefined,
    };

    return buildIntegrationSnippet(input, this.integrationClient, this.integrationSerde);
  }

  private getAdapterTypeString(type: AdapterType | string | number): string {
    // Handle if type is already a string from backend
    if (typeof type === 'string') {
      const typeStr = type.toLowerCase();
      if (typeStr === 'sse' || typeStr.includes('sse')) {
        return 'sse';
      }
      if (typeStr === 'streamablehttp' || typeStr.includes('streamable')) {
        return 'streamable-http';
      }
      return 'sse';
    }
    
    // Handle enum values
    switch (type) {
      case AdapterType.Sse:
      case 2:
        return 'sse';
      case AdapterType.StreamableHttp:
      case 1:
        return 'streamable-http';
      default:
        return 'sse';
    }
  }

  copyIntegrationCode(): void {
    const code = this.getIntegrationCode();
    navigator.clipboard.writeText(code).then(() => {
      console.log('Integration code copied to clipboard');
      // You can add a toast notification here
    }).catch(err => {
      console.error('Failed to copy code:', err);
    });
  }

  getHeadersCount(headers: { [key: string]: string }): number {
    return Object.keys(headers).length;
  }

  getHeadersArray(headers: { [key: string]: string }): Array<{ key: string; value: string }> {
    return Object.entries(headers).map(([key, value]) => ({ key, value }));
  }
}