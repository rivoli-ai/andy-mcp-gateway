import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MarkdownModule } from 'ngx-markdown';
import { McpAdapterService } from '../../../core/services/mcp-adapter.service';
import { McpAdapter, AdapterHealth, AdapterType } from '../../../core/models/mcp-adapter.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-adapter-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MarkdownModule,
    LoadingSpinnerComponent,
    StatusBadgeComponent
  ],
  templateUrl: './adapter-details.component.html',
  styleUrl: './adapter-details.component.scss'
})
export class AdapterDetailsComponent implements OnInit {
  adapter: McpAdapter | null = null;
  isLoading = false;
  isCheckingHealth = false;

  constructor(
    private adapterService: McpAdapterService,
    private route: ActivatedRoute
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

  getIntegrationCode(): string {
    if (!this.adapter) return '';
    
    console.log('Adapter type from backend:', this.adapter.type, 'Type:', typeof this.adapter.type);
    const adapterType = this.getAdapterTypeString(this.adapter.type);
    console.log('Converted to:', adapterType);
    
    const apiUrl = 'http://localhost:8080'; // Using user preference for port 8080
    const timeout = this.adapter.timeoutSeconds || 30;
    const disabled = !this.adapter.enabled;
    
    // Determine the endpoint based on the type (always use 'sse' for SSE, 'mcp' for StreamableHttp)
    const endpoint = adapterType === 'sse' ? 'sse' : 'mcp';

    let code = `"${this.adapter.name}": {
  "autoApprove": [],
  "disabled": ${disabled},
  "timeout": ${timeout},
  "type": "${adapterType}",
  "url": "${apiUrl}/adapters/${this.adapter.name}/${endpoint}"`;

    // Add headers if they exist
    if (this.adapter.headers && Object.keys(this.adapter.headers).length > 0) {
      code += `,\n  "headers": {`;
      const headerEntries = Object.entries(this.adapter.headers);
      headerEntries.forEach(([key, value], index) => {
        code += `\n    "${key}": "${value}"`;
        if (index < headerEntries.length - 1) {
          code += ',';
        }
      });
      code += '\n  }';
    }

    code += '\n}';
    return code;
  }

  private getAdapterTypeString(type: AdapterType | string | number): string {
    // Handle if type is already a string from backend
    if (typeof type === 'string') {
      const typeStr = type.toLowerCase();
      if (typeStr === 'sse' || typeStr.includes('sse')) {
        return 'sse';
      }
      if (typeStr === 'streamablehttp' || typeStr.includes('streamable')) {
        return 'streamableHttp';
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