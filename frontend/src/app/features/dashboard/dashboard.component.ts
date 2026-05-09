import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { McpAdapterService } from '../../core/services/mcp-adapter.service';
import { AdapterList } from '../../core/models/mcp-adapter.model';
import { NotificationService } from '../../shared/components/notification/notification.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="dashboard">
      <!-- Header Section (same control pattern as adapters list) -->
      <div class="dashboard-header">
        <div class="dashboard-header__content">
          <div class="header-text ds-page-heading">
            <div class="ds-icon-chip ds-page-heading__icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <rect x="3" y="3" width="7" height="7" rx="1"/>
                <rect x="14" y="3" width="7" height="7" rx="1"/>
                <rect x="14" y="14" width="7" height="7" rx="1"/>
                <rect x="3" y="14" width="7" height="7" rx="1"/>
              </svg>
            </div>
            <div class="ds-page-heading__text">
              <h1 class="dashboard-title">Dashboard</h1>
              <p class="dashboard-subtitle">Gateway health, counts, and bulk actions</p>
            </div>
          </div>
          <div class="header-actions">
            <div class="header-actions__buttons">
              <button
                class="action-btn secondary"
                (click)="exportAdapters()"
                [disabled]="isLoading || adapterStats.total === 0"
                title="Export all adapters to Excel"
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                  <polyline points="14 2 14 8 20 8"/>
                  <line x1="12" y1="18" x2="12" y2="12"/>
                  <line x1="9" y1="15" x2="12" y2="18"/>
                  <line x1="15" y1="15" x2="12" y2="18"/>
                </svg>
                <span>Export</span>
              </button>
              <button
                class="action-btn secondary"
                (click)="fileInput.click()"
                [disabled]="isLoading"
                title="Import adapters from Excel"
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                  <polyline points="14 2 14 8 20 8"/>
                  <line x1="12" y1="11" x2="12" y2="17"/>
                  <line x1="9" y1="14" x2="12" y2="11"/>
                  <line x1="15" y1="14" x2="12" y2="11"/>
                </svg>
                <span>Import</span>
              </button>
              <input #fileInput type="file" accept=".xlsx" (change)="onFileSelected($event)" style="display: none;" />
              <button
                type="button"
                class="action-btn secondary"
                (click)="checkAllHealth()"
                [disabled]="isLoading || adapterStats.total === 0"
                title="Run health check on all adapters"
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                  <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                  <path d="M22 4L12 14.01l-3-3"/>
                </svg>
                <span>Run health checks</span>
              </button>
              <button class="action-btn primary" routerLink="/adapters/new" title="Create new adapter">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                  <circle cx="12" cy="12" r="10"/>
                  <line x1="12" y1="8" x2="12" y2="16"/>
                  <line x1="8" y1="12" x2="16" y2="12"/>
                </svg>
                <span>New adapter</span>
              </button>
              <button class="repo-icon-btn" (click)="refreshData()" [disabled]="isLoading" title="Refresh" type="button">
                <svg
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  aria-hidden="true"
                  [class.spinning]="isLoading"
                >
                  <polyline points="23 4 23 10 17 10"/>
                  <polyline points="1 20 1 14 7 14"/>
                  <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/>
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Gateway monitor -->
      <section class="monitor" aria-labelledby="monitor-heading">
        <div class="monitor__intro">
          <h2 id="monitor-heading" class="monitor__title">Gateway monitor</h2>
          <p class="monitor__sub">Health mix and adapter counts across your gateway.</p>
        </div>

        <div class="repo-card monitor__hero" *ngIf="adapterStats.total > 0">
          <div class="monitor__hero-inner">
            <div class="monitor__hero-stat">
              <span class="monitor__hero-value">{{ adapterStats.total }}</span>
              <span class="monitor__hero-label">registered adapters</span>
              <span class="monitor__hero-meta">{{ enabledAdapterCount }} enabled</span>
            </div>
            <div class="monitor__distribution">
              <p class="monitor__distribution-title">Status mix</p>
              <div class="monitor__bar" role="img" [attr.aria-label]="healthBarAriaLabel">
                <div
                  class="monitor__bar-seg monitor__bar-seg--healthy"
                  [style.width.%]="healthSegmentPercent('healthy')"
                ></div>
                <div
                  class="monitor__bar-seg monitor__bar-seg--unhealthy"
                  [style.width.%]="healthSegmentPercent('unhealthy')"
                ></div>
                <div
                  class="monitor__bar-seg monitor__bar-seg--disabled"
                  [style.width.%]="healthSegmentPercent('disabled')"
                ></div>
              </div>
              <ul class="monitor__legend">
                <li>
                  <span class="monitor__dot monitor__dot--healthy" aria-hidden="true"></span>
                  <span>Healthy <strong>{{ adapterStats.healthy }}</strong> · {{ getPercentage(adapterStats.healthy, adapterStats.total) }}%</span>
                </li>
                <li>
                  <span class="monitor__dot monitor__dot--unhealthy" aria-hidden="true"></span>
                  <span>Unhealthy <strong>{{ adapterStats.unhealthy }}</strong> · {{ getPercentage(adapterStats.unhealthy, adapterStats.total) }}%</span>
                </li>
                <li>
                  <span class="monitor__dot monitor__dot--disabled" aria-hidden="true"></span>
                  <span>Disabled <strong>{{ adapterStats.disabled }}</strong> · {{ getPercentage(adapterStats.disabled, adapterStats.total) }}%</span>
                </li>
              </ul>
            </div>
          </div>
        </div>

        <div class="repo-card monitor__empty" *ngIf="adapterStats.total === 0 && !isLoading">
          <div class="monitor__empty-icon" aria-hidden="true">
            <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"/>
              <circle cx="12" cy="12" r="3"/>
            </svg>
          </div>
          <p class="monitor__empty-text">No adapters yet. Add one to see health and counts here.</p>
          <a routerLink="/adapters/new" class="action-btn primary">Create adapter</a>
        </div>

        <div class="stats-grid monitor__stats" *ngIf="adapterStats.total > 0">
          <div class="repo-card stat-card">
            <div class="stat-icon stat-icon-primary">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
              </svg>
            </div>
            <div class="stat-content">
              <div class="stat-value">{{ adapterStats.total }}</div>
              <div class="stat-label">Total</div>
              <div class="stat-change">{{ enabledAdapterCount }} enabled</div>
            </div>
          </div>

          <div class="repo-card stat-card">
            <div class="stat-icon stat-icon-success">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                <path d="M22 4L12 14.01l-3-3"/>
              </svg>
            </div>
            <div class="stat-content">
              <div class="stat-value">{{ adapterStats.healthy }}</div>
              <div class="stat-label">Healthy</div>
              <div class="stat-change positive">{{ getPercentage(adapterStats.healthy, adapterStats.total) }}% of total</div>
            </div>
          </div>

          <div class="repo-card stat-card">
            <div class="stat-icon stat-icon-error">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"/>
                <line x1="15" y1="9" x2="9" y2="15"/>
                <line x1="9" y1="9" x2="15" y2="15"/>
              </svg>
            </div>
            <div class="stat-content">
              <div class="stat-value">{{ adapterStats.unhealthy }}</div>
              <div class="stat-label">Unhealthy</div>
              <div class="stat-change" [class.negative]="adapterStats.unhealthy > 0">{{ getPercentage(adapterStats.unhealthy, adapterStats.total) }}% of total</div>
            </div>
          </div>

          <div class="repo-card stat-card">
            <div class="stat-icon stat-icon-warning">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 9v4M12 17h.01M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z"/>
              </svg>
            </div>
            <div class="stat-content">
              <div class="stat-value">{{ adapterStats.disabled }}</div>
              <div class="stat-label">Disabled</div>
              <div class="stat-change">{{ getPercentage(adapterStats.disabled, adapterStats.total) }}% of total</div>
            </div>
          </div>
        </div>
      </section>

      <!-- Loading State -->
      <div class="loading-overlay" *ngIf="isLoading">
        <div class="loading-spinner">
          <div class="loading"></div>
          <p>Loading dashboard data...</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard {
      max-width: 1200px;
      margin: 0 auto;
    }

    .dashboard-header {
      margin-bottom: var(--space-10);
    }

    .dashboard-header__content {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 2rem;
    }

    .header-text {
      flex: 1;
      min-width: 0;
    }

    .dashboard-title {
      margin: 0 0 0.35rem 0;
      font-size: var(--text-3xl);
      font-weight: 700;
      letter-spacing: -0.02em;
      line-height: 1.2;
      color: var(--text-primary);
    }

    .dashboard-subtitle {
      font-size: var(--text-base);
      color: var(--text-secondary);
      margin: 0;
      line-height: 1.5;
    }

    .header-actions {
      display: flex;
      align-items: center;
      gap: 1rem;
      flex-shrink: 0;
      flex-wrap: wrap;
    }

    .repo-icon-btn svg.spinning {
      animation: dashboard-refresh-spin 1s linear infinite;
    }

    @keyframes dashboard-refresh-spin {
      from {
        transform: rotate(0deg);
      }
      to {
        transform: rotate(360deg);
      }
    }

    .monitor {
      display: flex;
      flex-direction: column;
      gap: var(--space-6);
      margin-bottom: var(--space-8);
    }

    .monitor__intro {
      min-width: 0;
    }

    .monitor__title {
      font-size: var(--text-xl);
      font-weight: 700;
      letter-spacing: -0.02em;
      color: var(--text-primary);
      margin: 0 0 var(--space-1);
      line-height: 1.25;
    }

    .monitor__sub {
      margin: 0;
      font-size: var(--text-sm);
      color: var(--text-secondary);
      line-height: 1.5;
      max-width: 36rem;
    }

    .monitor__hero {
      padding: 1.25rem 1.35rem;
      flex-direction: column;
      align-items: stretch;
    }

    .monitor__hero-inner {
      display: grid;
      grid-template-columns: minmax(0, 14rem) minmax(0, 1fr);
      gap: 1.5rem 2rem;
      align-items: start;
    }

    .monitor__hero-stat {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .monitor__hero-value {
      font-size: clamp(2rem, 4vw, 2.75rem);
      font-weight: 700;
      line-height: 1;
      letter-spacing: -0.03em;
      color: var(--text-primary);
    }

    .monitor__hero-label {
      font-size: var(--text-sm);
      color: var(--text-secondary);
    }

    .monitor__hero-meta {
      font-size: var(--text-xs);
      color: var(--text-tertiary);
      margin-top: var(--space-2);
    }

    .monitor__distribution-title {
      margin: 0 0 10px;
      font-size: 0.6875rem;
      font-weight: 600;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      color: var(--text-tertiary);
    }

    .monitor__bar {
      display: flex;
      height: 10px;
      border-radius: 999px;
      overflow: hidden;
      /* Use DevPilot-like subtle edge in dark mode */
      background: rgba(148, 163, 184, 0.08);
    }

    .monitor__bar-seg {
      height: 100%;
      flex-shrink: 0;
      transition: width 0.35s ease;
    }

    .monitor__bar-seg--healthy {
      background: var(--success-500);
    }

    .monitor__bar-seg--unhealthy {
      background: var(--error-500);
    }

    .monitor__bar-seg--disabled {
      background: var(--warning-500);
    }

    body:not(.dark-theme) .monitor__bar {
      background: #e5e7eb;
    }

    body.dark-theme .monitor__bar {
      background: rgba(148, 163, 184, 0.08);
    }

    .monitor__legend {
      list-style: none;
      margin: 12px 0 0;
      padding: 0;
      display: grid;
      gap: 6px;
      font-size: var(--text-sm);
      color: var(--text-secondary);
    }

    .monitor__legend li {
      display: flex;
      align-items: center;
    }

    .monitor__legend strong {
      color: var(--text-primary);
      font-weight: 600;
    }

    .monitor__dot {
      display: inline-block;
      width: 8px;
      height: 8px;
      border-radius: 50%;
      margin-right: 8px;
      vertical-align: middle;
    }

    .monitor__dot--healthy {
      background: var(--success-500);
    }

    .monitor__dot--unhealthy {
      background: var(--error-500);
    }

    .monitor__dot--disabled {
      background: var(--warning-500);
    }

    .monitor__empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      padding: 2.25rem 1.5rem;
      gap: var(--space-3);
    }

    .monitor__empty-icon {
      color: var(--text-tertiary);
      opacity: 0.9;
    }

    .monitor__empty-text {
      margin: 0;
      font-size: var(--text-sm);
      color: var(--text-secondary);
      max-width: 22rem;
      line-height: 1.5;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(min(100%, 260px), 1fr));
      gap: 1rem 1.125rem;
      align-items: stretch;
      width: 100%;
      min-width: 0;
    }

    /* Monitor tiles: global .repo-card (border, hover, fade-in) + horizontal row */
    .repo-card.stat-card {
      flex-direction: row;
      align-items: center;
      gap: var(--space-4);
      padding: 14px 16px;
      height: 100%;
    }

    .stat-icon {
      position: relative;
      z-index: 1;
      width: 3rem;
      height: 3rem;
      border-radius: var(--radius-lg);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      box-shadow: var(--shadow-xs);
      border: 1px solid rgba(148, 163, 184, 0.10);

      svg {
        width: 22px;
        height: 22px;
        flex-shrink: 0;
      }

      &.stat-icon-primary {
        color: var(--brand-primary);
        background: color-mix(in srgb, var(--brand-primary) 14%, var(--surface-card));
      }

      &.stat-icon-success {
        color: var(--success-600);
        background: color-mix(in srgb, var(--success-500) 14%, var(--surface-card));
      }

      &.stat-icon-error {
        color: var(--error-600);
        background: color-mix(in srgb, var(--error-500) 12%, var(--surface-card));
      }

      &.stat-icon-warning {
        color: var(--warning-600);
        background: color-mix(in srgb, var(--warning-500) 12%, var(--surface-card));
      }
    }

    body:not(.dark-theme) .stat-icon {
      border-color: #e5e7eb;
    }

    .stat-content {
      position: relative;
      z-index: 1;
      flex: 1;
      min-width: 0;
    }

    .stat-value {
      font-size: var(--text-3xl);
      font-weight: 700;
      color: var(--text-primary);
      line-height: 1;
      margin-bottom: var(--space-1);
    }

    .stat-label {
      font-size: var(--text-sm);
      color: var(--text-secondary);
      margin-bottom: var(--space-1);
    }

    .stat-change {
      font-size: var(--text-xs);
      color: var(--text-tertiary);

      &.positive {
        color: var(--success-600);
      }

      &.negative {
        color: var(--error-600);
      }
    }

    .loading-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: color-mix(in srgb, var(--surface-ground) 75%, transparent);
      backdrop-filter: blur(6px);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
    }

    .dark-theme .loading-overlay {
      background: color-mix(in srgb, var(--surface-ground) 88%, transparent);
    }

    .loading-spinner {
      text-align: center;

      p {
        margin-top: var(--space-4);
        color: var(--text-secondary);
        font-size: var(--text-sm);
      }
    }

    /* Responsive Design */
    @media (max-width: 1024px) {
      .dashboard-header__content {
        flex-wrap: wrap;
      }

      .header-actions {
        width: 100%;
        justify-content: flex-end;
      }

      .header-actions__buttons {
        justify-content: flex-end;
      }
    }

    @media (max-width: 768px) {
      .dashboard-header__content {
        flex-direction: column;
        gap: 1.5rem;
      }

      .header-actions {
        width: 100%;
        justify-content: stretch;
      }

      .header-actions__buttons {
        width: 100%;
        flex-wrap: wrap;
      }

      .header-actions__buttons .action-btn {
        flex: 1;
        min-width: min(100%, 140px);
        justify-content: center;
      }

      .header-actions__buttons .repo-icon-btn {
        flex: 0 0 auto;
      }

      .stats-grid {
        grid-template-columns: 1fr;
      }

      .monitor__hero-inner {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class DashboardComponent implements OnInit {
  adapterStats: AdapterList = {
    adapters: [],
    total: 0,
    healthy: 0,
    unhealthy: 0,
    disabled: 0
  };
  
  isLoading = false;

  constructor(
    private adapterService: McpAdapterService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.isLoading = true;
    this.adapterService.getAllAdapters().subscribe({
      next: (data) => {
        this.adapterStats = data;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading dashboard data:', error);
        this.isLoading = false;
      }
    });
  }

  refreshData(): void {
    this.loadDashboardData();
  }

  checkAllHealth(): void {
    this.isLoading = true;
    this.adapterService.checkAllAdaptersHealth().subscribe({
      next: (healthChecks) => {
        console.log('Health checks completed:', healthChecks);
        this.isLoading = false;
        this.loadDashboardData(); // Refresh data to show updated health status
      },
      error: (error) => {
        console.error('Error checking health:', error);
        this.isLoading = false;
      }
    });
  }

  getPercentage(value: number, total: number): number {
    if (total === 0) return 0;
    return Math.round((value / total) * 100);
  }

  get enabledAdapterCount(): number {
    const list = this.adapterStats.adapters;
    if (list?.length) {
      return list.filter((a) => a.enabled).length;
    }
    return Math.max(0, this.adapterStats.total - this.adapterStats.disabled);
  }

  get healthBarAriaLabel(): string {
    const t = this.adapterStats.total;
    if (!t) return 'No adapters';
    return `Health distribution: ${this.adapterStats.healthy} healthy, ${this.adapterStats.unhealthy} unhealthy, ${this.adapterStats.disabled} disabled, of ${t} total`;
  }

  healthSegmentPercent(segment: 'healthy' | 'unhealthy' | 'disabled'): number {
    const t = this.adapterStats.total;
    if (!t) return 0;
    const v =
      segment === 'healthy'
        ? this.adapterStats.healthy
        : segment === 'unhealthy'
          ? this.adapterStats.unhealthy
          : this.adapterStats.disabled;
    return Math.min(100, Math.max(0, (v / t) * 100));
  }

  exportAdapters(): void {
    this.isLoading = true;
    this.adapterService.exportAdaptersToExcel().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `MCP_Adapters_Export_${new Date().toISOString().split('T')[0]}.xlsx`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.isLoading = false;
        this.notificationService.success(
          'Export Successful',
          `${this.adapterStats.total} adapter(s) exported to Excel file`
        );
      },
      error: (error) => {
        console.error('Error exporting adapters:', error);
        this.notificationService.error(
          'Export Failed',
          error.message || 'Failed to export adapters. Please try again.'
        );
        this.isLoading = false;
      }
    });
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (file) {
      this.importAdapters(file);
    }
    // Reset file input
    event.target.value = '';
  }

  importAdapters(file: File): void {
    if (!file.name.endsWith('.xlsx')) {
      this.notificationService.error(
        'Invalid File Type',
        'Please select an Excel (.xlsx) file'
      );
      return;
    }

    this.isLoading = true;
    this.adapterService.importAdaptersFromExcel(file).subscribe({
      next: (result) => {
        this.isLoading = false;
        
        // Build detailed message
        const details: string[] = [];
        
        if (result.validationErrors && result.validationErrors.length > 0) {
          details.push('Validation Warnings: ' + result.validationErrors.length);
        }
        
        if (result.failedAdapters && result.failedAdapters.length > 0) {
          details.push('Failed: ' + result.failedCount);
        }
        
        const detailMessage = details.length > 0 ? details.join(', ') : 'All adapters imported successfully';
        
        // Show appropriate notification
        if (result.successCount > 0 && result.failedCount === 0) {
          this.notificationService.success(
            'Import Successful',
            `${result.successCount} adapter(s) imported. ${detailMessage}`
          );
        } else if (result.successCount > 0) {
          this.notificationService.warning(
            'Import Partially Successful',
            `${result.successCount} adapter(s) imported. ${detailMessage}`
          );
        } else {
          this.notificationService.error(
            'Import Failed',
            `No adapters were imported. ${detailMessage}`
          );
        }
        
        // Log details to console
        if (result.validationErrors && result.validationErrors.length > 0) {
          console.warn('Validation errors:', result.validationErrors);
        }
        if (result.failedAdapters && result.failedAdapters.length > 0) {
          console.error('Failed adapters:', result.failedAdapters);
        }
        
        // Refresh dashboard if any adapters were imported
        if (result.successCount > 0) {
          this.loadDashboardData();
        }
      },
      error: (error) => {
        console.error('Error importing adapters:', error);
        this.notificationService.error(
          'Import Failed',
          error.message || 'Failed to import adapters. Please try again.'
        );
        this.isLoading = false;
      }
    });
  }
}