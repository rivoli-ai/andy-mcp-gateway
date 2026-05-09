import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { IntegrationClientId } from '../../../core/utils/mcp-integration-snippets';

@Component({
  selector: 'app-integration-client-tab-icon',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="integration-tab-icon" aria-hidden="true">
      <ng-container [ngSwitch]="clientId">
        <!-- OpenCode: angle code brackets -->
        <svg *ngSwitchCase="'opencode'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <polyline points="16 18 22 12 16 6" />
          <polyline points="8 6 2 12 8 18" />
        </svg>
        <!-- Claude: starburst (stroke, reads at small sizes) -->
        <svg *ngSwitchCase="'claude'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83" />
          <circle cx="12" cy="12" r="2.5" fill="currentColor" stroke="none" />
        </svg>
        <!-- Cline: simple “bot” face -->
        <svg *ngSwitchCase="'cline'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
          <rect x="5" y="7" width="14" height="12" rx="3" />
          <circle cx="9.5" cy="13" r="1.25" fill="currentColor" stroke="none" />
          <circle cx="14.5" cy="13" r="1.25" fill="currentColor" stroke="none" />
          <path d="M12 4v3M9 4h6" />
        </svg>
        <!-- Continue: forward / resume cue -->
        <svg *ngSwitchCase="'continue'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <polygon points="5 4 15 12 5 20 5 4" />
          <line x1="19" y1="5" x2="19" y2="19" />
        </svg>
      </ng-container>
    </span>
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      width: 0.95rem;
      height: 0.95rem;
    }
    .integration-tab-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 100%;
      height: 100%;
    }
    svg {
      width: 100%;
      height: 100%;
      display: block;
    }
  `,
})
export class IntegrationClientTabIconComponent {
  @Input({ required: true }) clientId!: IntegrationClientId;
}
