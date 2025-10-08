import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdapterStatus } from '../../../core/models/mcp-adapter.model';


@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="badge" [ngClass]="getStatusClass()">
      {{ getStatusText() }}
    </span>
  `,
  styles: [`
    .badge {
      display: inline-flex;
      align-items: center;
      padding: 0.25rem 0.75rem;
      font-size: 0.75rem;
      font-weight: 500;
      border-radius: 9999px;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      white-space: nowrap;
    }
    
    .badge-success {
      background: #dcfce7;
      color: #166534;
    }
    
    .badge-error {
      background: #fee2e2;
      color: #991b1b;
    }
    
    .badge-warning {
      background: #fef3c7;
      color: #92400e;
    }
    
    .badge-info {
      background: #dbeafe;
      color: #1e40af;
    }
    
    .badge-neutral {
      background: #f1f5f9;
      color: #475569;
    }
    
    :host-context(.dark-theme) .badge-success {
      background: #166534;
      color: #dcfce7;
    }
    
    :host-context(.dark-theme) .badge-error {
      background: #991b1b;
      color: #fee2e2;
    }
    
    :host-context(.dark-theme) .badge-warning {
      background: #92400e;
      color: #fef3c7;
    }
    
    :host-context(.dark-theme) .badge-info {
      background: #1e40af;
      color: #dbeafe;
    }
    
    :host-context(.dark-theme) .badge-neutral {
      background: #475569;
      color: #f1f5f9;
    }
  `]
})
export class StatusBadgeComponent {
  @Input() status: AdapterStatus | string | number = AdapterStatus.Unknown;
  
  AdapterStatus = AdapterStatus ;

  getStatusClass(): string {
    const normalizedStatus = this.normalizeStatus(this.status);
    switch (normalizedStatus) {
      case AdapterStatus.Healthy:
        return 'badge-success';
      case AdapterStatus.Unhealthy:
        return 'badge-error';
      case AdapterStatus.Disabled:
        return 'badge-warning';
      default:
        return 'badge-neutral';
    }
  }

  getStatusText(): string {
    const normalizedStatus = this.normalizeStatus(this.status);
    switch (normalizedStatus) {
      case AdapterStatus.Healthy:
        return 'Healthy';
      case AdapterStatus.Unhealthy:
        return 'Unhealthy';
      case AdapterStatus.Disabled:
        return 'Disabled';
      default:
        return 'Unknown';
    }
  }

  private normalizeStatus(status: AdapterStatus | string | number): AdapterStatus {
    if (typeof status === 'string') {
      // Backend returns string like "unknown", "healthy", "unhealthy", "disabled"
      const statusMap: { [key: string]: AdapterStatus } = {
        'healthy': AdapterStatus.Healthy,
        'unhealthy': AdapterStatus.Unhealthy,
        'disabled': AdapterStatus.Disabled,
        'unknown': AdapterStatus.Unknown
      };
      return statusMap[status.toLowerCase()] || AdapterStatus.Unknown;
    }
    if (typeof status === 'number') {
      return status as AdapterStatus;
    }
    return status;
  }
}